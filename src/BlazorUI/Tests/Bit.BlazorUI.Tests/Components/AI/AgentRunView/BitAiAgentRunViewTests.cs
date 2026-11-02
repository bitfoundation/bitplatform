using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.AgentRunView;

[TestClass]
public class BitAiAgentRunViewTests : BitAiTestBase
{
    private CultureInfo? _culture;

    // The usage and the clock are formatted in the culture of the page, so the expectations below are written in one.
    [TestInitialize]
    public void UseEnglishNumbers()
    {
        _culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [TestCleanup]
    public void RestoreCulture()
    {
        if (_culture is not null) CultureInfo.CurrentCulture = _culture;
    }

    private static int Subscribers(object target) =>
        ((Delegate?)target.GetType()
            .GetField("Changed", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(target))?.GetInvocationList().Length ?? 0;

    private static BitAiAgentRun RunningRun()
    {
        var run = new BitAiAgentRun("run_1", "Researcher");
        run.SetPlan([new BitAiAgentPlanStep("search", "Search"), new BitAiAgentPlanStep("write", "Write")]);
        run.GetOrAddStep("search").Start();
        run.SetStatus(BitAiAgentRunStatus.Running);
        return run;
    }

    private static BitAiAgentRun AskingRun(params string[] suggestions)
    {
        var run = RunningRun();
        run.RequestInput("Which region?", suggestions.Length == 0 ? null : suggestions);
        return run;
    }



    [TestMethod]
    public void BitAiAgentRunViewShouldRenderItsRootClass()
    {
        var component = RenderComponent<BitAiAgentRunView>(p => p.Add(c => c.Run, RunningRun()));

        var root = component.Find(".bit-aiarv");

        Assert.AreEqual("DIV", root.TagName);
        Assert.IsNotNull(root.GetAttribute("id"));
        Assert.AreEqual("Researcher", component.Find(".bit-aiarv-ttl").TextContent);
        Assert.AreEqual(2, component.FindAll(".bit-aiarv-stl > .bit-aiarv-sti > .bit-aiasv").Count);
        Assert.IsNotNull(component.Find(".bit-aiarv-hdr .bit-aiasb"));

        // The sparkle is no glyph of the core font, so the icon also carries the class of the package's own font.
        var icon = component.Find(".bit-aiarv-hdr > .bit-aiarv-hic");
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Glimmer"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon-ai--Glimmer"));
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitAiAgentRunViewShouldNameAPlanWithoutAnAgent()
    {
        var run = new BitAiAgentRun();

        var component = RenderComponent<BitAiAgentRunView>(p => p.Add(c => c.Run, run));

        Assert.AreEqual("Plan", component.Find(".bit-aiarv-ttl").TextContent);
        // A run with no plan has no progress to show.
        Assert.IsEmpty(component.FindAll(".bit-aiarv-prg"));
    }

    [TestMethod]
    public void BitAiAgentRunViewShouldReadTheProgressAsAMeasureOfTheSteps()
    {
        var run = RunningRun();
        run.Steps[0].Complete();

        var component = RenderComponent<BitAiAgentRunView>(p => p.Add(c => c.Run, run));

        var bar = component.Find(".bit-aiarv-prg [role=progressbar]");

        Assert.AreEqual("1", bar.GetAttribute("aria-valuenow"));
        Assert.AreEqual("0", bar.GetAttribute("aria-valuemin"));
        Assert.AreEqual("2", bar.GetAttribute("aria-valuemax"));
        Assert.AreEqual("1 / 2", bar.GetAttribute("aria-valuetext"));
        Assert.AreEqual("Plan", bar.GetAttribute("aria-label"));
        Assert.AreEqual("1 / 2", component.Find(".bit-aiarv-cnt").TextContent);

        var clock = component.Find(".bit-aiarv-elp");
        Assert.AreEqual("timer", clock.GetAttribute("role"));
        Assert.AreEqual("Elapsed", clock.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitAiAgentRunViewShouldShowTheGoal()
    {
        var run = RunningRun();
        run.Goal = "Find the cheapest region";

        var component = RenderComponent<BitAiAgentRunView>(p => p.Add(c => c.Run, run));

        Assert.AreEqual("Find the cheapest region", component.Find(".bit-aiarv-gol").TextContent);
    }

    [TestMethod]
    public async Task ARunViewStopsListeningToItsRunAndStepsWhenRemoved()
    {
        var run = RunningRun();
        var step = run.Steps[0];
        var before = Subscribers(run);
        var stepBefore = Subscribers(step);

        RenderComponent<BitAiAgentRunView>(p => p.Add(c => c.Run, run));
        Assert.IsGreaterThan(before, Subscribers(run));
        Assert.IsGreaterThan(stepBefore, Subscribers(step));

        await Context.DisposeComponentsAsync();

        Assert.AreEqual(before, Subscribers(run));
        Assert.AreEqual(stepBefore, Subscribers(step));
    }

    [TestMethod]
    public void ARunViewSwitchesItsSubscriptionWhenGivenAnotherRun()
    {
        var first = RunningRun();
        var second = RunningRun();
        var before = Subscribers(first);

        var component = RenderComponent<BitAiAgentRunView>(p => p.Add(c => c.Run, first));
        component.Render(p => p.Add(c => c.Run, second));

        Assert.AreEqual(before, Subscribers(first));
        Assert.IsGreaterThan(0, Subscribers(second));
    }

    [TestMethod]
    public void AQuestionWithoutSuggestionsStillTakesAFreeTextAnswer()
    {
        var run = AskingRun();
        var answers = new List<string>();

        var component = RenderComponent<BitAiAgentRunView>(p => p
            .Add(c => c.Run, run)
            .Add(c => c.OnRespond, a => answers.Add(a)));

        Assert.IsEmpty(component.FindAll(".bit-aiarv-prm .bit-btn-dft-ter"));
        var input = component.Find(".bit-aiarv-ans input");
        Assert.AreEqual(component.Find(".bit-aiarv-qst").Id, input.GetAttribute("aria-labelledby"));
        Assert.AreEqual("Type your answer...", input.GetAttribute("placeholder"));

        input.Input("eu-west");
        component.Find(".bit-aiarv-ans input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        CollectionAssert.AreEqual(new[] { "eu-west" }, answers);
        // The field is emptied for the next question.
        Assert.AreEqual(string.Empty, component.Find(".bit-aiarv-ans input").GetAttribute("value") ?? string.Empty);
    }

    // The answer takes the question away with the button or the field that had the focus, which would fall to the page;
    // the steps, where the run goes on, take it instead.
    [TestMethod]
    public void AnAnswerTheRunTakesHandsTheFocusToTheSteps()
    {
        var run = AskingRun("eu-west");

        var component = RenderComponent<BitAiAgentRunView>(p => p
            .Add(c => c.Run, run)
            .Add(c => c.OnRespond, _ =>
            {
                run.ResolveInput();
                run.Steps[0].Complete(summary: "eu-west");
            }));

        // Read before the click: a re-render leaves the reference off the markup of an element it keeps.
        var steps = component.Find(".bit-aiarv-stl");
        Assert.AreEqual("-1", steps.GetAttribute("tabindex"));
        var reference = steps.GetAttribute("blazor:elementReference");

        component.Find(".bit-aiarv-prm > .bit-btn").Click();

        component.WaitForAssertion(() =>
        {
            Assert.IsEmpty(component.FindAll(".bit-aiarv-prm"));
            var focus = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"];
            Assert.HasCount(1, focus);
            Assert.AreEqual(reference, ((ElementReference)focus[0].Arguments[0]!).Id);
        }, TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void AnAnswerTheRunDoesNotTakeLeavesTheFocusWhereItIs()
    {
        var run = AskingRun("eu-west");

        var component = RenderComponent<BitAiAgentRunView>(p => p
            .Add(c => c.Run, run)
            .Add(c => c.OnRespond, _ => { }));

        component.Find(".bit-aiarv-prm > .bit-btn").Click();
        component.Render();

        Assert.HasCount(1, component.FindAll(".bit-aiarv-prm"));
        Assert.IsEmpty(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);

        // Nor does the run moving on later, after the answer was turned away, take the focus away from the page.
        run.ResolveInput();

        component.WaitForAssertion(() => Assert.IsEmpty(component.FindAll(".bit-aiarv-prm")), TimeSpan.FromSeconds(2));
        Assert.IsEmpty(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [TestMethod]
    public void TheCountOfTheStepsComesFromTheStringTable()
    {
        var run = RunningRun();
        run.Steps[0].Complete();

        var component = RenderComponent<BitAiAgentRunView>(p => p
            .Add(c => c.Run, run)
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { Strings = new CountStrings() }));

        Assert.AreEqual("1 of 2", component.Find(".bit-aiarv-cnt").TextContent);
        Assert.AreEqual("1 of 2", component.Find(".bit-aiarv-prg [role=progressbar]").GetAttribute("aria-valuetext"));
    }

    // A pace changed while the run is under way takes effect at once, not with the next run.
    [TestMethod]
    public void AClockIntervalChangedWhileTheRunGoesOnTakesEffect()
    {
        var run = RunningRun();
        var component = RenderComponent<BitAiAgentRunView>(p => p
            .Add(c => c.Run, run)
            .Add(c => c.ClockInterval, TimeSpan.FromMinutes(5)));

        component.Render(p => p.Add(c => c.Run, run).Add(c => c.ClockInterval, TimeSpan.FromMilliseconds(20)));
        var afterTheChange = component.Find(".bit-aiarv-elp").TextContent;

        // Nothing but the clock renders the view from here on, and a clock left at five minutes would not.
        component.WaitForAssertion(() => Assert.AreNotEqual(afterTheChange, component.Find(".bit-aiarv-elp").TextContent), TimeSpan.FromSeconds(3));
        Assert.AreEqual(TimeSpan.FromMilliseconds(20), typeof(BitAiAgentRunView)
            .GetField("_clockInterval", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(component.Instance));
    }

    [TestMethod]
    public void AnEmptyAnswerIsNotSent()
    {
        var run = AskingRun();
        var answers = new List<string>();

        var component = RenderComponent<BitAiAgentRunView>(p => p
            .Add(c => c.Run, run)
            .Add(c => c.OnRespond, a => answers.Add(a)));

        var send = component.Find(".bit-aiarv-ans .bit-btn");
        // A BitButton stays focusable while unavailable, so it says so through aria-disabled alone.
        Assert.AreEqual("true", send.GetAttribute("aria-disabled"));

        component.Find(".bit-aiarv-ans input").Input("   ");
        component.Find(".bit-aiarv-ans input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsEmpty(answers);
        Assert.AreEqual("true", component.Find(".bit-aiarv-ans .bit-btn").GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void TheSuggestionsAreNeutralButtonsAndTheAnswerButtonIsThePrimaryOne()
    {
        var run = AskingRun("eu-west", "us-east");

        var component = RenderComponent<BitAiAgentRunView>(p => p.Add(c => c.Run, run).Add(c => c.OnRespond, _ => { }));

        var suggestions = component.FindAll(".bit-aiarv-prm > .bit-btn");

        Assert.HasCount(2, suggestions);
        CollectionAssert.AreEqual(new[] { "eu-west", "us-east" }, suggestions.Select(s => s.TextContent.Trim()).ToArray());
        Assert.IsTrue(suggestions.All(s => s.ClassList.Contains("bit-btn-dft-ter") && s.ClassList.Contains("bit-btn-otl")));

        var send = component.Find(".bit-aiarv-ans .bit-btn");
        Assert.IsFalse(send.ClassList.Contains("bit-btn-dft-ter"));
        Assert.AreEqual("Answer", send.TextContent.Trim());
    }

    [TestMethod]
    public async Task AnAnswerGoesToTheCallbackOrTheSessionButNeverBoth()
    {
        var session = new BitAiChatSession(new SilentTransport());
        var run = session.Conversation.GetOrAddRun("run_1");
        run.SetPlan([new BitAiAgentPlanStep("ask", "Ask")]);
        run.RequestInput("Proceed?", ["Yes", "No"]);
        var answers = new List<string>();

        var component = RenderComponent<BitAiAgentRunView>(p => p
            .Add(c => c.Run, run)
            .Add(c => c.Session, session)
            .Add(c => c.OnRespond, a => answers.Add(a)));

        await component.Find(".bit-aiarv-prm > .bit-btn").ClickAsync(new MouseEventArgs());

        CollectionAssert.AreEqual(new[] { "Yes" }, answers);
        Assert.AreEqual(BitAiAgentRunStatus.WaitingForInput, run.Status);
        Assert.IsEmpty(session.Conversation.Messages);
    }

    [TestMethod]
    public async Task WithoutACallbackTheSessionAnswersTheRun()
    {
        var session = new BitAiChatSession(new SilentTransport());
        var run = session.Conversation.GetOrAddRun("run_1");
        run.SetPlan([new BitAiAgentPlanStep("ask", "Ask")]);
        run.RequestInput("Proceed?", ["Yes"]);

        var component = RenderComponent<BitAiAgentRunView>(p => p.Add(c => c.Run, run).Add(c => c.Session, session));
        await component.Find(".bit-aiarv-prm > .bit-btn").ClickAsync(new MouseEventArgs());

        component.WaitForAssertion(() => Assert.IsTrue(session.Conversation.Messages.Any(m => m.GetText() == "Yes")));
        Assert.AreEqual(1, session.Conversation.Messages.Count(m => m.Role == BitAiRole.User));
    }

    [TestMethod]
    public void TheElapsedTimeTicksWhileTheRunIsActiveAndStopsWhenItEnds()
    {
        var run = RunningRun();
        var component = RenderComponent<BitAiAgentRunView>(p => p
            .Add(c => c.Run, run)
            .Add(c => c.ClockInterval, TimeSpan.FromMilliseconds(20)));

        var first = component.Find(".bit-aiarv-elp").TextContent;
        component.WaitForAssertion(() => Assert.AreNotEqual(first, component.Find(".bit-aiarv-elp").TextContent), TimeSpan.FromSeconds(3));

        run.SetStatus(BitAiAgentRunStatus.Completed);
        component.WaitForAssertion(() => StringAssert.Contains(component.Markup, "Done"));
        var frozen = component.Find(".bit-aiarv-elp").TextContent;
        Thread.Sleep(80);
        component.Render();
        Assert.AreEqual(frozen, component.Find(".bit-aiarv-elp").TextContent);
    }

    [TestMethod]
    public void AnAgentRunShowsItsUsageWhenUsageDisplayIsOn()
    {
        var run = new BitAiAgentRun(agentName: "Researcher");

        var component = RenderComponent<BitAiAgentRunView>(p => p
            .Add(c => c.Run, run)
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { ShowUsage = true }));
        Assert.IsEmpty(component.FindAll(".bit-aiarv-hdr .bit-aituv"));

        run.AddUsage(new BitAiTokenUsage { InputTokens = 1200, OutputTokens = 300 });

        component.WaitForAssertion(() =>
            StringAssert.Contains(component.Find(".bit-aiarv-hdr .bit-aituv").TextContent, "1.5k"));
    }

    [TestMethod]
    public void AnAgentRunHidesItsUsageWhenUsageDisplayIsOff()
    {
        var run = new BitAiAgentRun(agentName: "Researcher");
        run.AddUsage(new BitAiTokenUsage { InputTokens = 1200, OutputTokens = 300 });

        var component = RenderComponent<BitAiAgentRunView>(p => p.Add(c => c.Run, run));

        Assert.IsEmpty(component.FindAll(".bit-aituv"));
    }

    [TestMethod]
    public void TheStatusOfTheRunIsAnnouncedInItsHeader()
    {
        var run = RunningRun();

        var component = RenderComponent<BitAiAgentRunView>(p => p.Add(c => c.Run, run));
        Assert.AreEqual("Running", component.Find(".bit-aiarv-hdr [role=status]").TextContent.Trim());

        run.RequestInput("Which region?");

        component.WaitForAssertion(() => Assert.AreEqual("Waiting for your answer", component.Find(".bit-aiarv-hdr [role=status]").TextContent.Trim()));
    }

    [TestMethod]
    public void AnErrorOfTheRunIsShownUnderTheSteps()
    {
        var run = RunningRun();
        run.SetError(new BitAiError("The tool timed out."));

        var component = RenderComponent<BitAiAgentRunView>(p => p.Add(c => c.Run, run));

        StringAssert.Contains(component.Find(".bit-aiarv-err").TextContent, "The tool timed out.");
    }

    [TestMethod]
    public void ADisabledRunViewTakesNoAnswer()
    {
        var run = AskingRun("eu-west");
        var answers = new List<string>();

        var component = RenderComponent<BitAiAgentRunView>(p => p
            .Add(c => c.Run, run)
            .Add(c => c.Disabled, true)
            .Add(c => c.OnRespond, a => answers.Add(a)));

        Assert.IsTrue(component.Find(".bit-aiarv").ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", component.Find(".bit-aiarv-prm > .bit-btn").GetAttribute("aria-disabled"));
        Assert.IsTrue(component.Find(".bit-aiarv-ans input").HasAttribute("disabled"));

        component.Find(".bit-aiarv-prm > .bit-btn").Click();

        Assert.IsEmpty(answers);
    }

    [TestMethod]
    public void BitAiAgentRunViewShouldRespectClassAndStyle()
    {
        var component = RenderComponent<BitAiAgentRunView>(p => p
            .Add(c => c.Run, RunningRun())
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin: 1px"));

        var root = component.Find(".bit-aiarv");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiAgentRunViewShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiAgentRunView>(p => p
            .Add(c => c.Run, RunningRun())
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aiarv").GetAttribute("style");

        if (expected is null)
        {
            Assert.IsNull(style);
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Auto, "auto")]
    public void BitAiAgentRunViewShouldRespectDir(BitDir? dir, string? expected)
    {
        var component = RenderComponent<BitAiAgentRunView>(p => p
            .Add(c => c.Run, RunningRun())
            .Add(c => c.Dir, dir));

        var root = component.Find(".bit-aiarv");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitAiAgentRunViewShouldHandTheClassesAndStylesToItsParts()
    {
        var component = RenderComponent<BitAiAgentRunView>(p => p
            .Add(c => c.Run, AskingRun("eu-west"))
            .Add(c => c.OnRespond, _ => { })
            .Add(c => c.Classes, new()
            {
                Root = "c-root",
                Header = "c-header",
                Body = "c-body",
                StepItem = "c-item",
                Step = new() { Root = "c-step" },
                StatusBadge = new() { Root = "c-badge" },
                Prompt = "c-prompt",
                SuggestionButton = new() { Root = "c-suggestion" },
                AnswerField = new() { Root = "c-field" },
                AnswerButton = new() { Root = "c-send" },
            })
            .Add(c => c.Styles, new() { Root = "color: red", Header = "color: blue" }));

        var root = component.Find(".bit-aiarv");
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        StringAssert.Contains(root.GetAttribute("style"), "color: red");
        Assert.AreEqual("color: blue", component.Find(".bit-aiarv-hdr").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aiarv-hdr").ClassList.Contains("c-header"));
        Assert.IsTrue(component.Find(".bit-aiarv-bdy").ClassList.Contains("c-body"));
        Assert.IsTrue(component.Find(".bit-aiarv-sti").ClassList.Contains("c-item"));
        Assert.IsTrue(component.Find(".bit-aiasv").ClassList.Contains("c-step"));
        Assert.IsTrue(component.Find(".bit-aiasb").ClassList.Contains("c-badge"));
        Assert.IsTrue(component.Find(".bit-aiarv-prm").ClassList.Contains("c-prompt"));
        Assert.IsTrue(component.Find(".bit-aiarv-prm > .bit-btn").ClassList.Contains("c-suggestion"));
        Assert.IsTrue(component.Find(".bit-aiarv-ans .bit-tfl").ClassList.Contains("c-field"));
        Assert.IsTrue(component.Find(".bit-aiarv-ans > .bit-btn").ClassList.Contains("c-send"));
    }

    [TestMethod]
    public void BitAiAgentRunViewShouldTakeTheCascadedParameters()
    {
        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams>
            {
                new BitAiAgentRunViewParams { Class = "cascaded", Classes = new() { Header = "cascaded-header" } }
            });
            p.AddChildContent<BitAiAgentRunView>(c => c.Add(x => x.Run, RunningRun()));
        });

        Assert.IsTrue(component.Find(".bit-aiarv").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-aiarv-hdr").ClassList.Contains("cascaded-header"));
    }

    [TestMethod]
    public void BitAiAgentRunViewParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiAgentRunView", BitAiAgentRunViewParams.ParamName);
        Assert.AreEqual(BitAiAgentRunViewParams.ParamName, new BitAiAgentRunViewParams().Name);
    }



    [TestMethod]
    public async Task AnAnswerTheSessionRefusesWhileItStillStreamsIsKeptAndTheAcceptedOneIsCleared()
    {
        var transport = new HeldTransport();
        var session = new BitAiChatSession(transport);
        var run = session.Conversation.GetOrAddRun("run_1");
        run.SetPlan([new BitAiAgentPlanStep("ask", "Ask")]);
        run.RequestInput("Which region?");

        // The turn that asked is still streaming: the session refuses an answer until it ends.
        var turn = session.SubmitAsync("migrate it");
        Assert.IsTrue(session.IsGenerating);

        var component = RenderComponent<BitAiAgentRunView>(p => p.Add(c => c.Run, run).Add(c => c.Session, session));

        component.Find(".bit-aiarv-ans input").Input("eu-west");
        await component.Find(".bit-aiarv-ans input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("eu-west", component.Find(".bit-aiarv-ans input").GetAttribute("value"));
        Assert.AreEqual(BitAiAgentRunStatus.WaitingForInput, run.Status);
        Assert.IsFalse(session.Conversation.Messages.Any(m => m.GetText() == "eu-west"));

        // Once the turn has ended, the same answer goes in, and the field is emptied for the next question.
        transport.Release();
        await turn;

        await component.Find(".bit-aiarv-ans .bit-btn").ClickAsync(new MouseEventArgs());

        component.WaitForAssertion(() => Assert.AreEqual(1, session.Conversation.Messages.Count(m => m.Role == BitAiRole.User && m.GetText() == "eu-west")));
        Assert.AreNotEqual(BitAiAgentRunStatus.WaitingForInput, run.Status);

        // The accepted answer was cleared: the field of the run's next question starts empty.
        run.RequestInput("Which zone?");

        component.WaitForAssertion(() => Assert.AreEqual(string.Empty, component.Find(".bit-aiarv-ans input").GetAttribute("value") ?? string.Empty));
    }

    // Streams nothing past its first turn's start until it is released; every later turn completes at once.
    private sealed class HeldTransport : IBitAiTransport
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _turns;

        public void Release() => _release.TrySetResult();

        public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new BitAiMessageStartedEvent();

            if (Interlocked.Increment(ref _turns) == 1) await _release.Task.WaitAsync(cancellationToken);

            yield return new BitAiMessageCompletedEvent();
        }
    }

    private sealed class CountStrings : BitAiStrings
    {
        public override string StepsCompleted(int done, int total) => $"{done} of {total}";
    }

    private sealed class SilentTransport : IBitAiTransport
    {
        public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new BitAiMessageCompletedEvent();
        }
    }
}
