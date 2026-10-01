using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DocSequence.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace DocSequence.Tests;

[Collection(ApiCollection.Name)]
public sealed class OperationalTests(DocSequenceApiFactory factory) : ApiTestBase(factory)
{
    private const string CorrelationHeader = "X-Correlation-ID";

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        var response = await Client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Fact] // AC-018
    public async Task Supplied_correlation_id_is_echoed_in_header_and_problem_details()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/document-numbers/999999999");
        request.Headers.Add(CorrelationHeader, "test-corr-123");

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        response.Headers.GetValues(CorrelationHeader).Single().ShouldBe("test-corr-123");
        (await ReadCorrelationIdAsync(response)).ShouldBe("test-corr-123");
    }

    [Fact] // AC-018
    public async Task Correlation_id_is_generated_when_missing_and_matches_the_body()
    {
        var response = await AllocateAsync(Cxy, documentName: "   "); // invalid -> 400 ValidationProblemDetails

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var header = response.Headers.GetValues(CorrelationHeader).Single();
        header.ShouldNotBeNullOrWhiteSpace();
        (await ReadCorrelationIdAsync(response)).ShouldBe(header);
    }

    [Fact] // Log-injection guard in CorrelationIdMiddleware
    public async Task Unsafe_correlation_id_is_replaced()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/document-types");
        request.Headers.TryAddWithoutValidation(CorrelationHeader, "not safe!");

        var response = await Client.SendAsync(request);

        response.Headers.GetValues(CorrelationHeader).Single().ShouldNotBe("not safe!");
    }

    [Fact] // AC-020: limiter can be enabled by configuration; reads and health are never limited
    public async Task Generation_is_rate_limited_when_enabled_but_reads_and_health_are_not()
    {
        // Same Docker database, but a host with a tiny limit switched on
        await using var limited = Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Enabled"] = "true",
                ["RateLimiting:PermitLimit"] = "2",
                ["RateLimiting:WindowSeconds"] = "60",
            })));
        var client = limited.CreateClient();

        var first = await PostAllocation(client);
        var second = await PostAllocation(client);
        var third = await PostAllocation(client);

        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        second.StatusCode.ShouldBe(HttpStatusCode.Created);
        third.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        third.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");

        (await client.GetAsync("/api/document-numbers?pageSize=1")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/health")).StatusCode.ShouldBe(HttpStatusCode.OK);

        static Task<HttpResponseMessage> PostAllocation(HttpClient client) =>
            client.PostAsJsonAsync("/api/document-numbers", new
            {
                documentTypeId = Cxy,
                documentName = "Rate limit test",
                engineerName = "Test Engineer",
                requestKey = Guid.NewGuid()
            });
    }

    private static async Task<string?> ReadCorrelationIdAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("correlationId").GetString();
    }
}