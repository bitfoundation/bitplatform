using Microsoft.AspNetCore.Components;

namespace Bit.Belemetry;

/// <summary>
/// Connects the browser script and .NET once the app is interactive: starts the script if <see cref="BelemetryScript"/>
/// has not, synchronizes consent and the user context, enables relaying (Hybrid) and the unload flush (WebAssembly).
/// Place it once in <c>Routes.razor</c> or the main layout. Renders nothing.
/// </summary>
public sealed class BelemetryInitializer : ComponentBase
{
    [Inject] private BelemetryJsBridge Bridge { get; set; } = default!;

    // Resolved here, on the renderer's synchronization context: a server circuit's diagnostic timeline registers under it.
    [Inject] private IBelemetryDiagnostics Diagnostics { get; set; } = default!;

    /// <summary>Raised once the script is connected.</summary>
    [Parameter] public EventCallback OnReady { get; set; }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false) return;

        (Diagnostics as BelemetryDiagnosticsService)?.EnsureRegistered();

        if (await Bridge.StartAsync()) await OnReady.InvokeAsync();
    }
}
