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

    /// <summary>Registers a delegate-based middleware (handy for quick logging).</summary>
    public BlexOptions UseMiddleware(Action<BlexActionContext> handler)
        => UseMiddleware(new BlexDelegateMiddleware(handler));

    /// <summary>
    /// Registers a veto filter that runs before each action. Return <c>false</c> to cancel the
    /// action so its mutation never runs (e.g. guard rails, read-only modes, validation).
    /// </summary>
    public BlexOptions UseFilter(Func<BlexPreActionContext, bool> filter)
        => UseMiddleware(new BlexFilterMiddleware(filter));
}
