using DocSequence.Api.Contracts;
using DocSequence.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DocSequence.Api.Services;

public sealed class HistoryService(AppDbContext db)
{
    // SRS §13.3: optional filters, newest first, deterministic paging
    public async Task<PagedResponse<AllocationSummary>> SearchAsync(HistoryQuery query, CancellationToken ct)
    {
        var documents = db.GeneratedDocuments.AsNoTracking();

        if (query.DocumentTypeId is int documentTypeId)
            documents = documents.Where(d => d.DocumentTypeId == documentTypeId);

        // Contains -> LIKE; case-insensitive under the database's default collation
        if (!string.IsNullOrWhiteSpace(query.Engineer))
        {
            var engineer = query.Engineer.Trim();
            documents = documents.Where(d => d.EngineerName.Contains(engineer));
        }

        if (!string.IsNullOrWhiteSpace(query.Identifier))
        {
            var identifier = query.Identifier.Trim();
            documents = documents.Where(d => d.Identifier.Contains(identifier));
        }

        if (query.FromUtc is DateTime fromUtc)
            documents = documents.Where(d => d.CreatedAt >= fromUtc);

        if (query.ToUtc is DateTime toUtc)
            documents = documents.Where(d => d.CreatedAt <= toUtc);

        var totalCount = await documents.CountAsync(ct);

        var items = await documents
            .OrderByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.GeneratedDocumentId)   // tie-breaker so pages never overlap
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new AllocationSummary(
                d.GeneratedDocumentId, d.Identifier, d.DocumentTypeId, d.DocumentType.Prefix, d.Number,
                d.DocumentName, d.EngineerName, DateTime.SpecifyKind(d.CreatedAt, DateTimeKind.Utc)))
            .ToListAsync(ct);

        var totalPages = (totalCount + query.PageSize - 1) / query.PageSize;
        return new PagedResponse<AllocationSummary>(items, query.Page, query.PageSize, totalCount, totalPages);
    }
}