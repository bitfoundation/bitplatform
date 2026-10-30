using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Storage.InMemory;
using Bit.Bsqlite.Testing;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// How an application moving off Bit.Besql keeps its users' data while new users start on the preferred
/// backend: the backend holding the old database is registered but not preferred, and
/// <see cref="BsqliteStorageMigrationMode.AutomaticOnOpen"/> moves the database on its first open.
/// </summary>
[TestClass]
public sealed class SessionFactoryMigrationTests
{
    private const string Database = "App_Data/AppOffline.db";

    private static readonly byte[] Image =
    [
        .. "SQLite format 3\0"u8,
        .. Enumerable.Repeat((byte)7, 48),
    ];

    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    /// <summary>
    /// A database with no binding is looked for on every registered backend before a new one is created, so the
    /// one Bit.Besql left is found although its backend is not in the preference - and then moved to the preferred one.
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

    private static BsqliteSessionFactory Factory(IBsqliteStorageBindingStore store, params IBsqliteStorageProvider[] providers)
        => new(
            new BsqliteStorageProviderResolver(providers, store),
            new InProcessTransportFactory(),
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
}
