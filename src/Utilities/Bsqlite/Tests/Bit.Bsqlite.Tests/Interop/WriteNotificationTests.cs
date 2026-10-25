using System.Data.Common;
using Bit.Bsqlite.Data;
using Bit.Bsqlite.Testing;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// When a live query hears about a write, and what it hears. Two paths exist and both are pinned
/// here: a transport that reports its own writes from the engine's hooks (the worker, and the
/// in-process transport that mirrors it), and the command layer's fallback for a transport that
/// cannot, which derives the tables from the SQL text.
/// </summary>
[TestClass]
public sealed class WriteNotificationTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    // ---------------------------------------------------------------------------------------------
    // The engine-hook path, through the in-process transport.
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task InProcessTransport_ReportsItsOwnWrites_SoTheConnectionDoesNot()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "notify.db");
        await connection.OpenAsync(Ct);
        await ExecuteAsync(connection, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");

        Assert.IsTrue(transport.ReportsLocalWrites);

        var raised = new List<IReadOnlySet<string>>();
        connection.TablesChanged += (_, e) => raised.Add(e.Tables);

        await ExecuteAsync(connection, "INSERT INTO product (name) VALUES ('x')");

        // Once, not twice: the transport spoke, and the command layer stayed quiet.
        var tables = Assert.ContainsSingle(raised);
        Assert.Contains("product", tables);
    }

    /// <summary>
    /// The reason the hooks are worth having: nothing in this DELETE's text names the child table,
    /// yet its rows are gone.
    /// </summary>
    [TestMethod]
    public async Task ACascadingDelete_NamesTheChildTable()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "cascade.db");
        await connection.OpenAsync(Ct);
        await ExecuteAsync(connection, "PRAGMA foreign_keys=ON");
        await ExecuteAsync(connection, "CREATE TABLE parent (id INTEGER PRIMARY KEY)");
        await ExecuteAsync(
            connection,
            "CREATE TABLE child (id INTEGER PRIMARY KEY, "
            + "parent_id INTEGER REFERENCES parent(id) ON DELETE CASCADE)");
        await ExecuteAsync(connection, "INSERT INTO parent (id) VALUES (1)");
        await ExecuteAsync(connection, "INSERT INTO child (id, parent_id) VALUES (1, 1)");

        var raised = new List<IReadOnlySet<string>>();
        connection.TablesChanged += (_, e) => raised.Add(e.Tables);

        await ExecuteAsync(connection, "DELETE FROM parent WHERE id = 1");

        var tables = Assert.ContainsSingle(raised);
        Assert.Contains("parent", tables);
        Assert.Contains("child", tables);
    }

    /// <summary>
    /// SQLite's truncate optimisation bypasses the update hook for a DELETE with no WHERE, so the
    /// statement text has to carry the name.
    /// </summary>
    [TestMethod]
    public async Task ATruncatingDelete_StillNamesTheTable()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "truncate.db");
        await connection.OpenAsync(Ct);
        await ExecuteAsync(connection, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecuteAsync(connection, "INSERT INTO product (name) VALUES ('x'), ('y')");

        var raised = new List<IReadOnlySet<string>>();
        connection.TablesChanged += (_, e) => raised.Add(e.Tables);

        await ExecuteAsync(connection, "DELETE FROM product");

        Assert.Contains("product", Assert.ContainsSingle(raised));
    }

    [TestMethod]
    public async Task ATransactionsWrites_AreReportedOnceAtCommit()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "tx-notify.db");
        await connection.OpenAsync(Ct);
        await ExecuteAsync(connection, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecuteAsync(connection, "CREATE TABLE customer (id INTEGER PRIMARY KEY, name TEXT)");

        var raised = new List<IReadOnlySet<string>>();
        connection.TablesChanged += (_, e) => raised.Add(e.Tables);

        await using (var transaction = await connection.BeginTransactionAsync(Ct))
        {
            await ExecuteAsync(connection, "INSERT INTO product (name) VALUES ('x')");
            await ExecuteAsync(connection, "INSERT INTO customer (name) VALUES ('y')");

            // Nothing yet: another reader could not see these rows, and they may still roll back.
            Assert.IsEmpty(raised);

            await transaction.CommitAsync(Ct);
        }

        var tables = Assert.ContainsSingle(raised);
        Assert.Contains("product", tables);
        Assert.Contains("customer", tables);
    }

    [TestMethod]
    public async Task ARolledBackTransaction_ReportsNothing()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "tx-rollback.db");
        await connection.OpenAsync(Ct);
        await ExecuteAsync(connection, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");

        var raised = 0;
        connection.TablesChanged += (_, _) => raised++;

        await using (var transaction = await connection.BeginTransactionAsync(Ct))
        {
            await ExecuteAsync(connection, "INSERT INTO product (name) VALUES ('ghost')");
            await transaction.RollbackAsync(Ct);
        }

        // Also for the implicit rollback that disposal performs.
        await using (await connection.BeginTransactionAsync(Ct))
        {
            await ExecuteAsync(connection, "INSERT INTO product (name) VALUES ('ghost')");
        }

        Assert.AreEqual(0, raised);
    }

    /// <summary>
    /// A transaction driven by SQL text rather than the ADO.NET API is a transaction all the same:
    /// the transport reads the engine's autocommit state, not the command that changed it.
    /// </summary>
    [TestMethod]
    public async Task ARawSqlTransaction_IsReportedAtItsCommit()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "tx-raw.db");
        await connection.OpenAsync(Ct);
        await ExecuteAsync(connection, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");

        var raised = new List<IReadOnlySet<string>>();
        connection.TablesChanged += (_, e) => raised.Add(e.Tables);

        await ExecuteAsync(connection, "BEGIN");
        await ExecuteAsync(connection, "INSERT INTO product (name) VALUES ('x')");
        Assert.IsEmpty(raised);

        await ExecuteAsync(connection, "COMMIT");
        Assert.Contains("product", Assert.ContainsSingle(raised));
    }

    // ---------------------------------------------------------------------------------------------
    // The command-layer fallback, for a transport that cannot report its own writes.
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task SilentTransport_TheConnectionReportsFromTheSql()
    {
        var transport = new SilentTransport();
        await using var connection = new BsqliteConnection(transport, "silent.db");
        await connection.OpenAsync(Ct);

        var raised = new List<IReadOnlySet<string>>();
        connection.TablesChanged += (_, e) => raised.Add(e.Tables);

        await ExecuteAsync(connection, "INSERT INTO product (name) VALUES ('x')");

        Assert.Contains("product", Assert.ContainsSingle(raised));
    }

    [TestMethod]
    public async Task SilentTransport_ATransactionsWrites_WaitForCommit()
    {
        var transport = new SilentTransport();
        await using var connection = new BsqliteConnection(transport, "silent-tx.db");
        await connection.OpenAsync(Ct);

        var raised = new List<IReadOnlySet<string>>();
        connection.TablesChanged += (_, e) => raised.Add(e.Tables);

        await using (var transaction = await connection.BeginTransactionAsync(Ct))
        {
            await ExecuteAsync(connection, "INSERT INTO product (name) VALUES ('x')");
            await ExecuteAsync(connection, "UPDATE customer SET name = 'y'");
            Assert.IsEmpty(raised);

            await transaction.CommitAsync(Ct);
        }

        var tables = Assert.ContainsSingle(raised);
        Assert.Contains("product", tables);
        Assert.Contains("customer", tables);
    }

    [TestMethod]
    public async Task SilentTransport_ARolledBackTransaction_ReportsNothing()
    {
        var transport = new SilentTransport();
        await using var connection = new BsqliteConnection(transport, "silent-rollback.db");
        await connection.OpenAsync(Ct);

        var raised = 0;
        connection.TablesChanged += (_, _) => raised++;

        await using (var transaction = await connection.BeginTransactionAsync(Ct))
        {
            await ExecuteAsync(connection, "INSERT INTO product (name) VALUES ('ghost')");
            await transaction.RollbackAsync(Ct);
        }

        Assert.AreEqual(0, raised);
    }

    private async Task ExecuteAsync(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(Ct);
    }

    /// <summary>A transport that runs nothing and reports nothing - the default contract.</summary>
    private sealed class SilentTransport : IBsqliteTransport
    {
        public Task OpenAsync(string databaseName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<BsqliteCommandResult>> ExecuteAsync(
            IReadOnlyList<BsqliteCommandRequest> batch,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BsqliteCommandResult>>(
                [.. batch.Select(_ => new BsqliteCommandResult())]);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
