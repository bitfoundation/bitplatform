using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Blex;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

/// <summary>
/// bit Blex in BevTools' State panel: the global state of every store, a change per action - its qualified name
/// ("Cart/AddItem") and its arguments, which BevTools reads as it reads any application value - and restoring the state
/// after any of them through Blex's own time travel (<see cref="BlexManager.RestoreGlobalState"/>), the way the Redux
/// DevTools jump to a state. A restore Blex makes for another reason (the Redux DevTools, persistence, another tab) is a
/// change too.
/// </summary>
public sealed class BevBlexStateProvider : IBevStateProvider, IBevStateRestorer, IDisposable
{
    private const int MaxStates = 100;

    // The change this thread is reporting, while BevTools handles it: GetSnapshot then answers with its state.
    [ThreadStatic]
    private static Reporting? _reporting;

    private readonly BlexManager _manager;
    private readonly IDisposable _subscription;
    private readonly object _lock = new();
    // The global state after each change BevTools recorded, by its token: what a restore puts back. The newest hundred,
    // immutable, so nothing else holding the state can change one.
    private readonly Dictionary<string, JsonElement> _states = [];
    private readonly Queue<string> _order = new();
    private int _restores;
    private int _disposed;

    public BevBlexStateProvider(BlexManager manager)
    {
        _manager = manager;
        // Nested actions (an action an action dispatched) are part of the outer one's change.
        _subscription = manager.Subscribe(OnAction, context => !context.IsNested);
        manager.StateRestored += OnRestored;
    }

    public string Name => "Blex";

    public string? Description => _manager.Stores.Count + " store" + (_manager.Stores.Count == 1 ? "" : "s") + ": " + string.Join(", ", _manager.Stores.Select(s => s.Name));

    public event Action<BevStateChangeInfo>? Changed;

    /// <summary>The global state as JSON, one property per store: what the Redux DevTools show for Blex.</summary>
    public object? GetSnapshot()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return null;
        }

        // BevTools asks while it records a change, and only then (not while it is paused): the state the change left is
        // read once, serialized once, and kept for a restore.
        if (_reporting is { } reporting && ReferenceEquals(reporting.Provider, this))
        {
            var state = JsonSerializer.SerializeToElement(reporting.State());
            Remember(reporting.Token, state);
            return state;
        }

        return JsonSerializer.SerializeToElement(_manager.CaptureGlobalState());
    }

    public async ValueTask RestoreAsync(string token, CancellationToken cancellationToken = default)
    {
        JsonElement state;
        bool kept;
        lock (_lock)
        {
            kept = _states.TryGetValue(token, out state);
        }

        if (!kept)
        {
            throw new InvalidOperationException("The state after that change is no longer kept (the newest " + MaxStates + " are).");
        }

        // Through the manager's dispatcher: a store's state belongs to the renderer it is rendered on.
        await _manager.InvokeAsync(() => _manager.RestoreGlobalState(JsonObject.Create(state)!));
    }

    private void OnAction(BlexActionContext context)
    {
        if (Volatile.Read(ref _disposed) != 0 || Changed is not { } changed)
        {
            return;
        }

        var token = context.Sequence.ToString(CultureInfo.InvariantCulture);
        // The state every observer of this action shares, captured when first read: by GetSnapshot, if BevTools records it.
        Report(changed, new BevStateChangeInfo(context.QualifiedName) { RestoreToken = token, Arguments = Arguments(context.Args) }, token, () => context.GlobalState);
    }

    private void OnRestored()
    {
        if (Volatile.Read(ref _disposed) != 0 || Changed is not { } changed)
        {
            return;
        }

        var token = "restored-" + Interlocked.Increment(ref _restores).ToString(CultureInfo.InvariantCulture);
        Report(changed, new BevStateChangeInfo("(state restored)", "time travel, persistence or another tab moved the state without an action") { RestoreToken = token }, token, _manager.CaptureGlobalState);
    }

    private void Report(Action<BevStateChangeInfo> changed, BevStateChangeInfo info, string token, Func<JsonObject> state)
    {
        var previous = _reporting;
        _reporting = new Reporting(this, token, state);
        try
        {
            changed(info);
        }
        finally
        {
            _reporting = previous;
        }
    }

    private void Remember(string token, JsonElement state)
    {
        lock (_lock)
        {
            if (_states.TryAdd(token, state))
            {
                _order.Enqueue(token);
            }

            while (_order.Count > MaxStates)
            {
                _states.Remove(_order.Dequeue());
            }
        }
    }

    /// <summary>The action's arguments as they are: BevTools redacts and shows them at its value access, the payload being the application's data.</summary>
    internal static IReadOnlyList<KeyValuePair<string, object?>>? Arguments(IReadOnlyList<BlexActionArg> args) =>
        args.Count == 0 ? null : args.Select(a => KeyValuePair.Create(a.Name, a.Value)).ToArray();

    private sealed record Reporting(BevBlexStateProvider Provider, string Token, Func<JsonObject> State);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _subscription.Dispose();
        _manager.StateRestored -= OnRestored;
        Changed = null;
    }
}

public static class BitBevToolsBlex
{
    /// <summary>
    /// Shows bit Blex in BevTools' State panel: every store's state, a change per action, and restoring an earlier state
    /// (where BevTools allows restoring: Development, or a process that serves one user). Call after
    /// <c>AddBitBlexServices</c> and <c>AddBitBevToolsServices</c>.
    /// </summary>
    public static IServiceCollection AddBevToolsBlex(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IBevStateProvider>(sp => new BevBlexStateProvider(sp.GetRequiredService<BlexManager>()));
        return services;
    }
}
