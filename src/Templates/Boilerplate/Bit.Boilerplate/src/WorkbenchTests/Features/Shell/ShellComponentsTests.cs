using Bunit;

namespace Boilerplate.WorkbenchTests.Features.Shell;

/// <summary>The page list and the View tab, rendered in memory.</summary>
[TestClass, TestCategory("UITest")]
public class ShellComponentsTests
{
    private static string[] PageLinks(IRenderedComponent<PageNavigator> cut) =>
        [.. cut.FindAll("a.page-link").Select(a => a.GetAttribute("href")!).Where(href => href.StartsWith("/workbench/", StringComparison.Ordinal))];

    [TestMethod]
    public async Task ThePageList_Should_ListEveryPageAndKeepTheViewsInItsLinks()
    {
        await using var ctx = WorkbenchBunit.Create();

        var store = ctx.Services.GetRequiredService<WorkbenchStore>();
        store.ChangeScenario(new WorkbenchScenario { Theme = WorkbenchThemeMode.Dark, Variant = "cards" }.WithSwitch("amount", "few"));

        var cut = ctx.Render<PageNavigator>(p => p.Add(x => x.CurrentKey, "buildings").Add(x => x.Query, store.QueryString));

        var links = PageLinks(cut);

        Assert.HasCount(WorkbenchPageCatalog.Pages.Count, links);
        Assert.Contains("/workbench/buildings?theme=dark&variant=cards&sw-amount=few", links, "The current page keeps its design option and switches.");
        Assert.Contains("/workbench/theme?theme=dark", links, "Another page gets the same views without them.");
        Assert.AreEqual("page", cut.Find("a.page-link.active").GetAttribute("aria-current"));
        Assert.Contains("/flows", cut.FindAll("a.page-link").Select(a => a.GetAttribute("href")).ToArray());
    }

    [TestMethod]
    public async Task AFlowsTabWithoutFlowsOfItsOwn_Should_OfferTheSamplesWithoutAskingARemoteViewerForAnAgent()
    {
        await using var ctx = WorkbenchBunit.Create(isLocal: false);

        var cut = ctx.Render<FlowsPanel>(p => p.Add(x => x.PageKey, "shifts"));

        Assert.Contains(WorkbenchStrings.SampleFlowBuildings, cut.Markup);
        Assert.DoesNotContain(WorkbenchStrings.NoFlowsYet, cut.Markup);
        Assert.DoesNotContain(WorkbenchStrings.FlowsEmpty, cut.Markup);
        Assert.DoesNotContain(WorkbenchStrings.AddToFlow, cut.Markup);
    }

    [TestMethod]
    [DataRow("bortable", "pos-dashboard")]
    [DataRow("MAP", "buildings")]
    [DataRow("BitFullCalendar", "shifts")]
    [DataRow("Theme and", "theme")]
    public async Task SearchingThePageList_Should_MatchTitlesDescriptionsAndComponents(string search, string expected)
    {
        await using var ctx = WorkbenchBunit.Create();

        var cut = ctx.Render<PageNavigator>(p => p.Add(x => x.CurrentKey, "theme"));

        cut.Find($"input[aria-label='{WorkbenchStrings.SearchPages}']").Input(search);

        cut.WaitForAssertion(() =>
        {
            var links = PageLinks(cut);

            Assert.Contains(link => link.StartsWith($"/workbench/{expected}", StringComparison.Ordinal), links);
            Assert.IsLessThan(WorkbenchPageCatalog.Pages.Count, links.Length);
            Assert.DoesNotContain(WorkbenchStrings.AllFlows, cut.Markup, "The flows hide while searching.");
        });
    }

    [TestMethod]
    public async Task TheViewTab_Should_SendEveryChangeAsANewScenario()
    {
        await using var ctx = WorkbenchBunit.Create();

        WorkbenchScenario? changed = null;
        var cut = ctx.Render<ScenarioInspector>(p => p
            .Add(x => x.Page, WorkbenchPageCatalog.Find("buildings"))
            .Add(x => x.Value, new WorkbenchScenario())
            .Add(x => x.ValueChanged, (WorkbenchScenario scenario) => changed = scenario));

        ButtonNamed(cut, WorkbenchStrings.DataEmpty).Click();
        Assert.AreEqual(WorkbenchDataState.Empty, changed!.Data);

        ButtonNamed(cut, WorkbenchStrings.ThemeDark).Click();
        Assert.AreEqual(WorkbenchThemeMode.Dark, changed!.Theme);

        ButtonNamed(cut, WorkbenchStrings.SignedOut).Click();
        Assert.AreEqual(WorkbenchAuthState.SignedOut, changed!.Auth);

        ButtonNamed(cut, WorkbenchStrings.Landscape).Click();
        Assert.IsTrue(changed!.Rotated);

        ButtonNamed(cut, "130%").Click();
        Assert.AreEqual(130, changed!.TextScale);

        ButtonNamed(cut, WorkbenchStrings.BuildingsVariantCards).Click();
        Assert.AreEqual("cards", changed!.Variant);

        ButtonNamed(cut, WorkbenchStrings.BuildingsAmountFew).Click();
        Assert.AreEqual("few", changed!.GetSwitch("amount"));
    }

    [TestMethod]
    public async Task TheDefaultOption_Should_LeaveItsKeyOutOfTheScenario()
    {
        await using var ctx = WorkbenchBunit.Create();

        WorkbenchScenario? changed = null;
        var cut = ctx.Render<ScenarioInspector>(p => p
            .Add(x => x.Page, WorkbenchPageCatalog.Find("buildings"))
            .Add(x => x.Value, new WorkbenchScenario { Variant = "cards" }.WithSwitch("amount", "few"))
            .Add(x => x.ValueChanged, (WorkbenchScenario scenario) => changed = scenario));

        Assert.AreEqual("true", ButtonNamed(cut, WorkbenchStrings.BuildingsVariantCards).GetAttribute("aria-pressed"));

        ButtonNamed(cut, WorkbenchStrings.BuildingsVariantTable).Click();
        Assert.IsNull(changed!.Variant);

        ButtonNamed(cut, WorkbenchStrings.BuildingsAmountNormal).Click();
        Assert.IsNull(changed!.GetSwitch("amount"));
    }

    [TestMethod]
    public async Task ThePageSection_Should_ShowOnlyForAPageThatHasOptions()
    {
        await using var ctx = WorkbenchBunit.Create();

        var buildings = ctx.Render<ScenarioInspector>(p => p.Add(x => x.Page, WorkbenchPageCatalog.Find("buildings")));
        var shifts = ctx.Render<ScenarioInspector>(p => p.Add(x => x.Page, WorkbenchPageCatalog.Find("shifts")));

        Assert.Contains(WorkbenchStrings.PageSection, buildings.Markup);
        Assert.DoesNotContain(WorkbenchStrings.PageSection, shifts.Markup);
    }

    [TestMethod]
    public async Task AStoreSize_Should_ShowThatStoresRules()
    {
        await using var ctx = WorkbenchBunit.Create();

        var cut = ctx.Render<ScenarioInspector>(p => p.Add(x => x.Value, new WorkbenchScenario { Device = "store-play-phone" }));

        Assert.Contains(WorkbenchStrings.StoreRuleGooglePlay, cut.Markup);

        cut.Render(p => p.Add(x => x.Value, new WorkbenchScenario { Device = "store-microsoft" }));

        Assert.Contains(WorkbenchStrings.StoreRuleMicrosoftStore, cut.Markup);
    }

    [TestMethod]
    public async Task TheAccentSwatches_Should_TryAColorAndGoBackToTheTheme()
    {
        await using var ctx = WorkbenchBunit.Create();

        WorkbenchScenario? changed = null;
        var cut = ctx.Render<ScenarioInspector>(p => p
            .Add(x => x.Value, new WorkbenchScenario { Accent = "#123456" })
            .Add(x => x.ValueChanged, (WorkbenchScenario scenario) => changed = scenario));

        var swatches = cut.FindAll("div.accents button[role='radio']");

        Assert.IsGreaterThan(2, swatches.Count);
        Assert.AreEqual("false", swatches[0].GetAttribute("aria-checked"));

        swatches[1].Click();
        Assert.IsNotNull(changed);
        Assert.IsNotNull(WorkbenchScenario.ReadAccent(changed.Accent));

        cut.FindAll("div.accents button[role='radio']")[0].Click();
        Assert.IsNull(changed!.Accent, "The first swatch is the theme's own primary.");
    }

    [TestMethod]
    public async Task ACustomSize_Should_OfferItsWidthAndHeight()
    {
        await using var ctx = WorkbenchBunit.Create();

        var cut = ctx.Render<ScenarioInspector>(p => p.Add(x => x.Value, new WorkbenchScenario().WithSize(900, 600)));

        Assert.Contains(WorkbenchStrings.CustomSizeHint, cut.Markup);
        Assert.HasCount(2, cut.FindAll(".size-fields input"));
    }

    private static AngleSharp.Dom.IElement ButtonNamed<T>(IRenderedComponent<T> cut, string text) where T : IComponent
    {
        return cut.FindAll("button").Single(b => b.TextContent.Trim() == text);
    }
}
