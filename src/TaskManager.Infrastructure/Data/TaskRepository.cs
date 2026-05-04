using Microsoft.Data.Sqlite;
using TaskManager.Application.Abstractions;
using TaskManager.Domain;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Data;

public sealed class TaskRepository(SqliteConnectionFactory connectionFactory) : ITaskRepository
{
    public async Task<IReadOnlyList<TaskItem>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, UserId, Title, Description, Status, DueDate
            FROM TaskItems
            WHERE UserId = $userId
            ORDER BY DueDate IS NULL, DueDate ASC, Title ASC;
            """;
        cmd.Parameters.AddWithValue("$userId", userId.ToString());
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var list = new List<TaskItem>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(Map(reader));
        }

        return list;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, UserId, Title, Description, Status, DueDate
            FROM TaskItems
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

    public async Task<TaskItem> CreateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO TaskItems (Id, UserId, Title, Description, Status, DueDate)
            VALUES ($id, $userId, $title, $desc, $status, $due);
            """;
        cmd.Parameters.AddWithValue("$id", task.Id.ToString());
        cmd.Parameters.AddWithValue("$userId", task.UserId.ToString());
        cmd.Parameters.AddWithValue("$title", task.Title);
        cmd.Parameters.AddWithValue("$desc", task.Description);
        cmd.Parameters.AddWithValue("$status", (int)task.Status);
        cmd.Parameters.AddWithValue("$due", task.DueDate.HasValue ? task.DueDate.Value.ToUniversalTime().ToString("O") : DBNull.Value);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return task;
    }

    public async Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE TaskItems
            SET Title = $title,
                Description = $desc,
                Status = $status,
                DueDate = $due
            WHERE Id = $id AND UserId = $userId;
            """;
        cmd.Parameters.AddWithValue("$title", task.Title);
        cmd.Parameters.AddWithValue("$desc", task.Description);
        cmd.Parameters.AddWithValue("$status", (int)task.Status);
        cmd.Parameters.AddWithValue("$due", task.DueDate.HasValue ? task.DueDate.Value.ToUniversalTime().ToString("O") : DBNull.Value);
        cmd.Parameters.AddWithValue("$id", task.Id.ToString());
        cmd.Parameters.AddWithValue("$userId", task.UserId.ToString());
        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            DELETE FROM TaskItems
            WHERE Id = $id AND UserId = $userId;
            """;
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.Parameters.AddWithValue("$userId", userId.ToString());
        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return rows > 0;
    }

    private static TaskItem Map(SqliteDataReader reader)
    {
        DateTime? due = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind);
        return new TaskItem
        {
            Id = Guid.Parse(reader.GetString(0)),
            UserId = Guid.Parse(reader.GetString(1)),
            Title = reader.GetString(2),
            Description = reader.GetString(3),
            Status = (TaskItemStatus)reader.GetInt32(4),
            DueDate = due
        };
    }
}
