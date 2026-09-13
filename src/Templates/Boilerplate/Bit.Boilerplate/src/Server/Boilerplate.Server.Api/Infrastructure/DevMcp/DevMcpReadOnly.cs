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

                    var result = await read(token);

                    // Disposal would roll back too, but the security property here is "this never writes" - it is
                    // worth stating rather than leaving to a using. Not cancellable: it is cleanup.
                    await transaction.RollbackAsync(CancellationToken.None);

                    return result;
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
    /// PostgreSQL alone accepts this statement once a transaction is open. T-SQL has no such statement at all ("Incorrect
    /// syntax near the keyword 'READ'"), MySQL refuses to change transaction characteristics mid-transaction (error 1568)
    /// and would need START TRANSACTION READ ONLY, which BeginTransactionAsync cannot express, and SQLite's PRAGMA
    /// query_only is connection-scoped and would poison the pool AppDbContext shares. Everywhere else the read-only
    /// guarantee is the one the tools themselves give: AsNoTracking, a validated projection, and never SaveChanges.
    /// </summary>
    public static bool SupportsReadOnlyTransaction(string? providerName)
    {
        return providerName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) is true;
    }
}
