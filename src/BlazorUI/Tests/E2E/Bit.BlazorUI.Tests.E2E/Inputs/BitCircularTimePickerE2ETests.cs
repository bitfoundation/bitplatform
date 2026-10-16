using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// Drives BitCircularTimePicker (/e2e/circular-time-picker) in a real browser: the field and its callout (opening,
/// positioning, Escape, the overlay, the close button, Tab), the dial dragged with the real mouse on both rings of the
/// 24-hour face and on the 12-hour face, a click on a number, the keyboard of the listbox (arrows, Page, Home/End,
/// digits typed ahead, the meridiem letters, Enter/Space settling a view), Shift+wheel, the toolbar views and
/// SwitchView/View/StartView/EditMode, 12/24-hour and AmPmInClock, seconds, steps, MinTime/MaxTime/Allowed*/
/// DisablePast/DisableFuture, AutoClose and its delay, the now/clear/close buttons, AllowTextInput and ValueFormat,
/// validation in an EditForm (field and standalone), two-way binding of Value and IsOpen, every event, templates,
/// Landscape, Color/Size/Style/Class/Styles/Classes, the CSS variables, the BitParams cascade, RTL, Visibility, the
/// disabled and read-only contracts, Escape inside a modal, reduced motion and the lifecycle.
/// </summary>
[TestClass]
public class BitCircularTimePickerE2ETests : E2ETestBase
{
    private const string Url = "/e2e/circular-time-picker";

    // ------------------------------------------------------------------ helpers

    private ILocator Root(string scope) => Page.Locator($"#{scope} .bit-ctp").First;

    private ILocator Field(string scope) => Page.Locator($"#{scope} .bit-ctp-inp").First;

    private ILocator Out(string id) => Page.Locator($"#{id}");

    private ILocator StandaloneCallout(string scope) => Page.Locator($"#{scope} .bit-ctp-cal").First;

    private static ILocator Dial(ILocator callout) => callout.Locator(".bit-ctp-clf");

    private static ILocator Number(ILocator callout, string text) => callout.GetByRole(AriaRole.Option, new() { Name = text, Exact = true });

    private static ILocator HourButton(ILocator callout) => callout.Locator(".bit-ctp-hmc button").Nth(0);

    private static ILocator MinuteButton(ILocator callout) => callout.Locator(".bit-ctp-hmc button").Nth(1);

    private static ILocator SecondButton(ILocator callout) => callout.Locator(".bit-ctp-hmc button").Nth(2);

    private static ILocator Am(ILocator callout) => callout.Locator(".bit-ctp-apb").Nth(0);

    private static ILocator Pm(ILocator callout) => callout.Locator(".bit-ctp-apb").Nth(1);

    private static ILocator Action(ILocator callout, string text) => callout.Locator(".bit-ctp-abn", new() { HasTextString = text });

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

    private Task<LocatorBoundingBoxResult?> BoxAsync(ILocator locator) => locator.BoundingBoxAsync();

    // The point of the dial at an angle clockwise from 12 o'clock and a distance from its center as a fraction of its
    // radius, which is exactly what the script reads a pointer as.
    private static async Task<(float X, float Y)> PointAsync(ILocator callout, double degrees, double distance)
    {
        await callout.ScrollIntoViewIfNeededAsync();
        await SettleAsync(callout);
        var box = (await Dial(callout).BoundingBoxAsync())!;
        var radius = Math.Min(box.Width, box.Height) / 2;
        var radians = degrees * Math.PI / 180;

        return ((float)(box.X + box.Width / 2 + Math.Sin(radians) * distance * radius),
                (float)(box.Y + box.Height / 2 - Math.Cos(radians) * distance * radius));
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

    private static Task<string> StyleOfAsync(ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>($"el => getComputedStyle(el).getPropertyValue('{property}')");
    }

    private static float Px(string cssLength) => float.Parse(cssLength.Replace("px", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture);

    // ------------------------------------------------------------------ lifecycle

    [TestMethod]
    public async Task Prerender_TheServerHtml_AlreadyContainsTheField()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.AreEqual(200, response.Status);
        StringAssert.Contains(html, "id=\"ctp-basic\"");
        StringAssert.Contains(html, "class=\"bit-ctp-inp");
        StringAssert.Contains(html, "role=\"listbox\"");
        StringAssert.Contains(html, "Status: <span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Lifecycle_ParametersChangedAtRuntime_ReRenderThePicker()
    {
        await OpenAsync(Url);

        await Expect(Field("lifecycle")).ToHaveValueAsync("15:20");
        await Page.ClickAsync("#btn-relabel");
        await Expect(Page.Locator("#lifecycle .bit-ctp-lbl")).ToHaveTextAsync("Relabelled");

        await Page.ClickAsync("#btn-twelve");
        await Expect(Field("lifecycle")).ToHaveValueAsync("03:20 PM");

        await Page.ClickAsync("#btn-seconds");
        await Expect(Field("lifecycle")).ToHaveValueAsync("03:20:10 PM");

        var callout = await OpenCalloutAsync("lifecycle");
        await Expect(SecondButton(callout)).ToHaveTextAsync("10");
        await Expect(Pm(callout)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    public async Task Lifecycle_UnmountedWhileOpen_RemovesTheCalloutWithoutAnError()
    {
        await OpenAsync(Url);

        var calloutId = (await Field("lifecycle").GetAttributeAsync("id"))!.Replace("-input", "-callout", StringComparison.Ordinal);
        await OpenCalloutAsync("lifecycle");

        await Page.Locator("#btn-toggle-mounted").DispatchEventAsync("click");
        await Expect(Page.Locator("#lifecycle .bit-ctp")).ToHaveCountAsync(0);
        await Expect(Page.Locator($"[id='{calloutId}']")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-toggle-mounted");
        var callout = await OpenCalloutAsync("lifecycle");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("lifecycle-value")).ToHaveTextAsync("16:20:10");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // ------------------------------------------------------------------ basic

    [TestMethod]
    public async Task Basic_ClickTheField_OpensADialogBelowItWithTheDialFocused()
    {
        await OpenAsync(Url);

        var field = Field("basic");
        await Expect(field).ToHaveAttributeAsync("role", "combobox");
        await Expect(field).ToHaveAttributeAsync("aria-haspopup", "dialog");
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "false");

        var callout = await OpenCalloutAsync("basic");
        var dialog = callout.Locator(".bit-ctp-cac");
        await Expect(dialog).ToHaveAttributeAsync("role", "dialog");
        await Expect(dialog).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(dialog).ToHaveAttributeAsync("aria-label", "Clock");
        await Expect(Dial(callout)).ToBeFocusedAsync();
        await Expect(Dial(callout)).ToHaveAttributeAsync("role", "listbox");
        await Expect(Dial(callout)).ToHaveAttributeAsync("aria-label", "Select hour");
        await Expect(Root("basic")).ToHaveClassAsync(HasClass("bit-ctp-foc"));
        await Expect(Page.Locator("#basic .bit-ctp-wrp")).ToHaveAttributeAsync("aria-owns", (await callout.GetAttributeAsync("id"))!);

        // The 24-hour face: 13-23 and 00 outside, 01-12 on the inner ring.
        await Expect(Dial(callout).Locator(".bit-ctp-num")).ToHaveCountAsync(24);
        await Expect(Dial(callout).Locator(".bit-ctp-inn")).ToHaveCountAsync(12);
        await Expect(HourButton(callout)).ToHaveTextAsync("--");
        await Expect(MinuteButton(callout)).ToHaveTextAsync("--");

        await SettleAsync(callout);
        var fieldBox = (await BoxAsync(Page.Locator("#basic .bit-ctp-icn")))!;
        var calloutBox = (await BoxAsync(callout))!;
        Assert.IsTrue(calloutBox.Y >= fieldBox.Y + fieldBox.Height - 2, "The callout opens below the field.");
        Assert.AreEqual(fieldBox.X, calloutBox.X, 2, "The callout is aligned with the start of the field.");
    }

    [TestMethod]
    public async Task Basic_ClickANumber_PicksItAndMovesOnToTheMinutes()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("basic");
        await Number(callout, "15").ClickAsync();
        await Expect(Out("basic-value")).ToHaveTextAsync("15:00:00");
        await Expect(HourButton(callout)).ToHaveTextAsync("15");
        await Expect(MinuteButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Dial(callout)).ToHaveAttributeAsync("aria-label", "Select minute");
        await Expect(Dial(callout).Locator(".bit-ctp-num")).ToHaveCountAsync(12);

        await Number(callout, "45").ClickAsync();
        await Expect(Out("basic-value")).ToHaveTextAsync("15:45:00");
        await Expect(Number(callout, "45")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Number(callout, "45")).ToHaveClassAsync(HasClass("bit-ctp-sel"));
        await Expect(Dial(callout)).ToHaveAttributeAsync("aria-activedescendant", (await Number(callout, "45").GetAttributeAsync("id"))!);
        await Expect(callout).ToBeVisibleAsync();

        await HourButton(callout).ClickAsync();
        await Expect(HourButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Number(callout, "03").ClickAsync();
        await Expect(Out("basic-value")).ToHaveTextAsync("03:45:00");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Field("basic")).ToHaveValueAsync("03:45");
    }

    [TestMethod]
    public async Task Basic_Escape_ClosesTheCalloutAndHandsTheFocusBackToTheField()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("basic");
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
        await SettleAsync(callout);
        var box = (await BoxAsync(callout))!;
        await Page.Mouse.ClickAsync(box.X + box.Width + 100, box.Y + 20);

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("basic")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Basic_NearTheBottomOfTheViewport_TheCalloutOpensAboveTheField()
    {
        await OpenAsync(Url);

        await Page.Locator("#underlined .bit-ctp-icn").EvaluateAsync(
            "(el, height) => window.scrollBy(0, el.getBoundingClientRect().bottom - height + 8)", Page.ViewportSize!.Height);

        var callout = await OpenCalloutAsync("underlined");
        await SettleAsync(callout);
        var fieldBox = (await BoxAsync(Page.Locator("#underlined .bit-ctp-icn")))!;
        var calloutBox = (await BoxAsync(callout))!;
        Assert.IsTrue(calloutBox.Y + calloutBox.Height <= fieldBox.Y + 2, "With no room below, the callout drops above the field.");
    }

    [TestMethod]
    public async Task PlaceholderAndIconName_AreRendered()
    {
        await OpenAsync(Url);

        await Expect(Field("placeholder")).ToHaveAttributeAsync("placeholder", "Select a time");
        await Expect(Page.Locator("#placeholder .bit-ctp-ico")).ToHaveClassAsync(HasClass("bit-icon--Airplane"));
        await Expect(Page.Locator("#basic .bit-ctp-ico")).ToHaveClassAsync(HasClass("bit-icon--Clock"));
    }

    [TestMethod]
    public async Task Required_MarksTheLabelAndTheInput()
    {
        await OpenAsync(Url);

        await Expect(Root("required")).ToHaveClassAsync(HasClass("bit-ctp-req"));
        await Expect(Field("required")).ToHaveAttributeAsync("required", "");
        Assert.AreEqual("\"*\"", await Page.Locator("#required .bit-ctp-lbl").EvaluateAsync<string>("el => getComputedStyle(el, '::after').content"));
    }

    [TestMethod]
    public async Task Disabled_RendersTheDisabledContract_AndRunsNoneOfThePageHandlers()
    {
        await OpenAsync(Url);

        var root = Root("disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Field("disabled")).ToBeDisabledAsync();
        await Expect(Field("disabled")).ToHaveAttributeAsync("aria-disabled", "true");

        await Page.Locator("#disabled .bit-ctp-icn").ClickAsync(new() { Force = true });
        await root.DispatchEventAsync("click");
        await root.DispatchEventAsync("keydown", new { key = "Enter" });
        await Expect(Field("disabled")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Out("disabled-click-count")).ToHaveTextAsync("0");
        await Expect(Out("disabled-key-count")).ToHaveTextAsync("0");

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await root.DispatchEventAsync("click");
        await Expect(Out("disabled-click-count")).ToHaveTextAsync("1");

        var callout = await OpenCalloutAsync("disabled");
        // The dial takes the focus through a round trip; the key waits for it.
        await Expect(callout.Locator(":focus")).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("disabled-value")).ToHaveTextAsync("11:10:00");
    }

    [TestMethod]
    public async Task Disabled_Standalone_DisablesTheDialTheToolbarAndTheButtons()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("disabled-standalone");
        await Expect(Root("disabled-standalone")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(callout).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Dial(callout)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Dial(callout)).ToHaveAttributeAsync("tabindex", "-1");

        foreach (var button in await callout.Locator("button").AllAsync())
        {
            await Expect(button).ToBeDisabledAsync();
            await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        }

        var before = await Page.Locator("#disabled-standalone input.bit-input-hidden").InputValueAsync();
        await Number(callout, "3").ClickAsync(new() { Force = true });
        await Dial(callout).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Page.Locator("#disabled-standalone input.bit-input-hidden")).ToHaveValueAsync(before);
        await Expect(Number(callout, "10")).ToHaveAttributeAsync("aria-selected", "true");
    }

    // ------------------------------------------------------------------ standalone

    [TestMethod]
    public async Task Standalone_RendersTheDialInlineAsAGroup_AndTheLabelFocusesTheDial()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("standalone");
        await Expect(Root("standalone")).ToHaveClassAsync(HasClass("bit-ctp-sta"));
        await Expect(callout).ToHaveClassAsync(HasClass("bit-ctp-sta"));
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#standalone .bit-ctp-inp")).ToHaveCountAsync(0);
        await Expect(callout.Locator(".bit-ctp-cac")).ToHaveAttributeAsync("role", "group");
        Assert.IsNull(await callout.Locator(".bit-ctp-cac").GetAttributeAsync("aria-modal"));
        await Expect(Dial(callout)).ToHaveAttributeAsync("aria-label", "Standalone picker Select hour");

        var hidden = Page.Locator("#standalone input.bit-input-hidden");
        await Expect(hidden).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(hidden).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(hidden).ToHaveAttributeAsync("name", "standalone-time");

        await Page.Locator("#standalone .bit-ctp-lbl").ClickAsync();
        await Expect(Dial(callout)).ToBeFocusedAsync();

        await Number(callout, "18").ClickAsync();
        await Expect(Out("standalone-value")).ToHaveTextAsync("18:00:00");
        await Expect(hidden).ToHaveValueAsync("18:00");
        await Expect(callout).ToBeVisibleAsync();
    }

    // ------------------------------------------------------------------ dragging the dial

    [TestMethod]
    public async Task Drag_WithTheMouse_FollowsThePointerAcrossBothRingsAndPicksWhereItIsReleased()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("drag");
        var pointer = callout.Locator(".bit-ctp-ptr");

        var (x, y) = await PointAsync(callout, 90, 0.85);
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Expect(Out("drag-value")).ToHaveTextAsync("15:00:00");
        await Expect(Dial(callout)).ToBeFocusedAsync();
        // The hand follows the pointer without the transition it takes between resting places.
        await Expect(pointer).Not.ToHaveClassAsync(HasClass("bit-ctp-ani"));

        (x, y) = await PointAsync(callout, 150, 0.85);
        await Page.Mouse.MoveAsync(x, y, new() { Steps = 8 });
        await Expect(Out("drag-value")).ToHaveTextAsync("17:00:00");
        await Expect(pointer).ToHaveAttributeAsync("style", new Regex(@"rotateZ\(150deg\)"));
        await Expect(pointer).Not.ToHaveClassAsync(HasClass("bit-ctp-pti"));

        // Inside the boundary between the rings the inner ring's 1-12 are picked, and the hand shortens to it.
        (x, y) = await PointAsync(callout, 150, 0.5);
        await Page.Mouse.MoveAsync(x, y, new() { Steps = 4 });
        await Expect(Out("drag-value")).ToHaveTextAsync("05:00:00");
        await Expect(pointer).ToHaveClassAsync(HasClass("bit-ctp-pti"));

        // A drag that wanders off the dial is still tracked to its end.
        (x, y) = await PointAsync(callout, 210, 0.85);
        await Page.Mouse.MoveAsync(x, y, new() { Steps = 4 });
        await Page.Mouse.UpAsync();
        await Expect(Out("drag-value")).ToHaveTextAsync("19:00:00");
        await Expect(pointer).ToHaveClassAsync(HasClass("bit-ctp-ani"));

        // The release settles the hour and the dial moves on to the minutes.
        await Expect(MinuteButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");

        (x, y) = await PointAsync(callout, 0, 0.85);
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        (x, y) = await PointAsync(callout, 222, 0.85);
        await Page.Mouse.MoveAsync(x, y, new() { Steps = 10 });
        await Page.Mouse.UpAsync();

        // 222 degrees is minute 37, which has no number of its own: the hand marks it with the small ring.
        await Expect(Out("drag-value")).ToHaveTextAsync("19:37:00");
        await Expect(callout.Locator(".bit-ctp-pth")).ToHaveClassAsync(HasClass("bit-ctp-ptm"));
        Assert.IsNull(await Dial(callout).GetAttributeAsync("aria-activedescendant"));
        await Expect(callout.Locator(".bit-ctp-lvr").First).ToHaveTextAsync("Select minute 37");
    }

    [TestMethod]
    public async Task Drag_OnTheTwelveHourDial_KeepsTheHalfOfTheDay()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("drag-12");
        await Expect(Dial(callout).Locator(".bit-ctp-num")).ToHaveCountAsync(12);
        await Expect(Pm(callout)).ToHaveAttributeAsync("aria-pressed", "true");

        var (x, y) = await PointAsync(callout, 90, 0.85);
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        (x, y) = await PointAsync(callout, 270, 0.85);
        await Page.Mouse.MoveAsync(x, y, new() { Steps = 10 });
        await Page.Mouse.UpAsync();

        await Expect(Out("drag-12-value")).ToHaveTextAsync("21:00:00");
        await Expect(HourButton(callout)).ToHaveTextAsync("09");

        await HourButton(callout).ClickAsync();
        await Am(callout).ClickAsync();
        await Expect(Out("drag-12-value")).ToHaveTextAsync("09:00:00");
        (x, y) = await PointAsync(callout, 0, 0.85);
        await Page.Mouse.ClickAsync(x, y);
        await Expect(Out("drag-12-value")).ToHaveTextAsync("00:00:00");
        await Expect(HourButton(callout)).ToHaveTextAsync("12");
    }

    [TestMethod]
    public async Task Drag_WithTheRightButton_PicksNothing()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("drag");
        var (x, y) = await PointAsync(callout, 60, 0.85);
        await Page.Mouse.ClickAsync(x, y, new() { Button = MouseButton.Right });
        await Page.Keyboard.PressAsync("Escape");

        // A press of the primary button afterwards is the first pick.
        (x, y) = await PointAsync(callout, 30, 0.85);
        await Page.Mouse.ClickAsync(x, y);
        await Expect(Out("drag-value")).ToHaveTextAsync("13:00:00");
    }

    // ------------------------------------------------------------------ time format

    [TestMethod]
    public async Task TwentyFourHours_ShowsTheHourWithoutAMeridiem()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("format-24");
        await Expect(HourButton(callout)).ToHaveTextAsync("21");
        await Expect(MinuteButton(callout)).ToHaveTextAsync("45");
        await Expect(callout.Locator(".bit-ctp-apb")).ToHaveCountAsync(0);
        await Expect(Number(callout, "21")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod]
    public async Task TwelveHours_TheMeridiemButtonsMoveTheHourByTwelve()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("format-12");
        await Expect(HourButton(callout)).ToHaveTextAsync("09");
        await Expect(Am(callout)).ToHaveTextAsync("AM");
        await Expect(Pm(callout)).ToHaveTextAsync("PM");
        await Expect(Pm(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Pm(callout)).ToHaveClassAsync(HasClass("bit-ctp-aps"));
        await Expect(Am(callout)).ToHaveClassAsync(HasClass("bit-ctp-ina"));
        await Expect(Am(callout)).ToHaveAttributeAsync("title", "AM");
        await Expect(Number(callout, "9")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(callout.Locator(".bit-ctp-tlb .bit-ctp-apc")).ToHaveCountAsync(1);

        await Am(callout).ClickAsync();
        await Expect(Out("format-12-value")).ToHaveTextAsync("09:45:00");
        await Expect(Am(callout)).ToHaveAttributeAsync("aria-pressed", "true");

        await Pm(callout).ClickAsync();
        await Expect(Out("format-12-value")).ToHaveTextAsync("21:45:00");
    }

    [TestMethod]
    public async Task AmPmInClock_PutsTheMeridiemUnderTheClock()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("format-12-clock");
        var pair = callout.Locator(".bit-ctp-apc");
        await Expect(pair).ToHaveClassAsync(HasClass("bit-ctp-apk"));
        await Expect(callout.Locator(".bit-ctp-tlb .bit-ctp-apc")).ToHaveCountAsync(0);
        await Expect(callout.Locator(".bit-ctp-bdy .bit-ctp-apc")).ToHaveCountAsync(1);

        Assert.IsTrue((await BoxAsync(pair))!.Y > (await BoxAsync(callout.Locator(".bit-ctp-clk")))!.Y, "The pair sits under the clock.");

        await Am(callout).ClickAsync();
        await Expect(Out("format-12-clock-value")).ToHaveTextAsync("09:45:00");
    }

    // ------------------------------------------------------------------ seconds

    [TestMethod]
    public async Task ShowSeconds_AddsTheSecondViewAfterTheMinutes()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("seconds");
        await Expect(SecondButton(callout)).ToHaveTextAsync("15");
        await Expect(callout.Locator(".bit-ctp-sep")).ToHaveCountAsync(2);

        await MinuteButton(callout).ClickAsync();
        await Number(callout, "40").ClickAsync();
        await Expect(Out("seconds-value")).ToHaveTextAsync("09:40:15");
        await Expect(SecondButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");

        await Number(callout, "55").ClickAsync();
        await Expect(Out("seconds-value")).ToHaveTextAsync("09:40:55");
        // The last view has nowhere to move on to.
        await Expect(SecondButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    public async Task SecondStep_SnapsTheSecondsToTheGrid()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("seconds-step");
        await Expect(SecondButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Number(callout, "05")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Number(callout, "05")).ToHaveClassAsync(HasClass("bit-ctp-dis"));
        await Expect(Number(callout, "10")).Not.ToHaveAttributeAsync("aria-disabled", "true");

        // A press between two marks snaps to the nearest one the step allows.
        var (x, y) = await PointAsync(callout, 33 * 6, 0.85);
        await Page.Mouse.ClickAsync(x, y);
        await Expect(Out("seconds-step-value")).ToHaveTextAsync("09:30:30");

        await Dial(callout).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("seconds-step-value")).ToHaveTextAsync("09:30:40");
    }

    [TestMethod]
    public async Task ShowSeconds_WithTheField_WritesThem()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("seconds-field");
        await Number(callout, "08").ClickAsync();
        await Number(callout, "15").ClickAsync();
        await Number(callout, "30").ClickAsync();
        await Expect(Out("seconds-field-value")).ToHaveTextAsync("08:15:30");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Field("seconds-field")).ToHaveValueAsync("08:15:30");
    }

    // ------------------------------------------------------------------ edit mode & views

    [TestMethod]
    public async Task EditMode_OnlyHours_StaysOnTheHourAndReadsTheWholeTime()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("only-hours");
        await Expect(callout.Locator(".bit-ctp-hmc button")).ToHaveCountAsync(0);
        await Expect(callout.Locator(".bit-ctp-hmc")).ToHaveTextAsync("09:30");
        await Expect(Dial(callout)).ToHaveAttributeAsync("aria-label", "OnlyHours Select hour");

        await Number(callout, "14").ClickAsync();
        await Expect(Out("only-hours-value")).ToHaveTextAsync("14:30:00");
        await Expect(Dial(callout)).ToHaveAttributeAsync("aria-label", "OnlyHours Select hour");
        await Expect(callout.Locator(".bit-ctp-hmc")).ToHaveTextAsync("14:30");
    }

    [TestMethod]
    public async Task EditMode_OnlyMinutes_ShowsButDoesNotOfferTheMeridiem()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("only-minutes");
        await Expect(Dial(callout)).ToHaveAttributeAsync("aria-label", "OnlyMinutes Select minute");
        await Expect(Pm(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Am(callout)).ToBeDisabledAsync();

        await Number(callout, "10").ClickAsync();
        await Expect(Out("only-minutes-value")).ToHaveTextAsync("21:10:00");
        await Expect(callout.Locator(".bit-ctp-hmc")).ToHaveTextAsync("09:10");
    }

    [TestMethod]
    public async Task EditMode_OnlySeconds_EditsTheSecondsAlone()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("only-seconds");
        await Expect(Dial(callout)).ToHaveAttributeAsync("aria-label", "OnlySeconds Select second");
        await Expect(callout.Locator(".bit-ctp-hmc")).ToHaveTextAsync("09:30:15");

        await Number(callout, "50").ClickAsync();
        await Expect(Out("only-seconds-value")).ToHaveTextAsync("09:30:50");
    }

    [TestMethod]
    public async Task StartView_OpensOnTheMinutes_AndOnViewChangeReportsEverySwitch()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("start-view");
        await Expect(MinuteButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(HourButton(callout)).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(HourButton(callout)).ToHaveClassAsync(HasClass("bit-ctp-ina"));

        await HourButton(callout).ClickAsync();
        await Expect(Out("start-view-views")).ToHaveTextAsync("Hour");
        await Number(callout, "11").ClickAsync();
        await Expect(Out("start-view-views")).ToHaveTextAsync("Hour,Minute");
        await Expect(Out("start-view-value")).ToHaveTextAsync("11:30:00");

        // A switch to the view the dial is already on reports nothing.
        await MinuteButton(callout).ClickAsync();
        await Expect(Out("start-view-views")).ToHaveTextAsync("Hour,Minute");
    }

    [TestMethod]
    public async Task SwitchView_AndView_DriveAndReadTheDialFromCode()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("programmatic-standalone");
        await Page.ClickAsync("#btn-read-view");
        await Expect(Out("view")).ToHaveTextAsync("Hour");

        await Page.ClickAsync("#btn-switch-second");
        await Expect(Out("switched-to")).ToHaveTextAsync("Second");
        await Expect(SecondButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Page.ClickAsync("#btn-read-view");
        await Expect(Out("view")).ToHaveTextAsync("Second");

        await Page.ClickAsync("#btn-switch-minute");
        await Expect(MinuteButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Page.ClickAsync("#btn-switch-hour");
        await Expect(HourButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.ClickAsync("#btn-focus-standalone");
        await Expect(Dial(callout)).ToBeFocusedAsync();
    }

    // ------------------------------------------------------------------ steps

    [TestMethod]
    public async Task MinuteStep_DisablesTheMarksOffTheGridAndStepsByIt()
    {
        await OpenAsync(Url);

        var five = StandaloneCallout("minute-step-5");
        await Expect(five.Locator(".bit-ctp-num.bit-ctp-dis")).ToHaveCountAsync(0);
        await Dial(five).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("minute-step-5-value")).ToHaveTextAsync("10:20:00");

        var fifteen = StandaloneCallout("minute-step-15");
        await Expect(fifteen.Locator(".bit-ctp-num.bit-ctp-dis")).ToHaveCountAsync(8);
        await Expect(Number(fifteen, "20")).ToHaveAttributeAsync("aria-disabled", "true");
        await Dial(fifteen).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("minute-step-15-value")).ToHaveTextAsync("10:30:00");
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Out("minute-step-15-value")).ToHaveTextAsync("10:15:00");

        // A disabled mark takes no pointer: a press on it lands on the dial and picks nothing.
        await Number(fifteen, "20").ClickAsync(new() { Force = true });
        await Expect(Out("minute-step-15-value")).ToHaveTextAsync("10:15:00");
    }

    [TestMethod]
    public async Task HourStep_DisablesTheHoursOffTheGrid()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("hour-step-3");
        await Expect(Number(callout, "10")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Number(callout, "12")).Not.ToHaveAttributeAsync("aria-disabled", "true");

        await Dial(callout).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("hour-step-3-value")).ToHaveTextAsync("12:00:00");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Out("hour-step-3-value")).ToHaveTextAsync("06:00:00");
    }

    // ------------------------------------------------------------------ selectable range

    [TestMethod]
    public async Task MinTimeMaxTime_RuleOutTheTimesOutsideTheRange()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("range");
        await Expect(Number(callout, "07")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Number(callout, "18")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Number(callout, "17")).Not.ToHaveAttributeAsync("aria-disabled", "true");

        await Number(callout, "17").ClickAsync();
        // The minute the new hour puts out of range is pulled back to the end of it.
        await Expect(Out("range-value")).ToHaveTextAsync("17:15:00");
        await Expect(Number(callout, "20")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Number(callout, "15")).Not.ToHaveAttributeAsync("aria-disabled", "true");

        await Dial(callout).FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Out("range-value")).ToHaveTextAsync("17:00:00");
        await Page.Keyboard.PressAsync("End");
        await Expect(Out("range-value")).ToHaveTextAsync("17:15:00");
    }

    [TestMethod]
    public async Task AllowedHoursAndMinutes_OnlyTheirValuesCanBePicked()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("range-allowed");
        await Expect(callout.Locator(".bit-ctp-num:not(.bit-ctp-dis)")).ToHaveCountAsync(9);
        await Number(callout, "20").ClickAsync(new() { Force = true });
        await Expect(Out("range-allowed-value")).ToHaveTextAsync("09:00:00");

        await Dial(callout).FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Out("range-allowed-value")).ToHaveTextAsync("17:00:00");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(MinuteButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(callout.Locator(".bit-ctp-num:not(.bit-ctp-dis)")).ToHaveCountAsync(2);
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("range-allowed-value")).ToHaveTextAsync("17:30:00");
    }

    [TestMethod]
    public async Task AllowedHoursAcrossMidnight_AndAllowedSeconds()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("range-night");
        await Dial(callout).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("range-night-value")).ToHaveTextAsync("00:00:00");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Out("range-night-value")).ToHaveTextAsync("22:00:00");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Out("range-night-value")).ToHaveTextAsync("06:00:00");

        await SecondButton(callout).ClickAsync();
        await Expect(callout.Locator(".bit-ctp-num:not(.bit-ctp-dis)")).ToHaveCountAsync(3);
        await Dial(callout).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("range-night-value")).ToHaveTextAsync("06:00:20");
    }

    [TestMethod]
    public async Task DisablePastAndDisableFuture_RuleOutTheHoursOnTheWrongSideOfNow()
    {
        await OpenAsync(Url);

        var now = DateTime.Now;
        if (now.Hour is 0 or 23 || (now.Minute == 59 && now.Second > 45)) Assert.Inconclusive("Too close to the turn of an hour, or to the ends of the day, to tell the sides of now apart.");

        var past = StandaloneCallout("range-past");
        var future = StandaloneCallout("range-future");
        var before = (now.Hour - 1).ToString("D2", CultureInfo.InvariantCulture);
        var after = (now.Hour + 1).ToString("D2", CultureInfo.InvariantCulture);

        await Expect(Number(past, before)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Number(past, after)).Not.ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Number(future, after)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Number(future, before)).Not.ToHaveAttributeAsync("aria-disabled", "true");
    }

    // ------------------------------------------------------------------ actions

    [TestMethod]
    public async Task NowAndClear_SetTheCurrentTimeAndTakeItBack()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("actions");
        await Expect(Action(callout, "Reset")).ToBeDisabledAsync();

        var before = DateTime.Now;
        await Action(callout, "Right now").ClickAsync();
        await Expect(Out("actions-value")).Not.ToHaveTextAsync("null");
        var after = DateTime.Now;
        var shown = TimeSpan.Parse((await Out("actions-value").TextContentAsync())!, CultureInfo.InvariantCulture);
        Assert.IsTrue(shown == new TimeSpan(before.Hour, before.Minute, 0) || shown == new TimeSpan(after.Hour, after.Minute, 0),
            $"The now button set {shown}, the local time is {before:HH:mm}.");
        await Expect(callout).ToBeVisibleAsync();

        await Action(callout, "Reset").ClickAsync();
        await Expect(Out("actions-value")).ToHaveTextAsync("null");
        await Expect(Out("actions-clear-count")).ToHaveTextAsync("1");
        await Expect(HourButton(callout)).ToHaveTextAsync("--");
        // The clear button disables itself, so the focus is handed to the dial rather than dropped.
        await Expect(Dial(callout)).ToBeFocusedAsync();
        await Expect(HourButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task CloseButton_ClosesTheCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("close-button");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-ctp-wcb"));
        var close = callout.Locator(".bit-ctp-cbn");
        await Expect(close).ToHaveAttributeAsync("title", "Dismiss the dial");
        await Expect(close).ToHaveAttributeAsync("aria-label", "Dismiss the dial");
        await Expect(close.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));

        await close.ClickAsync();
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("close-button")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AutoClose_ClosesOnceTheLastPartIsPicked()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("autoclose");
        await Number(callout, "16").ClickAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Number(callout, "20").ClickAsync();

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Out("autoclose-value")).ToHaveTextAsync("16:20:00");
        await Expect(Out("autoclose-closes")).ToHaveTextAsync("1");
        await Expect(Field("autoclose")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AutoCloseDelay_LetsThePickBeSeenBeforeClosing()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("autoclose-delay");
        await Number(callout, "16").ClickAsync();
        await Number(callout, "20").ClickAsync();
        await Expect(Out("autoclose-delay-value")).ToHaveTextAsync("16:20:00");

        // Still open right after the pick, closed once the delay has run out.
        await Expect(callout).ToBeVisibleAsync();
        await Expect(MinuteButton(callout)).ToHaveTextAsync("20");
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task AutoClose_TheNowButtonCompletesTheSelection()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("autoclose-now");
        await Action(callout, "Now").ClickAsync();
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Out("autoclose-now-value")).Not.ToHaveTextAsync("null");
    }

    // ------------------------------------------------------------------ open state

    [TestMethod]
    public async Task IsOpen_BindsBothWays_AndOpenCalloutDismissCalloutDriveIt()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-open");
        var callout = await CalloutOfAsync("programmatic");
        await Expect(Out("is-open")).ToHaveTextAsync("True");
        await Expect(Out("prog-opens")).ToHaveTextAsync("1");
        await Expect(Dial(callout)).ToBeFocusedAsync();

        await Page.Locator("#btn-dismiss").DispatchEventAsync("click");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Out("is-open")).ToHaveTextAsync("False");
        await Expect(Out("prog-closes")).ToHaveTextAsync("1");
        await Expect(Field("programmatic")).Not.ToBeFocusedAsync();

        await Page.ClickAsync("#btn-set-open");
        await CalloutOfAsync("programmatic");
        await Expect(Out("prog-opens")).ToHaveTextAsync("2");
        await Expect(Dial(callout)).ToBeFocusedAsync();

        await Page.Locator("#btn-set-closed").DispatchEventAsync("click");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Out("prog-closes")).ToHaveTextAsync("2");

        await Page.ClickAsync("#btn-focus");
        await Expect(Field("programmatic")).ToBeFocusedAsync();
        await Page.ClickAsync("#btn-read");
        await Expect(Out("input-id")).ToHaveTextAsync((await Field("programmatic").GetAttributeAsync("id"))!);
    }

    [TestMethod]
    public async Task IsOpen_InitiallyTrue_OpensTheCalloutOnceInteractive()
    {
        await OpenAsync("/e2e/circular-time-picker/setup?mode=open");

        var callout = await CalloutOfAsync("setup");
        await Expect(Out("is-open")).ToHaveTextAsync("True");
        await Number(callout, "09").ClickAsync();
        await Expect(Field("setup")).ToHaveValueAsync("09:00");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Out("is-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task AutoFocus_FocusesTheField_OrTheDialOfAStandalonePicker()
    {
        await OpenAsync("/e2e/circular-time-picker/setup?mode=autofocus");
        await Expect(Field("setup")).ToBeFocusedAsync();
        await Expect(Out("focus-count")).ToHaveTextAsync("1");

        await OpenAsync("/e2e/circular-time-picker/setup?mode=autofocus-standalone");
        await Expect(Dial(StandaloneCallout("setup"))).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task DropDirectionAll_OpensTheCalloutInsideTheViewport()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("drop-all");
        await SettleAsync(callout);
        var box = (await BoxAsync(callout))!;
        var viewport = Page.ViewportSize!;
        Assert.IsTrue(box.X >= 0 && box.Y >= 0 && box.X + box.Width <= viewport.Width && box.Y + box.Height <= viewport.Height);
    }

    // ------------------------------------------------------------------ text input & value format

    [TestMethod]
    public async Task ValueFormat_WritesTheFieldInTheGivenPattern()
    {
        await OpenAsync(Url);

        await Expect(Field("value-format")).ToHaveValueAsync("02-05 PM");
    }

    [TestMethod]
    public async Task AllowTextInput_ReadsATypedTime_AndRefusesText()
    {
        await OpenAsync(Url);

        var field = Field("text-input");
        await Expect(field).Not.ToHaveAttributeAsync("readonly", "");
        await Expect(field).ToHaveClassAsync(HasClass("bit-ctp-ein"));

        await field.FillAsync("13:45");
        await Expect(Out("text-input-value")).ToHaveTextAsync("13:45:00");

        await field.FillAsync("bogus");
        await Expect(field).ToHaveValueAsync("bogus");
        await Expect(Out("text-input-value")).ToHaveTextAsync("13:45:00");

        await field.FillAsync("");
        await Expect(Out("text-input-value")).ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task AllowTextInput_OnlyArrowDownOpens_AndTheFieldKeepsTheFocus()
    {
        await OpenAsync(Url);

        var field = Field("text-input-culture");
        await field.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Out("text-opens")).ToHaveTextAsync("0");

        await Page.Keyboard.PressAsync("ArrowDown");
        var callout = await CalloutOfAsync("text-input-culture");
        await Expect(field).ToBeFocusedAsync();
        await Expect(Out("text-opens")).ToHaveTextAsync("1");

        // A typed time is read in the clock format of the picker, with or without its leading zeros.
        await field.FillAsync("9:05");
        await Expect(Out("text-input-culture-value")).ToHaveTextAsync("09:05:00");
        await field.FillAsync("14:30");
        await Expect(Out("text-input-culture-value")).ToHaveTextAsync("14:30:00");
        await Expect(HourButton(callout)).ToHaveTextAsync("14");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Out("text-closes")).ToHaveTextAsync("1");
    }

    // ------------------------------------------------------------------ binding

    [TestMethod]
    public async Task Binding_FlowsBothWays()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("binding");
        await Number(callout, "20").ClickAsync();
        await Expect(Out("binding-value")).ToHaveTextAsync("20:00:00");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Field("binding")).ToHaveValueAsync("20:00");

        await Page.ClickAsync("#btn-set-bound");
        await Expect(Field("binding")).ToHaveValueAsync("07:45");
        callout = await OpenCalloutAsync("binding");
        await Expect(HourButton(callout)).ToHaveTextAsync("07");
        await Expect(MinuteButton(callout)).ToHaveTextAsync("45");
        await Page.Keyboard.PressAsync("Escape");

        await Page.ClickAsync("#btn-clear-bound");
        await Expect(Field("binding")).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task DefaultValue_AndOnChange_WorkWithoutABinding()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("uncontrolled");
        await Expect(HourButton(callout)).ToHaveTextAsync("07");
        await Number(callout, "19").ClickAsync();
        await Expect(Out("uncontrolled-value")).ToHaveTextAsync("19:30:00");
    }

    [TestMethod]
    public async Task StartingValue_LeavesThePickerEmptyButSeedsTheFirstChange()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("starting-value");
        await Expect(HourButton(callout)).ToHaveTextAsync("--");

        await Dial(callout).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("starting-value-value")).ToHaveTextAsync("10:30:00");

        await Action(callout, "Clear").ClickAsync();
        await Expect(Out("starting-value-value")).ToHaveTextAsync("null");
        await Expect(HourButton(callout)).ToHaveTextAsync("--");
    }

    [TestMethod]
    public async Task OneWayValue_WithoutAChangeHandler_IsNotInteractive()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("one-way");
        await Number(callout, "15").ClickAsync();
        await Dial(callout).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Number(callout, "10")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page.Locator("#one-way input.bit-input-hidden")).ToHaveValueAsync("10:10");
    }

    // ------------------------------------------------------------------ culture

    [TestMethod]
    public async Task Culture_FaIr_WritesItsDesignators()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("culture-fa");
        var am = (await Out("fa-am").TextContentAsync())!.Trim();
        var pm = (await Out("fa-pm").TextContentAsync())!.Trim();
        await Expect(Am(callout)).ToHaveTextAsync(am);
        await Expect(Pm(callout)).ToHaveTextAsync(pm);
        await Expect(Pm(callout)).ToHaveAttributeAsync("aria-pressed", "true");

        // The plain "a" is a meridiem key whatever the culture.
        await Dial(callout).FocusAsync();
        await Page.Keyboard.PressAsync("a");
        await Expect(Out("culture-fa-value")).ToHaveTextAsync("02:30:00");
    }

    // ------------------------------------------------------------------ read-only

    [TestMethod]
    public async Task ReadOnly_ShowsTheTimeButTakesNoChange()
    {
        await OpenAsync(Url);

        await Expect(Field("readonly")).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(Root("readonly")).ToHaveClassAsync(HasClass("bit-ctp-rdl"));

        var callout = await OpenCalloutAsync("readonly");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-ctp-rdl"));
        await Expect(Dial(callout)).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(Action(callout, "Now")).ToBeDisabledAsync();
        await Expect(Am(callout)).ToBeDisabledAsync();

        await Number(callout, "3").ClickAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("p");
        await Expect(Out("readonly-value")).ToHaveTextAsync("10:10:00");
        // The toolbar still switches between the parts, which changes nothing.
        await MinuteButton(callout).ClickAsync();
        await Expect(MinuteButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Field("readonly-text")).ToHaveAttributeAsync("readonly", "");

        var standalone = StandaloneCallout("readonly-standalone");
        await Dial(standalone).FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Out("readonly-value")).ToHaveTextAsync("10:10:00");
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
        await Field("form-default").FillAsync("zz");
        await Expect(Root("form-default")).ToHaveClassAsync(HasClass("bit-inv"));
    }

    // ------------------------------------------------------------------ appearance

    [TestMethod]
    public async Task UnderlinedNoBorderAndIconPlacement_ChangeTheFrame()
    {
        await OpenAsync(Url);

        await Expect(Root("underlined")).ToHaveClassAsync(HasClass("bit-ctp-und"));
        Assert.AreEqual("none", await StyleOfAsync(Page.Locator("#underlined .bit-ctp-icn"), "border-top-style"));
        Assert.AreEqual("solid", await StyleOfAsync(Page.Locator("#underlined .bit-ctp-wrp"), "border-bottom-style"));

        await Expect(Root("no-border")).ToHaveClassAsync(HasClass("bit-ctp-nbd"));
        Assert.AreEqual("none", await StyleOfAsync(Page.Locator("#no-border .bit-ctp-icn"), "border-bottom-style"));
        Assert.AreEqual("solid", await StyleOfAsync(Page.Locator("#basic .bit-ctp-icn"), "border-bottom-style"));

        await Expect(Root("icon-start")).ToHaveClassAsync(HasClass("bit-ctp-lic"));
        Assert.IsTrue((await BoxAsync(Page.Locator("#icon-start .bit-ctp-ico")))!.X < (await BoxAsync(Field("icon-start")))!.X);
        Assert.IsTrue((await BoxAsync(Page.Locator("#basic .bit-ctp-ico")))!.X > (await BoxAsync(Field("basic")))!.X);
    }

    // ------------------------------------------------------------------ templates

    [TestMethod]
    public async Task LabelTemplate_ReplacesTheLabel_AndNamesAStandaloneDial()
    {
        await OpenAsync(Url);

        var label = Page.Locator("#templates-label label");
        await Expect(label.Locator("#custom-label")).ToHaveTextAsync("Custom label");
        await Expect(label).ToHaveAttributeAsync("for", (await Field("templates-label").GetAttributeAsync("id"))!);

        var standaloneLabel = Page.Locator("#templates-label-standalone label");
        var dial = Dial(StandaloneCallout("templates-label-standalone"));
        var labelledBy = (await dial.GetAttributeAsync("aria-labelledby"))!.Split(' ');
        Assert.AreEqual(await standaloneLabel.GetAttributeAsync("id"), labelledBy[0]);
        await Expect(Page.Locator($"[id='{labelledBy[1]}']")).ToHaveTextAsync("Select hour");
        Assert.IsNull(await dial.GetAttributeAsync("aria-label"));

        await standaloneLabel.ClickAsync();
        await Expect(dial).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task IconTemplate_ReplacesTheIcon()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#templates-icon #custom-icon")).ToHaveTextAsync("ICON");
        await Expect(Page.Locator("#templates-icon .bit-ctp-ico")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task CalloutHeaderAndFooterTemplates_FrameTheDial()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("templates-callout");
        await Expect(callout.Locator("#callout-header")).ToHaveTextAsync("Pick a time");
        // A header takes the corner the close button would otherwise share with the toolbar.
        await Expect(callout).Not.ToHaveClassAsync(HasClass("bit-ctp-wcb"));

        await SettleAsync(callout);
        var header = (await BoxAsync(callout.Locator("#callout-header")))!;
        var toolbar = (await BoxAsync(callout.Locator(".bit-ctp-tlb")))!;
        var footer = (await BoxAsync(callout.Locator("#callout-footer")))!;
        Assert.IsTrue(header.Y < toolbar.Y && toolbar.Y < footer.Y);

        await callout.Locator("#preset-0815").ClickAsync();
        await Expect(Out("templates-callout-value")).ToHaveTextAsync("08:15:00");
        await Expect(HourButton(callout)).ToHaveTextAsync("08");
    }

    // ------------------------------------------------------------------ layout

    [TestMethod]
    public async Task Landscape_PutsTheToolbarBesideTheClock()
    {
        await OpenAsync(Url);

        var landscape = StandaloneCallout("landscape");
        await Expect(landscape).ToHaveClassAsync(HasClass("bit-ctp-lnd"));
        var toolbar = (await BoxAsync(landscape.Locator(".bit-ctp-tlb")))!;
        var clock = (await BoxAsync(landscape.Locator(".bit-ctp-clk")))!;
        Assert.IsTrue(toolbar.X + toolbar.Width <= clock.X + 1, "In landscape the toolbar is beside the clock.");
        await Expect(landscape.Locator(".bit-ctp-sep").First).ToBeHiddenAsync();
        // The parts stack down the column of the toolbar.
        Assert.IsTrue((await BoxAsync(MinuteButton(landscape)))!.Y > (await BoxAsync(HourButton(landscape)))!.Y);

        var portrait = StandaloneCallout("portrait");
        var portraitToolbar = (await BoxAsync(portrait.Locator(".bit-ctp-tlb")))!;
        var portraitClock = (await BoxAsync(portrait.Locator(".bit-ctp-clk")))!;
        Assert.IsTrue(portraitToolbar.Y + portraitToolbar.Height <= portraitClock.Y + 1, "In portrait the toolbar is above the clock.");

        var twelve = StandaloneCallout("landscape-12");
        Assert.IsTrue((await BoxAsync(SecondButton(twelve)))!.Y > (await BoxAsync(MinuteButton(twelve)))!.Y);
        Assert.IsTrue((await BoxAsync(twelve.Locator(".bit-ctp-tlb .bit-ctp-apc")))!.Y > (await BoxAsync(SecondButton(twelve)))!.Y);

        var callout = await OpenCalloutAsync("landscape-callout");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-ctp-lnd"));
        await SettleAsync(callout);
        Assert.IsTrue((await BoxAsync(callout.Locator(".bit-ctp-tlb")))!.X < (await BoxAsync(callout.Locator(".bit-ctp-clk")))!.X);
    }

    [TestMethod]
    public async Task Responsive_OnADesktop_OpensAsAnOrdinaryCallout()
    {
        await OpenAsync(Url);

        // The callout is taller than half the viewport, so the field is put at the top where there is room under it.
        await Page.Locator("#responsive").EvaluateAsync("el => el.scrollIntoView({ block: 'start' })");
        var callout = await OpenCalloutAsync("responsive");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-ctp-res"));
        await SettleAsync(callout);
        var field = (await BoxAsync(Page.Locator("#responsive .bit-ctp-icn")))!;
        var box = (await BoxAsync(callout))!;
        Assert.IsTrue(box.Y >= field.Y + field.Height - 2 || box.Y + box.Height <= field.Y + 2, "Above the phone breakpoint the callout opens against its field.");
        Assert.IsTrue(box.Width < Page.ViewportSize!.Width / 2f, "Above the phone breakpoint the callout is not a sheet.");
    }

    // ------------------------------------------------------------------ keyboard & accessibility

    [TestMethod]
    public async Task Keyboard_EnterSpaceAndArrowDownOpenTheField()
    {
        await OpenAsync(Url);

        var field = Field("keyboard");
        await field.FocusAsync();

        foreach (var key in new[] { "Enter", " ", "ArrowDown", "ArrowUp" })
        {
            await Page.Keyboard.PressAsync(key);
            var callout = await CalloutOfAsync("keyboard");
            await Expect(Dial(callout)).ToBeFocusedAsync();
            await Page.Keyboard.PressAsync("Escape");
            await Expect(callout).ToBeHiddenAsync();
            await Expect(field).ToBeFocusedAsync();
        }
    }

    [TestMethod]
    public async Task Keyboard_TheListboxKeysMoveTheDial_AndEnterSettlesEachView()
    {
        await OpenAsync(Url);

        var dial = Dial(StandaloneCallout("keyboard-24"));
        var value = Out("keyboard-24-value");
        await dial.FocusAsync();

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(value).ToHaveTextAsync("00:00:00");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(value).ToHaveTextAsync("02:00:00");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(value).ToHaveTextAsync("01:00:00");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(value).ToHaveTextAsync("23:00:00");
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(value).ToHaveTextAsync("04:00:00");
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(value).ToHaveTextAsync("23:00:00");
        await Page.Keyboard.PressAsync("Home");
        await Expect(value).ToHaveTextAsync("00:00:00");
        await Page.Keyboard.PressAsync("End");
        await Expect(value).ToHaveTextAsync("23:00:00");

        // Digits typed within a second of each other are one number.
        await Page.Keyboard.TypeAsync("14");
        await Expect(value).ToHaveTextAsync("14:00:00");
        await Expect(dial).ToHaveAttributeAsync("aria-activedescendant", (await Number(StandaloneCallout("keyboard-24"), "14").GetAttributeAsync("id"))!);

        await Page.Keyboard.PressAsync("Enter");
        await Expect(dial).ToHaveAttributeAsync("aria-label", "Keyboard 24 Select minute");
        await Page.Keyboard.TypeAsync("45");
        await Expect(value).ToHaveTextAsync("14:45:00");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(value).ToHaveTextAsync("14:46:00");
        // Between two marks the live region spells the value out.
        await Expect(StandaloneCallout("keyboard-24").Locator(".bit-ctp-lvr").First).ToHaveTextAsync("Select minute 46");

        // The keys move the dial, never the page.
        var scroll = await Page.EvaluateAsync<double>("() => window.scrollY");
        await Page.Keyboard.PressAsync("PageDown");
        await Page.Keyboard.PressAsync(" ");
        Assert.AreEqual(scroll, await Page.EvaluateAsync<double>("() => window.scrollY"));
        await Expect(value).ToHaveTextAsync("14:41:00");
    }

    [TestMethod]
    public async Task Keyboard_MeridiemLettersAndTheTwelveHourTypeAhead()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("keyboard");
        await Page.Keyboard.TypeAsync("9");
        await Expect(Out("keyboard-value")).ToHaveTextAsync("09:00:00");

        await Page.Keyboard.PressAsync("p");
        await Expect(Out("keyboard-value")).ToHaveTextAsync("21:00:00");
        await Expect(Pm(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Page.Keyboard.PressAsync("A");
        await Expect(Out("keyboard-value")).ToHaveTextAsync("09:00:00");

        // "1" then "3" is no hour of the 12-hour face, so the "3" starts a number of its own.
        await Page.Keyboard.TypeAsync("13");
        await Expect(Out("keyboard-value")).ToHaveTextAsync("03:00:00");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(MinuteButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(SecondButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Page.Keyboard.PressAsync("Home");
        await Expect(Out("keyboard-value")).ToHaveTextAsync("03:59:00");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(callout).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Field("keyboard")).ToHaveValueAsync("03:59:00 AM");
    }

    [TestMethod]
    public async Task Keyboard_TabIsHeldInsideTheOpenCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("keyboard");
        await Expect(Dial(callout)).ToBeFocusedAsync();

        var stops = new List<string>();
        for (var i = 0; i < 8; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            stops.Add(await Page.EvaluateAsync<string>("() => document.activeElement.className + '|' + (document.activeElement.getAttribute('aria-label') || document.activeElement.textContent.trim())"));
            await Expect(callout).ToBeVisibleAsync();
        }

        Assert.IsTrue(stops.All(s => s.Contains("bit-ctp", StringComparison.Ordinal)), $"The focus stays inside the callout: {string.Join(" / ", stops)}");
        Assert.IsTrue(stops.Any(s => s.Contains("bit-ctp-clf", StringComparison.Ordinal)), "Tabbing comes round to the dial again.");
        Assert.IsTrue(stops.Any(s => s.Contains("Meeting hour", StringComparison.Ordinal)));
        Assert.IsTrue(stops.Any(s => s.Contains("bit-ctp-abn", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Aria_TheFieldTheDialogTheToolbarAndTheDialAreNamed()
    {
        await OpenAsync(Url);

        await Expect(Field("keyboard")).ToHaveAttributeAsync("aria-label", "Meeting field");
        var callout = await OpenCalloutAsync("keyboard");
        await Expect(callout.Locator(".bit-ctp-cac")).ToHaveAttributeAsync("aria-label", "Pick a meeting time");
        await Expect(HourButton(callout)).ToHaveAttributeAsync("title", "Meeting hour");
        await Expect(HourButton(callout)).ToHaveAttributeAsync("aria-label", "Meeting hour");
        await Expect(MinuteButton(callout)).ToHaveAttributeAsync("title", "Meeting minute");
        await Expect(SecondButton(callout)).ToHaveAttributeAsync("title", "Meeting second");
        await Expect(Dial(callout)).ToHaveAttributeAsync("aria-label", "Meeting hour");
        await Expect(callout.Locator(".bit-ctp-lvr").First).ToHaveTextAsync("Meeting hour");

        await Page.Keyboard.TypeAsync("7");
        await Expect(HourButton(callout)).ToHaveAttributeAsync("aria-label", "Meeting hour 07");
        await Expect(Number(callout, "7")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Number(callout, "8")).ToHaveAttributeAsync("aria-selected", "false");

        await MinuteButton(callout).ClickAsync();
        await Expect(Dial(callout)).ToHaveAttributeAsync("aria-label", "Meeting minute");
        await Expect(callout.Locator(".bit-ctp-lvr").First).ToHaveTextAsync("Meeting minute");
        await Expect(callout.Locator(".bit-ctp-pin")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(callout.Locator(".bit-ctp-sep").First).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [TestMethod]
    public async Task Wheel_ShiftWheelOverTheFocusedDialStepsIt_AndAHoveredOneIgnoresIt()
    {
        await OpenAsync(Url);

        var dial = Dial(StandaloneCallout("wheel"));
        await Expect(dial).ToHaveAttributeAsync("data-bit-wheel", "1");

        // Hovered only: nothing.
        await dial.HoverAsync();
        await Page.Keyboard.DownAsync("Shift");
        await Page.Mouse.WheelAsync(0, -100);
        await Page.Keyboard.UpAsync("Shift");

        await dial.FocusAsync();
        await Page.Keyboard.DownAsync("Shift");
        await Page.Mouse.WheelAsync(0, -100);
        await Expect(Out("wheel-value")).ToHaveTextAsync("11:30:00");
        await Page.Mouse.WheelAsync(0, 100);
        await Page.Mouse.WheelAsync(0, 100);
        await Expect(Out("wheel-value")).ToHaveTextAsync("09:30:00");
        await Page.Keyboard.UpAsync("Shift");

        // Without Shift the wheel is the page's.
        await Page.Mouse.WheelAsync(0, -100);
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("wheel-value")).ToHaveTextAsync("10:30:00");
    }

    [TestMethod]
    public async Task NoMouseWheel_AndInvertMouseWheel()
    {
        await OpenAsync(Url);

        var off = Dial(StandaloneCallout("wheel-off"));
        Assert.IsNull(await off.GetAttributeAsync("data-bit-wheel"));
        await off.FocusAsync();
        await off.HoverAsync();
        await Page.Keyboard.DownAsync("Shift");
        await Page.Mouse.WheelAsync(0, -100);
        await Page.Keyboard.UpAsync("Shift");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Out("wheel-off-value")).ToHaveTextAsync("09:30:00");

        var invert = Dial(StandaloneCallout("wheel-invert"));
        await invert.FocusAsync();
        await invert.HoverAsync();
        await Page.Keyboard.DownAsync("Shift");
        await Page.Mouse.WheelAsync(0, -100);
        await Page.Keyboard.UpAsync("Shift");
        await Expect(Out("wheel-invert-value")).ToHaveTextAsync("09:30:00");
    }

    // ------------------------------------------------------------------ validation

    [TestMethod]
    public async Task EditForm_RequiredAndTypedRejections_ShowTheirMessages()
    {
        await OpenAsync(Url);

        var field = Field("form");
        var message = Page.Locator("#form-message");

        await Page.ClickAsync("#form-submit");
        await WaitForStatusAsync("Rejected 1");
        await Expect(message).ToHaveTextAsync("The time is required.");
        await Expect(Root("form")).ToHaveClassAsync(HasClass("bit-inv"));

        await field.FillAsync("xx");
        await Expect(message).ToContainTextAsync("That is not a time - write it as HH:mm.");
        await field.FillAsync("19:00");
        await Expect(message).ToContainTextAsync("Please enter a time between 08:00 and 18:00.");
        await field.FillAsync("09:10");
        await Expect(message).ToContainTextAsync("Appointments start on the quarter hour.");

        await field.FillAsync("09:15");
        await Expect(message).ToHaveTextAsync("");
        await Expect(Root("form")).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await Page.ClickAsync("#form-submit");
        await WaitForStatusAsync("Saved 2");
    }

    [TestMethod]
    public async Task EditForm_DefaultMessages_NameTheField()
    {
        await OpenAsync(Url);

        var field = Field("form-default");
        var message = Page.Locator("#form2-message");
        await field.FillAsync("zz");
        await Expect(message).ToContainTextAsync("The Arrival field is not valid.");
        await field.FillAsync("07:00");
        await Expect(message).ToContainTextAsync("The Arrival field is out of the allowed range.");
        await field.FillAsync("12:15");
        await Expect(message).ToContainTextAsync("The Arrival field is not an allowed time.");
        await field.FillAsync("13:15");
        await Expect(message).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task EditForm_Standalone_ValidatesThroughTheDial()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("form-standalone");
        await Expect(Dial(callout)).ToHaveAttributeAsync("aria-required", "true");

        await Page.ClickAsync("#form3-submit");
        await Expect(Out("form3-status")).ToHaveTextAsync(new Regex("^Rejected"));
        await Expect(Page.Locator("#form3-message")).ToHaveTextAsync("The time is required.");
        await Expect(Dial(callout)).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-inv"));

        await Number(callout, "10").ClickAsync();
        await Expect(Page.Locator("#form3-message")).ToHaveTextAsync("");
        await Expect(callout).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await Page.ClickAsync("#form3-submit");
        await Expect(Out("form3-status")).ToHaveTextAsync(new Regex("^Saved"));
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
        await Expect(Out("ev-focusout")).ToHaveTextAsync("1");

        await Number(callout, "06").ClickAsync();
        await Expect(Out("ev-select")).ToHaveTextAsync("1");
        await Expect(Out("ev-select-arg")).ToHaveTextAsync("06:00:00");
        await Expect(Out("ev-change")).ToHaveTextAsync("1");
        await Expect(Out("ev-change-arg")).ToHaveTextAsync("06:00:00");
        await Expect(Out("ev-view")).ToHaveTextAsync("1");
        await Expect(Out("ev-view-arg")).ToHaveTextAsync("Minute");

        await Action(callout, "Clear").ClickAsync();
        await Expect(Out("ev-clear")).ToHaveTextAsync("1");
        await Expect(Out("ev-select")).ToHaveTextAsync("2");
        await Expect(Out("ev-select-arg")).ToHaveTextAsync("null");
        await Expect(Out("ev-change-arg")).ToHaveTextAsync("null");
        await Expect(Out("ev-view")).ToHaveTextAsync("2");
        await Expect(Out("ev-view-arg")).ToHaveTextAsync("Hour");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Out("ev-close")).ToHaveTextAsync("1");
        await Expect(Out("ev-focus")).ToHaveTextAsync("2");

        await Page.Keyboard.PressAsync("Escape");
        await Page.ClickAsync("#events-outside");
        await Expect(Out("ev-focusout")).ToHaveTextAsync("2");
        await Expect(Out("ev-close")).ToHaveTextAsync("1");
        await Expect(Out("ev-open")).ToHaveTextAsync("1");
    }

    // ------------------------------------------------------------------ cascading parameters

    [TestMethod]
    public async Task BitParams_CascadesItsDefaults_AndAPickerOwnParameterWins()
    {
        await OpenAsync(Url);

        await Expect(Field("cascade-1")).ToHaveAttributeAsync("placeholder", "Cascaded placeholder");
        await Expect(Field("cascade-2")).ToHaveAttributeAsync("placeholder", "Cascaded placeholder");
        await Expect(Root("cascade-1")).ToHaveClassAsync(HasClass("bit-ctp-suc"));
        await Expect(Root("cascade-3")).ToHaveClassAsync(HasClass("bit-ctp-sec"));

        var callout = await OpenCalloutAsync("cascade-1");
        await Expect(MinuteButton(callout)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(callout.Locator(".bit-ctp-num.bit-ctp-dis")).ToHaveCountAsync(8);
        await Expect(Action(callout, "Cascaded now")).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-ctp-apb")).ToHaveCountAsync(2);
        await Page.Keyboard.PressAsync("Escape");

        callout = await OpenCalloutAsync("cascade-3");
        await Expect(callout.Locator(".bit-ctp-num.bit-ctp-dis")).ToHaveCountAsync(0);
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Out("cascade-3-value")).ToHaveTextAsync("00:01:00");
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
    public async Task Color_EachValue_PaintsTheToolbarTheSelectionAndTheHand(string name, string suffix, string token)
    {
        await OpenAsync(Url);

        var scope = $"color-{name}";
        var callout = StandaloneCallout(scope);
        await Expect(Root(scope)).ToHaveClassAsync(HasClass($"bit-ctp-{suffix}"));
        await Expect(callout).ToHaveClassAsync(HasClass($"bit-ctp-{suffix}"));

        var expected = await ResolveColorAsync($"var({token})");
        Assert.AreEqual(expected, await StyleOfAsync(callout.Locator(".bit-ctp-tlb"), "background-color"), $"toolbar of {name}");
        Assert.AreEqual(expected, await StyleOfAsync(callout.Locator(".bit-ctp-sel"), "background-color"), $"selected number of {name}");
        Assert.AreEqual(expected, await StyleOfAsync(callout.Locator(".bit-ctp-ptr"), "background-color"), $"hand of {name}");
    }

    [TestMethod]
    public async Task Color_Unset_FallsBackToPrimary()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("color-none");
        await Expect(Root("color-none")).Not.ToHaveClassAsync(new Regex(@"bit-ctp-(pri|sec|ter|inf|suc|wrn|swr|err|pbg|sbg|tbg|pfg|sfg|tfg|pbr|sbr|tbr)(\s|$)"));
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-pri)"), await StyleOfAsync(callout.Locator(".bit-ctp-tlb"), "background-color"));
    }

    // ------------------------------------------------------------------ icons

    [TestMethod]
    public async Task ExternalIcons_RenderTheClassesOfTheirLibrary()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#ext-string .bit-ctp-ico")).ToHaveClassAsync(new Regex(@"(^|\s)fa-solid fa-clock(\s|$)"));
        await Expect(Page.Locator("#ext-css .bit-ctp-ico")).ToHaveClassAsync(new Regex(@"(^|\s)bi bi-heart-fill(\s|$)"));
        await Expect(Page.Locator("#ext-fa .bit-ctp-ico")).ToHaveClassAsync(new Regex(@"(^|\s)fa-solid fa-clock(\s|$)"));
        await Expect(Page.Locator("#ext-bi .bit-ctp-ico")).ToHaveClassAsync(new Regex(@"(^|\s)bi bi-clock-fill(\s|$)"));

        var fa = await OpenCalloutAsync("ext-fa");
        await Expect(fa.Locator(".bit-ctp-cbi")).ToHaveClassAsync(new Regex(@"(^|\s)fa-solid fa-xmark(\s|$)"));
        await Page.Keyboard.PressAsync("Escape");

        var bi = await OpenCalloutAsync("ext-bi");
        await Expect(bi.Locator(".bit-ctp-cbi")).ToHaveClassAsync(new Regex(@"(^|\s)bi bi-x-lg(\s|$)"));
        await Page.Keyboard.PressAsync("Escape");

        var named = await OpenCalloutAsync("ext-close-name");
        await Expect(named.Locator(".bit-ctp-cbi")).ToHaveClassAsync(HasClass("bit-icon--ChromeClose"));
    }

    // ------------------------------------------------------------------ size

    [TestMethod]
    public async Task Size_EachValue_SetsItsClassAndScalesTheFieldAndTheDial()
    {
        await OpenAsync(Url);

        var heights = new List<float>();
        var clocks = new List<float>();
        foreach (var (scope, css) in new[] { ("small", "bit-ctp-sm"), ("medium", "bit-ctp-md"), ("large", "bit-ctp-lg") })
        {
            await Expect(Root($"size-{scope}")).ToHaveClassAsync(HasClass(css));
            heights.Add(Px(await StyleOfAsync(Page.Locator($"#size-{scope} .bit-ctp-icn"), "height")));
            var standalone = StandaloneCallout($"size-{scope}-standalone");
            await Expect(standalone).ToHaveClassAsync(HasClass(css));
            clocks.Add((await BoxAsync(standalone.Locator(".bit-ctp-clk")))!.Width);
        }

        Assert.IsTrue(heights[0] < heights[1] && heights[1] < heights[2], $"Field heights grow: {string.Join(", ", heights)}");
        Assert.IsTrue(clocks[0] < clocks[1] && clocks[1] < clocks[2], $"Dials grow: {string.Join(", ", clocks)}");
        Assert.AreEqual(heights[1], Px(await StyleOfAsync(Page.Locator("#basic .bit-ctp-icn"), "height")), "The medium size is what an unset one stands for.");
    }

    // ------------------------------------------------------------------ style & class

    [TestMethod]
    public async Task StyleAndClass_ApplyToTheRoot()
    {
        await OpenAsync(Url);

        Assert.AreEqual("7px", await StyleOfAsync(Root("sc-style"), "margin-top"));
        StringAssert.Contains(await StyleOfAsync(Root("sc-style"), "box-shadow"), "rgb(30, 144, 255)");
        await Expect(Root("sc-class")).ToHaveClassAsync(HasClass("custom-class"));
    }

    [TestMethod]
    public async Task Styles_ReachEveryPart()
    {
        await OpenAsync(Url);

        Assert.AreEqual("11px", await StyleOfAsync(Root("sc-styles"), "margin-left"));
        Assert.AreEqual("rgb(1, 1, 1)", await ComputedStyleAsync("#sc-styles .bit-ctp-lbl", "color"));
        Assert.AreEqual("3px", await ComputedStyleAsync("#sc-styles .bit-ctp-wrp", "padding-top"));
        Assert.AreEqual("rgb(2, 2, 2)", await ComputedStyleAsync("#sc-styles .bit-ctp-icn", "background-color"));
        Assert.AreEqual("9px", await ComputedStyleAsync("#sc-styles .bit-ctp-inp", "padding-left"));
        Assert.AreEqual("rgb(3, 3, 3)", await ComputedStyleAsync("#sc-styles .bit-ctp-ico", "color"));

        var callout = await OpenCalloutAsync("sc-styles");
        StringAssert.Contains(await StyleOfAsync(Root("sc-styles"), "outline"), "rgb(255, 0, 0)");

        var expected = new (string Selector, string Property, string Value)[]
        {
            (":scope", "min-width", "333px"),
            (".bit-ctp-cac", "padding-top", "6px"),
            (".bit-ctp-tlb", "padding-bottom", "13px"),
            (".bit-ctp-bdy", "padding-top", "4px"),
            (".bit-ctp-hmc", "padding-top", "2px"),
            (".bit-ctp-hmc button >> nth=0", "color", "rgb(4, 4, 4)"),
            (".bit-ctp-hmc button >> nth=1", "color", "rgb(5, 5, 5)"),
            (".bit-ctp-hmc button >> nth=2", "color", "rgb(6, 6, 6)"),
            (".bit-ctp-sep >> nth=0", "color", "rgb(7, 7, 7)"),
            (".bit-ctp-apc", "padding-top", "5px"),
            (".bit-ctp-apb >> nth=0", "color", "rgb(8, 8, 8)"),
            (".bit-ctp-apb >> nth=1", "color", "rgb(9, 9, 9)"),
            (".bit-ctp-apb >> nth=0", "text-decoration-line", "underline"),
            (".bit-ctp-hmc button >> nth=0", "text-decoration-line", "underline"),
            (".bit-ctp-clk", "background-color", "rgb(10, 10, 10)"),
            (".bit-ctp-clf", "padding-top", "1px"),
            (".bit-ctp-pin", "background-color", "rgb(11, 11, 11)"),
            (".bit-ctp-num >> nth=0", "color", "rgb(12, 12, 12)"),
            (".bit-ctp-sel", "background-color", "rgb(13, 13, 13)"),
            (".bit-ctp-ptr", "background-color", "rgb(14, 14, 14)"),
            (".bit-ctp-pth", "border-top-color", "rgb(15, 15, 15)"),
            (".bit-ctp-cbn", "color", "rgb(17, 17, 17)"),
            (".bit-ctp-cbi", "color", "rgb(18, 18, 18)"),
            (".bit-ctp-act", "padding-top", "7px"),
            (".bit-ctp-abn >> nth=0", "color", "rgb(19, 19, 19)"),
            (".bit-ctp-abn >> nth=1", "color", "rgb(20, 20, 20)"),
        };

        foreach (var (selector, property, value) in expected)
        {
            var target = selector == ":scope" ? callout : callout.Locator(selector);
            Assert.AreEqual(value, await StyleOfAsync(target, property), $"{selector} {property}");
        }

        // The minute hand between marks takes the minute style of the thumb.
        await MinuteButton(callout).ClickAsync();
        Assert.AreEqual("rgb(16, 16, 16)", await StyleOfAsync(callout.Locator(".bit-ctp-ptm"), "color"));

        Assert.AreEqual("rgba(0, 0, 0, 0.1)", await StyleOfAsync(Page.Locator(".bit-ctp-ovl").Filter(new() { Visible = true }), "background-color"));
    }

    [TestMethod]
    public async Task Styles_ReachTheSingleModeTextAndTheHeaderAndFooter()
    {
        await OpenAsync(Url);

        var text = StandaloneCallout("sc-hm-text").Locator(".bit-ctp-hmc > span");
        await Expect(text).ToHaveClassAsync(HasClass("c-hm-text"));
        Assert.AreEqual("rgb(21, 21, 21)", await StyleOfAsync(text, "color"));

        var callout = StandaloneCallout("sc-header-footer");
        var header = callout.Locator(".bit-ctp-cac > div.c-header");
        var footer = callout.Locator(".bit-ctp-cac > div.c-footer");
        await Expect(header).ToHaveTextAsync("Header");
        await Expect(footer).ToHaveTextAsync("Footer");
        Assert.AreEqual("rgb(22, 22, 22)", await StyleOfAsync(header, "color"));
        Assert.AreEqual("rgb(23, 23, 23)", await StyleOfAsync(footer, "color"));
    }

    [TestMethod]
    public async Task Classes_ReachEveryPart()
    {
        await OpenAsync(Url);

        await Expect(Root("sc-classes")).ToHaveClassAsync(HasClass("c-root"));
        foreach (var (selector, css) in new[]
                 {
                     (".bit-ctp-lbl", "c-label"), (".bit-ctp-wrp", "c-input-wrapper"), (".bit-ctp-icn", "c-input-container"),
                     (".bit-ctp-inp", "c-input"), (".bit-ctp-ico", "c-icon"), (".bit-ctp-ovl", "c-overlay")
                 })
        {
            await Expect(Page.Locator($"#sc-classes {selector}")).ToHaveClassAsync(HasClass(css));
        }

        var callout = await OpenCalloutAsync("sc-classes");
        await Expect(Root("sc-classes")).ToHaveClassAsync(HasClass("c-focused"));
        await Expect(callout).ToHaveClassAsync(HasClass("c-callout"));

        foreach (var (selector, css) in new[]
                 {
                     (".bit-ctp-cac", "c-callout-container"), (".bit-ctp-tlb", "c-toolbar"), (".bit-ctp-bdy", "c-body"), (".bit-ctp-hmc", "c-hm"),
                     (".bit-ctp-hmc button >> nth=0", "c-hour"), (".bit-ctp-hmc button >> nth=0", "c-selected"), (".bit-ctp-hmc button >> nth=1", "c-minute"),
                     (".bit-ctp-hmc button >> nth=2", "c-second"), (".bit-ctp-sep >> nth=0", "c-sep"), (".bit-ctp-apc", "c-ampm"),
                     (".bit-ctp-apb >> nth=0", "c-am"), (".bit-ctp-apb >> nth=0", "c-selected"), (".bit-ctp-apb >> nth=1", "c-pm"),
                     (".bit-ctp-clk", "c-clock"), (".bit-ctp-clf", "c-face"), (".bit-ctp-pin", "c-pin"), (".bit-ctp-num >> nth=0", "c-number"),
                     (".bit-ctp-sel", "c-selected-number"), (".bit-ctp-dis >> nth=0", "c-disabled-number"), (".bit-ctp-ptr", "c-pointer"),
                     (".bit-ctp-pth", "c-thumb"), (".bit-ctp-cbn", "c-close"), (".bit-ctp-cbi", "c-close-icon"), (".bit-ctp-act", "c-actions"),
                     (".bit-ctp-abn >> nth=0", "c-now"), (".bit-ctp-abn >> nth=1", "c-clear")
                 })
        {
            await Expect(callout.Locator(selector)).ToHaveClassAsync(HasClass(css));
        }

        await MinuteButton(callout).ClickAsync();
        await Dial(callout).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(callout.Locator(".bit-ctp-ptm")).ToHaveClassAsync(HasClass("c-thumb-minute"));
        await Expect(MinuteButton(callout)).ToHaveClassAsync(HasClass("c-selected"));
    }

    // ------------------------------------------------------------------ css variables

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_ReachTheFieldAndTheRelocatedCallout()
    {
        await OpenAsync(Url);

        var frame = Page.Locator("#css-vars-default .bit-ctp-icn");
        Assert.AreEqual("20px", await StyleOfAsync(frame, "border-top-left-radius"));
        Assert.AreEqual("50px", await StyleOfAsync(frame, "height"));
        Assert.AreEqual("rgb(1, 2, 3)", await StyleOfAsync(Page.Locator("#css-vars-default .bit-ctp-ico"), "color"));
        Assert.AreEqual("rgb(4, 5, 6)", await StyleOfAsync(Page.Locator("#css-vars-default .bit-ctp-lbl"), "color"));

        var callout = await OpenCalloutAsync("css-vars-default");
        await SettleAsync(callout);
        Assert.AreEqual("24px", await StyleOfAsync(callout, "border-top-left-radius"));
        Assert.AreEqual(300, (await BoxAsync(callout.Locator(".bit-ctp-clk")))!.Width, 1);
        Assert.AreEqual("rgb(7, 8, 9)", await StyleOfAsync(callout.Locator(".bit-ctp-clk"), "background-color"));
        Assert.AreEqual("rgb(10, 11, 12)", await StyleOfAsync(callout.Locator(".bit-ctp-num:not(.bit-ctp-sel)").First, "color"));
        Assert.AreEqual("rgb(0, 128, 128)", await StyleOfAsync(callout.Locator(".bit-ctp-tlb"), "background-color"));
        Assert.AreEqual("rgb(0, 128, 128)", await StyleOfAsync(callout.Locator(".bit-ctp-sel"), "background-color"));

        // A standalone dial shares the root's ancestors, so the ancestor's values reach it as well.
        var standalone = StandaloneCallout("css-vars-standalone");
        Assert.AreEqual(300, (await BoxAsync(standalone.Locator(".bit-ctp-clk")))!.Width, 1);
    }

    [TestMethod]
    public async Task CssVariables_AnExplicitColorAndSizeWinOverThem()
    {
        await OpenAsync(Url);

        Assert.AreNotEqual("50px", await StyleOfAsync(Page.Locator("#css-vars-explicit .bit-ctp-icn"), "height"));
        var callout = await OpenCalloutAsync("css-vars-explicit");
        await SettleAsync(callout);
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-err)"), await StyleOfAsync(callout.Locator(".bit-ctp-tlb"), "background-color"));
        Assert.AreNotEqual(300, (await BoxAsync(callout.Locator(".bit-ctp-clk")))!.Width, 1);
        // What the parameters do not set stays the variable's.
        Assert.AreEqual("rgb(7, 8, 9)", await StyleOfAsync(callout.Locator(".bit-ctp-clk"), "background-color"));
    }

    [TestMethod]
    public async Task CssVariables_OnTheInstanceStyleAndOnStylesCallout()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("css-vars-instance");
        Assert.AreEqual(220, (await BoxAsync(callout.Locator(".bit-ctp-clk")))!.Width, 1);
        Assert.AreEqual("rgb(46, 139, 87)", await StyleOfAsync(callout.Locator(".bit-ctp-tlb"), "background-color"));
        Assert.AreEqual("5px", await StyleOfAsync(callout.Locator(".bit-ctp-ptr"), "width"));
    }

    // ------------------------------------------------------------------ rtl

    [TestMethod]
    public async Task Rtl_Explicit_MirrorsTheFieldAndTheToolbarButNotTheDial()
    {
        await OpenAsync(Url);

        await Expect(Root("rtl-field")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Root("rtl-field")).ToHaveAttributeAsync("dir", "rtl");
        Assert.IsTrue((await BoxAsync(Page.Locator("#rtl-field .bit-ctp-ico")))!.X < (await BoxAsync(Field("rtl-field")))!.X);

        var callout = await OpenCalloutAsync("rtl-field");
        await Expect(callout).ToHaveAttributeAsync("dir", "rtl");
        await SettleAsync(callout);
        var box = (await BoxAsync(callout))!;
        var close = (await BoxAsync(callout.Locator(".bit-ctp-cbn")))!;
        Assert.IsTrue(close.X - box.X < box.Width / 2f, "In RTL the close button sits at the left corner.");
        await Page.Keyboard.PressAsync("Escape");

        var rtl = StandaloneCallout("rtl-standalone");
        var ltr = StandaloneCallout("ltr-standalone");
        // The parts of the time stay in reading order.
        Assert.IsTrue((await BoxAsync(HourButton(rtl)))!.X < (await BoxAsync(MinuteButton(rtl)))!.X);
        // The meridiem pair moves to the other side of the read-out.
        Assert.IsTrue((await BoxAsync(rtl.Locator(".bit-ctp-apc")))!.X < (await BoxAsync(HourButton(rtl)))!.X);
        Assert.IsTrue((await BoxAsync(ltr.Locator(".bit-ctp-apc")))!.X > (await BoxAsync(HourButton(ltr)))!.X);
        // The dial keeps running clockwise: 3 o'clock is on the right of the center in both.
        var dial = (await BoxAsync(Dial(rtl)))!;
        Assert.IsTrue((await BoxAsync(Number(rtl, "3")))!.X > dial.X + dial.Width / 2);
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_IsFollowedByTheField()
    {
        await OpenAsync(Url);

        await Expect(Root("rtl-inherited")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        Assert.IsTrue((await BoxAsync(Page.Locator("#rtl-inherited .bit-ctp-ico")))!.X < (await BoxAsync(Field("rtl-inherited")))!.X);

        var callout = await OpenCalloutAsync("rtl-inherited");
        Assert.AreEqual("rtl", await StyleOfAsync(callout.Locator(".bit-ctp-cac"), "direction"));
    }

    // ------------------------------------------------------------------ base behaviours

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheSpace_CollapsedRemovesIt_StandaloneDialIncluded()
    {
        await OpenAsync(Url);

        var root = Page.Locator("#visibility > .bit-ctp");
        var standalone = StandaloneCallout("visibility-standalone");
        var after = Page.Locator("#visibility-after");
        Task<double> TopAsync() => after.EvaluateAsync<double>("el => el.getBoundingClientRect().top + window.scrollY");
        var visibleY = await TopAsync();

        await Page.ClickAsync("#btn-hidden");
        await Expect(root).ToHaveCSSAsync("visibility", "hidden");
        await Expect(standalone).ToHaveCSSAsync("visibility", "hidden");
        Assert.AreEqual(visibleY, await TopAsync(), 1);

        await Page.ClickAsync("#btn-collapsed");
        await Expect(root).ToHaveCSSAsync("display", "none");
        await Expect(standalone).ToHaveCSSAsync("display", "none");
        Assert.IsTrue(await TopAsync() < visibleY - 10);

        await Page.ClickAsync("#btn-visible");
        await Expect(standalone).ToBeVisibleAsync();
        Assert.AreEqual(visibleY, await TopAsync(), 1);
    }

    [TestMethod]
    public async Task Attributes_IdAriaLabelHtmlAttributesNameAndTabIndexReachTheirElements()
    {
        await OpenAsync(Url);

        var root = Page.Locator("#ctp-attrs");
        await Expect(root).ToHaveClassAsync(HasClass("bit-ctp"));
        await Expect(root).ToHaveAttributeAsync("data-root", "root-value");
        await Expect(root).ToHaveAttributeAsync("title", "Root title");

        var field = Field("attributes");
        await Expect(field).ToHaveAttributeAsync("aria-label", "Arrival time");
        await Expect(field).ToHaveAttributeAsync("name", "arrival");
        await Expect(field).ToHaveAttributeAsync("tabindex", "3");
        await Expect(field).ToHaveAttributeAsync("data-input", "input-value");
        await Expect(field).ToHaveAttributeAsync("aria-disabled", "false");

        var callout = await OpenCalloutAsync("attributes");
        await Expect(callout.Locator(".bit-ctp-cac")).ToHaveAttributeAsync("data-callout", "callout-value");
        // TabIndex reaches the dial as well as the field.
        await Expect(Dial(callout)).ToHaveAttributeAsync("tabindex", "3");
    }

    [TestMethod]
    public async Task InsideAModal_EscapeClosesTheCalloutFirstAndTheModalOnTheNextPress()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-open-modal");
        await Expect(Page.Locator("#modal-content")).ToBeVisibleAsync();

        var callout = await OpenCalloutAsync("modal-content");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#modal-picker-closes")).ToHaveTextAsync("1");
        await Expect(Out("modal-open")).ToHaveTextAsync("True");
        await Expect(Field("modal-content")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Out("modal-open")).ToHaveTextAsync("False");
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
        // The hand's transition between resting places collapses too.
        var hand = await StyleOfAsync(reduced.Locator(".bit-ctp-ptr"), "transition-duration");
        await Page.Keyboard.PressAsync("Escape");

        var forced = await OpenCalloutAsync("force-animation");
        await Expect(forced).ToHaveClassAsync(HasClass("bit-fam"));
        var restored = await StyleOfAsync(forced, "animation-duration");

        Assert.IsTrue(Seconds(full) > 0.05, $"The callout animates by default ({full}).");
        Assert.IsTrue(Seconds(collapsed) < 0.01, $"Reduced motion collapses the animation ({collapsed}).");
        Assert.IsTrue(hand.Split(',').All(d => Seconds(d.Trim()) < 0.01), $"Reduced motion collapses the hand's transition ({hand}).");
        Assert.AreEqual(full, restored, "ForceAnimation restores the full duration under reduced motion.");

        static double Seconds(string duration) => duration.EndsWith("ms", StringComparison.Ordinal)
            ? double.Parse(duration[..^2], CultureInfo.InvariantCulture) / 1000
            : double.Parse(duration.TrimEnd('s'), CultureInfo.InvariantCulture);
    }
}
