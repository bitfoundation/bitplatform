using System.Collections.Concurrent;

namespace Bit.Bonboarding;

/// <summary>
/// Keeps progress in memory for the lifetime of the scope. The right choice for tests, for
/// prerendering, and for applications that persist onboarding state server-side themselves.
/// </summary>
public sealed class BonboardingInMemoryStore : IBonboardingStore
{
    private readonly ConcurrentDictionary<string, BonboardingRecord> _records = new(StringComparer.Ordinal);

    public ValueTask<BonboardingRecord?> GetAsync(string tourId, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_records.GetValueOrDefault(tourId));

    public ValueTask SetAsync(BonboardingRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        _records[record.TourId] = record;
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(string tourId, CancellationToken cancellationToken = default)
    {
        _records.TryRemove(tourId, out _);
        return ValueTask.CompletedTask;
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        _records.Clear();
        return ValueTask.CompletedTask;
    }
}
