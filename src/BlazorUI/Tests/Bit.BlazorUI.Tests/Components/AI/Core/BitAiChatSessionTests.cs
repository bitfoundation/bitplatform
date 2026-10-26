using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

[TestClass]
public class BitAiChatSessionTests
{
    private static BitAiChatSession Create(IBitAiTransport transport, IBitAiToolRegistry? tools = null,
        BitAiChatSessionOptions? options = null, IBitAiToolApprovalPolicy? policy = null) =>
        new(transport, null, options ?? new BitAiChatSessionOptions { Pacing = null }, tools, policy);

    [TestMethod]
    public async Task SubmitAppendsAUserMessageAndStreamsAReply()
    {
        var transport = new BitAiFakeTransport(
            new BitAiMessageStartedEvent(),
            new BitAiTextDeltaEvent("Hi "),
            new BitAiTextDeltaEvent("there"),
            new BitAiMessageCompletedEvent());

        await using var session = Create(transport);
        await session.SubmitAsync("hello");

        Assert.AreEqual(2, session.Conversation.Messages.Count);
        Assert.AreEqual("hello", session.Conversation.Messages[0].GetText());
        Assert.AreEqual("Hi there", session.Conversation.Messages[1].GetText());
        Assert.IsFalse(session.IsGenerating);
    }

    [TestMethod]
    public async Task TheFirstUserMessageBecomesTheConversationTitle()
    {
        await using var session = Create(new BitAiFakeTransport(new BitAiMessageCompletedEvent()));
        await session.SubmitAsync("Explain quantum tunnelling in plain English");

        Assert.AreEqual("Explain quantum tunnelling in plain English", session.Conversation.Title);
    }

    [TestMethod]
    public async Task TheRequestCarriesTheConversationButNotTheEmptyAssistantMessage()
    {
        var transport = new BitAiFakeTransport(new BitAiMessageCompletedEvent());
        await using var session = Create(transport);

        await session.SubmitAsync("first");
        await session.SubmitAsync("second");

        Assert.AreEqual(2, transport.Requests.Count);
        Assert.ContainsSingle(transport.Requests[0].Messages);
        Assert.AreEqual(3, transport.Requests[1].Messages.Count);
    }

    [TestMethod]
    public async Task ToolsAreExecutedAndTheirResultsFeedTheNextRound()
    {
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(
            new BitAiToolDefinition("add") { Description = "Adds two numbers." },
            (invocation, _) =>
            {
                var args = invocation.Arguments;
                var sum = args.GetProperty("a").GetInt32() + args.GetProperty("b").GetInt32();
                return ValueTask.FromResult(BitAiToolOutcome.Success(new { sum }));
            }));

        var round = 0;
        var transport = new BitAiFakeTransport(_ => round++ == 0
            ?
            [
                new BitAiToolCallStartedEvent("c1", "add")
                {
                    Arguments = JsonDocument.Parse("""{"a":2,"b":3}""").RootElement,
                },
                new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls),
            ]
            :
            [
                new BitAiTextDeltaEvent("The sum is 5."),
                new BitAiMessageCompletedEvent(),
            ]);

        await using var session = Create(transport, registry);
        await session.SubmitAsync("what is 2 plus 3");

        var assistant = session.Conversation.Messages[^1];
        var call = Assert.IsExactInstanceOfType<BitAiToolCallPart>(assistant.Parts[0]);
        Assert.AreEqual(BitAiToolCallStatus.Completed, call.Status);
        Assert.AreEqual(5, call.Result!.Value!.Value.GetProperty("sum").GetInt32());
        Assert.AreEqual("The sum is 5.", assistant.GetText());
        Assert.AreEqual(2, transport.Requests.Count);
    }

    // A reply is not finished while the tools it asked for run: it stays in generation from the first request to the
    // last, so a transcript never offers the actions of a finished reply in between.
    [TestMethod]
    public async Task AReplyIsNeverFinishedBeforeItsLastRound()
    {
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(
            new BitAiToolDefinition("search"),
            (_, _) => ValueTask.FromResult(BitAiToolOutcome.FromText("found"))));

        var round = 0;
        var transport = new BitAiFakeTransport(_ => round++ == 0
            ?
            [
                new BitAiTextDeltaEvent("Let me check."),
                new BitAiToolCallStartedEvent("c1", "search") { Arguments = JsonDocument.Parse("{}").RootElement },
                new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls),
            ]
            :
            [
                new BitAiTextDeltaEvent("Found it."),
                new BitAiMessageCompletedEvent(),
            ]);

        await using var session = Create(transport, registry);

        // Read on every change the session reports before the request of the last round has gone out: the first round,
        // the completion that stops it for the tool call, and the tool running.
        var finishedEarly = false;
        session.StateChanged += () =>
        {
            if (transport.Requests.Count < 2 && session.Conversation.Messages is [_, { IsBusy: false }]) finishedEarly = true;
        };

        var assistant = await session.SubmitAsync("look it up");

        Assert.IsFalse(finishedEarly);
        Assert.AreEqual(BitAiMessageStatus.Complete, assistant!.Status);
        Assert.AreEqual(2, transport.Requests.Count);
    }

    // Whatever the transport said as it stopped for the call, the reply waits with it: it is still being generated.
    [TestMethod]
    public async Task AReplyWaitingForADecisionOnItsToolIsNotFinished()
    {
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(
            new BitAiToolDefinition("delete_all") { Risk = BitAiToolRisk.Destructive },
            (_, _) => ValueTask.FromResult(BitAiToolOutcome.FromText("deleted"))));

        var round = 0;
        var transport = new BitAiFakeTransport(_ => round++ == 0
            ?
            [
                new BitAiToolCallStartedEvent("c1", "delete_all") { Arguments = JsonDocument.Parse("{}").RootElement },
                new BitAiMessageCompletedEvent(),
            ]
            : [new BitAiTextDeltaEvent("Done."), new BitAiMessageCompletedEvent()]);

        await using var session = Create(transport, registry);

        BitAiToolCallPart? awaiting = null;
        session.ApprovalRequested += call => awaiting = call;

        var turn = session.SubmitAsync("clean up");
        await WaitFor(() => awaiting is not null);

        var assistant = session.Conversation.Messages[^1];
        Assert.AreEqual(BitAiMessageStatus.Streaming, assistant.Status);

        session.Approve("c1", BitAiApprovalDecision.Approved);
        await turn;

        Assert.AreEqual(BitAiMessageStatus.Complete, assistant.Status);
        Assert.AreEqual("Done.", assistant.GetText());
    }

    // A card names its call by the tool's definition from its first render, so the session describes a call by its own
    // registry as soon as the call starts rather than once it runs.
    [TestMethod]
    public async Task ACallIsDescribedByItsToolAsSoonAsItStarts()
    {
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(
            new BitAiToolDefinition("search_docs") { Title = "Search the docs" },
            (_, _) => ValueTask.FromResult(BitAiToolOutcome.FromText("found"))));

        var round = 0;
        var transport = new BitAiFakeTransport(_ => round++ == 0
            ?
            [
                new BitAiToolCallStartedEvent("c1", "search_docs"),
                new BitAiToolCallDeltaEvent("c1", "{}"),
                new BitAiToolCallCompletedEvent("c1"),
                new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls),
            ]
            : [new BitAiMessageCompletedEvent()]);

        await using var session = Create(transport, registry);

        // Read while the arguments of the call are still streaming in, long before it runs.
        string? described = null;
        session.StateChanged += () =>
        {
            if (described is null
                && session.Conversation.Messages is [_, var reply]
                && reply.FindToolCall("c1") is { IsStreaming: true } call)
            {
                described = call.Definition?.DisplayName;
            }
        };

        await session.SubmitAsync("search");

        Assert.AreEqual("Search the docs", described);
    }

    // What a tool run here cites is listed under the reply, as a result the transport reports is.
    [TestMethod]
    public async Task TheSourcesAToolCitesAreTheReplys()
    {
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(
            new BitAiToolDefinition("search_docs"),
            (_, _) => ValueTask.FromResult(BitAiToolOutcome.FromText("found") with
            {
                Citations = [new BitAiCitation("cite1", new BitAiCitationSource("s1", "Reference Guide"))],
            })));

        var round = 0;
        var transport = new BitAiFakeTransport(_ => round++ == 0
            ?
            [
                new BitAiToolCallStartedEvent("c1", "search_docs") { Arguments = JsonDocument.Parse("{}").RootElement },
                new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls),
            ]
            : [new BitAiTextDeltaEvent("See the guide."), new BitAiMessageCompletedEvent()]);

        await using var session = Create(transport, registry);
        var assistant = await session.SubmitAsync("where is it documented");

        var citation = Assert.ContainsSingle(assistant!.Citations);
        Assert.AreEqual("Reference Guide", citation.Source.Title);
        Assert.AreEqual(1, citation.Number);
    }

    [TestMethod]
    public async Task AnUnknownToolFailsTheCallWithoutFailingTheTurn()
    {
        var transport = new BitAiFakeTransport(_ =>
        [
            new BitAiToolCallStartedEvent("c1", "does_not_exist") { Arguments = JsonDocument.Parse("{}").RootElement },
            new BitAiMessageCompletedEvent(),
        ]);

        await using var session = Create(transport, new BitAiToolRegistry(),
            new BitAiChatSessionOptions { Pacing = null, MaxToolRoundtrips = 1 });

        await session.SubmitAsync("go");

        var call = Assert.IsExactInstanceOfType<BitAiToolCallPart>(session.Conversation.Messages[^1].Parts[0]);
        Assert.AreEqual(BitAiToolCallStatus.Failed, call.Status);
        Assert.AreEqual("unknown_tool", call.Result!.Error!.Code);
    }

    [TestMethod]
    public async Task ADestructiveToolWaitsForApprovalAndRunsOnceApproved()
    {
        var executed = false;
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(
            new BitAiToolDefinition("delete_all") { Risk = BitAiToolRisk.Destructive },
            (_, _) =>
            {
                executed = true;
                return ValueTask.FromResult(BitAiToolOutcome.FromText("deleted"));
            }));

        var transport = new BitAiFakeTransport(_ =>
        [
            new BitAiToolCallStartedEvent("c1", "delete_all") { Arguments = JsonDocument.Parse("{}").RootElement },
            new BitAiMessageCompletedEvent(),
        ]);

        await using var session = Create(transport, registry,
            new BitAiChatSessionOptions { Pacing = null, MaxToolRoundtrips = 1 });

        BitAiToolCallPart? awaiting = null;
        session.ApprovalRequested += call => awaiting = call;

        var turn = session.SubmitAsync("clean up");

        await WaitFor(() => awaiting is not null);
        Assert.IsFalse(executed);
        Assert.AreEqual(BitAiToolCallStatus.AwaitingApproval, awaiting!.Status);

        Assert.IsTrue(session.Approve("c1", BitAiApprovalDecision.Approved));
        await turn;

        Assert.IsTrue(executed);
        Assert.AreEqual(BitAiToolCallStatus.Completed, awaiting.Status);
    }

    [TestMethod]
    public async Task ARejectedToolDoesNotRun()
    {
        var executed = false;
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(
            new BitAiToolDefinition("delete_all") { Risk = BitAiToolRisk.Destructive },
            (_, _) =>
            {
                executed = true;
                return ValueTask.FromResult(BitAiToolOutcome.FromText("deleted"));
            }));

        var transport = new BitAiFakeTransport(_ =>
        [
            new BitAiToolCallStartedEvent("c1", "delete_all") { Arguments = JsonDocument.Parse("{}").RootElement },
            new BitAiMessageCompletedEvent(),
        ]);

        await using var session = Create(transport, registry,
            new BitAiChatSessionOptions { Pacing = null, MaxToolRoundtrips = 1 });

        BitAiToolCallPart? awaiting = null;
        session.ApprovalRequested += call => awaiting = call;

        var turn = session.SubmitAsync("clean up");
        await WaitFor(() => awaiting is not null);
        session.Approve("c1", BitAiApprovalDecision.Rejected);
        await turn;

        Assert.IsFalse(executed);
        Assert.AreEqual(BitAiToolCallStatus.Rejected, awaiting!.Status);
    }

    // The deadline only bounds a failing run; a passing one returns as soon as the condition holds.
    private static async Task WaitFor(Func<bool> condition, int timeoutMs = 10_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(5);
        Assert.IsTrue(condition(), "The expected condition was not reached in time.");
    }
}
