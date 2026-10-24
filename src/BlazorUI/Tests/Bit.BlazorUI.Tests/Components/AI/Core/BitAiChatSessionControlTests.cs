using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

[TestClass]
public class BitAiChatSessionControlTests
{
    private static BitAiChatSessionOptions Immediate => new() { Pacing = null };

    [TestMethod]
    public async Task StopCancelsTheTurnAndKeepsThePartialAnswer()
    {
        await using var session = new BitAiChatSession(new BitAiHangingTransport(), null, Immediate);

        var turn = session.SubmitAsync("go");
        // Polls the length rather than the text: materialising the text from this thread races the
        // turn appending to the same builder.
        await WaitFor(() => session.Conversation.Messages.Count == 2
            && session.Conversation.Messages[1].Parts is [BitAiTextPart { Length: > 0 }, ..]);

        session.Stop();
        await turn;

        var assistant = session.Conversation.Messages[1];
        Assert.AreEqual(BitAiMessageStatus.Cancelled, assistant.Status);
        Assert.IsNotEmpty(assistant.GetText());
        Assert.AreEqual(BitAiErrorKind.Cancelled, session.LastError!.Kind);
        Assert.IsFalse(session.IsGenerating);
    }

    [TestMethod]
    public async Task ATransportFailureBecomesARetryableErrorRatherThanAnException()
    {
        var transport = new BitAiFakeTransport { Throw = new InvalidOperationException("boom") };
        await using var session = new BitAiChatSession(transport, null, Immediate);

        await session.SubmitAsync("go");

        var assistant = session.Conversation.Messages[1];
        Assert.AreEqual(BitAiMessageStatus.Failed, assistant.Status);
        Assert.AreEqual("boom", assistant.Error!.Message);
        Assert.IsTrue(assistant.Error.IsRetryable);
    }

    [TestMethod]
    public async Task RetryReplacesTheFailedAnswerAndKeepsTheQuestion()
    {
        var attempts = 0;
        var transport = new BitAiFakeTransport(_ => attempts++ == 0
            ? [new BitAiErrorEvent(new BitAiError("transient") { IsRetryable = true })]
            : [new BitAiTextDeltaEvent("second time lucky"), new BitAiMessageCompletedEvent()]);

        await using var session = new BitAiChatSession(transport, null, Immediate);
        await session.SubmitAsync("go");
        await session.RetryAsync();

        Assert.AreEqual(2, session.Conversation.Messages.Count);
        Assert.AreEqual("go", session.Conversation.Messages[0].GetText());
        Assert.AreEqual("second time lucky", session.Conversation.Messages[1].GetText());
    }

    [TestMethod]
    public async Task RegenerateDiscardsTheAnswerAndEverythingAfterIt()
    {
        var replies = 0;
        var transport = new BitAiFakeTransport(_ =>
            [new BitAiTextDeltaEvent($"reply {++replies}"), new BitAiMessageCompletedEvent()]);

        await using var session = new BitAiChatSession(transport, null, Immediate);
        await session.SubmitAsync("one");
        await session.SubmitAsync("two");

        var first = session.Conversation.Messages[1];
        await session.RegenerateAsync(first.MessageId);

        Assert.AreEqual(2, session.Conversation.Messages.Count);
        Assert.AreEqual("one", session.Conversation.Messages[0].GetText());
        Assert.AreEqual("reply 3", session.Conversation.Messages[1].GetText());
    }

    [TestMethod]
    public async Task EditRewritesTheQuestionAndRegeneratesFromThere()
    {
        var transport = new BitAiFakeTransport(request =>
            [new BitAiTextDeltaEvent($"answering: {request.Messages[^1].GetText()}"), new BitAiMessageCompletedEvent()]);

        await using var session = new BitAiChatSession(transport, null, Immediate);
        await session.SubmitAsync("original");

        var question = session.Conversation.Messages[0];
        await session.EditAsync(question.MessageId, "corrected");

        Assert.AreEqual(2, session.Conversation.Messages.Count);
        Assert.AreEqual("corrected", session.Conversation.Messages[0].GetText());
        Assert.AreEqual("answering: corrected", session.Conversation.Messages[1].GetText());
    }

    [TestMethod]
    public async Task SubmittingWhileGeneratingIsIgnored()
    {
        await using var session = new BitAiChatSession(new BitAiHangingTransport(), null, Immediate);

        var turn = session.SubmitAsync("first");
        await WaitFor(() => session.IsGenerating);

        var second = await session.SubmitAsync("second");
        Assert.IsNull(second);

        session.Stop();
        await turn;
        Assert.AreEqual(2, session.Conversation.Messages.Count);
    }

    [TestMethod]
    public async Task TheToolLoopIsBoundedByTheConfiguredLimit()
    {
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(
            new BitAiToolDefinition("loop"),
            (_, _) => ValueTask.FromResult(BitAiToolOutcome.FromText("again"))));

        var calls = 0;
        var transport = new BitAiFakeTransport(_ =>
        [
            new BitAiToolCallStartedEvent($"c{++calls}", "loop")
            {
                Arguments = System.Text.Json.JsonDocument.Parse("{}").RootElement,
            },
            new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls),
        ]);

        await using var session = new BitAiChatSession(
            transport, null, new BitAiChatSessionOptions { Pacing = null, MaxToolRoundtrips = 3 }, registry);

        await session.SubmitAsync("go");

        Assert.AreEqual(4, transport.Requests.Count);
        Assert.Contains(p => p is BitAiErrorPart { Error.Code: "tool_loop_limit" }, session.Conversation.Messages[1].Parts);
    }

    [TestMethod]
    public async Task ATimeoutIsReportedAsATimeoutRatherThanAsTheUserStopping()
    {
        await using var session = new BitAiChatSession(new BitAiHangingTransport(), null,
            new BitAiChatSessionOptions { Pacing = null, Timeout = TimeSpan.FromMilliseconds(50) });

        await session.SubmitAsync("go");

        Assert.AreEqual(BitAiErrorKind.Timeout, session.LastError!.Kind);
        Assert.IsTrue(session.LastError.IsRetryable);
        Assert.AreEqual(BitAiMessageStatus.Failed, session.Conversation.Messages[1].Status);
    }

    [TestMethod]
    public async Task ATurnSubmittedAfterDisposalDoesNothing()
    {
        var transport = new BitAiFakeTransport(new BitAiMessageCompletedEvent());
        var session = new BitAiChatSession(transport, null, Immediate);
        await session.DisposeAsync();

        Assert.IsNull(await session.SubmitAsync("go"));
        Assert.IsEmpty(transport.Requests);
        Assert.IsEmpty(session.Conversation.Messages);
    }

    [TestMethod]
    public async Task ConcurrentSubmissionsStartExactlyOneTurn()
    {
        await using var session = new BitAiChatSession(new BitAiHangingTransport(), null, Immediate);
        using var start = new ManualResetEventSlim();

        var submissions = Enumerable.Range(0, 8)
            .Select(i => Task.Run(async () =>
            {
                start.Wait();
                return await session.SubmitAsync($"message {i}");
            }))
            .ToArray();

        start.Set();
        await WaitFor(() => session.Conversation.Messages.Count >= 2);
        await Task.Delay(50);
        session.Stop();

        var results = await Task.WhenAll(submissions);
        Assert.ContainsSingle(r => r is not null, results);
        Assert.ContainsSingle(m => m.Role.IsUser, session.Conversation.Messages);
    }

    [TestMethod]
    public async Task RegeneratingFromAUserMessageDoesNotDeleteTheQuestion()
    {
        var transport = new BitAiFakeTransport(_ => [new BitAiTextDeltaEvent("reply"), new BitAiMessageCompletedEvent()]);
        await using var session = new BitAiChatSession(transport, null, Immediate);
        await session.SubmitAsync("question");

        var result = await session.RegenerateAsync(session.Conversation.Messages[0].MessageId);

        Assert.IsNull(result);
        Assert.AreEqual(2, session.Conversation.Messages.Count);
        Assert.ContainsSingle(transport.Requests);
    }

    [TestMethod]
    public async Task AnsweringAnAgentWhileBusyLeavesTheRunWaiting()
    {
        await using var session = new BitAiChatSession(new BitAiHangingTransport(), null, Immediate);
        var run = session.Conversation.GetOrAddRun("run1");
        run.RequestInput("When should I take the downtime?");

        var turn = session.SubmitAsync("first");
        await WaitFor(() => session.Conversation.Messages.Count == 2);

        Assert.IsNull(await session.RespondToAgentAsync("run1", "Tonight"));
        Assert.AreEqual(BitAiAgentRunStatus.WaitingForInput, run.Status);

        session.Stop();
        await turn;
    }

    [TestMethod]
    public async Task ALateApprovalDoesNotReviveACancelledCall()
    {
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(
            new BitAiToolDefinition("delete_all") { Risk = BitAiToolRisk.Destructive },
            (_, _) => ValueTask.FromResult(BitAiToolOutcome.FromText("deleted"))));

        var transport = new BitAiFakeTransport(_ =>
        [
            new BitAiToolCallStartedEvent("c1", "delete_all") { Arguments = System.Text.Json.JsonDocument.Parse("{}").RootElement },
            new BitAiMessageCompletedEvent(),
        ]);

        await using var session = new BitAiChatSession(transport, null, Immediate, registry);
        BitAiToolCallPart? awaiting = null;
        session.ApprovalRequested += call => awaiting = call;

        var turn = session.SubmitAsync("clean up");
        await WaitFor(() => awaiting is not null);
        session.Stop();
        await turn;

        session.Approve("c1", BitAiApprovalDecision.Approved);

        Assert.AreEqual(BitAiToolCallStatus.Cancelled, awaiting!.Status);
        Assert.AreEqual(BitAiApprovalDecision.Undecided, awaiting.Approval);
    }

    [TestMethod]
    public async Task AToolThatCancelsItselfFailsTheCallNotTheTurn()
    {
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(
            new BitAiToolDefinition("fetch"),
            (_, _) => throw new TaskCanceledException("The HTTP request timed out.")));

        var round = 0;
        var transport = new BitAiFakeTransport(_ => round++ == 0
            ?
            [
                new BitAiToolCallStartedEvent("c1", "fetch") { Arguments = System.Text.Json.JsonDocument.Parse("{}").RootElement },
                new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls),
            ]
            : [new BitAiTextDeltaEvent("The fetch failed."), new BitAiMessageCompletedEvent()]);

        await using var session = new BitAiChatSession(transport, null, Immediate, registry);
        var assistant = await session.SubmitAsync("go");

        var call = Assert.IsExactInstanceOfType<BitAiToolCallPart>(assistant!.Parts[0]);
        Assert.AreEqual(BitAiToolCallStatus.Failed, call.Status);
        Assert.AreEqual(BitAiMessageStatus.Complete, assistant.Status);
        Assert.AreEqual(2, transport.Requests.Count);
    }

    [TestMethod]
    public async Task AToolIsExecutedOnceEvenWhenItsCompletionIsRepeated()
    {
        var executions = 0;
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(
            new BitAiToolDefinition("send_email"),
            (_, _) =>
            {
                Interlocked.Increment(ref executions);
                return ValueTask.FromResult(BitAiToolOutcome.FromText("sent"));
            }));

        var arguments = System.Text.Json.JsonDocument.Parse("{}").RootElement;
        var round = 0;
        var transport = new BitAiFakeTransport(_ => round++ == 0
            ?
            [
                new BitAiToolCallStartedEvent("c1", "send_email") { Arguments = arguments },
                new BitAiToolCallCompletedEvent("c1", arguments),
                new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls),
            ]
            : [new BitAiMessageCompletedEvent()]);

        await using var session = new BitAiChatSession(transport, null, Immediate, registry);
        await session.SubmitAsync("go");

        Assert.AreEqual(1, executions);
    }

    [TestMethod]
    public async Task AToolTheServerAlreadyAnsweredIsNotExecutedAgain()
    {
        var executions = 0;
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(
            new BitAiToolDefinition("search"),
            (_, _) =>
            {
                executions++;
                return ValueTask.FromResult(BitAiToolOutcome.FromText("client"));
            }));

        var transport = new BitAiFakeTransport(_ =>
        [
            new BitAiToolCallStartedEvent("c1", "search") { Arguments = System.Text.Json.JsonDocument.Parse("{}").RootElement },
            new BitAiToolResultEvent("c1", BitAiToolOutcome.FromText("server")),
            new BitAiTextDeltaEvent("Found it."),
            new BitAiMessageCompletedEvent(),
        ]);

        await using var session = new BitAiChatSession(transport, null, Immediate, registry);
        var assistant = await session.SubmitAsync("go");

        Assert.AreEqual(0, executions);
        Assert.ContainsSingle(transport.Requests);
        Assert.AreEqual("server", assistant!.FindToolCall("c1")!.Result!.Text);
    }

    // Parallel streams merged out of order: the result of a call the server ran arrives ahead of the call itself, which
    // starts with its arguments or streams them in afterwards. Either way the call is answered before it is ready.
    [TestMethod,
        DataRow(false),
        DataRow(true)]
    public async Task AToolTheServerAnsweredAheadOfItsCallIsNotExecutedEither(bool streamedArguments)
    {
        var executions = 0;
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(
            new BitAiToolDefinition("search"),
            (_, _) =>
            {
                executions++;
                return ValueTask.FromResult(BitAiToolOutcome.FromText("client"));
            }));

        var arguments = JsonDocument.Parse("""{"q":"x"}""").RootElement;
        BitAiEvent[] call = streamedArguments
            ?
            [
                new BitAiToolCallStartedEvent("c1", "search"),
                new BitAiToolCallDeltaEvent("c1", """{"q":"x"}"""),
                new BitAiToolCallCompletedEvent("c1"),
            ]
            : [new BitAiToolCallStartedEvent("c1", "search") { Arguments = arguments }];

        var transport = new BitAiFakeTransport(_ =>
        [
            new BitAiToolResultEvent("c1", BitAiToolOutcome.FromText("server")),
            .. call,
            new BitAiTextDeltaEvent("Found it."),
            new BitAiMessageCompletedEvent(),
        ]);

        await using var session = new BitAiChatSession(transport, null, Immediate, registry);
        var assistant = await session.SubmitAsync("go");

        Assert.AreEqual(0, executions);
        Assert.ContainsSingle(transport.Requests);
        Assert.AreEqual("server", assistant!.FindToolCall("c1")!.Result!.Text);
        Assert.AreEqual(BitAiToolCallStatus.Completed, assistant.FindToolCall("c1")!.Status);
        Assert.IsEmpty(assistant.PartsOf<BitAiErrorPart>());
        Assert.IsEmpty(assistant.PartsOf<BitAiToolResultPart>());
    }

    [TestMethod]
    public void TheHoldTimeOfAToolResultIsTenSecondsByDefaultAndNeverNegative()
    {
        Assert.AreEqual(TimeSpan.FromSeconds(10), new BitAiChatSessionOptions().ToolResultHoldTime);
        Assert.AreEqual(TimeSpan.Zero, new BitAiChatSessionOptions { ToolResultHoldTime = TimeSpan.Zero }.ToolResultHoldTime);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BitAiChatSessionOptions { ToolResultHoldTime = TimeSpan.FromMilliseconds(-1) });
    }

    [TestMethod]
    public async Task TheHoldTimeOfTheOptionsIsHandedToTheProcessorAtTheStartOfEveryTurn()
    {
        await using var session = new BitAiChatSession(
            new BitAiFakeTransport(new BitAiTextDeltaEvent("Hi."), new BitAiMessageCompletedEvent()),
            null,
            Immediate with { ToolResultHoldTime = TimeSpan.FromSeconds(3) });

        Assert.AreEqual(BitAiConversationStreamProcessor.DefaultToolResultHoldTime, session.Processor.ToolResultHoldTime);

        await session.SubmitAsync("go");
        Assert.AreEqual(TimeSpan.FromSeconds(3), session.Processor.ToolResultHoldTime);

        // Options changed between turns are read by the next one.
        session.Options = session.Options with { ToolResultHoldTime = TimeSpan.FromSeconds(1) };
        Assert.AreEqual(TimeSpan.FromSeconds(3), session.Processor.ToolResultHoldTime);

        await session.SubmitAsync("again");
        Assert.AreEqual(TimeSpan.FromSeconds(1), session.Processor.ToolResultHoldTime);
    }

    // A hold time of zero drops a result for a call that never started at once, so the session's part of it is told
    // without waiting for a clock: the error on the answer, the warning in the log, and a StateChanged for the UI - also
    // for a drop after the turn ended, which is what the timer of the processor does.
    [TestMethod]
    public async Task AResultForACallThatNeverStartsLeavesAnErrorAndRendersTheSession()
    {
        var logger = new RecordingLogger();
        var transport = new BitAiFakeTransport(
            new BitAiToolResultEvent("ghost", BitAiToolOutcome.FromText("server")),
            new BitAiTextDeltaEvent("Done."),
            new BitAiMessageCompletedEvent());

        await using var session = new BitAiChatSession(
            transport, null, Immediate with { ToolResultHoldTime = TimeSpan.Zero }, logger: logger);

        var assistant = await session.SubmitAsync("go");

        // The turn goes on: the answer is complete, with the error beside it.
        Assert.AreEqual(BitAiMessageStatus.Complete, assistant!.Status);
        Assert.IsNull(session.LastError);
        Assert.AreEqual("Done.", assistant.GetText());
        var error = Assert.ContainsSingle(assistant.PartsOf<BitAiErrorPart>()).Error;
        Assert.AreEqual(BitAiOrphanedToolResultException.ErrorCode, error.Code);
        Assert.AreEqual("ghost", Assert.IsExactInstanceOfType<BitAiOrphanedToolResultException>(error.Exception).ToolCallId);

        var warning = Assert.ContainsSingle(logger.Entries);
        Assert.AreEqual(LogLevel.Warning, warning.Level);
        Assert.AreSame(error.Exception, warning.Exception);

        var changes = 0;
        session.StateChanged += () => changes++;

        session.Processor.Apply(new BitAiToolResultEvent("late", BitAiToolOutcome.FromText("server")) { MessageId = assistant.MessageId });

        Assert.AreEqual(1, changes);
        Assert.HasCount(2, assistant.PartsOf<BitAiErrorPart>().ToList());
        Assert.HasCount(2, logger.Entries);
    }

    [TestMethod]
    public async Task ADroppedResultRendersNothingOnceTheSessionIsDisposed()
    {
        var session = new BitAiChatSession(
            new BitAiFakeTransport(new BitAiMessageCompletedEvent()),
            null,
            Immediate with { ToolResultHoldTime = TimeSpan.Zero });

        var assistant = await session.SubmitAsync("go");

        var changes = 0;
        session.StateChanged += () => changes++;
        await session.DisposeAsync();

        session.Processor.Apply(new BitAiToolResultEvent("late", BitAiToolOutcome.FromText("server")) { MessageId = assistant!.MessageId });

        Assert.ContainsSingle(assistant.PartsOf<BitAiErrorPart>());
        Assert.AreEqual(0, changes);
    }

    [TestMethod]
    public async Task AResultStillHeldWhenTheSessionIsDisposedIsDiscarded()
    {
        var clock = new BitAiManualTimeProvider();
        var session = new BitAiChatSession(
            new BitAiFakeTransport(new BitAiMessageCompletedEvent()),
            null,
            Immediate with { ToolResultHoldTime = TimeSpan.FromMilliseconds(300) },
            timeProvider: clock);

        var assistant = await session.SubmitAsync("go");
        session.Processor.Apply(new BitAiToolResultEvent("late", BitAiToolOutcome.FromText("server")) { MessageId = assistant!.MessageId });
        Assert.AreEqual(1, clock.ActiveTimers);

        await session.DisposeAsync();

        // Discarded with its timer: past the hold time, nothing drops it onto the message.
        Assert.AreEqual(0, clock.ActiveTimers);
        clock.Advance(TimeSpan.FromMilliseconds(600));

        Assert.IsEmpty(assistant.PartsOf<BitAiErrorPart>());
    }

    [TestMethod]
    public async Task UsageAcrossToolRoundTripsIsTheSumOfEachRequestsFinalReport()
    {
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(
            new BitAiToolDefinition("search"),
            (_, _) => ValueTask.FromResult(BitAiToolOutcome.FromText("found"))));

        var round = 0;
        var transport = new BitAiFakeTransport(_ => round++ == 0
            ?
            [
                new BitAiAgentRunStartedEvent("run1"),
                new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 100, OutputTokens = 3, Cost = 0.01m }),
                new BitAiToolCallStartedEvent("c1", "search") { Arguments = System.Text.Json.JsonDocument.Parse("{}").RootElement },
                new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 100, OutputTokens = 9, Cost = 0.02m }),
                new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls),
            ]
            :
            [
                new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 130, OutputTokens = 2, CachedInputTokens = 100 }),
                new BitAiTextDeltaEvent("Here it is."),
                new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 130, OutputTokens = 6, CachedInputTokens = 100, Cost = 0.005m }),
                new BitAiAgentRunCompletedEvent("run1"),
                new BitAiMessageCompletedEvent(),
            ]);

        await using var session = new BitAiChatSession(transport, null, Immediate, registry);
        var assistant = await session.SubmitAsync("go");

        var expected = new BitAiTokenUsage { InputTokens = 230, OutputTokens = 15, CachedInputTokens = 100, Cost = 0.025m };
        Assert.AreEqual(2, transport.Requests.Count);
        Assert.AreEqual(expected, assistant!.Metadata!.Usage);
        Assert.AreEqual(expected, session.Conversation.FindRun("run1")!.Usage);
        Assert.AreEqual(expected, session.Conversation.TotalUsage);
    }

    [TestMethod]
    public async Task AStopWhileTheTurnIsBeingPreparedIsNotLost()
    {
        var transport = new BitAiFakeTransport(_ => [new BitAiTextDeltaEvent("answer"), new BitAiMessageCompletedEvent()]);
        await using var session = new BitAiChatSession(transport, null, Immediate);

        // The user message is appended after the turn is claimed and before the request is sent:
        // exactly the window in which a Stop used to find no token source to cancel.
        session.Conversation.MessageAdded += (_, message) =>
        {
            if (message.Role.IsUser) session.Stop();
        };

        var assistant = await session.SubmitAsync("go");

        Assert.AreEqual(BitAiMessageStatus.Cancelled, assistant!.Status);
        Assert.AreEqual(BitAiErrorKind.Cancelled, session.LastError!.Kind);
        Assert.IsEmpty(transport.Requests);
        Assert.IsFalse(session.IsGenerating);
    }

    [TestMethod]
    public async Task AStopWithNoTurnInFlightDoesNotCancelTheNextOne()
    {
        var transport = new BitAiFakeTransport(_ => [new BitAiTextDeltaEvent("answer"), new BitAiMessageCompletedEvent()]);
        await using var session = new BitAiChatSession(transport, null, Immediate);

        session.Stop();
        var assistant = await session.SubmitAsync("go");

        Assert.AreEqual(BitAiMessageStatus.Complete, assistant!.Status);
        Assert.AreEqual("answer", assistant.GetText());
    }

    [TestMethod]
    public async Task NoEventIsRaisedAfterDisposal()
    {
        var blocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new BitAiChatSession(new BlockingTransport(blocked), null, Immediate);

        var disposed = false;
        var late = 0;
        session.StateChanged += () => { if (disposed) late++; };
        session.TurnCompleted += _ => { if (disposed) late++; };
        session.ApprovalRequested += _ => { if (disposed) late++; };

        var turn = session.SubmitAsync("go");

        // Once the transport is parked nothing is mid-way through raising an event, so anything
        // counted from here on was raised by disposal or after it. Cancelling can unwind the turn
        // synchronously inside DisposeAsync, which is why the flag is set before the call.
        await blocked.Task;
        disposed = true;
        await session.DisposeAsync();

        var assistant = await turn;
        session.Stop();
        session.Approve("unknown", BitAiApprovalDecision.Approved);

        Assert.AreEqual(0, late);
        Assert.AreEqual(BitAiMessageStatus.Cancelled, assistant!.Status);
        Assert.IsFalse(session.IsGenerating);
    }

    /// <summary>Streams one delta, then waits for cancellation, signalling once it is waiting.</summary>
    private sealed class BlockingTransport(TaskCompletionSource blocked) : IBitAiTransport
    {
        public async IAsyncEnumerable<BitAiEvent> StreamAsync(
            BitAiRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new BitAiTextDeltaEvent("partial");
            blocked.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            yield break;
        }
    }

    /// <summary>Keeps what the session logs.</summary>
    private sealed class RecordingLogger : ILogger<BitAiChatSession>
    {
        public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (Entries) Entries.Add((logLevel, exception));
        }
    }

    [TestMethod]
    public async Task ATransportFailureThrownToTheCallerStillClosesTheGeneration()
    {
        var transport = new BitAiFakeTransport { Throw = new InvalidOperationException("boom") };
        await using var session = new BitAiChatSession(transport, null, Immediate with { CatchTransportExceptions = false });

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => session.SubmitAsync("go"));

        var assistant = session.Conversation.Messages[1];
        Assert.AreEqual(BitAiMessageStatus.Failed, assistant.Status);
        Assert.AreEqual("boom", assistant.Error!.Message);
        Assert.AreEqual("boom", session.LastError!.Message);
        Assert.IsNull(session.Processor.Current);
        Assert.IsFalse(session.IsGenerating);
    }

    [TestMethod]
    public async Task AToolTheTransportFlaggedForApprovalWaitsWhateverTheLocalPolicySays()
    {
        var executed = false;
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(
            new BitAiToolDefinition("delete_file"),
            (_, _) =>
            {
                executed = true;
                return ValueTask.FromResult(BitAiToolOutcome.FromText("deleted"));
            }));

        var round = 0;
        var transport = new BitAiFakeTransport(_ => round++ == 0
            ?
            [
                new BitAiToolCallStartedEvent("c1", "delete_file") { Arguments = System.Text.Json.JsonDocument.Parse("{}").RootElement },
                new BitAiToolApprovalRequestedEvent("c1") { Reason = "deletes 40 files" },
                new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls),
            ]
            : [new BitAiMessageCompletedEvent()]);

        await using var session = new BitAiChatSession(transport, null, Immediate, registry);
        BitAiToolCallPart? awaiting = null;
        session.ApprovalRequested += call => awaiting = call;

        var turn = session.SubmitAsync("clean up");
        await WaitFor(() => awaiting is not null);

        Assert.IsFalse(executed);
        Assert.AreEqual(BitAiToolCallStatus.AwaitingApproval, awaiting!.Status);
        Assert.AreEqual("deletes 40 files", awaiting.ApprovalReason);

        session.Approve("c1", BitAiApprovalDecision.Rejected);
        await turn;

        Assert.IsFalse(executed);
        Assert.AreEqual(BitAiToolCallStatus.Rejected, awaiting.Status);
        Assert.AreEqual("rejected", awaiting.Result!.Error!.Code);
    }

    [TestMethod]
    public async Task TheErrorsTheSessionWritesReadInItsStrings()
    {
        var transport = new BitAiFakeTransport { Throw = new OperationCanceledException() };
        await using var session = new BitAiChatSession(transport, null, Immediate with { Strings = new OwnStrings() });

        await session.SubmitAsync("go");

        Assert.AreEqual("Angehalten.", session.LastError!.Message);
        Assert.AreEqual(BitAiErrorKind.Cancelled, session.LastError.Kind);
    }

    private sealed class OwnStrings : BitAiStrings
    {
        public override string GenerationStopped => "Angehalten.";
    }

    // The deadline only bounds a failing run; a passing one returns as soon as the condition holds.
    private static async Task WaitFor(Func<bool> condition, int timeoutMs = 10_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(5);
        Assert.IsTrue(condition(), "The expected condition was not reached in time.");
    }
}
