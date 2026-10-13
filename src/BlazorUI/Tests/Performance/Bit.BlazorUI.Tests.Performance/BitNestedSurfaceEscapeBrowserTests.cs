using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for an Escape pressed in a surface opened from inside another one, driven with real keyboard
/// and mouse input.
///
/// The first press closes the inner surface alone and the second the outer one. No surface stops the key, so it bubbles
/// on from the inner one's root through the outer one's, and the outer one leaves it alone only because the inner one
/// recorded itself as its owner on the way (Utils._escapeSurfaceOwners) - an Overlay as well as a Dialog, a Panel or a
/// Modal, whichever of them is the inner one. An Overlay opened from a button of a Panel, which keeps the focus, owns
/// the press when it is the one on top of the Panel (Utils.layerOver). Those are native listeners bUnit cannot run, so the behavior is
/// pinned down here, in every engine (BROWSER=chromium, firefox, webkit).
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitNestedSurfaceEscapeBrowserTests
public class BitNestedSurfaceEscapeBrowserTests : PerformanceTestBase
{
    [TestMethod]
    public async Task Dialog_InADropMenuPopup_ClosesFirstAndThePopupClosesOnTheNextPress()
    {
        await OpenPage("menu-dialog");

        await OpenOuter(Page.Locator("#outer-trigger button.bit-drm-btn"), surfaceId: null);
        await OpenInner(Page.Locator("#btn-open-inner"));

        await AssertOneSurfaceClosesPerPress();
    }

    [TestMethod]
    public async Task DropMenuPopup_InADialog_ClosesFirstAndTheDialogClosesOnTheNextPress()
    {
        await OpenPage("dialog-menu");

        await OpenOuter(Page.Locator("#btn-open-outer"));
        await OpenInner(Page.Locator("#inner-trigger button.bit-drm-btn"), surfaceId: null);

        await AssertOneSurfaceClosesPerPress();
    }

    [TestMethod]
    public async Task Dialog_InADialog_ClosesFirstAndTheOuterDialogClosesOnTheNextPress()
    {
        await OpenPage("dialog-dialog");

        await OpenOuter(Page.Locator("#btn-open-outer"));
        await OpenInner(Page.Locator("#btn-open-inner"));

        await AssertOneSurfaceClosesPerPress();
    }

    [TestMethod]
    public async Task Dialog_InAnOverlay_ClosesFirstAndTheOverlayClosesOnTheNextPress()
    {
        await OpenPage("overlay-dialog");

        await OpenOuter(Page.Locator("#btn-open-outer"));
        await OpenInner(Page.Locator("#btn-open-inner"));

        await AssertOneSurfaceClosesPerPress();
    }

    [TestMethod]
    public async Task Panel_InAnOverlay_ClosesFirstAndTheOverlayClosesOnTheNextPress()
    {
        await OpenPage("overlay-panel");

        await OpenOuter(Page.Locator("#btn-open-outer"));
        await OpenInner(Page.Locator("#btn-open-inner"));

        await AssertOneSurfaceClosesPerPress();
    }

    // The Overlay is an escape root of its own, and the Modal leaves a press made below one alone; before the Overlay
    // recorded itself as the owner, it took a Modal around it for the surface the press was made in, and neither closed.
    [TestMethod]
    public async Task Overlay_InAModal_ClosesFirstAndTheModalClosesOnTheNextPress()
    {
        await OpenPage("modal-overlay");

        await OpenOuter(Page.Locator("#btn-open-outer"));
        await OpenInner(Page.Locator("#btn-open-inner"));

        await AssertOneSurfaceClosesPerPress();
    }

    [TestMethod]
    public async Task Overlay_InAPanel_ClosesFirstAndThePanelClosesOnTheNextPress()
    {
        await OpenPage("panel-overlay");

        await OpenOuter(Page.Locator("#btn-open-outer"));
        await OpenInner(Page.Locator("#btn-open-inner"));

        await AssertOneSurfaceClosesPerPress();
    }

    [TestMethod]
    public async Task Overlay_InADialog_ClosesFirstAndTheDialogClosesOnTheNextPress()
    {
        await OpenPage("dialog-overlay");

        await OpenOuter(Page.Locator("#btn-open-outer"));
        await OpenInner(Page.Locator("#btn-open-inner"));

        await AssertOneSurfaceClosesPerPress();
    }

    // The Overlay does not take the focus, so the press is made from the Panel's button, under the layer: the Overlay
    // the user sees on top of it is the one it closes, wherever in the page it is rendered. One rendered outside the
    // Panel is under the Panel's surface (the overlay layer is below the one the surface is lifted to) unless its ZIndex
    // lifts it over it.
    [TestMethod,
        DataRow("panel-overlay-inside", null),
        DataRow("panel-overlay-outside", 1400)
    ]
    public async Task Overlay_OpenedOverAPanelThatKeepsTheFocus_ClosesFirstAndThePanelClosesOnTheNextPress(string @case, int? zIndex)
    {
        await OpenPage(@case, zIndex: zIndex);

        await OpenOuter(Page.Locator("#btn-open-outer"));
        await OpenInner(Page.Locator("#btn-open-inner"), focusInnerInput: false);

        await Expect(Page.Locator("#btn-open-inner")).ToBeFocusedAsync();

        await AssertOneSurfaceClosesPerPress();
    }

    // The same Overlay left under the Panel's surface: the Panel is the one on top, so it closes first, and the Overlay
    // - the only surface left, with the focus handed back to the page outside of it - on the next press.
    [TestMethod]
    public async Task Overlay_OpenedUnderAPanelThatKeepsTheFocus_StaysOpenWhileThePanelClosesFirst()
    {
        await OpenPage("panel-overlay-outside");

        await OpenOuter(Page.Locator("#btn-open-outer"));
        await OpenInner(Page.Locator("#btn-open-inner"), focusInnerInput: false);

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#outer-state")).ToHaveTextAsync("closed");

        await AwaitDotNet();
        await Expect(Page.Locator("#inner-state")).ToHaveTextAsync("open");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#inner-state")).ToHaveTextAsync("closed");
    }

    // A blocking Overlay refuses the key, and the press does not fall through to the Panel underneath it either.
    [TestMethod,
        DataRow("panel-overlay-inside", null),
        DataRow("panel-overlay-outside", 1400)
    ]
    public async Task BlockingOverlay_OpenedOverAPanelThatKeepsTheFocus_KeepsThePanelOpen(string @case, int? zIndex)
    {
        await OpenPage(@case, blocking: true, zIndex: zIndex);

        await OpenOuter(Page.Locator("#btn-open-outer"));
        await OpenInner(Page.Locator("#btn-open-inner"), focusInnerInput: false);

        await Page.Keyboard.PressAsync("Escape");
        await AwaitDotNet();

        await Expect(Page.Locator("#inner-state")).ToHaveTextAsync("open");
        await Expect(Page.Locator("#outer-state")).ToHaveTextAsync("open");
    }

    // A dialog of the consumer's own answers no Escape of its own: the Overlay is its backdrop, and closes with it.
    [TestMethod]
    public async Task ConsumerDialog_InAnOverlay_ClosesAlongWithTheOverlayOnOnePress()
    {
        await OpenPage("overlay-consumer-dialog");

        await OpenOuter(Page.Locator("#btn-open-outer"));

        var input = Page.Locator("#inner-input");
        await input.FocusAsync();
        await Expect(input).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#outer-state")).ToHaveTextAsync("closed");
    }

    private async Task OpenPage(string @case, bool blocking = false, int? zIndex = null)
    {
        var query = $"case={@case}&blocking={(blocking ? "true" : "false")}{(zIndex is null ? "" : $"&zindex={zIndex}")}";

        await Page.GotoAsync($"{BaseUrl}/regression/nested-surface-escape?{query}");
        await WaitForStatus("Ready");
    }

    // A null surface id stands for a DropMenu, whose popup is the open callout.
    private async Task OpenOuter(ILocator opener, string? surfaceId = "outer-surface")
    {
        await opener.ClickAsync();
        await Expect(Page.Locator("#outer-state")).ToHaveTextAsync("open");
        await WaitForEscapeWatch(surfaceId);
    }

    // The press is made from a field of the inner surface, which is where a user typing in it would make it, unless the
    // inner surface is one that leaves the focus where it was: that one is opened from the keyboard, so the focus is on
    // its opener in every engine (WebKit does not focus a button that is clicked).
    private async Task OpenInner(ILocator opener, string? surfaceId = "inner-surface", bool focusInnerInput = true)
    {
        if (focusInnerInput)
        {
            await opener.ClickAsync();
        }
        else
        {
            await opener.FocusAsync();
            await Page.Keyboard.PressAsync("Enter");
        }

        await Expect(Page.Locator("#inner-state")).ToHaveTextAsync("open");
        await WaitForEscapeWatch(surfaceId);

        if (focusInnerInput is false) return;

        var input = Page.Locator("#inner-input");
        await input.FocusAsync();
        await Expect(input).ToBeFocusedAsync();
    }

    private async Task AssertOneSurfaceClosesPerPress()
    {
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#inner-state")).ToHaveTextAsync("closed");

        // Whatever the press asked .NET to close has been closed once a later round trip is back.
        await AwaitDotNet();
        await Expect(Page.Locator("#outer-state")).ToHaveTextAsync("open");

        // No focus is moved by hand here: the second press is made from wherever closing the inner surface left it.
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#outer-state")).ToHaveTextAsync("closed");
    }

    // The components start listening in calls to the browser of their own once they have rendered, which nothing on the
    // page reports, and a key pressed before they land is a key nothing was listening for yet. So the script is asked:
    // every surface marks the element it listens on - as a surface the claims stop at, or as an escape root - in the
    // same call that starts the listening (Utils.setupSurfaceEscape, setupEscapeGuard, setupEscape, watchEscape,
    // watchLayerEscape). A DropMenu's is its callout, the open one. The surface is also let finish moving in: a Panel
    // still sliding in has its content off the screen, where nothing - the user included - can see what is on top of it.
    private Task WaitForEscapeWatch(string? surfaceId) => Page.WaitForFunctionAsync(@"(id) => {
        const element = document.getElementById(id ?? BitBlazorUI.Callouts.current.calloutId);
        return !!element && (BitBlazorUI.Utils._escapeSurfaces.has(element) || !!element.__bitEscapeRoot)
            && element.getAnimations({ subtree: true }).every(a => a.playState !== 'running');
    }", surfaceId);

    // The surfaces answer the key through calls into .NET made while it is pressed - a Dialog's from a task queued
    // behind the dispatch - and the circuit takes the messages in order, so a click sent through it once those tasks
    // have run comes back rendered only after everything the press asked for. The button is clicked from the script,
    // which leaves the focus where it is.
    private async Task AwaitDotNet()
    {
        var count = int.Parse(await Page.Locator("#ping-count").TextContentAsync() ?? "0");

        await Page.EvaluateAsync("() => new Promise(resolve => setTimeout(() => { document.getElementById('btn-ping').click(); resolve(); }))");

        await Expect(Page.Locator("#ping-count")).ToHaveTextAsync($"{count + 1}");
    }
}
