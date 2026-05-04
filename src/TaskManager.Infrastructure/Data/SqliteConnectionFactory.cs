using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using TaskManager.Infrastructure.Options;

namespace TaskManager.Infrastructure.Data;

public sealed class SqliteConnectionFactory(IOptions<DatabaseOptions> options)
{
    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(options.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            await pragma.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return connection;
    }
}
