namespace Bit.BevTools;

/// <summary>
/// Exposes an application state container to the State Inspector. Implement this in your own state class or in an
/// adapter for a state-management library, then register it with <see cref="IBevToolsStateProviderRegistry"/> or as an
/// <c>IBevToolsStateProvider</c> service.
/// </summary>
public interface IBevToolsStateProvider
{
    /// <summary>Display name, e.g. "CartState". Unique in a session: a second provider of a name already registered is not shown, and an error says so.</summary>
    string Name { get; }

    /// <summary>Optional description of where the state lives (singleton service, scoped store, ...).</summary>
    string? Description => null;

    /// <summary>
    /// Returns the object to inspect. It is inspected lazily and never mutated. Returning a snapshot copy is not
    /// required; returning the live object is fine because BevTools only reads public members.
    /// </summary>
    object? GetSnapshot();

    /// <summary>Raised when state changed. BevTools records a state-change event and computes a before/after diff.</summary>
    event Action<BevToolsStateChangeInfo>? Changed;
}
