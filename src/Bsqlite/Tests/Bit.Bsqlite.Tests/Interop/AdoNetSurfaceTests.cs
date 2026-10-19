using System.Data;
using System.Data.Common;
using Bit.Bsqlite.Data;
using Bit.Bsqlite.Testing;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// The ADO.NET behaviours an application relies on without thinking about them: a transaction that
/// rolls back when abandoned, savepoints, batches, multi-result readers, and parameter names with or
/// without their sigil. Run against the in-process engine, which reports changes through the same
/// update hook the worker uses.
/// </summary>
[TestClass]
public sealed class AdoNetSurfaceTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    private async Task<(BsqliteInProcessTransport Transport, BsqliteConnection Connection)> OpenAsync()
    {
        var transport = new BsqliteInProcessTransport();
        var connection = new BsqliteConnection(transport, "ado.db");
        await connection.OpenAsync(Ct);
        await ExecAsync(connection, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT NOT NULL)");
        return (transport, connection);
    }

    private async Task<int> ExecAsync(BsqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteNonQueryAsync(Ct);
    }

    private async Task<long> CountAsync(BsqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM product";
        return Convert.ToInt64(await command.ExecuteScalarAsync(Ct));
    }

    // ---------------------------------------------------------------------------------------------
    // Transactions
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Transaction_DisposedWithoutCommit_RollsBack()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;

        await using (var tx = await connection.BeginTransactionAsync(Ct))
        {
            await ExecAsync(connection, "INSERT INTO product (name) VALUES ('Ghost')");
            Assert.AreEqual(1, await CountAsync(connection));
        }

        Assert.AreEqual(0, await CountAsync(connection));

        // The connection is free for a new transaction, which is the observable proof of the rollback.
        await using var next = await connection.BeginTransactionAsync(Ct);
        await next.CommitAsync(Ct);
    }

    [TestMethod]
    public async Task Transaction_BeginsImmediate_ForSerializable_AndDeferredOtherwise()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;

        await using (var tx = await connection.BeginTransactionAsync(Ct))
        {
            await tx.RollbackAsync(Ct);
        }

        await using (var tx = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, Ct))
        {
            await tx.RollbackAsync(Ct);
        }

        Assert.Contains("BEGIN IMMEDIATE", transport.ExecutedCommands);
        Assert.Contains("BEGIN", transport.ExecutedCommands);
    }

    [TestMethod]
    public async Task Transaction_NestedBegin_IsRefusedWithAClearMessage()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;

        await using var tx = await connection.BeginTransactionAsync(Ct);

        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await connection.BeginTransactionAsync(Ct));

        Assert.Contains("savepoint", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task Savepoints_RollBackPartOfATransaction()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;

        await using var tx = await connection.BeginTransactionAsync(Ct);
        Assert.IsTrue(tx.SupportsSavepoints);

        await ExecAsync(connection, "INSERT INTO product (name) VALUES ('Kept')");
        await tx.SaveAsync("before_ghost", Ct);
        await ExecAsync(connection, "INSERT INTO product (name) VALUES ('Ghost')");
        await tx.RollbackAsync("before_ghost", Ct);
        await tx.ReleaseAsync("before_ghost", Ct);
        await tx.CommitAsync(Ct);

        Assert.AreEqual(1, await CountAsync(connection));
    }

    [TestMethod]
    public async Task Transaction_CommitTwice_Throws()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;

        await using var tx = await connection.BeginTransactionAsync(Ct);
        await tx.CommitAsync(Ct);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => tx.CommitAsync(Ct));
    }

    // ---------------------------------------------------------------------------------------------
    // Batches and readers
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Batch_RunsEveryCommandInOneRoundTrip_AndReportsPerCommandCounts()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;

        Assert.IsTrue(connection.CanCreateBatch);
        await using var batch = connection.CreateBatch();
        for (var i = 0; i < 3; i++)
        {
            var command = batch.CreateBatchCommand();
            command.CommandText = "INSERT INTO product (name) VALUES (@name)";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@name";
            parameter.Value = $"Item {i}";
            command.Parameters.Add(parameter);
            batch.BatchCommands.Add(command);
        }

        var before = transport.ExecutedCommands.Count;
        Assert.AreEqual(3, await batch.ExecuteNonQueryAsync(Ct));
        foreach (BsqliteBatchCommand command in batch.BatchCommands)
        {
            Assert.AreEqual(1, command.RecordsAffected);
        }
        Assert.AreEqual(3, transport.ExecutedCommands.Count - before);
        Assert.AreEqual(3, await CountAsync(connection));
    }

    [TestMethod]
    public async Task Batch_Reader_WalksEveryResultSet()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;
        await ExecAsync(connection, "INSERT INTO product (name) VALUES ('A'), ('B')");

        await using var batch = connection.CreateBatch();
        batch.BatchCommands.Add(new BsqliteBatchCommand { CommandText = "SELECT name FROM product ORDER BY id" });
        batch.BatchCommands.Add(new BsqliteBatchCommand { CommandText = "SELECT COUNT(*) FROM product" });

        await using var reader = await batch.ExecuteReaderAsync(Ct);
        var names = new List<string>();
        while (await reader.ReadAsync(Ct))
        {
            names.Add(reader.GetString(0));
        }

        AssertSequence.Equal(["A", "B"], names);
        Assert.IsTrue(await reader.NextResultAsync(Ct));
        Assert.IsTrue(await reader.ReadAsync(Ct));
        Assert.AreEqual(2L, reader.GetInt64(0));
        Assert.IsFalse(await reader.NextResultAsync(Ct));
    }

    [TestMethod]
    public async Task Reader_SingleRow_LimitsTheResult()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;
        await ExecAsync(connection, "INSERT INTO product (name) VALUES ('A'), ('B'), ('C')");

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM product ORDER BY id";
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, Ct);

        Assert.IsTrue(await reader.ReadAsync(Ct));
        Assert.IsFalse(await reader.ReadAsync(Ct));
    }

    [TestMethod]
    public async Task Reader_SchemaOnly_ReturnsColumnsWithoutRows()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;
        await ExecAsync(connection, "INSERT INTO product (name) VALUES ('A')");

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name FROM product";
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly, Ct);

        Assert.AreEqual(2, reader.FieldCount);
        Assert.AreEqual("name", reader.GetName(1));
        Assert.IsFalse(reader.HasRows);
    }

    [TestMethod]
    public async Task Reader_GetFieldType_WorksBeforeRead_FromTheColumnType()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name FROM product";
        await using var reader = await command.ExecuteReaderAsync(Ct);

        Assert.AreEqual(typeof(long), reader.GetFieldType(0));
        Assert.AreEqual(typeof(string), reader.GetFieldType(1));
        Assert.AreEqual(1, reader.GetOrdinal("NAME"));
    }

    [TestMethod]
    public async Task Command_LastInsertRowId_IsAvailableWithoutAQuery()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;

        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO product (name) VALUES ('A')";
        await command.ExecuteNonQueryAsync(Ct);
        await command.ExecuteNonQueryAsync(Ct);

        Assert.AreEqual(2L, command.LastInsertRowId);
        Assert.AreEqual(2L, connection.LastInsertRowId);
    }

    [TestMethod]
    public async Task Parameters_MatchWithOrWithoutTheirSigil()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;

        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO product (name) VALUES (@name)";
        command.Parameters.AddWithValue("@name", "Sigil");

        Assert.IsTrue(command.Parameters.Contains("name"));
        Assert.AreSame(command.Parameters["@name"], command.Parameters["name"]);
        Assert.AreEqual(1, await command.ExecuteNonQueryAsync(Ct));
    }

    [TestMethod]
    public async Task Command_RequiresAnOpenConnection()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "closed.db");
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => command.ExecuteScalarAsync(Ct));
    }

    // ---------------------------------------------------------------------------------------------
    // Change tracking and snapshots
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task TablesChanged_ComesFromTheEngine_SoCascadesAreSeen()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;
        await ExecAsync(connection, "PRAGMA foreign_keys = ON");
        await ExecAsync(connection, "CREATE TABLE line (id INTEGER PRIMARY KEY, product_id INTEGER REFERENCES product(id) ON DELETE CASCADE)");
        await ExecAsync(connection, "INSERT INTO product (name) VALUES ('A')");
        await ExecAsync(connection, "INSERT INTO line (product_id) VALUES (1)");

        var seen = new List<IReadOnlySet<string>>();
        connection.TablesChanged += (_, e) => seen.Add(e.Tables);

        await ExecAsync(connection, "DELETE FROM product WHERE id = 1");

        var tables = Assert.ContainsSingle(seen);
        Assert.Contains("product", tables);
        Assert.Contains("line", tables);
    }

    [TestMethod]
    public async Task TablesChanged_IsNotRaised_ForAReadOrAWriteThatChangedNothing()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;

        var raised = 0;
        connection.TablesChanged += (_, _) => raised++;

        await CountAsync(connection);
        await ExecAsync(connection, "DELETE FROM product WHERE id = 999");

        Assert.AreEqual(0, raised);
    }

    [TestMethod]
    public async Task RemoteChanges_ReachTheConnection_WhenTheTransportHearsThem()
    {
        var transport = new NotifyingTransport();
        await using var connection = new BsqliteConnection(transport, "remote.db");
        await connection.OpenAsync(Ct);

        IReadOnlySet<string>? seen = null;
        connection.TablesChanged += (_, e) => seen = e.Tables;

        transport.Raise("Orders");

        Assert.IsNotNull(seen);
        Assert.Contains("orders", seen);
    }

    [TestMethod]
    public async Task Export_SnapshotsTheOpenDatabase()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;
        await ExecAsync(connection, "INSERT INTO product (name) VALUES ('Exported')");

        var image = await connection.ExportAsync(Ct);

        Assert.IsTrue(image.Length >= 4096);
        CollectionAssert.AreEqual("SQLite format 3\0"u8.ToArray(), image[..16]);
    }

    // ---------------------------------------------------------------------------------------------
    // Live queries
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task LiveQuery_CoalescesABurstOfWrites_IntoFewRefreshes()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;

        var refreshes = 0;
        var gate = new SemaphoreSlim(0);
        await using var live = new BsqliteLiveQuery<long>(
            connection,
            async ct =>
            {
                Interlocked.Increment(ref refreshes);
                await gate.WaitAsync(ct);
                return await CountAsync(connection);
            },
            ["product"]);

        var first = live.RefreshAsync(Ct);
        gate.Release();
        await first;

        await ExecAsync(connection, "INSERT INTO product (name) VALUES ('x')");
        await WaitUntilAsync(() => Volatile.Read(ref refreshes) == 2);

        for (var i = 1; i < 10; i++)
        {
            await ExecAsync(connection, "INSERT INTO product (name) VALUES ('x')");
        }

        // One refresh is in flight (blocked on the gate); the other nine writes fold into one more.
        gate.Release(2);
        await WaitUntilAsync(() => Volatile.Read(ref refreshes) >= 3 && live.Current == 10);
        await Task.Delay(100, Ct);

        Assert.AreEqual(3, refreshes);
    }

    [TestMethod]
    public async Task LiveQuery_ReportsAFailedRefresh_InsteadOfSwallowingIt()
    {
        var (transport, connection) = await OpenAsync();
        await using var _ = transport;
        await using var __ = connection;

        Exception? failure = null;
        await using var live = new BsqliteLiveQuery<int>(
            connection,
            _ => throw new InvalidOperationException("boom"),
            ["product"]);
        live.RefreshFailed += (_, ex) => failure = ex;

        await ExecAsync(connection, "INSERT INTO product (name) VALUES ('x')");

        await WaitUntilAsync(() => failure is not null);
        Assert.AreEqual("boom", failure!.Message);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        Assert.IsTrue(condition());
    }

    private sealed class NotifyingTransport : IBsqliteTransport
    {
        public event EventHandler<BsqliteTablesChangedEventArgs>? TablesChanged;

        public void Raise(params string[] tables)
            => TablesChanged?.Invoke(this, new BsqliteTablesChangedEventArgs(tables));

        public Task OpenAsync(string databaseName, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<BsqliteCommandResult>> ExecuteAsync(
            IReadOnlyList<BsqliteCommandRequest> batch,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BsqliteCommandResult>>([new BsqliteCommandResult()]);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
