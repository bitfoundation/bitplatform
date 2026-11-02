using Boilerplate.WorkbenchTests.Features.Inspect;

namespace Boilerplate.WorkbenchTests.Features.EditText;

/// <summary>Edit text mode: a click on a text traces it to its entry in the translation files, in every language, to save it or send it to the agent.</summary>
[TestClass, TestCategory("UITest")]
public class EditTextUITests : WorkbenchCodeCopyPageTest
{
    private const string WorkbenchTexts = "Workbench/Resources/WorkbenchStrings.resx";

    private static readonly string defaultLanguage = CultureInfoManager.SupportedCultures[0].DisplayName;

    private ILocator Panel => Page.Locator(".text-panel");

    private ILocator Key => Panel.Locator(".key");

    private ILocator SaveButton => Panel.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.SaveTranslations, Exact = true });

    private ILocator AskButton => Panel.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AskTheAgent, Exact = true });

    private ILocator ModeButton(string name) => Page.Locator(".modes").GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private ILocator Field(string language) => Panel.GetByLabel(language, new() { Exact = true });

    private ILocator Heading(string text) => View().GetByRole(AriaRole.Heading, new() { Name = text, Exact = true });

    private static string Language(string culture) => CultureInfoManager.SupportedCultures.Single(supported => supported.Culture.Name == culture).DisplayName;

    private static string Quoted(string text) => $"“{text}”";

    private static string Entry(string path, string key) => Regex.Match(ReadTheCopy(path), $"<data name=\"{key}\"[^>]*>\\s*<value>(.*?)</value>", RegexOptions.Singleline).Groups[1].Value;

    private async Task EditText(string pageKey = "theme", string query = "")
    {
        await OpenWorkbench(pageKey, query);

        await Press(ModeButton(WorkbenchStrings.ModeText));
        await Expect(View().Locator("html")).ToHaveAttributeAsync("data-wb-mode", "text");
    }

    private async Task Pick(ILocator text, string expected)
    {
        await Press(text);
        await Expect(Panel.Locator(".picked")).ToHaveTextAsync(Quoted(expected));
    }

    [TestMethod, OnDesktops]
    public async Task EditText_Should_AskForATextInTheSideColumn(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Press(Button(WorkbenchStrings.Tools));
        await Expect(Page.Locator(".workbench")).ToHaveClassAsync(new Regex("inspector-collapsed"));

        await Press(ModeButton(WorkbenchStrings.ModeText));

        await Expect(ModeButton(WorkbenchStrings.ModeText)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator(".workbench")).Not.ToHaveClassAsync(new Regex("inspector-collapsed"));
        await Expect(Panel.GetByText(WorkbenchStrings.EditTextHint)).ToBeVisibleAsync();
        await Expect(Panel.GetByRole(AriaRole.Textbox)).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task EditText_Should_TakeTheColumnOverFromAnyTab(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");
        await OpenTab(WorkbenchModes.ChecksColumn);

        await Press(ModeButton(WorkbenchStrings.ModeText));

        await Expect(Panel.GetByText(WorkbenchStrings.EditTextHint)).ToBeVisibleAsync();
        await Expect(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.RunChecks })).ToHaveCountAsync(0);

        await Press(ModeButton(WorkbenchStrings.ModeBrowse));

        await Expect(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.RunChecks })).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task ATabPressed_Should_ShowThatTabAndLeaveEditText(WorkbenchSurface surface)
    {
        await EditText();

        await OpenTab(WorkbenchModes.ChecksColumn);

        await Expect(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.RunChecks })).ToBeVisibleAsync();
        await Expect(Panel).ToHaveCountAsync(0);
        await Expect(ModeButton(WorkbenchStrings.ModeBrowse)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("data-wb-mode", new Regex("."));
    }

    [TestMethod, OnDesktops]
    [TestCategory("Localization")]
    public async Task APickedText_Should_ShowItsEntryInEveryLanguage(WorkbenchSurface surface)
    {
        await EditText("shifts");

        await Pick(Heading(WorkbenchStrings.ShiftsHeading), WorkbenchStrings.ShiftsHeading);

        await Expect(Key).ToHaveTextAsync($"{nameof(WorkbenchStrings)} · {nameof(WorkbenchStrings.ShiftsHeading)}");
        await Expect(Panel.GetByRole(AriaRole.Combobox)).ToHaveCountAsync(0);

        foreach (var (displayName, culture) in CultureInfoManager.SupportedCultures)
        {
            await Expect(Field(displayName)).ToHaveValueAsync(InCulture(culture.Name, nameof(WorkbenchStrings.ShiftsHeading)));
        }
    }

    [TestMethod, OnDesktops]
    [TestCategory("Localization")]
    public async Task TheViewsLanguage_Should_ComeFirstAndEachLanguageRunItsOwnWay(WorkbenchSurface surface)
    {
        var heading = InCulture("fa-IR", nameof(WorkbenchStrings.ShiftsHeading));

        await EditText("shifts", "?culture=fa-IR");

        await Pick(Heading(heading), heading);

        await Expect(Panel.GetByRole(AriaRole.Textbox).First).ToHaveAccessibleNameAsync(Language("fa-IR"));
        await Expect(Field(Language("fa-IR"))).ToHaveValueAsync(heading);

        Assert.AreEqual("rtl", await Field(Language("fa-IR")).EvaluateAsync<string>("e => getComputedStyle(e).direction"));
        Assert.AreEqual("ltr", await Field(defaultLanguage).EvaluateAsync<string>("e => getComputedStyle(e).direction"));
    }

    [TestMethod, OnDesktops]
    public async Task ALongText_Should_GetAFieldOfSeveralLines(WorkbenchSurface surface)
    {
        Assert.IsGreaterThan(60, WorkbenchStrings.ThemeSubtitle.Length);

        await EditText();

        await Pick(View().GetByText(WorkbenchStrings.ThemeSubtitle), WorkbenchStrings.ThemeSubtitle);

        Assert.AreEqual("TEXTAREA", await Field(defaultLanguage).EvaluateAsync<string>("e => e.tagName"));
    }

    [TestMethod, OnDesktops]
    public async Task ATextOfSeveralEntries_Should_OfferAChoiceOfThem(WorkbenchSurface surface)
    {
        await EditText();

        await Pick(Heading(WorkbenchStrings.ThemeHeading).First, WorkbenchStrings.ThemeHeading);

        var choice = Panel.GetByRole(AriaRole.Combobox);

        await Expect(choice).ToHaveAccessibleNameAsync(new Regex($"^{WorkbenchStrings.TextKey}"));
        await Expect(Key).ToHaveTextAsync(new Regex($"^{nameof(WorkbenchStrings)} · (?:{nameof(WorkbenchStrings.ThemeHeading)}|{nameof(WorkbenchStrings.ThemePageTitle)})$"));

        var other = (await Key.InnerTextAsync()).EndsWith(nameof(WorkbenchStrings.ThemeHeading), StringComparison.Ordinal) ? nameof(WorkbenchStrings.ThemePageTitle) : nameof(WorkbenchStrings.ThemeHeading);

        await Press(choice);
        await Press(Page.GetByRole(AriaRole.Option, new() { Name = $"{other} · {WorkbenchStrings.ThemeHeading}", Exact = true }));

        await Expect(Key).ToHaveTextAsync($"{nameof(WorkbenchStrings)} · {other}");
    }

    [TestMethod, OnDesktops]
    public async Task AFormattedText_Should_LeadToItsEntryWithThePlaceholders(WorkbenchSurface surface)
    {
        var shown = string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ThemeMessage, nameof(BitColor.Warning));

        await EditText();

        await Pick(View().GetByText(shown).First, shown);

        await Expect(Key).ToHaveTextAsync($"{nameof(WorkbenchStrings)} · {nameof(WorkbenchStrings.ThemeMessage)}");
        await Expect(Field(defaultLanguage)).ToHaveValueAsync(WorkbenchStrings.ThemeMessage);
    }

    [TestMethod, OnDesktops]
    public async Task APlaceholderOrAButtonsName_Should_BePickedToo(WorkbenchSurface surface)
    {
        await EditText();

        await Pick(View().GetByRole(AriaRole.Searchbox, new() { Name = WorkbenchStrings.ThemeSearch }).First, WorkbenchStrings.ThemeSearch);

        await Pick(View().GetByLabel(WorkbenchStrings.ThemeIconButton, new() { Exact = true }), WorkbenchStrings.ThemeIconButton);
    }

    [TestMethod, OnDesktops]
    public async Task ASpotWithoutText_Should_AskForATextAgain(WorkbenchSurface surface)
    {
        await EditText();

        await Pick(Heading(WorkbenchStrings.ThemeButtons), WorkbenchStrings.ThemeButtons);

        await Press(View().Locator(".radius-box").First);

        await Expect(Panel.GetByText(WorkbenchStrings.EditTextHint)).ToBeVisibleAsync();
        await Expect(Panel.Locator(".picked")).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task AnotherPage_Should_ForgetThePickedText(WorkbenchSurface surface)
    {
        await EditText();

        await Pick(Heading(WorkbenchStrings.ThemeButtons), WorkbenchStrings.ThemeButtons);

        await Press(Page.Locator("#wb-navigator").GetByRole(AriaRole.Link).Filter(new() { HasText = WorkbenchStrings.ShiftsTitle }));

        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/shifts"));
        await WaitForViews();
        await Expect(View().Locator(".shifts")).ToBeVisibleAsync();
        await Expect(ModeButton(WorkbenchStrings.ModeText)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Panel.GetByText(WorkbenchStrings.EditTextHint)).ToBeVisibleAsync();
        await Expect(Panel.Locator(".picked")).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task APress_Should_PickInsteadOfActingOnThePage(WorkbenchSurface surface)
    {
        await EditText("pos-dashboard", "?device=desktop-1536");

        await Pick(View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosCustomize, Exact = true }), WorkbenchStrings.PosCustomize);

        await Expect(View().GetByText(WorkbenchStrings.PosEditHint)).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task APickInAView_Should_SelectThatViewAndPutItsLanguageFirst(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts", $"?panes={WorkbenchUrls.EncodePanes([new() { Device = "iphone-se" }, new() { Device = "iphone-se", Culture = "fa-IR" }])}");

        var panes = await Page.Locator("iframe[data-pane-id]").EvaluateAllAsync<string[]>("frames => frames.map(f => f.dataset.paneId)");
        var heading = InCulture("fa-IR", nameof(WorkbenchStrings.ShiftsHeading));

        await Press(ModeButton(WorkbenchStrings.ModeText));
        await Expect(View(panes[1]).Locator("html")).ToHaveAttributeAsync("data-wb-mode", "text");

        await Pick(View(panes[1]).GetByRole(AriaRole.Heading, new() { Name = heading, Exact = true }), heading);

        await Expect(Page.Locator(".pane").Nth(1)).ToHaveAttributeAsync("aria-current", "true");
        await Expect(Panel.GetByRole(AriaRole.Textbox).First).ToHaveAccessibleNameAsync(Language("fa-IR"));
    }

    [TestMethod, OnDesktops]
    public async Task ATextNotInTheTranslationFiles_Should_SaySoAndGoToTheAgentWhenThereIsOne(WorkbenchSurface surface)
    {
        await EditText();

        var text = await View().Locator(".type-name").First.InnerTextAsync();

        await Pick(View().Locator(".type-name").First, text);

        await Expect(Panel.GetByText(WorkbenchStrings.TextNotTranslated)).ToBeVisibleAsync();
        await Expect(Key).ToHaveCountAsync(0);
        await Expect(Panel.GetByRole(AriaRole.Textbox)).ToHaveCountAsync(0);

        if (HasAgent is false)
        {
            await Expect(Panel.GetByRole(AriaRole.Button)).ToHaveCountAsync(0);
            return;
        }

        var request = string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.MoveTextRequest, text);

        await Press(AskButton);

        var threads = await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Any(t => t.Messages[0].Text == request));

        Assert.AreEqual("theme", threads.First(t => t.Messages[0].Text == request).PageKey);
        await Expect(Page.Locator("#wb-tab-review")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(ModeButton(WorkbenchStrings.ModeComment)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AskTheAgent_Should_SendOnlyWhatChangedAsACommentOnTheText(WorkbenchSurface surface)
    {
        await EditText("shifts");

        await Pick(Heading(WorkbenchStrings.ShiftsHeading), WorkbenchStrings.ShiftsHeading);

        await Expect(AskButton).ToBeDisabledAsync();

        var text = $"Weekly schedule {Guid.NewGuid():N}";

        await Field(defaultLanguage).FillAsync(text);

        await Expect(AskButton).ToBeEnabledAsync();
        await Press(AskButton);

        var threads = await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Any(t => t.Messages[0].Text.Contains(text, StringComparison.Ordinal)));
        var request = threads.Single(t => t.Messages[0].Text.Contains(text, StringComparison.Ordinal));

        Assert.AreEqual("shifts", request.PageKey);
        Assert.AreEqual(WorkbenchStrings.ShiftsHeading, request.Anchor.Text);
        Assert.AreEqual($"{string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ChangeTextRequest, nameof(WorkbenchStrings), nameof(WorkbenchStrings.ShiftsHeading))}\n{defaultLanguage}: “{WorkbenchStrings.ShiftsHeading}” → “{text}”", request.Messages[0].Text);

        await Expect(Page.Locator("#wb-tab-review")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(ModeButton(WorkbenchStrings.ModeComment)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Panel).ToHaveCountAsync(0);
        await Expect(Tools.GetByText(text)).ToBeVisibleAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task Save_Should_WriteEachChangedLanguageAndReloadTheViews(WorkbenchSurface surface)
    {
        var projectFile = Path.Combine(ProjectSource, WorkbenchTexts.Replace('/', Path.DirectorySeparatorChar));
        var before = File.ReadAllText(projectFile);
        var german = ReadTheCopy("Workbench/Resources/WorkbenchStrings.de.resx");
        var english = $"Rounded corners {Guid.NewGuid():N}";
        var persian = $"گوشه‌ها {Guid.NewGuid():N}";

        await EditText();

        await Pick(Heading(WorkbenchStrings.ThemeShape), WorkbenchStrings.ThemeShape);

        await Expect(SaveButton).ToBeDisabledAsync();
        await Expect(Panel.GetByText(WorkbenchStrings.SaveTranslationsHint)).ToBeVisibleAsync();

        await Field(defaultLanguage).FillAsync(english);
        await Field(Language("fa-IR")).FillAsync(persian);

        await Expect(SaveButton).ToBeEnabledAsync();
        await Press(SaveButton);

        await Expect(Heading(english)).ToBeVisibleAsync();
        await WaitForViews();
        await Expect(SaveButton).ToBeDisabledAsync();
        await Expect(Field(defaultLanguage)).ToHaveValueAsync(english);

        Assert.AreEqual(english, Entry(WorkbenchTexts, nameof(WorkbenchStrings.ThemeShape)));
        Assert.AreEqual(persian, Entry("Workbench/Resources/WorkbenchStrings.fa.resx", nameof(WorkbenchStrings.ThemeShape)));
        Assert.AreEqual(german, ReadTheCopy("Workbench/Resources/WorkbenchStrings.de.resx"));
        Assert.AreEqual(before, File.ReadAllText(projectFile));
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task Save_Should_KeepMarkupSafeInTheFile(WorkbenchSurface surface)
    {
        var text = $"Sizes & <shapes> {Guid.NewGuid():N}";

        await EditText();

        await Pick(Heading(WorkbenchStrings.ThemeInputs), WorkbenchStrings.ThemeInputs);

        await Field(defaultLanguage).FillAsync(text);
        await Press(SaveButton);

        await Expect(Heading(text)).ToBeVisibleAsync();

        Assert.AreEqual(text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"), Entry(WorkbenchTexts, nameof(WorkbenchStrings.ThemeInputs)));
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task Save_Should_WriteTheAppsOwnTextsIntoTheirFile(WorkbenchSurface surface)
    {
        var text = $"Your details {Guid.NewGuid():N}";

        await EditText();

        await Pick(View().GetByText(AppStrings.ProfileTitle, new() { Exact = true }).First, AppStrings.ProfileTitle);

        await Expect(Key).ToHaveTextAsync($"{nameof(AppStrings)} · {nameof(AppStrings.ProfileTitle)}");

        await Field(defaultLanguage).FillAsync(text);
        await Press(SaveButton);

        await WorkbenchTestReviewer.WaitFor(() => Task.FromResult(Entry("Shared/Resources/AppStrings.resx", nameof(AppStrings.ProfileTitle))), value => value == text);
        await Expect(View().GetByText(text, new() { Exact = true }).First).ToBeVisibleAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    [TestCategory("Localization")]
    public async Task EveryLanguagesField_Should_BeSaved(WorkbenchSurface surface)
    {
        await EditText();

        await Pick(Heading(WorkbenchStrings.ThemeNavigation), WorkbenchStrings.ThemeNavigation);

        foreach (var (displayName, culture) in CultureInfoManager.SupportedCultures)
        {
            var text = $"{culture.Name} {Guid.NewGuid():N}";

            await Field(displayName).FillAsync(text);
            await Press(SaveButton);

            await Expect(Page.GetByText(WorkbenchStrings.SaveTranslationsFailed)).ToHaveCountAsync(0);
            await Expect(SaveButton).ToBeDisabledAsync();
            await Expect(Field(displayName)).ToHaveValueAsync(text);
        }
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    [TestCategory("Localization")]
    public async Task EnglishUK_Should_ShareTheFileOfTheDefaultLanguage(WorkbenchSurface surface)
    {
        var text = $"Forms {Guid.NewGuid():N}";

        await EditText();

        await Pick(Heading(WorkbenchStrings.ThemeSelection), WorkbenchStrings.ThemeSelection);

        await Field(Language("en-GB")).FillAsync(text);
        await Press(SaveButton);

        await Expect(SaveButton).ToBeDisabledAsync();
        await Expect(Field(defaultLanguage)).ToHaveValueAsync(text);
        await Expect(Field(Language("en-GB"))).ToHaveValueAsync(text);
        await Expect(Page.GetByText(WorkbenchStrings.SaveTranslationsFailed)).ToHaveCountAsync(0);

        Assert.AreEqual(text, Entry(WorkbenchTexts, nameof(WorkbenchStrings.ThemeSelection)));
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AChangeThatLosesAPlaceholder_Should_BeRefusedWithTheEditKept(WorkbenchSurface surface)
    {
        var shown = string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ThemeTab, 1);

        await EditText();

        await Pick(View().GetByText(shown, new() { Exact = true }).First, shown);

        await Expect(Key).ToHaveTextAsync($"{nameof(WorkbenchStrings)} · {nameof(WorkbenchStrings.ThemeTab)}");

        await Field(defaultLanguage).FillAsync("A tab");
        await Press(SaveButton);

        await Expect(Page.GetByText(WorkbenchStrings.TextKeepPlaceholders).First).ToBeVisibleAsync();
        await Expect(Field(defaultLanguage)).ToHaveValueAsync("A tab");
        await Expect(SaveButton).ToBeEnabledAsync();
        Assert.AreEqual(WorkbenchStrings.ThemeTab, Entry(WorkbenchTexts, nameof(WorkbenchStrings.ThemeTab)));

        var kept = $"{{0}} tab {Guid.NewGuid():N}";

        await Field(defaultLanguage).FillAsync(kept);
        await Press(SaveButton);

        await Expect(View().GetByText(string.Format(CultureInfo.InvariantCulture, kept, 1), new() { Exact = true }).First).ToBeVisibleAsync();
        Assert.AreEqual(kept, Entry(WorkbenchTexts, nameof(WorkbenchStrings.ThemeTab)));
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AFileThatIsGone_Should_SayTheTextsWereNotSavedWithTheEditKept(WorkbenchSurface surface)
    {
        File.Delete(InTheCopy("Workbench/Resources/WorkbenchStrings.de.resx"));

        await EditText();

        await Pick(Heading(WorkbenchStrings.ThemeButtons), WorkbenchStrings.ThemeButtons);

        await Field(Language("de-DE")).FillAsync("Knöpfe");
        await Press(SaveButton);

        await Expect(Page.GetByText(WorkbenchStrings.SaveTranslationsFailed).First).ToBeVisibleAsync();
        await Expect(Field(Language("de-DE"))).ToHaveValueAsync("Knöpfe");
        await Expect(SaveButton).ToBeEnabledAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task DevModeWithoutTheProject_Should_OnlyAskTheAgent(WorkbenchSurface surface)
    {
        LeaveTheProject();

        await EditText("shifts");

        await Pick(Heading(WorkbenchStrings.ShiftsHeading), WorkbenchStrings.ShiftsHeading);

        await Expect(Field(defaultLanguage)).ToBeEditableAsync();
        await Expect(SaveButton).ToHaveCountAsync(0);
        await Expect(Panel.GetByText(WorkbenchStrings.SaveTranslationsHint)).ToHaveCountAsync(0);
        await Expect(AskButton).ToBeVisibleAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithoutAgent)]
    public async Task WithoutAnAgent_Should_ShowEveryLanguageWithoutLettingItChange(WorkbenchSurface surface)
    {
        await EditText("shifts");

        await Pick(Heading(WorkbenchStrings.ShiftsHeading), WorkbenchStrings.ShiftsHeading);

        await Expect(Key).ToHaveTextAsync($"{nameof(WorkbenchStrings)} · {nameof(WorkbenchStrings.ShiftsHeading)}");

        foreach (var (displayName, culture) in CultureInfoManager.SupportedCultures)
        {
            await Expect(Field(displayName)).ToHaveValueAsync(InCulture(culture.Name, nameof(WorkbenchStrings.ShiftsHeading)));
            await Expect(Field(displayName)).Not.ToBeEditableAsync();
        }

        await Expect(Panel.GetByRole(AriaRole.Button)).ToHaveCountAsync(0);
        await Expect(Panel.GetByText(WorkbenchStrings.SaveTranslationsHint)).ToHaveCountAsync(0);
        await Expect(Page.GetByText(WorkbenchStrings.AgentNotConnected)).ToBeVisibleAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithoutAgent)]
    public async Task APublishedCopy_Should_ShowEveryLanguageWithoutLettingItChange(WorkbenchSurface surface)
    {
        LeaveTheProject();

        await EditText("shifts");

        await Pick(Heading(WorkbenchStrings.ShiftsHeading), WorkbenchStrings.ShiftsHeading);

        await Expect(Field(defaultLanguage)).ToHaveValueAsync(WorkbenchStrings.ShiftsHeading);
        await Expect(Field(defaultLanguage)).Not.ToBeEditableAsync();
        await Expect(Panel.GetByRole(AriaRole.Button)).ToHaveCountAsync(0);
    }
}
