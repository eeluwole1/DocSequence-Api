using System.Net;
using DocSequence.Tests.Infrastructure;
using Shouldly;

namespace DocSequence.Tests;

[Collection(ApiCollection.Name)]
public sealed class AllocationApiTests(DocSequenceApiFactory factory) : ApiTestBase(factory)
{
    [Fact] // AC-001
    public async Task Allocate_returns_201_with_next_number_and_location()
    {
        var start = await CurrentNumberAsync(Cxy);

        var response = await AllocateAsync(Cxy);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var allocation = await ReadAllocationAsync(response);
        allocation.Number.ShouldBe(start + 1);
        allocation.GeneratedIdentifier.ShouldBe($"CXY-{start + 1}");
        response.Headers.Location!.AbsolutePath.ShouldBe($"/api/document-numbers/{allocation.AllocationId}");
        (await CurrentNumberAsync(Cxy)).ShouldBe(start + 1);
    }

    [Fact] // AC-002
    public async Task Each_type_advances_only_its_own_sequence()
    {
        var cxyStart = await CurrentNumberAsync(Cxy);
        var pxyStart = await CurrentNumberAsync(Pxy);

        (await AllocateAsync(Pxy)).StatusCode.ShouldBe(HttpStatusCode.Created);

        (await CurrentNumberAsync(Pxy)).ShouldBe(pxyStart + 1);
        (await CurrentNumberAsync(Cxy)).ShouldBe(cxyStart);
    }

    [Fact] // AC-011, SRS §10.4 (names are compared after trimming)
    public async Task Retry_with_same_key_and_equivalent_body_replays_original()
    {
        var key = Guid.NewGuid();
        var original = await ReadAllocationAsync(await AllocateAsync(Cxy, key, "Pump drawing", "Ada"));
        var start = await CurrentNumberAsync(Cxy);

        var retry = await AllocateAsync(Cxy, key, "  Pump drawing  ", " Ada ");

        retry.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadAllocationAsync(retry)).AllocationId.ShouldBe(original.AllocationId);
        (await CurrentNumberAsync(Cxy)).ShouldBe(start);
    }

    [Fact] // AC-012
    public async Task Same_key_with_different_body_returns_409_without_allocating()
    {
        var key = Guid.NewGuid();
        await AllocateAsync(Cxy, key, documentName: "Original drawing");
        var start = await CurrentNumberAsync(Cxy);

        var response = await AllocateAsync(Cxy, key, documentName: "Changed drawing");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await CurrentNumberAsync(Cxy)).ShouldBe(start);
    }

    [Fact] // AC-006
    public async Task Unknown_type_returns_404_and_changes_no_sequence()
    {
        var cxyStart = await CurrentNumberAsync(Cxy);
        var pxyStart = await CurrentNumberAsync(Pxy);

        (await AllocateAsync(999)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await CurrentNumberAsync(Cxy)).ShouldBe(cxyStart);
        (await CurrentNumberAsync(Pxy)).ShouldBe(pxyStart);
    }

    [Theory] // AC-007
    [InlineData("", "Test Engineer")]
    [InlineData("   ", "Test Engineer")]
    [InlineData("Drawing", "   ")]
    public async Task Blank_names_return_400_without_allocating(string documentName, string engineerName)
    {
        var start = await CurrentNumberAsync(Cxy);

        var response = await AllocateAsync(Cxy, documentName: documentName, engineerName: engineerName);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await CurrentNumberAsync(Cxy)).ShouldBe(start);
    }

    [Fact] // AC-007: FR-001 maximum is 200 after trimming
    public async Task Overlong_document_name_returns_400()
    {
        var response = await AllocateAsync(Cxy, documentName: new string('x', 201));
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact] // AC-007: requestKey must be a non-empty UUID
    public async Task Empty_request_key_returns_400()
    {
        var response = await AllocateAsync(Cxy, requestKey: Guid.Empty);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}