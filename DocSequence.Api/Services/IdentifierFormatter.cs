namespace DocSequence.Api.Services;

public static class IdentifierFormatter
{
    // BR-009: pad to at least 4 digits, never truncate (1 -> CXY-0001, 10428 -> CXY-10428)
    public static string Format(string prefix, long number) => $"{prefix}-{number:D4}";
}