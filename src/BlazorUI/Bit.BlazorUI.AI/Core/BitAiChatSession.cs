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
/// rather than an error, because double-submit is a UI race, not a programming mistake.
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

    // Set for exactly as long as a turn is in flight: publishing it is how a turn claims the session,
    // so there is never a moment when a turn is running and Stop has nothing to cancel.
    private CancellationTokenSource? _cts;
    private volatile bool _disposed;

    /// <summary>Creates a session.</summary>
    /// <param name="transport">Produces responses.</param>
    /// <param name="conversation">The conversation to drive, or <see langword="null"/> for a new one.</param>
    /// <param name="options">Session settings.</param>
    /// <param name="tools">The tools the model may call.</param>
    /// <param name="approvalPolicy">Decides which calls need a human decision.</param>
    /// <param name="services">Scoped services made available to tools.</param>
    /// <param name="logger">Where to log transport and tool failures.</param>
    public BitAiChatSession(
        IBitAiTransport transport,
        BitAiConversation? conversation = null,
        BitAiChatSessionOptions? options = null,
        IBitAiToolRegistry? tools = null,
        IBitAiToolApprovalPolicy? approvalPolicy = null,
        IServiceProvider? services = null,
        ILogger<BitAiChatSession>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(transport);
        _transport = transport;
        _tools = tools;
        _approvalPolicy = approvalPolicy ?? new BitAiDefaultToolApprovalPolicy();
        _services = services;
        _logger = logger ?? NullLogger<BitAiChatSession>.Instance;

        Conversation = conversation ?? new BitAiConversation();
        Options = options ?? new BitAiChatSessionOptions();
        Processor = new BitAiConversationStreamProcessor(Conversation);
        Processor.ToolResultDropped += OnToolResultDropped;
    }

    /// <summary>The conversation this session drives.</summary>
    public BitAiConversation Conversation { get; }

    /// <summary>The settings in force.</summary>
    public BitAiChatSessionOptions Options { get; set; }

    /// <summary>The reducer applying events to the conversation.</summary>
    public BitAiConversationStreamProcessor Processor { get; }

    /// <summary><see langword="true"/> while a turn is in flight.</summary>
    public bool IsGenerating => Volatile.Read(ref _cts) is not null;

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
    /// Appends a user message and generates a reply.
    /// </summary>
    /// <param name="text">The user text. May be empty when attachments carry the request.</param>
    /// <param name="attachments">Files to attach.</param>
    /// <param name="cancellationToken">Cancels the turn.</param>
    /// <returns>The assistant message, or <see langword="null"/> when a turn was already running.</returns>
    public Task<BitAiMessage?> SubmitAsync(
        string text,
        IReadOnlyList<BitAiAttachment>? attachments = null,
        CancellationToken cancellationToken = default) =>
        StartTurnAsync(() =>
        {
            AppendUserMessage(text, attachments);
            return true;
        }, cancellationToken);

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
        try
        {
            Volatile.Read(ref _cts)?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The turn finished and released its token source between the read and the cancel.
        }

        RaiseStateChanged();
    }

    /// <summary>
    /// Records a human decision on a tool call that was waiting for one.
    /// </summary>
    /// <param name="toolCallId">The call being decided.</param>
    /// <param name="decision">The decision.</param>
    public void Approve(string toolCallId, BitAiApprovalDecision decision)
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
        if (waiter is null && call is not { Status: BitAiToolCallStatus.AwaitingApproval }) return;

        if (decision is BitAiApprovalDecision.ApprovedAlways
            && _approvalPolicy is BitAiDefaultToolApprovalPolicy policy
            && call is not null)
        {
            policy.RememberAlwaysApproved(call.ToolName);
        }

        call?.SetApproval(decision);
        waiter?.TrySetResult(decision);
        RaiseStateChanged();
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

        // Disposal sets its flag before cancelling the published source, and this reads the flag after
        // publishing, so one of the two always sees the other.
        if (Interlocked.CompareExchange(ref _cts, cts, null) is not null || _disposed)
        {
            Release(cts, timeout);
            return null;
        }

        bool proceed;
        try
        {
            proceed = prepare();
        }
        catch
        {
            Release(cts, timeout);
            throw;
        }

        if (!proceed)
        {
            Release(cts, timeout);
            return null;
        }

        return await RunTurnAsync(cts, timeout, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Ends a turn's claim on the session and disposes its token sources.</summary>
    /// <remarks>The turn owns its token sources. Stop and DisposeAsync only ever cancel them.</remarks>
    private void Release(CancellationTokenSource cts, CancellationTokenSource? timeout)
    {
        Interlocked.CompareExchange(ref _cts, null, cts);
        cts.Dispose();
        timeout?.Dispose();
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

    private async Task<BitAiMessage?> RunTurnAsync(
        CancellationTokenSource cts,
        CancellationTokenSource? timeout,
        CancellationToken cancellationToken)
    {
        LastError = null;
        var token = cts.Token;

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
                if (round > 0) assistant.SetStatus(BitAiMessageStatus.Streaming);

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
                    assistant.AddPart(new BitAiErrorPart(new BitAiError(
                        $"Stopped after {Options.MaxToolRoundtrips} tool round trips.")
                    {
                        Code = "tool_loop_limit",
                        Kind = BitAiErrorKind.Tool,
                    }));
                    break;
                }

                await ExecuteToolsAsync(ready, token).ConfigureAwait(false);
            }

            Processor.EndGeneration();
        }
        catch (OperationCanceledException)
        {
            var timedOut = timeout is { IsCancellationRequested: true } && !cancellationToken.IsCancellationRequested;
            LastError = timedOut
                ? new BitAiError("The request timed out.") { Kind = BitAiErrorKind.Timeout, Code = "timeout", IsRetryable = true }
                : new BitAiError("Generation stopped.") { Kind = BitAiErrorKind.Cancelled, Code = "cancelled" };

            assistant?.CompleteOpenParts();
            if (timedOut) assistant?.SetError(LastError);
            else assistant?.SetStatus(BitAiMessageStatus.Cancelled);
            Processor.EndGeneration(timedOut ? BitAiMessageStatus.Failed : BitAiMessageStatus.Cancelled);
        }
        catch (Exception exception) when (Options.CatchTransportExceptions)
        {
            _logger.LogError(exception, "The transport failed while generating a reply.");
            LastError = BitAiError.FromException(exception);
            assistant?.CompleteOpenParts();
            assistant?.SetError(LastError);
            Processor.EndGeneration(BitAiMessageStatus.Failed);
        }
        finally
        {
            Release(cts, timeout);
            RaiseStateChanged();
            if (assistant is not null && !_disposed) TurnCompleted?.Invoke(assistant);
        }

        return assistant;
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
            if (evt is BitAiErrorEvent { IsTerminal: true } error) LastError = error.Error;
            RaiseStateChanged();
        }
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

        call.Definition ??= tool.Definition;

        var invocation = new BitAiToolInvocation(call.ToolCallId, call.ToolName, call.Arguments ?? default)
        {
            ConversationId = Conversation.ConversationId,
            MessageId = call.Message?.MessageId,
            Services = _services,
            Progress = new ToolProgressReporter(call, RaiseStateChanged),
        };

        try
        {
            if (await NeedsApprovalAsync(tool.Definition, invocation, cancellationToken).ConfigureAwait(false))
            {
                var decision = await WaitForApprovalAsync(call, cancellationToken).ConfigureAwait(false);
                if (decision is BitAiApprovalDecision.Rejected or BitAiApprovalDecision.Undecided)
                {
                    call.SetResult(BitAiToolOutcome.Failure(new BitAiError("The user declined this tool call.")
                    {
                        Code = "rejected",
                        Kind = BitAiErrorKind.Tool,
                    }));
                    call.SetStatus(BitAiToolCallStatus.Rejected);
                    return;
                }
            }

            call.SetStatus(BitAiToolCallStatus.Running);
            RaiseStateChanged();

            var started = DateTimeOffset.UtcNow;
            var outcome = await tool.InvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
            call.SetResult(outcome with { Duration = outcome.Duration ?? DateTimeOffset.UtcNow - started });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            call.SetStatus(BitAiToolCallStatus.Cancelled);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Tool {ToolName} threw while executing.", call.ToolName);
            call.SetResult(BitAiToolOutcome.Failure(new BitAiError(exception.Message)
            {
                Kind = BitAiErrorKind.Tool,
                Detail = exception.ToString(),
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

        // Pairs with the publish-then-check in StartTurnAsync: the flag must be visible before the
        // token source is read, or a turn starting now could be missed by both sides.
        Interlocked.MemoryBarrier();

        // The running turn owns and disposes its token source, and keeps its claim until it has
        // unwound; disposing the source here as well would make a concurrent Stop throw.
        try
        {
            Volatile.Read(ref _cts)?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The turn ended while this was cancelling it.
        }

        lock (_approvals)
        {
            foreach (var waiter in _approvals.Values) waiter.TrySetCanceled();
            _approvals.Clear();
        }

        // A result still held for its call would otherwise be dropped onto the conversation after the session is gone.
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

    private sealed class ToolProgressReporter(BitAiToolCallPart call, Action notify) : IProgress<BitAiToolProgress>
    {
        public void Report(BitAiToolProgress value)
        {
            call.SetProgress(value);
            notify();
        }
    }
}
