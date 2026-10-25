namespace Bit.Blex.Tests;

/// <summary>
/// <see cref="BlexManager.CreateSelectorFamily{TArg, T}"/>: one memoized value per argument, recomputed only after a
/// state change, and bounded so a list that has shown many ids does not keep a selector alive for each.
/// </summary>
[TestClass]
public class SelectorFamilyTests
{
    private static (BlexManager Manager, CounterStore Counter) App()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        return (manager, counter);
    }

    [TestMethod]
    public void Each_argument_is_computed_once_per_state_change()
    {
        var (manager, counter) = App();
        var runs = new Dictionary<int, int>();
        using var times = manager.CreateSelectorFamily((int factor) =>
        {
            runs[factor] = runs.GetValueOrDefault(factor) + 1;
            return counter.Count * factor;
        });

        Assert.AreEqual(0, times[2]);
        Assert.AreEqual(0, times[2]);
        Assert.AreEqual(0, times[3]);
        Assert.AreEqual((1, 1), (runs[2], runs[3]), "one computation per argument");
        Assert.IsTrue(times.IsValid(2));

        counter.Increment();
        Assert.IsFalse(times.IsValid(2));
        Assert.AreEqual(2, times[2]);
        Assert.AreEqual(2, times[2]);
        Assert.AreEqual(2, runs[2]);
        Assert.AreEqual(1, runs[3], "an argument nobody read after the change costs nothing");
    }

    [TestMethod]
    public void The_least_recently_read_argument_is_evicted_beyond_max_size()
    {
        var (manager, _) = App();
        var runs = 0;
        using var family = manager.CreateSelectorFamily((string id) => { runs++; return id.Length; }, maxSize: 2);

        _ = family["a"];
        _ = family["bb"];
        _ = family["a"];      // "a" is the most recent now
        _ = family["ccc"];    // over the limit: "bb" goes

        Assert.AreEqual(2, family.Count);
        Assert.IsTrue(family.IsValid("a"));
        Assert.IsFalse(family.IsValid("bb"));
        Assert.AreEqual(3, runs);

        _ = family["bb"];
        Assert.AreEqual(4, runs, "an evicted argument just computes again");
    }

    [TestMethod]
    public void A_failed_computation_is_not_cached()
    {
        var (manager, counter) = App();
        using var family = manager.CreateSelectorFamily((int divisor) => 10 / (counter.Count + divisor));

        Assert.ThrowsExactly<DivideByZeroException>(() => family[0]);
        Assert.AreEqual(0, family.Count);

        counter.Increment();
        Assert.AreEqual(10, family[0]);
    }

    [TestMethod]
    public void Remove_clear_invalidate_and_dispose()
    {
        var (manager, _) = App();
        var runs = 0;
        var family = manager.CreateSelectorFamily((int id) => ++runs);

        _ = family[1];
        _ = family[2];
        Assert.IsTrue(family.Remove(1));
        Assert.IsFalse(family.Remove(1));
        Assert.AreEqual(1, family.Count);

        family.Invalidate();
        Assert.AreEqual(1, family.Count, "invalidating keeps the arguments");
        Assert.IsFalse(family.IsValid(2));

        family.Clear();
        Assert.AreEqual(0, family.Count);

        family.Dispose();
        _ = family[3];
        _ = family[3];
        Assert.AreEqual(0, family.Count, "a disposed family memoizes nothing");
        Assert.AreEqual(4, runs);
    }

    [TestMethod]
    public void Arguments_are_compared_with_the_given_comparer()
    {
        var (manager, _) = App();
        var runs = 0;
        using var family = manager.CreateSelectorFamily((string name) => { runs++; return name.ToUpperInvariant(); }, comparer: StringComparer.OrdinalIgnoreCase);

        _ = family["ada"];
        _ = family["ADA"];

        Assert.AreEqual(1, runs);
    }

    [TestMethod]
    public void A_projection_that_reads_its_own_family_does_not_corrupt_it()
    {
        var (manager, counter) = App();
        BlexSelectorFamily<int, int>? fib = null;
        fib = manager.CreateSelectorFamily((int n) => n < 2 ? n + counter.Count : fib![n - 1] + fib[n - 2], maxSize: 3);

        Assert.AreEqual(55, fib[10]);
        Assert.IsLessThanOrEqualTo(3, fib.Count);

        counter.Increment();
        Assert.AreEqual(144, fib[10]);   // fib with f(0) = 1, f(1) = 2
        fib.Dispose();
    }
}
