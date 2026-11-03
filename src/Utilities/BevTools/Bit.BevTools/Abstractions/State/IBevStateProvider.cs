namespace Bit.BevTools;

/// <summary>
/// Exposes an application state container to the State Inspector. Implement this in your own state class or in an
/// adapter for a state-management library, then register it with <see cref="IBevStateProviderRegistry"/> or as an
/// <c>IBevStateProvider</c> service.
/// </summary>
public interface IBevStateProvider
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
    event Action<BevStateChangeInfo>? Changed;
}

/// <summary>Optional metadata about a state change.</summary>
public sealed record BevStateChangeInfo(string? Action = null, string? Detail = null)
{
    public static readonly BevStateChangeInfo Empty = new();

    /// <summary>
    /// What the provider calls the state right after this change (a version, an action index), for a provider that
    /// implements <see cref="IBevStateRestorer"/>: BevTools offers to restore the change and hands the token back.
    /// </summary>
    public string? RestoreToken { get; init; }

    /// <summary>
    /// The values the change was made with, by name: an action's arguments. BevTools reads them through its inspector, at
    /// the session's value access, when the change is raised, and shows them after <see cref="Detail"/>, which is shown
    /// as it is: put the application's values here, not there.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, object?>>? Arguments { get; init; }
}

/// <summary>
/// A state provider that can put its state back to how it was right after an earlier change: time travel through the
/// provider's own contract. BevTools never writes application state itself - restoring an arbitrary object graph is
/// unsafe - so only a provider that knows how to restore itself (an immutable store keeping its versions, a reducer
/// that replays its actions) implements this, gives each change a <see cref="BevStateChangeInfo.RestoreToken"/>, and
/// decides what restoring means. BevTools offers it only where a forced collection is allowed too: Development, or a
/// process that serves one user.
/// </summary>
public interface IBevStateRestorer
{
    /// <summary>Restores the state identified by <paramref name="token"/>; raise <see cref="IBevStateProvider.Changed"/> afterwards as for any change.</summary>
    ValueTask RestoreAsync(string token, CancellationToken cancellationToken = default);
}

public interface IBevStateProviderRegistry
{
    /// <summary>Registers a provider for the current BevTools session. Dispose the result to unregister.</summary>
    IDisposable Register(IBevStateProvider provider);

    IReadOnlyList<IBevStateProvider> Providers { get; }
}
