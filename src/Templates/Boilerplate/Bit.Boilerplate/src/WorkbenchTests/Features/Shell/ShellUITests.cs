namespace Boilerplate.WorkbenchTests.Features.Shell;

/// <summary>The shell around a view: opening a page in it, the page list, the view's settings and what the address and the browser remember of them.</summary>
[TestClass, TestCategory("UITest")]
public class ShellUITests : WorkbenchPageTest
{
    private ILocator PageList => Tools.Or(Page.Locator("#wb-navigator")).GetByRole(AriaRole.Navigation, new() { Name = WorkbenchStrings.Pages });

    [TestMethod, EverySurface]
    public async Task APage_Should_OpenInAViewOfTheShell(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Expect(Page).ToHaveTitleAsync($"{WorkbenchStrings.ThemePageTitle} · {WorkbenchStrings.WorkbenchTitle}");
        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeHeading, Level = 2 }).First).ToBeVisibleAsync();

        if (IsPhone is false)
        {
            await Expect(Page.GetByRole(AriaRole.Group, new() { Name = DevicePresets.Default.Name })).ToBeVisibleAsync();
        }
    }

    [TestMethod, EverySurface]
    public async Task APageTheWorkbenchLacks_Should_SaySoWithAWayBack(WorkbenchSurface surface)
    {
        await Page.GotoAsync(new Uri(Server.Address, "workbench/missing").ToString());

        var back = Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.BackToHome });

        await Expect(Page.GetByText(WorkbenchStrings.PageNotFound)).ToBeVisibleAsync();
        await Expect(back).ToHaveAttributeAsync("href", "/");

        await Press(back);

        await Expect(Page.Locator("a.page-card").First).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task ThePageList_Should_OpenAnotherPageOnTheSameViews(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?theme=dark&device=ipad-mini");

        await OpenTools();
        await Press(PageList.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.BuildingsTitle }));

        await Expect(Page).ToHaveURLAsync(new Regex(@"/workbench/buildings\?theme=dark&device=ipad-mini$"));
        await WaitForViews();
        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.BuildingsHeading })).ToBeVisibleAsync();
        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));

        if (IsPhone)
        {
            await Expect(Tools).ToHaveAttributeAsync("inert", string.Empty);
        }
        else
        {
            await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "iPad mini" })).ToBeVisibleAsync();
        }
    }

    [TestMethod, EverySurface]
    public async Task ThePageListsSearch_Should_FindPagesAndHideTheRest(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenTools();

        var search = PageList.GetByRole(AriaRole.Searchbox, new() { Name = WorkbenchStrings.SearchPages });
        var buildings = PageList.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.BuildingsTitle });
        var shifts = PageList.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.ShiftsTitle });

        await Expect(shifts).ToBeVisibleAsync();

        await search.FillAsync(WorkbenchStrings.BuildingsTitle);

        await Expect(buildings).ToBeVisibleAsync();
        await Expect(shifts).ToHaveCountAsync(0);
        await Expect(PageList.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.AllFlows })).ToHaveCountAsync(0);

        await search.FillAsync(string.Empty);

        await Expect(shifts).ToBeVisibleAsync();
        await Expect(PageList.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.AllFlows })).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task ThePageList_Should_LeadToTheFlows(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenTools();
        await Press(PageList.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.AllFlows }));

        await Expect(Page).ToHaveURLAsync(new Regex($"{WorkbenchUrls.Flows}$"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.Flows, Level = 1 })).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task ThePageListAndTheSideColumn_Should_FoldAwayAndStayFoldedAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        var pages = Button(WorkbenchStrings.Pages);
        var tools = Button(WorkbenchStrings.Tools);

        await Expect(pages).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#wb-navigator")).ToBeVisibleAsync();

        await pages.ClickAsync();
        await tools.ClickAsync();

        await Expect(Page.Locator(".workbench.nav-collapsed.inspector-collapsed")).ToBeVisibleAsync();
        await Expect(pages).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(tools).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.ReloadAsync();
        await WaitForViews();

        await Expect(Page.Locator(".workbench.nav-collapsed.inspector-collapsed")).ToBeVisibleAsync();
        await Expect(pages).ToHaveAttributeAsync("aria-expanded", "false");

        await tools.ClickAsync();

        await Expect(Page.Locator(".workbench.nav-collapsed:not(.inspector-collapsed)")).ToBeVisibleAsync();
        await Expect(Button(WorkbenchStrings.Pages)).ToHaveAttributeAsync("aria-expanded", "false");

        await tools.ClickAsync();
        await Expect(Page.Locator(".workbench.nav-collapsed.inspector-collapsed")).ToBeVisibleAsync();

        // Edit text works in the side column, so it brings the column back.
        await Button(WorkbenchStrings.ModeText).ClickAsync();

        await Expect(Page.Locator(".workbench.nav-collapsed:not(.inspector-collapsed)")).ToBeVisibleAsync();
        await Expect(Page.GetByText(WorkbenchStrings.EditTextHint)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheDataState_Should_ReachTheViewAndTheAddressAndSurviveAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DataEmpty, Exact = true }));

        await Expect(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DataEmpty, Exact = true })).ToHaveAttributeAsync("aria-pressed", "true");

        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?data=empty$"));
        await Expect(View().GetByText(WorkbenchStrings.BuildingsEmptyTitle)).ToBeVisibleAsync();

        await Page.ReloadAsync();
        await WaitForViews();

        await Expect(View().GetByText(WorkbenchStrings.BuildingsEmptyTitle)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task EveryDataState_Should_ReachTheView(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DataError, Exact = true }));
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?data=error$"));
        await Expect(View().GetByText(WorkbenchStrings.BuildingsFailedTitle)).ToBeVisibleAsync();

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DataLoading, Exact = true }));
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?data=loading$"));
        await WaitForViews();
        await Expect(View().GetByText(WorkbenchStrings.BuildingsFailedTitle)).ToHaveCountAsync(0);
        await Expect(View().Locator(".buildings-grid .bit-dtg-body .name-cell")).ToHaveCountAsync(0);

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DataSlow, Exact = true }));
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?data=slow$"));
        await Expect(View().Locator(".buildings-grid .bit-dtg-body .name-cell").First.Or(View().Locator(".buildings-cards > *").First)).ToBeVisibleAsync();

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DataLive, Exact = true }));
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex(@"/workbench/buildings$"));
    }

    [TestMethod, EverySurface]
    public async Task TheAppsNavigationSwitch_Should_TakeTheChromeAwayFromTheView(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Expect(View().Locator(".persona").First).ToBeVisibleAsync();

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Switch, new() { Name = WorkbenchStrings.AppChrome }));
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?chrome=false$"));
        await WaitForViews();
        await Expect(View().Locator(".persona")).ToHaveCountAsync(0);
        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeHeading, Level = 1 })).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task AStoreBackground_Should_ComposeTheViewWithItsCaption(WorkbenchSurface surface)
    {
        var caption = $"Sell faster {Guid.NewGuid():N}"[..20];

        await OpenWorkbench("theme");

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Switch, new() { Name = WorkbenchStrings.Backdrop }));
        await Press(Tools.GetByRole(AriaRole.Radio, new() { Name = WorkbenchStrings.BackdropOcean, Exact = true }));
        await Tools.GetByLabel(WorkbenchStrings.Caption, new() { Exact = true }).FillAsync(caption);
        await Tools.GetByLabel(WorkbenchStrings.Caption, new() { Exact = true }).BlurAsync();
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex("backdrop=ocean"));
        await Expect(Page).ToHaveURLAsync(new Regex("caption="));
        await WaitForViews();

        var composition = View().Locator(".composition.ocean");

        await Expect(composition).ToBeVisibleAsync();
        await Expect(composition.Locator(".caption")).ToHaveTextAsync(caption);
        await Expect(View().FrameLocator("iframe.screen-frame").GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeHeading, Level = 2 }).First).ToBeVisibleAsync();

        // Scaled to fit, on a phone too, instead of running off the screen.
        var box = await Page.Locator("iframe[data-pane-id='1']").BoundingBoxAsync();
        var viewport = Page.ViewportSize!;

        Assert.IsGreaterThanOrEqualTo(0, box!.X);
        Assert.IsLessThanOrEqualTo(viewport.Width, box.X + box.Width);
        Assert.IsLessThanOrEqualTo(viewport.Height, box.Y + box.Height);
    }

    [TestMethod, EverySurface]
    public async Task Reset_Should_PutTheViewAndWhatWasDoneOnItBack(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings", "?theme=dark&data=empty");

        await Expect(View().GetByText(WorkbenchStrings.BuildingsEmptyTitle)).ToBeVisibleAsync();

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Reset, Exact = true }));
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex(@"/workbench/buildings$"));
        await WaitForViews();
        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(View().GetByText(WorkbenchStrings.BuildingsEmptyTitle)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheViews_Should_ComeBackWhenThePageIsOpenedAgainWithoutThemInTheAddress(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ThemeDark, Exact = true }));
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?theme=dark$"));
        await WaitForViews();

        await WaitUntilSaved("Dark");

        await OpenWorkbench("theme");

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?theme=dark$"));
        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
    }

    [TestMethod, EverySurface]
    public async Task BrowserBack_Should_ReturnToThePageBefore(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenTools();
        await Press(PageList.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.BuildingsTitle }));

        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/buildings"));
        await WaitForViews();
        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.BuildingsHeading })).ToBeVisibleAsync();

        await Page.GoBackAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/theme"));
        await WaitForViews();
        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeHeading, Level = 2 }).First).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task APageAlone_Should_TakeItsSettingsFromItsAddress(WorkbenchSurface surface)
    {
        await OpenPreview("theme", "?theme=dark&culture=fa-IR&data=loading");

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = InCulture("fa-IR", nameof(WorkbenchStrings.ThemeHeading)), Level = 2 }).First).ToBeVisibleAsync();
        await Expect(Page.Locator(".workbench")).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task APageAlone_Should_ChangeItsOwnLanguageAndTheme(WorkbenchSurface surface)
    {
        await OpenPreview("theme");

        await Press(Page.Locator(".persona").First);
        await Press(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Light, Exact = true }).Locator("visible=true"));

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(Page).ToHaveURLAsync(new Regex("theme=dark"));

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Language, Exact = true }).Locator("visible=true"));
        await Press(Page.GetByText("فارسی", new() { Exact = true }).Locator("visible=true"));

        await Expect(Page).ToHaveURLAsync(new Regex("culture=fa-IR"));
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
    }

    [TestMethod, OnDesktops]
    public async Task ADevice_Should_BePickedFromTheList(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Page.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.Device }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Galaxy S26 · 360×780", Exact = true }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?device=galaxy-s26$"));
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Galaxy S26", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.Locator(".pane-size")).ToHaveTextAsync("360×780 · 3x");
    }

    [TestMethod, OnDesktops]
    public async Task ACustomSize_Should_BeTypedAndReachTheView(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Page.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.Device }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = WorkbenchStrings.CustomSize }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("device=custom"));

        await Page.GetByLabel(WorkbenchStrings.Width, new() { Exact = true }).FillAsync("700");
        await Page.GetByLabel(WorkbenchStrings.Width, new() { Exact = true }).BlurAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"size=700x\d+"));
        await Expect(Page.Locator(".pane-size")).ToContainTextAsync("700×");
        await WaitForViews();
        await Expect(Page.Locator(".breakpoint[aria-pressed='true']")).ToHaveCountAsync(1);
    }

    [TestMethod, OnDesktops]
    public async Task DraggingAViewsCorner_Should_ResizeIt(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=custom&size=600x500");

        var handle = Page.Locator(".resize-handle.resize-xy");
        var box = await handle.BoundingBoxAsync();

        await Page.Mouse.MoveAsync(box!.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width / 2 - 100, box.Y + box.Height / 2 - 80, new() { Steps = 5 });
        await Page.Mouse.UpAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"device=custom&size=(?!600x500)\d+x\d+"));
    }

    [TestMethod, OnDesktops]
    public async Task RotatingAView_Should_TurnItsSize(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=iphone-se");

        await Expect(Page.Locator(".pane-size")).ToHaveTextAsync("375×667 · 2x");

        await Button(WorkbenchStrings.Rotate).ClickAsync();

        await Expect(Page.Locator(".pane-size")).ToHaveTextAsync("667×375 · 2x");
        await Expect(Page).ToHaveURLAsync(new Regex("rotated=true"));
        await Expect(Button(WorkbenchStrings.Landscape)).ToHaveAttributeAsync("aria-pressed", "true");

        await Button(WorkbenchStrings.Portrait).ClickAsync();

        await Expect(Page.Locator(".pane-size")).ToHaveTextAsync("375×667 · 2x");
    }

    [TestMethod, OnDesktops]
    public async Task TheDeviceFrameSwitch_Should_TakeTheFrameAwayFromTheView(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=iphone-se");

        await Expect(Page.Locator(".device.framed")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Switch, new() { Name = WorkbenchStrings.DeviceFrame }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("frame=false"));
        await Expect(Page.Locator(".device.bare")).ToBeVisibleAsync();
        await Expect(Page.Locator(".device.framed")).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task AZoom_Should_ScaleTheView(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=iphone-se");

        await Button("0.5").ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("zoom=0.5"));
        await Expect(Page.Locator(".pane-scaler")).ToHaveCSSAsync("--wb-scale", "0.5");

        await Button(WorkbenchStrings.ZoomFit).ClickAsync();

        await Expect(Page).Not.ToHaveURLAsync(new Regex("zoom="));
    }

    [TestMethod, OnDesktops]
    public async Task AddingAndRemovingViews_Should_ChangeTheCanvas(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Button(WorkbenchStrings.AddView).ClickAsync();
        await WaitForViews();

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(2);
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "iPad Pro 11\"" })).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"\?panes="));

        await Page.GetByRole(AriaRole.Group, new() { Name = "iPad Pro 11\"" }).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.RemoveView }).ClickAsync();

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(1);
        await Expect(Button(WorkbenchStrings.RemoveView)).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task SelectingAView_Should_PointTheToolsAtIt(WorkbenchSurface surface)
    {
        var panes = WorkbenchUrls.EncodePanes([new() { Device = "iphone-se" }, new() { Device = "ipad-mini" }]);

        await OpenWorkbench("theme", $"?panes={panes}");

        var first = Page.GetByRole(AriaRole.Group, new() { Name = "iPhone SE" });
        var second = Page.GetByRole(AriaRole.Group, new() { Name = "iPad mini" });

        await first.Locator(".pane-header").ClickAsync();
        await Expect(first).ToHaveAttributeAsync("aria-current", "true");

        await second.Locator(".pane-header").ClickAsync();
        await Expect(second).ToHaveAttributeAsync("aria-current", "true");
        await Expect(first).Not.ToHaveAttributeAsync("aria-current", "true");

        await Button(WorkbenchStrings.Rotate).Nth(1).ClickAsync();

        await Expect(second.Locator(".pane-size")).ToContainTextAsync("1133×744");
    }

    [TestMethod, OnDesktops]
    public async Task ASettingChangedWithSeveralViews_Should_ReachEveryView(WorkbenchSurface surface)
    {
        var panes = WorkbenchUrls.EncodePanes([new() { Device = "iphone-se" }, new() { Device = "ipad-mini" }]);

        await OpenWorkbench("theme", $"?panes={panes}");

        await Button(WorkbenchStrings.ThemeDark).ClickAsync();
        await WaitForViews();

        var ids = await Page.Locator("iframe[data-pane-id]").EvaluateAllAsync<string[]>("frames => frames.map(f => f.dataset.paneId)");

        Assert.HasCount(2, ids);

        foreach (var id in ids)
        {
            await Expect(View(id).Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        }

        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "iPhone SE" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "iPad mini" })).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task CompareDevices_Should_ShowAPhoneATabletAndADesktop(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Button(WorkbenchStrings.Compare).ClickAsync();
        await Page.GetByText(WorkbenchStrings.CompareDevices, new() { Exact = true }).ClickAsync();
        await WaitForViews();

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(3);

        foreach (var device in new[] { DevicePresets.Default.Name, "iPad Pro 11\"", "Desktop 1536×864" })
        {
            await Expect(Page.GetByRole(AriaRole.Group, new() { Name = device, Exact = true })).ToBeVisibleAsync();
        }

        await Button(WorkbenchStrings.Compare).ClickAsync();
        await Page.GetByText(WorkbenchStrings.CompareSingle, new() { Exact = true }).ClickAsync();

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(1);
    }

    [TestMethod, OnDesktops]
    public async Task CompareStates_Should_ShowEveryDataStateSideBySide(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");

        await Button(WorkbenchStrings.Compare).ClickAsync();
        await Page.GetByText(WorkbenchStrings.CompareStates, new() { Exact = true }).ClickAsync();
        await WaitForViews();

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(4);
        await Expect(Page.Locator(".pane-chip")).ToHaveTextAsync([WorkbenchStrings.DataLoading, WorkbenchStrings.DataEmpty, WorkbenchStrings.DataError]);

        var panes = await Page.Locator("iframe[data-pane-id]").EvaluateAllAsync<string[]>("frames => frames.map(f => f.dataset.paneId)");

        await Expect(View(panes[2]).GetByText(WorkbenchStrings.BuildingsEmptyTitle)).ToBeVisibleAsync();
        await Expect(View(panes[3]).GetByText(WorkbenchStrings.BuildingsFailedTitle)).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    [TestCategory("Localization")]
    public async Task CompareLanguages_Should_ShowTheViewInEachLanguage(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Button(WorkbenchStrings.Compare).ClickAsync();
        await Page.GetByText(WorkbenchStrings.CompareLanguages, new() { Exact = true }).ClickAsync();

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(3);
        await WaitForViews();

        var panes = await Page.Locator("iframe[data-pane-id]").EvaluateAllAsync<string[]>("frames => frames.map(f => f.dataset.paneId)");

        await Expect(View(panes[0]).Locator("html")).Not.ToHaveAttributeAsync("dir", "rtl");
        await Expect(View(panes[1]).Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(View(panes[2]).Locator("html")).Not.ToHaveAttributeAsync("lang", new Regex("^en"));
    }

    [TestMethod, OnDesktops]
    public async Task CompareAccounts_Should_ShowEachSignInStateSideBySide(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Button(WorkbenchStrings.Compare).ClickAsync();
        await Page.GetByText(WorkbenchStrings.CompareSignIn, new() { Exact = true }).ClickAsync();

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(3);
        await WaitForViews();

        var panes = await Page.Locator("iframe[data-pane-id]").EvaluateAllAsync<string[]>("frames => frames.map(f => f.dataset.paneId)");

        await Expect(View(panes[0]).GetByRole(AriaRole.Button, new() { Name = AppStrings.AccountMenuTitle }).First).ToBeVisibleAsync();
        await Expect(View(panes[1]).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DemoUserYas }).First).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task ComparingAPagesDesignOptionsAndSwitches_Should_ShowOneViewForEach(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");

        await Button(WorkbenchStrings.Compare).ClickAsync();
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = WorkbenchStrings.CompareVariants, Exact = true }).ClickAsync();
        await WaitForViews();

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(2);

        await Button(WorkbenchStrings.Compare).ClickAsync();
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = WorkbenchStrings.BuildingsSwitchAmount, Exact = true }).ClickAsync();
        await WaitForViews();

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(3);
    }

    [TestMethod, OnDesktops]
    public async Task ALinkWithSeveralViews_Should_OpenThoseViews(WorkbenchSurface surface)
    {
        var panes = WorkbenchUrls.EncodePanes([new() { Device = "iphone-se" }, new() { Device = "ipad-mini", Theme = WorkbenchThemeMode.Dark }]);

        await OpenWorkbench("theme", $"?panes={panes}");

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(2);
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "iPhone SE" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "iPad mini" }).Locator(".pane-chip")).ToHaveTextAsync(WorkbenchStrings.ThemeDark);
    }

    [TestMethod, OnDesktops]
    public async Task ABreakpoint_Should_SnapTheViewToItsWidth(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=desktop-1536");

        var breakpoints = Page.GetByRole(AriaRole.Group, new() { Name = WorkbenchStrings.Breakpoints }).GetByRole(AriaRole.Button);

        await Expect(breakpoints.First).ToBeVisibleAsync();
        await Expect(Page.Locator(".breakpoint[aria-pressed='true']")).ToHaveCountAsync(1);

        await breakpoints.Nth(2).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"device=custom&size=\d+x864"));
        await Expect(Page.Locator(".breakpoint").Nth(2)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod, OnDesktops]
    public async Task CopyLink_Should_PutAnAddressThatOpensTheSameViewsOnTheClipboard(WorkbenchSurface surface)
    {
        await Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);

        await OpenWorkbench("theme", "?theme=dark");

        await Button(WorkbenchStrings.CopyLink).ClickAsync();

        await WorkbenchTestReviewer.WaitFor(() => Page.EvaluateAsync<string>("() => navigator.clipboard.readText()"), text => text.EndsWith("/workbench/theme?theme=dark", StringComparison.Ordinal));
    }

    [TestMethod, OnDesktops]
    public async Task OpenInANewTab_Should_ShowThePageAloneInTheViewsSettings(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?theme=dark");

        var popup = await Page.RunAndWaitForPopupAsync(() => Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.OpenInNewTab }).Or(Button(WorkbenchStrings.OpenInNewTab)).First.ClickAsync());

        await Expect(popup).ToHaveURLAsync(new Regex(@"/preview/theme\?theme=dark"));
        await Expect(popup.Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
    }

    [TestMethod, OnDesktops]
    public async Task TheSideColumnsTabs_Should_MoveWithTheArrowKeys(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Page.Locator("#wb-tab-view").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        await Expect(Page.Locator("#wb-tab-review")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page.Locator("#wb-tab-review")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("End");

        await Expect(Page.Locator("#wb-tab-flows")).ToHaveAttributeAsync("aria-selected", "true");

        await Page.Keyboard.PressAsync("ArrowRight");

        await Expect(Page.Locator("#wb-tab-view")).ToHaveAttributeAsync("aria-selected", "true");

        await Page.Keyboard.PressAsync("Home");
        await Page.Keyboard.PressAsync("ArrowLeft");

        await Expect(Page.Locator("#wb-tab-flows")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod, OnDesktops]
    public async Task ThePagesTitleInTheTopBar_Should_FollowThePage(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Expect(Page.Locator(".topbar").GetByText(WorkbenchStrings.ThemePageTitle)).ToBeVisibleAsync();

        await Page.Locator("#wb-navigator").GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.ShiftsTitle }).ClickAsync();

        await Expect(Page.Locator(".topbar").GetByText(WorkbenchStrings.ShiftsTitle)).ToBeVisibleAsync();
        await Expect(Page).ToHaveTitleAsync($"{WorkbenchStrings.ShiftsTitle} · {WorkbenchStrings.WorkbenchTitle}");
    }

    [TestMethod, OnPhones]
    public async Task OnAPhone_Should_ShowThePageOnTheWholeScreenWithAButtonForTheTools(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");

        var box = await Page.Locator("iframe[data-pane-id='1']").BoundingBoxAsync();

        Assert.AreEqual(0, box!.X);
        Assert.AreEqual(0, box.Y);
        Assert.AreEqual(Page.ViewportSize!.Width, box.Width);
        Assert.AreEqual(Page.ViewportSize.Height, box.Height);

        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.BuildingsHeading })).ToBeVisibleAsync();
        await Expect(Page.Locator(".workbench")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".pane-header")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".device.framed")).ToHaveCountAsync(0);
        await Expect(View().Locator(".status-bar")).ToHaveCountAsync(0);

        await OpenTools();

        await Expect(Tools.GetByText(WorkbenchStrings.DataStateHint)).ToBeVisibleAsync();
        await Expect(Tools.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.Device })).ToHaveCountAsync(0);

        if (HasAgent)
        {
            await Expect(Button(WorkbenchStrings.PinComment)).ToBeVisibleAsync();
        }
        else
        {
            await Expect(Button(WorkbenchStrings.PinComment)).ToHaveCountAsync(0);
        }

        await CloseTools();
    }

    [TestMethod, OnPhones]
    public async Task APhone_Should_TouchThePageWithItsOwnFingerAndNoMadeUpOne(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Expect(View().Locator("body")).Not.ToHaveClassAsync(new Regex("wb-touch"));
    }

    [TestMethod, OnPhones]
    public async Task ALinkWithADeviceOnAPhone_Should_KeepTheDeviceInTheAddressAndStillFillTheScreen(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=ipad-mini");

        var box = await Page.Locator("iframe[data-pane-id='1']").BoundingBoxAsync();

        Assert.AreEqual(Page.ViewportSize!.Width, box!.Width);

        await Press(View().Locator(".persona").First);
        await Press(View().GetByRole(AriaRole.Button, new() { Name = AppStrings.Light, Exact = true }));

        await Expect(Page).ToHaveURLAsync(new Regex(@"theme=dark&device=ipad-mini$"));
    }

    [TestMethod, OnPhones]
    public async Task ALinkWithSeveralViewsOnAPhone_Should_ShowTheSelectedOne(WorkbenchSurface surface)
    {
        var panes = WorkbenchUrls.EncodePanes([new() { Device = "iphone-se", Theme = WorkbenchThemeMode.Dark }, new() { Device = "ipad-mini" }]);

        await OpenWorkbench("theme", $"?panes={panes}");

        await Expect(Page.Locator("iframe[data-pane-id]")).ToHaveCountAsync(1);
        await Expect(Page.Locator("iframe[data-pane-id]").ContentFrame.Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
    }

    [TestMethod, OnPhones]
    public async Task TurningAPhoneSideways_Should_GiveItTheWideShellAndBack(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?theme=dark");

        await Page.SetViewportSizeAsync(915, 412);

        await Expect(Page.Locator(".workbench")).ToBeVisibleAsync();
        await WaitForViews();
        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));

        await Page.SetViewportSizeAsync(412, 915);

        await Expect(Page.Locator(".phone-layout")).ToBeVisibleAsync();
        await WaitForViews();
        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
    }

    [TestMethod, Surfaces(WorkbenchSurface.PhoneWithAgent)]
    public async Task ThePinBanner_Should_HoldItsTextOnAPhone(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenTools();
        await Press(Button(WorkbenchStrings.PinComment));

        var banner = Page.Locator(".pick-banner");

        await Expect(banner).ToBeVisibleAsync();

        var box = await banner.BoundingBoxAsync();
        var text = await banner.Locator("span").First.BoundingBoxAsync();

        Assert.IsLessThanOrEqualTo(box!.Y + box.Height, text!.Y + text.Height);
        Assert.IsGreaterThanOrEqualTo(box.Y, text.Y);
        Assert.IsLessThanOrEqualTo(Page.ViewportSize!.Width, box.X + box.Width);
        Assert.IsGreaterThan(Page.ViewportSize.Width / 2, box.Width);
    }

    [TestMethod, EverySurface]
    public async Task OpeningAPage_Should_LoadItsViewOnce(WorkbenchSurface surface)
    {
        List<string> loads = [];

        Page.Request += (_, request) =>
        {
            if (request.IsNavigationRequest && request.Url.Contains("/preview/", StringComparison.Ordinal))
            {
                loads.Add(request.Url);
            }
        };

        await OpenWorkbench("theme");

        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeHeading, Level = 2 }).First).ToBeVisibleAsync();

        Assert.HasCount(1, loads, string.Join(Environment.NewLine, loads));
    }

    [TestMethod, EverySurface]
    public async Task TheNoAgentNotice_Should_ShowOnlyWithoutAnAgent(WorkbenchSurface surface)
    {
        var notice = Page.GetByRole(AriaRole.Status).Filter(new() { HasText = WorkbenchStrings.AgentNotConnected });

        await Page.GotoAsync(Server.Address.ToString());
        await WaitUntilInteractive();
        await ExpectNotice();

        await OpenWorkbench("theme");
        await ExpectNotice();

        await Page.GotoAsync(new Uri(Server.Address, WorkbenchUrls.Flows).ToString());
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.Flows, Level = 1 })).ToBeVisibleAsync();
        await ExpectNotice();

        async Task ExpectNotice()
        {
            if (HasAgent)
            {
                await Expect(notice).ToHaveCountAsync(0);
            }
            else
            {
                await Expect(notice).ToBeVisibleAsync();
                await Expect(notice).ToContainTextAsync(WorkbenchStrings.LimitedExperience);
            }
        }
    }

    [TestMethod, EverySurface]
    public async Task ThePagesConsole_Should_StayFreeOfErrorsWhileItsSettingsChange(WorkbenchSurface surface)
    {
        List<string> errors = [];

        // The internet is blocked in these tests, so a failed request is only an error when the Workbench itself refuses it. Hot reload's settings come from dotnet watch, which doesn't run here.
        Page.Response += (_, response) =>
        {
            if (response.Status >= 400 && response.Url.Contains("hot-reload-settings", StringComparison.Ordinal) is false)
            {
                errors.Add($"{response.Status} {response.Url}");
            }
        };
        Page.PageError += (_, error) => errors.Add(error);

        await OpenWorkbench("theme");

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ThemeDark, Exact = true }));
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?theme=dark$"));
        await WaitForViews();
        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));

        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
    }
}
