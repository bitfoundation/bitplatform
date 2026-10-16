using System;
using System.Threading.Tasks;
using Bit.Blex;
using Bit.Blex.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// <see cref="BlexSelector{T}"/> is <c>[BlexComputed]</c> lifted to the manager: a cross-store
/// derived value that recomputes at most once per action, however many readers ask for it.
/// </summary>
[TestClass]
public class SelectorMemoTests
{
    private static (BlexManager Manager, CounterStore Counter, ProfileStore Profile) Setup()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        var profile = new ProfileStore();
        manager.Register(counter);
        manager.Register(profile);
        return (manager, counter, profile);
    }

    [TestMethod]
    public void ManyReads_ComputeOnce()
    {
        var (manager, counter, _) = Setup();
        var runs = 0;

        using var selector = manager.CreateSelector(() =>
        {
            runs++;
            return counter.Count * 2;
        });

        Assert.AreEqual(0, runs);          // lazy: nothing computed until read
        Assert.AreEqual(0, selector.Value);
        Assert.AreEqual(0, selector.Value);
        Assert.AreEqual(0, selector.Value);
        Assert.AreEqual(1, runs);
    }

    [TestMethod]
    public void AnActionInvalidatesTheMemo()
    {
        var (manager, counter, _) = Setup();
        var runs = 0;
        using var selector = manager.CreateSelector(() => { runs++; return counter.Count; });

        Assert.AreEqual(0, selector.Value);
        counter.Increment();
        Assert.AreEqual(1, selector.Value);
        Assert.AreEqual(1, selector.Value);
        Assert.AreEqual(2, runs);
    }

    [TestMethod]
    public void AnActionNobodyReads_CostsNothing()
    {
        var (manager, counter, _) = Setup();
        var runs = 0;
        using var selector = manager.CreateSelector(() => { runs++; return counter.Count; });

        _ = selector.Value;
        counter.Increment();
        counter.Increment();
        counter.Increment();

        Assert.AreEqual(1, runs);   // still only the first read
        Assert.AreEqual(3, selector.Value);
        Assert.AreEqual(2, runs);
    }

    [TestMethod]
    public void ItSpansStores()
    {
        var (manager, counter, profile) = Setup();
        using var selector = manager.CreateSelector(() => $"{profile.UserName}:{counter.Count}");

        Assert.AreEqual("anonymous:0", selector.Value);

        profile.SignIn("ada");
        counter.Add(4);

        Assert.AreEqual("ada:4", selector.Value);
    }

    [TestMethod]
    public void ARestoreInvalidatesIt()
    {
        var (manager, counter, _) = Setup();
        var snapshot = manager.CaptureGlobalState();
        counter.Add(5);

        using var selector = manager.CreateSelector(() => counter.Count);
        Assert.AreEqual(5, selector.Value);

        manager.RestoreGlobalState(snapshot);

        Assert.IsFalse(selector.IsValid);
        Assert.AreEqual(0, selector.Value);
    }

    [TestMethod]
    public void SelectorsCompose()
    {
        var (manager, counter, _) = Setup();
        var innerRuns = 0;

        using var doubled = manager.CreateSelector(() => { innerRuns++; return counter.Count * 2; });
        using var label = manager.CreateSelector(() => $"={doubled.Value}");

        counter.Add(3);
        Assert.AreEqual("=6", label.Value);
        Assert.AreEqual(6, doubled.Value);
        Assert.AreEqual(1, innerRuns);   // the composed read reused the inner memo
    }

    [TestMethod]
    public void ExplicitInvalidation_ForcesARecompute()
    {
        var (manager, _, _) = Setup();
        var runs = 0;
        using var selector = manager.CreateSelector(() => ++runs);

        Assert.AreEqual(1, selector.Value);
        selector.Invalidate();
        Assert.IsFalse(selector.IsValid);
        Assert.AreEqual(2, selector.Value);
    }

    [TestMethod]
    public void AThrowingSelector_PropagatesAndIsNotCached()
    {
        var (manager, counter, _) = Setup();
        var fail = true;
        using var selector = manager.CreateSelector(() => fail ? throw new InvalidOperationException("boom") : counter.Count);

        Assert.ThrowsExactly<InvalidOperationException>(() => selector.Value);

        // A failed computation must not be memoized as a success.
        fail = false;
        Assert.AreEqual(0, selector.Value);
    }

    [TestMethod]
    public void DisposingDetachesIt()
    {
        var (manager, counter, _) = Setup();
        var runs = 0;
        var selector = manager.CreateSelector(() => { runs++; return counter.Count; });

        _ = selector.Value;
        selector.Dispose();
        selector.Dispose();   // idempotent

        counter.Increment();

        // Memoization stops with the subscription: nothing is left to invalidate the cache, and a
        // value frozen at the moment of disposal is the one failure mode a memo must not have.
        Assert.AreEqual(1, selector.Value);
        Assert.AreEqual(1, selector.Value);
        Assert.AreEqual(3, runs);
        Assert.IsFalse(selector.IsValid);
    }

    [TestMethod]
    public async Task ManagerWaitForAsync_WaitsOnACrossStoreCondition()
    {
        var (manager, counter, profile) = Setup();

        var pending = manager.WaitForAsync(() => counter.Count == 2 && profile.UserName == "ada");

        counter.Increment();
        profile.SignIn("ada");
        counter.Increment();

        await pending.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task ManagerWaitForAsync_TimesOut()
    {
        var (manager, _, _) = Setup();
        await Assert.ThrowsExactlyAsync<TimeoutException>(() => manager.WaitForAsync(() => false, TimeSpan.FromMilliseconds(50)));
    }

    [TestMethod]
    public async Task ManagerWaitForAsync_ReturnsImmediatelyWhenAlreadyTrue()
    {
        var (manager, _, _) = Setup();
        await manager.WaitForAsync(() => true).WaitAsync(TimeSpan.FromSeconds(5));
    }
}
