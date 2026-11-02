namespace Boilerplate.WorkbenchTests.Features.Shell;

/// <summary>A view's theme, language and look, whichever way they are changed: from the tools, the shell's own switch or the app's menu in the view.</summary>
[TestClass, TestCategory("UITest")]
public class ThemeAndCultureUITests : WorkbenchPageTest
{
    private const string Persian = "فارسی";

    private async Task<string?> ViewRootAttribute(string name, string paneId = "1")
    {
        return await View(paneId).Locator("html").GetAttributeAsync(name);
    }

    private async Task PickLanguageInTheTools()
    {
        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.Language }));
        await Press(Page.GetByRole(AriaRole.Option, new() { Name = Persian, Exact = true }));
        await CloseTools();
    }

    private async Task ExpectTheViewInPersian()
    {
        var heading = InCulture("fa-IR", nameof(WorkbenchStrings.ThemeHeading));

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?culture=fa-IR$"));
        await WaitForViews();
        await Expect(View().Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(View().Locator("html")).ToHaveAttributeAsync("lang", "fa-IR");
        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = heading, Level = 2 }).First).ToBeVisibleAsync();
        await Expect(Page.Locator("html")).Not.ToHaveAttributeAsync("dir", "rtl");
    }

    private async Task OpenTheAppMenu()
    {
        await Press(View().Locator(".persona").First);
        await Expect(View().GetByRole(AriaRole.Button, new() { Name = AppStrings.Language, Exact = true })).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task Dark_Should_ReachTheViewOnly(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ThemeDark, Exact = true }));
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?theme=dark$"));
        await WaitForViews();
        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(Page.Locator("html")).Not.ToHaveAttributeAsync("bit-theme", new Regex("dark"));

        if (IsPhone is false)
        {
            await Expect(Page.Locator(".pane-chip")).ToHaveTextAsync(WorkbenchStrings.ThemeDark);
        }
    }

    [TestMethod, EverySurface]
    public async Task TheAddress_Should_OpenAViewInItsThemeAndLanguage(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?theme=dark&culture=fa-IR");

        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(View().Locator("html")).ToHaveAttributeAsync("dir", "rtl");
    }

    [TestMethod, OnDesktops]
    public async Task CompareThemes_Should_ShowLightAndDarkSideBySide(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Button(WorkbenchStrings.Compare).ClickAsync();
        await Page.GetByText(WorkbenchStrings.CompareThemes, new() { Exact = true }).ClickAsync();

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(2);
        await WaitForViews();

        var panes = await Page.Locator("iframe[data-pane-id]").EvaluateAllAsync<string[]>("frames => frames.map(f => f.dataset.paneId)");

        Assert.HasCount(2, panes);
        Assert.DoesNotMatchRegex(new Regex("dark"), await ViewRootAttribute("bit-theme", panes[0]) ?? string.Empty);
        Assert.MatchesRegex(new Regex("dark"), await ViewRootAttribute("bit-theme", panes[1]) ?? string.Empty);
    }

    [TestMethod, EverySurface]
    [TestCategory("Localization")]
    public async Task ALanguage_Should_TranslateTheViewAndTurnItRightToLeft(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await PickLanguageInTheTools();

        await ExpectTheViewInPersian();
    }

    [TestMethod, EverySurface]
    public async Task AnAccent_Should_BeTriedOnTheViewOnly(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        var before = await View().Locator("body").EvaluateAsync<string>("e => getComputedStyle(e).getPropertyValue('--bit-clr-pri').trim()");

        await OpenTools();
        await Press(Tools.Locator("div.accents button[role='radio']").Nth(2));
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?accent=[0-9a-f]{6}$"));
        await WaitForViews();

        await WorkbenchTestReviewer.WaitFor(() => View().Locator("body").EvaluateAsync<string>("e => getComputedStyle(e).getPropertyValue('--bit-clr-pri').trim()"), primary => primary != before);

        if (IsPhone is false)
        {
            await Expect(Page.Locator(".pane-chip")).ToHaveTextAsync(WorkbenchStrings.AccentColor);
        }
    }

    [TestMethod, EverySurface]
    public async Task TextSizeAndColorVision_Should_ChangeTheViewsLook(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?text=200&vision=achromatopsia&motion=reduce");

        await Expect(View().Locator("html")).ToHaveAttributeAsync("style", new Regex(@"font-size:\s*200%"));
        await Expect(View().Locator("html")).ToHaveAttributeAsync("data-wb-vision", "achromatopsia");
        await Expect(View().Locator("html")).ToHaveAttributeAsync("data-wb-motion", "reduce");

        if (IsPhone is false)
        {
            await Expect(Page.Locator(".pane-chip")).ToHaveTextAsync([$"{WorkbenchStrings.TextSize} 200%", WorkbenchStrings.ReduceMotion, WorkbenchStrings.VisionAchromatopsia]);
        }
    }

    [TestMethod, EverySurface]
    public async Task TheToolsForTextSizeMotionAndVision_Should_ChangeTheViewsLook(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = "130%", Exact = true }));
        await Press(Tools.GetByRole(AriaRole.Switch, new() { Name = WorkbenchStrings.ReduceMotion }));
        await Press(Tools.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.ColorVision }));
        await Press(Page.GetByRole(AriaRole.Option, new() { Name = WorkbenchStrings.VisionProtanopia, Exact = true }));
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex("text=130"));
        await Expect(Page).ToHaveURLAsync(new Regex("motion=reduce"));
        await Expect(Page).ToHaveURLAsync(new Regex("vision=protanopia"));
        await WaitForViews();

        await Expect(View().Locator("html")).ToHaveAttributeAsync("style", new Regex(@"font-size:\s*130%"));
        await Expect(View().Locator("html")).ToHaveAttributeAsync("data-wb-motion", "reduce");
        await Expect(View().Locator("html")).ToHaveAttributeAsync("data-wb-vision", "protanopia");
    }

    [TestMethod, EverySurface]
    public async Task TheAppMenusThemeSwitch_Should_TurnTheViewDarkInTheAddressToo(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenTheAppMenu();
        await Press(View().GetByRole(AriaRole.Button, new() { Name = AppStrings.Light, Exact = true }));

        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(Page).ToHaveURLAsync(new Regex(@"\?theme=dark$"));
        await Expect(Page.Locator("html")).Not.ToHaveAttributeAsync("bit-theme", new Regex("dark"));
    }

    [TestMethod, EverySurface]
    [TestCategory("Localization")]
    public async Task TheAppMenusLanguage_Should_TranslateTheView(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenTheAppMenu();
        await Press(View().GetByRole(AriaRole.Button, new() { Name = AppStrings.Language, Exact = true }));
        await Press(View().GetByText(Persian, new() { Exact = true }));

        await ExpectTheViewInPersian();
    }

    [TestMethod, EverySurface]
    public async Task TheShellsOwnThemeSwitch_Should_LeaveTheViewAsItIsAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenTools();
        await Press(Button(WorkbenchStrings.ToggleWorkbenchTheme));
        await CloseTools();

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("bit-theme", new Regex("dark"));

        await Page.ReloadAsync();
        await WaitForViews();

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("bit-theme", new Regex("dark"));
    }
}
