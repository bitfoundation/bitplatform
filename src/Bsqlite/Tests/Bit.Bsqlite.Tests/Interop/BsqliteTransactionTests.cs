using Bit.Bsqlite.Data;
using Bit.Bsqlite.Testing;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// The transport keeps one database open for the life of a session, so an open transaction outlives
/// the scope that started it. That makes the ADO.NET rule about disposal - an uncommitted
/// transaction rolls back - the difference between a tidy unwind and a connection nothing can write
/// to again.
/// </summary>
[TestClass]
public sealed class BsqliteTransactionTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task Dispose_RollsBackATransactionNobodyCompleted()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "tx.db");
        await connection.OpenAsync(Ct);
        await ExecuteAsync(connection, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");

        await using (var transaction = await connection.BeginTransactionAsync(Ct))
        {
            await ExecuteAsync(connection, "INSERT INTO product (name) VALUES ('Ghost')");
        }

        Assert.AreEqual(0L, await CountAsync(connection));

        // The connection is writable again, which it would not be if the BEGIN were still open.
        await ExecuteAsync(connection, "INSERT INTO product (name) VALUES ('Kept')");
        Assert.AreEqual(1L, await CountAsync(connection));
    }

    [TestMethod]
    public async Task Dispose_AfterCommit_LeavesTheWriteInPlace()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "tx.db");
        await connection.OpenAsync(Ct);
        await ExecuteAsync(connection, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");

        await using (var transaction = await connection.BeginTransactionAsync(Ct))
        {
            await ExecuteAsync(connection, "INSERT INTO product (name) VALUES ('Kept')");
            await transaction.CommitAsync(Ct);
        }

        Assert.AreEqual(1L, await CountAsync(connection));
    }

    [TestMethod]
    public async Task Dispose_AfterRollback_DoesNotRollBackTwice()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "tx.db");
        await connection.OpenAsync(Ct);
        await ExecuteAsync(connection, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");

        var transaction = await connection.BeginTransactionAsync(Ct);
        await transaction.RollbackAsync(Ct);
        await transaction.DisposeAsync();

        Assert.DoesNotContain(
            sql => sql == "ROLLBACK",
            transport.ExecutedCommands.Skip(transport.ExecutedCommands.IndexOf("ROLLBACK") + 1));
    }

    /// <summary>
    /// A cancel that lands after BEGIN was sent: the worker runs it regardless, so the transaction
    /// has to come back owned. Otherwise the engine sits in a transaction no object can end, holding
    /// the write lock and swallowing every later write.
    /// </summary>
    [TestMethod]
    public async Task Begin_CancelledOnceSent_StillHandsBackTheTransaction()
    {
        using var cancel = new CancellationTokenSource();
        await using var inner = new BsqliteInProcessTransport();
        await using var transport = new AbandoningTransport(inner, "BEGIN", cancel);
        await using var connection = new BsqliteConnection(transport, "tx.db");
        await connection.OpenAsync(Ct);
        await ExecuteAsync(connection, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");

        await using (var transaction = await connection.BeginTransactionAsync(cancel.Token))
        {
            await ExecuteAsync(connection, "INSERT INTO product (name) VALUES ('Ghost')");
        }

        Assert.AreEqual(0L, await CountAsync(connection));

        await using var next = await connection.BeginTransactionAsync(Ct);
        await next.CommitAsync(Ct);
    }

    [TestMethod]
    public async Task Begin_AlreadyCancelled_SendsNothing()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "tx.db");
        await connection.OpenAsync(Ct);

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await connection.BeginTransactionAsync(new CancellationToken(canceled: true)));

        Assert.DoesNotContain(sql => sql.StartsWith("BEGIN", StringComparison.Ordinal), transport.ExecutedCommands);

        await using var transaction = await connection.BeginTransactionAsync(Ct);
        await transaction.CommitAsync(Ct);
    }

    [TestMethod]
    public async Task Commit_CancelledOnceSent_StillCompletes()
    {
        using var cancel = new CancellationTokenSource();
        await using var inner = new BsqliteInProcessTransport();
        await using var transport = new AbandoningTransport(inner, "COMMIT", cancel);
        await using var connection = new BsqliteConnection(transport, "tx.db");
        await connection.OpenAsync(Ct);
        await ExecuteAsync(connection, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");

        var transaction = await connection.BeginTransactionAsync(Ct);
        await ExecuteAsync(connection, "INSERT INTO product (name) VALUES ('Kept')");
        await transaction.CommitAsync(cancel.Token);
        await transaction.DisposeAsync();

        Assert.AreEqual(1L, await CountAsync(connection));
        Assert.DoesNotContain("ROLLBACK", inner.ExecutedCommands);
    }

    /// <summary>
    /// Runs every batch, then - for the statement it watches - cancels the caller's token and stops
    /// waiting on it, which is what Blazor interop does when a token fires after the call went out.
    /// </summary>
    private sealed class AbandoningTransport(
        BsqliteInProcessTransport inner,
        string statementPrefix,
        CancellationTokenSource cancel) : IBsqliteTransport
    {
        public Task OpenAsync(string databaseName, CancellationToken cancellationToken = default)
            => inner.OpenAsync(databaseName, cancellationToken);

        public Task CloseAsync(CancellationToken cancellationToken = default)
            => inner.CloseAsync(cancellationToken);

        public async Task<IReadOnlyList<BsqliteCommandResult>> ExecuteAsync(
            IReadOnlyList<BsqliteCommandRequest> batch,
            CancellationToken cancellationToken = default)
        {
            var results = await inner.ExecuteAsync(batch, CancellationToken.None);

            if (batch[0].CommandText.StartsWith(statementPrefix, StringComparison.Ordinal))
            {
                await cancel.CancelAsync();
                cancellationToken.ThrowIfCancellationRequested();
            }

            return results;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private async Task ExecuteAsync(BsqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(Ct);
    }

    private async Task<long> CountAsync(BsqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM product";
        return Convert.ToInt64(await command.ExecuteScalarAsync(Ct));
    }
}
