using DocSequence.Api.Contracts;
using DocSequence.Api.Data;
using DocSequence.Api.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DocSequence.Api.Services;

public sealed class AllocationService(AppDbContext db, ILogger<AllocationService> logger)
{
    public const string RequestKeyConstraint = "UX_GeneratedDocuments_RequestKey";

    public async Task<AllocationResult> AllocateAsync(AllocateRequest request, CancellationToken ct)
    {
        var documentName = request.DocumentName.Trim();
        var engineerName = request.EngineerName.Trim();

        // 1. Idempotency pre-check (SRS §10.2). Fast path for ordinary retries;
        //    races are settled by the unique constraint in step 4.
        var existing = await FindByRequestKeyAsync(request.RequestKey, ct);
        if (existing is not null)
            return ReplayOrConflict(existing, request.DocumentTypeId, documentName, engineerName);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // 2. Atomic per-type increment (SRS §10.1). The row lock serializes same-type
        //    requests; different types lock different rows. ToListAsync (not First)
        //    so EF sends the SQL as-is instead of wrapping it in a subquery.
        var rows = await db.Database.SqlQuery<AllocatedRow>($"""
            UPDATE DocumentTypes
            SET CurrentNumber = CurrentNumber + 1, UpdatedAt = SYSUTCDATETIME()
            OUTPUT inserted.CurrentNumber AS Number, inserted.Prefix AS Prefix
            WHERE DocumentTypeId = {request.DocumentTypeId} AND IsActive = 1
            """).ToListAsync(ct);

        if (rows.Count == 0)
        {
            await tx.RollbackAsync(ct);
            return AllocationResult.NotFound();   // unknown or inactive type; nothing changed
        }

        var (number, prefix) = (rows[0].Number, rows[0].Prefix);

        // 3. Insert the allocation in the same transaction
        var document = new GeneratedDocument
        {
            DocumentTypeId = request.DocumentTypeId,
            Number = number,
            Identifier = IdentifierFormatter.Format(prefix, number),
            DocumentName = documentName,
            EngineerName = engineerName,
            RequestKey = request.RequestKey
        };
        db.GeneratedDocuments.Add(document);

        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return AllocationResult.Created(ToResponse(document, prefix));
        }
        catch (DbUpdateException ex) when (ViolatesConstraint(ex, RequestKeyConstraint))
        {
            // 4. Lost a same-RequestKey race (SRS §10.2-10.3): roll back (including the
            //    counter increment), clear the failed entity, reload the winner.
            await tx.RollbackAsync(ct);
            db.ChangeTracker.Clear();

            var winner = await FindByRequestKeyAsync(request.RequestKey, ct);
            return ReplayOrConflict(winner!, request.DocumentTypeId, documentName, engineerName);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Type+Number or Identifier collision should be impossible; treat as a defect (SRS §10.3)
            logger.LogCritical(ex, "Business-number integrity violation for type {DocumentTypeId}, number {Number}",
                request.DocumentTypeId, number);
            throw;   // rolled back on dispose; becomes 500 in the global handler (Step 8)
        }
    }

    // SRS §10.4: exact type id, ordinal comparison of trimmed names
    private static AllocationResult ReplayOrConflict(
        AllocationResponse existing, int documentTypeId, string documentName, string engineerName)
    {
        var equivalent = existing.DocumentTypeId == documentTypeId
            && string.Equals(existing.DocumentName, documentName, StringComparison.Ordinal)
            && string.Equals(existing.EngineerName, engineerName, StringComparison.Ordinal);

        return equivalent ? AllocationResult.Replayed(existing) : AllocationResult.Conflict();
    }

    private Task<AllocationResponse?> FindByRequestKeyAsync(Guid requestKey, CancellationToken ct) =>
        db.GeneratedDocuments.AsNoTracking()
            .Where(d => d.RequestKey == requestKey)
            .Select(d => new AllocationResponse(
                d.GeneratedDocumentId, d.DocumentTypeId, d.DocumentType.Prefix, d.Number, d.Identifier,
                d.DocumentName, d.EngineerName, d.RequestKey,
                DateTime.SpecifyKind(d.CreatedAt, DateTimeKind.Utc)))
            .FirstOrDefaultAsync(ct);

    private static AllocationResponse ToResponse(GeneratedDocument d, string prefix) => new(
        d.GeneratedDocumentId, d.DocumentTypeId, prefix, d.Number, d.Identifier,
        d.DocumentName, d.EngineerName, d.RequestKey,
        DateTime.SpecifyKind(d.CreatedAt, DateTimeKind.Utc));   // SQL Server returns Unspecified; it's UTC

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };

    private static bool ViolatesConstraint(DbUpdateException ex, string constraintName) =>
        IsUniqueViolation(ex) && ex.InnerException!.Message.Contains(constraintName, StringComparison.Ordinal);

    private sealed class AllocatedRow
    {
        public long Number { get; set; }
        public string Prefix { get; set; } = "";
    }
}