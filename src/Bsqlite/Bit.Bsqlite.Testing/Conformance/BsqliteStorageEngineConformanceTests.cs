using Bit.Bsqlite.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Bsqlite.Storage.ConformanceTests;

/// <summary>
/// Engine-level rules every backend must satisfy once a real database is open. Inherit it next
/// to <see cref="BsqliteStorageProviderConformanceTests"/> and return an already-opened
/// transport.
/// </summary>
/// <remarks>
/// Write atomicity and the WAL ban can be checked against any transport, including the in-process
/// one. Crash safety and the declared concurrency levels need a worker that can be killed and a
/// backend that actually shares files; those tests skip unless the subclass says it can provide
/// that. A skip is an acknowledged gap, not a pass.
/// </remarks>
public abstract class BsqliteStorageEngineConformanceTests : IAsyncDisposable
{
    /// <summary>Set by MSTest for each test; absent when a rule is called directly.</summary>
    public TestContext? TestContext { get; set; }

    private readonly List<IAsyncDisposable> _owned = [];

    private CancellationToken Ct => TestContext?.CancellationToken ?? CancellationToken.None;

    /// <summary>
    /// An opened transport talking to a fresh database. Called more than once when a test needs a
    /// second connection.
    /// </summary>
    protected abstract ValueTask<IBsqliteTransport> CreateOpenTransportAsync();

    /// <summary>
    /// Whether this backend can be crash-tested: the transport must survive a kill mid-commit and
    /// the data must outlive it. In-memory and in-process engines cannot.
    /// </summary>
    protected virtual bool SupportsCrashInjection => false;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var owned in _owned)
        {
            await owned.DisposeAsync();
        }

        _owned.Clear();
        GC.SuppressFinalize(this);
    }

    private async Task<BsqliteConnection> ConnectAsync()
    {
        var transport = await CreateOpenTransportAsync();
        _owned.Add(transport);

        var connection = new BsqliteConnection(transport, "conformance.db");
        _owned.Add(connection);
        await connection.OpenAsync(Ct);
        return connection;
    }

    [TestMethod]
    public async Task InsertThenSelect_RoundTripsARow()
    {
        await using var connection = await ConnectAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)";
        await command.ExecuteNonQueryAsync(Ct);

        command.CommandText = "INSERT INTO product (name) VALUES ('Widget')";
        Assert.AreEqual(1, await command.ExecuteNonQueryAsync(Ct));

        command.CommandText = "SELECT name FROM product";
        Assert.AreEqual("Widget", await command.ExecuteScalarAsync(Ct));
    }

    /// <summary>
    /// A rolled-back write must be invisible. This is the atomicity the admin-layer kit cannot
    /// check, because it never runs SQL.
    /// </summary>
    [TestMethod]
    public async Task Rollback_LeavesTheDatabaseUnchanged()
    {
        await using var connection = await ConnectAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)";
        await command.ExecuteNonQueryAsync(Ct);

        command.CommandText = "BEGIN; INSERT INTO product (name) VALUES ('Ghost'); ROLLBACK;";
        await command.ExecuteNonQueryAsync(Ct);

        command.CommandText = "SELECT COUNT(*) FROM product";
        Assert.AreEqual(0L, Convert.ToInt64(await command.ExecuteScalarAsync(Ct)));
    }

    [TestMethod]
    public async Task Commit_MakesTheWriteVisible()
    {
        await using var connection = await ConnectAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)";
        await command.ExecuteNonQueryAsync(Ct);

        command.CommandText = "BEGIN; INSERT INTO product (name) VALUES ('Kept'); COMMIT;";
        await command.ExecuteNonQueryAsync(Ct);

        command.CommandText = "SELECT name FROM product";
        Assert.AreEqual("Kept", await command.ExecuteScalarAsync(Ct));
    }

    /// <summary>
    /// SQLite defaults foreign keys off and Microsoft.Data.Sqlite turns them on for every
    /// connection, so a transport that leaves them off enforces a model's relationships differently
    /// from the same model on the server - silently, and only for writes that were already wrong.
    /// </summary>
    [TestMethod]
    public async Task ForeignKeys_AreEnforced()
    {
        await using var connection = await ConnectAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = "PRAGMA foreign_keys";
        Assert.AreEqual(1L, Convert.ToInt64(await command.ExecuteScalarAsync(Ct)));

        command.CommandText = "CREATE TABLE parent (id INTEGER PRIMARY KEY)";
        await command.ExecuteNonQueryAsync(Ct);

        command.CommandText =
            "CREATE TABLE child (id INTEGER PRIMARY KEY, parent_id INTEGER REFERENCES parent(id))";
        await command.ExecuteNonQueryAsync(Ct);

        command.CommandText = "INSERT INTO child (id, parent_id) VALUES (1, 999)";
        await Assert.ThrowsAsync<Exception>(() => command.ExecuteNonQueryAsync(Ct));
    }

    [TestMethod]
    public async Task JournalModeWal_IsRejected()
    {
        await using var connection = await ConnectAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL";

        var error = await Assert.ThrowsExactlyAsync<BsqliteException>(
            () => command.ExecuteNonQueryAsync(Ct));

        Assert.Contains("WAL", error.Message);
        Assert.Contains("shared-memory", error.Message);
    }

    [TestMethod]
    public async Task CrashSafety_IsNotClaimed_WhenTheBackendCannotBeKilled()
    {
        SkipUnless(SupportsCrashInjection,
            "This backend cannot be crash-tested: there is no worker to kill, or the data does not "
            + "outlive the process. Persistent providers implement this by terminating the worker "
            + "inside a commit and reopening.");
    }

    /// <summary>
    /// Reports the calling test as inconclusive - MSTest's skip - with <paramref name="reason"/> when
    /// <paramref name="condition"/> does not hold.
    /// </summary>
    private static void SkipUnless(bool condition, string reason)
    {
        if (!condition)
        {
            Assert.Inconclusive(reason);
        }
    }
}
