using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bit.Bonboarding;

/// <summary>
/// The default store: one <c>localStorage</c> entry per tour, with an in-memory mirror so reads
/// still work while prerendering, in private browsing, and when storage is blocked entirely.
/// </summary>
public sealed class BonboardingBrowserStore : IBonboardingStore
{
    private readonly IBonboardingInterop _interop;
    private readonly BonboardingOptions _options;
    private readonly ILogger<BonboardingBrowserStore> _logger;
    private readonly BonboardingInMemoryStore _mirror = new();

    public BonboardingBrowserStore(
        IBonboardingInterop interop,
        IOptions<BonboardingOptions> options,
        ILogger<BonboardingBrowserStore> logger)
    {
        _interop = interop;
        _options = options.Value;
        _logger = logger;
    }

    private string KeyFor(string tourId) => _options.StorageKeyPrefix + tourId;

    public async ValueTask<BonboardingRecord?> GetAsync(string tourId, CancellationToken cancellationToken = default)
    {
        var cached = await _mirror.GetAsync(tourId, cancellationToken).ConfigureAwait(false);
        if (cached is not null) return cached;

        try
        {
            var json = await _interop.GetStorageAsync(KeyFor(tourId), cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(json)) return null;

            var record = JsonSerializer.Deserialize(json, BonboardingJsonContext.Default.BonboardingRecord);
            if (record is not null) await _mirror.SetAsync(record, cancellationToken).ConfigureAwait(false);

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

        await _mirror.SetAsync(record, cancellationToken).ConfigureAwait(false);

        try
        {
            var json = JsonSerializer.Serialize(record, BonboardingJsonContext.Default.BonboardingRecord);
            await _interop.SetStorageAsync(KeyFor(record.TourId), json, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService progress was kept in memory only.");
        }
    }

    public async ValueTask RemoveAsync(string tourId, CancellationToken cancellationToken = default)
    {
        await _mirror.RemoveAsync(tourId, cancellationToken).ConfigureAwait(false);

        try
        {
            await _interop.RemoveStorageAsync(KeyFor(tourId), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not clear storage for tour {TourId}.", tourId);
        }
    }

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        await _mirror.ClearAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Only remove this library's keys: the application owns the rest of localStorage.
            var keys = await _interop.ListStorageKeysAsync(_options.StorageKeyPrefix, cancellationToken)
                .ConfigureAwait(false);

            foreach (var key in keys)
            {
                await _interop.RemoveStorageAsync(key, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not clear stored progress.");
        }
    }
}
