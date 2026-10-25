using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bit.Blex.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// <c>DebounceMilliseconds</c> gates an effect behind a quiet period. The property that makes it
/// worth generating rather than hand-rolling is that a superseded invocation leaves <em>no</em>
/// trace: it does not flip the loading flag, does not clear the error, is never offered to
/// middleware and records no action. Anything less produces a spinner that flickers once per
/// keystroke and a DevTools timeline full of searches that never happened.
/// </summary>
[TestClass]
public class EffectDebounceTests
{
    /// <summary>
    /// Mirrors <c>DebounceMilliseconds</c> on <c>EffectConcurrencyStore</c>'s debounced effects.
    /// Several tests assert on state <em>inside</em> the quiet period, so the window has to stay
    /// wider than a scheduling hiccup on a loaded two-core CI runner - a value tuned to a developer
    /// machine turns those assertions into a race that only fails in CI.
    /// </summary>
    private const int Quiet = 250;

    /// <summary>Long enough for the gate to elapse, short enough to keep the suite fast.</summary>
    private static Task SettleAsync() => Task.Delay(Quiet * 4);

    [TestMethod]
    public async Task OnlyTheLastInvocationOfABurstRuns()
    {
        using var harness = BlexTestHarness.For<EffectConcurrencyStore>();
        var store = harness.Store;

        await Task.WhenAll(store.TypeAhead("a"), store.TypeAhead("ab"), store.TypeAhead("abc"));

        Assert.AreEqual(1, store.DebouncedBodyRuns);
        Assert.AreEqual("abc", store.Last);
        Assert.AreEqual(1, store.Completed);
    }

    [TestMethod]
    public async Task SupersededInvocationsRecordNoAction()
    {
        using var harness = BlexTestHarness.For<EffectConcurrencyStore>();
        var store = harness.Store;

        await Task.WhenAll(store.TypeAhead("a"), store.TypeAhead("ab"));

        // One TypeAhead, plus the "Set Last"/"Set Completed" writes its body performs.
        Assert.ContainsSingle(n => n == "TypeAhead", harness.Log.Names);
    }

    [TestMethod]
    public async Task SupersededInvocationsAreNeverOfferedToMiddleware()
    {
        var seen = new List<string>();
        using var harness = BlexTestHarness.WithFilter<EffectConcurrencyStore>(ctx =>
        {
            seen.Add(ctx.ActionName);
            return true;
        });

        await Task.WhenAll(harness.Store.TypeAhead("a"), harness.Store.TypeAhead("ab"));

        Assert.ContainsSingle(n => n == "TypeAhead", seen);
    }

    [TestMethod]
    public async Task LoadingStaysFalseWhileTheGateIsWaiting()
    {
        using var harness = BlexTestHarness.For<EffectConcurrencyStore>();
        var store = harness.Store;
        var gate = new TaskCompletionSource();
        store.Gates.Enqueue(gate);

        var run = store.TypeAhead("a");

        // Still inside the quiet period: nothing observable has happened yet, so a spinner bound to
        // IsLoading does not blink once per keystroke.
        Assert.IsFalse(store.TypeAheadIsLoading);

        await SettleAsync();
        Assert.IsTrue(store.TypeAheadIsLoading); // now the body is parked on its gate

        gate.SetResult();
        await run;
        Assert.IsFalse(store.TypeAheadIsLoading);
    }

    [TestMethod]
    public async Task ASupersededInvocationDoesNotClearAPreviousError()
    {
        using var harness = BlexTestHarness.For<EffectConcurrencyStore>();
        var store = harness.Store;

        await store.DebouncedBoom();
        Assert.IsNotNull(store.DebouncedBoomError);

        // Start a burst and supersede it immediately; the error must survive, because the invocation
        // that would have cleared it never ran.
        var first = store.DebouncedBoom();
        var second = store.DebouncedBoom();
        Assert.IsNotNull(store.DebouncedBoomError);

        await Task.WhenAll(first, second);
        Assert.AreEqual(2, store.DebouncedBodyRuns);
    }

    [TestMethod]
    public async Task InvocationsSpacedApartEachRun()
    {
        using var harness = BlexTestHarness.For<EffectConcurrencyStore>();
        var store = harness.Store;

        await store.TypeAhead("first");
        await store.TypeAhead("second");

        Assert.AreEqual(2, store.DebouncedBodyRuns);
        Assert.AreEqual("second", store.Last);
        Assert.AreEqual(2, store.Completed);
    }

    [TestMethod]
    public async Task AVetoIsStillHonoured_AfterTheGateElapses()
    {
        using var harness = BlexTestHarness.WithFilter<EffectConcurrencyStore>(
            ctx => ctx.ActionName != "TypeAhead");

        await harness.Store.TypeAhead("a");

        Assert.AreEqual(0, harness.Store.DebouncedBodyRuns);
        Assert.AreEqual("", harness.Store.Last);
    }

    [TestMethod]
    public async Task DebounceComposesWithLatest_CancellingARequestAlreadyInFlight()
    {
        using var harness = BlexTestHarness.For<EffectConcurrencyStore>();
        var store = harness.Store;
        var first = new TaskCompletionSource();
        var second = new TaskCompletionSource();
        store.Gates.Enqueue(first);
        store.Gates.Enqueue(second);

        var older = store.LiveSearch("a");
        await SettleAsync();          // the gate elapsed, so this one is genuinely in flight
        Assert.IsTrue(store.LiveSearchIsLoading);

        var newer = store.LiveSearch("ab");
        await SettleAsync();          // ...and this one gets through, superseding it

        // The older request was cancelled rather than allowed to finish and write a stale result:
        // its gate is still unset, and awaiting it completes without ever running its body.
        await older;
        Assert.IsFalse(first.Task.IsCompleted);
        Assert.AreEqual("", store.Last);

        second.SetResult();
        await newer;

        Assert.AreEqual(2, store.DebouncedBodyRuns);
        Assert.AreEqual("ab", store.Last);
        Assert.AreEqual(1, store.Completed);   // only the surviving request wrote a result
        Assert.IsNull(store.LiveSearchError); // supersession is not a failure
    }

    [TestMethod]
    public async Task ANewBurstAfterCompletionStartsCleanly()
    {
        using var harness = BlexTestHarness.For<EffectConcurrencyStore>();
        var store = harness.Store;

        await Task.WhenAll(store.TypeAhead("a"), store.TypeAhead("ab"));
        await Task.WhenAll(store.TypeAhead("x"), store.TypeAhead("xy"));

        Assert.AreEqual(2, store.DebouncedBodyRuns);
        Assert.AreEqual("xy", store.Last);
    }

    [TestMethod]
    public async Task AThrowingBodyStillReportsThroughXxxError()
    {
        using var harness = BlexTestHarness.For<EffectConcurrencyStore>();

        await harness.Store.DebouncedBoom();

        Assert.IsExactInstanceOfType<InvalidOperationException>(harness.Store.DebouncedBoomError);
        Assert.IsFalse(harness.Store.DebouncedBoomIsLoading);
    }
}
