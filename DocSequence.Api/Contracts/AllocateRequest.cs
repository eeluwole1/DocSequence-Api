namespace DocSequence.Api.Contracts;

public sealed record AllocateRequest(
    int DocumentTypeId,
    string DocumentName,
    string EngineerName,
    Guid RequestKey);