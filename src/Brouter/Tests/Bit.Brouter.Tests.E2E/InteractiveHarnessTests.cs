using Bit.Brouter.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Brouter.Tests.E2E;

/// <summary>
/// Everything Brouter promises once it runs interactively, asserted by its effect in a real browser.
/// Each interactive host - Server, WebAssembly and Auto, with and without prerendering, and
/// BlazorWebView - runs this same list, so a feature that works in one host and not another shows up
/// as exactly that.
/// </summary>
/// <remarks>
/// The assertions are on effects (scroll offsets, focus, the history stack, attributes Brouter's module
/// writes) rather than on the absence of errors, because Brouter degrades silently by design: when its
/// JS module cannot load, navigation still works and scrolling, focus, view transitions and preloading
/// simply stop happening.
/// </remarks>
public abstract class InteractiveHarnessTests : HarnessTest
{
    /// <summary>Whether the first response already carries the rendered route.</summary>
    protected abstract bool Prerenders { get; }

    /// <summary><c>RendererInfo.Name</c> values the host may render with (checked from .NET 9 on).</summary>
    protected abstract IReadOnlyList<string> ExpectedRenderers { get; }

    /// <summary><c>data-platform</c> values the host may render on: "dotnet" or "browser".</summary>
    protected abstract IReadOnlyList<string> ExpectedPlatforms { get; }

    /// <summary>Whether a modified click may open a new page that the suite can observe and close.</summary>
    protected virtual bool SupportsNewWindows => true;

    /// <summary>Whether the page can be closed to observe a beforeunload prompt.</summary>
    protected virtual bool SupportsBeforeUnload => true;

    [TestMethod]
    public async Task The_harness_runs_on_the_expected_runtime()
    {
        await GotoAsync("/");
        await WaitForInteractiveAsync();

        var status = Page.Locator("#status");
        CollectionAssert.Contains(ExpectedPlatforms.ToList(), await status.GetAttributeAsync("data-platform"));

        if (E2EEnvironment.FrameworkReportsRendererName(Framework))
        {
            CollectionAssert.Contains(ExpectedRenderers.ToList(), await status.GetAttributeAsync("data-renderer"));
        }
    }

    [TestMethod]
    public async Task A_deep_link_renders_its_route_and_becomes_interactive()
    {
        await GotoAsync("/items/42");

        await Expect(Page.Locator("#page-item")).ToHaveAttributeAsync("data-item-id", "42");
        await WaitForInteractiveAsync();
        await Expect(Page.Locator("#page-item")).ToHaveAttributeAsync("data-item-id", "42");
    }

    [TestMethod]
    public async Task A_link_click_navigates_without_reloading_the_document()
    {
        await GotoAsync("/");
        await WaitForInteractiveAsync();
        await Page.EvaluateAsync("() => { window.__sameDocument = 'yes'; }");

        await ClickAndExpectAsync("#nav-about", "#page-about");

        await ExpectUrlAsync("/about");
        Assert.AreEqual("yes", await Page.EvaluateAsync<string>("() => window.__sameDocument || 'reloaded'"));
    }

    [TestMethod]
    public async Task Browser_back_and_forward_move_between_routes()
    {
        await GotoAsync("/");
        await WaitForInteractiveAsync();
        await ClickAndExpectAsync("#nav-about", "#page-about");
        await ClickAndExpectAsync("#nav-item", "#page-item");

        await Page.GoBackAsync();
        await Expect(Page.Locator("#page-about")).ToBeVisibleAsync();
        await ExpectUrlAsync("/about");

        await Page.GoForwardAsync();
        await Expect(Page.Locator("#page-item")).ToHaveAttributeAsync("data-item-id", "7");
        await ExpectUrlAsync("/items/7");
    }

    [TestMethod]
    public async Task A_guard_redirect_lands_on_its_target()
    {
        await GotoAsync("/");
        await WaitForInteractiveAsync();

        await ClickAndExpectAsync("#nav-guarded", "#page-denied");

        await ExpectUrlAsync("/denied");
        await Expect(Page.Locator("#page-guarded")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task A_RedirectTo_route_lands_on_its_target()
    {
        await GotoAsync("/");
        await WaitForInteractiveAsync();

        await ClickAndExpectAsync("#nav-redirect", "#page-about");

        await ExpectUrlAsync("/about");
    }

    [TestMethod]
    public async Task An_unmatched_url_renders_the_not_found_content()
    {
        await GotoAsync("/");
        await WaitForInteractiveAsync();

        await ClickAndExpectAsync("#nav-nope", "#not-found");

        await Expect(Page.Locator("#not-found")).ToHaveAttributeAsync("data-path", "/nope");
        await ExpectUrlAsync("/nope");
    }

    [TestMethod]
    public async Task A_failing_loader_renders_the_route_error_content()
    {
        // The failure is the point of the test; Brouter may report it before rendering the error content.
        AllowConsoleError("harness loader failure");

        await GotoAsync("/");
        await WaitForInteractiveAsync();

        await ClickAndExpectAsync("#nav-broken", "#route-error");

        await Expect(Page.Locator("#route-error")).ToHaveTextAsync("harness loader failure");
    }

    [TestMethod]
    public async Task NavigationManager_NotFound_from_a_page_renders_the_not_found_content()
    {
        if (E2EEnvironment.FrameworkHasNotFound(Framework) is false)
            Assert.Inconclusive("NavigationManager.NotFound exists from .NET 10 on.");

        await GotoAsync("/");
        await WaitForInteractiveAsync();

        await ClickAndExpectAsync("#nav-missing", "#not-found");

        await ExpectUrlAsync("/missing/0");
    }

    [TestMethod]
    public async Task The_script_module_loads_from_the_app_base_when_the_app_starts_on_a_deep_url()
    {
        // Brouter imports "./_content/Bit.Brouter/bit-brouter.js". Resolved against the document instead
        // of <base href>, that becomes /deep/a/b/c/_content/... - a 404 Brouter would swallow, leaving
        // every JS-backed feature silently switched off. Brouter is a catch-all router, so most sessions
        // do start on a deep URL.
        await GotoAsync("/deep/a/b/c/leaf");
        await Expect(Page.Locator("#page-deep")).ToHaveAttributeAsync("data-leaf", "leaf");
        await WaitForInteractiveAsync();

        await ClickAndExpectAsync("#nav-about", "#page-about");

        // Stamped by the module's beginViewTransition: proof it loaded and ran.
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-brouter-nav", "push");

        var requests = await BrouterModuleRequestsAsync();
        Assert.IsTrue(requests.Length > 0, "The page never requested the Bit.Brouter module.");
        foreach (var request in requests)
        {
            StringAssert.StartsWith(new Uri(request.Url).AbsolutePath, "/_content/Bit.Brouter/", $"The module was requested from {request.Url}.");
            // 0 is what a host that does not expose the status (an intercepted WebView request) reports.
            Assert.IsTrue(request.Status is 0 or (>= 200 and < 300), $"{request.Url} answered {request.Status}.");
        }
    }

    [TestMethod]
    public async Task The_initial_load_is_not_animated_as_a_navigation()
    {
        await GotoAsync("/items/42");
        await WaitForInteractiveAsync();

        // Nothing to wait for: the assertion is that nothing happens. Hydration and the first
        // navigation pipeline are over well within this.
        await Page.WaitForTimeoutAsync(750);

        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => document.documentElement.hasAttribute('data-brouter-nav')"),
            "The initial load ran a view transition; with prerendering that re-animates over identical HTML.");
    }

    [TestMethod]
    public async Task View_transitions_are_stamped_with_the_direction_of_the_navigation()
    {
        await GotoAsync("/history/1");
        await WaitForInitialNavigationEffectsAsync("#page-history");
        var root = Page.Locator("html");

        await ClickAndExpectAsync("#plain-link", "#page-history[data-step='4']");
        await Expect(root).ToHaveAttributeAsync("data-brouter-nav", "push");

        await Page.GoBackAsync();
        await Expect(Page.Locator("#page-history[data-step='1']")).ToBeVisibleAsync();
        await Expect(root).ToHaveAttributeAsync("data-brouter-nav", "pop");

        await ClickAndExpectAsync("#replace-link", "#page-history[data-step='3']");
        await Expect(root).ToHaveAttributeAsync("data-brouter-nav", "replace");

        await Expect(Page.Locator("style#bit-brouter-view-transitions")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task A_new_navigation_scrolls_to_the_top_and_focuses_the_heading()
    {
        await GotoAsync("/long");
        await WaitForInitialNavigationEffectsAsync("#page-long");
        await Page.EvaluateAsync("() => window.scrollTo(0, 2000)");
        await WaitForScrollYAsync(2000);

        await ClickAndExpectAsync("#long-to-other", "#page-other");

        await WaitForScrollYAsync(0);
        await Expect(Page.Locator("#page-other")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Back_restores_the_scroll_position_of_the_page_it_returns_to()
    {
        await GotoAsync("/long");
        await WaitForInitialNavigationEffectsAsync("#page-long");
        await Page.EvaluateAsync("() => window.scrollTo(0, 2000)");
        await WaitForScrollYAsync(2000);
        await ClickAndExpectAsync("#long-to-other", "#page-other");
        await WaitForScrollYAsync(0);

        await Page.GoBackAsync();

        await Expect(Page.Locator("#page-long")).ToBeVisibleAsync();
        await WaitForScrollYAsync(2000);
        Assert.AreEqual("manual", await Page.EvaluateAsync<string>("() => history.scrollRestoration"));
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => sessionStorage.getItem('bit-brouter:scrollPositions') !== null"),
            "ScrollPositionStorage.SessionStorage persisted nothing.");
    }

    [TestMethod]
    public async Task A_new_navigation_after_Back_scrolls_to_the_top_instead_of_restoring()
    {
        // Back used to leave Brouter's "this was a history traversal" flags set on WebAssembly: Blazor's
        // own popstate listener runs the whole .NET commit synchronously, before Brouter's listener sees
        // the same event. The next ordinary navigation was then restored like a Back.
        await GotoAsync("/long");
        await WaitForInitialNavigationEffectsAsync("#page-long");
        await ClickAndExpectAsync("#long-to-other", "#page-other");
        // The arriving page is visible before Brouter has finished with it, and scrolling in that window is
        // undone by its scroll-to-top - two runs in six ended here with scrollY back at 0. The focused
        // heading is the signal that the navigation's effects have run.
        await Expect(Page.Locator("#page-other")).ToBeFocusedAsync();
        await Page.EvaluateAsync("() => window.scrollTo(0, 1500)");
        await WaitForScrollYAsync(1500);

        await Page.GoBackAsync();
        await Expect(Page.Locator("#page-long")).ToBeVisibleAsync();

        // /other has a remembered offset of 1500; only a Back/Forward may return to it.
        await ClickAndExpectAsync("#long-to-other", "#page-other");
        await WaitForScrollYAsync(0);
    }

    [TestMethod]
    public async Task A_fragment_navigation_scrolls_to_and_focuses_its_target()
    {
        await GotoAsync("/other");
        await WaitForInitialNavigationEffectsAsync("#page-other");

        await ClickAndExpectAsync("#other-to-anchor", "#page-long");

        await ExpectUrlAsync("/long#bottom-anchor");
        await Expect(Page.Locator("#bottom-anchor")).ToBeFocusedAsync();
        Assert.IsTrue(await ScrollYAsync() > 2500, $"window.scrollY is {await ScrollYAsync()}; the anchor was not scrolled into view.");
    }

    [TestMethod]
    public async Task Intent_preloading_runs_the_loader_on_hover_and_the_click_reuses_its_result()
    {
        await GotoAsync("/preload");
        await WaitForInitialNavigationEffectsAsync("#page-preload");
        await Expect(Page.Locator("#intent-runs")).ToHaveTextAsync("0");

        await Page.Locator("#intent-link").HoverAsync();
        await Expect(Page.Locator("#intent-runs")).ToHaveTextAsync("1");

        await ClickAndExpectAsync("#intent-link", "#page-preload-target");
        await Expect(Page.Locator("#target-run")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Viewport_preloading_runs_the_loader_when_the_link_scrolls_into_view()
    {
        await GotoAsync("/preload");
        // The initial load scrolls to the top when its effects land; scrolling before that would be undone.
        await WaitForInitialNavigationEffectsAsync("#page-preload");
        await Expect(Page.Locator("#viewport-runs")).ToHaveTextAsync("0");

        await Page.Locator("#viewport-link").ScrollIntoViewIfNeededAsync();

        await Expect(Page.Locator("#viewport-runs")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task A_leave_guard_cancels_a_link_navigation()
    {
        await GotoAsync("/leave");
        await WaitForInteractiveAsync();
        await Page.Locator("#block-leave").CheckAsync();

        await Page.Locator("#leave-to-about").ClickAsync();
        // A cancelled navigation leaves nothing to wait for; allow it the time a navigation takes here.
        await Page.WaitForTimeoutAsync(1000);

        await ExpectUrlAsync("/leave");
        await Expect(Page.Locator("#page-leave")).ToBeVisibleAsync();

        await Page.Locator("#block-leave").UncheckAsync();
        await ClickAndExpectAsync("#leave-to-about", "#page-about");
    }

    [TestMethod]
    public async Task A_leave_guard_cancels_browser_back()
    {
        await GotoAsync("/about");
        await WaitForInteractiveAsync();
        await ClickAndExpectAsync("#nav-leave", "#page-leave");
        await Page.Locator("#block-leave").CheckAsync();

        await Page.EvaluateAsync("() => history.back()");
        await Page.WaitForTimeoutAsync(1000);

        await ExpectUrlAsync("/leave");
        await Expect(Page.Locator("#page-leave")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task History_state_attached_by_a_link_survives_back_and_forward()
    {
        await GotoAsync("/history/1");
        await WaitForInitialNavigationEffectsAsync("#page-history");

        await ClickAndExpectAsync("#state-link", "#page-history[data-step='2']");
        await Expect(Page.Locator("#history-state")).ToHaveTextAsync("from-link");

        await ClickAndExpectAsync("#plain-link", "#page-history[data-step='4']");
        await Expect(Page.Locator("#history-state")).ToHaveTextAsync("(none)");

        await Page.GoBackAsync();
        await Expect(Page.Locator("#page-history[data-step='2']")).ToBeVisibleAsync();
        await Expect(Page.Locator("#history-state")).ToHaveTextAsync("from-link");
    }

    [TestMethod]
    public async Task A_Replace_link_does_not_add_a_history_entry()
    {
        await GotoAsync("/history/1");
        await WaitForInitialNavigationEffectsAsync("#page-history");
        var lengthBefore = await Page.EvaluateAsync<int>("() => history.length");

        await ClickAndExpectAsync("#replace-link", "#page-history[data-step='3']");

        await ExpectUrlAsync("/history/3");
        Assert.AreEqual(lengthBefore, await Page.EvaluateAsync<int>("() => history.length"));
    }

    [TestMethod]
    public async Task Programmatic_back_and_forward_walk_the_browser_history()
    {
        await GotoAsync("/history/1");
        await WaitForInitialNavigationEffectsAsync("#page-history");
        await ClickAndExpectAsync("#plain-link", "#page-history[data-step='4']");

        await ClickAndExpectAsync("#go-back", "#page-history[data-step='1']");
        await ExpectUrlAsync("/history/1");

        await ClickAndExpectAsync("#go-forward", "#page-history[data-step='4']");
        await ExpectUrlAsync("/history/4");
    }

    [TestMethod]
    public async Task A_modified_click_on_an_intercepted_link_keeps_the_native_new_tab_behavior()
    {
        if (SupportsNewWindows is false)
            Assert.Inconclusive("This host hands new-window requests to the operating system.");

        await GotoAsync("/history/1");
        await WaitForInitialNavigationEffectsAsync("#page-history");

        var opened = Page.Context.WaitForPageAsync();
        await Page.Locator("#replace-link").ClickAsync(new() { Modifiers = [KeyboardModifier.ControlOrMeta] });
        await (await opened).CloseAsync();

        // The Replace link intercepts plain clicks only; this one belonged to the browser.
        await Page.WaitForTimeoutAsync(500);
        await ExpectUrlAsync("/history/1");
        await Expect(Page.Locator("#page-history[data-step='1']")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task A_keep_alive_route_keeps_its_state_across_navigations()
    {
        await GotoAsync("/keepalive");
        await WaitForInteractiveAsync();
        for (var i = 0; i < 3; i++) await Page.Locator("#keepalive-increment").ClickAsync();
        await Expect(Page.Locator("#keepalive-count")).ToHaveTextAsync("3");

        await ClickAndExpectAsync("#nav-about", "#page-about");
        await ClickAndExpectAsync("#nav-keepalive", "#page-keepalive");

        await Expect(Page.Locator("#keepalive-count")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task Confirming_external_navigation_prompts_before_the_page_unloads()
    {
        if (SupportsBeforeUnload is false)
            Assert.Inconclusive("Closing the page here would close the host's only WebView.");

        await GotoAsync("/confirm");
        await WaitForInteractiveAsync();
        // A real click also gives the page the user activation browsers require before they prompt.
        await Page.Locator("#arm-confirm").ClickAsync();
        await Expect(Page.Locator("#confirm-state")).ToHaveTextAsync("armed");

        var dialogType = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        Page.Dialog += (_, dialog) =>
        {
            dialogType.TrySetResult(dialog.Type);
            _ = dialog.AcceptAsync();
        };

        await Page.CloseAsync(new() { RunBeforeUnload = true });

        Assert.AreEqual("beforeunload", await dialogType.Task.WaitAsync(TimeSpan.FromSeconds(15)));
    }

    [TestMethod]
    public async Task The_initial_route_loader_result_reaches_the_interactive_page()
    {
        var response = await GotoAsync("/data");
        var prerenderedDataScope = Prerenders ? PrerenderedText(await response!.TextAsync(), "data-scope") : null;

        await WaitForInteractiveAsync();
        var interactiveScope = await Page.Locator("#status").GetAttributeAsync("data-scope");
        await Expect(Page.Locator("#render-scope")).ToHaveTextAsync(interactiveScope!);
        var dataScope = await Page.Locator("#data-scope").TextContentAsync();

        if (Prerenders)
        {
            // PersistLoaderState: the interactive pass restores what the prerender loaded instead of
            // loading it again, so the data still names the prerender's scope.
            Assert.IsFalse(string.IsNullOrEmpty(prerenderedDataScope), "The prerendered response carried no loader data.");
            Assert.AreEqual(prerenderedDataScope, dataScope, "The interactive pass ran the loader again instead of restoring the prerendered result.");
            Assert.AreNotEqual(interactiveScope, dataScope);
        }
        else
        {
            Assert.AreEqual(interactiveScope, dataScope);
        }

        await Expect(Page.Locator("#data-run")).ToHaveTextAsync("1");
    }

    /// <summary>The text of the element with <paramref name="id"/> in raw prerendered HTML.</summary>
    protected static string? PrerenderedText(string html, string id)
    {
        var match = System.Text.RegularExpressions.Regex.Match(html, $"id=\"{id}\"[^>]*>([^<]*)<");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
