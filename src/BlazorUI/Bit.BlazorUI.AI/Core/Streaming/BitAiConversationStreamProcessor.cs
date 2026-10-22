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
/// </remarks>
public sealed class BitAiConversationStreamProcessor
{
    private readonly BitAiConversation _conversation;

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
    public BitAiConversationStreamProcessor(BitAiConversation conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        _conversation = conversation;
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
    /// </remarks>
    public event Action<BitAiToolCallPart>? ToolCallReady;

    /// <summary>Raised for every event, after it has been applied. Useful for logging and tracing.</summary>
    public event Action<BitAiEvent>? EventApplied;

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
        _startedAt = DateTimeOffset.UtcNow;
        _firstTokenAt = null;
        BeginStream();

        Current = message ?? _conversation.AddMessage(new BitAiMessage(BitAiRole.Assistant));
        Current.SetStatus(BitAiMessageStatus.Pending);
        Current.MergeMetadata(new BitAiGenerationMetadata { StartedAt = _startedAt });
        return Current;
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
        _messageUsageBefore.Clear();
        _runUsageBefore.Clear();
        _streamUsage = default;
    }

    /// <summary>
    /// Applies one event.
    /// </summary>
    /// <param name="evt">The event to apply. Unknown event types are ignored.</param>
    public void Apply(BitAiEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

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

        EventApplied?.Invoke(evt);
    }

    /// <summary>
    /// Ends the generation, closing any open parts and stamping the final timings.
    /// </summary>
    /// <param name="status">The terminal status. Defaults to complete.</param>
    public void EndGeneration(BitAiMessageStatus status = BitAiMessageStatus.Complete)
    {
        if (Current is not { } message) return;

        message.CompleteOpenParts();
        if (message.Status is BitAiMessageStatus.Pending or BitAiMessageStatus.Streaming) message.SetStatus(status);

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
        if (Current is null || (evt.MessageId is not null && Current.MessageId != evt.MessageId))
        {
            Current = evt.MessageId is not null
                ? _conversation.FindMessage(evt.MessageId) ?? _conversation.AddMessage(new BitAiMessage(evt.Role, evt.MessageId))
                : _conversation.AddMessage(new BitAiMessage(evt.Role));
        }

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
            ToolCallReady?.Invoke(part);
        }
    }

    private void OnToolCallCompleted(BitAiToolCallCompletedEvent evt)
    {
        // A call is ready once. A repeated completion, or one following a start that already carried
        // the arguments, would otherwise execute the tool a second time.
        if (FindToolCall(evt.ToolCallId) is not { IsStreaming: true } part) return;
        part.CompleteArguments(evt.Arguments);
        ToolCallReady?.Invoke(part);
    }

    private void OnToolResult(BitAiToolResultEvent evt)
    {
        if (FindToolCall(evt.ToolCallId) is { } part)
        {
            part.SetResult(evt.Outcome);
        }
        else if (Target(evt) is { } message)
        {
            message.AddPart(new BitAiToolResultPart(evt.ToolCallId, evt.Outcome));
        }

        if (evt.Outcome.Citations is { Count: > 0 } citations && Target(evt) is { } target)
        {
            foreach (var citation in citations) target.AddCitation(citation);
        }
    }

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
