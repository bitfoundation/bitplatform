using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bit.BlazorUI;

/// <summary>
/// Drives one conversation: submission, streaming, tool execution, approval, cancellation, retry,
/// and regeneration.
/// </summary>
/// <remarks>
/// <para>
/// This is the type most applications hold. Components bind to its <see cref="Conversation"/> and
/// call <see cref="SubmitAsync"/>; everything between a user pressing enter and the answer
/// appearing is handled here.
/// </para>
/// <para>
/// The session owns the tool loop. When the model asks for a tool the session resolves it from the
/// registry, checks the approval policy, waits for a human decision when one is required, executes
/// it, appends the result, and asks the model again, up to
/// <see cref="BitAiChatSessionOptions.MaxToolRoundtrips"/>. A transport therefore never has to implement
/// agentic behaviour to get it.
/// </para>
/// <para>
/// One session drives one conversation at a time. Submitting while a turn is in flight is a no-op
/// rather than an error, because double-submit is a UI race, not a programming mistake - unless
/// <see cref="BitAiChatSessionOptions.InterruptOnSubmit"/> asks for the turn to be stopped and the new prompt
/// answered instead.
/// </para>
/// </remarks>
public sealed class BitAiChatSession : IAsyncDisposable
{
    private readonly IBitAiTransport _transport;
    private readonly IBitAiToolRegistry? _tools;
    private readonly IBitAiToolApprovalPolicy _approvalPolicy;
    private readonly IServiceProvider? _services;
    private readonly ILogger _logger;
    private readonly Dictionary<string, TaskCompletionSource<BitAiApprovalDecision>> _approvals = new(StringComparer.Ordinal);
    // Cancelled as the session is disposed: what the session started that outlives a turn (a decision on its way to the
    // transport) stops with it.
    private readonly CancellationTokenSource _lifetime = new();

    // Set for exactly as long as a turn is in flight: publishing it is how a turn claims the session,
    // so there is never a moment when a turn is running and Stop has nothing to cancel.
    private Turn? _turn;
    // The turn that claimed the session last, which may still be unwinding (raising TurnCompleted) after giving its claim
    // up: a submission taking the session over waits for it (InterruptAsync).
    private Turn? _lastTurn;
    private volatile bool _disposed;

    /// <summary>Creates a session.</summary>
    /// <param name="transport">Produces responses.</param>
    /// <param name="conversation">The conversation to drive, or <see langword="null"/> for a new one.</param>
    /// <param name="options">Session settings.</param>
    /// <param name="tools">The tools the model may call.</param>
    /// <param name="approvalPolicy">Decides which calls need a human decision.</param>
    /// <param name="services">Scoped services made available to tools.</param>
    /// <param name="logger">Where to log transport and tool failures.</param>
    /// <param name="timeProvider">
    /// The clock the processor holds tool results by (<see cref="BitAiChatSessionOptions.ToolResultHoldTime"/>), or
    /// <see langword="null"/> for the system clock. A test passes a fake one to move time on.
    /// </param>
    public BitAiChatSession(
        IBitAiTransport transport,
        BitAiConversation? conversation = null,
        BitAiChatSessionOptions? options = null,
        IBitAiToolRegistry? tools = null,
        IBitAiToolApprovalPolicy? approvalPolicy = null,
        IServiceProvider? services = null,
        ILogger<BitAiChatSession>? logger = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(transport);
        _transport = transport;
        _tools = tools;
        _approvalPolicy = approvalPolicy ?? new BitAiDefaultToolApprovalPolicy();
        _services = services;
        _logger = logger ?? NullLogger<BitAiChatSession>.Instance;

        Conversation = conversation ?? new BitAiConversation();
        Options = options ?? new BitAiChatSessionOptions();
        Processor = new BitAiConversationStreamProcessor(Conversation, timeProvider);
        Processor.ToolResultDropped += OnToolResultDropped;
    }

    /// <summary>The conversation this session drives.</summary>
    public BitAiConversation Conversation { get; }

    /// <summary>The settings in force.</summary>
    public BitAiChatSessionOptions Options { get; set; }

    /// <summary>The reducer applying events to the conversation.</summary>
    public BitAiConversationStreamProcessor Processor { get; }

    /// <summary>The text of the errors the session writes itself (<see cref="BitAiChatSessionOptions.Strings"/>).</summary>
    public BitAiStrings Strings => Options.Strings ?? BitAiStrings.Default;

    /// <summary><see langword="true"/> while a turn is in flight.</summary>
    public bool IsGenerating => Volatile.Read(ref _turn) is not null;

    /// <summary>The failure from the last turn, cleared when a new turn starts.</summary>
    public BitAiError? LastError { get; private set; }

    /// <summary>Raised whenever anything the UI cares about changes.</summary>
    /// <remarks>
    /// Coarse by design. Components should route it through a <see cref="BitAiRenderScheduler"/> so a
    /// fast stream cannot render more often than the frame rate. Never raised once the session has
    /// been disposed, like the other session events.
    /// </remarks>
    public event Action? StateChanged;

    /// <summary>Raised when a turn ends, successfully or not.</summary>
    public event Action<BitAiMessage>? TurnCompleted;

    /// <summary>Raised when a tool call needs a human decision.</summary>
    public event Action<BitAiToolCallPart>? ApprovalRequested;

    /// <summary>
    /// Raised after <see cref="Approve"/> has recorded a decision, with the call and the decision as recorded.
    /// </summary>
    /// <remarks>
    /// For every decision, whoever runs the call - the session itself or the transport's side. A transport implementing
    /// <see cref="IBitAiApprovalTransport"/> is sent the decisions on the calls the session does not run itself without
    /// handling this; it is here for an app that delivers them some other way, or records them.
    /// </remarks>
    public event Action<BitAiToolCallPart, BitAiApprovalDecision>? ApprovalDecided;

    /// <summary>
    /// Appends a user message and generates a reply.
    /// </summary>
    /// <remarks>
    /// With <see cref="BitAiChatSessionOptions.InterruptOnSubmit"/> on, a turn in flight is stopped and waited for
    /// first, and the prompt is answered in its place.
    /// </remarks>
    /// <param name="text">The user text. May be empty when attachments carry the request.</param>
    /// <param name="attachments">Files to attach.</param>
    /// <param name="cancellationToken">
    /// Cancels the turn - and, with <see cref="BitAiChatSessionOptions.InterruptOnSubmit"/> on, the wait for the turn in
    /// flight to stop. A token already cancelled leaves the turn in flight alone.
    /// </param>
    /// <returns>
    /// The assistant message, or <see langword="null"/> when a turn was already running (and was not interrupted) and
    /// the prompt was not taken.
    /// </returns>
    public async Task<BitAiMessage?> SubmitAsync(
        string text,
        IReadOnlyList<BitAiAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        if (Options.InterruptOnSubmit) await InterruptAsync(cancellationToken).ConfigureAwait(false);

        return await SubmitTurnAsync(text, attachments, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Appends a user message and generates a reply, only while no turn is in flight - whatever
    /// <see cref="BitAiChatSessionOptions.InterruptOnSubmit"/> says.
    /// </summary>
    /// <remarks>
    /// For a prompt sent by a one-shot button - a follow-up, a suggestion - rather than typed: a second click on it before
    /// the render takes it away would otherwise stop the turn the first click started and send the same prompt again.
    /// </remarks>
    internal Task<BitAiMessage?> SubmitIfIdleAsync(string text) => SubmitTurnAsync(text, null, default);

    private Task<BitAiMessage?> SubmitTurnAsync(
        string text,
        IReadOnlyList<BitAiAttachment>? attachments,
        CancellationToken cancellationToken) =>
        StartTurnAsync(() =>
        {
            AppendUserMessage(text, attachments);
            return true;
        }, cancellationToken);

    // Stops the turn in flight and waits for it to have unwound - its claim on the session released and its end reported
    // (TurnCompleted) - so the next turn can claim it, and appends to the conversation only once the handlers of that end
    // are done reading it. A turn that a third caller starts meanwhile is stopped in a further round; one that still wins
    // the race keeps the session, and the prompt is then not taken. So is it when the submission is cancelled first.
    private async Task InterruptAsync(CancellationToken cancellationToken)
    {
        for (var round = 0; round < 3 && cancellationToken.IsCancellationRequested is false; round++)
        {
            // The turn holding the session, else the last one, which may still be unwinding after giving its claim up.
            var turn = Volatile.Read(ref _turn) ?? Volatile.Read(ref _lastTurn);
            if (turn is null || turn.Ended.Task.IsCompleted) return;

            turn.Cancel();
            RaiseStateChanged();

            try
            {
                await turn.Ended.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Regenerates an assistant message, discarding it and everything after it.
    /// </summary>
    /// <param name="messageId">
    /// The assistant message to regenerate, or <see langword="null"/> for the most recent one.
    /// </param>
    /// <param name="cancellationToken">Cancels the turn.</param>
    /// <returns>The new assistant message, or <see langword="null"/> when there was nothing to regenerate.</returns>
    public Task<BitAiMessage?> RegenerateAsync(string? messageId = null, CancellationToken cancellationToken = default) =>
        StartTurnAsync(() =>
        {
            var target = messageId is not null
                ? Conversation.FindMessage(messageId)
                : Conversation.Messages.LastOrDefault(m => m.Role.IsAssistant);

            // Truncating at a user message would delete the question rather than regenerate an answer.
            if (target is null || !target.Role.IsAssistant) return false;

            Conversation.TruncateAfter(target.MessageId, inclusive: true);
            return true;
        }, cancellationToken);

    /// <summary>
    /// Retries the last turn after a failure, keeping the user message in place.
    /// </summary>
    /// <param name="cancellationToken">Cancels the turn.</param>
    /// <returns>The new assistant message, or <see langword="null"/> when there is nothing to retry.</returns>
    public Task<BitAiMessage?> RetryAsync(CancellationToken cancellationToken = default) =>
        StartTurnAsync(() =>
        {
            if (Conversation.LastMessage is { } last && last.Role.IsAssistant)
            {
                Conversation.RemoveMessage(last.MessageId);
            }

            return Conversation.LastMessage is not null;
        }, cancellationToken);

    /// <summary>
    /// Replaces the text of a user message and regenerates everything after it.
    /// </summary>
    /// <param name="messageId">The user message to edit.</param>
    /// <param name="text">The replacement text.</param>
    /// <param name="cancellationToken">Cancels the turn.</param>
    /// <returns>The new assistant message, or <see langword="null"/> when the message was not found.</returns>
    public Task<BitAiMessage?> EditAsync(string messageId, string text, CancellationToken cancellationToken = default) =>
        StartTurnAsync(() =>
        {
            var message = Conversation.FindMessage(messageId);
            if (message is null || !message.Role.IsUser) return false;

            Conversation.TruncateAfter(messageId);
            if (message.PartsOf<BitAiTextPart>().FirstOrDefault() is { } part)
            {
                part.SetText(text);
            }
            else
            {
                message.AddPart(new BitAiTextPart(text));
            }

            return true;
        }, cancellationToken);

    /// <summary>Stops the current turn. The partial answer is kept and marked cancelled.</summary>
    /// <remarks>
    /// Safe to call from any thread. A turn that has started but not yet sent its request is stopped
    /// too; with no turn in flight this does nothing, and never affects a later turn.
    /// </remarks>
    public void Stop()
    {
        Volatile.Read(ref _turn)?.Cancel();

        RaiseStateChanged();
    }

    /// <summary>
    /// Records a human decision on a tool call that was waiting for one.
    /// </summary>
    /// <remarks>
    /// A call that does not allow it to be approved for good (<see cref="BitAiToolCallPart.AllowApproveAlways"/>) records
    /// <see cref="BitAiApprovalDecision.ApprovedAlways"/> as <see cref="BitAiApprovalDecision.Approved"/>. A decision on a
    /// call the transport's side holds - one the session does not run itself, since it executes no tools
    /// (<see cref="BitAiChatSessionOptions.ExecuteTools"/>) or has no tool of that name - is sent on to a transport that
    /// implements <see cref="IBitAiApprovalTransport"/>. <see cref="ApprovalDecided"/> is raised for every decision
    /// recorded.
    /// </remarks>
    /// <param name="toolCallId">The call being decided.</param>
    /// <param name="decision">The decision.</param>
    /// <returns>
    /// <see langword="true"/> when the decision was recorded; <see langword="false"/> when no call of that id was waiting
    /// for one - it has run, been decided or been cancelled, or is not known - and nothing changed.
    /// </returns>
    public bool Approve(string toolCallId, BitAiApprovalDecision decision)
    {
        ArgumentNullException.ThrowIfNull(toolCallId);

        TaskCompletionSource<BitAiApprovalDecision>? waiter;
        lock (_approvals)
        {
            _approvals.Remove(toolCallId, out waiter);
        }

        var call = FindToolCall(toolCallId);

        // Without a waiter only a call a server-side policy is holding can be decided. Anything else
        // has already run, been rejected, or been cancelled, and must not move back to pending.
        if (waiter is null && call is not { Status: BitAiToolCallStatus.AwaitingApproval }) return false;

        // Recorded on the call first, which records a decision the call does not allow (AllowApproveAlways) as the one it
        // does: what the session acts on, sends on and reports is the decision as recorded.
        if (call is not null)
        {
            call.SetApproval(decision);
            decision = call.Approval;
        }

        if (decision is BitAiApprovalDecision.ApprovedAlways
            && _approvalPolicy is BitAiDefaultToolApprovalPolicy policy
            && call is not null)
        {
            policy.RememberAlwaysApproved(call.ToolName);
        }

        waiter?.TrySetResult(decision);
        RaiseStateChanged();

        if (call is not null)
        {
            // A call nobody here waits for, and that the session will not run itself, is the transport's side's to run:
            // the decision is what it is waiting for. One the session runs itself is never sent on, or both would run it.
            if (waiter is null && RunsItself(call) is false && _transport is IBitAiApprovalTransport approvals)
            {
                _ = SendApprovalAsync(approvals, call, decision);
            }

            if (!_disposed) ApprovalDecided?.Invoke(call, decision);
        }

        return true;
    }

    /// <summary>
    /// Answers a question an agent run is blocked on, then continues the turn.
    /// </summary>
    /// <param name="runId">The blocked run.</param>
    /// <param name="answer">The user answer, appended as a user message.</param>
    /// <param name="cancellationToken">Cancels the turn.</param>
    /// <returns>The assistant message, or <see langword="null"/> when the run was not waiting.</returns>
    public Task<BitAiMessage?> RespondToAgentAsync(
        string runId,
        string answer,
        CancellationToken cancellationToken = default) =>
        StartTurnAsync(() =>
        {
            // Checked inside the turn so a busy session leaves the run waiting rather than resolving
            // it and then dropping the answer.
            var run = Conversation.FindRun(runId);
            if (run is null || run.Status is not BitAiAgentRunStatus.WaitingForInput) return false;

            run.ResolveInput();
            AppendUserMessage(answer, null);
            return true;
        }, cancellationToken);

    /// <summary>
    /// Claims the session for one turn, runs <paramref name="prepare"/> to set the conversation up,
    /// and generates the reply.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The claim is a compare-and-swap rather than a check of <see cref="IsGenerating"/>, so two
    /// callers racing on different threads cannot both start a turn or both edit the thread.
    /// </para>
    /// <para>
    /// What is swapped in is the turn's own token source, created beforehand, so claiming the turn
    /// and making it cancellable are one step. A <see cref="Stop"/> from another thread while the
    /// conversation is being prepared therefore cancels the turn instead of finding nothing to cancel.
    /// </para>
    /// </remarks>
    private async Task<BitAiMessage?> StartTurnAsync(Func<bool> prepare, CancellationToken cancellationToken)
    {
        if (_disposed) return null;

        // The timeout has its own source so that running out of time can be told apart from Stop.
        var timeout = Options.Timeout is { } limit ? new CancellationTokenSource(limit) : null;
        var cts = timeout is null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var turn = new Turn(cts, timeout);

        // Disposal sets its flag before cancelling the published turn, and this reads the flag after
        // publishing, so one of the two always sees the other.
        if (Interlocked.CompareExchange(ref _turn, turn, null) is not null || _disposed)
        {
            Release(turn);
            End(turn);
            return null;
        }

        Volatile.Write(ref _lastTurn, turn);

        bool proceed;
        try
        {
            proceed = prepare();
        }
        catch
        {
            Release(turn);
            End(turn);
            throw;
        }

        if (!proceed)
        {
            Release(turn);
            End(turn);
            return null;
        }

        return await RunTurnAsync(turn, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Ends a turn's claim on the session and disposes its token sources.</summary>
    /// <remarks>The turn owns its token sources. Stop and DisposeAsync only ever cancel them.</remarks>
    private void Release(Turn turn)
    {
        // Only the turn holding the claim gives it up: one that never got it has nothing to release.
        Interlocked.CompareExchange(ref _turn, null, turn);

        turn.Cts.Dispose();
        turn.Timeout?.Dispose();
    }

    // Reports the turn over to a submission waiting to take the session over (InterruptAsync) - once it has released its
    // claim and reported its end, so the submission's prompt is never appended while the handlers of that end still read
    // the conversation.
    private void End(Turn turn)
    {
        Interlocked.CompareExchange(ref _lastTurn, null, turn);

        turn.Ended.TrySetResult();
    }

    private void AppendUserMessage(string text, IReadOnlyList<BitAiAttachment>? attachments)
    {
        var message = new BitAiMessage(BitAiRole.User);
        if (!string.IsNullOrEmpty(text)) message.AddPart(new BitAiTextPart(text));
        if (attachments is not null)
        {
            foreach (var attachment in attachments) message.AddAttachment(attachment);
        }

        Conversation.AddMessage(message);

        if (Options.AutoTitle && Conversation.Title is null && !string.IsNullOrWhiteSpace(text))
        {
            Conversation.SetTitle(Summarize(text));
        }
    }

    private async Task<BitAiMessage?> RunTurnAsync(Turn turn, CancellationToken cancellationToken)
    {
        LastError = null;
        var token = turn.Cts.Token;
        var timeout = turn.Timeout;

        BitAiMessage? assistant = null;
        try
        {
            Processor.ToolResultHoldTime = Options.ToolResultHoldTime;
            assistant = Processor.BeginGeneration();
            RaiseStateChanged();

            for (var round = 0; round <= Options.MaxToolRoundtrips; round++)
            {
                // Stop may have arrived before the first request, or while tools ran.
                token.ThrowIfCancellationRequested();

                var ready = new List<BitAiToolCallPart>();
                void OnReady(BitAiToolCallPart part) => ready.Add(part);

                Processor.ToolCallReady += OnReady;
                try
                {
                    await ConsumeAsync(BuildRequest(assistant), token).ConfigureAwait(false);
                }
                finally
                {
                    Processor.ToolCallReady -= OnReady;
                }

                // A call the transport already answered ran on the server; running it again here
                // would repeat its side effects.
                ready.RemoveAll(call => call.Result is not null);

                if (!Options.ExecuteTools || ready.Count == 0) break;
                if (round == Options.MaxToolRoundtrips)
                {
                    assistant.AddPart(new BitAiErrorPart(new BitAiError(Strings.ToolLoopLimitReached(Options.MaxToolRoundtrips))
                    {
                        Code = "tool_loop_limit",
                        Kind = BitAiErrorKind.Tool,
                    }));
                    break;
                }

                // The reply goes on once its tools have run, so it is still being generated while they run or wait for
                // the user's decision - whatever the transport said as it stopped for them (the processor leaves a reply
                // that stopped for its tool calls as it was): it stays busy from the first request to the last, and a
                // reader is never offered the actions of a finished reply in between.
                assistant.SetStatus(BitAiMessageStatus.Streaming);

                await ExecuteToolsAsync(ready, token).ConfigureAwait(false);
            }

            Processor.EndGeneration();
        }
        catch (OperationCanceledException)
        {
            var timedOut = timeout is { IsCancellationRequested: true } && !cancellationToken.IsCancellationRequested;
            LastError = timedOut
                ? new BitAiError(Strings.RequestTimedOut) { Kind = BitAiErrorKind.Timeout, Code = "timeout", IsRetryable = true }
                : new BitAiError(Strings.GenerationStopped) { Kind = BitAiErrorKind.Cancelled, Code = "cancelled" };

            assistant?.CompleteOpenParts();
            CancelHeldApprovals(assistant);
            if (timedOut) assistant?.SetError(LastError);
            else assistant?.SetStatus(BitAiMessageStatus.Cancelled);
            Processor.EndGeneration(timedOut ? BitAiMessageStatus.Failed : BitAiMessageStatus.Cancelled);
        }
        catch (Exception exception)
        {
            // The generation is closed either way - the message failed, its open parts completed, the processor's
            // current message released - so a caller that takes the exception is never left with a reply still streaming.
            _logger.LogError(exception, "The transport failed while generating a reply.");
            LastError = BitAiError.FromException(exception);
            assistant?.CompleteOpenParts();
            CancelHeldApprovals(assistant);
            assistant?.SetError(LastError);
            Processor.EndGeneration(BitAiMessageStatus.Failed);

            if (!Options.CatchTransportExceptions) throw;
        }
        finally
        {
            Release(turn);

            // Disposal discards the results held for their call, but an event the turn was applying as it was disposed
            // may have held one after that: the turn, which is what applied it, discards it as it unwinds.
            if (_disposed) Processor.DiscardHeldToolResults();

            try
            {
                RaiseStateChanged();
                if (assistant is not null && !_disposed) TurnCompleted?.Invoke(assistant);
            }
            finally
            {
                End(turn);
            }
        }

        return assistant;
    }

    // A call the transport's side holds for a decision - one nobody here waits for - has no stream left to go on in once
    // the turn is stopped or has failed: it is cancelled, as a restored conversation shows such a call
    // (BitAiConversationSerializer), so its prompt goes rather than sending a decision nothing will act on - or one that a
    // server keeping its calls across requests acts on after the user has moved on. A call the session runs itself has
    // been cancelled already, with the wait for its decision.
    private static void CancelHeldApprovals(BitAiMessage? assistant)
    {
        if (assistant is null) return;

        foreach (var part in assistant.Parts)
        {
            if (part is BitAiToolCallPart { Status: BitAiToolCallStatus.AwaitingApproval } call)
            {
                call.SetStatus(BitAiToolCallStatus.Cancelled);
            }
        }
    }

    private async Task ConsumeAsync(BitAiRequest request, CancellationToken cancellationToken)
    {
        // Each request is a stream of its own, whose usage adds to the earlier rounds' rather than
        // replacing it.
        Processor.BeginStream();

        var stream = _transport.StreamAsync(request, cancellationToken);
        if (Options.Pacing is { } pacing) stream = stream.Paced(pacing, cancellationToken);

        await foreach (var evt in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            Processor.Apply(evt);
            if (evt is BitAiToolCallStartedEvent started) DescribeToolCall(started.ToolCallId);
            if (evt is BitAiErrorEvent { IsTerminal: true } error) LastError = error.Error;
            RaiseStateChanged();
        }
    }

    // A call is described by its tool's definition - the display name and the risk its card shows - from its first
    // render rather than from when it runs, and by the session's own registry, which is the one that knows the tool. The
    // definition a transport sends with a call (BitAiToolCallStartedEvent.Definition) is for a call its side runs: a call
    // the session runs itself is described by the tool it runs whatever the transport sent, so the prompt to allow it
    // shows the risk of what will run.
    private void DescribeToolCall(string toolCallId)
    {
        if (_tools is null || FindToolCall(toolCallId) is not { } call) return;
        if (call.Definition is not null && RunsItself(call) is false) return;

        call.Definition = _tools.Find(call.ToolName)?.Definition ?? call.Definition;
    }

    private BitAiRequest BuildRequest(BitAiMessage assistant)
    {
        var messages = new List<IBitAiMessage>(Conversation.Messages.Count);
        foreach (var message in Conversation.Messages)
        {
            if (ReferenceEquals(message, assistant) && message.Parts.Count == 0) continue;
            messages.Add(message);
        }

        return new BitAiRequest(messages)
        {
            ConversationId = Conversation.ConversationId,
            Options = Options.RequestOptions,
            Tools = _tools is null ? [] : _tools.Tools.Select(t => t.Definition).ToArray(),
            Services = _services,
        };
    }

    private async Task ExecuteToolsAsync(IReadOnlyList<BitAiToolCallPart> calls, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>(calls.Count);
        foreach (var call in calls) tasks.Add(ExecuteToolAsync(call, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ExecuteToolAsync(BitAiToolCallPart call, CancellationToken cancellationToken)
    {
        var tool = _tools?.Find(call.ToolName);
        if (tool is null)
        {
            call.SetResult(BitAiToolOutcome.Failure(new BitAiError($"No tool named '{call.ToolName}' is registered.")
            {
                Code = "unknown_tool",
                Kind = BitAiErrorKind.Tool,
            }));
            return;
        }

        // The tool that runs describes the call, rather than a definition the transport sent (see DescribeToolCall).
        call.Definition = tool.Definition;

        var invocation = new BitAiToolInvocation(call.ToolCallId, call.ToolName, call.Arguments ?? default)
        {
            ConversationId = Conversation.ConversationId,
            MessageId = call.Message?.MessageId,
            Services = _services,
            Progress = new ToolProgressReporter(call, RaiseStateChanged),
        };

        try
        {
            // A decision the user already recorded on the call (Approve, while the stream was still coming in) stands.
            // Without one, the call waits when the transport asked for approval (a BitAiToolApprovalRequestedEvent put it
            // in AwaitingApproval) as much as when the local policy does: the server's request is not the policy's to
            // override.
            var decision = call.Approval;
            if (decision is BitAiApprovalDecision.Undecided
                && (call.Status is BitAiToolCallStatus.AwaitingApproval
                    || await NeedsApprovalAsync(tool.Definition, invocation, cancellationToken).ConfigureAwait(false)))
            {
                decision = await WaitForApprovalAsync(call, cancellationToken).ConfigureAwait(false);
                if (decision is BitAiApprovalDecision.Undecided) decision = BitAiApprovalDecision.Rejected;
            }

            if (decision is BitAiApprovalDecision.Rejected)
            {
                call.SetResult(BitAiToolOutcome.Failure(new BitAiError(Strings.ToolCallDeclined)
                {
                    Code = "rejected",
                    Kind = BitAiErrorKind.Tool,
                }));
                call.SetStatus(BitAiToolCallStatus.Rejected);
                return;
            }

            call.SetStatus(BitAiToolCallStatus.Running);
            RaiseStateChanged();

            var started = DateTimeOffset.UtcNow;
            var outcome = await tool.InvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
            call.SetResult(outcome with { Duration = outcome.Duration ?? DateTimeOffset.UtcNow - started });

            // The sources a tool consulted are the reply's to list, as they are for a result the transport reports (which
            // the processor merges).
            if (call.Message is { } message && outcome.Citations is { Count: > 0 } citations)
            {
                foreach (var citation in citations) message.AddCitation(citation);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            call.SetStatus(BitAiToolCallStatus.Cancelled);
            throw;
        }
        catch (Exception exception)
        {
            // The message is shown as the failure of the call, but never the stack trace: the detail is shown too, behind
            // a disclosure, and a trace hands the user the internals of the server. It stays on Exception, for the logs.
            _logger.LogError(exception, "Tool {ToolName} threw while executing.", call.ToolName);
            call.SetResult(BitAiToolOutcome.Failure(new BitAiError(exception.Message)
            {
                Kind = BitAiErrorKind.Tool,
                Exception = exception,
                IsRetryable = true,
            }));
        }
        finally
        {
            RaiseStateChanged();
        }
    }

    private async ValueTask<bool> NeedsApprovalAsync(
        BitAiToolDefinition definition,
        BitAiToolInvocation invocation,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _approvalPolicy.RequiresApprovalAsync(definition, invocation, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "The approval policy threw; requiring approval to stay safe.");
            return true;
        }
    }

    private async Task<BitAiApprovalDecision> WaitForApprovalAsync(BitAiToolCallPart call, CancellationToken cancellationToken)
    {
        var waiter = new TaskCompletionSource<BitAiApprovalDecision>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_approvals) _approvals[call.ToolCallId] = waiter;

        call.SetStatus(BitAiToolCallStatus.AwaitingApproval);
        if (!_disposed) ApprovalRequested?.Invoke(call);
        RaiseStateChanged();

        await using var registration = cancellationToken.Register(() => waiter.TrySetCanceled(cancellationToken))
            .ConfigureAwait(false);

        try
        {
            return await waiter.Task.ConfigureAwait(false);
        }
        finally
        {
            // A cancelled wait must not stay registered, or a late Approve would revive the call.
            lock (_approvals)
            {
                if (_approvals.TryGetValue(call.ToolCallId, out var current) && ReferenceEquals(current, waiter))
                {
                    _approvals.Remove(call.ToolCallId);
                }
            }
        }
    }

    // Whether the session's own tool loop runs the call once it is decided: it executes tools, and has one of that name.
    private bool RunsItself(BitAiToolCallPart call) => Options.ExecuteTools && _tools?.Find(call.ToolName) is not null;

    private async Task SendApprovalAsync(IBitAiApprovalTransport transport, BitAiToolCallPart call, BitAiApprovalDecision decision)
    {
        var response = new BitAiApprovalResponse(call.ToolCallId, call.ToolName, decision)
        {
            ConversationId = Conversation.ConversationId,
            MessageId = call.Message?.MessageId,
        };

        try
        {
            await transport.SendApprovalAsync(response, _lifetime.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            // The session is gone, and the conversation with it.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "The decision on the tool call {ToolCallId} could not be sent to the transport.", call.ToolCallId);

            if (_disposed) return;

            // Only a call still where the decision left it is failed. One answered meanwhile - the decision got through
            // and only the response to the send was lost, or the turn was stopped - keeps what it has.
            var decided = decision is BitAiApprovalDecision.Rejected ? BitAiToolCallStatus.Rejected : BitAiToolCallStatus.Pending;
            if (call.Result is not null || call.Status != decided) return;

            // The side holding the call never heard the decision, so the call will not go on: it ends as failed rather
            // than showing a decision that never arrived.
            call.SetResult(BitAiToolOutcome.Failure(new BitAiError(Strings.ApprovalNotDelivered)
            {
                Code = "approval_not_delivered",
                Kind = BitAiErrorKind.Tool,
                Exception = exception,
                IsRetryable = false,
            }));
            RaiseStateChanged();
        }
    }

    private BitAiToolCallPart? FindToolCall(string toolCallId)
    {
        for (var i = Conversation.Messages.Count - 1; i >= 0; i--)
        {
            if (Conversation.Messages[i].FindToolCall(toolCallId) is { } call) return call;
        }

        return null;
    }

    private static string Summarize(string text)
    {
        var trimmed = text.Trim().ReplaceLineEndings(" ");
        return trimmed.Length <= 48 ? trimmed : string.Concat(trimmed.AsSpan(0, 48).TrimEnd(), "...");
    }

    /// <inheritdoc />
    /// <remarks>
    /// Cancels the turn in flight, if any, and detaches every handler: the session raises no events
    /// after this, so a disposed component is never asked to render.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        StateChanged = null;
        TurnCompleted = null;
        ApprovalRequested = null;
        ApprovalDecided = null;

        // Never disposed itself, so a decision still on its way can read it whenever it gets there.
        _lifetime.Cancel();

        // Pairs with the publish-then-check in StartTurnAsync: the flag must be visible before the
        // turn is read, or a turn starting now could be missed by both sides.
        Interlocked.MemoryBarrier();

        // The running turn owns and disposes its token source, and keeps its claim until it has
        // unwound; disposing the source here as well would make a concurrent Stop throw.
        Volatile.Read(ref _turn)?.Cancel();

        lock (_approvals)
        {
            foreach (var waiter in _approvals.Values) waiter.TrySetCanceled();
            _approvals.Clear();
        }

        // A result still held for its call would otherwise be dropped onto the conversation after the session is gone.
        // The turn cancelled above discards again as it unwinds (RunTurnAsync), for a result it held after this.
        Processor.DiscardHeldToolResults();

        await Task.CompletedTask.ConfigureAwait(false);
    }

    // A result the transport reported for a call it never made. The processor has put the error on the message already,
    // possibly after the turn ended (the hold runs out on a timer), so the UI is told about it too.
    private void OnToolResultDropped(BitAiOrphanedToolResultException exception)
    {
        _logger.LogWarning(exception, "The transport reported a result for the tool call {ToolCallId}, which never started; the result was dropped.", exception.ToolCallId);
        RaiseStateChanged();
    }

    private void RaiseStateChanged()
    {
        if (!_disposed) StateChanged?.Invoke();
    }

    // A turn's claim on the session: the token sources it owns, and the completion of its end. Published as one, so whoever
    // reads the turn in flight holds the completion of that very turn rather than of one that claimed the session since.
    private sealed class Turn(CancellationTokenSource cts, CancellationTokenSource? timeout)
    {
        public CancellationTokenSource Cts { get; } = cts;

        public CancellationTokenSource? Timeout { get; } = timeout;

        // Completed once the turn has released its claim and reported its end (End).
        public TaskCompletionSource Ended { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Cancel()
        {
            try
            {
                Cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The turn finished and released its token source between the read and the cancel.
            }
        }
    }

    private sealed class ToolProgressReporter(BitAiToolCallPart call, Action notify) : IProgress<BitAiToolProgress>
    {
        public void Report(BitAiToolProgress value)
        {
            call.SetProgress(value);
            notify();
        }
    }
}
