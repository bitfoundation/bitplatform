using System.Text.Json.Nodes;
using Bit.Blex.Testing;
using Microsoft.Extensions.Time.Testing;

namespace Bit.Blex.Tests;

[BlexStoreAttribute(Name = "invoices")]
public partial class InvoiceStore
{
    private static readonly IEqualityComparer<IEnumerable<int>?> SameIds = BlexComparers.Sequence<int>();

    private int _next = 1;

    public TaskCompletionSource<string>? Gate { get; set; }

    [BlexStateAttribute] private int _count;
    [BlexStateAttribute] private int _refreshes;
    [BlexStateAttribute(Comparer = nameof(SameIds))] private int[] _ids = [];
    [BlexStateAttribute(Comparer = nameof(IgnoreCase))] private string _owner = "ada";

    private IEqualityComparer<string> IgnoreCase { get; } = StringComparer.OrdinalIgnoreCase;

    /// <summary>A hand-written value-returning action, through the base class's <c>Dispatch&lt;T&gt;</c>.</summary>
    public int Peek() => Dispatch("Peek", () => Count = Count + 100, null);

    [BlexActionAttribute]
    private int OnCreate()
    {
        Count++;
        return _next++;
    }

    [BlexActionAttribute]
    private async Task<string> OnSend(int id)
    {
        var receipt = Gate is null ? $"sent {id}" : await Gate.Task;
        Count += 10;
        return receipt;
    }

    [BlexActionAttribute]
    private ValueTask<int> OnTotal() => new(Count);

    [BlexActionAttribute]
    private void OnLoad(int[] ids) => Ids = ids;

    [BlexEffectAttribute(ThrottleMilliseconds = 1000)]
    private Task OnRefresh()
    {
        Refreshes++;
        return Task.CompletedTask;
    }
}

/// <summary>A clock whose timestamps start near zero: compared with a system timestamp, the time elapsed comes out negative.</summary>
internal sealed class EarlyClock : TimeProvider
{
    public override long GetTimestamp() => 1;
}

[BlexStoreAttribute(Name = "queued-throttle")]
public partial class QueuedThrottleStore
{
    [BlexStateAttribute] private int _runs;

    /// <summary>When set as a run starts, that run waits for it (and keeps it as <see cref="FirstGate"/>).</summary>
    public TaskCompletionSource? Gate { get; set; }

    public TaskCompletionSource? FirstGate { get; private set; }

    [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Queue, ThrottleMilliseconds = 1000)]
    private async Task OnLoad()
    {
        Runs++;
        if (Gate is { } gate)
        {
            FirstGate = gate;
            await gate.Task;
        }
    }
}

/// <summary>
/// Value-returning actions (<c>T</c>, <c>Task&lt;T&gt;</c>, <c>ValueTask&lt;T&gt;</c>), <c>[BlexState(Comparer = ...)]</c>
/// and <c>[BlexEffect(ThrottleMilliseconds = ...)]</c>.
/// </summary>
[TestClass]
public class ActionResultAndThrottleTests
{
    [TestMethod]
    public void A_sync_action_returns_its_result_and_is_recorded_once()
    {
        using var harness = BlexTestHarness.For<InvoiceStore>();

        Assert.AreEqual(1, harness.Store.Create());
        Assert.AreEqual(2, harness.Store.Create());

        Assert.AreSequenceEqual(["Create", "Create"], harness.Log.Names);
        Assert.AreEqual(2, harness.Store.Count);
    }

    [TestMethod]
    public void A_hand_written_dispatch_of_a_value_runs_once_and_returns_it()
    {
        using var harness = BlexTestHarness.For<InvoiceStore>();

        Assert.AreEqual(100, harness.Store.Peek());
        Assert.AreEqual(100, harness.Store.Count);
        Assert.AreSequenceEqual(["Peek"], harness.Log.Names);
    }

    [TestMethod]
    public async Task An_async_action_completes_with_its_result_after_it_is_recorded()
    {
        using var harness = BlexTestHarness.For<InvoiceStore>();
        harness.Store.Gate = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        var send = harness.Store.Send(7);
        Assert.IsFalse(send.IsCompleted);
        harness.Store.Gate.SetResult("receipt 7");

        Assert.AreEqual("receipt 7", await send);
        Assert.AreEqual(10, harness.Store.Count);
        Assert.AreEqual(7, harness.Log.Last!.Args[0].Value);
        Assert.AreEqual(10, await harness.Store.Total());
    }

    [TestMethod]
    public async Task A_vetoed_action_returns_default_and_does_not_run()
    {
        using var harness = BlexTestHarness.WithFilter<InvoiceStore>(_ => false);

        Assert.AreEqual(0, harness.Store.Create());
        Assert.IsNull(await harness.Store.Send(1));
        Assert.AreEqual(0, harness.Store.Count);
        Assert.IsEmpty(harness.Log.Actions);
    }

    [TestMethod]
    public async Task A_value_returning_action_dispatches_by_name_and_replays()
    {
        using var original = BlexTestHarness.For<InvoiceStore>();
        original.Store.Create();
        await original.Store.Send(3);

        using var replica = BlexTestHarness.For<InvoiceStore>();
        await replica.Manager.ReplayAsync(original.Log.Actions);
        await replica.Manager.DispatchAsync("invoices/Send", new JsonObject { ["id"] = 4 });

        Assert.AreEqual(21, replica.Store.Count);
    }

    [TestMethod]
    public void A_state_comparer_turns_an_equal_copy_into_no_change_at_all()
    {
        using var harness = BlexTestHarness.For<InvoiceStore>();
        harness.Store.Load([1, 2, 3]);
        var notifications = harness.Store.CountNotifications(() =>
        {
            harness.Store.Load([1, 2, 3]);   // a new array, the same ids: what polling an endpoint returns
            harness.Store.Owner = "ADA";     // equal ignoring case
        });

        Assert.AreEqual(0, notifications);
        Assert.AreSequenceEqual(["Load"], harness.Log.Names, "an unchanged assignment records nothing");
        Assert.AreEqual("ada", harness.Store.Owner, "an equal value is not assigned either");

        harness.Store.Load([1, 2]);
        harness.Store.Owner = "grace";
        Assert.AreSequenceEqual([1, 2], harness.Store.Ids);
        Assert.AreSequenceEqual(["Load", "Load", "Set Owner"], harness.Log.Names);
    }

    [TestMethod]
    public async Task A_throttled_effect_runs_once_per_window_and_drops_the_rest_without_a_trace()
    {
        var time = new FakeTimeProvider();
        using var harness = BlexTestHarness.For<InvoiceStore>();
        harness.TimeProvider = time;
        var seen = 0;
        using var sub = harness.Manager.Subscribe(_ => seen++);

        await harness.Store.Refresh();
        await harness.Store.Refresh();
        time.Advance(TimeSpan.FromMilliseconds(999));
        await harness.Store.Refresh();

        Assert.AreEqual(1, harness.Store.Refreshes);
        Assert.AreEqual(1, seen, "a dropped invocation is never offered to middleware or recorded");

        time.Advance(TimeSpan.FromMilliseconds(1));
        await harness.Store.Refresh();
        Assert.AreEqual(2, harness.Store.Refreshes);
    }

    [TestMethod]
    public async Task A_vetoed_invocation_does_not_open_a_throttle_window()
    {
        var time = new FakeTimeProvider();
        var allow = false;
        using var harness = BlexTestHarness.WithFilter<InvoiceStore>(_ => allow);
        harness.TimeProvider = time;

        await harness.Store.Refresh();   // vetoed
        allow = true;
        await harness.Store.Refresh();

        Assert.AreEqual(1, harness.Store.Refreshes);
    }

    [TestMethod]
    public async Task A_window_opened_before_the_store_registered_does_not_throttle_on_the_managers_clock()
    {
        var store = new InvoiceStore();
        await store.Refresh();   // unregistered: the system clock

        using var harness = BlexTestHarness.For<InvoiceStore>();
        harness.TimeProvider = new EarlyClock();   // its timestamps sit far below the system clock's
        harness.Manager.Unregister(harness.Store);
        harness.Manager.Register(store);
        await store.Refresh();

        Assert.AreEqual(2, store.Refreshes, "a start read from another clock cannot be compared with this one");
    }

    [TestMethod]
    public async Task A_queued_throttled_run_opens_its_window_when_it_starts_and_one_waiting_drops_the_next()
    {
        var time = new FakeTimeProvider();
        using var harness = BlexTestHarness.For<QueuedThrottleStore>();
        harness.TimeProvider = time;
        var store = harness.Store;

        store.Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = store.Load();                          // runs at 0 and waits
        store.Gate = null;
        time.Advance(TimeSpan.FromMilliseconds(1000));
        var second = store.Load();                         // a window after the first started: queued behind it
        var third = store.Load();                          // the second still waits its turn: dropped
        Assert.IsTrue(third.IsCompleted);

        time.Advance(TimeSpan.FromMilliseconds(4000));
        store.FirstGate!.SetResult();
        await first;
        await second;                                      // starts now, at 5000, and opens its window there
        Assert.AreEqual(2, store.Runs);

        time.Advance(TimeSpan.FromMilliseconds(999));
        await store.Load();                                // 999 ms after the second started: dropped
        Assert.AreEqual(2, store.Runs);

        time.Advance(TimeSpan.FromMilliseconds(1));
        await store.Load();
        Assert.AreEqual(3, store.Runs);
    }

    [TestMethod]
    public async Task A_replay_never_throttles()
    {
        var time = new FakeTimeProvider();
        using var original = BlexTestHarness.For<InvoiceStore>();
        original.TimeProvider = time;
        await original.Store.Refresh();
        time.Advance(TimeSpan.FromSeconds(1));
        await original.Store.Refresh();
        Assert.AreEqual(2, original.Log.CountOf("Refresh"));

        // The replica's clock never moves: outside a replay, the second run would fall into the first one's window.
        using var replica = BlexTestHarness.For<InvoiceStore>();
        replica.TimeProvider = new FakeTimeProvider();
        await replica.Manager.ReplayAsync(original.Log.Actions);

        Assert.AreEqual(2, replica.Store.Refreshes);
    }

    [TestMethod]
    public async Task A_replay_opens_no_window_for_the_live_invocation_after_it()
    {
        using var original = BlexTestHarness.For<InvoiceStore>();
        await original.Store.Refresh();

        using var replica = BlexTestHarness.For<InvoiceStore>();
        replica.TimeProvider = new FakeTimeProvider();
        await replica.Manager.ReplayAsync(original.Log.Actions);
        await replica.Store.Refresh();   // the clock has not moved: a window the replay opened would drop it

        Assert.AreEqual(2, replica.Store.Refreshes);
    }
}
