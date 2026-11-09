namespace Boilerplate.WorkbenchTests.Features.Inspect;

/// <summary>A visitor through a tunnel on a copy in dev mode, with the project on the machine: Inspect and Edit text let them look and change nothing, and what writes the files refuses them too.</summary>
[TestClass, TestCategory("UITest")]
public class TunnelVisitorUITests : WorkbenchCodeCopyPageTest
{
    private const string ThemePage = "Workbench/Components/Pages/Showcases/Theme/ThemePage.razor";

    /// <summary>The file the theme page's icon button is written in, on its Buttons tab.</summary>
    private const string ButtonsSection = "Workbench/Components/Pages/Showcases/Theme/Sections/ButtonsSection.razor";

    private const string Texts = "Workbench/Resources/WorkbenchStrings.resx";

    private ILocator ComponentPanel => Page.Locator(".component-panel");

    private ILocator TextPanel => Page.Locator(".text-panel");

    private ILocator IconButton => View().GetByLabel(WorkbenchStrings.ThemeIconButton, new() { Exact = true });

    private ILocator Mode(string name) => Page.Locator(".modes").GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private ILocator Parameter(string name) => ComponentPanel.Locator(".parameter").Filter(new() { Has = Page.Locator(".parameter-name", new() { HasTextRegex = new Regex($"^{name}$") }) });

    private ILocator Variable(string shortName) => ComponentPanel.Locator(".variable").Filter(new() { Has = Page.Locator(".variable-name", new() { HasTextRegex = new Regex($"^{shortName}$") }) });

    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();

        options.ExtraHTTPHeaders = new Dictionary<string, string> { ["X-Forwarded-For"] = "203.0.113.7" };

        return options;
    }

    /// <summary>Opens a page in a mode, on a phone turned sideways, as an upright one has no modes to pick.</summary>
    private async Task OpenInMode(string pageKey, string mode, string frameMode, string query = "")
    {
        await OpenWorkbench(pageKey, query);

        if (IsPhone)
        {
            var size = Page.ViewportSize!;

            await Page.SetViewportSizeAsync(size.Height, size.Width);
            await WaitForViews();
        }

        await Press(Mode(mode));

        await Expect(Mode(mode)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(View().Locator("html")).ToHaveAttributeAsync("data-wb-mode", frameMode);
    }

    private async Task PickTheIconButton()
    {
        await OpenInMode("theme", WorkbenchStrings.ModeInspect, "inspect", "?sw-section=buttons");

        await Press(IconButton);

        await Expect(ComponentPanel.Locator(".component-name")).ToHaveTextAsync(nameof(BitButton));
    }

    /// <summary>One of the services that change the files, as a request of this kind reaches it.</summary>
    private static T Edits<T>(bool isLocal, bool isDevMode = true) where T : class
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection([new("DOTNET_WATCH", isDevMode ? "1" : "0")]).Build());
        services.AddSingleton<IHttpContextAccessor>(new FixedHttpContextAccessor(isLocal));
        services.AddSingleton<T>();

        return services.BuildServiceProvider().GetRequiredService<T>();
    }

    [TestMethod, WithAnAgent]
    public async Task Inspect_Should_ShowEveryFieldDisabled(WorkbenchSurface surface)
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
            await Expect(field).ToBeDisabledAsync();
        }

        await Expect(ComponentPanel.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ComponentReset })).ToHaveCountAsync(0);
        await Expect(ComponentPanel.GetByText(WorkbenchStrings.ComponentPanelNote)).ToHaveCountAsync(0);
        await Expect(Page.GetByText(WorkbenchStrings.AgentNotConnected)).ToBeVisibleAsync();
    }

    [TestMethod, WithAnAgent]
    public async Task AFieldSwitchedBackOnInTheBrowser_Should_ChangeNoCode(WorkbenchSurface surface)
    {
        await PickTheIconButton();

        var before = ReadTheCopy(ButtonsSection);
        var field = Parameter(nameof(BitButton.Href)).GetByRole(AriaRole.Textbox);

        await field.EvaluateAsync("e => { e.removeAttribute('disabled'); e.removeAttribute('aria-disabled'); }");
        await field.FillAsync("https://example.com");
        await field.BlurAsync();

        // The panel answers its filter only after the field's change, whatever became of it.
        await ComponentPanel.GetByRole(AriaRole.Searchbox, new() { Name = WorkbenchStrings.ComponentFilter }).FillAsync(nameof(BitButton.Href));
        await Expect(ComponentPanel.Locator(".parameter-name")).ToHaveTextAsync([nameof(BitButton.Href)]);

        await Expect(ComponentPanel.Locator(".status")).ToHaveCountAsync(0);

        Assert.AreEqual(before, ReadTheCopy(ButtonsSection));
    }

    [TestMethod, WithAnAgent]
    public async Task EditText_Should_ShowEveryLanguageWithNoSaveAndNoAsk(WorkbenchSurface surface)
    {
        await OpenInMode("shifts", WorkbenchStrings.ModeText, "text");

        await Press(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading, Exact = true }));

        await Expect(TextPanel.Locator(".key")).ToHaveTextAsync($"{nameof(WorkbenchStrings)} · {nameof(WorkbenchStrings.ShiftsHeading)}");

        foreach (var (displayName, culture) in CultureInfoManager.SupportedCultures)
        {
            var field = TextPanel.GetByLabel(displayName, new() { Exact = true });

            await Expect(field).ToHaveValueAsync(InCulture(culture.Name, nameof(WorkbenchStrings.ShiftsHeading)));
            await Expect(field).Not.ToBeEditableAsync();
        }

        await Expect(TextPanel.GetByRole(AriaRole.Button)).ToHaveCountAsync(0);
        await Expect(TextPanel.GetByText(WorkbenchStrings.SaveTranslationsHint)).ToHaveCountAsync(0);
        await Expect(Page.GetByText(WorkbenchStrings.AgentNotConnected)).ToBeVisibleAsync();
    }

    [TestMethod, WithAnAgent]
    public async Task ATextNotInTheTranslationFiles_Should_OfferNoAsk(WorkbenchSurface surface)
    {
        await OpenInMode("theme", WorkbenchStrings.ModeText, "text", "?sw-section=typography");

        await Press(View().Locator(".type-name").First);

        await Expect(TextPanel.GetByText(WorkbenchStrings.TextNotTranslated)).ToBeVisibleAsync();
        await Expect(TextPanel.GetByRole(AriaRole.Button)).ToHaveCountAsync(0);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task SavingATextWithThePagesGateBypassed_Should_BeRefusedExceptFromThisMachineInDevMode(WorkbenchSurface surface)
    {
        var (culture, before) = (CultureInfoManager.DefaultCulture.Name, ReadTheCopy(Texts));

        Task<bool> Save(bool isLocal, bool isDevMode) => Edits<WorkbenchTextEdits>(isLocal, isDevMode).Save(nameof(WorkbenchStrings), nameof(WorkbenchStrings.Save), culture, "Store");

        Assert.IsFalse(await Save(isLocal: false, isDevMode: true), "A request through a tunnel saved a text.");
        Assert.AreEqual(before, ReadTheCopy(Texts));

        Assert.IsFalse(await Save(isLocal: true, isDevMode: false), "A copy outside dev mode saved a text.");
        Assert.AreEqual(before, ReadTheCopy(Texts));

        Assert.IsTrue(await Save(isLocal: true, isDevMode: true));
        Assert.AreNotEqual(before, ReadTheCopy(Texts));
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task ChangingCodeWithThePagesGateBypassed_Should_BeRefusedExceptFromThisMachineInDevMode(WorkbenchSurface surface)
    {
        var before = ReadTheCopy(ThemePage);

        Task<bool> Set(bool isLocal, bool isDevMode) => Edits<WorkbenchComponentEdits>(isLocal, isDevMode).Set(nameof(BitButton), new(ThemePage, 0, 1), nameof(BitButton.Rounded), "true", null);

        Assert.IsFalse(await Set(isLocal: false, isDevMode: true), "A request through a tunnel changed the code.");
        Assert.AreEqual(before, ReadTheCopy(ThemePage));

        Assert.IsFalse(await Set(isLocal: true, isDevMode: false), "A copy outside dev mode changed the code.");
        Assert.AreEqual(before, ReadTheCopy(ThemePage));

        Assert.IsTrue(await Set(isLocal: true, isDevMode: true));
        Assert.AreNotEqual(before, ReadTheCopy(ThemePage));
    }
}
