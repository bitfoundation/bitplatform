using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>
/// A reactive state container. Implemented by <see cref="BlexStoreBase"/> and the generated stores.
/// </summary>
public interface IBlexStore
{
    /// <summary>Display name of the store (used as the slice key in DevTools state).</summary>
    string Name { get; }

    /// <summary>
    /// Whether this store opts in to automatic persistence (set via <c>[BlexStoreAttribute(Persist = true)]</c>).
    /// Consumed by the persistence coordinator; ignored when no persistence provider is configured.
    /// </summary>
    bool Persist { get; }

    /// <summary>Raised after the store's state changes so views can re-render.</summary>
    event Action? StateChanged;

    /// <summary>Captures the current state as a JSON object for snapshots / DevTools.</summary>
    JsonObject SerializeState();

    /// <summary>
    /// Restores state from a JSON object produced by <see cref="SerializeState"/>.
    /// Used for time-travel and persistence. Does not record a new action.
    /// </summary>
    void DeserializeState(JsonObject state);
}
