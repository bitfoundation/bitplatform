using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

/// <summary>
/// Regression tests for defects found in the first full review. Each test name states the
/// guarantee that was missing.
/// </summary>
[TestClass]
public class BitAiReviewRegressionTests
{
    [TestMethod]
    public void AToolCallAnnouncedWithArgumentsAndThenCompletedIsReadyOnlyOnce()
    {
        var conversation = new BitAiConversation();
        var processor = new BitAiConversationStreamProcessor(conversation);
        var fired = 0;
        processor.ToolCallReady += _ => fired++;

        processor.BeginGeneration();
        var args = JsonDocument.Parse("""{"q":1}""").RootElement;
        processor.Apply(new BitAiToolCallStartedEvent("c1", "search") { Arguments = args });
        processor.Apply(new BitAiToolCallCompletedEvent("c1", args));
        processor.Apply(new BitAiToolCallCompletedEvent("c1"));

        Assert.AreEqual(1, fired);
    }

    [TestMethod]
    public async Task ACallTheTransportAlreadyAnsweredIsNotExecutedAgainLocally()
    {
        var executions = 0;
        var tools = new BitAiToolRegistry([BitAiTool.Create<JsonElement, string>("lookup", "look", (_, _) =>
        {
            executions++;
            return ValueTask.FromResult("local");
        })]);

        var transport = new ScriptedTransport(
        [
            new BitAiMessageStartedEvent(),
            new BitAiToolCallStartedEvent("c1", "lookup") { Arguments = JsonDocument.Parse("{}").RootElement },
            new BitAiToolResultEvent("c1", BitAiToolOutcome.FromText("remote")),
            new BitAiMessageCompletedEvent(),
        ]);

        var session = new BitAiChatSession(transport, tools: tools, options: new BitAiChatSessionOptions { Pacing = null });
        var reply = await session.SubmitAsync("go");

        Assert.AreEqual(0, executions);
        Assert.AreEqual("remote", reply!.FindToolCall("c1")!.Result!.Text);
        Assert.AreEqual(1, transport.Calls);
    }

    [TestMethod]
    public void CreatedWithContentThenCompletedProducesOneVersion()
    {
        var conversation = new BitAiConversation();
        var processor = new BitAiConversationStreamProcessor(conversation);
        processor.BeginGeneration();

        processor.Apply(new BitAiArtifactCreatedEvent("a1", BitAiArtifactKind.Code, "t") { Content = "x" });
        processor.Apply(new BitAiArtifactCompletedEvent("a1") { Summary = "first draft" });

        var version = Assert.ContainsSingle(conversation.Artifacts["a1"].Versions);
        Assert.AreEqual("first draft", version.Summary);
    }

    [TestMethod]
    public void AMessageHoldingOnlyReasoningHasNoPlainText()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        message.AddPart(new BitAiReasoningPart("private"));
        Assert.AreEqual(string.Empty, message.GetText());
    }

    [TestMethod]
    public void PiecemealUsageReportsAreMergedFieldByField()
    {
        var conversation = new BitAiConversation();
        var processor = new BitAiConversationStreamProcessor(conversation);
        processor.BeginGeneration();
        processor.Apply(new BitAiAgentRunStartedEvent("r1"));

        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 100 }));
        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { OutputTokens = 40 }));
        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { OutputTokens = 50 }));

        var usage = conversation.Messages[0].Metadata!.Usage;
        Assert.AreEqual(100, usage.InputTokens);
        Assert.AreEqual(50, usage.OutputTokens);
        Assert.AreEqual(150, conversation.Runs["r1"].Usage.TotalTokens);
    }

    [TestMethod]
    public void StepsCreatedByAPlanNotifyTheRunWhenTheyChange()
    {
        var run = new BitAiAgentRun("r1");
        run.SetPlan([new BitAiAgentPlanStep("s1", "First")]);
        var before = run.Version;

        run.Steps[0].Start();

        Assert.IsTrue(run.Version > before);
    }

    [TestMethod]
    public async Task PacingKeepsSmoothingWhenCompletionArrivesRightAfterTheText()
    {
        var deltas = 0;
        var sawCompletedLast = false;
        await foreach (var evt in Burst().Paced(new BitAiTextPacingOptions { Interval = TimeSpan.FromMilliseconds(1) }))
        {
            if (evt is BitAiTextDeltaEvent) deltas++;
            sawCompletedLast = evt is BitAiMessageCompletedEvent;
        }

        Assert.IsTrue(deltas > 3, $"expected several paced deltas, got {deltas}");
        Assert.IsTrue(sawCompletedLast);
    }

    [TestMethod]
    public async Task ARenderRequestedDuringAManualFlushIsNotLost()
    {
        var renders = 0;
        var gate = new TaskCompletionSource();
        BitAiRenderScheduler? scheduler = null;
        scheduler = new BitAiRenderScheduler(async () =>
        {
            renders++;
            if (renders == 1)
            {
                scheduler!.Request();
                await gate.Task;
            }
        }, TimeSpan.FromMilliseconds(5));

        var flush = scheduler.FlushAsync();
        gate.SetResult();
        await flush;

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (renders < 2 && DateTime.UtcNow < deadline) await Task.Delay(10);

        Assert.AreEqual(2, renders);
        await scheduler.DisposeAsync();
    }

    [TestMethod]
    public void ANumberEndingInADecimalPointIsNotTreatedAsComplete()
    {
        var repaired = BitAiPartialJson.TryParse("""{"a": 1.""");
        Assert.IsNotNull(repaired);
        Assert.AreEqual(JsonValueKind.Object, repaired!.Value.ValueKind);
    }

    [TestMethod]
    public async Task TheTimeoutCoversTheTransportOnlyAndBecomesARetryableError()
    {
        var transport = new HangingTransport();
        var session = new BitAiChatSession(transport, options: new BitAiChatSessionOptions
        {
            Pacing = null,
            Timeout = TimeSpan.FromMilliseconds(50),
        });

        var reply = await session.SubmitAsync("hello");

        Assert.AreEqual(BitAiMessageStatus.Failed, reply!.Status);
        Assert.AreEqual(BitAiErrorKind.Timeout, session.LastError!.Kind);
        Assert.IsTrue(session.LastError.IsRetryable);
    }

    [TestMethod]
    public void RemovingMessagesPrunesTheArtifactsAndRunsTheyOwned()
    {
        var conversation = new BitAiConversation();
        var processor = new BitAiConversationStreamProcessor(conversation);
        conversation.AddMessage(BitAiRole.User, "write");
        var assistant = processor.BeginGeneration();
        processor.Apply(new BitAiAgentRunStartedEvent("r1"));
        processor.Apply(new BitAiArtifactCreatedEvent("a1", BitAiArtifactKind.Document, "Doc") { Content = "hi" });
        processor.Apply(new BitAiMessageCompletedEvent());
        processor.EndGeneration();

        Assert.ContainsSingle(conversation.Artifacts);
        Assert.ContainsSingle(conversation.Runs);

        conversation.TruncateAfter(assistant.MessageId, inclusive: true);

        Assert.IsEmpty(conversation.Artifacts);
        Assert.IsEmpty(conversation.Runs);
    }

    [TestMethod]
    public void AConversationRoundTripsThroughJson()
    {
        var conversation = new BitAiConversation("conv_1");
        conversation.SetTitle("Round trip");
        var user = conversation.AddMessage(BitAiRole.User, "Draft a README");
        user.AddAttachment(new BitAiAttachment("att_1", "notes.txt") { ContentType = "text/plain", Size = 12, Url = "https://x/notes.txt" });

        var processor = new BitAiConversationStreamProcessor(conversation);
        processor.BeginGeneration();
        processor.Apply(new BitAiAgentRunStartedEvent("r1") { AgentName = "writer", Goal = "README" });
        processor.Apply(new BitAiAgentPlanEvent("r1", [new BitAiAgentPlanStep("s1", "Outline"), new BitAiAgentPlanStep("s2", "Write")]));
        processor.Apply(new BitAiAgentStepStartedEvent("r1", "s1"));
        processor.Apply(new BitAiReasoningDeltaEvent("thinking..."));
        processor.Apply(new BitAiTextDeltaEvent("Here is a draft. "));
        processor.Apply(new BitAiToolCallStartedEvent("c1", "search") { Arguments = JsonDocument.Parse("""{"q":"readme"}""").RootElement });
        processor.Apply(new BitAiToolResultEvent("c1", BitAiToolOutcome.Success(new { hits = 3 })));
        processor.Apply(new BitAiCitationEvent(new BitAiCitation("cit1", new BitAiCitationSource("src1", "Docs") { Url = "https://docs" }) { StartIndex = 0, EndIndex = 4 }));
        processor.Apply(new BitAiArtifactCreatedEvent("a1", BitAiArtifactKind.Document, "README") { Content = "# Hello" });
        processor.Apply(new BitAiStructuredDataEvent(JsonDocument.Parse("""{"score":9}""").RootElement) { SchemaName = "score" });
        processor.Apply(new BitAiGenerativeUiEvent("card", JsonDocument.Parse("""{"city":"Oslo"}""").RootElement));
        processor.Apply(new BitAiImageEvent("https://img/1.png") { Alt = "one" });
        processor.Apply(new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 10, OutputTokens = 20 }));
        processor.Apply(new BitAiAgentStepCompletedEvent("r1", "s1"));
        processor.Apply(new BitAiAgentRunCompletedEvent("r1"));
        processor.Apply(new BitAiMessageCompletedEvent());
        processor.EndGeneration();
        conversation.Messages[1].SetFeedback(BitAiFeedbackKind.Positive, "nice");
        conversation.Messages[1].AddPart(new BitAiCustomJsonPart("acme:invoice", JsonDocument.Parse("""{"total":5}""").RootElement));

        var json = BitAiConversationSerializer.ToJson(conversation);
        var restored = BitAiConversationSerializer.FromJson(json);

        Assert.AreEqual("conv_1", restored.ConversationId);
        Assert.AreEqual("Round trip", restored.Title);
        Assert.AreEqual(2, restored.Messages.Count);
        Assert.AreEqual("notes.txt", restored.Messages[0].Attachments[0].Name);

        var assistant = restored.Messages[1];
        Assert.AreEqual(BitAiMessageStatus.Complete, assistant.Status);
        Assert.AreEqual(BitAiFeedbackKind.Positive, assistant.Feedback);
        Assert.AreEqual(30, assistant.Metadata!.Usage.TotalTokens);
        Assert.AreEqual(conversation.Messages[1].Parts.Count, assistant.Parts.Count);
        CollectionAssert.AreEqual(conversation.Messages[1].Parts.Select(p => p.Kind).ToArray(), assistant.Parts.Select(p => p.Kind).ToArray());
        Assert.AreEqual("thinking...", assistant.PartsOf<BitAiReasoningPart>().Single().Text);
        Assert.AreEqual(BitAiToolCallStatus.Completed, assistant.FindToolCall("c1")!.Status);
        Assert.AreEqual(3, assistant.FindToolCall("c1")!.Result!.Value!.Value.GetProperty("hits").GetInt32());
        Assert.AreEqual(1, assistant.Citations.Single().Number);
        Assert.AreEqual(9, assistant.PartsOf<BitAiStructuredDataPart>().Single().Value!.Value.GetProperty("score").GetInt32());
        Assert.AreEqual("Oslo", assistant.PartsOf<BitAiGenerativeUiPart>().Single().Props!.Value.GetProperty("city").GetString());
        Assert.AreEqual("acme:invoice", assistant.PartsOf<BitAiCustomJsonPart>().Single().Kind);
        Assert.AreSame(restored.Runs["r1"], assistant.Run);

        var artifact = restored.Artifacts["a1"];
        Assert.AreEqual("# Hello", artifact.Content);
        Assert.IsFalse(artifact.IsStreaming);
        Assert.ContainsSingle(artifact.Versions);

        var run = restored.Runs["r1"];
        Assert.AreEqual(BitAiAgentRunStatus.Completed, run.Status);
        Assert.AreEqual(2, run.Steps.Count);
        Assert.AreEqual(BitAiAgentStepStatus.Completed, run.Steps[0].Status);

        // Serialising the restored conversation again yields the same document.
        Assert.AreEqual(json, BitAiConversationSerializer.ToJson(restored));
    }

    [TestMethod]
    public void AnInterruptedMessageIsRestoredAsCancelled()
    {
        var conversation = new BitAiConversation();
        var processor = new BitAiConversationStreamProcessor(conversation);
        processor.BeginGeneration();
        processor.Apply(new BitAiTextDeltaEvent("half"));
        processor.Apply(new BitAiToolCallStartedEvent("c1", "slow"));

        var restored = BitAiConversationSerializer.ToConversation(BitAiConversationSerializer.ToSnapshot(conversation));

        Assert.AreEqual(BitAiMessageStatus.Cancelled, restored.Messages[0].Status);
        Assert.AreEqual(BitAiToolCallStatus.Cancelled, restored.Messages[0].FindToolCall("c1")!.Status);
    }

    [TestMethod]
    public void RolesAndKindsSerializeAsStrings()
    {
        var options = BitAiConversationSerializerOptions.CreateJsonOptions();
        Assert.AreEqual("\"assistant\"", JsonSerializer.Serialize(BitAiRole.Assistant, options));
        Assert.AreEqual("\"html\"", JsonSerializer.Serialize(BitAiArtifactKind.Html, options));
        Assert.AreEqual(BitAiRole.Tool, JsonSerializer.Deserialize<BitAiRole>("\"tool\"", options));
    }

    private static async IAsyncEnumerable<BitAiEvent> Burst([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new BitAiMessageStartedEvent();
        for (var i = 0; i < 60; i++) yield return new BitAiTextDeltaEvent("word ");
        yield return new BitAiMessageCompletedEvent();
        await Task.CompletedTask;
    }

    private sealed class HangingTransport : IBitAiTransport
    {
        public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new BitAiMessageStartedEvent();
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }

    private sealed class ScriptedTransport(IReadOnlyList<BitAiEvent> firstRound, IReadOnlyList<BitAiEvent>? secondRound = null) : IBitAiTransport
    {
        public int Calls { get; private set; }

        public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Calls++;
            var events = Calls == 1 || secondRound is null ? firstRound : secondRound;
            foreach (var evt in events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return evt;
            }

            await Task.CompletedTask;
        }
    }
}
