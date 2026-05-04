using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Abstractions;

namespace TaskManager.Infrastructure.Data;

public sealed class DatabaseMigrator(
    SqliteConnectionFactory connectionFactory,
    ILogger<DatabaseMigrator> logger) : IDatabaseMigrator
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT NOT NULL PRIMARY KEY,
                Email TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                DisplayName TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS TaskItems (
                Id TEXT NOT NULL PRIMARY KEY,
                UserId TEXT NOT NULL,
                Title TEXT NOT NULL,
                Description TEXT NOT NULL,
                Status INTEGER NOT NULL,
                DueDate TEXT NULL,
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_TaskItems_UserId ON TaskItems(UserId);
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Database schema ensured.");
    }
}
