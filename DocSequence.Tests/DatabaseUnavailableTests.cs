using System.Net;
using System.Net.Http.Json;
using DocSequence.Tests.Infrastructure;
using Shouldly;

namespace DocSequence.Tests;

public sealed class DatabaseUnavailableTests(UnreachableDatabaseFactory factory) : IClassFixture<UnreachableDatabaseFactory>
{
    [Fact] // AC-013, NFR-005: explicit error, never a fabricated identifier
    public async Task Allocation_returns_503_problem_details_without_an_identifier()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/document-numbers", new
        {
            documentTypeId = 1,
            documentName = "Outage test",
            engineerName = "Test Engineer",
            requestKey = Guid.NewGuid()
        });

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("No number was allocated");
        body.ShouldNotContain("generatedIdentifier");
    }

    [Fact]
    public async Task Health_reports_unhealthy_when_database_is_unreachable()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }
}