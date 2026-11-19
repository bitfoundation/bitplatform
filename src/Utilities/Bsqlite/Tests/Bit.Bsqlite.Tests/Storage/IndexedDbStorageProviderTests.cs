namespace Bit.Bsqlite.Storage.Tests;

[TestClass]
public sealed class IndexedDbStorageProviderTests
{
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// The VFS keeps 4096-byte blocks, so a database of other pages, imported as such blocks, turned up
    /// malformed after its first write. Restore and migration onto this backend now refuse it up front;
    /// the import refuses it before anything reaches the browser.
    /// </summary>
    [TestMethod]
    [DataRow(512)]
    [DataRow(1024)]
    [DataRow(8192)]
    [DataRow(65536)]
    public async Task Import_RefusesADatabaseWhosePagesAreNot4096Bytes(int pageSize)
    {
        var admin = new BsqliteIndexedDbStorageProvider().Admin;

        var error = await Assert.ThrowsExactlyAsync<NotSupportedException>(
            async () => await admin.ImportAsync("app.db", SqliteImages.Valid(pageSize: pageSize), TestContext.CancellationToken));

        StringAssert.Contains(error.Message, $"{pageSize}-byte pages");
    }

    [TestMethod]
    public void Import_TakesA4096BytePageDatabase_AndBytesThatAreNoDatabase()
    {
        BsqliteIndexedDbStorageAdmin.ThrowIfPageSizeUnsupported(SqliteImages.Valid(pageSize: 4096));
        BsqliteIndexedDbStorageAdmin.ThrowIfPageSizeUnsupported([]);
        BsqliteIndexedDbStorageAdmin.ThrowIfPageSizeUnsupported([1, 2, 3]);
    }

    [TestMethod]
    public void Name_IsTheStickyBindingKey()
    {
        Assert.AreEqual("indexeddb", new BsqliteIndexedDbStorageProvider().Name);
        Assert.AreEqual(BsqliteIndexedDbStorageProvider.ProviderName, new BsqliteIndexedDbStorageProvider().Name);
    }

    [TestMethod]
    public void Capabilities_MatchIdbBatchAtomic()
    {
        var capabilities = new BsqliteIndexedDbStorageProvider().Capabilities;

        Assert.AreEqual(BsqliteEngineBuild.AsyncCapable, capabilities.RequiredBuild);
        Assert.IsTrue(capabilities.IsPersistent);
        Assert.IsTrue(capabilities.SupportsMultipleConnections);

        // bit-bsqlite-idb-vfs.js registers the VFS without a lock policy, so WebLocksMixin's 'exclusive'
        // default applies and reads serialize behind each other like writes do.
        Assert.IsFalse(capabilities.SupportsConcurrentReads);

        Assert.IsTrue(capabilities.SupportsRelaxedDurability);
        Assert.IsTrue(capabilities.SupportsMultiDatabaseTransactions);
        Assert.IsFalse(capabilities.CanChangePageSize);
        Assert.IsTrue(capabilities.SupportedContexts.HasFlag(BsqliteExecutionContexts.DedicatedWorker));
        Assert.IsTrue(capabilities.SupportedContexts.HasFlag(BsqliteExecutionContexts.Window));
    }

    [TestMethod]
    public void VfsModule_IsDocumentRelative_LikeEveryOtherBlazorAsset()
    {
        var vfs = new BsqliteIndexedDbStorageProvider().VfsModule;

        Assert.IsNotNull(vfs);
        Assert.AreEqual(BsqliteIndexedDbStorageProvider.VfsModuleUrl, vfs.ModuleUrl);
        // Relative to the document, not the origin root: an application hosted under a sub-path must
        // still find it. The host resolves it against the document base before the worker imports it.
        Assert.StartsWith("./_content/", vfs.ModuleUrl, StringComparison.Ordinal);
        Assert.AreEqual("register", vfs.RegisterExport);
    }

    [TestMethod]
    public async Task Probe_WithoutJavaScript_ReportsUnavailable()
    {
        var probe = await new BsqliteIndexedDbStorageProvider().ProbeAsync(TestContext.CancellationToken);

        Assert.IsFalse(probe.IsAvailable);
        Assert.Contains("browser", probe.UnavailableReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.AreEqual("false", probe.Environment["javascript"]);
    }
}
