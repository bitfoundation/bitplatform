using Bit.Bcrdt;
using Microsoft.Extensions.DependencyInjection;

namespace Boilerplate.WorkbenchTests.Features.Progress;

/// <summary>
/// The agents' tasks are one map in the review document: what an agent's update changes, what it is refused, and what the
/// user's answers, finished steps and messages may change, decide what both of them see.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchProgressDocumentTests
{
    private readonly BcrdtDocument document = new();

    private BcrdtMap Progress => document.GetMap(WorkbenchProgressDocument.MapName);

    private bool Edit(Func<bool> edit)
    {
        var done = false;
        document.Transact(_ => done = edit());
        return done;
    }

    private WorkbenchProgressTask Task(string id = "t") => WorkbenchProgressDocument.ReadTask(Progress, id)!;

    private void Create(WorkbenchProgressUpdate update, string id = "t")
    {
        Assert.IsNull(WorkbenchProgressDocument.Check(update, null));
        document.Transact(_ => WorkbenchProgressDocument.Create(Progress, id, "Claude Code", update));
    }

    private static WorkbenchProgressUpdate Update(string? title = null,
                                                  string? status = null,
                                                  WorkbenchProgressStepUpdate[]? steps = null,
                                                  WorkbenchProgressNoteUpdate[]? notes = null,
                                                  WorkbenchProgressLinkUpdate[]? links = null,
                                                  WorkbenchProgressAskUpdate[]? asks = null,
                                                  string? log = null) => new(null, title, null, status, steps, notes, links, asks, log);

    [TestMethod]
    public void ANewTask_Should_ReadBackAsTheAgentSentIt()
    {
        Create(Update("Deploy", steps: [new("vm", "Get a server", "you", "doing", "Any provider", "https://example.com"), new("docker", "Install Docker", null, null, null, null)],
                      notes: [new(null, "warning", "No keys here")],
                      links: [new("Test site", "https://test.example.com")],
                      asks: [new("region", "Which region?", null, "choice", ["Europe", "US"], null)],
                      log: "Started"));

        var task = Task();

        Assert.AreEqual(("Deploy", "Claude Code", WorkbenchProgressStatus.Working), (task.Title, task.Agent, task.Status));
        Assert.AreEqual(new WorkbenchProgressStep("vm", "Get a server", WorkbenchProgressOwner.You, WorkbenchProgressStepStatus.Doing, "Any provider", "https://example.com"), task.Steps[0]);
        Assert.AreEqual((WorkbenchProgressOwner.Agent, WorkbenchProgressStepStatus.Todo), (task.Steps[1].Owner, task.Steps[1].Status));
        Assert.AreEqual((WorkbenchProgressNoteKind.Warning, "No keys here"), (task.Notes.Single().Kind, task.Notes.Single().Text));
        Assert.AreEqual(new WorkbenchProgressLink("Test site", "https://test.example.com"), task.Links.Single());
        Assert.AreSequenceEqual(new[] { "Europe", "US" }, task.Asks.Single().Options);
        Assert.AreEqual(("Started", false), (task.Feed.Single().Text, task.Feed.Single().FromYou));
        Assert.AreEqual(2, task.NeedsYouCount, "The open question and the step of the user's that is due.");
        Assert.IsTrue(task.IsAgentActive);
    }

    [TestMethod]
    public void AnUpdate_Should_ChangeOnlyWhatItNames()
    {
        Create(Update("Feature", steps: [new("a", "First", null, "doing", null, null), new("b", "Second", null, null, null, null)]));

        Edit(() => WorkbenchProgressDocument.Apply(Progress, "t", "Codex", Update(steps: [new("a", null, null, "done", null, null), new("c", "Third", null, null, null, null)], log: "First is done")));

        var task = Task();

        Assert.AreEqual("Feature", task.Title);
        Assert.AreSequenceEqual(new[] { "a", "b", "c" }, task.Steps.Select(s => s.Id).ToArray());
        Assert.AreEqual(("First", WorkbenchProgressStepStatus.Done), (task.Steps[0].Text, task.Steps[0].Status));
        Assert.AreEqual("Codex", task.Agent);
        Assert.AreEqual(33d, task.Percent, "One of three steps done.");

        Edit(() => WorkbenchProgressDocument.Apply(Progress, "t", "Codex", Update(steps: [new("b", null, null, null, null, null, Remove: true)])));

        Assert.AreSequenceEqual(new[] { "a", "c" }, Task().Steps.Select(s => s.Id).ToArray());
    }

    [TestMethod]
    [DataRow("{\"steps\":[{\"id\":\"x\"}]}", "needs a text")]
    [DataRow("{\"steps\":[{\"id\":\"x\",\"text\":\"t\",\"owner\":\"robot\"}]}", "neither agent nor you")]
    [DataRow("{\"steps\":[{\"id\":\"x\",\"text\":\"t\",\"status\":\"1\"}]}", "none of todo")]
    [DataRow("{\"steps\":[{\"id\":\"x\",\"text\":\"t\",\"link\":\"javascript:alert(1)\"}]}", "http(s)")]
    [DataRow("{\"steps\":[{\"id\":\"x\",\"text\":\"t\",\"link\":\"//evil.example.com\"}]}", "http(s)")]
    [DataRow("{\"links\":[{\"label\":\"Bad\",\"url\":\"file:///c:/secrets.txt\"}]}", "http(s)")]
    [DataRow("{\"asks\":[{\"id\":\"q\",\"question\":\"Pick\",\"kind\":\"choice\",\"options\":[\"only one\"]}]}", "2 to 8 options")]
    [DataRow("{\"asks\":[{\"id\":\"q\",\"kind\":\"text\"}]}", "needs a text")]
    [DataRow("{\"status\":\"finished\"}", "none of working")]
    [DataRow("{\"log\":\"CI is red\",\"logKind\":\"panic\"}", "none of info")]
    public void AnUpdateThatDoesNotFit_Should_BeRefusedWithAReason(string json, string reason)
    {
        Create(Update("Task"));

        var update = System.Text.Json.JsonSerializer.Deserialize<WorkbenchProgressUpdate>(json, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;

        Assert.Contains(reason, WorkbenchProgressDocument.Check(update, Task())!);
    }

    [TestMethod]
    public void ANewTaskWithoutATitle_Should_BeRefused()
    {
        Assert.Contains("title", WorkbenchProgressDocument.Check(Update(), null)!);
    }

    [TestMethod]
    public void AnAnswer_Should_FitItsQuestion()
    {
        Create(Update("Task", asks: [new("text", "Domain?", null, "text", null, null),
                                     new("one", "Region?", null, "choice", ["Europe", "US"], null),
                                     new("many", "Orders?", null, "choices", ["Online", "Store", "Gift card"], null),
                                     new("yes", "Close SSH?", null, "confirm", null, null)]));

        Assert.IsFalse(Edit(() => WorkbenchProgressDocument.Answer(Progress, "t", "text", [], "  ", "Sarah")), "A text answer needs text.");
        Assert.IsFalse(Edit(() => WorkbenchProgressDocument.Answer(Progress, "t", "one", ["Asia"], null, "Sarah")), "Only one of its options.");
        Assert.IsFalse(Edit(() => WorkbenchProgressDocument.Answer(Progress, "t", "one", ["Europe", "US"], null, "Sarah")), "One choice, not two.");
        Assert.IsFalse(Edit(() => WorkbenchProgressDocument.Answer(Progress, "t", "yes", ["maybe"], null, "Sarah")));

        Assert.IsTrue(Edit(() => WorkbenchProgressDocument.Answer(Progress, "t", "text", [], "test.example.com", "Sarah")));
        Assert.IsTrue(Edit(() => WorkbenchProgressDocument.Answer(Progress, "t", "one", ["US"], "cheaper", "Sarah")));
        Assert.IsTrue(Edit(() => WorkbenchProgressDocument.Answer(Progress, "t", "many", ["Online", "Gift card"], null, "Sarah")));
        Assert.IsTrue(Edit(() => WorkbenchProgressDocument.Answer(Progress, "t", "yes", [WorkbenchProgressDocument.No], null, "Sarah")));

        Assert.IsFalse(Edit(() => WorkbenchProgressDocument.Answer(Progress, "t", "one", ["Europe"], null, "Sarah")), "An answered question stays answered.");

        var task = Task();

        Assert.AreEqual(0, task.NeedsYouCount);
        Assert.AreEqual(("cheaper", "Sarah"), (task.Asks[1].Answer!.Text, task.Asks[1].Answer!.By));
        Assert.AreSequenceEqual(new[] { "Online", "Gift card" }, task.Asks[2].Answer!.Choices);
        Assert.AreEqual(4, task.Feed.Count(e => e.Kind is WorkbenchProgressEntryKind.Answer));
        Assert.IsGreaterThan(0L, task.Cursor);
    }

    [TestMethod]
    public void AnAnsweredQuestionSentAgain_Should_StayAsItWasAnswered()
    {
        Create(Update("Task", asks: [new("one", "Region?", null, "choice", ["Europe", "US"], null)]));
        Edit(() => WorkbenchProgressDocument.Answer(Progress, "t", "one", ["US"], null, "Sarah"));

        var again = Update(asks: [new("one", "Which region now?", null, "choice", ["Asia", "Africa"], null)]);

        Assert.IsNull(WorkbenchProgressDocument.Check(again, Task()));
        Edit(() => WorkbenchProgressDocument.Apply(Progress, "t", "Claude Code", again));

        Assert.AreEqual(("Region?", "US"), (Task().Asks[0].Question, Task().Asks[0].Answer!.Choices[0]));
    }

    [TestMethod]
    public void OnlyTheUsersOwnStep_Should_BeMarkedDoneByThem()
    {
        Create(Update("Task", steps: [new("mine", "Create a token", "you", "doing", null, null), new("agents", "Install Docker", null, "doing", null, null)]));

        Assert.IsFalse(Edit(() => WorkbenchProgressDocument.CompleteStep(Progress, "t", "agents", "Sarah")));
        Assert.IsTrue(Edit(() => WorkbenchProgressDocument.CompleteStep(Progress, "t", "mine", "Sarah")));
        Assert.IsFalse(Edit(() => WorkbenchProgressDocument.CompleteStep(Progress, "t", "mine", "Sarah")), "Done once is done.");

        Assert.AreEqual(WorkbenchProgressStepStatus.Done, Task().Steps[0].Status);
        Assert.AreEqual(("mine", "Create a token"), (Task().Feed.Last().RefId, Task().Feed.Last().Text));
    }

    [TestMethod]
    public void AFinishedTask_Should_TakeNoMoreAnswersOrSteps()
    {
        Create(Update("Task", steps: [new("mine", "Create a token", "you", "doing", null, null)], asks: [new("q", "Domain?", null, null, null, null)]));
        Edit(() => WorkbenchProgressDocument.Apply(Progress, "t", "Claude Code", Update(status: "stopped")));

        Assert.AreEqual(0, Task().NeedsYouCount);
        Assert.IsFalse(Edit(() => WorkbenchProgressDocument.Answer(Progress, "t", "q", [], "late", "Sarah")));
        Assert.IsFalse(Edit(() => WorkbenchProgressDocument.CompleteStep(Progress, "t", "mine", "Sarah")));
        Assert.IsTrue(Edit(() => WorkbenchProgressDocument.AddMessage(Progress, "t", "Thanks!", "Sarah")), "The user can still write to it.");
    }

    [TestMethod]
    public void AWithdrawnQuestion_Should_NoLongerWaitForTheUser()
    {
        Create(Update("Task", asks: [new("q", "Domain?", null, null, null, null)]));
        Edit(() => WorkbenchProgressDocument.Apply(Progress, "t", "Claude Code", Update(asks: [new("q", null, null, null, null, null, Withdraw: true)])));

        Assert.IsTrue(Task().Asks.Single().IsWithdrawn);
        Assert.AreEqual(0, Task().NeedsYouCount);
        Assert.IsFalse(Edit(() => WorkbenchProgressDocument.Answer(Progress, "t", "q", [], "too late", "Sarah")));
    }

    [TestMethod]
    public void AReportAndAMarkedLine_Should_ReadBackAsTheAgentSentThem()
    {
        Create(Update("Test the platform"));

        Edit(() => WorkbenchProgressDocument.Apply(Progress, "t", "Claude Code", new(null, null, null, "done", null, null, null, null, "All green", LogKind: "success", Report: "## Verdict\n\nShip it.")));

        var task = Task();

        Assert.AreEqual(("## Verdict\n\nShip it.", WorkbenchProgressNoteKind.Success), (task.Report, task.Feed.Single().Tone));

        Edit(() => WorkbenchProgressDocument.Apply(Progress, "t", "Claude Code", new(null, null, null, null, null, null, null, null, "A plain line", Report: "")));

        Assert.IsNull(Task().Report, "An empty report takes it away.");
        Assert.IsNull(Task().Feed[^1].Tone);
    }

    [TestMethod]
    public void TheActivity_Should_KeepOnlyItsNewestLines()
    {
        Create(Update("Task"));

        for (var line = 0; line < WorkbenchProgressDocument.MaxFeed + 25; line++)
        {
            var text = $"line {line}";
            Edit(() => WorkbenchProgressDocument.Apply(Progress, "t", "Claude Code", Update(log: text)));
        }

        var feed = Task().Feed;

        Assert.HasCount(WorkbenchProgressDocument.MaxFeed, feed);
        Assert.AreEqual($"line {WorkbenchProgressDocument.MaxFeed + 24}", feed[^1].Text);
    }

    [TestMethod]
    public async Task TheSamples_Should_ShowADeploymentWaitingForTheUserAFeatureAtWorkAndAFinishedFix()
    {
        await using var ctx = WorkbenchBunit.Create();

        foreach (var sample in WorkbenchSampleTasks.Create(ctx.Services.GetRequiredService<IStringLocalizer<WorkbenchStrings>>()))
        {
            document.Transact(_ => WorkbenchProgressDocument.Write(Progress, sample));
        }

        var deploy = Task(WorkbenchSampleTasks.DeployId);
        var feature = Task(WorkbenchSampleTasks.FeatureId);
        var fix = Task(WorkbenchSampleTasks.FixId);

        Assert.IsGreaterThan(0, deploy.NeedsYouCount);
        Assert.Contains(s => s.Owner is WorkbenchProgressOwner.You && s.Status is WorkbenchProgressStepStatus.Doing, deploy.Steps);
        Assert.Contains(s => s.Owner is WorkbenchProgressOwner.Agent && s.Status is WorkbenchProgressStepStatus.Doing, feature.Steps);
        Assert.AreEqual((WorkbenchProgressStatus.Done, 100d), (fix.Status, fix.Percent));
        Assert.IsFalse(string.IsNullOrWhiteSpace(fix.Report), "The finished one shows the report an agent ends with.");
        Assert.IsTrue(new[] { deploy, feature, fix }.SelectMany(t => t.Links).All(link => WorkbenchProgressDocument.IsSafeUrl(link.Url)));
        Assert.IsTrue(new[] { deploy, feature, fix }.SelectMany(t => t.Asks).Where(a => a.Kind is WorkbenchProgressAskKind.Choice or WorkbenchProgressAskKind.Choices).All(a => a.Options.Length >= 2));
    }
}
