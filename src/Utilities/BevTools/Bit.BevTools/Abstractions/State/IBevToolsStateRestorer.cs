namespace Bit.BevTools;

/// <summary>
/// A state provider that can put its state back to how it was right after an earlier change: time travel through the
/// provider's own contract. BevTools never writes application state itself - restoring an arbitrary object graph is
/// unsafe - so only a provider that knows how to restore itself (an immutable store keeping its versions, a reducer
/// that replays its actions) implements this, gives each change a <see cref="BevToolsStateChangeInfo.RestoreToken"/>, and
/// decides what restoring means. BevTools offers it only where a forced collection is allowed too: Development, or a
/// process that serves one user.
/// </summary>
public interface IBevToolsStateRestorer
{
    /// <summary>Restores the state identified by <paramref name="token"/>; raise <see cref="IBevToolsStateProvider.Changed"/> afterwards as for any change.</summary>
    ValueTask RestoreAsync(string token, CancellationToken cancellationToken = default);
}
