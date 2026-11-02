using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bit.Bonboarding;

/// <summary>
/// The default store: one <c>localStorage</c> (or <c>sessionStorage</c>) entry per tour, with an in-memory mirror so
/// reads still work while prerendering, in private browsing, and when storage is blocked entirely.
/// </summary>
/// <remarks>
/// With <see cref="BonboardingOptions.StorageScope"/> every key carries who the progress belongs to, so users sharing
/// a device never see each other's. Changes another tab makes are picked up through the <c>storage</c> event, which
/// drops the mirrored copy and raises <see cref="Changed"/>.
/// </remarks>
public sealed class BonboardingBrowserStore : IBonboardingStore, IBonboardingStoreEvents
{
    private readonly IBonboardingInterop _interop;
    private readonly BonboardingOptions _options;
    private readonly ILogger<BonboardingBrowserStore> _logger;
    private readonly IServiceProvider? _services;

    /// <summary>The records read or written in this scope, by full storage key (prefix, scope and tour id).</summary>
    private readonly ConcurrentDictionary<string, BonboardingRecord> _mirror = new(StringComparer.Ordinal);

    private int _watching;

    public BonboardingBrowserStore(
        IBonboardingInterop interop,
        IOptions<BonboardingOptions> options,
        ILogger<BonboardingBrowserStore> logger)
        : this(interop, options, logger, services: null)
    {
    }

    /// <summary>The constructor dependency injection uses: <paramref name="services"/> feeds <see cref="BonboardingOptions.StorageScope"/>.</summary>
    public BonboardingBrowserStore(
        IBonboardingInterop interop,
        IOptions<BonboardingOptions> options,
        ILogger<BonboardingBrowserStore> logger,
        IServiceProvider? services)
    {
        _interop = interop;
        _options = options.Value;
        _logger = logger;
        _services = services;
    }

    public event EventHandler<BonboardingStoreChangedEventArgs>? Changed;

    /// <summary>The key prefix of the records that belong to whoever <see cref="BonboardingOptions.StorageScope"/> names now.</summary>
    private async ValueTask<string> ScopedPrefixAsync(CancellationToken cancellationToken)
    {
        if (_options.StorageScope is not { } scope || _services is null) return _options.StorageKeyPrefix;

        string? owner = null;
        try
        {
            owner = await scope(_services, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Not knowing whose progress it is must not lose it for good: it is kept for the device as a whole.
            _logger.LogWarning(ex, "BonboardingService could not work out whose progress to use; using the device's.");
        }

        // "@{owner}/": the owner is escaped, so it holds no "/" and one user's id is never the start of another's
        // ("1" and "12"); the "@" tells a scoped key from the device's own keys.
        return string.IsNullOrEmpty(owner) ? _options.StorageKeyPrefix : $"{_options.StorageKeyPrefix}@{Uri.EscapeDataString(owner)}/";
    }

    /// <summary>True for a key that belongs to a user (written with a scope) rather than to the device.</summary>
    private bool IsScopedKey(string key)
    {
        var start = _options.StorageKeyPrefix.Length;
        return key.Length > start + 1 && key[start] == '@' && key.IndexOf('/', start) > start;
    }

    public async ValueTask<BonboardingRecord?> GetAsync(string tourId, CancellationToken cancellationToken = default)
    {
        await WatchAsync(cancellationToken).ConfigureAwait(false);

        var key = await ScopedPrefixAsync(cancellationToken).ConfigureAwait(false) + tourId;
        if (_mirror.TryGetValue(key, out var cached)) return cached;

        try
        {
            var json = await _interop.GetStorageAsync(key, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(json)) return null;

            var record = JsonSerializer.Deserialize(json, BonboardingJsonContext.Default.BonboardingRecord);
            if (record is not null) _mirror[key] = record;

            return record;
        }
        catch (JsonException ex)
        {
            // Corrupt or hand-edited storage should behave as "never seen this tour", not as a crash.
            _logger.LogWarning(ex, "Discarding unreadable onboarding record for tour {TourId}.", tourId);
            await RemoveAsync(tourId, cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService storage is unavailable; falling back to memory.");
            return null;
        }
    }

    public async ValueTask SetAsync(BonboardingRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var key = await ScopedPrefixAsync(cancellationToken).ConfigureAwait(false) + record.TourId;
        _mirror[key] = record;

        try
        {
            var json = JsonSerializer.Serialize(record, BonboardingJsonContext.Default.BonboardingRecord);
            await _interop.SetStorageAsync(key, json, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService progress was kept in memory only.");
        }
    }

    public async ValueTask RemoveAsync(string tourId, CancellationToken cancellationToken = default)
    {
        var key = await ScopedPrefixAsync(cancellationToken).ConfigureAwait(false) + tourId;
        _mirror.TryRemove(key, out _);

        try
        {
            await _interop.RemoveStorageAsync(key, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not clear storage for tour {TourId}.", tourId);
        }
    }

    /// <summary>Forgets every record of whoever <see cref="BonboardingOptions.StorageScope"/> names now (the device's when nobody).</summary>
    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        var prefix = await ScopedPrefixAsync(cancellationToken).ConfigureAwait(false);
        var scoped = prefix != _options.StorageKeyPrefix;

        foreach (var key in _mirror.Keys)
        {
            if (Owns(key)) _mirror.TryRemove(key, out _);
        }

        try
        {
            // Only remove this library's keys: the application owns the rest of the storage.
            var keys = await _interop.ListStorageKeysAsync(prefix, cancellationToken).ConfigureAwait(false);

            foreach (var key in keys)
            {
                if (Owns(key)) await _interop.RemoveStorageAsync(key, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not clear stored progress.");
        }

        // The device's records are the ones without a scope; another user's records are never theirs to clear.
        bool Owns(string key) => key.StartsWith(prefix, StringComparison.Ordinal) && (scoped || !IsScopedKey(key));
    }

    /// <summary>Starts listening to other tabs, once, on first use.</summary>
    private async ValueTask WatchAsync(CancellationToken cancellationToken)
    {
        // Session storage is not shared between tabs, so there is nothing to keep in step with.
        if (_options.StorageLocation != BonboardingStorageLocation.Local || Interlocked.Exchange(ref _watching, 1) == 1) return;

        try
        {
            await _interop.WatchStorageAsync(_options.StorageKeyPrefix, OnExternalChangeAsync, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            // Prerendering, or a closing circuit: try again next time.
            Volatile.Write(ref _watching, 0);
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not watch storage for changes made in other tabs.");
        }
    }

    private async ValueTask OnExternalChangeAsync(string? key)
    {
        if (key is null)
        {
            _mirror.Clear();
            RaiseChanged(null);
            return;
        }

        _mirror.TryRemove(key, out _);

        // Only the records of whoever this scope is for are news here.
        var prefix = await ScopedPrefixAsync(CancellationToken.None).ConfigureAwait(false);
        if (!key.StartsWith(prefix, StringComparison.Ordinal)) return;

        // Unscoped here, a user's record changing in another tab is not this scope's business.
        if (prefix == _options.StorageKeyPrefix && IsScopedKey(key)) return;

        RaiseChanged(key[prefix.Length..]);
    }

    private void RaiseChanged(string? tourId)
    {
        try
        {
            Changed?.Invoke(this, new BonboardingStoreChangedEventArgs(tourId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An onboarding store change handler threw.");
        }
    }
}
