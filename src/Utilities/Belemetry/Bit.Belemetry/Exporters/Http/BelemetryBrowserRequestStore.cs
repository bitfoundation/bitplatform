namespace Bit.Belemetry;

/// <summary>
/// The WebAssembly "store": undeliverable requests are handed to the browser script, which owns retry and the
/// <c>localStorage</c> queue (it can also send them with <c>keepalive</c> during unload, which .NET cannot).
/// </summary>
internal sealed class BelemetryBrowserRequestStore(BelemetryBrowserBridgeRegistry registry) : IBelemetryRequestStore
{
    public async ValueTask SaveAsync(BelemetryHttpRequest request, CancellationToken cancellationToken)
    {
        if (registry.SendRequests is { } send) await send([request]);
    }

    public ValueTask<IReadOnlyList<BelemetryHttpRequest>> LoadAsync(int max, CancellationToken cancellationToken) => ValueTask.FromResult<IReadOnlyList<BelemetryHttpRequest>>([]);

    public ValueTask DeleteAsync(string id, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    // The script clears its own queue on any revoke, and gives up a request handed to it after a revoke it saw first
    // (BelemetryHttpRequest.Features), so nothing of before the revoke is stored there again.
    public ValueTask ClearAsync() => ValueTask.CompletedTask;
}
