using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Blex;

namespace Bit.Blex.Tests;

[BlexStoreAttribute(Name = "counter")]
public partial class CounterStore
{
    [BlexStateAttribute] private int _count;
    [BlexStateAttribute] private string _label = "idle";

    [BlexComputedAttribute]
    private int ComputeDoubleCount() => Count * 2;

    private int _computeCalls;
    public int ComputeCalls => _computeCalls;

    [BlexComputedAttribute]
    private int ComputeTrackedCount()
    {
        _computeCalls++;
        return Count + 1;
    }

    [BlexActionAttribute]
    private void OnIncrement() => Count++;

    [BlexActionAttribute]
    private void OnAdd(int amount) => Count += amount;

    [BlexActionAttribute]
    private void OnReset()
    {
        Count = 0;
        Label = "idle";
    }

    [BlexActionAttribute(Name = "LoadData")]
    private async Task OnLoadAsync()
    {
        Label = "loading";
        await Task.Delay(1);
        Count = 42;
        Label = "loaded";
    }

    // Returns ValueTask<T>: the generator must await it (via AsTask) rather than fire-and-forget.
    // The explicit name contains spaces, so it is a display label only; the wrapper is "LoadValue".
    // BLEX011 (discarded return value) is expected here and suppressed via NoWarn in the csproj
    // -- exercising that shape is exactly what this fixture is for.
    [BlexActionAttribute(Name = "Load Value")]
    private async ValueTask<int> OnLoadValue()
    {
        await Task.Delay(1);
        Count = 7;
        return Count;
    }
}

[BlexStoreAttribute]
public partial class TodoStore
{
    [BlexStateAttribute] private List<string> _items = new();

    [BlexActionAttribute]
    private void OnAddItem(string text) => Items = new List<string>(Items) { text };
}

[BlexStoreAttribute(Name = "settings", Persist = true)]
public partial class SettingsStore
{
    [BlexStateAttribute] private string _theme = "light";
    [BlexStateAttribute] private int _fontSize = 14;

    [BlexActionAttribute]
    private void OnSetTheme(string theme) => Theme = theme;
}

[BlexStoreAttribute(Name = "data")]
public partial class DataStore
{
    [BlexStateAttribute] private string _value = "";
    public bool ShouldThrow { get; set; }

    [BlexEffectAttribute]
    private async Task OnLoad(string input)
    {
        await Task.Delay(1);
        if (ShouldThrow)
            throw new InvalidOperationException("boom");
        Value = input;
    }
}

[BlexStoreAttribute(Name = "profile")]
public partial class ProfileStore
{
    [BlexStateAttribute] private string? _userName = "anonymous";
    [BlexStateAttribute] private int _age;

    [BlexActionAttribute]
    private void OnSignOut() => UserName = null;

    [BlexActionAttribute]
    private void OnSignIn(string name) => UserName = name;
}

/// <summary>Gated effects for deterministic concurrency tests. Enqueue gates before invoking.</summary>
[BlexStoreAttribute(Name = "fx")]
public partial class EffectConcurrencyStore
{
    [BlexStateAttribute] private string _last = "";
    [BlexStateAttribute] private int _completed;

    public Queue<TaskCompletionSource> Gates { get; } = new();

    private Task NextGate() => Gates.Count > 0 ? Gates.Dequeue().Task : Task.CompletedTask;

    [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Latest)]
    private async Task OnSearch(string query, CancellationToken ct)
    {
        await NextGate().WaitAsync(ct);
        Last = query;
        Completed++;
    }

    [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Drop)]
    private async Task OnSubmit()
    {
        await NextGate();
        Completed++;
    }

    [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Queue)]
    private async Task OnWrite(string value)
    {
        await NextGate();
        Last = Last + value;
        Completed++;
    }

    [BlexEffectAttribute]
    private async Task OnFetch()
    {
        await NextGate();
        Completed++;
    }

    // The gate is intentionally NOT linked to the token: a superseded run keeps executing and
    // then fails, which must not clobber the newest run's error state.
    [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Latest)]
    private async Task OnFlaky(bool fail, CancellationToken ct)
    {
        await NextGate();
        if (fail)
            throw new InvalidOperationException("stale-boom");
        Last = "flaky-ok";
        Completed++;
    }

    // Throws a *foreign* cancellation (like an HttpClient timeout) while our token is NOT
    // cancelled -- this must surface as an error, not be swallowed as a benign cancel.
    [BlexEffectAttribute]
    private async Task OnTimeout(CancellationToken ct)
    {
        await Task.Yield();
        throw new TaskCanceledException("simulated http timeout");
    }

    // Queue effect whose runs can fail: a failed predecessor's error must not survive a
    // successful successor (the successor clears the error only after the predecessor ends).
    [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Queue)]
    private async Task OnQueuedFlaky(bool fail)
    {
        await NextGate();
        if (fail)
            throw new InvalidOperationException("queued-boom");
        Completed++;
    }

    public int DebouncedBodyRuns { get; private set; }

    // Short enough to keep tests quick, long enough that a burst dispatched in one go cannot
    // straddle it even on a loaded CI runner. Keep in sync with EffectDebounceTests.Quiet.
    [BlexEffectAttribute(DebounceMilliseconds = 250)]
    private async Task OnTypeAhead(string query)
    {
        DebouncedBodyRuns++;
        await NextGate();
        Last = query;
        Completed++;
    }

    // Debounce gates the burst; Latest cancels a request that is already in flight when the next
    // one gets through.
    [BlexEffectAttribute(DebounceMilliseconds = 250, Concurrency = BlexEffectConcurrency.Latest)]
    private async Task OnLiveSearch(string query, CancellationToken ct)
    {
        DebouncedBodyRuns++;
        await NextGate().WaitAsync(ct);
        Last = query;
        Completed++;
    }

    [BlexEffectAttribute(DebounceMilliseconds = 250)]
    private async Task OnDebouncedBoom()
    {
        DebouncedBodyRuns++;
        await Task.Yield();
        throw new InvalidOperationException("debounced-boom");
    }
}

/// <summary>
/// A store with one effect that parks on a gate, so a second action can be dispatched while the
/// first is still in flight (the default <c>BlexEffectConcurrency.Parallel</c> shape).
/// </summary>
[BlexStoreAttribute(Name = "gated")]
public partial class GatedStore
{
    [BlexStateAttribute] private int _count;

    public TaskCompletionSource Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    [BlexEffectAttribute]
    private async Task OnSlow()
    {
        await Gate.Task;
        Count++;
    }

    [BlexEffectAttribute]
    private async Task OnQuick()
    {
        await Task.Yield();
        Count += 10;
    }

    /// <summary>A plain synchronous action, for dispatching one while <c>Slow</c> is still awaiting.</summary>
    [BlexActionAttribute]
    private void OnBump() => Count += 100;
}

/// <summary>Actions and effects that fail on demand, for the action-error hooks.</summary>
[BlexStoreAttribute(Name = "failing")]
public partial class FailingStore
{
    [BlexStateAttribute] private int _value;

    public TaskCompletionSource Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    [BlexActionAttribute]
    private void OnBoom() => throw new InvalidOperationException("sync-boom");

    [BlexActionAttribute]
    private async Task OnBoomAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("async-boom");
    }

    [BlexActionAttribute]
    private void OnSet(int value) => Value = value;

    /// <summary>An action that calls another action which throws: one failure, several frames.</summary>
    [BlexActionAttribute]
    private void OnOuter() => Boom();

    /// <summary>An effect whose body calls a synchronous action that throws.</summary>
    [BlexEffectAttribute]
    private async Task OnOuterEffect()
    {
        await Task.Yield();
        Boom();
    }

    [BlexEffectAttribute]
    private async Task OnLoad(string key)
    {
        await Task.Yield();
        throw new InvalidOperationException("effect-boom: " + key);
    }

    /// <summary>Parks until the gate opens or the effect's own token is cancelled.</summary>
    [BlexEffectAttribute]
    private async Task OnWatch(CancellationToken ct)
    {
        await Gate.Task.WaitAsync(ct);
        Value = 1;
    }

    [BlexEffectAttribute(DebounceMilliseconds = 10_000)]
    private async Task OnSlowDebounce()
    {
        await Task.Yield();
        Value = 2;
    }
}

public record Todo(int Id, string Text, bool Done);

[BlexStoreAttribute(Name = "todos")]
public partial class EntityTodoStore
{
    private static readonly BlexEntityAdapter<Todo, int> Adapter = new(t => t.Id);

    [BlexStateAttribute] private BlexEntityState<Todo, int> _todos = Adapter.GetInitialState();

    [BlexComputedAttribute] private int ComputeRemaining()
    {
        var n = 0;
        foreach (var t in Todos.All)
            if (!t.Done) n++;
        return n;
    }

    [BlexActionAttribute] private void OnAdd(Todo todo) => Todos = Adapter.UpsertOne(Todos, todo);

    [BlexActionAttribute] private void OnToggle(int id) => Todos = Adapter.UpdateOne(Todos, id, t => t with { Done = !t.Done });

    [BlexActionAttribute] private void OnRemove(int id) => Todos = Adapter.RemoveOne(Todos, id);
}
