namespace Boilerplate.WorkbenchTests.Features.Checks;

/// <summary>The Checks tab: an accessibility check of the selected view and a search for colors outside the theme. A desktop's; a phone's tools don't offer it.</summary>
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

    private async Task OpenChecks(string pageKey = "theme", string query = "")
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
        await OpenChecks("theme", $"?panes={WorkbenchUrls.EncodePanes([new() { Device = "iphone-se" }, new() { Device = "iphone-se", Theme = WorkbenchThemeMode.Dark }])}");

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
