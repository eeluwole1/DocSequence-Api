namespace DocSequence.Api.Infrastructure;

// Bound from the "RateLimiting" config section (NFR-009, NFR-012)
public sealed class RateLimitSettings
{
    public const string GeneratePolicy = "generate";

    public bool Enabled { get; set; } = true;
    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 60;
}