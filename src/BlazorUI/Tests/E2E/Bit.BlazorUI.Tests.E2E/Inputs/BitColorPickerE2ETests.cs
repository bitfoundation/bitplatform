using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// Drives BitColorPicker (/e2e/color-picker) in a real browser: the saturation-brightness area dragged with the mouse
/// (clamped at its edges) and moved with the keyboard (arrows, Shift, Page, Home/End), the hue and alpha sliders by
/// pointer and keys, every Format and a Format switched at runtime, the notation detected from the value, the hex,
/// channel and alpha fields typed into in every inputs mode and the mode switch, the presets (click, the toolbar's
/// arrow keys, a grid, alpha presets), the eyedropper's presence and its result, the contrast readout, two-way binding
/// of Value, Alpha and InputsMode, a one-way Value, the events and the public readings, FocusAsync and AutoFocus,
/// validation in an EditForm, Texts, the BitParams cascade, Size/Style/Class/Styles/Classes, the CSS variables, RTL
/// (explicit and inherited), Visibility, the disabled and read-only contracts and the lifecycle.
/// </summary>
[TestClass]
public class BitColorPickerE2ETests : E2ETestBase
{
    private const string Url = "/e2e/color-picker";

    // A stand-in for the browser's eyedropper, so the button exists in every engine and answers with a known color.
    private const string EyeDropperStub = "window.EyeDropper = class { open() { return Promise.resolve({ sRGBHex: '#123456' }); } };";

    // ------------------------------------------------------------------ helpers

    private ILocator Root(string scope) => Page.Locator($"#{scope} .bit-clp").First;

    private ILocator Area(string scope) => Page.Locator($"#{scope} .bit-clp-rec").First;

    private ILocator Hue(string scope) => Page.Locator($"#{scope} .bit-clp-hsd input").First;

    private ILocator AlphaSlider(string scope) => Page.Locator($"#{scope} .bit-clp-asd input").First;

    private ILocator Preview(string scope) => Page.Locator($"#{scope} .bit-clp-prc").First;

    private ILocator Swatches(string scope) => Page.Locator($"#{scope} .bit-clp-prt");

    private ILocator Swatch(string scope, string value) => Page.Locator($"#{scope} .bit-clp-prt[title='{value}']");

    private ILocator Out(string id) => Page.Locator($"#{id}");

    private async Task<ILocator> FieldAsync(string scope, string name)
    {
        var id = await Root(scope).GetAttributeAsync("id");
        return Page.Locator($"[id='{id}-{name}']");
    }

    private static Task<string> StyleOfAsync(ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>($"el => getComputedStyle(el).getPropertyValue('{property}')");
    }

    private Task<string> ResolveColorAsync(string cssValue)
    {
        return Page.EvaluateAsync<string>(@"value => {
            const probe = document.createElement('div');
            probe.style.backgroundColor = value;
            document.body.appendChild(probe);
            const resolved = getComputedStyle(probe).backgroundColor;
            probe.remove();
            return resolved;
        }", cssValue);
    }

    private static float Px(string cssLength) => float.Parse(cssLength.Replace("px", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture);

    // A point of the saturation area, as fractions of its width and height from its top-left corner.
    private static async Task<(float X, float Y)> PointAsync(ILocator area, double x, double y)
    {
        await area.ScrollIntoViewIfNeededAsync();
        var box = (await area.BoundingBoxAsync())!;
        return ((float)(box.X + box.Width * x), (float)(box.Y + box.Height * y));
    }

    private static async Task<double> ValueNowAsync(ILocator area) => double.Parse((await area.GetAttributeAsync("aria-valuenow"))!, CultureInfo.InvariantCulture);

    // ------------------------------------------------------------------ lifecycle

    [TestMethod]
    public async Task Prerender_TheServerHtml_AlreadyContainsThePicker()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.AreEqual(200, response.Status);
        StringAssert.Contains(html, "id=\"clp-basic\"");
        StringAssert.Contains(html, "class=\"bit-clp-rec");
        StringAssert.Contains(html, "role=\"slider\"");
        StringAssert.Contains(html, "Status: <span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Lifecycle_PartsTurnedOnAndOffAtRuntime_KeepWorking_AndUnmountingLeavesNoError()
    {
        await OpenAsync(Url);

        await Expect(AlphaSlider("lifecycle")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-toggle-alpha");
        await Expect(AlphaSlider("lifecycle")).ToHaveCountAsync(1);

        await Page.ClickAsync("#btn-presets");
        await Expect(Swatches("lifecycle")).ToHaveCountAsync(10);
        await Swatch("lifecycle", "#2E75B6").ClickAsync();
        await Expect(Out("lifecycle-value")).ToHaveTextAsync("#2E75B6");
        await Page.ClickAsync("#btn-no-presets");
        await Expect(Swatches("lifecycle")).ToHaveCountAsync(0);

        // The gradient taken away and given back is dragged like any other: its listeners are registered again.
        await Page.ClickAsync("#btn-toggle-area");
        await Expect(Area("lifecycle")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-toggle-area");
        await Expect(Area("lifecycle")).ToHaveCountAsync(1);

        // The listeners are attached by a round trip after the area is back on the page, which nothing on the page
        // reports, so the press is repeated until one lands - as a person reaching for it a moment later would.
        var (x, y) = await PointAsync(Area("lifecycle"), 0.95, 0.95);
        for (var attempt = 0; attempt < 20; attempt++)
        {
            await Page.Mouse.ClickAsync(x, y);
            if (await Out("lifecycle-value").TextContentAsync() != "#2E75B6") break;
            await NextFramesAsync();
        }

        await Expect(Out("lifecycle-value")).ToHaveTextAsync(new Regex("^#0[0-9A-F]{5}$"));

        await Page.ClickAsync("#btn-toggle-mounted");
        await Expect(Root("lifecycle")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-toggle-mounted");
        await Expect(Root("lifecycle")).ToHaveCountAsync(1);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // ------------------------------------------------------------------ basic

    [TestMethod]
    public async Task Basic_RendersTheAreaAndTheHueSlider_NamedAndValued()
    {
        await OpenAsync(Url);

        var root = Root("basic");
        await Expect(root).ToHaveAttributeAsync("role", "group");

        var area = Area("basic");
        await Expect(area).ToHaveAttributeAsync("role", "slider");
        await Expect(area).ToHaveAttributeAsync("aria-roledescription", "2D slider");
        await Expect(area).ToHaveAttributeAsync("aria-label", "Saturation and brightness");
        await Expect(area).ToHaveAttributeAsync("aria-valuemin", "0");
        await Expect(area).ToHaveAttributeAsync("aria-valuemax", "100");
        await Expect(area).ToHaveAttributeAsync("aria-valuenow", "100");
        await Expect(area).ToHaveAttributeAsync("aria-valuetext", "vibrant red, Saturation 100%, Brightness 100%, #FF0000");
        await Expect(area).ToHaveAttributeAsync("tabindex", "0");
        Assert.AreEqual("rgb(255, 0, 0)", await StyleOfAsync(area, "background-color"));

        var hue = Hue("basic");
        await Expect(hue).ToHaveAttributeAsync("type", "range");
        await Expect(hue).ToHaveAttributeAsync("aria-label", "Hue");
        await Expect(hue).ToHaveAttributeAsync("aria-valuetext", "Hue 0 degrees, red");
        await Expect(hue).ToHaveValueAsync("0");
        await Expect(AlphaSlider("basic")).ToHaveCountAsync(0);

        // The thumb sits on the color: the top-right corner for a saturated, bright red.
        var box = (await area.BoundingBoxAsync())!;
        var thumb = (await Page.Locator("#basic .bit-clp-thumb").BoundingBoxAsync())!;
        Assert.AreEqual(box.X + box.Width, thumb.X + thumb.Width / 2, 2);
        Assert.AreEqual(box.Y, thumb.Y + thumb.Height / 2, 2);

        var hidden = Page.Locator("#basic input.bit-input-hidden");
        await Expect(hidden).ToHaveValueAsync("#FF0000");
        await Expect(hidden).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(hidden).ToHaveAttributeAsync("tabindex", "-1");
    }

    [TestMethod]
    public async Task Label_NamesTheGroup_AndRequiredMarksIt()
    {
        await OpenAsync(Url);

        var label = Page.Locator("#label .bit-clp-lbc");
        await Expect(label).ToHaveTextAsync("Brand color");
        await Expect(Root("label")).ToHaveAttributeAsync("aria-labelledby", (await label.GetAttributeAsync("id"))!);
        Assert.IsNull(await Root("label").GetAttributeAsync("aria-label"));
        await Expect(Root("label")).ToHaveClassAsync(HasClass("bit-clp-req"));
        Assert.AreEqual("\" *\"", await label.EvaluateAsync<string>("el => getComputedStyle(el, '::after').content"));
        // Required stays off the hidden input, whose native check would block a submit with no message to show.
        Assert.IsNull(await Page.Locator("#label input.bit-input-hidden").GetAttributeAsync("required"));

        await Expect(Page.Locator("#label-template .bit-clp-lbc #custom-label")).ToHaveTextAsync("Custom label");
        await Expect(Root("label-template")).ToHaveAttributeAsync("aria-labelledby", (await Page.Locator("#label-template .bit-clp-lbc").GetAttributeAsync("id"))!);
    }

    [TestMethod]
    public async Task Unnamed_TheGroupSpellsTheColorOut()
    {
        await OpenAsync(Url);

        await Expect(Root("unnamed")).ToHaveAttributeAsync("aria-label", "Color picker, vibrant red, Red 255 Green 0 Blue 0 and Alpha 100% selected.");
        await Expect(Root("a11y")).ToHaveAttributeAsync("aria-label", "Choose the brand color");
    }

    // ------------------------------------------------------------------ saturation area: mouse

    [TestMethod]
    public async Task Area_DraggedWithTheMouse_FollowsThePointer_ClampsAtTheEdges_AndEndsOnce()
    {
        await OpenAsync(Url);

        var area = Area("drag");
        var (x, y) = await PointAsync(area, 0.25, 0.5);
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();

        await Expect(area).ToBeFocusedAsync();
        await Expect(area).ToHaveAttributeAsync("aria-valuenow", new Regex("^(2[3-7])$"));
        await Expect(area).ToHaveAttributeAsync("aria-valuetext", new Regex(@"Brightness (4[89]|5[0-2])%"));

        (x, y) = await PointAsync(area, 0.75, 0.25);
        await Page.Mouse.MoveAsync(x, y, new() { Steps = 6 });
        await Expect(area).ToHaveAttributeAsync("aria-valuenow", new Regex("^(7[3-7])$"));
        await Expect(area).ToHaveAttributeAsync("aria-valuetext", new Regex(@"Brightness (7[3-7])%"));
        await Expect(Out("drag-ends")).ToHaveTextAsync("0");

        // A drag carried off the area is clamped to its edge: the bottom-right corner is black.
        var box = (await area.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(box.X + box.Width + 60, box.Y + box.Height + 60, new() { Steps = 6 });
        await Expect(Out("drag-value")).ToHaveTextAsync("#000000");
        await Page.Mouse.UpAsync();

        await Expect(Out("drag-ends")).ToHaveTextAsync("1");
        var changes = int.Parse((await Out("drag-changes").TextContentAsync())!, CultureInfo.InvariantCulture);
        Assert.IsTrue(changes >= 3, $"OnColorChange fires on the steps of the drag ({changes}).");

        // The fields follow the drag.
        await Expect(await FieldAsync("drag", "hex")).ToHaveValueAsync("#000000FF");
        await Expect(await FieldAsync("drag", "r")).ToHaveValueAsync("0");

        // Carried off past the top-left corner, it is clamped to white.
        (x, y) = await PointAsync(area, 0.1, 0.1);
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X - 40, box.Y - 40, new() { Steps = 6 });
        await Page.Mouse.UpAsync();
        await Expect(Out("drag-value")).ToHaveTextAsync("#FFFFFF");
        await Expect(Out("drag-ends")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Area_TheRightButton_PicksNothing()
    {
        await OpenAsync(Url);

        var (x, y) = await PointAsync(Area("drag"), 0.5, 0.5);
        await Page.Mouse.ClickAsync(x, y, new() { Button = MouseButton.Right });
        await Page.Keyboard.PressAsync("Escape");

        await Area("drag").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Out("drag-value")).ToHaveTextAsync("#FC0000");
        await Expect(Out("drag-ends")).ToHaveTextAsync("1");
    }

    // ------------------------------------------------------------------ saturation area: keyboard

    [TestMethod]
    public async Task Area_TheKeyboardWalksBothAxes()
    {
        await OpenAsync(Url);

        var area = Area("keyboard");
        var value = Out("keyboard-value");
        await area.FocusAsync();

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(value).ToHaveTextAsync("hsva(0,100%,51%,1)");
        await Page.Keyboard.PressAsync("Shift+ArrowUp");
        await Expect(value).ToHaveTextAsync("hsva(0,100%,61%,1)");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(value).ToHaveTextAsync("hsva(0,99%,61%,1)");
        await Page.Keyboard.PressAsync("Shift+ArrowLeft");
        await Expect(value).ToHaveTextAsync("hsva(0,89%,61%,1)");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(value).ToHaveTextAsync("hsva(0,90%,61%,1)");
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(value).ToHaveTextAsync("hsva(0,90%,71%,1)");
        await Page.Keyboard.PressAsync("PageDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(value).ToHaveTextAsync("hsva(0,90%,60%,1)");
        await Page.Keyboard.PressAsync("Home");
        await Expect(value).ToHaveTextAsync("hsva(0,0%,60%,1)");
        await Expect(area).ToHaveAttributeAsync("aria-valuenow", "0");
        await Page.Keyboard.PressAsync("End");
        await Expect(value).ToHaveTextAsync("hsva(0,100%,60%,1)");

        // A modified key belongs to the browser and moves nothing; the keys the area takes never scroll the page.
        await Page.Keyboard.PressAsync("Control+Home");
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await area.ScrollIntoViewIfNeededAsync();
        var scroll = await Page.EvaluateAsync<double>("() => window.scrollY");
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(value).ToHaveTextAsync("hsva(0,100%,50%,1)");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("End");
        await Expect(value).ToHaveTextAsync("hsva(0,100%,49%,1)");
        Assert.AreEqual(scroll, await Page.EvaluateAsync<double>("() => window.scrollY"));
    }

    [TestMethod]
    public async Task Sliders_TheHueAndTheAlphaMoveWithTheirKeys()
    {
        await OpenAsync(Url);

        var value = Out("keyboard-value");
        await Hue("keyboard").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(value).ToHaveTextAsync("hsva(1,100%,50%,1)");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(value).ToHaveTextAsync("hsva(3,100%,50%,1)");
        await Expect(Hue("keyboard")).ToHaveAttributeAsync("aria-valuetext", "Hue 3 degrees, red");
        await Page.Keyboard.PressAsync("Home");
        await Expect(value).ToHaveTextAsync("hsva(0,100%,50%,1)");

        var alpha = AlphaSlider("keyboard");
        await alpha.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(value).ToHaveTextAsync("hsva(0,100%,50%,0.99)");
        await Expect(alpha).ToHaveAttributeAsync("aria-valuetext", "Alpha 99%");
        await Page.Keyboard.PressAsync("Home");
        await Expect(value).ToHaveTextAsync("hsva(0,100%,50%,0)");
        await Page.Keyboard.PressAsync("End");
        await Expect(value).ToHaveTextAsync("hsva(0,100%,50%,1)");
    }

    [TestMethod]
    public async Task Sliders_ThePointerMovesTheHueAndTheAlpha()
    {
        await OpenAsync(Url);

        var hue = Hue("drag");
        await hue.ScrollIntoViewIfNeededAsync();
        var box = (await hue.BoundingBoxAsync())!;
        await Page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);

        var degrees = double.Parse(await hue.InputValueAsync(), CultureInfo.InvariantCulture);
        Assert.IsTrue(degrees is > 165 and < 195, $"A press halfway along the hue track picks a cyan ({degrees}).");
        await Expect(Area("drag")).ToHaveCSSAsync("background-color", new Regex(@"^rgb\(0, 255, \d+\)$|^rgb\(0, \d+, 255\)$"));
        await Expect(Out("drag-value")).ToHaveTextAsync(new Regex("^#00[0-9A-F]{4}$"));

        var alpha = AlphaSlider("drag");
        box = (await alpha.BoundingBoxAsync())!;
        await Page.Mouse.ClickAsync(box.X + box.Width / 4, box.Y + box.Height / 2);
        var opacity = double.Parse(await alpha.InputValueAsync(), CultureInfo.InvariantCulture);
        Assert.IsTrue(opacity is > 0.15 and < 0.35, $"A press a quarter along the alpha track picks a quarter opacity ({opacity}).");
        await Expect(await FieldAsync("drag", "alpha")).ToHaveValueAsync(new Regex("^(1[5-9]|2\\d|3[0-5])$"));

        // The slider is dragged too, not only pressed - at the pace of a hand, each step seen through before the
        // next, since every step is a round trip whose render writes the value back into the slider.
        await Page.Mouse.MoveAsync(box.X + box.Width / 4, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        foreach (var fraction in new[] { 0.5, 0.75, 1.0 })
        {
            await Page.Mouse.MoveAsync((float)(box.X + (box.Width - 1) * fraction), box.Y + box.Height / 2);
            // Near rather than exact: the native track is inset by half its thumb at either end.
            var percent = (int)Math.Round(fraction * 100);
            var near = string.Join("|", Enumerable.Range(percent - 6, 13).Where(p => p <= 100));
            await Expect(alpha).ToHaveAttributeAsync("aria-valuetext", new Regex($"^Alpha ({near})%$"));
        }

        await Page.Mouse.UpAsync();
        await Expect(alpha).ToHaveValueAsync("1");
    }

    // ------------------------------------------------------------------ format

    [TestMethod]
    public async Task Format_EveryValue_WritesTheColorInItsNotation_AndASwitchAtRuntimeRewritesTheValue()
    {
        await OpenAsync(Url);

        var expected = new (string Format, string Value)[]
        {
            ("hexalpha", "^#FF0000FF$"),
            ("rgb", @"^rgb\(255,0,0\)$"),
            ("rgba", @"^rgba\(255,0,0,1\)$"),
            ("hsl", @"^hsl\(0,100%,50%\)$"),
            ("hsla", @"^hsla\(0,100%,50%,1\)$"),
            ("hsv", @"^hsv\(0,100%,100%\)$"),
            ("hsva", @"^hsva\(0,100%,100%,1\)$"),
            ("hwb", @"^hwb\(0 0% 0%\)$"),
            ("hwba", @"^hwb\(0 0% 0% / 1\)$"),
            ("oklab", @"^oklab\(0\.62\d* 0\.22\d* 0\.12\d*\)$"),
            ("oklaba", @"^oklab\(0\.62\d* 0\.22\d* 0\.12\d* / 1\)$"),
            ("oklch", @"^oklch\(0\.62\d* 0\.25\d* 29\.\d+\)$"),
            ("oklcha", @"^oklch\(0\.62\d* 0\.25\d* 29\.\d+ / 1\)$"),
            ("hex", "^#FF0000$"),
        };

        foreach (var (format, value) in expected)
        {
            await Page.ClickAsync($"#fmt-{format}");
            await Expect(Out("format-value")).ToHaveTextAsync(new Regex(value));
        }

        // The alpha-carrying notations take the transparency along.
        await Page.ClickAsync("#fmt-rgba");
        await AlphaSlider("format").FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Out("format-value")).ToHaveTextAsync("rgba(255,0,0,0)");
        await Page.ClickAsync("#fmt-hexalpha");
        await Expect(Out("format-value")).ToHaveTextAsync("#FF000000");
        await Page.ClickAsync("#fmt-hsla");
        await Expect(Out("format-value")).ToHaveTextAsync("hsla(0,100%,50%,0)");
    }

    [TestMethod]
    public async Task Format_Unset_AnswersInTheNotationTheValueArrivedIn()
    {
        await OpenAsync(Url);

        var area = Area("format-detect");
        await area.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Out("format-detect-value")).ToHaveTextAsync("rgb(0,125,0)");

        await Page.ClickAsync("#btn-detect-hsl");
        await area.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Out("format-detect-value")).ToHaveTextAsync(new Regex(@"^hsl\(120,100%,24\.\d+%\)$"));

        await Page.ClickAsync("#btn-detect-hex");
        await area.FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Out("format-detect-value")).ToHaveTextAsync(new Regex("^#[0-9A-F]{6}$"));
    }

    // ------------------------------------------------------------------ inputs

    [TestMethod]
    public async Task Inputs_TheHexChannelAndAlphaFieldsCommitOnChange()
    {
        await OpenAsync(Url);

        var hex = await FieldAsync("inputs", "hex");
        var red = await FieldAsync("inputs", "r");
        var green = await FieldAsync("inputs", "g");
        var blue = await FieldAsync("inputs", "b");
        var alpha = await FieldAsync("inputs", "alpha");

        await Expect(hex).ToHaveValueAsync("#0078D4FF");
        await Expect(red).ToHaveValueAsync("0");
        await Expect(green).ToHaveValueAsync("120");
        await Expect(blue).ToHaveValueAsync("212");
        await Expect(alpha).ToHaveValueAsync("100");
        await Expect(red).ToHaveAttributeAsync("max", "255");
        await Expect(alpha).ToHaveAttributeAsync("max", "100");

        // Captions are labels of their fields.
        foreach (var (field, caption, title) in new[] { (hex, "Hex", (string?)null), (red, "R", "Red"), (green, "G", "Green"), (blue, "B", "Blue"), (alpha, "A%", "Alpha") })
        {
            var label = Page.Locator($"#inputs label[for='{await field.GetAttributeAsync("id")}']");
            await Expect(label).ToHaveTextAsync(caption);
            if (title is not null) await Expect(field).ToHaveAttributeAsync("title", title);
        }

        await hex.FillAsync("#00FF0080");
        await hex.PressAsync("Tab");
        await Expect(Out("inputs-value")).ToHaveTextAsync("#00FF0080");
        await Expect(alpha).ToHaveValueAsync("50");
        await Expect(Preview("inputs")).ToHaveCSSAsync("background-color", "rgba(0, 255, 0, 0.5)");

        await red.FillAsync("255");
        await red.PressAsync("Tab");
        await Expect(Out("inputs-value")).ToHaveTextAsync("#FFFF0080");

        await alpha.FillAsync("100");
        await alpha.PressAsync("Tab");
        await Expect(Out("inputs-value")).ToHaveTextAsync("#FFFF00FF");

        // The digits alone are read as a hex, and a refused value puts the current color back in the field.
        await hex.FillAsync("336699");
        await hex.PressAsync("Tab");
        await Expect(Out("inputs-value")).ToHaveTextAsync("#336699FF");
        await hex.FillAsync("zzz");
        await hex.PressAsync("Tab");
        await Expect(hex).ToHaveValueAsync("#336699FF");
        await Expect(Out("inputs-value")).ToHaveTextAsync("#336699FF");

        // An out-of-range channel is clamped.
        await blue.FillAsync("999");
        await blue.PressAsync("Tab");
        await Expect(Out("inputs-value")).ToHaveTextAsync("#3366FFFF");
        await Expect(blue).ToHaveValueAsync("255");
    }

    [TestMethod]
    public async Task Inputs_AKeywordTypedIntoTheHexFieldIsRead()
    {
        await OpenAsync(Url);

        var hex = await FieldAsync("inputs-rgb", "hex");
        await Expect(hex).ToHaveAttributeAsync("maxlength", "9");
        await hex.FillAsync("tomato");
        await hex.PressAsync("Enter");
        await Expect(Out("inputs-rgb-value")).ToHaveTextAsync("#FF6347");
        await Expect(hex).ToHaveValueAsync("#FF6347");
        await Expect(await FieldAsync("inputs-rgb", "g")).ToHaveValueAsync("99");
    }

    [TestMethod]
    public async Task InputsModeSwitch_CyclesThroughTheChannelSets_AndBindsTheMode()
    {
        await OpenAsync(Url);

        var button = Page.Locator("#inputs-switch .bit-clp-swb");
        var fields = Page.Locator("#inputs-switch .bit-clp-flb");

        await Expect(fields).ToHaveTextAsync(["Hex", "R", "G", "B"]);
        await Expect(button).ToHaveAttributeAsync("aria-label", "Color inputs: HEX & RGB. Switch to the next set.");
        await Expect(button).ToHaveAttributeAsync("title", "Color inputs: HEX & RGB");
        await Expect(button.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Sort"));

        var steps = new (string Mode, string[] Captions, string Label)[]
        {
            ("Hex", ["Hex"], "HEX"),
            ("Rgb", ["R", "G", "B"], "RGB"),
            ("Hsl", ["H", "S", "L"], "HSL"),
            ("Hsv", ["H", "S", "V"], "HSV"),
            ("HexRgb", ["Hex", "R", "G", "B"], "HEX & RGB"),
        };

        foreach (var (mode, captions, label) in steps)
        {
            await button.ClickAsync();
            await Expect(Out("inputs-switch-mode")).ToHaveTextAsync(mode);
            await Expect(fields).ToHaveTextAsync(captions);
            await Expect(button).ToHaveAttributeAsync("title", $"Color inputs: {label}");
        }

        // The switch moves how the color is typed, never the color.
        await Expect(Out("inputs-switch-value")).ToHaveTextAsync("#FF0000");
    }

    [TestMethod]
    public async Task InputsMode_HslAndHsv_TakeTheirChannels()
    {
        await OpenAsync(Url);

        var lightness = await FieldAsync("inputs-hsl", "l");
        await Expect(await FieldAsync("inputs-hsl", "h")).ToHaveValueAsync("0");
        await Expect(await FieldAsync("inputs-hsl", "s")).ToHaveValueAsync("100");
        await Expect(lightness).ToHaveValueAsync("50");
        await Expect(await FieldAsync("inputs-hsl", "h")).ToHaveAttributeAsync("max", "360");
        await lightness.FillAsync("25");
        await lightness.PressAsync("Tab");
        await Expect(Out("inputs-hsl-value")).ToHaveTextAsync("#800000");

        var hueField = await FieldAsync("inputs-hsl", "h");
        await hueField.FillAsync("120");
        await hueField.PressAsync("Tab");
        await Expect(Out("inputs-hsl-value")).ToHaveTextAsync("#008000");

        // DefaultInputsMode starts the fields in HSV, and the switch moves on from there.
        var brightness = await FieldAsync("inputs-hsv", "v");
        await Expect(brightness).ToHaveValueAsync("100");
        await brightness.FillAsync("50");
        await brightness.PressAsync("Tab");
        await Expect(Out("inputs-hsv-value")).ToHaveTextAsync("#800000");
        await Page.Locator("#inputs-hsv .bit-clp-swb").ClickAsync();
        await Expect(Page.Locator("#inputs-hsv .bit-clp-flb")).ToHaveTextAsync(["Hex", "R", "G", "B"]);
    }

    // ------------------------------------------------------------------ presets

    [TestMethod]
    public async Task Presets_AClickPicksTheSwatch_AndMarksItSelected()
    {
        await OpenAsync(Url);

        var toolbar = Page.Locator("#presets .bit-clp-prs");
        await Expect(toolbar).ToHaveAttributeAsync("role", "toolbar");
        await Expect(toolbar).ToHaveAttributeAsync("aria-label", "Color presets");
        await Expect(Swatches("presets")).ToHaveCountAsync(8);

        var selected = Swatch("presets", "#0078D4");
        await Expect(selected).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(selected).ToHaveClassAsync(HasClass("bit-clp-prt-sel"));
        await Expect(Swatch("presets", "#107C10")).ToHaveAttributeAsync("aria-label", new Regex(@", #107C10$"));
        Assert.AreEqual("rgb(16, 124, 16)", await Swatch("presets", "#107C10").EvaluateAsync<string>("el => getComputedStyle(el, '::after').backgroundColor"));

        await Swatch("presets", "#107C10").ClickAsync();
        await Expect(Out("presets-value")).ToHaveTextAsync("#107C10");
        await Expect(Out("presets-ends")).ToHaveTextAsync("1");
        await Expect(Swatch("presets", "#107C10")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(selected).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Preview("presets")).ToHaveCSSAsync("background-color", "rgb(16, 124, 16)");
    }

    [TestMethod]
    public async Task Presets_AreOneTabStop_WalkedWithTheArrowKeys()
    {
        await OpenAsync(Url);

        var swatches = Swatches("presets");
        // Only the swatch the picker is on is in the tab order.
        await Expect(swatches.Nth(4)).ToHaveAttributeAsync("tabindex", "0");
        await Expect(swatches.Nth(0)).ToHaveAttributeAsync("tabindex", "-1");

        await Hue("presets").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(swatches.Nth(4)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(swatches.Nth(5)).ToBeFocusedAsync();
        // Walking only moves the focus.
        await Expect(Out("presets-value")).ToHaveTextAsync("#0078D4");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(swatches.Nth(6)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(swatches.Nth(7)).ToBeFocusedAsync();
        // Clamped rather than wrapped at the ends: past the last swatch and back is the one before it.
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(swatches.Nth(6)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(swatches.Nth(0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(swatches.Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(swatches.Nth(0)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Out("presets-value")).ToHaveTextAsync("#D13438");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(swatches.Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync(" ");
        await Expect(Out("presets-value")).ToHaveTextAsync("#FF8C00");

        var scroll = await Page.EvaluateAsync<double>("() => window.scrollY");
        await Page.Keyboard.PressAsync("ArrowDown");
        Assert.AreEqual(scroll, await Page.EvaluateAsync<double>("() => window.scrollY"), "The arrow keys walk the palette rather than scroll the page.");
    }

    [TestMethod]
    public async Task Presets_PerRow_LaysThemOutOnAGridWalkedByRows()
    {
        await OpenAsync(Url);

        var toolbar = Page.Locator("#presets-grid .bit-clp-prs");
        await Expect(toolbar).ToHaveClassAsync(HasClass("bit-clp-prg"));
        var swatches = Swatches("presets-grid");
        var first = (await swatches.Nth(0).BoundingBoxAsync())!;
        var sixth = (await swatches.Nth(5).BoundingBoxAsync())!;
        var fifth = (await swatches.Nth(4).BoundingBoxAsync())!;
        Assert.AreEqual(first.X, sixth.X, 1, "The sixth swatch starts the second row.");
        Assert.IsTrue(sixth.Y > first.Y);
        Assert.AreEqual(first.Y, fifth.Y, 1);

        await swatches.Nth(3).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(swatches.Nth(8)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(swatches.Nth(3)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Presets_WithAnAlpha_ApplyItsTransparency()
    {
        await OpenAsync(Url);

        await Swatch("presets-alpha", "rgba(0,120,212,0.5)").ClickAsync();
        await Expect(Out("presets-alpha-value")).ToHaveTextAsync("rgba(0,120,212,0.5)");
        await Expect(AlphaSlider("presets-alpha")).ToHaveValueAsync("0.5");
        await Expect(Swatch("presets-alpha", "rgba(0,120,212,0.5)")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Swatch("presets-alpha", "rgba(0,120,212,1)")).ToHaveAttributeAsync("aria-pressed", "false");

        await Swatch("presets-alpha", "#D1343880").ClickAsync();
        await Expect(Out("presets-alpha-value")).ToHaveTextAsync("rgba(209,52,56,0.5)");
    }

    // ------------------------------------------------------------------ eyedropper

    [TestMethod]
    public async Task EyeDropper_IsOfferedOnlyWhereTheBrowserHasOne()
    {
        await OpenAsync(Url);

        var supported = await Page.EvaluateAsync<bool>("() => typeof window.EyeDropper === 'function'");
        await Expect(Page.Locator("#eyedropper .bit-clp-eyd")).ToHaveCountAsync(supported ? 1 : 0);
        await Expect(Page.Locator("#basic .bit-clp-eyd")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task EyeDropper_TheSampledColorIsApplied()
    {
        await Page.AddInitScriptAsync(EyeDropperStub);
        await OpenAsync(Url);

        var button = Page.Locator("#eyedropper .bit-clp-eyd");
        await Expect(button).ToHaveAttributeAsync("title", "Pick a color from the screen");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Pick a color from the screen");
        await Expect(button.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Eyedropper"));

        await button.ClickAsync();
        await Expect(Out("eyedropper-value")).ToHaveTextAsync("#123456");
        await Expect(Out("eyedropper-ends")).ToHaveTextAsync("1");
        await Expect(await FieldAsync("eyedropper", "hex")).ToHaveValueAsync("#123456");
    }

    [TestMethod]
    public async Task EyeDropper_Dismissed_LeavesTheColorAlone()
    {
        await Page.AddInitScriptAsync("window.EyeDropper = class { open() { return Promise.reject(new DOMException('dismissed', 'AbortError')); } };");
        await OpenAsync(Url);

        await Page.Locator("#eyedropper .bit-clp-eyd").ClickAsync();
        await Page.Locator("#eyedropper .bit-clp-eyd").ClickAsync();
        await Area("eyedropper").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Out("eyedropper-value")).ToHaveTextAsync("#FC0000");
        await Expect(Out("eyedropper-ends")).ToHaveTextAsync("1");
    }

    // ------------------------------------------------------------------ contrast

    [TestMethod]
    public async Task Contrast_ReadsTheRatioAgainstTheContrastColor_WithBothVerdicts()
    {
        await OpenAsync(Url);

        var ratio = Page.Locator("#contrast .bit-clp-cnv");
        var badges = Page.Locator("#contrast .bit-clp-cnb");
        await Expect(ratio).ToHaveTextAsync("4.54:1");
        await Expect(ratio).ToHaveAttributeAsync("aria-label", "Contrast ratio 4.54 to 1");
        await Expect(ratio).ToHaveAttributeAsync("role", "img");
        await Expect(badges).ToHaveTextAsync(["AA", "AA Large"]);
        await Expect(badges.Nth(0)).ToHaveClassAsync(HasClass("bit-clp-cnp"));
        await Expect(badges.Nth(0)).ToHaveAttributeAsync("aria-label", "WCAG AA for normal text: pass");
        await Expect(badges.Nth(1)).ToHaveAttributeAsync("aria-label", "WCAG AA for large text: pass");
        // The verdict is read with the area.
        await Expect(Area("contrast")).ToHaveAttributeAsync("aria-describedby", (await Page.Locator("#contrast .bit-clp-cnr").GetAttributeAsync("id"))!);

        var hex = await FieldAsync("contrast", "hex");
        await hex.FillAsync("#AAAAAA");
        await hex.PressAsync("Tab");
        await Expect(ratio).ToHaveTextAsync("2.32:1");
        await Expect(badges.Nth(0)).ToHaveClassAsync(HasClass("bit-clp-cnf"));
        await Expect(badges.Nth(1)).ToHaveClassAsync(HasClass("bit-clp-cnf"));
        await Expect(badges.Nth(1)).ToHaveAttributeAsync("aria-label", "WCAG AA for large text: fail");
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-err)"), await StyleOfAsync(badges.Nth(0), "color"));

        await Page.ClickAsync("#btn-contrast-black");
        await Expect(ratio).ToHaveTextAsync(new Regex(@"^9\.\d+:1$"));
        await Expect(badges.Nth(0)).ToHaveClassAsync(HasClass("bit-clp-cnp"));
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-suc)"), await StyleOfAsync(badges.Nth(0), "color"));
    }

    [TestMethod]
    public async Task Contrast_ATransparentColorIsCompositedFirst_AndADialWithoutAreaDescribesTheFirstControl()
    {
        await OpenAsync(Url);

        var ratio = Page.Locator("#contrast-dark .bit-clp-cnv");
        await Expect(ratio).ToHaveTextAsync(new Regex(@"^1[6-8](\.\d+)?:1$"));
        await AlphaSlider("contrast-dark").FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(ratio).ToHaveTextAsync("1:1");
        await Expect(Out("contrast-dark-value")).ToHaveTextAsync("rgba(255,255,255,0)");

        var describedBy = await Page.Locator("#contrast-alone .bit-clp-cnr").GetAttributeAsync("id");
        await Expect(Hue("contrast-alone")).ToHaveAttributeAsync("aria-describedby", describedBy!);
    }

    // ------------------------------------------------------------------ binding

    [TestMethod]
    public async Task Binding_TheValueAndTheAlphaFlowBothWays_InEveryNotationTheParserReads()
    {
        await OpenAsync(Url);

        var text = Page.Locator("#binding-text");
        var preview = Preview("binding");

        await Area("binding").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(text).ToHaveValueAsync("#FC0000");

        // Whatever notation the page writes is read - and left exactly as written: the picker never hands back a string
        // in another notation for a color nobody picked.
        foreach (var (typed, color, alpha) in new[]
                 {
                     ("tomato", "rgb(255, 99, 71)", "1"),
                     ("#0f8", "rgb(0, 255, 136)", "1"),
                     ("rgb(255 128 0 / 40%)", "rgba(255, 128, 0, 0.4)", "0.4"),
                     ("hwb(280 10% 20%)", null, "0.4"),
                     ("oklch(0.7 0.18 200)", null, "0.4"),
                     ("transparent", "rgba(0, 0, 0, 0)", "0"),
                 })
        {
            await text.FillAsync(typed);
            await text.PressAsync("Tab");
            await Expect(Out("binding-value")).ToHaveTextAsync(typed);
            await Expect(text).ToHaveValueAsync(typed);
            if (color is not null) await Expect(preview).ToHaveCSSAsync("background-color", color);
            // A color that carries its own alpha moves the alpha the picker is on; one that carries none keeps it.
            await Expect(AlphaSlider("binding")).ToHaveValueAsync(alpha);
        }

        // A color the user then picks is written back in the notation the value arrived in.
        await text.FillAsync("#0f8");
        await text.PressAsync("Tab");
        await Area("binding").FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Out("binding-value")).ToHaveTextAsync("#FFFFFF");

        // The page writes the alpha on its own as well.
        await text.FillAsync("#336699");
        await text.PressAsync("Tab");
        await Page.ClickAsync("#btn-set-alpha");
        await Expect(Page.Locator("#alpha .bit-clp-asd input")).ToHaveValueAsync("0.25");
        await Expect(Preview("alpha")).ToHaveCSSAsync("background-color", "rgba(0, 120, 212, 0.25)");

        // A move of the alpha slider writes the bound alpha back.
        await AlphaSlider("alpha").FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Out("alpha-alpha")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task OneWayValue_ThePageDrivesThePicker_AndItsControlsAreInert()
    {
        await OpenAsync(Url);

        var hex = await FieldAsync("one-way", "hex");
        await Expect(hex).ToBeDisabledAsync();
        await Expect(Hue("one-way")).ToBeDisabledAsync();
        await Expect(Area("one-way")).Not.ToHaveAttributeAsync("aria-disabled", "true");

        await Area("one-way").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(hex).ToHaveValueAsync("#FF0000");

        await Page.ClickAsync("#btn-one-way-green");
        await Expect(hex).ToHaveValueAsync("#00FF00");
        await Expect(Preview("one-way")).ToHaveCSSAsync("background-color", "rgb(0, 255, 0)");
        await Page.ClickAsync("#btn-one-way-tomato");
        await Expect(hex).ToHaveValueAsync("#FF6347");
    }

    // ------------------------------------------------------------------ events & API

    [TestMethod]
    public async Task Events_OnColorChangeOnChangeEndAndOnChange_FireWithTheColorInEveryNotation()
    {
        await OpenAsync(Url);

        await Area("events").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        await Expect(Out("ev-color")).ToHaveTextAsync("1");
        await Expect(Out("ev-end")).ToHaveTextAsync("1");
        await Expect(Out("ev-change")).ToHaveTextAsync("1");
        await Expect(Out("ev-change-arg")).ToHaveTextAsync("#FC0000");
        await Expect(Out("ev-color-arg")).ToHaveTextAsync("#FC0000|#FC0000|rgba(252,0,0,1)|1");
        await Expect(Out("ev-end-arg")).ToHaveTextAsync("#FC0000|#FC0000|#FC0000FF|rgb(252,0,0)|rgba(252,0,0,1)|vibrant red");
        await Expect(Out("ev-end-tuples")).ToHaveTextAsync(new Regex(@"^0,1,0\.5\|0,1,0\.99\|0,0,0\.01\|0\.6\d*,0\.2\d*,29\.\d+$"));

        // A slider dragged reports every step and ends once, when it is let go.
        var hue = Hue("events");
        var box = (await hue.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(box.X + 2, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width / 3, box.Y + box.Height / 2, new() { Steps = 6 });
        await Page.Mouse.UpAsync();
        await Expect(Out("ev-end")).ToHaveTextAsync("2");
        var changes = int.Parse((await Out("ev-color").TextContentAsync())!, CultureInfo.InvariantCulture);
        Assert.IsTrue(changes >= 3, $"OnColorChange fires on the steps of the slider ({changes}).");

        // The alpha alone moves no notation the value has room for: OnColorChange still reports it.
        var before = changes;
        await AlphaSlider("events").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Out("ev-color")).ToHaveTextAsync((before + 1).ToString(CultureInfo.InvariantCulture));
        await Expect(Out("ev-color-arg")).ToHaveTextAsync(new Regex(@"\|0\.99$"));
    }

    [TestMethod]
    public async Task PublicMembers_ReadTheColorInEveryNotation_AndFocusAsyncFocusesTheArea()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-read");
        await Expect(Out("api")).ToHaveTextAsync("#FF0000|#FF0000FF|rgb(255,0,0)|rgba(255,0,0,1)|vibrant red");
        await Expect(Out("api-tuples")).ToHaveTextAsync(new Regex(@"^0,1,0\.5\|0,1,1\|0,0,0\|0\.6\d*,0\.2\d*,29\.\d+$"));

        await Page.ClickAsync("#btn-focus");
        await Expect(Area("events")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task FocusAsync_OnACompactPicker_LandsOnItsFirstControl()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-focus-palette");
        // The palette is entered at the swatch the picker is on.
        await Expect(Swatch("palette", "#107C10")).ToBeFocusedAsync();

        await Page.ClickAsync("#btn-focus-fields");
        await Expect(AlphaSlider("fields")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AutoFocus_FocusesTheArea_OrTheFirstControlOfACompactPicker_AndNothingWhenDisabled()
    {
        await OpenAsync("/e2e/color-picker/setup?mode=area");
        await Expect(Area("setup")).ToBeFocusedAsync();

        await OpenAsync("/e2e/color-picker/setup?mode=palette");
        await Expect(Swatch("setup", "#107C10")).ToBeFocusedAsync();

        await OpenAsync("/e2e/color-picker/setup?mode=disabled");
        Assert.AreEqual("BODY", await Page.EvaluateAsync<string>("() => document.activeElement.tagName"));
    }

    // ------------------------------------------------------------------ compact pickers

    [TestMethod]
    public async Task CompactPickers_ShowOnlyTheirParts()
    {
        await OpenAsync(Url);

        await Expect(Area("palette")).ToHaveCountAsync(0);
        await Expect(Hue("palette")).ToHaveCountAsync(0);
        await Swatch("palette", "#5C2D91").ClickAsync();
        await Expect(Out("palette-value")).ToHaveTextAsync("#5C2D91");

        // A tint picker is pinned to its hue: the area moves the shade only.
        await Expect(Hue("tint")).ToHaveCountAsync(0);
        await Area("tint").FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Out("tint-value")).ToHaveTextAsync("#D4D4D4");
        await Page.Keyboard.PressAsync("End");
        await Expect(Out("tint-value")).ToHaveTextAsync("#0078D4");

        await Expect(Area("fields")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#fields .bit-clp-fin")).ToHaveCountAsync(5);

        // A picker with no visible part still carries its value.
        await Expect(Page.Locator("#nothing .bit-clp-cnt")).ToHaveCountAsync(0);
        var hidden = Page.Locator("#nothing input.bit-input-hidden");
        await Expect(hidden).ToHaveValueAsync("#123456");
        await Expect(hidden).ToHaveAttributeAsync("name", "lonely");
    }

    // ------------------------------------------------------------------ disabled & read-only

    [TestMethod]
    public async Task Disabled_RendersTheDisabledContract_AndRunsNoneOfThePageHandlers()
    {
        await OpenAsync(Url);

        var root = Root("disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(root).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Area("disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Area("disabled")).ToHaveAttributeAsync("tabindex", "-1");
        Assert.AreEqual("none", await StyleOfAsync(Area("disabled"), "pointer-events"));

        foreach (var control in await Page.Locator("#disabled input:not(.bit-input-hidden), #disabled button:not(#btn-toggle-disabled)").AllAsync())
        {
            await Expect(control).ToBeDisabledAsync();
            await Expect(control).ToHaveAttributeAsync("aria-disabled", "true");
        }

        await Expect(Page.Locator("#disabled input.bit-input-hidden")).ToBeDisabledAsync();

        await root.DispatchEventAsync("click");
        await root.DispatchEventAsync("keydown", new { key = "Enter" });
        await Area("disabled").DispatchEventAsync("keydown", new { key = "ArrowDown", bubbles = true });
        await Expect(Out("disabled-click-count")).ToHaveTextAsync("0");
        await Expect(Out("disabled-key-count")).ToHaveTextAsync("0");
        await Expect(Out("disabled-value")).ToHaveTextAsync("#B34D4D");

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Area("disabled")).ToHaveAttributeAsync("tabindex", "0");
        await root.DispatchEventAsync("click");
        await Expect(Out("disabled-click-count")).ToHaveTextAsync("1");

        await Swatch("disabled", "#107C10").ClickAsync();
        await Expect(Out("disabled-value")).ToHaveTextAsync("#107C10");
    }

    [TestMethod]
    public async Task ReadOnly_KeepsEveryControlReachableButRefusesEveryChange()
    {
        await OpenAsync(Url);

        await Expect(Root("readonly")).ToHaveClassAsync(HasClass("bit-clp-rdl"));
        var area = Area("readonly");
        await Expect(area).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(area).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Hue("readonly")).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(Hue("readonly")).ToBeEnabledAsync();
        await Expect(await FieldAsync("readonly", "hex")).ToHaveAttributeAsync("readonly", "");
        await Expect(Swatches("readonly").First).ToHaveAttributeAsync("aria-disabled", "true");

        await area.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Home");

        // A native range moves its own thumb before any handler runs, so the picker puts it back.
        await Hue("readonly").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Hue("readonly")).ToHaveValueAsync("0");
        await AlphaSlider("readonly").FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(AlphaSlider("readonly")).ToHaveValueAsync("1");

        await Swatch("readonly", "#107C10").ClickAsync(new() { Force = true });
        await Expect(Out("readonly-value")).ToHaveTextAsync("#B34D4D");
        await Expect(Out("readonly-ends")).ToHaveTextAsync("0");
        // The colors keep their contrast: a read-only picker is showing a value.
        Assert.AreEqual("1", await StyleOfAsync(area, "opacity"));

        // The mode switch only changes how the color is read, so it still answers.
        await Page.Locator("#readonly .bit-clp-swb").ClickAsync();
        await Expect(Page.Locator("#readonly .bit-clp-flb").First).ToHaveTextAsync("Hex");
        await Expect(Page.Locator("#readonly .bit-clp-flb")).ToHaveCountAsync(2);
    }

    // ------------------------------------------------------------------ texts

    [TestMethod]
    public async Task Texts_TranslateEveryStringThePickerWrites()
    {
        await Page.AddInitScriptAsync(EyeDropperStub);
        await OpenAsync(Url);

        var root = Root("texts");
        await Expect(root).ToHaveAttributeAsync("aria-label", "Waehler 255-0-0-100");
        await Expect(Area("texts")).ToHaveAttributeAsync("aria-label", "Sattigung");
        await Expect(Area("texts")).ToHaveAttributeAsync("aria-roledescription", "Flaeche");
        await Expect(Area("texts")).ToHaveAttributeAsync("aria-valuetext", "#FF0000 (100/100)");
        await Expect(Hue("texts")).ToHaveAttributeAsync("aria-label", "Farbton");
        await Expect(Hue("texts")).ToHaveAttributeAsync("aria-valuetext", "Farbton 0");
        await Expect(AlphaSlider("texts")).ToHaveAttributeAsync("aria-label", "Deckkraft");
        await Expect(AlphaSlider("texts")).ToHaveAttributeAsync("aria-valuetext", "Deckkraft 100");
        await Expect(Page.Locator("#texts .bit-clp-flb")).ToHaveTextAsync(["Hexa", "Ro", "Gr", "Bl", "D%"]);
        await Expect(await FieldAsync("texts", "r")).ToHaveAttributeAsync("title", "Rot");
        await Expect(Page.Locator("#texts .bit-clp-eyd")).ToHaveAttributeAsync("aria-label", "Pipette");
        await Expect(Page.Locator("#texts .bit-clp-swb")).ToHaveAttributeAsync("aria-label", "Felder HEXRGB wechseln");
        await Expect(Page.Locator("#texts .bit-clp-swb")).ToHaveAttributeAsync("title", "Felder: HEXRGB");
        await Expect(Page.Locator("#texts .bit-clp-prs")).ToHaveAttributeAsync("aria-label", "Vorlagen");
        await Expect(Swatch("texts", "#107C10")).ToHaveAttributeAsync("aria-label", "Vorlage #107C10");
        await Expect(Page.Locator("#texts .bit-clp-cnv")).ToHaveAttributeAsync("aria-label", "Kontrast 4");
        await Expect(Page.Locator("#texts .bit-clp-cnb")).ToHaveTextAsync(["AA!", "AA gross"]);
        await Expect(Page.Locator("#texts .bit-clp-cnb").Nth(0)).ToHaveAttributeAsync("aria-label", "AA normal nein");
        await Expect(Page.Locator("#texts .bit-clp-cnb").Nth(1)).ToHaveAttributeAsync("aria-label", "AA gross ja");
    }

    // ------------------------------------------------------------------ cascade

    [TestMethod]
    public async Task BitParams_CascadesItsDefaults_AndAPickerOwnParameterWins()
    {
        await OpenAsync(Url);

        await Expect(Swatches("cascade-1")).ToHaveCountAsync(10);
        await Expect(Page.Locator("#cascade-1 .bit-clp-pre")).ToHaveCountAsync(1);
        await Expect(AlphaSlider("cascade-1")).ToHaveCountAsync(1);
        // The mode is cascaded as a default: RGB fields, no hex field.
        await Expect(Page.Locator("#cascade-1 .bit-clp-flb")).ToHaveTextAsync(["R", "G", "B", "A%"]);
        await Area("cascade-1").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Out("cascade-1-value")).ToHaveTextAsync("#FC0000");

        await Expect(Page.Locator("#cascade-2 .bit-clp-pre")).ToHaveCountAsync(0);
        await Expect(Root("cascade-2")).ToHaveClassAsync(HasClass("bit-clp-sm"));
        await Expect(Swatches("cascade-2")).ToHaveCountAsync(10);
        await Area("cascade-2").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Out("cascade-2-value")).ToHaveTextAsync("rgb(252,0,0)");

        await Expect(Swatches("cascade-none")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cascade-none .bit-clp-inps")).ToHaveCountAsync(0);
    }

    // ------------------------------------------------------------------ validation

    [TestMethod]
    public async Task EditForm_ARequiredPickerBoundToNothingIsInvalidUntilAColorIsPicked()
    {
        await OpenAsync(Url);

        // Nothing bound: white is shown but not taken as the value.
        await Expect(await FieldAsync("form", "hex")).ToHaveValueAsync("#FFFFFF");
        await Expect(Out("form-value")).ToHaveTextAsync("");

        await Page.ClickAsync("#form-submit");
        await WaitForStatusAsync("Rejected 1");
        await Expect(Page.Locator("#form-message")).ToHaveTextAsync("Pick a brand color.");
        await Expect(Root("form")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Area("form")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(await FieldAsync("form", "hex")).ToHaveAttributeAsync("aria-invalid", "true");
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-err)"), await StyleOfAsync(Area("form"), "border-top-color"));

        await Swatch("form", "#D13438").ClickAsync();
        await Expect(Page.Locator("#form-message")).ToHaveTextAsync("");
        await Expect(Root("form")).Not.ToHaveClassAsync(HasClass("bit-inv"));
        // A value bound to nothing names no notation, so the picker answers in rgb().
        await Expect(Page.Locator("#form input.bit-input-hidden")).ToHaveValueAsync("rgb(209,52,56)");
        await Expect(Out("form-value")).ToHaveTextAsync("rgb(209,52,56)");
        await Expect(Page.Locator("#form input.bit-input-hidden")).ToHaveAttributeAsync("name", "brand");

        await Page.ClickAsync("#form-submit");
        await WaitForStatusAsync("Saved 2");
    }

    // ------------------------------------------------------------------ icons

    [TestMethod]
    public async Task Icons_TheEyeDropperAndSwitchTakeTheirIconNamesOrExternalIcons()
    {
        await Page.AddInitScriptAsync(EyeDropperStub);
        await OpenAsync(Url);

        await Expect(Page.Locator("#icons-default .bit-clp-eyi")).ToHaveClassAsync(HasClass("bit-icon--Eyedropper"));
        await Expect(Page.Locator("#icons-default .bit-clp-swi")).ToHaveClassAsync(HasClass("bit-icon--Sort"));
        await Expect(Page.Locator("#icons-bit .bit-clp-eyi")).ToHaveClassAsync(HasClass("bit-icon--Color"));
        await Expect(Page.Locator("#icons-bit .bit-clp-swi")).ToHaveClassAsync(HasClass("bit-icon--Switch"));
        await Expect(Page.Locator("#icons-fa .bit-clp-eyi")).ToHaveClassAsync(new Regex(@"(^|\s)fa-solid fa-eye-dropper(\s|$)"));
        await Expect(Page.Locator("#icons-fa .bit-clp-eyi")).Not.ToHaveClassAsync(HasClass("bit-icon--Color"));
        await Expect(Page.Locator("#icons-fa .bit-clp-swi")).ToHaveClassAsync(new Regex(@"(^|\s)fa-solid fa-right-left(\s|$)"));
        await Expect(Page.Locator("#icons-bi .bit-clp-eyi")).ToHaveClassAsync(new Regex(@"(^|\s)bi bi-eyedropper(\s|$)"));
        await Expect(Page.Locator("#icons-bi .bit-clp-swi")).ToHaveClassAsync(new Regex(@"(^|\s)bi bi-arrow-left-right(\s|$)"));
    }

    // ------------------------------------------------------------------ size

    [TestMethod]
    public async Task Size_EachValue_SetsItsClassAndScalesThePanel()
    {
        await OpenAsync(Url);

        var widths = new List<float>();
        var heights = new List<float>();
        var swatches = new List<float>();
        foreach (var (scope, css) in new[] { ("size-small", "bit-clp-sm"), ("size-medium", "bit-clp-md"), ("size-large", "bit-clp-lg") })
        {
            await Expect(Root(scope)).ToHaveClassAsync(HasClass(css));
            widths.Add((await Root(scope).BoundingBoxAsync())!.Width);
            heights.Add((await Area(scope).BoundingBoxAsync())!.Height);
            swatches.Add((await Swatches(scope).First.BoundingBoxAsync())!.Width);
        }

        Assert.IsTrue(widths[0] < widths[1] && widths[1] < widths[2], $"Widths grow: {string.Join(", ", widths)}");
        Assert.IsTrue(heights[0] < heights[1] && heights[1] < heights[2], $"Areas grow: {string.Join(", ", heights)}");
        Assert.IsTrue(swatches[0] <= swatches[1] && swatches[1] < swatches[2], $"Swatches grow: {string.Join(", ", swatches)}");
        Assert.AreEqual(widths[1], (await Root("size-none").BoundingBoxAsync())!.Width, 1, "Medium is what an unset size stands for.");
        await Expect(Root("size-none")).Not.ToHaveClassAsync(new Regex(@"bit-clp-(sm|md|lg)(\s|$)"));
    }

    // ------------------------------------------------------------------ style & class

    [TestMethod]
    public async Task StyleAndClass_ApplyToTheRoot_AndAFixedHeightShrinksTheArea()
    {
        await OpenAsync(Url);

        var styled = Root("sc-style");
        Assert.AreEqual("230px", await StyleOfAsync(styled, "width"));
        Assert.AreEqual("150px", await StyleOfAsync(styled, "height"));
        Assert.IsTrue((await Area("sc-style").BoundingBoxAsync())!.Height < (await Area("basic").BoundingBoxAsync())!.Height);
        await Expect(Root("sc-class")).ToHaveClassAsync(HasClass("custom-class"));
    }

    [TestMethod]
    public async Task Styles_ReachEveryPart()
    {
        await Page.AddInitScriptAsync(EyeDropperStub);
        await OpenAsync(Url);

        var s = "#sc-styles";
        var expected = new (string Selector, string Property, string Value)[]
        {
            (".bit-clp", "margin-top", "9px"),
            (".bit-clp-lbc", "padding-top", "3px"),
            (".bit-clp-lbl", "color", "rgb(1, 1, 1)"),
            (".bit-clp-rec", "border-top-left-radius", "16px"),
            (".bit-clp-thumb", "width", "24px"),
            (".bit-clp-cnt", "padding-top", "4px"),
            (".bit-clp-sct", "padding-top", "5px"),
            (".bit-clp-hsd", "padding-top", "6px"),
            (".bit-clp-asd", "padding-top", "7px"),
            (".bit-clp-inp", "opacity", "0.9"),
            (".bit-clp-eyd", "color", "rgb(2, 2, 2)"),
            (".bit-clp-eyi", "color", "rgb(3, 3, 3)"),
            (".bit-clp-pre", "border-top-left-radius", "50%"),
            (".bit-clp-inps", "padding-top", "8px"),
            (".bit-clp-fld", "padding-top", "1px"),
            (".bit-clp-fin", "color", "rgb(4, 4, 4)"),
            (".bit-clp-flb", "color", "rgb(5, 5, 5)"),
            (".bit-clp-swb", "color", "rgb(6, 6, 6)"),
            (".bit-clp-swi", "color", "rgb(7, 7, 7)"),
            (".bit-clp-cnr", "padding-top", "2px"),
            (".bit-clp-cnv", "color", "rgb(8, 8, 8)"),
            (".bit-clp-cnb", "padding-top", "3px"),
            (".bit-clp-prs", "padding-top", "9px"),
            (".bit-clp-prt", "border-top-left-radius", "50%"),
            (".bit-clp-prt-sel", "outline-color", "rgb(9, 9, 9)"),
        };

        foreach (var (selector, property, value) in expected)
        {
            Assert.AreEqual(value, await ComputedStyleAsync($"{s} {selector}", property), $"{selector} {property}");
        }

        // Only the selected swatch takes the selected style.
        Assert.AreNotEqual("rgb(9, 9, 9)", await StyleOfAsync(Swatch("sc-styles", "#107C10"), "outline-color"));
    }

    [TestMethod]
    public async Task Classes_ReachEveryPart()
    {
        await Page.AddInitScriptAsync(EyeDropperStub);
        await OpenAsync(Url);

        foreach (var (selector, css) in new[]
                 {
                     (".bit-clp", "c-root"), (".bit-clp-lbc", "c-label-container"), (".bit-clp-lbl", "c-label"), (".bit-clp-rec", "c-saturation"),
                     (".bit-clp-thumb", "c-thumb"), (".bit-clp-cnt", "c-content"), (".bit-clp-sct", "c-sliders"), (".bit-clp-hsd", "c-hue"),
                     (".bit-clp-asd", "c-alpha"), (".bit-clp-inp", "c-slider-input"), (".bit-clp-eyd", "c-eyedropper"), (".bit-clp-eyi", "c-eyedropper-icon"),
                     (".bit-clp-pre", "c-preview"), (".bit-clp-inps", "c-inputs"), (".bit-clp-fld", "c-field"), (".bit-clp-fin", "c-field-input"),
                     (".bit-clp-flb", "c-field-label"), (".bit-clp-swb", "c-switch"), (".bit-clp-swi", "c-switch-icon"), (".bit-clp-cnr", "c-contrast"),
                     (".bit-clp-cnv", "c-contrast-ratio"), (".bit-clp-cnb", "c-contrast-badge"), (".bit-clp-prs", "c-presets"), (".bit-clp-prt", "c-preset"),
                     (".bit-clp-prt-sel", "c-selected-preset")
                 })
        {
            await Expect(Page.Locator($"#sc-classes {selector}").First).ToHaveClassAsync(HasClass(css));
        }

        await Expect(Swatch("sc-classes", "#107C10")).Not.ToHaveClassAsync(HasClass("c-selected-preset"));
    }

    // ------------------------------------------------------------------ css variables

    [TestMethod]
    public async Task CssVariables_OnTheInstanceAndOnAnAncestor_RestyleThePanel_AndAnExplicitSizeWins()
    {
        await OpenAsync(Url);

        var root = Root("css-vars-instance");
        Assert.AreEqual("288px", await StyleOfAsync(root, "width"));
        Assert.AreEqual("6px", await StyleOfAsync(root, "padding-top"));
        Assert.AreEqual("rgb(250, 240, 230)", await StyleOfAsync(root, "background-color"));
        var area = Area("css-vars-instance");
        Assert.AreEqual("128px", await StyleOfAsync(area, "height"));
        Assert.AreEqual("16px", await StyleOfAsync(area, "border-top-left-radius"));
        Assert.AreEqual("rgb(126, 74, 226)", await StyleOfAsync(area, "border-top-color"));
        Assert.AreEqual(12, (await Page.Locator("#css-vars-instance .bit-clp-hsd").BoundingBoxAsync())!.Height, 1);
        var swatch = Swatches("css-vars-instance").First;
        Assert.AreEqual(20, (await swatch.BoundingBoxAsync())!.Width, 1);
        Assert.AreEqual("50%", await StyleOfAsync(swatch, "border-top-left-radius"));

        Assert.AreEqual("300px", await StyleOfAsync(Root("css-vars-ancestor"), "width"));
        Assert.AreEqual(40, (await Page.Locator("#css-vars-ancestor .bit-clp-pre").BoundingBoxAsync())!.Width, 1);
        Assert.AreEqual("rgb(10, 20, 30)", await ComputedStyleAsync("#css-vars-ancestor .bit-clp-fin", "background-color"));

        // An explicit Size wins over the size variables, the rest stays the variables'.
        Assert.AreNotEqual("300px", await StyleOfAsync(Root("css-vars-explicit"), "width"));
        Assert.AreEqual("rgb(10, 20, 30)", await ComputedStyleAsync("#css-vars-explicit .bit-clp-fin", "background-color"));
    }

    // ------------------------------------------------------------------ rtl

    [TestMethod]
    public async Task Rtl_Explicit_MirrorsTheAxesTheThumbAndTheKeys()
    {
        await OpenAsync(Url);

        var root = Root("rtl-explicit");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");

        // Full saturation is the inline-end edge: the left one.
        var area = Area("rtl-explicit");
        var box = (await area.BoundingBoxAsync())!;
        var thumb = (await Page.Locator("#rtl-explicit .bit-clp-thumb").BoundingBoxAsync())!;
        Assert.AreEqual(box.X, thumb.X + thumb.Width / 2, 2);

        await area.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Out("rtl-value")).ToHaveTextAsync("#FF0303");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Out("rtl-value")).ToHaveTextAsync("#FF0000");

        // Dragged off past the top-right corner, a right-to-left picker lands on no saturation: white.
        var (x, y) = await PointAsync(area, 0.5, 0.1);
        box = (await area.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width + 40, box.Y - 40, new() { Steps = 6 });
        await Page.Mouse.UpAsync();
        await Expect(Out("rtl-value")).ToHaveTextAsync("#FFFFFF");

        // The palette is walked in the reading direction.
        var swatches = Swatches("rtl-explicit");
        await swatches.Nth(3).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(swatches.Nth(2)).ToBeFocusedAsync();
        Assert.IsTrue((await swatches.Nth(0).BoundingBoxAsync())!.X > (await swatches.Nth(1).BoundingBoxAsync())!.X, "The swatches run right to left.");

        // The light gradient and the hue track mirror too.
        StringAssert.Contains(await StyleOfAsync(Page.Locator("#rtl-explicit .bit-clp-light"), "background-image"), "to left");
        StringAssert.Contains(await StyleOfAsync(Page.Locator("#ltr .bit-clp-light"), "background-image"), "to right");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_IsFollowedWithoutTheClass()
    {
        await OpenAsync(Url);

        await Expect(Root("rtl-inherited")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        var area = Area("rtl-inherited");
        var box = (await area.BoundingBoxAsync())!;
        var thumb = (await Page.Locator("#rtl-inherited .bit-clp-thumb").BoundingBoxAsync())!;
        Assert.AreEqual(box.X, thumb.X + thumb.Width / 2, 2);

        await area.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Out("rtl-inherited-value")).ToHaveTextAsync("#FF0303");

        // Dragged off past the top-left corner, an inherited right-to-left picker lands on full saturation.
        var (x, y) = await PointAsync(area, 0.5, 0.1);
        box = (await area.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X - 40, box.Y - 40, new() { Steps = 6 });
        await Page.Mouse.UpAsync();
        await Expect(Out("rtl-inherited-value")).ToHaveTextAsync("#FF0000");

        var swatches = Swatches("rtl-inherited");
        await swatches.Nth(3).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(swatches.Nth(4)).ToBeFocusedAsync();
    }

    // ------------------------------------------------------------------ base behaviours

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheSpace_CollapsedRemovesIt()
    {
        await OpenAsync(Url);

        var root = Root("visibility");
        var after = Page.Locator("#visibility-after");
        Task<double> TopAsync() => after.EvaluateAsync<double>("el => el.getBoundingClientRect().top + window.scrollY");
        var visibleY = await TopAsync();

        await Page.ClickAsync("#btn-hidden");
        await Expect(root).ToHaveCSSAsync("visibility", "hidden");
        Assert.AreEqual(visibleY, await TopAsync(), 1);

        await Page.ClickAsync("#btn-collapsed");
        await Expect(root).ToHaveCSSAsync("display", "none");
        Assert.IsTrue(await TopAsync() < visibleY - 10);

        await Page.ClickAsync("#btn-visible");
        await Expect(root).ToBeVisibleAsync();
        Assert.AreEqual(visibleY, await TopAsync(), 1);
    }

    [TestMethod]
    public async Task Attributes_IdHtmlAttributesInputHtmlAttributesNameAndTabIndexReachTheirElements()
    {
        await OpenAsync(Url);

        var root = Page.Locator("#clp-attrs");
        await Expect(root).ToHaveClassAsync(HasClass("bit-clp"));
        await Expect(root).ToHaveAttributeAsync("data-root", "root-value");
        await Expect(root).ToHaveAttributeAsync("title", "Root title");

        var hidden = Page.Locator("#attributes input.bit-input-hidden");
        await Expect(hidden).ToHaveAttributeAsync("name", "paint");
        await Expect(hidden).ToHaveAttributeAsync("data-input", "input-value");
        await Expect(hidden).ToHaveValueAsync("#0078D4");

        await Expect(Area("attributes")).ToHaveAttributeAsync("tabindex", "5");
        await Expect(Hue("attributes")).ToHaveAttributeAsync("tabindex", "5");
        await Expect(await FieldAsync("attributes", "hex")).ToHaveAttributeAsync("tabindex", "5");
        await Expect(Swatch("attributes", "#0078D4")).ToHaveAttributeAsync("tabindex", "5");
        await Expect(Swatch("attributes", "#107C10")).ToHaveAttributeAsync("tabindex", "-1");
    }
}
