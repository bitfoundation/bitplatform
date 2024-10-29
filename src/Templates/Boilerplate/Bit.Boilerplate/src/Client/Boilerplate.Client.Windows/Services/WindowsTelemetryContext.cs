namespace Boilerplate.Client.Windows.Services;

public class WindowsTelemetryContext : AppTelemetryContext
{
    public override string? WebView { get; set; } = $"EdgeWebView2 {Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString()}";
}
