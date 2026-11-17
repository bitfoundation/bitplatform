namespace Bit.Bsqlite.Interop.Tests;

[TestClass]
public sealed class StorageMigratorTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    private static readonly byte[] Image = SqliteImages.Valid();

    /// <summary>
    /// A database the target cannot keep was refused only at import, after the whole of it had been exported -
    /// and on the automatic path that export was repeated on every open. It is now refused on its header.
    /// </summary>
    [TestMethod]
    public async Task ADatabaseTheTargetCannotKeep_IsRefusedOnItsHeader_BeforeItIsExported()
    {
        var source = new HeaderProvider("source-memory", refuses: false);
        var target = new HeaderProvider("target-memory", refuses: true);
        var store = new BsqliteInMemoryStorageBindingStore();
        await source.Admin.ImportAsync("app.db", Image, Ct);

        await Assert.ThrowsExactlyAsync<NotSupportedException>(
            () => new BsqliteStorageMigrator().MigrateAsync("app.db", source, target, store, Ct));

        Assert.AreEqual(1, source.HeaderAdmin.HeaderReads);
        Assert.AreEqual(0, source.HeaderAdmin.Exports);
        Assert.IsTrue(await source.Admin.ExistsAsync("app.db", Ct));
        Assert.IsFalse(await target.Admin.ExistsAsync("app.db", Ct));
        Assert.IsNull(await store.GetProviderNameAsync("app.db", Ct));
    }

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

    /// <summary>
    /// The binding write landed and only its report failed - a cancel that fired once the interop
    /// call was out, say. The binding names the copy now, so dropping the copy as unbound would
    /// leave the binding pointing at nothing.
    /// </summary>
    [TestMethod]
    public async Task ABindingWriteThatLandedButReportedFailure_KeepsTheCopy()
    {
        var source = new BsqliteInMemoryStorageProvider("source-memory");
        var target = new BsqliteInMemoryStorageProvider("target-memory");
        var store = new WritesThenThrowsBindingStore();
        await source.Admin.ImportAsync("app.db", Image, Ct);

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(
            () => new BsqliteStorageMigrator().MigrateAsync("app.db", source, target, store, Ct));

        Assert.AreEqual(target.Name, await store.GetProviderNameAsync("app.db", Ct));
        CollectionAssert.AreEqual(Image, await target.Admin.ExportAsync("app.db", Ct));
    }

    /// <summary>
    /// The move would delete the only copy that survives a reload, and leave the binding on storage that
    /// is empty by the next load.
    /// </summary>
    [TestMethod]
    public async Task AMoveFromPersistentStorageOntoStorageThatDoesNotPersist_IsRefused()
    {
        var source = new Storage.Tests.FakeStorageProvider("disk", holds: "app.db");
        var target = new BsqliteInMemoryStorageProvider("target-memory");
        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "disk", Ct);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => new BsqliteStorageMigrator().MigrateAsync("app.db", source, target, store, Ct));

        Assert.AreEqual("disk", await store.GetProviderNameAsync("app.db", Ct));
        Assert.AreEqual(0, source.ProbeCount + source.ExistsCount, "Refused before touching anything.");
    }

    [TestMethod]
    public async Task AnExportThatIsCutShort_IsRefusedBeforeTheTargetIsWritten()
    {
        var source = new BsqliteInMemoryStorageProvider("source-memory");
        var target = new BsqliteInMemoryStorageProvider("target-memory");
        var store = new BsqliteInMemoryStorageBindingStore();
        await source.Admin.ImportAsync("app.db", SqliteImages.Valid(pages: 4)[..3000], Ct);

        await Assert.ThrowsExactlyAsync<BsqliteCorruptDatabaseException>(
            () => new BsqliteStorageMigrator().MigrateAsync("app.db", source, target, store, Ct));

        Assert.IsFalse(await target.Admin.ExistsAsync("app.db", Ct));
        Assert.IsTrue(await source.Admin.ExistsAsync("app.db", Ct));
        Assert.IsNull(await store.GetProviderNameAsync("app.db", Ct));
    }

    /// <summary>
    /// A file a device left in WAL mode opens nowhere a web VFS serves it; the copy is marked for a rollback
    /// journal on the way across, and verified against what was written, not against the source.
    /// </summary>
    [TestMethod]
    public async Task AWalModeExport_IsMovedAsARollbackJournalDatabase()
    {
        var source = new BsqliteInMemoryStorageProvider("source-memory");
        var target = new BsqliteInMemoryStorageProvider("target-memory");
        var store = new BsqliteInMemoryStorageBindingStore();
        await source.Admin.ImportAsync("app.db", SqliteImages.Valid(walMode: true), Ct);

        await new BsqliteStorageMigrator().MigrateAsync("app.db", source, target, store, Ct);

        CollectionAssert.AreEqual(SqliteImages.Valid(), await target.Admin.ExportAsync("app.db", Ct));
        Assert.AreEqual(target.Name, await store.GetProviderNameAsync("app.db", Ct));
    }

    /// <summary>
    /// Another tab moved the database while this one was exporting it: the binding no longer names the
    /// source, and the target holds the other tab's copy - which it may be writing to. This tab's older
    /// export is not imported over it.
    /// </summary>
    [TestMethod]
    public async Task AMoveAnotherTabFinishedFirst_IsLeftAlone()
    {
        var source = new BsqliteInMemoryStorageProvider("source-memory");
        var target = new BsqliteInMemoryStorageProvider("target-memory");
        var store = new BsqliteInMemoryStorageBindingStore();
        await source.Admin.ImportAsync("app.db", SqliteImages.Valid(fill: 1), Ct);
        var theirs = SqliteImages.Valid(fill: 2);
        await target.Admin.ImportAsync("app.db", theirs, Ct);
        await store.SetProviderNameAsync("app.db", target.Name, Ct);

        await new BsqliteStorageMigrator().MigrateAsync("app.db", source, target, store, Ct);

        CollectionAssert.AreEqual(theirs, await target.Admin.ExportAsync("app.db", Ct));
        Assert.AreEqual(target.Name, await store.GetProviderNameAsync("app.db", Ct));
    }

    /// <summary>A binding store whose writes land and then report a cancellation.</summary>
    private sealed class WritesThenThrowsBindingStore : IBsqliteStorageBindingStore
    {
        private readonly BsqliteInMemoryStorageBindingStore _inner = new();

        public ValueTask<string?> GetProviderNameAsync(string databaseName, CancellationToken cancellationToken = default)
            => _inner.GetProviderNameAsync(databaseName, cancellationToken);

        public async ValueTask SetProviderNameAsync(string databaseName, string providerName, CancellationToken cancellationToken = default)
        {
            await _inner.SetProviderNameAsync(databaseName, providerName, cancellationToken);
            throw new TaskCanceledException();
        }

        public ValueTask RemoveAsync(string databaseName, CancellationToken cancellationToken = default)
            => _inner.RemoveAsync(databaseName, cancellationToken);
    }

    /// <summary>An in-memory backend whose admin reads headers, counts what it reads, and may refuse every import.</summary>
    private sealed class HeaderProvider(string name, bool refuses) : IBsqliteStorageProvider
    {
        private readonly BsqliteInMemoryStorageProvider _inner = new(name);
        private HeaderAdmin? _admin;

        public HeaderAdmin HeaderAdmin => _admin ??= new HeaderAdmin(_inner.Admin, refuses);

        public string Name => name;

        public BsqliteStorageCapabilities Capabilities => _inner.Capabilities;

        public BsqliteJsModule? VfsModule => null;

        public IBsqliteStorageAdmin Admin => HeaderAdmin;

        public ValueTask<BsqliteProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(BsqliteProbeResult.Available());
    }

    private sealed class HeaderAdmin(IBsqliteStorageAdmin inner, bool refuses)
        : IBsqliteStorageAdmin, IBsqliteStorageHeaderReader, IBsqliteStorageImportCheck
    {
        public int HeaderReads { get; private set; }

        public int Exports { get; private set; }

        public ValueTask<bool> ExistsAsync(string databaseName, CancellationToken cancellationToken = default)
            => inner.ExistsAsync(databaseName, cancellationToken);

        public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
            => inner.ListAsync(cancellationToken);

        public ValueTask DeleteAsync(string databaseName, CancellationToken cancellationToken = default)
            => inner.DeleteAsync(databaseName, cancellationToken);

        public ValueTask<byte[]> ExportAsync(string databaseName, CancellationToken cancellationToken = default)
        {
            Exports++;
            return inner.ExportAsync(databaseName, cancellationToken);
        }

        public ValueTask ImportAsync(string databaseName, ReadOnlyMemory<byte> contents, CancellationToken cancellationToken = default)
            => inner.ImportAsync(databaseName, contents, cancellationToken);

        public async ValueTask<byte[]?> ReadHeaderAsync(string databaseName, CancellationToken cancellationToken = default)
        {
            HeaderReads++;
            return await inner.ExistsAsync(databaseName, cancellationToken)
                ? (await inner.ExportAsync(databaseName, cancellationToken))[..100]
                : null;
        }

        public void ThrowIfCannotImport(ReadOnlySpan<byte> header)
        {
            if (refuses)
            {
                throw new NotSupportedException("Refused on its header.");
            }
        }
    }
}
