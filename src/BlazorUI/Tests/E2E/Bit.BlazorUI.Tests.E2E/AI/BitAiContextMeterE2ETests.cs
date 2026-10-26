using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiContextMeter at /e2e/aicontextmeter in a real browser: the role=meter bar with its values, its name and
/// its value text, the readout and the native title, the Warn and Critical marks crossed both ways (the classes, the
/// colors of the words and the bar, and the one polite live region that says so), marks of the page's, the window taken
/// from the Window, the model or a conversation of the usage script (and nothing without one), the theme tokens and
/// every --bit-AiContextMeter-* variable, Styles and Classes, Disabled and RTL.
/// </summary>
[TestClass]
public class BitAiContextMeterE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aicontextmeter";

    private ILocator Meter(string id) => Page.Locator($"#{id}");

    private ILocator Bar(string id) => Page.Locator($"#{id} [role=meter]");

    private ILocator Percent(string id) => Page.Locator($"#{id} > .bit-aictm-pct");

    private ILocator Status(string id) => Page.Locator($"#{id} > .bit-aictm-sts");

    [TestMethod]
    public async Task Basic_TheMeter_ReadsTheShareOfTheWindowInUse()
    {
        await OpenAsync(Route);

        var meter = Meter("ctm-basic");
        await Expect(meter.Locator("> .bit-aictm-ttl")).ToHaveTextAsync("Context");
        await Expect(Percent("ctm-basic")).ToHaveTextAsync("15%");
        await Expect(meter).ToHaveAttributeAsync("title", "1,500 / 10,000 tokens");

        // A meter rather than a progress bar: how full the context is moves either way and never finishes.
        var bar = Bar("ctm-basic");
        await Expect(bar).ToHaveCountAsync(1);
        await Expect(meter.Locator("[role=progressbar]")).ToHaveCountAsync(0);
        await Expect(bar).ToHaveAttributeAsync("aria-valuenow", "15");
        await Expect(bar).ToHaveAttributeAsync("aria-valuemin", "0");
        await Expect(bar).ToHaveAttributeAsync("aria-valuemax", "100");
        await Expect(bar).ToHaveAttributeAsync("aria-label", "Context");
        await Expect(bar).ToHaveAttributeAsync("aria-valuetext", "1,500 / 10,000 tokens");
        await Expect(bar).ToHaveAccessibleNameAsync("Context");

        // The bar is filled as far as the reading goes.
        var track = await bar.RectAsync();
        var fill = await bar.Locator(".bit-prb-bar").RectAsync();
        Assert.AreEqual(0.15, (fill.Right - fill.Left) / (track.Right - track.Left), 0.02);

        // Below the marks the live region is there and says nothing.
        await Expect(Status("ctm-basic")).ToHaveAttributeAsync("role", "status");
        await Expect(Status("ctm-basic")).ToHaveTextAsync("");
        await Expect(meter).Not.ToHaveClassAsync(new Regex(@"(^|\s)bit-aictm-(wrn|crt)(\s|$)"));
    }

    [TestMethod]
    public async Task Crossing_TheWarnAndCriticalMarks_TurnTheMeterAndSaySoOnce()
    {
        await OpenAsync(Route);

        var meter = Meter("ctm-crossing");
        var status = Status("ctm-crossing");
        var progress = meter.Locator("> .bit-aictm-mtr");
        await status.EvaluateAsync("e => e.__e2eRegion = 'kept'");

        await Page.Locator("#btn-crossing-7900").ClickAsync();
        await Expect(Percent("ctm-crossing")).ToHaveTextAsync("79%");
        await Expect(meter).Not.ToHaveClassAsync(HasClass("bit-aictm-wrn"));
        await Expect(status).ToHaveTextAsync("");

        // The Warn mark: the warning color for the words and the bar, and a polite word about it.
        await Page.Locator("#btn-crossing-8000").ClickAsync();
        await Expect(meter).ToHaveClassAsync(HasClass("bit-aictm-wrn"));
        await Expect(status).ToHaveTextAsync("The context window is filling up");
        await Expect(meter).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-wrn-fg)"));
        await Expect(progress).ToHaveClassAsync(HasClass("bit-prb-wrn"));
        await Expect(progress.Locator(".bit-prb-bar")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-wrn)"));
        await Expect(Bar("ctm-crossing")).ToHaveAttributeAsync("aria-valuenow", "80");

        await Page.Locator("#btn-crossing-9400").ClickAsync();
        await Expect(Percent("ctm-crossing")).ToHaveTextAsync("94%");
        await Expect(meter).ToHaveClassAsync(HasClass("bit-aictm-wrn"));

        // The Critical mark: the error color, and another word.
        await Page.Locator("#btn-crossing-9500").ClickAsync();
        await Expect(meter).ToHaveClassAsync(HasClass("bit-aictm-crt"));
        await Expect(meter).Not.ToHaveClassAsync(HasClass("bit-aictm-wrn"));
        await Expect(status).ToHaveTextAsync("The context window is almost full");
        await Expect(meter).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-err-fg)"));
        await Expect(progress).ToHaveClassAsync(HasClass("bit-prb-err"));

        // Past the whole window the readout goes on while the bar stays full.
        await Page.Locator("#btn-crossing-12000").ClickAsync();
        await Expect(Percent("ctm-crossing")).ToHaveTextAsync("120%");
        await Expect(Bar("ctm-crossing")).ToHaveAttributeAsync("aria-valuenow", "100");
        await Expect(Bar("ctm-crossing")).ToHaveAttributeAsync("aria-valuetext", "12,000 / 10,000 tokens");

        // And back below the marks, quiet again - from the very region that spoke.
        await Page.Locator("#btn-crossing-1000").ClickAsync();
        await Expect(Percent("ctm-crossing")).ToHaveTextAsync("10%");
        await Expect(meter).Not.ToHaveClassAsync(new Regex(@"(^|\s)bit-aictm-(wrn|crt)(\s|$)"));
        await Expect(status).ToHaveTextAsync("");
        await Expect(progress).Not.ToHaveClassAsync(new Regex(@"(^|\s)bit-prb-(wrn|err)(\s|$)"));
        Assert.AreEqual("kept", await status.EvaluateAsync<string?>("e => e.__e2eRegion ?? null"), "The live region stayed on the page.");
    }

    [TestMethod]
    public async Task Status_TheLiveRegion_IsReadByScreenReadersAndHiddenFromSight()
    {
        await OpenAsync(Route);

        var status = Status("ctm-thr-warn");
        await Expect(status).ToHaveTextAsync("The context window is filling up");
        await Expect(status).ToHaveCSSAsync("position", "absolute");
        await Expect(status).ToHaveCSSAsync("width", "1px");
        await Expect(status).ToHaveCSSAsync("height", "1px");
        await Expect(status).ToHaveCSSAsync("overflow", "hidden");
        await Expect(status).ToHaveCSSAsync("clip-path", "inset(50%)");
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "The context window is filling up" })).Not.ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Thresholds_MarksOfThePage_MoveWhereTheMeterTurns()
    {
        await OpenAsync(Route);

        await Expect(Meter("ctm-thr-none")).Not.ToHaveClassAsync(new Regex(@"(^|\s)bit-aictm-(wrn|crt)(\s|$)"));
        await Expect(Meter("ctm-thr-warn")).ToHaveClassAsync(HasClass("bit-aictm-wrn"));
        await Expect(Meter("ctm-thr-critical")).ToHaveClassAsync(HasClass("bit-aictm-crt"));
        await Expect(Status("ctm-thr-critical")).ToHaveTextAsync("The context window is almost full");
    }

    [TestMethod]
    public async Task Sources_TheWindow_ComesFromTheWindowElseTheModelAndNothingShowsWithoutOne()
    {
        await OpenAsync(Route);

        // The tokens about to be added count on top of the ones used.
        await Expect(Bar("ctm-pending")).ToHaveAttributeAsync("aria-valuetext", "8,000 / 10,000 tokens");
        await Expect(Meter("ctm-pending")).ToHaveClassAsync(HasClass("bit-aictm-wrn"));

        await Expect(Bar("ctm-model")).ToHaveAttributeAsync("aria-valuetext", "50,000 / 200,000 tokens");
        await Expect(Percent("ctm-model")).ToHaveTextAsync("25%");
        await Expect(Bar("ctm-window-wins")).ToHaveAttributeAsync("aria-valuetext", "50,000 / 100,000 tokens");
        await Expect(Percent("ctm-window-wins")).ToHaveTextAsync("50%");

        await Expect(Meter("ctm-none")).ToHaveCountAsync(0);
        await Expect(Meter("ctm-zero")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#ctm-none-host > *")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#ctm-zero-host > *")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Conversation_TheMeter_ReadsTheLatestReplyOfTheConversation()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-conversation-ask").ClickAsync();
        await Expect(Page.Locator("#ctm-conversation-count")).ToHaveTextAsync("2");

        // 1,200 + 300 tokens of the 200,000-token window the reply's metadata reports.
        var bar = Bar("ctm-conversation");
        await Expect(bar).ToHaveAttributeAsync("aria-valuetext", "1,500 / 200,000 tokens");
        await Expect(bar).ToHaveAttributeAsync("aria-valuenow", "0");

        // The readout is the value of the meter, cut to a whole percent like it: 0.75% is 0%.
        await Expect(Percent("ctm-conversation")).ToHaveTextAsync("0%");
        await Expect(Meter("ctm-conversation")).ToHaveAttributeAsync("title", "1,500 / 200,000 tokens");
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Theme_TheWords_AreSmallSecondaryTextInDigitsOfOneWidth()
    {
        await OpenAsync(Route);

        var meter = Meter("ctm-basic");
        await Expect(meter).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));
        await Expect(meter).ToHaveCSSAsync("font-size", await meter.ResolveLengthInAsync("var(--bit-tpg-fs-xs)"));
        await Expect(meter).ToHaveCSSAsync("font-variant-numeric", "tabular-nums");
        await Expect(meter.Locator("> .bit-aictm-mtr")).ToHaveCSSAsync("max-width", await meter.ResolveLengthInAsync("calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale, 1) * 24)"));

        // The label, the bar and the readout sit on one row.
        var label = await meter.Locator("> .bit-aictm-ttl").RectAsync();
        var bar = await meter.Locator("> .bit-aictm-mtr").RectAsync();
        var percent = await Percent("ctm-basic").RectAsync();
        Assert.IsTrue(label.Right <= bar.Left && bar.Right <= percent.Left, "Label, bar, readout from left to right.");
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheMeterAtEachMark()
    {
        await OpenAsync(Route);

        await Expect(Meter("ctm-vars")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(Meter("ctm-vars-warn")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(Meter("ctm-vars-critical")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(Meter("ctm-vars")).ToHaveCSSAsync("font-size", "19px");
        await Expect(Meter("ctm-vars")).ToHaveCSSAsync("column-gap", "23px");
        await Expect(Meter("ctm-vars").Locator("> .bit-aictm-mtr")).ToHaveCSSAsync("max-width", "77px");
        await Expect(Meter("ctm-vars").Locator("> .bit-aictm-mtr .bit-prb-trc")).ToHaveCSSAsync("background-color", "rgb(10, 11, 12)");
    }

    [TestMethod]
    public async Task Track_ByDefault_IsDrawnInTheSecondaryBorderColorSoItStandsOffTheSurface()
    {
        await OpenAsync(Route);

        // A BitProgress draws its track in the secondary background, the very surface a meter sits on.
        await Expect(Meter("ctm-basic").Locator("> .bit-aictm-mtr .bit-prb-trc"))
            .ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-brd-sec)"));
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheParts_AndATitleOfThePageWins()
    {
        await OpenAsync(Route);

        await Expect(Meter("ctm-styles")).ToHaveClassAsync(HasClass("e2e-ctm-root"));
        await Expect(Meter("ctm-styles").Locator("> .bit-aictm-ttl")).ToHaveCSSAsync("font-weight", "700");
        await Expect(Percent("ctm-styles")).ToHaveCSSAsync("color", "rgb(40, 41, 42)");
        await Expect(Meter("ctm-styles").Locator("> .bit-aictm-mtr")).ToHaveClassAsync(HasClass("e2e-ctm-meter"));
        await Expect(Meter("ctm-title")).ToHaveAttributeAsync("title", "The page's own reading");
    }

    [TestMethod]
    public async Task Disabled_TheMeter_IsDrawnDisabledWhateverTheReading()
    {
        await OpenAsync(Route);

        var meter = Meter("ctm-disabled");
        await Expect(meter).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(meter).ToHaveClassAsync(HasClass("bit-aictm-wrn"));
        await Expect(meter).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-dis)"));
        await Expect(meter.Locator("> .bit-aictm-mtr")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Bar("ctm-disabled")).ToHaveAttributeAsync("aria-valuenow", "90");
    }

    [TestMethod]
    public async Task Rtl_AnExplicitOrAnInheritedDirection_MirrorsTheRowAndTheBar()
    {
        await OpenAsync(Route);

        await Expect(Meter("ctm-rtl")).ToHaveAttributeAsync("dir", "rtl");

        foreach (var id in new[] { "ctm-rtl", "ctm-rtl-inherited" })
        {
            var label = await Meter(id).Locator("> .bit-aictm-ttl").RectAsync();
            var percent = await Percent(id).RectAsync();
            Assert.IsTrue(label.Left > percent.Left, $"{id}: the label leads from the right.");

            // The bar fills from the right.
            var track = await Bar(id).RectAsync();
            var fill = await Bar(id).Locator(".bit-prb-bar").RectAsync();
            Assert.AreEqual(track.Right, fill.Right, 1.5, $"{id}: the fill starts on the right.");
            Assert.IsTrue(fill.Left > track.Left + 10, $"{id}: a quarter of the bar is filled.");
        }

        var ltrTrack = await Bar("ctm-ltr").RectAsync();
        var ltrFill = await Bar("ctm-ltr").Locator(".bit-prb-bar").RectAsync();
        Assert.AreEqual(ltrTrack.Left, ltrFill.Left, 1.5, "Left to right, the fill starts on the left.");
    }
}
