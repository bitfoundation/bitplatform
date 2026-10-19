namespace Bit.Belemetry;

/// <summary>Where undeliverable requests wait for a later retry (only with <see cref="BelemetryFeatures.Persistence"/> consent).</summary>
internal interface IBelemetryRequestStore
{
    ValueTask SaveAsync(BelemetryHttpRequest request, CancellationToken cancellationToken);

    /// <summary>Loads up to <paramref name="max"/> stored requests, oldest first, discarding expired ones.</summary>
    ValueTask<IReadOnlyList<BelemetryHttpRequest>> LoadAsync(int max, CancellationToken cancellationToken);

    ValueTask DeleteAsync(string id, CancellationToken cancellationToken);

    /// <summary>Removes everything stored (consent for persistence was withdrawn).</summary>
    ValueTask ClearAsync();
}
