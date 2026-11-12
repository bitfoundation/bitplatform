using Microsoft.Extensions.DependencyInjection;

namespace Boilerplate.WorkbenchTests.Features.Progress;

/// <summary>The Progress page in a browser, with the guide a first visit gets put away.</summary>
public abstract class ProgressPageTest : WorkbenchPageTest
{
    protected WorkbenchProgressAgent Agent => Server.Services.GetRequiredService<WorkbenchProgressAgent>();

    protected async Task OpenProgress(string path)
    {
        await Page.GotoAsync(new Uri(Server.Address, path).ToString());
        await WaitUntilInteractive();

        var skip = Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourSkip, Exact = true });

        await Expect(skip).ToBeVisibleAsync();
        await Press(skip);
        await Expect(skip).ToBeHiddenAsync();
    }

    protected ILocator Ask(string id) => Page.Locator($"[data-ask-id='{id}']");
}

/// <summary>
/// The Progress page on this machine: an agent's task shows what waits for the user, who answers it, finishes their step and
/// writes to the agent, and the agent's wait gets each one at once.
/// </summary>
[TestClass, TestCategory("UITest")]
public class ProgressUITests : ProgressPageTest
{

    [TestMethod, WithAnAgent]
    public async Task WhatTheUserDoes_Should_ReachTheWaitingAgentAtOnce(WorkbenchSurface surface)
    {
        var (id, _) = await Agent.Create("Claude Code", new(null, "Deploy to Test", null, null,
            [new("token", "Create an API token", "you", "doing", "Paste it into the file I opened.", null), new("docker", "Install Docker", null, "todo", null, null)],
            [new(null, "warning", "Never paste keys here.")],
            null,
            [new("domain", "Which domain should Test use?", null, "text", null, "test.example.com")],
            "Started"), CancellationToken.None);

        var cursor = (await Agent.GetTask(id!, CancellationToken.None))!.Cursor;

        await OpenProgress($"progress/{id}");

        var card = Page.Locator($".task-card[data-task-id='{id}']");

        if (IsPhone is false)
        {
            await Expect(card.Locator(".needs-count")).ToHaveTextAsync("2");
        }

        await Expect(Page.Locator(".your-turn")).ToContainTextAsync("Create an API token");
        await Expect(Page.Locator(".notes")).ToContainTextAsync("Never paste keys here.");
        await Expect(Page).ToHaveTitleAsync(new Regex(@"^\(\d+\) "));

        await Ask("domain").GetByRole(AriaRole.Textbox, new() { Name = "Which domain should Test use?" }).FillAsync("test.contoso.com");
        await Press(Ask("domain").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ProgressSendAnswer }));

        await Expect(Ask("domain")).ToHaveCountAsync(0);

        var answered = await Agent.Wait("Claude Code", id!, cursor, TimeSpan.FromSeconds(10), CancellationToken.None);

        Assert.AreEqual((WorkbenchProgressEntryKind.Answer, "domain", "test.contoso.com"), (answered!.Value.Events[0].Kind, answered.Value.Events[0].RefId, answered.Value.Events[0].Text));

        await Press(Page.Locator("[data-step-id='token']").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ProgressDidIt }));

        await Expect(Page.Locator(".your-turn")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".nothing-for-you")).ToBeVisibleAsync();

        var stepped = await Agent.Wait("Claude Code", id!, answered.Value.Cursor, TimeSpan.FromSeconds(10), CancellationToken.None);

        Assert.AreEqual((WorkbenchProgressEntryKind.Step, "token"), (stepped!.Value.Events[0].Kind, stepped.Value.Events[0].RefId));

        await Page.Locator(".activity").GetByRole(AriaRole.Textbox).FillAsync("Use the cheapest plan.");
        await Press(Page.Locator(".activity").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Send, Exact = true }));

        var written = await Agent.Wait("Claude Code", id!, stepped.Value.Cursor, TimeSpan.FromSeconds(10), CancellationToken.None);

        Assert.AreEqual("Use the cheapest plan.", written!.Value.Events[0].Text);
        await Expect(Page.Locator(".feed .from-you").First).ToContainTextAsync("Use the cheapest plan.");
    }

    [TestMethod, WithAnAgent]
    public async Task ANewQuestion_Should_ShowWhileThePageIsOpen(WorkbenchSurface surface)
    {
        var (id, _) = await Agent.Create("Codex", new(null, "Loyalty points", null, null, [new("ledger", "Build the ledger", null, "doing", null, null)], null, null, null, null), CancellationToken.None);

        await OpenProgress($"progress/{id}");

        await Expect(Page.Locator(".nothing-for-you")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-step-id='ledger'] .spinner")).ToBeVisibleAsync();

        await Agent.Update("Codex", id!, new(null, null, null, null, null, null, null, [new("orders", "Which orders earn points?", null, "choices", ["Online", "In store"], null)], null), CancellationToken.None);

        await Expect(Ask("orders")).ToBeVisibleAsync();
        await Expect(Page).ToHaveTitleAsync(new Regex(@"^\(\d+\) "));

        await Press(Ask("orders").GetByText("Online", new() { Exact = true }));
        await Press(Ask("orders").GetByText("In store", new() { Exact = true }));
        await Press(Ask("orders").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ProgressSendAnswer }));

        await Expect(Ask("orders")).ToHaveCountAsync(0);

        var task = await WorkbenchTestReviewer.WaitFor(() => Agent.GetTask(id!, CancellationToken.None), t => t!.Asks.Single().Answer is not null);

        Assert.AreSequenceEqual(new[] { "Online", "In store" }, task!.Asks.Single().Answer!.Choices);
    }

    [TestMethod, WithAnAgent]
    public async Task TheAgentsMarkdown_Should_ReadAsADocumentAndRunNothing(WorkbenchSurface surface)
    {
        var (id, _) = await Agent.Create("Claude Code", new(null, "Test the platform", null, "done",
            [new("ci", "All CI", null, "done", "Green on [run 42](https://github.com/bitfoundation/bitplatform/actions/runs/42) at `9cfbf7c2fd`", null)],
            null, null, null, "CI is green", LogKind: "success",
            Report: "## Verdict\n\nReady to ship.\n\n| Stage | Result |\n| --- | --- |\n| CI | Green |\n\n<img src=x onerror=\"document.title='hacked'\">"), CancellationToken.None);

        await OpenProgress($"progress/{id}");

        var report = Page.Locator(".report");

        await Expect(report.GetByRole(AriaRole.Heading, new() { Name = "Verdict" })).ToBeVisibleAsync();
        await Expect(report.Locator("table")).ToContainTextAsync("Green");
        await Expect(report).ToContainTextAsync("<img src=x");
        await Expect(report.Locator("img")).ToHaveCountAsync(0);

        var detail = Page.Locator("[data-step-id='ci'] .step-detail");
        var run = detail.GetByRole(AriaRole.Link, new() { Name = "run 42" });

        await Expect(run).ToHaveAttributeAsync("href", "https://github.com/bitfoundation/bitplatform/actions/runs/42");
        await Expect(run).ToHaveAttributeAsync("target", "_blank");
        await Expect(detail.Locator("code")).ToHaveTextAsync("9cfbf7c2fd");
        await Expect(Page.Locator(".feed .entry.tone-success")).ToContainTextAsync("CI is green");
    }
}

/// <summary>
/// The Progress page from elsewhere, like a published demo or a tunnel: three sample tasks show what an agent can put there, and
/// what the visitor does on them stays in their tab.
/// </summary>
[TestClass, TestCategory("UITest")]
public class ProgressSamplesUITests : ProgressPageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();

        options.ExtraHTTPHeaders = new Dictionary<string, string> { ["X-Forwarded-For"] = "203.0.113.7" };

        return options;
    }

    [TestMethod, EverySurface]
    public async Task TheSamples_Should_ShowWhatAnAgentCanDoAndTakeAnAnswer(WorkbenchSurface surface)
    {
        await OpenProgress("progress");

        await Expect(Page.Locator(".samples-note")).ToContainTextAsync(WorkbenchStrings.ProgressSamplesTitle);
        await Expect(Page.Locator(".task-card")).ToHaveCountAsync(3);
        await Expect(Page.Locator(".task-list")).ToContainTextAsync(WorkbenchStrings.SampleDeployTitle);
        await Expect(Page.Locator(".task-list")).ToContainTextAsync(WorkbenchStrings.SampleFeatureTitle);
        await Expect(Page.Locator(".task-list")).ToContainTextAsync(WorkbenchStrings.SampleFixTitle);

        await Press(Page.Locator($".task-card[data-task-id='{WorkbenchSampleTasks.DeployId}']"));

        await Expect(Page).ToHaveURLAsync(new Regex($"/progress/{WorkbenchSampleTasks.DeployId}$"));

        var email = Ask("email");
        var canDictate = await Page.EvaluateAsync<bool>("() => !!(window.SpeechRecognition || window.webkitSpeechRecognition)");

        await Expect(email.Locator(".dictation")).ToHaveCountAsync(canDictate ? 1 : 0);

        await Press(email.GetByText(WorkbenchStrings.SampleDeployAskEmailOption3, new() { Exact = true }));
        await Press(email.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ProgressSendAnswer }));

        await Expect(Page.GetByText(WorkbenchStrings.ProgressSampleSentTitle, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(email).ToHaveCountAsync(0);
        await Expect(Page.Locator(".feed .from-you").First).ToContainTextAsync(WorkbenchStrings.SampleDeployAskEmailOption3);

        Assert.IsTrue((await Server.Services.GetRequiredService<WorkbenchProgressAgent>().GetTasks(CancellationToken.None)).All(t => t.Id.StartsWith("sample-", StringComparison.Ordinal) is false), "A sample never reaches the review.");
    }
}
