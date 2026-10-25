using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>Configuration for the Blex manager, populated via <c>AddBitBlexServices</c>.</summary>
public sealed class BlexOptions
{
    /// <summary>
    /// Every registered middleware in registration order. Each entry is either a <see cref="Type"/>
    /// to resolve from DI or a ready <see cref="IBlexMiddleware"/> instance. Kept as one list so the
    /// pipeline runs in the order it was written: order decides which veto is consulted first and
    /// what a logger sees, and splitting the two kinds into separate lists silently reordered them.
    /// </summary>
    internal List<object> MiddlewareEntries { get; } = [];

    internal IEnumerable<Type> MiddlewareTypes => MiddlewareEntries.OfType<Type>();

    /// <summary>Display name shown in Redux DevTools. Defaults to <c>"Blex"</c>.</summary>
    public string DevToolsName { get; set; } = "Blex";

    /// <summary>
    /// Sanitizer applied to the state tree before it is sent to DevTools (display only). Wired onto
    /// <see cref="BlexManager.DevToolsStateSanitizer"/>. See the remarks there for the time-travel caveat.
    /// </summary>
    public Func<JsonObject, JsonObject>? DevToolsStateSanitizer { get; set; }

    /// <summary>Sanitizer applied to the action label sent to DevTools.</summary>
    public Func<string, string>? DevToolsActionSanitizer { get; set; }

    /// <summary>
    /// Sink for non-fatal errors Blex isolates from the dispatch pipeline (throwing
    /// subscribers, middleware, persistence writes, restores, ...). Wired onto
    /// <see cref="BlexManager.OnError"/>. When unset, errors go to <see cref="Console.Error"/>.
    /// </summary>
    public Action<BlexError>? OnError { get; set; }

    /// <summary>
    /// Called when an action or effect body throws, with the store, action, arguments and
    /// exception. Wired onto <see cref="BlexManager.ActionFailed"/>.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="OnError"/>, which reports failures Blex <em>isolates</em> from the
    /// pipeline (a throwing subscriber, a failed storage write). This one reports the application's
    /// own action bodies failing -- the thing a crash reporter actually wants -- and does not
    /// suppress the exception. Cancellation is not reported; see <see cref="BlexActionErrorContext"/>.
    /// </remarks>
    /// <example><code>
    /// options.OnActionError = ctx => _logger.LogError(ctx.Exception, "[blex] {Action} failed", ctx.QualifiedName);
    /// </code></example>
    public Action<BlexActionErrorContext>? OnActionError { get; set; }

    /// <summary>
    /// The clock the manager reads time from (<see cref="BlexManager.TimeProvider"/>): action
    /// timestamps, debounced effects and debounced persistence. When unset, a
    /// <see cref="System.TimeProvider"/> registered in the service collection is used, and
    /// <see cref="System.TimeProvider.System"/> otherwise.
    /// </summary>
    public TimeProvider? TimeProvider { get; set; }

    /// <summary>
    /// Whether a store changing outside the manager's bound <see cref="BlexManager.Dispatcher"/> (in
    /// Blazor, the renderer's) is reported through <see cref="OnError"/>. Defaults to <c>true</c>. Wired
    /// onto <see cref="BlexManager.ReportOffDispatcherChanges"/>.
    /// </summary>
    public bool ReportOffDispatcherChanges { get; set; } = true;

    /// <summary>
    /// Development aid: reports an action that changed state in place without assigning a state property
    /// (<c>Items.Add(item)</c>), which nothing would otherwise notice. Serializes stores around every action,
    /// so bind it to the development environment. Defaults to <c>false</c>. Wired onto
    /// <see cref="BlexManager.DetectInPlaceMutations"/>.
    /// </summary>
    public bool DetectInPlaceMutations { get; set; }

    /// <summary>
    /// Convenience: redacts the named keys (recursively, anywhere in the tree -- including inside
    /// arrays and action payloads) from the state sent to DevTools, replacing their values with
    /// <c>"&lt;redacted&gt;"</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Matching is case-insensitive: state slices are keyed by the generated PascalCase property
    /// name (<c>Token</c>) while action payloads use the camelCase parameter name (<c>token</c>),
    /// and a redaction helper that silently missed one of them would fail open.
    /// </para>
    /// <para>
    /// Additive and composing: calling this more than once widens the redaction set rather than
    /// replacing it, and a <see cref="DevToolsStateSanitizer"/> assigned <em>before</em> this call
    /// still runs (redaction is applied to its output). A redaction feature that silently dropped
    /// keys registered by an earlier call would fail open, which is the one thing it must not do.
    /// Assigning <see cref="DevToolsStateSanitizer"/> after this call replaces redaction entirely.
    /// </para>
    /// </remarks>
    public BlexOptions RedactDevToolsKeys(params string[] keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        if (_redactedKeys is not null)
        {
            // The installed sanitizer closes over this set, so widening it is enough.
            foreach (var key in keys)
                _redactedKeys.Add(key);
            return this;
        }

        var set = _redactedKeys = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
        var inner = DevToolsStateSanitizer;
        DevToolsStateSanitizer = inner is null
            ? state => RedactKeys(state, set)
            : state => RedactKeys(inner(state) ?? state, set);
        return this;
    }

    private HashSet<string>? _redactedKeys;

    private static JsonObject RedactKeys(JsonObject source, HashSet<string> keys)
    {
        var clone = (JsonObject)source.DeepClone();
        Walk(clone);
        return clone;

        void Walk(JsonObject obj)
        {
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (keys.Contains(key))
                    obj[key] = "<redacted>";
                else
                    WalkNode(obj[key]);
            }
        }

        // Arrays must be descended too: collection state (entity lists, action args) commonly
        // nests objects inside arrays, and a redaction feature must not leak through them.
        void WalkNode(System.Text.Json.Nodes.JsonNode? node)
        {
            switch (node)
            {
                case JsonObject child:
                    Walk(child);
                    break;
                case System.Text.Json.Nodes.JsonArray array:
                    foreach (var element in array)
                        WalkNode(element);
                    break;
            }
        }
    }

    /// <summary>Registers a middleware type resolved from DI. Runs in registration order.</summary>
    public BlexOptions UseMiddleware<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TMiddleware>() where TMiddleware : class, IBlexMiddleware
    {
        MiddlewareEntries.Add(typeof(TMiddleware));
        return this;
    }

    /// <summary>Registers a pre-built middleware instance. Runs in registration order.</summary>
    /// <remarks>
    /// The instance is stored on the (singleton) options and therefore shared across every DI
    /// scope. Under Blazor Server that means it is shared across all circuits, so avoid holding
    /// per-user state in it. Use <see cref="UseMiddleware{TMiddleware}"/> for scoped, per-circuit
    /// middleware. Middleware registered directly in the service collection, rather than through
    /// these methods, runs after everything registered here.
    /// </remarks>
    public BlexOptions UseMiddleware(IBlexMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        MiddlewareEntries.Add(middleware);
        return this;
    }

    /// <summary>
    /// Logs through the host's <c>ILogger</c> (category <c>Bit.Blex</c>): every action at
    /// <paramref name="actionLevel"/>, every action failure as an error, and every failure Blex isolates from
    /// the pipeline as a warning -- instead of <see cref="Console.Error"/> -- before <see cref="OnError"/>, if
    /// set, sees it too. Runs in registration order, like any middleware.
    /// </summary>
    /// <param name="actionLevel">The level actions are logged at. <c>LogLevel.None</c> logs failures only.</param>
    /// <remarks>
    /// Names only: action arguments and state never reach the log. Without an <c>ILoggerFactory</c> in the
    /// container (a bare <c>ServiceCollection</c>), nothing is logged and <see cref="OnError"/> behaves as before.
    /// </remarks>
    public BlexOptions UseLogging(Microsoft.Extensions.Logging.LogLevel actionLevel = Microsoft.Extensions.Logging.LogLevel.Debug)
    {
        MiddlewareEntries.Add(new BlexLoggingRegistration(actionLevel));
        return this;
    }

    /// <summary>Registers a delegate-based middleware (handy for quick logging).</summary>
    public BlexOptions UseMiddleware(Action<BlexActionContext> handler)
        => UseMiddleware(new BlexDelegateMiddleware(handler));

    /// <summary>
    /// Registers a veto filter that runs before each action. Return <c>false</c> to cancel the
    /// action so its mutation never runs (e.g. guard rails, read-only modes, validation).
    /// </summary>
    public BlexOptions UseFilter(Func<BlexPreActionContext, bool> filter)
        => UseMiddleware(new BlexFilterMiddleware(filter));

    /// <summary>
    /// Registers a veto filter for one parameterless action, named by its generated descriptor
    /// (<c>CartStore.Actions.Checkout</c>). Return <c>false</c> to cancel it; every other action passes.
    /// </summary>
    public BlexOptions UseFilter<TStore>(BlexAction<TStore> action, Func<TStore, bool> filter)
        where TStore : class, IBlexStore
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(filter);
        return UseFilter(ctx => !action.Matches(ctx) || filter((TStore)ctx.Store));
    }

    /// <summary>
    /// Registers a veto filter for one action, named by its generated descriptor
    /// (<c>CounterStore.Actions.Add</c>), that receives the action's payload as the type the action
    /// takes. Return <c>false</c> to cancel it; every other action passes.
    /// </summary>
    /// <example><code>
    /// options.UseFilter(CounterStore.Actions.Add, (counter, amount) => amount is > 0 and &lt;= 100);
    /// </code></example>
    public BlexOptions UseFilter<TStore, TPayload>(BlexAction<TStore, TPayload> action, Func<TStore, TPayload, bool> filter)
        where TStore : class, IBlexStore
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(filter);
        return UseFilter(ctx => !action.Matches(ctx) || filter((TStore)ctx.Store, action.GetPayload(ctx)));
    }
}
