using Bit.Bsqlite.Storage.InMemory;

namespace Bit.Bsqlite.Storage.Tests;

/// <summary>
/// What the in-memory backend promises beyond the contract. The generic rules - round-tripping,
/// replacement, detached exports, idempotent deletes - are checked by
/// <see cref="InMemoryStorageConformanceTests"/>, so this file covers only the choices specific to
/// this backend.
/// </summary>
[TestClass]
public sealed class InMemoryStorageProviderTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    private static readonly byte[] Image = [0x53, 0x51, 0x4C, 0x69, 0x74, 0x65, 0x00, 0x01];

    [TestMethod]
    public void DeclaresItselfVolatileAndSynchronous()
    {
        var provider = new BsqliteInMemoryStorageProvider();

        Assert.AreEqual("in-memory", provider.Name);
        Assert.IsFalse(provider.Capabilities.IsPersistent);
        Assert.AreEqual(BsqliteEngineBuild.Synchronous, provider.Capabilities.RequiredBuild);
        Assert.IsNull(provider.VfsModule);

        // A second worker is a second heap: nothing is shared, so nothing is claimed.
        Assert.IsFalse(provider.Capabilities.SupportsMultipleConnections);
        Assert.IsFalse(provider.Capabilities.SupportsConcurrentReads);
    }

    [TestMethod]
    public async Task ProbeIsAlwaysAvailable_AndExplainsItself()
    {
        var probe = await new BsqliteInMemoryStorageProvider().ProbeAsync(Ct);

        Assert.IsTrue(probe.IsAvailable);
        Assert.IsNull(probe.UnavailableReason);
        Assert.AreEqual("false", probe.Environment["persistent"]);
    }

    /// <summary>
    /// Ordering is this backend's own guarantee rather than a contract requirement, and it is what
    /// makes the diagnostics listing reproducible.
    /// </summary>
    [TestMethod]
    public async Task ListReturnsEveryDatabase_InAStableOrder()
    {
        var admin = new BsqliteInMemoryStorageProvider().Admin;
        await admin.ImportAsync("zebra.db", Image, Ct);
        await admin.ImportAsync("apple.db", Image, Ct);

        AssertSequence.Equal(["apple.db", "zebra.db"], await admin.ListAsync(Ct));
    }

    /// <summary>
    /// The contract only requires that exporting an absent database fails; this backend commits to the
    /// exception the migration protocol distinguishes a missing source by.
    /// </summary>
    [TestMethod]
    public async Task ExportingAnAbsentDatabase_ThrowsFileNotFound()
    {
        var admin = new BsqliteInMemoryStorageProvider().Admin;

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(
            () => admin.ExportAsync("never-existed.db", Ct).AsTask());
    }

    /// <summary>
    /// Each provider instance owns its own databases; sharing them through static state would make
    /// two independently configured stores silently interfere.
    /// </summary>
    [TestMethod]
    public async Task ProviderInstancesDoNotShareStorage()
    {
        var first = new BsqliteInMemoryStorageProvider();
        var second = new BsqliteInMemoryStorageProvider();

        await first.Admin.ImportAsync("app.db", Image, Ct);

        Assert.IsFalse(await second.Admin.ExistsAsync("app.db", Ct));
    }
}
