using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

[TestClass]
public class BitAiConversationStreamProcessorTests
{
    private static (BitAiConversation Conversation, BitAiConversationStreamProcessor Processor) Create()
    {
        var conversation = new BitAiConversation();
        return (conversation, new BitAiConversationStreamProcessor(conversation));
    }

    [TestMethod]
    public void TextDeltasAccumulateIntoOnePart()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiTextDeltaEvent("Hello"));
        processor.Apply(new BitAiTextDeltaEvent(", world"));
        processor.EndGeneration();

        var message = Assert.ContainsSingle(conversation.Messages);
        var part = Assert.IsExactInstanceOfType<BitAiTextPart>(Assert.ContainsSingle(message.Parts));
        Assert.AreEqual("Hello, world", part.Text);
        Assert.IsFalse(part.IsStreaming);
        Assert.AreEqual(BitAiMessageStatus.Complete, message.Status);
    }

    [TestMethod]
    public void TextAfterAToolCallStartsANewPart()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiTextDeltaEvent("Looking that up."));
        processor.Apply(new BitAiToolCallStartedEvent("call1", "search"));
        processor.Apply(new BitAiToolCallCompletedEvent("call1", JsonDocument.Parse("""{"q":"x"}""").RootElement));
        processor.Apply(new BitAiToolResultEvent("call1", BitAiToolOutcome.FromText("found")));
        processor.Apply(new BitAiTextDeltaEvent("Here it is."));
        processor.EndGeneration();

        var message = Assert.ContainsSingle(conversation.Messages);
        Assert.HasCount(3, message.Parts);
        Assert.AreEqual("Looking that up.", Assert.IsExactInstanceOfType<BitAiTextPart>(message.Parts[0]).Text);
        Assert.AreEqual("call1", Assert.IsExactInstanceOfType<BitAiToolCallPart>(message.Parts[1]).ToolCallId);
        Assert.AreEqual("Here it is.", Assert.IsExactInstanceOfType<BitAiTextPart>(message.Parts[2]).Text);
    }

    // A call the conversation holds - in the message being generated or an earlier one - takes the result itself. A
    // result for a call the conversation has not seen never becomes a part of its own: it is held for that call (see
    // BitAiHeldToolResultTests), and dropped, leaving an error, when the call never starts.
    [TestMethod]
    public void AResultIsReconciledOntoItsCallAndOneForAnUnseenCallIsNeverAPartOfItsOwn()
    {
        var conversation = new BitAiConversation();
        var clock = new BitAiManualTimeProvider();
        var processor = new BitAiConversationStreamProcessor(conversation, clock);

        processor.BeginGeneration();
        processor.Apply(new BitAiToolCallStartedEvent("call1", "search") { Arguments = JsonDocument.Parse("{}").RootElement });
        processor.EndGeneration();

        processor.BeginGeneration();
        processor.Apply(new BitAiToolResultEvent("call1", BitAiToolOutcome.FromText("found")));
        processor.Apply(new BitAiToolResultEvent("call2", BitAiToolOutcome.FromText("remote")));
        processor.EndGeneration();

        Assert.AreEqual("found", conversation.Messages[0].FindToolCall("call1")!.Result!.Text);
        Assert.IsEmpty(conversation.Messages[0].PartsOf<BitAiToolResultPart>());
        Assert.IsEmpty(conversation.Messages[1].PartsOf<BitAiToolResultPart>());
        Assert.IsEmpty(conversation.Messages[1].PartsOf<BitAiErrorPart>());

        clock.Advance(processor.ToolResultHoldTime);

        Assert.IsEmpty(conversation.Messages[1].PartsOf<BitAiToolResultPart>());
        var dropped = Assert.ContainsSingle(conversation.Messages[1].PartsOf<BitAiErrorPart>());
        Assert.AreEqual("call2", Assert.IsExactInstanceOfType<BitAiOrphanedToolResultException>(dropped.Error.Exception).ToolCallId);
    }

    [TestMethod]
    public void ReasoningGoesToItsOwnPartAndOutOfTheTextProjection()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiReasoningDeltaEvent("weighing options"));
        processor.Apply(new BitAiTextDeltaEvent("The answer is 4."));
        processor.EndGeneration();

        var message = Assert.ContainsSingle(conversation.Messages);
        Assert.AreEqual(2, message.Parts.Count);
        Assert.IsExactInstanceOfType<BitAiReasoningPart>(message.Parts[0]);
        Assert.AreEqual("The answer is 4.", message.GetText());
    }

    [TestMethod]
    public void ToolCallArgumentsStreamAndParseOnCompletion()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiToolCallStartedEvent("c1", "get_weather"));
        processor.Apply(new BitAiToolCallDeltaEvent("c1", """{"city":"Sea"""));

        var call = conversation.Messages[0].FindToolCall("c1");
        Assert.IsNotNull(call);
        Assert.AreEqual("Sea", call!.DisplayArguments!.Value.GetProperty("city").GetString());

        processor.Apply(new BitAiToolCallDeltaEvent("c1", """ttle"}"""));
        processor.Apply(new BitAiToolCallCompletedEvent("c1"));

        Assert.AreEqual(BitAiToolCallStatus.Pending, call.Status);
        Assert.AreEqual("Seattle", call.Arguments!.Value.GetProperty("city").GetString());
    }

    [TestMethod]
    public void ToolCallReadyFiresOnceArgumentsComplete()
    {
        var (_, processor) = Create();
        processor.BeginGeneration();

        BitAiToolCallPart? ready = null;
        processor.ToolCallReady += part => ready = part;

        processor.Apply(new BitAiToolCallStartedEvent("c1", "search"));
        Assert.IsNull(ready);

        processor.Apply(new BitAiToolCallCompletedEvent("c1"));
        Assert.IsNotNull(ready);
        Assert.AreEqual("search", ready!.ToolName);
    }

    [TestMethod]
    public void CitationsAreNumberedAndDeduplicatedBySource()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        var source = new BitAiCitationSource("s1", "Docs");
        processor.Apply(new BitAiCitationEvent(new BitAiCitation("c1", source)));
        processor.Apply(new BitAiCitationEvent(new BitAiCitation("c2", new BitAiCitationSource("s2", "Other"))));
        processor.Apply(new BitAiCitationEvent(new BitAiCitation("c3", source)));

        var message = conversation.Messages[0];
        Assert.AreEqual(3, message.Citations.Count);
        Assert.AreEqual(1, message.Citations[0].Number);
        Assert.AreEqual(2, message.Citations[1].Number);
        Assert.AreEqual(1, message.Citations[2].Number);
        Assert.AreEqual(2, message.GetSources().Count);
    }

    [TestMethod]
    public void ArtifactsStreamOntoTheConversationAndAreReferencedByTheMessage()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiArtifactCreatedEvent("a1", BitAiArtifactKind.Code, "Solver") { Language = "csharp" });
        processor.Apply(new BitAiArtifactDeltaEvent("a1", "int Add("));
        processor.Apply(new BitAiArtifactDeltaEvent("a1", "int a, int b) => a + b;"));
        processor.Apply(new BitAiArtifactCompletedEvent("a1") { Summary = "first draft" });

        var artifact = conversation.FindArtifact("a1");
        Assert.IsNotNull(artifact);
        Assert.AreEqual("int Add(int a, int b) => a + b;", artifact!.Content);
        Assert.IsFalse(artifact.IsStreaming);
        Assert.ContainsSingle(artifact.Versions);
        Assert.Contains(p => p is BitAiArtifactPart { ArtifactId: "a1" }, conversation.Messages[0].Parts);
    }

    [TestMethod]
    public void RevisingAnArtifactKeepsTheEarlierVersion()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiArtifactCreatedEvent("a1", BitAiArtifactKind.Document, "Notes"));
        processor.Apply(new BitAiArtifactDeltaEvent("a1", "first"));
        processor.Apply(new BitAiArtifactCompletedEvent("a1"));

        processor.Apply(new BitAiArtifactCreatedEvent("a1", BitAiArtifactKind.Document, "Notes"));
        processor.Apply(new BitAiArtifactDeltaEvent("a1", "second"));
        processor.Apply(new BitAiArtifactCompletedEvent("a1"));

        var artifact = conversation.FindArtifact("a1")!;
        Assert.AreEqual(2, artifact.Version);
        Assert.AreEqual("second", artifact.Content);
        Assert.AreEqual("first", artifact.Versions[0].Content);
    }

    [TestMethod]
    public void AgentPlanAndStepsBuildARun()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiAgentRunStartedEvent("r1") { AgentName = "Researcher", Goal = "Find sources" });
        processor.Apply(new BitAiAgentPlanEvent("r1",
        [
            new BitAiAgentPlanStep("s1", "Search"),
            new BitAiAgentPlanStep("s2", "Summarise"),
        ]));
        processor.Apply(new BitAiAgentStepStartedEvent("r1", "s1"));
        processor.Apply(new BitAiAgentStepCompletedEvent("r1", "s1") { Summary = "8 results" });
        processor.Apply(new BitAiAgentRunCompletedEvent("r1"));

        var run = conversation.FindRun("r1")!;
        Assert.AreEqual("Researcher", run.AgentName);
        Assert.AreEqual(2, run.Steps.Count);
        Assert.AreEqual(BitAiAgentStepStatus.Completed, run.Steps[0].Status);
        Assert.AreEqual("8 results", run.Steps[0].Summary);
        Assert.AreEqual(BitAiAgentRunStatus.Completed, run.Status);
        Assert.AreEqual(0.5, run.Progress);
    }

    [TestMethod]
    public void ToolCallsAreRecordedOnTheRunningStep()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiAgentPlanEvent("r1",
        [
            new BitAiAgentPlanStep("s1", "Search"),
            new BitAiAgentPlanStep("s2", "Summarise"),
        ]));
        processor.Apply(new BitAiToolCallStartedEvent("call_0", "search"));
        processor.Apply(new BitAiAgentStepStartedEvent("r1", "s1"));
        processor.Apply(new BitAiToolCallStartedEvent("call_1", "search"));
        processor.Apply(new BitAiToolCallStartedEvent("call_2", "search"));
        processor.Apply(new BitAiAgentStepCompletedEvent("r1", "s1"));
        processor.Apply(new BitAiToolCallStartedEvent("call_3", "search"));

        var run = conversation.FindRun("r1")!;
        CollectionAssert.AreEqual(new[] { "call_1", "call_2" }, run.Steps[0].ToolCallIds.ToArray());
        Assert.IsEmpty(run.Steps[1].ToolCallIds);
    }

    [TestMethod]
    public void PlanRevisionPreservesStateOfSurvivingSteps()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiAgentPlanEvent("r1", [new BitAiAgentPlanStep("s1", "Search")]));
        processor.Apply(new BitAiAgentStepStartedEvent("r1", "s1"));
        processor.Apply(new BitAiAgentStepCompletedEvent("r1", "s1"));
        processor.Apply(new BitAiAgentPlanEvent("r1",
        [
            new BitAiAgentPlanStep("s1", "Search the web"),
            new BitAiAgentPlanStep("s2", "Summarise"),
        ]));

        var run = conversation.FindRun("r1")!;
        Assert.AreEqual("Search the web", run.Steps[0].Title);
        Assert.AreEqual(BitAiAgentStepStatus.Completed, run.Steps[0].Status);
        Assert.AreEqual(BitAiAgentStepStatus.Pending, run.Steps[1].Status);
    }

    [TestMethod]
    public void TerminalErrorFailsTheMessageAndClosesOpenParts()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiTextDeltaEvent("partial"));
        processor.Apply(new BitAiErrorEvent(new BitAiError("rate limited") { Kind = BitAiErrorKind.RateLimit, IsRetryable = true }));

        var message = conversation.Messages[0];
        Assert.AreEqual(BitAiMessageStatus.Failed, message.Status);
        Assert.AreEqual("rate limited", message.Error!.Message);
        Assert.IsFalse(Assert.IsExactInstanceOfType<BitAiTextPart>(message.Parts[0]).IsStreaming);
    }

    [TestMethod]
    public void NonTerminalErrorBecomesAPartAndLeavesTheMessageAlive()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiTextDeltaEvent("one tool failed"));
        processor.Apply(new BitAiErrorEvent(new BitAiError("tool blew up"), isTerminal: false));
        processor.EndGeneration();

        var message = conversation.Messages[0];
        Assert.AreEqual(BitAiMessageStatus.Complete, message.Status);
        Assert.Contains(p => p is BitAiErrorPart, message.Parts);
    }

    [TestMethod]
    public void UsageAndMetadataAreMergedNotReplaced()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiMetadataEvent(new BitAiGenerationMetadata { ModelId = "m1" }));
        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 10, OutputTokens = 5 }));
        processor.Apply(new BitAiMessageCompletedEvent(BitAiFinishReason.Stop));
        processor.EndGeneration();

        var metadata = conversation.Messages[0].Metadata!;
        Assert.AreEqual("m1", metadata.ModelId);
        Assert.AreEqual(15, metadata.Usage.TotalTokens);
        Assert.AreEqual(BitAiFinishReason.Stop, metadata.FinishReason);
        Assert.IsNotNull(metadata.CompletedAt);
    }

    [TestMethod]
    public void RepeatedUsageUpdatesWithinAStreamReplaceRatherThanAdd()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiAgentRunStartedEvent("run1"));
        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 100, OutputTokens = 1 }));
        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 100, OutputTokens = 20 }));
        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 100, OutputTokens = 42, Cost = 0.01m }));
        processor.EndGeneration();

        var expected = new BitAiTokenUsage { InputTokens = 100, OutputTokens = 42, Cost = 0.01m };
        Assert.AreEqual(expected, conversation.Messages[0].Metadata!.Usage);
        Assert.AreEqual(expected, conversation.FindRun("run1")!.Usage);
    }

    [TestMethod]
    public void UsageFromSeparateStreamsAddsUpFieldByField()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiAgentRunStartedEvent("run1"));
        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 100, OutputTokens = 10, ReasoningTokens = 5, Cost = 0.01m }));
        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 100, OutputTokens = 30, ReasoningTokens = 8, Cost = 0.02m }));

        processor.BeginStream();
        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 150, OutputTokens = 5, CachedInputTokens = 100, Cost = 0.005m }));
        processor.Apply(new BitAiMetadataEvent(new BitAiGenerationMetadata
        {
            ModelId = "m1",
            Usage = new BitAiTokenUsage { InputTokens = 150, OutputTokens = 12, CachedInputTokens = 100, Cost = 0.006m },
        }));
        processor.EndGeneration();

        var expected = new BitAiTokenUsage
        {
            InputTokens = 250,
            OutputTokens = 42,
            CachedInputTokens = 100,
            ReasoningTokens = 8,
            Cost = 0.026m,
        };

        var metadata = conversation.Messages[0].Metadata!;
        Assert.AreEqual(expected, metadata.Usage);
        Assert.IsNull(metadata.Usage.CacheWriteTokens);
        Assert.AreEqual("m1", metadata.ModelId);
        Assert.AreEqual(expected, conversation.FindRun("run1")!.Usage);
    }

    [TestMethod]
    public void ARunSpanningSeveralTurnsSumsTheUsageOfEach()
    {
        var (conversation, processor) = Create();

        processor.BeginGeneration();
        processor.Apply(new BitAiAgentRunStartedEvent("run1"));
        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 10, OutputTokens = 2 }));
        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 10, OutputTokens = 4 }));
        processor.EndGeneration();

        processor.BeginGeneration();
        processor.Apply(new BitAiAgentStepStartedEvent("run1", "s2"));
        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 20, OutputTokens = 1 }));
        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 20, OutputTokens = 3 }));
        processor.EndGeneration();

        Assert.AreEqual(new BitAiTokenUsage { InputTokens = 10, OutputTokens = 4 }, conversation.Messages[0].Metadata!.Usage);
        Assert.AreEqual(new BitAiTokenUsage { InputTokens = 20, OutputTokens = 3 }, conversation.Messages[1].Metadata!.Usage);
        Assert.AreEqual(new BitAiTokenUsage { InputTokens = 30, OutputTokens = 7 }, conversation.FindRun("run1")!.Usage);
        Assert.AreEqual(conversation.TotalUsage, conversation.FindRun("run1")!.Usage);
    }

    [TestMethod]
    public void StatusTextIsTransientAndClearedOnCompletion()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiStatusEvent("Searching the web"));
        Assert.AreEqual("Searching the web", conversation.Messages[0].StatusText);

        processor.Apply(new BitAiMessageCompletedEvent());
        Assert.IsNull(conversation.Messages[0].StatusText);
    }

    [TestMethod]
    public void TheAnswerStreamingInSupersedesTheStatusLine()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiStatusEvent("Searching the web"));
        processor.Apply(new BitAiTextDeltaEvent("Here"));

        Assert.IsNull(conversation.Messages[0].StatusText);
        Assert.AreEqual(BitAiMessageStatus.Streaming, conversation.Messages[0].Status);
    }

    [TestMethod]
    public void AStatusSentBetweenPiecesOfTheAnswerShowsUntilTheAnswerMovesOn()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();
        processor.Apply(new BitAiTextDeltaEvent("Let me check. "));
        processor.Apply(new BitAiToolCallStartedEvent("c1", "search") { Arguments = JsonDocument.Parse("{}").RootElement });

        processor.Apply(new BitAiStatusEvent("Running search"));
        processor.Apply(new BitAiToolProgressEvent("c1", new BitAiToolProgress(0.5, "page 1")));
        processor.Apply(new BitAiToolResultEvent("c1", BitAiToolOutcome.FromText("found")));
        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 1 }));
        Assert.AreEqual("Running search", conversation.Messages[0].StatusText);

        processor.Apply(new BitAiTextDeltaEvent("Found it."));
        Assert.IsNull(conversation.Messages[0].StatusText);
    }

    [TestMethod,
        DataRow("reasoning"),
        DataRow("part"),
        DataRow("image"),
        DataRow("tool"),
        DataRow("structured"),
        DataRow("generative")]
    public void EveryPieceOfTheAnswerSupersedesTheStatusLine(string piece)
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();
        processor.Apply(new BitAiStatusEvent("Working"));

        processor.Apply(piece switch
        {
            "reasoning" => new BitAiReasoningDeltaEvent("hmm"),
            "part" => new BitAiPartStartedEvent(BitAiMessagePartKinds.Code),
            "image" => new BitAiImageEvent("https://img/1.png"),
            "tool" => new BitAiToolCallStartedEvent("c1", "search"),
            "structured" => new BitAiStructuredDataDeltaEvent("{\"a\":"),
            _ => new BitAiGenerativeUiEvent("card", JsonDocument.Parse("{}").RootElement),
        });

        Assert.IsNull(conversation.Messages[0].StatusText);
    }

    [TestMethod]
    public void ExplicitPartIdsKeepParallelPartsSeparate()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiTextDeltaEvent("left ") { PartId = "p1" });
        processor.Apply(new BitAiTextDeltaEvent("right ") { PartId = "p2" });
        processor.Apply(new BitAiTextDeltaEvent("side") { PartId = "p1" });

        var message = conversation.Messages[0];
        Assert.AreEqual(2, message.Parts.Count);
        Assert.AreEqual("left side", message.FindPart<BitAiTextPart>("p1")!.Text);
        Assert.AreEqual("right ", message.FindPart<BitAiTextPart>("p2")!.Text);
    }

    [TestMethod]
    public void StructuredDataStreamsAndIsReadableWhilePartial()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiStructuredDataDeltaEvent("""{"name":"Ada""") { SchemaName = "person" });
        var part = Assert.IsExactInstanceOfType<BitAiStructuredDataPart>(conversation.Messages[0].Parts[0]);
        Assert.AreEqual("Ada", part.Value!.Value.GetProperty("name").GetString());

        processor.Apply(new BitAiStructuredDataDeltaEvent("\",\"age\":36}"));
        processor.Apply(new BitAiPartCompletedEvent { PartId = part.PartId });

        Assert.IsFalse(part.IsStreaming);
        Assert.AreEqual(36, part.Value!.Value.GetProperty("age").GetInt32());
        Assert.AreEqual("person", part.SchemaName);
    }

    [TestMethod]
    public void AToolCallIsReportedReadyOnlyOnce()
    {
        var (_, processor) = Create();
        var ready = 0;
        processor.ToolCallReady += _ => ready++;
        processor.BeginGeneration();

        var arguments = JsonDocument.Parse("""{"q":"x"}""").RootElement;
        processor.Apply(new BitAiToolCallStartedEvent("call1", "search") { Arguments = arguments });
        processor.Apply(new BitAiToolCallCompletedEvent("call1", arguments));
        processor.Apply(new BitAiToolCallStartedEvent("call2", "search"));
        processor.Apply(new BitAiToolCallDeltaEvent("call2", """{"q":"y"}"""));
        processor.Apply(new BitAiToolCallCompletedEvent("call2"));
        processor.Apply(new BitAiToolCallCompletedEvent("call2"));

        Assert.AreEqual(2, ready);
    }

    [TestMethod]
    [DataRow(BitAiMessageStatus.Cancelled, BitAiAgentRunStatus.Cancelled)]
    [DataRow(BitAiMessageStatus.Failed, BitAiAgentRunStatus.Failed)]
    public void AnInterruptedGenerationEndsTheRunningAgentRun(BitAiMessageStatus ending, BitAiAgentRunStatus expected)
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiAgentRunStartedEvent("run1"));
        processor.Apply(new BitAiAgentStepStartedEvent("run1", "search"));
        processor.EndGeneration(ending);

        Assert.AreEqual(expected, conversation.Runs["run1"].Status);
    }

    [TestMethod]
    public void ARunWaitingForInputSurvivesTheEndOfTheMessage()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiAgentRunStartedEvent("run1"));
        processor.Apply(new BitAiAgentInputRequestedEvent("run1", "When?"));
        processor.Apply(new BitAiMessageCompletedEvent());
        processor.EndGeneration();

        Assert.AreEqual(BitAiAgentRunStatus.WaitingForInput, conversation.Runs["run1"].Status);
    }

    [TestMethod]
    public void PlannedStepsReportTheirProgressThroughTheRun()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();
        processor.Apply(new BitAiAgentRunStartedEvent("run1"));
        processor.Apply(new BitAiAgentPlanEvent("run1", [new BitAiAgentPlanStep("a", "A"), new BitAiAgentPlanStep("b", "B")]));

        var run = conversation.Runs["run1"];
        var changes = 0;
        run.Changed += _ => changes++;

        processor.Apply(new BitAiAgentStepStartedEvent("run1", "a"));
        processor.Apply(new BitAiAgentStepCompletedEvent("run1", "a"));

        Assert.IsTrue(changes >= 2, "A planned step changing must notify observers of the run.");
        Assert.AreEqual(0.5, run.Progress);
    }

    [TestMethod]
    public void APlanThatRepeatsAStepIdentifierDoesNotBreakTheNextRevision()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiAgentPlanEvent("run1", [new BitAiAgentPlanStep("a", "A"), new BitAiAgentPlanStep("a", "A again")]));
        processor.Apply(new BitAiAgentStepCompletedEvent("run1", "a"));
        processor.Apply(new BitAiAgentPlanEvent("run1", [new BitAiAgentPlanStep("a", "A"), new BitAiAgentPlanStep("b", "B")]));

        var steps = conversation.Runs["run1"].Steps;
        CollectionAssert.AreEqual(new[] { "a", "b" }, steps.Select(s => s.StepId).ToArray());
        Assert.AreEqual(BitAiAgentStepStatus.Completed, steps[0].Status);
    }

    [TestMethod]
    public void AnArtifactCreatedWithContentAndThenCompletedRecordsOneVersion()
    {
        var (conversation, processor) = Create();
        processor.BeginGeneration();

        processor.Apply(new BitAiArtifactCreatedEvent("art1", BitAiArtifactKind.Document, "Notes") { Content = "body" });
        processor.Apply(new BitAiArtifactCompletedEvent("art1"));

        Assert.ContainsSingle(conversation.FindArtifact("art1")!.Versions);
    }
}
