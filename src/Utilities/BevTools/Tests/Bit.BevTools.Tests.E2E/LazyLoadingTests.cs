using System.Text.RegularExpressions;
using Bit.BevTools.Tests.E2E.Infrastructure;
using Bit.Utilities.Testing;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.BevTools.Tests.E2E;

/// <summary>
/// An app that opens BevTools on demand and lazy loads it on WebAssembly, set up as the README's "Opening it on demand,
/// and lazy loading" section says: the lazy loading sample (<c>Samples/Lazy</c>), published - trimmed, with
/// <c>Bit.BevTools.wasm</c> marked lazy, <c>AddBitBevToolsLoaderServices</c> instead of <c>AddBitBevToolsServices</c>
/// in the browser, <c>&lt;BevToolsLoaderPanel /&gt;</c> in the layout - and driven in a browser, in the Production
/// environment, where BevTools waits to be attached on demand. Its pages never download <c>Bit.BevTools.wasm</c>; the
/// app's own "Open dev tools" button has <c>BevToolsLoader.OpenAsync</c> download it once, attach it to the running app
/// and open its panel, which then shows the app. The server prerenders the layout, and in a process that serves many
/// users the loader offers no button outside Development. Published with AOT (<c>BEVTOOLS_E2E_LAZY_AOT=1</c>), the sample
/// leaves the lazy mark out as the README says an AOT build must, and the same tests check that the loader then takes
/// Bit.BevTools from the assemblies the app started with, downloading nothing on demand.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : BevToolsTest
{
    private LazyLoadingSample? _sample;

    protected override string BaseUrl => _sample!.BaseUrl;

    [TestInitialize]
    public async Task StartSampleAsync()
    {
        try
        {
            _sample = await LazyLoadingSample.GetAsync("Bit.BevTools", "BEVTOOLS", E2EEnvironment.Configuration);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or TimeoutException)
        {
            var reason = $"the lazy loading sample could not be started in the {E2EEnvironment.Configuration} configuration: {ex.Message}";
            // A build server must not report a suite that never ran as anything but a failure.
            if (E2EEnvironment.IsContinuousIntegration) Assert.Fail("The browser tests could not run: " + reason);
            Assert.Inconclusive("The browser tests could not run: " + reason);
        }
    }

    [TestMethod]
    public async Task The_app_runs_without_Bit_BevTools_until_Open_dev_tools_downloads_it_once_and_attaches_it()
    {
        var page = await NewPageAsync();
        var requests = _sample!.Track(page);

        await GotoAsync(page, "/");
        await WaitForInteractiveAsync(page);
        await Expect(OpenDevTools(page)).ToBeVisibleAsync();

        // The app works without it: another page, a click that renders, and back.
        await GoToCounterAndClickAsync(page, expectedCount: 1);
        await page.GetByRole(AriaRole.Link, new() { Name = "Home", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Lazy loading");

        _sample.AssertNotLoaded(requests);
        await Expect(Panel(page)).ToHaveCountAsync(0);

        // The loader downloads Bit.BevTools, attaches it to the running app and opens its panel, which lists the
        // components that existed before - and not its own.
        var panel = await OpenBevToolsAsync(page);
        _sample.AssertLoadedOnce(requests);

        await Expect(TreeItem(panel, "MainLayout")).ToBeVisibleAsync();
        await Expect(TreeItem(panel, "Home")).ToBeVisibleAsync();
        await Expect(TreeItem(panel, "BevToolsPanel")).ToHaveCountAsync(0);
        await Expect(TreeItem(panel, "BevToolsLoaderPanel")).ToHaveCountAsync(0);

        // And what happens after the attach: a navigation and the page it rendered.
        await GoToCounterAndClickAsync(page, expectedCount: 1);
        await Expect(TreeItem(panel, "Counter")).ToBeVisibleAsync();
        await AssertCurrentRouteAsync(panel, "/counter", "Counter");

        // Closed and opened again, and moving around the app meanwhile: nothing is downloaded again.
        await panel.GetByRole(AriaRole.Button, new() { Name = "Close BevTools", Exact = true }).ClickAsync();
        await Expect(panel).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Link, new() { Name = "Home", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Lazy loading");
        await OpenBevToolsAsync(page);
        await AssertCurrentRouteAsync(panel, "/", "Home");

        _sample.AssertLoadedOnce(requests);
    }

    [TestMethod]
    public async Task A_deep_link_is_prerendered_without_the_button_and_opens_BevTools_once_the_browser_takes_over()
    {
        var page = await NewPageAsync();
        var requests = _sample!.Track(page);

        // The server prerenders the layout through the loader it registered too, and as a process that serves many
        // users it does not offer to open BevTools outside Development.
        var response = await GotoAsync(page, "/counter");
        Assert.AreEqual(200, response?.Status);
        var prerendered = await response!.TextAsync();
        Assert.Contains("Current count: 0", prerendered);
        Assert.DoesNotContain("Open dev tools", prerendered);
        Assert.DoesNotContain("bev-root", prerendered);

        // The browser's copy of the page does, as WebAssembly only ever shows its user their own process.
        await WaitForInteractiveAsync(page);
        await Expect(OpenDevTools(page)).ToBeVisibleAsync();
        _sample.AssertNotLoaded(requests);

        var panel = await OpenBevToolsAsync(page);
        await Expect(TreeItem(panel, "Counter")).ToBeVisibleAsync();
        await AssertCurrentRouteAsync(panel, "/counter", "Counter");

        // The app keeps working under it, with BevTools watching.
        await page.GetByRole(AriaRole.Button, new() { Name = "Click me", Exact = true }).ClickAsync();
        await Expect(page.Locator("#count")).ToHaveTextAsync("Current count: 1");

        _sample.AssertLoadedOnce(requests);
    }

    /// <summary>The app's own button, which the layout renders where <c>BevToolsLoader.CanOpen</c> is true.</summary>
    private static ILocator OpenDevTools(IPage page) => page.GetByRole(AriaRole.Button, new() { Name = "Open dev tools", Exact = true });

    /// <summary>
    /// Presses the app's "Open dev tools" button and waits for BevTools' panel - or for the reason the loader gave for
    /// not opening it (<c>BevToolsLoader.Problem</c>, which the layout shows), which fails the test with that reason.
    /// </summary>
    private static async Task<ILocator> OpenBevToolsAsync(IPage page)
    {
        await OpenDevTools(page).ClickAsync();
        var panel = Panel(page);
        var problem = page.Locator(".problem");
        await Expect(panel.Or(problem)).ToBeVisibleAsync();
        if (await problem.IsVisibleAsync()) Assert.Fail("Open dev tools did not open BevTools: " + await problem.TextContentAsync());
        return panel;
    }

    /// <summary>BevTools' open panel.</summary>
    private static ILocator Panel(IPage page) => page.GetByRole(AriaRole.Region, new() { Name = "BevTools developer tools", Exact = true });

    /// <summary>A component in the panel's component tree, by its name.</summary>
    private static ILocator TreeItem(ILocator panel, string name) =>
        panel.GetByRole(AriaRole.Tree, new() { Name = "Component tree" }).Locator(".bev-name")
            .Filter(new() { HasTextRegex = new Regex("^" + Regex.Escape(name) + "$") });

    private static async Task GoToCounterAndClickAsync(IPage page, int expectedCount)
    {
        await page.GetByRole(AriaRole.Link, new() { Name = "Counter", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Counter");
        await page.GetByRole(AriaRole.Button, new() { Name = "Click me", Exact = true }).ClickAsync();
        await Expect(page.Locator("#count")).ToHaveTextAsync($"Current count: {expectedCount}");
    }

    /// <summary>
    /// The Routing tab names the page the router rendered and its template. The interactive router's RouteData does
    /// not carry the template that matched, so BevTools shows the page's only <c>@page</c>, labelled as inferred.
    /// </summary>
    private static async Task AssertCurrentRouteAsync(ILocator panel, string template, string pageName)
    {
        await panel.GetByRole(AriaRole.Tab, new() { Name = "Routing", Exact = true }).ClickAsync();
        var current = panel.Locator(".bev-section", new() { Has = panel.Page.Locator(".bev-section-title", new() { HasText = "Current route" }) });
        await Expect(current.Locator("dt:text-is('Page') + dd")).ToContainTextAsync(pageName);
        await Expect(current.Locator("dt:text-is('Template') + dd")).ToHaveTextAsync(new Regex("^" + Regex.Escape(template) + @"(?![\w/-])"));
        await panel.GetByRole(AriaRole.Tab, new() { Name = "Components", Exact = true }).ClickAsync();
    }
}
