using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

/// <summary>
/// Covers what a chat whose server runs the tools and holds the approvals needs of the session and the conversation: the
/// presentation of a call the app has no tool for, the decisions sent back to the transport, the follow-ups and the
/// app's own values of a reply surviving a reload, and a prompt sent while a turn is in flight.
/// </summary>
[TestClass]
public class BitAiServerDrivenChatTests
{
    private static BitAiChatSessionOptions ServerTools => new() { Pacing = null, ExecuteTools = false };

    private static readonly JsonElement NoArguments = JsonDocument.Parse("{}").RootElement.Clone();

    [TestMethod]
    public async Task ADefinitionTheTransportSendsDescribesTheCallFromItsStart()
    {
        var definition = new BitAiToolDefinition("clear_files") { Title = "Clear app files", Icon = "Delete", Risk = BitAiToolRisk.Destructive };
        var transport = new BitAiFakeTransport(
            new BitAiToolCallStartedEvent("c1", "clear_files") { Arguments = NoArguments, Definition = definition },
            new BitAiToolResultEvent("c1", BitAiToolOutcome.FromText("cleared")),
            new BitAiMessageCompletedEvent());

        await using var session = new BitAiChatSession(transport, null, ServerTools);
        await session.SubmitAsync("clear");

        var call = session.Conversation.Messages[1].FindToolCall("c1")!;
        Assert.AreSame(definition, call.Definition);
        Assert.AreEqual("Clear app files", call.Definition!.DisplayName);
    }

    [TestMethod]
    public async Task ADefinitionTheTransportSendsIsNotReplacedByTheRegisteredOne()
    {
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(new BitAiToolDefinition("search") { Title = "Registered" },
                                           (_, _) => ValueTask.FromResult(BitAiToolOutcome.FromText("x"))));

        var sent = new BitAiToolDefinition("search") { Title = "From the server" };
        var transport = new BitAiFakeTransport(
            new BitAiToolCallStartedEvent("c1", "search") { Arguments = NoArguments, Definition = sent },
            new BitAiToolResultEvent("c1", BitAiToolOutcome.FromText("done")),
            new BitAiMessageCompletedEvent());

        await using var session = new BitAiChatSession(transport, null, ServerTools, registry);
        await session.SubmitAsync("find");

        Assert.AreSame(sent, session.Conversation.Messages[1].FindToolCall("c1")!.Definition);
    }

    [TestMethod]
    public async Task ADecisionOnACallTheServerHoldsIsSentToTheTransportAndTheStreamGoesOn()
    {
        var transport = new HoldingTransport();
        await using var session = new BitAiChatSession(transport, null, ServerTools);

        var decided = new List<(string Id, BitAiApprovalDecision Decision)>();
        session.ApprovalDecided += (call, decision) => decided.Add((call.ToolCallId, decision));

        var turn = session.SubmitAsync("clear");
        await WaitFor(() => session.Conversation.Messages.Count == 2
                            && session.Conversation.Messages[1].FindToolCall("c1") is { Status: BitAiToolCallStatus.AwaitingApproval });

        Assert.IsTrue(session.Approve("c1", BitAiApprovalDecision.Approved));
        await turn;

        Assert.AreEqual(1, transport.Responses.Count);
        Assert.AreEqual("c1", transport.Responses[0].ToolCallId);
        Assert.AreEqual("clear_files", transport.Responses[0].ToolName);
        Assert.AreEqual(BitAiApprovalDecision.Approved, transport.Responses[0].Decision);
        Assert.AreEqual(session.Conversation.ConversationId, transport.Responses[0].ConversationId);
        Assert.AreEqual(session.Conversation.Messages[1].MessageId, transport.Responses[0].MessageId);

        CollectionAssert.AreEqual(new[] { ("c1", BitAiApprovalDecision.Approved) }, decided);

        var call = session.Conversation.Messages[1].FindToolCall("c1")!;
        Assert.AreEqual(BitAiToolCallStatus.Completed, call.Status);
        Assert.AreEqual("Cleared.", session.Conversation.Messages[1].GetText());
    }

    [TestMethod]
    public async Task ADecisionOnACallTheSessionRunsItselfIsNeverSentToTheTransport()
    {
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(new BitAiToolDefinition("delete_file") { Risk = BitAiToolRisk.Destructive },
                                           (_, _) => ValueTask.FromResult(BitAiToolOutcome.FromText("deleted"))));

        var round = 0;
        var transport = new RecordingFakeTransport(_ => round++ == 0
            ? [new BitAiToolCallStartedEvent("c1", "delete_file") { Arguments = NoArguments }, new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls)]
            : [new BitAiTextDeltaEvent("done"), new BitAiMessageCompletedEvent()]);

        await using var session = new BitAiChatSession(transport, null, new() { Pacing = null }, registry);
        BitAiToolCallPart? awaiting = null;
        session.ApprovalRequested += call => awaiting = call;
        var decided = 0;
        session.ApprovalDecided += (_, _) => decided++;

        var turn = session.SubmitAsync("delete");
        await WaitFor(() => awaiting is not null);

        session.Approve("c1", BitAiApprovalDecision.Approved);
        await turn;

        Assert.AreEqual(0, transport.Responses.Count);
        Assert.AreEqual(1, decided);
        Assert.AreEqual(BitAiToolCallStatus.Completed, awaiting!.Status);
    }

    [TestMethod]
    public async Task ADecisionThatCannotBeSentEndsTheCallAsFailed()
    {
        var transport = new HoldingTransport { FailToReceive = true };
        await using var session = new BitAiChatSession(transport, null, ServerTools);

        var turn = session.SubmitAsync("clear");
        await WaitFor(() => session.Conversation.Messages.Count == 2
                            && session.Conversation.Messages[1].FindToolCall("c1") is { Status: BitAiToolCallStatus.AwaitingApproval });

        session.Approve("c1", BitAiApprovalDecision.Approved);

        var call = session.Conversation.Messages[1].FindToolCall("c1")!;
        await WaitFor(() => call.Status is BitAiToolCallStatus.Failed);

        Assert.AreEqual("approval_not_delivered", call.Result!.Error!.Code);
        Assert.AreEqual(BitAiStrings.Default.ApprovalNotDelivered, call.Result.Error.Message);

        session.Stop();
        await turn;
    }

    [TestMethod]
    public async Task ACallThatMustBeConfirmedEveryTimeIsNeverApprovedForGood()
    {
        var transport = new HoldingTransport { AllowApproveAlways = false };
        await using var session = new BitAiChatSession(transport, null, ServerTools);

        var turn = session.SubmitAsync("clear");
        await WaitFor(() => session.Conversation.Messages.Count == 2
                            && session.Conversation.Messages[1].FindToolCall("c1") is { Status: BitAiToolCallStatus.AwaitingApproval });

        var call = session.Conversation.Messages[1].FindToolCall("c1")!;
        Assert.IsFalse(call.AllowApproveAlways);

        session.Approve("c1", BitAiApprovalDecision.ApprovedAlways);
        await turn;

        Assert.AreEqual(BitAiApprovalDecision.Approved, call.Approval);
        Assert.AreEqual(BitAiApprovalDecision.Approved, transport.Responses[0].Decision);
    }

    [TestMethod]
    public void ExtraMetadataIsMergedKeyByKey()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);

        message.MergeMetadata(new BitAiGenerationMetadata { Extra = new Dictionary<string, object?> { ["a"] = 1, ["b"] = 2 } });
        message.MergeMetadata(new BitAiGenerationMetadata { Extra = new Dictionary<string, object?> { ["b"] = 3, ["c"] = 4 } });
        message.MergeMetadata(new BitAiGenerationMetadata { ModelId = "m" });

        var extra = message.Metadata!.Extra!;
        Assert.AreEqual(3, extra.Count);
        Assert.AreEqual(1, extra["a"]);
        Assert.AreEqual(3, extra["b"]);
        Assert.AreEqual(4, extra["c"]);
        Assert.AreEqual("m", message.Metadata.ModelId);
    }

    [TestMethod]
    public void AnExtensionReadsTheSameBeforeAndAfterAReload()
    {
        var conversation = new BitAiConversation();
        var message = conversation.AddMessage(BitAiRole.Assistant, "Here is the product.");
        message.Extensions["product"] = new ProductRef("p-1", 3);
        conversation.Extensions["count"] = 7;

        Assert.IsTrue(message.TryGetExtension<ProductRef>("product", ProductTypeInfo, out var live));
        Assert.AreEqual(new ProductRef("p-1", 3), live);

        var json = BitAiConversationSerializer.ToJson(conversation, new BitAiConversationSerializerOptions
        {
            Json = BitAiConversationSerializerOptions.CreateJsonOptionsWithReflectionFallback(),
        });
        var restored = BitAiConversationSerializer.FromJson(json);
        var restoredMessage = restored.Messages[0];

        Assert.IsInstanceOfType(restoredMessage.Extensions["product"], typeof(JsonElement));
        Assert.IsTrue(restoredMessage.TryGetExtension<ProductRef>("product", ProductTypeInfo, out var reloaded));
        Assert.AreEqual(new ProductRef("p-1", 3), reloaded);
        Assert.IsTrue(restoredMessage.TryGetExtension<ProductRef>("product", out var reflected));
        Assert.AreEqual(new ProductRef("p-1", 3), reflected);

        Assert.IsTrue(restored.TryGetExtension<int>("count", out var count));
        Assert.AreEqual(7, count);

        // The value is left the JSON it was read as.
        Assert.IsInstanceOfType(restoredMessage.Extensions["product"], typeof(JsonElement));
    }

    [TestMethod]
    public void AnExtensionOfAnotherShapeOrMissingIsNotRead()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        message.Extensions["text"] = "not a product";
        message.Extensions["json"] = JsonDocument.Parse("[1, 2]").RootElement.Clone();

        Assert.IsFalse(message.TryGetExtension<ProductRef>("missing", ProductTypeInfo, out _));
        Assert.IsFalse(message.TryGetExtension<ProductRef>("text", ProductTypeInfo, out _));
        Assert.IsFalse(message.TryGetExtension<ProductRef>("json", ProductTypeInfo, out _));
    }

    [TestMethod]
    public async Task FollowUpsArePutOnTheReplyAndSurviveAReload()
    {
        var transport = new BitAiFakeTransport(
            new BitAiTextDeltaEvent("Done."),
            new BitAiFollowUpsEvent(["Show more", " ", "Undo it"]),
            new BitAiMessageCompletedEvent());

        await using var session = new BitAiChatSession(transport, null, ServerTools);
        await session.SubmitAsync("go");

        var reply = session.Conversation.Messages[1];
        CollectionAssert.AreEqual(new[] { "Show more", "Undo it" }, reply.FollowUps!.ToArray());

        var restored = BitAiConversationSerializer.FromJson(BitAiConversationSerializer.ToJson(session.Conversation));
        CollectionAssert.AreEqual(new[] { "Show more", "Undo it" }, restored.Messages[1].FollowUps!.ToArray());

        reply.SetFollowUps([]);
        Assert.IsNull(reply.FollowUps);
        Assert.IsFalse(BitAiConversationSerializer.ToJson(session.Conversation).Contains("followUps", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task APromptSentDuringATurnIsTurnedAwayByDefault()
    {
        var transport = new InterruptibleTransport();
        await using var session = new BitAiChatSession(transport, null, new() { Pacing = null });

        var first = session.SubmitAsync("first");
        await WaitFor(() => transport.Started == 1);

        Assert.IsNull(await session.SubmitAsync("second"));
        Assert.AreEqual(2, session.Conversation.Messages.Count);

        session.Stop();
        await first;
    }

    [TestMethod]
    public async Task APromptSentDuringATurnInterruptsItWhenTheOptionsSaySo()
    {
        var transport = new InterruptibleTransport();
        await using var session = new BitAiChatSession(transport, null, new() { Pacing = null, InterruptOnSubmit = true });

        var first = session.SubmitAsync("first");
        await WaitFor(() => transport.Started == 1 && session.Conversation.Messages.Count == 2);

        var second = await session.SubmitAsync("second");
        var stopped = await first;

        Assert.IsNotNull(second);
        Assert.AreEqual(BitAiMessageStatus.Cancelled, stopped!.Status);
        Assert.AreEqual(BitAiMessageStatus.Complete, second.Status);
        Assert.AreEqual("answer to second", second.GetText());
        Assert.AreEqual(4, session.Conversation.Messages.Count);
        Assert.AreEqual("second", session.Conversation.Messages[2].GetText());
        Assert.IsFalse(session.IsGenerating);
    }

    private static JsonTypeInfo<ProductRef> ProductTypeInfo
    {
        get
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
            return (JsonTypeInfo<ProductRef>)options.GetTypeInfo(typeof(ProductRef));
        }
    }

    public sealed record ProductRef(string Id, int Quantity);

    // The deadline only bounds a failing run; a passing one returns as soon as the condition holds.
    private static async Task WaitFor(Func<bool> condition, int timeoutMs = 10_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(5);
        Assert.IsTrue(condition(), "The expected condition was not reached in time.");
    }

    /// <summary>
    /// A server that runs its tools and holds a call for approval on the open stream until the decision is sent back.
    /// </summary>
    private sealed class HoldingTransport : IBitAiTransport, IBitAiApprovalTransport
    {
        private readonly TaskCompletionSource<BitAiApprovalDecision> _decision = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<BitAiApprovalResponse> Responses { get; } = [];

        public bool FailToReceive { get; init; }

        public bool AllowApproveAlways { get; init; } = true;

        public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new BitAiToolCallStartedEvent("c1", "clear_files") { Arguments = NoArguments };
            yield return new BitAiToolApprovalRequestedEvent("c1") { AllowApproveAlways = AllowApproveAlways };

            var decision = await _decision.Task.WaitAsync(cancellationToken);

            yield return decision is BitAiApprovalDecision.Rejected
                ? new BitAiToolResultEvent("c1", BitAiToolOutcome.Failure(new BitAiError("declined")))
                : new BitAiToolResultEvent("c1", BitAiToolOutcome.FromText("cleared"));
            yield return new BitAiTextDeltaEvent("Cleared.");
            yield return new BitAiMessageCompletedEvent();
        }

        public ValueTask SendApprovalAsync(BitAiApprovalResponse response, CancellationToken cancellationToken = default)
        {
            if (FailToReceive) throw new InvalidOperationException("the hub is gone");

            Responses.Add(response);
            _decision.TrySetResult(response.Decision);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingFakeTransport(Func<BitAiRequest, IEnumerable<BitAiEvent>> script) : IBitAiTransport, IBitAiApprovalTransport
    {
        private readonly BitAiFakeTransport _inner = new(script);

        public List<BitAiApprovalResponse> Responses { get; } = [];

        public IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken = default) =>
            _inner.StreamAsync(request, cancellationToken);

        public ValueTask SendApprovalAsync(BitAiApprovalResponse response, CancellationToken cancellationToken = default)
        {
            Responses.Add(response);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Streams the first request until it is stopped, and answers every later one at once.
    /// </summary>
    private sealed class InterruptibleTransport : IBitAiTransport
    {
        private int _started;

        public int Started => Volatile.Read(ref _started);

        public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _started) == 1)
            {
                yield return new BitAiTextDeltaEvent("thinking about first ");

                while (true)
                {
                    await Task.Delay(10, cancellationToken);
                    yield return new BitAiTextDeltaEvent(".");
                }
            }

            yield return new BitAiTextDeltaEvent($"answer to {request.Messages[^1].GetText()}");
            yield return new BitAiMessageCompletedEvent();
        }
    }
}
