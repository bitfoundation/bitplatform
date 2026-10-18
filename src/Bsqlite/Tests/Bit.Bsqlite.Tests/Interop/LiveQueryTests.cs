using Bit.Bsqlite.Data;
using Bit.Bsqlite.Testing;

namespace Bit.Bsqlite.Interop.Tests;

[TestClass]
public sealed class LiveQueryTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task ReRuns_WhenAWatchedTableIsWritten()
    {
        var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "live.db");
        await connection.OpenAsync(Ct);
        await using var create = connection.CreateCommand();
        create.CommandText = "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)";
        await create.ExecuteNonQueryAsync(Ct);

        var seen = new List<int>();
        await using var live = new BsqliteLiveQuery<int>(
            connection,
            async ct =>
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM product";
                return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
            },
            ["product"]);

        live.Changed += (_, count) => seen.Add(count);
        Assert.AreEqual(0, await live.RefreshAsync(Ct));

        await using var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO product (name) VALUES ('Kept')";
        await insert.ExecuteNonQueryAsync(Ct);

        await WaitUntilAsync(() => seen.Contains(1));
        Assert.AreEqual(1, live.Current);
    }

    /// <summary>
    /// A write from another tab reaches the application only through the transport, so the
    /// connection has to relay it. Without this the cross-tab half of a live query is JavaScript
    /// plumbing with nothing on the other end.
    /// </summary>
    [TestMethod]
    public async Task ReRuns_WhenTheTransportReportsAnotherTabsWrite()
    {
        var transport = new RemoteWriteTransport();
        await using var connection = new BsqliteConnection(transport, "live-remote.db");
        await connection.OpenAsync(Ct);

        var runs = 0;
        await using var live = new BsqliteLiveQuery<int>(
            connection,
            _ => Task.FromResult(Interlocked.Increment(ref runs)),
            ["product"]);

        Assert.AreEqual(1, await live.RefreshAsync(Ct));

        transport.ReportRemoteWrite("product");

        await WaitUntilAsync(() => live.Current == 2);
    }

    [TestMethod]
    public async Task Ignores_ARemoteWriteToAnUnwatchedTable()
    {
        var transport = new RemoteWriteTransport();
        await using var connection = new BsqliteConnection(transport, "live-remote.db");
        await connection.OpenAsync(Ct);

        await using var live = new BsqliteLiveQuery<int>(
            connection,
            _ => Task.FromResult(1),
            ["product"]);

        await live.RefreshAsync(Ct);
        var changed = 0;
        live.Changed += (_, _) => Interlocked.Increment(ref changed);

        transport.ReportRemoteWrite("customer");

        await Task.Delay(100, Ct);
        Assert.AreEqual(0, changed);
    }

    /// <summary>
    /// The connection unsubscribes on dispose; otherwise the transport - which outlives every
    /// connection made against it - keeps every one of them alive.
    /// </summary>
    [TestMethod]
    public async Task DisposedConnection_StopsRelayingRemoteWrites()
    {
        var transport = new RemoteWriteTransport();
        var connection = new BsqliteConnection(transport, "live-remote.db");
        await connection.OpenAsync(Ct);

        var seen = 0;
        connection.TablesChanged += (_, _) => Interlocked.Increment(ref seen);
        await connection.DisposeAsync();

        transport.ReportRemoteWrite("product");

        Assert.AreEqual(0, seen);
    }

    /// <summary>
    /// A consumer that is busy with one item while the next refresh lands must not wait for a
    /// further write to see it - and when it catches up it gets the newest snapshot, not a stale
    /// intermediate one.
    /// </summary>
    [TestMethod]
    public async Task WithCancellation_CatchesUpOnRefreshesItWasNotWaitingFor()
    {
        var transport = new RemoteWriteTransport();
        await using var connection = new BsqliteConnection(transport, "live-enumerate.db");
        await connection.OpenAsync(Ct);

        var runs = 0;
        await using var live = new BsqliteLiveQuery<int>(
            connection,
            _ => Task.FromResult(Interlocked.Increment(ref runs)),
            ["product"]);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(Ct);
#pragma warning disable xUnit1051 // The enumerator's own token is the subject: it is cancelled below.
        await using var enumerator = live.WithCancellation(cts.Token).GetAsyncEnumerator(cts.Token);
#pragma warning restore xUnit1051

        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.AreEqual(1, enumerator.Current);

        // Two refreshes complete while nobody is awaiting MoveNextAsync.
        await live.RefreshAsync(Ct);
        await live.RefreshAsync(Ct);

        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.AreEqual(3, enumerator.Current);

        // Nothing new now, so the next item has to wait for the next refresh.
        var pending = enumerator.MoveNextAsync();
        Assert.IsFalse(pending.IsCompleted);

        await live.RefreshAsync(Ct);
        Assert.IsTrue(await pending);
        Assert.AreEqual(4, enumerator.Current);

        await cts.CancelAsync();
        Assert.IsFalse(await enumerator.MoveNextAsync());
    }

    /// <summary>A transport that can be told to speak for another tab.</summary>
    private sealed class RemoteWriteTransport : IBsqliteTransport
    {
        public event EventHandler<BsqliteTablesChangedEventArgs>? TablesChanged;

        public void ReportRemoteWrite(params string[] tables)
            => TablesChanged?.Invoke(this, new BsqliteTablesChangedEventArgs(tables));

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

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        Assert.IsTrue(condition());
    }
}
