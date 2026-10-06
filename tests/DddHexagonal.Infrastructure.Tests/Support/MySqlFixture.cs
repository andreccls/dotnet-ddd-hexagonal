using DddHexagonal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace DddHexagonal.Infrastructure.Tests.Support;

/// <summary>
/// One throw-away database per test class, created through the real EF migrations (so the migrations are
/// exercised too) and dropped at the end. Needs TEST_MYSQL_CONNECTION, e.g.
/// "Server=localhost;Port=3306;User=root;Password=dev_only_root_password" (no Database=).
/// </summary>
public sealed class MySqlFixture : IAsyncLifetime
{
    private string _serverConnection = string.Empty;
    private string _database = string.Empty;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _serverConnection = Environment.GetEnvironmentVariable("TEST_MYSQL_CONNECTION")
            ?? throw new InvalidOperationException(
                "TEST_MYSQL_CONNECTION is not set. Run `make up-db` and see the README (section 'Testes').");
        _database = $"ddd_test_{Guid.NewGuid():N}";
        ConnectionString = new MySqlConnectionStringBuilder(_serverConnection) { Database = _database }.ConnectionString;

        await using var db = NewContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var connection = new MySqlConnection(_serverConnection);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS `{_database}`";
        await command.ExecuteNonQueryAsync();
    }

    public AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseMySql(ConnectionString, new MySqlServerVersion(new Version(8, 4, 0))).Options);
}
