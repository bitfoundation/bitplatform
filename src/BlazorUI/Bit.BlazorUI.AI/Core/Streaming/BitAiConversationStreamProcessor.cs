namespace Bit.BlazorUI;

/// <summary>
/// Applies a stream of <see cref="BitAiEvent"/> to a <see cref="BitAiConversation"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the reducer at the centre of the framework. Transports produce events, this class turns
/// them into message parts, tool calls, artifacts, and agent steps, and components observe the
/// resulting state. Nothing else in the framework needs to know the event vocabulary.
/// </para>
/// <para>
/// It is deliberately usable on its own. Feeding recorded events through a processor is the
/// simplest way to test rendering, and the demo replays fixtures this way.
/// </para>
/// <para>
/// A tool result is given to the call of its id, once the arguments of that call are complete. One that arrives before
/// any such call has started is held for <see cref="ToolResultHoldTime"/>; when no call has started by then, the result
/// is dropped (see <see cref="ToolResultDropped"/>).
/// </para>
/// </remarks>
public sealed class BitAiConversationStreamProcessor
{
    /// <summary>The default of <see cref="ToolResultHoldTime"/>: ten seconds.</summary>
    public static readonly TimeSpan DefaultToolResultHoldTime = TimeSpan.FromSeconds(10);

    private readonly BitAiConversation _conversation;
    private readonly TimeProvider _time;

    // Serializes what the events do with what the expiry of a held tool result does, which runs on a timer: the two would
    // otherwise look up and change the same calls and messages at once.
    private readonly object _gate = new();

    // The tool results that arrived before their call, by the id of that call, with the message each arrived for and
    // when, and the one timer that drops the oldest when its time is up.
    private readonly Dictionary<string, HeldToolResult> _heldResults = new(StringComparer.Ordinal);
    private ITimer? _heldResultsTimer;
    private TimeSpan _toolResultHoldTime = DefaultToolResultHoldTime;

    // The tool results whose call has started but is still streaming its arguments. They wait for the arguments rather
    // than for the clock - a call whose result is set takes no more of them - however long those stream.
    private readonly Dictionary<string, HeldToolResult> _resultsAwaitingArguments = new(StringComparer.Ordinal);

    // The calls that became ready, and the results dropped, while the gate was held: raised as ToolCallReady and
    // ToolResultDropped once it is released, so neither handler runs while the processor is held. (The Changed handlers
    // of the messages and parts do run under it - on the thread applying the event, as they did before there was a
    // gate, or on the timer's when it drops a result.)
    private List<BitAiToolCallPart>? _readyCalls;
    private List<BitAiOrphanedToolResultException>? _droppedResults;

    // Usage within a stream is cumulative, so each report replaces the stream's last one. These hold
    // what each message and run had before the current stream began, which the stream's latest report
    // is added to.
    private readonly Dictionary<BitAiMessage, BitAiTokenUsage> _messageUsageBefore = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<BitAiAgentRun, BitAiTokenUsage> _runUsageBefore = new(ReferenceEqualityComparer.Instance);

    // The current stream's latest usage snapshot, merged field by field: a provider that reports
    // input tokens at the start and output tokens at the end must not have the first report erased.
    private BitAiTokenUsage _streamUsage;
    private DateTimeOffset? _startedAt;
    private DateTimeOffset? _firstTokenAt;

    /// <summary>Creates a processor targeting a conversation.</summary>
    /// <param name="conversation">The conversation to mutate.</param>
    public BitAiConversationStreamProcessor(BitAiConversation conversation) : this(conversation, null) { }

    /// <summary>Creates a processor targeting a conversation, timing held tool results with the given clock.</summary>
    /// <param name="conversation">The conversation to mutate.</param>
    /// <param name="timeProvider">
    /// The clock <see cref="ToolResultHoldTime"/> is measured with and expires on, or <see langword="null"/> for the system
    /// clock. A test passes a fake one to move time on.
    /// </param>
    public BitAiConversationStreamProcessor(BitAiConversation conversation, TimeProvider? timeProvider)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        _conversation = conversation;
        _time = timeProvider ?? TimeProvider.System;
    }

    /// <summary>The message events are currently being applied to.</summary>
    public BitAiMessage? Current { get; private set; }

    /// <summary>The agent run events are currently being applied to.</summary>
    public BitAiAgentRun? CurrentRun { get; private set; }

    /// <summary>
    /// Raised when a tool call has complete arguments and is ready to execute or request approval.
    /// </summary>
    /// <remarks>
    /// The processor never executes tools itself: it only records what the model asked for. The
    /// chat session subscribes to this to run them, which keeps the reducer free of side effects.
    /// A call the transport answered ahead of its arguments (see <see cref="ToolResultHoldTime"/>) is raised with its
    /// <see cref="BitAiToolCallPart.Result"/> set already: a subscriber that runs tools skips those, as the session does,
    /// or it would run here a tool the server ran.
    /// </remarks>
    public event Action<BitAiToolCallPart>? ToolCallReady;

    /// <summary>Raised for every event, after it has been applied. Useful for logging and tracing.</summary>
    public event Action<BitAiEvent>? EventApplied;

    /// <summary>
    /// Raised when a tool result that arrived before its call is dropped, because no call of its id started within
    /// <see cref="ToolResultHoldTime"/>.
    /// </summary>
    /// <remarks>
    /// Raised on the thread the hold expired on - a timer's, when no event arrived in the meantime - after an error part
    /// carrying the same exception has been added to the message the result arrived for. Useful for logging: a dropped
    /// result is a transport reporting a call it never made.
    /// </remarks>
    public event Action<BitAiOrphanedToolResultException>? ToolResultDropped;

    /// <summary>
    /// How long a tool result that arrives before any call of its id is held for that call to start. The default is
    /// <see cref="DefaultToolResultHoldTime"/>.
    /// </summary>
    /// <remarks>
    /// A call that starts within this time is given the result as soon as its arguments are complete, and is not run
    /// again. When none has started by then, the result is dropped: an error part carrying a
    /// <see cref="BitAiOrphanedToolResultException"/> is added to the message it arrived for, and
    /// <see cref="ToolResultDropped"/> is raised. <see cref="TimeSpan.Zero"/> drops such a result at once; there is no
    /// upper bound. A change applies to the results held from then on.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public TimeSpan ToolResultHoldTime
    {
        get => _toolResultHoldTime;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            _toolResultHoldTime = value;
        }
    }

    /// <summary>
    /// Discards every tool result held for a call that has not started, and stops the timer that would drop them.
    /// </summary>
    /// <remarks>
    /// Nothing is added to the conversation and <see cref="ToolResultDropped"/> is not raised: what is discarded was never
    /// shown. A <see cref="BitAiChatSession"/> calls this as it is disposed, so a conversation that outlives it is not
    /// handed an error part afterwards. A result waiting for the arguments of a call that has started is kept: it goes to
    /// that call.
    /// </remarks>
    public void DiscardHeldToolResults()
    {
        lock (_gate)
        {
            _heldResults.Clear();
            ScheduleHeldResultsTimer();
        }
    }

    /// <summary>
    /// Begins a generation, creating the assistant message the stream will fill in.
    /// </summary>
    /// <param name="message">
    /// An existing message to stream into, for regeneration. When <see langword="null"/> a new
    /// assistant message is appended.
    /// </param>
    /// <returns>The message being generated.</returns>
    public BitAiMessage BeginGeneration(BitAiMessage? message = null)
    {
        lock (_gate)
        {
            _startedAt = DateTimeOffset.UtcNow;
            _firstTokenAt = null;
            BeginStream();

            Current = message ?? _conversation.AddMessage(new BitAiMessage(BitAiRole.Assistant));
            Current.SetStatus(BitAiMessageStatus.Pending);
            Current.MergeMetadata(new BitAiGenerationMetadata { StartedAt = _startedAt });
            return Current;
        }
    }

    /// <summary>
    /// Marks the start of another transport stream within the current generation.
    /// </summary>
    /// <remarks>
    /// Usage reported within one stream is cumulative, so each <see cref="BitAiUsageUpdatedEvent"/>
    /// replaces the last. Usage from separate streams, such as the request made after each tool
    /// round trip, adds up. Call this before applying the events of each further stream so the two
    /// can be told apart; <see cref="BeginGeneration"/> begins the first stream itself.
    /// </remarks>
    public void BeginStream()
    {
        lock (_gate)
        {
            _messageUsageBefore.Clear();
            _runUsageBefore.Clear();
            _streamUsage = default;
        }
    }

    /// <summary>
    /// Applies one event.
    /// </summary>
    /// <param name="evt">The event to apply. Unknown event types are ignored.</param>
    public void Apply(BitAiEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        List<BitAiToolCallPart>? ready;
        List<BitAiOrphanedToolResultException>? dropped;

        lock (_gate)
        {
            // A held result whose time is up is dropped ahead of the event, whether or not the timer has fired yet, so a
            // call arriving after that never takes it.
            if (DropExpiredResults()) ScheduleHeldResultsTimer();

            ApplyEvent(evt);
            (ready, dropped) = TakePending();
        }

        Raise(ready, dropped);
        EventApplied?.Invoke(evt);
    }

    private void ApplyEvent(BitAiEvent evt)
    {
        switch (evt)
        {
            case BitAiMessageStartedEvent started: OnMessageStarted(started); break;
            case BitAiTextDeltaEvent text: OnTextDelta(text); break;
            case BitAiReasoningDeltaEvent reasoning: OnReasoningDelta(reasoning); break;
            case BitAiPartStartedEvent part: OnPartStarted(part); break;
            case BitAiPartCompletedEvent part: OnPartCompleted(part); break;
            case BitAiStatusEvent status: Target(evt)?.SetStatusText(status.Status); break;
            case BitAiImageEvent image: OnImage(image); break;
            case BitAiFileEvent file: Target(evt)?.AddPart(new BitAiFilePart(file.Attachment, file.PartId)); break;
            case BitAiCitationEvent citation: Target(evt)?.AddCitation(citation.Citation); break;
            case BitAiToolCallStartedEvent tool: OnToolCallStarted(tool); break;
            case BitAiToolCallDeltaEvent tool: FindToolCall(tool.ToolCallId)?.AppendArguments(tool.ArgumentsDelta); break;
            case BitAiToolCallCompletedEvent tool: OnToolCallCompleted(tool); break;
            case BitAiToolProgressEvent tool: FindToolCall(tool.ToolCallId)?.SetProgress(tool.Progress); break;
            case BitAiToolResultEvent tool: OnToolResult(tool); break;
            case BitAiToolApprovalRequestedEvent tool: OnApprovalRequested(tool); break;
            case BitAiArtifactCreatedEvent artifact: OnArtifactCreated(artifact); break;
            case BitAiArtifactDeltaEvent artifact: _conversation.FindArtifact(artifact.ArtifactId)?.Append(artifact.Delta); break;
            case BitAiArtifactCompletedEvent artifact: _conversation.FindArtifact(artifact.ArtifactId)?.Complete(artifact.Summary); break;
            case BitAiStructuredDataDeltaEvent structured: OnStructuredDelta(structured); break;
            case BitAiStructuredDataEvent structured: OnStructured(structured); break;
            case BitAiGenerativeUiEvent generative: OnGenerativeUi(generative); break;
            case BitAiGenerativeUiDeltaEvent generative: OnGenerativeUiDelta(generative); break;
            case BitAiUsageUpdatedEvent usage: OnUsage(usage); break;
            case BitAiMetadataEvent metadata: OnMetadata(metadata); break;
            case BitAiAgentRunStartedEvent run: OnRunStarted(run); break;
            case BitAiAgentPlanEvent plan: _conversation.GetOrAddRun(plan.RunId).SetPlan(plan.Steps); break;
            case BitAiAgentStepStartedEvent step: OnStepStarted(step); break;
            case BitAiAgentStepCompletedEvent step: OnStepCompleted(step); break;
            case BitAiAgentRunCompletedEvent run: _conversation.GetOrAddRun(run.RunId).SetStatus(run.Status); break;
            case BitAiAgentInputRequestedEvent input: OnInputRequested(input); break;
            case BitAiErrorEvent error: OnError(error); break;
            case BitAiMessageCompletedEvent completed: OnMessageCompleted(completed); break;
        }
    }

    /// <summary>
    /// Ends the generation, closing any open parts and stamping the final timings.
    /// </summary>
    /// <param name="status">The terminal status. Defaults to complete.</param>
    public void EndGeneration(BitAiMessageStatus status = BitAiMessageStatus.Complete)
    {
        lock (_gate) EndGenerationCore(status);
    }

    private void EndGenerationCore(BitAiMessageStatus status)
    {
        var message = Current;
        message?.CompleteOpenParts();
        if (message is { Status: BitAiMessageStatus.Pending or BitAiMessageStatus.Streaming }) message.SetStatus(status);

        // No more of their arguments will come: the calls still streaming them are completed - just above when they are
        // on the message being generated, here when the stream put them on another one - and a result waiting for them
        // is given now. Whether or not a message is being generated: the stream may have named existing messages only.
        foreach (var (toolCallId, awaiting) in _resultsAwaitingArguments)
        {
            if (FindToolCall(toolCallId) is not { } call) continue;
            if (call.IsStreaming) call.CompleteArguments();
            GiveResult(call, awaiting);
        }

        _resultsAwaitingArguments.Clear();

        if (message is null) return;

        // A run interrupted mid-flight never receives its completion event, and would otherwise
        // show as running for as long as the conversation is on screen.
        if (CurrentRun is { Status: BitAiAgentRunStatus.Planning or BitAiAgentRunStatus.Running } run
            && status is BitAiMessageStatus.Cancelled or BitAiMessageStatus.Failed)
        {
            run.SetStatus(status is BitAiMessageStatus.Cancelled ? BitAiAgentRunStatus.Cancelled : BitAiAgentRunStatus.Failed);
        }

        message.MergeMetadata(new BitAiGenerationMetadata
        {
            CompletedAt = DateTimeOffset.UtcNow,
            TimeToFirstToken = _firstTokenAt is { } first && _startedAt is { } start ? first - start : null,
        });

        Current = null;
        CurrentRun = null;

        // The generation's last stream ends with it, so events replayed afterwards start afresh.
        BeginStream();
    }

    private BitAiMessage? Target(BitAiEvent evt)
    {
        if (evt.MessageId is { } id)
        {
            var found = _conversation.FindMessage(id);
            if (found is not null) return found;
        }

        return Current ??= _conversation.AddMessage(new BitAiMessage(BitAiRole.Assistant, evt.MessageId));
    }

    private BitAiMessage? EnsureStreaming(BitAiEvent evt)
    {
        var message = Target(evt);
        if (message is null) return null;
        if (message.Status is BitAiMessageStatus.Pending or BitAiMessageStatus.Draft) message.SetStatus(BitAiMessageStatus.Streaming);

        // A status line says what is happening while nothing else shows it. The answer streaming in after it says more,
        // so it supersedes the line; a status the transport sends later (a tool it is running) shows until the answer
        // moves on again, or the message completes.
        if (message.StatusText is not null) message.SetStatusText(null);

        _firstTokenAt ??= DateTimeOffset.UtcNow;
        return message;
    }

    private BitAiToolCallPart? FindToolCall(string toolCallId)
    {
        if (Current?.FindToolCall(toolCallId) is { } fromCurrent) return fromCurrent;
        for (var i = _conversation.Messages.Count - 1; i >= 0; i--)
        {
            if (_conversation.Messages[i].FindToolCall(toolCallId) is { } found) return found;
        }

        return null;
    }

    private void OnMessageStarted(BitAiMessageStartedEvent evt)
    {
        if (evt.MessageId is { } id && Current?.MessageId != id && _conversation.FindMessage(id) is { } existing)
        {
            Current = existing;
        }
        else if (Current is null)
        {
            Current = _conversation.AddMessage(new BitAiMessage(evt.Role, evt.MessageId));
        }

        // Otherwise the message the generation began with takes the stream, whatever id the transport gave the message
        // on its side - as every other event carrying an id the conversation does not know lands on it (Target). Adding
        // another would leave the first one pending, with nothing ever coming for it.

        Current.SetStatus(BitAiMessageStatus.Streaming);
    }

    private void OnTextDelta(BitAiTextDeltaEvent evt)
    {
        if (EnsureStreaming(evt) is not { } message) return;
        var part = evt.PartId is { } id
            ? message.FindPart<BitAiTextPart>(id) ?? message.AddPart(new BitAiTextPart(null, id))
            : message.GetOrCreateOpenPart(BitAiMessagePartKinds.Text, () => new BitAiTextPart());
        part.Append(evt.Delta);
    }

    private void OnReasoningDelta(BitAiReasoningDeltaEvent evt)
    {
        if (EnsureStreaming(evt) is not { } message) return;
        var part = evt.PartId is { } id
            ? message.FindPart<BitAiReasoningPart>(id) ?? message.AddPart(new BitAiReasoningPart(null, id))
            : message.GetOrCreateOpenPart(BitAiMessagePartKinds.Reasoning, () => new BitAiReasoningPart());
        if (evt.Signature is not null) part.Signature = evt.Signature;
        part.Append(evt.Delta);
    }

    private void OnPartStarted(BitAiPartStartedEvent evt)
    {
        if (EnsureStreaming(evt) is not { } message) return;
        if (evt.PartId is { } existing && message.FindPart(existing) is not null) return;

        BitAiMessagePart part = evt.Kind switch
        {
            BitAiMessagePartKinds.Reasoning => new BitAiReasoningPart(null, evt.PartId),
            BitAiMessagePartKinds.Markdown => new BitAiMarkdownPart(null, evt.PartId),
            BitAiMessagePartKinds.Code => new BitAiCodePart(null, Attribute(evt, "language"), evt.PartId),
            BitAiMessagePartKinds.StructuredData => new BitAiStructuredDataPart(Attribute(evt, "schema"), evt.PartId),
            BitAiMessagePartKinds.Sources => new BitAiSourcesPart(evt.PartId),
            _ => new BitAiTextPart(null, evt.PartId),
        };

        if (part is BitAiCodePart code) code.FileName = Attribute(evt, "fileName");
        message.AddPart(part);
    }

    private static string? Attribute(BitAiPartStartedEvent evt, string name) =>
        evt.Attributes is not null && evt.Attributes.TryGetValue(name, out var value) ? value?.ToString() : null;

    private void OnPartCompleted(BitAiPartCompletedEvent evt)
    {
        if (Target(evt) is not { } message) return;
        var part = evt.PartId is { } id ? message.FindPart(id) : message.Parts.LastOrDefault();
        switch (part)
        {
            case BitAiReasoningPart reasoning: reasoning.Complete(); break;
            case BitAiTextPart text: text.Complete(); break;
            case BitAiStructuredDataPart structured: structured.Complete(); break;
            case BitAiGenerativeUiPart generative: generative.Complete(); break;
        }
    }

    private void OnImage(BitAiImageEvent evt)
    {
        if (EnsureStreaming(evt) is not { } message) return;
        message.AddPart(new BitAiImagePart(evt.Url, evt.PartId)
        {
            Alt = evt.Alt,
            Width = evt.Width,
            Height = evt.Height,
        });
    }

    private void OnToolCallStarted(BitAiToolCallStartedEvent evt)
    {
        if (EnsureStreaming(evt) is not { } message) return;
        if (FindToolCall(evt.ToolCallId) is not null) return;

        var part = message.AddPart(new BitAiToolCallPart(evt.ToolCallId, evt.ToolName));

        // Attributed to the step that is running, so a timeline can show what each step called.
        if (CurrentRun?.CurrentStep is { Status: BitAiAgentStepStatus.Running } step) step.AddToolCall(evt.ToolCallId);

        if (evt.Arguments is { } arguments)
        {
            part.CompleteArguments(arguments);
            GiveHeldResult(part);
            (_readyCalls ??= []).Add(part);
        }
        else if (_heldResults.Remove(evt.ToolCallId, out var held))
        {
            // Its call has started: the result now waits for the arguments rather than for the clock.
            _resultsAwaitingArguments[evt.ToolCallId] = held;
            ScheduleHeldResultsTimer();
        }
    }

    private void OnToolCallCompleted(BitAiToolCallCompletedEvent evt)
    {
        // A call is ready once. A repeated completion, or one following a start that already carried
        // the arguments, would otherwise execute the tool a second time.
        if (FindToolCall(evt.ToolCallId) is not { IsStreaming: true } part) return;
        part.CompleteArguments(evt.Arguments);
        GiveHeldResult(part);
        (_readyCalls ??= []).Add(part);
    }

    private void OnToolResult(BitAiToolResultEvent evt)
    {
        // A result never opens a message of its own (Target would): one applied after the turn has ended - a late push,
        // a replayed fixture - goes to its call, or is held, and the transcript gains no empty assistant message for it.
        // A later result for the same call replaces an earlier one, held or not, as it does on a call that has one.
        if (FindToolCall(evt.ToolCallId) is { } part)
        {
            // What the result carries for a message (its citations) is the call's message's.
            var result = new HeldToolResult(evt, part.Message ?? FindTarget(evt), _time.GetTimestamp(), ToolResultHoldTime);

            // A call still streaming its arguments takes no more of them once its result is set, so the result waits for
            // them (GiveHeldResult); every other call takes it now.
            if (part.IsStreaming) _resultsAwaitingArguments[evt.ToolCallId] = result;
            else GiveResult(part, result);

            return;
        }

        // A result ahead of its call - parallel streams merged out of order - is held for the call rather than shown on
        // its own, and given to it once it has started (GiveHeldResult). A hold time of zero drops it at once.
        _heldResults[evt.ToolCallId] = new HeldToolResult(evt, FindTarget(evt), _time.GetTimestamp(), ToolResultHoldTime);
        DropExpiredResults();
        ScheduleHeldResultsTimer();
    }

    // The message an event is for, when there is one: the one of its id, else the one being generated. Never adds one.
    private BitAiMessage? FindTarget(BitAiEvent evt) =>
        (evt.MessageId is { } id ? _conversation.FindMessage(id) : null) ?? Current;

    // The arguments of the call are complete: a result that came ahead of them is given to it now, so the session does
    // not run it again.
    private void GiveHeldResult(BitAiToolCallPart part)
    {
        if (_resultsAwaitingArguments.Remove(part.ToolCallId, out var held) || _heldResults.Remove(part.ToolCallId, out held))
        {
            GiveResult(part, held);
            ScheduleHeldResultsTimer();
        }
    }

    private static void GiveResult(BitAiToolCallPart part, HeldToolResult result)
    {
        part.SetResult(result.Event.Outcome);
        AddCitations(result.Message, result.Event.Outcome);
    }

    // Under the gate. A held result whose time is up and no call of its id has started for is dropped, leaving an error
    // part on the message it arrived for. Returns whether any was, for the caller to set the timer again then: with
    // nothing dropped it is still due when the oldest result's time is up. Allocates nothing on the way through a stream
    // that holds a result not yet due, which is every event of the hold.
    private bool DropExpiredResults()
    {
        if (_heldResults.Count == 0) return false;

        List<HeldToolResult>? expired = null;
        foreach (var held in _heldResults.Values)
        {
            if (_time.GetElapsedTime(held.HeldAt) >= held.HoldTime) (expired ??= []).Add(held);
        }

        if (expired is null) return false;

        foreach (var held in expired)
        {
            _heldResults.Remove(held.Event.ToolCallId);

            var exception = new BitAiOrphanedToolResultException(held.Event.ToolCallId, held.Event.Outcome, held.HoldTime);

            held.Message?.AddPart(new BitAiErrorPart(new BitAiError(exception.Message)
            {
                Code = BitAiOrphanedToolResultException.ErrorCode,
                Kind = BitAiErrorKind.Tool,
                Exception = exception,
            }));

            (_droppedResults ??= []).Add(exception);
        }

        return true;
    }

    // Under the gate. One timer, due when the oldest held result's time is up; none while nothing is held.
    private void ScheduleHeldResultsTimer()
    {
        if (_heldResults.Count == 0)
        {
            _heldResultsTimer?.Dispose();
            _heldResultsTimer = null;
            return;
        }

        // A timer waits at most 49.7 days (uint.MaxValue - 1 milliseconds) at a time, and throws for more: a longer hold
        // is waited for in such stretches, each finding nothing due yet and setting the next.
        var due = _heldResults.Values.Min(h => h.HoldTime - _time.GetElapsedTime(h.HeldAt));
        if (due < TimeSpan.Zero) due = TimeSpan.Zero;
        else if (due > MaxTimerDueTime) due = MaxTimerDueTime;

        _heldResultsTimer ??= _time.CreateTimer(static state => ((BitAiConversationStreamProcessor)state!).OnHeldResultsTimer(),
                                                this,
                                                Timeout.InfiniteTimeSpan,
                                                Timeout.InfiniteTimeSpan);
        _heldResultsTimer.Change(due, Timeout.InfiniteTimeSpan);
    }

    private static readonly TimeSpan MaxTimerDueTime = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    // The timer of the held results, for when no event arrives to drop them: a stream that went quiet, or a turn that
    // ended.
    private void OnHeldResultsTimer()
    {
        List<BitAiToolCallPart>? ready;
        List<BitAiOrphanedToolResultException>? dropped;

        try
        {
            lock (_gate)
            {
                DropExpiredResults();
                ScheduleHeldResultsTimer();
                (ready, dropped) = TakePending();
            }

            Raise(ready, dropped);
        }
        catch
        {
            // On the timer's thread an exception has no caller to reach: unhandled, it would end the process. What can
            // throw here is the app's own - a Changed handler of the message, a ToolResultDropped handler - which throws
            // to the caller applying an event otherwise. The result is dropped either way; what a handler left undone is
            // its own to guard.
        }
    }

    // Under the gate: the calls the event or the timer made ready and the results it dropped, taken to be raised once
    // the gate is released.
    private (List<BitAiToolCallPart>? Ready, List<BitAiOrphanedToolResultException>? Dropped) TakePending()
    {
        var pending = (_readyCalls, _droppedResults);
        _readyCalls = null;
        _droppedResults = null;
        return pending;
    }

    // Outside the gate: the session's handlers only collect the call and log the drop, but an app's may run the tool,
    // render, apply another event, or wait on another thread that does.
    private void Raise(List<BitAiToolCallPart>? ready, List<BitAiOrphanedToolResultException>? dropped)
    {
        if (ready is not null)
        {
            foreach (var call in ready) ToolCallReady?.Invoke(call);
        }

        if (dropped is not null)
        {
            foreach (var exception in dropped) ToolResultDropped?.Invoke(exception);
        }
    }

    private static void AddCitations(BitAiMessage? message, BitAiToolOutcome outcome)
    {
        if (message is null || outcome.Citations is not { Count: > 0 } citations) return;

        foreach (var citation in citations) message.AddCitation(citation);
    }

    private sealed record HeldToolResult(BitAiToolResultEvent Event, BitAiMessage? Message, long HeldAt, TimeSpan HoldTime);

    private void OnApprovalRequested(BitAiToolApprovalRequestedEvent evt)
    {
        if (FindToolCall(evt.ToolCallId) is not { } part) return;
        part.ApprovalReason = evt.Reason;
        part.SetStatus(BitAiToolCallStatus.AwaitingApproval);
    }

    private void OnArtifactCreated(BitAiArtifactCreatedEvent evt)
    {
        var artifact = _conversation.GetOrAddArtifact(evt.ArtifactId, evt.Kind, evt.Title);
        artifact.Language = evt.Language ?? artifact.Language;
        artifact.Url = evt.Url ?? artifact.Url;
        artifact.Metadata = evt.Metadata ?? artifact.Metadata;

        if (!artifact.IsStreaming) artifact.BeginRevision();
        if (evt.Content is not null)
        {
            artifact.SetContent(evt.Content);
            artifact.Complete();
        }

        if (Target(evt) is { } message && !message.PartsOf<BitAiArtifactPart>().Any(p => p.ArtifactId == evt.ArtifactId))
        {
            message.AddPart(new BitAiArtifactPart(evt.ArtifactId) { ArtifactVersion = artifact.Version });
        }
    }

    private void OnStructuredDelta(BitAiStructuredDataDeltaEvent evt)
    {
        if (EnsureStreaming(evt) is not { } message) return;
        var part = evt.PartId is { } id
            ? message.FindPart<BitAiStructuredDataPart>(id) ?? message.AddPart(new BitAiStructuredDataPart(evt.SchemaName, id))
            : message.GetOrCreateOpenPart(BitAiMessagePartKinds.StructuredData, () => new BitAiStructuredDataPart(evt.SchemaName));
        part.SchemaName ??= evt.SchemaName;
        part.Append(evt.JsonDelta);
    }

    private void OnStructured(BitAiStructuredDataEvent evt)
    {
        if (EnsureStreaming(evt) is not { } message) return;
        message.AddPart(new BitAiStructuredDataPart(evt.Value, evt.SchemaName, evt.PartId));
    }

    private void OnGenerativeUi(BitAiGenerativeUiEvent evt)
    {
        if (EnsureStreaming(evt) is not { } message) return;
        var part = evt.PartId is { } id
            ? message.FindPart<BitAiGenerativeUiPart>(id) ?? message.AddPart(new BitAiGenerativeUiPart(evt.ComponentName, id))
            : message.AddPart(new BitAiGenerativeUiPart(evt.ComponentName));
        part.SetProps(evt.Props);
        if (!evt.IsPartial) part.Complete();
    }

    private void OnGenerativeUiDelta(BitAiGenerativeUiDeltaEvent evt)
    {
        if (EnsureStreaming(evt) is not { } message) return;
        var part = evt.PartId is { } id
            ? message.FindPart<BitAiGenerativeUiPart>(id) ?? message.AddPart(new BitAiGenerativeUiPart(evt.ComponentName, id))
            : message.GetOrCreateOpenPart(BitAiMessagePartKinds.GenerativeUi, () => new BitAiGenerativeUiPart(evt.ComponentName));
        part.AppendProps(evt.PropsDelta);
    }

    private void OnUsage(BitAiUsageUpdatedEvent evt)
    {
        if (Target(evt) is not { } message) return;
        message.MergeMetadata(new BitAiGenerationMetadata { Usage = RecordUsage(message, evt.Usage) });
    }

    private void OnMetadata(BitAiMetadataEvent evt)
    {
        if (Target(evt) is not { } message) return;

        // Usage carried on metadata is a usage report like any other, and must not overwrite the
        // usage earlier streams contributed to the message.
        var metadata = evt.Metadata;
        if (!metadata.Usage.IsEmpty) metadata = metadata with { Usage = RecordUsage(message, metadata.Usage) };
        message.MergeMetadata(metadata);
    }

    /// <summary>
    /// Records the current stream's latest cumulative usage against the message and the current run.
    /// </summary>
    /// <returns>The message's usage including the stream's latest report.</returns>
    private BitAiTokenUsage RecordUsage(BitAiMessage message, BitAiTokenUsage streamUsage)
    {
        _streamUsage = BitAiTokenUsage.Merge(_streamUsage, streamUsage);
        if (CurrentRun is { } run) run.SetUsage(Before(_runUsageBefore, run, run.Usage) + _streamUsage);
        return Before(_messageUsageBefore, message, message.Metadata?.Usage ?? default) + _streamUsage;

        static BitAiTokenUsage Before<T>(Dictionary<T, BitAiTokenUsage> before, T key, BitAiTokenUsage current) where T : notnull
        {
            if (!before.TryGetValue(key, out var usage)) before[key] = usage = current;
            return usage;
        }
    }

    private void OnRunStarted(BitAiAgentRunStartedEvent evt)
    {
        var run = _conversation.GetOrAddRun(evt.RunId);
        run.AgentName = evt.AgentName ?? run.AgentName;
        run.Goal = evt.Goal ?? run.Goal;
        run.SetStatus(BitAiAgentRunStatus.Running);
        CurrentRun = run;
        if (Target(evt) is { } message) message.Run = run;
    }

    private void OnStepStarted(BitAiAgentStepStartedEvent evt)
    {
        var run = _conversation.GetOrAddRun(evt.RunId);
        run.SetStatus(BitAiAgentRunStatus.Running);
        run.GetOrAddStep(evt.StepId, evt.Title).Start();
        CurrentRun = run;
    }

    private void OnStepCompleted(BitAiAgentStepCompletedEvent evt)
    {
        var run = _conversation.GetOrAddRun(evt.RunId);
        run.GetOrAddStep(evt.StepId).Complete(evt.Status, evt.Summary, evt.Error);
    }

    private void OnInputRequested(BitAiAgentInputRequestedEvent evt)
    {
        var run = _conversation.GetOrAddRun(evt.RunId);
        run.RequestInput(evt.Prompt, evt.Suggestions);
        CurrentRun = run;
    }

    private void OnError(BitAiErrorEvent evt)
    {
        var message = Target(evt);
        if (message is null) return;

        if (evt.IsTerminal)
        {
            message.CompleteOpenParts();
            message.SetError(evt.Error);
            CurrentRun?.SetError(evt.Error);
        }
        else
        {
            message.AddPart(new BitAiErrorPart(evt.Error));
        }
    }

    private void OnMessageCompleted(BitAiMessageCompletedEvent evt)
    {
        if (Target(evt) is not { } message) return;
        message.CompleteOpenParts();
        message.MergeMetadata(new BitAiGenerationMetadata { FinishReason = evt.FinishReason });
        if (message.Status is BitAiMessageStatus.Pending or BitAiMessageStatus.Streaming)
        {
            message.SetStatus(BitAiMessageStatus.Complete);
        }
    }
}
