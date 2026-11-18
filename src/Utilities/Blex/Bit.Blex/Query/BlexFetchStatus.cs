namespace Bit.Blex;

/// <summary>Whether a fetch is running, independently of <see cref="BlexQueryStatus"/>.</summary>
public enum BlexFetchStatus
{
    /// <summary>No fetch is running.</summary>
    Idle,

    /// <summary>A fetch (or a retry of one) is running.</summary>
    Fetching,

    /// <summary>
    /// A fetch is waiting for the network to come back (<see cref="BlexNetworkMode.Online"/>): it goes on, from where
    /// it was, once <see cref="BlexQueryClient.NotifyReconnected"/> reports the app online again.
    /// </summary>
    Paused,
}
