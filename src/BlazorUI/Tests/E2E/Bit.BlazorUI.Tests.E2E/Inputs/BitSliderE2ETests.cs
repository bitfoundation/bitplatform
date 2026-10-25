using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// End-to-end tests of BitSlider (<c>/e2e/slider</c>, <c>/e2e/slider/autofocus</c>): the value set by a click on the
/// rail, by dragging the thumb with the mouse (single, ranged, vertical, right-to-left, the draggable band) and by the
/// keyboard (arrows, Page keys, LargeStep, Home/End); Min / Max / Step, marks and their labels and template,
/// RestrictToMarks, the origin and inversion of the fill, NoFill, the thumb label in every mode, the range constraints
/// (crossing, NoSwap, MinRange, MaxRange, Pushable), value formatting, one-way and two-way binding of Value, LowerValue,
/// UpperValue and RangeValue, OnChange / OnChangeEnd / OnRangeChange / OnRangeChangeEnd / OnFocusIn / OnFocusOut, EditForm
/// validation of a value and of a range, the accessibility tree (names, group, descriptions, value text), FocusAsync and
/// FocusUpperAsync, Escape inside a modal, the BitParams cascade, every Color and Size, Style / Class / Styles /
/// Classes, the public CSS variables, the disabled and read-only contracts, runtime re-renders, prerendering,
/// unmounting and reduced motion.
/// </summary>
[TestClass]
public class BitSliderE2ETests : E2ETestBase
{
    private const string Route = "/e2e/slider";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Input(string id) => Page.Locator($"#{id} input.bit-sld-inp").First;

    private ILocator Lower(string id) => Page.Locator($"#{id} input.bit-sld-inp-lwr");

    private ILocator Upper(string id) => Page.Locator($"#{id} input.bit-sld-inp-upr");

    private ILocator Thumb(string id) => Page.Locator($"#{id} .bit-sld-thb").First;

    private ILocator Fill(string id) => Page.Locator($"#{id} .bit-sld-fil").First;

    private ILocator ValueLabels(string id) => Page.Locator($"#{id} .bit-sld-vlb");

    private Task ExpectTextAsync(string id, string text) => Expect(Page.Locator($"#{id}")).ToHaveTextAsync(text);

    private async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no layout box.");
        return box;
    }

    private enum Axis { Ltr, Rtl, Vertical }

    /// <summary>
    /// The page point at the given fraction of the scale of a slider: along its track from the near end, which is the
    /// left of a left-to-right slider, the right of a right-to-left one and the bottom of a vertical one.
    /// </summary>
    private async Task<(float X, float Y)> PointAsync(string id, double fraction, Axis axis = Axis.Ltr)
    {
        // The mouse works in viewport coordinates, so the slider is brought to the middle of the viewport first.
        await Page.Locator($"#{id}").EvaluateAsync("el => el.scrollIntoView({ block: 'center', inline: 'nearest' })");
        var track = await BoxAsync(Page.Locator($"#{id} .bit-sld-trk"));

        return axis switch
        {
            Axis.Vertical => ((float)(track.X + track.Width / 2), (float)(track.Y + track.Height * (1 - fraction))),
            Axis.Rtl => ((float)(track.X + track.Width * (1 - fraction)), (float)(track.Y + track.Height / 2)),
            _ => ((float)(track.X + track.Width * fraction), (float)(track.Y + track.Height / 2)),
        };
    }

    private async Task ClickAtAsync(string id, double fraction, Axis axis = Axis.Ltr)
    {
        var (x, y) = await PointAsync(id, fraction, axis);
        await Page.Mouse.ClickAsync(x, y);
    }

    private async Task DragAsync(string id, double from, double to, Axis axis = Axis.Ltr)
    {
        var (x1, y1) = await PointAsync(id, from, axis);
        var (x2, y2) = await PointAsync(id, to, axis);

        await Page.Mouse.MoveAsync(x1, y1);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x2, y2, new() { Steps = 12 });
        await Page.Mouse.UpAsync();
    }

    /// <summary>
    /// The position of the centre of a drawn element along the scale of its slider, as a fraction of the track.
    /// </summary>
    private async Task<double> FractionOfAsync(string id, ILocator element, Axis axis = Axis.Ltr)
    {
        var track = await BoxAsync(Page.Locator($"#{id} .bit-sld-trk"));
        var box = await BoxAsync(element);
        var cx = box.X + box.Width / 2;
        var cy = box.Y + box.Height / 2;

        return axis switch
        {
            Axis.Vertical => (track.Y + track.Height - cy) / track.Height,
            Axis.Rtl => (track.X + track.Width - cx) / track.Width,
            _ => (cx - track.X) / track.Width,
        };
    }

    /// <summary>
    /// Waits for the drawn thumb to come to rest at the given fraction of the track (its move is transitioned).
    /// </summary>
    private async Task ExpectThumbAtAsync(string id, double fraction, Axis axis = Axis.Ltr, ILocator? thumb = null)
    {
        thumb ??= Thumb(id);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        double actual;
        do
        {
            actual = await FractionOfAsync(id, thumb, axis);
            if (Math.Abs(actual - fraction) < 0.02) return;
            await NextFramesAsync();
        }
        while (DateTime.UtcNow < deadline);

        Assert.Fail($"The thumb of {id} rests at {actual:0.000} of the track, not at {fraction:0.000}.");
    }

    private Task<string> ResolveColorAsync(string value)
    {
        return Page.EvaluateAsync<string>(@"value => {
            const probe = document.createElement('span');
            probe.style.color = value;
            document.body.appendChild(probe);
            const color = getComputedStyle(probe).color;
            probe.remove();
            return color;
        }", value);
    }

    private static double Px(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

    private Task<string> CssAsync(ILocator locator, string property) => locator.EvaluateAsync<string>($"el => getComputedStyle(el).getPropertyValue('{property}')");

    // ---------------------------------------------------------------- lifecycle

    [TestMethod]
    public async Task Prerender_ThePageHtml_ContainsTheSliderBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"sld-basic\"");
        StringAssert.Contains(html, "type=\"range\"");
        StringAssert.Contains(html, "bit-sld-thb");
        StringAssert.Contains(html, ">Loading<");

        await OpenAsync(Route);
        await Expect(Input("sld-basic")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Unmount_RemovingTheSlider_LeavesNoServerOrConsoleError()
    {
        await OpenAsync(Route);

        await Expect(Root("sld-unmount")).ToBeVisibleAsync();
        await Page.ClickAsync("#btn-unmount");
        await Expect(Root("sld-unmount")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Lower("sld-unmount")).ToHaveCountAsync(1);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Root("sld-unmount")).ToHaveCountAsync(0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Runtime_ChangingParametersFromThePage_ReRendersTheSlider()
    {
        await OpenAsync(Route);

        var root = Root("sld-runtime");
        await Expect(Thumb("sld-runtime")).ToHaveAttributeAsync("style", new Regex("inset-inline-start:80%"));

        var thumbBefore = await CssAsync(Thumb("sld-runtime"), "width");
        await Page.ClickAsync("#btn-runtime-color");
        await Expect(root).ToHaveClassAsync(HasClass("bit-sld-err"));
        await Page.ClickAsync("#btn-runtime-size");
        await Expect(root).ToHaveClassAsync(HasClass("bit-sld-lg"));
        Assert.IsTrue(Px(await CssAsync(Thumb("sld-runtime"), "width")) > Px(thumbBefore));

        await Page.ClickAsync("#btn-runtime-max");
        await Expect(Input("sld-runtime")).ToHaveAttributeAsync("max", "20");
        await Expect(Thumb("sld-runtime")).ToHaveAttributeAsync("style", new Regex("inset-inline-start:40%"));

        await Expect(root.Locator(".bit-sld-mrk")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-runtime-marks");
        await Expect(root.Locator(".bit-sld-mrk")).ToHaveCountAsync(21);

        await Page.ClickAsync("#btn-runtime-label");
        await Expect(root.Locator(".bit-sld-lbl")).ToHaveTextAsync("Changed label");
    }

    // ---------------------------------------------------------------- basic

    [TestMethod]
    public async Task Basic_ClickingTheRail_MovesTheThumbThereAndFiresOnChange()
    {
        await OpenAsync(Route);

        var input = Input("sld-basic");
        await Expect(input).ToHaveAttributeAsync("aria-valuenow", "0");
        await Expect(input).ToHaveAttributeAsync("min", "0");
        await Expect(input).ToHaveAttributeAsync("max", "10");
        await Expect(input).ToHaveAttributeAsync("step", "1");
        await Expect(ValueLabels("sld-basic")).ToHaveTextAsync("0");
        await Expect(Fill("sld-basic")).ToHaveAttributeAsync("style", new Regex("^inset-inline-start:0%;width:0%"));

        await ClickAtAsync("sld-basic", 0.7);

        await ExpectTextAsync("basic-value", "7");
        await ExpectTextAsync("basic-change-count", "1");
        await Expect(input).ToHaveAttributeAsync("aria-valuenow", "7");
        await Expect(input).ToHaveValueAsync("7");
        await Expect(ValueLabels("sld-basic")).ToHaveTextAsync("7");
        await Expect(Fill("sld-basic")).ToHaveAttributeAsync("style", new Regex("^inset-inline-start:0%;width:70%"));
        await ExpectThumbAtAsync("sld-basic", 0.7);
    }

    [TestMethod]
    public async Task Basic_DraggingTheThumb_FollowsThePointerAndLandsOnAStep()
    {
        await OpenAsync(Route);

        await ClickAtAsync("sld-basic", 0.8);
        await ExpectTextAsync("basic-value", "8");

        await DragAsync("sld-basic", 0.8, 0.31);

        await ExpectTextAsync("basic-value", "3");
        await ExpectThumbAtAsync("sld-basic", 0.3);
        Assert.IsTrue(int.Parse(await Page.Locator("#basic-change-count").InnerTextAsync()) >= 3, "The drag did not report its steps.");

        // Dragging past the end holds the value to the end of the scale.
        await DragAsync("sld-basic", 0.3, 1.3);
        await ExpectTextAsync("basic-value", "10");
        await DragAsync("sld-basic", 1, -0.4);
        await ExpectTextAsync("basic-value", "0");
    }

    [TestMethod]
    public async Task Label_IsTiedToTheInputAndNamesTheThumb()
    {
        await OpenAsync(Route);

        var label = Root("sld-basic").Locator("label.bit-sld-lbl");
        await Expect(label).ToHaveTextAsync("Basic slider");
        await Expect(label).ToHaveAttributeAsync("for", "sld-basic-input");
        await Expect(Input("sld-basic")).ToHaveAttributeAsync("id", "sld-basic-input");
        await Expect(Input("sld-basic")).ToHaveAccessibleNameAsync("Basic slider");
        await Expect(Page.GetByRole(AriaRole.Slider, new() { Name = "Basic slider", Exact = true })).ToHaveCountAsync(1);

        await label.ClickAsync();
        await Expect(Input("sld-basic")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task DefaultValueAndStep_StartTheSliderThereAndSnapToTheStep()
    {
        await OpenAsync(Route);

        await Expect(Input("sld-default")).ToHaveValueAsync("7");
        await Expect(ValueLabels("sld-default")).ToHaveTextAsync("7");
        await ExpectThumbAtAsync("sld-default", 0.7);

        await Expect(Input("sld-percent")).ToHaveAttributeAsync("step", "10");
        await ClickAtAsync("sld-percent", 0.33);
        await Expect(ValueLabels("sld-percent")).ToHaveTextAsync("30");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(ValueLabels("sld-percent")).ToHaveTextAsync("40");
    }

    // ---------------------------------------------------------------- disabled & read-only

    [TestMethod]
    public async Task Disabled_RendersTheDisabledMarkupAndIgnoresARealClickAndDrag()
    {
        await OpenAsync(Route);

        var root = Root("sld-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("sld-disabled")).ToBeDisabledAsync();
        await Expect(Input("sld-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Root("sld-range-disabled").Locator("input[disabled][aria-disabled='true']")).ToHaveCountAsync(2);
        await Expect(Root("sld-range-disabled")).ToHaveClassAsync(HasClass("bit-dis"));

        await ClickAtAsync("sld-disabled", 0.9);
        await DragAsync("sld-disabled", 0.5, 0.1);
        await root.Locator(".bit-sld-lbl").ClickAsync(new() { Force = true });
        await NextFramesAsync();

        await ExpectTextAsync("disabled-clicks", "0");
        await Expect(ValueLabels("sld-disabled")).ToHaveTextAsync("5");
        await Expect(Fill("sld-disabled")).ToHaveAttributeAsync("style", new Regex("width:50%"));

        // The disabled paint: the fill and the thumb in the disabled color, the labels in the disabled foreground.
        Assert.AreNotEqual(await CssAsync(Fill("sld-default"), "background-color"), await CssAsync(Fill("sld-disabled"), "background-color"));
        Assert.AreEqual(await CssAsync(Fill("sld-disabled"), "background-color"), await CssAsync(Thumb("sld-disabled"), "border-top-color"));
        await Expect(Input("sld-disabled")).ToHaveCSSAsync("cursor", "not-allowed");

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("sld-disabled")).ToBeEnabledAsync();
        await Expect(Input("sld-disabled")).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));

        await ClickAtAsync("sld-disabled", 0.9);
        await ExpectTextAsync("disabled-value", "9");
        Assert.IsTrue(int.Parse(await Page.Locator("#disabled-clicks").InnerTextAsync()) >= 1, "The page's handlers did not run once enabled.");
    }

    [TestMethod]
    public async Task ReadOnly_PinsTheValueAgainstThePointerAndTheKeyboard()
    {
        await OpenAsync(Route);

        var root = Root("sld-readonly");
        var input = Input("sld-readonly");
        await Expect(root).ToHaveClassAsync(HasClass("bit-sld-rdl"));
        await Expect(input).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(input).ToHaveAttributeAsync("min", "6");
        await Expect(input).ToHaveAttributeAsync("max", "6");
        await Expect(input).ToHaveAttributeAsync("aria-valuemin", "0");
        await Expect(input).ToHaveAttributeAsync("aria-valuemax", "10");
        await Expect(input).ToBeEnabledAsync();

        await ClickAtAsync("sld-readonly", 0.1);
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Page.Keyboard.PressAsync("Home");
        await NextFramesAsync();

        await ExpectTextAsync("readonly-value", "0");
        await Expect(ValueLabels("sld-readonly")).ToHaveTextAsync("6");
        await ExpectThumbAtAsync("sld-readonly", 0.6);
        await Expect(input).ToHaveCSSAsync("cursor", "default");
    }

    // ---------------------------------------------------------------- range

    [TestMethod]
    public async Task Range_TwoThumbsSelectAGroupedPairFromTheKeyboard()
    {
        await OpenAsync(Route);

        var root = Root("sld-range");
        await Expect(root).ToHaveClassAsync(HasClass("bit-sld-rgd"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-sld-rgd-hrz"));
        await Expect(root).ToHaveAttributeAsync("role", "group");
        await Expect(root).ToHaveAccessibleNameAsync("Basic range");
        await Expect(root.Locator(".bit-sld-thb")).ToHaveCountAsync(2);
        await Expect(ValueLabels("sld-range")).ToHaveCountAsync(2);

        await Upper("sld-range").FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await ExpectTextAsync("range-value", "0-10");

        await Lower("sld-range").FocusAsync();
        for (var i = 0; i < 3; i++) await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectTextAsync("range-value", "3-10");

        await Expect(Fill("sld-range")).ToHaveAttributeAsync("style", new Regex("^inset-inline-start:30%;width:70%"));
        await Expect(ValueLabels("sld-range").Nth(0)).ToHaveTextAsync("3");
        await Expect(ValueLabels("sld-range").Nth(1)).ToHaveTextAsync("10");
        await Expect(Lower("sld-range")).ToHaveAttributeAsync("aria-valuenow", "3");
        await Expect(Upper("sld-range")).ToHaveAttributeAsync("aria-valuenow", "10");
    }

    [TestMethod]
    public async Task Range_DraggingEitherThumb_MovesItsEndAndFormatsTheLabels()
    {
        await OpenAsync(Route);

        await Expect(ValueLabels("sld-price").Nth(0)).ToHaveTextAsync("$200");
        await Expect(ValueLabels("sld-price").Nth(1)).ToHaveTextAsync("$750");

        await DragAsync("sld-age", 0.2, 0.3);
        await ExpectTextAsync("age-value", "30-40");

        await DragAsync("sld-age", 0.4, 0.75);
        await ExpectTextAsync("age-value", "30-75");
        await Expect(Fill("sld-age")).ToHaveAttributeAsync("style", new Regex("^inset-inline-start:30%;width:45%"));
    }

    [TestMethod]
    public async Task RangeValueBinding_ThePageSetsThePairAndAReversedPairIsOrderedAndReportedBack()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-age-set");
        await ExpectThumbAtAsync("sld-age", 0.1, thumb: Root("sld-age").Locator(".bit-sld-thb-lwr"));
        await ExpectThumbAtAsync("sld-age", 0.9, thumb: Root("sld-age").Locator(".bit-sld-thb-upr"));

        await Page.ClickAsync("#btn-age-reversed");
        await ExpectTextAsync("age-value", "20-80");
        await Expect(Lower("sld-age")).ToHaveAttributeAsync("aria-valuenow", "20");
        await Expect(Upper("sld-age")).ToHaveAttributeAsync("aria-valuenow", "80");
    }

    // ---------------------------------------------------------------- marks

    [TestMethod]
    public async Task Marks_ShowMarksDrawsOneAtEveryStepAndHighlightsTheFilledOnes()
    {
        await OpenAsync(Route);

        var marks = Root("sld-show-marks").Locator(".bit-sld-mrk");
        await Expect(marks).ToHaveCountAsync(11);
        await Expect(Root("sld-show-marks").Locator(".bit-sld-mrk-act")).ToHaveCountAsync(7);
        await Expect(Root("sld-show-marks").Locator(".bit-sld-mlb")).ToHaveCountAsync(0);

        // Each mark sits on its value along the track.
        Assert.AreEqual(0.5, await FractionOfAsync("sld-show-marks", marks.Nth(5)), 0.02);
        Assert.AreEqual(1, await FractionOfAsync("sld-show-marks", marks.Nth(10)), 0.02);

        Assert.AreNotEqual(await CssAsync(marks.Nth(0), "background-color"), await CssAsync(marks.Nth(10), "background-color"));

        await ClickAtAsync("sld-show-marks", 0.2);
        await Expect(Root("sld-show-marks").Locator(".bit-sld-mrk-act")).ToHaveCountAsync(3);
    }

    [TestMethod]
    public async Task MarkLabels_WriteTheValuesUnderTheMarksAtTheMarkStep()
    {
        await OpenAsync(Route);

        var labels = Root("sld-mark-labels").Locator(".bit-sld-mlb");
        await Expect(labels).ToHaveTextAsync(["0", "1", "2", "3", "4", "5"]);
        Assert.AreEqual(0.6, await FractionOfAsync("sld-mark-labels", labels.Nth(3)), 0.03);

        // The labels sit under the track.
        var track = await BoxAsync(Root("sld-mark-labels").Locator(".bit-sld-trk"));
        Assert.IsTrue((await BoxAsync(labels.Nth(0))).Y >= track.Y + track.Height - 1);

        await Expect(Root("sld-mark-step").Locator(".bit-sld-mlb")).ToHaveTextAsync(["$0", "$200", "$400", "$600", "$800", "$1,000"]);
        await Expect(Root("sld-mark-step").Locator(".bit-sld-mrk")).ToHaveCountAsync(6);
    }

    [TestMethod]
    public async Task CustomMarks_KeepTheirLabelsClassesAndStylesAndSkipTheOutOfRangeOnes()
    {
        await OpenAsync(Route);

        await Expect(Root("sld-custom-marks").Locator(".bit-sld-mlb")).ToHaveTextAsync(["Draft", "Low", "Medium", "High", "Lossless"]);
        await Expect(Root("sld-custom-marks").Locator(".bit-sld-mrk")).ToHaveCountAsync(5);
        await Expect(ValueLabels("sld-custom-marks")).ToHaveCountAsync(0);

        var load = Root("sld-load-marks");
        await Expect(load.Locator(".bit-sld-mlb")).ToHaveTextAsync(["0", "25", "50", "75", "limit", "100"]);
        await Expect(load.Locator(".bit-sld-mrk.limit-mark")).ToHaveCSSAsync("background-color", "rgb(255, 99, 71)");
        await Expect(load.Locator(".bit-sld-mlb.limit-mark")).ToHaveCSSAsync("color", "rgb(255, 99, 71)");
        Assert.AreEqual(0.9, await FractionOfAsync("sld-load-marks", load.Locator(".bit-sld-mrk.limit-mark")), 0.02);
    }

    [TestMethod]
    public async Task MarkLabelTemplate_LabelsEveryMarkWithCustomContent()
    {
        await OpenAsync(Route);

        var faces = Root("sld-mark-template").Locator(".bit-sld-mlb .face");
        await Expect(faces).ToHaveTextAsync(["awful", "bad", "okay", "good", "great"]);
        await Expect(faces.Nth(2)).ToHaveAttributeAsync("data-value", "2");
        await Expect(Root("sld-mark-template").Locator(".bit-sld-mrk")).ToHaveCountAsync(5);
    }

    [TestMethod]
    public async Task RestrictToMarks_ThePointerAndTheKeysLandOnMarksOnly()
    {
        await OpenAsync(Route);

        // 300 of 2000 is nearer 256 than 512.
        await ClickAtAsync("sld-restrict", 0.15);
        await ExpectTextAsync("restrict-value", "256");

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectTextAsync("restrict-value", "512");
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectTextAsync("restrict-value", "1000");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectTextAsync("restrict-value", "512");

        await ClickAtAsync("sld-restrict", 0.9);
        await ExpectTextAsync("restrict-value", "2000");
    }

    [TestMethod]
    public async Task RestrictToMarks_ARangeSnapsEachEndToAMarkAndKeepsItsMinRange()
    {
        await OpenAsync(Route);

        await Expect(Root("sld-restrict-range").Locator(".bit-sld-mrk")).ToHaveCountAsync(7);

        await DragAsync("sld-restrict-range", 8 / 24d, 2.6 / 24d);
        await ExpectTextAsync("restrict-range-value", "4-16");

        await DragAsync("sld-restrict-range", 16 / 24d, 5 / 24d);
        await ExpectTextAsync("restrict-range-value", "4-8");
        await Expect(ValueLabels("sld-restrict-range").Nth(1)).ToHaveTextAsync("8h");
    }

    // ---------------------------------------------------------------- vertical

    [TestMethod]
    public async Task Vertical_StandsUprightFillsFromTheBottomAndFollowsTheKeys()
    {
        await OpenAsync(Route);

        var root = Root("sld-vertical");
        await Expect(root).ToHaveClassAsync(HasClass("bit-sld-vrt"));
        await Expect(Input("sld-vertical")).ToHaveAttributeAsync("aria-orientation", "vertical");

        var box = await BoxAsync(root.Locator(".bit-sld-box"));
        Assert.AreEqual(192, box.Height, 1, "The default length is 12rem.");
        Assert.IsTrue(box.Height > box.Width);

        await Expect(Fill("sld-vertical")).ToHaveAttributeAsync("style", new Regex("^bottom:0%;height:40%"));
        await ExpectThumbAtAsync("sld-vertical", 0.4, Axis.Vertical);

        await ClickAtAsync("sld-vertical", 0.8, Axis.Vertical);
        await ExpectTextAsync("vertical-value", "8");
        await ExpectThumbAtAsync("sld-vertical", 0.8, Axis.Vertical);

        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectTextAsync("vertical-value", "9");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectTextAsync("vertical-value", "7");
    }

    [TestMethod]
    public async Task Vertical_DraggingTheThumbUpAndDown_ChangesTheValue()
    {
        await OpenAsync(Route);

        await DragAsync("sld-vertical", 0.4, 0.91, Axis.Vertical);
        await ExpectTextAsync("vertical-value", "9");
        await DragAsync("sld-vertical", 0.9, 0.19, Axis.Vertical);
        await ExpectTextAsync("vertical-value", "2");
    }

    [TestMethod]
    public async Task Vertical_ARangeStacksItsThumbsAndLabels()
    {
        await OpenAsync(Route);

        var root = Root("sld-vertical-range");
        await Expect(root).ToHaveClassAsync(HasClass("bit-sld-rgd-vrt"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-sld-vrt"));
        await Expect(Fill("sld-vertical-range")).ToHaveAttributeAsync("style", new Regex("^bottom:20%;height:50%"));

        // The upper value is the label on top, the lower one the label at the bottom.
        await Expect(ValueLabels("sld-vertical-range").Nth(0)).ToHaveTextAsync("7");
        await Expect(ValueLabels("sld-vertical-range").Nth(1)).ToHaveTextAsync("2");
        var top = await BoxAsync(ValueLabels("sld-vertical-range").Nth(0));
        var bottom = await BoxAsync(ValueLabels("sld-vertical-range").Nth(1));
        Assert.IsTrue(top.Y < bottom.Y);

        await DragAsync("sld-vertical-range", 0.7, 0.9, Axis.Vertical);
        await ExpectTextAsync("vertical-range-value", "2-9");
        await DragAsync("sld-vertical-range", 0.2, 0.41, Axis.Vertical);
        await ExpectTextAsync("vertical-range-value", "4-9");
    }

    [TestMethod]
    public async Task Vertical_MarksAndTheLengthVariable()
    {
        await OpenAsync(Route);

        await Expect(Root("sld-vertical-marks").Locator(".bit-sld-mlb")).ToHaveTextAsync(["0", "2", "4", "6", "8", "10"]);
        var labels = Root("sld-vertical-marks").Locator(".bit-sld-mlb");
        Assert.IsTrue((await BoxAsync(labels.Nth(0))).Y > (await BoxAsync(labels.Nth(5))).Y, "The marks do not run bottom-up.");

        Assert.AreEqual(288, (await BoxAsync(Root("sld-vertical-tall").Locator(".bit-sld-box"))).Height, 1);
    }

    // ---------------------------------------------------------------- fill

    [TestMethod]
    public async Task OriginFromZero_GrowsTheFillOutOfZeroInEitherDirection()
    {
        await OpenAsync(Route);

        await Expect(Fill("sld-origin-zero")).ToHaveAttributeAsync("style", new Regex("^inset-inline-start:50%;width:30%"));
        var tick = Root("sld-origin-zero").Locator(".bit-sld-ztk");
        await Expect(tick).ToHaveAttributeAsync("style", new Regex("^inset-inline-start:50%"));
        Assert.AreEqual(0.5, await FractionOfAsync("sld-origin-zero", tick), 0.02);

        await ClickAtAsync("sld-origin-zero", 0.3);
        await ExpectTextAsync("origin-zero-value", "-2");
        await Expect(Fill("sld-origin-zero")).ToHaveAttributeAsync("style", new Regex("^inset-inline-start:30%;width:20%"));

        await Expect(Fill("sld-origin")).ToHaveAttributeAsync("style", new Regex("^inset-inline-start:60%;width:25%"));
        await Expect(Root("sld-origin").Locator(".bit-sld-ztk")).ToHaveAttributeAsync("style", new Regex("^inset-inline-start:60%"));
    }

    [TestMethod]
    public async Task Inverted_FillsWhatLiesBeyondTheValueAndOutsideARange()
    {
        await OpenAsync(Route);

        await Expect(Root("sld-inverted")).ToHaveClassAsync(HasClass("bit-sld-ivt"));
        await Expect(Fill("sld-inverted")).ToHaveAttributeAsync("style", new Regex("^inset-inline-start:30%;width:70%"));

        var fills = Root("sld-inverted-range").Locator(".bit-sld-fil");
        await Expect(fills).ToHaveCountAsync(2);
        await Expect(fills.Nth(0)).ToHaveAttributeAsync("style", new Regex("^inset-inline-start:0%;width:35%"));
        await Expect(fills.Nth(1)).ToHaveAttributeAsync("style", new Regex("^inset-inline-start:65%;width:35%"));

        // The marks outside the thumbs are the highlighted ones: 0..30 and 70..100.
        await Expect(Root("sld-inverted-range").Locator(".bit-sld-mrk")).ToHaveCountAsync(11);
        await Expect(Root("sld-inverted-range").Locator(".bit-sld-mrk-act")).ToHaveCountAsync(8);
    }

    [TestMethod]
    public async Task NoFill_DrawsTheBareRailAndHighlightsNoMark()
    {
        await OpenAsync(Route);

        await Expect(Root("sld-nofill").Locator(".bit-sld-fil")).ToHaveCountAsync(0);
        await Expect(Root("sld-nofill").Locator(".bit-sld-mrk")).ToHaveCountAsync(11);
        await Expect(Root("sld-nofill").Locator(".bit-sld-mrk-act")).ToHaveCountAsync(0);
        await Expect(Root("sld-nofill-range").Locator(".bit-sld-fil")).ToHaveCountAsync(0);
        await Expect(Root("sld-nofill-range").Locator(".bit-sld-thb")).ToHaveCountAsync(2);
        await ExpectThumbAtAsync("sld-nofill", 0.45);

        await Expect(Root("sld-fill-marks").Locator(".bit-sld-mrk-act")).ToHaveCountAsync(5);
    }

    // ---------------------------------------------------------------- thumb label

    [TestMethod]
    public async Task ThumbLabel_OffRendersNoneAndOnShowsTheFormattedValueAboveTheThumb()
    {
        await OpenAsync(Route);

        await Expect(Root("sld-tl-off").Locator(".bit-sld-tlb")).ToHaveCountAsync(0);

        var on = Root("sld-tl-on");
        await Expect(on).ToHaveClassAsync(HasClass("bit-sld-tlo"));
        var label = on.Locator(".bit-sld-tlb");
        await Expect(label).ToHaveTextAsync("35%");
        await Expect(label).ToHaveCSSAsync("opacity", "1");
        await Expect(label).ToHaveAttributeAsync("aria-hidden", "true");

        var labelBox = await BoxAsync(label);
        var thumbBox = await BoxAsync(Thumb("sld-tl-on"));
        Assert.IsTrue(labelBox.Y + labelBox.Height <= thumbBox.Y + 0.5, "The label is not above the thumb.");
        Assert.AreEqual(thumbBox.X + thumbBox.Width / 2, labelBox.X + labelBox.Width / 2, 1.5, "The label is not centred on the thumb.");

        await Input("sld-tl-on").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(label).ToHaveTextAsync("40%");

        await Expect(Root("sld-tl-range").Locator(".bit-sld-tlb")).ToHaveTextAsync(["$250", "$700"]);
    }

    [TestMethod]
    public async Task ThumbLabel_AutoShowsWhileHoveredOrFocused()
    {
        await OpenAsync(Route);

        var auto = Root("sld-tl-auto");
        await Expect(auto).ToHaveClassAsync(HasClass("bit-sld-tla"));
        var label = auto.Locator(".bit-sld-tlb");
        await Expect(label).ToHaveCSSAsync("opacity", "0");

        await auto.Locator(".bit-sld-box").HoverAsync();
        await Expect(label).ToHaveCSSAsync("opacity", "1");

        await Page.Mouse.MoveAsync(0, 0);
        await Expect(label).ToHaveCSSAsync("opacity", "0");

        await Page.FocusAsync("#sld-tl-off-input");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Input("sld-tl-auto")).ToBeFocusedAsync();
        await Expect(label).ToHaveCSSAsync("opacity", "1");
        await Expect(label).ToHaveTextAsync("4");
    }

    [TestMethod]
    public async Task ThumbLabelTemplate_DrawsTheLabelFromTheValue()
    {
        await OpenAsync(Route);

        var template = Root("sld-tl-template").Locator(".bit-sld-tlb .thumb-template");
        await Expect(template).ToHaveTextAsync("vol 60");
        await Input("sld-tl-template").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(template).ToHaveTextAsync("vol 65");
    }

    // ---------------------------------------------------------------- constraints

    [TestMethod]
    public async Task Constraints_AFreePairCrossesAndStaysOrdered()
    {
        await OpenAsync(Route);

        await DragAsync("sld-free", 0.3, 0.8);
        await ExpectTextAsync("free-value", "70-80");
        await Expect(ValueLabels("sld-free").Nth(0)).ToHaveTextAsync("70");
        await Expect(ValueLabels("sld-free").Nth(1)).ToHaveTextAsync("80");

        // The input that crossed now drives the upper end, and announces it.
        await Expect(Lower("sld-free")).ToHaveAttributeAsync("aria-valuenow", "80");
        await Expect(Lower("sld-free")).ToHaveClassAsync(HasClass("bit-sld-inp-far"));
    }

    [TestMethod]
    public async Task Constraints_NoSwapStopsAThumbAtTheOther()
    {
        await OpenAsync(Route);

        await DragAsync("sld-noswap", 0.3, 0.9);
        await ExpectTextAsync("noswap-value", "70-70");
        await Expect(Lower("sld-noswap")).ToHaveAttributeAsync("aria-valuemax", "70");
    }

    [TestMethod]
    public async Task Constraints_MinRangeKeepsTheThumbsApart()
    {
        await OpenAsync(Route);

        await DragAsync("sld-minrange", 0.2, 0.9);
        await ExpectTextAsync("minrange-value", "40-60");
        await DragAsync("sld-minrange", 0.6, 0.1);
        await ExpectTextAsync("minrange-value", "40-60");
        await Expect(Upper("sld-minrange")).ToHaveAttributeAsync("aria-valuemin", "60");
    }

    [TestMethod]
    public async Task Constraints_MaxRangeKeepsTheThumbsWithinReachAndNarrowsAWiderPair()
    {
        await OpenAsync(Route);

        await DragAsync("sld-maxrange", 0.3, 0.02);
        await ExpectTextAsync("maxrange-value", "20-50");
        await DragAsync("sld-maxrange", 0.5, 0.98);
        await ExpectTextAsync("maxrange-value", "20-50");

        await ExpectTextAsync("narrowed-value", "10-50");
    }

    [TestMethod]
    public async Task Constraints_PushableShovesTheOtherThumbAlong()
    {
        await OpenAsync(Route);

        await DragAsync("sld-pushable", 0.2, 0.5);
        await ExpectTextAsync("pushable-value", "50-70");

        await DragAsync("sld-pushable", 0.5, 0.995);
        await ExpectTextAsync("pushable-value", "80-100");
    }

    // ---------------------------------------------------------------- draggable band

    [TestMethod]
    public async Task DraggableTrack_DraggingTheBandMovesTheWholeRangeAndKeepsItsWidth()
    {
        await OpenAsync(Route);

        await Expect(Root("sld-band").Locator("input.bit-sld-inp-bar")).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Root("sld-band").Locator("input.bit-sld-inp-bar")).ToHaveAttributeAsync("aria-hidden", "true");

        await DragAsync("sld-band", 0.3, 0.6);
        await ExpectTextAsync("band-value", "50-70");
        await ExpectTextAsync("band-end", "1");

        // The band hands the focus back to the lower thumb when the gesture ends.
        await Expect(Lower("sld-band")).ToBeFocusedAsync();

        // It stops when the leading end reaches the end of the scale.
        await DragAsync("sld-band", 0.6, 1.2);
        await ExpectTextAsync("band-value", "80-100");
    }

    [TestMethod]
    public async Task DraggableTrack_AFixedWidthBandAndAVerticalBand()
    {
        await OpenAsync(Route);

        await DragAsync("sld-band-fixed", 0.4, 0.1);
        await ExpectTextAsync("band-fixed-value", "0-20");

        await DragAsync("sld-band-vertical", 0.45, 0.65, Axis.Vertical);
        await ExpectTextAsync("band-vertical-value", "50-80");
    }

    // ---------------------------------------------------------------- value display

    [TestMethod]
    public async Task ValueDisplay_FormatsTheLabelsAndTheSpokenValue()
    {
        await OpenAsync(Route);

        await Expect(ValueLabels("sld-currency")).ToHaveTextAsync("$450");
        await Expect(ValueLabels("sld-percentage")).ToHaveTextAsync("69 %");
        await Expect(ValueLabels("sld-decimals")).ToHaveTextAsync("2.5 rem");
        await Expect(Input("sld-currency")).ToHaveAttributeAsync("aria-valuetext", "$450");

        await Expect(ValueLabels("sld-time")).ToHaveTextAsync("12:30");
        await Expect(Input("sld-time")).ToHaveAttributeAsync("aria-valuetext", "12:30");
        await Expect(Root("sld-time").Locator(".bit-sld-mlb")).ToHaveTextAsync(["08:00", "10:00", "12:00", "14:00", "16:00", "18:00"]);

        await Input("sld-time").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(ValueLabels("sld-time")).ToHaveTextAsync("13:00");

        await Expect(ValueLabels("sld-no-value")).ToHaveCountAsync(0);
        await Expect(Root("sld-label-template").Locator("label.bit-sld-lbl .template-label")).ToHaveTextAsync("Templated label");
    }

    [TestMethod]
    public async Task ValueLabel_IsWideEnoughForTheWidestValueSoTheTrackDoesNotJump()
    {
        await OpenAsync(Route);

        var before = await BoxAsync(Root("sld-percent").Locator(".bit-sld-trk"));
        await Input("sld-percent").FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(ValueLabels("sld-percent")).ToHaveTextAsync("100");
        var after = await BoxAsync(Root("sld-percent").Locator(".bit-sld-trk"));
        Assert.AreEqual(before.Width, after.Width, 0.5);
        Assert.AreEqual(before.X, after.X, 0.5);
    }

    // ---------------------------------------------------------------- binding

    [TestMethod]
    public async Task OneWayBinding_TheUserCannotMoveItButThePageCan()
    {
        await OpenAsync(Route);

        await ClickAtAsync("sld-one-way", 0.9);
        await NextFramesAsync();
        await Expect(Input("sld-one-way")).ToHaveValueAsync("2");
        await Expect(ValueLabels("sld-one-way")).ToHaveTextAsync("2");
        await ExpectThumbAtAsync("sld-one-way", 0.2);

        await Page.ClickAsync("#btn-one-way");
        await Expect(ValueLabels("sld-one-way")).ToHaveTextAsync("8");
        await ExpectThumbAtAsync("sld-one-way", 0.8);
    }

    [TestMethod]
    public async Task TwoWayBinding_FlowsBothWaysAndAnOutOfRangeValueIsClamped()
    {
        await OpenAsync(Route);

        await DragAsync("sld-two-way", 0.5, 0.71);
        await ExpectTextAsync("two-way-value", "7");

        await Page.ClickAsync("#btn-two-way");
        await Expect(ValueLabels("sld-two-way")).ToHaveTextAsync("3");
        await Expect(Input("sld-two-way")).ToHaveValueAsync("3");
        await ExpectThumbAtAsync("sld-two-way", 0.3);

        await Page.ClickAsync("#btn-two-way-over");
        await ExpectTextAsync("two-way-value", "10");
        await Expect(ValueLabels("sld-two-way")).ToHaveTextAsync("10");
    }

    [TestMethod]
    public async Task LowerAndUpperValueBinding_FlowBothWays()
    {
        await OpenAsync(Route);

        await DragAsync("sld-lower-upper", 0.2, 0.35);
        await ExpectTextAsync("lower-value", "35");
        await ExpectTextAsync("upper-value", "80");

        await DragAsync("sld-lower-upper", 0.8, 0.6);
        await ExpectTextAsync("upper-value", "60");

        await Page.ClickAsync("#btn-lower-upper");
        await Expect(Lower("sld-lower-upper")).ToHaveAttributeAsync("aria-valuenow", "40");
        await Expect(Upper("sld-lower-upper")).ToHaveAttributeAsync("aria-valuenow", "60");
        await Expect(Fill("sld-lower-upper")).ToHaveAttributeAsync("style", new Regex("^inset-inline-start:40%;width:20%"));
    }

    // ---------------------------------------------------------------- events

    [TestMethod]
    public async Task OnChangeAndOnChangeEnd_ADragReportsEveryStepAndEndsOnce()
    {
        await OpenAsync(Route);

        await DragAsync("sld-events", 0.2, 0.6);

        await ExpectTextAsync("change-value", "60");
        await ExpectTextAsync("change-end-count", "1");
        await ExpectTextAsync("change-end-value", "60");
        Assert.IsTrue(int.Parse(await Page.Locator("#change-count").InnerTextAsync()) > 3, "The drag did not report its steps.");

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectTextAsync("change-value", "61");
        await ExpectTextAsync("change-end-value", "61");
    }

    [TestMethod]
    public async Task OnRangeChangeAndEnd_ADragOfAThumbReportsThePair()
    {
        await OpenAsync(Route);

        await DragAsync("sld-range-events", 0.6, 0.8);

        await ExpectTextAsync("range-change-value", "20-80");
        await ExpectTextAsync("range-change-end-count", "1");
        await ExpectTextAsync("range-change-end-value", "20-80");
        Assert.IsTrue(int.Parse(await Page.Locator("#range-change-count").InnerTextAsync()) > 3, "The drag did not report its steps.");
    }

    [TestMethod]
    public async Task OnFocusInAndOut_FireForEachThumbInTabOrder()
    {
        await OpenAsync(Route);

        await Page.FocusAsync("#before-focus");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Lower("sld-focus")).ToBeFocusedAsync();
        await ExpectTextAsync("focus-in", "1");
        await ExpectTextAsync("focus-type", "focusin");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Upper("sld-focus")).ToBeFocusedAsync();
        await ExpectTextAsync("focus-in", "2");
        await ExpectTextAsync("focus-out", "1");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-focus")).ToBeFocusedAsync();
        await ExpectTextAsync("focus-out", "2");
        await ExpectTextAsync("focus-type", "focusout");
    }

    // ---------------------------------------------------------------- validation

    [TestMethod]
    public async Task Validation_AValueOutsideTheAnnotatedRangeFailsTheFormUntilMoved()
    {
        await OpenAsync(Route);

        var root = Root("sld-form");
        var input = Input("sld-form");
        await Expect(root).ToHaveClassAsync(HasClass("bit-sld-req"));
        await Expect(input).ToHaveAttributeAsync("aria-required", "true");
        await Expect(input).ToHaveAttributeAsync("name", "days");
        await Expect(input).ToHaveAttributeAsync("data-input", "days");
        var asterisk = await root.Locator(".bit-sld-lbl").EvaluateAsync<string>("el => getComputedStyle(el, '::after').content");
        StringAssert.StartsWith(asterisk, "\"*\"");

        await Page.ClickAsync("#sld-form-submit");

        await ExpectTextAsync("form-result", "invalid");
        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(input).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#sld-form-msg")).ToHaveTextAsync("Pick at least 4 days a week");
        await Expect(input).ToHaveAccessibleDescriptionAsync("Pick at least 4 days a week");
        await Expect(Fill("sld-form")).ToHaveCSSAsync("background-color", "rgb(4, 5, 6)");
        await Expect(Thumb("sld-form")).ToHaveCSSAsync("border-top-color", "rgb(4, 5, 6)");

        await input.FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await ExpectTextAsync("form-days", "7");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(input).Not.ToHaveAttributeAsync("aria-invalid", new Regex(".*"));
    }

    [TestMethod]
    public async Task Validation_ARangeBoundThroughRangeValueIsOneValidatedField()
    {
        await OpenAsync(Route);

        await Expect(Lower("sld-form-range")).ToHaveAttributeAsync("name", "budget");
        await Expect(Upper("sld-form-range")).ToHaveAttributeAsync("name", "budget-upper");

        await Page.ClickAsync("#sld-form-submit");
        await Expect(Page.Locator("#sld-form-range-msg")).ToHaveTextAsync("Cover a span of at least 20");
        await Expect(Root("sld-form-range")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Lower("sld-form-range")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Upper("sld-form-range")).ToHaveAttributeAsync("aria-invalid", "true");

        await DragAsync("sld-form-range", 0.5, 0.9);
        await Expect(Root("sld-form-range")).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Page.Locator("#sld-form-range-msg")).ToHaveTextAsync("");

        // Dragging the thumbs together again brings the error straight back, without another submit.
        await DragAsync("sld-form-range", 0.9, 0.5);
        await Expect(Root("sld-form-range")).ToHaveClassAsync(HasClass("bit-inv"));
        await DragAsync("sld-form-range", 0.5, 0.95);

        await Input("sld-form").FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await Page.ClickAsync("#sld-form-submit");
        await ExpectTextAsync("form-result", "valid");
    }

    // ---------------------------------------------------------------- accessibility & keyboard

    [TestMethod]
    public async Task Keyboard_ArrowsHomeEndAndPageKeysMoveTheValue()
    {
        await OpenAsync(Route);

        await Input("sld-keys").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectTextAsync("keys-value", "510");
        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectTextAsync("keys-value", "520");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectTextAsync("keys-value", "510");
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectTextAsync("keys-value", "500");
        await Page.Keyboard.PressAsync("Home");
        await ExpectTextAsync("keys-value", "0");
        await Page.Keyboard.PressAsync("End");
        await ExpectTextAsync("keys-value", "1000");

        // Without a LargeStep the Page keys are the browser's own jump, which is larger than a step.
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Page.Locator("#keys-value")).Not.ToHaveTextAsync("1000");
        Assert.IsTrue(double.Parse(await Page.Locator("#keys-value").InnerTextAsync(), CultureInfo.InvariantCulture) < 990);
    }

    [TestMethod]
    public async Task LargeStep_PinsThePageKeysAndGivesShiftTheSameReach()
    {
        await OpenAsync(Route);

        await Input("sld-largestep").FocusAsync();
        await Page.Keyboard.PressAsync("PageUp");
        await ExpectTextAsync("largestep-value", "500");
        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        await ExpectTextAsync("largestep-value", "600");
        await Page.Keyboard.PressAsync("PageDown");
        await ExpectTextAsync("largestep-value", "500");
        await Page.Keyboard.PressAsync("Shift+ArrowLeft");
        await ExpectTextAsync("largestep-value", "400");
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectTextAsync("largestep-value", "410");
        await Expect(ValueLabels("sld-largestep")).ToHaveTextAsync("410 Hz");
    }

    [TestMethod]
    public async Task LargeStep_OnARangeIsHeldInsideTheConstraints()
    {
        await OpenAsync(Route);

        await Expect(Lower("sld-from-to")).ToHaveAccessibleNameAsync("From");
        await Expect(Upper("sld-from-to")).ToHaveAccessibleNameAsync("To");
        await Expect(Lower("sld-from-to")).ToHaveAttributeAsync("aria-valuemax", "600");
        await Expect(Upper("sld-from-to")).ToHaveAttributeAsync("aria-valuemin", "200");

        await Lower("sld-from-to").FocusAsync();
        await Page.Keyboard.PressAsync("PageUp");
        await ExpectTextAsync("from-to-value", "450-600");
        await Page.Keyboard.PressAsync("PageUp");
        await ExpectTextAsync("from-to-value", "600-600");

        await Upper("sld-from-to").FocusAsync();
        await Page.Keyboard.PressAsync("PageUp");
        await ExpectTextAsync("from-to-value", "600-850");
    }

    [TestMethod]
    public async Task Accessibility_ValueTextGroupNamesAndDescriptions()
    {
        await OpenAsync(Route);

        await Expect(Input("sld-valuetext")).ToHaveAttributeAsync("aria-valuetext", "Medium");
        await Input("sld-valuetext").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Input("sld-valuetext")).ToHaveAttributeAsync("aria-valuetext", "High");
        await Expect(ValueLabels("sld-valuetext")).ToHaveTextAsync("3");

        var group = Root("sld-group");
        await Expect(group).ToHaveAttributeAsync("role", "group");
        await Expect(group).ToHaveAttributeAsync("aria-label", "Price range");
        await Expect(Lower("sld-group")).ToHaveAccessibleNameAsync("Minimum price");
        await Expect(Upper("sld-group")).ToHaveAccessibleNameAsync("Maximum price");
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Price range" })).ToHaveCountAsync(1);

        await Expect(Input("sld-description")).ToHaveAccessibleDescriptionAsync("In thousands of euros.");
        await Expect(Root("sld-description").Locator(".bit-sld-dsc")).ToHaveCSSAsync("position", "absolute");
        Assert.IsTrue((await BoxAsync(Root("sld-description").Locator(".bit-sld-dsc"))).Width <= 1);

        var labelled = Root("sld-labelled-group");
        await Expect(labelled).ToHaveAccessibleNameAsync("Delivery window");
        await Expect(Lower("sld-labelled-group")).ToHaveAccessibleNameAsync("Earliest");
        await Expect(Upper("sld-labelled-group")).ToHaveAccessibleNameAsync("Latest");
        await Expect(Upper("sld-labelled-group")).ToHaveAccessibleDescriptionAsync("Both ends are days from today.");

        await Expect(Lower("sld-template-group")).ToHaveAccessibleNameAsync("Frequency band");
        await Expect(Upper("sld-template-group")).ToHaveAccessibleNameAsync("Frequency band");
        await Expect(Root("sld-template-group")).ToHaveAccessibleNameAsync("Frequency band");

        await Expect(Input("sld-aria-label")).ToHaveAccessibleNameAsync("Brightness");
        await Expect(Input("sld-tabindex")).ToHaveAttributeAsync("tabindex", "5");
        await Expect(Input("sld-basic")).ToHaveAttributeAsync("tabindex", "0");

        // The value labels are kept out of the accessibility tree.
        await Expect(ValueLabels("sld-default")).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [TestMethod]
    public async Task FocusAsyncAndFocusUpperAsync_ReachEitherThumbFromCode()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-focus-upper");
        await Expect(Upper("sld-focus-api")).ToBeFocusedAsync();
        await Page.ClickAsync("#btn-focus-lower");
        await Expect(Lower("sld-focus-api")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AutoFocus_TheSliderHasTheFocusWhenThePageRenders()
    {
        await OpenAsync("/e2e/slider/autofocus");

        await Expect(Input("sld-autofocus")).ToHaveAttributeAsync("autofocus", "");
        await Expect(Input("sld-no-autofocus")).Not.ToHaveAttributeAsync("autofocus", new Regex(".*"));
        await Expect(Input("sld-autofocus")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task EscapeInsideAModal_ClosesTheModalAndLeavesTheValueAlone()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-modal");
        await Expect(Input("sld-modal")).ToBeVisibleAsync();

        // The modal moves the focus into itself once it has opened; the test waits for that before it types.
        await Page.WaitForFunctionAsync("() => document.activeElement && document.activeElement.closest('.bit-mdl')");
        await Input("sld-modal").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectTextAsync("modal-value", "6");

        await Page.Keyboard.PressAsync("Escape");

        await ExpectTextAsync("modal-open", "False");
        await Expect(Root("sld-modal")).ToHaveCountAsync(0);
        await ExpectTextAsync("modal-value", "6");
    }

    // ---------------------------------------------------------------- cascade, color, size

    [TestMethod]
    public async Task CascadingParameters_FillWhatTheSliderLeavesUnsetAndYieldToWhatItSets()
    {
        await OpenAsync(Route);

        var cascaded = Root("sld-cascade-1");
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-sld-suc"));
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-sld-lg"));
        await Expect(Input("sld-cascade-1")).ToHaveAttributeAsync("max", "100");
        await Expect(Input("sld-cascade-1")).ToHaveAttributeAsync("step", "5");
        await Expect(cascaded.Locator(".bit-sld-mlb")).ToHaveTextAsync(["0", "25", "50", "75", "100"]);

        var own = Root("sld-cascade-own");
        await Expect(own).ToHaveClassAsync(HasClass("bit-sld-wrn"));
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-sld-suc"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-sld-lg"));
        await Expect(Input("sld-cascade-own")).ToHaveAttributeAsync("max", "50");
        await Expect(own.Locator(".bit-sld-mrk")).ToHaveCountAsync(3);
        await Expect(own.Locator(".bit-sld-mlb")).ToHaveCountAsync(0);

        var outside = Root("sld-cascade-outside");
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-sld-suc"));
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-sld-lg"));
        await Expect(Input("sld-cascade-outside")).ToHaveAttributeAsync("max", "10");
        await Expect(outside.Locator(".bit-sld-mrk")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Color_EveryValuePaintsTheFillThumbAndThumbLabelInItsRole()
    {
        await OpenAsync(Route);

        string[] slugs = ["pri", "sec", "ter", "inf", "suc", "wrn", "swr", "err", "pbg", "sbg", "tbg", "pfg", "sfg", "tfg", "pbr", "sbr", "tbr"];

        var unset = await CssAsync(Fill("sld-color-none"), "background-color");
        var colors = new Dictionary<string, string>();

        foreach (var slug in slugs)
        {
            var id = $"sld-color-{slug}";
            await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-sld-{slug}"));

            var role = await Root(id).EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--bit-sld-clr').trim()");
            var on = await Root(id).EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--bit-sld-clr-on').trim()");
            var expected = await ResolveColorAsync(role);

            Assert.AreEqual(expected, await CssAsync(Fill(id), "background-color"), $"The fill of {slug}.");
            Assert.AreEqual(expected, await CssAsync(Thumb(id), "border-top-color"), $"The thumb of {slug}.");
            var thumbLabel = Root(id).Locator(".bit-sld-tlb");
            Assert.AreEqual(expected, await CssAsync(thumbLabel, "background-color"), $"The thumb label of {slug}.");
            Assert.AreEqual(await ResolveColorAsync(on), await CssAsync(thumbLabel, "color"), $"The thumb label text of {slug}.");

            colors[slug] = expected;
        }

        Assert.AreEqual(unset, colors["pri"], "Unset is not primary.");
        Assert.IsTrue(colors.Values.Distinct().Count() >= 12, string.Join(", ", colors.Select(c => $"{c.Key}={c.Value}")));
    }

    [TestMethod]
    [DataRow("sm", "md")]
    [DataRow("md", "lg")]
    public async Task Size_ScalesTheThumbRailAndLabels(string smaller, string larger)
    {
        await OpenAsync(Route);

        await Expect(Root($"sld-size-{smaller}")).ToHaveClassAsync(HasClass($"bit-sld-{smaller}"));
        await Expect(Root($"sld-size-{larger}")).ToHaveClassAsync(HasClass($"bit-sld-{larger}"));

        Assert.IsTrue((await BoxAsync(Thumb($"sld-size-{larger}"))).Width > (await BoxAsync(Thumb($"sld-size-{smaller}"))).Width, "thumb");
        Assert.IsTrue(Px(await CssAsync(Fill($"sld-size-{larger}"), "height")) > Px(await CssAsync(Fill($"sld-size-{smaller}"), "height")), "rail");

        foreach (var part in new[] { ".bit-sld-lbl", ".bit-sld-vlb", ".bit-sld-mlb", ".bit-sld-tlb" })
        {
            var small = Px(await ComputedStyleAsync($"#sld-size-{smaller} {part}", "font-size"));
            var large = Px(await ComputedStyleAsync($"#sld-size-{larger} {part}", "font-size"));
            Assert.IsTrue(large > small, $"{part}: {large} is not larger than {small}.");
        }
    }

    [TestMethod]
    public async Task Size_SetsTheLengthOfAVerticalSliderAndKeepsTheGrabAreaAtLeast24Pixels()
    {
        await OpenAsync(Route);

        var small = await BoxAsync(Root("sld-size-vsm").Locator(".bit-sld-box"));
        var large = await BoxAsync(Root("sld-size-vlg").Locator(".bit-sld-box"));
        Assert.IsTrue(large.Height > small.Height);
        Assert.IsTrue(small.Width >= 23.9, $"{small.Width}");

        var smallBox = await BoxAsync(Root("sld-size-sm").Locator(".bit-sld-box"));
        Assert.IsTrue(smallBox.Height >= 23.9, $"{smallBox.Height}");
    }

    // ---------------------------------------------------------------- style & class

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRoot()
    {
        await OpenAsync(Route);

        await Expect(Root("sld-style")).ToHaveCSSAsync("margin-left", "13px");
        await Expect(Root("sld-style")).ToHaveCSSAsync("box-shadow", new Regex(@"rgb\(255, 99, 71\)"));
        await Expect(Root("sld-class")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Root("sld-class")).ToHaveClassAsync(HasClass("bit-sld"));
    }

    [TestMethod]
    public async Task Styles_ReachEveryPart()
    {
        await OpenAsync(Route);

        var root = Root("sld-styles");
        await Expect(root).ToHaveCSSAsync("padding-top", "5px");
        await Expect(root.Locator(".bit-sld-lbl")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(root.Locator(".bit-sld-cnt")).ToHaveCSSAsync("column-gap", "7px");
        await Expect(root.Locator(".bit-sld-box")).ToHaveCSSAsync("outline-color", "rgb(1, 1, 1)");
        await Expect(root.Locator(".bit-sld-trk")).ToHaveCSSAsync("opacity", "0.9");
        await Expect(root.Locator(".bit-sld-fil")).ToHaveCSSAsync("background-color", "rgb(2, 2, 2)");
        await Expect(root.Locator(".bit-sld-thb")).ToHaveCSSAsync("background-color", "rgb(3, 3, 3)");
        await Expect(root.Locator("input.bit-sld-inp")).ToHaveCSSAsync("outline-color", "rgb(4, 4, 4)");
        await Expect(root.Locator(".bit-sld-ztk")).ToHaveCSSAsync("background-color", "rgb(5, 5, 5)");
        await Expect(root.Locator(".bit-sld-vlb")).ToHaveCSSAsync("color", "rgb(6, 6, 6)");
        await Expect(root.Locator(".bit-sld-mrk").First).ToHaveCSSAsync("background-color", "rgb(7, 7, 7)");
        await Expect(root.Locator(".bit-sld-mlb").First).ToHaveCSSAsync("color", "rgb(8, 8, 8)");
        await Expect(root.Locator(".bit-sld-tlb")).ToHaveCSSAsync("color", "rgb(9, 9, 9)");

        var range = Root("sld-styles-range");
        await Expect(range.Locator(".bit-sld-thb-lwr")).ToHaveCSSAsync("background-color", "rgb(10, 10, 10)");
        await Expect(range.Locator(".bit-sld-thb-upr")).ToHaveCSSAsync("background-color", "rgb(11, 11, 11)");
        await Expect(range.Locator(".bit-sld-inp-lwr")).ToHaveCSSAsync("outline-color", "rgb(12, 12, 12)");
        await Expect(range.Locator(".bit-sld-inp-upr")).ToHaveCSSAsync("outline-color", "rgb(13, 13, 13)");
        await Expect(range.Locator(".bit-sld-inp-bar")).ToHaveCSSAsync("outline-color", "rgb(14, 14, 14)");
        await Expect(range.Locator(".bit-sld-vlb").Nth(0)).ToHaveCSSAsync("color", "rgb(15, 15, 15)");
        await Expect(range.Locator(".bit-sld-vlb").Nth(1)).ToHaveCSSAsync("color", "rgb(16, 16, 16)");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPart()
    {
        await OpenAsync(Route);

        var root = Root("sld-classes");
        await Expect(root).ToHaveClassAsync(HasClass("c-root"));
        foreach (var (part, cssClass) in new[]
        {
            (".bit-sld-lbl", "c-lbl"), (".bit-sld-cnt", "c-cnt"), (".bit-sld-box", "c-box"), (".bit-sld-trk", "c-trk"),
            (".bit-sld-fil", "c-fil"), (".bit-sld-thb", "c-thb"), ("input.bit-sld-inp", "c-inp"), (".bit-sld-ztk", "c-ztk"),
            (".bit-sld-vlb", "c-vlb"), (".bit-sld-tlb", "c-tlb")
        })
        {
            await Expect(root.Locator(part).First).ToHaveClassAsync(HasClass(cssClass));
        }

        await Expect(root.Locator(".bit-sld-mrk.c-mrk")).ToHaveCountAsync(await root.Locator(".bit-sld-mrk").CountAsync());
        await Expect(root.Locator(".bit-sld-mlb.c-mlb")).ToHaveCountAsync(await root.Locator(".bit-sld-mlb").CountAsync());

        var range = Root("sld-classes-range");
        await Expect(range.Locator(".bit-sld-thb-lwr")).ToHaveClassAsync(HasClass("c-lthb"));
        await Expect(range.Locator(".bit-sld-thb-upr")).ToHaveClassAsync(HasClass("c-uthb"));
        await Expect(range.Locator(".bit-sld-inp-lwr")).ToHaveClassAsync(HasClass("c-linp"));
        await Expect(range.Locator(".bit-sld-inp-upr")).ToHaveClassAsync(HasClass("c-uinp"));
        await Expect(range.Locator(".bit-sld-inp-bar")).ToHaveClassAsync(HasClass("c-tinp"));
        await Expect(range.Locator(".bit-sld-vlb").Nth(0)).ToHaveClassAsync(HasClass("c-lvlb"));
        await Expect(range.Locator(".bit-sld-vlb").Nth(1)).ToHaveClassAsync(HasClass("c-uvlb"));
    }

    [TestMethod]
    public async Task HtmlAttributes_AreSplattedOnTheRootAndTheBox()
    {
        await OpenAsync(Route);

        await Expect(Root("sld-attrs")).ToHaveAttributeAsync("data-test", "attr-value");
        await Expect(Root("sld-attrs")).ToHaveAttributeAsync("title", "root title");
        await Expect(Root("sld-attrs").Locator(".bit-sld-box")).ToHaveAttributeAsync("data-box", "box-value");
    }

    // ---------------------------------------------------------------- css variables

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheSlider()
    {
        await OpenAsync(Route);

        var root = Root("sld-vars");
        await Expect(Fill("sld-vars")).ToHaveCSSAsync("background-color", "rgb(218, 165, 32)");
        await Expect(Fill("sld-vars")).ToHaveCSSAsync("height", "6px");
        await Expect(Fill("sld-vars")).ToHaveCSSAsync("border-top-left-radius", "1px");
        var rail = await root.Locator(".bit-sld-trk").EvaluateAsync<string>("el => getComputedStyle(el, '::before').backgroundColor");
        Assert.AreEqual("rgb(216, 201, 163)", rail);

        var thumb = Thumb("sld-vars");
        await Expect(thumb).ToHaveCSSAsync("width", "30px");
        await Expect(thumb).ToHaveCSSAsync("border-top-left-radius", "4px");
        await Expect(thumb).ToHaveCSSAsync("border-top-width", "3px");
        await Expect(thumb).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(thumb).ToHaveCSSAsync("box-shadow", "rgb(9, 9, 9) 0px 0px 2px 0px");
        await Expect(thumb).ToHaveCSSAsync("border-top-color", "rgb(218, 165, 32)");

        await Expect(root.Locator(".bit-sld-cnt")).ToHaveCSSAsync("column-gap", "9px");
        var label = root.Locator(".bit-sld-lbl");
        await Expect(label).ToHaveCSSAsync("font-size", "17px");
        await Expect(label).ToHaveCSSAsync("font-weight", "300");
        await Expect(label).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(label).ToHaveCSSAsync("color", "rgb(220, 20, 60)");
        Assert.AreEqual("rgb(0, 0, 255)", await label.EvaluateAsync<string>("el => getComputedStyle(el, '::after').color"));
        await Expect(ValueLabels("sld-vars")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");

        var mark = root.Locator(".bit-sld-mrk").First;
        await Expect(mark).ToHaveCSSAsync("width", "6px");
        await Expect(root.Locator(".bit-sld-mrk:not(.bit-sld-mrk-act)").First).ToHaveCSSAsync("background-color", "rgb(10, 11, 12)");
        await Expect(root.Locator(".bit-sld-mrk-act").First).ToHaveCSSAsync("background-color", "rgb(13, 14, 15)");
        await Expect(root.Locator(".bit-sld-mlb").First).ToHaveCSSAsync("color", "rgb(16, 17, 18)");
        await Expect(root.Locator(".bit-sld-mks")).ToHaveCSSAsync("font-size", "11px");
        await Expect(root.Locator(".bit-sld-mks")).ToHaveCSSAsync("height", "30px");

        var thumbLabel = root.Locator(".bit-sld-tlb");
        await Expect(thumbLabel).ToHaveCSSAsync("color", "rgb(19, 20, 21)");
        // The thumb label's own background variable is read ahead of the slider's color.
        await Expect(thumbLabel).ToHaveCSSAsync("background-color", "rgb(22, 23, 24)");
        await Expect(thumbLabel).ToHaveCSSAsync("border-top-left-radius", "5px");
        await Expect(thumbLabel).ToHaveCSSAsync("padding-top", "3px");
        await Expect(thumbLabel).ToHaveCSSAsync("font-size", "13px");
        await Expect(root.Locator(".bit-sld-cnt")).ToHaveCSSAsync("margin-top", "50px");

        var tick = root.Locator(".bit-sld-ztk");
        await Expect(tick).ToHaveCSSAsync("background-color", "rgb(25, 26, 27)");
        await Expect(tick).ToHaveCSSAsync("width", "5px");

        StringAssert.StartsWith(await CssAsync(Fill("sld-vars"), "transition-duration"), "0.3s");
        Assert.AreEqual(150, (await BoxAsync(Root("sld-vars-vertical").Locator(".bit-sld-box"))).Height, 1);
    }

    [TestMethod]
    public async Task CssVariables_AnExplicitColorOrSizeWinsOverThem()
    {
        await OpenAsync(Route);

        await Expect(Fill("sld-vars-explicit")).Not.ToHaveCSSAsync("background-color", "rgb(218, 165, 32)");
        Assert.AreEqual(await CssAsync(Fill("sld-color-err"), "background-color"), await CssAsync(Fill("sld-vars-explicit"), "background-color"));
        await Expect(Thumb("sld-vars-explicit")).Not.ToHaveCSSAsync("width", "30px");
        Assert.AreEqual(await CssAsync(Thumb("sld-size-sm"), "width"), await CssAsync(Thumb("sld-vars-explicit"), "width"));
    }

    [TestMethod]
    public async Task CssVariables_DisabledHoverActiveAndFocusVariablesApplyInTheirState()
    {
        await OpenAsync(Route);

        await Expect(Fill("sld-vars-disabled")).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(Thumb("sld-vars-disabled")).ToHaveCSSAsync("border-top-color", "rgb(1, 2, 3)");

        var box = Root("sld-vars-state").Locator(".bit-sld-box");
        await box.HoverAsync();
        await Expect(Fill("sld-vars-state")).ToHaveCSSAsync("background-color", "rgb(7, 8, 9)");
        await Expect(Thumb("sld-vars-state")).ToHaveCSSAsync("border-top-color", "rgb(7, 8, 9)");

        var (x, y) = await PointAsync("sld-vars-state", 0.3);
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Expect(Fill("sld-vars-state")).ToHaveCSSAsync("background-color", "rgb(40, 50, 60)");
        await Page.Mouse.UpAsync();

        await Page.Mouse.MoveAsync(0, 0);
        await Page.FocusAsync("#before-vars-state");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Input("sld-vars-state")).ToBeFocusedAsync();
        await Expect(Thumb("sld-vars-state")).ToHaveCSSAsync("box-shadow", new Regex(@"rgb\(10, 20, 30\)"));
    }

    // ---------------------------------------------------------------- rtl

    [TestMethod]
    public async Task Rtl_RunsTheScaleFromTheRightAndThePointerFollowsIt()
    {
        await OpenAsync(Route);

        var root = Root("sld-rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");
        await ExpectThumbAtAsync("sld-rtl", 0.3, Axis.Rtl);

        await ClickAtAsync("sld-rtl", 0.8, Axis.Rtl);
        await ExpectTextAsync("rtl-value", "8");
        await ExpectThumbAtAsync("sld-rtl", 0.8, Axis.Rtl);

        await DragAsync("sld-rtl", 0.8, 0.21, Axis.Rtl);
        await ExpectTextAsync("rtl-value", "2");

        // The fill grows from the right edge.
        var fill = await BoxAsync(Fill("sld-rtl"));
        var track = await BoxAsync(root.Locator(".bit-sld-trk"));
        Assert.AreEqual(track.X + track.Width, fill.X + fill.Width, 1.5);
    }

    [TestMethod]
    public async Task Rtl_TheArrowKeysFollowTheReadingDirection()
    {
        await OpenAsync(Route);

        await Input("sld-rtl").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectTextAsync("rtl-value", "4");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectTextAsync("rtl-value", "2");
        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectTextAsync("rtl-value", "3");
    }

    [TestMethod]
    public async Task Rtl_ARangeAndAnInheritedDirection()
    {
        await OpenAsync(Route);

        var lower = await BoxAsync(Root("sld-rtl-range").Locator(".bit-sld-thb-lwr"));
        var upper = await BoxAsync(Root("sld-rtl-range").Locator(".bit-sld-thb-upr"));
        Assert.IsTrue(lower.X > upper.X, "The lower thumb is not on the right.");

        await DragAsync("sld-rtl-range", 0.2, 0.4, Axis.Rtl);
        await ExpectTextAsync("rtl-range-value", "40-60");
        await DragAsync("sld-rtl-range", 0.6, 0.9, Axis.Rtl);
        await ExpectTextAsync("rtl-range-value", "40-90");

        var inherited = Root("sld-rtl-inherited");
        await Expect(inherited).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        await ExpectThumbAtAsync("sld-rtl-inherited", 0.3, Axis.Rtl);
        await ClickAtAsync("sld-rtl-inherited", 0.7, Axis.Rtl);
        await ExpectTextAsync("rtl-inherited-value", "7");
    }

    // ---------------------------------------------------------------- motion

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheTransitionsUnlessForceAnimationRestoresThem()
    {
        await OpenAsync(Route);

        StringAssert.StartsWith(await CssAsync(Fill("sld-motion"), "transition-duration"), "0.1s");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        await Expect(Fill("sld-motion")).Not.ToHaveCSSAsync("transition-duration", new Regex("^0\\.1s"));
        var reduced = (await CssAsync(Fill("sld-motion"), "transition-duration")).Split(',')[0].Trim();
        Assert.IsTrue(double.Parse(reduced.TrimEnd('s'), CultureInfo.InvariantCulture) < 0.001, reduced);

        await Expect(Root("sld-motion-forced")).ToHaveClassAsync(HasClass("bit-fam"));
        StringAssert.StartsWith(await CssAsync(Fill("sld-motion-forced"), "transition-duration"), "0.1s");
    }
}
