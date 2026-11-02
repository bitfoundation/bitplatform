using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bit.Balette;

/// <summary>
/// Tracks recents, pins and frecency (frequency + recency) per command, backed by an <see cref="IBaletteHistoryStore"/>.
/// </summary>
/// <remarks>
/// Every change publishes a new snapshot instead of editing the current one, so the palette can read the history on
/// the renderer's thread while a load or a save finishes on another. A store that fails to load or save is logged and
/// otherwise ignored: history is a convenience, and a broken store must not break the palette.
/// </remarks>
public sealed class BaletteHistory
{
    private readonly IBaletteHistoryStore _store;
    private readonly TimeProvider _time;
    private readonly BaletteOptions _options;
    private readonly ILogger _logger;
    private readonly object _gate = new();
    private volatile Dictionary<string, BaletteCommandUsage>? _usage;
    private Task? _loading;

    /// <summary>Creates the history over <paramref name="store"/>.</summary>
    public BaletteHistory(IBaletteHistoryStore store, BaletteOptions options, TimeProvider? time = null)
        : this(store, options, time, null)
    {
    }

    /// <summary>Creates the history over <paramref name="store"/>, logging store failures to <paramref name="logger"/>.</summary>
    public BaletteHistory(IBaletteHistoryStore store, BaletteOptions options, TimeProvider? time, ILogger<BaletteHistory>? logger)
    {
        _store = store;
        _options = options;
        _time = time ?? TimeProvider.System;
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }

    /// <summary>Raised after usage changed (load, record, pin, clear).</summary>
    public event Action? Changed;

    /// <summary>True once the store has been read (or failed to be read).</summary>
    public bool IsLoaded => _usage is not null;

    /// <summary>Loads usage from the store once.</summary>
    public Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_usage is not null)
        {
            return Task.CompletedTask;
        }

        Task? loading;
        lock (_gate)
        {
            loading = _loading;
        }

        if (loading is null)
        {
            // Started outside the lock: a store that answers synchronously raises Changed from inside LoadCoreAsync, and
            // no subscriber should run under it. Two threads racing here both load; only the first result is kept.
            var started = LoadCoreAsync();
            lock (_gate)
            {
                loading = _usage is not null ? Task.CompletedTask : _loading ??= started;
            }
        }

        // The load is shared, so it never takes a caller's token: one caller giving up stops its own wait, not the load
        // every other caller waits for.
        return cancellationToken.CanBeCanceled && !loading.IsCompleted ? loading.WaitAsync(cancellationToken) : loading;
    }

    private async Task LoadCoreAsync()
    {
        IDictionary<string, BaletteCommandUsage>? data = null;
        try
        {
            data = await _store.LoadAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            BaletteLog.HistoryLoadFailed(_logger, ex);
        }

        lock (_gate)
        {
            if (_usage is not null)
            {
                return;
            }

            // A record or pin that waited for the load is applied on top of what was loaded (see MutateAsync).
            _usage = Sanitize(data);
        }

        Changed?.Invoke();
    }

    /// <summary>Drops what a hand-edited, corrupt or older store can hold and the history cannot use.</summary>
    private static Dictionary<string, BaletteCommandUsage> Sanitize(IDictionary<string, BaletteCommandUsage>? data)
    {
        var usage = new Dictionary<string, BaletteCommandUsage>(StringComparer.Ordinal);
        if (data is null)
        {
            return usage;
        }

        foreach (var (id, u) in data)
        {
            if (string.IsNullOrWhiteSpace(id) || u is null)
            {
                continue;
            }

            usage[id] = u.Count < 0 ? u with { Count = 0 } : u;
        }

        return usage;
    }

    /// <summary>The usage of one command, or null when it was never used or pinned.</summary>
    public BaletteCommandUsage? Get(string commandId) => _usage is { } usage && usage.TryGetValue(commandId, out var u) ? u : null;

    /// <summary>Whether the command is pinned.</summary>
    public bool IsPinned(string commandId) => Get(commandId)?.Pinned ?? false;

    /// <summary>Frecency score: higher for commands used often and recently. 0 when never used.</summary>
    public double Frecency(string commandId)
    {
        var u = Get(commandId);
        return u is null ? 0 : ComputeFrecency(u, _time.GetUtcNow());
    }

    /// <summary>Weighted frecency similar to Firefox's algorithm: capped count × recency weight.</summary>
    public static double ComputeFrecency(BaletteCommandUsage usage, DateTimeOffset now)
    {
        var age = now - usage.LastUsed;
        var weight = age.TotalHours switch
        {
            < 4 => 100,
            < 24 => 70,
            < 72 => 50,
            < 24 * 7 => 30,
            < 24 * 30 => 10,
            _ => 5,
        };

        var cappedCount = Math.Clamp(usage.Count, 0, 20);
        return cappedCount * weight / 10.0;
    }

    /// <summary>Most recently used command ids, newest first, excluding pinned (which are listed separately).</summary>
    public IReadOnlyList<string> Recents(int max)
    {
        if (_usage is not { } usage)
        {
            return [];
        }

        return usage
            .Where(kv => !kv.Value.Pinned && kv.Value.Count > 0)
            .OrderByDescending(kv => kv.Value.LastUsed)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Take(Math.Max(0, max))
            .Select(kv => kv.Key)
            .ToList();
    }

    /// <summary>The pinned command ids, in the order they were pinned.</summary>
    public IReadOnlyList<string> Pinned()
    {
        if (_usage is not { } usage)
        {
            return [];
        }

        return usage
            .Where(kv => kv.Value.Pinned)
            .OrderBy(kv => kv.Value.PinnedAt ?? DateTimeOffset.MinValue)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => kv.Key)
            .ToList();
    }

    /// <summary>Counts one more use of the command, now.</summary>
    public Task RecordAsync(string commandId, CancellationToken cancellationToken = default)
        => MutateAsync(usage =>
        {
            var now = _time.GetUtcNow();
            usage[commandId] = usage.TryGetValue(commandId, out var u)
                ? u with { Count = u.Count + 1, LastUsed = now }
                : new BaletteCommandUsage(1, now, false);
            Prune(usage);
            return true;
        }, cancellationToken);

    /// <summary>Pins the command, or unpins it when it is pinned.</summary>
    public Task TogglePinAsync(string commandId, CancellationToken cancellationToken = default)
        => MutateAsync(usage =>
        {
            var now = _time.GetUtcNow();
            usage[commandId] = usage.TryGetValue(commandId, out var u)
                ? u with { Pinned = !u.Pinned, PinnedAt = u.Pinned ? null : now }
                : new BaletteCommandUsage(0, now, true) { PinnedAt = now };
            return true;
        }, cancellationToken);

    /// <summary>Forgets the command: its uses and its pin.</summary>
    public Task RemoveAsync(string commandId, CancellationToken cancellationToken = default)
        => MutateAsync(usage => usage.Remove(commandId), cancellationToken);

    /// <summary>Forgets every use, keeping the pins.</summary>
    public Task ClearAsync(CancellationToken cancellationToken = default)
        => MutateAsync(usage =>
        {
            foreach (var (id, u) in usage.ToList())
            {
                if (u.Pinned)
                {
                    usage[id] = u with { Count = 0 };
                }
                else
                {
                    usage.Remove(id);
                }
            }

            return true;
        }, cancellationToken);

    /// <summary>Applies <paramref name="change"/> to a copy of the current usage, publishes the copy and saves it.</summary>
    private async Task MutateAsync(Func<Dictionary<string, BaletteCommandUsage>, bool> change, CancellationToken cancellationToken)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        Dictionary<string, BaletteCommandUsage> next;
        lock (_gate)
        {
            next = new Dictionary<string, BaletteCommandUsage>(_usage!, StringComparer.Ordinal);
            if (!change(next))
            {
                return;
            }

            _usage = next;
        }

        Changed?.Invoke();

        try
        {
            await _store.SaveAsync(next, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            BaletteLog.HistorySaveFailed(_logger, ex);
        }
    }

    private void Prune(Dictionary<string, BaletteCommandUsage> usage)
    {
        var max = Math.Max(0, _options.MaxHistoryEntries);
        if (usage.Count <= max)
        {
            return;
        }

        foreach (var key in usage.Where(kv => !kv.Value.Pinned).OrderBy(kv => kv.Value.LastUsed).Take(usage.Count - max).Select(kv => kv.Key).ToList())
        {
            usage.Remove(key);
        }
    }
}
