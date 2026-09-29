using DocSequence.Api.Contracts;

namespace DocSequence.Api.Services;

public enum AllocationStatus { Created, Replayed, NotFound, Conflict }

public sealed record AllocationResult(AllocationStatus Status, AllocationResponse? Allocation = null)
{
    public static AllocationResult Created(AllocationResponse a) => new(AllocationStatus.Created, a);
    public static AllocationResult Replayed(AllocationResponse a) => new(AllocationStatus.Replayed, a);
    public static AllocationResult NotFound() => new(AllocationStatus.NotFound);
    public static AllocationResult Conflict() => new(AllocationStatus.Conflict);
}