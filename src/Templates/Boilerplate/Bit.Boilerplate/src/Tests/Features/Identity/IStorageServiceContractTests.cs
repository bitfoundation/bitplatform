//+:cnd:noEmit
namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// The contract every <see cref="IStorageService"/> has to honour, written once so the four implementations have
/// something to be in sync <b>with</b> rather than only a <c>[mirror]</c> comment saying they should be.
/// <para>
/// It exists because <c>AuthManager.StoreTokens</c> re-derives the user's "Remember me" choice from
/// <see cref="IStorageService.IsPersistent"/> on every refresh, so the two questions "which store is this key in?"
/// and "which value does a read return?" decide whether a session the user asked not to remember survives - and all
/// three shipped implementations answered at least one of them wrongly.
/// </para>
/// <para>
/// ⚠ <b>Only the test double is exercised here, and that is a real gap, not an oversight.</b>
/// <c>MauiStorageService</c> and <c>WindowsStorageService</c> live in projects this one does not reference and cannot:
/// Client.Maui needs the MAUI workloads and Client.Windows is <c>net10.0-windows</c>, so referencing either would
/// retarget the test project and break the Linux CI job. <c>WebStorageService</c> is reachable as a type but its
/// stores are <c>localStorage</c> and <c>sessionStorage</c> behind JS interop, so it needs a browser. What this class
/// buys is a single, executable statement of the contract that a reader can diff the three by hand against; what it
/// does not buy is a guard that fails when one of them drifts.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest"), TestCategory("Identity")]
public partial class IStorageServiceContractTests
{
    private const string Key = "refresh_token";

    private static IEnumerable<object[]> Implementations()
    {
        yield return [new TestStorageService()];
    }

    /// <summary>
    /// A key lives in exactly one of the two stores. A write that leaves the superseded copy behind is not merely
    /// untidy: every implementation's <c>GetItem</c> prefers one store over the other, so the stale copy WINS the
    /// next read. On the web client that meant a sign-in with "Remember me" unchecked wrote its refresh token where
    /// nothing would read it, while the previous session's token in localStorage kept being used - so the user
    /// carried on as the account they had just replaced.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(Implementations))]
    public async Task AWriteToOneStore_Should_EvictTheKeyFromTheOther(IStorageService storageService)
    {
        await storageService.SetItem(Key, "remembered", persistent: true);
        Assert.IsTrue(await storageService.IsPersistent(Key));

        await storageService.SetItem(Key, "not-remembered", persistent: false);

        Assert.AreEqual("not-remembered", await storageService.GetItem(Key),
            "The persistent copy shadowed the value that was just written, so the app would keep using the superseded token.");
        Assert.IsFalse(await storageService.IsPersistent(Key),
            "A key written to temporary storage must not still report as persistent - AuthManager.StoreTokens derives " +
            "'Remember me' from that answer on every refresh, so a wrong one re-promotes the session.");

        await storageService.SetItem(Key, "remembered-again", persistent: true);

        Assert.AreEqual("remembered-again", await storageService.GetItem(Key));
        Assert.IsTrue(await storageService.IsPersistent(Key));
    }

    /// <summary>
    /// <c>IsPersistent</c> answers "is this key in the persistent store", not "does this key have a value anywhere".
    /// The Windows implementation delegated to <c>GetItem</c>, which reads temporary storage first, so a
    /// remember-me-off refresh token reported as persistent and was promoted to a 14 day on-disk credential.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(Implementations))]
    public async Task IsPersistent_Should_AnswerFromThePersistentStoreAlone(IStorageService storageService)
    {
        Assert.IsFalse(await storageService.IsPersistent(Key), "An absent key is not persistent.");

        await storageService.SetItem(Key, "temporary", persistent: false);

        Assert.AreEqual("temporary", await storageService.GetItem(Key), "The value has to be readable to begin with.");
        Assert.IsFalse(await storageService.IsPersistent(Key));
    }

    [TestMethod]
    [DynamicData(nameof(Implementations))]
    public async Task RemoveItem_Should_ClearBothStores(IStorageService storageService)
    {
        await storageService.SetItem(Key, "remembered", persistent: true);
        await storageService.RemoveItem(Key);
        Assert.IsNull(await storageService.GetItem(Key));

        await storageService.SetItem(Key, "temporary", persistent: false);
        await storageService.RemoveItem(Key);
        Assert.IsNull(await storageService.GetItem(Key));

        Assert.IsFalse(await storageService.IsPersistent(Key));
    }

    /// <summary>
    /// <c>AuthManager.ClearTokens</c> removes a key that may be in either store, and <c>StoreTokens</c> can be handed
    /// a null refresh token, so neither a missing key nor a null value may throw.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(Implementations))]
    public async Task AMissingKeyAndANullValue_Should_BeTolerated(IStorageService storageService)
    {
        Assert.IsNull(await storageService.GetItem("never-written"));
        await storageService.RemoveItem("never-written");

        await storageService.SetItem(Key, null, persistent: true);
        Assert.IsNull(await storageService.GetItem(Key));

        await storageService.Clear();
        Assert.IsNull(await storageService.GetItem(Key));
        Assert.IsFalse(await storageService.IsPersistent(Key));
    }
}
