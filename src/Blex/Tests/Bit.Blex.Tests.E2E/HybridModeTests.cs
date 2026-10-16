using Bit.Blex.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Blex.Tests.E2E;

/// <summary>
/// Blazor Hybrid: the harness inside a WinForms BlazorWebView (the same WebView core MAUI Blazor Hybrid
/// uses), driven over CDP. What differs from the web: assets come from the WebView's virtual host rather
/// than an HTTP server, there is no prerendering, the renderer is "WebView" - and localStorage is the
/// WebView profile's, which outlives a page load.
/// </summary>
/// <remarks>
/// Windows only. The tests share the app's single window and run in order; each starts by clearing the
/// WebView's storage and navigating, which reloads the page and gives the test a fresh DI scope.
/// </remarks>
[TestClass]
public class HybridModeTests : InteractiveHarnessTests
{
    protected override string BaseUrl => HybridHarnessHost.AppOrigin;

    protected override string Framework => "net10.0";

    protected override IReadOnlyList<string> ExpectedRenderers => ["WebView"];

    protected override IReadOnlyList<string> ExpectedPlatforms => ["dotnet"];

    // BlazorWebView hands new-window requests to the operating system's browser.
    protected override bool SupportsSecondPage => false;

    protected override async Task<IPage> OpenPageAsync()
    {
        if (OperatingSystem.IsWindows() is false)
            Assert.Inconclusive("BlazorWebView renders through WebView2, which needs Windows.");

        var page = (await HybridSession.GetAsync()).Page;

        // The previous test's persisted state would otherwise be rehydrated into this one.
        await page.EvaluateAsync("() => { localStorage.clear(); sessionStorage.clear(); }");

        return page;
    }

    // The page belongs to the shared app window; the session closes it at the end of the run.
    protected override Task ClosePageAsync() => Task.CompletedTask;

    /// <summary>
    /// A failed hybrid test is most often the app itself having gone, which the page cannot say - so the
    /// app's own console output goes into the test's.
    /// </summary>
    [TestCleanup]
    public async Task ReportTheAppAsync()
    {
        if (OperatingSystem.IsWindows() is false || TestContext.CurrentTestOutcome == UnitTestOutcome.Passed) return;

        var host = await HybridSession.GetAsync();

        TestContext.WriteLine($"The hybrid app has {(host.HasExited ? "exited" : "not exited")}. Its last output:{Environment.NewLine}{host.RecentOutput}");
    }
}
