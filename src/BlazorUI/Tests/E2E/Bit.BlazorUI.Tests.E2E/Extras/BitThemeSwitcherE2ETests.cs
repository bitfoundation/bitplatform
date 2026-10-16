namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// End-to-end tests of BitThemeSwitcher on /e2e/theme-switcher and /e2e/theme-switcher/persist: the design system
/// picker and the light/dark toggle acting on the document's bit-theme (every packaged design system, linked as its
/// bundle, re-skinning the computed colors and shapes of a real component), the switcher following a theme applied
/// from elsewhere, the DesignSystems items (text and aria fallbacks, disabled and blank items), the parts, titles and
/// icons, OnChange, InitialTheme / InitialThemePair in the prerendered markup, keyboard use, Style / Class / Styles /
/// Classes, RTL, the disabled contract, visibility, forced colors, runtime parameter changes and unmounting, Escape
/// inside a BitDialog (the open picker first, the dialog on the next press), a design system the page does not ship
/// (bit-theme-available) refused on /e2e/theme-switcher/available, and the pick surviving a reload on a page that
/// persists the theme.
/// </summary>
[TestClass]
public class BitThemeSwitcherE2ETests : E2ETestBase
{
    private const string Route = "/e2e/theme-switcher";
    private const string PersistRoute = "/e2e/theme-switcher/persist";

    // The primary color and the page background of every theme the switcher can land on, as the packaged
    // stylesheets declare them (core: light / dark; Extras: the three design systems).
    private const string FluentLightPrimary = "rgb(18, 118, 198)";
    private const string FluentDarkPrimary = "rgb(79, 163, 244)";
    private const string FluentDarkBackground = "rgb(15, 19, 24)";
    private const string White = "rgb(255, 255, 255)";

    [TestMethod]
    public async Task Prerender_TheServerHtmlCarriesTheSwitcher_BeforeItBecomesInteractive()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"ths-basic\"");
        StringAssert.Contains(html, "class=\"bit-ths\"");
        StringAssert.Contains(html, "Turn off light");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Route);

        var root = Page.Locator("#ths-basic");
        await Expect(root).ToHaveAttributeAsync("role", "group");
        await Expect(root).ToHaveAttributeAsync("aria-label", "Theme");
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Basic_InTheLightTheme_ShowsFluentAndOnlyTheButtonThatTurnsTheLightOff()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "light");
        await Expect(PickerText("ths-basic")).ToHaveTextAsync("Fluent");

        var toDark = Page.Locator("#ths-basic .bit-ths-lgt");
        var toLight = Page.Locator("#ths-basic .bit-ths-drk");

        await Expect(toDark).ToBeVisibleAsync();
        await Expect(toLight).ToBeHiddenAsync();
        await Expect(toDark).ToHaveAttributeAsync("title", "Turn off light");
        await Expect(toDark).ToHaveAttributeAsync("aria-label", "Turn off light");
        await Expect(toDark.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--ClearNight"));
        await Expect(toLight).ToHaveAttributeAsync("title", "Turn on light");
        await Expect(toLight.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Sunny"));

        // The button is the icon-only circle of the primary color.
        await Expect(toDark).ToHaveCSSAsync("background-color", FluentLightPrimary);
        await Expect(toDark).ToHaveCSSAsync("height", "32px");

        var options = await OpenPickerAsync("ths-basic");
        await Expect(options).ToHaveCountAsync(4);
        await Expect(options.Nth(0)).ToHaveAttributeAsync("aria-label", "Fluent");
        await Expect(options.Nth(1)).ToHaveAttributeAsync("aria-label", "Fluent 2");
        await Expect(options.Nth(2)).ToHaveAttributeAsync("aria-label", "Material");
        await Expect(options.Nth(3)).ToHaveAttributeAsync("aria-label", "Cupertino");
        await Expect(options.Nth(0)).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod]
    public async Task Toggle_ClickingTheSchemeButton_SwitchesToDarkAndBack_RepaintingComponents()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", FluentLightPrimary);
        await Expect(Page.Locator("#probe-surface")).ToHaveCSSAsync("background-color", White);

        await Page.Locator("#ths-basic .bit-ths-lgt").ClickAsync();

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");
        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", FluentDarkPrimary);
        await Expect(Page.Locator("#probe-surface")).ToHaveCSSAsync("background-color", FluentDarkBackground);
        await Expect(Page.Locator("#ths-basic .bit-ths-drk")).ToBeVisibleAsync();
        await Expect(Page.Locator("#ths-basic .bit-ths-lgt")).ToBeHiddenAsync();
        await Expect(Page.Locator("#basic-change-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-last-theme")).ToHaveTextAsync("dark");
        await Expect(PickerText("ths-basic")).ToHaveTextAsync("Fluent");

        await Page.Locator("#ths-basic .bit-ths-drk").ClickAsync();

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "light");
        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", FluentLightPrimary);
        await Expect(Page.Locator("#probe-surface")).ToHaveCSSAsync("background-color", White);
        await Expect(Page.Locator("#basic-change-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#basic-last-theme")).ToHaveTextAsync("light");
    }

    [TestMethod]
    [DataRow("Fluent 2", "fluent2", "rgb(15, 108, 189)", "rgb(71, 158, 245)", "rgb(19, 19, 19)")]
    [DataRow("Material", "material", FluentLightPrimary, FluentDarkPrimary, "rgb(12, 19, 27)")]
    [DataRow("Cupertino", "cupertino", "rgb(0, 113, 227)", "rgb(90, 161, 255)", "rgb(19, 19, 19)")]
    public async Task DesignSystem_PickingOne_AppliesItsPair_AndTheToggleStaysWithinIt(string text, string stem, string lightPrimary, string darkPrimary, string darkBackground)
    {
        await OpenAsync(Route);

        await PickDesignSystemAsync("ths-basic", text);

        var html = Page.Locator("html");
        await Expect(html).ToHaveAttributeAsync("bit-theme", $"{stem}-light");
        await Expect(html).ToHaveAttributeAsync("bit-theme-light", $"{stem}-light");
        await Expect(html).ToHaveAttributeAsync("bit-theme-dark", $"{stem}-dark");
        await Expect(PickerText("ths-basic")).ToHaveTextAsync(text);
        await Expect(Page.Locator("#basic-last-theme")).ToHaveTextAsync($"{stem}-light");
        await Expect(Page.Locator("#basic-change-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", lightPrimary);

        await Page.Locator("#ths-basic .bit-ths-lgt").ClickAsync();

        await Expect(html).ToHaveAttributeAsync("bit-theme", $"{stem}-dark");
        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", darkPrimary);
        await Expect(Page.Locator("#probe-surface")).ToHaveCSSAsync("background-color", darkBackground);
        await Expect(Page.Locator("#basic-last-theme")).ToHaveTextAsync($"{stem}-dark");
        await Expect(PickerText("ths-basic")).ToHaveTextAsync(text);
    }

    [TestMethod]
    public async Task DesignSystem_SwitchedWhileDark_KeepsTheDarkScheme_AndReshapesComponents()
    {
        await OpenAsync(Route);

        var fluentRadius = await ComputedStyleAsync("#probe-button", "border-top-left-radius");

        await Page.Locator("#ths-basic .bit-ths-lgt").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");

        await PickDesignSystemAsync("ths-basic", "Material");

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "material-dark");
        await Expect(Page.Locator("#probe-surface")).ToHaveCSSAsync("background-color", "rgb(12, 19, 27)");

        // Material dresses its buttons as pills where Fluent keeps the control corner.
        var materialRadius = await ComputedStyleAsync("#probe-button", "border-top-left-radius");
        Assert.AreNotEqual(fluentRadius, materialRadius);
        Assert.IsTrue(ParsePx(materialRadius) > ParsePx(fluentRadius), $"{materialRadius} should round more than {fluentRadius}");

        await PickDesignSystemAsync("ths-basic", "Fluent");

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");
        await Expect(Page.Locator("#probe-surface")).ToHaveCSSAsync("background-color", FluentDarkBackground);
    }

    [TestMethod]
    public async Task EverySwitcherOnThePage_FollowsAPickMadeInAnother()
    {
        await OpenAsync(Route);

        await PickDesignSystemAsync("ths-basic", "Cupertino");

        await Expect(PickerText("ths-titles")).ToHaveTextAsync("Cupertino");
        await Expect(PickerText("ths-no-scheme")).ToHaveTextAsync("Cupertino");
        await Expect(PickerText("ths-custom")).ToHaveTextAsync("cupertino");
        await Expect(Page.Locator("#notified-theme")).ToHaveTextAsync("cupertino-light");

        // And the toggle of a switcher without a picker flips within the pair that is worn.
        await Page.Locator("#ths-no-design .bit-ths-lgt").ClickAsync();

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "cupertino-dark");
        await Expect(Page.Locator("#ths-basic .bit-ths-drk")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task External_AThemeAppliedFromCode_IsReflectedByThePickerAndTheToggle()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-set-material-dark").ClickAsync();

        await Expect(Page.Locator("#current-theme")).ToHaveTextAsync("material-dark");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "material-dark");
        await Expect(PickerText("ths-basic")).ToHaveTextAsync("Material");
        await Expect(Page.Locator("#ths-basic .bit-ths-drk")).ToBeVisibleAsync();
        await Expect(Page.Locator("#ths-basic .bit-ths-lgt")).ToBeHiddenAsync();
        await Expect(Page.Locator("#notified-theme")).ToHaveTextAsync("material-dark");

        // A pair switched from code keeps the scheme in use.
        await Page.Locator("#btn-set-pair-cupertino").ClickAsync();

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "cupertino-dark");
        await Expect(PickerText("ths-basic")).ToHaveTextAsync("Cupertino");

        // The toggle of the switcher then flips within that pair.
        await Page.Locator("#ths-basic .bit-ths-drk").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "cupertino-light");

        await Page.Locator("#btn-read-theme").ClickAsync();
        await Expect(Page.Locator("#current-theme")).ToHaveTextAsync("cupertino-light");
    }

    [TestMethod]
    public async Task External_AThemeNoItemClaims_FallsBackToTheFirstItem()
    {
        await OpenAsync(Route);

        await PickDesignSystemAsync("ths-basic", "Material");
        await Expect(PickerText("ths-basic")).ToHaveTextAsync("Material");

        // A disabled item still claims its theme: it only cannot be picked.
        await Expect(PickerText("ths-custom")).ToHaveTextAsync("Material (off)");

        await Page.Locator("#btn-set-custom").ClickAsync();

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "brand-light");
        await Expect(PickerText("ths-basic")).ToHaveTextAsync("Fluent");
        await Expect(PickerText("ths-custom")).ToHaveTextAsync("Classic");
    }

    [TestMethod]
    public async Task DesignSystems_CustomItems_FallBackOnTextAndValue_SkipBlankOnes_AndHonorIsDisabled()
    {
        await OpenAsync(Route);

        await Expect(PickerText("ths-custom")).ToHaveTextAsync("Classic");

        var options = await OpenPickerAsync("ths-custom");

        // The item with an empty Value is not offered at all.
        await Expect(options).ToHaveCountAsync(3);
        await Expect(options.Nth(0)).ToHaveAttributeAsync("aria-label", "Classic Fluent");
        await Expect(options.Nth(0)).ToContainTextAsync("Classic");
        await Expect(options.Nth(1)).ToContainTextAsync("cupertino");
        await Expect(options.Nth(2)).ToContainTextAsync("Material (off)");
        await Expect(options.Nth(2)).ToHaveAttributeAsync("aria-disabled", "true");

        // The disabled one cannot be picked.
        await options.Nth(2).ClickAsync(new() { Force = true });
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "light");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#ths-custom [role=combobox]")).ToHaveAttributeAsync("aria-expanded", "false");

        // An item without LightTheme / DarkTheme is spelled "{Value}-light" / "{Value}-dark".
        await PickDesignSystemAsync("ths-custom", "cupertino");

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "cupertino-light");
        await Expect(Page.Locator("#custom-last-theme")).ToHaveTextAsync("cupertino-light");

        await Page.Locator("#ths-custom .bit-ths-lgt").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "cupertino-dark");

        // Back to the item whose pair is spelled out: light / dark, in the scheme in use.
        await PickDesignSystemAsync("ths-custom", "Classic Fluent");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");
        await Expect(Page.Locator("#custom-last-theme")).ToHaveTextAsync("dark");
    }

    [TestMethod]
    public async Task Parts_NoColorSchemeAndNoDesignSystem_DropEitherHalf()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#ths-no-scheme .bit-ths-dds")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#ths-no-scheme .bit-ths-csb")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#ths-no-design .bit-ths-dds")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#ths-no-design .bit-ths-csb")).ToHaveCountAsync(2);
        await Expect(Page.Locator("#ths-no-design .bit-ths-csb:visible")).ToHaveCountAsync(1);

        await Expect(Page.Locator("#ths-none .bit-ths-dds")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#ths-none .bit-ths-csb")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#ths-none")).ToHaveAttributeAsync("role", "group");
    }

    [TestMethod]
    public async Task IconsAndTitles_AreTheParametersGiven_AndTheIconsRender()
    {
        await OpenAsync(Route);

        var combobox = Page.Locator("#ths-titles [role=combobox]");
        await Expect(combobox).ToHaveAttributeAsync("title", "Skin");
        await Expect(combobox).ToHaveAttributeAsync("aria-label", "Skin");

        var toDark = Page.Locator("#ths-titles .bit-ths-lgt");
        await Expect(toDark).ToHaveAttributeAsync("title", "Go dark");
        await Expect(toDark).ToHaveAttributeAsync("aria-label", "Go dark");
        await Expect(toDark.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Brightness"));
        await Expect(toDark.Locator("i")).ToHaveAttributeAsync("aria-hidden", "true");

        var toLight = Page.Locator("#ths-titles .bit-ths-drk");
        await Expect(toLight).ToHaveAttributeAsync("title", "Go light");
        await Expect(toLight.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Sunny"));

        // The glyph is drawn by the icon font: its ::before carries content.
        var content = await toDark.Locator("i").EvaluateAsync<string>("el => getComputedStyle(el, '::before').content");
        Assert.IsFalse(string.IsNullOrEmpty(content) || content is "none" or "normal", $"The icon drew no glyph ({content}).");

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Go dark" })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Keyboard_TheToggleAndThePicker_AreOperatedFromTheKeyboard()
    {
        await OpenAsync(Route);

        var toDark = Page.Locator("#ths-basic .bit-ths-lgt");
        await toDark.FocusAsync();
        await Expect(toDark).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");

        var toLight = Page.Locator("#ths-basic .bit-ths-drk");
        await toLight.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "light");

        // Shift+Tab from the toggle reaches the picker, which opens and picks from the keyboard.
        await toDark.FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        var combobox = Page.Locator("#ths-basic [role=combobox]");
        await Expect(combobox).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(combobox).ToHaveAttributeAsync("aria-expanded", "true");

        var list = Page.Locator($"[id='{await combobox.GetAttributeAsync("aria-controls")}']");
        await Expect(list.GetByRole(AriaRole.Option, new() { Name = "Fluent", Exact = true })).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(list.GetByRole(AriaRole.Option, new() { Name = "Fluent 2", Exact = true })).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(list.GetByRole(AriaRole.Option, new() { Name = "Material", Exact = true })).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "material-light");
        await Expect(PickerText("ths-basic")).ToHaveTextAsync("Material");
    }

    [TestMethod]
    public async Task Keyboard_TheFocusedSchemeButton_DrawsTheFocusRing()
    {
        await OpenAsync(Route);

        var combobox = Page.Locator("#ths-basic [role=combobox]");
        await combobox.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        var toDark = Page.Locator("#ths-basic .bit-ths-lgt");
        await Expect(toDark).ToBeFocusedAsync();

        // The ring is the themed box-shadow of the focus-ring mixin, which only :focus-visible draws.
        await Expect(toDark).Not.ToHaveCSSAsync("box-shadow", "none");

        await Page.Locator("#ths-basic .bit-ths-lgt").BlurAsync();
        await Expect(toDark).ToHaveCSSAsync("box-shadow", "none");
    }

    [TestMethod]
    public async Task StyleAndClass_ReachTheRoot_AndStylesAndClassesReachEveryPart()
    {
        await OpenAsync(Route);

        var styled = Page.Locator("#ths-style");
        await Expect(styled).ToHaveClassAsync(HasClass("custom-root-class"));
        await Expect(styled).ToHaveClassAsync(HasClass("bit-ths"));
        await Expect(styled).ToHaveCSSAsync("padding-top", "7px");
        await Expect(styled).ToHaveCSSAsync("outline-style", "dashed");

        await Expect(Page.Locator("#ths-styles")).ToHaveCSSAsync("column-gap", "21px");
        await Expect(Page.Locator("#ths-styles .bit-ths-dds")).ToHaveCSSAsync("margin-right", "3px");
        await Expect(Page.Locator("#ths-styles .bit-ths-lgt")).ToHaveCSSAsync("border-top-left-radius", "3px");
        await Expect(Page.Locator("#ths-styles .bit-ths-lgt")).ToHaveCSSAsync("outline-color", "rgb(0, 0, 255)");
        await Expect(Page.Locator("#ths-styles .bit-ths-drk")).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("rgb\\(0, 128, 0\\)"));
        await Expect(Page.Locator("#ths-styles .bit-ths-lgt i")).ToHaveCSSAsync("font-size", "11px");

        await Expect(Page.Locator("#ths-classes")).ToHaveClassAsync(HasClass("c-root"));
        await Expect(Page.Locator("#ths-classes .bit-ths-dds")).ToHaveClassAsync(HasClass("c-ds"));
        await Expect(Page.Locator("#ths-classes .bit-ths-drk")).ToHaveClassAsync(HasClass("c-csb"));
        await Expect(Page.Locator("#ths-classes .bit-ths-drk")).ToHaveClassAsync(HasClass("c-drk"));
        await Expect(Page.Locator("#ths-classes .bit-ths-lgt")).ToHaveClassAsync(HasClass("c-csb"));
        await Expect(Page.Locator("#ths-classes .bit-ths-lgt")).ToHaveClassAsync(HasClass("c-lgt"));
        await Expect(Page.Locator("#ths-classes .bit-ths-lgt i")).ToHaveClassAsync(HasClass("c-ico"));
    }

    [TestMethod]
    public async Task Rtl_ExplicitAndInherited_LayTheControlsOutRightToLeft()
    {
        await OpenAsync(Route);

        var rtl = Page.Locator("#ths-rtl");
        await Expect(rtl).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(rtl).ToHaveAttributeAsync("dir", "rtl");

        await AssertPickerIsOnTheRightAsync("ths-rtl", true);
        await AssertPickerIsOnTheRightAsync("ths-rtl-inherited", true);
        await AssertPickerIsOnTheRightAsync("ths-ltr", false);

        await Expect(Page.Locator("#ths-rtl-inherited")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
    }

    [TestMethod]
    public async Task Disabled_MarksEveryControl_AndNothingActivatesIt()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ths-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));

        foreach (var button in new[] { root.Locator(".bit-ths-lgt"), root.Locator(".bit-ths-drk") })
        {
            await Expect(button).ToBeDisabledAsync();
            await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        }

        await Expect(root.Locator(".bit-ths-lgt")).ToHaveCSSAsync("cursor", "default");
        Assert.IsTrue(double.Parse(await ComputedStyleAsync("#ths-disabled .bit-ths-lgt", "opacity"), System.Globalization.CultureInfo.InvariantCulture) < 1);
        await Expect(root.Locator(".bit-drp")).ToHaveClassAsync(HasClass("bit-dis"));

        // A real click on the disabled toggle, and a click delivered to the root, run nothing of the page's.
        await root.Locator(".bit-ths-lgt").ClickAsync(new() { Force = true });
        await root.DispatchEventAsync("click");
        await root.Locator(".bit-drp-wrp").ClickAsync(new() { Force = true });

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "light");
        await Expect(Page.Locator("#disabled-root-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-change-count")).ToHaveTextAsync("0");
        await Expect(root.Locator("[role=combobox]")).Not.ToHaveAttributeAsync("aria-expanded", "true");

        // Enabled again at runtime, the same click toggles the theme and reaches the page's handler.
        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(root.Locator(".bit-ths-lgt")).ToBeEnabledAsync();
        await Expect(root.Locator(".bit-ths-lgt")).Not.ToHaveAttributeAsync("aria-disabled", "true");

        await root.Locator(".bit-ths-lgt").ClickAsync();

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");
        await Expect(Page.Locator("#disabled-root-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#disabled-change-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task AriaLabelAndHtmlAttributes_ReachTheRoot()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ths-aria");
        await Expect(root).ToHaveAttributeAsync("aria-label", "Appearance");
        await Expect(root).ToHaveAttributeAsync("data-testid", "switcher-attrs");
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Appearance" })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheBox_CollapsedRemovesIt()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#ths-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Page.Locator("#ths-hidden")).ToBeHiddenAsync();
        var box = await Page.Locator("#ths-hidden").BoundingBoxAsync();
        Assert.IsNotNull(box);
        Assert.IsTrue(box.Height > 0);

        await Expect(Page.Locator("#ths-collapsed")).ToHaveCSSAsync("display", "none");
        Assert.IsNull(await Page.Locator("#ths-collapsed").BoundingBoxAsync());
    }

    [TestMethod]
    public async Task InitialTheme_IsWhatThePrerenderShows_UntilTheAppliedThemeIsReadBack()
    {
        var html = await (await Page.APIRequest.GetAsync($"{BaseUrl}{Route}")).TextAsync();

        Assert.AreEqual("Cupertino", ReadPrerenderedPick(html, "ths-initial"));
        Assert.AreEqual("Material", ReadPrerenderedPick(html, "ths-initial-pair"));
        Assert.AreEqual("Fluent", ReadPrerenderedPick(html, "ths-basic"));

        await OpenAsync(Route);

        // The document wears the light theme, which is Fluent's.
        await Expect(PickerText("ths-initial")).ToHaveTextAsync("Fluent");
        await Expect(PickerText("ths-initial-pair")).ToHaveTextAsync("Fluent");
    }

    [TestMethod]
    public async Task Runtime_ParametersChangedByThePage_RerenderTheSwitcher_AndItUnmountsCleanly()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ths-runtime");
        await Expect(root.Locator(".bit-ths-csb")).ToHaveCountAsync(2);

        await Page.Locator("#btn-runtime-toggle-scheme").ClickAsync();
        await Expect(root.Locator(".bit-ths-csb")).ToHaveCountAsync(0);
        await Page.Locator("#btn-runtime-toggle-scheme").ClickAsync();
        await Expect(root.Locator(".bit-ths-csb")).ToHaveCountAsync(2);

        await Page.Locator("#btn-runtime-title").ClickAsync();
        await Expect(root.Locator("[role=combobox]")).ToHaveAttributeAsync("title", "Look");

        await Page.Locator("#btn-runtime-items").ClickAsync();
        var options = await OpenPickerAsync("ths-runtime");
        await Expect(options).ToHaveCountAsync(1);
        await Expect(options.First).ToHaveAttributeAsync("aria-label", "Only Material");
        await Page.Keyboard.PressAsync("Escape");

        await Page.Locator("#btn-runtime-mount").ClickAsync();
        await Expect(Page.Locator("#runtime-mounted")).ToHaveTextAsync("False");
        await Expect(root).ToHaveCountAsync(0);

        // A theme change after the unmount reaches no torn-down switcher.
        await Page.Locator("#btn-set-material-dark").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "material-dark");
        await Expect(PickerText("ths-basic")).ToHaveTextAsync("Material");

        await Page.Locator("#btn-runtime-mount").ClickAsync();
        await Expect(PickerText("ths-runtime")).ToHaveTextAsync("Only Material");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task ForcedColors_TheSchemeButton_IsPaintedAsANativeButton()
    {
        await OpenAsync(Route);

        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        var toDark = Page.Locator("#ths-basic .bit-ths-lgt");
        await Expect(toDark).ToHaveCSSAsync("border-top-style", "solid");

        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.None });
        await Expect(toDark).ToHaveCSSAsync("border-top-style", "none");
    }

    [TestMethod]
    public async Task Persistence_APickAndAToggle_SurviveAReload()
    {
        await OpenAsync(PersistRoute);

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme-persist", "");

        await PickDesignSystemAsync("ths-persist", "Material");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "material-light");

        await Page.Locator("#ths-persist .bit-ths-lgt").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "material-dark");
        await Expect(Page.Locator("#persist-last-theme")).ToHaveTextAsync("material-dark");

        Assert.AreEqual("material-dark", await Page.EvaluateAsync<string>("() => localStorage.getItem('bit-current-theme')"));

        var cookies = await Page.Context.CookiesAsync();
        Assert.AreEqual("material-dark", cookies.FirstOrDefault(c => c.Name == "bit-theme-preference")?.Value);
        Assert.IsTrue(cookies.Any(c => c.Name == "bit-theme-pair"), "The pair cookie was not written.");

        await Page.ReloadAsync();
        await WaitForStatusAsync("Ready");

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "material-dark");
        await Expect(PickerText("ths-persist")).ToHaveTextAsync("Material");
        await Expect(Page.Locator("#ths-persist .bit-ths-drk")).ToBeVisibleAsync();
        await Expect(Page.Locator("#probe-surface")).ToHaveCSSAsync("background-color", "rgb(12, 19, 27)");

        await Page.Locator("#btn-read-persisted").ClickAsync();
        await Expect(Page.Locator("#persisted-theme")).ToHaveTextAsync("material-dark");

        // The toggle after the reload still flips within the restored pair.
        await Page.Locator("#ths-persist .bit-ths-drk").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "material-light");
    }

    [TestMethod]
    public async Task Persistence_TheRestoredTheme_IsPaintedBeforeTheCircuitConnects()
    {
        await OpenAsync(PersistRoute);

        await PickDesignSystemAsync("ths-persist", "Cupertino");
        await Page.Locator("#ths-persist .bit-ths-lgt").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "cupertino-dark");

        // Blazor's own script is served empty, so the page stays the prerendered one: whatever is painted now is the
        // theme script's own restore.
        await Page.RouteAsync("**/_framework/blazor.web.js", route => route.FulfillAsync(new() { Status = 200, ContentType = "text/javascript", Body = "" }));
        await Page.ReloadAsync();

        await Expect(Page.Locator("#status")).ToHaveTextAsync("Loading");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "cupertino-dark");
        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", "rgb(90, 161, 255)");
        await Expect(Page.Locator("#ths-persist .bit-ths-drk")).ToBeVisibleAsync();
        await Expect(Page.Locator("#ths-persist .bit-ths-lgt")).ToBeHiddenAsync();
    }



    [TestMethod]
    public async Task Available_ADesignSystemThePageDoesNotShip_IsRefused_AndThePickerStaysOnTheOneWorn()
    {
        await OpenAsync("/e2e/theme-switcher/available");

        var html = Page.Locator("html");
        await Expect(html).ToHaveAttributeAsync("bit-theme-available", "light dark material-light material-dark");

        await PickDesignSystemAsync("ths-persist", "Cupertino");

        await Expect(html).ToHaveAttributeAsync("bit-theme", "light");
        await Expect(PickerText("ths-persist")).ToHaveTextAsync("Fluent");
        await Expect(Page.Locator("#persist-change-count")).ToHaveTextAsync("0");

        await PickDesignSystemAsync("ths-persist", "Material");

        await Expect(html).ToHaveAttributeAsync("bit-theme", "material-light");
        await Expect(PickerText("ths-persist")).ToHaveTextAsync("Material");
        await Expect(Page.Locator("#persist-change-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#persist-last-theme")).ToHaveTextAsync("material-light");
    }



    [TestMethod]
    public async Task InADialog_EscapeClosesThePickerFirst_AndTheDialogOnTheNextPress()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await Expect(Page.Locator("#ths-dialog")).ToBeVisibleAsync();

        var combobox = Page.Locator("#ths-dialog [role=combobox]");
        await combobox.ClickAsync();
        await Expect(combobox).ToHaveAttributeAsync("aria-expanded", "true");

        // The open list takes the focus onto its selected option, which is where the reader's Escape lands.
        var list = Page.Locator($"[id='{await combobox.GetAttributeAsync("aria-controls")}']");
        await Expect(list.GetByRole(AriaRole.Option, new() { Name = "Fluent", Exact = true })).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(combobox).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(combobox).ToBeFocusedAsync();
        await Expect(Page.Locator("#ths-dialog")).ToBeVisibleAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#ths-dialog")).ToBeHiddenAsync();
    }



    private ILocator PickerText(string switcherId) => Page.Locator($"#{switcherId} .bit-drp-tdp");

    private async Task<ILocator> OpenPickerAsync(string switcherId)
    {
        var combobox = Page.Locator($"#{switcherId} [role=combobox]");
        await combobox.ClickAsync();
        await Expect(combobox).ToHaveAttributeAsync("aria-expanded", "true");

        var listId = await combobox.GetAttributeAsync("aria-controls");
        var options = Page.Locator($"[id='{listId}'] [role=option]");
        await Expect(options.First).ToBeVisibleAsync();

        return options;
    }

    private async Task PickDesignSystemAsync(string switcherId, string name)
    {
        await OpenPickerAsync(switcherId);

        var listId = await Page.Locator($"#{switcherId} [role=combobox]").GetAttributeAsync("aria-controls");

        await Page.Locator($"[id='{listId}']").GetByRole(AriaRole.Option, new() { Name = name, Exact = true }).ClickAsync();
    }

    private async Task AssertPickerIsOnTheRightAsync(string switcherId, bool expected)
    {
        var picker = await Page.Locator($"#{switcherId} .bit-ths-dds").BoundingBoxAsync();
        var button = await Page.Locator($"#{switcherId} .bit-ths-csb:visible").BoundingBoxAsync();

        Assert.IsNotNull(picker);
        Assert.IsNotNull(button);
        Assert.AreEqual(expected, picker.X > button.X, $"{switcherId}: picker at {picker.X}, button at {button.X}");
    }

    private static string? ReadPrerenderedPick(string html, string switcherId)
    {
        var start = html.IndexOf($"id=\"{switcherId}\"", StringComparison.Ordinal);
        if (start < 0) return null;

        var match = System.Text.RegularExpressions.Regex.Match(html[start..], "class=\"bit-drp-tdp\">([^<]*)<");

        return match.Success ? match.Groups[1].Value : null;
    }

    private static double ParsePx(string value) => double.Parse(value.Replace("px", string.Empty), System.Globalization.CultureInfo.InvariantCulture);
}
