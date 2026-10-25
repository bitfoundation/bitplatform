using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;

namespace Bit.Bdnd.Tests.Harness.Hybrid;

internal sealed class HarnessForm : Form
{
    public HarnessForm(string? remoteDebuggingPort, string? userDataFolder)
    {
        Text = "Bdnd hybrid harness";
        ClientSize = new Size(1280, 800);

        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
        services.AddBitBdndServices();

        var webView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            HostPage = "wwwroot/index.html",
            Services = services.BuildServiceProvider(),
        };
        webView.RootComponents.Add<HybridCheck>("#app");

        // The debugging port goes through the WebView2 API: since WebView2 Runtime 150 an elevated host (CI runners
        // run as admin) ignores --remote-debugging-port in WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS. A user data folder
        // of its own keeps a second instance from sharing this one's browser process (and its port).
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
