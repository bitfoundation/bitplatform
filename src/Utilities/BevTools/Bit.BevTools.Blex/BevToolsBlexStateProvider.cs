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
public sealed class BevToolsBlexStateProvider : IBevToolsStateProvider, IBevToolsStateRestorer, IDisposable
{
    private const int DefaultMaxStates = 100;

    // The change this thread is reporting, while BevTools handles it: GetSnapshot then answers with its state.
    [ThreadStatic]
    private static Reporting? _reporting;

    private readonly BlexManager _manager;
    private readonly IDisposable _subscription;
    private readonly object _lock = new();
    // The global state after each change BevTools recorded, by its token: what a restore puts back. The newest ones, as
    // many as the State panel's history keeps, immutable, so nothing else holding the state can change one.
    private readonly Dictionary<string, JsonElement> _states = [];
    private readonly Queue<string> _order = new();
    private readonly int _maxStates;
    private int _restores;
    private int _disposed;

    /// <param name="manager">The Blex manager whose stores are shown.</param>
    /// <param name="maxStates">
    /// How many recorded states are kept for a restore: at least the session's
    /// <see cref="BevToolsOptions.MaxStateChangesPerProvider"/>, so every change the history shows can be restored.
    /// </param>
    public BevToolsBlexStateProvider(BlexManager manager, int maxStates = DefaultMaxStates)
    {
        _manager = manager;
        _maxStates = Math.Max(1, maxStates);
        // Nested actions (an action an action dispatched) are part of the outer one's change.
        _subscription = manager.Subscribe(OnAction, context => !context.IsNested);
        manager.StateRestored += OnRestored;
    }

    public string Name => "Blex";

    public string? Description => _manager.Stores.Count + " store" + (_manager.Stores.Count == 1 ? "" : "s") + ": " + string.Join(", ", _manager.Stores.Select(s => s.Name));

    public event Action<BevToolsStateChangeInfo>? Changed;

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
            throw new InvalidOperationException("The state after that change is no longer kept (the newest " + _maxStates + " are).");
        }

        // Through the manager's dispatcher: a store's state belongs to the renderer it is rendered on.
        await _manager.InvokeAsync(() =>
        {
            // Blex reports a store it could not restore through OnError and goes on with the others; seen here, that
            // fails the restore rather than reading as one. The app's own handler still gets every error.
            List<BlexError>? failures = null;
            var handler = _manager.OnError;
            _manager.OnError = error =>
            {
                if (error.Source == "restore")
                {
                    (failures ??= []).Add(error);
                }

                if (handler is not null)
                {
                    handler(error);
                }
                else
                {
                    Console.Error.WriteLine($"[Blex] {error.Source} failed{(error.Detail is null ? "" : $" ({error.Detail})")}: {error.Exception.Message}");
                }
            };
            try
            {
                _manager.RestoreGlobalState(JsonObject.Create(state)!);
            }
            finally
            {
                _manager.OnError = handler;
            }

            if (failures is not null)
            {
                throw new InvalidOperationException("Blex could not restore every store: "
                    + string.Join("; ", failures.Select(f => (f.Detail ?? "?") + " (" + f.Exception.GetType().Name + ": " + f.Exception.Message + ")")), failures[0].Exception);
            }
        });
    }

    private void OnAction(BlexActionContext context)
    {
        if (Volatile.Read(ref _disposed) != 0 || Changed is not { } changed)
        {
            return;
        }

        var token = context.Sequence.ToString(CultureInfo.InvariantCulture);
        // The state every observer of this action shares, captured when first read: by GetSnapshot, if BevTools records it.
        Report(changed, new BevToolsStateChangeInfo(context.QualifiedName) { RestoreToken = token, Arguments = Arguments(context.Args) }, token, () => context.GlobalState);
    }

    private void OnRestored()
    {
        if (Volatile.Read(ref _disposed) != 0 || Changed is not { } changed)
        {
            return;
        }

        var token = "restored-" + Interlocked.Increment(ref _restores).ToString(CultureInfo.InvariantCulture);
        Report(changed, new BevToolsStateChangeInfo("(state restored)", "time travel, persistence or another tab moved the state without an action") { RestoreToken = token }, token, _manager.CaptureGlobalState);
    }

    private void Report(Action<BevToolsStateChangeInfo> changed, BevToolsStateChangeInfo info, string token, Func<JsonObject> state)
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

            while (_order.Count > _maxStates)
            {
                _states.Remove(_order.Dequeue());
            }
        }
    }

    /// <summary>The action's arguments as they are: BevTools redacts and shows them at its value access, the payload being the application's data.</summary>
    internal static IReadOnlyList<KeyValuePair<string, object?>>? Arguments(IReadOnlyList<BlexActionArg> args) =>
        args.Count == 0 ? null : args.Select(a => KeyValuePair.Create(a.Name, a.Value)).ToArray();

    private sealed record Reporting(BevToolsBlexStateProvider Provider, string Token, Func<JsonObject> State);

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
