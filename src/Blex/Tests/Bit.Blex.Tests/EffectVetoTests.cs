using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// A veto means the action never happened. An effect's loading counter, error slot, cancellation
/// token and queue slot are all observable state, so a vetoed effect must not touch any of them.
/// </summary>
[TestClass]
public class EffectVetoTests
{
    private static (EffectConcurrencyStore Store, BlexManager Manager) Setup(Func<BlexPreActionContext, bool> filter)
    {
        var manager = new BlexManager([new BlexFilterMiddleware(filter)]);
        var store = new EffectConcurrencyStore();
        manager.Register(store);
        return (store, manager);
    }

    [TestMethod]
    public async Task VetoedEffect_NeverReportsLoading()
    {
        var (store, _) = Setup(_ => false);

        var loadingSeen = false;
        store.StateChanged += () => loadingSeen |= store.FetchIsLoading;

        await store.Fetch();

        Assert.IsFalse(loadingSeen);
        Assert.IsFalse(store.FetchIsLoading);
    }

    [TestMethod]
    public async Task VetoedEffect_DoesNotClearAPreviousError()
    {
        var allow = true;
        var (store, _) = Setup(_ => allow);

        // Let one run fail so there is an error to preserve.
        store.Gates.Enqueue(new TaskCompletionSource());
        var failing = store.Timeout();
        await failing;
        Assert.IsNotNull(store.TimeoutError);
        var original = store.TimeoutError;

        allow = false;
        await store.Timeout(); // vetoed retry

        Assert.AreSame(original, store.TimeoutError);
    }

    [TestMethod]
    public async Task VetoedLatestEffect_DoesNotCancelTheRunInFlight()
    {
        var allow = true;
        var (store, _) = Setup(_ => allow);

        var gate = new TaskCompletionSource();
        store.Gates.Enqueue(gate);
        var inFlight = store.Search("first");

        allow = false;
        await store.Search("second"); // vetoed: must not supersede "first"

        gate.SetResult();
        await inFlight;

        Assert.AreEqual("first", store.Last);
        Assert.AreEqual(1, store.Completed);
        Assert.IsNull(store.SearchError);
    }

    [TestMethod]
    public async Task VetoedQueueEffect_DoesNotTakeASlot()
    {
        var allow = true;
        var (store, _) = Setup(_ => allow);

        var first = new TaskCompletionSource();
        var third = new TaskCompletionSource();
        store.Gates.Enqueue(first);
        store.Gates.Enqueue(third);

        var a = store.Write("a");

        allow = false;
        var vetoed = store.Write("b"); // must not queue behind "a"
        await vetoed;
        Assert.IsTrue(vetoed.IsCompleted);

        allow = true;
        var c = store.Write("c");

        first.SetResult();
        third.SetResult();
        await Task.WhenAll(a, c);

        Assert.AreEqual("ac", store.Last); // "b" never ran
        Assert.AreEqual(2, store.Completed);
    }

    [TestMethod]
    public async Task VetoedEffect_RecordsNothing()
    {
        var (store, manager) = Setup(ctx => ctx.ActionName != "Fetch");
        var recorded = new List<string>();
        using var sub = manager.Subscribe(ctx => recorded.Add(ctx.ActionName));

        await store.Fetch();

        Assert.IsEmpty(recorded);
    }

    [TestMethod]
    public async Task AllowedEffect_StillRunsItsFullLifecycle()
    {
        var (store, manager) = Setup(_ => true);
        var recorded = new List<string>();
        using var sub = manager.Subscribe(ctx => recorded.Add(ctx.ActionName));

        var gate = new TaskCompletionSource();
        store.Gates.Enqueue(gate);
        var run = store.Fetch();

        Assert.IsTrue(store.FetchIsLoading);
        gate.SetResult();
        await run;

        Assert.IsFalse(store.FetchIsLoading);
        Assert.AreEqual(1, store.Completed);
        Assert.AreSequenceEqual(["Fetch"], recorded);
    }

    [TestMethod]
    public async Task DroppedEffect_NeverReachesTheFilter()
    {
        var seen = new List<string>();
        var manager = new BlexManager([new BlexFilterMiddleware(ctx => { seen.Add(ctx.ActionName); return true; })]);
        var store = new EffectConcurrencyStore();
        manager.Register(store);

        var gate = new TaskCompletionSource();
        store.Gates.Enqueue(gate);
        var first = store.Submit();
        await store.Submit(); // dropped: nothing was dispatched, so nothing to veto

        gate.SetResult();
        await first;

        Assert.AreSequenceEqual(["Submit"], seen);
    }

    [TestMethod]
    public async Task EffectArguments_ReachTheFilterBeforeTheBodyRuns()
    {
        List<BlexActionArg>? captured = null;
        var manager = new BlexManager([new BlexFilterMiddleware(ctx =>
        {
            captured = [.. ctx.Args];
            return false;
        })]);
        var store = new EffectConcurrencyStore();
        manager.Register(store);

        await store.Search("needle");

        Assert.IsNotNull(captured);
        var arg = Assert.ContainsSingle(captured);
        Assert.AreEqual("query", arg.Name);
        Assert.AreEqual("needle", arg.Value);
        Assert.AreEqual("", store.Last); // body never ran
    }
}
