using Bit.Blex.Query;
using Bit.Blex.Testing;
using Bit.Blex.Tests.Query;
using static Bit.Blex.Tests.Query.QueryTest;

namespace Bit.Blex.Tests;

/// <summary>A ledger whose failing action first tells another store, the way a cross-store workflow does.</summary>
[BlexStoreAttribute(Name = "notifyingLedger", RollbackOnError = true)]
public partial class NotifyingLedgerStore
{
    [BlexStateAttribute] private int _balance = 100;

    public Action? Notifier { get; set; }

    [BlexActionAttribute]
    private void OnWithdrawThenNotify(int amount)
    {
        Balance -= amount;
        Notifier?.Invoke();
        if (Balance < 0)
            throw new InvalidOperationException("Insufficient funds.");
    }
}

/// <summary>Effects that overlap: one changes state and parks, others run and finish while it is parked.</summary>
[BlexStoreAttribute(Name = "midflight")]
public partial class MidFlightStore
{
    [BlexStateAttribute] private int _items;

    public TaskCompletionSource Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    [BlexEffectAttribute]
    private async Task OnSlow()
    {
        Items = 1;
        await Gate.Task;
    }

    /// <summary>Parks without changing anything.</summary>
    [BlexEffectAttribute]
    private async Task OnIdle() => await Gate.Task;

    [BlexEffectAttribute]
    private async Task OnQuick() => await Task.Yield();

    [BlexEffectAttribute]
    private async Task OnFail()
    {
        await Task.Yield();
        throw new InvalidOperationException("quick failure");
    }
}

/// <summary>An effect that waits for something its own store's action does: the workflow WhenAsync exists for.</summary>
[BlexStoreAttribute(Name = "checkout")]
public partial class CheckoutStore
{
    [BlexStateAttribute] private bool _confirmed;
    [BlexStateAttribute] private bool _submitted;

    public BlexManager? Host { get; set; }

    [BlexEffectAttribute]
    private async Task OnSubmit()
    {
        await Host!.WhenAsync(() => Confirmed);
        Submitted = true;
    }

    [BlexActionAttribute]
    private void OnConfirm() => Confirmed = true;
}

/// <summary>
/// Regressions found by independent reviews: each test pins a failure that the first versions of these
/// features had.
/// </summary>
[TestClass]
[DoNotParallelize]
public class ReviewRegressionTests
{
    [TestMethod]
    public async Task An_effects_lifecycle_while_another_action_runs_does_not_let_a_reaction_overtake_its_cause()
    {
        var manager = new BlexManager();
        var store = new MidFlightStore();
        var counter = new CounterStore();
        manager.Register(store);
        manager.Register(counter);
        var recorded = new List<string>();
        using var log = manager.Subscribe(ctx => recorded.Add(ctx.QualifiedName));
        using var reactor = manager.Subscribe(() => store.Items, items => counter.Add(items));

        var slow = store.Slow();     // Items = 1, then parks: recorded only when it ends
        await store.Quick();         // its IsLoading flips while Slow is still running

        Assert.IsFalse(recorded.Contains("counter/Add"), "a re-evaluation now would record the reaction before its cause");

        store.Gate.SetResult();
        await slow;
        Assert.AreSequenceEqual(["midflight/Slow", "counter/Add"], recorded);
    }

    [TestMethod]
    public async Task A_change_deferred_behind_an_action_that_records_nothing_is_announced_when_it_ends()
    {
        var manager = new BlexManager();
        var store = new MidFlightStore();
        manager.Register(store);
        var seen = new List<bool>();
        using var subscription = manager.Subscribe(() => store.FailError is not null, seen.Add);

        var idle = store.Idle();     // parks, changing nothing: it will record nothing
        await store.Fail();          // FailError is set while Idle is still running
        Assert.IsEmpty(seen, "deferred while Idle runs");

        store.Gate.SetResult();
        await idle;
        Assert.AreSequenceEqual([true], seen, "and announced once it ends, recorded or not");
    }

    [TestMethod]
    public async Task An_effect_waiting_for_its_own_stores_action_is_not_left_hanging()
    {
        var manager = new BlexManager();
        var store = new CheckoutStore { Host = manager };
        manager.Register(store);

        var submit = store.Submit();   // waits for Confirmed, and keeps the store's record open while it does
        store.Confirm();               // folded into Submit's record, which only comes once Submit ends

        await submit.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(store.Submitted);
    }

    [TestMethod]
    public void Undo_never_returns_to_the_state_a_rolled_back_action_left_mid_body()
    {
        var manager = new BlexManager();
        var ledger = new NotifyingLedgerStore();
        var counter = new CounterStore();
        manager.Register(ledger);
        manager.Register(counter);
        // Two steps recorded mid-body, with the balance at -50: one ends up on the undo stack, one in the present.
        ledger.Notifier = () => { counter.Increment(); counter.Increment(); };
        var history = new BlexHistory(manager);
        history.Start();

        Assert.ThrowsExactly<InvalidOperationException>(() => ledger.WithdrawThenNotify(150));
        Assert.AreEqual(100, ledger.Balance);
        Assert.AreEqual(2, counter.Count, "the other store's actions stand");

        history.Undo();
        Assert.AreEqual((100, 1), (ledger.Balance, counter.Count), "the step between the increments never held -50");
        history.Undo();
        Assert.AreEqual((100, 0), (ledger.Balance, counter.Count));
        history.Redo();
        history.Redo();
        Assert.AreEqual((100, 2), (ledger.Balance, counter.Count), "redo lands on the rolled-back balance too");

        counter.Increment();
        history.Undo();
        Assert.AreEqual((100, 2), (ledger.Balance, counter.Count));
    }

    [TestMethod]
    public async Task A_waiter_never_sees_a_tree_half_restored()
    {
        var manager = new BlexManager();
        var first = new CounterStore();
        var second = new NotifyingLedgerStore();
        manager.Register(first);
        manager.Register(second);
        var before = manager.CaptureGlobalState();
        first.Increment();
        second.WithdrawThenNotify(30);   // 70

        // True only while the counter is back at 0 but the ledger is not yet back at 100: never, in a whole restore.
        var torn = manager.WhenAsync(() => first.Count == 0 && second.Balance == 70);
        manager.RestoreGlobalState(before);
        await Task.Yield();

        Assert.IsFalse(torn.IsCompleted);
        Assert.AreEqual((0, 100), (first.Count, second.Balance));
    }

    [TestMethod]
    public async Task An_invalidation_while_an_unobserved_fetch_runs_leaves_its_answer_stale()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<string>();
        var read = client.FetchAsync(["todos"], server.Fetch());   // nobody observes the entry

        await client.InvalidateAsync(["todos"]);                   // a mutation landed meanwhile
        server.Respond(0, "before the mutation");
        Assert.AreEqual("before the mutation", await read.WaitAsync(TimeSpan.FromSeconds(5)), "the read gets the answer it asked for");

        using var view = client.Query(["todos"], server.Fetch(), new BlexQueryOptions { StaleTime = TimeSpan.FromMinutes(5) });
        Assert.AreEqual(2, server.Calls, "that answer predates the invalidation, so it is not fresh");
    }

    [TestMethod]
    public async Task A_read_through_cancelled_by_a_clear_throws_instead_of_returning_the_old_data()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<string>();
        client.SetQueryData<string>(["me"], "ada");
        await client.InvalidateAsync(["me"]);   // stale, so the read fetches

        var read = client.FetchAsync(["me"], server.Fetch());
        client.Clear();                          // sign-out

        await Assert.ThrowsAsync<OperationCanceledException>(() => read.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [TestMethod]
    public async Task Disposing_the_client_stops_the_polling_of_a_query_nobody_disposed()
    {
        var (client, clock, _) = Client();
        var poll = client.Query<int>(["poll"], _ => Task.FromResult(1), new BlexQueryOptions { RefetchInterval = TimeSpan.FromSeconds(10) });
        await poll.WhenSettledAsync();
        Assert.AreEqual(1, clock.LiveTimers, "the polling timer");

        client.Dispose();

        Assert.AreEqual(0, clock.LiveTimers);
    }

    [TestMethod]
    public async Task A_fresh_read_through_leaves_the_entry_collectable()
    {
        var (client, clock, _) = Client(o => o.GcTime = TimeSpan.FromSeconds(30));
        var fresh = new BlexQueryOptions { StaleTime = TimeSpan.FromMinutes(5) };
        await client.FetchAsync<int>(["n"], _ => Task.FromResult(1), fresh);
        await client.FetchAsync<int>(["n"], _ => Task.FromResult(2), fresh);   // a fresh hit

        clock.Advance(TimeSpan.FromSeconds(30));

        Assert.IsFalse(client.TryGetQueryData<int>(["n"], out _), "an unobserved entry read while fresh must still be collected");
    }

    [TestMethod]
    public void A_throwing_update_leaves_the_entry_collectable()
    {
        var (client, clock, _) = Client(o => o.GcTime = TimeSpan.FromSeconds(30));
        client.SetQueryData<int>(["n"], 1);

        Assert.ThrowsExactly<InvalidOperationException>(() => client.SetQueryData<int>(["n"], _ => throw new InvalidOperationException()));
        Assert.ThrowsExactly<InvalidOperationException>(() => client.Optimistic<int>(["n"], _ => throw new InvalidOperationException()));
        clock.Advance(TimeSpan.FromSeconds(30));

        Assert.IsFalse(client.TryGetQueryData<int>(["n"], out _));
    }

    [TestMethod]
    public async Task A_throwing_telemetry_listener_breaks_neither_actions_nor_fetches()
    {
        using var meters = new System.Diagnostics.Metrics.MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == BlexDiagnostics.SourceName)
                    listener.EnableMeasurementEvents(instrument);
            },
        };
        meters.SetMeasurementEventCallback<long>((_, _, _, _) => throw new InvalidOperationException("broken exporter"));
        meters.SetMeasurementEventCallback<double>((_, _, _, _) => throw new InvalidOperationException("broken exporter"));
        meters.Start();
        using var spans = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == BlexDiagnostics.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _ => throw new InvalidOperationException("broken exporter"),
        };
        System.Diagnostics.ActivitySource.AddActivityListener(spans);

        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        counter.Increment();
        Assert.AreEqual(1, counter.Count);

        var (client, _, _) = Client();
        using var query = client.Query<int>(["telemetry"], _ => Task.FromResult(7));
        await query.WhenSettledAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(7, query.Data);
        Assert.IsFalse(query.IsFetching, "the entry settled despite the listener");
    }
    [TestMethod]
    public async Task A_read_through_superseded_by_an_invalidation_returns_the_newer_answer()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<string>();
        using var observer = client.Query(["n"], server.Fetch());   // fetch 0 in flight, observed

        var read = client.FetchAsync(["n"], server.Fetch());          // joins fetch 0
        var invalidation = client.InvalidateAsync(["n"]);             // supersedes it with fetch 1

        server.Respond(1, "new");
        Assert.AreEqual("new", await read.WaitAsync(TimeSpan.FromSeconds(5)), "neither stale data nor a spurious cancellation");
        await invalidation;
    }

    [TestMethod]
    public async Task WhenSettledAsync_waits_for_the_fetch_that_superseded_the_one_it_joined()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<string>();
        using var query = client.Query(["n"], server.Fetch());
        var settled = query.WhenSettledAsync();

        _ = query.RefetchAsync();
        await Task.Delay(50);
        Assert.IsFalse(settled.IsCompleted, "the superseded fetch's cancellation is not a settlement");

        server.Respond(1, "latest");
        await settled.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual("latest", query.Data);
    }

    [TestMethod]
    public void ResetState_is_not_mistaken_for_an_in_place_mutation()
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add, DetectInPlaceMutations = true };
        var store = new BasketStore();
        manager.Register(store);
        store.Add("apple");

        store.ResetState();
        manager.ResetAll();

        Assert.IsEmpty(store.Items);
        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public void An_error_hook_reading_a_computed_sees_the_rolled_back_state()
    {
        using var harness = BlexTestHarness.For<LedgerStore>();
        int? seen = null;
        harness.Manager.ActionFailed += _ => seen = harness.Store.Doubled;

        Assert.ThrowsExactly<InvalidOperationException>(() => harness.Store.WithdrawChecked(150));

        Assert.AreEqual(200, seen, "Doubled was memoized mid-body at -100; the hook must not see that");
    }

    [TestMethod]
    public void A_subscription_that_saw_the_undone_state_mid_body_lands_back_on_the_real_one()
    {
        var manager = new BlexManager();
        var ledger = new NotifyingLedgerStore();
        var counter = new CounterStore();
        manager.Register(ledger);
        manager.Register(counter);
        ledger.Notifier = counter.Increment;   // a recorded action of another store, mid-body

        var seen = new List<int>();
        using var sub = manager.Subscribe(() => ledger.Balance, seen.Add);

        Assert.ThrowsExactly<InvalidOperationException>(() => ledger.WithdrawThenNotify(150));

        Assert.AreSequenceEqual([-50, 100], seen);
        Assert.AreEqual(100, ledger.Balance);
    }

    [TestMethod]
    public async Task Clearing_the_cache_moves_its_observers_to_fresh_entries()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<string>();
        using var me = client.Query(["me"], server.Fetch());
        server.Respond(0, "ada");
        await me.WhenSettledAsync();

        client.Clear();   // sign-out

        Assert.IsFalse(me.HasData, "the previous user's data is gone from the observer too");
        Assert.AreEqual(2, server.Calls, "the observer fetches again on its fresh entry");
        server.Respond(1, "grace");
        await me.WhenSettledAsync();
        Assert.AreEqual("grace", me.Data);

        client.SetQueryData<string>(["me"], "changed");
        Assert.AreEqual("changed", me.Data, "the observer is on the entry the cache holds now");
    }

    [TestMethod]
    public void A_failed_fetch_for_a_new_key_does_not_hide_behind_the_previous_keys_data()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<string>();
        using var next = client.Query(["page", 2], server.Fetch(), options: null, hasPlaceholder: true, placeholder: "page 1");
        Assert.IsTrue(next.IsPlaceholderData);

        server.Fail(0, new InvalidOperationException("down"));
        next.WhenSettledAsync().Wait(TimeSpan.FromSeconds(5));

        Assert.IsTrue(next.IsError);
        Assert.IsFalse(next.HasData);
        Assert.IsFalse(next.IsPlaceholderData);
    }

    [TestMethod]
    public void Each_client_owns_a_copy_of_its_options()
    {
        var shared = new BlexQueryClientOptions();
        var a = new BlexQueryClient(null, shared);
        var b = new BlexQueryClient(null, shared);

        a.Options.StaleTime = TimeSpan.FromMinutes(5);
        shared.RetryCount = 9;

        Assert.AreEqual(TimeSpan.Zero, b.Options.StaleTime, "one circuit's client must not change another's");
        Assert.AreEqual(3, a.Options.RetryCount);
    }

    [TestMethod]
    public async Task Using_an_entry_during_its_gc_wait_postpones_its_removal()
    {
        var (client, clock, _) = Client(o => o.GcTime = TimeSpan.FromSeconds(30));
        var query = client.Query<string>(["n"], _ => Task.FromResult("v"));
        await query.WhenSettledAsync();
        query.Dispose();

        clock.Advance(TimeSpan.FromSeconds(20));
        client.SetQueryData<string>(["n"], "touched");   // in use again: a fresh 30 s
        clock.Advance(TimeSpan.FromSeconds(20));
        Assert.AreEqual("touched", client.GetQueryData<string>(["n"]));

        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.IsFalse(client.TryGetQueryData<string>(["n"], out _));
    }
}
