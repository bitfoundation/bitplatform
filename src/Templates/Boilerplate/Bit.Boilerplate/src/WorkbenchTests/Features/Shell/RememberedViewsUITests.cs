namespace Boilerplate.WorkbenchTests.Features.Shell;

/// <summary>What the browser remembers of the shell, so a reload or a restart comes back to the same place: the views, the selected one, the zoom and the open tab, and what a link with them opens.</summary>
[TestClass, TestCategory("UITest")]
public class RememberedViewsUITests : ShellPageTest
{
    private static readonly string twoViews = WorkbenchUrls.EncodePanes([new() { Device = "iphone-se" }, new() { Device = "ipad-mini", Theme = WorkbenchThemeMode.Dark }]);

    private ILocator IPhoneSE => Page.GetByRole(AriaRole.Group, new() { Name = "iPhone SE", Exact = true });

    private ILocator IPadMini => Page.GetByRole(AriaRole.Group, new() { Name = "iPad mini", Exact = true });

    private ILocator ZoomButton(string name) => Page.Locator("#wb-inspector").GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    /// <summary>A setting as the saved state holds it, which is JSON inside a string.</summary>
    private static string Saved(string key, string value) => $"{key}\\\":\\\"{value}\\\"";

    private async Task ExpectTheTwoViewsWithTheIPadSelected()
    {
        await Expect(Page.Locator(".pane")).ToHaveCountAsync(2);
        await Expect(IPhoneSE).ToBeVisibleAsync();
        await Expect(IPadMini).ToHaveAttributeAsync("aria-current", "true");
        await Expect(IPhoneSE).Not.ToHaveAttributeAsync("aria-current", "true");
        await Expect(IPadMini.Locator(".pane-chip")).ToHaveTextAsync(WorkbenchStrings.ThemeDark);

        var ids = await PaneIds();

        await ExpectViewSize(375, 667, ids[0]);
        await ExpectViewSize(744, 1133, ids[1]);
        await Expect(View(ids[1]).Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(View(ids[0]).Locator("html")).Not.ToHaveAttributeAsync("bit-theme", new Regex("dark"));
    }

    [TestMethod, OnDesktops]
    public async Task SeveralViews_Should_ComeBackWithTheSelectedOneAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", $"?panes={twoViews}");

        await IPadMini.Locator(".pane-header").ClickAsync();
        await Expect(IPadMini).ToHaveAttributeAsync("aria-current", "true");

        await WaitUntilSaved(Saved("SelectedPaneId", (await PaneIds())[1]));

        await Page.ReloadAsync();
        await WaitForViews();

        await ExpectTheTwoViewsWithTheIPadSelected();
    }

    [TestMethod, OnDesktops]
    public async Task SeveralViews_Should_ComeBackWhenThePageIsOpenedAgainWithoutThemInTheAddress(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", $"?panes={twoViews}");

        await IPadMini.Locator(".pane-header").ClickAsync();
        await Expect(IPadMini).ToHaveAttributeAsync("aria-current", "true");

        await WaitUntilSaved(Saved("SelectedPaneId", (await PaneIds())[1]));

        await OpenWorkbench("theme");

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?panes="));
        await ExpectTheTwoViewsWithTheIPadSelected();
    }

    [TestMethod, OnDesktops]
    public async Task EachView_Should_RememberWhatWasDoneOnItsOwnPage(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings", $"?panes={twoViews}");

        var ids = await PaneIds();

        await BuildingsSearch(ids[0]).FillAsync("Rotterdam");
        await BuildingsSearch(ids[1]).FillAsync("Euromast");

        await WaitUntilSaved("Rotterdam");
        await WaitUntilSaved("Euromast");

        await Page.ReloadAsync();
        await WaitForViews();

        await Expect(BuildingsSearch(ids[0])).ToHaveValueAsync("Rotterdam");
        await Expect(BuildingsSearch(ids[1])).ToHaveValueAsync("Euromast");
    }

    [TestMethod, OnDesktops]
    public async Task TheZoom_Should_ComeBackAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=iphone-se");

        await Button("0.5").ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("zoom=0.5"));
        await WaitUntilSaved(Saved("Zoom", "0.5"));

        await OpenWorkbench("theme");

        await Expect(Page).ToHaveURLAsync(new Regex("device=iphone-se&zoom=0.5$"));
        await Expect(Page.Locator(".pane-scaler")).ToHaveCSSAsync("--wb-scale", "0.5");
        await Expect(ZoomButton("0.5")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(ZoomButton(WorkbenchStrings.ZoomFit)).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [TestMethod, OnDesktops]
    public async Task ALinkWithAZoom_Should_OpenTheViewAtThatScale(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=iphone-se&zoom=0.75");

        await Expect(Page.Locator(".pane-scaler")).ToHaveCSSAsync("--wb-scale", "0.75");
        await Expect(ZoomButton("0.75")).ToHaveAttributeAsync("aria-pressed", "true");

        var box = (await Page.Locator("iframe[data-pane-id='1']").BoundingBoxAsync())!;

        Assert.AreEqual(375 * 0.75, box.Width, 1);
        Assert.AreEqual(667 * 0.75, box.Height, 1);
        await ExpectViewSize(375, 667);
    }

    [TestMethod, OnDesktops]
    public async Task ALinkWithAZoomAlone_Should_LeaveTheViewsItFinds(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=iphone-se");

        await Button(WorkbenchStrings.Rotate).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("device=iphone-se&rotated=true$"));
        await WaitUntilSaved("rotated\\\":true");

        await OpenWorkbench("theme", "?zoom=0.5");

        await Expect(Page).ToHaveURLAsync(new Regex("device=iphone-se&rotated=true&zoom=0.5$"));
        await Expect(Page.Locator(".pane-scaler")).ToHaveCSSAsync("--wb-scale", "0.5");
        await ExpectViewSize(667, 375);
    }

    [TestMethod, OnDesktops]
    public async Task TheOpenTab_Should_ComeBackAfterAReloadAndAModeShouldNot(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenTab(WorkbenchModes.ChecksColumn);

        await Expect(Page.Locator("#wb-tab-checks")).ToHaveAttributeAsync("aria-selected", "true");
        await WaitUntilSaved(Saved("Column", WorkbenchModes.ChecksColumn));

        await Button(WorkbenchStrings.ModeText).ClickAsync();
        await Expect(Button(WorkbenchStrings.ModeText)).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.ReloadAsync();
        await WaitForViews();

        await Expect(Button(WorkbenchStrings.ModeBrowse)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator("#wb-tab-checks")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page.Locator("#wb-tab-view")).ToHaveAttributeAsync("aria-selected", "false");
        await Expect(Page.Locator("#wb-column-panel")).ToHaveAttributeAsync("aria-labelledby", "wb-tab-checks");
    }

    [TestMethod, OnPhones]
    public async Task ALinkWithSeveralViewsOnAPhone_Should_ComeBackToTheSelectedOneWithoutThemInTheAddress(WorkbenchSurface surface)
    {
        var panes = WorkbenchUrls.EncodePanes([new() { Device = "iphone-se", Theme = WorkbenchThemeMode.Dark }, new() { Device = "ipad-mini" }]);

        await OpenWorkbench("theme", $"?panes={panes}");

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = "130%", Exact = true }));
        await CloseTools();

        await WaitUntilSaved("ipad-mini");

        await OpenWorkbench("theme");

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?panes="));
        await Expect(Page.Locator("iframe[data-pane-id]")).ToHaveCountAsync(1);

        var view = Page.Locator("iframe[data-pane-id]").ContentFrame.Locator("html");

        await Expect(view).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(view).ToHaveAttributeAsync("style", new Regex(@"font-size:\s*130%"));
    }
}
