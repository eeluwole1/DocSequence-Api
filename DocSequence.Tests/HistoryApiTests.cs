using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using DocSequence.Api.Contracts;
using DocSequence.Tests.Infrastructure;
using Shouldly;

namespace DocSequence.Tests;

[Collection(ApiCollection.Name)]
public sealed class HistoryApiTests(DocSequenceApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Document_types_lists_active_types_ordered_by_prefix()
    {
        var types = await Client.GetFromJsonAsync<List<DocumentTypeResponse>>("/api/document-types");

        types!.Select(t => t.Prefix).ToArray().ShouldBe(new[] { "CXY", "PXY" });
    }

    [Fact] // AC-009, AC-010: engineer filter is case-insensitive; newest first
    public async Task Engineer_filter_returns_matching_rows_newest_first()
    {
        var engineer = NewEngineer();
        var first = await CreateAsync(Cxy, engineer);
        var second = await CreateAsync(Cxy, engineer);
        var third = await CreateAsync(Pxy, engineer);

        var page = await GetHistoryAsync($"engineer={engineer.ToUpperInvariant()}");

        page.TotalCount.ShouldBe(3);
        page.Items.Select(i => i.AllocationId).ToArray()
            .ShouldBe(new[] { third.AllocationId, second.AllocationId, first.AllocationId });
        page.Items[0].GeneratedIdentifier.ShouldBe(third.GeneratedIdentifier);
    }

    [Fact] // AC-010: paging envelope
    public async Task Paging_splits_results_and_reports_totals()
    {
        var engineer = NewEngineer();
        for (var i = 0; i < 3; i++)
            await CreateAsync(Cxy, engineer);

        var page1 = await GetHistoryAsync($"engineer={engineer}&pageSize=2&page=1");
        var page2 = await GetHistoryAsync($"engineer={engineer}&pageSize=2&page=2");

        page1.Items.Count.ShouldBe(2);
        page1.TotalCount.ShouldBe(3);
        page1.TotalPages.ShouldBe(2);
        page2.Items.Count.ShouldBe(1);
        page1.Items.Select(i => i.AllocationId).ShouldNotContain(page2.Items[0].AllocationId);
    }

    [Fact] // AC-010: type filter
    public async Task Type_filter_returns_only_that_type()
    {
        var engineer = NewEngineer();
        var pxy = await CreateAsync(Pxy, engineer);
        await CreateAsync(Cxy, engineer);

        var page = await GetHistoryAsync($"engineer={engineer}&documentTypeId={Pxy}");

        page.Items.ShouldHaveSingleItem().GeneratedIdentifier.ShouldBe(pxy.GeneratedIdentifier);
    }

    [Fact] // AC-010: identifier search is a case-insensitive substring match
    public async Task Identifier_search_is_case_insensitive()
    {
        var created = await CreateAsync(Cxy, NewEngineer());

        var page = await GetHistoryAsync($"identifier={created.GeneratedIdentifier.ToLowerInvariant()}");

        page.Items.ShouldContain(i => i.AllocationId == created.AllocationId);
    }

    [Fact] // AC-023: a timestamp without an offset is treated as UTC
    public async Task Offsetless_dates_are_treated_as_utc()
    {
        var engineer = NewEngineer();
        var created = await CreateAsync(Cxy, engineer);
        var oneSecondBefore = created.CreatedAt.AddSeconds(-1)
            .ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture);   // no 'Z', no offset

        (await GetHistoryAsync($"engineer={engineer}&from={oneSecondBefore}")).TotalCount.ShouldBe(1);
        (await GetHistoryAsync($"engineer={engineer}&to={oneSecondBefore}")).TotalCount.ShouldBe(0);
    }

    [Theory] // AC-023 and paging limits
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=0")]
    [InlineData("from=not-a-date")]
    [InlineData("from=2026-09-30T10:00:00Z&to=2026-09-29T10:00:00Z")]
    public async Task Invalid_query_returns_400(string queryString)
    {
        var response = await Client.GetAsync($"/api/document-numbers?{queryString}");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static string NewEngineer() => $"eng-{Guid.NewGuid():N}";

    private async Task<AllocationResponse> CreateAsync(int documentTypeId, string engineer) =>
        await ReadAllocationAsync(await AllocateAsync(documentTypeId, engineerName: engineer));

    private async Task<PagedResponse<AllocationSummary>> GetHistoryAsync(string queryString) =>
        (await Client.GetFromJsonAsync<PagedResponse<AllocationSummary>>($"/api/document-numbers?{queryString}"))!;
}