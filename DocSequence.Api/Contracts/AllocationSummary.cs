namespace DocSequence.Api.Contracts;

// One history row (SRS §13.4)
public sealed record AllocationSummary(
    long AllocationId,
    string GeneratedIdentifier,
    int DocumentTypeId,
    string Prefix,
    long Number,
    string DocumentName,
    string EngineerName,
    DateTime CreatedAt);