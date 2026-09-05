using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

public static class DevMcpReadOnly
{
    /// <summary>
    /// Runs <paramref name="read"/> against the database inside a transaction that is never committed, with the Dev
    /// MCP's command timeout.
    /// </summary>
    /// <remarks>
    /// The transaction is opened inside the execution strategy rather than around it: every provider but SQLite
    /// configures EnableRetryOnFailure, and a retrying strategy refuses a user-initiated transaction unless the whole
    /// unit is retriable. One read is retriable, so this is that unit.
    /// </remarks>
    public static async Task<T> ReadAsync<T>(DbContext db, Func<CancellationToken, Task<T>> read, CancellationToken cancellationToken)
    {
        var previousTimeout = db.Database.GetCommandTimeout();
        db.Database.SetCommandTimeout(DevMcpLimits.CommandTimeoutSeconds);

        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async token =>
            {
                await db.Database.OpenConnectionAsync(token);

                try
                {
                    await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);

                    if (SupportsReadOnlyTransaction(db.Database.ProviderName))
                    {
                        var connection = db.Database.GetDbConnection();
                        await using var command = connection.CreateCommand();
                        command.CommandText = "SET TRANSACTION READ ONLY";
                        command.Transaction = transaction.GetDbTransaction();
                        await command.ExecuteNonQueryAsync(token);
                    }

                    return await read(token);
                }
                finally
                {
                    // Matches OpenConnectionAsync: EF counts opens, so without this the connection stays out of the
                    // pool until the request scope ends.
                    await db.Database.CloseConnectionAsync();
                }
            }, cancellationToken);
        }
        finally
        {
            db.Database.SetCommandTimeout(previousTimeout);
        }
    }

    /// <summary>
    /// The Dev MCP's command timeout and nothing else. For metadata reads whose own probing may legitimately fail: a
    /// transaction would turn PostgreSQL's "relation does not exist" into an aborted transaction (25P02) instead of the
    /// empty list SQL Server and SQLite return.
    /// </summary>
    public static async Task<T> ReadMetadataAsync<T>(DbContext db, Func<CancellationToken, Task<T>> read, CancellationToken cancellationToken)
    {
        var previousTimeout = db.Database.GetCommandTimeout();
        db.Database.SetCommandTimeout(DevMcpLimits.CommandTimeoutSeconds);

        try
        {
            return await read(cancellationToken);
        }
        finally
        {
            db.Database.SetCommandTimeout(previousTimeout);
        }
    }

    /// <summary>
    /// Only PostgreSQL and MySQL have this statement, and there it is transaction-scoped. T-SQL has no equivalent (its
    /// SET TRANSACTION only sets an isolation level), and SQLite's PRAGMA query_only is connection-scoped and would
    /// poison the pool AppDbContext shares. Everywhere else the read-only guarantee is the one the tools themselves
    /// give: AsNoTracking, a validated projection, and nothing that ever calls SaveChanges.
    /// </summary>
    public static bool SupportsReadOnlyTransaction(string? providerName)
    {
        return providerName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) is true
            || providerName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) is true;
    }
}
