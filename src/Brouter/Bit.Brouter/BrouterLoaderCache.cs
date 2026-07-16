namespace Bit.Brouter;

/// <summary>
/// The scoped stale-while-revalidate store for route <see cref="Broute.Loader"/> results (see
/// <see cref="Broute.StaleTime"/> / <see cref="BrouterOptions.DefaultLoaderStaleTime"/>), also fed
/// by link preloading. Keys combine the route's full template with the URL's path+query, so every
/// chain node caches independently per concrete URL. Entries expire fully after
/// <see cref="BrouterOptions.LoaderCacheGcTime"/> and the store is capped at
/// <see cref="BrouterOptions.MaxLoaderCacheEntries"/> (oldest-written evicted first).
/// </summary>
/// <remarks>
/// Owned by the scoped <see cref="BrouterService"/>, so Blazor Server circuits are isolated from
/// each other for free. All access happens on the renderer's single dispatcher (navigation pipeline,
/// revalidation, preload commits are all dispatched there), mirroring the threading discipline of
/// the rest of the router - no locking needed.
/// </remarks>
internal sealed class BrouterLoaderCache
{
    internal sealed class Entry
    {
        public object? Value;
        public DateTime WrittenUtc;
        // True when the entry was produced by a preload rather than a committed navigation/revalidate.
        // Preload entries are readable even by routes with no StaleTime configured, using
        // BrouterOptions.PreloadStaleTime as their freshness window.
        public bool FromPreload;
    }

    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    internal static string MakeKey(string fullTemplate, BrouterLocation to) =>
        $"{fullTemplate}|{to.Path}|{to.Query}";

    /// <summary>
    /// Looks up a cached loader result. <paramref name="staleTime"/> is the route's effective
    /// freshness window (null when the route doesn't cache - only preload-produced entries are
    /// then eligible, judged against <paramref name="preloadStaleTime"/>). A hit older than
    /// <paramref name="gcTime"/> is dropped and reported as a miss.
    /// </summary>
    public bool TryGet(string key, TimeSpan? staleTime, TimeSpan preloadStaleTime, TimeSpan gcTime,
                       out object? value, out bool isStale)
    {
        value = null;
        isStale = false;

        if (_entries.TryGetValue(key, out var entry) is false) return false;

        var age = DateTime.UtcNow - entry.WrittenUtc;
        if (age > gcTime)
        {
            _entries.Remove(key);
            return false;
        }

        var window = staleTime ?? (entry.FromPreload ? preloadStaleTime : (TimeSpan?)null);
        if (window is null) return false;

        value = entry.Value;
        isStale = age > window.Value;
        return true;
    }

    /// <summary>Stores/refreshes a loader result. Re-writing an existing key refreshes its timestamp.</summary>
    public void Set(string key, object? value, int maxEntries, bool fromPreload = false)
    {
        // Delete-then-add keeps insertion order aligned with write recency, so the eviction scan
        // below (oldest WrittenUtc) stays cheap in the common case and correct always.
        _entries.Remove(key);

        while (_entries.Count >= maxEntries && _entries.Count > 0)
        {
            // Evict the oldest-written entry. Linear scan is fine at the default cap (50).
            string? oldestKey = null;
            var oldestTime = DateTime.MaxValue;
            foreach (var kv in _entries)
            {
                if (kv.Value.WrittenUtc < oldestTime)
                {
                    oldestTime = kv.Value.WrittenUtc;
                    oldestKey = kv.Key;
                }
            }
            if (oldestKey is null) break;
            _entries.Remove(oldestKey);
        }

        _entries[key] = new Entry { Value = value, WrittenUtc = DateTime.UtcNow, FromPreload = fromPreload };
    }

    public void Clear() => _entries.Clear();

    internal int Count => _entries.Count;
}
