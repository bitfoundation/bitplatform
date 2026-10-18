using Bit.Bsqlite.Interop;

namespace Bit.Bsqlite.Storage.Tests;

[TestClass]
public sealed class StorageProviderResolverTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    private const string Database = "app.db";

    private static BsqliteStorageSelection Select(
        Action<BsqliteStorageSelectionBuilder> configure)
        => BsqliteStorageSelectionBuilder.Create(configure);

    private static BsqliteStorageProviderResolver Resolver(
        IBsqliteStorageBindingStore bindingStore,
        params IBsqliteStorageProvider[] providers)
        => new(providers, bindingStore);

    [TestMethod]
    public async Task SingleAvailableProvider_IsSelected()
    {
        var opfs = new FakeStorageProvider("opfs");
        var resolver = Resolver(new BsqliteInMemoryStorageBindingStore(), opfs);

        var resolution = await resolver.ResolveAsync(Database, Select(s => s.Prefer("opfs")), Ct);

        Assert.AreSame(opfs, resolution.Provider);
        Assert.IsTrue(resolution.IsFirstChoice);
        Assert.IsFalse(resolution.WasDecidedByExistingData);
        Assert.IsNull(resolution.BetterProviderAvailable);
    }

    /// <summary>
    /// A lone candidate is a strict instruction. Substituting something else would mean the
    /// application silently got storage it never asked for.
    /// </summary>
    [TestMethod]
    public async Task SingleUnavailableProvider_Throws_WithoutSubstituting()
    {
        var resolver = Resolver(
            new BsqliteInMemoryStorageBindingStore(),
            new FakeStorageProvider("opfs", isAvailable: false),
            new FakeStorageProvider("indexeddb"));

        var failure = await Assert.ThrowsExactlyAsync<BsqliteStorageUnavailableException>(
            () => resolver.ResolveAsync(Database, Select(s => s.Prefer("opfs")), Ct).AsTask());

        Assert.AreEqual(Database, failure.DatabaseName);
        var attempt = Assert.ContainsSingle(failure.Attempts);
        Assert.AreEqual(BsqliteStorageCandidateStatus.Unavailable, attempt.Status);
        Assert.Contains("switched off", attempt.Probe!.UnavailableReason!, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task FallbackIsUsed_WhenPreferredIsUnavailable()
    {
        var indexedDb = new FakeStorageProvider("indexeddb");
        var resolver = Resolver(
            new BsqliteInMemoryStorageBindingStore(),
            new FakeStorageProvider("opfs", isAvailable: false),
            indexedDb);

        var resolution = await resolver.ResolveAsync(
            Database,
            Select(s => s.Prefer("opfs").Fallback("indexeddb")),
            Ct);

        Assert.AreSame(indexedDb, resolution.Provider);
        Assert.IsFalse(resolution.IsFirstChoice);
        CollectionAssert.AreEqual(
            new[] { BsqliteStorageCandidateStatus.Unavailable, BsqliteStorageCandidateStatus.Selected },
            resolution.Attempts.Select(a => a.Status).ToArray());
    }

    [TestMethod]
    public async Task UnregisteredCandidate_IsReported_AndSkipped()
    {
        var indexedDb = new FakeStorageProvider("indexeddb");
        var resolver = Resolver(new BsqliteInMemoryStorageBindingStore(), indexedDb);

        var resolution = await resolver.ResolveAsync(
            Database,
            Select(s => s.Prefer("opfs").Fallback("indexeddb")),
            Ct);

        Assert.AreSame(indexedDb, resolution.Provider);
        Assert.AreEqual(BsqliteStorageCandidateStatus.NotRegistered, resolution.Attempts[0].Status);
    }

    [TestMethod]
    public async Task AllCandidatesFail_ReportsEveryReason()
    {
        var resolver = Resolver(
            new BsqliteInMemoryStorageBindingStore(),
            new FakeStorageProvider("opfs", isAvailable: false),
            new FakeStorageProvider("indexeddb", isAvailable: false));

        var failure = await Assert.ThrowsExactlyAsync<BsqliteStorageUnavailableException>(
            () => resolver
                .ResolveAsync(Database, Select(s => s.Prefer("opfs").Fallback("indexeddb")), Ct)
                .AsTask());

        Assert.AreEqual(2, failure.Attempts.Count);
        foreach (var attempt in failure.Attempts)
        {
            Assert.AreEqual(BsqliteStorageCandidateStatus.Unavailable, attempt.Status);
        }
        Assert.Contains("opfs", failure.Message, StringComparison.Ordinal);
        Assert.Contains("indexeddb", failure.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task ProviderThatThrowsWhileProbing_IsTreatedAsUnavailable()
    {
        var indexedDb = new FakeStorageProvider("indexeddb");
        var resolver = Resolver(
            new BsqliteInMemoryStorageBindingStore(),
            new FakeStorageProvider("opfs", probeThrows: new InvalidOperationException("boom")),
            indexedDb);

        var resolution = await resolver.ResolveAsync(
            Database,
            Select(s => s.Prefer("opfs").Fallback("indexeddb")),
            Ct);

        Assert.AreSame(indexedDb, resolution.Provider);
        Assert.Contains("boom", resolution.Attempts[0].Probe!.UnavailableReason!, StringComparison.Ordinal);
    }

    [TestMethod]
    public void DuplicateProviderNames_AreRejected()
    {
        var failure = Assert.ThrowsExactly<InvalidOperationException>(
            () => Resolver(
                new BsqliteInMemoryStorageBindingStore(),
                new FakeStorageProvider("opfs"),
                new FakeStorageProvider("OPFS")));

        Assert.Contains("must be unique", failure.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task ProbeResultIsCached_AcrossResolves()
    {
        var opfs = new FakeStorageProvider("opfs");
        var resolver = Resolver(new BsqliteInMemoryStorageBindingStore(), opfs);
        var selection = Select(s => s.Prefer("opfs"));

        await resolver.ResolveAsync("one.db", selection, Ct);
        await resolver.ResolveAsync("two.db", selection, Ct);

        Assert.AreEqual(1, opfs.ProbeCount);
    }

    [TestClass]
    public sealed class NonPersistentFallback
    {
        public TestContext TestContext { get; set; } = null!;

        private CancellationToken Ct => TestContext.CancellationToken;

        [TestMethod]
        public async Task IsRejected_WithoutOptIn()
        {
            var resolver = Resolver(
                new BsqliteInMemoryStorageBindingStore(),
                new FakeStorageProvider("opfs", isAvailable: false),
                new FakeStorageProvider("in-memory", isPersistent: false));

            var failure = await Assert.ThrowsExactlyAsync<BsqliteStorageUnavailableException>(
                () => resolver
                    .ResolveAsync(Database, Select(s => s.Prefer("opfs").Fallback("in-memory")), Ct)
                    .AsTask());

            Assert.AreEqual(
                BsqliteStorageCandidateStatus.RejectedAsNonPersistent,
                failure.Attempts[1].Status);
            Assert.Contains(
                "AllowNonPersistentFallback",
                failure.Attempts[1].Explanation!,
                StringComparison.Ordinal);
        }

        [TestMethod]
        public async Task IsAccepted_WithOptIn()
        {
            var inMemory = new FakeStorageProvider("in-memory", isPersistent: false);
            var resolver = Resolver(
                new BsqliteInMemoryStorageBindingStore(),
                new FakeStorageProvider("opfs", isAvailable: false),
                inMemory);

            var resolution = await resolver.ResolveAsync(
                Database,
                Select(s => s.Prefer("opfs").Fallback("in-memory").AllowNonPersistentFallback()),
                Ct);

            Assert.AreSame(inMemory, resolution.Provider);
        }

        /// <summary>
        /// Choosing volatile storage outright is a decision the application already made, so it needs
        /// no second opt-in - the guard exists for silent downgrades, not deliberate ones.
        /// </summary>
        [TestMethod]
        public async Task IsAllowed_AsAnExplicitFirstChoice()
        {
            var inMemory = new FakeStorageProvider("in-memory", isPersistent: false);
            var resolver = Resolver(new BsqliteInMemoryStorageBindingStore(), inMemory);

            var resolution = await resolver.ResolveAsync(
                Database,
                Select(s => s.Prefer("in-memory")),
                Ct);

            Assert.AreSame(inMemory, resolution.Provider);
        }
    }

    [TestClass]
    public sealed class StickyBinding
    {
        public TestContext TestContext { get; set; } = null!;

        private CancellationToken Ct => TestContext.CancellationToken;

        [TestMethod]
        public async Task ExistingData_OutranksPreference()
        {
            var store = new BsqliteInMemoryStorageBindingStore();
            await store.SetProviderNameAsync(Database, "indexeddb", Ct);

            var opfs = new FakeStorageProvider("opfs");
            var indexedDb = new FakeStorageProvider("indexeddb");
            var resolver = Resolver(store, opfs, indexedDb);

            var resolution = await resolver.ResolveAsync(
                Database,
                Select(s => s.Prefer("opfs").Fallback("indexeddb")),
                Ct);

            Assert.AreSame(indexedDb, resolution.Provider);
            Assert.IsTrue(resolution.WasDecidedByExistingData);
            Assert.IsFalse(resolution.IsFirstChoice);
            Assert.AreSame(opfs, resolution.BetterProviderAvailable);
        }

        /// <summary>
        /// The failure this whole mechanism exists to prevent: OPFS becomes available, preference says
        /// OPFS, and the user's IndexedDB database would be replaced by an empty one with no error.
        /// </summary>
        [TestMethod]
        public async Task UnreachableExistingData_Throws_RatherThanOpeningAnEmptyDatabase()
        {
            var store = new BsqliteInMemoryStorageBindingStore();
            await store.SetProviderNameAsync(Database, "indexeddb", Ct);

            var resolver = Resolver(
                store,
                new FakeStorageProvider("opfs"),
                new FakeStorageProvider("indexeddb", isAvailable: false));

            var failure = await Assert.ThrowsExactlyAsync<BsqliteStorageUnavailableException>(
                () => resolver
                    .ResolveAsync(Database, Select(s => s.Prefer("opfs").Fallback("indexeddb")), Ct)
                    .AsTask());

            var attempt = Assert.ContainsSingle(failure.Attempts);
            Assert.AreEqual("indexeddb", attempt.ProviderName);
            Assert.Contains("will not open an empty database", attempt.Explanation!, StringComparison.Ordinal);
        }

        [TestMethod]
        public async Task ExistingDataOnUnregisteredProvider_ThrowsWithActionableMessage()
        {
            var store = new BsqliteInMemoryStorageBindingStore();
            await store.SetProviderNameAsync(Database, "cache-storage", Ct);

            var resolver = Resolver(store, new FakeStorageProvider("opfs"));

            var failure = await Assert.ThrowsExactlyAsync<BsqliteStorageUnavailableException>(
                () => resolver.ResolveAsync(Database, Select(s => s.Prefer("opfs")), Ct).AsTask());

            var attempt = Assert.ContainsSingle(failure.Attempts);
            Assert.AreEqual(BsqliteStorageCandidateStatus.NotRegistered, attempt.Status);
            Assert.Contains("Register it", attempt.Explanation!, StringComparison.Ordinal);
        }

        [TestMethod]
        public async Task NoBetterProviderIsReported_WhenBoundProviderIsTheFirstChoice()
        {
            var store = new BsqliteInMemoryStorageBindingStore();
            await store.SetProviderNameAsync(Database, "opfs", Ct);

            var resolver = Resolver(
                store,
                new FakeStorageProvider("opfs"),
                new FakeStorageProvider("indexeddb"));

            var resolution = await resolver.ResolveAsync(
                Database,
                Select(s => s.Prefer("opfs").Fallback("indexeddb")),
                Ct);

            Assert.IsTrue(resolution.IsFirstChoice);
            Assert.IsNull(resolution.BetterProviderAvailable);
        }

        /// <summary>
        /// Moving a database onto volatile storage is never an upgrade, however the preference list is
        /// ordered.
        /// </summary>
        [TestMethod]
        public async Task NonPersistentProviderIsNeverReportedAsBetter()
        {
            var store = new BsqliteInMemoryStorageBindingStore();
            await store.SetProviderNameAsync(Database, "indexeddb", Ct);

            var resolver = Resolver(
                store,
                new FakeStorageProvider("in-memory", isPersistent: false),
                new FakeStorageProvider("indexeddb"));

            var resolution = await resolver.ResolveAsync(
                Database,
                Select(s => s.Prefer("in-memory").Fallback("indexeddb")),
                Ct);

            Assert.IsNull(resolution.BetterProviderAvailable);
        }

        [TestMethod]
        public async Task CommitBinding_MakesTheChoiceStick()
        {
            var store = new BsqliteInMemoryStorageBindingStore();
            var indexedDb = new FakeStorageProvider("indexeddb");
            var resolver = Resolver(store, new FakeStorageProvider("opfs", isAvailable: false), indexedDb);
            var selection = Select(s => s.Prefer("opfs").Fallback("indexeddb"));

            var first = await resolver.ResolveAsync(Database, selection, Ct);
            await resolver.CommitBindingAsync(first, Ct);

            Assert.AreEqual("indexeddb", await store.GetProviderNameAsync(Database, Ct));

            var second = await resolver.ResolveAsync(Database, selection, Ct);
            Assert.AreSame(indexedDb, second.Provider);
            Assert.IsTrue(second.WasDecidedByExistingData);
        }

        /// <summary>
        /// Resolving is a read-only decision; a database that never opened must not leave a binding
        /// behind claiming it did.
        /// </summary>
        [TestMethod]
        public async Task Resolve_DoesNotRecordAnything_UntilCommitted()
        {
            var store = new BsqliteInMemoryStorageBindingStore();
            var resolver = Resolver(store, new FakeStorageProvider("opfs"));

            await resolver.ResolveAsync(Database, Select(s => s.Prefer("opfs")), Ct);

            Assert.IsNull(await store.GetProviderNameAsync(Database, Ct));
        }
    }
}
