using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DocSequence.Tests.Infrastructure;

// Hosts the API against a SQL Server address where nothing is listening (port 1),
// simulating a database outage without Docker (AC-013).
public sealed class UnreachableDatabaseFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default",
            "Server=tcp:127.0.0.1,1;Database=Unreachable;User Id=sa;Password=NotUsed1!;TrustServerCertificate=True;Connect Timeout=3");
    }
}