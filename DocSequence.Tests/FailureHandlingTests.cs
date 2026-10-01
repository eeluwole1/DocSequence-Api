using DocSequence.Api.Data;
using DocSequence.Tests.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace DocSequence.Tests;

[Collection(ApiCollection.Name)]
public sealed class FailureHandlingTests(DocSequenceApiFactory factory) : ApiTestBase(factory)
{
    [Fact] // AC-015, Appendix A.4: a failure after the counter UPDATE must not consume a number
    public async Task Failure_after_counter_update_rolls_back_and_consumes_no_number()
    {
        var start = await CurrentNumberAsync(Cxy);

        // A host whose SaveChanges always fails, i.e. after UPDATE ... OUTPUT has run in the transaction
        await using var failing = Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.ConfigureDbContext<AppDbContext>(options => options.AddInterceptors(new FailingSaveInterceptor()))));

        var response = await failing.CreateClient().PostAsJsonAsync("/api/document-numbers", new
        {
            documentTypeId = Cxy,
            documentName = "Rollback test",
            engineerName = "Test Engineer",
            requestKey = Guid.NewGuid()
        });

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        (await CurrentNumberAsync(Cxy)).ShouldBe(start);            // the UPDATE was rolled back

        (await AllocateAsync(Cxy)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await CurrentNumberAsync(Cxy)).ShouldBe(start + 1);        // next request receives the "lost" number
    }

    [Fact] // AC-022: a business-number collision is a defect (500), not a client conflict (409)
    public async Task Business_number_collision_returns_500_and_rolls_back()
    {
        var start = await CurrentNumberAsync(Cxy);
        var planted = start + 1;                                      // the number the next allocation will try to use
        var marker = $"TEST-COLLISION-{Guid.NewGuid():N}"[..30];      // Identifier column is varchar(30)

        await ExecuteSqlAsync($"""
            INSERT INTO GeneratedDocuments (DocumentTypeId, Number, Identifier, DocumentName, EngineerName, RequestKey)
            VALUES ({Cxy}, {planted}, {marker}, 'Planted', 'Test', NEWID())
            """);
        try
        {
            var response = await AllocateAsync(Cxy);

            response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
            (await CurrentNumberAsync(Cxy)).ShouldBe(start);         // counter increment rolled back
        }
        finally
        {
            await ExecuteSqlAsync($"DELETE FROM GeneratedDocuments WHERE Identifier = {marker}");
        }
    }

    private sealed class FailingSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated failure after the counter update (AC-015)");
    }
}