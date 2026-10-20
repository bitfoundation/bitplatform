using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;

namespace Bit.Belemetry.Tests.Harness.Hybrid;

internal sealed class HarnessForm : Form
{
    public HarnessForm(string startPath, string collectorOrigin, string? remoteDebuggingPort, string? userDataFolder)
    {
        Text = "Belemetry hybrid harness";
        ClientSize = new Size(1280, 720);

        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
        services.AddLogging();
        // No web host is registered, so Belemetry detects Hybrid and defaults to the Relay transport: the WebView's
        // records reach the collector from this process, over .NET's HttpClient.
        services.AddBelemetryHarness(collectorOrigin);

        var webView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            HostPage = "wwwroot/index.html",
            StartPath = startPath,
            Services = services.BuildServiceProvider(),
        };
        webView.RootComponents.Add<HarnessApp>("#app");

        // The debugging port goes through the WebView2 API, not WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS: since Runtime
        // 150 an elevated host (a CI runner's admin session) ignores the environment variable.
        webView.BlazorWebViewInitializing += (_, e) =>
        {
            if (remoteDebuggingPort is not null)
                e.EnvironmentOptions = new CoreWebView2EnvironmentOptions { AdditionalBrowserArguments = $"--remote-debugging-port={remoteDebuggingPort}" };
            if (userDataFolder is not null)
                e.UserDataFolder = userDataFolder;
        };
        webView.WebView.CoreWebView2InitializationCompleted += (_, e) =>
        {
            if (e.IsSuccess is false) Program.Fail("WebView2 failed to initialize", e.InitializationException);

            Console.Error.WriteLine($"WebView2 initialized, browser process {webView.WebView.CoreWebView2.BrowserProcessId}");
        };

        Controls.Add(webView);
    }
}
