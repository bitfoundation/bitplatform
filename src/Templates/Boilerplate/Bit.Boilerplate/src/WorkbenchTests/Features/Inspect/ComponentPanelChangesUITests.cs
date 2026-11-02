namespace Boilerplate.WorkbenchTests.Features.Inspect;

/// <summary>
/// Dev mode's component panel: each kind of field writes its change into the component's tag, here in a copy of the code. These
/// tests have no hot reload, so what reaches the views is left out; <see cref="HotReload"/> stands in where the panel waits for it.
/// </summary>
[TestClass, TestCategory("UITest")]
public class ComponentPanelChangesUITests : WorkbenchCodeCopyPageTest
{
    private const string ThemePage = "Workbench/Components/Pages/Showcases/Theme/ThemePage.razor";

    private ILocator Panel => Page.Locator(".component-panel");

    private ILocator Status => Panel.Locator(".status");

    private ILocator IconButton => View().GetByLabel(WorkbenchStrings.ThemeIconButton, new() { Exact = true });

    private ILocator ProgressLabel => View().GetByText(WorkbenchStrings.ThemeProgress).First;

    private ILocator Parameter(string name) => Panel.Locator(".parameter").Filter(new() { Has = Page.Locator(".parameter-name", new() { HasTextRegex = new Regex($"^{name}$") }) });

    private ILocator Variable(string shortName) => Panel.Locator(".variable").Filter(new() { Has = Page.Locator(".variable-name", new() { HasTextRegex = new Regex($"^{shortName}$") }) });

    /// <summary>What dotnet watch calls once it has applied a change.</summary>
    private static void HotReload() => WorkbenchComponentSources.UpdateApplication(null);

    /// <summary>The tag in the copy that holds the text.</summary>
    private static string TagHolding(string text)
    {
        var markup = ReadTheCopy(ThemePage);
        var at = markup.IndexOf(text, StringComparison.Ordinal);

        // A file caught in the middle of a write may not hold the tag yet.
        return at < 0 ? string.Empty : markup[markup.LastIndexOf('<', at)..(markup.IndexOf("/>", at, StringComparison.Ordinal) + 2)];
    }

    private static Task<string> IconButtonTag(Func<string, bool> holds) => WorkbenchTestReviewer.WaitFor(() => Task.FromResult(TagHolding(nameof(WorkbenchStrings.ThemeIconButton))), holds);

    private static Task<string> ProgressTag(Func<string, bool> holds) => WorkbenchTestReviewer.WaitFor(() => Task.FromResult(TagHolding(nameof(WorkbenchStrings.ThemeProgress))), holds);

    private static Func<string, bool> Has(string text) => tag => tag.Contains(text, StringComparison.Ordinal);

    private async Task Pick(ILocator target, string component)
    {
        await OpenWorkbench("theme");

        await Press(Page.Locator(".modes").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ModeInspect, Exact = true }));
        await Expect(View().Locator("html")).ToHaveAttributeAsync("data-wb-mode", "inspect");

        await Press(target);
        await Expect(Panel.Locator(".component-name")).ToHaveTextAsync(component);
    }

    private Task PickTheIconButton() => Pick(IconButton, nameof(BitButton));

    private async Task Type(ILocator field, string text)
    {
        await Expect(field).ToBeEnabledAsync();
        await field.FillAsync(text);
        await field.BlurAsync();
    }

    private Task ExpectSaved() => Expect(Status).ToHaveTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ComponentSavedHotReload, "ThemePage.razor"));

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AToggle_Should_AddItsAttributeThenWriteFalse(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var rounded = Parameter(nameof(BitButton.Rounded)).GetByRole(AriaRole.Switch);

        await Press(rounded);

        await IconButtonTag(Has(" Rounded"));
        await ExpectSaved();
        await Expect(rounded).ToBeCheckedAsync();

        await Expect(rounded).ToBeEnabledAsync();
        await Press(rounded);

        await IconButtonTag(Has("Rounded=\"false\""));
        await Expect(rounded).Not.ToBeCheckedAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task ADropdown_Should_WriteTheEnumValue(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var color = Parameter(nameof(BitButton.Color)).GetByRole(AriaRole.Combobox);

        await Press(color);
        await Press(Page.GetByRole(AriaRole.Option, new() { Name = nameof(BitColor.Warning), Exact = true }));

        await IconButtonTag(Has("Color=\"BitColor.Warning\""));
        await ExpectSaved();
        await Expect(color).ToContainTextAsync(nameof(BitColor.Warning));
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task NotSetInADropdown_Should_TakeTheAttributeAway(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var color = Parameter(nameof(BitButton.Color)).GetByRole(AriaRole.Combobox);

        await Press(color);
        await Press(Page.GetByRole(AriaRole.Option, new() { Name = nameof(BitColor.Warning), Exact = true }));
        await IconButtonTag(Has("Color=\"BitColor.Warning\""));
        await Expect(color).ToBeEnabledAsync();

        await Press(color);
        await Press(Page.GetByRole(AriaRole.Option, new() { Name = WorkbenchStrings.ComponentNotSet, Exact = true }));

        await IconButtonTag(Has("Color=\"null\""));

        HotReload();

        await IconButtonTag(tag => tag.Contains("Color=", StringComparison.Ordinal) is false);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task ATextField_Should_WriteTheTextEscapedAsNeeded(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var href = Parameter(nameof(BitButton.Href)).GetByRole(AriaRole.Textbox);

        await Type(href, "https://bitplatform.dev/");

        await IconButtonTag(Has("Href=\"https://bitplatform.dev/\""));
        await ExpectSaved();
        await Expect(href).ToHaveValueAsync("https://bitplatform.dev/");
        await Expect(Parameter(nameof(BitButton.Href)).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ComponentReset })).ToBeVisibleAsync();

        await Type(Parameter(nameof(BitButton.SecondaryText)).GetByRole(AriaRole.Textbox), "Say \"hi\"");

        await IconButtonTag(Has("SecondaryText=\"@(\"Say \\\"hi\\\"\")\""));
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task ANumberField_Should_WriteANumberAndRefuseAnythingElse(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var delay = Parameter(nameof(BitButton.LoadingDelay)).GetByRole(AriaRole.Textbox);

        await Type(delay, "soon");

        await Expect(Page.GetByText(WorkbenchStrings.ComponentSaveFailed).First).ToBeVisibleAsync();
        Assert.DoesNotContain(nameof(BitButton.LoadingDelay), TagHolding(nameof(WorkbenchStrings.ThemeIconButton)));
        await Expect(delay).ToHaveValueAsync("0");

        await Type(delay, "20");

        await IconButtonTag(Has("LoadingDelay=\"20\""));
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task TheIconPicker_Should_SearchAndWriteTheIconPicked(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var row = Parameter(nameof(BitButton.IconName));
        var picker = row.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.IconPick });
        var icons = row.GetByRole(AriaRole.Listbox, new() { Name = WorkbenchStrings.IconPick });

        await Expect(picker).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(row.Locator(".preview i")).ToHaveClassAsync(new Regex("bit-icon--Settings"));

        await Press(picker);

        await Expect(picker).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(icons.GetByRole(AriaRole.Option)).ToHaveCountAsync(120);
        await Expect(row.GetByText(new Regex(Regex.Escape(WorkbenchStrings.IconMore).Replace(@"\{0}", @"\d+")))).ToBeVisibleAsync();

        await row.GetByRole(AriaRole.Searchbox, new() { Name = WorkbenchStrings.IconSearch }).FillAsync("trash");

        await Expect(icons.GetByRole(AriaRole.Option, new() { Name = "Delete", Exact = true })).ToBeVisibleAsync();

        await Press(icons.GetByRole(AriaRole.Option, new() { Name = "Delete", Exact = true }));

        await IconButtonTag(Has("IconName=\"@BitIconName.Delete\""));
        await ExpectSaved();
        await Expect(icons).ToHaveCountAsync(0);
        await Expect(picker).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(row.GetByRole(AriaRole.Textbox)).ToHaveValueAsync("Delete");
        await Expect(row.Locator(".preview i")).ToHaveClassAsync(new Regex("bit-icon--Delete"));
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task TheIconPicker_Should_CloseWithItsButtonAndMarkTheCurrentIcon(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var row = Parameter(nameof(BitButton.IconName));
        var picker = row.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.IconPick });

        await Press(picker);
        await row.GetByRole(AriaRole.Searchbox, new() { Name = WorkbenchStrings.IconSearch }).FillAsync("settings");

        await Expect(row.GetByRole(AriaRole.Option, new() { Name = "Settings", Exact = true })).ToHaveAttributeAsync("aria-selected", "true");

        await Press(picker);

        await Expect(row.GetByRole(AriaRole.Listbox)).ToHaveCountAsync(0);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AnIconNameTyped_Should_BeWrittenToo(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        await Type(Parameter(nameof(BitButton.IconName)).GetByRole(AriaRole.Textbox), "RecycleBin");

        await IconButtonTag(Has("IconName=\"@BitIconName.RecycleBin\""));
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task TheSuggestions_Should_OfferTheValuesOfTheirKindAndTakeAnyOther(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var row = Parameter(nameof(BitButton.Target));
        var toggle = row.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.SuggestShow });

        await Press(toggle);

        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(row.GetByRole(AriaRole.Listbox, new() { Name = nameof(BitButton.Target) }).GetByRole(AriaRole.Option)).ToHaveTextAsync(WorkbenchValueSuggestions.Of(nameof(BitButton.Target)));

        await Press(row.GetByRole(AriaRole.Option, new() { Name = "_blank", Exact = true }));

        await IconButtonTag(Has("Target=\"_blank\""));
        await ExpectSaved();
        await Expect(row.GetByRole(AriaRole.Listbox)).ToHaveCountAsync(0);
        await Expect(row.GetByRole(AriaRole.Textbox)).ToHaveValueAsync("_blank");

        await Press(toggle);
        await Expect(row.GetByRole(AriaRole.Option, new() { Name = "_blank", Exact = true })).ToHaveAttributeAsync("aria-selected", "true");
        await Press(toggle);
        await Expect(row.GetByRole(AriaRole.Listbox)).ToHaveCountAsync(0);

        await Type(row.GetByRole(AriaRole.Textbox), "preview");

        await IconButtonTag(Has("Target=\"preview\""));
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AColorParameter_Should_TakeATypedColorAndShowIt(WorkbenchSurface surface)
    {
        await Pick(ProgressLabel, nameof(BitProgress));

        var row = Parameter(nameof(BitProgress.BarColor));

        await Expect(row.Locator(".swatch")).ToHaveClassAsync(new Regex("empty"));

        await Type(row.GetByRole(AriaRole.Textbox), "#ff0000");

        await ProgressTag(Has("BarColor=\"#ff0000\""));
        await Expect(row.Locator(".swatch")).Not.ToHaveClassAsync(new Regex("empty"));
        await Expect(row.Locator(".swatch")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task TheColorPicker_Should_WriteItsColorWhenADragEnds(WorkbenchSurface surface)
    {
        await Pick(ProgressLabel, nameof(BitProgress));

        var row = Parameter(nameof(BitProgress.BarColor));
        var pick = row.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ColorPick, Exact = true });

        await Expect(pick).ToHaveAttributeAsync("aria-expanded", "false");
        await Press(pick);
        await Expect(pick).ToHaveAttributeAsync("aria-expanded", "true");

        await row.Locator(".picker [role='slider']").First.ClickAsync(new() { Position = new() { X = 60, Y = 20 } });

        var tag = await ProgressTag(Has("BarColor=\"#"));
        var color = Regex.Match(tag, "BarColor=\"(#[0-9a-fA-F]+)\"").Groups[1].Value;

        await Expect(row.GetByRole(AriaRole.Textbox)).ToHaveValueAsync(color);
        await Expect(row.Locator(".swatch")).Not.ToHaveClassAsync(new Regex("empty"));

        await Press(pick);
        await Expect(row.Locator(".picker")).ToHaveCountAsync(0);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task HotReload_Should_ShowThePartsTheTagSetsAndBackToDefaultTakeThemAway(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var styles = Parameter("Styles");
        var root = styles.GetByRole(AriaRole.Textbox, new() { Name = nameof(BitButtonClassStyles.Root), Exact = true });
        var reset = styles.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ComponentReset });

        await Expect(reset).ToHaveCountAsync(0);

        await Type(root, "color: red");
        await IconButtonTag(Has("Root = \"color: red\""));
        await ExpectSaved();

        HotReload();

        await Expect(reset).ToBeVisibleAsync();
        await Expect(root).ToHaveValueAsync("color: red");

        await Press(reset);

        await IconButtonTag(Has("Styles=\"null\""));

        HotReload();

        await IconButtonTag(tag => tag.Contains("Styles", StringComparison.Ordinal) is false);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AColorVariable_Should_GoIntoTheStyleAndShowItsColor(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        await Type(Variable("btn-sbg").GetByRole(AriaRole.Textbox), "#00ff00");

        await IconButtonTag(Has("Style=\"--bit-btn-sbg: #00ff00;\""));
        await ExpectSaved();
        await Expect(Variable("btn-sbg").Locator(".swatch")).ToHaveCSSAsync("background-color", "rgb(0, 255, 0)");
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task ASuggestedVariable_Should_GoIntoTheStyle(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var variable = Variable("btn-radius");

        await Press(variable.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.SuggestShow }));
        await Press(variable.GetByRole(AriaRole.Option, new() { Name = "0.5rem", Exact = true }));

        await IconButtonTag(Has("--bit-btn-radius: 0.5rem;"));
        await Expect(variable.GetByRole(AriaRole.Textbox)).ToHaveValueAsync("0.5rem");
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task APlainVariable_Should_GoIntoTheStyle(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        await Type(Variable("btn-pad-ntx").GetByRole(AriaRole.Textbox), "3px");

        await IconButtonTag(Has("--bit-btn-pad-ntx: 3px;"));
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task BackToDefault_Should_WriteTheDefaultForHotReloadThenTakeTheAttributeAway(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var row = Parameter(nameof(BitButton.IconOnly));
        var other = Parameter(nameof(BitButton.Rounded)).GetByRole(AriaRole.Switch);

        await Press(row.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ComponentReset }));

        await IconButtonTag(Has("IconOnly=\"false\""));
        await Expect(other).ToBeDisabledAsync();

        HotReload();

        await IconButtonTag(tag => tag.Contains("IconOnly", StringComparison.Ordinal) is false);
        await Expect(other).ToBeEnabledAsync();
        await Expect(row.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ComponentReset })).ToHaveCountAsync(0);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task HotReload_Should_ReadTheComponentAgainFromItsTag(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var names = Panel.Locator("section").Filter(new() { Has = Page.GetByText(WorkbenchStrings.ComponentParameters, new() { Exact = true }) }).Locator(".parameter-name");

        await Press(Parameter(nameof(BitButton.Rounded)).GetByRole(AriaRole.Switch));
        await IconButtonTag(Has(" Rounded"));
        await ExpectSaved();

        await Expect(names.Nth(2)).ToHaveTextAsync(nameof(BitButton.Title));

        HotReload();

        await Expect(names.Nth(2)).ToHaveTextAsync(nameof(BitButton.Rounded));
        await Expect(Panel.Locator(".source")).ToBeVisibleAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AFileThatIsGone_Should_SayTheChangeWasNotSavedAndPutTheFieldBack(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var rounded = Parameter(nameof(BitButton.Rounded)).GetByRole(AriaRole.Switch);

        File.Delete(InTheCopy(ThemePage));

        await Press(rounded);

        await Expect(Page.GetByText(WorkbenchStrings.ComponentSaveFailed).First).ToBeVisibleAsync();
        await Expect(Status).ToHaveCountAsync(0);
        await Expect(rounded).Not.ToBeCheckedAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AnotherPick_Should_ClearWhatWasSaidAboutTheLastChange(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        await Press(Parameter(nameof(BitButton.Rounded)).GetByRole(AriaRole.Switch));
        await ExpectSaved();

        await Press(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeButtons, Exact = true }));

        await Expect(Panel.Locator(".component-name")).ToHaveTextAsync(nameof(BitText));
        await Expect(Status).ToHaveCountAsync(0);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AChange_Should_LandInTheCopyAndLeaveTheProjectsFileAlone(WorkbenchSurface surface)
    {
        var projectFile = Path.Combine(ProjectSource, ThemePage.Replace('/', Path.DirectorySeparatorChar));
        var before = File.ReadAllText(projectFile);

        await PickTheIconButton();

        await Press(Parameter(nameof(BitButton.Rounded)).GetByRole(AriaRole.Switch));

        await IconButtonTag(Has(" Rounded"));
        Assert.AreEqual(before, File.ReadAllText(projectFile));
    }
}
