using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Blex;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

/// <summary>
/// bit Blex in BevTools' State panel: the global state of every store, a change per action - its qualified name
/// ("Cart/AddItem") and payload, redacted - and restoring the state after any of them through Blex's own time travel
/// (<see cref="BlexManager.RestoreGlobalState"/>), the way the Redux DevTools jump to a state. A restore Blex makes
/// for another reason (the Redux DevTools, persistence, another tab) is a change too.
/// </summary>
public sealed class BevBlexStateProvider : IBevStateProvider, IBevStateRestorer, IDisposable
{
    private const int MaxStates = 100;

    private readonly BlexManager _manager;
    private readonly IDisposable _subscription;
    private readonly object _lock = new();
    // The global state after each change, by its token: what a restore puts back. The newest hundred.
    private readonly Dictionary<string, JsonObject> _states = [];
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
    public object? GetSnapshot() => Volatile.Read(ref _disposed) != 0 ? null : JsonSerializer.SerializeToElement(_manager.CaptureGlobalState());

    public async ValueTask RestoreAsync(string token, CancellationToken cancellationToken = default)
    {
        JsonObject? state;
        lock (_lock)
        {
            _states.TryGetValue(token, out state);
        }

        if (state is null)
        {
            throw new InvalidOperationException("The state after that change is no longer kept (the newest " + MaxStates + " are).");
        }

        // Through the manager's dispatcher: a store's state belongs to the renderer it is rendered on.
        await _manager.InvokeAsync(() => _manager.RestoreGlobalState((JsonObject)state.DeepClone()));
    }

    private void OnAction(BlexActionContext context)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        var token = context.Sequence.ToString(CultureInfo.InvariantCulture);
        Remember(token, (JsonObject)context.GlobalState.DeepClone());
        Changed?.Invoke(new BevStateChangeInfo(context.QualifiedName, Payload(context.Args)) { RestoreToken = token });
    }

    private void OnRestored()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        var token = "restored-" + Interlocked.Increment(ref _restores).ToString(CultureInfo.InvariantCulture);
        Remember(token, _manager.CaptureGlobalState());
        Changed?.Invoke(new BevStateChangeInfo("(state restored)", "time travel, persistence or another tab moved the state without an action") { RestoreToken = token });
    }

    private void Remember(string token, JsonObject state)
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

    /// <summary>
    /// "sku=A-12, quantity=2": scalars as they are, a value under a sensitive name redacted, anything else by its type,
    /// credential-shaped text redacted - the payload is the application's data.
    /// </summary>
    internal static string? Payload(IReadOnlyList<BlexActionArg> args)
    {
        if (args.Count == 0)
        {
            return null;
        }

        var text = string.Join(", ", args.Take(8).Select(a => a.Name + "=" + (BevRedactor.Default.IsSensitiveName(a.Name) ? BevRedactor.RedactedValue : Display(a.Value))));
        text = BevRedactor.RedactText(text);
        return text.Length <= 300 ? text : text[..299] + "…";
    }

    private static string Display(object? value) => value switch
    {
        null => "null",
        string s => s.Length <= 60 ? s : s[..59] + "…",
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTime or DateTimeOffset => ((IFormattable)value).ToString("O", CultureInfo.InvariantCulture),
        bool or char or Enum or Guid or TimeSpan or TimeOnly => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
        IFormattable number when value.GetType().IsPrimitive || value is decimal => number.ToString(null, CultureInfo.InvariantCulture),
        _ => value.GetType().Name,
    };

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
