using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

[TestClass]
public class BitAiAgentRunTests
{
    private static BitAiAgentRun ThreeSteps()
    {
        var run = new BitAiAgentRun("run1", "Deploy agent");
        run.SetPlan([new BitAiAgentPlanStep("build", "Build"), new BitAiAgentPlanStep("test", "Test"), new BitAiAgentPlanStep("deploy", "Deploy")]);
        run.SetStatus(BitAiAgentRunStatus.Running);
        return run;
    }



    // A run that ends without finishing reaches none of the steps left: they are abandoned rather than left pending - or
    // running, with a clock that keeps going - under a run that has ended.
    [TestMethod,
        DataRow(BitAiAgentRunStatus.Failed),
        DataRow(BitAiAgentRunStatus.Cancelled)]
    public void ARunThatEndsUnfinishedSkipsTheStepsItDidNotFinish(BitAiAgentRunStatus status)
    {
        var run = ThreeSteps();
        run.Steps[0].Start();
        run.Steps[0].Complete(summary: "Built");
        run.Steps[1].Start();

        run.SetStatus(status);

        CollectionAssert.AreEqual(
            new[] { BitAiAgentStepStatus.Completed, BitAiAgentStepStatus.Skipped, BitAiAgentStepStatus.Skipped },
            run.Steps.Select(s => s.Status).ToArray());
        Assert.AreEqual("Built", run.Steps[0].Summary);
        Assert.IsNotNull(run.Steps[1].CompletedAt);
    }

    [TestMethod]
    public void AFailedStepStaysFailedWhenTheRunFails()
    {
        var run = ThreeSteps();
        run.Steps[0].Start();
        run.Steps[0].Complete(BitAiAgentStepStatus.Failed, error: new BitAiError("3 tests failed."));

        run.SetError(new BitAiError("The run stopped."));

        Assert.AreEqual(BitAiAgentRunStatus.Failed, run.Status);
        Assert.AreEqual(BitAiAgentStepStatus.Failed, run.Steps[0].Status);
        Assert.AreEqual("3 tests failed.", run.Steps[0].Error?.Message);
        Assert.AreEqual(BitAiAgentStepStatus.Skipped, run.Steps[1].Status);
        Assert.AreEqual(BitAiAgentStepStatus.Skipped, run.Steps[2].Status);
    }

    [TestMethod]
    public void ACompletedRunLeavesItsStepsAsTheyAre()
    {
        var run = ThreeSteps();
        run.Steps[0].Start();

        run.SetStatus(BitAiAgentRunStatus.Completed);

        Assert.AreEqual(BitAiAgentStepStatus.Running, run.Steps[0].Status);
        Assert.AreEqual(BitAiAgentStepStatus.Pending, run.Steps[1].Status);
    }

    [TestMethod]
    public void AnInterruptedGenerationSkipsTheStepItStoppedIn()
    {
        var conversation = new BitAiConversation();
        var processor = new BitAiConversationStreamProcessor(conversation);
        processor.BeginGeneration();

        processor.Apply(new BitAiAgentRunStartedEvent("run1"));
        processor.Apply(new BitAiAgentPlanEvent("run1", [new BitAiAgentPlanStep("search", "Search"), new BitAiAgentPlanStep("write", "Write")]));
        processor.Apply(new BitAiAgentStepStartedEvent("run1", "search"));
        processor.EndGeneration(BitAiMessageStatus.Cancelled);

        var run = conversation.Runs["run1"];
        Assert.AreEqual(BitAiAgentRunStatus.Cancelled, run.Status);
        Assert.IsTrue(run.Steps.All(s => s.Status is BitAiAgentStepStatus.Skipped));
    }

    // A step waits for the answer with its run, rather than being drawn as running under a run that says it waits.
    [TestMethod]
    public void AQuestionBlocksTheRunningStepAndItsAnswerResumesIt()
    {
        var run = ThreeSteps();
        run.Steps[0].Start();
        var startedAt = run.Steps[0].StartedAt;

        run.RequestInput("Which environment?", ["Staging", "Production"]);

        Assert.AreEqual(BitAiAgentRunStatus.WaitingForInput, run.Status);
        Assert.AreEqual(BitAiAgentStepStatus.Blocked, run.Steps[0].Status);
        Assert.AreEqual(BitAiAgentStepStatus.Pending, run.Steps[1].Status);
        Assert.AreSame(run.Steps[0], run.CurrentStep);

        run.ResolveInput();

        Assert.AreEqual(BitAiAgentRunStatus.Running, run.Status);
        Assert.AreEqual(BitAiAgentStepStatus.Running, run.Steps[0].Status);
        Assert.AreEqual(startedAt, run.Steps[0].StartedAt);
        Assert.AreEqual(BitAiAgentStepStatus.Pending, run.Steps[1].Status);
        Assert.IsNull(run.PendingPrompt);
    }

    [TestMethod]
    public void AQuestionLeavesTheFinishedStepsAsTheyAre()
    {
        var run = ThreeSteps();
        run.Steps[0].Start();
        run.Steps[0].Complete();

        run.RequestInput("Which environment?");
        run.ResolveInput();

        Assert.AreEqual(BitAiAgentStepStatus.Completed, run.Steps[0].Status);
        Assert.AreEqual(BitAiAgentStepStatus.Pending, run.Steps[1].Status);
    }
}
