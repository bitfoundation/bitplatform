using Bit.Belemetry.Samples.Core;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Belemetry.Samples.WinForms;

internal sealed class MainForm : Form
{
    private readonly IServiceProvider _services;

    public MainForm(IServiceProvider services)
    {
        _services = services;
        Text = "Belemetry samples (Windows Forms)";
        ClientSize = new Size(1024, 720);

        var webView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            HostPage = "wwwroot/index.html",
            Services = services,
        };
        webView.RootComponents.Add<Routes>("#app");
        Controls.Add(webView);
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel) return;

        try
        {
            // The app process is the exporter in Hybrid: flush what is queued before the window goes away, as a MAUI
            // app does in OnSleep. IBelemetry is scoped; the queue it flushes is the app-wide pipeline.
            await using var scope = _services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IBelemetry>().FlushAsync();
        }
        catch (Exception ex)
        {
            // async void: an exception here would take the app down on its way out.
            System.Diagnostics.Debug.WriteLine($"Flushing telemetry failed: {ex}");
        }
    }
}
