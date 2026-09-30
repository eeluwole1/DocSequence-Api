using System.Net;
using DocSequence.Tests.Infrastructure;
using Shouldly;

namespace DocSequence.Tests;

[Collection(ApiCollection.Name)]
public sealed class ConcurrencyTests(DocSequenceApiFactory factory) : ApiTestBase(factory)
{
    [Fact] // AC-004, NFR-003
    public async Task Hundred_parallel_same_type_requests_get_distinct_contiguous_numbers()
    {
        const int count = 100;
        var start = await CurrentNumberAsync(Cxy);

        var responses = await Task.WhenAll(Enumerable.Range(0, count).Select(_ => AllocateAsync(Cxy)));

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.Created);
        var numbers = await Task.WhenAll(responses.Select(async r => (await ReadAllocationAsync(r)).Number));
        numbers.Order().ToArray().ShouldBe(Enumerable.Range(1, count).Select(i => start + i).ToArray());
        (await CurrentNumberAsync(Cxy)).ShouldBe(start + count);
    }

    [Fact] // AC-017, FR-015
    public async Task Parallel_requests_with_one_key_produce_exactly_one_allocation()
    {
        const int count = 10;
        var key = Guid.NewGuid();
        var start = await CurrentNumberAsync(Cxy);

        var responses = await Task.WhenAll(Enumerable.Range(0, count).Select(_ => AllocateAsync(Cxy, key)));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(count - 1);
        var ids = await Task.WhenAll(responses.Select(async r => (await ReadAllocationAsync(r)).AllocationId));
        ids.Distinct().Count().ShouldBe(1);
        (await CurrentNumberAsync(Cxy)).ShouldBe(start + 1);   // losers' increments were rolled back
    }
}