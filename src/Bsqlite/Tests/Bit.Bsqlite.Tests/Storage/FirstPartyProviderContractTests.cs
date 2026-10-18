using Bit.Bsqlite;
using Bit.Bsqlite.Interop;
using Bit.Bsqlite.Storage.CacheStorage;
using Bit.Bsqlite.Storage.IndexedDb;
using Bit.Bsqlite.Storage.InMemory;
using Bit.Bsqlite.Storage.Opfs;

namespace Bit.Bsqlite.Storage.Tests;

/// <summary>
/// S8's freeze check: the three first-party providers sit on the same contract without the core
/// knowing their names, and sticky binding still refuses to open an empty database elsewhere.
/// </summary>
[TestClass]
public sealed class FirstPartyProviderContractTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public void FirstPartyNames_AreTheOnesSelectionAlreadyUses()
    {
        Assert.AreEqual("in-memory", new BsqliteInMemoryStorageProvider().Name);
        Assert.AreEqual("opfs", new BsqliteOpfsStorageProvider().Name);
        Assert.AreEqual("indexeddb", new BsqliteIndexedDbStorageProvider().Name);
        Assert.AreEqual("cache-storage", new BsqliteCacheStorageProvider().Name);
    }

    [TestMethod]
    public void PersistentProviders_DoNotShareAName()
    {
        Assert.AreNotEqual(new BsqliteOpfsStorageProvider().Name, new BsqliteIndexedDbStorageProvider().Name);
    }

    [TestMethod]
    public async Task BoundToIndexedDb_DoesNotOpenOpfs_WhenIndexedDbCannotBeProbed()
    {
        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "indexeddb", Ct);

        var resolver = new BsqliteStorageProviderResolver(
            [new BsqliteOpfsStorageProvider(), new BsqliteIndexedDbStorageProvider()],
            store);

        var failure = await Assert.ThrowsExactlyAsync<BsqliteStorageUnavailableException>(
            () => resolver
                .ResolveAsync(
                    "app.db",
                    BsqliteStorageSelectionBuilder.Create(s => s.Prefer("opfs").Fallback("indexeddb")),
                    Ct)
                .AsTask());

        var attempt = Assert.ContainsSingle(failure.Attempts);
        Assert.AreEqual("indexeddb", attempt.ProviderName);
        Assert.Contains("will not open an empty database", attempt.Explanation!, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task BoundToOpfs_DoesNotOpenIndexedDb_WhenOpfsCannotBeProbed()
    {
        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "opfs", Ct);

        var resolver = new BsqliteStorageProviderResolver(
            [new BsqliteOpfsStorageProvider(), new BsqliteIndexedDbStorageProvider()],
            store);

        var failure = await Assert.ThrowsExactlyAsync<BsqliteStorageUnavailableException>(
            () => resolver
                .ResolveAsync(
                    "app.db",
                    BsqliteStorageSelectionBuilder.Create(s => s.Prefer("indexeddb").Fallback("opfs")),
                    Ct)
                .AsTask());

        var attempt = Assert.ContainsSingle(failure.Attempts);
        Assert.AreEqual("opfs", attempt.ProviderName);
        Assert.Contains("will not open an empty database", attempt.Explanation!, StringComparison.Ordinal);
    }
}
