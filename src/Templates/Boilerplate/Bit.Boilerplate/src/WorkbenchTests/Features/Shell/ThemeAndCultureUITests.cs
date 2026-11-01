using System.Resources;

namespace Boilerplate.WorkbenchTests.Features.Shell;

[TestClass, TestCategory("UITest")]
public class ThemeAndCultureUITests : WorkbenchPageTest
{
    private ILocator Button(string name) => Page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private async Task<string?> ViewRootAttribute(string name, string paneId = "1")
    {
        return await View(paneId).Locator("html").GetAttributeAsync(name);
    }

    [TestMethod]
    public async Task Dark_Should_ReachTheViewOnly()
    {
        await OpenWorkbench("theme");

        await Button(WorkbenchStrings.ThemeDark).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?theme=dark$"));
        await WaitForViews();
        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(Page.Locator(".pane-chip")).ToHaveTextAsync(WorkbenchStrings.ThemeDark);
        await Expect(Page.Locator("html")).Not.ToHaveAttributeAsync("bit-theme", new Regex("dark"));
    }

    [TestMethod]
    public async Task CompareThemes_Should_ShowLightAndDarkSideBySide()
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

    [TestMethod]
    [TestCategory("Localization")]
    public async Task ALanguage_Should_TranslateTheViewAndTurnItRightToLeft()
    {
        await OpenWorkbench("theme");

        await Page.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.Language }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "فارسی", Exact = true }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?culture=fa-IR$"));
        await WaitForViews();

        var heading = new ResourceManager(typeof(WorkbenchStrings)).GetString(nameof(WorkbenchStrings.ThemeHeading), CultureInfo.GetCultureInfo("fa-IR"))!;

        await Expect(View().Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(View().Locator("html")).ToHaveAttributeAsync("lang", "fa-IR");
        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = heading, Level = 2 }).First).ToBeVisibleAsync();
        await Expect(Page.Locator("html")).Not.ToHaveAttributeAsync("dir", "rtl");
    }

    [TestMethod]
    public async Task AnAccent_Should_BeTriedOnTheViewOnly()
    {
        await OpenWorkbench("theme");

        var before = await View().Locator("body").EvaluateAsync<string>("e => getComputedStyle(e).getPropertyValue('--bit-clr-pri').trim()");

        await Page.Locator("div.accents button[role='radio']").Nth(2).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?accent=[0-9a-f]{6}$"));
        await WaitForViews();

        await WorkbenchTestReviewer.WaitFor(() => View().Locator("body").EvaluateAsync<string>("e => getComputedStyle(e).getPropertyValue('--bit-clr-pri').trim()"), primary => primary != before);
        await Expect(Page.Locator(".pane-chip")).ToHaveTextAsync(WorkbenchStrings.AccentColor);
    }

    [TestMethod]
    public async Task TextSizeAndColorVision_Should_ChangeTheViewsLook()
    {
        await OpenWorkbench("theme", "?text=200&vision=achromatopsia&motion=reduce");

        await Expect(View().Locator("html")).ToHaveAttributeAsync("style", new Regex(@"font-size:\s*200%"));
        await Expect(View().Locator("html")).ToHaveAttributeAsync("data-wb-vision", "achromatopsia");
        await Expect(View().Locator("html")).ToHaveAttributeAsync("data-wb-motion", "reduce");
        await Expect(Page.Locator(".pane-chip")).ToHaveTextAsync([$"{WorkbenchStrings.TextSize} 200%", WorkbenchStrings.ReduceMotion, WorkbenchStrings.VisionAchromatopsia]);
    }
}
