namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>A page's review status: draft, in review, approved or built, set in the review or by the agent, shown in the review, a desktop's top bar and the home page.</summary>
[TestClass, TestCategory("UITest")]
public class PageStatusUITests : ReviewPageTest
{
    private const string AgentName = "Claude Code";

    private ILocator TopBarStatus => Page.Locator(".topbar .page-status");

    private ILocator SetByLine => Tools.GetByText(new Regex($"^{Regex.Escape(WorkbenchStrings.PageStatusSetBy[..WorkbenchStrings.PageStatusSetBy.IndexOf('{')])}"));

    private static string TextOf(WorkbenchPageStatus status) => status switch
    {
        WorkbenchPageStatus.InReview => WorkbenchStrings.PageStatusInReview,
        WorkbenchPageStatus.Approved => WorkbenchStrings.PageStatusApproved,
        WorkbenchPageStatus.Built => WorkbenchStrings.PageStatusBuilt,
        _ => WorkbenchStrings.PageStatusDraft
    };

    [TestMethod, EverySurface]
    public async Task APageWithoutAStatus_Should_ShowAsADraftInTheReviewAndNotInTheTopBar(WorkbenchSurface surface)
    {
        await ClearPageStatus("settings");

        await OpenWorkbench("settings");
        await OpenReview();

        if (HasAgent)
        {
            await Expect(ToolsButton(WorkbenchStrings.PageStatusDraft)).ToHaveAttributeAsync("aria-pressed", "true");
        }
        else
        {
            await Expect(Tools.GetByText(WorkbenchStrings.PageStatusDraft, new() { Exact = true })).ToBeVisibleAsync();
        }

        await Expect(SetByLine).ToHaveCountAsync(0);
        await Expect(TopBarStatus).ToHaveCountAsync(0);
    }

    [TestMethod, WithAnAgent]
    public async Task TheStatusButtons_Should_SetTheStatusEverywhere(WorkbenchSurface surface)
    {
        await ClearPageStatus("settings");

        await OpenWorkbench("settings");
        await OpenReview();

        foreach (var status in Enum.GetValues<WorkbenchPageStatus>().Reverse())
        {
            await Press(ToolsButton(TextOf(status)));

            await Expect(ToolsButton(TextOf(status))).ToHaveAttributeAsync("aria-pressed", "true");
            await Expect(Tools.Locator(".segments.four button[aria-pressed='true']")).ToHaveCountAsync(1);

            if (IsPhone is false)
            {
                await Expect(TopBarStatus).ToHaveTextAsync(TextOf(status));
                await Expect(TopBarStatus).ToHaveClassAsync(new Regex($"status-{status.ToString().ToLowerInvariant()}"));
            }

            await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetPages(CancellationToken.None), pages => pages.GetValueOrDefault("settings")?.Status == status);
        }

        await Expect(SetByLine).ToContainTextAsync(WorkbenchStrings.Reviewer);
    }

    [TestMethod, WithAnAgent]
    public async Task TheStatus_Should_BeSignedWithYourNameAndStayAfterAReload(WorkbenchSurface surface)
    {
        var name = $"Nora {Guid.NewGuid().ToString("N")[..6]}";

        await ClearPageStatus("shifts");
        await ClearComments("shifts");

        await OpenWorkbench("shifts");

        // The name is typed in the composer, which a pinned comment opens.
        await PickSpot(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading }));

        await Tools.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.YourName, Exact = true }).FillAsync(name);
        await Press(ToolsButton(WorkbenchStrings.PageStatusApproved));

        await Expect(SetByLine).ToContainTextAsync(name);

        var review = await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetPages(CancellationToken.None), pages => pages.GetValueOrDefault("shifts")?.Status is WorkbenchPageStatus.Approved);

        Assert.AreEqual(name, review["shifts"].By);

        await Page.ReloadAsync();
        await WaitForViews();
        await OpenReview();

        await Expect(ToolsButton(WorkbenchStrings.PageStatusApproved)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(SetByLine).ToContainTextAsync(name);
    }

    [TestMethod, EverySurface]
    public async Task TheAgentsStatus_Should_ArriveLive(WorkbenchSurface surface)
    {
        await ClearPageStatus("settings");

        await OpenWorkbench("settings");
        await OpenReview();
        await MarkThePage();

        foreach (var status in Enum.GetValues<WorkbenchPageStatus>())
        {
            await Server.Agent.SetPageStatus(AgentName, "settings", status, CancellationToken.None);

            if (HasAgent)
            {
                await Expect(ToolsButton(TextOf(status))).ToHaveAttributeAsync("aria-pressed", "true");
            }
            else
            {
                await Expect(Tools.GetByText(TextOf(status), new() { Exact = true })).ToBeVisibleAsync();
            }

            if (IsPhone is false)
            {
                await Expect(TopBarStatus).ToHaveTextAsync(TextOf(status));
            }

            await Expect(SetByLine).ToContainTextAsync(AgentName);
        }

        await ExpectNoReload();
    }

    [TestMethod, EverySurface]
    public async Task TheHomePage_Should_ShowAPagesStatus(WorkbenchSurface surface)
    {
        await Server.Agent.SetPageStatus(AgentName, "settings", WorkbenchPageStatus.Approved, CancellationToken.None);

        await Page.GotoAsync(Server.Address.ToString());

        await Expect(Page.Locator(".page-review .review-status.status-approved").Filter(new() { HasText = WorkbenchStrings.PageStatusApproved }).First).ToBeVisibleAsync();
    }

    [TestMethod, WithoutAnAgent]
    public async Task WithoutAnAgent_Should_ShowTheStatusAsPlainText(WorkbenchSurface surface)
    {
        await Server.Agent.SetPageStatus(AgentName, "settings", WorkbenchPageStatus.InReview, CancellationToken.None);

        await OpenWorkbench("settings");
        await OpenReview();

        await Expect(Tools.GetByText(WorkbenchStrings.PageStatusInReview, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(SetByLine).ToContainTextAsync(AgentName);

        foreach (var status in Enum.GetValues<WorkbenchPageStatus>())
        {
            await Expect(ToolsButton(TextOf(status))).ToHaveCountAsync(0);
        }

        if (IsPhone is false)
        {
            await Expect(TopBarStatus).ToHaveTextAsync(WorkbenchStrings.PageStatusInReview);
        }
    }
}
