using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Bsqlite.Storage.ConformanceTests;

/// <summary>
/// The conformance suite every storage backend must pass. Inherit it in your provider's test project
/// and return your provider from <see cref="CreateProvider"/>; the tests come with the base class.
/// </summary>
/// <remarks>
/// <para>
/// The core trusts <see cref="IBsqliteStorageProvider.Capabilities"/> without verifying it -
/// selection, pragma guarding, and durability options are all driven by what a backend claims. This
/// suite is what makes that trust reasonable: it checks that the claims are internally coherent and
/// that the admin surface behaves the way cross-provider migration assumes.
/// </para>
/// <para>
/// It covers what can be checked without a running engine. Claims that only a real database can
/// settle - write atomicity, crash safety, the concurrency levels - are verified by the engine-level
/// suite, which needs the worker host.
/// </para>
/// </remarks>
public abstract class BsqliteStorageProviderConformanceTests : IAsyncDisposable
{
    /// <summary>Set by MSTest for each test; absent when a rule is called directly.</summary>
    public TestContext? TestContext { get; set; }

    private readonly List<IBsqliteStorageProvider> _created = [];

    private CancellationToken Ct => TestContext?.CancellationToken ?? CancellationToken.None;

    /// <summary>
    /// Returns a provider to test. Called more than once per run, and each call must return an
    /// instance that behaves like a fresh one in a fresh browser session.
    /// </summary>
    /// <remarks>
    /// Instances that implement <see cref="IAsyncDisposable"/> or <see cref="IDisposable"/> are
    /// disposed for you, so a backend holding file handles can be tested without leaking them.
    /// </remarks>
    protected abstract IBsqliteStorageProvider CreateProvider();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var provider in _created)
        {
            switch (provider)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }

        _created.Clear();
        GC.SuppressFinalize(this);
    }

    private IBsqliteStorageProvider Provider()
    {
        var provider = CreateProvider();
        Assert.IsNotNull(provider);
        _created.Add(provider);
        return provider;
    }

    /// <summary>
    /// Skips the calling test when the backend cannot run here, so that the suite reports honestly on
    /// a browser or OS that lacks the underlying API instead of failing for the wrong reason.
    /// </summary>
    private async ValueTask<IBsqliteStorageAdmin> AdminAsync(
        IBsqliteStorageProvider provider)
    {
        var probe = await provider.ProbeAsync(Ct);
        SkipUnless(probe.IsAvailable,
            $"'{provider.Name}' is unavailable here: {probe.UnavailableReason}");

        return provider.Admin;
    }

    /// <summary>
    /// Reports the calling test as inconclusive - MSTest's skip - with <paramref name="reason"/> when
    /// <paramref name="condition"/> does not hold.
    /// </summary>
    private static void SkipUnless(bool condition, string reason)
    {
        if (!condition)
        {
            Assert.Inconclusive(reason);
        }
    }

    /// <summary>
    /// Builds a plausible SQLite file image of the requested size. It carries the real file magic so
    /// that a backend which validates what it is handed still sees a valid database, and its body is
    /// deterministic per seed so a failure names the byte that differed.
    /// </summary>
    private static byte[] Image(int length, int seed)
    {
        var bytes = new byte[length];
        new Random(seed).NextBytes(bytes);

        ReadOnlySpan<byte> magic = "SQLite format 3\u0000"u8;
        if (length >= magic.Length)
        {
            magic.CopyTo(bytes);
        }

        return bytes;
    }

    // ---------------------------------------------------------------------------------------------
    // Declaration: the facts the core reads once and then relies on everywhere.
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public void Name_IsNotBlank()
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(Provider().Name));
    }

    /// <summary>
    /// The name is recorded as the sticky binding of every database the backend creates, so an
    /// instance-dependent name would orphan data on the next session.
    /// </summary>
    [TestMethod]
    public void Name_IsTheSameForEveryInstance()
    {
        Assert.AreEqual(Provider().Name, Provider().Name);
    }

    /// <summary>
    /// Capabilities are read once and cached, and probing must not change them - a backend that
    /// downgrades itself after probing would have already been selected on the stronger claim.
    /// </summary>
    [TestMethod]
    public async Task Capabilities_DoNotChangeWhenProbed()
    {
        var provider = Provider();
        var before = provider.Capabilities;

        await provider.ProbeAsync(Ct);

        Assert.AreEqual(before, provider.Capabilities);
    }

    [TestMethod]
    public void Capabilities_DeclareAtLeastOneExecutionContext()
    {
        var provider = Provider();

        Assert.AreNotEqual(
            BsqliteExecutionContexts.None,
            provider.Capabilities.SupportedContexts);
    }

    /// <summary>
    /// v1 hosts the engine only in a dedicated worker, so a backend that cannot run there cannot be
    /// selected at all. Asserted rather than left to fail later at connection time.
    /// </summary>
    [TestMethod]
    public void Capabilities_SupportTheDedicatedWorker()
    {
        var provider = Provider();

        Assert.IsTrue(
            provider.Capabilities.SupportedContexts.HasFlag(
                BsqliteExecutionContexts.DedicatedWorker),
            $"'{provider.Name}' cannot run in a dedicated worker, which is the only context that "
                + "hosts the engine.");
    }

    /// <summary>
    /// Relaxed durability trades crash safety for speed. There is nothing to trade when the data does
    /// not outlive the page, and offering the option would imply a persistence guarantee.
    /// </summary>
    [TestMethod]
    public void NonPersistentBackend_DoesNotOfferRelaxedDurability()
    {
        var capabilities = Provider().Capabilities;

        Assert.IsFalse(
            !capabilities.IsPersistent && capabilities.SupportsRelaxedDurability,
            "A backend whose data does not survive a reload cannot offer a durability level.");
    }

    /// <summary>
    /// A VFS needing the async engine build is necessarily one we load as JavaScript; the only VFS
    /// available without a module is the one compiled into the engine, which is synchronous.
    /// </summary>
    [TestMethod]
    public void BackendNeedingTheAsyncBuild_ShipsAVfsModule()
    {
        var provider = Provider();

        if (provider.Capabilities.RequiredBuild is BsqliteEngineBuild.Synchronous)
        {
            return;
        }

        Assert.IsNotNull(provider.VfsModule);
    }

    // ---------------------------------------------------------------------------------------------
    // Probing: selection walks the candidate list, so a probe must answer rather than abort the walk.
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Selection depends on being able to try a candidate and move on. A throwing probe is tolerated
    /// by the resolver but loses the explanation, so the contract asks backends to report instead.
    /// </summary>
    [TestMethod]
    public async Task ProbeAsync_ReportsUnavailability_RatherThanThrowing()
    {
        var provider = Provider();

        var probe = await provider.ProbeAsync(Ct);

        Assert.IsNotNull(probe);
    }

    [TestMethod]
    public async Task ProbeAsync_GivesTheSameVerdictEachTime()
    {
        var provider = Provider();

        var first = await provider.ProbeAsync(Ct);
        var second = await provider.ProbeAsync(Ct);

        Assert.AreEqual(first.IsAvailable, second.IsAvailable);
    }

    // ---------------------------------------------------------------------------------------------
    // Admin: the operations diagnostics and cross-provider migration are built from.
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Exists_IsFalse_ForADatabaseNeverCreated()
    {
        var admin = await AdminAsync(Provider());

        Assert.IsFalse(await admin.ExistsAsync($"absent-{Guid.NewGuid():N}", Ct));
    }

    [TestMethod]
    public async Task Import_MakesTheDatabaseExistAndAppearInTheList()
    {
        var admin = await AdminAsync(Provider());
        var name = $"conformance-{Guid.NewGuid():N}";

        await admin.ImportAsync(name, Image(4096, seed: 1), Ct);

        Assert.IsTrue(await admin.ExistsAsync(name, Ct));
        Assert.Contains(name, await admin.ListAsync(Ct));
    }

    [TestMethod]
    public async Task Export_ReturnsExactlyWhatWasImported()
    {
        var admin = await AdminAsync(Provider());
        var name = $"conformance-{Guid.NewGuid():N}";
        var image = Image(4096, seed: 2);

        await admin.ImportAsync(name, image, Ct);

        CollectionAssert.AreEqual(image, await admin.ExportAsync(name, Ct));
    }

    /// <summary>
    /// Migration copies a database that is about to be written to, so the exported image has to be a
    /// snapshot the caller owns rather than a window onto live storage.
    /// </summary>
    [TestMethod]
    public async Task Export_ReturnsACopy_NotAViewOfLiveStorage()
    {
        var admin = await AdminAsync(Provider());
        var name = $"conformance-{Guid.NewGuid():N}";
        var image = Image(4096, seed: 3);
        await admin.ImportAsync(name, image, Ct);

        var exported = await admin.ExportAsync(name, Ct);
        exported[^1] ^= 0xFF;

        CollectionAssert.AreEqual(image, await admin.ExportAsync(name, Ct));
    }

    /// <summary>
    /// Crossing several SQLite page boundaries, which is where a block-oriented backend splits and
    /// reassembles an image and where off-by-one block bugs surface.
    /// </summary>
    [TestMethod]
    public async Task Export_RoundTripsAnImageSpanningManyPages()
    {
        var admin = await AdminAsync(Provider());
        var name = $"conformance-{Guid.NewGuid():N}";
        var image = Image(384 * 1024 + 517, seed: 4);

        await admin.ImportAsync(name, image, Ct);

        CollectionAssert.AreEqual(image, await admin.ExportAsync(name, Ct));
    }

    /// <summary>
    /// A newly created SQLite database is a zero-length file, so migration and backup both encounter
    /// empty images. Importing one must produce an existing, empty database rather than nothing.
    /// </summary>
    [TestMethod]
    public async Task Import_AcceptsAnEmptyImage()
    {
        var admin = await AdminAsync(Provider());
        var name = $"conformance-{Guid.NewGuid():N}";

        await admin.ImportAsync(name, ReadOnlyMemory<byte>.Empty, Ct);

        Assert.IsTrue(await admin.ExistsAsync(name, Ct));
        Assert.IsEmpty(await admin.ExportAsync(name, Ct));
    }

    /// <summary>
    /// Import is specified to replace, and must leave no trace of the previous image - a backend that
    /// overwrote in place would leave the tail of a larger database behind.
    /// </summary>
    [TestMethod]
    public async Task Import_ReplacesAnExistingDatabaseEntirely()
    {
        var admin = await AdminAsync(Provider());
        var name = $"conformance-{Guid.NewGuid():N}";
        await admin.ImportAsync(name, Image(64 * 1024, seed: 5), Ct);

        var replacement = Image(4096, seed: 6);
        await admin.ImportAsync(name, replacement, Ct);

        CollectionAssert.AreEqual(replacement, await admin.ExportAsync(name, Ct));
    }

    [TestMethod]
    public async Task Delete_RemovesTheDatabaseFromExistenceAndTheList()
    {
        var admin = await AdminAsync(Provider());
        var name = $"conformance-{Guid.NewGuid():N}";
        await admin.ImportAsync(name, Image(4096, seed: 7), Ct);

        await admin.DeleteAsync(name, Ct);

        Assert.IsFalse(await admin.ExistsAsync(name, Ct));
        Assert.DoesNotContain(name, await admin.ListAsync(Ct));
    }

    /// <summary>
    /// Migration and teardown both delete defensively, so deleting what is not there is a success.
    /// </summary>
    [TestMethod]
    public async Task Delete_SucceedsQuietly_WhenTheDatabaseIsAbsent()
    {
        var admin = await AdminAsync(Provider());

        await admin.DeleteAsync($"absent-{Guid.NewGuid():N}", Ct);
    }

    [TestMethod]
    public async Task Export_ThrowsForADatabaseThatDoesNotExist()
    {
        var admin = await AdminAsync(Provider());

        await Assert.ThrowsAsync<Exception>(
            async () => await admin.ExportAsync($"absent-{Guid.NewGuid():N}", Ct));
    }

    [TestMethod]
    public async Task Databases_DoNotLeakIntoEachOther()
    {
        var admin = await AdminAsync(Provider());
        var first = $"conformance-{Guid.NewGuid():N}";
        var second = $"conformance-{Guid.NewGuid():N}";
        var firstImage = Image(4096, seed: 8);
        var secondImage = Image(8192, seed: 9);

        await admin.ImportAsync(first, firstImage, Ct);
        await admin.ImportAsync(second, secondImage, Ct);
        await admin.DeleteAsync(second, Ct);

        CollectionAssert.AreEqual(firstImage, await admin.ExportAsync(first, Ct));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    public async Task Admin_RejectsABlankDatabaseName(string name)
    {
        var admin = await AdminAsync(Provider());

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await admin.ExistsAsync(name, Ct));
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await admin.DeleteAsync(name, Ct));
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await admin.ExportAsync(name, Ct));
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await admin.ImportAsync(name, Image(4096, seed: 10), Ct));
    }

    [TestMethod]
    public async Task Admin_RejectsANullDatabaseName()
    {
        var admin = await AdminAsync(Provider());

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await admin.ExistsAsync(null!, Ct));
    }
}
