using System;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for an open callout that the on-screen keyboard shrinks the viewport under.
///
/// A callout whose search box takes the focus when it opens raises the keyboard, which leaves the visible band
/// covering only the top of the screen. An anchor below that band used to be read as scrolled off the page: the
/// callout was hidden as "detached", placed behind the keyboard with a negative bottom, or dismissed outright by
/// the window resize handler - while the callout itself sat perfectly visible above the keyboard. It has to be
/// laid out inside the band the keyboard left instead.
///
/// The platforms disagree on which viewport the keyboard shrinks, so both shapes are driven here: Android Chrome
/// shrinks the layout viewport, which <see cref="IPage.SetViewportSizeAsync"/> reproduces exactly, while iOS
/// shrinks only the visual one, which is stubbed - <c>window.resize</c> never fires for it.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitCalloutKeyboardBrowserTests
public class BitCalloutKeyboardBrowserTests : PerformanceTestBase
{
    private const int ViewportWidth = 412;
    private const int ViewportHeight = 800;
    // What is left of the screen once the keyboard is up, which is where the callout has to end up.
    private const int BandHeight = 400;

    // The predicate that keeps the callout alive only holds where the primary pointer is a finger: on anything
    // else a resize is the window being resized and goes on dismissing the callout, which the desktop test
    // classes below cover. Touch alone is what makes it so - IsMobile is left out, since Firefox cannot take it.
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        HasTouch = true,
        ViewportSize = new() { Width = ViewportWidth, Height = ViewportHeight }
    };

    /// <summary>
    /// Android Chrome's shape: the keyboard shrinks the layout viewport, so window 'resize' fires and every
    /// measurement - window.innerHeight, visualViewport.height, the fixed probe - moves together. Nothing is
    /// stubbed here; the viewport really is made shorter.
    /// </summary>
    [TestMethod]
    public async Task Callout_WhenTheKeyboardShrinksTheLayoutViewport_StaysVisibleInsideTheBand()
    {
        await OpenTheCalloutWithTheSearchBox();

        await Page.SetViewportSizeAsync(ViewportWidth, BandHeight);

        await AssertCalloutIsInsideTheBand(BandHeight);
    }

    /// <summary>
    /// iOS Safari's shape: the layout viewport keeps its height and only the visual one shrinks, so no window
    /// 'resize' fires at all and the callout's fate is decided entirely by the reposition that the visual
    /// viewport's own resize drives.
    /// </summary>
    [TestMethod]
    public async Task Callout_WhenTheKeyboardShrinksOnlyTheVisualViewport_StaysVisibleInsideTheBand()
    {
        await StubTheVisualViewport();
        await OpenTheCalloutWithTheSearchBox();

        await RaiseTheStubbedKeyboard(ViewportHeight - BandHeight);

        await AssertCalloutIsInsideTheBand(BandHeight);
    }

    /// <summary>
    /// The control for the change: an anchor that leaves the band with nothing editable focused was not put
    /// there by a keyboard, so the callout still has nothing on screen to point at and is still hidden. The
    /// visual viewport is what is moved, because a real resize would dismiss the callout before the placement
    /// ever ran and the assertion would pass for the wrong reason.
    /// </summary>
    [TestMethod]
    public async Task Callout_WhenTheAnchorLeavesTheBandWithNoEditableFocused_IsStillHidden()
    {
        await StubTheVisualViewport();
        await GoToThePage();

        // No search box, so the focus lands on the dropdown button - which raises no keyboard and is not an
        // editable element, the one thing the predicate asks for.
        await Page.Locator("#without-search .bit-phi-drp").TapAsync();
        var calloutId = await CalloutIdOf("#without-search");
        await Expect(Page.Locator($"#{calloutId}")).ToBeVisibleAsync();

        await RaiseTheStubbedKeyboard(ViewportHeight - BandHeight);

        await Expect(Page.Locator($"#{calloutId}")).ToBeHiddenAsync();
    }

    /// <summary>
    /// The window resize handler used to keep the callout only below a 600px screen, which excluded every
    /// landscape phone, tablet and unfolded foldable - all of which raise a keyboard just the same, and all of
    /// which had the callout genuinely dismissed out from under it.
    /// </summary>
    [TestMethod]
    public async Task Callout_WhenTheKeyboardOpensOnAWideTouchScreen_IsNotDismissed()
    {
        await Page.SetViewportSizeAsync(800, 400);
        await OpenTheCalloutWithTheSearchBox();

        await Page.SetViewportSizeAsync(800, 200);

        // Dismissal is what this guards: a dismissed callout is closed by .NET and loses its display, where a
        // callout that merely moved is still there to be measured.
        await AssertCalloutIsInsideTheBand(200);
    }

    /// <summary>
    /// The keyboard going away has to give the callout back to its anchor, rather than leaving it pinned to the
    /// band it was clamped into while the keyboard was up.
    /// </summary>
    [TestMethod]
    public async Task Callout_WhenTheKeyboardCloses_GoesBackToItsAnchor()
    {
        await OpenTheCalloutWithTheSearchBox();

        await Page.SetViewportSizeAsync(ViewportWidth, BandHeight);
        await AssertCalloutIsInsideTheBand(BandHeight);

        await Page.SetViewportSizeAsync(ViewportWidth, ViewportHeight);

        // Where the anchor ends up rather than where it started: a short viewport scrolls the page to keep
        // the focused element in view and does not scroll back, so the anchor is somewhere else by now. What
        // has to be true is that the callout is tucked against it again, on one side or the other, instead of
        // still pinned to the band it was clamped into.
        await Page.WaitForFunctionAsync(
            @"([calloutId, componentId]) => {
                const callout = document.getElementById(calloutId).getBoundingClientRect();
                const component = document.getElementById(componentId).getBoundingClientRect();
                return Math.abs(callout.bottom - component.top) < 4 || Math.abs(callout.top - component.bottom) < 4;
            }",
            new object[] { await CalloutIdOf("#with-search"), await FieldGroupIdOf("#with-search") },
            new PageWaitForFunctionOptions { Timeout = 5000 });
    }

    private async Task GoToThePage()
    {
        await Page.GotoAsync($"{BaseUrl}/regression/callout-keyboard");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive
    }

    private async Task OpenTheCalloutWithTheSearchBox()
    {
        await GoToThePage();

        await Page.Locator("#with-search .bit-phi-drp").TapAsync();

        // The search box taking the focus is what raises the keyboard, so nothing is driven until it has.
        await Expect(Page.Locator($"#{await CalloutIdOf("#with-search")} .bit-phi-srch")).ToBeFocusedAsync();
    }

    /// <summary>
    /// The callout is moved to the body when it opens, so it is no longer under the wrapper that names it. Its
    /// id is the dropdown button's with the suffix swapped, which is what ties the two back together.
    /// </summary>
    private Task<string> CalloutIdOf(string wrapper) => ElementIdOf(wrapper, "callout");

    /// <summary>The anchor the callout is placed against, which is the field as a whole, not the button.</summary>
    private Task<string> FieldGroupIdOf(string wrapper) => ElementIdOf(wrapper, "field-group");

    private Task<string> ElementIdOf(string wrapper, string suffix)
    {
        return Page.EvaluateAsync<string>(
            "([sel, suffix]) => document.querySelector(sel + ' .bit-phi-drp').id.replace('-dropdown', '-' + suffix)",
            new[] { wrapper, suffix });
    }

    private async Task AssertCalloutIsInsideTheBand(double bandHeight)
    {
        var calloutId = await CalloutIdOf("#with-search");

        await Expect(Page.Locator($"#{calloutId}")).ToBeVisibleAsync();

        // The viewport changing is a burst of events, and the reposition that follows it is throttled to a
        // frame and run once more when the burst settles - which is the pass the callout's final geometry
        // comes from. Polling is what waits for it; the assertion below is what reports the state it
        // settled in, since a timeout here says only that it never arrived.
        try
        {
            await Page.WaitForFunctionAsync(
                @"([id, band]) => {
                    const box = document.getElementById(id).getBoundingClientRect();
                    return box.height > 0 && box.top >= 0 && box.bottom <= band + 1;
                }",
                new object[] { calloutId, bandHeight },
                new PageWaitForFunctionOptions { Timeout = 5000 });
        }
        catch (TimeoutException)
        {
            var box = await Page.Locator($"#{calloutId}").BoundingBoxAsync();
            Assert.IsNotNull(box, "The callout has no box, so it is not laid out at all.");
            Assert.IsTrue(box.Height > 0, "The callout was collapsed to nothing rather than measured against the band.");
            Assert.IsTrue(box.Y >= 0, $"The callout starts above the screen at {box.Y}.");
            Assert.IsTrue(box.Y + box.Height <= bandHeight + 1,
                $"The callout ends at {box.Y + box.Height}, behind a keyboard that leaves {bandHeight}.");
        }
    }

    /// <summary>
    /// Replaces window.visualViewport before any page script runs, so that the keyboard can be raised without
    /// touching the layout viewport - which is what iOS does, and what no Playwright API can ask a browser for.
    /// general.ts reads window.visualViewport once at load to subscribe, and Utils.getViewport() reads it on
    /// every measurement, so the stub is all either of them ever sees.
    /// </summary>
    private Task StubTheVisualViewport() => StubTheVisualViewport(Page);

    internal static Task StubTheVisualViewport(IPage page)
    {
        return page.AddInitScriptAsync(@"
            const target = new EventTarget();
            const vv = {
                width: window.innerWidth,
                height: window.innerHeight,
                offsetLeft: 0,
                offsetTop: 0,
                scale: 1,
                addEventListener: (...a) => target.addEventListener(...a),
                removeEventListener: (...a) => target.removeEventListener(...a)
            };
            Object.defineProperty(window, 'visualViewport', { value: vv, configurable: true });
            window.__raiseKeyboard = inset => {
                vv.height = window.innerHeight - inset;
                target.dispatchEvent(new Event('resize'));
            };");
    }

    private Task RaiseTheStubbedKeyboard(int inset) => RaiseTheStubbedKeyboard(Page, inset);

    internal static async Task RaiseTheStubbedKeyboard(IPage page, int inset)
    {
        await page.EvaluateAsync("inset => window.__raiseKeyboard(inset)", inset);

        // The reposition is throttled to a frame and re-run once the burst settles, so the settled pass is
        // what the assertions have to see rather than the first one.
        await page.WaitForTimeoutAsync(250);
    }
}

/// <summary>
/// The desktop control for <see cref="BitCalloutKeyboardBrowserTests"/>: with no touch screen there is no
/// keyboard to blame a resize on, so a resize is the window being resized and goes on dismissing the callout.
/// It is a class of its own because the touch screen is a property of the browser context, not of the page.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser]
public class BitCalloutResizeBrowserTests : PerformanceTestBase
{
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 1024, Height = 800 }
    };

    [TestMethod]
    public async Task Callout_WhenTheWindowIsResizedOnADesktop_IsStillDismissed()
    {
        await Page.GotoAsync($"{BaseUrl}/regression/callout-keyboard");
        await WaitForStatus("Ready");

        await Page.Locator("#with-search .bit-phi-drp").ClickAsync();

        var calloutId = await Page.EvaluateAsync<string>(
            "() => document.querySelector('#with-search .bit-phi-drp').id.replace('-dropdown', '-callout')");
        await Expect(Page.Locator($"#{calloutId}")).ToBeVisibleAsync();

        await Page.SetViewportSizeAsync(700, 500);

        await Expect(Page.Locator($"#{calloutId}")).ToBeHiddenAsync();
    }
}

/// <summary>
/// A touch-screen laptop: it has a touch screen, but its primary pointer is the mouse or the trackpad and its
/// keyboard is a physical one, so a short viewport there is the window made shorter - never a keyboard - even
/// with the focus in a callout's search box. Touch being available is not what decides it, the primary pointer
/// is: Chromium makes the pointer coarse along with HasTouch, so the laptop's fine one is stubbed back in.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser]
public class BitCalloutTouchLaptopBrowserTests : PerformanceTestBase
{
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        HasTouch = true,
        ViewportSize = new() { Width = 1024, Height = 800 }
    };

    /// <summary>The window resize handler goes on dismissing the callout, as it does on any desktop.</summary>
    [TestMethod]
    public async Task Callout_WhenTheWindowIsResizedOnATouchLaptop_IsStillDismissed()
    {
        var calloutId = await OpenTheCalloutWithTheSearchBox();

        await Page.SetViewportSizeAsync(1024, 400);

        await Expect(Page.Locator($"#{calloutId}")).ToBeHiddenAsync();
    }

    /// <summary>
    /// The placement does not blame the short band on a keyboard either: an anchor below it is detached and the
    /// callout hidden with it, rather than pinned to the bottom of the window away from what it points at. The
    /// visual viewport is what is moved, because a real resize would dismiss the callout before the placement
    /// ever ran and the assertion would pass for the wrong reason.
    /// </summary>
    [TestMethod]
    public async Task Callout_WhenTheBandShrinksAboveTheAnchorOnATouchLaptop_IsHiddenNotPinned()
    {
        await BitCalloutKeyboardBrowserTests.StubTheVisualViewport(Page);
        var calloutId = await OpenTheCalloutWithTheSearchBox();

        await BitCalloutKeyboardBrowserTests.RaiseTheStubbedKeyboard(Page, 400);

        await Expect(Page.Locator($"#{calloutId}")).ToBeHiddenAsync();
    }

    private async Task<string> OpenTheCalloutWithTheSearchBox()
    {
        await StubAFinePrimaryPointer();

        await Page.GotoAsync($"{BaseUrl}/regression/callout-keyboard");
        await WaitForStatus("Ready");

        // Touch is still there to be read, which is what a touch laptop is: the stub takes away only the
        // coarse primary pointer. Firefox and WebKit report no touch points but do take touch events, which is
        // as much a touch screen to the library. Both halves are checked, since a stub that threw would leave
        // every query failing and the callout dismissed for a reason that has nothing to do with the pointer.
        Assert.IsTrue(await Page.EvaluateAsync<bool>(
            "() => ('ontouchstart' in window || navigator.maxTouchPoints > 0) && matchMedia('(pointer: coarse)').matches === false && matchMedia('(min-width: 1px)').matches"),
            "The context is not a touch laptop: no touch screen, or the primary pointer stub is not in place.");

        await Page.Locator("#with-search .bit-phi-drp").ClickAsync();

        var calloutId = await Page.EvaluateAsync<string>(
            "() => document.querySelector('#with-search .bit-phi-drp').id.replace('-dropdown', '-callout')");
        await Expect(Page.Locator($"#{calloutId} .bit-phi-srch")).ToBeFocusedAsync();

        return calloutId;
    }

    private Task StubAFinePrimaryPointer()
    {
        return Page.AddInitScriptAsync(@"
            const matchMedia = window.matchMedia.bind(window);
            window.matchMedia = query => /\(\s*pointer\s*:\s*coarse\s*\)/.test(query) && !/any-pointer/.test(query)
                ? matchMedia('not all')
                : matchMedia(query);");
    }
}
