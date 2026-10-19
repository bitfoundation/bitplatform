using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Storage.CacheStorage;

namespace Bit.Bsqlite.Storage.Tests;

[TestClass]
public sealed class CacheStorageProviderTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void Name_IsTheStickyBindingKey()
        => Assert.AreEqual("cache-storage", new BsqliteCacheStorageProvider().Name);

    [TestMethod]
    public void Capabilities_MatchJournalBasedCacheStorage()
    {
        var capabilities = new BsqliteCacheStorageProvider().Capabilities;

        Assert.AreEqual(BsqliteEngineBuild.AsyncCapable, capabilities.RequiredBuild);
        Assert.IsTrue(capabilities.IsPersistent);
        Assert.IsTrue(capabilities.SupportsMultipleConnections);
        Assert.IsTrue(capabilities.SupportsConcurrentReads);
        Assert.IsFalse(capabilities.SupportsRelaxedDurability);
        Assert.IsTrue(capabilities.SupportsMultiDatabaseTransactions);
        Assert.IsFalse(capabilities.CanChangePageSize);
    }

    [TestMethod]
    public async Task Probe_WithoutJavaScript_ReportsUnavailable()
    {
        var probe = await new BsqliteCacheStorageProvider().ProbeAsync(TestContext.CancellationToken);

        Assert.IsFalse(probe.IsAvailable);
        Assert.Contains("browser", probe.UnavailableReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
