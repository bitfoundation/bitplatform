namespace Bit.Balette;

/// <summary>
/// Tracks recents, pins and frecency (frequency + recency) per command, backed by an <see cref="IBaletteHistoryStore"/>.
/// </summary>
public sealed class BaletteHistory
{
    private readonly IBaletteHistoryStore _store;
    private readonly TimeProvider _time;
    private readonly BaletteOptions _options;
    private Dictionary<string, BaletteCommandUsage>? _usage;
    private Task? _loading;

    public BaletteHistory(IBaletteHistoryStore store, BaletteOptions options, TimeProvider? time = null)
    {
        _store = store;
        _options = options;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>Raised after usage changed (record, pin, clear).</summary>
    public event Action? Changed;

    public bool IsLoaded => _usage is not null;

    /// <summary>Loads usage from the store once.</summary>
    public Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_usage is not null)
        {
            return Task.CompletedTask;
        }

        return _loading ??= LoadCoreAsync(cancellationToken);
    }

    private async Task LoadCoreAsync(CancellationToken cancellationToken)
    {
        var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        _usage = new Dictionary<string, BaletteCommandUsage>(data, StringComparer.Ordinal);
        Changed?.Invoke();
    }

    public BaletteCommandUsage? Get(string commandId) => _usage is not null && _usage.TryGetValue(commandId, out var u) ? u : null;

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

        var cappedCount = Math.Min(usage.Count, 20);
        return cappedCount * weight / 10.0;
    }

    /// <summary>Most recently used command ids, newest first, excluding pinned (which are listed separately).</summary>
    public IReadOnlyList<string> Recents(int max)
    {
        if (_usage is null)
        {
            return [];
        }

        return _usage
            .Where(kv => !kv.Value.Pinned)
            .OrderByDescending(kv => kv.Value.LastUsed)
            .Take(max)
            .Select(kv => kv.Key)
            .ToList();
    }

    public IReadOnlyList<string> Pinned()
    {
        if (_usage is null)
        {
            return [];
        }

        return _usage.Where(kv => kv.Value.Pinned).OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key).ToList();
    }

    public async Task RecordAsync(string commandId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        var now = _time.GetUtcNow();
        _usage![commandId] = _usage.TryGetValue(commandId, out var u)
            ? u with { Count = u.Count + 1, LastUsed = now }
            : new BaletteCommandUsage(1, now, false);

        Prune();
        Changed?.Invoke();
        await _store.SaveAsync(_usage, cancellationToken).ConfigureAwait(false);
    }

    public async Task TogglePinAsync(string commandId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        var now = _time.GetUtcNow();
        _usage![commandId] = _usage.TryGetValue(commandId, out var u)
            ? u with { Pinned = !u.Pinned }
            : new BaletteCommandUsage(0, now, true);
        Changed?.Invoke();
        await _store.SaveAsync(_usage, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string commandId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        if (_usage!.Remove(commandId))
        {
            Changed?.Invoke();
            await _store.SaveAsync(_usage, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        var pinned = _usage!.Where(kv => kv.Value.Pinned).ToDictionary(kv => kv.Key, kv => kv.Value with { Count = 0 }, StringComparer.Ordinal);
        _usage = pinned;
        Changed?.Invoke();
        await _store.SaveAsync(_usage, cancellationToken).ConfigureAwait(false);
    }

    private void Prune()
    {
        if (_usage is null || _usage.Count <= _options.MaxHistoryEntries)
        {
            return;
        }

        foreach (var key in _usage.Where(kv => !kv.Value.Pinned).OrderBy(kv => kv.Value.LastUsed).Take(_usage.Count - _options.MaxHistoryEntries).Select(kv => kv.Key).ToList())
        {
            _usage.Remove(key);
        }
    }
}
