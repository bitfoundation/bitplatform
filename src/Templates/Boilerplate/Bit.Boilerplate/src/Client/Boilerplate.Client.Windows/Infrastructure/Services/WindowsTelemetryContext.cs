// [mirror] telemetry context properties - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/Infrastructure/Services/MauiTelemetryContext.cs

namespace Boilerplate.Client.Windows.Infrastructure.Services;

public class WindowsTelemetryContext : AppTelemetryContext
{
    public override string? WebView { get; set; } = $"EdgeWebView2 {Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString()}";
}
