using DocSequence.Api.Data;
using Microsoft.EntityFrameworkCore;
using DocSequence.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<AllocationService>();
builder.Services.AddScoped<HistoryService>();

var app = builder.Build();

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

app.MapControllers();

app.Run();

public partial class Program { }