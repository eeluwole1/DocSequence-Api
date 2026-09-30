using System.ComponentModel.DataAnnotations;
using DocSequence.Api.Validation;

namespace DocSequence.Api.Contracts;

public sealed record AllocateRequest(
    [Range(1, int.MaxValue)] int DocumentTypeId,
    [Required, TrimmedLength(1, 200)] string DocumentName,
    [Required, TrimmedLength(1, 100)] string EngineerName,
    [NotEmptyGuid] Guid RequestKey);