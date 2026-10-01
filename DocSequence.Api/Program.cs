using System.Threading.RateLimiting;
using DocSequence.Api.Data;
using DocSequence.Api.Infrastructure;
using DocSequence.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Every ProblemDetails (validation, 404, 409, 429, 500, 503) carries the correlation ID (SRS §13.5)
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
        ctx.ProblemDetails.Extensions["correlationId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// No EnableRetryOnFailure: it conflicts with the explicit transactions used for allocation
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<AllocationService>();
builder.Services.AddScoped<HistoryService>();

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

// Rate limiting (NFR-009). Settings are read per request so tests can switch it off (AC-020).
builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(RateLimitSettings.GeneratePolicy, httpContext =>
    {
        var settings = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value;
        if (!settings.Enabled)
            return RateLimitPartition.GetNoLimiter("disabled");

        return RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = settings.PermitLimit,
                Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                QueueLimit = 0
            });
    });

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = context.HttpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = "Generation rate limit exceeded. Wait a moment and try again."
                }
            });
    };
});

// CORS for the separately hosted Angular app (SRS §21). Empty locally: the dev proxy needs no CORS.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .WithMethods("GET", "POST")
    .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();   // first, so even error responses get the header
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // Local development runs over plain http behind the Angular proxy; redirecting
    // to https there would bounce proxied requests to another port. Production enforces HTTPS.
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }