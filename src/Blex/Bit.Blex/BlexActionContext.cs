using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>
/// Context passed to middleware after an action has mutated a store.
/// </summary>
public sealed class BlexActionContext
{
    private readonly Func<JsonObject>? _capture;
    private JsonObject? _globalState;

    internal BlexActionContext(IBlexStore store, string actionName, Func<JsonObject> captureGlobalState, int sequence, IReadOnlyList<BlexActionArg>? args)
    {
        Store = store;
        ActionName = actionName;
        _capture = captureGlobalState;
        Sequence = sequence;
        Args = args ?? [];
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>The store that produced the action.</summary>
    public IBlexStore Store { get; }

    /// <summary>The action name (e.g. <c>"Increment"</c> or <c>"Set Count"</c>).</summary>
    public string ActionName { get; }

    /// <summary>
    /// Snapshot of the whole application state immediately after the action. Captured lazily on
    /// first access (serializing every store is expensive, and many observers -- persistence, for
    /// example -- never need the full tree). Access it synchronously inside your handler; reading
    /// it after later actions have run would capture their state instead. Treat the returned tree
    /// as read-only: the same instance is shared by every observer of this action.
    /// </summary>
    public JsonObject GlobalState => _globalState ??= _capture!();

    /// <summary>Monotonically increasing action sequence number.</summary>
    public int Sequence { get; }

    /// <summary>
    /// The action's arguments (parameter name/value pairs), captured by the generated wrappers.
    /// Empty for parameterless actions. A standalone <c>Set X</c> carries the assigned value.
    /// </summary>
    public IReadOnlyList<BlexActionArg> Args { get; }

    /// <summary>When the action completed (UTC).</summary>
    public DateTimeOffset Timestamp { get; }

    private string? _qualifiedName;

    /// <summary>Fully-qualified action label including the originating store.</summary>
    public string QualifiedName => _qualifiedName ??= $"{Store.Name}/{ActionName}";
}
