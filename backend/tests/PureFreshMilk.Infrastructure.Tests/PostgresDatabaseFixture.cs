using Microsoft.EntityFrameworkCore;
using Npgsql;
using PureFreshMilk.Infrastructure.Persistence;

namespace PureFreshMilk.Infrastructure.Tests;

/// <summary>
/// Creates a throwaway PostgreSQL database for one test class, builds it from the migrations, and drops it
/// afterwards. The server comes from <c>ConnectionStrings__PureFreshMilkTests</c>: the same Compose PostgreSQL
/// that local development uses. A missing setting is a failure, never a silent skip, so these tests cannot
/// quietly stop running in CI.
/// </summary>
public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    public const string ConnectionStringVariable = "ConnectionStrings__PureFreshMilkTests";

    private string serverConnectionString = string.Empty;
    private string databaseName = string.Empty;

    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException(
                $"The database tests need PostgreSQL. Start it with 'docker compose up -d --wait' and set {ConnectionStringVariable} " +
                "to its connection string (see docs/development.md). To run everything else, exclude the 'Database' category.");
        }

        serverConnectionString = configured;
        databaseName = $"purefreshmilk_test_{Guid.NewGuid():N}";

        await ExecuteOnServerAsync($"CREATE DATABASE \"{databaseName}\"");

        ConnectionString = new NpgsqlConnectionStringBuilder(configured) { Database = databaseName }.ConnectionString;

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (databaseName.Length == 0)
        {
            return;
        }

        NpgsqlConnection.ClearAllPools();
        await ExecuteOnServerAsync($"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)");
    }

    internal PureFreshMilkDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<PureFreshMilkDbContext>().UseNpgsql(ConnectionString).Options);

    private async Task ExecuteOnServerAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(serverConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
