using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace DocSequence.Api.Contracts;

// SRS §13.3. From/To are bound as strings on purpose: the default DateTimeOffset
// binder assumes the *server's* time zone for values without an offset, but the
// SRS says offset-less timestamps are UTC (AC-023).
public sealed class HistoryQuery : IValidatableObject
{
    public int? DocumentTypeId { get; set; }
    public string? Engineer { get; set; }
    public string? Identifier { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }

    [Range(1, 100_000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public DateTime? FromUtc => ParseUtc(From);
    public DateTime? ToUtc => ParseUtc(To);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var fromValid = From is null || TryParseUtc(From, out _);
        var toValid = To is null || TryParseUtc(To, out _);

        if (!fromValid)
            yield return new ValidationResult("from must be an ISO-8601 timestamp.", [nameof(From)]);
        if (!toValid)
            yield return new ValidationResult("to must be an ISO-8601 timestamp.", [nameof(To)]);
        if (fromValid && toValid && FromUtc > ToUtc)
            yield return new ValidationResult("from must not be later than to.", [nameof(From), nameof(To)]);
    }

    private static bool TryParseUtc(string value, out DateTime utc)
    {
        var parsed = DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out var result);
        utc = result.UtcDateTime;
        return parsed;
    }

    private static DateTime? ParseUtc(string? value) =>
        value is not null && TryParseUtc(value, out var utc) ? utc : null;
}