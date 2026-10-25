using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// End-to-end tests of BitToggle on <c>/e2e/toggle</c> (and <c>/e2e/toggle/autofocus</c>): the prerendered switch,
/// flipping by pointer, label and keyboard with the knob sliding to the other end, the state texts, track and knob
/// content, every label placement and layout flag, FullWidth, the busy states (Loading, a page-driven save, AutoLoading),
/// read-only / required, one-way and two-way binding, ToggleAsync (read-only, loading, vetoed, disabled), every callback
/// with its order and arguments, cancellation, StopPropagation, the description, EditForm validation, the plain form
/// value carrier, ErrorMessage / Invalid / ErrorMessageTemplate with the live region, the accessible name and
/// description rules, AllowDisabledFocus, Role, the 24px target, the BitParams cascade, every color and size against the
/// computed styles, the external icons, Style / Class / Styles / Classes, the public CSS variables, RTL (explicit and
/// inherited), the disabled-markup and activation contracts, runtime re-renders, unmounting and reduced motion.
/// </summary>
[TestClass]
public class BitToggleE2ETests : E2ETestBase
{
    private const string Url = "/e2e/toggle";

    private static readonly (string Suffix, string Main, string On)[] Colors =
    [
        ("pri", "pri", "pri-text"), ("sec", "sec", "sec-text"), ("ter", "ter", "ter-text"), ("inf", "inf", "inf-text"),
        ("suc", "suc", "suc-text"), ("wrn", "wrn", "wrn-text"), ("swr", "swr", "swr-text"), ("err", "err", "err-text"),
        ("pbg", "bg-pri", "fg-pri"), ("sbg", "bg-sec", "fg-pri"), ("tbg", "bg-ter", "fg-pri"),
        ("pfg", "fg-pri", "bg-pri"), ("sfg", "fg-sec", "bg-pri"), ("tfg", "fg-ter", "bg-pri"),
        ("pbr", "brd-pri", "fg-pri"), ("sbr", "brd-sec", "fg-pri"), ("tbr", "brd-ter", "fg-pri")
    ];

    private static readonly Regex NotNone = new("^(?!none$).+");
    private static readonly Regex AnyValue = new(".*");

    private ILocator Root(string id) => Page.Locator($"#{id}");
    private ILocator Button(string id) => Page.Locator($"#{id} .bit-tgl-btn");
    private ILocator Label(string id) => Page.Locator($"#{id} .bit-tgl-lbl");
    private ILocator Thumb(string id) => Page.Locator($"#{id} .bit-tgl-sta");
    private ILocator StateText(string id) => Page.Locator($"#{id} .bit-tgl-stx");
    private ILocator Hidden(string id) => Page.Locator($"#{id} input.bit-input-hidden");
    private ILocator Span(string id) => Page.Locator($"#{id}");

    private Task<string> ResolveColorAsync(string cssColor)
    {
        return Page.EvaluateAsync<string>(@"v => {
            const d = document.createElement('div');
            d.style.color = v;
            document.body.appendChild(d);
            const c = getComputedStyle(d).color;
            d.remove();
            return c;
        }", cssColor);
    }

    private static Task<string> StyleOfAsync(ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>($"el => getComputedStyle(el).getPropertyValue('{property}')");
    }

    private static async Task<LocatorBoundingBoxResult> BoxOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no layout box.");
        return box;
    }

    private static double Px(string value) => double.Parse(value.Replace("px", string.Empty), CultureInfo.InvariantCulture);

    /// <summary>
    /// Waits for the knob of a toggle to rest at the given physical side of its track, once its slide is over.
    /// </summary>
    private Task WaitForThumbAsync(string id, string side)
    {
        return Page.WaitForFunctionAsync(@"([id, side]) => {
            const b = document.querySelector('#' + id + ' .bit-tgl-btn').getBoundingClientRect();
            const t = document.querySelector('#' + id + ' .bit-tgl-sta').getBoundingClientRect();
            const gap = side === 'left' ? t.left - b.left : b.right - t.right;
            return gap >= 0 && gap < 6;
        }", new object[] { id, side });
    }

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheSwitchBeforeTheCircuitStarts()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"tg-basic\"");
        StringAssert.Contains(html, "role=\"switch\"");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
        Assert.IsTrue(Regex.IsMatch(html, "id=\"tg-default-on\" class=\"[^\"]*bit-tgl-chk"), "The on-by-default toggle is prerendered on.");

        await OpenAsync(Url);
        await Expect(Button("tg-basic")).ToHaveAttributeAsync("aria-checked", "false");
    }

    [TestMethod]
    public async Task Basic_ClickingTheSwitchOrItsLabel_FlipsTheUncontrolledToggle()
    {
        await OpenAsync(Url);

        await Expect(Button("tg-basic")).ToHaveAttributeAsync("role", "switch");
        await Expect(Button("tg-basic")).ToHaveAttributeAsync("type", "button");
        await WaitForThumbAsync("tg-basic", "left");

        await Button("tg-basic").ClickAsync();

        await Expect(Button("tg-basic")).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Root("tg-basic")).ToHaveClassAsync(HasClass("bit-tgl-chk"));
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");
        await Expect(Span("basic-last")).ToHaveTextAsync("True");
        await Expect(Hidden("tg-basic")).ToBeCheckedAsync();
        await Expect(Hidden("tg-basic")).ToHaveAttributeAsync("value", "true");
        await WaitForThumbAsync("tg-basic", "right");

        await Label("tg-basic").ClickAsync();

        await Expect(Button("tg-basic")).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(Span("basic-change-count")).ToHaveTextAsync("2");
        await Expect(Span("basic-last")).ToHaveTextAsync("False");
        await WaitForThumbAsync("tg-basic", "left");
    }

    [TestMethod]
    public async Task Basic_DefaultValue_StartsOnAndStaysUncontrolled()
    {
        await OpenAsync(Url);

        await Expect(Button("tg-default-on")).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Button("tg-default-on")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri)"));

        await Button("tg-default-on").ClickAsync();

        await Expect(Button("tg-default-on")).ToHaveAttributeAsync("aria-checked", "false");
    }

    [TestMethod]
    public async Task Disabled_RendersTheDisabledMarkupAndIgnoresClicks()
    {
        await OpenAsync(Url);

        await Expect(Root("tg-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Button("tg-disabled")).ToHaveAttributeAsync("disabled", "");
        await Expect(Button("tg-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Hidden("tg-disabled")).ToHaveAttributeAsync("disabled", "");
        await Expect(Hidden("tg-disabled")).ToHaveAttributeAsync("aria-disabled", "true");

        await Button("tg-disabled").ClickAsync(new() { Force = true });
        await Label("tg-disabled").ClickAsync(new() { Force = true });
        await Button("tg-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");

        await Expect(Button("tg-disabled")).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(Button("tg-disabled-on")).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Button("tg-disabled-on")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-bg-dis)"));
        await Expect(Label("tg-disabled")).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-fg-dis)"));
    }

    [TestMethod]
    public async Task Texts_Text_IsTheSameStateTextInBothStates()
    {
        await OpenAsync(Url);

        await Expect(StateText("tg-text")).ToHaveTextAsync("This is a toggle!");

        await Button("tg-text").ClickAsync();

        await Expect(Button("tg-text")).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(StateText("tg-text")).ToHaveTextAsync("This is a toggle!");
    }

    [TestMethod]
    public async Task Texts_OnTextAndOffText_FollowTheStateAndTheStateTextIsAClickTarget()
    {
        await OpenAsync(Url);

        await Expect(StateText("tg-onoff")).ToHaveTextAsync("Toggle is Off");

        await StateText("tg-onoff").ClickAsync();

        await Expect(Button("tg-onoff")).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(StateText("tg-onoff")).ToHaveTextAsync("Toggle is On");

        await Expect(StateText("tg-ontext-only")).ToHaveTextAsync("Not enabled yet");
        await Button("tg-ontext-only").ClickAsync();
        await Expect(StateText("tg-ontext-only")).ToHaveTextAsync("Enabled");
    }

    [TestMethod]
    public async Task TrackContent_ShowsTheSideOfTheCurrentStateAndKeepsTheTrackWidth()
    {
        await OpenAsync(Url);

        var on = Page.Locator("#tg-content .bit-tgl-cno");
        var off = Page.Locator("#tg-content .bit-tgl-cnf");

        await Expect(Page.Locator("#tg-content .bit-tgl-cnn")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(off).ToHaveTextAsync("OFF");
        await Expect(off).ToHaveCSSAsync("visibility", "visible");
        await Expect(on).ToHaveCSSAsync("visibility", "hidden");

        await Button("tg-content").ClickAsync();

        await Expect(on).ToHaveCSSAsync("visibility", "visible");
        await Expect(off).ToHaveCSSAsync("visibility", "hidden");
        await Expect(on).ToHaveTextAsync("ON");

        var oneSideBefore = await BoxOfAsync(Button("tg-content-one"));
        await Button("tg-content-one").ClickAsync();
        await Expect(Button("tg-content-one")).ToHaveAttributeAsync("aria-checked", "true");
        var oneSideAfter = await BoxOfAsync(Button("tg-content-one"));
        Assert.AreEqual(oneSideBefore.Width, oneSideAfter.Width, 0.5, "The track keeps its width through a flip.");

        var plain = await BoxOfAsync(Button("tg-plain"));
        Assert.IsTrue(oneSideAfter.Width > plain.Width, "The track widens to fit its content.");
    }

    [TestMethod]
    public async Task KnobContent_StateIcons_SwapWithTheStateInARoomierKnob()
    {
        await OpenAsync(Url);

        var icon = Page.Locator("#tg-icons .bit-tgl-ico");
        await Expect(Root("tg-icons")).ToHaveClassAsync(HasClass("bit-tgl-tic"));
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Cancel"));
        await Expect(Thumb("tg-icons")).ToHaveAttributeAsync("aria-hidden", "true");

        await Button("tg-icons").ClickAsync();
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Accept"));

        var roomy = await BoxOfAsync(Thumb("tg-icons"));
        var plain = await BoxOfAsync(Thumb("tg-plain"));
        Assert.IsTrue(roomy.Width > plain.Width, "A knob with a glyph is roomier.");

        await Expect(Page.Locator("#tg-icon-on .bit-tgl-ico")).ToHaveClassAsync(HasClass("bit-icon--CheckMark"));
        var before = await BoxOfAsync(Button("tg-icon-on"));
        await Button("tg-icon-on").ClickAsync();
        await Expect(Page.Locator("#tg-icon-on .bit-tgl-ico")).ToHaveCountAsync(0);
        await Expect(Root("tg-icon-on")).ToHaveClassAsync(HasClass("bit-tgl-tic"));
        var after = await BoxOfAsync(Button("tg-icon-on"));
        Assert.AreEqual(before.Height, after.Height, 0.5, "The geometry stays roomy in the state without a glyph.");

        await Expect(Root("tg-icon-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Page.Locator("#tg-icon-disabled .bit-tgl-ico")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));
    }

    [TestMethod]
    public async Task ThumbTemplate_IsRenderedInTheKnobWithTheCurrentState()
    {
        await OpenAsync(Url);

        var content = Page.Locator("#tg-thumb-tpl .bit-tgl-sta .thumb-state");
        await Expect(content).ToHaveTextAsync("N");
        await Expect(Root("tg-thumb-tpl")).ToHaveClassAsync(HasClass("bit-tgl-tic"));

        await Button("tg-thumb-tpl").ClickAsync();

        await Expect(content).ToHaveTextAsync("D");
    }

    [TestMethod]
    public async Task Layout_EachPlacementAndFlag_PutsTheLabelOnThatSide()
    {
        await OpenAsync(Url);

        var cases = new (string Id, string? Class, string Side)[]
        {
            ("tg-place-top", "bit-tgl-ltp", "top"),
            ("tg-place-bottom", "bit-tgl-lbt", "bottom"),
            ("tg-place-start", "bit-tgl-lst", "left"),
            ("tg-place-end", "bit-tgl-lnd", "right"),
            ("tg-default-layout", null, "top"),
            ("tg-inline", "bit-tgl-inl", "left"),
            ("tg-reversed", "bit-tgl-rvs", "bottom"),
            ("tg-reversed-inline", "bit-tgl-rvs", "right"),
            ("tg-placement-wins", "bit-tgl-ltp", "top"),
        };

        foreach (var (id, cls, side) in cases)
        {
            if (cls is not null) await Expect(Root(id)).ToHaveClassAsync(HasClass(cls));

            var label = await BoxOfAsync(Label(id));
            var button = await BoxOfAsync(Button(id));

            var holds = side switch
            {
                "top" => label.Y + label.Height <= button.Y + 0.5,
                "bottom" => button.Y + button.Height <= label.Y + 0.5,
                "left" => label.X + label.Width <= button.X + 0.5,
                _ => button.X + button.Width <= label.X + 0.5,
            };

            Assert.IsTrue(holds, $"{id}: the label is on the {side} of the switch.");
        }

        await Expect(Root("tg-reversed-inline")).ToHaveClassAsync(HasClass("bit-tgl-inl"));
    }

    [TestMethod]
    public async Task FullWidth_PushesTheLabelAndTheSwitchToOppositeEdges()
    {
        await OpenAsync(Url);

        var panel = await BoxOfAsync(Page.Locator("#fw-panel"));

        await Expect(Root("tg-fullwidth")).ToHaveClassAsync(HasClass("bit-tgl-fwi"));
        Assert.AreEqual(panel.Width, (await BoxOfAsync(Root("tg-fullwidth"))).Width, 1);
        var button = await BoxOfAsync(Button("tg-fullwidth"));
        var label = await BoxOfAsync(Label("tg-fullwidth"));
        Assert.AreEqual(panel.X + panel.Width, button.X + button.Width, 1, "The switch sits at the far edge.");
        Assert.AreEqual(panel.X, label.X, 1, "The label starts at the reading edge.");

        var reversedButton = await BoxOfAsync(Button("tg-fullwidth-reversed"));
        Assert.AreEqual(panel.X, reversedButton.X, 1, "Reversed, the switch sits at the reading edge.");
    }

    [TestMethod]
    public async Task LabelTemplate_NamesTheSwitchAndStaysTheClickTarget()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#tg-label-tpl .bit-tgl-lbl #label-tpl-content")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Switch, new() { Name = "This is custom Label" })).ToHaveCountAsync(1);

        await Page.Locator("#label-tpl-content").ClickAsync();

        await Expect(Span("label-tpl-value")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task Loading_ShowsASpinnerInTheKnobTurnsAwayClicksAndAnnouncesBusy()
    {
        await OpenAsync(Url);

        await Expect(Root("tg-loading")).ToHaveClassAsync(HasClass("bit-tgl-ldg"));
        await Expect(Page.Locator("#tg-loading .bit-tgl-sta .bit-tgl-spn")).ToBeVisibleAsync();
        await Expect(Button("tg-loading")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Button("tg-loading")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Button("tg-loading")).Not.ToHaveAttributeAsync("disabled", AnyValue);

        await Button("tg-loading").ClickAsync(new() { Force = true });
        await Button("tg-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");
        await Expect(Span("loading-changes")).ToHaveTextAsync("0");
        await Expect(Button("tg-loading")).ToHaveAttributeAsync("aria-checked", "false");

        await Expect(Button("tg-loading-on")).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Page.Locator("#tg-loading-icons .bit-tgl-spn")).ToBeVisibleAsync();
        await Expect(Page.Locator("#tg-loading-icons .bit-tgl-ico")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#tg-loading-tpl .thumb-state")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#tg-loading-tpl .bit-tgl-spn")).ToBeVisibleAsync();

        var busyKnob = await BoxOfAsync(Thumb("tg-loading-icons"));
        var iconKnob = await BoxOfAsync(Thumb("tg-icons"));
        Assert.AreEqual(iconKnob.Width, busyKnob.Width, 0.5, "The spinner takes the knob it lands in without resizing it.");
    }

    [TestMethod]
    public async Task Loading_DrivenByThePage_KeepsTheToggleBusyUntilTheSaveLands()
    {
        await OpenAsync(Url);

        await Button("tg-save").ClickAsync();

        await Expect(Root("tg-save")).ToHaveClassAsync(HasClass("bit-tgl-ldg"));
        await Expect(Button("tg-save")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Span("saved-value")).ToHaveTextAsync("False");

        await Page.Locator("#btn-release").ClickAsync();

        await Expect(Span("saved-value")).ToHaveTextAsync("True");
        await Expect(Root("tg-save")).Not.ToHaveClassAsync(HasClass("bit-tgl-ldg"));
        await Expect(Button("tg-save")).ToHaveAttributeAsync("aria-checked", "true");
    }

    [TestMethod]
    public async Task AutoLoading_IsBusyWhileTheCallbacksRunAndDropsASecondClick()
    {
        await OpenAsync(Url);

        await Button("tg-autoloading").ClickAsync();

        await Expect(Span("auto-calls")).ToHaveTextAsync("1");
        await Expect(Root("tg-autoloading")).ToHaveClassAsync(HasClass("bit-tgl-ldg"));
        await Expect(Page.Locator("#tg-autoloading .bit-tgl-spn")).ToBeVisibleAsync();

        await Button("tg-autoloading").ClickAsync(new() { Force = true });
        await Button("tg-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");

        await Page.Locator("#btn-release").ClickAsync();

        await Expect(Span("auto-saved")).ToHaveTextAsync("1");
        await Expect(Root("tg-autoloading")).Not.ToHaveClassAsync(HasClass("bit-tgl-ldg"));
        await Expect(Span("auto-value")).ToHaveTextAsync("True");
        await Expect(Span("auto-calls")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task ReadOnly_IgnoresClicksSpaceAndEnterButFollowsThePage()
    {
        await OpenAsync(Url);

        await Expect(Root("tg-readonly")).ToHaveClassAsync(HasClass("bit-tgl-rdl"));
        await Expect(Button("tg-readonly")).ToHaveAttributeAsync("aria-readonly", "true");

        await Button("tg-readonly").ClickAsync();
        await Button("tg-readonly").FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await Page.Keyboard.PressAsync("Enter");
        await Button("tg-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");

        await Expect(Span("readonly-value")).ToHaveTextAsync("False");
        await Expect(Button("tg-readonly")).ToHaveAttributeAsync("aria-checked", "false");

        await Page.Locator("#btn-readonly").ClickAsync();
        await Expect(Button("tg-readonly")).ToHaveAttributeAsync("aria-checked", "true");
    }

    [TestMethod]
    public async Task Required_MarksTheLabelAndAnnouncesTheRequirementWithoutANativeRequired()
    {
        await OpenAsync(Url);

        await Expect(Root("tg-required")).ToHaveClassAsync(HasClass("bit-tgl-req"));
        await Expect(Button("tg-required")).ToHaveAttributeAsync("aria-required", "true");
        await Expect(Hidden("tg-required")).Not.ToHaveAttributeAsync("required", AnyValue);

        var asterisk = await Label("tg-required").EvaluateAsync<string>("el => getComputedStyle(el, '::after').content");
        StringAssert.Contains(asterisk, "*");
        var color = await Label("tg-required").EvaluateAsync<string>("el => getComputedStyle(el, '::after').color");
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-req)"), color);

        await Expect(Root("tg-required-disabled")).Not.ToHaveClassAsync(HasClass("bit-tgl-req"));
        await Expect(Button("tg-required-disabled")).ToHaveAttributeAsync("aria-required", "true");
    }

    [TestMethod]
    public async Task Binding_FixedValue_ClicksCannotMoveItAndOnChangingIsNotAsked()
    {
        await OpenAsync(Url);

        await Button("tg-fixed").ClickAsync();
        await Button("tg-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");

        await Expect(Button("tg-fixed")).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Span("fixed-changing")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Binding_OneWayValue_FollowsThePageAndRefusesClicks()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-oneway").ClickAsync();
        await Expect(Button("tg-oneway")).ToHaveAttributeAsync("aria-checked", "true");

        await Button("tg-oneway").ClickAsync();
        await Button("tg-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");
        await Expect(Button("tg-oneway")).ToHaveAttributeAsync("aria-checked", "true");

        await Page.Locator("#btn-oneway").ClickAsync();
        await Expect(Button("tg-oneway")).ToHaveAttributeAsync("aria-checked", "false");
    }

    [TestMethod]
    public async Task Binding_TwoWayValue_FlowsBothWays()
    {
        await OpenAsync(Url);

        await Button("tg-twoway").ClickAsync();
        await Expect(Span("twoway-value")).ToHaveTextAsync("True");

        await Page.Locator("#btn-twoway").ClickAsync();
        await Expect(Span("twoway-value")).ToHaveTextAsync("False");
        await Expect(Button("tg-twoway")).ToHaveAttributeAsync("aria-checked", "false");
        await WaitForThumbAsync("tg-twoway", "left");
    }

    [TestMethod]
    public async Task ToggleAsync_FlipsAReadOnlyToggleFromCodeAndTheOverloadMovesToAState()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-method-flip").ClickAsync();
        await Expect(Span("method-log")).ToHaveTextAsync("T");
        await Expect(Button("tg-method")).ToHaveAttributeAsync("aria-checked", "true");

        await Page.Locator("#btn-method-on").ClickAsync();
        await Page.Locator("#btn-method-off").ClickAsync();
        await Expect(Span("method-log")).ToHaveTextAsync("TF");

        await Page.Locator("#btn-method-on").ClickAsync();
        await Expect(Span("method-log")).ToHaveTextAsync("TFT");

        // The user still cannot move it.
        await Button("tg-method").ClickAsync();
        await Page.Locator("#btn-method-on").ClickAsync();
        await Page.Locator("#btn-method-flip").ClickAsync();
        await Expect(Span("method-log")).ToHaveTextAsync("TFTF");
    }

    [TestMethod]
    public async Task ToggleAsync_IsVetoedByOnChangingIgnoredWhileDisabledAndRunsWhileLoading()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-guarded-method").ClickAsync();
        await Expect(Span("guarded-method-cancels")).ToHaveTextAsync("1");
        await Expect(Button("tg-guarded-method")).ToHaveAttributeAsync("aria-checked", "false");

        await Page.Locator("#btn-loading-method").ClickAsync();
        await Expect(Button("tg-loading-method")).ToHaveAttributeAsync("aria-checked", "true");

        await Page.Locator("#btn-disabled-method").ClickAsync();
        await Page.Locator("#btn-guarded-method").ClickAsync();
        await Expect(Span("guarded-method-cancels")).ToHaveTextAsync("2");
        await Expect(Span("disabled-method-changes")).ToHaveTextAsync("0");
        await Expect(Button("tg-disabled-method")).ToHaveAttributeAsync("aria-checked", "false");
    }

    [TestMethod]
    public async Task Events_Click_FiresOnClickOnChangingOnChangeInOrderAndOnce()
    {
        await OpenAsync(Url);

        await Button("tg-events").ClickAsync();

        await Expect(Page.Locator("#ev-counts")).ToContainTextAsync("click=1;changing=1;change=1;");
        await Expect(Page.Locator("#ev-log")).ToHaveTextAsync("OnClick>OnChanging(True)>OnChange(True)");

        await Button("tg-events").ClickAsync();
        await Expect(Page.Locator("#ev-log")).ToHaveTextAsync("OnClick>OnChanging(True)>OnChange(True)>OnClick>OnChanging(False)>OnChange(False)");
    }

    [TestMethod]
    public async Task Events_Focus_FiresOnFocusOnFocusInOnFocusOutAndOnBlur()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-before-events").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Button("tg-events")).ToBeFocusedAsync();
        await Expect(Page.Locator("#ev-counts")).ToContainTextAsync("focus=1;focusin=1;focusout=0;blur=0");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#btn-after-events")).ToBeFocusedAsync();
        await Expect(Page.Locator("#ev-counts")).ToContainTextAsync("focus=1;focusin=1;focusout=1;blur=1");
    }

    [TestMethod]
    public async Task OnChanging_Cancel_KeepsTheToggleWhereItWas()
    {
        await OpenAsync(Url);

        await Button("tg-guard").ClickAsync();
        await Expect(Span("guard-cancelled")).ToHaveTextAsync("1");
        await Expect(Button("tg-guard")).ToHaveAttributeAsync("aria-checked", "false");

        await Button("tg-allow").ClickAsync();
        await Button("tg-guard").ClickAsync();

        await Expect(Span("guard-value")).ToHaveTextAsync("True");
        await Expect(Button("tg-guard")).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Span("guard-cancelled")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task StopPropagation_KeepsTheClicksFromTheContainer()
    {
        await OpenAsync(Url);

        await Button("tg-bubbles").ClickAsync();
        await Expect(Span("container-clicks")).ToHaveTextAsync("1");

        await Button("tg-stops").ClickAsync();
        await Expect(Button("tg-stops")).ToHaveAttributeAsync("aria-checked", "true");
        await Label("tg-stops").ClickAsync();
        await Expect(Button("tg-stops")).ToHaveAttributeAsync("aria-checked", "false");
        await Button("tg-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");

        await Expect(Span("container-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Description_DescribesTheSwitchOnALineUnderIt()
    {
        await OpenAsync(Url);

        var description = Page.Locator("#tg-desc .bit-tgl-des");
        await Expect(description).ToHaveTextAsync("The subscription is renewed one day before it expires.");
        await Expect(Root("tg-desc")).ToHaveClassAsync(HasClass("bit-tgl-hds"));
        await Expect(Page.GetByRole(AriaRole.Switch, new() { Name = "Auto renew", Exact = true }).First).ToHaveAccessibleDescriptionAsync("The subscription is renewed one day before it expires.");

        var button = await BoxOfAsync(Button("tg-desc"));
        var line = await BoxOfAsync(description);
        Assert.IsTrue(line.Y >= button.Y + button.Height - 1, "The description sits under the switch.");

        await description.ClickAsync();
        await Button("tg-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");
        await Expect(Span("desc-value")).ToHaveTextAsync("False");

        var inlineDescription = await BoxOfAsync(Page.Locator("#tg-desc-inline .bit-tgl-des"));
        var inlineButton = await BoxOfAsync(Button("tg-desc-inline"));
        Assert.IsTrue(inlineDescription.Y >= inlineButton.Y + inlineButton.Height - 1, "Inline, the description takes a line of its own.");
        Assert.AreEqual(400, (await BoxOfAsync(Root("tg-desc-inline"))).Width, 1);

        await Expect(Page.Locator("#tg-desc-tpl .bit-tgl-des #desc-tpl-link")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Validation_AnOffRequiredTerm_RefusesTheSubmitAndMarksTheSwitchInvalid()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-submit").ClickAsync();

        await Expect(Span("submit-result")).ToHaveTextAsync("invalid:1");
        await Expect(Page.Locator("#terms-error")).ToHaveTextAsync("You must agree to the terms and conditions.");
        await Expect(Button("tg-terms")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("tg-terms")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Button("tg-terms")).ToHaveCSSAsync("border-top-color", await ResolveColorAsync("var(--bit-clr-err)"));

        await Expect(Button("tg-novalidate")).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("tg-novalidate")).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await Button("tg-terms").ClickAsync();

        await Expect(Page.Locator("#terms-error")).ToHaveTextAsync(string.Empty);
        await Expect(Button("tg-terms")).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("tg-terms")).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await Button("tg-novalidate").ClickAsync();
        await Page.Locator("#btn-submit").ClickAsync();
        await Expect(Span("submit-result")).ToHaveTextAsync("valid:1");
    }

    [TestMethod]
    public async Task Name_TheHiddenCheckboxCarriesTheStateIntoAPlainForm()
    {
        await OpenAsync(Url);

        const string read = "name => new FormData(document.getElementById('plain-form')).get(name)";

        await Expect(Hidden("tg-named")).ToHaveAttributeAsync("name", "notify");
        await Expect(Hidden("tg-named")).ToHaveAttributeAsync("data-carrier", "yes");
        await Expect(Hidden("tg-named")).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Hidden("tg-named")).ToHaveAttributeAsync("aria-hidden", "true");
        Assert.IsNull(await Page.EvaluateAsync<string?>(read, "notify"), "An off toggle is left out of the post.");

        await Button("tg-named").ClickAsync();
        await Expect(Hidden("tg-named")).ToBeCheckedAsync();
        Assert.AreEqual("true", await Page.EvaluateAsync<string?>(read, "notify"));

        Assert.IsNull(await Page.EvaluateAsync<string?>(read, "locked"), "A disabled toggle is not posted.");
    }

    [TestMethod]
    public async Task ErrorMessage_SaysWhyOnALineOfItsOwnAndAnnouncesItThroughALiveRegion()
    {
        await OpenAsync(Url);

        var error = Page.Locator("#tg-2fa .bit-tgl-erm");
        var live = Page.Locator("#tg-2fa [role=status]");

        await Expect(error).ToHaveTextAsync("Your organization requires two-factor authentication.");
        await Expect(live).ToHaveTextAsync("Your organization requires two-factor authentication.");
        await Expect(live).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(Root("tg-2fa")).ToHaveClassAsync(HasClass("bit-tgl-her"));
        await Expect(Root("tg-2fa")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Button("tg-2fa")).ToHaveAttributeAsync("aria-invalid", "true");

        var errorId = await error.GetAttributeAsync("id");
        var descriptionId = await Page.Locator("#tg-2fa .bit-tgl-des").GetAttributeAsync("id");
        await Expect(Button("tg-2fa")).ToHaveAttributeAsync("aria-describedby", $"{errorId} {descriptionId}");

        var errorBox = await BoxOfAsync(error);
        var descriptionBox = await BoxOfAsync(Page.Locator("#tg-2fa .bit-tgl-des"));
        Assert.IsTrue(errorBox.Y < descriptionBox.Y, "The error line sits above the description.");
        await Expect(error).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-err)"));

        await Button("tg-2fa").ClickAsync();

        await Expect(error).ToHaveCountAsync(0);
        await Expect(live).ToHaveTextAsync(string.Empty);
        await Expect(Button("tg-2fa")).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("tg-2fa")).Not.ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task Invalid_AndErrorMessageTemplate_MarkTheSwitchInvalid()
    {
        await OpenAsync(Url);

        await Expect(Root("tg-invalid")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Button("tg-invalid")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#tg-invalid .bit-tgl-erm")).ToHaveCountAsync(0);
        await Expect(Button("tg-invalid")).ToHaveCSSAsync("border-top-color", await ResolveColorAsync("var(--bit-clr-err)"));

        await Expect(Page.Locator("#tg-error-tpl .bit-tgl-erm #error-tpl-link")).ToBeVisibleAsync();
        await Expect(Root("tg-error-tpl")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Button("tg-error-tpl")).ToHaveAttributeAsync("aria-invalid", "true");
    }

    [TestMethod]
    public async Task Keyboard_TabFocusesTheSwitchDrawsTheRingAndSpaceAndEnterFlipIt()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-before-a11y").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        await Expect(Button("tg-a11y")).ToBeFocusedAsync();
        await Expect(Button("tg-a11y")).ToHaveCSSAsync("box-shadow", NotNone);

        await Page.Keyboard.PressAsync("Space");
        await Expect(Span("a11y-value")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Span("a11y-value")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task TabIndex_MinusOne_TakesTheSwitchOutOfTheTabOrder()
    {
        await OpenAsync(Url);

        await Expect(Button("tg-tabindex")).ToHaveAttributeAsync("tabindex", "-1");

        await Button("tg-title").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        await Expect(Button("tg-after-tabindex")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Title_IsTheTooltipOfTheLabelTheRowAndTheSwitch()
    {
        await OpenAsync(Url);

        await Expect(Label("tg-title")).ToHaveAttributeAsync("title", "The native tooltip of the toggle");
        await Expect(Page.Locator("#tg-title .bit-tgl-cnt")).ToHaveAttributeAsync("title", "The native tooltip of the toggle");
        await Expect(Button("tg-title")).ToHaveAttributeAsync("title", "The native tooltip of the toggle");
        await Expect(Root("tg-title")).Not.ToHaveAttributeAsync("title", AnyValue);
    }

    [TestMethod]
    public async Task FocusAsync_MovesTheFocusToTheSwitch()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-focus").ClickAsync();

        await Expect(Button("tg-focus-target")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AutoFocus_FocusesTheSwitchOnItsFirstRender()
    {
        await OpenAsync($"{Url}/autofocus");

        await Expect(Button("tg-autofocus")).ToBeFocusedAsync();
        await Expect(Button("tg-autofocus-disabled")).Not.ToHaveAttributeAsync("autofocus", AnyValue);
    }

    [TestMethod]
    public async Task AccessibleName_ComesFromTheLabelAriaLabelOrAriaLabelledbyNeverFromAChangingStateText()
    {
        await OpenAsync(Url);

        await Expect(Page.GetByRole(AriaRole.Switch, new() { Name = "Dark mode", Exact = true })).ToHaveCountAsync(2);
        await Expect(Button("tg-arialabel")).ToHaveAttributeAsync("aria-label", "Dark mode");

        var hidden = Page.Locator("#tg-ariadesc .bit-tgl-dsc");
        await Expect(hidden).ToHaveTextAsync("The subscription will be renewed one day before it expires.");
        await Expect(Button("tg-ariadesc")).ToHaveAttributeAsync("aria-describedby", (await hidden.GetAttributeAsync("id"))!);

        // AriaLabelledby outranks the toggle's own label; AriaDescribedby is added after its own state text.
        await Expect(Button("tg-labelledby")).ToHaveAttributeAsync("aria-labelledby", "offline-mode-heading");
        var labelledBy = Button("tg-labelledby");
        await Expect(labelledBy).ToHaveAccessibleNameAsync("Offline mode");
        await Expect(labelledBy).ToHaveAccessibleDescriptionAsync("Enabled Keep a copy of the last synced data on this device.");

        await Expect(Button("tg-controls")).ToHaveAttributeAsync("aria-controls", "advanced-settings-panel");
        await Button("tg-controls").ClickAsync();
        await Expect(Page.Locator("#advanced-settings-panel")).ToHaveTextAsync("Advanced settings");

        // A state text that differs between the states describes the switch rather than naming it.
        var notifications = Page.GetByRole(AriaRole.Switch, new() { Name = "Notifications", Exact = true });
        await Expect(notifications).ToHaveAccessibleDescriptionAsync("Blocked");
        await notifications.ClickAsync();
        await Expect(notifications).ToHaveAccessibleDescriptionAsync("Allowed");
        await Expect(notifications).ToHaveCountAsync(1);

        await Expect(Button("tg-onoff-only")).Not.ToHaveAttributeAsync("aria-labelledby", AnyValue);
    }

    [TestMethod]
    public async Task AllowDisabledFocus_KeepsADisabledToggleReachableButInert()
    {
        await OpenAsync(Url);

        await Expect(Button("tg-dis-skipped")).ToHaveAttributeAsync("disabled", "");
        await Expect(Button("tg-dis-focus")).Not.ToHaveAttributeAsync("disabled", AnyValue);
        await Expect(Button("tg-dis-focus")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Label("tg-dis-skipped")).ToHaveAttributeAsync("title", "Out of the tab order");

        await Page.Locator("#btn-before-disabled-focus").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Button("tg-dis-focus")).ToBeFocusedAsync();
        await Expect(Button("tg-dis-focus")).ToHaveCSSAsync("box-shadow", NotNone);

        await Page.Keyboard.PressAsync("Space");
        await Page.Keyboard.PressAsync("Enter");
        await Button("tg-basic").ClickAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");

        await Expect(Button("tg-dis-focus")).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(Span("dis-focus-changes")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Role_Checkbox_AnnouncesTheToggleAsACheckbox()
    {
        await OpenAsync(Url);

        var checkbox = Page.GetByRole(AriaRole.Checkbox, new() { Name = "A checkbox, not a switch" });
        await Expect(checkbox).ToHaveCountAsync(1);
        await Expect(checkbox).Not.ToBeCheckedAsync();

        await checkbox.ClickAsync();
        await Expect(checkbox).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task TargetSize_ThePointerTargetIsAtLeast24PixelsHigh()
    {
        await OpenAsync(Url);

        // The extent of the column through the middle of the track that a pointer lands on the switch in; hit
        // testing only reaches what is in the viewport.
        await Button("tg-plain").ScrollIntoViewIfNeededAsync();
        var span = await Button("tg-plain").EvaluateAsync<double[]>(@"btn => {
            const r = btn.getBoundingClientRect();
            const x = r.left + r.width / 2;
            let top = null, bottom = null;
            for (let y = r.top - 16; y <= r.bottom + 16; y += 0.5) {
                const el = document.elementFromPoint(x, y);
                if (el && btn.contains(el)) { if (top === null) top = y; bottom = y; }
            }
            return [r.height, top === null ? 0 : bottom - top + 0.5];
        }");

        Assert.IsTrue(span[0] < 24, $"The plain track is drawn shorter than the target ({span[0]}px).");
        Assert.IsTrue(span[1] >= 24, $"The pointer target is {span[1]}px high.");
    }

    [TestMethod]
    public async Task Cascading_BitParams_FillsWhatTheToggleLeavesUnset()
    {
        await OpenAsync(Url);

        await Expect(Root("tg-cascade-1")).ToHaveClassAsync(HasClass("bit-tgl-inl"));
        await Expect(Root("tg-cascade-1")).ToHaveClassAsync(HasClass("bit-tgl-fwi"));
        await Expect(Root("tg-cascade-1")).ToHaveClassAsync(HasClass("bit-tgl-lst"));
        await Expect(StateText("tg-cascade-1")).ToHaveTextAsync("Off");
        Assert.AreEqual(400, (await BoxOfAsync(Root("tg-cascade-1"))).Width, 1);

        await Button("tg-cascade-1").ClickAsync();
        await Expect(StateText("tg-cascade-1")).ToHaveTextAsync("On");

        await Expect(StateText("tg-cascade-own")).ToHaveTextAsync("Keeping");
        await Expect(Root("tg-cascade-own")).ToHaveClassAsync(HasClass("bit-tgl-lst"));

        await Expect(Root("tg-cascade-outside")).Not.ToHaveClassAsync(HasClass("bit-tgl-inl"));
        await Expect(StateText("tg-cascade-outside")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Color_EachValue_FillsTheOnTrackAndPaintsTheKnob()
    {
        await OpenAsync(Url);

        foreach (var (suffix, main, on) in Colors)
        {
            var id = $"tg-color-{suffix}";
            await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-tgl-{suffix}"));
            await Expect(Button(id)).ToHaveCSSAsync("background-color", await ResolveColorAsync($"var(--bit-clr-{main})"));
            await Expect(Thumb(id)).ToHaveCSSAsync("background-color", await ResolveColorAsync($"var(--bit-clr-{on})"));

            // Off, the track keeps the neutral fill whatever the color.
            await Expect(Button($"{id}-off")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-bg-pri)"));
        }

        await Expect(Root("tg-color-default")).Not.ToHaveClassAsync(HasClass("bit-tgl-pri"));
        await Expect(Button("tg-color-default")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri)"));
        Assert.AreNotEqual(await StyleOfAsync(Button("tg-color-pri"), "background-color"), await StyleOfAsync(Button("tg-color-err"), "background-color"));
    }

    [TestMethod]
    public async Task Color_AnExplicitColor_WinsOverTheCheckedBackgroundVariable()
    {
        await OpenAsync(Url);

        await Expect(Button("tg-color-wins")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-err)"));
    }

    [TestMethod]
    public async Task ExternalIcons_OnIconAndOffIcon_RenderTheirClassesForEachState()
    {
        await OpenAsync(Url);

        var cases = new (string Id, string Off, string On)[]
        {
            ("tg-ext-string", "fa-xmark", "fa-check"),
            ("tg-ext-fa", "fa-moon", "fa-sun"),
            ("tg-ext-bi", "bi-bell-slash", "bi-bell-fill"),
            ("tg-ext-css", "my-off", "my-on"),
        };

        foreach (var (id, off, on) in cases)
        {
            var icon = Page.Locator($"#{id} .bit-tgl-ico");
            await Expect(icon).ToHaveClassAsync(HasClass(off));
            await Expect(icon).Not.ToHaveClassAsync(HasClass("bit-icon"));

            await Button(id).ClickAsync();

            await Expect(icon).ToHaveClassAsync(HasClass(on));
        }

        await Expect(Page.Locator("#tg-ext-fa .bit-tgl-ico")).ToHaveClassAsync(HasClass("fa-solid"));
        await Expect(Page.Locator("#tg-ext-bi .bit-tgl-ico")).ToHaveClassAsync(HasClass("bi"));
    }

    [TestMethod]
    public async Task Size_EachValue_ScalesTheTrackTheKnobAndTheLabel()
    {
        await OpenAsync(Url);

        foreach (var suffix in new[] { "sm", "md", "lg" })
        {
            await Expect(Root($"tg-size-{suffix}")).ToHaveClassAsync(HasClass($"bit-tgl-{suffix}"));
        }

        var sm = await BoxOfAsync(Button("tg-size-sm"));
        var md = await BoxOfAsync(Button("tg-size-md"));
        var lg = await BoxOfAsync(Button("tg-size-lg"));
        var dflt = await BoxOfAsync(Button("tg-size-default"));

        Assert.IsTrue(sm.Width < md.Width && md.Width < lg.Width, $"Track widths {sm.Width} < {md.Width} < {lg.Width}.");
        Assert.IsTrue(sm.Height < md.Height && md.Height < lg.Height, $"Track heights {sm.Height} < {md.Height} < {lg.Height}.");
        Assert.AreEqual(md.Width, dflt.Width, 0.5, "Medium is the default.");

        var smThumb = await BoxOfAsync(Thumb("tg-size-sm"));
        var lgThumb = await BoxOfAsync(Thumb("tg-size-lg"));
        Assert.IsTrue(smThumb.Width < lgThumb.Width);

        var smFont = Px(await StyleOfAsync(Label("tg-size-sm"), "font-size"));
        var mdFont = Px(await StyleOfAsync(Label("tg-size-md"), "font-size"));
        var lgFont = Px(await StyleOfAsync(Label("tg-size-lg"), "font-size"));
        Assert.IsTrue(smFont < mdFont && mdFont < lgFont, $"Label sizes {smFont} < {mdFont} < {lgFont}.");

        var smIcons = await BoxOfAsync(Button("tg-size-sm-icons"));
        var lgIcons = await BoxOfAsync(Button("tg-size-lg-icons"));
        Assert.IsTrue(smIcons.Height >= sm.Height && lgIcons.Height >= lg.Height, "A knob with a glyph is never smaller than the plain one.");
        Assert.IsTrue(smIcons.Height < lgIcons.Height);

        var wins = await BoxOfAsync(Button("tg-size-wins"));
        Assert.AreEqual(lg.Width, wins.Width, 0.5, "An explicit Size wins over --bit-Toggle-track-width.");
    }

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRoot()
    {
        await OpenAsync(Url);

        await Expect(Root("tg-style")).ToHaveCSSAsync("background-color", "rgb(255, 106, 0)");
        await Expect(Root("tg-style")).ToHaveCSSAsync("padding-top", "8px");
        await Expect(Root("tg-class")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Root("tg-class")).ToHaveClassAsync(HasClass("bit-tgl"));
    }

    [TestMethod]
    public async Task Styles_ReachEveryPartAndTheCheckedEntryAppliesOnlyWhileOn()
    {
        await OpenAsync(Url);

        await Expect(Button("tg-styles")).ToHaveCSSAsync("background-color", "rgb(211, 211, 211)");
        await Expect(Thumb("tg-styles")).ToHaveCSSAsync("background-color", "rgb(245, 245, 245)");
        await Expect(Label("tg-styles")).ToHaveCSSAsync("color", "rgb(0, 0, 255)");
        await Expect(Page.Locator("#tg-styles .bit-tgl-des")).ToHaveCSSAsync("font-style", "italic");
        await Expect(Page.Locator("#tg-styles .bit-tgl-cnt")).ToHaveCSSAsync("padding-top", "2px");
        await Expect(Button("tg-styles")).ToHaveCSSAsync("outline-color", "rgb(255, 0, 0)");
        await Expect(StateText("tg-styles")).ToHaveCSSAsync("letter-spacing", "3px");

        // The pointer is moved off after each click: the hover tier repaints the track under it.
        await Button("tg-styles").ClickAsync();
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(Button("tg-styles")).ToHaveCSSAsync("background-color", "rgb(46, 204, 113)");

        await Button("tg-styles").ClickAsync();
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(Button("tg-styles")).ToHaveCSSAsync("background-color", "rgb(211, 211, 211)");

        await Expect(Page.Locator("#tg-classes-content .bit-tgl-cnf")).ToHaveCSSAsync("color", "rgb(2, 2, 2)");
        await Expect(Page.Locator("#tg-classes-content .bit-tgl-cno")).ToHaveCSSAsync("color", "rgb(1, 1, 1)");
        await Expect(Page.Locator("#tg-classes-content .bit-tgl-cnn")).ToHaveCSSAsync("padding-left", "10px");
        await Expect(Page.Locator("#tg-classes-spinner .bit-tgl-spn")).ToHaveCSSAsync("opacity", "0.5");
        await Expect(Page.Locator("#tg-styles-error .bit-tgl-erm")).ToHaveCSSAsync("padding-top", "4px");
        await Expect(Page.Locator("#tg-styles-error .bit-tgl-erm span")).ToHaveCSSAsync("font-weight", "700");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPartAndTheCheckedEntryAppliesOnlyWhileOn()
    {
        await OpenAsync(Url);

        await Expect(Root("tg-classes")).ToHaveClassAsync(HasClass("c-root"));
        await Expect(Thumb("tg-classes")).ToHaveClassAsync(HasClass("c-thumb"));
        await Expect(Button("tg-classes")).ToHaveClassAsync(HasClass("c-button"));
        await Expect(Label("tg-classes")).ToHaveClassAsync(HasClass("c-label"));
        await Expect(Page.Locator("#tg-classes .bit-tgl-des")).ToHaveClassAsync(HasClass("c-desc"));
        await Expect(Page.Locator("#tg-classes .bit-tgl-cnt")).ToHaveClassAsync(HasClass("c-container"));
        await Expect(Page.Locator("#tg-classes .bit-tgl-ico")).ToHaveClassAsync(HasClass("c-icon"));
        await Expect(StateText("tg-classes")).ToHaveClassAsync(HasClass("c-text"));
        await Expect(Page.Locator("#tg-classes .bit-tgl-erm")).ToHaveClassAsync(HasClass("c-erc"));
        await Expect(Page.Locator("#tg-classes .bit-tgl-erm .c-erm")).ToHaveTextAsync("Err");
        await Expect(Root("tg-classes")).Not.ToHaveClassAsync(HasClass("c-checked"));

        await Button("tg-classes").ClickAsync();
        await Expect(Root("tg-classes")).ToHaveClassAsync(HasClass("c-checked"));

        await Expect(Page.Locator("#tg-classes-content .bit-tgl-cnn")).ToHaveClassAsync(HasClass("c-content"));
        await Expect(Page.Locator("#tg-classes-content .bit-tgl-cno")).ToHaveClassAsync(HasClass("c-on"));
        await Expect(Page.Locator("#tg-classes-content .bit-tgl-cnf")).ToHaveClassAsync(HasClass("c-off"));
        await Expect(Page.Locator("#tg-classes-spinner .bit-tgl-spn")).ToHaveClassAsync(HasClass("c-spinner"));
    }

    [TestMethod]
    public async Task CssVariables_OnTheInstance_RestyleTheGeometryThePaletteAndTheMotion()
    {
        await OpenAsync(Url);

        await Expect(Button("tg-var-geometry")).ToHaveCSSAsync("height", "32px");
        Assert.AreEqual(52, (await BoxOfAsync(Button("tg-var-geometry"))).Width, 0.5);
        await Expect(Thumb("tg-var-geometry")).ToHaveCSSAsync("width", "28px");
        await Expect(Thumb("tg-var-geometry")).ToHaveCSSAsync("box-shadow", "rgba(0, 0, 0, 0.3) 0px 2px 4px 0px");

        await Expect(Button("tg-var-square")).ToHaveCSSAsync("border-top-left-radius", "4px");
        await Expect(Thumb("tg-var-square")).ToHaveCSSAsync("border-top-left-radius", "2px");
        await Expect(Button("tg-var-square")).ToHaveCSSAsync("border-top-width", "3px");
        StringAssert.StartsWith(await StyleOfAsync(Thumb("tg-var-square"), "transition-duration"), "0.4s");

        await Expect(Button("tg-var-palette")).ToHaveCSSAsync("background-color", "rgb(102, 51, 153)");
        await Expect(Thumb("tg-var-palette")).ToHaveCSSAsync("background-color", "rgb(255, 215, 0)");
        await Expect(Page.Locator("#tg-var-palette .bit-tgl-ico")).ToHaveCSSAsync("color", "rgb(102, 51, 153)");
        await Expect(Label("tg-var-palette")).ToHaveCSSAsync("color", "rgb(102, 51, 153)");

        await Expect(Button("tg-var-off")).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(Button("tg-var-off")).ToHaveCSSAsync("border-top-color", "rgb(40, 50, 60)");
        await Expect(Thumb("tg-var-off")).ToHaveCSSAsync("background-color", "rgb(70, 80, 90)");
        await Expect(StateText("tg-var-off")).ToHaveCSSAsync("color", "rgb(100, 0, 0)");
        await Expect(Label("tg-var-off")).ToHaveCSSAsync("font-size", "22px");
        await Expect(StateText("tg-var-off")).ToHaveCSSAsync("font-size", "18px");
    }

    [TestMethod]
    public async Task CssVariables_HoverTier_RestylesTheTrackAndTheKnobUnderThePointer()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        await Expect(Button("tg-var-hover")).Not.ToHaveCSSAsync("border-top-color", "rgb(255, 0, 0)");

        await Button("tg-var-hover").HoverAsync();

        await Expect(Button("tg-var-hover")).ToHaveCSSAsync("border-top-color", "rgb(255, 0, 0)");
        await Expect(Button("tg-var-hover")).ToHaveCSSAsync("background-color", "rgb(0, 0, 255)");
        await Expect(Thumb("tg-var-hover")).ToHaveCSSAsync("background-color", "rgb(0, 255, 0)");
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_AreInheritedByEveryToggleInside()
    {
        await OpenAsync(Url);

        await Expect(Button("tg-var-anc-on")).ToHaveCSSAsync("background-color", "rgb(0, 100, 0)");
        await Expect(Label("tg-var-anc-on")).ToHaveCSSAsync("font-weight", "400");
        await Expect(Root("tg-var-anc-on")).ToHaveCSSAsync("row-gap", "16px");
        await Expect(Page.Locator("#tg-var-anc-on .bit-tgl-des")).ToHaveCSSAsync("color", "rgb(0, 128, 128)");
        await Expect(Page.Locator("#tg-var-anc-err .bit-tgl-erm")).ToHaveCSSAsync("color", "rgb(128, 0, 128)");
        await Expect(Button("tg-var-anc-err")).ToHaveCSSAsync("border-top-color", "rgb(128, 0, 128)");
        await Expect(Page.Locator("#tg-var-anc-err .bit-tgl-cnn")).ToHaveCSSAsync("color", "rgb(9, 9, 9)");

        var asterisk = await Label("tg-var-anc-err").EvaluateAsync<string>("el => getComputedStyle(el, '::after').color");
        Assert.AreEqual("rgb(0, 0, 128)", asterisk);

        await Expect(Button("tg-var-disabled")).ToHaveCSSAsync("background-color", "rgb(200, 0, 0)");
        await Expect(Label("tg-var-disabled")).ToHaveCSSAsync("color", "rgb(0, 0, 200)");
    }

    [TestMethod]
    public async Task Rtl_ExplicitOrInherited_RestsTheKnobOnTheRightAndSlidesItLeft()
    {
        await OpenAsync(Url);

        await Expect(Root("tg-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Root("tg-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await WaitForThumbAsync("tg-rtl", "right");
        await Expect(StateText("tg-rtl")).ToHaveTextAsync("خاموش");

        await Button("tg-rtl").ClickAsync();

        await WaitForThumbAsync("tg-rtl", "left");
        await Expect(StateText("tg-rtl")).ToHaveTextAsync("روشن");

        var label = await BoxOfAsync(Label("tg-rtl-start"));
        var button = await BoxOfAsync(Button("tg-rtl-start"));
        Assert.IsTrue(label.X > button.X, "Start is the right in RTL.");

        await Expect(Root("tg-rtl-inherited")).Not.ToHaveAttributeAsync("dir", AnyValue);
        await WaitForThumbAsync("tg-rtl-inherited", "right");
        await Button("tg-rtl-inherited").ClickAsync();
        await WaitForThumbAsync("tg-rtl-inherited", "left");
    }

    [TestMethod]
    public async Task Base_VisibilityAndHtmlAttributes_AreAppliedToTheRoot()
    {
        await OpenAsync(Url);

        await Expect(Root("tg-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Root("tg-collapsed")).ToHaveCSSAsync("display", "none");
        await Expect(Root("tg-html-attrs")).ToHaveAttributeAsync("data-custom", "abc");
        await Expect(Root("tg-html-attrs")).ToHaveAttributeAsync("data-extra", "xyz");
    }

    [TestMethod]
    public async Task Disabled_DropsThePageClickHandlersUntilEnabledAgain()
    {
        await OpenAsync(Url);

        await Button("tg-handler-disabled").ClickAsync(new() { Force = true });
        await Label("tg-handler-disabled").DispatchEventAsync("click");
        await Root("tg-handler-disabled").DispatchEventAsync("click");

        await Button("tg-handler-enabled").ClickAsync();
        await Expect(Span("enabled-root-clicks")).ToHaveTextAsync("1");
        await Expect(Span("disabled-root-clicks")).ToHaveTextAsync("0");
        await Expect(Button("tg-handler-disabled")).ToHaveAttributeAsync("aria-checked", "false");

        await Page.Locator("#btn-toggle-handler-disabled").ClickAsync();
        await Expect(Root("tg-handler-disabled")).Not.ToHaveClassAsync(HasClass("bit-dis"));

        await Button("tg-handler-disabled").ClickAsync();
        await Expect(Span("disabled-root-clicks")).ToHaveTextAsync("1");
        await Expect(Button("tg-handler-disabled")).ToHaveAttributeAsync("aria-checked", "true");
    }

    [TestMethod]
    public async Task Runtime_ParameterChanges_ReRenderTheToggle()
    {
        await OpenAsync(Url);

        await Expect(Label("tg-runtime")).ToHaveTextAsync("Initial label");

        await Page.Locator("#btn-runtime-label").ClickAsync();
        await Expect(Label("tg-runtime")).ToHaveTextAsync("Changed label");
        await Expect(Page.GetByRole(AriaRole.Switch, new() { Name = "Changed label" })).ToHaveCountAsync(1);

        await Page.Locator("#btn-runtime-color").ClickAsync();
        await Expect(Root("tg-runtime")).ToHaveClassAsync(HasClass("bit-tgl-suc"));

        await Page.Locator("#btn-runtime-size").ClickAsync();
        await Expect(Root("tg-runtime")).ToHaveClassAsync(HasClass("bit-tgl-lg"));

        await Page.Locator("#btn-runtime-ontext").ClickAsync();
        await Page.Locator("#btn-runtime-value").ClickAsync();
        await Expect(Button("tg-runtime")).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(StateText("tg-runtime")).ToHaveTextAsync("Now on");
        await Expect(Button("tg-runtime")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-suc)"));

        await Page.Locator("#btn-runtime-disable").ClickAsync();
        await Expect(Root("tg-runtime")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Button("tg-runtime")).ToHaveAttributeAsync("disabled", "");

        await Page.Locator("#btn-runtime-disable").ClickAsync();
        await Expect(Button("tg-runtime")).Not.ToHaveAttributeAsync("disabled", AnyValue);

        await Button("tg-runtime").ClickAsync();
        await Expect(Span("runtime-value")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Unmount_RemovesTheToggleWithoutAnError()
    {
        await OpenAsync(Url);

        await Button("tg-runtime").ClickAsync();
        await Expect(Span("runtime-value")).ToHaveTextAsync("True");

        await Page.Locator("#btn-runtime-unmount").ClickAsync();
        await Expect(Root("tg-runtime")).ToHaveCountAsync(0);

        await Page.Locator("#btn-runtime-unmount").ClickAsync();
        await Expect(Button("tg-runtime")).ToHaveAttributeAsync("aria-checked", "true");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheKnobSlideUnlessForceAnimation()
    {
        await OpenAsync(Url);

        StringAssert.StartsWith(await StyleOfAsync(Thumb("tg-motion"), "transition-duration"), "0.1s");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        await Expect(Thumb("tg-motion")).Not.ToHaveCSSAsync("transition-duration", new Regex(@"0\.1s"));
        await Expect(Root("tg-motion-forced")).ToHaveClassAsync(HasClass("bit-fam"));
        StringAssert.StartsWith(await StyleOfAsync(Thumb("tg-motion-forced"), "transition-duration"), "0.1s");

        // A flip under reduced motion lands at once.
        await Button("tg-motion").ClickAsync();
        await WaitForThumbAsync("tg-motion", "right");
    }
}
