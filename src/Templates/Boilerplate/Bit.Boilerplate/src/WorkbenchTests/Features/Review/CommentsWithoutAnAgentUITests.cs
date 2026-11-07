namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>A comment written while no agent is connected: it is written all the same, and the review says what becomes of it.</summary>
[TestClass, TestCategory("UITest")]
public class CommentsWithoutAnAgentUITests : ReviewPageTest
{
    private const string AgentName = "Claude Code";

    private ILocator ShiftsHeading => View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading });

    private ILocator AutoReply(string text) => ThreadWith(text).Locator(".message.auto-reply");

    private async Task OpenShiftsWithNoAgent()
    {
        await ClearComments("shifts");
        await SetAgentLastSeen(AgentName, null, DateTimeOffset.UtcNow.AddHours(-1));
        await OpenWorkbench("shifts");
    }

    [TestMethod, EverySurface]
    public async Task AComment_Should_ReachTheReviewAndSayWhatHappensNext(WorkbenchSurface surface)
    {
        await OpenShiftsWithNoAgent();

        var text = Unique("Make the heading bolder");
        var id = await LeaveAComment(ShiftsHeading, text);

        await Expect(AutoReply(text)).ToContainTextAsync(WorkbenchStrings.CommentWaitsForAgent);
        await Expect(AutoReply(text).Locator(".message-author")).ToHaveTextAsync(WorkbenchStrings.WorkbenchTitle);

        var thread = (await Server.Agent.GetThreads(CancellationToken.None)).Single(t => t.Id == id);

        Assert.HasCount(1, thread.Messages);
    }

    [TestMethod, WithAnAgent]
    public async Task TheAnswer_Should_GoOnceAnAgentConnects(WorkbenchSurface surface)
    {
        await OpenShiftsWithNoAgent();

        var text = Unique("Make the heading smaller");

        await LeaveAComment(ShiftsHeading, text);
        await Expect(AutoReply(text)).ToBeVisibleAsync();

        await Server.Agent.Wait(AgentName, long.MaxValue, TimeSpan.Zero, CancellationToken.None);

        await Expect(AutoReply(text)).ToHaveCountAsync(0);

        await SetAgentLastSeen(AgentName, null, DateTimeOffset.UtcNow.AddHours(-1));
    }

    [TestMethod, WithAnAgent]
    public async Task AnAnsweredComment_Should_NotBeToldToWait(WorkbenchSurface surface)
    {
        await OpenShiftsWithNoAgent();

        var text = Unique("Is this heading right?");
        var id = await LeaveAComment(ShiftsHeading, text);

        await Server.Agent.Reply(AgentName, id, Unique("It is"), CancellationToken.None);
        await SetAgentLastSeen(AgentName, null, DateTimeOffset.UtcNow.AddHours(-1));

        await Expect(ThreadWith(text).Locator(".message.from-agent")).ToBeVisibleAsync();
        await Expect(AutoReply(text)).ToHaveCountAsync(0);
    }

    [TestMethod, WithoutAnAgent]
    public async Task TheAgentThatConnectsLater_Should_FindTheComment(WorkbenchSurface surface)
    {
        await OpenShiftsWithNoAgent();

        var text = Unique("Left before the agent was running");
        var id = await LeaveAComment(ShiftsHeading, text);

        var thread = (await Server.Agent.GetThreads(CancellationToken.None)).Single(t => t.Id == id);

        Assert.AreEqual(WorkbenchReviewStatus.Open, thread.Status);
        await Expect(AutoReply(text)).ToContainTextAsync(WorkbenchStrings.CommentWaitsForAgent);
    }
}
