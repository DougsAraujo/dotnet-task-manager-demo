using Microsoft.Data.Sqlite;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Data;

public sealed class UserRepository(SqliteConnectionFactory connectionFactory) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Email, PasswordHash, DisplayName
            FROM Users
            WHERE Id = $id
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("$id", id.ToString());
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Email, PasswordHash, DisplayName
            FROM Users
            WHERE Email = $email
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("$email", email);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Users (Id, Email, PasswordHash, DisplayName)
            VALUES ($id, $email, $hash, $display);
            """;
        cmd.Parameters.AddWithValue("$id", user.Id.ToString());
        cmd.Parameters.AddWithValue("$email", user.Email);
        cmd.Parameters.AddWithValue("$hash", user.PasswordHash);
        cmd.Parameters.AddWithValue("$display", user.DisplayName);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return user;
    }

    private static User Map(SqliteDataReader reader) =>
        new()
        {
            Id = Guid.Parse(reader.GetString(0)),
            Email = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            DisplayName = reader.GetString(3)
        };
}
