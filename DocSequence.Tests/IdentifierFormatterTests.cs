using DocSequence.Api.Services;
using Shouldly;

namespace DocSequence.Tests;

public sealed class IdentifierFormatterTests
{
    [Theory] // AC-014, BR-009
    [InlineData(1L, "CXY-0001")]
    [InlineData(42L, "CXY-0042")]
    [InlineData(10428L, "CXY-10428")]
    [InlineData(123456L, "CXY-123456")]
    public void Pads_to_four_digits_and_never_truncates(long number, string expected) =>
        IdentifierFormatter.Format("CXY", number).ShouldBe(expected);
}