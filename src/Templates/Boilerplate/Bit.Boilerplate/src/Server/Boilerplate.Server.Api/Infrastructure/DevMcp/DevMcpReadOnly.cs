using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

internal static class DevMcpReadOnly
{
    public static async Task<IAsyncDisposable> BeginAsync(DbContext db, CancellationToken cancellationToken)
    {
        var previousTimeout = db.Database.GetCommandTimeout();
        db.Database.SetCommandTimeout(DevMcpLimits.CommandTimeoutSeconds);
        await db.Database.OpenConnectionAsync(cancellationToken);

        var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        // Transaction-scoped on SQL Server / PostgreSQL / MySQL. Never PRAGMA query_only: that is
        // connection-scoped and would poison the shared SQLite pool used by AppDbContext.
        if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) is not true)
        {
            var connection = db.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "SET TRANSACTION READ ONLY";
            command.Transaction = transaction.GetDbTransaction();
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return new Session(db, previousTimeout, transaction);
    }

    private sealed class Session(DbContext db, int? previousTimeout, IDbContextTransaction transaction) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await transaction.DisposeAsync();
            // Matches the OpenConnectionAsync above: EF counts opens, so without this the connection stays out of the
            // pool until the request scope ends.
            await db.Database.CloseConnectionAsync();
            db.Database.SetCommandTimeout(previousTimeout);
        }
    }
}
