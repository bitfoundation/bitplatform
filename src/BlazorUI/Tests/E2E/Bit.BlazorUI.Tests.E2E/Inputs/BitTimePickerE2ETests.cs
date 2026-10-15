using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// Drives BitTimePicker (/e2e/time-picker) in a real browser: the field and its callout (opening, positioning,
/// dismissal by Escape, the overlay, the close button and Tab), the hour/minute/second inputs and their spin buttons
/// (click, press-and-hold, Shift+wheel, the arrow/Page/Home/End keys, typing with AutoAdvance, paste), the 12/24-hour
/// clocks and the meridiem per culture, MinTime/MaxTime/Allowed*/DisablePast/DisableFuture/TimeZone, the steps and
/// their grid, the now/clear/close buttons, AllowTextInput and its parsing, validation in an EditForm, Standalone,
/// two-way binding of Value and IsOpen, the public methods, every event, templates, Color/Size/Style/Class/Styles/
/// Classes, the CSS variables, the BitParams cascade, RTL (explicit, culture-implied and inherited), Visibility, the
/// disabled and read-only contracts, Escape inside a modal, reduced motion and the lifecycle.
/// </summary>
[TestClass]
public class BitTimePickerE2ETests : E2ETestBase
{
    private const string Url = "/e2e/time-picker";

    // ------------------------------------------------------------------ helpers

    private ILocator Root(string scope) => Page.Locator($"#{scope} .bit-tpc").First;

    private ILocator Field(string scope) => Page.Locator($"#{scope} .bit-tpc-inp").First;

    private ILocator Out(string id) => Page.Locator($"#{id}");

    private ILocator StandaloneCallout(string scope) => Page.Locator($"#{scope} .bit-tpc-cal").First;

    private static ILocator Hour(ILocator callout) => callout.Locator(".bit-tpc-tin").Nth(0);

    private static ILocator Minute(ILocator callout) => callout.Locator(".bit-tpc-tin").Nth(1);

    private static ILocator Second(ILocator callout) => callout.Locator(".bit-tpc-tin").Nth(2);

    private static ILocator Spin(ILocator callout, string label) => callout.Locator($"button[aria-label='{label}']");

    private static ILocator Am(ILocator callout) => callout.Locator(".bit-tpc-bam");

    private static ILocator Pm(ILocator callout) => callout.Locator(".bit-tpc-bpm");

    private static ILocator Action(ILocator callout, string text) => callout.Locator(".bit-tpc-abn", new() { HasTextString = text });

    // The callout leaves the root for the body while it is open, so it is found through the id the field points at.
    private async Task<ILocator> CalloutOfAsync(string scope)
    {
        var field = Field(scope);
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "true");

        var id = await field.GetAttributeAsync("aria-controls");
        Assert.IsFalse(string.IsNullOrEmpty(id), "An open picker names its callout through aria-controls.");

        var callout = Page.Locator($"[id='{id}']");
        await Expect(callout).ToBeVisibleAsync();

        return callout;
    }

    private async Task<ILocator> OpenCalloutAsync(string scope)
    {
        await Field(scope).ClickAsync();

        return await CalloutOfAsync(scope);
    }

    // The callout fades and slides in; its box is read once that entry has run out.
    private static Task SettleAsync(ILocator element)
    {
        return element.EvaluateAsync("el => Promise.all(el.getAnimations({ subtree: true }).map(a => a.finished))");
    }

    private async Task<string> CalloutIdAsync(string scope)
    {
        var id = await Field(scope).GetAttributeAsync("id");
        return id!.Replace("-input", "-callout", StringComparison.Ordinal);
    }

    private Task<LocatorBoundingBoxResult?> BoxAsync(ILocator locator) => locator.BoundingBoxAsync();

    // A color as the browser resolves it, from a probe element painted with the given CSS value.
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

    private static Task<string> StyleOfAsync(ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>($"el => getComputedStyle(el).getPropertyValue('{property}')");
    }

    private static float Px(string cssLength) => float.Parse(cssLength.Replace("px", string.Empty, StringComparison.Ordinal), System.Globalization.CultureInfo.InvariantCulture);

    // ------------------------------------------------------------------ lifecycle

    [TestMethod]
    public async Task Prerender_TheServerHtml_AlreadyContainsTheField()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.AreEqual(200, response.Status);
        StringAssert.Contains(html, "id=\"tp-basic\"");
        StringAssert.Contains(html, "class=\"bit-tpc-inp");
        StringAssert.Contains(html, "Basic TimePicker");
        StringAssert.Contains(html, "Status: <span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Lifecycle_ParametersChangedAtRuntime_ReRenderTheField()
    {
        await OpenAsync(Url);

        await Expect(Field("lifecycle")).ToHaveValueAsync("15:20");
        await Expect(Page.Locator("#lifecycle .bit-tpc-lbl")).ToHaveTextAsync("Lifecycle");

        await Page.ClickAsync("#btn-relabel");
        await Expect(Page.Locator("#lifecycle .bit-tpc-lbl")).ToHaveTextAsync("Relabelled");

        await Page.ClickAsync("#btn-twelve");
        await Expect(Field("lifecycle")).ToHaveValueAsync("03:20 PM");

        await Page.ClickAsync("#btn-seconds");
        await Expect(Field("lifecycle")).ToHaveValueAsync("03:20:10 PM");

        var callout = await OpenCalloutAsync("lifecycle");
        await Expect(callout.Locator(".bit-tpc-tin")).ToHaveCountAsync(3);
        await Expect(Pm(callout)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    public async Task Lifecycle_UnmountedWhileOpen_RemovesTheCalloutWithoutAnError()
    {
        await OpenAsync(Url);

        var calloutId = await CalloutIdAsync("lifecycle");
        await OpenCalloutAsync("lifecycle");

        // The overlay covers the page while the callout is open, so the page's own button is clicked from code.
        await Page.Locator("#btn-toggle-mounted").DispatchEventAsync("click");

        await Expect(Page.Locator("#lifecycle .bit-tpc")).ToHaveCountAsync(0);
        await Expect(Page.Locator($"[id='{calloutId}']")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-toggle-mounted");
        await Expect(Field("lifecycle")).ToHaveValueAsync("15:20");
        var callout = await OpenCalloutAsync("lifecycle");
        await Spin(callout, "Increase minute").ClickAsync();
        await Expect(Out("lifecycle-value")).ToHaveTextAsync("15:21:10");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // ------------------------------------------------------------------ basic

    [TestMethod]
    public async Task Basic_ClickTheField_OpensADialogBelowItAndFocusesTheHour()
    {
        await OpenAsync(Url);

        var field = Field("basic");
        await Expect(field).ToHaveAttributeAsync("role", "combobox");
        await Expect(field).ToHaveAttributeAsync("aria-haspopup", "dialog");
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(field).ToHaveAttributeAsync("readonly", "");

        var callout = await OpenCalloutAsync("basic");

        var dialog = callout.Locator(".bit-tpc-cac");
        await Expect(dialog).ToHaveAttributeAsync("role", "dialog");
        await Expect(dialog).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(dialog).ToHaveAttributeAsync("aria-label", "Clock");
        await Expect(Page.Locator("#basic .bit-tpc-wrp")).ToHaveAttributeAsync("aria-owns", (await callout.GetAttributeAsync("id"))!);
        await Expect(Hour(callout)).ToBeFocusedAsync();
        await Expect(Root("basic")).ToHaveClassAsync(HasClass("bit-tpc-foc"));

        await SettleAsync(callout);
        var fieldBox = (await BoxAsync(Page.Locator("#basic .bit-tpc-icn")))!;
        var calloutBox = (await BoxAsync(callout))!;
        Assert.IsTrue(calloutBox.Y >= fieldBox.Y + fieldBox.Height - 2, $"The callout ({calloutBox.Y}) opens below the field ({fieldBox.Y + fieldBox.Height}).");
        Assert.IsTrue(Math.Abs(calloutBox.X - fieldBox.X) < 2, $"The callout ({calloutBox.X}) is aligned with the field ({fieldBox.X}).");
        Assert.IsTrue(calloutBox.Width >= fieldBox.Width - 2, "The callout is at least as wide as the field it opens from.");
    }

    [TestMethod]
    public async Task Basic_SpinButtons_StepEveryPartWrappingAndBindTheValue()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("basic");
        await Expect(Hour(callout)).ToHaveValueAsync("");
        await Expect(Minute(callout)).ToHaveValueAsync("");

        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("basic-value")).ToHaveTextAsync("01:00:00");
        await Expect(Hour(callout)).ToHaveValueAsync("01");
        await Expect(Minute(callout)).ToHaveValueAsync("00");
        await Expect(Field("basic")).ToHaveValueAsync("01:00");

        await Spin(callout, "Increase minute").ClickAsync();
        await Expect(Out("basic-value")).ToHaveTextAsync("01:01:00");

        await Spin(callout, "Decrease hour").ClickAsync();
        await Expect(Out("basic-value")).ToHaveTextAsync("00:01:00");

        await Spin(callout, "Decrease hour").ClickAsync();
        await Expect(Out("basic-value")).ToHaveTextAsync("23:01:00");

        await Spin(callout, "Decrease minute").ClickAsync();
        await Spin(callout, "Decrease minute").ClickAsync();
        await Expect(Out("basic-value")).ToHaveTextAsync("23:59:00");
        await Expect(Field("basic")).ToHaveValueAsync("23:59");

        await Spin(callout, "Increase minute").ClickAsync();
        await Expect(Out("basic-value")).ToHaveTextAsync("23:00:00");
    }

    [TestMethod]
    public async Task Basic_Escape_ClosesTheCalloutAndHandsTheFocusBackToTheField()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("basic");
        await Expect(Hour(callout)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("basic")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Field("basic")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Basic_ClickOutside_ClosesTheCalloutThroughTheOverlay()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("basic");

        // The callout is as wide as the field, so the page under it is clicked below it.
        var box = (await BoxAsync(callout))!;
        await Page.Mouse.ClickAsync(10, box.Y + box.Height + 40);

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("basic")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Basic_NearTheBottomOfTheViewport_TheCalloutOpensAboveTheField()
    {
        await OpenAsync(Url);

        var viewport = Page.ViewportSize!;
        await Page.Locator("#underlined .bit-tpc-icn").EvaluateAsync(
            "(el, height) => window.scrollBy(0, el.getBoundingClientRect().bottom - height + 8)", viewport.Height);

        var callout = await OpenCalloutAsync("underlined");

        await SettleAsync(callout);
        var fieldBox = (await BoxAsync(Page.Locator("#underlined .bit-tpc-icn")))!;
        var calloutBox = (await BoxAsync(callout))!;
        Assert.IsTrue(calloutBox.Y + calloutBox.Height <= fieldBox.Y + 2,
            $"With no room below, the callout ({calloutBox.Y}+{calloutBox.Height}) drops above the field ({fieldBox.Y}).");
        Assert.IsTrue(calloutBox.Y >= 0, "The callout stays inside the viewport.");
    }

    [TestMethod]
    public async Task Placeholder_IsShownOnTheEmptyField()
    {
        await OpenAsync(Url);

        await Expect(Field("placeholder")).ToHaveAttributeAsync("placeholder", "Select a time...");
        await Expect(Field("placeholder")).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task Required_MarksTheLabelAndTheInput()
    {
        await OpenAsync(Url);

        await Expect(Root("required")).ToHaveClassAsync(HasClass("bit-tpc-req"));
        await Expect(Field("required")).ToHaveAttributeAsync("required", "");

        var asterisk = await Page.Locator("#required .bit-tpc-lbl").EvaluateAsync<string>("el => getComputedStyle(el, '::after').content");
        Assert.AreEqual("\"*\"", asterisk);

        var plain = await Page.Locator("#basic .bit-tpc-lbl").EvaluateAsync<string>("el => getComputedStyle(el, '::after').content");
        Assert.AreEqual("none", plain);
    }

    [TestMethod]
    public async Task ReadOnly_ShowsTheValueButRefusesEveryChange()
    {
        await OpenAsync(Url);

        await Expect(Field("readonly")).ToHaveValueAsync("10:24");
        await Expect(Field("readonly")).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(Root("readonly")).ToHaveClassAsync(HasClass("bit-tpc-rdl"));

        var callout = await OpenCalloutAsync("readonly");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-tpc-rdl"));
        await Expect(Hour(callout)).ToHaveValueAsync("10");
        await Expect(Hour(callout)).ToHaveAttributeAsync("readonly", "");
        await Expect(Spin(callout, "Increase hour")).ToBeDisabledAsync();
        await Expect(Spin(callout, "Increase hour")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Action(callout, "Now")).ToBeDisabledAsync();
        await Expect(Action(callout, "Clear")).ToBeDisabledAsync();

        await Hour(callout).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("End");
        await Spin(callout, "Increase minute").ClickAsync(new() { Force = true });

        // The read-only spin buttons keep the look of live ones: only a disabled picker greys them out.
        var readOnlyColor = await StyleOfAsync(Spin(callout, "Increase hour"), "color");
        var disabledColor = await StyleOfAsync(Page.Locator("#disabled-standalone button[aria-label='Increase hour']"), "color");
        Assert.AreNotEqual(disabledColor, readOnlyColor);

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Out("readonly-value")).ToHaveTextAsync("10:24:00");
        await Expect(Field("readonly")).ToHaveValueAsync("10:24");
    }

    [TestMethod]
    public async Task OneWayValue_WithoutAChangeHandler_IsNotInteractive()
    {
        await OpenAsync(Url);

        await Expect(Field("one-way")).ToHaveValueAsync("10:24");

        var callout = await OpenCalloutAsync("one-way");
        await Expect(Spin(callout, "Increase hour")).ToBeDisabledAsync();
        await Expect(Spin(callout, "Decrease minute")).ToBeDisabledAsync();

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Hour(callout)).ToHaveValueAsync("10");
    }

    [TestMethod]
    public async Task Disabled_RendersTheDisabledContract_AndRunsNoneOfThePageHandlers()
    {
        await OpenAsync(Url);

        var root = Root("disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Field("disabled")).ToBeDisabledAsync();
        await Expect(Field("disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Field("disabled")).ToHaveValueAsync("10:24");

        await Page.Locator("#disabled .bit-tpc-icn").ClickAsync(new() { Force = true });
        // A screen reader activates what it announces with a click of its own, which pointer-events never stop.
        await root.DispatchEventAsync("click");
        await root.DispatchEventAsync("keydown", new { key = "Enter" });

        await Expect(Field("disabled")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Out("disabled-click-count")).ToHaveTextAsync("0");
        await Expect(Out("disabled-key-count")).ToHaveTextAsync("0");

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Field("disabled")).ToBeEnabledAsync();
        await Expect(Field("disabled")).Not.ToHaveAttributeAsync("aria-disabled", "true");

        await root.DispatchEventAsync("click");
        await Expect(Out("disabled-click-count")).ToHaveTextAsync("1");

        var callout = await OpenCalloutAsync("disabled");
        await Expect(Out("disabled-click-count")).ToHaveTextAsync("2");
        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("disabled-value")).ToHaveTextAsync("11:24:00");
    }

    [TestMethod]
    public async Task Disabled_Standalone_DisablesEveryPart()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("disabled-standalone");
        await Expect(Root("disabled-standalone")).ToHaveClassAsync(HasClass("bit-dis"));

        foreach (var input in await callout.Locator(".bit-tpc-tin").AllAsync())
        {
            await Expect(input).ToBeDisabledAsync();
            await Expect(input).ToHaveAttributeAsync("aria-disabled", "true");
        }

        foreach (var button in await callout.Locator("button").AllAsync())
        {
            await Expect(button).ToBeDisabledAsync();
            await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        }

        await Expect(callout.Locator("button")).ToHaveCountAsync(8);
        var hidden = Page.Locator("#disabled-standalone input.bit-input-hidden");
        await Expect(hidden).ToBeDisabledAsync();
        await Expect(hidden).ToHaveAttributeAsync("aria-disabled", "true");
    }

    // ------------------------------------------------------------------ time format

    [TestMethod]
    public async Task TwentyFourHours_RunsTheHourFrom0To23WithoutAMeridiem()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("format-24");
        await Expect(Hour(callout)).ToHaveAttributeAsync("min", "0");
        await Expect(Hour(callout)).ToHaveAttributeAsync("max", "23");
        await Expect(callout.Locator(".bit-tpc-am-pm")).ToHaveCountAsync(0);
        await Expect(callout.Locator(".bit-tpc-tin")).ToHaveCountAsync(2);

        await Spin(callout, "Decrease hour").ClickAsync();
        await Expect(Hour(callout)).ToHaveValueAsync("23");
        await Expect(Out("format-24-value")).ToHaveTextAsync("23:00:00");
        await Expect(Field("format-24")).ToHaveValueAsync("23:00");
    }

    [TestMethod]
    public async Task TwelveHours_ShowsTheMeridiem_AndTheSpinButtonsCarryItAcrossNoon()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("format-12");
        await Expect(Hour(callout)).ToHaveAttributeAsync("min", "1");
        await Expect(Hour(callout)).ToHaveAttributeAsync("max", "12");
        await Expect(Am(callout)).ToHaveTextAsync("AM");
        await Expect(Pm(callout)).ToHaveTextAsync("PM");
        await Expect(Am(callout)).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Pm(callout)).ToHaveAttributeAsync("aria-pressed", "false");

        // An empty picker has no hour to carry, so the meridiem buttons leave it empty.
        await Pm(callout).ClickAsync();
        await Expect(Out("format-12-value")).ToHaveTextAsync("null");

        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("format-12-value")).ToHaveTextAsync("01:00:00");
        await Expect(Hour(callout)).ToHaveValueAsync("01");
        await Expect(Am(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Am(callout)).ToHaveClassAsync(HasClass("bit-tpc-bns"));
        await Expect(Field("format-12")).ToHaveValueAsync("01:00 AM");

        await Pm(callout).ClickAsync();
        await Expect(Out("format-12-value")).ToHaveTextAsync("13:00:00");
        await Expect(Hour(callout)).ToHaveValueAsync("01");
        await Expect(Pm(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Pm(callout)).ToHaveClassAsync(HasClass("bit-tpc-bns"));
        await Expect(Am(callout)).Not.ToHaveClassAsync(HasClass("bit-tpc-bns"));
        await Expect(Field("format-12")).ToHaveValueAsync("01:00 PM");

        await Spin(callout, "Decrease hour").ClickAsync();
        await Expect(Out("format-12-value")).ToHaveTextAsync("12:00:00");
        await Expect(Hour(callout)).ToHaveValueAsync("12");
        await Expect(Field("format-12")).ToHaveValueAsync("12:00 PM");

        await Spin(callout, "Decrease hour").ClickAsync();
        await Expect(Out("format-12-value")).ToHaveTextAsync("11:00:00");
        await Expect(Am(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Field("format-12")).ToHaveValueAsync("11:00 AM");

        await Am(callout).ClickAsync();
        await Expect(Out("format-12-value")).ToHaveTextAsync("11:00:00");
    }

    [TestMethod]
    public async Task ShowSeconds_AddsTheSecondsInputAndWritesThem()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("format-seconds");
        await Expect(callout.Locator(".bit-tpc-tin")).ToHaveCountAsync(3);
        await Expect(callout.Locator(".bit-tpc-tdv")).ToHaveCountAsync(2);
        await Expect(Second(callout)).ToHaveAttributeAsync("max", "59");

        await Spin(callout, "Increase second").ClickAsync();
        await Expect(Out("format-seconds-value")).ToHaveTextAsync("00:00:01");
        await Spin(callout, "Decrease second").ClickAsync();
        await Spin(callout, "Decrease second").ClickAsync();
        await Expect(Out("format-seconds-value")).ToHaveTextAsync("00:00:59");
        await Expect(Second(callout)).ToHaveValueAsync("59");
        await Expect(Field("format-seconds")).ToHaveValueAsync("00:00:59");
    }

    [TestMethod]
    public async Task ShowSeconds_InTwelveHours_WritesTheSecondsBeforeTheMeridiem()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("format-12-seconds");
        await Spin(callout, "Increase second").ClickAsync();

        await Expect(Out("format-12-seconds-value")).ToHaveTextAsync("00:00:01");
        await Expect(Hour(callout)).ToHaveValueAsync("12");
        await Expect(Field("format-12-seconds")).ToHaveValueAsync("12:00:01 AM");
    }

    // ------------------------------------------------------------------ text input

    [TestMethod]
    public async Task AllowTextInput_TakesATimeTypedInEitherClockFormat()
    {
        await OpenAsync(Url);

        var field = Field("text-input");
        await Expect(field).Not.ToHaveAttributeAsync("readonly", "");
        await Expect(field).ToHaveClassAsync(HasClass("bit-tpc-ein"));

        await field.FillAsync("2:05 PM");
        await Expect(Out("text-input-value")).ToHaveTextAsync("14:05:00");

        await field.FillAsync("09:30");
        await Expect(Out("text-input-value")).ToHaveTextAsync("09:30:00");

        await field.FillAsync("7:15:20");
        await Expect(Out("text-input-value")).ToHaveTextAsync("07:15:20");

        await field.FillAsync("");
        await Expect(Out("text-input-value")).ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task AllowTextInput_TextThatIsNoTime_IsKeptButSetsNoValue()
    {
        await OpenAsync(Url);

        var field = Field("text-input");
        await field.FillAsync("10:00");
        await Expect(Out("text-input-value")).ToHaveTextAsync("10:00:00");

        await field.FillAsync("not a time");
        await Expect(field).ToHaveValueAsync("not a time");
        await Expect(Out("text-input-value")).ToHaveTextAsync("10:00:00");
    }

    [TestMethod]
    public async Task AllowTextInput_WithSecondsInTwelveHours_RewritesATwentyFourHourTime()
    {
        await OpenAsync(Url);

        var field = Field("text-input-12s");
        await field.FillAsync("14:05:09");
        await Expect(Out("text-input-12s-value")).ToHaveTextAsync("14:05:09");

        await Page.Locator("#text-input-12s-value").ClickAsync();
        await Expect(field).ToHaveValueAsync("02:05:09 PM");

        await field.FillAsync("3:04 AM");
        await Expect(Out("text-input-12s-value")).ToHaveTextAsync("03:04:00");
    }

    [TestMethod]
    public async Task AllowTextInput_OnlyArrowDownOpens_AndTabFromTheFieldClosesAndMovesOn()
    {
        await OpenAsync(Url);

        var field = Field("keyboard-text");
        await field.FocusAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync(" ");
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Out("keyboard-text-opens")).ToHaveTextAsync("0");
        await Expect(field).ToHaveValueAsync(" ");

        await Page.Keyboard.PressAsync("ArrowDown");
        var callout = await CalloutOfAsync("keyboard-text");
        await Expect(Out("keyboard-text-opens")).ToHaveTextAsync("1");
        // The text the person came to type keeps the focus.
        await Expect(field).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Out("keyboard-text-closes")).ToHaveTextAsync("1");
        await Expect(field).Not.ToBeFocusedAsync();
    }

    // ------------------------------------------------------------------ icons

    [TestMethod]
    public async Task IconName_RendersTheGivenGlyph_AndTheClockByDefault()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#icon .bit-tpc-ico")).ToHaveClassAsync(HasClass("bit-icon--HourGlass"));
        await Expect(Page.Locator("#icon .bit-tpc-ico")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Page.Locator("#basic .bit-tpc-ico")).ToHaveClassAsync(HasClass("bit-icon--Clock"));
    }

    [TestMethod]
    public async Task IconPlacement_Start_PutsTheIconBeforeTheInput()
    {
        await OpenAsync(Url);

        await Expect(Root("icon-start")).ToHaveClassAsync(HasClass("bit-tpc-lic"));
        await Expect(Root("icon")).Not.ToHaveClassAsync(HasClass("bit-tpc-lic"));

        var startIcon = (await BoxAsync(Page.Locator("#icon-start .bit-tpc-ico")))!;
        var startInput = (await BoxAsync(Field("icon-start")))!;
        Assert.IsTrue(startIcon.X < startInput.X, "IconPlacement.Start draws the icon left of the input.");

        var endIcon = (await BoxAsync(Page.Locator("#icon .bit-tpc-ico")))!;
        var endInput = (await BoxAsync(Field("icon")))!;
        Assert.IsTrue(endIcon.X > endInput.X, "The default placement draws the icon right of the input.");
    }

    [TestMethod]
    public async Task SpinAndCloseIcons_AreReplacedByTheirIconNames()
    {
        await OpenAsync(Url);

        var defaults = await OpenCalloutAsync("format-seconds");
        await Expect(Spin(defaults, "Increase hour").Locator("i")).ToHaveClassAsync(new Regex(@"bit-icon--ChevronDownSmall.*bit-ico-r180"));
        await Expect(Spin(defaults, "Decrease hour").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--ChevronDownSmall"));
        await Page.Keyboard.PressAsync("Escape");

        var callout = await OpenCalloutAsync("icon-custom");
        await Expect(Spin(callout, "Increase hour").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--CaretSolidUp"));
        await Expect(Spin(callout, "Decrease hour").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--CaretSolidDown"));
        await Expect(Spin(callout, "Increase minute").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--CaretSolidUp"));
        await Expect(Spin(callout, "Decrease minute").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--CaretSolidDown"));
        await Expect(Spin(callout, "Increase second").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--CaretUpSolid8"));
        await Expect(Spin(callout, "Decrease second").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--CaretDownSolid8"));
        await Expect(callout.Locator(".bit-tpc-cbn i")).ToHaveClassAsync(HasClass("bit-icon--ChromeClose"));
        await Expect(callout.Locator(".bit-tpc-cbn")).ToHaveAttributeAsync("title", "Close time picker");
        await Expect(callout.Locator(".bit-tpc-cbn")).ToHaveAttributeAsync("aria-label", "Close time picker");
    }

    [TestMethod]
    public async Task ExternalIcons_RenderTheClassesOfTheirLibrary()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#ext-css .bit-tpc-ico")).ToHaveClassAsync(new Regex(@"(^|\s)fa-solid fa-hourglass-half(\s|$)"));
        await Expect(Page.Locator("#ext-fa .bit-tpc-ico")).ToHaveClassAsync(new Regex(@"(^|\s)fa-solid fa-stopwatch(\s|$)"));
        await Expect(Page.Locator("#ext-bi .bit-tpc-ico")).ToHaveClassAsync(new Regex(@"(^|\s)bi bi-stopwatch-fill(\s|$)"));
        await Expect(Page.Locator("#ext-string .bit-tpc-ico")).ToHaveClassAsync(new Regex(@"(^|\s)bi bi-clock-fill(\s|$)"));
        await Expect(Page.Locator("#ext-css .bit-tpc-ico")).Not.ToHaveClassAsync(HasClass("bit-icon--Clock"));

        var spin = StandaloneCallout("ext-spin");
        foreach (var (label, css) in new[] { ("Increase hour", "ext-ih"), ("Decrease hour", "ext-dh"), ("Increase minute", "ext-im"),
                                             ("Decrease minute", "ext-dm"), ("Increase second", "ext-is"), ("Decrease second", "ext-ds") })
        {
            await Expect(Spin(spin, label).Locator("i")).ToHaveClassAsync(HasClass(css));
        }

        // The icon wins over the icon name when both are given.
        await Expect(Page.Locator("#ext-buttons .bit-tpc-clr i")).ToHaveClassAsync(HasClass("ext-clear"));
        await Expect(Page.Locator("#ext-buttons .bit-tpc-clr i")).Not.ToHaveClassAsync(HasClass("bit-icon--Delete"));
        await Expect(Page.Locator("#ext-clear-name .bit-tpc-clr i")).ToHaveClassAsync(HasClass("bit-icon--Delete"));

        var callout = await OpenCalloutAsync("ext-buttons");
        await Expect(callout.Locator(".bit-tpc-cbn i")).ToHaveClassAsync(HasClass("ext-close"));
    }

    // ------------------------------------------------------------------ formatting & culture

    [TestMethod]
    public async Task ValueFormat_WritesAndReadsTheGivenPattern()
    {
        await OpenAsync(Url);

        var field = Field("value-format");
        await Expect(field).ToHaveValueAsync("02-05.09");

        await field.FillAsync("03-10.20");
        await Expect(Out("value-format-value")).ToHaveTextAsync("03:10:20");

        // The pattern the app set is the only one read: the culture's own is refused.
        await field.FillAsync("04:11");
        await Expect(Out("value-format-value")).ToHaveTextAsync("03:10:20");
    }

    [TestMethod]
    public async Task Culture_FaIr_WritesItsDesignatorsAndLaysTheCalloutOutRightToLeft()
    {
        await OpenAsync(Url);

        var root = Root("culture-fa");
        var callout = StandaloneCallout("culture-fa");
        var am = await Out("fa-am").TextContentAsync();
        var pm = await Out("fa-pm").TextContentAsync();

        await Expect(Am(callout)).ToHaveTextAsync(am!.Trim());
        await Expect(Pm(callout)).ToHaveTextAsync(pm!.Trim());
        await Expect(Pm(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Hour(callout)).ToHaveValueAsync("02");

        // A culture that writes right to left implies the direction of a picker given no Dir.
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(callout).ToHaveClassAsync(HasClass("bit-tpc-rtl"));
        Assert.AreEqual("rtl", await StyleOfAsync(callout, "direction"));
        // The parts of the time keep running left to right.
        Assert.AreEqual("ltr", await StyleOfAsync(callout.Locator(".bit-tpc-tic"), "direction"));
        var hour = (await BoxAsync(Hour(callout)))!;
        var minute = (await BoxAsync(Minute(callout)))!;
        Assert.IsTrue(hour.X < minute.X);
        var ampm = (await BoxAsync(callout.Locator(".bit-tpc-am-pm")))!;
        Assert.IsTrue(ampm.X < hour.X, "The meridiem section mirrors to the left of the time in a right-to-left culture.");

        // A press of the culture's own PM letter is the meridiem shortcut.
        // A letter off the US layout has no physical key Playwright could press, so its keydown is dispatched.
        await Hour(callout).FocusAsync();
        await Hour(callout).DispatchEventAsync("keydown", new { key = am.Trim()[..1], bubbles = true });
        await Expect(Out("culture-fa-value")).ToHaveTextAsync("02:30:00");
    }

    [TestMethod]
    public async Task Culture_JaJp_WritesTheFieldWithItsDesignators()
    {
        await OpenAsync(Url);

        var expected = (await Out("ja-expected").TextContentAsync())!.Trim();
        await Expect(Field("culture-ja")).ToHaveValueAsync(expected);

        var callout = await OpenCalloutAsync("culture-ja");
        await Expect(Am(callout)).ToHaveTextAsync((await Out("ja-am").TextContentAsync())!.Trim());
        await Expect(Pm(callout)).ToHaveTextAsync((await Out("ja-pm").TextContentAsync())!.Trim());
        await Expect(Pm(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Root("culture-ja")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
    }

    [TestMethod]
    public async Task Culture_DeDe_WritesItsTwentyFourHourPattern()
    {
        await OpenAsync(Url);

        await Expect(Field("culture-de")).ToHaveValueAsync("14:30");
    }

    // ------------------------------------------------------------------ binding

    [TestMethod]
    public async Task Binding_FlowsBothWays_AndWrapsSpansOutsideADay()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("binding");
        await Spin(callout, "Increase minute").ClickAsync();
        await Expect(Out("binding-value")).ToHaveTextAsync("00:01:00");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Field("binding")).ToHaveValueAsync("00:01");

        await Page.ClickAsync("#btn-set-bound");
        await Expect(Field("binding")).ToHaveValueAsync("07:45");

        await Page.ClickAsync("#btn-set-bound-overflow");
        await Expect(Field("binding")).ToHaveValueAsync("02:03");

        await Page.ClickAsync("#btn-set-bound-negative");
        await Expect(Field("binding")).ToHaveValueAsync("23:00");

        callout = await OpenCalloutAsync("binding");
        await Expect(Hour(callout)).ToHaveValueAsync("23");
        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("binding-value")).ToHaveTextAsync("00:00:00");
        await Page.Keyboard.PressAsync("Escape");

        await Page.ClickAsync("#btn-clear-bound");
        await Expect(Field("binding")).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task DefaultValue_StartsThePickerWithAValue()
    {
        await OpenAsync(Url);

        await Expect(Field("default-value")).ToHaveValueAsync("08:15");

        var callout = await OpenCalloutAsync("default-value");
        await Expect(Hour(callout)).ToHaveValueAsync("08");
        await Expect(Minute(callout)).ToHaveValueAsync("15");
        await Spin(callout, "Increase minute").ClickAsync();
        await Expect(Minute(callout)).ToHaveValueAsync("16");
        await Expect(Field("default-value")).ToHaveValueAsync("08:16");
    }

    [TestMethod]
    public async Task StartingValue_LeavesThePickerEmptyButSeedsTheFirstChange()
    {
        await OpenAsync(Url);

        await Expect(Field("starting-value")).ToHaveValueAsync("");

        var callout = await OpenCalloutAsync("starting-value");
        await Expect(Hour(callout)).ToHaveValueAsync("");
        await Expect(Out("starting-value-value")).ToHaveTextAsync("null");

        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("starting-value-value")).ToHaveTextAsync("10:30:00");
        await Expect(Field("starting-value")).ToHaveValueAsync("10:30");
    }

    // ------------------------------------------------------------------ standalone

    [TestMethod]
    public async Task Standalone_RendersTheInputsInlineAsAGroupNamedByItsLabel()
    {
        await OpenAsync(Url);

        var root = Root("standalone");
        var callout = StandaloneCallout("standalone");
        await Expect(root).ToHaveClassAsync(HasClass("bit-tpc-sta"));
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#standalone .bit-tpc-inp")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#standalone .bit-tpc-ovl")).ToHaveCountAsync(0);

        var group = callout.Locator(".bit-tpc-cac");
        var label = Page.Locator("#standalone .bit-tpc-lbl");
        await Expect(group).ToHaveAttributeAsync("role", "group");
        Assert.IsNull(await group.GetAttributeAsync("aria-modal"));
        Assert.IsNull(await group.GetAttributeAsync("aria-label"));
        await Expect(group).ToHaveAttributeAsync("aria-labelledby", (await label.GetAttributeAsync("id"))!);
        await Expect(label).ToHaveAttributeAsync("for", (await Hour(callout).GetAttributeAsync("id"))!);

        // Clicking the label puts the focus on the hour input, the part actually operated.
        await label.ClickAsync();
        await Expect(Hour(callout)).ToBeFocusedAsync();

        var hidden = Page.Locator("#standalone input.bit-input-hidden");
        await Expect(hidden).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(hidden).ToHaveAttributeAsync("name", "standalone-time");
        await Expect(hidden).ToHaveValueAsync("");

        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("standalone-value")).ToHaveTextAsync("01:00:00");
        await Expect(hidden).ToHaveValueAsync("01:00");
        await Expect(callout).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Standalone_WithoutALabel_IsNamedByTheCalloutAriaLabel()
    {
        await OpenAsync(Url);

        var group = StandaloneCallout("standalone-nolabel").Locator(".bit-tpc-cac");
        await Expect(group).ToHaveAttributeAsync("aria-label", "Pick a time");
        Assert.IsNull(await group.GetAttributeAsync("aria-labelledby"));
    }

    [TestMethod]
    public async Task Standalone_TwelveHours_SetsTheMeridiemInline()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("standalone-12");
        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("standalone-12-value")).ToHaveTextAsync("01:00:00");

        await Pm(callout).ClickAsync();
        await Expect(Out("standalone-12-value")).ToHaveTextAsync("13:00:00");
        await Expect(Page.Locator("#standalone-12 input.bit-input-hidden")).ToHaveValueAsync("01:00 PM");
    }

    [TestMethod]
    public async Task Standalone_ReadOnly_ShowsTheTimeButTakesNoChange()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("standalone-readonly");
        await Expect(Hour(callout)).ToHaveValueAsync("10");
        await Expect(Minute(callout)).ToHaveValueAsync("24");
        await Expect(Am(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Pm(callout)).ToBeDisabledAsync();
        await Expect(Root("standalone-readonly")).ToHaveClassAsync(HasClass("bit-tpc-rdl"));

        await Hour(callout).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("p");
        await Expect(Hour(callout)).ToHaveValueAsync("10");
        await Expect(Out("standalone-readonly-value")).ToHaveTextAsync("10:24:00");

        // The selected meridiem keeps the accent of a live one rather than the grey of a disabled picker.
        var selected = await StyleOfAsync(Am(callout), "background-color");
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-pri)"), selected);
    }

    // ------------------------------------------------------------------ selectable range

    [TestMethod]
    public async Task MinTimeMaxTime_TheSpinButtonsStayInsideTheRange()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("range-working");
        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("range-working-value")).ToHaveTextAsync("09:00:00");

        await Spin(callout, "Decrease hour").ClickAsync();
        await Expect(Out("range-working-value")).ToHaveTextAsync("17:00:00");

        // 17:00 is the last time: no minute after it can be reached.
        await Spin(callout, "Increase minute").ClickAsync();
        await Expect(Out("range-working-value")).ToHaveTextAsync("17:00:00");

        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("range-working-value")).ToHaveTextAsync("09:00:00");
    }

    [TestMethod]
    public async Task MinTimeMaxTime_BoundsInsideAnHour_PullTheMinutesIn()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("range-bounds-minutes");
        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("range-bounds-minutes-value")).ToHaveTextAsync("09:20:00");

        await Hour(callout).FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Out("range-bounds-minutes-value")).ToHaveTextAsync("17:20:00");

        await Minute(callout).FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Out("range-bounds-minutes-value")).ToHaveTextAsync("17:40:00");

        await Hour(callout).FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Out("range-bounds-minutes-value")).ToHaveTextAsync("09:40:00");

        await Minute(callout).FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Out("range-bounds-minutes-value")).ToHaveTextAsync("09:20:00");
    }

    [TestMethod]
    public async Task AllowedHoursAndMinutes_TheControlsSkipWhatTheRulesRefuse()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("range-allowed");
        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("range-allowed-value")).ToHaveTextAsync("02:00:00");

        await Spin(callout, "Increase minute").ClickAsync();
        await Expect(Out("range-allowed-value")).ToHaveTextAsync("02:15:00");

        await Spin(callout, "Decrease hour").ClickAsync();
        await Spin(callout, "Decrease hour").ClickAsync();
        await Expect(Out("range-allowed-value")).ToHaveTextAsync("22:15:00");

        // A typed hour snaps to the nearest one the rule accepts.
        await Hour(callout).FillAsync("7");
        await Expect(Out("range-allowed-value")).ToHaveTextAsync(new Regex("^(06|08):15:00$"));
    }

    [TestMethod]
    public async Task AllowedSeconds_TheSecondsSkipWhatTheRuleRefuses()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("range-seconds");
        await Spin(callout, "Increase second").ClickAsync();
        await Expect(Out("range-seconds-value")).ToHaveTextAsync("00:00:10");
        await Spin(callout, "Decrease second").ClickAsync();
        await Spin(callout, "Decrease second").ClickAsync();
        await Expect(Out("range-seconds-value")).ToHaveTextAsync("00:00:50");
    }

    [TestMethod]
    public async Task DisablePast_NothingBeforeTheCurrentTimeCanBePicked()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("range-past");
        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("range-past-value")).ToHaveTextAsync("13:47:00");

        await Spin(callout, "Decrease minute").ClickAsync();
        await Expect(Out("range-past-value")).ToHaveTextAsync("13:59:00");

        await Action(callout, "Now").ClickAsync();
        await Expect(Out("range-past-value")).ToHaveTextAsync("13:47:00");

        await Spin(callout, "Decrease hour").ClickAsync();
        await Expect(Out("range-past-value")).ToHaveTextAsync("23:47:00");
    }

    [TestMethod]
    public async Task DisableFuture_NothingAfterTheCurrentTimeCanBePicked()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("range-future");
        await Spin(callout, "Decrease hour").ClickAsync();
        await Expect(Out("range-future-value")).ToHaveTextAsync("13:00:00");

        await Action(callout, "Now").ClickAsync();
        await Expect(Out("range-future-value")).ToHaveTextAsync("13:47:00");

        await Spin(callout, "Increase minute").ClickAsync();
        await Expect(Out("range-future-value")).ToHaveTextAsync("13:00:00");

        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("range-future-value")).ToHaveTextAsync("00:00:00");
    }

    [TestMethod]
    public async Task TimeZone_TheNowButtonReadsTheClockInTheGivenZone()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("range-tz");
        var before = DateTime.UtcNow;
        await Action(callout, "Now").ClickAsync();
        await Expect(Out("range-tz-value")).Not.ToHaveTextAsync("null");
        var after = DateTime.UtcNow;

        var shown = TimeSpan.Parse((await Out("range-tz-value").TextContentAsync())!, System.Globalization.CultureInfo.InvariantCulture);
        var low = new TimeSpan(before.Hour, before.Minute, 0);
        var high = new TimeSpan(after.Hour, after.Minute, 0);
        Assert.IsTrue(shown == low || shown == high || (low > high && (shown >= low || shown <= high)),
            $"The now button read {shown}, expected the UTC time between {low} and {high}.");
    }

    [TestMethod]
    public async Task TypedTime_OutsideTheRulesSetsNoValue()
    {
        await OpenAsync(Url);

        var field = Field("range-text");
        await field.FillAsync("08:00");
        await Expect(Out("range-text-value")).ToHaveTextAsync("null");
        await Expect(field).ToHaveValueAsync("08:00");

        await field.FillAsync("09:15");
        await Expect(Out("range-text-value")).ToHaveTextAsync("09:15:00");

        // A typed time is held to the rules but not to the steps: an off-rule minute is refused.
        await field.FillAsync("09:10");
        await Expect(Out("range-text-value")).ToHaveTextAsync("09:15:00");
        await Expect(field).ToHaveValueAsync("09:10");
    }

    // ------------------------------------------------------------------ steps

    [TestMethod]
    public async Task HourStep_MovesTheHourOnItsGrid_AndPageUpMakesFiveSteps()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("steps-hour");
        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("steps-hour-value")).ToHaveTextAsync("02:00:00");
        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("steps-hour-value")).ToHaveTextAsync("04:00:00");

        await Hour(callout).FocusAsync();
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(Out("steps-hour-value")).ToHaveTextAsync("14:00:00");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Out("steps-hour-value")).ToHaveTextAsync("12:00:00");
    }

    [TestMethod]
    public async Task MinuteStep_MovesTheMinuteOnItsGrid()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("steps-minute");
        await Spin(callout, "Increase minute").ClickAsync();
        await Expect(Out("steps-minute-value")).ToHaveTextAsync("00:15:00");

        await Minute(callout).FocusAsync();
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(Out("steps-minute-value")).ToHaveTextAsync("00:30:00");
        await Page.Keyboard.PressAsync("End");
        await Expect(Out("steps-minute-value")).ToHaveTextAsync("00:45:00");
    }

    [TestMethod]
    public async Task SecondStep_MovesTheSecondOnItsGrid()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("steps-second");
        await Spin(callout, "Increase second").ClickAsync();
        await Expect(Out("steps-second-value")).ToHaveTextAsync("00:00:30");
        await Spin(callout, "Increase second").ClickAsync();
        await Expect(Out("steps-second-value")).ToHaveTextAsync("00:00:00");
    }

    [TestMethod]
    public async Task Steps_TheGridIsAnchoredAtMinTime()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("steps-anchor");
        await Spin(callout, "Increase minute").ClickAsync();
        await Expect(Out("steps-anchor-value")).ToHaveTextAsync("09:07:00");
        await Spin(callout, "Increase minute").ClickAsync();
        await Expect(Out("steps-anchor-value")).ToHaveTextAsync("09:22:00");
        await Spin(callout, "Increase minute").ClickAsync();
        await Expect(Out("steps-anchor-value")).ToHaveTextAsync("09:37:00");
    }

    // ------------------------------------------------------------------ action buttons

    [TestMethod]
    public async Task NowButton_WithAutoClose_PicksNowAndClosesTheCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("actions");
        await Action(callout, "Now").ClickAsync();

        await Expect(Out("actions-value")).ToHaveTextAsync("13:47:00");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("actions")).ToHaveValueAsync("13:47");
        await Expect(Field("actions")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task NowButton_WithoutAutoClose_KeepsTheCalloutOpen()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("actions-open");
        await Action(callout, "Now").ClickAsync();

        await Expect(Out("actions-open-value")).ToHaveTextAsync("08:05:33");
        await Expect(Second(callout)).ToHaveValueAsync("33");
        await Expect(callout).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ClearButton_ClearsTheValueAndRaisesOnClear()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("actions");
        await Expect(Action(callout, "Clear")).ToBeDisabledAsync();
        await Expect(Action(callout, "Clear")).ToHaveAttributeAsync("aria-disabled", "true");

        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("actions-value")).ToHaveTextAsync("01:00:00");
        await Expect(Action(callout, "Clear")).ToBeEnabledAsync();

        await Action(callout, "Clear").ClickAsync();
        await Expect(Out("actions-value")).ToHaveTextAsync("null");
        await Expect(Out("actions-clear-count")).ToHaveTextAsync("1");
        await Expect(Hour(callout)).ToHaveValueAsync("");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Action(callout, "Clear")).ToBeDisabledAsync();
        await Expect(Hour(callout)).ToBeFocusedAsync();

        // The focus is still in the dialog, so Escape still closes it.
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task InputClearButton_ClearsTheValueWithoutOpeningTheCallout()
    {
        await OpenAsync(Url);

        var clear = Page.Locator("#actions-field-clear .bit-tpc-clr");
        await Expect(clear).ToHaveAttributeAsync("title", "Remove the time");
        await Expect(clear).ToHaveAttributeAsync("aria-label", "Remove the time");
        await Expect(Field("actions-field-clear")).ToHaveValueAsync("14:30");

        await clear.ClickAsync();

        await Expect(Out("actions-field-clear-value")).ToHaveTextAsync("null");
        await Expect(Out("actions-field-clear-count")).ToHaveTextAsync("1");
        await Expect(Field("actions-field-clear")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Field("actions-field-clear")).ToBeFocusedAsync();
        await Expect(clear).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task CloseButton_ClosesTheCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("actions-field-clear");
        var close = callout.Locator(".bit-tpc-cbn");
        await Expect(close).ToHaveAttributeAsync("title", "Dismiss the picker");

        await close.ClickAsync();
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("actions-field-clear")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task ActionButtons_TakeTheirCustomTexts()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("actions-texts");
        await Expect(callout.Locator(".bit-tpc-abn")).ToHaveTextAsync(["Current time", "Reset"]);

        await Action(callout, "Current time").ClickAsync();
        await Expect(Out("actions-texts-value")).ToHaveTextAsync("06:07:00");

        await Action(callout, "Reset").ClickAsync();
        await Expect(Out("actions-texts-value")).ToHaveTextAsync("null");
        // A standalone picker has no close button even when its callout would.
        await Expect(callout.Locator(".bit-tpc-cbn")).ToHaveCountAsync(0);
    }

    // ------------------------------------------------------------------ validation & helper text

    [TestMethod]
    public async Task Description_IsRenderedAndDescribesTheInput()
    {
        await OpenAsync(Url);

        var description = Page.Locator("#helper .bit-tpc-des");
        await Expect(description).ToHaveTextAsync("When the meeting starts, in your local time.");
        var ariaDescription = Page.Locator("#helper span.bit-tpc-vhd");
        await Expect(ariaDescription).ToHaveTextAsync("Screen reader help.");
        Assert.AreEqual("1px", await StyleOfAsync(ariaDescription, "width"));

        var describedBy = (await Field("helper").GetAttributeAsync("aria-describedby"))!.Split(' ');
        CollectionAssert.AreEqual(new[] { await description.GetAttributeAsync("id"), await ariaDescription.GetAttributeAsync("id") }, describedBy);
    }

    [TestMethod]
    public async Task ErrorMessage_MarksThePickerInvalidAndShowsTheMessage()
    {
        await OpenAsync(Url);

        var root = Root("error");
        var error = Page.Locator("#error .bit-tpc-erm");
        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Field("error")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(error).ToHaveTextAsync("That slot was taken while you were choosing.");
        await Expect(Field("error")).ToHaveAttributeAsync("aria-describedby", (await error.GetAttributeAsync("id"))!);
        await Expect(Page.Locator("#error [role=status]")).ToHaveTextAsync("That slot was taken while you were choosing.");

        var invalidBorder = await StyleOfAsync(Page.Locator("#error .bit-tpc-icn"), "border-top-color");
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-err)"), invalidBorder);
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-err)"), await StyleOfAsync(error, "color"));
    }

    [TestMethod]
    public async Task Invalid_MarksThePickerInvalidWithoutAMessage()
    {
        await OpenAsync(Url);

        await Expect(Root("invalid")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Field("invalid")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#invalid .bit-tpc-erm")).ToHaveCountAsync(0);
        await Expect(Field("basic")).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }

    [TestMethod]
    public async Task EditForm_RequiredAndTypedRejections_ShowTheirCustomMessages()
    {
        await OpenAsync(Url);

        var field = Field("form");
        var message = Page.Locator("#form-message");

        await Page.ClickAsync("#form-submit");
        await WaitForStatusAsync("Rejected 1");
        await Expect(message).ToHaveTextAsync("The time is required.");
        await Expect(Root("form")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(field).ToHaveAttributeAsync("aria-invalid", "true");

        // The model still holds no time while the typed text is refused, so its Required message stays beside the
        // message of the rejection.
        await field.FillAsync("xx");
        await Expect(message).ToContainTextAsync("Invalid Time!");

        await field.FillAsync("07:00");
        await Expect(message).ToContainTextAsync("The time must be between 08:00 and 18:00!");
        await Expect(message).Not.ToContainTextAsync("Invalid Time!");

        await field.FillAsync("09:10");
        await Expect(message).ToContainTextAsync("Only quarter hours can be booked!");
        await Expect(message).Not.ToContainTextAsync("between");

        await field.FillAsync("09:15");
        await Expect(message).ToHaveTextAsync("");
        await Expect(Out("form-value")).ToHaveTextAsync("09:15:00");
        await Expect(Root("form")).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await Page.ClickAsync("#form-submit");
        await WaitForStatusAsync("Saved 2");
    }

    [TestMethod]
    public async Task EditForm_DefaultMessages_NameTheFieldByItsDisplayName()
    {
        await OpenAsync(Url);

        var field = Field("form-default-messages");
        var message = Page.Locator("#form2-message");

        await field.FillAsync("zz");
        await Expect(message).ToContainTextAsync("The Arrival field is not valid.");

        await field.FillAsync("07:00");
        await Expect(message).ToContainTextAsync("The Arrival field is out of the allowed range.");
        await Expect(message).Not.ToContainTextAsync("not valid");

        await field.FillAsync("12:15");
        await Expect(message).ToContainTextAsync("The Arrival field is not an allowed time.");
        await Expect(message).Not.ToContainTextAsync("range");

        await field.FillAsync("13:15");
        await Expect(message).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task EditForm_Standalone_ValidatesThroughTheHourInput()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("form-standalone");

        await Page.ClickAsync("#form3-submit");
        await Expect(Out("form3-status")).ToHaveTextAsync(new Regex("^Rejected"));
        await Expect(Page.Locator("#form3-message")).ToHaveTextAsync("The time is required.");
        await Expect(Hour(callout)).ToHaveAttributeAsync("aria-invalid", "true");

        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Page.Locator("#form3-message")).ToHaveTextAsync("");
        await Expect(Hour(callout)).Not.ToHaveAttributeAsync("aria-invalid", "true");

        await Page.ClickAsync("#form3-submit");
        await Expect(Out("form3-status")).ToHaveTextAsync(new Regex("^Saved"));
    }

    [TestMethod]
    public async Task NoValidate_TextThatIsNoTime_RaisesNoValidationMessage()
    {
        await OpenAsync(Url);

        var field = Field("form-novalidate");
        await field.FillAsync("10:15");
        await Expect(Out("form4-value")).ToHaveTextAsync("10:15:00");

        await field.FillAsync("zz");
        await Expect(field).ToHaveValueAsync("zz");
        await Expect(Page.Locator("#form4-message")).ToHaveTextAsync("");
        await Expect(Root("form-novalidate")).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Out("form4-value")).ToHaveTextAsync("10:15:00");

        // The same text in a picker that validates is refused with a message.
        await Field("form-default-messages").FillAsync("zz");
        await Expect(Root("form-default-messages")).ToHaveClassAsync(HasClass("bit-inv"));
    }

    // ------------------------------------------------------------------ appearance

    [TestMethod]
    public async Task PrefixAndSuffix_SitAtTheEndsOfTheFieldAndDescribeIt()
    {
        await OpenAsync(Url);

        var prefix = Page.Locator("#prefix-suffix .bit-tpc-pre");
        var suffix = Page.Locator("#prefix-suffix .bit-tpc-suf");
        await Expect(prefix).ToHaveTextAsync("Starts at");
        await Expect(suffix).ToHaveTextAsync("UTC");

        var input = (await BoxAsync(Field("prefix-suffix")))!;
        Assert.IsTrue((await BoxAsync(prefix))!.X < input.X);
        Assert.IsTrue((await BoxAsync(suffix))!.X > (await BoxAsync(Page.Locator("#prefix-suffix .bit-tpc-ico")))!.X);

        var describedBy = await Field("prefix-suffix").GetAttributeAsync("aria-describedby");
        Assert.AreEqual($"{await prefix.GetAttributeAsync("id")} {await suffix.GetAttributeAsync("id")}", describedBy);
    }

    [TestMethod]
    public async Task Underlined_DrawsOnlyARuleUnderTheField()
    {
        await OpenAsync(Url);

        await Expect(Root("underlined")).ToHaveClassAsync(HasClass("bit-tpc-und"));
        Assert.AreEqual("none", await StyleOfAsync(Page.Locator("#underlined .bit-tpc-icn"), "border-top-style"));
        Assert.AreEqual("solid", await StyleOfAsync(Page.Locator("#underlined .bit-tpc-wrp"), "border-bottom-style"));
        Assert.AreEqual("solid", await StyleOfAsync(Page.Locator("#basic .bit-tpc-icn"), "border-top-style"));
    }

    [TestMethod]
    public async Task HasBorderFalse_DrawsNoBorder()
    {
        await OpenAsync(Url);

        await Expect(Root("no-border")).ToHaveClassAsync(HasClass("bit-tpc-nbd"));
        Assert.AreEqual("none", await StyleOfAsync(Page.Locator("#no-border .bit-tpc-icn"), "border-top-style"));
        Assert.AreEqual("none", await StyleOfAsync(Page.Locator("#no-border .bit-tpc-icn"), "border-bottom-style"));
    }

    [TestMethod]
    public async Task DropDirectionAll_OpensTheCalloutInsideTheViewport()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("drop-all");
        await SettleAsync(callout);
        var box = (await BoxAsync(callout))!;
        var viewport = Page.ViewportSize!;
        Assert.IsTrue(box.X >= 0 && box.Y >= 0 && box.X + box.Width <= viewport.Width && box.Y + box.Height <= viewport.Height,
            "The callout is placed inside the viewport.");
    }

    [TestMethod]
    public async Task Responsive_OnADesktop_OpensAsAnOrdinaryCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("responsive");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-tpc-res"));
        await SettleAsync(callout);

        var box = (await BoxAsync(callout))!;
        var field = (await BoxAsync(Page.Locator("#responsive .bit-tpc-icn")))!;
        Assert.IsTrue(box.Y >= field.Y + field.Height - 2, "Above the phone breakpoint the responsive callout opens against its field, not as a sheet.");
        Assert.AreEqual(field.X, box.X, 2);
    }

    // ------------------------------------------------------------------ keyboard & accessibility

    [TestMethod]
    public async Task Keyboard_EnterSpaceAndArrowDownOpen_EscapeAndAltUpClose()
    {
        await OpenAsync(Url);

        var field = Field("keyboard");
        await field.FocusAsync();

        await Page.Keyboard.PressAsync("Enter");
        var callout = await CalloutOfAsync("keyboard");
        await Expect(Hour(callout)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(field).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync(" ");
        await CalloutOfAsync("keyboard");
        await Expect(Hour(callout)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(field).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await CalloutOfAsync("keyboard");
        await Expect(Hour(callout)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        // Alt+Up on the field itself is the close of a closed picker: nothing happens.
        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Keyboard_ArrowsPageHomeAndEnd_MoveEachTimeInput_EnterAcceptsAndCloses()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("keyboard");
        await Expect(Hour(callout)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("keyboard-value")).ToHaveTextAsync("01:00:00");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Out("keyboard-value")).ToHaveTextAsync("23:00:00");
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(Out("keyboard-value")).ToHaveTextAsync("04:00:00");
        await Page.Keyboard.PressAsync("Home");
        await Expect(Out("keyboard-value")).ToHaveTextAsync("00:00:00");
        await Page.Keyboard.PressAsync("End");
        await Expect(Out("keyboard-value")).ToHaveTextAsync("23:00:00");

        // Tab skips the spin buttons: the next stop is the minute input.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Minute(callout)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("keyboard-value")).ToHaveTextAsync("23:01:00");
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Out("keyboard-value")).ToHaveTextAsync("23:56:00");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Second(callout)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Out("keyboard-value")).ToHaveTextAsync("23:56:59");
        await Page.Keyboard.PressAsync("Home");
        await Expect(Out("keyboard-value")).ToHaveTextAsync("23:56:00");

        // The arrow keys move the value, not the page.
        var scroll = await Page.EvaluateAsync<double>("() => window.scrollY");
        await Page.Keyboard.PressAsync("ArrowDown");
        Assert.AreEqual(scroll, await Page.EvaluateAsync<double>("() => window.scrollY"));

        await Page.Keyboard.PressAsync("Enter");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("keyboard")).ToBeFocusedAsync();
        await Expect(Field("keyboard")).ToHaveValueAsync("23:56:59");
    }

    [TestMethod]
    public async Task Keyboard_TheMeridiemLettersSwitchTheHalfOfTheDay()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("keyboard-12");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("keyboard-12-value")).ToHaveTextAsync("01:00:00");

        await Page.Keyboard.PressAsync("p");
        await Expect(Out("keyboard-12-value")).ToHaveTextAsync("13:00:00");
        await Expect(Pm(callout)).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.Keyboard.PressAsync("A");
        await Expect(Out("keyboard-12-value")).ToHaveTextAsync("01:00:00");

        // A modified letter is a shortcut of the browser, not a designator.
        await Page.Keyboard.PressAsync("Control+p");
        await Expect(Out("keyboard-12-value")).ToHaveTextAsync("01:00:00");

        // Home/End stay in the half of the day; ArrowUp from 11 AM carries over noon.
        await Page.Keyboard.PressAsync("End");
        await Expect(Out("keyboard-12-value")).ToHaveTextAsync("11:00:00");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("keyboard-12-value")).ToHaveTextAsync("12:00:00");
        await Expect(Hour(callout)).ToHaveValueAsync("12");
        await Expect(Pm(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Page.Keyboard.PressAsync("Home");
        await Expect(Out("keyboard-12-value")).ToHaveTextAsync("12:00:00");

        // A typed hour stays in the half of the day the picker is on.
        await Hour(callout).FillAsync("5");
        await Expect(Out("keyboard-12-value")).ToHaveTextAsync("17:00:00");
    }

    [TestMethod]
    public async Task Keyboard_TabIsHeldInsideTheOpenCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("keyboard-12");
        await Expect(Hour(callout)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Minute(callout)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Am(callout)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Pm(callout)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Hour(callout)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Pm(callout)).ToBeFocusedAsync();
        await Expect(callout).ToBeVisibleAsync();

        // A time input left behind at once never pulls the focus back: its text is selected as the focus lands rather
        // than a round trip later, when select() would take the focus back onto it. A round trip is waited out here.
        await Page.WaitForTimeoutAsync(300);
        await Expect(Pm(callout)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Page.WaitForTimeoutAsync(300);
        await Expect(Hour(callout)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AutoAdvance_TypesAWholeTimeAsOneRunOfDigits()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("autoadvance");
        await Hour(callout).ClickAsync();
        await Expect(Out("autoadvance-focus")).ToHaveTextAsync("1");

        await Page.Keyboard.TypeAsync("1");
        await Expect(Hour(callout)).ToHaveValueAsync("01");
        await Expect(Hour(callout)).ToBeFocusedAsync();

        await Page.Keyboard.TypeAsync("4");
        await Expect(Out("autoadvance-value")).ToHaveTextAsync("14:00:00");
        await Expect(Minute(callout)).ToBeFocusedAsync();
        await Expect(Out("autoadvance-focus")).ToHaveTextAsync("2");

        await Page.Keyboard.TypeAsync("3");
        await Expect(Minute(callout)).ToHaveValueAsync("03");
        await Page.Keyboard.TypeAsync("0");
        await Expect(Out("autoadvance-value")).ToHaveTextAsync("14:30:00");
        await Expect(Second(callout)).ToBeFocusedAsync();
        await Expect(Out("autoadvance-focus")).ToHaveTextAsync("3");

        await Page.Keyboard.TypeAsync("4");
        await Expect(Second(callout)).ToHaveValueAsync("04");
        await Page.Keyboard.TypeAsync("5");
        await Expect(Out("autoadvance-value")).ToHaveTextAsync("14:30:45");
        // The last input keeps the focus.
        await Expect(Second(callout)).ToBeFocusedAsync();

        // A first digit no second one could extend completes the part at once.
        await Hour(callout).ClickAsync();
        await Expect(Out("autoadvance-focus")).ToHaveTextAsync("4");
        await Page.Keyboard.TypeAsync("7");
        await Expect(Out("autoadvance-value")).ToHaveTextAsync("07:30:45");
        await Expect(Minute(callout)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task WithoutAutoAdvance_TypingKeepsTheFocusOnTheInput()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("no-autoadvance");
        await Hour(callout).ClickAsync();
        await Expect(Out("no-autoadvance-focus")).ToHaveTextAsync("1");

        await Page.Keyboard.TypeAsync("1");
        await Expect(Hour(callout)).ToHaveValueAsync("01");
        await Page.Keyboard.TypeAsync("4");
        await Expect(Out("no-autoadvance-value")).ToHaveTextAsync("14:00:00");
        await Expect(Hour(callout)).ToBeFocusedAsync();

        // A typed hour past the clock is clamped to its end.
        await Hour(callout).FillAsync("99");
        await Expect(Out("no-autoadvance-value")).ToHaveTextAsync("23:00:00");
    }

    [TestMethod]
    public async Task Wheel_ShiftWheelOverTheFocusedInputStepsIt_AndNothingElseDoes()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("wheel");
        await Expect(Hour(callout)).ToHaveAttributeAsync("data-bit-wheel", "1");

        await Hour(callout).ClickAsync();
        await Hour(callout).HoverAsync();

        await Page.Keyboard.DownAsync("Shift");
        await Page.Mouse.WheelAsync(0, -100);
        await Expect(Out("wheel-value")).ToHaveTextAsync("11:30:00");
        await Page.Mouse.WheelAsync(0, 100);
        await Expect(Out("wheel-value")).ToHaveTextAsync("10:30:00");

        // A merely hovered input never reacts.
        await Minute(callout).HoverAsync();
        await Page.Mouse.WheelAsync(0, -100);
        await Page.Keyboard.UpAsync("Shift");

        // The arrow key of the focused hour is the next change: the wheel over the minute made none.
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("wheel-value")).ToHaveTextAsync("11:30:00");
        await Expect(Minute(callout)).ToHaveValueAsync("30");
    }

    [TestMethod]
    public async Task Wheel_WithoutShift_LeavesTheFocusedInputAlone()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("wheel");
        await Hour(callout).ClickAsync();
        await Hour(callout).HoverAsync();
        await Page.Mouse.WheelAsync(0, -100);

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Out("wheel-value")).ToHaveTextAsync("09:30:00");
    }

    [TestMethod]
    public async Task NoMouseWheel_TheTimeInputsAreNotMarkedForTheWheel()
    {
        await OpenAsync(Url);

        foreach (var input in await StandaloneCallout("wheel-off").Locator(".bit-tpc-tin").AllAsync())
        {
            Assert.IsNull(await input.GetAttributeAsync("data-bit-wheel"));
        }

        foreach (var input in await StandaloneCallout("wheel").Locator(".bit-tpc-tin").AllAsync())
        {
            await Expect(input).ToHaveAttributeAsync("data-bit-wheel", "1");
        }

        // A read-only picker takes no wheel either.
        foreach (var input in await StandaloneCallout("standalone-readonly").Locator(".bit-tpc-tin").AllAsync())
        {
            Assert.IsNull(await input.GetAttributeAsync("data-bit-wheel"));
        }
    }

    [TestMethod]
    public async Task NoMouseWheel_TheWheelNeverMovesTheInputs()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("wheel-off");
        Assert.IsNull(await Hour(callout).GetAttributeAsync("data-bit-wheel"));

        await Hour(callout).ClickAsync();
        await Page.Keyboard.DownAsync("Shift");
        await Page.Mouse.WheelAsync(0, -100);
        await Page.Keyboard.UpAsync("Shift");

        // The arrow key still steps, which proves the wheel's press reached the picker and was ignored.
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("wheel-off-value")).ToHaveTextAsync("11:30:00");
    }

    [TestMethod]
    public async Task InvertMouseWheel_ReversesTheWheel()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("wheel-invert");
        await Hour(callout).ClickAsync();
        await Hour(callout).HoverAsync();
        await Page.Keyboard.DownAsync("Shift");
        await Page.Mouse.WheelAsync(0, -100);
        await Page.Keyboard.UpAsync("Shift");

        await Expect(Out("wheel-invert-value")).ToHaveTextAsync("09:30:00");
    }

    [TestMethod]
    public async Task Aria_TheFieldTheCalloutAndEveryPartAreNamed()
    {
        await OpenAsync(Url);

        var field = Field("aria");
        var label = Page.Locator("#aria .bit-tpc-lbl");
        await Expect(field).ToHaveAttributeAsync("aria-label", "Meeting time field");
        await Expect(field).ToHaveAttributeAsync("aria-labelledby", (await label.GetAttributeAsync("id"))!);
        await Expect(label).ToHaveAttributeAsync("for", (await field.GetAttributeAsync("id"))!);

        var callout = await OpenCalloutAsync("aria");
        await Expect(callout.Locator(".bit-tpc-cac")).ToHaveAttributeAsync("aria-label", "Pick a meeting time");
        await Expect(Hour(callout)).ToHaveAttributeAsync("aria-label", "Meeting hour");
        await Expect(Minute(callout)).ToHaveAttributeAsync("aria-label", "Meeting minute");
        await Expect(Second(callout)).ToHaveAttributeAsync("aria-label", "Meeting second");
        await Expect(Hour(callout)).ToHaveAttributeAsync("inputmode", "numeric");

        foreach (var title in new[] { "One hour later", "One hour earlier", "One minute later", "One minute earlier", "One second later", "One second earlier" })
        {
            var button = callout.Locator($"button[title='{title}']");
            await Expect(button).ToHaveAttributeAsync("aria-label", title);
            // The spin buttons are out of the tab order: the input beside each pair is the spin button.
            await Expect(button).ToHaveAttributeAsync("tabindex", "-1");
        }

        // Every change is announced as a whole time through the live region.
        await Page.Keyboard.PressAsync("ArrowUp");
        var status = Page.Locator("#aria [role=status]");
        await Expect(status).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(status).ToHaveTextAsync("01:00:00 AM");
        await Expect(Am(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Pm(callout)).ToHaveAttributeAsync("aria-pressed", "false");
        Assert.IsNull(await Am(callout).GetAttributeAsync("title"));
    }

    // ------------------------------------------------------------------ events

    [TestMethod]
    public async Task Events_FireOnceEachWithTheRightArguments()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("events");
        await Expect(Out("ev-click")).ToHaveTextAsync("1");
        await Expect(Out("ev-open")).ToHaveTextAsync("1");
        await Expect(Out("ev-focus")).ToHaveTextAsync("1");
        await Expect(Out("ev-focusin")).ToHaveTextAsync("1");
        // The focus moved on into the callout's hour input.
        await Expect(Out("ev-focusout")).ToHaveTextAsync("1");

        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Out("ev-select")).ToHaveTextAsync("1");
        await Expect(Out("ev-select-arg")).ToHaveTextAsync("01:00:00");
        await Expect(Out("ev-change")).ToHaveTextAsync("1");
        await Expect(Out("ev-change-arg")).ToHaveTextAsync("01:00:00");

        await Action(callout, "Clear").ClickAsync();
        await Expect(Out("ev-clear")).ToHaveTextAsync("1");
        // The clear button disables itself on the empty value, so the focus is handed to the hour input rather
        // than dropped out of the dialog.
        await Expect(Hour(callout)).ToBeFocusedAsync();
        await Expect(Out("ev-select")).ToHaveTextAsync("2");
        await Expect(Out("ev-select-arg")).ToHaveTextAsync("null");
        await Expect(Out("ev-change")).ToHaveTextAsync("2");
        await Expect(Out("ev-change-arg")).ToHaveTextAsync("null");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Out("ev-close")).ToHaveTextAsync("1");
        await Expect(Out("ev-focus")).ToHaveTextAsync("2");
        await Expect(Out("ev-focusin")).ToHaveTextAsync("2");

        // A close of a closed picker is silent.
        await Page.Keyboard.PressAsync("Escape");
        await Page.ClickAsync("#events-outside");
        await Expect(Out("ev-focusout")).ToHaveTextAsync("2");
        await Expect(Out("ev-close")).ToHaveTextAsync("1");
        await Expect(Out("ev-open")).ToHaveTextAsync("1");
        await Expect(Out("ev-click")).ToHaveTextAsync("1");
    }

    // ------------------------------------------------------------------ programmatic control

    [TestMethod]
    public async Task IsOpen_BindsBothWays_AndOpenCalloutDismissCalloutDriveIt()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-open");
        var callout = await CalloutOfAsync("programmatic");
        await Expect(Out("is-open")).ToHaveTextAsync("True");
        await Expect(Out("prog-opens")).ToHaveTextAsync("1");
        await Expect(Hour(callout)).ToBeFocusedAsync();

        // The overlay covers the page while the callout is open, so the page's buttons are clicked from code.
        await Page.Locator("#btn-dismiss").DispatchEventAsync("click");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Out("is-open")).ToHaveTextAsync("False");
        await Expect(Out("prog-closes")).ToHaveTextAsync("1");
        // DismissCallout leaves the focus where it is.
        await Expect(Field("programmatic")).Not.ToBeFocusedAsync();

        await Page.ClickAsync("#btn-set-open");
        await CalloutOfAsync("programmatic");
        await Expect(Out("prog-opens")).ToHaveTextAsync("2");
        await Expect(Hour(callout)).ToBeFocusedAsync();

        await Page.Locator("#btn-set-closed").DispatchEventAsync("click");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Out("prog-closes")).ToHaveTextAsync("2");

        await Field("programmatic").ClickAsync();
        await Expect(Out("is-open")).ToHaveTextAsync("True");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Out("is-open")).ToHaveTextAsync("False");
        await Expect(Out("prog-opens")).ToHaveTextAsync("3");
        await Expect(Out("prog-closes")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task FocusAsync_FocusesTheField_OrTheHourOfAStandalonePicker()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-focus");
        await Expect(Field("programmatic")).ToBeFocusedAsync();

        await Page.ClickAsync("#btn-focus-standalone");
        await Expect(Hour(StandaloneCallout("programmatic-standalone"))).ToBeFocusedAsync();

        // A standalone picker has no callout to open.
        await Page.ClickAsync("#btn-open-standalone");
        await Expect(StandaloneCallout("programmatic-standalone")).ToBeVisibleAsync();
        await Expect(Page.Locator("#programmatic-standalone .bit-tpc-ovl")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task InputId_IsTheIdOfTheField()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-read-input-id");
        await Expect(Out("input-id")).ToHaveTextAsync((await Field("programmatic").GetAttributeAsync("id"))!);
    }

    [TestMethod]
    public async Task IsOpen_InitiallyTrue_OpensTheCalloutOnceInteractive()
    {
        await OpenAsync("/e2e/time-picker/setup?mode=open");

        var callout = await CalloutOfAsync("setup");
        await Expect(Out("is-open")).ToHaveTextAsync("True");
        await SettleAsync(callout);
        var field = (await BoxAsync(Page.Locator("#setup .bit-tpc-icn")))!;
        Assert.IsTrue((await BoxAsync(callout))!.Y >= field.Y + field.Height - 2, "The initially open callout is placed against its field.");

        await Spin(callout, "Increase hour").ClickAsync();
        await Expect(Field("setup")).ToHaveValueAsync("01:00");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Out("is-open")).ToHaveTextAsync("False");
    }

    // ------------------------------------------------------------------ templates

    [TestMethod]
    public async Task LabelTemplate_ReplacesTheLabelAndStillNamesTheField()
    {
        await OpenAsync(Url);

        var label = Page.Locator("#templates-label label");
        await Expect(label.Locator("#custom-label")).ToHaveTextAsync("Custom label");
        await Expect(label).ToHaveAttributeAsync("for", (await Field("templates-label").GetAttributeAsync("id"))!);
        await Expect(Field("templates-label")).ToHaveAttributeAsync("aria-labelledby", (await label.GetAttributeAsync("id"))!);
    }

    [TestMethod]
    public async Task IconTemplate_ReplacesTheIcon()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#templates-icon #custom-icon")).ToHaveTextAsync("ICON");
        await Expect(Page.Locator("#templates-icon .bit-tpc-ico")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task CalloutHeaderAndFooterTemplates_FrameTheInputs_AndCanSetTheValue()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("templates-callout");
        await SettleAsync(callout);
        await Expect(callout.Locator(".bit-tpc-chd #callout-header")).ToHaveTextAsync("Pick a meeting time");
        await Expect(callout.Locator(".bit-tpc-cft #callout-footer")).ToBeVisibleAsync();

        var header = (await BoxAsync(callout.Locator(".bit-tpc-chd")))!;
        var inputs = (await BoxAsync(callout.Locator(".bit-tpc-tic")))!;
        var footer = (await BoxAsync(callout.Locator(".bit-tpc-cft")))!;
        Assert.IsTrue(header.Y < inputs.Y && inputs.Y < footer.Y, "The header is above the time inputs and the footer below them.");

        await callout.Locator("#preset-1330").ClickAsync();
        await Expect(Out("templates-callout-value")).ToHaveTextAsync("13:30:00");
        await Expect(Hour(callout)).ToHaveValueAsync("13");
        await Expect(Minute(callout)).ToHaveValueAsync("30");
    }

    [TestMethod]
    public async Task TextTemplates_ReplaceTheDescriptionAddonsAndError()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#templates-text .bit-tpc-des #custom-description")).ToHaveTextAsync("Custom description");
        await Expect(Page.Locator("#templates-text .bit-tpc-erm #custom-error")).ToHaveTextAsync("Custom error");
        await Expect(Page.Locator("#templates-text #custom-prefix")).ToHaveTextAsync("PRE");
        await Expect(Page.Locator("#templates-text #custom-suffix")).ToHaveTextAsync("SUF");
        await Expect(Page.Locator("#templates-text .bit-tpc-pre")).ToHaveCountAsync(0);
        // An error template marks the picker invalid as the plain message does.
        await Expect(Root("templates-text")).ToHaveClassAsync(HasClass("bit-inv"));
    }

    // ------------------------------------------------------------------ css variables

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_RestyleTheFieldAndTheCallout()
    {
        await OpenAsync(Url);

        var frame = Page.Locator("#css-vars-default .bit-tpc-icn");
        Assert.AreEqual("20px", await StyleOfAsync(frame, "border-top-left-radius"));
        Assert.AreEqual("50px", await StyleOfAsync(frame, "height"));
        Assert.AreEqual("rgb(0, 128, 128)", await StyleOfAsync(frame, "border-top-color"));
        Assert.AreEqual("rgb(1, 2, 3)", await StyleOfAsync(Page.Locator("#css-vars-default .bit-tpc-ico"), "color"));
        Assert.AreEqual("rgb(4, 5, 6)", await StyleOfAsync(Page.Locator("#css-vars-default .bit-tpc-lbl"), "color"));

        // The callout leaves the picker while open, yet keeps inheriting from it.
        var callout = await OpenCalloutAsync("css-vars-default");
        Assert.AreEqual("24px", await StyleOfAsync(callout, "border-top-left-radius"));
        Assert.AreEqual("rgb(7, 8, 9)", await StyleOfAsync(callout.Locator(".bit-tpc-cac"), "background-color"));
        Assert.AreEqual("50px", await StyleOfAsync(Spin(callout, "Increase hour"), "width"));
        Assert.AreEqual("rgb(0, 128, 128)", await StyleOfAsync(Am(callout), "background-color"));
        Assert.AreEqual("rgb(0, 128, 128)", await StyleOfAsync(Action(callout, "Now"), "color"));
    }

    [TestMethod]
    public async Task CssVariables_AnExplicitColorAndSizeWinOverThem()
    {
        await OpenAsync(Url);

        var frame = Page.Locator("#css-vars-explicit .bit-tpc-icn");
        Assert.AreNotEqual("50px", await StyleOfAsync(frame, "height"));
        // What a parameter does not set stays the variable's.
        Assert.AreEqual("20px", await StyleOfAsync(frame, "border-top-left-radius"));

        var callout = await OpenCalloutAsync("css-vars-explicit");
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-err)"), await StyleOfAsync(Am(callout), "background-color"));
        Assert.AreNotEqual("50px", await StyleOfAsync(Spin(callout, "Increase hour"), "width"));
    }

    [TestMethod]
    public async Task CssVariables_OnTheInstanceStyle_RestyleThatPicker()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("css-vars-style");
        Assert.AreEqual("44px", await StyleOfAsync(Spin(callout, "Increase hour"), "width"));
        Assert.AreEqual("rgb(10, 20, 30)", await StyleOfAsync(Am(callout), "background-color"));
    }

    // ------------------------------------------------------------------ cascading parameters

    [TestMethod]
    public async Task BitParams_CascadesItsDefaults_AndAPickerOwnParameterWins()
    {
        await OpenAsync(Url);

        await Expect(Field("cascade-1")).ToHaveAttributeAsync("placeholder", "Cascaded placeholder");
        await Expect(Field("cascade-2")).ToHaveAttributeAsync("placeholder", "Cascaded placeholder");
        await Expect(Root("cascade-1")).ToHaveClassAsync(HasClass("bit-tpc-suc"));

        var callout = await OpenCalloutAsync("cascade-1");
        await Expect(callout.Locator(".bit-tpc-am-pm")).ToHaveCountAsync(1);
        await Expect(Action(callout, "Cascaded now")).ToBeVisibleAsync();
        await Spin(callout, "Increase minute").ClickAsync();
        await Expect(Out("cascade-1-value")).ToHaveTextAsync("00:15:00");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Field("cascade-1")).ToHaveValueAsync("12:15 AM");

        callout = await OpenCalloutAsync("cascade-3");
        await Expect(callout.Locator(".bit-tpc-am-pm")).ToHaveCountAsync(0);
        await Spin(callout, "Increase minute").ClickAsync();
        await Expect(Out("cascade-3-value")).ToHaveTextAsync("00:01:00");
        await Expect(Action(callout, "Cascaded now")).ToBeVisibleAsync();
    }

    // ------------------------------------------------------------------ color

    [TestMethod]
    [DataRow("primary", "pri", "--bit-clr-pri")]
    [DataRow("secondary", "sec", "--bit-clr-sec")]
    [DataRow("tertiary", "ter", "--bit-clr-ter")]
    [DataRow("info", "inf", "--bit-clr-inf")]
    [DataRow("success", "suc", "--bit-clr-suc")]
    [DataRow("warning", "wrn", "--bit-clr-wrn")]
    [DataRow("severewarning", "swr", "--bit-clr-swr")]
    [DataRow("error", "err", "--bit-clr-err")]
    [DataRow("primarybackground", "pbg", "--bit-clr-bg-pri")]
    [DataRow("secondarybackground", "sbg", "--bit-clr-bg-sec")]
    [DataRow("tertiarybackground", "tbg", "--bit-clr-bg-ter")]
    [DataRow("primaryforeground", "pfg", "--bit-clr-fg-pri")]
    [DataRow("secondaryforeground", "sfg", "--bit-clr-fg-sec")]
    [DataRow("tertiaryforeground", "tfg", "--bit-clr-fg-ter")]
    [DataRow("primaryborder", "pbr", "--bit-clr-brd-pri")]
    [DataRow("secondaryborder", "sbr", "--bit-clr-brd-sec")]
    [DataRow("tertiaryborder", "tbr", "--bit-clr-brd-ter")]
    public async Task Color_EachValue_SetsItsClassAndPaintsTheSelectionWithItsRole(string name, string suffix, string token)
    {
        await OpenAsync(Url);

        var scope = $"color-{name}";
        var callout = StandaloneCallout(scope);
        await Expect(Root(scope)).ToHaveClassAsync(HasClass($"bit-tpc-{suffix}"));
        await Expect(callout).ToHaveClassAsync(HasClass($"bit-tpc-{suffix}"));

        var expected = await ResolveColorAsync($"var({token})");
        Assert.AreEqual(expected, await StyleOfAsync(Am(callout), "background-color"), $"The selected meridiem of {name} is painted with {token}.");
        Assert.AreEqual(expected, await StyleOfAsync(Action(callout, "Now"), "color"), $"The now button of {name} reads {token}.");
    }

    [TestMethod]
    public async Task Color_Unset_PublishesNoClassAndFallsBackToPrimary()
    {
        await OpenAsync(Url);

        await Expect(Root("color-none")).Not.ToHaveClassAsync(new Regex(@"bit-tpc-(pri|sec|ter|inf|suc|wrn|swr|err|pbg|sbg|tbg|pfg|sfg|tfg|pbr|sbr|tbr)(\s|$)"));
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-pri)"), await StyleOfAsync(Am(StandaloneCallout("color-none")), "background-color"));

        var distinct = new HashSet<string>();
        foreach (var name in new[] { "primary", "secondary", "info", "success", "warning", "error" })
        {
            distinct.Add(await StyleOfAsync(Am(StandaloneCallout($"color-{name}")), "background-color"));
        }

        Assert.IsTrue(distinct.Count >= 5, "The accent roles paint the selection with different colors.");
    }

    // ------------------------------------------------------------------ size

    [TestMethod]
    public async Task Size_EachValue_SetsItsClassAndScalesTheField()
    {
        await OpenAsync(Url);

        await Expect(Root("size-small")).ToHaveClassAsync(HasClass("bit-tpc-sm"));
        await Expect(Root("size-medium")).ToHaveClassAsync(HasClass("bit-tpc-md"));
        await Expect(Root("size-large")).ToHaveClassAsync(HasClass("bit-tpc-lg"));

        var heights = new List<float>();
        var labels = new List<float>();
        foreach (var scope in new[] { "size-small", "size-medium", "size-large" })
        {
            heights.Add(Px(await StyleOfAsync(Page.Locator($"#{scope} .bit-tpc-icn"), "height")));
            labels.Add(Px(await StyleOfAsync(Page.Locator($"#{scope} .bit-tpc-lbl"), "font-size")));
        }

        Assert.IsTrue(heights[0] < heights[1] && heights[1] < heights[2], $"Field heights grow with the size: {string.Join(", ", heights)}");
        Assert.IsTrue(labels[0] < labels[1] && labels[1] < labels[2], $"Label sizes grow with the size: {string.Join(", ", labels)}");

        // The medium size is what an unset one stands for.
        Assert.AreEqual(heights[1], Px(await StyleOfAsync(Page.Locator("#basic .bit-tpc-icn"), "height")));

        var small = Px(await StyleOfAsync(Spin(StandaloneCallout("size-small-standalone"), "Increase hour"), "width"));
        var large = Px(await StyleOfAsync(Spin(StandaloneCallout("size-large-standalone"), "Increase hour"), "width"));
        Assert.IsTrue(small < large, $"The spin buttons scale with the size: {small} < {large}");
        var smallFont = Px(await StyleOfAsync(Hour(StandaloneCallout("size-small-standalone")), "font-size"));
        var largeFont = Px(await StyleOfAsync(Hour(StandaloneCallout("size-large-standalone")), "font-size"));
        Assert.IsTrue(smallFont < largeFont);
    }

    // ------------------------------------------------------------------ style & class

    [TestMethod]
    public async Task StyleAndClass_ApplyToTheRoot()
    {
        await OpenAsync(Url);

        var styled = Root("sc-style");
        Assert.AreEqual("7px", await StyleOfAsync(styled, "margin-top"));
        StringAssert.Contains(await StyleOfAsync(styled, "box-shadow"), "rgb(30, 144, 255)");
        await Expect(Root("sc-class")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Root("sc-class")).ToHaveClassAsync(HasClass("bit-tpc"));
    }

    [TestMethod]
    public async Task Styles_ReachEveryPartOfTheFieldAndTheCallout()
    {
        await OpenAsync(Url);

        var s = "#sc-styles";
        Assert.AreEqual("11px", await StyleOfAsync(Root("sc-styles"), "margin-left"));
        Assert.AreEqual("rgb(1, 1, 1)", await ComputedStyleAsync($"{s} .bit-tpc-lbl", "color"));
        Assert.AreEqual("3px", await ComputedStyleAsync($"{s} .bit-tpc-wrp", "padding-top"));
        Assert.AreEqual("rgb(2, 2, 2)", await ComputedStyleAsync($"{s} .bit-tpc-icn", "background-color"));
        Assert.AreEqual("9px", await ComputedStyleAsync($"{s} .bit-tpc-inp", "padding-left"));
        Assert.AreEqual("rgb(3, 3, 3)", await ComputedStyleAsync($"{s} .bit-tpc-ico", "color"));
        Assert.AreEqual("rgb(4, 4, 4)", await ComputedStyleAsync($"{s} .bit-tpc-pre", "background-color"));
        Assert.AreEqual("rgb(5, 5, 5)", await ComputedStyleAsync($"{s} .bit-tpc-pre > span", "color"));
        Assert.AreEqual("rgb(6, 6, 6)", await ComputedStyleAsync($"{s} .bit-tpc-suf", "background-color"));
        Assert.AreEqual("rgb(7, 7, 7)", await ComputedStyleAsync($"{s} .bit-tpc-suf > span", "color"));
        Assert.AreEqual("5px", await ComputedStyleAsync($"{s} .bit-tpc-des", "padding-top"));
        Assert.AreEqual("rgb(8, 8, 8)", await ComputedStyleAsync($"{s} .bit-tpc-des > span", "color"));

        var callout = await OpenCalloutAsync("sc-styles");
        // The focused style applies while the callout is open.
        StringAssert.Contains(await StyleOfAsync(Root("sc-styles"), "outline"), "rgb(255, 0, 0)");
        Assert.AreEqual("333px", await StyleOfAsync(callout, "min-width"));
        Assert.AreEqual("6px", await StyleOfAsync(callout.Locator(".bit-tpc-cac"), "padding-top"));
        Assert.AreEqual("13px", await StyleOfAsync(callout.Locator(".bit-tpc-tic"), "column-gap"));

        var expected = new (string Selector, string Property, string Value)[]
        {
            (".bit-tpc-tic > .bit-tpc-tpr:nth-child(1)", "padding-top", "1px"),
            ("button[aria-label='Increase hour']", "color", "rgb(9, 9, 9)"),
            ("button[aria-label='Increase hour'] i", "color", "rgb(10, 10, 10)"),
            (".bit-tpc-tin >> nth=0", "color", "rgb(11, 11, 11)"),
            ("button[aria-label='Decrease hour']", "color", "rgb(12, 12, 12)"),
            ("button[aria-label='Decrease hour'] i", "color", "rgb(13, 13, 13)"),
            (".bit-tpc-tdv >> nth=0", "color", "rgb(14, 14, 14)"),
            (".bit-tpc-tic > .bit-tpc-tpr:nth-child(3)", "padding-top", "2px"),
            ("button[aria-label='Increase minute']", "color", "rgb(15, 15, 15)"),
            ("button[aria-label='Increase minute'] i", "color", "rgb(16, 16, 16)"),
            (".bit-tpc-tin >> nth=1", "color", "rgb(17, 17, 17)"),
            ("button[aria-label='Decrease minute']", "color", "rgb(18, 18, 18)"),
            ("button[aria-label='Decrease minute'] i", "color", "rgb(19, 19, 19)"),
            (".bit-tpc-tdv >> nth=1", "color", "rgb(20, 20, 20)"),
            (".bit-tpc-tic > .bit-tpc-tpr:nth-child(5)", "padding-top", "3px"),
            ("button[aria-label='Increase second']", "color", "rgb(21, 21, 21)"),
            ("button[aria-label='Increase second'] i", "color", "rgb(22, 22, 22)"),
            (".bit-tpc-tin >> nth=2", "color", "rgb(23, 23, 23)"),
            ("button[aria-label='Decrease second']", "color", "rgb(24, 24, 24)"),
            ("button[aria-label='Decrease second'] i", "color", "rgb(25, 25, 25)"),
            (".bit-tpc-am-pm", "padding-top", "4px"),
            (".bit-tpc-bam", "color", "rgb(26, 26, 26)"),
            (".bit-tpc-bpm", "color", "rgb(27, 27, 27)"),
            (".bit-tpc-act", "padding-top", "7px"),
            (".bit-tpc-abn >> nth=0", "color", "rgb(28, 28, 28)"),
            (".bit-tpc-abn >> nth=1", "color", "rgb(29, 29, 29)"),
            (".bit-tpc-cbn", "color", "rgb(30, 30, 30)"),
            (".bit-tpc-cbn i", "color", "rgb(31, 31, 31)"),
        };

        foreach (var (selector, property, value) in expected)
        {
            Assert.AreEqual(value, await StyleOfAsync(callout.Locator(selector), property), $"{selector} {property}");
        }

        var overlay = Page.Locator(".bit-tpc-ovl").Filter(new() { Visible = true });
        Assert.AreEqual("rgba(0, 0, 0, 0.1)", await StyleOfAsync(overlay, "background-color"));
    }

    [TestMethod]
    public async Task Classes_ReachEveryPartOfTheFieldAndTheCallout()
    {
        await OpenAsync(Url);

        var s = "#sc-classes";
        await Expect(Root("sc-classes")).ToHaveClassAsync(HasClass("c-root"));
        foreach (var (selector, css) in new[]
                 {
                     (".bit-tpc-lbl", "c-label"), (".bit-tpc-wrp", "c-input-wrapper"), (".bit-tpc-icn", "c-input-container"),
                     (".bit-tpc-inp", "c-input"), (".bit-tpc-ico", "c-icon"), (".bit-tpc-pre", "c-prefix-container"),
                     (".bit-tpc-pre > span", "c-prefix"), (".bit-tpc-suf", "c-suffix-container"), (".bit-tpc-suf > span", "c-suffix"),
                     (".bit-tpc-clr", "c-input-clear"), (".bit-tpc-clr i", "c-input-clear-icon"), (".bit-tpc-erm", "c-error-container"),
                     (".bit-tpc-erm > span", "c-error"), (".bit-tpc-des", "c-description-container"), (".bit-tpc-des > span", "c-description"),
                     (".bit-tpc-ovl", "c-overlay")
                 })
        {
            await Expect(Page.Locator($"{s} {selector}")).ToHaveClassAsync(HasClass(css));
        }

        await Expect(Root("sc-classes")).Not.ToHaveClassAsync(HasClass("c-focused"));
        var callout = await OpenCalloutAsync("sc-classes");
        await Expect(Root("sc-classes")).ToHaveClassAsync(HasClass("c-focused"));
        await Expect(callout).ToHaveClassAsync(HasClass("c-callout"));

        foreach (var (selector, css) in new[]
                 {
                     (".bit-tpc-cac", "c-callout-container"), (".bit-tpc-tic", "c-time-inputs"),
                     (".bit-tpc-tic > .bit-tpc-tpr:nth-child(1)", "c-hour-container"), ("button[aria-label='Increase hour']", "c-inc-hour"),
                     ("button[aria-label='Increase hour'] i", "c-inc-hour-icon"), (".bit-tpc-tin >> nth=0", "c-hour"),
                     ("button[aria-label='Decrease hour']", "c-dec-hour"), ("button[aria-label='Decrease hour'] i", "c-dec-hour-icon"),
                     (".bit-tpc-tdv >> nth=0", "c-hm-sep"), (".bit-tpc-tic > .bit-tpc-tpr:nth-child(3)", "c-minute-container"),
                     ("button[aria-label='Increase minute']", "c-inc-minute"), ("button[aria-label='Increase minute'] i", "c-inc-minute-icon"),
                     (".bit-tpc-tin >> nth=1", "c-minute"), ("button[aria-label='Decrease minute']", "c-dec-minute"),
                     ("button[aria-label='Decrease minute'] i", "c-dec-minute-icon"), (".bit-tpc-tdv >> nth=1", "c-ms-sep"),
                     (".bit-tpc-tic > .bit-tpc-tpr:nth-child(5)", "c-second-container"), ("button[aria-label='Increase second']", "c-inc-second"),
                     ("button[aria-label='Increase second'] i", "c-inc-second-icon"), (".bit-tpc-tin >> nth=2", "c-second"),
                     ("button[aria-label='Decrease second']", "c-dec-second"), ("button[aria-label='Decrease second'] i", "c-dec-second-icon"),
                     (".bit-tpc-am-pm", "c-ampm"), (".bit-tpc-bam", "c-am"), (".bit-tpc-bpm", "c-pm"), (".bit-tpc-act", "c-actions"),
                     (".bit-tpc-abn >> nth=0", "c-now"), (".bit-tpc-abn >> nth=1", "c-clear"), (".bit-tpc-cbn", "c-close"),
                     (".bit-tpc-cbn i", "c-close-icon")
                 })
        {
            await Expect(callout.Locator(selector)).ToHaveClassAsync(HasClass(css));
        }
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheCalloutHeaderAndFooter()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("sc-header-footer");
        await Expect(callout.Locator(".bit-tpc-chd")).ToHaveClassAsync(HasClass("c-header"));
        await Expect(callout.Locator(".bit-tpc-cft")).ToHaveClassAsync(HasClass("c-footer"));
        Assert.AreEqual("rgb(40, 40, 40)", await StyleOfAsync(callout.Locator(".bit-tpc-chd"), "color"));
        Assert.AreEqual("rgb(41, 41, 41)", await StyleOfAsync(callout.Locator(".bit-tpc-cft"), "color"));
    }

    // ------------------------------------------------------------------ rtl

    [TestMethod]
    public async Task Rtl_Explicit_MirrorsTheFieldAndTheCallout()
    {
        await OpenAsync(Url);

        var root = Root("rtl-field");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");

        var icon = (await BoxAsync(Page.Locator("#rtl-field .bit-tpc-ico")))!;
        var input = (await BoxAsync(Field("rtl-field")))!;
        Assert.IsTrue(icon.X < input.X, "In RTL the end-side icon is on the left of the input.");

        var callout = await OpenCalloutAsync("rtl-field");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-tpc-rtl"));
        await Expect(callout).ToHaveAttributeAsync("dir", "rtl");
        await SettleAsync(callout);
        var calloutBox = (await BoxAsync(callout))!;
        var close = (await BoxAsync(callout.Locator(".bit-tpc-cbn")))!;
        Assert.IsTrue(close.X - calloutBox.X < calloutBox.Width / 2f, "In RTL the close button sits at the left edge of the callout.");
        var fieldBox = (await BoxAsync(Page.Locator("#rtl-field .bit-tpc-icn")))!;
        Assert.IsTrue(Math.Abs((calloutBox.X + calloutBox.Width) - (fieldBox.X + fieldBox.Width)) < 2 || Math.Abs(calloutBox.X - fieldBox.X) < 2,
            "The callout is aligned with the field it opens from.");

        var standalone = StandaloneCallout("rtl-standalone");
        var hour = (await BoxAsync(Hour(standalone)))!;
        var minute = (await BoxAsync(Minute(standalone)))!;
        var ampm = (await BoxAsync(standalone.Locator(".bit-tpc-am-pm")))!;
        Assert.IsTrue(hour.X < minute.X, "The parts of the time keep running left to right.");
        Assert.IsTrue(ampm.X < hour.X, "The meridiem section mirrors to the left of the time.");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_IsFollowedByTheFieldAndTheRelocatedCallout()
    {
        await OpenAsync(Url);

        await Expect(Root("rtl-inherited")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        var icon = (await BoxAsync(Page.Locator("#rtl-inherited .bit-tpc-ico")))!;
        var input = (await BoxAsync(Field("rtl-inherited")))!;
        Assert.IsTrue(icon.X < input.X, "An inherited RTL direction puts the end-side icon on the left.");

        var callout = await OpenCalloutAsync("rtl-inherited");
        Assert.AreEqual("rtl", await StyleOfAsync(callout, "direction"));
        await SettleAsync(callout);
        var calloutBox = (await BoxAsync(callout))!;
        var close = (await BoxAsync(callout.Locator(".bit-tpc-cbn")))!;
        Assert.IsTrue(close.X - calloutBox.X < calloutBox.Width / 2f, "The relocated callout still reads right to left.");
        await Page.Keyboard.PressAsync("Escape");

        var ltr = await OpenCalloutAsync("ltr-field");
        await SettleAsync(ltr);
        var ltrBox = (await BoxAsync(ltr))!;
        var ltrClose = (await BoxAsync(ltr.Locator(".bit-tpc-cbn")))!;
        Assert.IsTrue(ltrClose.X - ltrBox.X > ltrBox.Width / 2f, "Left to right the close button sits at the right edge.");
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
    public async Task Attributes_IdAriaLabelHtmlAttributesNameAndTabIndexReachTheirElements()
    {
        await OpenAsync(Url);

        var root = Page.Locator("#tp-attrs");
        await Expect(root).ToHaveClassAsync(HasClass("bit-tpc"));
        await Expect(root).ToHaveAttributeAsync("data-root", "root-value");
        await Expect(root).ToHaveAttributeAsync("title", "Root title");

        var field = Field("attributes");
        await Expect(field).ToHaveAttributeAsync("aria-label", "Arrival time");
        await Expect(field).ToHaveAttributeAsync("name", "arrival");
        await Expect(field).ToHaveAttributeAsync("tabindex", "3");
        await Expect(field).ToHaveAttributeAsync("data-input", "input-value");
        // An enabled input keeps the page's own aria-disabled.
        await Expect(field).ToHaveAttributeAsync("aria-disabled", "false");

        var callout = await OpenCalloutAsync("attributes");
        await Expect(callout.Locator(".bit-tpc-cac")).ToHaveAttributeAsync("data-callout", "callout-value");
    }

    [TestMethod]
    public async Task PressAndHold_SpinsContinuouslyUntilReleased()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("continuous");
        await Spin(callout, "Increase minute").HoverAsync();
        await Page.Mouse.DownAsync();

        // One step at the press, then one every interval once the delay has run out.
        await Expect(Out("continuous-value")).ToHaveTextAsync(new Regex(@"^10:(0[5-9]|[1-5]\d):00$"), new() { Timeout = 10000 });
        await Page.Mouse.UpAsync();

        // A tick already on its way when the button was let go may still land: a round trip of the page's own
        // button, processed after the release, says the circuit has caught up.
        await Page.ClickAsync("#continuous-sync");
        await Expect(Out("continuous-syncs")).ToHaveTextAsync("1");
        var released = await Out("continuous-value").TextContentAsync();
        // A held button would have stepped several times over this span; a released one never steps again.
        await Page.WaitForTimeoutAsync(500);
        await Expect(Out("continuous-value")).ToHaveTextAsync(released!);
    }

    [TestMethod]
    public async Task PressAndHold_PointerLeavingTheButton_StopsTheSpin()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("continuous");
        await Spin(callout, "Decrease hour").HoverAsync();
        await Page.Mouse.DownAsync();
        await Expect(Out("continuous-value")).ToHaveTextAsync(new Regex(@"^0[0-7]:00:00$"), new() { Timeout = 10000 });

        await Page.Mouse.MoveAsync(5, 5);
        var left = await Out("continuous-value").TextContentAsync();
        await Page.WaitForTimeoutAsync(500);
        await Expect(Out("continuous-value")).ToHaveTextAsync(left!);
        await Page.Mouse.UpAsync();
    }

    [TestMethod]
    public async Task PressAndHold_StaysInsideTheRange()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("continuous-bounded");
        await Spin(callout, "Increase minute").HoverAsync();
        await Page.Mouse.DownAsync();

        var seen = new HashSet<string>();
        for (var i = 0; i < 25; i++)
        {
            seen.Add((await Out("continuous-bounded-value").TextContentAsync())!);
            await NextFramesAsync();
        }

        await Page.Mouse.UpAsync();
        await Expect(Out("continuous-bounded-value")).ToHaveTextAsync(new Regex(@"^10:0[0-5]:00$"));
        Assert.IsTrue(seen.All(v => Regex.IsMatch(v, @"^10:0[0-5]:00$")), $"Every value the held button produced is in range: {string.Join(", ", seen)}");
        Assert.IsTrue(seen.Count > 1, "The held button moved the minute.");
    }

    [TestMethod]
    public async Task Paste_AWholeTimeIntoATimeInput_SetsEveryPart()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("paste");
        const string paste = @"(el, text) => {
            const data = new DataTransfer();
            data.setData('text', text);
            el.dispatchEvent(new ClipboardEvent('paste', { clipboardData: data, bubbles: true, cancelable: true }));
        }";

        await Minute(callout).EvaluateAsync(paste, "14:30");
        await Expect(Out("paste-value")).ToHaveTextAsync("14:30:00");

        await Hour(callout).EvaluateAsync(paste, "2:30:15 PM");
        await Expect(Out("paste-value")).ToHaveTextAsync("14:30:15");

        await Second(callout).EvaluateAsync(paste, "not a time");
        await Spin(callout, "Increase second").ClickAsync();
        await Expect(Out("paste-value")).ToHaveTextAsync("14:30:16");
    }

    [TestMethod]
    public async Task InsideAModal_EscapeClosesTheCalloutFirstAndTheModalOnTheNextPress()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-open-modal");
        await Expect(Page.Locator("#modal-content")).ToBeVisibleAsync();
        await Expect(Out("modal-open")).ToHaveTextAsync("True");

        var callout = await OpenCalloutAsync("modal-content");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#modal-picker-closes")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#modal-content")).ToBeVisibleAsync();
        await Expect(Out("modal-open")).ToHaveTextAsync("True");
        await Expect(Field("modal-content")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Out("modal-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task AutoFocus_FocusesTheFieldOnTheFirstInteractiveRender()
    {
        await OpenAsync("/e2e/time-picker/setup?mode=autofocus");

        await Expect(Field("setup")).ToBeFocusedAsync();
        await Expect(Out("focus-count")).ToHaveTextAsync("1");
        await Expect(Field("setup")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task AutoFocus_OnAStandalonePicker_FocusesTheHourInput()
    {
        await OpenAsync("/e2e/time-picker/setup?mode=autofocus-standalone");

        await Expect(Hour(StandaloneCallout("setup"))).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheCalloutAnimation_AndForceAnimationRestoresIt()
    {
        await OpenAsync(Url);

        var normal = await OpenCalloutAsync("reduced-motion");
        var full = await StyleOfAsync(normal, "animation-duration");
        await Page.Keyboard.PressAsync("Escape");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        var reduced = await OpenCalloutAsync("reduced-motion");
        var collapsed = await StyleOfAsync(reduced, "animation-duration");
        await Page.Keyboard.PressAsync("Escape");

        var forced = await OpenCalloutAsync("force-animation");
        var restored = await StyleOfAsync(forced, "animation-duration");

        Assert.IsTrue(Seconds(full) > 0.05, $"The callout animates by default ({full}).");
        Assert.IsTrue(Seconds(collapsed) < 0.01, $"Reduced motion collapses the animation ({collapsed}).");
        Assert.AreEqual(full, restored, "ForceAnimation restores the full duration under reduced motion.");

        static double Seconds(string duration) => duration.EndsWith("ms", StringComparison.Ordinal)
            ? double.Parse(duration[..^2], System.Globalization.CultureInfo.InvariantCulture) / 1000
            : double.Parse(duration.TrimEnd('s'), System.Globalization.CultureInfo.InvariantCulture);
    }
}
