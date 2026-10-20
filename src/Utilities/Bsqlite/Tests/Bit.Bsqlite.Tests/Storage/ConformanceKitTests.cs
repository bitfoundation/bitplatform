using Bit.Bsqlite.Storage.ConformanceTests;

namespace Bit.Bsqlite.Storage.Tests;

/// <summary>
/// Tests the conformance kit itself. A suite that only ever runs against a correct provider proves
/// nothing, so each rule is aimed at a backend that breaks exactly that rule and is expected to fail.
/// </summary>
[TestClass]
public sealed class ConformanceKitTests
{
    /// <summary>
    /// Runs the kit's assertions against a chosen provider. Private, so the kit's inherited tests are
    /// not collected a second time from here.
    /// </summary>
#pragma warning disable MSTEST0030 // Deliberately not a [TestClass]: see above.
    private sealed class Harness(IBsqliteStorageProvider provider) : BsqliteStorageProviderConformanceTests
#pragma warning restore MSTEST0030
    {
        protected override IBsqliteStorageProvider CreateProvider() => provider;
    }

    private static async Task RejectsAsync(
        IBsqliteStorageProvider provider,
        Func<Harness, Task> rule)
    {
        var harness = new Harness(provider);

        await using (harness)
        {
            await Assert.ThrowsAsync<AssertFailedException>(async () => await rule(harness));
        }
    }

    private static async Task RejectsAsync(
        IBsqliteStorageProvider provider,
        Action<Harness> rule)
        => await RejectsAsync(provider, harness =>
        {
            rule(harness);
            return Task.CompletedTask;
        });

    /// <summary>The correct provider is expected to pass; the rest of this class expects failures.</summary>
    [TestMethod]
    public async Task AcceptsAConformantBackend()
    {
        var harness = new Harness(new LyingStorageProvider());

        await using (harness)
        {
            harness.NonPersistentBackend_DoesNotOfferRelaxedDurability();
            harness.Capabilities_SupportTheDedicatedWorker();
            await harness.Export_ReturnsACopy_NotAViewOfLiveStorage();
            await harness.Import_ReplacesAnExistingDatabaseEntirely();
            await harness.Admin_RejectsABlankDatabaseName(" ");
        }
    }

    [TestMethod]
    public async Task RejectsDurabilityPromisedWithoutPersistence()
        => await RejectsAsync(
            new LyingStorageProvider { IsPersistent = false, SupportsRelaxedDurability = true },
            harness => harness.NonPersistentBackend_DoesNotOfferRelaxedDurability());

    [TestMethod]
    public async Task RejectsAnAsyncBackendWithNoVfsModule()
        => await RejectsAsync(
            new LyingStorageProvider { RequiredBuild = BsqliteEngineBuild.AsyncCapable },
            harness => harness.BackendNeedingTheAsyncBuild_ShipsAVfsModule());

    [TestMethod]
    public async Task RejectsABackendThatRunsNowhere()
        => await RejectsAsync(
            new LyingStorageProvider { SupportedContexts = BsqliteExecutionContexts.None },
            harness => harness.Capabilities_DeclareAtLeastOneExecutionContext());

    [TestMethod]
    public async Task RejectsABackendThatCannotRunInTheWorker()
        => await RejectsAsync(
            new LyingStorageProvider { SupportedContexts = BsqliteExecutionContexts.Window },
            harness => harness.Capabilities_SupportTheDedicatedWorker());

    [TestMethod]
    public async Task RejectsABlankName()
        => await RejectsAsync(
            new LyingStorageProvider { Name = "  " },
            harness => harness.Name_IsNotBlank());

    [TestMethod]
    public async Task RejectsAnExportThatAliasesLiveStorage()
        => await RejectsAsync(
            new LyingStorageProvider { ExportsLiveArray = true },
            async harness => await harness.Export_ReturnsACopy_NotAViewOfLiveStorage());

    [TestMethod]
    public async Task RejectsAnImportThatLeavesTheOldTailBehind()
        => await RejectsAsync(
            new LyingStorageProvider { OverwritesInPlace = true },
            async harness => await harness.Import_ReplacesAnExistingDatabaseEntirely());

    [TestMethod]
    public async Task RejectsAnAdminThatAcceptsABlankName()
        => await RejectsAsync(
            new LyingStorageProvider { SkipsNameValidation = true },
            async harness => await harness.Admin_RejectsABlankDatabaseName(""));
}
