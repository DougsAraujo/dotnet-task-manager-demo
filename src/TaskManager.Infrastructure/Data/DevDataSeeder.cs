using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Options;

namespace TaskManager.Infrastructure.Data;

public sealed class DevDataSeeder(
    SqliteConnectionFactory connectionFactory,
    IPasswordHasher passwordHasher,
    IOptions<DemoUserOptions> demoOptions,
    ILogger<DevDataSeeder> logger) : IDevDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var demo = demoOptions.Value;
        var email = demo.Email.Trim().ToLowerInvariant();

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var check = connection.CreateCommand();
        check.CommandText = "SELECT COUNT(1) FROM Users WHERE Email = $email;";
        check.Parameters.AddWithValue("$email", email);
        var exists = Convert.ToInt64(await check.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) > 0;
        if (exists)
        {
            logger.LogInformation("Demo user already present; skipping seed.");
            return;
        }

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = email,
            PasswordHash = passwordHasher.Hash(demo.Password),
            DisplayName = demo.DisplayName
        };

        await using var insertUser = connection.CreateCommand();
        insertUser.CommandText = """
            INSERT INTO Users (Id, Email, PasswordHash, DisplayName)
            VALUES ($id, $email, $hash, $display);
            """;
        insertUser.Parameters.AddWithValue("$id", user.Id.ToString());
        insertUser.Parameters.AddWithValue("$email", user.Email);
        insertUser.Parameters.AddWithValue("$hash", user.PasswordHash);
        insertUser.Parameters.AddWithValue("$display", user.DisplayName);
        await insertUser.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        var taskId = Guid.NewGuid();
        await using var insertTask = connection.CreateCommand();
        insertTask.CommandText = """
            INSERT INTO TaskItems (Id, UserId, Title, Description, Status, DueDate)
            VALUES ($id, $userId, $title, $desc, $status, $due);
            """;
        insertTask.Parameters.AddWithValue("$id", taskId.ToString());
        insertTask.Parameters.AddWithValue("$userId", userId.ToString());
        insertTask.Parameters.AddWithValue("$title", "Welcome to Task Manager");
        insertTask.Parameters.AddWithValue("$desc", "This is seeded demo data. You can edit or delete this task.");
        insertTask.Parameters.AddWithValue("$status", 0);
        insertTask.Parameters.AddWithValue("$due", DateTime.UtcNow.AddDays(7).ToString("O"));
        await insertTask.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Seeded demo user {Email} and a sample task.", email);
    }
}
