using Boilerplate.Workbench.Components.Pages.Showcases.Theme;

namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>
/// The theme page in a view: its tabs, one section at a time, every token family with its values, every category of
/// components in their states, the forced states, the Section and Density switches, and each overlay, opened by its switch
/// and by its own control.
/// </summary>
[TestClass, TestCategory("UITest")]
public class ThemePageUITests : ShowcaseUITest
{
    private const string PageKey = "theme";

    /// <summary>The tabs of the demo's categories of components, in their order.</summary>
    private static readonly string[] componentTabs =
    [
        WorkbenchStrings.ThemeButtons, WorkbenchStrings.ThemeInputs, WorkbenchStrings.ThemePickers, WorkbenchStrings.ThemeLayouts, WorkbenchStrings.ThemeLists,
        WorkbenchStrings.ThemeNavigation, WorkbenchStrings.ThemeNotifications, WorkbenchStrings.ThemeProgressSection, WorkbenchStrings.ThemeSurfaces,
        WorkbenchStrings.ThemeUtilities, WorkbenchStrings.ThemeExtras, WorkbenchStrings.ThemeAi
    ];

    private ILocator Section(string heading) => View().Locator("section").Filter(new() { Has = View().GetByRole(AriaRole.Heading, new() { Name = heading, Level = 2, Exact = true }) });

    private ILocator Tab(string name) => View().GetByRole(AriaRole.Tab, new() { Name = name, Exact = true });

    private ILocator Component(string name) => View().Locator(".component").Filter(new() { Has = View().GetByRole(AriaRole.Heading, new() { Name = name, Level = 3, Exact = true }) });

    private ILocator StateOf(string component, string state) => Component(component).Locator(".state").Filter(new() { Has = View().GetByText(state, new() { Exact = true }) });

    private ILocator Swatch(string token) => View().Locator(".swatch-cell").Filter(new() { Has = View().GetByText(token, new() { Exact = true }) });

    private ILocator Overlays => Section(WorkbenchStrings.ThemeOverlays);

    private ILocator TheDialog => View().GetByRole(AriaRole.Dialog, new() { Name = WorkbenchStrings.ThemeDialogTitle });

    private ILocator ThePanel => View().Locator(".bit-pnl").GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeOverlayPanel, Exact = true });

    private ILocator TheOverlaysCalendar => OpenPopups.GetByRole(AriaRole.Dialog);

    private ILocator TheOverlaysMenu => OpenPopups.GetByRole(AriaRole.Menu);

    private Task<string> ValueOf(string token) => Swatch(token).Locator(".value").InnerTextAsync();

    /// <summary>Presses the page's tab and waits for its section, which has the tab's name as its heading.</summary>
    private async Task ShowTab(string name)
    {
        await Press(Tab(name));

        await Expect(Section(name)).ToBeVisibleAsync();
    }

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

        // Each role in its 15 slots, the three neutral families, the grays, the special colors and the intent tier.
        await Expect(colors.Locator(".swatch-cell")).ToHaveCountAsync(ThemeTokens.Roles.Sum(r => r.Tokens.Length) + ThemeTokens.Neutrals.Sum(n => n.Tokens.Length)
                                                                     + ThemeTokens.Grays.Length + ThemeTokens.SpecialColors.Length + ThemeTokens.Semantic.Length);
        await Expect(Swatch("--bit-clr-pri-tint")).ToBeVisibleAsync();

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
    public async Task TheTabs_Should_ShowOneSectionAtATimeInTheirOrder(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeHeading, Level = 2, Exact = true })).ToBeVisibleAsync();
        await Expect(View().GetByText(WorkbenchStrings.ThemeStatesNote)).ToBeVisibleAsync();

        // The token families, the overlays, then the demo's categories; the first is open and the only one rendered.
        string[] tabs = [WorkbenchStrings.ThemeColors, WorkbenchStrings.ThemeTypography, WorkbenchStrings.ThemeShape, WorkbenchStrings.ThemeShadows, WorkbenchStrings.ThemeSizes,
                         WorkbenchStrings.ThemeMotion, WorkbenchStrings.ThemeLayers, WorkbenchStrings.ThemeOverlays, .. componentTabs];

        await Expect(View().GetByRole(AriaRole.Tablist).GetByRole(AriaRole.Tab)).ToHaveTextAsync(tabs);
        await Expect(Tab(WorkbenchStrings.ThemeColors)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(View().Locator("section.theme-section")).ToHaveCountAsync(1);

        await ShowTab(WorkbenchStrings.ThemeButtons);

        await Expect(Tab(WorkbenchStrings.ThemeButtons)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Section(WorkbenchStrings.ThemeColors)).ToHaveCountAsync(0);
        await Expect(View().Locator("section.theme-section")).ToHaveCountAsync(1);
        await Expect(Page).ToHaveURLAsync(new Regex("sw-section=buttons"));

        await ShowTab(WorkbenchStrings.ThemeColors);

        await Expect(Page).Not.ToHaveURLAsync(new Regex("sw-section"));
        await ExpectNoSidewaysScroll();
    }

    [TestMethod, EverySurface]
    public async Task EveryTokenTab_Should_ShowItsParts(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-section=typography");

        await Expect(Section(WorkbenchStrings.ThemeTypography).Locator(".type-row")).ToHaveCountAsync(Enum.GetValues<BitTypography>().Length - 1);
        await Expect(Section(WorkbenchStrings.ThemeTypography).Locator(".ramp-row")).ToHaveCountAsync(ThemeTokens.FontSizes.Length);

        await ShowTab(WorkbenchStrings.ThemeShape);
        await Expect(Section(WorkbenchStrings.ThemeShape).Locator(".radius-box")).ToHaveCountAsync(ThemeTokens.Radii.Sum(group => group.Tokens.Length));

        await ShowTab(WorkbenchStrings.ThemeShadows);
        await Expect(Section(WorkbenchStrings.ThemeShadows).Locator(".shadow-box")).ToHaveCountAsync(ThemeTokens.Shadows.Sum(group => group.Tokens.Length));

        await ShowTab(WorkbenchStrings.ThemeSizes);
        await Expect(Section(WorkbenchStrings.ThemeSizes).Locator(".bar")).ToHaveCountAsync(ThemeTokens.Sizes.Sum(group => group.Tokens.Length));

        await ShowTab(WorkbenchStrings.ThemeMotion);
        await Expect(Section(WorkbenchStrings.ThemeMotion).Locator(".track")).ToHaveCountAsync(5);

        await ShowTab(WorkbenchStrings.ThemeLayers);
        await Expect(Section(WorkbenchStrings.ThemeLayers).Locator(".layer")).ToHaveCountAsync(ThemeTokens.Layers.Length);

        await ExpectNoSidewaysScroll();
    }

    [TestMethod, EverySurface]
    public async Task TheTokenValues_Should_BeReadForEveryFamily(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-section=shadows");

        // A shadow, a size the density scales, a duration and a breakpoint each read as the browser resolves them.
        await WorkbenchTestReviewer.WaitFor(() => Section(WorkbenchStrings.ThemeShadows).Locator(".value").AllInnerTextsAsync(), values => values.All(value => value.Length > 0));

        await ShowTab(WorkbenchStrings.ThemeSizes);
        await Expect(Section(WorkbenchStrings.ThemeSizes).Locator(".value").First).ToHaveTextAsync(new Regex(@"^\d+(\.\d+)?px$"));

        await ShowTab(WorkbenchStrings.ThemeMotion);
        await Expect(Section(WorkbenchStrings.ThemeMotion).Locator(".value").First).ToHaveTextAsync(new Regex(@"\d+(ms|s)$"));

        await ShowTab(WorkbenchStrings.ThemeLayers);
        await Expect(Section(WorkbenchStrings.ThemeLayers).Locator(".breakpoint .value").Last).ToHaveTextAsync(new Regex(@"\d"));
    }

    [TestMethod, EverySurface]
    public async Task TheDensitySwitch_Should_ScaleTheSizes(WorkbenchSurface surface)
    {
        const string height = "--bit-siz-ctrl-md";

        var sizes = Section(WorkbenchStrings.ThemeSizes);

        async Task<double> ControlHeight() => double.Parse((await sizes.Locator(".size-row").Filter(new() { HasText = height["--bit-".Length..] }).Locator(".value").InnerTextAsync()).TrimEnd('p', 'x'), CultureInfo.InvariantCulture);

        await OpenWorkbench(PageKey, "?sw-section=sizes");

        await WorkbenchTestReviewer.WaitFor(() => sizes.Locator(".value").First.InnerTextAsync(), value => value.Length > 0);
        var normal = await ControlHeight();

        await OpenWorkbench(PageKey, "?sw-section=sizes&sw-density=compact");

        await WorkbenchTestReviewer.WaitFor(() => sizes.Locator(".value").First.InnerTextAsync(), value => value.Length > 0);
        Assert.IsLessThan(normal, await ControlHeight());
        await Expect(sizes.GetByText("× 0.9")).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheSectionSwitch_Should_OpenThePageOnItsTab(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-section=pickers");

        await Expect(Section(WorkbenchStrings.ThemePickers)).ToBeVisibleAsync();
        await Expect(Tab(WorkbenchStrings.ThemePickers)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Section(WorkbenchStrings.ThemeColors)).ToHaveCountAsync(0);
        await Expect(Section(WorkbenchStrings.ThemeButtons)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task EveryComponent_Should_ShowItsBlockUnderItsTab(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        // The categories of the bit BlazorUI demo, each with its components.
        (string Section, string[] Components)[] sections =
        [
            (WorkbenchStrings.ThemeButtons, ["BitActionButton", .. Enum.GetValues<BitVariant>().Select(variant => $"BitButton · {variant}"), "BitButton", "BitButtonGroup", "BitMenuButton", "BitToggleButton"]),
            (WorkbenchStrings.ThemeInputs, ["BitCalendar", "BitCheckbox", "BitChoiceGroup", "BitDropdown", "BitFileInput", "BitFileUpload", "BitNumberField", "BitOtpInput", "BitRating", "BitSearchBox", "BitSlider", "BitTagsInput", "BitTextField", "BitToggle"]),
            (WorkbenchStrings.ThemePickers, ["BitCircularTimePicker", "BitColorPicker", "BitDatePicker", "BitDateRangePicker", "BitTimePicker"]),
            (WorkbenchStrings.ThemeLayouts, ["BitFooter", "BitGrid", "BitHeader", "BitLayout", "BitSpacer", "BitStack"]),
            (WorkbenchStrings.ThemeLists, ["BitBasicList", "BitCarousel", "BitSwiper", "BitTimeline"]),
            (WorkbenchStrings.ThemeNavigation, ["BitBreadcrumb", "BitDropMenu", "BitNav", "BitNavBar", "BitPagination", "BitPivot"]),
            (WorkbenchStrings.ThemeNotifications, ["BitBadge", "BitMessage", "BitPersona", "BitSnackBar", "BitTag"]),
            (WorkbenchStrings.ThemeProgressSection, [WorkbenchStrings.ThemeLoaders, "BitProgress", "BitShimmer"]),
            (WorkbenchStrings.ThemeSurfaces, ["BitAccordion", "BitCallout", "BitCard", "BitCollapse", "BitDialog", "BitModal", "BitModalService", "BitPanel", "BitScrollablePane", "BitSplitter", "BitTooltip"]),
            (WorkbenchStrings.ThemeUtilities, ["BitElement", "BitIcon", "BitImage", "BitLink", "BitMediaQuery", "BitOverlay", "BitPullToRefresh", "BitSeparator", "BitSticky", "BitSwipeTrap", "BitText"]),
            (WorkbenchStrings.ThemeExtras, ["BitAccordionList", "BitChart", "BitCountryPicker", "BitDataGrid", "BitErrorBoundary", "BitFlag", "BitFullCalendar", "BitInfiniteScrolling", "BitMap",
                                            "BitMarkdownEditor", "BitMarkdownViewer", "BitMessageBox", "BitNavPanel", "BitPdfViewer", "BitPhoneInput", "BitRichTextEditor", "BitTextShimmer",
                                            "BitTimeZonePicker", "BitVirtualize"]),
            (WorkbenchStrings.ThemeAi, ["BitAiAgentRunView", "BitAiAgentStatusBadge", "BitAiAgentStepView", "BitAiArtifactPartView", "BitAiArtifactView", "BitAiAttachmentChip", "BitAiChat",
                                        "BitAiChatMessage", "BitAiCitationMarker", "BitAiCodeBlockView", "BitAiCollapsible", "BitAiContextMeter", "BitAiConversationList", "BitAiCopyButton",
                                        "BitAiDataTable", "BitAiEmptyState", "BitAiErrorView", "BitAiForm", "BitAiGenerativeUiView", "BitAiImagePartView", "BitAiJsonView", "BitAiMarkdownView",
                                        "BitAiMessageActions", "BitAiMessageList", "BitAiMessagePartView", "BitAiModelSelector", "BitAiPromptInput", "BitAiReasoningPartView",
                                        "BitAiScrollToLatest", "BitAiSourceCard", "BitAiSourceList", "BitAiStreamingMarkdown", "BitAiStructuredDataView", "BitAiTextPartView",
                                        "BitAiThinkingIndicator", "BitAiTokenUsageView", "BitAiToolApproval", "BitAiToolCallView", "BitAiToolResultView", "BitAiVoiceInput"])
        ];

        Assert.AreSequenceEqual(componentTabs, sections.Select(section => section.Section).ToArray());

        foreach (var (section, components) in sections)
        {
            await ShowTab(section);

            foreach (var component in components)
            {
                await Expect(Section(section).GetByRole(AriaRole.Heading, new() { Name = component, Level = 3, Exact = true })).ToBeVisibleAsync();
            }
        }
    }

    [TestMethod, EverySurface]
    public async Task TheMessages_Should_ReadProperlyForEveryColor(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-section=notifications");

        await Expect(Component("BitMessage").Locator(".bit-msg").First).ToBeVisibleAsync();
        await Expect(Component("BitMessage").GetByText(new Regex(@"\bA (Info|Error)\b"))).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task EveryForcedState_Should_ReachItsComponent(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-section=buttons");

        await Expect(View().Locator(".state-sample[data-wb-state]").First).ToBeVisibleAsync();

        const string missingStates = """
            body => [...body.querySelectorAll('.state-sample[data-wb-state]')]
                .filter(sample => sample.querySelector(`[data-wb-pseudo~="${sample.dataset.wbState}"]`) === null)
                .map(sample => `${sample.closest('.component')?.querySelector('h3')?.textContent}: ${sample.dataset.wbState}`)
            """;

        // Each tab's samples are forced as the tab is shown.
        foreach (var tab in componentTabs)
        {
            await ShowTab(tab);

            await WorkbenchTestReviewer.WaitFor(() => View().Locator("body").EvaluateAsync<string[]>(missingStates), missing => missing.Length == 0);
        }
    }

    [TestMethod, EverySurface]
    public async Task TheForcedStates_Should_LookDifferentFromTheDefault(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-section=buttons");

        const string look = "element => { const style = getComputedStyle(element); return [style.backgroundColor, style.borderColor, style.color, style.boxShadow, style.outlineStyle].join('|'); }";

        var fill = $"BitButton · {BitVariant.Fill}";
        var original = await StateOf(fill, WorkbenchStrings.ThemeStateDefault).GetByRole(AriaRole.Button).EvaluateAsync<string>(look);

        foreach (var state in new[] { WorkbenchStrings.ThemeStateHover, WorkbenchStrings.ThemeStatePressed })
        {
            await WorkbenchTestReviewer.WaitFor(() => StateOf(fill, state).Locator("button").EvaluateAsync<string>(look), forced => forced != original);
        }

        await ShowTab(WorkbenchStrings.ThemeInputs);

        var focused = StateOf("BitTextField", WorkbenchStrings.ThemeStateFocused).Locator("input");

        await WorkbenchTestReviewer.WaitFor(() => focused.EvaluateAsync<string>("input => input.parentElement.getAttribute('data-wb-pseudo') ?? ''"), pseudo => pseudo.Contains("focus-within", StringComparison.Ordinal));
    }

    [TestMethod, EverySurface]
    public async Task AForcedSample_Should_NotBeUsableButADefaultOneShould(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-section=inputs");

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

        await ShowTab(WorkbenchStrings.ThemeButtons);

        var favorite = StateOf("BitToggleButton", WorkbenchStrings.ThemeStateOff).GetByRole(AriaRole.Button);

        await Expect(favorite).ToHaveAttributeAsync("aria-pressed", "false");
        await Press(favorite);
        await Expect(favorite).ToHaveAttributeAsync("aria-pressed", "true");

        await ShowTab(WorkbenchStrings.ThemeSurfaces);

        var accordion = StateOf("BitAccordion", WorkbenchStrings.ThemeStateCollapsed).GetByRole(AriaRole.Button);

        await Expect(accordion).ToHaveAttributeAsync("aria-expanded", "false");
        await Press(accordion);
        await Expect(accordion).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [TestMethod, EverySurface]
    public async Task NoOverlay_Should_BeOpenWithoutASwitch(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-section=overlays");

        await Expect(TheDialog).ToHaveCountAsync(0);
        await Expect(ThePanel).Not.ToBeInViewportAsync();
        await Expect(OpenPopups.GetByRole(AriaRole.Listbox)).ToHaveCountAsync(0);
        await Expect(TheOverlaysMenu).Not.ToBeVisibleAsync();
        await Expect(View().GetByRole(AriaRole.Status, new() { Name = WorkbenchStrings.ThemeSuccess, Exact = true })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheDropdownSwitch_Should_OpenTheListWithItsSectionInSight(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-overlay=dropdown");

        await Expect(VisibleOption(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ThemeOption, 1))).ToBeInViewportAsync();
        await Expect(Overlays.GetByRole(AriaRole.Combobox).First).ToBeInViewportAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheDateSwitch_Should_OpenTheCalendarWithItsSectionInSight(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-overlay=date");

        await Expect(TheOverlaysCalendar).ToBeVisibleAsync();
        await Expect(TheOverlaysCalendar).ToBeInViewportAsync();
        await Expect(Overlays.GetByRole(AriaRole.Combobox).Nth(1)).ToBeInViewportAsync();
        await Expect(TheOverlaysCalendar).ToContainTextAsync("1990");
    }

    [TestMethod, EverySurface]
    public async Task TheMenuSwitch_Should_OpenTheMenuWithItsSectionInSight(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-overlay=menu");

        await Expect(TheOverlaysMenu.GetByRole(AriaRole.Menuitem, new() { Name = AppStrings.Edit, Exact = true })).ToBeVisibleAsync();
        await Expect(TheOverlaysMenu.GetByRole(AriaRole.Menuitem, new() { Name = AppStrings.Edit, Exact = true })).ToBeInViewportAsync();
        await Expect(TheOverlaysMenu.GetByRole(AriaRole.Menuitem, new() { Name = AppStrings.Delete, Exact = true })).ToBeInViewportAsync();
        await Expect(Overlays.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ThemeActions, Exact = true })).ToBeInViewportAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheDialogSwitch_Should_OpenTheDialog(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-overlay=dialog");

        await Expect(TheDialog).ToBeInViewportAsync();
        await Expect(TheDialog).ToContainTextAsync(WorkbenchStrings.ThemeDialogMessage);
    }

    [TestMethod, EverySurface]
    public async Task TheCalloutSwitch_Should_OpenTheCalloutWithItsSectionInSight(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-overlay=callout");

        var content = View().GetByText(WorkbenchStrings.ThemeOverlaysOrdersBefore1000Arrive).Locator("visible=true").First;

        await Expect(content).ToBeInViewportAsync();
        await Expect(Overlays.GetByRole(AriaRole.Button, new() { Name = "Callout", Exact = true })).ToBeInViewportAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheModalSwitch_Should_OpenTheModal(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-overlay=modal");

        await Expect(View().GetByText(WorkbenchStrings.ThemeTheNewNameShowsOn)).ToBeInViewportAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheOverlaySwitch_Should_OpenOverAnyTabOrOnTheOverlaysTab(WorkbenchSurface surface)
    {
        // A dialog opens over the tab the Section switch picked.
        await OpenWorkbench(PageKey, "?sw-section=buttons&sw-overlay=dialog");

        await Expect(Section(WorkbenchStrings.ThemeButtons)).ToBeVisibleAsync();
        await Expect(TheDialog).ToBeInViewportAsync();

        // A menu opens in place, so the page opens on its tab.
        await OpenWorkbench(PageKey, "?sw-section=buttons&sw-overlay=menu");

        await Expect(Tab(WorkbenchStrings.ThemeOverlays)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(TheOverlaysMenu.GetByRole(AriaRole.Menuitem, new() { Name = AppStrings.Edit, Exact = true })).ToBeInViewportAsync();
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
        await OpenWorkbench(PageKey, "?sw-section=overlays");

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
        await OpenWorkbench(PageKey, "?sw-section=overlays");

        var field = Overlays.GetByRole(AriaRole.Combobox).Nth(1);

        await ScrollToAndWaitUntilStill(field);
        await Press(field);

        await Expect(TheOverlaysCalendar).ToBeInViewportAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheMenu_Should_OpenFromItsButtonAndCloseOnAnItem(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-section=overlays");

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
        await OpenWorkbench(PageKey, "?sw-section=overlays");

        await Press(Overlays.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ThemeOverlayDialog, Exact = true }));

        await Expect(TheDialog).ToBeInViewportAsync();

        await Press(TheDialog.GetByRole(AriaRole.Button, new() { Name = AppStrings.Cancel, Exact = true }));

        await Expect(TheDialog).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task ThePanel_Should_OpenFromItsButtonAndCloseWithItsCloseButton(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-section=overlays");

        await Press(Overlays.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ThemeOverlayPanel, Exact = true }));

        await Expect(ThePanel).ToBeInViewportAsync();

        await Press(View().Locator(".bit-pnl").GetByRole(AriaRole.Button).Locator("visible=true").First);

        await Expect(ThePanel).Not.ToBeInViewportAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheSnackBars_Should_ShowFromTheirButtonAndCloseOneByOne(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-section=overlays");

        await Press(Overlays.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ThemeOverlaySnackBar, Exact = true }));

        var success = View().GetByRole(AriaRole.Status, new() { Name = WorkbenchStrings.ThemeSuccess, Exact = true });

        await Expect(success).ToBeInViewportAsync();
        await Expect(View().GetByRole(AriaRole.Status, new() { Name = WorkbenchStrings.ThemeWarning, Exact = true })).ToBeInViewportAsync();
        await Expect(View().GetByRole(AriaRole.Status, new() { Name = WorkbenchStrings.ThemeError, Exact = true })).ToBeInViewportAsync();

        await Press(success.GetByRole(AriaRole.Button));

        await Expect(success).ToHaveCountAsync(0);
        await Expect(View().GetByRole(AriaRole.Status, new() { Name = WorkbenchStrings.ThemeError, Exact = true })).ToBeVisibleAsync();
    }
}
