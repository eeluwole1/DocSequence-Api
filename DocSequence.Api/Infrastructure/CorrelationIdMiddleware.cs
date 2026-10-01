namespace DocSequence.Api.Infrastructure;

// SRS §13.5: every response carries X-Correlation-ID; a safe incoming value is propagated,
// otherwise one is created. The same ID goes into ProblemDetails and the log scope.
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = IsSafe(incoming) ? incoming! : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;

        // OnStarting survives the response reset the exception handler performs
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    // Reject oversized or odd values so a client can't inject junk into our logs
    private static bool IsSafe(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 64
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}