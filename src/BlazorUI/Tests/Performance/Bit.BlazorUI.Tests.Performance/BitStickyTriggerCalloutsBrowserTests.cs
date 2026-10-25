using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for callouts whose trigger sits in a sticky header of a scrolled container (#13732).
///
/// An open callout is dismissed when the page scrolls under it, and a focus() scrolls its element into view - for a
/// sticky trigger, back to where its header sits in the flow, which Chromium and WebKit both do on the focus a click
/// asks for. A click on the trigger then scrolled the container and the scroll dismissed the callout it had just
/// opened. These pin both halves of the fix: a focus the components ask for after a click does not scroll, and a scroll
/// that leaves the trigger where it was does not dismiss its callout - while a scroll that moves it still does, and a
/// component opened through its API still brings its field into view before its callout is placed against it.
/// Run them with BROWSER=chromium, BROWSER=firefox and BROWSER=webkit.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitStickyTriggerCalloutsBrowserTests
public class BitStickyTriggerCalloutsBrowserTests : PerformanceTestBase
{
    // Whether the innermost open callout is still registered and on the screen.
    private const string IsCalloutOpen = @"() => {
        const current = BitBlazorUI.Callouts.current;
        if (!current.calloutId) return false;
        const callout = document.getElementById(current.calloutId);
        return callout != null && getComputedStyle(callout).display !== 'none';
    }";

    [TestMethod]
    [DataRow("dropmenu", ".bit-drm-btn", 40)]
    [DataRow("dropmenu", ".bit-drm-btn", 400)]
    [DataRow("breadcrumb", ".bit-brc-obt", 40)]
    [DataRow("breadcrumb", ".bit-brc-obt", 400)]
    [DataRow("dropdown", ".bit-drp-wrp", 40)]
    [DataRow("dropdown", ".bit-drp-wrp", 400)]
    [DataRow("dropdown-combo", ".bit-drp-wrp", 40)]
    [DataRow("dropdown-combo", ".bit-drp-wrp", 400)]
    [DataRow("daterangepicker", ".bit-dtrp-wrp", 40)]
    [DataRow("daterangepicker", ".bit-dtrp-wrp", 400)]
    [DataRow("datepicker", ".bit-dtp-wrp", 40)]
    [DataRow("datepicker", ".bit-dtp-wrp", 400)]
    [DataRow("timepicker", ".bit-tpc-wrp", 40)]
    [DataRow("timepicker", ".bit-tpc-wrp", 400)]
    [DataRow("circulartimepicker", ".bit-ctp-wrp", 40)]
    [DataRow("circulartimepicker", ".bit-ctp-wrp", 400)]
    [DataRow("phoneinput", "button.bit-phi-drp", 40)]
    [DataRow("phoneinput", "button.bit-phi-drp", 400)]
    [DataRow("phoneinput-search", "button.bit-phi-drp", 40)]
    [DataRow("phoneinput-search", "button.bit-phi-drp", 400)]
    [DataRow("callout", "#clo-anchor", 40)]
    [DataRow("callout", "#clo-anchor", 400)]
    public async Task ClickOnAStickyTrigger_OfAScrolledContainer_KeepsTheCalloutOpenAndTheContainerInPlace(string part, string trigger, int scrollTop)
    {
        await Open($"sticky-{part}");
        await ScrollTo(scrollTop);

        await ClickInPlace($"#hdr {trigger}");

        await ExpectCalloutOpen();

        // Every round trip of the open and the scroll event a focus would fire have had their turn by now.
        await Settle();

        Assert.IsTrue(await Page.EvaluateAsync<bool>(IsCalloutOpen), $"The callout of {part} was dismissed.");
        Assert.AreEqual(scrollTop, await ScrollTop(), 1, $"Opening {part} scrolled its container.");
    }

    [TestMethod]
    public async Task ScrollOfTheContainer_ThatLeavesAStickyTriggerInPlace_KeepsItsCalloutOpen()
    {
        await Open("sticky-dropmenu");
        await ScrollTo(400);

        await ClickInPlace("#hdr .bit-drm-btn");
        await ExpectCalloutOpen();

        // What a focus() of the app's own does to the container: back to where the header sits in the flow.
        await Page.EvaluateAsync("() => document.querySelector('#hdr .bit-drm-btn').scrollIntoView()");
        await Settle();

        Assert.IsTrue(await Page.EvaluateAsync<bool>(IsCalloutOpen));

        // And the user scrolling the content under the header.
        await ScrollTo(900);
        await Settle();

        Assert.IsTrue(await Page.EvaluateAsync<bool>(IsCalloutOpen));
    }

    [TestMethod]
    public async Task ScrollOfTheContainer_ThatMovesTheTrigger_StillDismissesTheCallout()
    {
        await Open("plain-dropmenu");

        await ClickInPlace("#hdr .bit-drm-btn");
        await ExpectCalloutOpen();

        await ScrollTo(100);

        await Page.WaitForFunctionAsync($"() => !({IsCalloutOpen})()", null, new() { Timeout = 5000 });
    }

    [TestMethod]
    [DataRow("autofocus-datepicker", "#btn-mount")]
    [DataRow("autofocus-daterangepicker", "#btn-mount")]
    [DataRow("autofocus-timepicker", "#btn-mount")]
    [DataRow("autofocus-circulartimepicker", "#btn-mount")]
    [DataRow("open-daterangepicker", "#btn-open")]
    [DataRow("isopen-phoneinput", "#btn-open")]
    [DataRow("openasync-phoneinput", "#btn-open")]
    public async Task OpenThroughTheApi_BelowTheFold_BringsTheFieldIntoViewAndKeepsTheCalloutOpen(string part, string button)
    {
        await Open(part);

        if (button == "#btn-open")
        {
            await Page.ClickAsync("#btn-mount");
            await Page.WaitForSelectorAsync("#target > *");
        }

        await Page.ClickAsync(button);

        await ExpectCalloutOpen();
        await Settle();

        Assert.IsTrue(await Page.EvaluateAsync<bool>(IsCalloutOpen), $"The callout of {part} was dismissed.");

        // The focus went to the field, which brought it into the visible part of the container.
        Assert.IsTrue(await Page.EvaluateAsync<bool>(@"() => {
            const target = document.getElementById('target');
            const active = document.activeElement;
            return target.contains(active) || BitBlazorUI.Callouts.calloutContains(active);
        }"), $"The focus of {part} is not on the field.");

        Assert.IsTrue(await Page.EvaluateAsync<bool>(@"() => {
            const scroller = document.getElementById('scroller').getBoundingClientRect();
            const field = document.getElementById('target').getBoundingClientRect();
            return field.bottom > scroller.top && field.top < scroller.bottom;
        }"), $"The field of {part} was left out of view.");
    }

    private async Task Open(string part)
    {
        await Page.GotoAsync($"{BaseUrl}/regression/sticky-trigger-callouts?part={part}");
        await WaitForStatus("Ready");
    }

    // A press where the trigger is on the screen, as a user's: a locator's click scrolls its element into view first,
    // honouring the container's scroll padding, which is the very scroll these tests have to tell apart.
    private async Task ClickInPlace(string selector)
    {
        var box = await Page.Locator(selector).First.BoundingBoxAsync();
        Assert.IsNotNull(box, $"{selector} is not on the page.");

        await Page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
    }

    private Task ExpectCalloutOpen()
    {
        return Page.WaitForFunctionAsync(IsCalloutOpen, null, new() { Timeout = 5000 });
    }

    private Task ScrollTo(int top)
    {
        return Page.EvaluateAsync(@"top => new Promise(resolve => {
            document.getElementById('scroller').scrollTop = top;
            requestAnimationFrame(() => requestAnimationFrame(resolve));
        })", top);
    }

    private Task<double> ScrollTop()
    {
        return Page.EvaluateAsync<double>("() => document.getElementById('scroller').scrollTop");
    }

    private Task Settle()
    {
        return Page.WaitForTimeoutAsync(600);
    }
}
