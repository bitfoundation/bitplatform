using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Storage.InMemory;
using Bit.Bsqlite.Testing;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// How an application that changes the backend it prefers keeps its users' data while new users start on the
/// preferred backend: the backend holding the old database is registered but not preferred, and
/// <see cref="BsqliteStorageMigrationMode.AutomaticOnOpen"/> moves the database on its first open.
/// </summary>
[TestClass]
public sealed class SessionFactoryMigrationTests
{
    private const string Database = "App_Data/AppOffline.db";

    private static readonly byte[] Image = SqliteImages.Valid();

    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    /// <summary>
    /// A database with no binding is looked for on every registered backend before a new one is created, so the
    /// one an earlier version left is found although its backend is not in the preference - and then moved to the preferred one.
    /// </summary>
    [TestMethod]
    public async Task ADatabaseOnABackendNotPreferred_IsFound_AndMovedOnFirstOpen()
    {
        var opfs = new PersistentProvider("opfs");
        var indexedDb = new PersistentProvider("indexeddb");
        var cacheStorage = new PersistentProvider("cache-storage");
        await cacheStorage.Admin.ImportAsync(Database, Image, Ct);
        var store = new BsqliteInMemoryStorageBindingStore();

        var factory = Factory(store, opfs, indexedDb, cacheStorage);
        await using (var session = await factory.OpenAsync(Database, Ct))
        {
            Assert.AreEqual("opfs", session.Resolution.Provider.Name);
        }

        CollectionAssert.AreEqual(Image, await opfs.Admin.ExportAsync(Database, Ct));
        Assert.IsFalse(await cacheStorage.Admin.ExistsAsync(Database, Ct));
        Assert.IsFalse(await indexedDb.Admin.ExistsAsync(Database, Ct));
        Assert.AreEqual("opfs", await store.GetProviderNameAsync(Database, Ct));
    }

    [TestMethod]
    public async Task ANewDatabase_StartsOnThePreferredBackend()
    {
        var opfs = new PersistentProvider("opfs");
        var cacheStorage = new PersistentProvider("cache-storage");
        var store = new BsqliteInMemoryStorageBindingStore();

        await using var session = await Factory(store, opfs, cacheStorage).OpenAsync(Database, Ct);

        Assert.AreEqual("opfs", session.Resolution.Provider.Name);
        Assert.IsFalse(session.Resolution.WasDecidedByExistingData);
    }

    /// <summary>
    /// Two tabs opening a database that moves on open used to run side by side - both exporting the source and
    /// importing it over each other on the target - with nothing between them. The open now runs under a lock on
    /// the database's name, taken before the binding is read and let go once the database is open.
    /// </summary>
    [TestMethod]
    public async Task AnOpenThatMayMigrate_RunsUnderTheOpenLock_FromResolvingToOpening()
    {
        var opfs = new PersistentProvider("opfs");
        var cacheStorage = new PersistentProvider("cache-storage");
        await cacheStorage.Admin.ImportAsync(Database, Image, Ct);
        var store = new BsqliteInMemoryStorageBindingStore();
        var transports = new LockingTransportFactory(() => store.GetProviderNameAsync(Database, Ct).AsTask());

        await using (var session = await Factory(store, transports, opfs, cacheStorage).OpenAsync(Database, Ct))
        {
            Assert.AreEqual("opfs", session.Resolution.Provider.Name);
        }

        AssertSequence.Equal(
            [$"lock {Database}", "binding when locked: ", "open", "release, binding: opfs"],
            transports.Events);
    }

    private static BsqliteSessionFactory Factory(IBsqliteStorageBindingStore store, params IBsqliteStorageProvider[] providers)
        => Factory(store, new InProcessTransportFactory(), providers);

    private static BsqliteSessionFactory Factory(
        IBsqliteStorageBindingStore store,
        IBsqliteTransportFactory transports,
        params IBsqliteStorageProvider[] providers)
        => new(
            new BsqliteStorageProviderResolver(providers, store),
            transports,
            BsqliteStorageSelectionBuilder.Create(s => s
                .Prefer("opfs")
                .Fallback("indexeddb")
                .WithMigrationMode(BsqliteStorageMigrationMode.AutomaticOnOpen)),
            store);

    /// <summary>The in-memory backend's admin surface, under a persistent backend's name.</summary>
    private sealed class PersistentProvider(string name) : IBsqliteStorageProvider
    {
        private readonly BsqliteInMemoryStorageProvider _inner = new(name);

        public string Name => name;

        public BsqliteStorageCapabilities Capabilities { get; } = new()
        {
            RequiredBuild = BsqliteEngineBuild.Synchronous,
            IsPersistent = true,
            SupportedContexts = BsqliteExecutionContexts.DedicatedWorker,
        };

        public BsqliteJsModule? VfsModule => null;

        public IBsqliteStorageAdmin Admin => _inner.Admin;

        public ValueTask<BsqliteProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(BsqliteProbeResult.Available());
    }

    private sealed class InProcessTransportFactory : IBsqliteTransportFactory
    {
        public IBsqliteTransport Create(IBsqliteStorageProvider provider) => new BsqliteInProcessTransport();
    }

    /// <summary>Records when the open lock is taken and let go, and what the binding said at each point.</summary>
    private sealed class LockingTransportFactory(Func<Task<string?>> binding) : IBsqliteTransportFactory, IBsqliteOpenLock
    {
        private readonly Func<Task<string?>> _binding = binding;

        public List<string> Events { get; } = [];

        public IBsqliteTransport Create(IBsqliteStorageProvider provider)
        {
            Events.Add("open");
            return new BsqliteInProcessTransport();
        }

        public async Task<IAsyncDisposable?> AcquireOpenLockAsync(string databaseName, CancellationToken cancellationToken)
        {
            Events.Add($"lock {databaseName}");
            Events.Add($"binding when locked: {await _binding()}");
            return new Release(this);
        }

        private sealed class Release(LockingTransportFactory owner) : IAsyncDisposable
        {
            public async ValueTask DisposeAsync() => owner.Events.Add($"release, binding: {await owner._binding()}");
        }
    }
}
