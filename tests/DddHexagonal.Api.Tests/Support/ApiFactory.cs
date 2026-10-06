using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MySqlConnector;

namespace DddHexagonal.Api.Tests.Support;

/// <summary>
/// Boots the REAL application (all layers, real MySQL) in memory. One throw-away database per factory,
/// created by the app itself through <c>Database:MigrateOnStartup</c>. Needs TEST_MYSQL_CONNECTION.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _serverConnection = Environment.GetEnvironmentVariable("TEST_MYSQL_CONNECTION")
        ?? throw new InvalidOperationException(
            "TEST_MYSQL_CONNECTION is not set. Run `make up-db` and see the README (section 'Testes').");

    private readonly string _database = $"ddd_api_test_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:Default",
            new MySqlConnectionStringBuilder(_serverConnection) { Database = _database }.ConnectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using var connection = new MySqlConnection(_serverConnection);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS `{_database}`";
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiFixtureDefinition : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
