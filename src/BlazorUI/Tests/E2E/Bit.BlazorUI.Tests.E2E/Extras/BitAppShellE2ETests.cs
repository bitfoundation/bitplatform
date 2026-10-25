using System.Globalization;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// End-to-end tests of BitAppShell on /e2e/app-shell and /e2e/app-shell/nav-a|nav-b: the main container as the one
/// region that scrolls (with a sticky header kept in place), the four safe area bars sized from the public variables and
/// taken away one by one or all at once (StableInsets, RTL), the keyboard inset (a simulated on-screen keyboard through
/// the visual viewport: the published variable, the marker attribute, OnKeyboardInsetChanged and the focused field
/// brought back into view), the scrolling API (GoToTop / GoToBottom / ScrollTo / ScrollBy / ScrollToElement /
/// GetScrollOffset, ScrollBehavior), the scroll callbacks, ScrollThrottle and the scroll state attributes, the horizontal edges,
/// OverflowX / OverflowY / NoScroll / Gutter / Overscroll, ScrollPadding, AutoScroll / PreserveScroll / Refresh, the
/// cascaded values and the container cascade, BitParams, Style / Class / Styles / Classes, the public CSS variables,
/// the base attributes, Disabled (the content stays live), inherited RTL, the well-known container id, reduced motion,
/// unmounting, FullScreen, print, the scrolling keys handed to the shell, and the
/// navigation features: PersistScroll in both ScrollRestoration modes (across a reload too), a query or fragment change
/// left alone, AutoGoToTop and ClearPersistedScroll.
/// </summary>
[TestClass]
public class BitAppShellE2ETests : E2ETestBase
{
    private const string Route = "/e2e/app-shell";
    private const string NavRoute = "/e2e/app-shell/nav-a";

    [TestMethod]
    public async Task Prerender_TheServerHtmlCarriesTheShellAndItsContainer()
    {
        var html = await (await Page.APIRequest.GetAsync($"{BaseUrl}{Route}")).TextAsync();

        StringAssert.Contains(html, "id=\"ash-basic\"");
        StringAssert.Contains(html, "id=\"ash-basic-container\"");
        StringAssert.Contains(html, "data-bit-ash-main");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Route);
        await Expect(Page.Locator("#ash-basic")).ToHaveClassAsync(HasClass("bit-ash"));
    }

    [TestMethod]
    public async Task Basic_TheMainContainerIsTheRegionThatScrolls_AndAStickyHeaderStaysPut()
    {
        await OpenAsync(Route);

        var main = Main("ash-basic");
        await Expect(main).ToHaveAttributeAsync("id", "ash-basic-container");
        await Expect(main).ToHaveClassAsync(HasClass("bit-ash-main"));
        await Expect(main).ToHaveCSSAsync("overflow-y", "auto");
        await Expect(Page.Locator("#ash-basic")).ToHaveCSSAsync("background-color", "rgb(255, 255, 255)");

        var size = await main.EvaluateAsync<double[]>("el => [el.scrollHeight, el.clientHeight]");
        Assert.AreEqual(200, size[1]);
        Assert.IsTrue(size[0] > size[1]);

        var windowScroll = await Page.EvaluateAsync<double>("() => window.scrollY");
        await main.EvaluateAsync("el => el.scrollTop = 300");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 300);

        // The document did not move, and the header is still at the top of the container.
        Assert.AreEqual(windowScroll, await Page.EvaluateAsync<double>("() => window.scrollY"));
        var head = await Page.Locator("#basic-head").BoundingBoxAsync();
        var box = await main.BoundingBoxAsync();
        Assert.AreEqual(box!.Y, head!.Y, 1);
    }

    [TestMethod]
    public async Task Insets_TheFourBarsAreSizedFromThePublicVariables_AndPaintedFromTheirOwn()
    {
        await OpenAsync(Route);

        await Expect(Bar("ash-insets", "top")).ToHaveCSSAsync("height", "20px");
        await Expect(Bar("ash-insets", "bottom")).ToHaveCSSAsync("height", "30px");
        await Expect(Bar("ash-insets", "left")).ToHaveCSSAsync("width", "10px");
        await Expect(Bar("ash-insets", "right")).ToHaveCSSAsync("width", "15px");
        await Expect(Main("ash-insets")).ToHaveCSSAsync("width", "295px");
        await Expect(Page.Locator("#ash-insets .bit-ash-center")).ToHaveCSSAsync("height", "150px");

        await Expect(Bar("ash-insets", "top")).ToHaveCSSAsync("background-color", "rgb(0, 128, 0)");
        await Expect(Bar("ash-insets", "bottom")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
        await Expect(Bar("ash-insets", "left")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");

        // What the shell measured is published on its root.
        Assert.AreEqual("20px", (await ComputedStyleAsync("#ash-insets", "--bit-ash-inset-top")).Trim());
        Assert.AreEqual("15px", (await ComputedStyleAsync("#ash-insets", "--bit-ash-inset-end")).Trim());
    }

    [TestMethod]
    [DataRow("btn-no-top", "bit-ash-nit", "top", "height")]
    [DataRow("btn-no-bottom", "bit-ash-nib", "bottom", "height")]
    [DataRow("btn-no-start", "bit-ash-nis", "left", "width")]
    [DataRow("btn-no-end", "bit-ash-nie", "right", "width")]
    public async Task Insets_EachEdgeCanBeTakenAwayOnItsOwn(string button, string cssClass, string bar, string property)
    {
        await OpenAsync(Route);

        await Page.Locator($"#{button}").ClickAsync();

        await Expect(Page.Locator("#ash-insets")).ToHaveClassAsync(HasClass(cssClass));
        await Expect(Bar("ash-insets", bar)).ToHaveCSSAsync(property, "0px");

        // The other three stay where they are.
        var others = new[] { ("top", "height", "20px"), ("bottom", "height", "30px"), ("left", "width", "10px"), ("right", "width", "15px") };
        foreach (var (other, otherProperty, value) in others.Where(o => o.Item1 != bar))
        {
            await Expect(Bar("ash-insets", other)).ToHaveCSSAsync(otherProperty, value);
        }

        await Page.Locator($"#{button}").ClickAsync();
        await Expect(Page.Locator("#ash-insets")).Not.ToHaveClassAsync(HasClass(cssClass));
    }

    [TestMethod]
    public async Task Insets_NoInsetsRemovesAllFour_StableInsetsKeepsAnEdgeTakenAway_AndRtlSwapsTheSides()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-no-insets").ClickAsync();
        await Expect(Page.Locator("#ash-insets")).ToHaveClassAsync(HasClass("bit-ash-nin"));
        await Expect(Bar("ash-insets", "top")).ToHaveCSSAsync("height", "0px");
        await Expect(Bar("ash-insets", "bottom")).ToHaveCSSAsync("height", "0px");
        await Expect(Bar("ash-insets", "left")).ToHaveCSSAsync("width", "0px");
        await Expect(Bar("ash-insets", "right")).ToHaveCSSAsync("width", "0px");
        await Expect(Main("ash-insets")).ToHaveCSSAsync("width", "320px");
        await Page.Locator("#btn-no-insets").ClickAsync();

        await Page.Locator("#btn-stable").ClickAsync();
        await Page.Locator("#btn-no-top").ClickAsync();
        await Expect(Page.Locator("#ash-insets")).ToHaveClassAsync(HasClass("bit-ash-sin"));
        await Expect(Bar("ash-insets", "top")).ToHaveCSSAsync("height", "0px");
        await Expect(Bar("ash-insets", "bottom")).ToHaveCSSAsync("height", "30px");
        await Page.Locator("#btn-no-top").ClickAsync();

        await Page.Locator("#btn-insets-rtl").ClickAsync();
        await Expect(Page.Locator("#ash-insets")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Page.Locator("#ash-insets .bit-ash-center")).ToHaveAttributeAsync("dir", "rtl");

        // The start bar (10px) is the physically right one in a right-to-left shell.
        var start = await Bar("ash-insets", "left").BoundingBoxAsync();
        var end = await Bar("ash-insets", "right").BoundingBoxAsync();
        Assert.IsTrue(start!.X > end!.X, $"start bar at {start.X}, end bar at {end.X}");
        Assert.AreEqual(10, start.Width, 0.5);
        Assert.AreEqual(15, end.Width, 0.5);
    }

    [TestMethod]
    public async Task Keyboard_AnOnScreenKeyboard_IsTakenOffTheShell_ReportedAndMarked()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ash-keyboard");
        await Expect(root).ToHaveAttributeAsync("data-bit-ash-kbd", new System.Text.RegularExpressions.Regex(".+"));
        await Expect(Page.Locator("#keyboard-inset")).ToHaveTextAsync("0");

        // The measurement taken as the tracking is set up is reported once, as 0.
        await Expect(Page.Locator("#keyboard-inset-count")).ToHaveTextAsync("1");
        var reportsBefore = int.Parse((await Page.Locator("#keyboard-inset-count").TextContentAsync())!, CultureInfo.InvariantCulture);

        await Page.Locator("#keyboard-input").FocusAsync();
        await SimulateKeyboardAsync(150);

        await Expect(root).ToHaveAttributeAsync("data-bit-ash-keyboard", "");
        await Expect(Page.Locator("#keyboard-inset")).ToHaveTextAsync("150");
        Assert.AreEqual("150px", (await ComputedStyleAsync("#ash-keyboard", "--bit-ash-keyboard-inset")).Trim());
        await Expect(Page.Locator("#ash-keyboard .bit-ash-center")).ToHaveCSSAsync("height", "50px");

        // The focused field, left below the shorter middle, is scrolled back into view inside the container.
        await Expect(Main("ash-keyboard")).Not.ToHaveJSPropertyAsync("scrollTop", 0);
        var input = await Page.Locator("#keyboard-input").BoundingBoxAsync();
        var main = await Main("ash-keyboard").BoundingBoxAsync();
        Assert.IsTrue(input!.Y + input.Height <= main!.Y + main.Height + 1, "The focused field stayed below the container.");

        await SimulateKeyboardAsync(0);

        await Expect(root).Not.ToHaveAttributeAsync("data-bit-ash-keyboard", "");
        await Expect(Page.Locator("#keyboard-inset")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#keyboard-inset-count")).ToHaveTextAsync((reportsBefore + 2).ToString(CultureInfo.InvariantCulture));
        await Expect(Page.Locator("#ash-keyboard .bit-ash-center")).ToHaveCSSAsync("height", "200px");

        // Turned off at runtime, the shell stops measuring.
        await Page.Locator("#btn-avoid-keyboard").ClickAsync();
        await Expect(root).Not.ToHaveAttributeAsync("data-bit-ash-kbd", new System.Text.RegularExpressions.Regex(".*"));

        await SimulateKeyboardAsync(150);
        await NextFramesAsync();
        await Expect(root).Not.ToHaveAttributeAsync("data-bit-ash-keyboard", "");
        await Expect(Page.Locator("#keyboard-inset")).ToHaveTextAsync("0");
        await SimulateKeyboardAsync(0);
    }

    [TestMethod]
    public async Task ScrollApi_EveryMethodMovesTheContainer_AndGetScrollOffsetReadsIt()
    {
        await OpenAsync(Route);

        var main = Main("ash-scroll");
        await Expect(Page.Locator("#scroll-container-id")).ToHaveTextAsync("ash-scroll-container");
        await Expect(Page.Locator("#scroll-container-ref")).ToHaveTextAsync("set");

        await Page.Locator("#btn-instant").ClickAsync();
        await Expect(main).Not.ToHaveClassAsync(HasClass("bit-ash-smt"));
        await Expect(main).ToHaveCSSAsync("scroll-behavior", "auto");

        await Page.Locator("#btn-go-bottom").ClickAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 1000);

        await Page.Locator("#btn-scroll-to").ClickAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 240);

        await Page.Locator("#btn-scroll-by").ClickAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 360);

        // A NaN distance is dropped rather than thrown.
        await Page.Locator("#btn-scroll-by-nan").ClickAsync();
        await NextFramesAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 360);

        await Page.Locator("#btn-go-top").ClickAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 0);

        await Page.Locator("#btn-scroll-to-element").ClickAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 760);

        await Page.Locator("#btn-scroll-to-element-offset").ClickAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 720);

        await Page.Locator("#btn-scroll-to-element-end").ClickAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 600);

        await Page.Locator("#btn-get-offset").ClickAsync();
        await Expect(Page.Locator("#scroll-offset")).ToHaveTextAsync("600|1000|False|False|200");

        await Page.Locator("#btn-scroll-to-element-center").ClickAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 680);

        await Page.Locator("#btn-go-top").ClickAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 0);
        await Page.Locator("#btn-get-offset").ClickAsync();
        await Expect(Page.Locator("#scroll-offset")).ToHaveTextAsync("0|1000|True|False|200");
    }

    [TestMethod]
    public async Task ScrollApi_TheDefaultBehaviorIsSmooth_AndAMoveStillArrives()
    {
        await OpenAsync(Route);

        var main = Main("ash-scroll");
        await Expect(main).ToHaveClassAsync(HasClass("bit-ash-smt"));
        await Expect(main).ToHaveCSSAsync("scroll-behavior", "smooth");

        await Page.Locator("#btn-go-bottom").ClickAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 1000);

        // A behavior handed to the call wins over the shell's.
        await Page.Locator("#btn-go-top").ClickAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 0);
        await Page.Locator("#btn-go-bottom-instant").ClickAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 1000);
    }

    [TestMethod]
    public async Task ScrollEvents_TheCallbacksReportTheReadersScrolling_AndTheRootCarriesTheScrollState()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ash-events");
        var main = Main("ash-events");
        await WaitForShellAsync("ash-events");
        await Expect(root).Not.ToHaveAttributeAsync("data-bit-ash-scrolled", "");

        // A few notches rather than one long fling: a move of more than a screenful in one frame is read as a jump.
        await main.HoverAsync();
        for (var i = 0; i < 3; i++)
        {
            await Page.Mouse.WheelAsync(0, 100);
            await NextFramesAsync();
        }

        await Expect(main).Not.ToHaveJSPropertyAsync("scrollTop", 0);
        await Expect(root).ToHaveAttributeAsync("data-bit-ash-scrolled", "");
        await Expect(root).ToHaveAttributeAsync("data-bit-ash-scroll-direction", "down");
        await Expect(Page.Locator("#events-direction")).ToHaveTextAsync("down");
        await Expect(Page.Locator("#events-scroll-count")).Not.ToHaveTextAsync("0");
        await Expect(Page.Locator("#events-start-count")).Not.ToHaveTextAsync("0");

        await WaitForScrollStillAsync(main);
        await Page.Mouse.WheelAsync(0, -60);
        await NextFramesAsync();
        await Page.Mouse.WheelAsync(0, -60);
        await Expect(root).ToHaveAttributeAsync("data-bit-ash-scroll-direction", "up");
        await Expect(Page.Locator("#events-direction")).ToHaveTextAsync("up");

        // Within ReachOffset (16px) of the end counts as having reached it - once per arrival. The wheel's own smooth
        // scroll is let finish first, since it would carry on over a position set in the middle of it.
        await WaitForScrollStillAsync(main);
        var bottomBefore = await Page.Locator("#events-bottom-count").TextContentAsync();
        await main.EvaluateAsync("el => el.scrollTop = el.scrollHeight - el.clientHeight - 10");
        await Expect(Page.Locator("#events-bottom-count")).Not.ToHaveTextAsync(bottomBefore!);
        await Expect(Page.Locator("#events-percent")).Not.ToHaveTextAsync("-");

        var topBefore = await Page.Locator("#events-top-count").TextContentAsync();
        await main.EvaluateAsync("el => el.scrollTop = 0");
        await Expect(Page.Locator("#events-top-count")).Not.ToHaveTextAsync(topBefore!);
        await Expect(root).Not.ToHaveAttributeAsync("data-bit-ash-scrolled", "");
        await Expect(root).Not.ToHaveAttributeAsync("data-bit-ash-scroll-direction", new System.Text.RegularExpressions.Regex(".*"));
        await Expect(Page.Locator("#events-percent")).ToHaveTextAsync("0.00");
    }

    [TestMethod]
    public async Task ScrollEnd_AScrollThatComesToAStop_IsReportedWithWhereItStopped()
    {
        await OpenAsync(Route);

        var main = Main("ash-events");
        await WaitForShellAsync("ash-events");

        // A smooth move over several frames, which every engine closes with one end. (The wheel the tests can
        // synthesize is not closed with a scrollend by Firefox and WebKit, which OnScrollEnd follows where it exists.)
        await main.EvaluateAsync("el => el.scrollTo({ top: 300, behavior: 'smooth' })");

        await Expect(Page.Locator("#events-end-count")).Not.ToHaveTextAsync("0");
        await Expect(Page.Locator("#events-end-top")).ToHaveTextAsync("300");
        await Expect(Page.Locator("#events-start-count")).Not.ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task ScrollThrottle_ReportsTheScrollingNoMoreOftenThanAsked_AndAlwaysTheLastPosition()
    {
        await OpenAsync(Route);

        var main = Main("ash-throttle");
        await WaitForShellAsync("ash-throttle");

        // Twenty moves, one per frame - about a third of a second, well within one 1000ms interval.
        await main.EvaluateAsync(@"el => new Promise(resolve => {
            let i = 0;
            const step = () => { el.scrollTop = (++i) * 20; if (i < 20) requestAnimationFrame(step); else resolve(); };
            requestAnimationFrame(step);
        })");

        var count = int.Parse((await Page.Locator("#throttle-count").TextContentAsync())!, CultureInfo.InvariantCulture);
        Assert.IsTrue(count <= 2, $"{count} reports were made inside one throttle interval.");

        // The last position always follows once the interval is up.
        await Expect(Page.Locator("#throttle-top")).ToHaveTextAsync("400");
        var final = int.Parse((await Page.Locator("#throttle-count").TextContentAsync())!, CultureInfo.InvariantCulture);
        Assert.IsTrue(final <= 3, $"{final} reports for one burst of scrolling.");
    }

    [TestMethod]
    public async Task HorizontalEdges_ReachingTheLeftAndRightOfTheContent_IsReported()
    {
        await OpenAsync(Route);

        var main = Main("ash-wide");
        await WaitForShellAsync("ash-wide");
        var rightBefore = await Page.Locator("#wide-right-count").TextContentAsync();

        await main.EvaluateAsync("el => el.scrollLeft = el.scrollWidth");
        await Expect(Page.Locator("#wide-right-count")).Not.ToHaveTextAsync(rightBefore!);

        var leftBefore = await Page.Locator("#wide-left-count").TextContentAsync();
        await main.EvaluateAsync("el => el.scrollLeft = 0");
        await Expect(Page.Locator("#wide-left-count")).Not.ToHaveTextAsync(leftBefore!);
    }

    [TestMethod]
    public async Task Overflow_EachAxisAndTheGutter_AreWrittenOntoTheContainer()
    {
        await OpenAsync(Route);

        var main = Main("ash-overflow");
        await Expect(main).ToHaveCSSAsync("overflow-x", "auto");

        await Page.Locator("#btn-overflow-x").ClickAsync();
        await Expect(main).ToHaveCSSAsync("overflow-x", "hidden");
        await Expect(main).ToHaveCSSAsync("overflow-y", "auto");

        await Page.Locator("#btn-overflow-y").ClickAsync();
        await Expect(main).ToHaveCSSAsync("overflow-y", "scroll");

        await Page.Locator("#btn-gutter-stable").ClickAsync();
        await Expect(main).ToHaveCSSAsync("scrollbar-gutter", "stable");
        await Page.Locator("#btn-gutter-both").ClickAsync();
        await Expect(main).ToHaveCSSAsync("scrollbar-gutter", "stable both-edges");
    }

    [TestMethod]
    public async Task Overscroll_NoneByDefault_AndEveryValueIsWrittenOntoTheContainer()
    {
        await OpenAsync(Route);

        var main = Main("ash-overflow");
        await Expect(main).Not.ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("overscroll"));

        foreach (var value in new[] { "contain", "auto", "none" })
        {
            await Page.Locator($"#btn-overscroll-{value}").ClickAsync();
            await Expect(main).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex($"overscroll-behavior:{value}"));
        }

        if (await Page.EvaluateAsync<bool>("() => CSS.supports('overscroll-behavior', 'none')") is false)
        {
            Assert.Inconclusive("This engine does not implement overscroll-behavior, so its computed value cannot be read back.");
        }

        await Expect(main).ToHaveCSSAsync("overscroll-behavior-y", "none");
        await Expect(Main("ash-basic")).ToHaveCSSAsync("overscroll-behavior-y", "none");
        await Page.Locator("#btn-overscroll-contain").ClickAsync();
        await Expect(main).ToHaveCSSAsync("overscroll-behavior-y", "contain");
        await Page.Locator("#btn-overscroll-auto").ClickAsync();
        await Expect(main).ToHaveCSSAsync("overscroll-behavior-y", "auto");
    }

    [TestMethod]
    [DataRow("auto")]
    [DataRow("hidden")]
    [DataRow("scroll")]
    [DataRow("visible")]
    public async Task Overflow_EveryValueOfEachAxis_IsWrittenAsItsOwnLonghand(string value)
    {
        await OpenAsync(Route);

        var main = Main("ash-overflow");

        await Page.Locator($"#btn-ox-{value}").ClickAsync();
        await Expect(main).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex($"overflow-x:{value}"));
        await Expect(main).Not.ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("overflow-y"));

        await Page.Locator($"#btn-oy-{value}").ClickAsync();
        await Expect(main).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex($"overflow-y:{value}"));

        // Both axes given the same value is a pair CSS keeps as written.
        await Expect(main).ToHaveCSSAsync("overflow-x", value);
        await Expect(main).ToHaveCSSAsync("overflow-y", value);

        await Page.Locator("#btn-gutter-auto").ClickAsync();
        await Expect(main).ToHaveCSSAsync("scrollbar-gutter", "auto");
        await Expect(main).Not.ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("scrollbar-gutter"));
    }

    [TestMethod]
    public async Task NoScroll_TheReaderCannotScroll_ButTheApiStillMovesTheContainer()
    {
        await OpenAsync(Route);

        var main = Main("ash-overflow");

        // NoScroll wins over an axis asked for explicitly.
        await Page.Locator("#btn-overflow-y").ClickAsync();
        await Page.Locator("#btn-no-scroll").ClickAsync();

        await Expect(main).ToHaveClassAsync(HasClass("bit-ash-nsc"));
        await Expect(main).ToHaveCSSAsync("overflow-y", "hidden");
        await Expect(main).ToHaveCSSAsync("overflow-x", "hidden");

        await main.HoverAsync();
        await Page.Mouse.WheelAsync(0, 200);
        await NextFramesAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 0);

        await Page.Locator("#btn-overflow-scroll-by").ClickAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 80);
    }

    [TestMethod]
    public async Task ScrollPadding_KeepsWhatIsScrolledToOutFromUnderTheStickyHeader()
    {
        await OpenAsync(Route);

        var main = Main("ash-padding");

        await Page.Locator("#btn-padding-scroll").ClickAsync();
        await Expect(main).Not.ToHaveJSPropertyAsync("scrollTop", 0);
        Assert.AreEqual(0, await RowOffsetAsync(), 1.5);

        await Page.Locator("#btn-padding").ClickAsync();
        await Expect(main).ToHaveCSSAsync("scroll-padding-top", "40px");

        await main.EvaluateAsync("el => el.scrollTop = 0");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 0);
        await Page.Locator("#btn-padding-scroll").ClickAsync();
        await Expect(main).Not.ToHaveJSPropertyAsync("scrollTop", 0);

        // The row now comes to rest below the 40px header rather than under it.
        Assert.AreEqual(40, await RowOffsetAsync(), 1.5);

        async Task<double> RowOffsetAsync()
        {
            await NextFramesAsync();
            var row = await Page.Locator("#padded-row").BoundingBoxAsync();
            var box = await main.BoundingBoxAsync();
            return row!.Y - box!.Y;
        }
    }

    [TestMethod]
    public async Task Endless_AutoScrollPinsToNewContent_OnlyWhileTheReaderIsAtTheEnd()
    {
        await OpenAsync(Route);

        var main = Main("ash-feed");

        await Page.Locator("#btn-auto-scroll").ClickAsync();
        await Expect(Page.Locator("#auto-scroll")).ToHaveTextAsync("True");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 280);

        await Page.Locator("#btn-append").ClickAsync();
        await Expect(Page.Locator("#feed-count")).ToHaveTextAsync("13");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 320);

        // A reader who scrolled up is not dragged back down.
        await main.EvaluateAsync("el => el.scrollTop = 100");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 100);
        await Page.Locator("#btn-append").ClickAsync();
        await Expect(Page.Locator("#feed-count")).ToHaveTextAsync("14");
        await NextFramesAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 100);

        // Back at the end, the pinning takes up again.
        await main.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Page.Locator("#btn-append").ClickAsync();
        await Expect(Page.Locator("#feed-count")).ToHaveTextAsync("15");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 400);

        await Page.Locator("#btn-refresh").ClickAsync();
        await NextFramesAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 400);
    }

    [TestMethod]
    public async Task Endless_PreserveScroll_KeepsTheReadersPlaceWhenContentArrivesAbove()
    {
        await OpenAsync(Route);

        var main = Main("ash-feed");

        await Page.Locator("#btn-preserve").ClickAsync();
        await Expect(Page.Locator("#preserve-scroll")).ToHaveTextAsync("True");

        await main.EvaluateAsync("el => el.scrollTop = 120");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 120);
        var before = (await Page.Locator("#feed-Message-5").BoundingBoxAsync())!.Y;

        await Page.Locator("#btn-prepend").ClickAsync();
        await Expect(Page.Locator("#feed-count")).ToHaveTextAsync("17");

        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 320);
        await NextFramesAsync();
        Assert.AreEqual(before, (await Page.Locator("#feed-Message-5").BoundingBoxAsync())!.Y, 1);
    }

    [TestMethod]
    public async Task Values_TheShellCascadesItsValuesAndItsContainer_ToEverythingInside()
    {
        await OpenAsync(Route);

        // Values win over ValueList for the same name.
        await Expect(Page.Locator("#cascade-reader")).ToHaveTextAsync("Saleh|Admin|Acme|From the list|container");
        await Expect(Page.Locator("#outside-reader")).ToHaveTextAsync("-|no-container");

        await Page.Locator("#btn-rename").ClickAsync();
        await Expect(Page.Locator("#cascade-reader")).ToHaveTextAsync("Yaser|Admin|Acme|From the list|container");
    }

    [TestMethod]
    public async Task Params_BitParamsSetsTheDefaults_AndAShellsOwnValueWins()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#ash-params-1")).ToHaveClassAsync(HasClass("cascaded-root"));
        await Expect(Bar("ash-params-1", "bottom")).ToHaveCSSAsync("height", "0px");
        await Expect(Bar("ash-params-1", "top")).ToHaveCSSAsync("height", "8px");
        await Expect(Main("ash-params-1")).ToHaveCSSAsync("scroll-padding-top", "11px");

        await Expect(Page.Locator("#ash-params-2")).ToHaveClassAsync(HasClass("own-root"));
        await Expect(Page.Locator("#ash-params-2")).Not.ToHaveClassAsync(HasClass("cascaded-root"));
        await Expect(Bar("ash-params-2", "bottom")).ToHaveCSSAsync("height", "12px");
        await Expect(Main("ash-params-2")).ToHaveCSSAsync("scroll-padding-top", "11px");
    }

    [TestMethod]
    public async Task StyleAndClass_ReachTheRoot_AndStylesAndClassesReachEveryPart()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ash-styled");
        await Expect(root).ToHaveClassAsync(HasClass("custom-root-class"));
        await Expect(root).ToHaveClassAsync(HasClass("c-root"));
        await Expect(root).ToHaveCSSAsync("outline-style", "dashed");
        await Expect(root).ToHaveCSSAsync("padding-top", "3px");

        await Expect(Bar("ash-styled", "top")).ToHaveClassAsync(HasClass("c-top"));
        await Expect(Bar("ash-styled", "top")).ToHaveCSSAsync("min-height", "4px");
        await Expect(Page.Locator("#ash-styled .bit-ash-center")).ToHaveClassAsync(HasClass("c-center"));
        await Expect(Page.Locator("#ash-styled .bit-ash-center")).ToHaveCSSAsync("opacity", "0.9");
        await Expect(Bar("ash-styled", "left")).ToHaveClassAsync(HasClass("c-left"));
        await Expect(Bar("ash-styled", "left")).ToHaveCSSAsync("min-width", "5px");
        await Expect(Main("ash-styled")).ToHaveClassAsync(HasClass("c-main"));
        await Expect(Main("ash-styled")).ToHaveCSSAsync("padding-top", "6px");
        await Expect(Bar("ash-styled", "right")).ToHaveClassAsync(HasClass("c-right"));
        await Expect(Bar("ash-styled", "right")).ToHaveCSSAsync("min-width", "7px");
        await Expect(Bar("ash-styled", "bottom")).ToHaveClassAsync(HasClass("c-bottom"));
        await Expect(Bar("ash-styled", "bottom")).ToHaveCSSAsync("min-height", "8px");
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_PaintTheShellAndItsText()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#ash-vars")).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(Page.Locator("#vars-row")).ToHaveCSSAsync("color", "rgb(250, 240, 230)");
        await Expect(Bar("ash-vars", "top")).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
    }

    [TestMethod]
    public async Task Base_ANamedShellIsAGroup_AndAttributesTabIndexAndVisibilityReachTheRoot()
    {
        await OpenAsync(Route);

        var named = Page.Locator("#ash-aria");
        await Expect(named).ToHaveAttributeAsync("role", "group");
        await Expect(named).ToHaveAttributeAsync("aria-label", "Application");
        await Expect(named).ToHaveAttributeAsync("tabindex", "0");
        await Expect(named).ToHaveAttributeAsync("data-testid", "shell-attrs");
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Application" })).ToHaveCountAsync(1);

        await Expect(Page.Locator("#ash-unnamed")).Not.ToHaveAttributeAsync("role", new System.Text.RegularExpressions.Regex(".*"));

        await Expect(Page.Locator("#ash-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Page.Locator("#ash-collapsed")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task ReducedMotion_TurnsSmoothScrollingOff_UnlessForceAnimationIsSet()
    {
        await OpenAsync(Route);

        await Expect(Main("ash-smooth")).ToHaveCSSAsync("scroll-behavior", "smooth");
        await Expect(Page.Locator("#ash-forced")).ToHaveClassAsync(HasClass("bit-fam"));

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        await Expect(Main("ash-smooth")).ToHaveCSSAsync("scroll-behavior", "auto");
        await Expect(Main("ash-forced")).ToHaveCSSAsync("scroll-behavior", "smooth");
    }

    [TestMethod]
    public async Task Disabled_TheShellIsMarked_ButItsContentStaysLive()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ash-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));

        // A layout keeps the page's handlers while disabled: what bubbles up to it is an enabled control's own event.
        await Page.Locator("#disabled-shell-button").ClickAsync();

        await Expect(Page.Locator("#disabled-shell-button-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#disabled-shell-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_PutsTheStartBarOnTheRight()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#ash-rtl-inherited")).Not.ToHaveClassAsync(HasClass("bit-rtl"));

        var start = await Bar("ash-rtl-inherited", "left").BoundingBoxAsync();
        var end = await Bar("ash-rtl-inherited", "right").BoundingBoxAsync();
        Assert.IsTrue(start!.X > end!.X, $"start bar at {start.X}, end bar at {end.X}");
        Assert.AreEqual(10, start.Width, 0.5);
    }

    [TestMethod]
    public async Task ContainerId_AShellWithoutAnIdOfItsOwn_UsesTheWellKnownContainerId()
    {
        await OpenAsync(Route);

        var container = Page.Locator("#BitAppShell-container");
        await Expect(container).ToHaveCountAsync(1);
        await Expect(container).ToHaveAttributeAsync("data-bit-ash-main", "");
        await Expect(container.Locator("#default-id-row")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Runtime_AShellWithEveryBrowserFeatureOn_UnmountsWithoutAnError()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#ash-runtime")).ToHaveAttributeAsync("data-bit-ash-kbd", new System.Text.RegularExpressions.Regex(".+"));

        await Page.Locator("#btn-mount").ClickAsync();
        await Expect(Page.Locator("#runtime-mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#ash-runtime")).ToHaveCountAsync(0);

        await Page.Locator("#btn-mount").ClickAsync();
        await Expect(Page.Locator("#ash-runtime")).ToHaveCountAsync(1);
        await Expect(Main("ash-runtime")).Not.ToHaveJSPropertyAsync("scrollTop", 0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task FullScreen_PinsTheShellToTheViewport_AndPrintsAtTheLengthOfItsContent()
    {
        await OpenAsync(NavRoute);

        var root = Page.Locator("#ash-nav");
        await Expect(root).ToHaveClassAsync(HasClass("bit-ash-fsc"));
        await Expect(root).ToHaveCSSAsync("position", "fixed");

        var box = await root.BoundingBoxAsync();
        var viewport = Page.ViewportSize!;
        Assert.AreEqual(0, box!.X, 0.5);
        Assert.AreEqual(0, box.Y, 0.5);
        Assert.AreEqual(viewport.Width, box.Width, 0.5);
        Assert.AreEqual(viewport.Height, box.Height, 0.5);

        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        await Expect(root).ToHaveCSSAsync("position", "static");
        await Expect(Main("ash-nav")).ToHaveCSSAsync("overflow-y", "visible");
        await Expect(Bar("ash-nav", "top")).ToHaveCSSAsync("display", "none");

        await Page.EmulateMediaAsync(new() { Media = Media.Screen });
        await Expect(root).ToHaveCSSAsync("position", "fixed");
    }

    [TestMethod]
    public async Task Keys_WhileNothingHasFocus_TheScrollingKeysScrollTheShell()
    {
        await OpenAsync(NavRoute);

        var main = Main("ash-nav");
        var client = await main.EvaluateAsync<double>("el => el.clientHeight");
        var max = await main.EvaluateAsync<double>("el => el.scrollHeight - el.clientHeight");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 40);

        await Page.Keyboard.PressAsync("PageDown");
        await WaitForScrollTopAsync(main, 40 + Math.Max(40, client * 0.875));

        await Page.Keyboard.PressAsync("End");
        await WaitForScrollTopAsync(main, max);

        await Page.Keyboard.PressAsync("Home");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 0);

        await Page.Keyboard.PressAsync("Space");
        await WaitForScrollTopAsync(main, Math.Max(40, client * 0.875));

        await Page.Keyboard.PressAsync("ArrowUp");
        await WaitForScrollTopAsync(main, Math.Max(40, client * 0.875) - 40);
    }

    [TestMethod]
    public async Task PersistScroll_EachPageIsReopenedWhereItWasLeft_AcrossAReloadToo()
    {
        await OpenAsync(NavRoute);

        var main = Main("ash-nav");
        await Expect(Page.Locator("#nav-mode")).ToHaveTextAsync("url");

        await ScrollToAsync(main, 400);

        await Page.Locator("#link-b").ClickAsync();
        await Expect(Page.Locator("#nav-page")).ToHaveTextAsync("B");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 0);

        await ScrollToAsync(main, 200);

        await Page.Locator("#link-a").ClickAsync();
        await Expect(Page.Locator("#nav-page")).ToHaveTextAsync("A");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 400);

        await Page.Locator("#link-b").ClickAsync();
        await Expect(Page.Locator("#nav-page")).ToHaveTextAsync("B");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 200);

        Assert.IsNotNull(await Page.EvaluateAsync<string?>("() => sessionStorage.getItem('bit-appshell-scrolls')"));

        await Page.ReloadAsync();
        await WaitForStatusAsync("Ready");
        await Expect(Page.Locator("#nav-page")).ToHaveTextAsync("B");
        await Expect(Main("ash-nav")).ToHaveJSPropertyAsync("scrollTop", 200);
    }

    [TestMethod]
    public async Task PersistScroll_AQueryOrAFragmentChange_IsLeftWhereItStands()
    {
        await OpenAsync(NavRoute);

        var main = Main("ash-nav");
        await ScrollToAsync(main, 400);

        await Page.Locator("#link-a-query").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("filter=1"));
        await NextFramesAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 400);

        // Following an in-page anchor scrolls to it rather than to a stored position or to the top.
        await Page.Locator("#link-fragment").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("#nav-row-25"));
        await Expect(Page.Locator("#nav-row-25")).ToBeInViewportAsync();
        await Expect(main).Not.ToHaveJSPropertyAsync("scrollTop", 0);
    }

    [TestMethod]
    public async Task PersistScroll_HistoryRestoration_RestoresBackAndForwardOnly()
    {
        await OpenAsync(NavRoute);

        await Page.Locator("#btn-mode-history").ClickAsync();
        await Expect(Page.Locator("#nav-mode")).ToHaveTextAsync("history");

        var main = Main("ash-nav");
        await ScrollToAsync(main, 400);

        await Page.Locator("#link-b").ClickAsync();
        await Expect(Page.Locator("#nav-page")).ToHaveTextAsync("B");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 0);

        await Page.GoBackAsync();
        await Expect(Page.Locator("#nav-page")).ToHaveTextAsync("A");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 400);

        await Page.GoForwardAsync();
        await Expect(Page.Locator("#nav-page")).ToHaveTextAsync("B");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 0);

        // A link to a page read halfway down earlier opens it at its top.
        await Page.Locator("#link-a").ClickAsync();
        await Expect(Page.Locator("#nav-page")).ToHaveTextAsync("A");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 0);
    }

    [TestMethod]
    public async Task AutoGoToTop_OpensEveryPageAtItsTop_AndWithoutItTheScrollerStays()
    {
        await OpenAsync(NavRoute);

        var main = Main("ash-nav");

        await Page.Locator("#btn-mode-top").ClickAsync();
        await Expect(Page.Locator("#nav-mode")).ToHaveTextAsync("top");
        await ScrollToAsync(main, 400);

        await Page.Locator("#link-b").ClickAsync();
        await Expect(Page.Locator("#nav-page")).ToHaveTextAsync("B");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 0);

        await Page.Locator("#btn-mode-none").ClickAsync();
        await Expect(Page.Locator("#nav-mode")).ToHaveTextAsync("none");
        await ScrollToAsync(main, 400);

        await Page.Locator("#link-a").ClickAsync();
        await Expect(Page.Locator("#nav-page")).ToHaveTextAsync("A");
        await NextFramesAsync();
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 400);
    }

    [TestMethod]
    public async Task ClearPersistedScroll_ForgetsTheKeptPositions()
    {
        await OpenAsync(NavRoute);

        var main = Main("ash-nav");
        await ScrollToAsync(main, 400);

        await Page.Locator("#link-b").ClickAsync();
        await Expect(Page.Locator("#nav-page")).ToHaveTextAsync("B");

        await Page.Locator("#btn-clear-all").ClickAsync();

        await Page.Locator("#link-a").ClickAsync();
        await Expect(Page.Locator("#nav-page")).ToHaveTextAsync("A");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", 0);
    }



    private ILocator Main(string shellId) => Page.Locator($"#{shellId}-container");

    private ILocator Bar(string shellId, string edge) => Page.Locator($"#{shellId} .bit-ash-{edge}");

    /// <summary>Scrolls a container the way the reader does, and waits for the position to have been taken.</summary>
    private async Task ScrollToAsync(ILocator main, double top)
    {
        await main.EvaluateAsync($"el => el.scrollTo({{ top: {top.ToString(CultureInfo.InvariantCulture)}, behavior: 'instant' }})");
        await Expect(main).ToHaveJSPropertyAsync("scrollTop", top);
        await NextFramesAsync();
    }

    /// <summary>
    /// Waits for the browser side of a shell to be set up, which the page's Ready does not wait for: the marker of the
    /// keyboard tracking the shells scrolled straight away carry, which is set up after their scroll reporting.
    /// </summary>
    private Task WaitForShellAsync(string shellId)
    {
        return Expect(Page.Locator($"#{shellId}")).ToHaveAttributeAsync("data-bit-ash-kbd", new System.Text.RegularExpressions.Regex(".+"));
    }

    /// <summary>Waits until a container has stood still for a few frames - the end of a smooth or wheel scroll.</summary>
    private Task WaitForScrollStillAsync(ILocator main)
    {
        return main.EvaluateAsync(@"el => new Promise(resolve => {
            let last = -1, still = 0;
            const check = () => {
                if (el.scrollTop === last) { if (++still >= 10) { resolve(); return; } } else { still = 0; last = el.scrollTop; }
                requestAnimationFrame(check);
            };
            check();
        })");
    }

    private async Task WaitForScrollTopAsync(ILocator main, double expected)
    {
        var value = expected.ToString(CultureInfo.InvariantCulture);
        await main.EvaluateAsync($"el => new Promise(resolve => {{ const check = () => Math.abs(el.scrollTop - {value}) <= 1 ? resolve() : requestAnimationFrame(check); check(); }})");
    }

    /// <summary>
    /// Stands an on-screen keyboard of the given height in: the visual viewport is shortened by it (the way a mobile
    /// browser shortens it while the layout viewport stays) and told it was resized.
    /// </summary>
    private Task SimulateKeyboardAsync(int height)
    {
        return Page.EvaluateAsync(@"height => {
            const vv = window.visualViewport;
            if (height > 0) {
                Object.defineProperty(vv, 'height', { configurable: true, get: () => document.documentElement.clientHeight - height });
            } else {
                delete vv.height;
            }
            vv.dispatchEvent(new Event('resize'));
        }", height);
    }
}
