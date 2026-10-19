using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Storage.IndexedDb;

namespace Bit.Bsqlite.Storage.Tests;

[TestClass]
public sealed class IndexedDbStorageProviderTests
{
    public TestContext TestContext { get; set; } = null!;

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
