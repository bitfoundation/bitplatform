using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Brouter.Tests.Harness.Hybrid;

internal sealed class HarnessForm : Form
{
    public HarnessForm(string startPath)
    {
        Text = "Brouter hybrid harness";
        ClientSize = new Size(1280, 720);

        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
        services.AddBrouterHarness();

        var webView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            HostPage = "wwwroot/index.html",
            StartPath = startPath,
            Services = services.BuildServiceProvider(),
        };
        webView.RootComponents.Add<HarnessApp>("#app");
        webView.WebView.CoreWebView2InitializationCompleted += (_, e) =>
        {
            if (e.IsSuccess is false) Program.Fail("WebView2 failed to initialize", e.InitializationException);
        };

        Controls.Add(webView);
    }
}
