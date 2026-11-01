using Bunit;
using Bit.Blex.Blazor;
using Boilerplate.Workbench.Components.Preview;
using Microsoft.AspNetCore.Components.Authorization;

namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>
/// Every showcase in every data state, rendered in memory the way a view renders it: each state shows what the real
/// page will show, a message and a way forward when empty, and a retry that works on error.
/// </summary>
[TestClass, TestCategory("UITest")]
public class ShowcaseStatesTests
{
    private static IRenderedComponent<BlexProvider> Render(BunitContext ctx, string pageKey, WorkbenchScenario scenario)
    {
        ctx.Services.GetRequiredService<WorkbenchScenarioState>().Set(scenario);

        return ctx.Render<BlexProvider>(p => p.AddChildContent<CascadingAuthenticationState>(auth => auth
            .AddChildContent<PreviewPageView>(view => view.Add(x => x.Page, WorkbenchPageCatalog.Find(pageKey)!))));
    }

    private static readonly TimeSpan timeout = TimeSpan.FromSeconds(10);

    [TestMethod]
    [DataRow("buildings", nameof(WorkbenchStrings.BuildingsFailedTitle))]
    [DataRow("pos-dashboard", nameof(WorkbenchStrings.PosWidgetFailedTitle))]
    [DataRow("shifts", nameof(WorkbenchStrings.LoadFailed))]
    [DataRow("assistant", nameof(WorkbenchStrings.AssistantHistoryFailed))]
    [DataRow("settings", nameof(WorkbenchStrings.LoadFailed))]
    public async Task Error_Should_ShowAMessageWithARetryThatWorks(string pageKey, string messageKey)
    {
        await using var ctx = WorkbenchBunit.Create();
        var message = WorkbenchStrings.ResourceManager.GetString(messageKey)!;

        var cut = Render(ctx, pageKey, new() { Data = WorkbenchDataState.Error });

        cut.WaitForAssertion(() => Assert.Contains(message, cut.Markup), timeout);

        var retries = cut.FindAll("button").Where(b => b.TextContent.Trim() is var text && (text == WorkbenchStrings.PosRetry || text == WorkbenchStrings.AssistantRetry)).ToArray();

        Assert.IsNotEmpty(retries, "Every error offers a retry.");

        var failures = Count(cut.Markup, message);

        retries[0].Click();

        cut.WaitForAssertion(() => Assert.AreEqual(failures - 1, Count(cut.Markup, message), "The retry loaded what had failed."), timeout);
    }

    [TestMethod]
    [DataRow("buildings", nameof(WorkbenchStrings.BuildingsEmptyTitle))]
    [DataRow("pos-dashboard", nameof(WorkbenchStrings.PosNoSalesYet))]
    [DataRow("shifts", nameof(WorkbenchStrings.ShiftsEmpty))]
    [DataRow("assistant", nameof(WorkbenchStrings.AssistantNoHistory))]
    public async Task Empty_Should_SayThereIsNothingYet(string pageKey, string messageKey)
    {
        await using var ctx = WorkbenchBunit.Create();

        var cut = Render(ctx, pageKey, new() { Data = WorkbenchDataState.Empty });

        cut.WaitForAssertion(() => Assert.Contains(WorkbenchStrings.ResourceManager.GetString(messageKey)!, cut.Markup), timeout);
    }

    [TestMethod]
    public async Task Loading_Should_ShowPlaceholdersAndNeverFinish()
    {
        await using var ctx = WorkbenchBunit.Create();

        var cut = Render(ctx, "pos-dashboard", new() { Data = WorkbenchDataState.Loading });

        cut.WaitForAssertion(() => Assert.HasCount(PosWidgetCatalog.All.Count, cut.FindAll($"[role='status'][aria-label='{WorkbenchStrings.PosLoadingWidget}']")), timeout);

        await Task.Delay(1000);

        Assert.HasCount(PosWidgetCatalog.All.Count, cut.FindAll($"[role='status'][aria-label='{WorkbenchStrings.PosLoadingWidget}']"));
        Assert.DoesNotContain(WorkbenchStrings.PosWidgetFailedTitle, cut.Markup);
    }

    [TestMethod]
    public async Task Slow_Should_ShowPlaceholdersFirstAndTheDataAfterAWhile()
    {
        await using var ctx = WorkbenchBunit.Create();

        var cut = Render(ctx, "pos-dashboard", new() { Data = WorkbenchDataState.Slow });

        cut.WaitForAssertion(() => Assert.HasCount(PosWidgetCatalog.All.Count, cut.FindAll($"[role='status'][aria-label='{WorkbenchStrings.PosLoadingWidget}']")), timeout);
        cut.WaitForAssertion(() => Assert.IsEmpty(cut.FindAll($"[role='status'][aria-label='{WorkbenchStrings.PosLoadingWidget}']")), timeout);

        Assert.IsEmpty(cut.FindAll(".widget-state"));
    }

    [TestMethod]
    public async Task Mixed_Should_GiveEachDashboardWidgetTheStateOfItsSource()
    {
        await using var ctx = WorkbenchBunit.Create();

        var mixed = new WorkbenchScenarioState();
        mixed.Set(new() { Data = WorkbenchDataState.Mixed });

        string[] sources = ["pos.net-sales", "pos.transactions", "pos.average-sale", "pos.refunds", "pos.sales-by-hour", "pos.payment-methods",
                            "pos.top-products", "pos.low-stock", "pos.recent-orders", "pos.staff", "pos.daily-goal", "pos.peak-hours"];

        var cut = Render(ctx, "pos-dashboard", new() { Data = WorkbenchDataState.Mixed });

        cut.WaitForAssertion(() => Assert.IsEmpty(cut.FindAll($"[role='status'][aria-label='{WorkbenchStrings.PosLoadingWidget}']")), timeout);

        Assert.HasCount(sources.Count(s => mixed.ResolveState(s) is WorkbenchDataState.Empty), cut.FindAll(".widget-state.empty"));
        Assert.HasCount(sources.Count(s => mixed.ResolveState(s) is WorkbenchDataState.Error), cut.FindAll(".widget-state.failed"));
        Assert.IsGreaterThan(cut.FindAll(".widget-state").Count, cut.FindAll("section.widget").Count, "The other widgets show their data.");
    }

    [TestMethod]
    public async Task Live_Should_ShowEveryDashboardWidgetWithItsData()
    {
        await using var ctx = WorkbenchBunit.Create();

        var cut = Render(ctx, "pos-dashboard", new());

        cut.WaitForAssertion(() =>
        {
            Assert.HasCount(PosWidgetCatalog.All.Count, cut.FindAll("section.widget"));
            Assert.IsEmpty(cut.FindAll($"section.widget [role='status'][aria-label='{WorkbenchStrings.PosLoadingWidget}']"));
            Assert.IsEmpty(cut.FindAll(".widget-state"));
        }, timeout);
    }

    [TestMethod]
    [DataRow("settings")]
    [DataRow("pos-dashboard")]
    [DataRow("buildings")]
    [DataRow("assistant")]
    [DataRow("shifts")]
    //#if (module == "Admin")
    [DataRow("dashboard")]
    //#endif
    public async Task APageThatNeedsSignIn_Should_ShowTheAppsNotAuthorizedPageWhenSignedOut(string pageKey)
    {
        await using var ctx = WorkbenchBunit.Create();

        var cut = Render(ctx, pageKey, new() { Auth = WorkbenchAuthState.SignedOut });

        cut.WaitForAssertion(() => Assert.Contains(AppStrings.YouAreNotAuthorized, cut.Markup), timeout);
    }

    [TestMethod]
    [DataRow(0, true)]
    [DataRow(2, false)]
    public async Task OnlyAnAdmin_Should_BeOfferedToAddABuilding(int user, bool canAdd)
    {
        await using var ctx = WorkbenchBunit.Create();

        var cut = Render(ctx, "buildings", new() { User = user });

        cut.WaitForAssertion(() => Assert.Contains("Rijksmuseum", cut.Markup), timeout);

        Assert.AreEqual(canAdd, cut.FindAll("button").Any(b => b.TextContent.Trim() == WorkbenchStrings.BuildingsAdd));
    }

    [TestMethod]
    public async Task TheCardsDesignOption_Should_ListTheBuildingsAsCards()
    {
        await using var ctx = WorkbenchBunit.Create();

        var cut = Render(ctx, "buildings", new WorkbenchScenario { Variant = "cards" }.WithSwitch("amount", "few"));

        cut.WaitForAssertion(() => Assert.HasCount(3, cut.FindAll("div.cards [role='listitem']")), timeout);
    }

    private static int Count(string markup, string text) => Regex.Matches(markup, Regex.Escape(text)).Count;
}
