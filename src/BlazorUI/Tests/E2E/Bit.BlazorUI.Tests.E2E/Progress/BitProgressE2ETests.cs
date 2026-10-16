using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Progress;

/// <summary>
/// Drives BitProgress (/e2e/progress) in a real browser: the linear bar and the ring (the progressbar role, its name,
/// description and values, the filled share actually drawn), Label / Description and their templates, Indeterminate
/// (the running sweep and spin), Delay (the prerendered hold and the interactive reveal), Thickness, Diameter and
/// Radius, the percent readout in every position with its format and template, Value with Min and Max, Buffer,
/// Rounded and every Shape, Striped and its animation, Segments, Reversed, Vertical with Length, the gauge's
/// GapDegree in every GapPlacement (left to right and right to left), Meter, the AnnounceProgress live region,
/// BarColor / TrackColor, every Color and Size, the BitParams cascade, Style / Class / Styles / Classes, the public
/// --bit-Progress-* variables and the parameters winning over them, Disabled, Visibility, reduced motion, RTL, the
/// prerender, runtime re-renders and unmount.
/// </summary>
[TestClass]
public class BitProgressE2ETests : E2ETestBase
{
    private const string Path = "/e2e/progress";

    [TestMethod]
    public async Task Prerender_ThePageHtml_ContainsTheBars_AndHoldsADelayedOneHidden()
    {
        var html = await (await Page.APIRequest.GetAsync($"{BaseUrl}{Path}")).TextAsync();

        StringAssert.Matches(html, new Regex("id=\"prg-basic\" class=\"bit-prb\">.*?role=\"progressbar\"[^>]*aria-valuenow=\"42\""));
        StringAssert.Contains(html, "id=\"prg-delay-prerendered\" class=\"bit-prb-dlh bit-prb\"");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Path);
        await Expect(Page.Locator("#prg-basic .bit-prb-lbl")).ToHaveTextAsync("Uploading report.pdf");
    }

    [TestMethod]
    public async Task Basic_TheBarIsANamedDescribedProgressbar_FilledToItsPercent()
    {
        await OpenAsync(Path);

        var bar = Page.GetByRole(AriaRole.Progressbar, new() { Name = "Uploading report.pdf" }).First;
        await Expect(bar).ToHaveAttributeAsync("aria-valuenow", "42");
        await Expect(bar).ToHaveAttributeAsync("aria-valuemin", "0");
        await Expect(bar).ToHaveAttributeAsync("aria-valuemax", "100");
        await Expect(bar).ToHaveAccessibleDescriptionAsync("4.2 MB of 10 MB");
        await Expect(bar).ToHaveClassAsync(HasClass("bit-prb-bcn"));

        await WaitForFillAsync("#prg-basic", 0.42);
        await Expect(Page.Locator("#prg-basic .bit-prb-bar")).ToHaveCSSAsync("background-color", await ResolveAsync("color", "var(--bit-clr-pri)"));
        await Expect(Page.Locator("#prg-basic .bit-prb-trc")).ToHaveCSSAsync("background-color", await ResolveAsync("color", "var(--bit-clr-bg-sec)"));
        await Expect(Page.Locator("#prg-basic .bit-prb-des")).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-fg-sec)"));

        // Nothing and everything are clamped to the track.
        await WaitForFillAsync("#prg-zero", 0);
        await WaitForFillAsync("#prg-over", 1);
        await Expect(Page.Locator("#prg-over .bit-prb-bcn")).ToHaveAttributeAsync("aria-valuenow", "100");
    }

    [TestMethod]
    public async Task Circular_DrawsTheSameProgressAsARing_WithTheRoleOnTheSvg()
    {
        await OpenAsync(Path);

        var svg = Page.Locator("#prg-circular svg.bit-prb-cir");
        await Expect(svg).ToHaveAttributeAsync("role", "progressbar");
        await Expect(svg).ToHaveAttributeAsync("aria-valuenow", "42");
        await Expect(svg).ToHaveAccessibleNameAsync("Uploading report.pdf");
        await Expect(svg).ToHaveAccessibleDescriptionAsync("4.2 MB of 10 MB");
        await Expect(Page.Locator("#prg-circular .bit-prb-crp")).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        // The arc is 42% of the circle's circumference.
        await Expect(Page.Locator("#prg-circular .bit-prb-cbr")).ToHaveCSSAsync("stroke-dasharray", $"{Css(3.141 * 42 * 0.8)}%, 500%");
        await Expect(Page.Locator("#prg-circular .bit-prb-cbr")).ToHaveCSSAsync("stroke", await ResolveAsync("color", "var(--bit-clr-pri)"));
        await Expect(Page.Locator("#prg-circular .bit-prb-crt")).ToHaveCSSAsync("stroke", await ResolveAsync("color", "var(--bit-clr-bg-sec)"));
        Assert.AreEqual((50, 50), await SizeAsync("#prg-circular svg"));
    }

    [TestMethod]
    public async Task Templates_RenderTheLabelAndTheDescription_AndAnAriaLabelWinsOverTheLabel()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#prg-templates .bit-prb-lbl #label-template")).ToHaveTextAsync("Bold label");
        await Expect(Page.Locator("#prg-templates .bit-prb-des #description-template")).ToHaveTextAsync("Italic description");
        await Expect(Page.Locator("#prg-templates .bit-prb-bcn")).ToHaveAccessibleNameAsync("Bold label");

        var named = Page.Locator("#prg-aria-label .bit-prb-bcn");
        await Expect(named).ToHaveAttributeAsync("aria-label", "Named by aria");
        await Expect(named).Not.ToHaveAttributeAsync("aria-labelledby", new Regex(".*"));
        await Expect(named).ToHaveAccessibleNameAsync("Named by aria");
    }

    [TestMethod]
    public async Task Indeterminate_SweepsAndSpinsInTheBrowser_WithoutAnyValue()
    {
        await OpenAsync(Path);

        var container = Page.Locator("#prg-indeterminate .bit-prb-bcn");
        await Expect(container).Not.ToHaveAttributeAsync("aria-valuenow", new Regex(".*"));
        await Expect(container).Not.ToHaveAttributeAsync("aria-valuemin", new Regex(".*"));
        await Expect(container).Not.ToHaveAttributeAsync("aria-valuemax", new Regex(".*"));

        var bar = Page.Locator("#prg-indeterminate .bit-prb-ind");
        await Expect(bar).ToHaveCSSAsync("animation-name", "bit-prb-animation");
        await Expect(bar).ToHaveCSSAsync("animation-iteration-count", "infinite");
        var first = await bar.EvaluateAsync<string>("el => getComputedStyle(el).left");
        await Page.WaitForFunctionAsync("first => getComputedStyle(document.querySelector('#prg-indeterminate .bit-prb-ind')).left !== first", first);

        var ring = Page.Locator("#prg-cir-indeterminate .bit-prb-cri");
        await Expect(ring).ToHaveCSSAsync("animation-name", "bit-prb-cir-animation");
        Assert.AreEqual("running", await ring.EvaluateAsync<string>("el => el.getAnimations()[0]?.playState"));
        await Expect(Page.Locator("#prg-cir-indeterminate svg")).Not.ToHaveAttributeAsync("aria-valuenow", new Regex(".*"));
    }

    [TestMethod]
    public async Task Delay_HidesAnIndeterminateBarForThatLong_ThenRevealsIt()
    {
        await OpenAsync(Path);

        var watch = Stopwatch.StartNew();
        await Page.Locator("#btn-show-delayed").ClickAsync();

        var delayed = Page.Locator("#prg-delay");
        await Expect(delayed).ToHaveClassAsync(HasClass("bit-prb-dly"));
        await Expect(delayed).ToHaveAttributeAsync("style", new Regex("--bit-prb-delay: 2000ms"));
        await Expect(delayed).ToHaveCSSAsync("visibility", "hidden");

        await Expect(delayed).ToHaveCSSAsync("visibility", "visible");
        Assert.IsTrue(watch.ElapsedMilliseconds >= 1500, $"Revealed after {watch.ElapsedMilliseconds}ms of its 2000.");
        await Expect(delayed).Not.ToHaveClassAsync(HasClass("bit-prb-dly"));

        // A delay set to zero ends the wait at once.
        await Expect(Page.Locator("#prg-delay-cancel")).ToHaveClassAsync(HasClass("bit-prb-dly"));
        await Page.Locator("#btn-cancel-delay").ClickAsync();
        await Expect(Page.Locator("#prg-delay-cancel")).Not.ToHaveClassAsync(HasClass("bit-prb-dly"));
        await Expect(Page.Locator("#prg-delay-cancel")).ToHaveCSSAsync("visibility", "visible");
    }

    [TestMethod]
    public async Task ThicknessDiameterAndRadius_SizeTheBarAndTheRing()
    {
        await OpenAsync(Path);

        Assert.AreEqual(14, (await SizeAsync("#prg-thick .bit-prb-bcn")).Height);
        Assert.AreEqual(14, (await SizeAsync("#prg-thick .bit-prb-bar")).Height);

        Assert.AreEqual((96, 96), await SizeAsync("#prg-cir-thick svg"));
        await Expect(Page.Locator("#prg-cir-thick svg")).ToHaveAttributeAsync("width", "96px");
        await Expect(Page.Locator("#prg-cir-thick .bit-prb-cbr")).ToHaveCSSAsync("stroke-width", "min(8px, 20%)");

        Assert.AreEqual((24, 24), await SizeAsync("#prg-cir-24 svg"));

        // The stroke never takes more than a fifth of the ring.
        await Expect(Page.Locator("#prg-cir-thin-cap .bit-prb-cbr")).ToHaveCSSAsync("stroke-width", "min(40px, 20%)");

        // Without a Diameter, the ring is Thickness times Radius.
        Assert.AreEqual((80, 80), await SizeAsync("#prg-cir-radius svg"));
    }

    [TestMethod]
    [DataRow("end", "bit-prb-pce", "86 %", "end")]
    [DataRow("format", "bit-prb-pce", "85.69 %", "end")]
    [DataRow("bad-format", "bit-prb-pce", "86 %", "end")]
    [DataRow("Start", "bit-prb-pcs", "42 %", "start")]
    [DataRow("Center", "bit-prb-pcc", "42 %", "center")]
    public async Task PercentNumber_IsWrittenUnderTheBar_InItsFormat_AtItsPosition(string id, string className, string text, string align)
    {
        await OpenAsync(Path);

        var readout = Page.Locator($"#prg-pct-{id} > .bit-prb-pct");

        await Expect(readout).ToHaveClassAsync(HasClass(className));
        await Expect(readout.Locator("bdi.bit-prb-pnm")).ToHaveTextAsync(text);
        await Expect(readout).ToHaveCSSAsync("text-align", align);
        await Expect(readout.Locator("bdi")).ToHaveCSSAsync("direction", "ltr");

        var bar = await Page.Locator($"#prg-pct-{id} .bit-prb-bcn").BoundingBoxAsync();
        Assert.IsTrue((await readout.BoundingBoxAsync())!.Y >= bar!.Y + bar.Height - 0.5, "Under the bar.");
    }

    [TestMethod]
    public async Task PercentNumber_TopSitsOnTheLabelsRow_InsideRidesTheBar_AndATemplateReplacesIt()
    {
        await OpenAsync(Path);

        var top = Page.Locator("#prg-pct-Top .bit-prb-hdr > .bit-prb-pco");
        await Expect(top).ToHaveTextAsync("42 %");
        var label = await Page.Locator("#prg-pct-Top .bit-prb-lbl").BoundingBoxAsync();
        var topBox = await top.BoundingBoxAsync();
        Assert.IsTrue(topBox!.X > label!.X + label.Width - 1, "To the right of the label, on its row.");
        Assert.IsTrue(topBox.Y < (await Page.Locator("#prg-pct-Top .bit-prb-bcn").BoundingBoxAsync())!.Y);

        var inside = Page.Locator("#prg-pct-Inside .bit-prb-bar > .bit-prb-pci");
        await Expect(inside).ToHaveTextAsync("42 %");
        await Expect(Page.Locator("#prg-pct-Inside .bit-prb-bar")).ToHaveClassAsync(HasClass("bit-prb-bri"));
        await Expect(inside).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-pri-text)"));

        await Expect(Page.Locator("#prg-pct-template .bit-prb-pct #pct-template")).ToHaveTextAsync("86% done");

        // An indeterminate bar has no number to show.
        await Expect(Page.Locator("#prg-pct-indeterminate .bit-prb-pct")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task PercentNumber_OfARing_IsCenteredInIt_AndScalesWithTheDiameter()
    {
        await OpenAsync(Path);

        var small = Page.Locator("#prg-pct-ring .bit-prb-ctx");
        await Expect(small).ToHaveTextAsync("86 %");
        await Expect(small).ToHaveClassAsync(HasClass("bit-prb-fxs"));
        await Expect(small).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", "var(--bit-tpg-fs-xs)"));

        var ring = await Page.Locator("#prg-pct-ring svg").BoundingBoxAsync();
        var text = await small.BoundingBoxAsync();
        Assert.AreEqual(ring!.X + ring.Width / 2, text!.X + text.Width / 2, 1.5);
        Assert.AreEqual(ring.Y + ring.Height / 2, text.Y + text.Height / 2, 1.5);

        var large = Page.Locator("#prg-pct-ring-large .bit-prb-ctx");
        await Expect(large).ToHaveTextAsync("85.7 %");
        await Expect(large).ToHaveClassAsync(HasClass("bit-prb-flg"));
        await Expect(large).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", "var(--bit-tpg-fs-lg)"));
    }

    [TestMethod]
    public async Task Value_WithMinAndMax_FillsFromTheRatio_AndPublishesTheRealNumbers()
    {
        await OpenAsync(Path);

        var files = Page.Locator("#prg-value .bit-prb-bcn");
        await Expect(files).ToHaveAttributeAsync("aria-valuenow", "3");
        await Expect(files).ToHaveAttributeAsync("aria-valuemax", "12");
        await Expect(files).ToHaveAttributeAsync("aria-valuetext", "3 of 12 files");
        await WaitForFillAsync("#prg-value", 0.25);
        await Expect(Page.Locator("#prg-value .bit-prb-pnm")).ToHaveTextAsync("25 %");

        var rows = Page.Locator("#prg-value-min .bit-prb-bcn");
        await Expect(rows).ToHaveAttributeAsync("aria-valuemin", "1000");
        await Expect(rows).ToHaveAttributeAsync("aria-valuenow", "1500");
        await WaitForFillAsync("#prg-value-min", 0.25);

        await Expect(Page.Locator("#prg-value-clamp .bit-prb-bcn")).ToHaveAttributeAsync("aria-valuenow", "20");
        await WaitForFillAsync("#prg-value-clamp", 1);

        await Expect(Page.Locator("#prg-value-ring svg")).ToHaveAttributeAsync("aria-valuenow", "7");
        await Expect(Page.Locator("#prg-value-ring .bit-prb-ctx")).ToHaveTextAsync("58 %");
    }

    [TestMethod]
    public async Task Buffer_DrawsAFainterSecondValueBehindTheBar()
    {
        await OpenAsync(Path);

        var buffer = Page.Locator("#prg-buffer .bit-prb-buf");
        var container = await Page.Locator("#prg-buffer .bit-prb-bcn").BoundingBoxAsync();
        await Page.WaitForFunctionAsync("() => { const b = document.querySelector('#prg-buffer .bit-prb-buf').getBoundingClientRect(); const c = document.querySelector('#prg-buffer .bit-prb-bcn').getBoundingClientRect(); return Math.abs(b.width / c.width - 0.55) < 0.01; }");
        await WaitForFillAsync("#prg-buffer", 0.30);
        Assert.IsTrue((await buffer.BoundingBoxAsync())!.Width < container!.Width);

        // A translucent shade of the bar's color.
        var color = await buffer.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        StringAssert.Contains(color, "/ 0.38");

        // Against a Value, the buffer is in the same units.
        await Page.WaitForFunctionAsync("() => { const b = document.querySelector('#prg-buffer-value .bit-prb-buf').getBoundingClientRect(); const c = document.querySelector('#prg-buffer-value .bit-prb-bcn').getBoundingClientRect(); return Math.abs(b.width / c.width - 0.8) < 0.01; }");

        await Expect(Page.Locator("#prg-cir-buffer .bit-prb-cbf")).ToHaveCSSAsync("stroke-dasharray", $"{Css(3.141 * 55 * 0.8)}%, 500%");
        await Expect(Page.Locator("#prg-buffer-indeterminate .bit-prb-buf")).ToHaveCountAsync(0);
    }

    [TestMethod]
    [DataRow("rounded", "bit-prb-rnd", "9999px")]
    [DataRow("shape-Pill", "bit-prb-rnd", "9999px")]
    [DataRow("shape-Rounded", "bit-prb-shr", "var(--bit-shp-radius-progress)")]
    [DataRow("shape-Square", "bit-prb-sqr", "0px")]
    public async Task Shape_EveryValue_SetsTheCornersOfTheBar(string id, string className, string radius)
    {
        await OpenAsync(Path);

        await Expect(Page.Locator($"#prg-{id}")).ToHaveClassAsync(HasClass(className));
        await Expect(Page.Locator($"#prg-{id} .bit-prb-bar")).ToHaveCSSAsync("border-top-left-radius", await ResolveAsync("border-top-left-radius", radius));
        await Expect(Page.Locator($"#prg-{id} .bit-prb-trc")).ToHaveCSSAsync("border-top-left-radius", await ResolveAsync("border-top-left-radius", radius));
    }

    [TestMethod]
    public async Task Rounded_GivesTheRingARoundCap()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#prg-cir-rounded .bit-prb-cbr")).ToHaveCSSAsync("stroke-linecap", "round");
        await Expect(Page.Locator("#prg-circular .bit-prb-cbr")).ToHaveCSSAsync("stroke-linecap", "butt");
    }

    [TestMethod]
    public async Task Striped_PaintsStripes_AndStripedAnimationMovesThem()
    {
        await OpenAsync(Path);

        var striped = Page.Locator("#prg-striped .bit-prb-bar");
        await Expect(striped).ToHaveClassAsync(HasClass("bit-prb-stp"));
        StringAssert.StartsWith(await striped.EvaluateAsync<string>("el => getComputedStyle(el).backgroundImage"), "linear-gradient(45deg");
        await Expect(striped).ToHaveCSSAsync("animation-name", "none");

        var moving = Page.Locator("#prg-striped-animated .bit-prb-bar");
        await Expect(moving).ToHaveClassAsync(HasClass("bit-prb-sta"));
        await Expect(moving).ToHaveCSSAsync("animation-name", "bit-prb-stripe-animation");
        var first = await moving.EvaluateAsync<string>("el => getComputedStyle(el).backgroundPositionX");
        await Page.WaitForFunctionAsync("first => getComputedStyle(document.querySelector('#prg-striped-animated .bit-prb-bar')).backgroundPositionX !== first", first);
    }

    [TestMethod]
    public async Task Segments_CutTheBarIntoEqualSteps()
    {
        await OpenAsync(Path);

        var root = Page.Locator("#prg-segments");
        await Expect(root).ToHaveClassAsync(HasClass("bit-prb-seg"));
        await Expect(root).ToHaveAttributeAsync("style", new Regex("--bit-prb-segments: 5;--bit-prb-segment-gap: 4px;"));
        StringAssert.StartsWith(await ComputedStyleAsync("#prg-segments .bit-prb-bcn", "mask-image"), "repeating-linear-gradient(to right");

        await Expect(Page.Locator("#prg-segments-gap")).ToHaveAttributeAsync("style", new Regex("--bit-prb-segment-gap: 2px;"));

        // A segmented bar has square steps, and a single segment is no segmenting at all.
        await Expect(Page.Locator("#prg-segments .bit-prb-bar")).ToHaveCSSAsync("border-top-left-radius", "0px");
        await Expect(Page.Locator("#prg-segments-one")).Not.ToHaveClassAsync(HasClass("bit-prb-seg"));
    }

    [TestMethod]
    public async Task Reversed_FillsFromTheEnd_SweepsBack_AndTurnsTheRingCounterClockwise()
    {
        await OpenAsync(Path);

        await WaitForFillAsync("#prg-reversed", 0.42);
        var bar = await Page.Locator("#prg-reversed .bit-prb-bar").BoundingBoxAsync();
        var container = await Page.Locator("#prg-reversed .bit-prb-bcn").BoundingBoxAsync();
        Assert.AreEqual(container!.X + container.Width, bar!.X + bar.Width, 1, "Anchored to the end.");

        await Expect(Page.Locator("#prg-reversed-indeterminate .bit-prb-ind")).ToHaveCSSAsync("animation-direction", "reverse");

        // Mirrored horizontally and turned: the arc runs the other way round.
        await Expect(Page.Locator("#prg-cir-reversed .bit-prb-cbr")).ToHaveCSSAsync("transform", "matrix(0, -1, -1, 0, 0, 0)");
        await Expect(Page.Locator("#prg-circular .bit-prb-cbr")).ToHaveCSSAsync("transform", "matrix(0, -1, 1, 0, 0, 0)");
    }

    [TestMethod]
    public async Task Vertical_StandsTheBarOnItsEnd_FillingFromTheBottom_LengthTall()
    {
        await OpenAsync(Path);

        var container = await Page.Locator("#prg-vertical .bit-prb-bcn").BoundingBoxAsync();
        await Expect(Page.Locator("#prg-vertical")).ToHaveClassAsync(HasClass("bit-prb-ver"));
        Assert.AreEqual(12, Math.Round(container!.Width));
        Assert.AreEqual(160, Math.Round(container.Height), "spacing(20) by default.");

        await Page.WaitForFunctionAsync("() => { const b = document.querySelector('#prg-vertical .bit-prb-bar').getBoundingClientRect(); return Math.abs(b.height - 160 * 0.42) < 1; }");
        var bar = await Page.Locator("#prg-vertical .bit-prb-bar").BoundingBoxAsync();
        Assert.AreEqual(container.Y + container.Height, bar!.Y + bar.Height, 1, "Filled from the bottom.");

        Assert.AreEqual(96, (await SizeAsync("#prg-vertical-length .bit-prb-bcn")).Height);

        var reversedBar = await Page.Locator("#prg-vertical-reversed .bit-prb-bar").BoundingBoxAsync();
        var reversedContainer = await Page.Locator("#prg-vertical-reversed .bit-prb-bcn").BoundingBoxAsync();
        Assert.AreEqual(reversedContainer!.Y, reversedBar!.Y, 1, "Reversed fills from the top.");

        await Expect(Page.Locator("#prg-vertical-indeterminate .bit-prb-ind")).ToHaveCSSAsync("animation-name", "bit-prb-ver-animation");
    }

    [TestMethod]
    [DataRow("Bottom", "bit-prb-gap", "90deg", "matrix(-0.707107, 0.707107, -0.707107, -0.707107, 0, 0)")]
    [DataRow("Top", "bit-prb-gpt", "120deg", "matrix(0.866025, -0.5, 0.5, 0.866025, 0, 0)")]
    [DataRow("Start", "bit-prb-gps", "120deg", "matrix(-0.5, -0.866025, 0.866025, -0.5, 0, 0)")]
    [DataRow("End", "bit-prb-gpe", "120deg", "matrix(0.5, 0.866025, -0.866025, 0.5, 0, 0)")]
    public async Task GapDegree_CutsAGaugeOutOfTheRing_WhereGapPlacementSays(string placement, string className, string gap, string transform)
    {
        await OpenAsync(Path);

        var root = Page.Locator($"#prg-gauge-{placement}");
        await Expect(root).ToHaveClassAsync(HasClass("bit-prb-gap"));
        await Expect(root).ToHaveClassAsync(HasClass(className));
        await Expect(root).ToHaveAttributeAsync("style", new Regex($"--bit-prb-gap: {gap};"));

        // The arc starts right after the gap, and the track is shortened by it too.
        await Expect(root.Locator(".bit-prb-cbr")).ToHaveCSSAsync("transform", transform);
        var arc = (360 - double.Parse(gap.Replace("deg", ""), CultureInfo.InvariantCulture)) / 360;
        await Expect(root.Locator(".bit-prb-crt")).ToHaveCSSAsync("stroke-dasharray", $"{Css(Math.Round(3.141 * 100 * 0.8 * arc, 3))}%, 500%");
    }

    [TestMethod]
    public async Task GapDegree_IsClamped_AndStartAndEndFollowTheDirection()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#prg-gauge-clamped")).ToHaveAttributeAsync("style", new Regex("--bit-prb-gap: 295deg;"));

        // Start is the right-hand side in a right-to-left page: the opening the End gauge has left to right.
        await Expect(Page.Locator("#prg-gauge-rtl-Start .bit-prb-cbr")).ToHaveCSSAsync("transform", await ComputedStyleAsync("#prg-gauge-End .bit-prb-cbr", "transform"));
    }

    [TestMethod]
    public async Task Meter_PublishesAReadingAsAMeter()
    {
        await OpenAsync(Path);

        var meter = Page.GetByRole(AriaRole.Meter, new() { Name = "Disk usage" });
        await Expect(meter).ToHaveAttributeAsync("aria-valuenow", "72");
        await Expect(meter).ToHaveAttributeAsync("aria-valuemax", "128");
        await Expect(meter).ToHaveAttributeAsync("aria-valuetext", "72 of 128 GB");
        await WaitForFillAsync("#prg-meter", 72.0 / 128);

        // A reading cannot be indeterminate, so that one stays a progressbar.
        await Expect(Page.Locator("#prg-meter-indeterminate .bit-prb-bcn")).ToHaveAttributeAsync("role", "progressbar");
    }

    [TestMethod]
    public async Task AnnounceProgress_SpeaksOncePerStepCrossed_AndAtCompletion()
    {
        await OpenAsync(Path);

        var region = Page.Locator("#prg-announce .bit-prb-lvr");
        await Expect(region).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(region).ToHaveAttributeAsync("aria-atomic", "true");
        await Expect(region).ToHaveTextAsync("");
        var box = await region.BoundingBoxAsync();
        Assert.IsTrue(box!.Width <= 1 && box.Height <= 1, "Heard, not seen.");

        await Page.Locator("#btn-announce-30").ClickAsync();
        await Expect(region).ToHaveTextAsync("Importing rows: 25 %");
        await Expect(Page.Locator("#prg-announce-step .bit-prb-lvr")).ToHaveTextAsync("Uploading: 30 rows");

        await Page.Locator("#btn-announce-60").ClickAsync();
        await Expect(region).ToHaveTextAsync("Importing rows: 50 %");

        await Page.Locator("#btn-announce-100").ClickAsync();
        await Expect(region).ToHaveTextAsync("Importing rows: 100 %");
        await Expect(Page.Locator("#prg-announce-step .bit-prb-lvr")).ToHaveTextAsync("Uploading: 100 rows");
    }

    [TestMethod]
    public async Task CustomColors_BarColorAndTrackColor_PaintTheBarTheTrackAndTheBuffer()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#prg-custom .bit-prb-bar")).ToHaveCSSAsync("background-color", "rgb(139, 92, 246)");
        await Expect(Page.Locator("#prg-custom .bit-prb-trc")).ToHaveCSSAsync("background-color", "rgb(233, 213, 255)");
        StringAssert.StartsWith(await ComputedStyleAsync("#prg-custom .bit-prb-buf", "background-color"), "color(srgb 0.545");
        await Expect(Page.Locator("#prg-cir-custom .bit-prb-cbr")).ToHaveCSSAsync("stroke", "rgb(255, 99, 71)");
        await Expect(Page.Locator("#prg-cir-custom .bit-prb-crt")).ToHaveCSSAsync("stroke", "rgb(255, 224, 214)");
    }

    [TestMethod]
    public async Task BitParams_CascadeTheirDefaults_AndAProgresssOwnValueWins()
    {
        await OpenAsync(Path);

        var first = Page.Locator("#prg-params-1");
        await Expect(first).ToHaveClassAsync(HasClass("bit-prb-rnd"));
        await Expect(first).ToHaveClassAsync(HasClass("bit-prb-suc"));
        Assert.AreEqual(8, (await SizeAsync("#prg-params-1 .bit-prb-bcn")).Height);
        await Expect(first.Locator(".bit-prb-hdr > .bit-prb-pco")).ToHaveTextAsync("34 %");

        await Expect(Page.Locator("#prg-params-2")).Not.ToHaveClassAsync(HasClass("bit-prb-rnd"));
        await Expect(Page.Locator("#prg-params-2 .bit-prb-hdr > .bit-prb-pco")).ToHaveTextAsync("92 %");

        await Expect(Page.Locator("#prg-params-outside .bit-prb-pct")).ToHaveCountAsync(0);
    }

    [TestMethod]
    [DataRow("Primary", "pri", "--bit-clr-pri", "--bit-clr-pri-text")]
    [DataRow("Secondary", "sec", "--bit-clr-sec", "--bit-clr-sec-text")]
    [DataRow("Tertiary", "ter", "--bit-clr-ter", "--bit-clr-ter-text")]
    [DataRow("Info", "inf", "--bit-clr-inf", "--bit-clr-inf-text")]
    [DataRow("Success", "suc", "--bit-clr-suc", "--bit-clr-suc-text")]
    [DataRow("Warning", "wrn", "--bit-clr-wrn", "--bit-clr-wrn-text")]
    [DataRow("SevereWarning", "swr", "--bit-clr-swr", "--bit-clr-swr-text")]
    [DataRow("Error", "err", "--bit-clr-err", "--bit-clr-err-text")]
    [DataRow("PrimaryBackground", "pbg", "--bit-clr-bg-pri", "--bit-clr-fg-pri")]
    [DataRow("SecondaryBackground", "sbg", "--bit-clr-bg-sec", "--bit-clr-fg-pri")]
    [DataRow("TertiaryBackground", "tbg", "--bit-clr-bg-ter", "--bit-clr-fg-pri")]
    [DataRow("PrimaryForeground", "pfg", "--bit-clr-fg-pri", "--bit-clr-bg-pri")]
    [DataRow("SecondaryForeground", "sfg", "--bit-clr-fg-sec", "--bit-clr-bg-pri")]
    [DataRow("TertiaryForeground", "tfg", "--bit-clr-fg-ter", "--bit-clr-bg-pri")]
    [DataRow("PrimaryBorder", "pbr", "--bit-clr-brd-pri", "--bit-clr-fg-pri")]
    [DataRow("SecondaryBorder", "sbr", "--bit-clr-brd-sec", "--bit-clr-fg-pri")]
    [DataRow("TertiaryBorder", "tbr", "--bit-clr-brd-ter", "--bit-clr-fg-pri")]
    public async Task Color_EveryValue_PaintsTheBarAndTheTextOnIt(string color, string suffix, string main, string on)
    {
        await OpenAsync(Path);

        var root = Page.Locator($"#prg-color-{color}");

        await Expect(root).ToHaveClassAsync(HasClass($"bit-prb-{suffix}"));
        await Expect(root.Locator(".bit-prb-bar")).ToHaveCSSAsync("background-color", await ResolveAsync("color", $"var({main})"));
        await Expect(root.Locator(".bit-prb-pci")).ToHaveCSSAsync("color", await ResolveAsync("color", $"var({on})"));
    }

    [TestMethod]
    [DataRow("Small", "sm", "--bit-siz-track-sm", "--bit-tpg-fs-xs", 32)]
    [DataRow("Medium", "md", "--bit-siz-track-md", "--bit-tpg-fs-sm", 50)]
    [DataRow("Large", "lg", "--bit-siz-track-lg", "--bit-tpg-fs-md", 72)]
    public async Task Size_EveryValue_ScalesTheThicknessTheRingAndTheType(string size, string suffix, string track, string fontSize, int diameter)
    {
        await OpenAsync(Path);

        var root = Page.Locator($"#prg-size-{size}");

        await Expect(root).ToHaveClassAsync(HasClass($"bit-prb-{suffix}"));
        Assert.AreEqual(Math.Round(Px(await ResolveAsync("height", $"var({track})"))), (await SizeAsync($"#prg-size-{size} .bit-prb-bcn")).Height);
        await Expect(root).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", $"var({fontSize})"));
        Assert.AreEqual((diameter, diameter), await SizeAsync($"#prg-cir-size-{size} svg"));
    }

    [TestMethod]
    public async Task Size_Unset_IsTheThinBarAndTheMediumRing()
    {
        await OpenAsync(Path);

        Assert.AreEqual(Math.Round(Px(await ResolveAsync("height", "var(--bit-siz-track-sm)"))), (await SizeAsync("#prg-size-none .bit-prb-bcn")).Height);
        Assert.AreEqual((50, 50), await SizeAsync("#prg-cir-size-none svg"));
        await Expect(Page.Locator("#prg-size-none")).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", "var(--bit-tpg-fs-sm)"));
    }

    [TestMethod]
    public async Task StyleClassStylesAndClasses_ReachTheRootAndEveryPart()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#prg-style")).ToHaveCSSAsync("padding-top", "3px");
        await Expect(Page.Locator("#prg-style")).ToHaveCSSAsync("background-color", "rgb(230, 135, 220)");
        await Expect(Page.Locator("#prg-style")).ToHaveClassAsync(HasClass("e2e-progress-class"));

        var parts = new (string Selector, string Property, string Value, string Class)[]
        {
            ("#prg-styles", "margin-top", "5px", "e2e-root"),
            ("#prg-styles .bit-prb-lbl", "color", "rgb(1, 1, 1)", "e2e-label"),
            ("#prg-styles .bit-prb-bcn", "border-top-color", "rgb(2, 2, 2)", "e2e-container"),
            ("#prg-styles .bit-prb-trc", "background-color", "rgb(3, 3, 3)", "e2e-track"),
            ("#prg-styles .bit-prb-buf", "background-color", "rgb(4, 4, 4)", "e2e-buffer"),
            ("#prg-styles .bit-prb-bar", "background-color", "rgb(5, 5, 5)", "e2e-bar"),
            ("#prg-styles .bit-prb-pct", "color", "rgb(6, 6, 6)", "e2e-percent"),
            ("#prg-styles .bit-prb-des", "color", "rgb(7, 7, 7)", "e2e-description"),
        };

        foreach (var (selector, property, value, className) in parts)
        {
            await Expect(Page.Locator(selector)).ToHaveCSSAsync(property, value);
            await Expect(Page.Locator(selector)).ToHaveClassAsync(HasClass(className));
        }

        await Expect(Page.Locator("#prg-cir-styles .bit-prb-cbr")).ToHaveCSSAsync("stroke", "rgb(8, 8, 8)");
        await Expect(Page.Locator("#prg-cir-styles .bit-prb-crt")).ToHaveCSSAsync("stroke", "rgb(9, 9, 9)");
        await Expect(Page.Locator("#prg-cir-styles .bit-prb-cbf")).ToHaveCSSAsync("stroke", "rgb(10, 10, 10)");

        await Expect(Page.Locator("#prg-attrs")).ToHaveAttributeAsync("data-e2e", "splatted");
        await Expect(Page.Locator("#prg-attrs")).ToHaveAttributeAsync("title", "A title");
    }

    [TestMethod]
    public async Task VisibilityAndDisabled_HideOrDimTheProgress_AndStopItsAnimations()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#prg-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Page.Locator("#prg-collapsed")).ToHaveCSSAsync("display", "none");

        await Expect(Page.Locator("#prg-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Page.Locator("#prg-disabled")).ToHaveCSSAsync("opacity", await ResolveAsync("opacity", "var(--bit-opa-dis)"));
        await Expect(Page.Locator("#prg-disabled .bit-prb-ind")).ToHaveCSSAsync("animation-name", "none");
        await Expect(Page.Locator("#prg-disabled-striped .bit-prb-sta")).ToHaveCSSAsync("animation-name", "none");
    }

    [TestMethod]
    public async Task CssVariables_SetOnAContainer_RestyleEveryPart()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#prg-css")).ToHaveCSSAsync("font-size", "17px");
        Assert.AreEqual(13, (await SizeAsync("#prg-css .bit-prb-bcn")).Height);
        await Expect(Page.Locator("#prg-css .bit-prb-bar")).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(Page.Locator("#prg-css .bit-prb-bar")).ToHaveCSSAsync("border-top-left-radius", "5px");
        await Expect(Page.Locator("#prg-css .bit-prb-bar")).ToHaveCSSAsync("transition-duration", "1.234s");
        await Expect(Page.Locator("#prg-css .bit-prb-trc")).ToHaveCSSAsync("background-color", "rgb(40, 50, 60)");
        await Expect(Page.Locator("#prg-css .bit-prb-buf")).ToHaveCSSAsync("background-color", "rgb(70, 80, 90)");
        await Expect(Page.Locator("#prg-css .bit-prb-lbl")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(Page.Locator("#prg-css .bit-prb-lbl")).ToHaveCSSAsync("font-weight", "700");
        await Expect(Page.Locator("#prg-css .bit-prb-pct")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(Page.Locator("#prg-css .bit-prb-des")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(Page.Locator("#prg-css .bit-prb-des")).ToHaveCSSAsync("font-size", "11px");

        await Expect(Page.Locator("#prg-css-inside .bit-prb-pci")).ToHaveCSSAsync("color", "rgb(250, 250, 250)");
        await Expect(Page.Locator("#prg-css-striped .bit-prb-bar")).ToHaveCSSAsync("background-size", "24px 24px");
        StringAssert.Contains(await ComputedStyleAsync("#prg-css-striped .bit-prb-bar", "background-image"), "rgba(255, 0, 0, 0.5)");

        Assert.AreEqual((70, 70), await SizeAsync("#prg-css-ring svg"));
        await Expect(Page.Locator("#prg-css-ring .bit-prb-ctx")).ToHaveCSSAsync("font-size", "19px");
        Assert.AreEqual(120, (await SizeAsync("#prg-css-vertical .bit-prb-bcn")).Height);
    }

    [TestMethod]
    public async Task CssVariables_TheParametersWinOverThem()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#prg-css-params .bit-prb-bar")).ToHaveCSSAsync("background-color", await ResolveAsync("color", "var(--bit-clr-err)"));
        Assert.AreEqual(Math.Round(Px(await ResolveAsync("height", "var(--bit-siz-track-lg)"))), (await SizeAsync("#prg-css-params .bit-prb-bcn")).Height);
        await Expect(Page.Locator("#prg-css-params .bit-prb-bar")).ToHaveCSSAsync("border-top-left-radius", "0px");
        Assert.AreEqual(3, (await SizeAsync("#prg-css-thickness .bit-prb-bcn")).Height);
    }

    [TestMethod]
    public async Task ReducedMotion_SlowsTheSweep_AndMakesTheFillInstant()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync(Path);

        await Expect(Page.Locator("#prg-indeterminate .bit-prb-ind")).ToHaveCSSAsync("animation-duration", "9s");
        Assert.IsTrue(Seconds(await ComputedStyleAsync("#prg-basic .bit-prb-bar", "transition-duration")) < 0.001);
    }

    [TestMethod]
    public async Task Rtl_TheBarFillsFromTheRight_AndAReversedOneFromTheLeft()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#prg-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.Locator("#prg-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));

        foreach (var id in new[] { "#prg-rtl", "#prg-rtl-inherited" })
        {
            await WaitForFillAsync(id, 0.42);
            var bar = await Page.Locator($"{id} .bit-prb-bar").BoundingBoxAsync();
            var container = await Page.Locator($"{id} .bit-prb-bcn").BoundingBoxAsync();
            Assert.AreEqual(container!.X + container.Width, bar!.X + bar.Width, 1, $"{id} starts at the right.");
        }

        var reversed = await Page.Locator("#prg-rtl-reversed .bit-prb-bar").BoundingBoxAsync();
        var reversedContainer = await Page.Locator("#prg-rtl-reversed .bit-prb-bcn").BoundingBoxAsync();
        Assert.AreEqual(reversedContainer!.X, reversed!.X, 1);

        // The readout reads the number left to right, and the segments are cut from the right.
        await Expect(Page.Locator("#prg-rtl .bit-prb-pnm")).ToHaveCSSAsync("direction", "ltr");
        StringAssert.StartsWith(await ComputedStyleAsync("#prg-rtl-segments .bit-prb-bcn", "mask-image"), "repeating-linear-gradient(to left");
    }

    [TestMethod]
    public async Task Runtime_ChangingTheParameters_ReRendersAndAnimatesTheBar()
    {
        await OpenAsync(Path);

        await WaitForFillAsync("#prg-runtime", 0.2);

        await Page.Locator("#btn-set-percent").ClickAsync();
        await Expect(Page.Locator("#prg-runtime .bit-prb-bcn")).ToHaveAttributeAsync("aria-valuenow", "80");
        await Expect(Page.Locator("#prg-runtime .bit-prb-pnm")).ToHaveTextAsync("80 %");
        await WaitForFillAsync("#prg-runtime", 0.8);

        await Page.Locator("#btn-set-color").ClickAsync();
        await Expect(Page.Locator("#prg-runtime .bit-prb-bar")).ToHaveCSSAsync("background-color", await ResolveAsync("color", "var(--bit-clr-suc)"));

        await Page.Locator("#btn-toggle-indeterminate").ClickAsync();
        await Expect(Page.Locator("#prg-runtime .bit-prb-ind")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#prg-runtime .bit-prb-bcn")).Not.ToHaveAttributeAsync("aria-valuenow", new Regex(".*"));
        await Page.Locator("#btn-toggle-indeterminate").ClickAsync();

        await Page.Locator("#btn-toggle-circular").ClickAsync();
        await Expect(Page.Locator("#prg-runtime svg")).ToHaveAttributeAsync("aria-valuenow", "80");
        await Expect(Page.Locator("#prg-runtime .bit-prb-ctx")).ToHaveTextAsync("80 %");
    }

    [TestMethod]
    public async Task Unmount_WhileADelayIsPending_LeavesNoError()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#prg-mounted")).ToHaveClassAsync(HasClass("bit-prb-dly"));

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#prg-mounted")).ToHaveCountAsync(0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // Waits until the bar of a linear progress fills that share of its container, its width transition included.
    private Task WaitForFillAsync(string root, double share)
    {
        return Page.WaitForFunctionAsync(
            @"([root, share]) => {
                const bar = document.querySelector(root + ' .bit-prb-bar').getBoundingClientRect();
                const container = document.querySelector(root + ' .bit-prb-bcn').getBoundingClientRect();
                return Math.abs(bar.width / container.width - share) < 0.01;
            }", new object[] { root, share });
    }

    private async Task<(int Width, int Height)> SizeAsync(string selector)
    {
        var box = await Page.Locator(selector).First.BoundingBoxAsync();

        return ((int)Math.Round(box!.Width), (int)Math.Round(box.Height));
    }

    private static string Css(double value) => Math.Round(value, 3).ToString(CultureInfo.InvariantCulture);

    private static double Px(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

    private static double Seconds(string value) => value.EndsWith("ms")
        ? double.Parse(value[..^2], CultureInfo.InvariantCulture) / 1000
        : double.Parse(value.TrimEnd('s'), CultureInfo.InvariantCulture);

    // What a CSS value - a theme token, typically - resolves to on this page, read off a probe element.
    private Task<string> ResolveAsync(string property, string value)
    {
        return Page.EvaluateAsync<string>(
            @"([property, value]) => {
                const probe = document.createElement('div');
                probe.style.setProperty(property, value);
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).getPropertyValue(property);
                probe.remove();
                return resolved;
            }", new[] { property, value });
    }
}
