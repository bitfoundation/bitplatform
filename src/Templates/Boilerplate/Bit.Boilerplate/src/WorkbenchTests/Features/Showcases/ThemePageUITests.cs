namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>The theme page in a view: the theme's colors, every section of components in their states, the forced states and each overlay, opened by its switch and by its own control.</summary>
[TestClass, TestCategory("UITest")]
public class ThemePageUITests : ShowcaseUITest
{
    private const string PageKey = "theme";

    private ILocator Section(string heading) => View().Locator("section").Filter(new() { Has = View().GetByRole(AriaRole.Heading, new() { Name = heading, Level = 2, Exact = true }) });

    private ILocator Component(string name) => View().Locator(".component").Filter(new() { Has = View().GetByRole(AriaRole.Heading, new() { Name = name, Level = 3, Exact = true }) });

    private ILocator StateOf(string component, string state) => Component(component).Locator(".state").Filter(new() { Has = View().GetByText(state, new() { Exact = true }) });

    private ILocator Swatch(string token) => View().Locator(".swatch-cell").Filter(new() { Has = View().GetByText(token, new() { Exact = true }) });

    private ILocator Overlays => Section(WorkbenchStrings.ThemeOverlays);

    private ILocator TheDialog => View().GetByRole(AriaRole.Dialog, new() { Name = WorkbenchStrings.ThemeDialogTitle });

    private ILocator ThePanel => View().Locator(".bit-pnl").GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeOverlayPanel, Exact = true });

    private ILocator TheOverlaysCalendar => OpenPopups.GetByRole(AriaRole.Dialog);

    private ILocator TheOverlaysMenu => OpenPopups.GetByRole(AriaRole.Menu);

    private Task<string> ValueOf(string token) => Swatch(token).Locator(".value").InnerTextAsync();

    [TestMethod, EverySurface]
    public async Task TheColors_Should_ShowEveryTokenWithItsValue(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var colors = Section(WorkbenchStrings.ThemeColors);

        string[] families = [WorkbenchStrings.ThemePrimary, WorkbenchStrings.ThemeSecondary, WorkbenchStrings.ThemeTertiary, WorkbenchStrings.ThemeInfo, WorkbenchStrings.ThemeSuccess,
                             WorkbenchStrings.ThemeWarning, WorkbenchStrings.ThemeSevereWarning, WorkbenchStrings.ThemeError, WorkbenchStrings.ThemeForeground, WorkbenchStrings.ThemeBackground, WorkbenchStrings.ThemeBorder];

        foreach (var family in families)
        {
            await Expect(colors.GetByRole(AriaRole.Heading, new() { Name = family, Level = 3, Exact = true })).ToBeVisibleAsync();
        }

        await Expect(colors.Locator(".swatch-cell")).ToHaveCountAsync(8 * 6 + 4 + 5 + 4);

        await WorkbenchTestReviewer.WaitFor(() => colors.Locator(".value").AllInnerTextsAsync(), values => values.All(value => value.Length > 0));

        await Expect(Swatch("--bit-clr-pri").Locator(".value")).ToHaveTextAsync(new Regex("^(#|rgb)"));
    }

    [TestMethod, EverySurface]
    public async Task TheColors_Should_FollowTheViewsThemeAsItChanges(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(Swatch("--bit-clr-bg-pri").Locator(".value")).Not.ToBeEmptyAsync();

        var light = await ValueOf("--bit-clr-bg-pri");

        await UseTheTools(WorkbenchStrings.ThemeDark);

        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await WorkbenchTestReviewer.WaitFor(() => ValueOf("--bit-clr-bg-pri"), dark => dark.Length > 0 && dark != light);
    }

    [TestMethod, EverySurface]
    public async Task TheColors_Should_ShowTheAccentTriedOnTheView(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?accent=ff0000");

        await Expect(Swatch("--bit-clr-pri").Locator(".value")).ToHaveTextAsync(new Regex("^#ff0000|^rgb\\(255, 0, 0\\)", RegexOptions.IgnoreCase));
    }

    [TestMethod, EverySurface]
    public async Task EverySection_Should_ShowItsParts(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeHeading, Level = 2, Exact = true })).ToBeVisibleAsync();
        await Expect(View().GetByText(WorkbenchStrings.ThemeStatesNote)).ToBeVisibleAsync();

        await Expect(Section(WorkbenchStrings.ThemeTypography).Locator(".type-row")).ToHaveCountAsync(Enum.GetValues<BitTypography>().Length - 1);
        await Expect(Section(WorkbenchStrings.ThemeShape).Locator(".radius-box")).ToHaveCountAsync(11);

        var feedback = Section(WorkbenchStrings.ThemeFeedback);

        await Expect(feedback.Locator(".column").First.Locator(".bit-msg")).ToHaveCountAsync(5);
        await Expect(feedback.GetByText(WorkbenchStrings.ThemeProgress)).ToBeVisibleAsync();

        await Expect(Section(WorkbenchStrings.ThemeContainers).GetByText(WorkbenchStrings.RoleGlobalAdmin)).ToBeVisibleAsync();

        await ExpectNoSidewaysScroll();
    }

    [TestMethod, EverySurface]
    public async Task EveryComponent_Should_ShowItsBlockUnderItsSection(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        (string Section, string[] Components)[] sections =
        [
            (WorkbenchStrings.ThemeButtons, [.. Enum.GetValues<BitVariant>().Select(variant => $"BitButton · {variant}"), "BitActionButton", "BitToggleButton", "BitLink", "BitMenuButton"]),
            (WorkbenchStrings.ThemeInputs, ["BitTextField", "BitSearchBox", "BitNumberField", "BitOtpInput", "BitPhoneInput", "BitDatePicker"]),
            (WorkbenchStrings.ThemeSelection, ["BitDropdown", "BitCheckbox", "BitToggle", "BitChoiceGroup", "BitSlider"]),
            (WorkbenchStrings.ThemeNavigation, ["BitPivot", "BitNavBar", "BitNav", "BitAccordion"]),
            (WorkbenchStrings.ThemeFeedback, [WorkbenchStrings.ThemeLoaders, "BitTooltip"]),
            (WorkbenchStrings.ThemeContainers, ["BitPersona"])
        ];

        foreach (var (section, components) in sections)
        {
            foreach (var component in components)
            {
                await Expect(Section(section).GetByRole(AriaRole.Heading, new() { Name = component, Level = 3, Exact = true })).ToBeVisibleAsync();
            }
        }
    }

    [TestMethod, EverySurface]
    public async Task EveryForcedState_Should_ReachItsComponent(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(View().Locator(".state-sample[data-wb-state]").First).ToBeVisibleAsync();

        const string missingStates = """
            body => [...body.querySelectorAll('.state-sample[data-wb-state]')]
                .filter(sample => sample.querySelector(`[data-wb-pseudo~="${sample.dataset.wbState}"]`) === null)
                .map(sample => `${sample.closest('.component')?.querySelector('h3')?.textContent}: ${sample.dataset.wbState}`)
            """;

        await WorkbenchTestReviewer.WaitFor(() => View().Locator("body").EvaluateAsync<string[]>(missingStates), missing => missing.Length == 0);
    }

    [TestMethod, EverySurface]
    public async Task TheForcedStates_Should_LookDifferentFromTheDefault(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        const string look = "element => { const style = getComputedStyle(element); return [style.backgroundColor, style.borderColor, style.color, style.boxShadow, style.outlineStyle].join('|'); }";

        var fill = $"BitButton · {BitVariant.Fill}";
        var original = await StateOf(fill, WorkbenchStrings.ThemeStateDefault).GetByRole(AriaRole.Button).EvaluateAsync<string>(look);

        foreach (var state in new[] { WorkbenchStrings.ThemeStateHover, WorkbenchStrings.ThemeStatePressed })
        {
            await WorkbenchTestReviewer.WaitFor(() => StateOf(fill, state).Locator("button").EvaluateAsync<string>(look), forced => forced != original);
        }

        var focused = await StateOf("BitTextField", WorkbenchStrings.ThemeStateFocused).Locator("input").EvaluateAsync<string>("input => input.parentElement.getAttribute('data-wb-pseudo') ?? ''");

        Assert.Contains("focus-within", focused);
    }

    [TestMethod, EverySurface]
    public async Task AForcedSample_Should_NotBeUsableButADefaultOneShould(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(Component("BitToggle").Locator(".state-sample[data-wb-state]").First).ToHaveAttributeAsync("inert", string.Empty);
        await Expect(StateOf("BitToggle", WorkbenchStrings.ThemeStateOff).Locator(".state-sample")).Not.ToHaveAttributeAsync("inert", string.Empty);

        var toggle = StateOf("BitToggle", WorkbenchStrings.ThemeStateOff).GetByRole(AriaRole.Switch);

        await Expect(toggle).Not.ToBeCheckedAsync();
        await Press(toggle);
        await Expect(toggle).ToBeCheckedAsync();

        var checkbox = StateOf("BitCheckbox", WorkbenchStrings.ThemeStateUnchecked).GetByRole(AriaRole.Checkbox);
        var checkboxLabel = StateOf("BitCheckbox", WorkbenchStrings.ThemeStateUnchecked).Locator("label");

        await Press(checkboxLabel);
        await Expect(checkbox).ToBeCheckedAsync();

        var favorite = StateOf("BitToggleButton", WorkbenchStrings.ThemeStateOff).GetByRole(AriaRole.Button);

        await Expect(favorite).ToHaveAttributeAsync("aria-pressed", "false");
        await Press(favorite);
        await Expect(favorite).ToHaveAttributeAsync("aria-pressed", "true");

        var accordion = StateOf("BitAccordion", WorkbenchStrings.ThemeStateCollapsed).GetByRole(AriaRole.Button);

        await Expect(accordion).ToHaveAttributeAsync("aria-expanded", "false");
        await Press(accordion);
        await Expect(accordion).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [TestMethod, EverySurface]
    public async Task NoOverlay_Should_BeOpenWithoutASwitch(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(TheDialog).ToHaveCountAsync(0);
        await Expect(ThePanel).Not.ToBeInViewportAsync();
        await Expect(OpenPopups.GetByRole(AriaRole.Listbox)).ToHaveCountAsync(0);
        await Expect(TheOverlaysMenu).Not.ToBeVisibleAsync();
        await Expect(View().GetByRole(AriaRole.Status, new() { Name = WorkbenchStrings.ThemeSuccess, Exact = true })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheDialogSwitch_Should_OpenTheDialog(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-overlay=dialog");

        await Expect(TheDialog).ToBeInViewportAsync();
        await Expect(TheDialog).ToContainTextAsync(WorkbenchStrings.ThemeDialogMessage);
    }

    [TestMethod, EverySurface]
    public async Task ThePanelSwitch_Should_OpenThePanel(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-overlay=panel");

        await Expect(ThePanel).ToBeInViewportAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheSnackBarSwitch_Should_ShowASnackBarOfEachKind(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-overlay=snackbar");

        foreach (var kind in new[] { WorkbenchStrings.ThemeSuccess, WorkbenchStrings.ThemeWarning, WorkbenchStrings.ThemeError })
        {
            await Expect(View().GetByRole(AriaRole.Status, new() { Name = kind, Exact = true })).ToBeInViewportAsync();
        }
    }

    [TestMethod, EverySurface]
    public async Task TheOverlaySwitchInTheTools_Should_OpenItAndNoneShouldCloseIt(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await UseTheTools(WorkbenchStrings.ThemeOverlayDialog);

        await Expect(Page).ToHaveURLAsync(new Regex("sw-overlay=dialog"));
        await Expect(TheDialog).ToBeVisibleAsync();

        await UseTheTools(WorkbenchStrings.ThemeOverlayNone);

        await Expect(Page).Not.ToHaveURLAsync(new Regex("sw-overlay"));
        await Expect(TheDialog).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheDropdown_Should_OpenFromItsFieldAndKeepWhatIsPicked(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var dropdown = Overlays.GetByRole(AriaRole.Combobox).First;
        var option = string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ThemeOption, 3);

        await ScrollToAndWaitUntilStill(dropdown);
        await Pick(dropdown, option);

        await Expect(dropdown).ToContainTextAsync(option);
        await Expect(OpenPopups.GetByRole(AriaRole.Listbox)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheDatePicker_Should_OpenItsCalendarFromItsField(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var field = Overlays.GetByRole(AriaRole.Combobox).Nth(1);

        await ScrollToAndWaitUntilStill(field);
        await Press(field);

        await Expect(TheOverlaysCalendar).ToBeInViewportAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheMenu_Should_OpenFromItsButtonAndCloseOnAnItem(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await ScrollToAndWaitUntilStill(Overlays.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ThemeActions, Exact = true }));
        await Press(Overlays.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ThemeActions, Exact = true }));

        var edit = TheOverlaysMenu.GetByRole(AriaRole.Menuitem, new() { Name = AppStrings.Edit, Exact = true });

        await Expect(edit).ToBeVisibleAsync();

        await Press(edit);

        await Expect(edit).Not.ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheDialog_Should_OpenFromItsButtonAndCloseWithCancel(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(Overlays.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ThemeOverlayDialog, Exact = true }));

        await Expect(TheDialog).ToBeInViewportAsync();

        await Press(TheDialog.GetByRole(AriaRole.Button, new() { Name = AppStrings.Cancel, Exact = true }));

        await Expect(TheDialog).ToHaveCountAsync(0);
    }
}
