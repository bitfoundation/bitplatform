namespace Bit.BevTools;

/// <summary>
/// Exposes an application state container to the State Inspector. Implement this in your own state class or in an
/// adapter for a state-management library, then register it with <see cref="IBevStateProviderRegistry"/> or as an
/// <c>IBevStateProvider</c> service.
/// </summary>
public interface IBevStateProvider
{
    /// <summary>Display name, e.g. "CartState".</summary>
    string Name { get; }

    /// <summary>Optional description of where the state lives (singleton service, scoped store, ...).</summary>
    string? Description => null;

    /// <summary>
    /// Returns the object to inspect. It is inspected lazily and never mutated. Returning a snapshot copy is not
    /// required; returning the live object is fine because BevTools only reads public members.
    /// </summary>
    object? GetSnapshot();

    /// <summary>Raised when state changed. BevTools records a state-change event and computes a before/after diff.</summary>
    event Action<BevStateChangeInfo>? Changed;
}

/// <summary>Optional metadata about a state change.</summary>
public sealed record BevStateChangeInfo(string? Action = null, string? Detail = null)
{
    public static readonly BevStateChangeInfo Empty = new();
}

public interface IBevStateProviderRegistry
{
    /// <summary>Registers a provider for the current BevTools session. Dispose the result to unregister.</summary>
    IDisposable Register(IBevStateProvider provider);

    IReadOnlyList<IBevStateProvider> Providers { get; }
}
