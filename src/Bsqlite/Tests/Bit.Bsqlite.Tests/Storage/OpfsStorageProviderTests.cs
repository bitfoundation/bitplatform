using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Storage.Opfs;

namespace Bit.Bsqlite.Storage.Tests;

[TestClass]
public sealed class OpfsStorageProviderTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void Name_IsTheStickyBindingKey()
    {
        Assert.AreEqual("opfs", new BsqliteOpfsStorageProvider().Name);
        Assert.AreEqual(BsqliteOpfsStorageProvider.ProviderName, new BsqliteOpfsStorageProvider().Name);
    }

    [TestMethod]
    public void Capabilities_MatchOpfsCoopSync()
    {
        var capabilities = new BsqliteOpfsStorageProvider().Capabilities;

        Assert.AreEqual(BsqliteEngineBuild.Synchronous, capabilities.RequiredBuild);
        Assert.IsTrue(capabilities.IsPersistent);
        Assert.IsTrue(capabilities.SupportsMultipleConnections);
        Assert.IsFalse(capabilities.SupportsConcurrentReads);
        Assert.IsFalse(capabilities.SupportsRelaxedDurability);
        Assert.IsFalse(capabilities.SupportsMultiDatabaseTransactions);
        Assert.IsTrue(capabilities.CanChangePageSize);
        Assert.AreEqual(BsqliteExecutionContexts.DedicatedWorker, capabilities.SupportedContexts);
    }

    [TestMethod]
    public void VfsModule_IsDocumentRelative_LikeEveryOtherBlazorAsset()
    {
        var vfs = new BsqliteOpfsStorageProvider().VfsModule;

        Assert.IsNotNull(vfs);
        Assert.AreEqual(BsqliteOpfsStorageProvider.VfsModuleUrl, vfs.ModuleUrl);
        // Relative to the document, not the origin root: an application hosted under a sub-path must
        // still find it. The host resolves it against the document base before the worker imports it.
        Assert.StartsWith("./_content/", vfs.ModuleUrl, StringComparison.Ordinal);
        Assert.AreEqual("register", vfs.RegisterExport);
    }

    [TestMethod]
    public async Task Probe_WithoutJavaScript_ReportsUnavailable()
    {
        var probe = await new BsqliteOpfsStorageProvider().ProbeAsync(TestContext.CancellationToken);

        Assert.IsFalse(probe.IsAvailable);
        Assert.Contains("browser", probe.UnavailableReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.AreEqual("false", probe.Environment["javascript"]);
    }
}
