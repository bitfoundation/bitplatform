using Bit.Brouter.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Brouter.Tests.E2E;

/// <summary>
/// Static server-side rendering: no circuit, no WebAssembly, no JS interop. Every navigation is a
/// request (turned into a DOM patch by Blazor's enhanced navigation), so Brouter's whole pipeline -
/// matching, guards, redirects, loaders, not-found - runs once per request on the server, and the
/// response status is the only way to tell a crawler a page does not exist.
/// </summary>
[TestClass]
public class StaticSsrModeTests : HarnessTest
{
    private readonly WebSession _session = new();

    protected override string BaseUrl => _session.Host.BaseUrl;

    protected override string Framework => E2EEnvironment.Framework;

    protected override Task<IPage> OpenPageAsync() => _session.OpenAsync("ssr");

    protected override Task ClosePageAsync() => _session.CloseAsync();

    [TestMethod]
    public async Task A_deep_link_is_served_as_static_html()
    {
        var response = await GotoAsync("/items/42");

        Assert.AreEqual(200, response!.Status);
        await Expect(Page.Locator("#page-item")).ToHaveAttributeAsync("data-item-id", "42");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator("#status")).ToHaveAttributeAsync("data-interactive", "false");
        if (E2EEnvironment.FrameworkReportsRendererName(Framework))
        {
            await Expect(Page.Locator("#status")).ToHaveAttributeAsync("data-renderer", "Static");
        }
    }

    [TestMethod]
    public async Task Enhanced_navigation_follows_a_link_without_reloading_the_document()
    {
        await GotoStartedAsync("/");
        await Page.EvaluateAsync("() => { window.__sameDocument = 'yes'; }");

        await ClickAndExpectAsync("#nav-item", "#page-item");

        await Expect(Page.Locator("#page-item")).ToHaveAttributeAsync("data-item-id", "7");
        await ExpectUrlAsync("/items/7");
        Assert.AreEqual("yes", await Page.EvaluateAsync<string>("() => window.__sameDocument || 'reloaded'"));
    }

    [TestMethod]
    public async Task Enhanced_navigation_follows_a_guard_redirect()
    {
        await GotoStartedAsync("/");

        await ClickAndExpectAsync("#nav-guarded", "#page-denied");

        await ExpectUrlAsync("/denied");
    }

    [TestMethod]
    public async Task A_deep_link_to_a_guarded_route_is_redirected()
    {
        await GotoAsync("/guarded");

        await Expect(Page.Locator("#page-denied")).ToBeVisibleAsync();
        await ExpectUrlAsync("/denied");
    }

    [TestMethod]
    public async Task A_deep_link_to_a_RedirectTo_route_is_redirected()
    {
        await GotoAsync("/old-about");

        await Expect(Page.Locator("#page-about")).ToBeVisibleAsync();
        await ExpectUrlAsync("/about");
    }

    [TestMethod]
    public async Task An_unmatched_url_is_answered_with_the_not_found_status_and_content()
    {
        AllowConsoleError("404");

        var response = await GotoAsync("/nope/deeper");

        Assert.AreEqual(E2EEnvironment.FrameworkHasNotFound(Framework) ? 404 : 200, response!.Status);
        await Expect(Page.Locator("#not-found")).ToHaveAttributeAsync("data-path", "/nope/deeper");
    }

    [TestMethod]
    public async Task A_page_calling_NavigationManager_NotFound_is_answered_with_404()
    {
        if (E2EEnvironment.FrameworkHasNotFound(Framework) is false)
            Assert.Inconclusive("NavigationManager.NotFound exists from .NET 10 on.");

        AllowConsoleError("404");

        var response = await GotoAsync("/missing/0");

        Assert.AreEqual(404, response!.Status);
        await Expect(Page.Locator("#not-found")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task The_loader_runs_in_the_request_that_renders_its_page()
    {
        await GotoAsync("/data");

        var renderScope = await Page.Locator("#render-scope").TextContentAsync();
        await Expect(Page.Locator("#data-scope")).ToHaveTextAsync(renderScope!);
        await Expect(Page.Locator("#data-run")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task A_failing_loader_renders_the_route_error_content()
    {
        await GotoAsync("/broken");

        await Expect(Page.Locator("#route-error")).ToHaveTextAsync("harness loader failure");
    }

    [TestMethod]
    public async Task Static_rendering_never_loads_the_script_module()
    {
        await GotoStartedAsync("/");
        await ClickAndExpectAsync("#nav-long", "#page-long");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var requests = await BrouterModuleRequestsAsync();

        Assert.AreEqual(0, requests.Length, $"Static rendering requested {string.Join(", ", requests.Select(r => r.Url))}.");
    }

    [TestMethod]
    public async Task Back_after_enhanced_navigation_returns_to_the_previous_route()
    {
        await GotoStartedAsync("/");
        await ClickAndExpectAsync("#nav-about", "#page-about");
        await ClickAndExpectAsync("#nav-item", "#page-item");

        await Page.GoBackAsync();

        await Expect(Page.Locator("#page-about")).ToBeVisibleAsync();
        await ExpectUrlAsync("/about");
    }

    /// <summary>Opens <paramref name="path"/> and waits until blazor.web.js intercepts links.</summary>
    private async Task GotoStartedAsync(string path)
    {
        await GotoAsync(path);
        await Page.WaitForFunctionAsync("() => !!window.Blazor");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
