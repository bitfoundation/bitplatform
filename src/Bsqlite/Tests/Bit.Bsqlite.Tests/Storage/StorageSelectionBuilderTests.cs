
namespace Bit.Bsqlite.Storage.Tests;

/// <summary>
/// Configuration mistakes here are silent in production - the wrong backend or the wrong order - so
/// the builder rejects them at the point they are written instead.
/// </summary>
[TestClass]
public sealed class StorageSelectionBuilderTests
{
    [TestMethod]
    public void CandidateOrder_IsThePreferenceOrder()
    {
        var selection = BsqliteStorageSelectionBuilder.Create(
            s => s.Prefer("opfs").Fallback("indexeddb").Fallback("cache-storage"));

        AssertSequence.Equal(["opfs", "indexeddb", "cache-storage"], selection.Candidates);
        Assert.IsFalse(selection.IsStrict);
    }

    [TestMethod]
    public void SingleCandidate_IsStrict()
    {
        var selection = BsqliteStorageSelectionBuilder.Create(s => s.Prefer("opfs"));

        Assert.IsTrue(selection.IsStrict);
    }

    [TestMethod]
    public void Defaults_AreTheConservativeChoices()
    {
        var selection = BsqliteStorageSelectionBuilder.Create(s => s.Prefer("opfs"));

        Assert.IsFalse(selection.AllowNonPersistentFallback);
        Assert.AreEqual(BsqliteStorageMigrationMode.KeepExisting, selection.MigrationMode);
    }

    [TestMethod]
    public void PreferringTwice_PointsAtFallback()
    {
        var failure = Assert.ThrowsExactly<InvalidOperationException>(
            () => BsqliteStorageSelectionBuilder.Create(s => s.Prefer("opfs").Prefer("indexeddb")));

        Assert.Contains("Fallback(\"indexeddb\")", failure.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void FallbackWithoutPrefer_PointsAtPrefer()
    {
        var failure = Assert.ThrowsExactly<InvalidOperationException>(
            () => BsqliteStorageSelectionBuilder.Create(s => s.Fallback("opfs")));

        Assert.Contains("Prefer(\"opfs\")", failure.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RepeatingACandidate_IsRejected()
    {
        var failure = Assert.ThrowsExactly<InvalidOperationException>(
            () => BsqliteStorageSelectionBuilder.Create(
                s => s.Prefer("opfs").Fallback("OPFS")));

        Assert.Contains("already a candidate", failure.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void SelectingNothing_IsRejected()
    {
        var failure = Assert.ThrowsExactly<InvalidOperationException>(
            () => BsqliteStorageSelectionBuilder.Create(_ => { }));

        Assert.Contains("No storage provider was selected", failure.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void MigrationMode_IsCarriedThrough()
    {
        var selection = BsqliteStorageSelectionBuilder.Create(
            s => s.Prefer("opfs").WithMigrationMode(BsqliteStorageMigrationMode.AutomaticOnOpen));

        Assert.AreEqual(BsqliteStorageMigrationMode.AutomaticOnOpen, selection.MigrationMode);
    }
}
