namespace Boilerplate.WorkbenchTests.Features.Checks;

/// <summary>
/// The Checks tab: an accessibility check of the selected view and of each tab, panel and menu it opens, and a search for
/// colors outside the theme, with a finding sent to the agent or all of them exported. A desktop's; a phone's tools don't
/// offer it.
/// </summary>
[TestClass, TestCategory("UITest")]
public class ChecksUITests : WorkbenchPageTest
{
    private const string Axe = "**/scripts/axe.min.js";

    private ILocator Run => Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.RunChecks, Exact = true });

    private ILocator Panel => Page.Locator(".checks");

    private static Regex CountedTitle(string format) => new($"^{Regex.Escape(format).Replace(@"\{0}", @"\d+")}$");

    private ILocator AccessibilityTitle => Panel.GetByText(CountedTitle(WorkbenchStrings.AccessibilityFindings));

    private ILocator ThemeSection => Panel.Locator("section").Filter(new() { Has = Page.GetByText(CountedTitle(WorkbenchStrings.ThemeFindings)) });

    private ILocator Finding(string impact) => Panel.Locator(".finding").Filter(new() { Has = Page.Locator($".impact-{impact}") });

    private Task AddToTheView(string html, string paneId = "1") => View(paneId).Locator("section").Last.EvaluateAsync("(section, html) => section.insertAdjacentHTML('afterend', html)", html);

    private async Task OpenChecks(string pageKey = "pos-dashboard", string query = "")
    {
        await OpenWorkbench(pageKey, query);
        await OpenTab(WorkbenchModes.ChecksColumn);
    }

    private async Task RunTheChecks()
    {
        await Press(Run);
        await Expect(AccessibilityTitle).ToBeVisibleAsync(new() { Timeout = 60_000 });
    }

    [TestMethod, OnDesktops]
    public async Task TheChecksTab_Should_CheckTheSelectedView(WorkbenchSurface surface)
    {
        await OpenChecks("pos-dashboard", "?device=iphone-se");

        await Expect(Panel.GetByText(WorkbenchStrings.ChecksTitle, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Panel.GetByText(WorkbenchStrings.ChecksHint)).ToBeVisibleAsync();
        await Expect(AccessibilityTitle).ToHaveCountAsync(0);

        await RunTheChecks();

        await Expect(Page.GetByText(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ThemeFindings, 0))).ToBeVisibleAsync();
        await Expect(ThemeSection.GetByText(WorkbenchStrings.ThemeFindingsHint)).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task TheChecks_Should_ShowTheyAreRunningUntilTheyEnd(WorkbenchSurface surface)
    {
        var release = new TaskCompletionSource();

        await Page.RouteAsync(Axe, async route =>
        {
            await release.Task;
            await route.ContinueAsync();
        });

        await OpenChecks();

        await Press(Run);

        await Expect(Run).ToBeDisabledAsync();
        await Expect(Panel.Locator(".run .bit-ldn-rng")).ToBeVisibleAsync();

        release.SetResult();

        await Expect(AccessibilityTitle).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Expect(Run).ToBeEnabledAsync();
    }

    [TestMethod, OnDesktops]
    public async Task AFinding_Should_ShowItsImpactCountAndAWayToLearnMore(WorkbenchSurface surface)
    {
        await OpenChecks();

        await AddToTheView("<button class='nameless'></button><button class='nameless'></button>");

        await RunTheChecks();

        var finding = Finding("critical");

        await Expect(finding.Locator(".impact")).ToHaveTextAsync(WorkbenchStrings.ImpactCritical);
        await Expect(finding.Locator(".count")).ToHaveTextAsync("× 2");
        await Expect(finding.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowOnPage })).ToBeVisibleAsync();

        var learnMore = finding.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.LearnMore });

        await Expect(learnMore).ToHaveAttributeAsync("href", new Regex("button-name"));
        await Expect(learnMore).ToHaveAttributeAsync("target", "_blank");
    }

    [TestMethod, OnDesktops]
    public async Task TheFindings_Should_ComeMostSevereFirstUnderTheirCount(WorkbenchSurface surface)
    {
        await OpenChecks();

        await AddToTheView("<h3 class='empty'></h3><a class='nameless' href='#'></a><button class='nameless'></button>");

        await RunTheChecks();

        var impacts = await Panel.Locator(".finding .impact").AllInnerTextsAsync();
        string[] order = [WorkbenchStrings.ImpactCritical, WorkbenchStrings.ImpactSerious, WorkbenchStrings.ImpactModerate, WorkbenchStrings.ImpactMinor];

        Assert.IsGreaterThan(1, impacts.Count);
        Assert.AreEqual(WorkbenchStrings.ImpactCritical, impacts[0]);
        Assert.AreSequenceEqual(impacts.OrderBy(impact => Array.IndexOf(order, impact)).ToArray(), impacts.ToArray());
        await Expect(AccessibilityTitle).ToHaveTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.AccessibilityFindings, impacts.Count));
    }

    [TestMethod, OnDesktops]
    public async Task ShowOnPage_Should_ScrollToTheElementAndFlashIt(WorkbenchSurface surface)
    {
        await OpenChecks();

        await AddToTheView("<button id='nameless'></button>");

        await RunTheChecks();

        var element = View().Locator("#nameless");

        await View().Locator(".brand, h1, header").First.EvaluateAsync("e => e.scrollIntoView()");

        await Press(Finding("critical").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowOnPage }));

        await WorkbenchTestReviewer.WaitFor(() => element.EvaluateAsync<int>("e => e.getAnimations().length"), count => count > 0);
        await Expect(element).ToBeInViewportAsync();
    }

    [TestMethod, OnDesktops]
    public async Task AColorOutsideTheTheme_Should_BeFoundWithItsSwatchAndWhereItIs(WorkbenchSurface surface)
    {
        await OpenChecks();

        await AddToTheView("""
            <div class='sample' style='color: #ff00ff'>Own text</div>
            <div class='sample' style='color: #ff00ff'>More text</div>
            <div class='sample' style='background-color: #00ff00'>On a ground</div>
            <div class='sample' style='border: 1px solid #00ffff'>In a box</div>
            """);

        await RunTheChecks();

        var findings = ThemeSection.Locator(".finding");

        await Expect(ThemeSection.GetByText(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ThemeFindings, 3))).ToBeVisibleAsync();
        await Expect(findings).ToHaveCountAsync(3);

        var text = findings.Filter(new() { HasText = "color rgb(255, 0, 255)" });

        await Expect(text.Locator(".count")).ToHaveTextAsync("× 2");
        await Expect(text.Locator(".example")).ToHaveTextAsync("div.sample “Own text”");
        await Expect(text.Locator(".swatch")).ToHaveAttributeAsync("style", new Regex(@"--wb-swatch:\s*rgb\(255, 0, 255\)"));
        await Expect(findings.Filter(new() { HasText = "background-color rgb(0, 255, 0)" })).ToHaveCountAsync(1);
        await Expect(findings.Filter(new() { HasText = "border-color rgb(0, 255, 255)" })).ToHaveCountAsync(1);
    }

    [TestMethod, OnDesktops]
    public async Task TheSearchForColors_Should_LeaveOutComponentsAndDataColors(WorkbenchSurface surface)
    {
        await OpenChecks();

        await AddToTheView("""
            <div class='bit-sample' style='color: #ff00ff'>A component's own text</div>
            <div style='--data-color: #00ff00; background-color: #00ff00'>A chart's color</div>
            <div style='color: var(--bit-clr-pri)'>The theme's color</div>
            <div style='background-color: color-mix(in srgb, var(--bit-clr-pri) 30%, white)'>A mix of the theme's color</div>
            """);

        await RunTheChecks();

        await Expect(ThemeSection.GetByText(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ThemeFindings, 0))).ToBeVisibleAsync();
        await Expect(ThemeSection.Locator(".finding")).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task AViewWithoutFindings_Should_SaySo(WorkbenchSurface surface)
    {
        await Page.RouteAsync(Axe, route => route.FulfillAsync(new() { ContentType = "text/javascript", Body = "window.axe = { run: async () => ({ violations: [] }) };" }));

        await OpenChecks();

        await RunTheChecks();

        await Expect(AccessibilityTitle).ToHaveTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.AccessibilityFindings, 0));
        await Expect(Panel.GetByText(WorkbenchStrings.NoAccessibilityFindings)).ToBeVisibleAsync();
        await Expect(Panel.Locator(".finding .impact")).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task ChecksThatCanNotRun_Should_SaySoAndWorkWhenTriedAgain(WorkbenchSurface surface)
    {
        await Page.RouteAsync(Axe, route => route.AbortAsync());

        await OpenChecks();

        await Press(Run);

        await Expect(Panel.GetByText(WorkbenchStrings.ChecksFailed)).ToBeVisibleAsync();
        await Expect(AccessibilityTitle).ToHaveCountAsync(0);
        await Expect(Run).ToBeEnabledAsync();

        await Page.UnrouteAsync(Axe);

        await RunTheChecks();

        await Expect(Panel.GetByText(WorkbenchStrings.ChecksFailed)).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task AFindingsResult_Should_GoWhenTheViewChanges(WorkbenchSurface surface)
    {
        await OpenChecks();

        await RunTheChecks();

        await Press(Page.Locator(".pane-header").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Rotate }));

        await Expect(AccessibilityTitle).ToHaveCountAsync(0);
        await Expect(Run).ToBeEnabledAsync();
    }

    [TestMethod, OnDesktops]
    public async Task TheChecks_Should_RunOnTheSelectedViewOnly(WorkbenchSurface surface)
    {
        await OpenChecks("pos-dashboard", $"?panes={WorkbenchUrls.EncodePanes([new() { Device = "iphone-se" }, new() { Device = "iphone-se", Theme = WorkbenchThemeMode.Dark }])}");

        var panes = await Page.Locator("iframe[data-pane-id]").EvaluateAllAsync<string[]>("frames => frames.map(f => f.dataset.paneId)");

        await AddToTheView("<button class='nameless'></button>", panes[1]);

        await Press(Page.Locator(".pane").Nth(1).Locator(".pane-header .pane-device"));
        await Expect(Page.Locator(".pane").Nth(1)).ToHaveAttributeAsync("aria-current", "true");

        await RunTheChecks();
        await Expect(Finding("critical")).ToHaveCountAsync(1);

        await Press(Page.Locator(".pane").Nth(0).Locator(".pane-header .pane-device"));
        await Expect(Page.Locator(".pane").Nth(0)).ToHaveAttributeAsync("aria-current", "true");

        await Expect(AccessibilityTitle).ToHaveCountAsync(0);

        await RunTheChecks();
        await Expect(Finding("critical")).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task AStoreViewsChecks_Should_CoverThePageInItsScreen(WorkbenchSurface surface)
    {
        await OpenChecks("pos-dashboard", "?device=store-play-phone&backdrop=ocean");

        var screen = View().FrameLocator("iframe.screen-frame");

        await screen.Locator("section").Last.EvaluateAsync("section => section.insertAdjacentHTML('afterend', `<button id='nameless'></button><div style='color: #ff00ff'>Own text</div>`)");

        await RunTheChecks();

        await Expect(Finding("critical").Locator(".count")).ToHaveTextAsync("× 1");
        await Expect(ThemeSection.GetByText(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ThemeFindings, 1))).ToBeVisibleAsync();

        await Press(Finding("critical").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowOnPage }));

        await WorkbenchTestReviewer.WaitFor(() => screen.Locator("#nameless").EvaluateAsync<int>("e => e.getAnimations().length"), count => count > 0);
    }

    [TestMethod, OnDesktops]
    public async Task EveryTab_Should_BeCheckedInTurnAndNamedOnItsFindings(WorkbenchSurface surface)
    {
        // A stand-in checker names the section of the tab it finds shown, so each tab leaves a finding of its own.
        await Page.RouteAsync(Axe, route => route.FulfillAsync(new()
        {
            ContentType = "text/javascript",
            Body = """
                window.axe = { run: async () => {
                    const section = document.querySelector('.theme-section[id]');
                    return { violations: section ? [{ id: 'stand-in', impact: 'serious', help: section.id, helpUrl: 'https://example.com', nodes: [{ target: [`#${section.id}`] }] }] : [] };
                } };
                """
        }));

        await OpenChecks("theme", "?sw-section=pickers");

        var address = Page.Url;

        await Press(Run);

        await Expect(AccessibilityTitle).ToBeVisibleAsync(new() { Timeout = 300_000 });

        // The tab shown at the start is checked first, then every other one in turn.
        var tabs = WorkbenchPageCatalog.Find("theme")!.Switches.Single(pageSwitch => pageSwitch.Key == "section").Options;
        var inTurn = tabs.Where(tab => tab.Value == "pickers").Concat(tabs.Where(tab => tab.Value != "pickers")).ToArray();
        var findings = Panel.Locator(".finding");

        await Expect(findings.Locator(".where")).ToHaveTextAsync([.. inTurn.Select(tab => WorkbenchStrings.ResourceManager.GetString(tab.TextKey, CultureInfo.InvariantCulture)!)]);
        await Expect(findings.Locator(".finding-text")).ToContainTextAsync([.. inTurn.Select(tab => tab.Value)]);

        // The view is back on the tab it showed, and its address never followed the tabs the checks went through.
        await Expect(View().GetByRole(AriaRole.Tab, new() { Name = WorkbenchStrings.ThemePickers, Exact = true })).ToHaveAttributeAsync("aria-selected", "true");
        Assert.AreEqual(address, Page.Url);

        // Showing a finding of another tab picks that tab, as the user would, and the result stays.
        await Press(findings.Filter(new() { HasText = "inputs" }).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowOnPage }));

        await Expect(View().GetByRole(AriaRole.Tab, new() { Name = WorkbenchStrings.ThemeInputs, Exact = true })).ToHaveAttributeAsync("aria-selected", "true");
        await WorkbenchTestReviewer.WaitFor(() => View().Locator("#inputs").EvaluateAsync<int>("e => e.getAnimations().length"), count => count > 0);
        await Expect(Page).ToHaveURLAsync(new Regex("sw-section=inputs"));
        await Expect(findings).ToHaveCountAsync(tabs.Length);
    }

    [TestMethod, OnDesktops]
    public async Task ThePanelsAndMenusOfAPage_Should_BeOpenedInTurnAndClosedAgain(WorkbenchSurface surface)
    {
        // A stand-in checker finds each tab shown and each part opened, so each state leaves a finding where it opened.
        await Page.RouteAsync(Axe, route => route.FulfillAsync(new()
        {
            ContentType = "text/javascript",
            Body = """
                window.axe = { run: async () => {
                    const place = element => element === document.body ? 'body' : `${place(element.parentElement)} > :nth-child(${Array.from(element.parentElement.children).indexOf(element) + 1})`;
                    const open = Array.from(document.querySelectorAll('[role="tab"][aria-selected="true"], [aria-expanded="true"]'));
                    return { violations: [{ id: 'stand-in', impact: 'serious', help: 'Open', helpUrl: 'https://example.com', nodes: open.map(element => ({ target: [place(element)] })) }] };
                } };
                """
        }));

        // On a desktop, where the account section shows all of its tabs instead of putting some in a menu.
        await OpenChecks("settings", "?device=desktop-1536");

        const string openParts = "elements => elements.map(element => element.id || element.outerHTML.slice(0, 60))";
        var openBefore = await View().Locator("[aria-expanded=true]").EvaluateAllAsync<string[]>(openParts);

        await RunTheChecks();

        var account = AppStrings.AccountTitle;
        var password = $"{account} › {AppStrings.Password}";
        var where = await Panel.Locator(".finding .where").AllInnerTextsAsync();

        foreach (var part in new[] { account, $"{account} › {AppStrings.Phone}", password, AppStrings.TfaTitle })
        {
            Assert.Contains(part, where);
        }

        // What was open at the start, like the first section of the accordion, is open again, and nothing else is.
        await WorkbenchTestReviewer.WaitFor(() => View().Locator("[aria-expanded=true]").EvaluateAllAsync<string[]>(openParts), open => open.SequenceEqual(openBefore));

        // Showing a finding opens the parts it was found in.
        var finding = Panel.Locator(".finding").Filter(new() { Has = Page.Locator(".where", new() { HasTextString = password }) });

        await Press(finding.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowOnPage }));

        await Expect(View().GetByRole(AriaRole.Tab, new() { Name = AppStrings.Password, Exact = true })).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AskTheAgent_Should_PinTheFindingOnItsElementAsAComment(WorkbenchSurface surface)
    {
        await OpenChecks();

        await AddToTheView("<button id='nameless'></button>");

        await RunTheChecks();

        var finding = Finding("critical");

        await Press(finding.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AskTheAgent }));

        await Expect(finding.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ChecksAsked })).ToBeDisabledAsync();

        static bool IsAsked(WorkbenchReviewThread thread) => thread.PageKey == "pos-dashboard" && thread.Messages[0].Text.StartsWith(WorkbenchStrings.ChecksAskAccessibility, StringComparison.Ordinal);

        var threads = await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Any(IsAsked));
        var thread = threads.Last(IsAsked);

        Assert.Contains("(button-name)", thread.Messages[0].Text);
        Assert.Contains("<button id=\"nameless\"></button>", thread.Messages[0].Text);

        // The comment is pinned on the element itself.
        Assert.AreEqual("nameless", await View().Locator(thread.Anchor.Selector).GetAttributeAsync("id"));

        // The Checks tab stays open for the next finding.
        await Expect(AccessibilityTitle).ToBeVisibleAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithoutAgent)]
    public async Task WithoutAnAgent_Should_NotOfferToAskIt(WorkbenchSurface surface)
    {
        await OpenChecks();

        await AddToTheView("<button id='nameless'></button>");

        await RunTheChecks();

        await Expect(Finding("critical").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowOnPage })).ToBeVisibleAsync();
        await Expect(Panel.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AskTheAgent })).ToHaveCountAsync(0);
        await Expect(Panel.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ChecksExportAll })).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task ExportAll_Should_CopyAPromptWithEveryFindingAndWhereItIs(WorkbenchSurface surface)
    {
        await Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);

        await OpenChecks();

        await AddToTheView("<button id='nameless'></button><div style='color: #ff00ff'>Own text</div>");

        await RunTheChecks();

        await Press(Panel.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ChecksExportAll }));

        await Expect(Panel.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ChecksExported })).ToBeVisibleAsync();

        var title = WorkbenchStrings.ResourceManager.GetString(WorkbenchPageCatalog.Find("pos-dashboard")!.TitleKey, CultureInfo.InvariantCulture)!;
        var prompt = await WorkbenchTestReviewer.WaitFor(() => Page.EvaluateAsync<string>("() => navigator.clipboard.readText()"), text => text.StartsWith("# ", StringComparison.Ordinal));

        Assert.StartsWith($"# {string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ChecksPromptTitle, title)}", prompt);
        Assert.Contains("`/api/checks/pos-dashboard", prompt);
        Assert.Contains("(`button-name`)", prompt);
        Assert.Contains("`<button id=\"nameless\"></button>`", prompt);
        Assert.Contains("`color: rgb(255, 0, 255)` × 1", prompt);
    }

    [TestMethod, OnDesktops]
    public async Task ExportAll_Should_SaveAFileWhenTheClipboardIsRefused(WorkbenchSurface surface)
    {
        await Context.AddInitScriptAsync("navigator.clipboard.writeText = () => Promise.reject(new Error('Refused'))");

        await OpenChecks();

        await AddToTheView("<button id='nameless'></button>");

        await RunTheChecks();

        var download = await Page.RunAndWaitForDownloadAsync(() => Press(Panel.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ChecksExportAll })));

        Assert.AreEqual("pos-dashboard-checks.md", download.SuggestedFilename);
        Assert.Contains("(`button-name`)", await File.ReadAllTextAsync(await download.PathAsync()));
    }

    [TestMethod, OnPhones]
    public async Task APhone_Should_NotOfferTheChecks(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenTools();

        await Expect(Tools.GetByText(WorkbenchStrings.PageStatus, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Tab, new() { Name = WorkbenchStrings.ChecksTab })).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.RunChecks })).ToHaveCountAsync(0);
    }
}
