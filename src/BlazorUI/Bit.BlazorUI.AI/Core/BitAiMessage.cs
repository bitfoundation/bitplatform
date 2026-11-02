using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// A single message: a role, an ordered list of heterogeneous parts, and the metadata around them.
/// </summary>
/// <remarks>
/// <para>
/// Mutable and observable. Streaming mutates the message in place and raises change events rather
/// than producing a new object per token, because a conversation of five hundred messages cannot
/// afford to reallocate on every delta.
/// </para>
/// <para>
/// Two events are exposed deliberately. <see cref="Changed"/> fires for structural changes such as
/// a part being added or the status moving, and <see cref="PartChanged"/> fires for content changes
/// inside an existing part. A message component subscribes to the first, a part component to the
/// second, so a token arriving in the third of six parts re-renders one small subtree.
/// </para>
/// <para>
/// The part, attachment, and citation lists are copy-on-write: a writer replaces the array under a
/// lock, and readers always see an immutable snapshot. Events are applied on the transport's thread
/// while the renderer enumerates these lists, and a <see cref="List{T}"/> enumerated during an add
/// throws. Structural changes are rare next to deltas, so copying on each one is cheap.
/// </para>
/// </remarks>
public sealed class BitAiMessage : IBitAiMessage
{
    private readonly object _gate = new();
    private volatile BitAiMessagePart[] _parts = [];
    private volatile BitAiAttachment[] _attachments = [];
    private volatile BitAiCitation[] _citations = [];
    private int _version;

    /// <summary>Creates a message.</summary>
    /// <param name="role">Who authored it.</param>
    /// <param name="messageId">The identifier, or <see langword="null"/> to generate one.</param>
    public BitAiMessage(BitAiRole role, string? messageId = null)
    {
        Role = role;
        MessageId = messageId ?? BitAiId.NewMessageId();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public string MessageId { get; }

    /// <inheritdoc />
    public BitAiRole Role { get; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>The parts that make up the message, in order.</summary>
    public IReadOnlyList<BitAiMessagePart> Parts => _parts;

    /// <inheritdoc />
    IReadOnlyList<IBitAiMessagePart> IBitAiMessage.Parts => _parts;

    /// <summary>Files carried by the message.</summary>
    public IReadOnlyList<BitAiAttachment> Attachments => _attachments;

    /// <inheritdoc />
    IReadOnlyList<BitAiAttachment> IBitAiMessage.Attachments => _attachments;

    /// <summary>
    /// Every source cited by the message, deduplicated and numbered in the order first seen.
    /// </summary>
    public IReadOnlyList<BitAiCitation> Citations => _citations;

    /// <inheritdoc />
    public BitAiMessageStatus Status { get; private set; } = BitAiMessageStatus.Complete;

    /// <inheritdoc />
    public BitAiGenerationMetadata? Metadata { get; private set; }

    /// <summary>The failure that ended the message, when it failed.</summary>
    public BitAiError? Error { get; private set; }

    /// <summary>A transient status line shown while the message is generating.</summary>
    /// <remarks>
    /// It says what is happening while nothing else shows it (<c>Searching the web</c>), so it does not outlive that:
    /// it is cleared as the message leaves the pending and streaming states, and a
    /// <see cref="BitAiConversationStreamProcessor"/> clears it as soon as the answer streams in after it (see
    /// <see cref="BitAiStatusEvent"/>).
    /// </remarks>
    public string? StatusText { get; private set; }

    /// <summary>User sentiment attached to the message.</summary>
    public BitAiFeedbackKind Feedback { get; private set; }

    /// <summary>Free-text feedback the user supplied alongside <see cref="Feedback"/>.</summary>
    public string? FeedbackComment { get; private set; }

    /// <summary>
    /// The agent run that produced this message, when it came from a multi-step agent.
    /// </summary>
    public BitAiAgentRun? Run { get; internal set; }

    /// <summary>Application-defined values attached to the message.</summary>
    public Dictionary<string, object?> Extensions { get; } = [];

    /// <summary>Increments on every change, structural or otherwise.</summary>
    public int Version => _version;

    /// <summary><see langword="true"/> while the message is pending or streaming.</summary>
    public bool IsBusy => Status is BitAiMessageStatus.Pending or BitAiMessageStatus.Streaming;

    /// <summary>Raised after a structural change: a part added or removed, or the status moving.</summary>
    public event Action<BitAiMessage>? Changed;

    /// <summary>Raised after a part reports a content change.</summary>
    public event Action<BitAiMessage, BitAiMessagePart>? PartChanged;

    /// <summary>Adds a part to the end of the message.</summary>
    /// <typeparam name="TPart">The part type, returned so calls can be chained.</typeparam>
    /// <param name="part">The part to add.</param>
    /// <returns>The added part.</returns>
    public TPart AddPart<TPart>(TPart part) where TPart : BitAiMessagePart
    {
        ArgumentNullException.ThrowIfNull(part);
        part.Message = this;
        lock (_gate) _parts = [.. _parts, part];
        NotifyChanged();
        return part;
    }

    /// <summary>Inserts a part at a position.</summary>
    /// <typeparam name="TPart">The part type, returned so calls can be chained.</typeparam>
    /// <param name="index">Where to insert.</param>
    /// <param name="part">The part to insert.</param>
    /// <returns>The inserted part.</returns>
    public TPart InsertPart<TPart>(int index, TPart part) where TPart : BitAiMessagePart
    {
        ArgumentNullException.ThrowIfNull(part);
        lock (_gate)
        {
            var parts = new List<BitAiMessagePart>(_parts);
            parts.Insert(index, part);
            _parts = [.. parts];
        }

        part.Message = this;
        NotifyChanged();
        return part;
    }

    /// <summary>Removes a part.</summary>
    /// <param name="partId">The part to remove.</param>
    /// <returns><see langword="true"/> when a part was removed.</returns>
    public bool RemovePart(string partId)
    {
        BitAiMessagePart removed;
        lock (_gate)
        {
            var parts = _parts;
            var index = Array.FindIndex(parts, p => p.PartId == partId);
            if (index < 0) return false;
            removed = parts[index];
            _parts = [.. parts.AsSpan(0, index), .. parts.AsSpan(index + 1)];
        }

        removed.Message = null;
        NotifyChanged();
        return true;
    }

    /// <summary>Finds a part by identifier.</summary>
    /// <param name="partId">The part identifier.</param>
    /// <returns>The part, or <see langword="null"/> when the message has no such part.</returns>
    public BitAiMessagePart? FindPart(string partId)
    {
        var parts = _parts;
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].PartId == partId) return parts[i];
        }

        return null;
    }

    /// <summary>Finds a part by identifier and type.</summary>
    /// <typeparam name="TPart">The expected part type.</typeparam>
    /// <param name="partId">The part identifier.</param>
    /// <returns>The part, or <see langword="null"/> when it is missing or of another type.</returns>
    public TPart? FindPart<TPart>(string partId) where TPart : BitAiMessagePart => FindPart(partId) as TPart;

    /// <summary>
    /// Returns the last part when it matches the given kind and is still open, and otherwise appends
    /// a new one. This is how a delta that names no part finds its target.
    /// </summary>
    /// <typeparam name="TPart">The part type.</typeparam>
    /// <param name="kind">The kind the open part must have.</param>
    /// <param name="factory">Creates the part when one has to be opened.</param>
    /// <returns>The open part.</returns>
    /// <remarks>
    /// Matching only the last part is deliberate. Text that arrives after a tool call belongs in a
    /// new paragraph below the tool card, not appended to the paragraph above it.
    /// </remarks>
    public TPart GetOrCreateOpenPart<TPart>(string kind, Func<TPart> factory) where TPart : BitAiMessagePart
    {
        ArgumentNullException.ThrowIfNull(factory);
        var parts = _parts;
        if (parts.Length > 0
            && parts[^1] is TPart last
            && string.Equals(last.Kind, kind, StringComparison.Ordinal)
            && last is not IBitAiStreamablePart { IsStreaming: false })
        {
            return last;
        }

        return AddPart(factory());
    }

    /// <summary>Enumerates the parts of a given type, in order.</summary>
    /// <typeparam name="TPart">The part type.</typeparam>
    /// <returns>The matching parts.</returns>
    public IEnumerable<TPart> PartsOf<TPart>() where TPart : BitAiMessagePart => _parts.OfType<TPart>();

    /// <summary>Finds a tool call anywhere in the message by its call identifier.</summary>
    /// <param name="toolCallId">The call identifier.</param>
    /// <returns>The tool call part, or <see langword="null"/> when the message has no such call.</returns>
    public BitAiToolCallPart? FindToolCall(string toolCallId)
    {
        var parts = _parts;
        for (var i = parts.Length - 1; i >= 0; i--)
        {
            if (parts[i] is BitAiToolCallPart call && call.ToolCallId == toolCallId) return call;
        }

        return null;
    }

    /// <summary>Adds an attachment.</summary>
    /// <param name="attachment">The file to attach.</param>
    public void AddAttachment(BitAiAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        lock (_gate) _attachments = [.. _attachments, attachment];
        NotifyChanged();
    }

    /// <summary>Removes an attachment.</summary>
    /// <param name="attachmentId">The attachment to remove.</param>
    /// <returns><see langword="true"/> when an attachment was removed.</returns>
    public bool RemoveAttachment(string attachmentId)
    {
        lock (_gate)
        {
            var attachments = _attachments;
            var index = Array.FindIndex(attachments, a => a.AttachmentId == attachmentId);
            if (index < 0) return false;
            _attachments = [.. attachments.AsSpan(0, index), .. attachments.AsSpan(index + 1)];
        }

        NotifyChanged();
        return true;
    }

    /// <summary>
    /// Adds a citation, deduplicating by source and assigning the next marker number.
    /// </summary>
    /// <param name="citation">The citation to add.</param>
    /// <returns>
    /// The stored citation, carrying its assigned <see cref="BitAiCitation.Number"/>. When the same source
    /// was already cited, that marker number is reused.
    /// </returns>
    public BitAiCitation AddCitation(BitAiCitation citation)
    {
        ArgumentNullException.ThrowIfNull(citation);

        BitAiCitation stored;
        lock (_gate)
        {
            var citations = _citations;
            var existing = citations.FirstOrDefault(c => c.CitationId == citation.CitationId);
            if (existing is not null) return existing;

            var number = citations
                .FirstOrDefault(c => c.Source.SourceId == citation.Source.SourceId)?.Number
                ?? citations.Select(c => c.Number ?? 0).DefaultIfEmpty(0).Max() + 1;

            stored = citation with { Number = citation.Number ?? number };
            _citations = [.. citations, stored];
        }

        NotifyChanged();
        return stored;
    }

    /// <summary>The distinct sources cited by this message, in marker order.</summary>
    /// <returns>The sources.</returns>
    public IReadOnlyList<BitAiCitationSource> GetSources() => _citations
        .GroupBy(c => c.Source.SourceId)
        .OrderBy(g => g.Min(c => c.Number ?? int.MaxValue))
        .Select(g => g.First().Source)
        .ToList();

    /// <summary>Moves the message to a new lifecycle status.</summary>
    /// <param name="status">The new status.</param>
    public void SetStatus(BitAiMessageStatus status)
    {
        if (Status == status) return;
        Status = status;
        if (status is not (BitAiMessageStatus.Pending or BitAiMessageStatus.Streaming)) StatusText = null;
        NotifyChanged();
    }

    /// <summary>Sets the transient status line shown while generating.</summary>
    /// <remarks>
    /// Raises <see cref="Changed"/> only when the text actually changes. The line stays until it is replaced, cleared,
    /// or the message completes; a <see cref="BitAiConversationStreamProcessor"/> also clears it when the answer streams
    /// in after it.
    /// </remarks>
    /// <param name="statusText">The status text, or <see langword="null"/> to clear it.</param>
    public void SetStatusText(string? statusText)
    {
        if (StatusText == statusText) return;
        StatusText = statusText;
        NotifyChanged();
    }

    /// <summary>Records the failure that ended the message and moves it to a terminal status.</summary>
    /// <param name="error">The failure.</param>
    public void SetError(BitAiError? error)
    {
        Error = error;
        if (error is not null)
        {
            Status = error.Kind is BitAiErrorKind.Cancelled ? BitAiMessageStatus.Cancelled : BitAiMessageStatus.Failed;
        }

        NotifyChanged();
    }

    /// <summary>
    /// Merges generation metadata, keeping existing values wherever the incoming record leaves a
    /// field null. Providers report metadata piecemeal, so replacing wholesale loses information.
    /// </summary>
    /// <param name="metadata">The metadata to merge.</param>
    public void MergeMetadata(BitAiGenerationMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        Metadata = Metadata is null ? metadata : new BitAiGenerationMetadata
        {
            ModelId = metadata.ModelId ?? Metadata.ModelId,
            ProviderId = metadata.ProviderId ?? Metadata.ProviderId,
            Usage = metadata.Usage.IsEmpty ? Metadata.Usage : metadata.Usage,
            FinishReason = metadata.FinishReason is BitAiFinishReason.Unknown
                ? Metadata.FinishReason
                : metadata.FinishReason,
            StartedAt = metadata.StartedAt ?? Metadata.StartedAt,
            CompletedAt = metadata.CompletedAt ?? Metadata.CompletedAt,
            TimeToFirstToken = metadata.TimeToFirstToken ?? Metadata.TimeToFirstToken,
            Temperature = metadata.Temperature ?? Metadata.Temperature,
            ContextWindow = metadata.ContextWindow ?? Metadata.ContextWindow,
            Extra = metadata.Extra ?? Metadata.Extra,
        };
        NotifyChanged();
    }

    /// <summary>
    /// Restores persisted state in one step without raising intermediate notifications. Used when
    /// a conversation is rebuilt from a snapshot.
    /// </summary>
    internal void Restore(BitAiMessageStatus status, BitAiGenerationMetadata? metadata, BitAiError? error, BitAiFeedbackKind feedback, string? feedbackComment, BitAiAgentRun? run)
    {
        Status = status;
        Metadata = metadata;
        Error = error;
        Feedback = feedback;
        FeedbackComment = feedbackComment;
        Run = run;
        NotifyChanged();
    }

    /// <summary>Records user sentiment about the message.</summary>
    /// <param name="feedback">The sentiment.</param>
    /// <param name="comment">Optional free-text detail.</param>
    public void SetFeedback(BitAiFeedbackKind feedback, string? comment = null)
    {
        Feedback = feedback;
        FeedbackComment = comment;
        NotifyChanged();
    }

    /// <inheritdoc />
    public string GetText()
    {
        var parts = _parts;
        if (parts.Length == 1 && parts[0] is BitAiTextPart single and not BitAiReasoningPart) return single.Text;

        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            if (part is BitAiReasoningPart) continue;
            if (part is not IBitAiTextualPart { Text: { Length: > 0 } text }) continue;
            if (builder.Length > 0) builder.Append("\n\n");
            builder.Append(text);
        }

        return builder.ToString();
    }

    /// <summary>Marks every open part complete, used when a message ends abruptly.</summary>
    public void CompleteOpenParts()
    {
        foreach (var part in _parts)
        {
            switch (part)
            {
                case BitAiReasoningPart reasoning: reasoning.Complete(); break;
                case BitAiTextPart text: text.Complete(); break;
                case BitAiStructuredDataPart structured: structured.Complete(); break;
                case BitAiGenerativeUiPart generative: generative.Complete(); break;
                case BitAiToolCallPart { Status: BitAiToolCallStatus.Streaming } call: call.CompleteArguments(); break;
            }
        }
    }

    internal void OnPartChanged(BitAiMessagePart part)
    {
        Interlocked.Increment(ref _version);
        PartChanged?.Invoke(this, part);
    }

    private void NotifyChanged()
    {
        Interlocked.Increment(ref _version);
        Changed?.Invoke(this);
    }
}
