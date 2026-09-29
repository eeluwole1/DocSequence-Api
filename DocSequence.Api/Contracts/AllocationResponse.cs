namespace DocSequence.Api.Contracts;

// Shape from SRS §13.2
public sealed record AllocationResponse(
    long AllocationId,
    int DocumentTypeId,
    string Prefix,
    long Number,
    string GeneratedIdentifier,
    string DocumentName,
    string EngineerName,
    Guid RequestKey,
    DateTime CreatedAt);