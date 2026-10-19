using System.Data;
using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// The session factory is the only place that both chooses a backend and opens it, so the rule
/// "the binding is written only after the open succeeds" is asserted here rather than left as a
/// comment on two collaborating types.
/// </summary>
[TestClass]
public sealed class BsqliteSessionFactoryTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task Open_UsesTheResolvedProvider_AndCommitsTheBinding()
    {
        var opfs = Available("opfs", BsqliteEngineBuild.Synchronous);
        var store = new BsqliteInMemoryStorageBindingStore();
        var transports = new RecordingTransportFactory();
        var factory = Factory(store, transports, s => s.Prefer("opfs"), opfs);

        await using var session = await factory.OpenAsync("app.db", Ct);

        Assert.AreEqual("opfs", session.Resolution.Provider.Name);
        Assert.AreEqual("opfs", await store.GetProviderNameAsync("app.db", Ct));
        Assert.AreEqual("app.db", transports.Last.OpenedAs);
        Assert.AreEqual(opfs, transports.Created[0].Provider);
        Assert.AreEqual(ConnectionState.Open, session.Connection.State);
    }

    [TestMethod]
    public async Task Open_PassesTheProvidersBuildAndVfs_ThroughTheTransportFactory()
    {
        var indexed = Available(
            "indexeddb",
            BsqliteEngineBuild.AsyncCapable,
            vfs: new BsqliteJsModule("./_content/Bit.Bsqlite.Storage.IndexedDb/bit-bsqlite-idb-vfs.js"));

        var capturing = new RecordingTransportFactory();
        var factory = Factory(
            new BsqliteInMemoryStorageBindingStore(),
            capturing,
            s => s.Prefer("indexeddb"),
            indexed);

        await using var session = await factory.OpenAsync("app.db", Ct);

        var seen = capturing.Created[0].Provider;
        Assert.AreSame(indexed, seen);
        Assert.AreEqual(BsqliteEngineBuild.AsyncCapable, seen.Capabilities.RequiredBuild);
        Assert.AreEqual("./_content/Bit.Bsqlite.Storage.IndexedDb/bit-bsqlite-idb-vfs.js", seen.VfsModule!.ModuleUrl);
    }

    [TestMethod]
    public async Task FailedOpen_DoesNotCommitABinding_AndDisposesTheTransport()
    {
        var store = new BsqliteInMemoryStorageBindingStore();
        var transports = new RecordingTransportFactory(
            () => new ScriptedTransport
            {
                OpenThrows = new BsqliteException("disk I/O error", sqliteErrorCode: 10),
            });
        var factory = Factory(store, transports, s => s.Prefer("opfs"), Available("opfs"));

        var error = await Assert.ThrowsExactlyAsync<BsqliteException>(
            () => factory.OpenAsync("app.db", Ct));

        Assert.AreEqual(10, error.SqliteErrorCode);
        Assert.IsNull(await store.GetProviderNameAsync("app.db", Ct));
        Assert.IsTrue(transports.Last.Disposed);
    }

    [TestMethod]
    public async Task ExistingData_OpensOnTheBoundBackend_NotThePreferredOne()
    {
        var indexed = Available("indexeddb", BsqliteEngineBuild.AsyncCapable);
        var opfs = Available("opfs", BsqliteEngineBuild.Synchronous);
        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "indexeddb", Ct);

        var transports = new RecordingTransportFactory();
        var factory = Factory(
            store,
            transports,
            s => s.Prefer("opfs").Fallback("indexeddb"),
            opfs,
            indexed);

        await using var session = await factory.OpenAsync("app.db", Ct);

        Assert.AreEqual("indexeddb", session.Resolution.Provider.Name);
        Assert.IsTrue(session.Resolution.WasDecidedByExistingData);
        Assert.AreSame(indexed, transports.Created[0].Provider);
    }

    [TestMethod]
    public async Task AutomaticOnOpen_WithoutABetterProvider_JustOpens()
    {
        var store = new BsqliteInMemoryStorageBindingStore();
        var factory = Factory(
            store,
            new RecordingTransportFactory(),
            s => s.Prefer("opfs").WithMigrationMode(BsqliteStorageMigrationMode.AutomaticOnOpen),
            Available("opfs"));

        await using var session = await factory.OpenAsync("app.db", Ct);

        Assert.AreEqual("opfs", session.Resolution.Provider.Name);
        Assert.AreEqual("opfs", await store.GetProviderNameAsync("app.db", Ct));
    }

    [TestMethod]
    public async Task AutomaticOnOpen_MovesTheDatabaseToTheBetterProvider_BeforeOpening()
    {
        var slow = new PersistentTestProvider("slow");
        var fast = new PersistentTestProvider("fast");
        byte[] image = [.. "SQLite format 3\0"u8, 1, 2, 3];
        await slow.Admin.ImportAsync("app.db", image, Ct);

        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "slow", Ct);

        var transports = new RecordingTransportFactory();
        var factory = Factory(
            store,
            transports,
            s => s.Prefer("fast").Fallback("slow").WithMigrationMode(BsqliteStorageMigrationMode.AutomaticOnOpen),
            fast,
            slow);

        await using var session = await factory.OpenAsync("app.db", Ct);

        Assert.AreEqual("fast", session.Resolution.Provider.Name);
        Assert.AreSame(fast, transports.Created[0].Provider);
        Assert.AreEqual("fast", await store.GetProviderNameAsync("app.db", Ct));
        CollectionAssert.AreEqual(image, await fast.Admin.ExportAsync("app.db", Ct));
        Assert.IsFalse(await slow.Admin.ExistsAsync("app.db", Ct));
    }

    /// <summary>
    /// An opportunistic move that fails is not a reason to keep the user from data that is still
    /// exactly where it was. The open proceeds on the bound backend, the binding is unchanged, and
    /// the resolution still says a better backend exists.
    /// </summary>
    [TestMethod]
    public async Task AutomaticOnOpen_WhenTheMoveFails_StillOpensWhereTheDataIs()
    {
        var slow = new PersistentTestProvider("slow");
        var fast = new PersistentTestProvider("fast", refusesImports: true);
        await slow.Admin.ImportAsync("app.db", "SQLite format 3\0"u8.ToArray(), Ct);

        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "slow", Ct);

        var transports = new RecordingTransportFactory();
        var factory = Factory(
            store,
            transports,
            s => s.Prefer("fast").Fallback("slow").WithMigrationMode(BsqliteStorageMigrationMode.AutomaticOnOpen),
            fast,
            slow);

        await using var session = await factory.OpenAsync("app.db", Ct);

        Assert.AreEqual("slow", session.Resolution.Provider.Name);
        Assert.AreSame(fast, session.Resolution.BetterProviderAvailable);
        Assert.AreEqual("slow", await store.GetProviderNameAsync("app.db", Ct));
        Assert.IsTrue(await slow.Admin.ExistsAsync("app.db", Ct));
    }

    /// <summary>
    /// The copy was verified and bound, then deleting the source failed with the source still whole -
    /// another tab holding it open, say. The flip is undone, so the database opens on the source and
    /// the binding agrees: writes made now are where the next load reads.
    /// </summary>
    [TestMethod]
    public async Task AutomaticOnOpen_WhenDeletingTheSourceFails_AndItSurvived_OpensOnTheSource_AndKeepsItBound()
    {
        var slow = new PersistentTestProvider("slow", deletes: DeleteBehavior.FailsLeavingIt);
        var fast = new PersistentTestProvider("fast");
        await slow.Admin.ImportAsync("app.db", "SQLite format 3 "u8.ToArray(), Ct);

        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "slow", Ct);

        var factory = Factory(
            store,
            new RecordingTransportFactory(),
            s => s.Prefer("fast").Fallback("slow").WithMigrationMode(BsqliteStorageMigrationMode.AutomaticOnOpen),
            fast,
            slow);

        await using var session = await factory.OpenAsync("app.db", Ct);

        Assert.AreEqual("slow", session.Resolution.Provider.Name);
        Assert.AreEqual("slow", await store.GetProviderNameAsync("app.db", Ct));
        Assert.IsTrue(await slow.Admin.ExistsAsync("app.db", Ct));
        Assert.IsFalse(await fast.Admin.ExistsAsync("app.db", Ct), "The unbound copy is dropped.");
    }

    /// <summary>
    /// Deleting the source failed part-way, so the verified copy is the only whole database left. It
    /// stays bound, and the session opens on it rather than on what remains of the source.
    /// </summary>
    [TestMethod]
    public async Task AutomaticOnOpen_WhenDeletingTheSourceFailsPartway_OpensOnTheTarget()
    {
        var slow = new PersistentTestProvider("slow", deletes: DeleteBehavior.FailsAfterDeleting);
        var fast = new PersistentTestProvider("fast");
        byte[] image = [.. "SQLite format 3 "u8, 7];
        await slow.Admin.ImportAsync("app.db", image, Ct);

        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "slow", Ct);

        var transports = new RecordingTransportFactory();
        var factory = Factory(
            store,
            transports,
            s => s.Prefer("fast").Fallback("slow").WithMigrationMode(BsqliteStorageMigrationMode.AutomaticOnOpen),
            fast,
            slow);

        await using var session = await factory.OpenAsync("app.db", Ct);

        Assert.AreEqual("fast", session.Resolution.Provider.Name);
        Assert.AreSame(fast, transports.Created[0].Provider);
        Assert.AreEqual("fast", await store.GetProviderNameAsync("app.db", Ct));
        CollectionAssert.AreEqual(image, await fast.Admin.ExportAsync("app.db", Ct));
    }

    [TestMethod]
    public async Task Dispose_TearsDownTheTransport()
    {
        var transports = new RecordingTransportFactory();
        var factory = Factory(
            new BsqliteInMemoryStorageBindingStore(),
            transports,
            s => s.Prefer("opfs"),
            Available("opfs"));

        var session = await factory.OpenAsync("app.db", Ct);
        await session.DisposeAsync();

        Assert.IsTrue(transports.Last.Disposed);
    }

    private enum DeleteBehavior
    {
        Deletes,

        /// <summary>Throws with the database untouched - an OPFS file another tab holds open.</summary>
        FailsLeavingIt,

        /// <summary>Removes the database, then throws - a related file that could not go.</summary>
        FailsAfterDeleting,
    }

    /// <summary>
    /// A persistent, available backend with a real (in-memory) admin surface, so a migration can be
    /// run against it. Optionally refuses every import, which is what an origin out of quota does,
    /// and fails deletes the ways <see cref="DeleteBehavior"/> lists.
    /// </summary>
    private sealed class PersistentTestProvider(
        string name,
        bool refusesImports = false,
        DeleteBehavior deletes = DeleteBehavior.Deletes)
        : IBsqliteStorageProvider
    {
        public string Name { get; } = name;

        public BsqliteStorageCapabilities Capabilities { get; } = new()
        {
            RequiredBuild = BsqliteEngineBuild.Synchronous,
            IsPersistent = true,
        };

        public BsqliteJsModule? VfsModule => null;

        public IBsqliteStorageAdmin Admin { get; } = new MemoryAdmin(refusesImports, deletes);

        public ValueTask<BsqliteProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(BsqliteProbeResult.Available());

        private sealed class MemoryAdmin(bool refusesImports, DeleteBehavior deletes) : IBsqliteStorageAdmin
        {
            private readonly Dictionary<string, byte[]> _images = new(StringComparer.Ordinal);

            public ValueTask<bool> ExistsAsync(string databaseName, CancellationToken cancellationToken = default)
                => ValueTask.FromResult(_images.ContainsKey(databaseName));

            public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
                => ValueTask.FromResult<IReadOnlyList<string>>([.. _images.Keys]);

            public ValueTask DeleteAsync(string databaseName, CancellationToken cancellationToken = default)
            {
                if (deletes is DeleteBehavior.FailsLeavingIt)
                {
                    throw new IOException("The file is in use.");
                }

                _images.Remove(databaseName);

                return deletes is DeleteBehavior.FailsAfterDeleting
                    ? throw new IOException("The journal is in use.")
                    : ValueTask.CompletedTask;
            }

            public ValueTask<byte[]> ExportAsync(string databaseName, CancellationToken cancellationToken = default)
                => _images.TryGetValue(databaseName, out var image)
                    ? ValueTask.FromResult(image.ToArray())
                    : throw new FileNotFoundException(databaseName);

            public ValueTask ImportAsync(
                string databaseName,
                ReadOnlyMemory<byte> contents,
                CancellationToken cancellationToken = default)
            {
                if (refusesImports)
                {
                    throw new BsqliteQuotaExceededException("The origin is out of storage.");
                }

                _images[databaseName] = contents.ToArray();
                return ValueTask.CompletedTask;
            }
        }
    }

    private static ConfigurableProvider Available(
        string name,
        BsqliteEngineBuild build = BsqliteEngineBuild.Synchronous,
        BsqliteJsModule? vfs = null)
        => new()
        {
            Name = name,
            RequiredBuild = build,
            Vfs = vfs,
        };

    private static BsqliteSessionFactory Factory(
        BsqliteInMemoryStorageBindingStore store,
        IBsqliteTransportFactory transports,
        Action<BsqliteStorageSelectionBuilder> configure,
        params IBsqliteStorageProvider[] providers)
        => new(
            new BsqliteStorageProviderResolver(providers, store),
            transports,
            BsqliteStorageSelectionBuilder.Create(configure),
            store);
}
