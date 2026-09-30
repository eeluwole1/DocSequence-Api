using DocSequence.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Microsoft.Extensions.Logging;

namespace DocSequence.Tests.Infrastructure;

// Starts a throwaway SQL Server in Docker, applies the real migrations,
// and hosts the API in memory against it. EF InMemory would not reproduce
// SQL Server locking, rollback or constraints (SRS §20).
public sealed class DocSequenceApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string ConnectionString { get; private set; } = "";

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        // The container's default database is master; use our own. Extra pool
        // headroom because the concurrency test holds ~100 connections at once.
        ConnectionString = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = "DocSequence_Tests",
            MaxPoolSize = 200
        }.ConnectionString;

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");   // skips appsettings.Development.json (LocalDB)
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.ConfigureLogging(logging => logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
    }
}