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

    private Task<string> ViewPrimaryColor() => View().Locator("body").EvaluateAsync<string>("e => getComputedStyle(e).getPropertyValue('--bit-clr-pri').trim()");

    private Task<string> ViewBackgroundColor() => View().Locator("body").EvaluateAsync<string>("e => getComputedStyle(e).getPropertyValue('--bit-clr-bg-pri').trim()");

    /// <summary>The theme's color swatches as the screen shows them, as the largest gap between a pixel's red, green and blue: none on a gray picture.</summary>
    private async Task<int> ColorOfTheSwatches()
    {
        var picture = await View().Locator(".swatches").First.ScreenshotAsync();

        return await Page.EvaluateAsync<int>("""
            async picture => {
                const bytes = Uint8Array.from(atob(picture), character => character.charCodeAt(0));
                const image = await createImageBitmap(new Blob([bytes], { type: 'image/png' }));
                const canvas = new OffscreenCanvas(image.width, image.height);
                const context = canvas.getContext('2d');

                context.drawImage(image, 0, 0);

                const { data } = context.getImageData(0, 0, image.width, image.height);
                let most = 0;

                for (let index = 0; index < data.length; index += 4) {
                    most = Math.max(most, Math.abs(data[index] - data[index + 1]), Math.abs(data[index + 1] - data[index + 2]));
                }

                return most;
            }
            """, Convert.ToBase64String(picture));
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
    public async Task Light_Should_BringTheViewBackFromDark(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?theme=dark");

        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ThemeLight, Exact = true }));
        await CloseTools();

        await Expect(Page).Not.ToHaveURLAsync(new Regex("theme="));
        await WaitForViews();
        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(View().Locator("body")).ToHaveCSSAsync("background-color", new Regex(@"rgb\((2[0-9]{2}), "));
    }

    [TestMethod, EverySurface]
    public async Task System_Should_MakeTheViewFollowTheDevice(WorkbenchSurface surface)
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });

        await OpenWorkbench("theme");

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ThemeSystem, Exact = true }));
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?theme=system$"));
        await WaitForViews();
        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));

        if (IsPhone is false)
        {
            await Expect(Page.Locator(".pane-chip")).ToHaveTextAsync(WorkbenchStrings.ThemeSystem);
        }

        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });

        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("bit-theme", new Regex("dark"));
    }

    [TestMethod, EverySurface]
    public async Task TheAddress_Should_OpenAViewInTheDevicesThemeWithSystem(WorkbenchSurface surface)
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });

        await OpenWorkbench("theme", "?theme=system");

        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));

        await OpenTools();
        await Expect(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ThemeSystem, Exact = true })).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod, EverySurface]
    public async Task TheAddress_Should_OpenAViewInItsThemeAndLanguage(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?theme=dark&culture=fa-IR");

        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(View().Locator("html")).ToHaveAttributeAsync("dir", "rtl");
    }

    [TestMethod, EverySurface]
    public async Task AView_Should_WearTheProjectsTheme(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", AppThemePresets.Light);
        Assert.AreEqual(AppThemePresets.LightBackground, await ViewBackgroundColor(), ignoreCase: true);

        await OpenWorkbench("theme", "?theme=dark");

        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", AppThemePresets.Dark);
        Assert.AreEqual(AppThemePresets.DarkBackground, await ViewBackgroundColor(), ignoreCase: true);

        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await OpenWorkbench("theme", "?theme=system");

        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", AppThemePresets.Dark);
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
    public async Task ACustomColor_Should_BeTypedAndBeTriedOnTheViewOnly(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        var field = Tools.GetByLabel(WorkbenchStrings.CustomColor, new() { Exact = true });
        var before = await ViewPrimaryColor();
        var shell = await Page.Locator("body").EvaluateAsync<string>("e => getComputedStyle(e).getPropertyValue('--bit-clr-pri').trim()");

        await OpenTools();
        await field.FillAsync("#c81e6e");
        await field.BlurAsync();
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?accent=c81e6e$"));
        await WaitForViews();

        await WorkbenchTestReviewer.WaitFor(ViewPrimaryColor, primary => primary != before);

        Assert.MatchesRegex(new Regex("(?i)c81e6e|rgb\\(200, 30, 110\\)"), await ViewPrimaryColor());
        Assert.AreEqual(shell, await Page.Locator("body").EvaluateAsync<string>("e => getComputedStyle(e).getPropertyValue('--bit-clr-pri').trim()"));

        // What isn't a color takes the theme's own primary back.
        await OpenTools();
        await field.FillAsync("nope");
        await field.BlurAsync();
        await CloseTools();

        await Expect(Page).Not.ToHaveURLAsync(new Regex("accent="));
        await WaitForViews();

        await WorkbenchTestReviewer.WaitFor(ViewPrimaryColor, primary => primary == before);
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
    public async Task AColorVision_Should_ChangeTheColorsOnTheScreen(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        var typical = await ColorOfTheSwatches();

        Assert.IsGreaterThan(60, typical, "The swatches have color to lose.");

        await OpenWorkbench("theme", "?vision=achromatopsia");

        Assert.IsLessThanOrEqualTo(2, await ColorOfTheSwatches(), "No color is left.");

        await OpenWorkbench("theme", "?vision=tritanopia");

        Assert.AreNotEqual(typical, await ColorOfTheSwatches());
    }

    [TestMethod, EverySurface]
    public async Task TypicalVision_Should_TakeTheFilterAway(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?vision=achromatopsia");

        await Expect(View().Locator("html")).ToHaveAttributeAsync("data-wb-vision", "achromatopsia");

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.ColorVision }));
        await Press(Page.GetByRole(AriaRole.Option, new() { Name = WorkbenchStrings.VisionTypical, Exact = true }));
        await CloseTools();

        await Expect(Page).Not.ToHaveURLAsync(new Regex("vision="));
        await WaitForViews();
        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("data-wb-vision", new Regex("."));

        Assert.IsGreaterThan(60, await ColorOfTheSwatches(), "The colors are back.");
    }

    [TestMethod, EverySurface]
    public async Task ReducedMotion_Should_EndTheTransitionsAtOnce(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        Assert.IsGreaterThan(0.01, await LongestTransitionOfASwitch(), "The page's components have transitions to end.");
        Assert.AreEqual(2, await TransitionOfThePagesOwn(), 0.001);

        await OpenWorkbench("theme", "?motion=reduce");

        Assert.IsLessThan(0.001, await LongestTransitionOfASwitch());
        Assert.IsLessThan(0.001, await TransitionOfThePagesOwn());
    }

    private Task<double> LongestTransitionOfASwitch() => View().GetByRole(AriaRole.Switch).First.EvaluateAsync<double>("element => Math.max(...getComputedStyle(element).transitionDuration.split(',').map(parseFloat))");

    /// <summary>How long an element of the page's own, with a transition of 2 seconds, really takes.</summary>
    private Task<double> TransitionOfThePagesOwn() => View().Locator("body").EvaluateAsync<double>("""
        body => {
            const element = document.createElement('div');
            element.style.transition = 'opacity 2s';
            body.append(element);
            const duration = parseFloat(getComputedStyle(element).transitionDuration);
            element.remove();
            return duration;
        }
        """);

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
