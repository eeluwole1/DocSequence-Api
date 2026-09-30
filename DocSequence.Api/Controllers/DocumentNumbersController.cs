using DocSequence.Api.Contracts;
using DocSequence.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocSequence.Api.Controllers;

[ApiController]
[Route("api/document-numbers")]
public sealed class DocumentNumbersController(AllocationService allocationService, HistoryService historyService) : ControllerBase   // ① added HistoryService
{
    // SRS §13.6: 201 new, 200 replay, 404 unknown/inactive type, 409 key conflict; 400 is automatic via [ApiController]
    [HttpPost]
    public async Task<IActionResult> Allocate(AllocateRequest request, CancellationToken ct)
    {
        var result = await allocationService.AllocateAsync(request, ct);

        return result.Status switch
        {
            AllocationStatus.Created => CreatedAtAction(nameof(GetById), new { id = result.Allocation!.AllocationId }, result.Allocation),
            AllocationStatus.Replayed => Ok(result.Allocation),
            AllocationStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Document type not found",
                detail: "The selected document type does not exist or is inactive. No number was allocated."),
            AllocationStatus.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Request key already used",
                detail: "This requestKey was already used with different request data. No number was allocated."),
            _ => throw new InvalidOperationException($"Unhandled allocation status {result.Status}")
        };
    }

    // ② NEW. SRS §13.3-13.4. [FromQuery] is required: [ApiController] would otherwise expect a JSON body.
    [HttpGet]
    public Task<PagedResponse<AllocationSummary>> GetHistory([FromQuery] HistoryQuery query, CancellationToken ct) =>
        historyService.SearchAsync(query, ct);

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AllocationResponse>> GetById(long id, CancellationToken ct)
    {
        var allocation = await allocationService.FindByIdAsync(id, ct);
        return allocation is null ? NotFound() : allocation;
    }
}