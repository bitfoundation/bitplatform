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
