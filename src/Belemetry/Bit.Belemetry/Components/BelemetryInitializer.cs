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

    /// <summary>Raised once the script is connected.</summary>
    [Parameter] public EventCallback OnReady { get; set; }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false) return;

        if (await Bridge.StartAsync()) await OnReady.InvokeAsync();
    }
}
