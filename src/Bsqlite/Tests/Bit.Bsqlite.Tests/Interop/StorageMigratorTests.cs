using Bit.Bsqlite.Data;
using Bit.Bsqlite.Interop;
using Bit.Bsqlite.Storage.InMemory;

namespace Bit.Bsqlite.Interop.Tests;

[TestClass]
public sealed class StorageMigratorTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    private static readonly byte[] Image =
    [
        .. "SQLite format 3\0"u8,
        .. Enumerable.Repeat((byte)7, 48),
    ];

    [TestMethod]
    public async Task CopiesTheImage_FlipsTheBinding_AndDeletesTheSource()
    {
        var source = new BsqliteInMemoryStorageProvider("source-memory");
        var target = new BsqliteInMemoryStorageProvider("target-memory");
        var store = new BsqliteInMemoryStorageBindingStore();
        await source.Admin.ImportAsync("app.db", Image, Ct);

        await new BsqliteStorageMigrator().MigrateAsync("app.db", source, target, store, Ct);

        Assert.IsFalse(await source.Admin.ExistsAsync("app.db", Ct));
        CollectionAssert.AreEqual(Image, await target.Admin.ExportAsync("app.db", Ct));
        Assert.AreEqual(target.Name, await store.GetProviderNameAsync("app.db", Ct));
    }

    /// <summary>
    /// There is nothing to copy, so the binding moves and the open continues. Exporting anyway
    /// would throw, and on the <c>AutomaticOnOpen</c> path that turns "a better backend became
    /// available" into a database the application can no longer open at all.
    /// </summary>
    [TestMethod]
    public async Task ASourceHoldingNothing_MovesTheBindingWithoutFailing()
    {
        var source = new BsqliteInMemoryStorageProvider("source-memory");
        var target = new BsqliteInMemoryStorageProvider("target-memory");
        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", source.Name, Ct);

        await new BsqliteStorageMigrator().MigrateAsync("app.db", source, target, store, Ct);

        Assert.AreEqual(target.Name, await store.GetProviderNameAsync("app.db", Ct));
        Assert.IsFalse(await target.Admin.ExistsAsync("app.db", Ct));
    }

    [TestMethod]
    public async Task ACorruptImage_LeavesTheSourceAndBindingUntouched()
    {
        var source = new BsqliteInMemoryStorageProvider("source-memory");
        var target = new BsqliteInMemoryStorageProvider("target-memory");
        var store = new BsqliteInMemoryStorageBindingStore();
        await source.Admin.ImportAsync("app.db", new byte[] { 1, 2, 3, 4 }, Ct);

        await Assert.ThrowsExactlyAsync<BsqliteCorruptDatabaseException>(
            () => new BsqliteStorageMigrator().MigrateAsync("app.db", source, target, store, Ct));

        Assert.IsTrue(await source.Admin.ExistsAsync("app.db", Ct));
        Assert.IsFalse(await target.Admin.ExistsAsync("app.db", Ct));
        Assert.IsNull(await store.GetProviderNameAsync("app.db", Ct));
    }
}
