using Bit.Brouter.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Brouter.Tests.E2E;

/// <summary>
/// Blazor Hybrid: the harness inside a WinForms BlazorWebView (the same WebView core MAUI uses), driven
/// over CDP. What differs from the web: assets come from the WebView's virtual host rather than an HTTP
/// server, there is no prerendering and no response status, and the renderer is "WebView".
/// </summary>
/// <remarks>
/// Windows only. The tests share the app's single window and run in order; each starts by navigating
/// the WebView, which reloads the page and gives the test a fresh DI scope.
/// </remarks>
[TestClass]
public class HybridModeTests : InteractiveHarnessTests
{
    protected override string BaseUrl => HybridHarnessHost.AppOrigin;

    protected override string Framework => "net11.0";

    protected override bool Prerenders => false;

    protected override IReadOnlyList<string> ExpectedRenderers => ["WebView"];

    protected override IReadOnlyList<string> ExpectedPlatforms => ["dotnet"];

    // BlazorWebView hands new-window requests to the operating system's browser.
    protected override bool SupportsNewWindows => false;

    // Closing the page would close the app's only WebView.
    protected override bool SupportsBeforeUnload => false;

    protected override async Task<IPage> OpenPageAsync()
    {
        if (OperatingSystem.IsWindows() is false)
            Assert.Inconclusive("BlazorWebView renders through WebView2, which needs Windows.");

        return (await HybridSession.GetAsync()).Page;
    }

    // The page belongs to the shared app window; the session closes it at the end of the run.
    protected override Task ClosePageAsync() => Task.CompletedTask;

    [TestMethod]
    public async Task StartPath_opens_the_WebView_on_a_deep_route()
    {
        await using var host = await HybridHarnessHost.StartAsync("/items/9");

        await Expect(host.Page.Locator("#status")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 90_000 });
        await Expect(host.Page.Locator("#page-item")).ToHaveAttributeAsync("data-item-id", "9");
        await Expect(host.Page).ToHaveURLAsync(BaseUrl + "/items/9");
    }
}
