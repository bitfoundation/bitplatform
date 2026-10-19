using System.Text.Json.Nodes;

namespace Bit.Blex.Testing;

/// <summary>A single recorded action and the state snapshot taken immediately after it.</summary>
/// <param name="Store">The store that produced the action.</param>
/// <param name="ActionName">The bare action name (e.g. <c>"Increment"</c>).</param>
/// <param name="QualifiedName">The qualified name (e.g. <c>"counter/Increment"</c>).</param>
/// <param name="Sequence">The monotonic action sequence number.</param>
/// <param name="State">The global state tree right after the action.</param>
/// <param name="Args">The action's arguments (parameter name/value pairs), if any.</param>
public sealed record BlexRecordedAction(
    IBlexStore Store,
    string ActionName,
    string QualifiedName,
    int Sequence,
    JsonObject State,
    IReadOnlyList<BlexActionArg> Args);
