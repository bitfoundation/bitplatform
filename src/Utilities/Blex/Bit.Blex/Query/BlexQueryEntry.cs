using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>
/// One key's cached query: its data, its error, whether a fetch is running and who observes it. All
/// state is read and written under <see cref="BlexQueryClient.Gate"/>; observers are told about a change
/// after the lock is released, and user code (the fetch, observer handlers) never runs under it.
/// </summary>
internal abstract class BlexQueryEntry(BlexQueryClient client, BlexQueryKey key)
{
    public BlexQueryClient Client { get; } = client;

    public BlexQueryKey Key { get; } = key;

    public BlexQueryStatus Status { get; protected set; }

    public BlexFetchStatus FetchStatus { get; protected set; }

    public Exception? Error { get; protected set; }

    public DateTimeOffset? DataUpdatedAt { get; protected set; }

    public int FailureCount { get; protected set; }

    public Exception? FailureReason { get; protected set; }

    public bool IsInvalidated { get; protected set; }

    public abstract bool HasData { get; }

    public abstract Type DataType { get; }

    public abstract int ObserverCount { get; }

    /// <summary>
    /// The longest <see cref="BlexQueryOptions.GcTime"/> any query of this key asked for, or <c>null</c> while none has
    /// (the client's default applies then).
    /// </summary>
    public TimeSpan? GcTime { get; set; }

    /// <summary>The removal scheduled for when nobody observes the entry; guarded by the gate.</summary>
    public ITimer? GcTimer { get; set; }

    /// <summary>
    /// When the GC wait ends, for one longer than a timer can wait (<see cref="BlexQueryClient.MaxTimerDue"/>): the
    /// timer fires early, and the rest is waited from here; <c>null</c> when the timer waits the whole time. Guarded by the gate.
    /// </summary>
    public DateTimeOffset? GcDueAt { get; set; }

    /// <summary>Whether the data is older than <paramref name="staleTime"/>, missing, or invalidated.</summary>
    public bool IsStale(TimeSpan staleTime, DateTimeOffset now)
    {
        if (IsInvalidated || !HasData || DataUpdatedAt is not { } updatedAt)
            return true;

        return staleTime != TimeSpan.MaxValue && now - updatedAt >= staleTime;
    }

    /// <summary>Whether an enabled observer wants it, so an invalidation or a focus refetches it now.</summary>
    public abstract bool IsActive { get; }

    /// <summary>A snapshot of the observers watching the entry.</summary>
    public abstract IReadOnlyList<BlexQuery> Observers();

    /// <summary>
    /// The removal scheduled for this entry, told apart from one scheduled before or after it: a timer that
    /// fires after the entry was touched again finds a different ticket and leaves the entry alone.
    /// </summary>
    public object? GcTicket { get; set; }

    /// <summary>Refetches with the fetch the entry last used, cancelling one already running when asked to.</summary>
    public abstract Task RefetchAsync(bool cancelRunning, Func<BlexQuery, bool>? observerFilter = null);

    /// <summary>How many times the entry was invalidated, so a fetch can tell whether that happened while it ran.</summary>
    protected int Invalidations { get; private set; }

    /// <summary>Marks the data stale whatever its age; the next mount or refetch fetches it again.</summary>
    public void Invalidate()
    {
        lock (Client.Gate)
        {
            IsInvalidated = true;
            Invalidations++;
        }

        Notify();
    }

    /// <summary>
    /// How the fetch a caller waited for -- or the one that superseded it -- ended: succeeded, failed with
    /// <see cref="Error"/>, or neither (cancelled). The error travels with it: the entry's own may be gone by the
    /// time the caller looks.
    /// </summary>
    public readonly record struct FetchResult(bool Succeeded, Exception? Error);

    /// <summary>Cancels the running fetch, if any; the entry returns to idle with what it had before.</summary>
    public abstract void Cancel();

    /// <summary>The data as JSON for the prerender handoff; <c>false</c> when there is no successfully fetched data to hand over.</summary>
    public abstract bool TrySerializeData(out JsonNode? data, out DateTimeOffset updatedAt);

    /// <summary>Seeds the entry from the prerender handoff, unless it already holds newer data.</summary>
    public abstract void Hydrate(JsonNode? data, DateTimeOffset updatedAt);

    /// <summary>Tells every observer the entry changed. Called outside the gate.</summary>
    public abstract void Notify();

    /// <summary>Releases the entry when it leaves the cache: cancels its fetch and its removal timer.</summary>
    public void Evict()
    {
        lock (Client.Gate)
        {
            GcTimer?.Dispose();
            GcTimer = null;
            GcTicket = null;
        }

        Cancel();
    }

    /// <summary>The wait before retry number <paramref name="failures"/> + 1: doubling, capped at 30 seconds (or the first wait, if longer).</summary>
    public static TimeSpan Backoff(TimeSpan first, int failures)
    {
        if (first <= TimeSpan.Zero)
            return TimeSpan.Zero;

        var cap = Math.Max(30_000, first.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(Math.Min(first.TotalMilliseconds * Math.Pow(2, Math.Min(failures, 20)), cap));
    }
}
