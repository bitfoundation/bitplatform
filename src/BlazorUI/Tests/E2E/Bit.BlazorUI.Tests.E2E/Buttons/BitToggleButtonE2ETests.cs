using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Buttons;

/// <summary>
/// The end-to-end tests of BitToggleButton against /e2e/toggle-button and /e2e/toggle-button/autofocus: toggling by
/// pointer and keyboard, every Variant checked and unchecked, Text/OnText/OffText and the titles, the icons per state
/// and IconPlacement, IconOnly and its name, DefaultIsChecked, the two-way bound IsChecked, OnChange, ToggleAsync and
/// FocusAsync, OnColor/OffColor and OnVariant/OffVariant, the check mark (ShowCheckMark, FixedCheckMark, a custom
/// icon), ChildContent and OnTemplate/OffTemplate, the order of OnClick, OnChanging and OnChange and the veto of
/// OnChanging, StopPropagation, the loading states (IsLoading, Reclickable, AutoLoading, LoadingDelay, the label
/// placements, the live region, LoadingTemplate), FullWidth and NoWrap, FixedColor, every AriaMode and the automatic
/// aria-pressed rule, OnAriaLabel/OffAriaLabel, AriaLabelledBy, AriaControls, a page role, AriaDescription, the
/// disabled contract, the BitParams cascade, every Color and Size, Style/Class/Styles/Classes, the public CSS
/// variables and their variant twins, RTL, Visibility, reduced motion, forced colors, prerendering, re-rendering,
/// AutoFocus and unmounting.
/// </summary>
[TestClass]
public class BitToggleButtonE2ETests : E2ETestBase
{
    private const string Transparent = "rgba(0, 0, 0, 0)";

    public static IEnumerable<object[]> ColorRows => ButtonsE2EHelpers.ColorRows;

    private ILocator Btn(string id) => Page.Locator($"#{id}");

    private Task<string> Token(string token) => Page.ResolveColorAsync($"var(--bit-clr-{token})");

    // ---------- toggling ----------

    [TestMethod]
    public async Task Basic_ClickedAndPressedFromTheKeyboard_TogglesTheCheckedState()
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn("tgb-basic");

        await Expect(button).ToHaveAttributeAsync("type", "button");
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-tgb-chk"));
        await Expect(button).ToHaveCSSAsync("background-color", await Token("pri"));

        await button.ClickAsync();
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(button).ToHaveClassAsync(HasClass("bit-tgb-chk"));
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(button).ToHaveCSSAsync("background-color", await Token("pri-dark"));

        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "false");

        await Page.Keyboard.PressAsync("Space");
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    [DataRow("default", "bit-tgb-fil", "Fill")]
    [DataRow("Fill", "bit-tgb-fil", "Fill")]
    [DataRow("Outline", "bit-tgb-otl", "Outline")]
    [DataRow("Text", "bit-tgb-txt", "Text")]
    public async Task Variant_EachValue_PaintsItsLookAndTheCheckedState(string variant, string cssClass, string look)
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn($"variant-{variant}");
        var primary = await Token("pri");

        await Expect(button).ToHaveClassAsync(HasClass(cssClass));
        await Expect(button).ToHaveCSSAsync("background-color", look == "Fill" ? primary : Transparent);
        await Expect(button).ToHaveCSSAsync("color", look == "Fill" ? await Token("pri-text") : primary);
        await Expect(button).ToHaveCSSAsync("border-top-color", look == "Text" ? Transparent : primary);

        var checkedButton = Btn($"variant-checked-{(variant == "default" ? "Fill" : variant)}");
        await Expect(checkedButton).ToHaveClassAsync(HasClass("bit-tgb-chk"));
        await Expect(checkedButton).ToHaveCSSAsync("background-color", await Token("pri-dark"));
        await Expect(checkedButton).ToHaveCSSAsync("color", await Token("pri-text"));

        await button.HoverAsync();
        await Expect(button).ToHaveCSSAsync("background-color", await Token("pri-hover"));
    }

    [TestMethod]
    [DataRow("Fill")]
    [DataRow("Outline")]
    [DataRow("Text")]
    public async Task Variant_DisabledEachLook_MutesItAndKeepsTheCheckedOneDimmed(string variant)
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn($"variant-dis-{variant}");

        await Expect(button).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).ToHaveCSSAsync("color", await Token("pri-dis-text"));
        await Expect(button).ToHaveCSSAsync("background-color", variant == "Fill" ? await Token("pri-dis") : Transparent);

        var disabledChecked = Btn("variant-dis-checked");
        await Expect(disabledChecked).ToHaveCSSAsync("background-color", await Token("pri-dark"));
        var opacity = await Page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).getPropertyValue('--bit-opa-dis').trim()");
        Assert.AreEqual(double.Parse(opacity, System.Globalization.CultureInfo.InvariantCulture), double.Parse(await disabledChecked.StyleAsync("opacity"), System.Globalization.CultureInfo.InvariantCulture), 0.001);
    }

    // ---------- texts, titles, icons ----------

    [TestMethod]
    public async Task Texts_TextOrOnOffText_LabelTheStates()
    {
        await OpenAsync("/e2e/toggle-button");

        var text = Btn("txt-text");
        await Expect(text.Locator(".bit-tgb-btx")).ToHaveTextAsync("Microphone");
        await text.ClickAsync();
        await Expect(text.Locator(".bit-tgb-btx")).ToHaveTextAsync("Microphone");

        var onOff = Btn("txt-onoff");
        await Expect(onOff.Locator(".bit-tgb-btx")).ToHaveTextAsync("Unmuted");
        await onOff.ClickAsync();
        await Expect(onOff.Locator(".bit-tgb-btx")).ToHaveTextAsync("Muted");
        await onOff.ClickAsync();
        await Expect(onOff.Locator(".bit-tgb-btx")).ToHaveTextAsync("Unmuted");
    }

    [TestMethod]
    public async Task Titles_TitleOrOnOffTitle_FillTheTooltipPerState()
    {
        await OpenAsync("/e2e/toggle-button");

        await Expect(Btn("txt-title")).ToHaveAttributeAsync("title", "Plain title");

        var titles = Btn("txt-titles");
        await Expect(titles).ToHaveAttributeAsync("title", "Mute");
        await Expect(titles).ToHaveAccessibleNameAsync("Microphone");
        await titles.ClickAsync();
        await Expect(titles).ToHaveAttributeAsync("title", "Unmute");
        await Expect(titles).ToHaveAccessibleNameAsync("Microphone");
    }

    [TestMethod]
    public async Task Icons_PerStateAndPlaced_FollowTheStateAndThePlacement()
    {
        await OpenAsync("/e2e/toggle-button");

        await Expect(Btn("ico-both").Locator(".bit-tgb-ico")).ToHaveClassAsync(HasClass("bit-icon--Microphone"));

        var onOff = Btn("ico-onoff");
        await Expect(onOff.Locator(".bit-tgb-ico")).ToHaveClassAsync(HasClass("bit-icon--Microphone"));
        await onOff.ClickAsync();
        await Expect(onOff.Locator(".bit-tgb-ico")).ToHaveClassAsync(HasClass("bit-icon--MicOff"));

        var start = Btn("ico-start");
        Assert.IsTrue((await start.Locator(".bit-tgb-ico").BoxAsync()).X < (await start.Locator(".bit-tgb-btx").BoxAsync()).X);

        var end = Btn("ico-end");
        await Expect(end).ToHaveClassAsync(HasClass("bit-tgb-eni"));
        Assert.IsTrue((await end.Locator(".bit-tgb-ico").BoxAsync()).X > (await end.Locator(".bit-tgb-btx").BoxAsync()).X);

        var ext = Btn("ico-ext");
        await Expect(ext.Locator(".bit-tgb-ico")).ToHaveClassAsync(HasClass("fa-house"));
        await ext.ClickAsync();
        await Expect(ext.Locator(".bit-tgb-ico")).ToHaveClassAsync(HasClass("bi-house-fill"));
    }

    [TestMethod]
    public async Task IconOnly_WithATextOrAnAriaLabel_DrawsTheIconAloneAndKeepsAName()
    {
        await OpenAsync("/e2e/toggle-button");

        var withText = Btn("ico-only-text");
        await Expect(withText).ToHaveClassAsync(HasClass("bit-tgb-ntx"));
        await Expect(withText.Locator(".bit-tgb-btx")).ToHaveCountAsync(0);
        await Expect(withText).ToHaveAttributeAsync("aria-label", "Microphone");
        await Expect(withText).ToHaveAccessibleNameAsync("Microphone");

        var withLabel = Btn("ico-only-aria");
        await Expect(withLabel).ToHaveAccessibleNameAsync("Mute");
        var box = await withLabel.BoxAsync();
        Assert.IsTrue(box.Width >= box.Height - 0.5);
    }

    // ---------- binding ----------

    [TestMethod]
    public async Task DefaultIsChecked_SeedsTheStateOnceAndLeavesItToTheButton()
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn("bind-default");
        await Expect(button).ToHaveClassAsync(HasClass("bit-tgb-chk"));
        await Expect(button.Locator(".bit-tgb-btx")).ToHaveTextAsync("On");

        await button.ClickAsync();
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-tgb-chk"));
        await Expect(button.Locator(".bit-tgb-btx")).ToHaveTextAsync("Off");
    }

    [TestMethod]
    public async Task IsChecked_BoundTwoWay_FollowsTheButtonAndThePage()
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn("bind-twoway");

        await button.ClickAsync();
        await Expect(Btn("bind-value")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#bind-checkbox")).ToBeCheckedAsync();

        await Page.Locator("#bind-checkbox").UncheckAsync();
        await Expect(Btn("bind-value")).ToHaveTextAsync("False");
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-tgb-chk"));
        await Expect(button.Locator(".bit-tgb-btx")).ToHaveTextAsync("Unchecked");

        await Page.Locator("#bind-checkbox").CheckAsync();
        await Expect(button).ToHaveClassAsync(HasClass("bit-tgb-chk"));
    }

    [TestMethod]
    public async Task OnChange_OnEveryToggle_ReportsTheNewState()
    {
        await OpenAsync("/e2e/toggle-button");

        await Btn("bind-onchange").ClickAsync();
        await Expect(Btn("bind-onchange-value")).ToHaveTextAsync("True");
        await Btn("bind-onchange").ClickAsync();
        await Expect(Btn("bind-onchange-value")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task ToggleAsyncAndFocusAsync_CalledFromCode_ToggleAndFocusTheButton()
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn("bind-programmatic");

        await Page.Locator("#btn-toggle-async").ClickAsync();
        await Expect(button.Locator(".bit-tgb-btx")).ToHaveTextAsync("Muted");
        await Expect(Btn("programmatic-changes")).ToHaveTextAsync("1");

        await Page.Locator("#btn-toggle-async").ClickAsync();
        await Expect(button.Locator(".bit-tgb-btx")).ToHaveTextAsync("Unmuted");
        await Expect(Btn("programmatic-changes")).ToHaveTextAsync("2");

        await Page.Locator("#btn-focus-async").ClickAsync();
        await Expect(button).ToBeFocusedAsync();
    }

    // ---------- appearance ----------

    [TestMethod]
    public async Task OnOffColorAndVariant_ChangeTheAppearanceWithTheState()
    {
        await OpenAsync("/e2e/toggle-button");

        var color = Btn("app-color");
        await Expect(color).ToHaveClassAsync(HasClass("bit-tgb-err"));
        await Expect(color).ToHaveCSSAsync("background-color", await Token("err"));
        await color.ClickAsync();
        await Expect(color).ToHaveClassAsync(HasClass("bit-tgb-suc"));
        await Expect(color).Not.ToHaveClassAsync(HasClass("bit-tgb-err"));
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(color).ToHaveCSSAsync("background-color", await Token("suc-dark"));

        var variant = Btn("app-variant");
        await Expect(variant).ToHaveClassAsync(HasClass("bit-tgb-otl"));
        await variant.ClickAsync();
        await Expect(variant).ToHaveClassAsync(HasClass("bit-tgb-fil"));

        var fallback = Btn("app-fallback");
        await Expect(fallback).ToHaveClassAsync(HasClass("bit-tgb-inf"));
        await Expect(fallback).ToHaveClassAsync(HasClass("bit-tgb-txt"));
        await fallback.ClickAsync();
        await Expect(fallback).ToHaveClassAsync(HasClass("bit-tgb-wrn"));
        await Expect(fallback).ToHaveClassAsync(HasClass("bit-tgb-txt"));
    }

    [TestMethod]
    public async Task CheckMark_ShownWhenCheckedReservedWhenFixedAndReplaceable()
    {
        await OpenAsync("/e2e/toggle-button");

        var checkedOne = Btn("chm-checked").Locator(".bit-tgb-chm");
        await Expect(checkedOne).ToBeVisibleAsync();
        await Expect(checkedOne).ToHaveClassAsync(HasClass("bit-icon--Accept"));
        await Expect(checkedOne).ToHaveAttributeAsync("aria-hidden", "true");

        var notChecked = Btn("chm-unchecked");
        await Expect(notChecked.Locator(".bit-tgb-chm")).ToHaveCountAsync(0);
        await notChecked.ClickAsync();
        await Expect(notChecked.Locator(".bit-tgb-chm")).ToBeVisibleAsync();

        var fixedMark = Btn("chm-fixed");
        var reserved = fixedMark.Locator(".bit-tgb-chm");
        await Expect(reserved).ToHaveClassAsync(HasClass("bit-tgb-chmh"));
        await Expect(reserved).ToHaveCSSAsync("visibility", "hidden");
        var before = await fixedMark.BoxAsync();
        await fixedMark.ClickAsync();
        await Expect(reserved).ToHaveCSSAsync("visibility", "visible");
        ButtonsE2EHelpers.AreClose(before.Width, (await fixedMark.BoxAsync()).Width, 0.5, "FixedCheckMark keeps the width");

        await Expect(Btn("chm-custom").Locator(".bit-tgb-chm")).ToHaveClassAsync(HasClass("bit-icon--FavoriteStarFill"));

        // IconPlacement moves the icon, never the check mark, which stays at the leading edge
        var end = Btn("chm-end");
        var text = await end.Locator(".bit-tgb-btx").BoxAsync();
        Assert.IsTrue((await end.Locator(".bit-tgb-chm").BoxAsync()).X < text.X, "The check mark stays before the content.");
        Assert.IsTrue((await end.Locator(".bit-tgb-ico").BoxAsync()).X > text.X, "The icon goes after the content.");
    }

    [TestMethod]
    public async Task Templates_ChildContentOrPerState_ReplaceTheBody()
    {
        await OpenAsync("/e2e/toggle-button");

        await Expect(Btn("tpl-child").Locator(".tpl-child-content")).ToHaveTextAsync("Child content");

        var onOff = Btn("tpl-onoff");
        await Expect(onOff.Locator(".tpl-off")).ToHaveTextAsync("Subscribe");
        await Expect(onOff).ToHaveAccessibleNameAsync("Subscribe to this channel");
        await onOff.ClickAsync();
        await Expect(onOff.Locator(".tpl-on")).ToHaveTextAsync("Subscribed");
        await Expect(onOff.Locator(".tpl-off")).ToHaveCountAsync(0);
        await Expect(onOff).ToHaveAttributeAsync("aria-pressed", "true");
    }

    // ---------- events ----------

    [TestMethod]
    public async Task Events_AroundAClick_FireOnClickThenOnChangingThenOnChange()
    {
        await OpenAsync("/e2e/toggle-button");

        await Btn("evt-order").ClickAsync();
        await Expect(Btn("evt-log")).ToHaveTextAsync("click:False,changing:True:False,change:True:True");

        await Btn("evt-order").ClickAsync();
        await Expect(Btn("evt-log")).ToHaveTextAsync("click:False,changing:True:False,change:True:True,click:True,changing:False:True,change:False:False");
    }

    [TestMethod]
    public async Task OnChanging_SettingCancel_VetoesTheChange()
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn("evt-cancel");

        await button.ClickAsync();
        await Expect(Btn("evt-cancelled")).ToHaveTextAsync("1");
        await Expect(button.Locator(".bit-tgb-btx")).ToHaveTextAsync("Draft");
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-tgb-chk"));

        await Page.Locator("#evt-allow").CheckAsync();
        await button.ClickAsync();
        await Expect(button.Locator(".bit-tgb-btx")).ToHaveTextAsync("Published");
        await Expect(Btn("evt-cancelled")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task StopPropagation_SetOrNot_KeepsTheClickFromTheParentOrLetsItBubble()
    {
        await OpenAsync("/e2e/toggle-button");

        await Btn("evt-stop").ClickAsync();
        await Expect(Btn("evt-stop")).ToHaveClassAsync(HasClass("bit-tgb-chk"));
        await Btn("evt-bubble").ClickAsync();
        await Expect(Btn("evt-bubble")).ToHaveClassAsync(HasClass("bit-tgb-chk"));
        await Expect(Btn("evt-parent-clicks")).ToHaveTextAsync("1");
    }

    // ---------- loading ----------

    [TestMethod]
    public async Task IsLoading_TurnedOn_CoversTheContentAnnouncesItAndRefusesTheClick()
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn("ld-toggle");
        var status = Page.Locator("#ld-toggle + .bit-tgb-sts");

        await Expect(status).ToHaveTextAsync("");
        await Page.Locator("#btn-toggle-loading").ClickAsync();

        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(button).ToHaveClassAsync(HasClass("bit-tgb-lod"));
        await Expect(button).ToHaveClassAsync(HasClass("bit-tgb-lda"));
        await Expect(button).ToHaveCSSAsync("cursor", "default");
        await Expect(button.Locator(".bit-tgb-spn")).ToBeVisibleAsync();
        await Expect(button.Locator(".bit-tgb-lbl")).ToHaveTextAsync("Saving...");
        await Expect(button.Locator(".bit-tgb-ldg")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(button.Locator(".bit-tgb-hcn")).ToHaveCSSAsync("opacity", "0");
        await Expect(button).ToHaveAccessibleNameAsync("Unmuted");
        await Expect(status).ToHaveTextAsync("Saving...");

        var before = await button.StyleAsync("background-color");
        await button.HoverAsync(new() { Force = true });
        await NextFramesAsync();
        Assert.AreEqual(before, await button.StyleAsync("background-color"), "A blocked toggle button keeps no hover shade.");

        await button.ClickAsync(new() { Force = true });
        await Page.Locator("#btn-toggle-loading").ClickAsync();

        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);
        await Expect(button.Locator(".bit-tgb-spn")).ToHaveCountAsync(0);
        await Expect(button.Locator(".bit-tgb-btx")).ToHaveTextAsync("Unmuted");
        await Expect(Btn("ld-clicks")).ToHaveTextAsync("0");

        await button.ClickAsync();
        await Expect(button.Locator(".bit-tgb-btx")).ToHaveTextAsync("Muted");
        await Expect(Btn("ld-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Reclickable_WhileLoading_StillTogglesAndRaisesOnClick()
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn("ld-reclickable");

        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(button).Not.ToHaveAttributeAsync("aria-disabled", ButtonsE2EHelpers.AnyValue);
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-tgb-lod"));
        await Expect(button).ToHaveCSSAsync("cursor", "pointer");

        await button.ClickAsync();
        await Expect(Btn("ld-reclicks")).ToHaveTextAsync("1");
        await Expect(button).ToHaveClassAsync(HasClass("bit-tgb-chk"));
        await button.ClickAsync();
        await Expect(Btn("ld-reclicks")).ToHaveTextAsync("2");
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-tgb-chk"));
    }

    [TestMethod]
    public async Task AutoLoading_WhileOnChangeRuns_IsLoadingAndRefusesClicks()
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn("ld-auto");

        await button.ClickAsync();
        await Expect(Btn("ld-auto-loading")).ToHaveTextAsync("True");
        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(button.Locator(".bit-tgb-spn")).ToBeVisibleAsync();
        await Expect(button).ToHaveClassAsync(HasClass("bit-tgb-chk"));

        await button.ClickAsync(new() { Force = true });
        await Page.Locator("#btn-release-auto").ClickAsync();

        await Expect(Btn("ld-auto-loading")).ToHaveTextAsync("False");
        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);
        await Expect(button).ToHaveClassAsync(HasClass("bit-tgb-chk"));
        await Expect(button.Locator(".bit-tgb-btx")).ToHaveTextAsync("Muted");
    }

    [TestMethod]
    public async Task LoadingDelay_ALongRun_ShowsTheSpinnerOnlyOnceTheDelayIsUp()
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn("ld-delay");

        await button.EvaluateAsync(
            """
            el => {
                window.__delay = {};
                const start = performance.now();
                new MutationObserver(() => {
                    if (el.getAttribute('aria-busy') === 'true' && window.__delay.busy === undefined) window.__delay.busy = performance.now() - start;
                    if (el.querySelector('.bit-tgb-spn') && window.__delay.spinner === undefined) window.__delay.spinner = performance.now() - start;
                }).observe(el, { attributes: true, childList: true, subtree: true });
            }
            """);

        await button.ClickAsync();
        await Expect(button.Locator(".bit-tgb-spn")).ToBeVisibleAsync();

        var busy = await Page.EvaluateAsync<double>("() => window.__delay.busy");
        var spinner = await Page.EvaluateAsync<double>("() => window.__delay.spinner");
        Assert.IsTrue(spinner - busy >= 450, $"The spinner came {spinner - busy}ms after the busy state; the delay is 500ms.");

        await Page.Locator("#btn-release-delay").ClickAsync();
        await Expect(button.Locator(".bit-tgb-spn")).ToHaveCountAsync(0);
    }

    [TestMethod]
    [DataRow("End", "row")]
    [DataRow("Start", "row-reverse")]
    [DataRow("Top", "column-reverse")]
    [DataRow("Bottom", "column")]
    public async Task LoadingLabelPlacement_EachValue_PlacesTheLabelAroundTheSpinner(string placement, string direction)
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn($"ld-placement-{placement}");
        await Expect(button.Locator(".bit-tgb-ldg")).ToHaveCSSAsync("flex-direction", direction);

        var spinner = await button.Locator(".bit-tgb-spn").BoxAsync();
        var label = await button.Locator(".bit-tgb-lbl").BoxAsync();

        switch (placement)
        {
            case "End": Assert.IsTrue(label.X >= spinner.X + spinner.Width - 0.5); break;
            case "Start": Assert.IsTrue(label.X + label.Width <= spinner.X + 0.5); break;
            case "Top": Assert.IsTrue(label.Y + label.Height <= spinner.Y + 0.5); break;
            default: Assert.IsTrue(label.Y >= spinner.Y + spinner.Height - 0.5); break;
        }
    }

    [TestMethod]
    public async Task Loading_IconOnlyAndTemplate_KeepTheSquareOrRenderThePagesOwnVisual()
    {
        await OpenAsync("/e2e/toggle-button");

        var iconOnly = Btn("ld-icon-only");
        await Expect(iconOnly.Locator(".bit-tgb-spn")).ToBeVisibleAsync();
        await Expect(iconOnly.Locator(".bit-tgb-lbl")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#ld-icon-only + .bit-tgb-sts")).ToHaveTextAsync("Saving...");

        var template = Btn("ld-template");
        await Expect(template.Locator(".tpl-loading")).ToHaveTextAsync("Working on it...");
        await Expect(template.Locator(".bit-tgb-spn")).ToHaveCountAsync(0);
        await Expect(template.Locator(".tpl-loading")).Not.ToHaveAttributeAsync("aria-hidden", ButtonsE2EHelpers.AnyValue);
    }

    // ---------- layout ----------

    [TestMethod]
    public async Task FullWidthAndNoWrap_StretchTheButtonAndClampItsText()
    {
        await OpenAsync("/e2e/toggle-button");

        await Expect(Btn("fw-btn")).ToHaveClassAsync(HasClass("bit-tgb-flw"));
        ButtonsE2EHelpers.AreClose((await Btn("fw-container").BoxAsync()).Width, (await Btn("fw-btn").BoxAsync()).Width, 0.5, "full width");

        var noWrap = Btn("fw-nowrap");
        await Expect(noWrap).ToHaveClassAsync(HasClass("bit-tgb-nwr"));
        await Expect(noWrap.Locator(".bit-tgb-btx")).ToHaveCSSAsync("text-overflow", "ellipsis");
        await Expect(noWrap.Locator(".bit-tgb-btx")).ToHaveCSSAsync("white-space", "nowrap");
        Assert.IsTrue(await noWrap.Locator(".bit-tgb-btx").EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"));
        Assert.IsTrue((await Btn("fw-wrap").BoxAsync()).Height > (await noWrap.BoxAsync()).Height);
    }

    [TestMethod]
    public async Task FixedColor_OnOutlineAndText_PaintsTheOnColorAtRestAndOnHover()
    {
        await OpenAsync("/e2e/toggle-button");

        var on = await Token("fg-pri");

        await Expect(Btn("fxc-notfixed")).ToHaveCSSAsync("color", await Token("bg-ter"));
        await Expect(Btn("fxc-fixed")).ToHaveClassAsync(HasClass("bit-tgb-fxc"));
        await Expect(Btn("fxc-fixed")).ToHaveCSSAsync("color", on);
        await Expect(Btn("fxc-fixed-text")).ToHaveCSSAsync("color", on);

        await Btn("fxc-fixed").HoverAsync();
        await Expect(Btn("fxc-fixed")).ToHaveCSSAsync("background-color", await Token("bg-ter-hover"));
        await Expect(Btn("fxc-fixed")).ToHaveCSSAsync("color", on);
    }

    // ---------- accessibility ----------

    [TestMethod]
    public async Task AriaPressed_Automatic_IsDroppedWhenTheNameCarriesTheStateUnlessTheNameIsPinned()
    {
        await OpenAsync("/e2e/toggle-button");

        await Expect(Btn("a11y-auto-stable")).ToHaveAttributeAsync("aria-pressed", "false");

        var changing = Btn("a11y-auto-changing");
        await Expect(changing).Not.ToHaveAttributeAsync("aria-pressed", ButtonsE2EHelpers.AnyValue);
        await changing.ClickAsync();
        await Expect(changing).ToHaveAccessibleNameAsync("Muted");
        await Expect(changing).Not.ToHaveAttributeAsync("aria-pressed", ButtonsE2EHelpers.AnyValue);

        var pinned = Btn("a11y-auto-pinned");
        await Expect(pinned).ToHaveAttributeAsync("aria-pressed", "false");
        await pinned.ClickAsync();
        await Expect(pinned).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(pinned).ToHaveAccessibleNameAsync("Mute");
    }

    [TestMethod]
    public async Task AriaMode_EachValue_RendersTheStateAttributeItNames()
    {
        await OpenAsync("/e2e/toggle-button");

        var pressed = Btn("a11y-pressed");
        await Expect(pressed).ToHaveAttributeAsync("aria-pressed", "false");
        await pressed.ClickAsync();
        await Expect(pressed).ToHaveAttributeAsync("aria-pressed", "true");

        var toggleSwitch = Btn("a11y-switch");
        await Expect(toggleSwitch).ToHaveAttributeAsync("role", "switch");
        await Expect(toggleSwitch).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(toggleSwitch).Not.ToHaveAttributeAsync("aria-pressed", ButtonsE2EHelpers.AnyValue);
        await Expect(Page.GetByRole(AriaRole.Switch, new() { Name = "Airplane mode" })).ToHaveCountAsync(1);
        await toggleSwitch.ClickAsync();
        await Expect(toggleSwitch).ToHaveAttributeAsync("aria-checked", "true");

        var none = Btn("a11y-none");
        await Expect(none).Not.ToHaveAttributeAsync("aria-pressed", ButtonsE2EHelpers.AnyValue);
        await Expect(none).Not.ToHaveAttributeAsync("aria-checked", ButtonsE2EHelpers.AnyValue);

        var expanded = Btn("a11y-expanded");
        await Expect(expanded).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(expanded).ToHaveAttributeAsync("aria-controls", "a11y-details");
        await expanded.ClickAsync();
        await Expect(expanded).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(expanded).Not.ToHaveAttributeAsync("aria-pressed", ButtonsE2EHelpers.AnyValue);
    }

    [TestMethod]
    public async Task AriaLabels_PerStateLabelledByAndAPageRole_NameTheButtonAndCarryTheState()
    {
        await OpenAsync("/e2e/toggle-button");

        var onOff = Btn("a11y-onoff-label");
        await Expect(onOff).ToHaveAccessibleNameAsync("Mute");
        await onOff.ClickAsync();
        await Expect(onOff).ToHaveAccessibleNameAsync("Unmute");

        var labelledBy = Btn("a11y-labelledby");
        await Expect(labelledBy).ToHaveAttributeAsync("aria-labelledby", "a11y-wifi-label");
        await Expect(labelledBy).ToHaveAccessibleNameAsync("Wi-Fi");

        var role = Btn("a11y-role");
        await Expect(role).ToHaveAttributeAsync("role", "checkbox");
        await Expect(role).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(role).Not.ToHaveAttributeAsync("aria-pressed", ButtonsE2EHelpers.AnyValue);
        await role.ClickAsync();
        await Expect(role).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Page.GetByRole(AriaRole.Checkbox, new() { Name = "Show the status bar" })).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task AriaDescriptionHiddenAndTabIndex_AreRendered()
    {
        await OpenAsync("/e2e/toggle-button");

        var described = Btn("a11y-desc");
        await Expect(described).ToHaveAttributeAsync("aria-describedby", "a11y-extra a11y-desc-dsc");
        await Expect(described).ToHaveAccessibleDescriptionAsync("Extra Turns the microphone off");

        var hidden = Btn("a11y-hidden");
        await Expect(hidden).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(hidden).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Page.Locator("#a11y-hidden + .bit-tgb-sts")).ToHaveCountAsync(0);

        await Expect(Btn("a11y-tabindex")).ToHaveAttributeAsync("tabindex", "-1");
    }

    [TestMethod]
    public async Task AutoFocus_OnTheFirstRender_FocusesTheButtonButNeverAHiddenOrUnfocusableOne()
    {
        await OpenAsync("/e2e/toggle-button/autofocus");

        await Expect(Btn("autofocus-hidden")).Not.ToHaveAttributeAsync("autofocus", ButtonsE2EHelpers.AnyValue);
        await Expect(Btn("autofocus-disabled")).Not.ToHaveAttributeAsync("autofocus", ButtonsE2EHelpers.AnyValue);
        await Expect(Btn("autofocus-btn")).ToHaveAttributeAsync("autofocus", "");
        await Expect(Btn("autofocus-btn")).ToBeFocusedAsync();
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_ByDefault_StaysFocusableAndRunsNoHandler()
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn("dis-focusable");

        await Expect(button).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(button).Not.ToHaveAttributeAsync("disabled", ButtonsE2EHelpers.AnyValue);
        await Expect(button).ToHaveAttributeAsync("title", "Connect a microphone to enable this");

        await Page.Locator("#dis-before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(button).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Space");
        await button.ClickAsync(new() { Force = true });
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#dis-after")).ToBeFocusedAsync();

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Btn("dis-toggle").ClickAsync();
        await Expect(Btn("dis-toggle-changes")).ToHaveTextAsync("1");

        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-tgb-chk"));
        await Expect(Btn("dis-changes")).ToHaveTextAsync("0");
        await Expect(Btn("dis-page-events")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Disabled_WithoutAllowDisabledFocus_RendersTheNativeAttribute()
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn("dis-native");

        await Expect(button).ToBeDisabledAsync();
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(button).ToHaveAttributeAsync("tabindex", "-1");
    }

    [TestMethod]
    public async Task Disabled_ToggledAtRuntime_RestoresTheHandlers()
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn("dis-toggle");
        await button.ClickAsync(new() { Force = true });

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Btn("dis-toggle-changes")).ToHaveTextAsync("0");
        await Expect(Btn("dis-toggle-page-events")).ToHaveTextAsync("0");

        await button.ClickAsync();
        await Expect(Btn("dis-toggle-changes")).ToHaveTextAsync("1");
        await Expect(Btn("dis-toggle-page-events")).ToHaveTextAsync("1");
        await Expect(button).ToHaveClassAsync(HasClass("bit-tgb-chk"));
    }

    // ---------- params, color, size ----------

    [TestMethod]
    public async Task BitParams_AroundToggleButtons_FillsInWhatEachLeftUnset()
    {
        await OpenAsync("/e2e/toggle-button");

        var toolbar = Btn("params-toolbar");
        await Expect(toolbar).ToHaveClassAsync(HasClass("bit-tgb-ntx"));
        await Expect(toolbar).ToHaveClassAsync(HasClass("bit-tgb-otl"));
        await Expect(toolbar.Locator(".bit-tgb-btx")).ToHaveCountAsync(0);
        await Expect(toolbar).ToHaveAccessibleNameAsync("Bold");

        var own = Btn("params-own");
        await Expect(own).ToHaveClassAsync(HasClass("bit-tgb-txt"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-tgb-ntx"));

        await Expect(Btn("params-checked")).ToHaveClassAsync(HasClass("bit-tgb-chk"));
        await Expect(Btn("params-checked")).ToHaveAttributeAsync("aria-pressed", "true");

        var outside = Btn("params-outside");
        await Expect(outside).ToHaveClassAsync(HasClass("bit-tgb-fil"));
        await Expect(outside.Locator(".bit-tgb-btx")).ToHaveTextAsync("Outside");
    }

    [TestMethod]
    [DynamicData(nameof(ColorRows))]
    public async Task Color_EachValue_PaintsRestCheckedDisabledAndHoverFromItsRole(string name, string suffix, string token, string onToken)
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn($"color-{name}");
        await Expect(button).ToHaveClassAsync(HasClass($"bit-tgb-{suffix}"));
        await Expect(button).ToHaveCSSAsync("background-color", await Token(token));
        await Expect(button).ToHaveCSSAsync("color", await Token(onToken));

        var checkedButton = Btn($"color-checked-{name}");
        await Expect(checkedButton).ToHaveCSSAsync("background-color", await Token($"{token}-dark"));
        await Expect(checkedButton).ToHaveCSSAsync("color", await Token(onToken));

        var disabled = Btn($"color-dis-{name}");
        await Expect(disabled).ToHaveCSSAsync("background-color", await Token($"{token}-dis"));
        await Expect(disabled).ToHaveCSSAsync("color", await Token($"{token}-dis-text"));

        await button.HoverAsync();
        await Expect(button).ToHaveCSSAsync("background-color", await Token($"{token}-hover"));
    }

    [TestMethod]
    [DataRow("Small", "sm", "xs", 2.00)]
    [DataRow("Medium", "md", "sm", 2.35)]
    [DataRow("Large", "lg", "md", 2.75)]
    public async Task Size_EachValue_SetsTheHeightTypeIconAndSpinnerOfItsClass(string size, string suffix, string font, double iconSteps)
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn($"size-{size}");
        await Expect(button).ToHaveClassAsync(HasClass($"bit-tgb-{suffix}"));
        Assert.AreEqual(await Page.ResolvePxAsync(ButtonsE2EHelpers.ControlHeight(suffix)), await button.PxAsync("min-height"), "min-height");
        Assert.AreEqual(await Page.ResolvePxAsync($"var(--bit-tpg-fs-{font})"), await button.PxAsync("font-size"), "font-size");

        var glyph = await Page.ResolvePxAsync(ButtonsE2EHelpers.Spacing(iconSteps));
        Assert.AreEqual(glyph, await Btn($"size-icon-{size}").Locator(".bit-tgb-ico").PxAsync("font-size"), 0.07, "icon-only glyph size (Firefox snaps a font size to 1/16px)");
        Assert.AreEqual(glyph, await Btn($"size-loading-{size}").Locator(".bit-tgb-spn").PxAsync("width"), 0.07, "spinner size");
    }

    // ---------- styles and variables ----------

    [TestMethod]
    public async Task StyleClassStylesAndClasses_ReachTheRootAndEveryPart()
    {
        await OpenAsync("/e2e/toggle-button");

        await Expect(Btn("sc-style")).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(Btn("sc-class")).ToHaveClassAsync(HasClass("e2e-custom-class"));
        await Expect(Btn("sc-attrs")).ToHaveAttributeAsync("data-e2e", "custom-attribute");

        var styles = Btn("sc-styles");
        await Expect(styles).ToHaveCSSAsync("outline-style", "dotted");
        await Expect(styles).ToHaveCSSAsync("border-top-color", "rgb(0, 0, 255)");
        await Expect(styles.Locator(".bit-tgb-ico")).ToHaveCSSAsync("color", "rgb(255, 0, 0)");
        await Expect(styles.Locator(".bit-tgb-btx")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");
        await Expect(styles.Locator(".bit-tgb-chm")).ToHaveCSSAsync("color", "rgb(128, 0, 128)");
        await styles.ClickAsync();
        await Expect(styles).Not.ToHaveCSSAsync("border-top-color", "rgb(0, 0, 255)");

        var stylesLoading = Btn("sc-styles-loading");
        await Expect(stylesLoading.Locator(".bit-tgb-hcn")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(stylesLoading.Locator(".bit-tgb-ldg")).ToHaveCSSAsync("padding-top", "5px");
        await Expect(stylesLoading.Locator(".bit-tgb-spn")).ToHaveCSSAsync("border-top-color", "rgb(218, 165, 32)");
        await Expect(stylesLoading.Locator(".bit-tgb-lbl")).ToHaveCSSAsync("color", "rgb(255, 99, 71)");

        var classes = Btn("sc-classes");
        await Expect(classes).ToHaveClassAsync(HasClass("c-root"));
        await Expect(classes).ToHaveClassAsync(HasClass("c-checked"));
        await Expect(classes.Locator(".bit-tgb-ico")).ToHaveClassAsync(HasClass("c-icon"));
        await Expect(classes.Locator(".bit-tgb-btx")).ToHaveClassAsync(HasClass("c-text"));
        await Expect(classes.Locator(".bit-tgb-chm")).ToHaveClassAsync(HasClass("c-checkmark"));
        await classes.ClickAsync();
        await Expect(classes).Not.ToHaveClassAsync(HasClass("c-checked"));

        var classesLoading = Btn("sc-classes-loading");
        await Expect(classesLoading.Locator(".bit-tgb-hcn")).ToHaveClassAsync(HasClass("c-hidden"));
        await Expect(classesLoading.Locator(".bit-tgb-ldg")).ToHaveClassAsync(HasClass("c-ldg"));
        await Expect(classesLoading.Locator(".bit-tgb-spn")).ToHaveClassAsync(HasClass("c-spinner"));
        await Expect(classesLoading.Locator(".bit-tgb-lbl")).ToHaveClassAsync(HasClass("c-label"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheDefaultsAndLoseToExplicitParameters()
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn("vars-default");
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(button).ToHaveCSSAsync("color", "rgb(250, 240, 230)");
        await Expect(button).ToHaveCSSAsync("border-top-left-radius", "0px");
        await Expect(button).ToHaveCSSAsync("min-height", "50px");
        await Expect(button).ToHaveCSSAsync("min-width", "211px");
        await Expect(button).ToHaveCSSAsync("font-size", "19px");
        await Expect(button).ToHaveCSSAsync("font-weight", "700");
        await Expect(button).ToHaveCSSAsync("border-top-width", "3px");
        await Expect(button).ToHaveCSSAsync("padding-left", "25px");
        await Expect(button).ToHaveCSSAsync("column-gap", "14px");
        await Expect(button.Locator(".bit-tgb-ico")).ToHaveCSSAsync("font-size", "24px");

        await Expect(Btn("vars-checked")).ToHaveCSSAsync("background-color", "rgb(0, 100, 0)");
        await Expect(Btn("vars-checked")).ToHaveCSSAsync("color", "rgb(255, 255, 0)");
        await Expect(Btn("vars-color")).ToHaveCSSAsync("background-color", await Token("err"));
        Assert.AreEqual(await Page.ResolvePxAsync(ButtonsE2EHelpers.ControlHeight("sm")), await Btn("vars-size").PxAsync("min-height"));
        await Expect(Btn("vars-disabled")).ToHaveCSSAsync("background-color", "rgb(77, 77, 77)");
        await Expect(Btn("vars-disabled-checked")).ToHaveCSSAsync("opacity", "0.25");
        await Expect(Btn("vars-spinner").Locator(".bit-tgb-spn")).ToHaveCSSAsync("width", "30px");

        await button.HoverAsync();
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(200, 100, 0)");
    }

    [TestMethod]
    public async Task CssVariables_VariantTwins_RestyleTheirOwnLookOnly()
    {
        await OpenAsync("/e2e/toggle-button");

        await Expect(Btn("vars-twin-outline")).ToHaveCSSAsync("border-top-color", "rgb(200, 0, 0)");
        await Expect(Btn("vars-twin-text")).ToHaveCSSAsync("color", "rgb(0, 0, 200)");
        await Expect(Btn("vars-twin-fill")).Not.ToHaveCSSAsync("border-top-color", "rgb(200, 0, 0)");
        await Expect(Btn("vars-twin-fill")).Not.ToHaveCSSAsync("color", "rgb(0, 0, 200)");
    }

    // ---------- direction, visibility, motion ----------

    [TestMethod]
    public async Task Rtl_ExplicitOrInherited_PutsTheIconOnTheRight()
    {
        await OpenAsync("/e2e/toggle-button");

        async Task<bool> IconAfterText(string id)
        {
            var button = Btn(id);
            return (await button.Locator(".bit-tgb-ico").BoxAsync()).X > (await button.Locator(".bit-tgb-btx").BoxAsync()).X;
        }

        Assert.IsFalse(await IconAfterText("rtl-ltr"));
        await Expect(Btn("rtl-explicit")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Btn("rtl-explicit")).ToHaveClassAsync(HasClass("bit-rtl"));
        Assert.IsTrue(await IconAfterText("rtl-explicit"));
        Assert.IsFalse(await IconAfterText("rtl-explicit-end"));
        Assert.IsTrue(await IconAfterText("rtl-inherited"));
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed_HideTheButton()
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn("vis-btn");
        await Page.Locator("#btn-vis-hidden").ClickAsync();
        await Expect(button).ToHaveCSSAsync("visibility", "hidden");
        await Page.Locator("#btn-vis-collapsed").ClickAsync();
        await Expect(button).ToHaveCSSAsync("display", "none");
        await Page.Locator("#btn-vis-visible").ClickAsync();
        await Expect(button).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ReducedMotion_Requested_CollapsesTheTransitionsUnlessForceAnimationIsSet()
    {
        await OpenAsync("/e2e/toggle-button");

        var full = ButtonsE2EHelpers.ParseSeconds(await Btn("motion-default").StyleAsync("transition-duration"));
        Assert.IsTrue(full >= 0.05);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        Assert.IsTrue(ButtonsE2EHelpers.ParseSeconds(await Btn("motion-default").StyleAsync("transition-duration")) < 0.001);
        Assert.AreEqual(full, ButtonsE2EHelpers.ParseSeconds(await Btn("motion-forced").StyleAsync("transition-duration")), 0.0001);
    }

    [TestMethod]
    public async Task ForcedColors_Active_MarksTheCheckedStateWithHighlight()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync("/e2e/toggle-button");
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        await Expect(Btn("variant-checked-Fill")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("Highlight"));
        await Expect(Btn("variant-dis-Fill")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("GrayText"));
    }

    // ---------- lifecycle ----------

    [TestMethod]
    public async Task Prerender_ThePagesHtml_ContainsTheToggleButtons()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/toggle-button");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Matches(html, new Regex(@"<button[^>]*id=""tgb-basic""[^>]*class=""[^""]*\bbit-tgb\b"));
        StringAssert.Matches(html, new Regex(@"id=""bind-default"" class=""[^""]*\bbit-tgb-chk\b"));
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Parameters_ChangedAtRuntime_ReRenderTheButton()
    {
        await OpenAsync("/e2e/toggle-button");

        var button = Btn("rerender-btn");
        await Expect(button.Locator(".bit-tgb-ico")).ToHaveCountAsync(0);

        await Page.Locator("#btn-rerender").ClickAsync();

        await Expect(button).ToHaveClassAsync(HasClass("bit-tgb-err"));
        await Expect(button).ToHaveClassAsync(HasClass("bit-tgb-otl"));
        await Expect(button).ToHaveClassAsync(HasClass("bit-tgb-lg"));
        await Expect(button.Locator(".bit-tgb-btx")).ToHaveTextAsync("After");
        await Expect(button.Locator(".bit-tgb-ico")).ToHaveClassAsync(HasClass("bit-icon--Emoji"));
        await Expect(button).ToHaveCSSAsync("border-top-color", await Token("err"));
    }

    [TestMethod]
    public async Task Unmount_WhileAnAutoLoadingRunIsInFlight_LeavesNoErrorBehind()
    {
        await OpenAsync("/e2e/toggle-button");

        await Btn("mounted-btn").ClickAsync();
        await Expect(Btn("mounted-btn")).ToHaveAttributeAsync("aria-busy", "true");
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Btn("mounted-btn")).ToHaveCountAsync(0);
        await Expect(Btn("mounted-done")).ToHaveTextAsync("1");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
