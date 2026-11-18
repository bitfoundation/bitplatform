namespace Bit.Blex;

/// <summary>Whether fetches and mutations wait for the network while the app is offline.</summary>
public enum BlexNetworkMode
{
    /// <summary>
    /// Requests wait while <see cref="BlexQueryClient.IsOnline"/> is <c>false</c> -- paused, not failed -- and go on
    /// once the network returns, rather than spending their retries on requests that cannot succeed.
    /// </summary>
    Online,

    /// <summary>Requests run whatever the network: for data that does not come from it (IndexedDB, a local service).</summary>
    Always,
}
