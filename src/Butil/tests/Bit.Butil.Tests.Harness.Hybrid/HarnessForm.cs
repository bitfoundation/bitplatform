using Bit.Butil.Samples.Core;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;

namespace ButilTests.Harness.Hybrid;

internal sealed class HarnessForm : Form
{
    public HarnessForm(string hostPage, string? remoteDebuggingPort, string? userDataFolder)
    {
        Text = "Butil hybrid harness";
        ClientSize = new Size(1280, 720);

        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
        services.AddCoreServices();

        var webView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            HostPage = hostPage,
            Services = services.BuildServiceProvider(),
        };
        webView.RootComponents.Add<Routes>("#app");
        webView.RootComponents.Add<HeadOutlet>("head::after");

        // The debugging port goes through the WebView2 API, not WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS: since
        // Runtime 150 an elevated host (a CI runner's admin session) ignores the environment variable, and the
        // port would silently never open.
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

            System.Console.Error.WriteLine($"WebView2 initialized, browser process {webView.WebView.CoreWebView2.BrowserProcessId}");
        };

        Controls.Add(webView);
    }
}
