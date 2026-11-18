namespace Bit.Blex;

/// <summary>
/// How one observer of a query wants it fetched. Every value left <c>null</c> falls back to
/// <see cref="BlexQueryClientOptions"/>.
/// </summary>
public sealed record BlexQueryOptions
{
    /// <summary>
    /// How long data stays fresh after it was fetched. Fresh data is served from the cache without a
    /// refetch; stale data is served too, but refetched in the background when an observer mounts or
    /// the app regains focus. <see cref="TimeSpan.MaxValue"/> keeps it fresh until invalidated.
    /// </summary>
    public TimeSpan? StaleTime { get; init; }

    /// <summary>How long a query nobody observes any more stays cached before it is removed.</summary>
    public TimeSpan? GcTime { get; init; }

    /// <summary>How many times a failed fetch is retried before the error is kept.</summary>
    public int? RetryCount { get; init; }

    /// <summary>The wait before the first retry; it doubles before each further one, up to 30 seconds.</summary>
    public TimeSpan? RetryDelay { get; init; }

    /// <summary>
    /// Whether the query may fetch at all. A disabled observer still sees whatever is cached; turn it on
    /// once what the fetch depends on is known (a dependent query). Defaults to <c>true</c>.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Refetches on this interval while the observer exists (polling). <c>null</c> turns it off.</summary>
    public TimeSpan? RefetchInterval { get; init; }

    /// <summary>Whether a new observer refetches stale data. Defaults to <c>true</c>.</summary>
    public bool RefetchOnMount { get; init; } = true;

    /// <summary>
    /// Whether stale data is refetched when <see cref="BlexQueryClient.NotifyFocused"/> or
    /// <see cref="BlexQueryClient.NotifyReconnected"/> reports the app is back. Defaults to <c>true</c>.
    /// </summary>
    public bool RefetchOnFocus { get; init; } = true;

    /// <summary>
    /// When the observer moves to another key (the next page, another filter), keeps showing the previous
    /// key's data -- flagged <see cref="BlexQuery{T}.IsPlaceholderData"/> -- until the new key has its own,
    /// instead of falling back to a loading state.
    /// </summary>
    public bool KeepPreviousData { get; init; }

    /// <summary>Whether the fetch waits for the network while the app is offline. <c>null</c> uses the client's.</summary>
    public BlexNetworkMode? NetworkMode { get; init; }
}
