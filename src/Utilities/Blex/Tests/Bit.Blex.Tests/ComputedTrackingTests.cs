namespace Bit.Blex.Tests;

/// <summary>Counts how often each computed of <see cref="TrackedStore"/> runs; static, so reading it does not count as reading the store.</summary>
public static class TrackedRuns
{
    public static Dictionary<string, int> Runs { get; } = [];

    public static int Tick(string computed)
    {
        Runs[computed] = Runs.GetValueOrDefault(computed) + 1;
        return 0;
    }
}

/// <summary>Computeds that read different fields, one through another computed, one through a helper.</summary>
[BlexStoreAttribute(Name = "tracked")]
public partial class TrackedStore
{
    [BlexStateAttribute] private int _a;
    [BlexStateAttribute] private int _b;
    [BlexStateAttribute] private string _label = "";

    [BlexComputedAttribute] private int ComputeSum() => A + _b + TrackedRuns.Tick("Sum");

    [BlexComputedAttribute] private int ComputeDoubleA() => A * 2 + TrackedRuns.Tick("DoubleA");

    [BlexComputedAttribute] private int ComputeChain() => DoubleA + 1;

    [BlexComputedAttribute] private int ComputeHelped() => Helper();

    private int Helper() => _b * 10 + TrackedRuns.Tick("Helped");

    [BlexActionAttribute] private void OnSetA(int value) => A = value;

    [BlexActionAttribute] private void OnSetB(int value) => B = value;

    [BlexActionAttribute] private void OnSetLabel(string value) => Label = value;

    [BlexActionAttribute]
    private void OnSetBoth(int a, int b)
    {
        A = a;
        B = b;
    }
}

/// <summary>
/// A write invalidates only the computeds that read what it wrote -- found in each computed's body by the generator --
/// so a value derived from other fields is not recomputed for nothing. One read through a helper the generator cannot
/// see into is invalidated by every write, as all were before.
/// </summary>
[TestClass]
[DoNotParallelize]
public class ComputedTrackingTests
{
    private static TrackedStore Store()
    {
        var store = new TrackedStore();
        new BlexManager().Register(store);
        _ = (store.Sum, store.DoubleA, store.Chain, store.Helped);   // every computed memoized once
        TrackedRuns.Runs.Clear();
        return store;
    }

    private static void ReadAll(TrackedStore store) => _ = (store.Sum, store.DoubleA, store.Chain, store.Helped);

    [TestMethod]
    public void Writing_one_field_leaves_what_derives_from_others_memoized()
    {
        var store = Store();

        store.SetB(2);
        ReadAll(store);

        Assert.AreEqual(1, TrackedRuns.Runs.GetValueOrDefault("Sum"));
        Assert.AreEqual(0, TrackedRuns.Runs.GetValueOrDefault("DoubleA"), "reads only A");
        Assert.AreEqual(1, TrackedRuns.Runs.GetValueOrDefault("Helped"), "reads through a helper: every write");
        Assert.AreEqual((2, 0, 1, 20), (store.Sum, store.DoubleA, store.Chain, store.Helped));
    }

    [TestMethod]
    public void A_computed_read_through_another_follows_its_dependencies()
    {
        var store = Store();

        store.SetA(3);
        ReadAll(store);

        Assert.AreEqual(1, TrackedRuns.Runs.GetValueOrDefault("DoubleA"));
        Assert.AreEqual((3, 6, 7), (store.Sum, store.DoubleA, store.Chain));
    }

    [TestMethod]
    public void A_field_no_computed_reads_invalidates_only_the_untraceable_ones()
    {
        var store = Store();

        store.SetLabel("x");
        ReadAll(store);

        Assert.AreEqual(0, TrackedRuns.Runs.GetValueOrDefault("Sum"));
        Assert.AreEqual(0, TrackedRuns.Runs.GetValueOrDefault("DoubleA"));
        Assert.AreEqual(1, TrackedRuns.Runs.GetValueOrDefault("Helped"));
    }

    [TestMethod]
    public void Values_stay_right_through_actions_restores_and_resets()
    {
        var store = Store();
        var before = store.SerializeState();

        store.SetBoth(4, 5);
        Assert.AreEqual((9, 8, 9, 50), (store.Sum, store.DoubleA, store.Chain, store.Helped));

        store.RestoreState(before);
        Assert.AreEqual((0, 0, 1, 0), (store.Sum, store.DoubleA, store.Chain, store.Helped));

        store.SetA(1);
        store.ResetState();
        Assert.AreEqual((0, 0, 1, 0), (store.Sum, store.DoubleA, store.Chain, store.Helped));
    }

    [TestMethod]
    public void A_computed_read_later_in_the_same_action_sees_the_write()
    {
        var store = Store();
        int? seen = null;
        store.Batch("check", () =>
        {
            store.A = 5;
            seen = store.Chain;
        });

        Assert.AreEqual(11, seen);
    }
}
