using System.Net.Http.Json;
using DocSequence.Api.Contracts;
using DocSequence.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DocSequence.Tests.Infrastructure;

public abstract class ApiTestBase(DocSequenceApiFactory factory)
{
    protected const int Cxy = 1;   // seeded in the migration
    protected const int Pxy = 2;

    protected DocSequenceApiFactory Factory { get; } = factory;
    protected HttpClient Client { get; } = factory.CreateClient();

    protected Task<HttpResponseMessage> AllocateAsync(
        int documentTypeId, Guid? requestKey = null,
        string documentName = "Test drawing", string engineerName = "Test Engineer") =>
        Client.PostAsJsonAsync("/api/document-numbers", new
        {
            documentTypeId,
            documentName,
            engineerName,
            requestKey = requestKey ?? Guid.NewGuid()
        });

    protected static async Task<AllocationResponse> ReadAllocationAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<AllocationResponse>())!;

    // Tests compare against the counter's current value rather than assuming 10428,
    // so they don't depend on which tests ran first.
    protected async Task<long> CurrentNumberAsync(int documentTypeId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.DocumentTypes
            .Where(t => t.DocumentTypeId == documentTypeId)
            .Select(t => t.CurrentNumber)
            .SingleAsync();
    }
}