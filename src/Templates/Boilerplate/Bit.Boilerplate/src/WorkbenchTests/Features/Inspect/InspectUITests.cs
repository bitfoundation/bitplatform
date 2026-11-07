namespace Boilerplate.WorkbenchTests.Features.Inspect;

/// <summary>Inspect mode: the card over what's under the pointer, and the panel a click opens, with an agent or without one.</summary>
[TestClass, TestCategory("UITest")]
public class InspectUITests : WorkbenchCodeCopyPageTest
{
    private const string ThemePage = "Workbench/Components/Pages/Showcases/Theme/ThemePage.razor";

    private const string StoreView = "?device=store-play-phone&backdrop=ocean";

    private ILocator Panel => Page.Locator(".component-panel");

    private ILocator Card => View().Locator(".wb-info");

    private ILocator IconButton => View().GetByLabel(WorkbenchStrings.ThemeIconButton, new() { Exact = true });

    private ILocator SectionHeading => View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeButtons, Exact = true });

    private ILocator Mode(string name) => Page.Locator(".modes").GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private ILocator Parameter(string name) => Panel.Locator(".parameter").Filter(new() { Has = Page.Locator(".parameter-name", new() { HasTextRegex = new Regex($"^{name}$") }) });

    private ILocator Section(string title) => Panel.Locator("section").Filter(new() { Has = Page.GetByText(title, new() { Exact = true }) });

    private ILocator Variable(string shortName) => Panel.Locator(".variable").Filter(new() { Has = Page.Locator(".variable-name", new() { HasTextRegex = new Regex($"^{shortName}$") }) });

    private async Task SetMode(string name, string? frameMode)
    {
        await Press(Mode(name));

        await Expect(Mode(name)).ToHaveAttributeAsync("aria-pressed", "true");

        if (frameMode is null)
        {
            await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("data-wb-mode", new Regex("."));
        }
        else
        {
            await Expect(View().Locator("html")).ToHaveAttributeAsync("data-wb-mode", frameMode);
        }
    }

    private async Task Inspect(string pageKey = "theme", string query = "")
    {
        await OpenWorkbench(pageKey, query);
        await SetMode(WorkbenchStrings.ModeInspect, "inspect");
    }

    private async Task PickTheIconButton()
    {
        await Inspect();
        await Press(IconButton);
        await Expect(Panel.Locator(".component-name")).ToHaveTextAsync(nameof(BitButton));
    }

    /// <summary>Writes an attribute into the icon button's tag in the copy, as an edit the running code doesn't have yet.</summary>
    private static void AddToTheIconButton(string attribute)
    {
        var file = InTheCopy(ThemePage);

        File.WriteAllText(file, File.ReadAllText(file).Replace("<BitButton IconOnly IconName=\"@BitIconName.Settings\"", $"<BitButton {attribute} IconOnly IconName=\"@BitIconName.Settings\"", StringComparison.Ordinal));
    }

    private static string LineOf(string text) => (Array.FindIndex(ReadTheCopy(ThemePage).ReplaceLineEndings().Split(Environment.NewLine), line => line.Contains(text, StringComparison.Ordinal)) + 1).ToString(CultureInfo.InvariantCulture);

    [TestMethod, OnDesktops]
    public async Task TheCard_Should_DescribeWhatIsUnderThePointer(WorkbenchSurface surface)
    {
        await Inspect();

        await SectionHeading.HoverAsync();

        var width = await SectionHeading.EvaluateAsync<int>("e => Math.round(e.getBoundingClientRect().width)");
        var height = await SectionHeading.EvaluateAsync<int>("e => Math.round(e.getBoundingClientRect().height)");
        var fontSize = await SectionHeading.EvaluateAsync<string>("e => getComputedStyle(e).fontSize");

        await Expect(Card).ToBeVisibleAsync();
        await Expect(Card).ToContainTextAsync($"{nameof(BitText)} h2", new() { UseInnerText = true });
        await Expect(Card).ToContainTextAsync(FormattableString.Invariant($"{width} × {height}"), new() { UseInnerText = true });
        await Expect(Card).ToContainTextAsync(new Regex(@"padding \d.* · margin \d"), new() { UseInnerText = true });
        await Expect(Card).ToContainTextAsync($"font {fontSize}", new() { UseInnerText = true });
        await Expect(Card).ToContainTextAsync(new Regex("color --bit-clr-"), new() { UseInnerText = true });
        await Expect(View().Locator(".wb-hover")).ToBeVisibleAsync();
        await Expect(View().Locator(".wb-pad")).ToBeAttachedAsync();
    }

    [TestMethod, OnDesktops]
    public async Task TheCard_Should_NameTheThemesColorsInADarkViewToo(WorkbenchSurface surface)
    {
        await Inspect(query: "?theme=dark");

        await SectionHeading.HoverAsync();

        await Expect(Card).ToContainTextAsync(new Regex("color --bit-clr-"), new() { UseInnerText = true });
        await Expect(Card).Not.ToContainTextAsync(WorkbenchStrings.NotFromTheme, new() { UseInnerText = true });
    }

    [TestMethod, OnDesktops]
    public async Task TheCard_Should_MarkAColorOutsideTheThemeAndNameItsBoxAndCorners(WorkbenchSurface surface)
    {
        await Inspect();

        await View().Locator("body").EvaluateAsync("""
            body => {
                const sample = document.createElement('div');
                sample.id = 'sample';
                sample.textContent = 'Sample';
                sample.style.cssText = 'position: fixed; top: 180px; left: 40px; z-index: 99; color: #123456; background-color: #654321; border: 2px solid #abcdef; border-radius: 7px; padding: 3px 4px 5px 6px; margin: 1px';
                body.appendChild(sample);
            }
            """);

        await View().Locator("#sample").HoverAsync();

        await Expect(Card).ToContainTextAsync("padding 3 4 5 6 · margin 1", new() { UseInnerText = true });
        await Expect(Card).ToContainTextAsync($"color rgb(18, 52, 86) · {WorkbenchStrings.NotFromTheme}", new() { UseInnerText = true });
        await Expect(Card).ToContainTextAsync($"background rgb(101, 67, 33) · {WorkbenchStrings.NotFromTheme}", new() { UseInnerText = true });
        await Expect(Card).ToContainTextAsync($"border 2px rgb(171, 205, 239) · {WorkbenchStrings.NotFromTheme}", new() { UseInnerText = true });
        await Expect(Card).ToContainTextAsync("radius 7px", new() { UseInnerText = true });
    }

    [TestMethod, OnDesktops]
    public async Task TheCard_Should_LeaveWithThePointerAndWithTheMode(WorkbenchSurface surface)
    {
        await Inspect();

        await SectionHeading.HoverAsync();
        await Expect(Card).ToBeVisibleAsync();

        await Page.Locator(".brand").HoverAsync();
        await Expect(Card).ToBeHiddenAsync();
        await Expect(View().Locator(".wb-hover")).ToBeHiddenAsync();

        await SectionHeading.HoverAsync();
        await Expect(Card).ToBeVisibleAsync();

        await SetMode(WorkbenchStrings.ModeBrowse, null);
        await Expect(Card).ToBeHiddenAsync();
        await Expect(View().Locator(".wb-hover")).ToBeHiddenAsync();

        await SectionHeading.HoverAsync();
        await Expect(Card).ToBeHiddenAsync();
    }

    [TestMethod, OnDesktops]
    public async Task EditTextMode_Should_OutlineWhatIsUnderThePointerWithoutACard(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");
        await SetMode(WorkbenchStrings.ModeText, "text");

        await SectionHeading.HoverAsync();

        await Expect(View().Locator(".wb-hover")).ToBeVisibleAsync();
        await Expect(Card).ToBeHiddenAsync();
    }

    [TestMethod, OnDesktops]
    public async Task Inspect_Should_AskForAPickAndNameItsTabUntilItEnds(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Expect(Page.Locator("#wb-tab-view")).ToHaveTextAsync(WorkbenchStrings.ViewTab);
        await Expect(Mode(WorkbenchStrings.ModeBrowse)).ToHaveAttributeAsync("aria-pressed", "true");

        await SetMode(WorkbenchStrings.ModeInspect, "inspect");

        await Expect(Page.Locator("#wb-tab-view")).ToHaveTextAsync(WorkbenchStrings.ComponentTab);
        await Expect(Page.Locator("#wb-tab-view")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Panel.GetByText(WorkbenchStrings.ComponentPanelHint)).ToBeVisibleAsync();
        await Expect(Panel.Locator(".parameter")).ToHaveCountAsync(0);

        await Press(IconButton);
        await Expect(Panel.Locator(".parameter").First).ToBeVisibleAsync();
        await Expect(Panel.GetByText(WorkbenchStrings.ComponentPanelHint)).ToHaveCountAsync(0);

        await SetMode(WorkbenchStrings.ModeBrowse, null);
        await Expect(Page.Locator("#wb-tab-view")).ToHaveTextAsync(WorkbenchStrings.ViewTab);
        await Expect(Panel).ToHaveCountAsync(0);
        await Expect(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ThemeDark, Exact = true })).ToBeVisibleAsync();

        await SetMode(WorkbenchStrings.ModeInspect, "inspect");
        await Expect(Panel.GetByText(WorkbenchStrings.ComponentPanelHint)).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task Inspect_Should_OpenItsPanelFromAnyTabAndLetTheOtherTabsWork(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");
        await OpenTab(WorkbenchModes.ChecksColumn);

        await SetMode(WorkbenchStrings.ModeInspect, "inspect");

        await Expect(Page.Locator("#wb-tab-view")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Panel.GetByText(WorkbenchStrings.ComponentPanelHint)).ToBeVisibleAsync();

        await OpenTab(WorkbenchModes.ChecksColumn);
        await Expect(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.RunChecks })).ToBeVisibleAsync();
        await Expect(Panel).ToHaveCountAsync(0);
        await Expect(Mode(WorkbenchStrings.ModeInspect)).ToHaveAttributeAsync("aria-pressed", "true");

        await OpenTab(WorkbenchModes.ViewColumn);
        await Expect(Panel.GetByText(WorkbenchStrings.ComponentPanelHint)).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task APick_Should_NameTheComponentAndItsSourceFile(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var source = Panel.Locator(".source");

        await Expect(source).ToHaveTextAsync($"ThemePage.razor:{LineOf(nameof(WorkbenchStrings.ThemeIconButton))}");
        await Expect(source).ToHaveAttributeAsync("title", ThemePage);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithoutAgent)]
    public async Task APublishedCopy_Should_ShowTheComponentWithoutASourceOrAWayToChangeIt(WorkbenchSurface surface)
    {
        LeaveTheProject();

        await PickTheIconButton();

        await Expect(Panel.Locator(".parameter").First).ToBeVisibleAsync();
        await Expect(Panel.Locator(".source")).ToHaveCountAsync(0);
        await Expect(Page.GetByText(WorkbenchStrings.AgentNotConnected)).ToBeVisibleAsync();
        await Expect(Panel.GetByText(WorkbenchStrings.ComponentPanelNote)).ToHaveCountAsync(0);
        await Expect(Panel.GetByText(WorkbenchStrings.ComponentTagNotFound)).ToHaveCountAsync(0);
        await Expect(Panel.GetByText(WorkbenchStrings.ComponentVariablesHint)).ToHaveCountAsync(0);
        await Expect(Panel.Locator("[role='switch']").First).ToBeDisabledAsync();
        await Expect(Panel.Locator(".parameter input").First).ToBeDisabledAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task DevModeWithoutTheProject_Should_LetNothingChange(WorkbenchSurface surface)
    {
        LeaveTheProject();

        await PickTheIconButton();

        await Expect(Panel.Locator(".source")).ToHaveCountAsync(0);
        await Expect(Panel.GetByText(WorkbenchStrings.ComponentPanelNote)).ToHaveCountAsync(0);
        await Expect(Panel.Locator("[role='switch']").First).ToBeDisabledAsync();
    }

    [TestMethod, OnDesktops]
    public async Task ThePanel_Should_ListWhatTheTagSetsFirstThenTheOtherParametersAndTheInheritedOnes(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var own = Section(WorkbenchStrings.ComponentParameters);
        var inherited = Section(WorkbenchStrings.ComponentInheritedParameters);

        await Expect(own.Locator(".parameter-name").First).ToHaveTextAsync(nameof(BitButton.IconName));

        Assert.AreSequenceEqual(new[] { nameof(BitButton.IconName), nameof(BitButton.IconOnly), nameof(BitButton.Title) }, (await own.Locator(".parameter-name").AllInnerTextsAsync()).Take(3).ToArray());
        await Expect(inherited.Locator(".parameter-name").First).ToHaveTextAsync(nameof(BitComponentBase.AriaLabel));

        await Expect(own.Locator(".parameter-name").GetByText(nameof(BitButton.Rounded), new() { Exact = true })).ToBeVisibleAsync();
        await Expect(inherited.Locator(".parameter-name").GetByText(nameof(BitComponentBase.Style), new() { Exact = true })).ToBeVisibleAsync();
        await Expect(own.Locator(".parameter-name").GetByText(nameof(BitComponentBase.Style), new() { Exact = true })).ToHaveCountAsync(0);

        await Expect(Parameter(nameof(BitButton.Color)).Locator(".parameter-type")).ToHaveTextAsync("BitColor?");
        await Expect(Parameter(nameof(BitButton.IconOnly)).Locator(".parameter-type")).ToHaveTextAsync("Boolean");
        await Expect(Parameter(nameof(BitButton.LoadingDelay)).GetByRole(AriaRole.Textbox)).ToHaveValueAsync("0");
        await Expect(Parameter(nameof(BitButton.LoadingLabelPlacement)).GetByRole(AriaRole.Combobox)).ToContainTextAsync(nameof(BitPlacement.End));
    }

    [TestMethod, OnDesktops]
    public async Task AParameter_Should_CarryItsSummaryFromTheComponentsDocs(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        await Expect(Parameter(nameof(BitButton.IconOnly)).Locator(".parameter-head")).ToHaveAttributeAsync("data-doc", new Regex(@"\w"));
        await Expect(Variable("btn-sbg").Locator(".variable-head")).ToHaveAttributeAsync("data-doc", "--bit-btn-sbg");
    }

    [TestMethod, OnDesktops]
    public async Task AValueCodeSets_Should_ShowItsCodeInsteadOfAField(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var title = Parameter(nameof(BitButton.Title));

        await Expect(title.Locator("code")).ToHaveTextAsync($"@Localizer[nameof(WorkbenchStrings.{nameof(WorkbenchStrings.ThemeIconButton)})]");
        await Expect(title.Locator("code")).ToHaveAttributeAsync("title", WorkbenchStrings.ComponentSetByCode);
        await Expect(title.GetByRole(AriaRole.Textbox)).ToHaveCountAsync(0);
        await Expect(title.GetByRole(AriaRole.Button)).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task EachKindOfParameter_Should_GetItsField(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        await Expect(Parameter(nameof(BitButton.Rounded)).GetByRole(AriaRole.Switch)).ToBeVisibleAsync();
        await Expect(Parameter(nameof(BitButton.IconOnly)).GetByRole(AriaRole.Switch)).ToBeCheckedAsync();
        await Expect(Parameter(nameof(BitButton.Color)).GetByRole(AriaRole.Combobox)).ToContainTextAsync(WorkbenchStrings.ComponentNotSet);
        await Expect(Parameter(nameof(BitButton.IconName)).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.IconPick })).ToBeVisibleAsync();
        await Expect(Parameter(nameof(BitButton.IconName)).GetByRole(AriaRole.Textbox)).ToHaveValueAsync("Settings");
        await Expect(Parameter(nameof(BitButton.Target)).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.SuggestShow })).ToBeVisibleAsync();
        await Expect(Parameter(nameof(BitButton.Href)).GetByRole(AriaRole.Textbox)).ToHaveAttributeAsync("placeholder", WorkbenchStrings.ComponentNotSet);
        await Expect(Parameter(nameof(BitButton.Href)).GetByRole(AriaRole.Button)).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task AColorParameter_Should_GetAColorField(WorkbenchSurface surface)
    {
        await Inspect();

        await Press(View().GetByText(WorkbenchStrings.ThemeProgress).First);

        await Expect(Panel.Locator(".component-name")).ToHaveTextAsync(nameof(BitProgress));
        await Expect(Parameter(nameof(BitProgress.BarColor)).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ColorPick, Exact = true })).ToBeVisibleAsync();
        await Expect(Parameter(nameof(BitProgress.BarColor)).GetByRole(AriaRole.Textbox)).ToHaveAttributeAsync("placeholder", WorkbenchStrings.ComponentNotSet);
    }

    [TestMethod, OnDesktops]
    public async Task TheStylesAndClasses_Should_ListTheirParts(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        await Expect(Section(WorkbenchStrings.ComponentStyles).Locator(".parameter-name")).ToHaveTextAsync(["Classes", "Styles"]);

        foreach (var parameter in new[] { "Classes", "Styles" })
        {
            await Expect(Parameter(parameter).GetByRole(AriaRole.Textbox, new() { Name = nameof(BitButtonClassStyles.Root), Exact = true })).ToBeVisibleAsync();
            await Expect(Parameter(parameter).GetByRole(AriaRole.Textbox, new() { Name = nameof(BitButtonClassStyles.Icon), Exact = true })).ToBeVisibleAsync();
        }
    }

    [TestMethod, OnDesktops]
    public async Task TheCssVariables_Should_ListTheComponentsOwnThenTheThemesWithTheirValues(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var section = Section(WorkbenchStrings.ComponentCssVariables);

        await Expect(section.Locator(".variable-name").First).ToContainTextAsync("btn-");
        await Expect(section.Locator(".group-title")).ToHaveTextAsync(WorkbenchStrings.ComponentThemeVariables);
        await Expect(section.Locator(".group-title ~ .variable .variable-name").Filter(new() { HasTextRegex = new Regex("^clr-") }).First).ToBeVisibleAsync();

        var value = await IconButton.EvaluateAsync<string>("e => getComputedStyle(e).getPropertyValue('--bit-btn-sbg').trim()");

        await Expect(Variable("btn-sbg").GetByRole(AriaRole.Textbox, new() { Name = "--bit-btn-sbg", Exact = true })).ToHaveValueAsync(value);
        await Expect(Variable("btn-sbg").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ColorPick, Exact = true })).ToBeVisibleAsync();
        await Expect(Variable("btn-radius").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.SuggestShow })).ToBeVisibleAsync();
        await Expect(Variable("btn-pad-ntx").GetByRole(AriaRole.Button)).ToHaveCountAsync(0);

        if (HasAgent)
        {
            await Expect(section.GetByText(WorkbenchStrings.ComponentVariablesHint)).ToBeVisibleAsync();
        }
        else
        {
            await Expect(section.GetByText(WorkbenchStrings.ComponentVariablesHint)).ToHaveCountAsync(0);
        }
    }

    [TestMethod, OnDesktops]
    public async Task TheFilter_Should_NarrowEverySectionByName(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var filter = Panel.GetByRole(AriaRole.Searchbox, new() { Name = WorkbenchStrings.ComponentFilter });

        await filter.FillAsync("iconn");

        await Expect(Panel.Locator(".parameter-name")).ToHaveTextAsync([nameof(BitButton.IconName)]);
        await Expect(Panel.GetByText(WorkbenchStrings.ComponentInheritedParameters, new() { Exact = true })).ToHaveCountAsync(0);
        await Expect(Panel.GetByText(WorkbenchStrings.ComponentStyles, new() { Exact = true })).ToHaveCountAsync(0);
        await Expect(Panel.GetByText(WorkbenchStrings.ComponentCssVariables, new() { Exact = true })).ToHaveCountAsync(0);

        await filter.FillAsync("btn-sbg");

        await Expect(Panel.Locator(".parameter-name")).ToHaveCountAsync(0);
        await Expect(Panel.Locator(".variable-name")).ToHaveTextAsync(["btn-sbg"]);

        await filter.FillAsync("Styles");

        await Expect(Panel.Locator(".parameter-name")).ToHaveTextAsync(["Styles"]);
        await Expect(Panel.GetByText(WorkbenchStrings.ComponentStyles, new() { Exact = true })).ToBeVisibleAsync();

        await filter.FillAsync("no such parameter");

        await Expect(Panel.Locator(".parameter-name")).ToHaveCountAsync(0);
        await Expect(Panel.Locator(".variable-name")).ToHaveCountAsync(0);

        await filter.FillAsync(string.Empty);

        await Expect(Panel.GetByText(WorkbenchStrings.ComponentInheritedParameters, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Panel.Locator(".variable-name").First).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task EveryField_Should_BeOpenWithAnAgentAndDisabledWithoutOne(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        ILocator[] fields =
        [
            Parameter(nameof(BitButton.Rounded)).GetByRole(AriaRole.Switch),
            Parameter(nameof(BitButton.Color)).GetByRole(AriaRole.Combobox),
            Parameter(nameof(BitButton.Href)).GetByRole(AriaRole.Textbox),
            Parameter(nameof(BitButton.LoadingDelay)).GetByRole(AriaRole.Textbox),
            Parameter(nameof(BitButton.IconName)).GetByRole(AriaRole.Textbox),
            Parameter(nameof(BitButton.IconName)).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.IconPick }),
            Parameter(nameof(BitButton.Target)).GetByRole(AriaRole.Textbox),
            Parameter(nameof(BitButton.Target)).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.SuggestShow }),
            Parameter("Styles").GetByRole(AriaRole.Textbox, new() { Name = nameof(BitButtonClassStyles.Root), Exact = true }),
            Parameter(nameof(BitComponentBase.Style)).GetByRole(AriaRole.Textbox),
            Variable("btn-sbg").GetByRole(AriaRole.Textbox),
            Variable("btn-sbg").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ColorPick, Exact = true }),
            Variable("btn-radius").GetByRole(AriaRole.Textbox),
            Variable("btn-radius").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.SuggestShow })
        ];

        foreach (var field in fields)
        {
            if (HasAgent)
            {
                await Expect(field).ToBeEnabledAsync();
            }
            else
            {
                await Expect(field).ToBeDisabledAsync();
            }
        }

        if (HasAgent)
        {
            await Expect(Parameter(nameof(BitButton.IconOnly)).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ComponentReset })).ToBeVisibleAsync();
            await Expect(Page.GetByText(WorkbenchStrings.AgentNotConnected)).ToHaveCountAsync(0);
        }
        else
        {
            await Expect(Panel.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ComponentReset })).ToHaveCountAsync(0);
            await Expect(Page.GetByText(WorkbenchStrings.AgentNotConnected)).ToBeVisibleAsync();
        }
    }

    [TestMethod, OnDesktops]
    public async Task TheNote_Should_ShowOnlyWithAnAgentAndStayDismissedAfterAReload(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var note = Panel.GetByText(WorkbenchStrings.ComponentPanelNote);

        if (HasAgent is false)
        {
            await Expect(note).ToHaveCountAsync(0);
            return;
        }

        await Expect(note).ToBeVisibleAsync();

        await Press(Panel.Locator(".note").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Close, Exact = true }));
        await Expect(note).ToHaveCountAsync(0);

        await Page.ReloadAsync();
        await WaitForViews();
        await Expect(Mode(WorkbenchStrings.ModeBrowse)).ToHaveAttributeAsync("aria-pressed", "true");
        await SetMode(WorkbenchStrings.ModeInspect, "inspect");
        await Press(IconButton);

        await Expect(Panel.Locator(".component-name")).ToHaveTextAsync(nameof(BitButton));
        await Expect(note).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task APickOutsideAnyComponent_Should_SaySo(WorkbenchSurface surface)
    {
        await Inspect(query: "?chrome=false");

        await Press(View().Locator(".type-name").First);

        await Expect(Panel.GetByText(WorkbenchStrings.ComponentNotReadable)).ToBeVisibleAsync();
        await Expect(Panel.Locator(".parameter")).ToHaveCountAsync(0);
        await Expect(Panel.Locator(".component-name")).ToHaveCountAsync(0);

        await Press(IconButton);

        await Expect(Panel.Locator(".component-name")).ToHaveTextAsync(nameof(BitButton));
        await Expect(Panel.GetByText(WorkbenchStrings.ComponentNotReadable)).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task APartAComponentDrawsItself_Should_NameThatComponentAndStayReadOnly(WorkbenchSurface surface)
    {
        await Inspect("assistant", "?device=desktop-1536");

        await Press(View().Locator(".model").GetByRole(AriaRole.Combobox));

        await Expect(Panel.Locator(".component-name")).ToHaveTextAsync("BitDropdown");
        await Expect(Panel.GetByText(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ComponentDrawnBy, "BitAiModelSelector"))).ToBeVisibleAsync();
        await Expect(Panel.Locator(".source")).ToHaveCountAsync(0);
        await Expect(Panel.Locator("[role='switch']").First).ToBeDisabledAsync();
        await Expect(Panel.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ComponentReset })).ToHaveCountAsync(0);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task ATagThatNoLongerAddsUp_Should_SaySoAndStayReadOnly(WorkbenchSurface surface)
    {
        var file = InTheCopy(ThemePage);

        File.WriteAllText(file, File.ReadAllText(file) + Environment.NewLine + "<BitButton>A tag the running code doesn't have yet</BitButton>");

        await PickTheIconButton();

        await Expect(Panel.GetByText(WorkbenchStrings.ComponentTagNotFound)).ToBeVisibleAsync();
        await Expect(Section(WorkbenchStrings.ComponentCssVariables).GetByText(WorkbenchStrings.ComponentVariablesReadOnly)).ToBeVisibleAsync();
        await Expect(Panel.Locator(".source")).ToHaveCountAsync(0);
        await Expect(Parameter(nameof(BitButton.Rounded)).GetByRole(AriaRole.Switch)).ToBeDisabledAsync();
        await Expect(Variable("btn-sbg").GetByRole(AriaRole.Textbox)).ToBeDisabledAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task ATagThatDisagreesWithTheView_Should_CountAsAnotherTag(WorkbenchSurface surface)
    {
        AddToTheIconButton("Variant=\"BitVariant.Outline\"");

        await PickTheIconButton();

        await Expect(Panel.GetByText(WorkbenchStrings.ComponentTagNotFound)).ToBeVisibleAsync();
        await Expect(Panel.Locator(".source")).ToHaveCountAsync(0);
        await Expect(Parameter(nameof(BitButton.Rounded)).GetByRole(AriaRole.Switch)).ToBeDisabledAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task StylesCodeSets_Should_ShowTheirCodeInsteadOfTheParts(WorkbenchSurface surface)
    {
        AddToTheIconButton("Styles=\"@customStyles\"");

        await PickTheIconButton();

        await Expect(Parameter("Styles").Locator("code")).ToHaveTextAsync("@customStyles");
        await Expect(Parameter("Styles").GetByRole(AriaRole.Textbox)).ToHaveCountAsync(0);
        await Expect(Parameter("Classes").GetByRole(AriaRole.Textbox).First).ToBeEnabledAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AStyleCodeSets_Should_KeepTheCssVariablesReadOnly(WorkbenchSurface surface)
    {
        AddToTheIconButton("Style=\"@customStyle\"");

        await PickTheIconButton();

        await Expect(Section(WorkbenchStrings.ComponentCssVariables).GetByText(WorkbenchStrings.ComponentVariablesReadOnly)).ToBeVisibleAsync();
        await Expect(Variable("btn-sbg").GetByRole(AriaRole.Textbox)).ToBeDisabledAsync();
        await Expect(Variable("btn-radius").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.SuggestShow })).ToBeDisabledAsync();
        await Expect(Parameter(nameof(BitButton.Rounded)).GetByRole(AriaRole.Switch)).ToBeEnabledAsync();
    }

    [TestMethod, OnDesktops]
    public async Task APress_Should_PickInsteadOfActingOnThePage(WorkbenchSurface surface)
    {
        await Inspect("pos-dashboard", "?device=desktop-1536");

        var customize = View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosCustomize, Exact = true });

        await Press(customize);
        await Expect(Panel.Locator(".component-name")).ToHaveTextAsync(nameof(BitButton));
        await Expect(View().GetByText(WorkbenchStrings.PosEditHint)).ToHaveCountAsync(0);

        await customize.FocusAsync();
        await customize.PressAsync("Enter");
        await customize.PressAsync(" ");
        await Expect(View().GetByText(WorkbenchStrings.PosEditHint)).ToHaveCountAsync(0);

        await SetMode(WorkbenchStrings.ModeBrowse, null);

        await Press(customize);
        await Expect(View().GetByText(WorkbenchStrings.PosEditHint)).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task APickInAView_Should_SelectThatView(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", $"?panes={WorkbenchUrls.EncodePanes([new() { Device = "iphone-se" }, new() { Device = "iphone-se", Theme = WorkbenchThemeMode.Dark }])}");

        var panes = await Page.Locator("iframe[data-pane-id]").EvaluateAllAsync<string[]>("frames => frames.map(f => f.dataset.paneId)");

        await Press(Mode(WorkbenchStrings.ModeInspect));
        await Expect(View(panes[0]).Locator("html")).ToHaveAttributeAsync("data-wb-mode", "inspect");
        await Expect(View(panes[1]).Locator("html")).ToHaveAttributeAsync("data-wb-mode", "inspect");

        await Press(View(panes[1]).GetByLabel(WorkbenchStrings.ThemeIconButton, new() { Exact = true }));

        await Expect(Panel.Locator(".component-name")).ToHaveTextAsync(nameof(BitButton));
        await Expect(Page.Locator(".pane").Nth(1)).ToHaveAttributeAsync("aria-current", "true");
    }

    [TestMethod, OnDesktops]
    public async Task AnotherPage_Should_ForgetThePickedComponentAndKeepTheMode(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        await Press(Page.Locator("#wb-navigator").GetByRole(AriaRole.Link).Filter(new() { HasText = WorkbenchStrings.ShiftsTitle }));

        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/shifts"));
        await WaitForViews();
        await Expect(View().Locator(".shifts")).ToBeVisibleAsync();
        await Expect(Mode(WorkbenchStrings.ModeInspect)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Panel.GetByText(WorkbenchStrings.ComponentPanelHint)).ToBeVisibleAsync();
        await Expect(Panel.Locator(".component-name")).ToHaveCountAsync(0);

        await OpenTab(WorkbenchModes.ReviewColumn);
        await Expect(Page.Locator(".composer")).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task AStoreView_Should_BeInspectedInTheScreenItShows(WorkbenchSurface surface)
    {
        await Inspect("pos-dashboard", StoreView);

        var screen = View().FrameLocator("iframe.screen-frame");
        var customize = screen.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosCustomize, Exact = true });

        await Expect(screen.Locator("html")).ToHaveAttributeAsync("data-wb-mode", "inspect");

        await customize.HoverAsync();
        await Expect(screen.Locator(".wb-info")).ToContainTextAsync(nameof(BitButton), new() { UseInnerText = true });

        await Press(customize);

        await Expect(Panel.Locator(".component-name")).ToHaveTextAsync(nameof(BitButton));
        await Expect(Panel.Locator(".parameter").First).ToBeVisibleAsync();
        await Expect(screen.GetByText(WorkbenchStrings.PosEditHint)).ToHaveCountAsync(0);

        await SetMode(WorkbenchStrings.ModeBrowse, null);
        await Expect(screen.Locator("html")).Not.ToHaveAttributeAsync("data-wb-mode", new Regex("."));

        await Press(customize);
        await Expect(screen.GetByText(WorkbenchStrings.PosEditHint)).ToBeVisibleAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AStoreView_Should_TakeACommentInTheScreenItShows(WorkbenchSurface surface)
    {
        await OpenWorkbench("pos-dashboard", StoreView);
        await SetMode(WorkbenchStrings.ModeComment, "comment");

        var screen = View().FrameLocator("iframe.screen-frame");

        await Expect(screen.Locator("html")).ToHaveAttributeAsync("data-wb-mode", "comment");

        await Press(screen.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosCustomize, Exact = true }));

        await Expect(Page.Locator(".composer")).ToContainTextAsync(nameof(BitButton));
        await Expect(screen.GetByText(WorkbenchStrings.PosEditHint)).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task TheMode_Should_EndWithAReload(WorkbenchSurface surface)
    {
        await Inspect();

        await Page.ReloadAsync();
        await WaitForViews();

        await Expect(Mode(WorkbenchStrings.ModeBrowse)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("data-wb-mode", new Regex("."));
        await Expect(Panel).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task TheReviewTab_Should_StartACommentOnThePickedComponent(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        await OpenTab(WorkbenchModes.ReviewColumn);

        var composer = Page.Locator(".composer");

        await Expect(composer).ToContainTextAsync(nameof(BitButton));

        var text = $"Make it rounded {Guid.NewGuid():N}";

        await composer.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.NewComment }).FillAsync(text);
        await Press(composer.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Send, Exact = true }));

        var threads = await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Any(t => t.Messages[0].Text == text));
        var thread = threads.Single(t => t.Messages[0].Text == text);

        Assert.AreEqual("theme", thread.PageKey);
        Assert.AreEqual(nameof(BitButton), thread.Anchor.Component);
        await Expect(composer).ToHaveCountAsync(0);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task ACancelledComment_Should_NotStartAgainUntilAnotherPick(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        await OpenTab(WorkbenchModes.ReviewColumn);

        var composer = Page.Locator(".composer");

        await Expect(composer).ToBeVisibleAsync();
        await Press(composer.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Cancel, Exact = true }));
        await Expect(composer).ToHaveCountAsync(0);

        await OpenTab(WorkbenchModes.ViewColumn);
        await Expect(Panel.Locator(".component-name")).ToHaveTextAsync(nameof(BitButton));

        await OpenTab(WorkbenchModes.ReviewColumn);
        await Expect(Tools.GetByText(WorkbenchStrings.PageStatus, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(composer).ToHaveCountAsync(0);

        await OpenTab(WorkbenchModes.ViewColumn);
        await Press(SectionHeading);
        await Expect(Panel.Locator(".component-name")).ToHaveTextAsync(nameof(BitText));

        await OpenTab(WorkbenchModes.ReviewColumn);
        await Expect(composer).ToContainTextAsync(nameof(BitText));
    }
}
