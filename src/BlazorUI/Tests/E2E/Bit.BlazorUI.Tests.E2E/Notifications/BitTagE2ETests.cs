using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Notifications;

/// <summary>
/// BitTag in a real browser (/e2e/tag): it prerenders, is a static label, a real button (click, Enter/Space, hover,
/// press, focus ring, StopPropagation), a toggle chip (Selected binding, OnChange, OnChanging cancel, DefaultSelected,
/// the checkmark and its icon) or a real link (rel, target, the new-tab sentence, download, aria-current, disabled);
/// dismisses through its button and Delete/Backspace with the labels it builds and the focus handed to a neighbour;
/// lays its icons, picture, secondary text, templates, NoWrap and FullWidth out in both directions; carries its
/// accessible name and description to the control it becomes; drops the page's activation handlers while disabled
/// and keeps a focusable disabled control with AllowDisabledFocus; paints every color, variant, shape and size,
/// re-skins through its public CSS variables and the BitParams cascade, and unmounts without an error.
/// </summary>
[TestClass]
public class BitTagE2ETests : E2ETestBase
{
    private const string Transparent = "rgba(0, 0, 0, 0)";

    private static readonly Dictionary<string, string> OnTokens = new()
    {
        ["pri"] = "--bit-clr-pri-text", ["sec"] = "--bit-clr-sec-text", ["ter"] = "--bit-clr-ter-text", ["inf"] = "--bit-clr-inf-text",
        ["suc"] = "--bit-clr-suc-text", ["wrn"] = "--bit-clr-wrn-text", ["swr"] = "--bit-clr-swr-text", ["err"] = "--bit-clr-err-text",
        ["pbg"] = "--bit-clr-fg-pri", ["sbg"] = "--bit-clr-fg-pri", ["tbg"] = "--bit-clr-fg-pri",
        ["pfg"] = "--bit-clr-bg-pri", ["sfg"] = "--bit-clr-bg-pri", ["tfg"] = "--bit-clr-bg-pri",
        ["pbr"] = "--bit-clr-fg-pri", ["sbr"] = "--bit-clr-fg-pri", ["tbr"] = "--bit-clr-fg-pri",
    };

    private static Regex Any => new(".*");

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Content(string id) => Page.Locator($"#{id} > .bit-tag-cnt");

    private ILocator Dismiss(string id) => Page.Locator($"#{id} > .bit-tag-cls");

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheTag()
    {
        var html = await Page.PrerenderedHtmlAsync($"{BaseUrl}/e2e/tag");

        StringAssert.Contains(html, "id=\"basic-tag\"");
        StringAssert.Contains(html, "Basic tag");

        await OpenAsync("/e2e/tag");
        await Expect(Page.Locator("#basic-tag .bit-tag-tex")).ToHaveTextAsync("Basic tag");
    }

    [TestMethod]
    public async Task Basic_AStaticTag_IsALabelThatTakesNoFocus_AndRerendersOnAParameterChange()
    {
        await OpenAsync("/e2e/tag");

        var root = Root("basic-tag");
        await Expect(root).ToHaveClassAsync(HasClass("bit-tag"));
        await Expect(root.Locator("button, a")).ToHaveCountAsync(0);
        await Expect(root).Not.ToHaveAttributeAsync("tabindex", Any);
        await Expect(root).Not.ToHaveAttributeAsync("role", Any);
        Assert.AreEqual("SPAN", await Content("basic-tag").EvaluateAsync<string>("el => el.tagName"));

        await Expect(Root("basic-info")).ToHaveClassAsync(HasClass("bit-tag-inf"));
        await Expect(Root("basic-info")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-inf"));

        await Page.ClickAsync("#btn-runtime");
        await Expect(Page.Locator("#basic-runtime .bit-tag-tex")).ToHaveTextAsync("Changed");
        await Expect(Root("basic-runtime")).ToHaveClassAsync(HasClass("bit-tag-err"));
        await Expect(Root("basic-runtime")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-err"));
    }

    [TestMethod]
    public async Task Variant_EachVariant_PaintsItsOwnLook()
    {
        await OpenAsync("/e2e/tag");

        var primary = await Page.ResolveColorAsync("--bit-clr-pri");

        await Expect(Root("var-fill")).ToHaveClassAsync(HasClass("bit-tag-fil"));
        await Expect(Root("var-fill")).ToHaveCSSAsync("background-color", primary);
        await Expect(Root("var-fill")).ToHaveCSSAsync("border-top-color", primary);
        await Expect(Root("var-fill")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("--bit-clr-pri-text"));

        await Expect(Root("var-outline")).ToHaveClassAsync(HasClass("bit-tag-otl"));
        await Expect(Root("var-outline")).ToHaveCSSAsync("background-color", Transparent);
        await Expect(Root("var-outline")).ToHaveCSSAsync("border-top-color", primary);
        await Expect(Root("var-outline")).ToHaveCSSAsync("color", primary);

        await Expect(Root("var-text")).ToHaveClassAsync(HasClass("bit-tag-txt"));
        await Expect(Root("var-text")).ToHaveCSSAsync("background-color", Transparent);
        await Expect(Root("var-text")).ToHaveCSSAsync("border-top-color", Transparent);
        await Expect(Root("var-text")).ToHaveCSSAsync("color", primary);

        await Expect(Root("var-default")).ToHaveClassAsync(HasClass("bit-tag-fil"));
    }

    [TestMethod]
    public async Task Shape_EachShape_SetsTheCorner()
    {
        await OpenAsync("/e2e/tag");

        var chip = await ComputedStyleAsync("#shape-default", "border-top-left-radius");
        await Expect(Root("shape-rounded")).ToHaveClassAsync(HasClass("bit-tag-rnd"));
        await Expect(Root("shape-rounded")).ToHaveCSSAsync("border-top-left-radius", chip);
        Assert.IsTrue(NotificationsE2EHelpers.Px(chip) > 0, "The default is the theme's chip corner.");

        foreach (var id in new[] { "shape-pill", "shape-circle" })
        {
            await Expect(Root(id)).ToHaveClassAsync(HasClass("bit-tag-cir"));
            var height = (await Root(id).BoxAsync()).Height;
            Assert.IsTrue(NotificationsE2EHelpers.Px(await ComputedStyleAsync($"#{id}", "border-top-left-radius")) >= height / 2, $"{id} is a pill.");
        }

        await Expect(Root("shape-square")).ToHaveClassAsync(HasClass("bit-tag-sqr"));
        await Expect(Root("shape-square")).ToHaveCSSAsync("border-top-left-radius", "0px");
    }

    [TestMethod]
    public async Task Disabled_EachVariant_KeepsItsLookAndTakesTheDisabledColorsOfItsRole()
    {
        await OpenAsync("/e2e/tag");

        var disabled = await Page.ResolveColorAsync("--bit-clr-pri-dis");
        var disabledText = await Page.ResolveColorAsync("--bit-clr-pri-dis-text");

        await Expect(Root("dis-fill")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Root("dis-fill")).ToHaveCSSAsync("background-color", disabled);
        await Expect(Root("dis-fill")).ToHaveCSSAsync("color", disabledText);
        await Expect(Root("dis-outline")).ToHaveCSSAsync("background-color", Transparent);
        await Expect(Root("dis-outline")).ToHaveCSSAsync("border-top-color", disabled);
        await Expect(Root("dis-outline")).ToHaveClassAsync(HasClass("bit-tag-cir"));
        await Expect(Root("dis-text")).ToHaveCSSAsync("border-top-color", Transparent);
        await Expect(Root("dis-text")).ToHaveCSSAsync("color", disabledText);

        await Expect(Root("dis-error")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-err-dis"));
        await Expect(Root("dis-error")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("--bit-clr-err-dis-text"));
    }

    [TestMethod]
    public async Task Icon_LeadingAndTrailingGlyphs_FrameTheLabel_AndReversedMirrorsTheRow()
    {
        await OpenAsync("/e2e/tag");

        var leading = Page.Locator("#icon-leading .bit-tag-icn");
        await Expect(leading).ToHaveClassAsync(HasClass("bit-icon--Calendar"));
        await Expect(leading).ToHaveAttributeAsync("aria-hidden", "true");
        Assert.IsTrue((await leading.BoxAsync()).Right <= (await Page.Locator("#icon-leading .bit-tag-lbl").BoxAsync()).Left + 0.5);
        Assert.AreEqual(await ComputedStyleAsync("#icon-leading", "font-size"), await ComputedStyleAsync("#icon-leading .bit-tag-icn", "font-size"), "The glyph is sized to the text.");

        var trailing = Page.Locator("#icon-trailing .bit-tag-sci");
        await Expect(trailing).ToHaveClassAsync(HasClass("bit-icon--ChevronDown"));
        Assert.IsTrue((await trailing.BoxAsync()).Left >= (await Page.Locator("#icon-trailing .bit-tag-lbl").BoxAsync()).Right - 0.5);

        await Expect(Root("icon-reversed")).ToHaveClassAsync(HasClass("bit-tag-rvs"));
        var icon = await Page.Locator("#icon-reversed .bit-tag-icn").BoxAsync();
        var label = await Page.Locator("#icon-reversed .bit-tag-lbl").BoxAsync();
        var secondary = await Page.Locator("#icon-reversed .bit-tag-sci").BoxAsync();
        var dismiss = await Dismiss("icon-reversed").BoxAsync();
        Assert.IsTrue(icon.Left >= label.Right - 0.5 && secondary.Right <= label.Left + 0.5, "Reversed mirrors the content.");
        Assert.IsTrue(dismiss.Right <= secondary.Left + 0.5, "Reversed puts the dismiss button first.");
    }

    [TestMethod]
    public async Task ImageAndSecondaryText_ThePictureIsACircle_AndTheSecondLineSitsUnderTheLabel()
    {
        await OpenAsync("/e2e/tag");

        var image = Page.Locator("#img-plain img.bit-tag-img");
        await Expect(image).ToHaveAttributeAsync("src", "samples/persona-sample.png");
        await Expect(image).ToHaveAttributeAsync("alt", "");
        await Expect(image).ToHaveCSSAsync("width", "16px");
        await Expect(image).ToHaveCSSAsync("height", "16px");
        Assert.IsTrue(NotificationsE2EHelpers.Px(await ComputedStyleAsync("#img-plain img", "border-top-left-radius")) >= 8);
        await Expect(Page.Locator("#img-alt img")).ToHaveAttributeAsync("alt", "Assigned to Annie");

        var text = await Page.Locator("#img-secondary .bit-tag-tex").BoxAsync();
        var second = await Page.Locator("#img-secondary .bit-tag-stx").BoxAsync();
        Assert.IsTrue(second.Top >= text.Bottom - 0.5, "The secondary text is on a line of its own.");
        await Expect(Page.Locator("#img-secondary .bit-tag-tex")).ToHaveCSSAsync("font-weight", "600");
        Assert.IsTrue(NotificationsE2EHelpers.Px(await ComputedStyleAsync("#img-secondary .bit-tag-stx", "font-size")) < NotificationsE2EHelpers.Px(await ComputedStyleAsync("#img-secondary .bit-tag-tex", "font-size")));
        await Expect(Page.Locator("#img-secondary .bit-tag-stx")).ToHaveCSSAsync("font-weight", "400");
    }

    [TestMethod]
    public async Task Dismiss_TheButtonAndDeleteAndBackspace_DismissTheTag_AndTheFocusMovesToItsNeighbour()
    {
        await OpenAsync("/e2e/tag");

        var design = Dismiss("dm-Design");
        await Expect(design).ToHaveAttributeAsync("aria-label", "Remove Design");
        await Expect(design).ToHaveAttributeAsync("title", "Remove Design");
        await Expect(design).ToHaveAttributeAsync("type", "button");
        await Expect(design.Locator(".bit-tag-dsi")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));

        await design.ClickAsync();
        await Expect(Page.Locator("#dismissed")).ToHaveTextAsync("Design");
        await Expect(Root("dm-Design")).ToHaveCountAsync(0);
        await Expect(Dismiss("dm-Research")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Delete");
        await Expect(Page.Locator("#dismissed")).ToHaveTextAsync("Design,Research");
        await Expect(Dismiss("dm-Urgent")).ToBeFocusedAsync();

        // WebKit's Windows build takes Backspace for the browser's Back as well (the tag acts on the key without preventing
        // its default), so the page would be left; there the third tag is dismissed with Delete.
        await Page.Keyboard.PressAsync(IsWebKit ? "Delete" : "Backspace");
        await Expect(Page.Locator("#dismissed")).ToHaveTextAsync("Design,Research,Urgent");

        // Other keys do nothing.
        await Page.ClickAsync("#btn-dm-reset");
        await Dismiss("dm-Design").FocusAsync();
        await Page.Keyboard.PressAsync("a");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#dismissed")).ToHaveTextAsync("Design");
    }

    [TestMethod]
    public async Task Dismiss_TheLabelAndGlyph_FollowDismissLabelTheFormatAndTheIcon_AndADisabledTagStays()
    {
        await OpenAsync("/e2e/tag");

        await Expect(Dismiss("dm-custom")).ToHaveAttributeAsync("aria-label", "Clear the custom glyph tag");
        await Expect(Dismiss("dm-custom").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--ChromeClose"));
        await Expect(Dismiss("dm-format")).ToHaveAttributeAsync("aria-label", "Take Formatted label off the list");
        await Expect(Dismiss("dm-bad-format")).ToHaveAttributeAsync("aria-label", "Bad format");
        await Expect(Dismiss("dm-no-name")).ToHaveAttributeAsync("aria-label", "Dismiss");

        var disabled = Dismiss("dm-disabled");
        await Expect(Root("dm-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(disabled).ToBeDisabledAsync();
        await Expect(disabled).ToHaveAttributeAsync("aria-disabled", "true");
        await disabled.DispatchEventAsync("click");
        await disabled.DispatchEventAsync("keydown", new { key = "Delete" });
        await Expect(Page.Locator("#other-dismissals")).ToHaveTextAsync("0");

        await Dismiss("dm-custom").ClickAsync();
        await Expect(Page.Locator("#other-dismissals")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Click_ATagWithOnClick_IsARealButton_ActivatedByClickEnterAndSpace()
    {
        await OpenAsync("/e2e/tag");

        var button = Page.Locator("#click-tag > button.bit-tag-cnt");
        await Expect(button).ToHaveAttributeAsync("type", "button");
        await Expect(button).ToHaveClassAsync(HasClass("bit-tag-int"));
        Assert.AreEqual("pointer", await ComputedStyleAsync("#click-tag > .bit-tag-cnt", "cursor"));

        await button.ClickAsync();
        await Expect(Page.Locator("#clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-button")).ToHaveTextAsync("0");

        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#clicks")).ToHaveTextAsync("2");
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#clicks")).ToHaveTextAsync("3");

        // The dismiss button's click never reaches the tag.
        await Dismiss("click-dismiss").ClickAsync();
        await Expect(Page.Locator("#click-dismissals")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#clicks")).ToHaveTextAsync("3");

        // The keyboard reaches the content and then its dismiss button.
        await Content("click-dismiss").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Dismiss("click-dismiss")).ToBeFocusedAsync();
        Assert.AreEqual("solid", await ComputedStyleAsync("#click-dismiss > .bit-tag-cls", "outline-style"), "The focus ring is drawn.");
    }

    [TestMethod]
    public async Task Click_StopPropagation_KeepsTheClickFromAClickableParent()
    {
        await OpenAsync("/e2e/tag");

        await Content("click-bubbles").ClickAsync();
        await Expect(Page.Locator("#clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#card-clicks")).ToHaveTextAsync("1");

        await Content("click-stops").ClickAsync();
        await Expect(Page.Locator("#clicks")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#card-clicks")).ToHaveTextAsync("1");

        await Content("click-static").ClickAsync();
        await Expect(Page.Locator("#card-clicks")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Click_ADisabledTag_IsADisabledButtonThatDoesNotFire_UntilEnabledAgain()
    {
        await OpenAsync("/e2e/tag");

        var button = Content("click-disabled");
        await Expect(button).ToBeDisabledAsync();
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Root("click-disabled")).ToHaveCSSAsync("pointer-events", "none");
        await button.DispatchEventAsync("click");
        await Expect(Page.Locator("#clicks")).ToHaveTextAsync("0");

        await Page.ClickAsync("#btn-toggle-click-disabled");
        await Expect(button).ToBeEnabledAsync();
        await Expect(button).Not.ToHaveAttributeAsync("aria-disabled", Any);
        await button.ClickAsync();
        await Expect(Page.Locator("#clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Click_HoverAndPress_TintTheContentAndTheDismissButton()
    {
        await OpenAsync("/e2e/tag");

        var fill = Content("click-tag");
        await Expect(fill).ToHaveCSSAsync("background-color", Transparent);
        await fill.HoverAsync();
        await Expect(fill).ToHaveCSSAsync("background-color", await Page.ResolveCssColorAsync("color-mix(in srgb, var(--bit-clr-pri-text) 12%, transparent)"));
        await Page.Mouse.DownAsync();
        await Expect(fill).ToHaveCSSAsync("background-color", await Page.ResolveCssColorAsync("color-mix(in srgb, var(--bit-clr-pri-text) 20%, transparent)"));
        await Page.Mouse.UpAsync();

        var outline = Content("click-dismiss");
        await outline.HoverAsync();
        await Expect(outline).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-pri-light"));
        await Page.Mouse.DownAsync();
        await Expect(outline).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-pri-light-hover"));
        await Page.Mouse.UpAsync();

        await Dismiss("click-dismiss").HoverAsync();
        await Expect(Dismiss("click-dismiss")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-pri-light"));
    }

    [TestMethod]
    public async Task Selection_TheSelectedBinding_MakesAToggleChipWithACheckmark_BothWays()
    {
        await OpenAsync("/e2e/tag");

        var chip = Content("sel-bound");
        await Expect(chip).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Page.Locator("#sel-bound .bit-tag-sic")).ToHaveCountAsync(0);

        await chip.ClickAsync();
        await Expect(chip).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Root("sel-bound")).ToHaveClassAsync(HasClass("bit-tag-sel"));
        await Expect(Page.Locator("#selected")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#changes")).ToHaveTextAsync("True");

        var check = Page.Locator("#sel-bound .bit-tag-sic");
        await Expect(check).ToHaveClassAsync(HasClass("bit-icon--Accept"));
        await Expect(check).ToHaveAttributeAsync("aria-hidden", "true");
        Assert.IsTrue((await check.BoxAsync()).Right <= (await Page.Locator("#sel-bound .bit-tag-lbl").BoxAsync()).Left + 0.5, "The checkmark leads the tag.");

        // Selected outline: the light fill of the role.
        await Expect(Root("sel-bound")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-pri-light"));

        await chip.ClickAsync();
        await Expect(chip).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Page.Locator("#changes")).ToHaveTextAsync("True,False");

        // The page sets the binding; OnChange reports only what the tag itself changes.
        await Page.ClickAsync("#btn-set-selected");
        await Expect(chip).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator("#changes")).ToHaveTextAsync("True,False");
    }

    [TestMethod]
    public async Task Selection_TheCheckmarkIcon_DefaultSelected_OnChanging_AndAStaticSelection()
    {
        await OpenAsync("/e2e/tag");

        await Content("sel-no-check").ClickAsync();
        await Expect(Root("sel-no-check")).ToHaveClassAsync(HasClass("bit-tag-sel"));
        await Expect(Page.Locator("#sel-no-check .bit-tag-sic")).ToHaveCountAsync(0);

        await Content("sel-star").ClickAsync();
        await Expect(Page.Locator("#sel-star .bit-tag-sic")).ToHaveClassAsync(HasClass("bit-icon--FavoriteStarFill"));

        await Expect(Content("sel-default")).ToHaveAttributeAsync("aria-pressed", "false");
        await Content("sel-default").ClickAsync();
        await Expect(Content("sel-default")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Content("sel-default-true")).ToHaveAttributeAsync("aria-pressed", "true");

        // OnChanging is asked first and can refuse.
        await Content("sel-asks").ClickAsync();
        await Expect(Page.Locator("#changing-values")).ToHaveTextAsync("True");
        await Expect(Content("sel-asks")).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Page.Locator("#ask-changes")).ToHaveTextAsync("");

        await Page.ClickAsync("#btn-allow");
        await Content("sel-asks").ClickAsync();
        await Expect(Content("sel-asks")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator("#ask-changes")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#changing-values")).ToHaveTextAsync("True,True");

        // A Selected without a callback is a state the tag shows and cannot change.
        await Expect(Root("sel-static")).ToHaveClassAsync(HasClass("bit-tag-sel"));
        await Expect(Root("sel-static").Locator("button")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#sel-static .bit-tag-sic")).ToBeVisibleAsync();
        await Expect(Root("sel-static")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-suc-active"));
    }

    [TestMethod]
    public async Task Link_ATagWithHref_IsARealLink_WithRelTargetTheNewTabSentenceAndDownload()
    {
        await OpenAsync("/e2e/tag");

        var self = Page.Locator("#link-self > a.bit-tag-cnt");
        await Expect(self).ToHaveAttributeAsync("href", "/e2e/tag#link-target");
        await Expect(self).Not.ToHaveAttributeAsync("rel", Any);
        await Expect(self.Locator(".bit-tag-vhd")).ToHaveCountAsync(0);

        var blank = Page.Locator("#link-blank > a");
        await Expect(blank).ToHaveAttributeAsync("target", "_blank");
        await Expect(blank).ToHaveAttributeAsync("rel", "noopener");
        await Expect(blank.Locator(".bit-tag-vhd")).ToHaveTextAsync("(opens in a new tab)");
        await Expect(blank).ToHaveAccessibleNameAsync(new Regex("Docs\\s*\\(opens in a new tab\\)"));

        await Expect(Page.Locator("#link-rel > a")).ToHaveAttributeAsync("rel", "nofollow noreferrer");
        await Expect(Page.Locator("#link-rel > a .bit-tag-vhd")).ToHaveTextAsync("(opens GitHub in a new tab)");
        await Expect(Page.Locator("#link-nofollow > a")).ToHaveAttributeAsync("rel", "nofollow noopener");
        await Expect(Page.Locator("#link-nofollow > a .bit-tag-vhd")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#link-labelled > a")).ToHaveAttributeAsync("aria-label", "Open the docs (opens in a new tab)");
        await Expect(Page.Locator("#link-download > a")).ToHaveAttributeAsync("download", "logo.png");

        var disabled = Page.Locator("#link-disabled > a");
        await Expect(disabled).Not.ToHaveAttributeAsync("href", Any);
        await Expect(disabled).ToHaveAttributeAsync("role", "link");
        await Expect(disabled).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(disabled).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(disabled.Locator(".bit-tag-vhd")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#link-current > a")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Page.Locator("#link-current-default > a")).ToHaveAttributeAsync("aria-current", "true");
        await Expect(Page.Locator("#link-blank > a")).Not.ToHaveAttributeAsync("aria-current", Any);

        await self.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/tag#link-target$"));
        await Expect(Root("link-self")).ToBeVisibleAsync();

        // A selected link navigates rather than toggling.
        await Page.Locator("#link-current > a").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/tag#link$"));
        await Expect(Page.Locator("#link-current > a")).ToHaveAttributeAsync("aria-current", "page");
    }

    [TestMethod]
    public async Task Templates_ChildContentReplacesTheText_AndPrefixAndSuffixFrameTheGlyphs()
    {
        await OpenAsync("/e2e/tag");

        await Expect(Page.Locator("#tpl-child #tpl-child-bold")).ToHaveTextAsync("Alex");
        await Expect(Page.Locator("#tpl-child .bit-tag-lbl")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#tpl-child .bit-tag-icn")).ToBeVisibleAsync();
        await Expect(Page.Locator("#tpl-child .bit-tag-sci")).ToBeVisibleAsync();

        var prefix = await Page.Locator("#tpl-prefix-mark").BoxAsync();
        var icon = await Page.Locator("#tpl-prefix .bit-tag-icn").BoxAsync();
        var secondary = await Page.Locator("#tpl-prefix .bit-tag-sci").BoxAsync();
        var suffix = await Page.Locator("#tpl-suffix-mark").BoxAsync();
        Assert.IsTrue(prefix.Right <= icon.Left + 0.5, "The prefix is ahead of the icon.");
        Assert.IsTrue(suffix.Left >= secondary.Right - 0.5, "The suffix is after the trailing glyph.");
        await Expect(Page.Locator("#tpl-prefix > button #tpl-suffix-mark")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task NoWrapAndFullWidth_ALongLabelWrapsOrEllipsizes_AndAFullWidthTagSpansItsContainer()
    {
        await OpenAsync("/e2e/tag");

        var wrapped = await Root("wrap-default").BoxAsync();
        var single = await Root("wrap-nowrap").BoxAsync();
        Assert.IsTrue(wrapped.Height > single.Height + 5, "A tag wraps by default.");

        await Expect(Root("wrap-nowrap")).ToHaveClassAsync(HasClass("bit-tag-nwr"));
        await Expect(Page.Locator("#wrap-nowrap .bit-tag-tex")).ToHaveCSSAsync("text-overflow", "ellipsis");
        Assert.IsTrue(await Page.Locator("#wrap-nowrap .bit-tag-tex").EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"), "The label is cut off.");
        await Expect(Root("wrap-nowrap")).ToHaveAttributeAsync("title", "A tag with a label long enough to wrap onto a second line");

        var box = await Page.BoxAsync("#wrap-box");
        var full = await Root("wrap-full").BoxAsync();
        await Expect(Root("wrap-full")).ToHaveClassAsync(HasClass("bit-tag-flw"));
        Assert.AreEqual(box.Width, full.Width, 0.5, "FullWidth spans the container.");
        Assert.AreEqual(full.Right, (await Dismiss("wrap-full").BoxAsync()).Right, 2, "The dismiss button is pushed to the end.");

        var trailingTag = await Root("wrap-full-trailing").BoxAsync();
        var trailing = await Page.Locator("#wrap-full-trailing .bit-tag-sci").BoxAsync();
        Assert.IsTrue(trailingTag.Right - trailing.Right < 20, "The trailing glyph is pushed to the end.");
    }

    [TestMethod]
    public async Task Accessibility_TheNameAndDescription_GoToTheControlTheTagBecomes()
    {
        await OpenAsync("/e2e/tag");

        await Expect(Content("a11y-icon-button")).ToHaveAttributeAsync("aria-label", "Show the filters");
        await Expect(Content("a11y-icon-button")).ToHaveAccessibleNameAsync("Show the filters");

        // A static tag is named by hidden text, and what it would have said is hidden in its place.
        var root = Root("a11y-static-label");
        await Expect(root).Not.ToHaveAttributeAsync("aria-label", Any);
        await Expect(Page.Locator("#a11y-static-label > .bit-tag-vhd").First).ToHaveTextAsync("3 unread messages");
        Assert.IsTrue(await root.EvaluateAsync<bool>("el => el.firstElementChild.classList.contains('bit-tag-vhd')"), "The name comes first.");
        await Expect(Page.Locator("#a11y-static-label .bit-tag-lbl")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Page.Locator("#a11y-static-label img")).ToHaveAttributeAsync("aria-hidden", "true");

        var described = Content("a11y-described");
        await Expect(described).ToHaveAttributeAsync("aria-describedby", "a11y-described-dsc");
        await Expect(Page.Locator("#a11y-described-dsc")).ToHaveTextAsync("Shows only the items you own");
        await Expect(described).ToHaveAccessibleDescriptionAsync("Shows only the items you own");

        await Expect(Root("a11y-dismiss-described")).ToHaveAttributeAsync("aria-describedby", "a11y-dismiss-described-dsc");
        await Expect(Dismiss("a11y-dismiss-described")).ToHaveAttributeAsync("aria-describedby", "a11y-dismiss-described-dsc");
        await Expect(Root("a11y-static-described")).ToHaveAttributeAsync("aria-describedby", "a11y-static-described-dsc");

        // What came in through the attributes joins the tag's own.
        await Expect(Content("a11y-splatted")).ToHaveAttributeAsync("aria-describedby", "a11y-splatted-dsc a11y-extra");
        await Expect(Root("a11y-splatted")).ToHaveAttributeAsync("title", "Splatted title");
        await Expect(Root("a11y-splatted")).Not.ToHaveAttributeAsync("aria-describedby", Any);
    }

    [TestMethod]
    public async Task Focus_TabIndexAndFocusAsync_ReachEveryKindOfTag_AndAllowDisabledFocusKeepsADisabledOneReachable()
    {
        await OpenAsync("/e2e/tag");

        await Expect(Root("focus-plain")).ToHaveAttributeAsync("tabindex", "0");
        await Page.ClickAsync("#btn-focus-plain");
        await Expect(Root("focus-plain")).ToBeFocusedAsync();
        await Page.ClickAsync("#btn-focus-button");
        await Expect(Content("focus-button")).ToBeFocusedAsync();
        await Page.ClickAsync("#btn-focus-dismiss");
        await Expect(Dismiss("focus-dismiss")).ToBeFocusedAsync();

        var disabled = Content("focus-disabled");
        await Expect(disabled).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(disabled).Not.ToHaveAttributeAsync("disabled", Any);
        await Expect(disabled).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(disabled).ToHaveAccessibleDescriptionAsync("Unavailable while offline");

        await Dismiss("focus-dismiss").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(disabled).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Space");
        await Expect(disabled).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Page.Locator("#disabled-changes")).ToHaveTextAsync("0");

        var link = Page.Locator("#focus-disabled-link > a");
        await Expect(link).ToHaveAttributeAsync("tabindex", "0");
        await Expect(link).Not.ToHaveAttributeAsync("href", Any);
        await Page.Keyboard.PressAsync("Tab");
        await Expect(link).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task DisabledContract_ADisabledTag_DropsThePagesActivationHandlers_AndKeepsTheHover()
    {
        await OpenAsync("/e2e/tag");

        var root = Root("dc-tag");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await root.DispatchEventAsync("click");
        await root.DispatchEventAsync("keydown", new { key = "Enter" });
        await root.DispatchEventAsync("mouseover");
        await Expect(Page.Locator("#root-hovers")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#root-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#root-keys")).ToHaveTextAsync("0");

        await Page.ClickAsync("#btn-contract");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await root.ClickAsync();
        await root.DispatchEventAsync("keydown", new { key = "Enter" });
        await Expect(Page.Locator("#root-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#root-keys")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Cascade_BitParams_SetsTheDefaultsAndAnOwnValueWins()
    {
        await OpenAsync("/e2e/tag");

        await Expect(Root("cas-1")).ToHaveClassAsync(HasClass("bit-tag-inf"));
        await Expect(Root("cas-1")).ToHaveClassAsync(HasClass("bit-tag-otl"));
        await Expect(Root("cas-1")).ToHaveClassAsync(HasClass("bit-tag-cir"));
        await Expect(Page.Locator("#cas-1 .bit-tag-icn")).ToHaveClassAsync(HasClass("bit-icon--Filter"));
        await Expect(Dismiss("cas-1")).ToHaveAttributeAsync("aria-label", "Remove the Design filter");

        await Expect(Root("cas-2")).ToHaveClassAsync(HasClass("bit-tag-err"));
        await Expect(Root("cas-2")).Not.ToHaveClassAsync(HasClass("bit-tag-inf"));
        await Expect(Root("cas-2")).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("--bit-clr-err"));

        await Expect(Root("cas-outside")).ToHaveClassAsync(HasClass("bit-tag-fil"));
        await Expect(Page.Locator("#cas-outside .bit-tag-icn")).ToHaveCountAsync(0);
        await Expect(Dismiss("cas-outside")).ToHaveAttributeAsync("aria-label", "Remove Outside");
    }

    [TestMethod]
    public async Task Color_EachColor_PaintsEveryVariantAndTheSelectedState()
    {
        await OpenAsync("/e2e/tag");

        foreach (var (name, suffix, token) in NotificationsE2EHelpers.Colors)
        {
            var main = await Page.ResolveColorAsync(token);

            await Expect(Root($"clr-{name}")).ToHaveClassAsync(HasClass($"bit-tag-{suffix}"));
            await Expect(Root($"clr-{name}")).ToHaveCSSAsync("background-color", main);
            await Expect(Root($"clr-{name}")).ToHaveCSSAsync("color", await Page.ResolveColorAsync(OnTokens[suffix]));
            await Expect(Root($"clr-otl-{name}")).ToHaveCSSAsync("border-top-color", main);
            await Expect(Root($"clr-otl-{name}")).ToHaveCSSAsync("color", main);
            await Expect(Root($"clr-txt-{name}")).ToHaveCSSAsync("color", main);
            await Expect(Root($"clr-sel-{name}")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync($"{token}-active"));
        }
    }

    [TestMethod]
    public async Task ExternalIcons_EveryIconTakesABitIconInfo()
    {
        await OpenAsync("/e2e/tag");

        await Expect(Page.Locator("#ext-string .bit-tag-icn")).ToHaveClassAsync(HasClass("fa-house"));
        await Expect(Page.Locator("#ext-css .bit-tag-icn")).ToHaveClassAsync(HasClass("custom-glyph"));
        Assert.AreEqual("\"*\"", await Page.PseudoStyleAsync("#ext-css .bit-tag-icn", "::before", "content"));
        await Expect(Page.Locator("#ext-fa .bit-tag-icn")).ToHaveClassAsync(HasClass("fa-github"));
        await Expect(Page.Locator("#ext-dismiss .bit-tag-icn")).ToHaveClassAsync(HasClass("bi-tag-fill"));
        await Expect(Page.Locator("#ext-dismiss .bit-tag-dsi")).ToHaveClassAsync(HasClass("bi-x-lg"));
        await Expect(Page.Locator("#ext-selected .bit-tag-sic")).ToHaveClassAsync(HasClass("fa-check"));
        await Expect(Page.Locator("#ext-trailing .bit-tag-sci")).ToHaveClassAsync(HasClass("bi-chevron-down"));
    }

    [TestMethod]
    public async Task Size_EachSize_SetsTheTypeTheHeightAndThePicture()
    {
        await OpenAsync("/e2e/tag");

        var fonts = new List<double>();
        var heights = new List<double>();
        var images = new List<double>();
        foreach (var (name, suffix) in new[] { ("Small", "sm"), ("Medium", "md"), ("Large", "lg") })
        {
            await Expect(Root($"size-{name}")).ToHaveClassAsync(HasClass($"bit-tag-{suffix}"));
            fonts.Add(NotificationsE2EHelpers.Px(await ComputedStyleAsync($"#size-{name}", "font-size")));
            heights.Add(NotificationsE2EHelpers.Px(await ComputedStyleAsync($"#size-{name} > .bit-tag-cnt", "min-height")));
            images.Add((await Page.Locator($"#size-{name} img").BoxAsync()).Width);
        }

        foreach (var (what, values) in new[] { ("type", fonts), ("height", heights), ("picture", images) })
        {
            Assert.IsTrue(values[0] < values[1] && values[1] < values[2], $"The {what} grows with the size ({string.Join(", ", values)}).");
        }

        Assert.IsTrue((await Dismiss("size-Medium").BoxAsync()).Width >= 24, "From Medium up the dismiss button meets the 24px target.");
    }

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRootAndStylesAndClassesOnEveryPart_IncludingTheSelectedState()
    {
        await OpenAsync("/e2e/tag");

        await Expect(Root("sc-style")).ToHaveCSSAsync("font-style", "italic");
        await Expect(Root("sc-style")).ToHaveCSSAsync("margin-left", "6px");
        await Expect(Root("sc-class")).ToHaveClassAsync(HasClass("custom-class"));

        var styles = new (string Selector, string Property, string Value)[]
        {
            ("#sc-styles", "margin-top", "3px"),
            ("#sc-styles > .bit-tag-cnt", "padding-top", "7px"),
            ("#sc-styles .bit-tag-lbl", "padding-left", "2px"),
            ("#sc-styles .bit-tag-tex", "font-style", "italic"),
            ("#sc-styles .bit-tag-stx", "text-decoration-line", "underline"),
            ("#sc-styles .bit-tag-icn", "color", "rgb(255, 0, 0)"),
            ("#sc-styles .bit-tag-sci", "color", "rgb(0, 128, 0)"),
            ("#sc-styles", "outline-color", "rgb(0, 0, 255)"),
            ("#sc-styles .bit-tag-sic", "color", "rgb(1, 2, 3)"),
            ("#sc-styles > .bit-tag-cls", "opacity", "0.8"),
            ("#sc-styles .bit-tag-dsi", "color", "rgb(4, 5, 6)"),
            ("#sc-image-only img", "opacity", "0.4"),
        };

        foreach (var (selector, property, value) in styles)
        {
            await Expect(Page.Locator(selector).First).ToHaveCSSAsync(property, value);
        }

        var classes = new (string Selector, string Class)[]
        {
            ("#sc-classes", "c-root"), ("#sc-classes > .bit-tag-cnt", "c-cnt"), ("#sc-classes .bit-tag-lbl", "c-lbl"),
            ("#sc-classes .bit-tag-tex", "c-tex"), ("#sc-classes .bit-tag-stx", "c-stx"), ("#sc-classes .bit-tag-icn", "c-icn"),
("#sc-classes .bit-tag-sci", "c-sci"), ("#sc-classes", "c-sel"),
            ("#sc-classes .bit-tag-sic", "c-sic"), ("#sc-classes > .bit-tag-cls", "c-cls"), ("#sc-classes .bit-tag-dsi", "c-dsi"),
            ("#sc-image-only img", "c-img2"),
        };

        foreach (var (selector, @class) in classes)
        {
            await Expect(Page.Locator(selector).First).ToHaveClassAsync(HasClass(@class));
        }
    }

    [TestMethod]
    public async Task Rtl_ExplicitAndInherited_SwapTheIconAndTheDismissButton_AndReversedMirrorsThat()
    {
        await OpenAsync("/e2e/tag");

        await Expect(Root("rtl-tag")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Root("rtl-tag")).ToHaveClassAsync(HasClass("bit-rtl"));

        foreach (var id in new[] { "rtl-tag", "rtl-inherited" })
        {
            var icon = await Page.Locator($"#{id} .bit-tag-icn").BoxAsync();
            var label = await Page.Locator($"#{id} .bit-tag-lbl").BoxAsync();
            var dismiss = await Dismiss(id).BoxAsync();
            Assert.IsTrue(icon.Left >= label.Right - 0.5, $"{id}: the icon is on the right.");
            Assert.IsTrue(dismiss.Right <= label.Left + 0.5, $"{id}: the dismiss button is on the left.");
        }

        var rIcon = await Page.Locator("#rtl-reversed .bit-tag-icn").BoxAsync();
        var rLabel = await Page.Locator("#rtl-reversed .bit-tag-lbl").BoxAsync();
        var rDismiss = await Dismiss("rtl-reversed").BoxAsync();
        Assert.IsTrue(rIcon.Right <= rLabel.Left + 0.5 && rDismiss.Left >= rLabel.Right - 0.5, "Reversed mirrors the right-to-left order.");

        var lIcon = await Page.Locator("#ltr-tag .bit-tag-icn").BoxAsync();
        var lLabel = await Page.Locator("#ltr-tag .bit-tag-lbl").BoxAsync();
        Assert.IsTrue(lIcon.Right <= lLabel.Left + 0.5);
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_RestyleTheDefault_AndTheParametersWinOverThem()
    {
        await OpenAsync("/e2e/tag");

        var root = Root("cv-tag");
        await Expect(root).ToHaveCSSAsync("color", "rgb(91, 33, 182)");
        await Expect(root).ToHaveCSSAsync("background-color", "rgb(237, 233, 254)");
        await Expect(root).ToHaveCSSAsync("border-top-color", "rgb(196, 181, 253)");
        await Expect(root).ToHaveCSSAsync("border-top-width", "2px");
        await Expect(root).ToHaveCSSAsync("border-top-left-radius", "11px");
        await Expect(root).ToHaveCSSAsync("font-weight", "700");
        await Expect(root).ToHaveCSSAsync("font-size", "15px");
        await Expect(root).ToHaveCSSAsync("box-shadow", "rgb(0, 0, 0) 0px 0px 3px 0px");
        await Expect(Page.Locator("#cv-tag .bit-tag-icn")).ToHaveCSSAsync("color", "rgb(219, 39, 119)");
        var content = Content("cv-tag");
        await Expect(content).ToHaveCSSAsync("padding-left", "17px");
        await Expect(content).ToHaveCSSAsync("padding-top", "5px");
        await Expect(content).ToHaveCSSAsync("column-gap", "9px");
        await Expect(content).ToHaveCSSAsync("min-height", "34px");

        await content.HoverAsync();
        await Expect(content).ToHaveCSSAsync("background-color", "rgb(221, 214, 254)");
        await Page.Mouse.DownAsync();
        await Expect(content).ToHaveCSSAsync("background-color", "rgb(200, 190, 250)");
        await Page.Mouse.UpAsync();

        await Page.Mouse.MoveAsync(0, 0);
        await content.FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(content).ToBeFocusedAsync();
        await Expect(content).ToHaveCSSAsync("outline-color", "rgb(255, 0, 255)");

        Assert.IsTrue((await Root("cv-long").BoxAsync()).Width <= 150.5, "The max-width variable caps the tag.");
        await Expect(Page.Locator("#cv-parts .bit-tag-stx")).ToHaveCSSAsync("font-size", "9px");
        await Expect(Page.Locator("#cv-parts .bit-tag-icn")).ToHaveCSSAsync("font-size", "21px");
        await Expect(Page.Locator("#cv-parts .bit-tag-dsi")).ToHaveCSSAsync("font-size", "21px");
        await Expect(Page.Locator("#cv-image img")).ToHaveCSSAsync("width", "22px");

        var explicitTag = Root("cv-explicit");
        await Expect(explicitTag).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-err"));
        await Expect(explicitTag).ToHaveCSSAsync("border-top-left-radius", "0px");
        await Expect(explicitTag).Not.ToHaveCSSAsync("font-size", "15px");
        await Expect(Content("cv-explicit")).Not.ToHaveCSSAsync("min-height", "34px");

        await Expect(Root("cv-selected")).ToHaveCSSAsync("background-color", "rgb(0, 128, 0)");
        await Expect(Root("cv-selected")).ToHaveCSSAsync("border-top-color", "rgb(0, 100, 0)");
        await Expect(Root("cv-selected")).ToHaveCSSAsync("color", "rgb(255, 255, 255)");
        await Expect(Root("cv-disabled")).ToHaveCSSAsync("background-color", "rgb(230, 230, 230)");
        await Expect(Root("cv-disabled")).ToHaveCSSAsync("border-top-color", "rgb(210, 210, 210)");
        await Expect(Root("cv-disabled")).ToHaveCSSAsync("color", "rgb(120, 120, 120)");
        await Expect(Root("cv-twin-outline")).ToHaveCSSAsync("border-top-color", "rgb(255, 0, 0)");
        await Expect(Root("cv-twin-fill")).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("--bit-clr-pri"));
    }

    [TestMethod]
    public async Task BaseParameters_VisibilityTitleAndHtmlAttributes_LandOnTheRoot()
    {
        await OpenAsync("/e2e/tag");

        await Expect(Root("vis-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Root("vis-collapsed")).ToHaveCSSAsync("display", "none");
        await Expect(Root("html-attrs")).ToHaveAttributeAsync("data-test", "tag-attr");
        await Expect(Root("html-attrs")).ToHaveAttributeAsync("title", "Own title");
    }

    [TestMethod]
    public async Task Unmount_RemovingTags_LeavesNoErrorOnThePageOrTheServer()
    {
        await OpenAsync("/e2e/tag");

        await Expect(Page.Locator("#unmount-host .bit-tag")).ToHaveCountAsync(3);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-host")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-host .bit-tag")).ToHaveCountAsync(3);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-host")).ToHaveCountAsync(0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
