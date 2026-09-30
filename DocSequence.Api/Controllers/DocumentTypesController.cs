using DocSequence.Api.Contracts;
using DocSequence.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocSequence.Api.Controllers;

[ApiController]
[Route("api/document-types")]
public sealed class DocumentTypesController(AppDbContext db) : ControllerBase
{
    // Active types only; inactive ones cannot allocate (SRS §13)
    [HttpGet]
    public Task<List<DocumentTypeResponse>> GetActive(CancellationToken ct) =>
        db.DocumentTypes.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Prefix)
            .Select(t => new DocumentTypeResponse(t.DocumentTypeId, t.Prefix, t.Name))
            .ToListAsync(ct);
}