namespace Bit.Blex;

/// <summary>The defaults every query of a <see cref="BlexQueryClient"/> starts from.</summary>
public sealed class BlexQueryClientOptions
{
    /// <summary>
    /// How long data stays fresh after it was fetched. Defaults to zero: data is stale at once, so every
    /// new observer refetches it in the background while showing what is cached.
    /// </summary>
    public TimeSpan StaleTime { get; set; } = TimeSpan.Zero;

    /// <summary>How long an unobserved query stays cached. Defaults to five minutes.</summary>
    public TimeSpan GcTime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>How many times a failed fetch is retried. Defaults to <c>3</c>.</summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>The wait before the first retry; it doubles before each further one, up to 30 seconds. Defaults to one second.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Whether fetches and mutations wait for the network while the app is offline. Defaults to
    /// <see cref="BlexNetworkMode.Online"/>: they pause, and go on once it returns.
    /// </summary>
    public BlexNetworkMode NetworkMode { get; set; } = BlexNetworkMode.Online;

    /// <summary>
    /// Passes key invalidations on to the app's other tabs (a <c>BroadcastChannel</c>, through <c>&lt;BlexProvider&gt;</c>),
    /// which invalidate the same keys: a change saved in one tab refetches the lists every other tab shows. Defaults
    /// to <c>false</c>. Only invalidations by key travel (<see cref="BlexQueryClient.InvalidateAsync(BlexQueryKey, bool)"/>,
    /// a mutation's <c>Invalidates</c>), matched by the key's text (<see cref="BlexQueryKey.ToString"/>).
    /// </summary>
    public bool SyncAcrossTabs { get; set; }
}
