namespace DocSequence.Tests.Infrastructure;

// All API test classes share one container (fast) and run one after another
// (so one class's allocations can't interfere with another's counter checks).
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<DocSequenceApiFactory>
{
    public const string Name = "api";
}