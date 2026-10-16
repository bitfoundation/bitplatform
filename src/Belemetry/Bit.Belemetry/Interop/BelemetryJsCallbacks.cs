using System.Text.Json;
using Microsoft.JSInterop;

namespace Bit.Belemetry;

/// <summary>
/// The small relay object handed to JavaScript. Kept separate from <see cref="BelemetryJsBridge"/> so a
/// <c>DotNetObjectReference</c> exposes exactly these three entry points and nothing else. Payloads cross as
/// <see cref="JsonElement"/> and are read with the source-generated context, keeping interop trimming-safe.
/// </summary>
internal sealed class BelemetryJsCallbacks(BelemetryJsBridge bridge)
{
    [JSInvokable("Belemetry.Relay")]
    public Task Relay(JsonElement batch) => bridge.RelayAsync(batch);

    [JSInvokable("Belemetry.ContextChanged")]
    public void ContextChanged(JsonElement context)
    {
        try
        {
            if (context.Deserialize(BelemetryJsonContext.Default.BelemetryJsContext) is { } value) bridge.Apply(value);
        }
        catch (JsonException)
        {
            // A malformed context is ignored; the next change resends it whole.
        }
    }

    /// <summary>Synchronous on purpose: the page is unloading, and only a synchronous WebAssembly call completes in time.</summary>
    [JSInvokable("Belemetry.DrainForUnload")]
    public JsonElement DrainForUnload() => bridge.DrainForUnload();
}
