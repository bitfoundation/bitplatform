namespace Bit.BlazorUI;

/// <summary>
/// One unit of progress in a generation.
/// </summary>
/// <remarks>
/// <para>
/// A transport turns whatever its provider sends into a stream of these, and the framework turns
/// them into conversation state. That single indirection is what keeps UI components free of any
/// provider SDK: everything downstream of <see cref="IBitAiTransport"/> speaks only this vocabulary.
/// </para>
/// <para>
/// Identifiers are optional throughout. A transport that produces one assistant message with one
/// text part can emit a bare text delta and let the framework resolve the target, while a transport
/// driving several parallel parts addresses each one explicitly.
/// </para>
/// </remarks>
public abstract record BitAiEvent
{
    /// <summary>When the event was produced.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The message the event applies to, or <see langword="null"/> to target the message currently
    /// being generated.
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>A stable discriminator, used for logging, tracing, and JSON round-tripping.</summary>
    public abstract string Type { get; }
}

/// <summary>
/// An event that targets one part of a message.
/// </summary>
public abstract record BitAiPartScopedEvent : BitAiEvent
{
    /// <summary>
    /// The part the event applies to, or <see langword="null"/> to target the newest open part of a
    /// compatible kind, creating one if none is open.
    /// </summary>
    public string? PartId { get; init; }
}

/// <summary>
/// The provider accepted the request and generation has begun.
/// </summary>
/// <remarks>
/// Optional. The framework starts an assistant message on the first content event if this never
/// arrives, but emitting it lets the UI show a thinking indicator sooner.
/// </remarks>
public sealed record BitAiMessageStartedEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="role">The role of the message being started. Defaults to assistant.</param>
    public BitAiMessageStartedEvent(BitAiRole? role = null) => Role = role ?? BitAiRole.Assistant;

    /// <summary>The role of the message being started.</summary>
    public BitAiRole Role { get; }

    /// <inheritdoc />
    public override string Type => "message.started";
}

/// <summary>
/// Generation finished. No further events will target this message.
/// </summary>
public sealed record BitAiMessageCompletedEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="finishReason">Why generation stopped.</param>
    public BitAiMessageCompletedEvent(BitAiFinishReason finishReason = BitAiFinishReason.Stop) => FinishReason = finishReason;

    /// <summary>Why generation stopped.</summary>
    public BitAiFinishReason FinishReason { get; }

    /// <inheritdoc />
    public override string Type => "message.completed";
}

/// <summary>
/// A new part is opening. Optional, because content events create parts implicitly.
/// </summary>
/// <remarks>
/// Emit this when the part needs attributes a delta cannot carry, such as the language of a code
/// block, or when several parts of the same kind must stay distinct.
/// </remarks>
public sealed record BitAiPartStartedEvent : BitAiPartScopedEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="kind">The kind of part being opened, matching <see cref="IBitAiMessagePart.Kind"/>.</param>
    public BitAiPartStartedEvent(string kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        Kind = kind;
    }

    /// <summary>The kind of part being opened.</summary>
    public string Kind { get; }

    /// <summary>Attributes for the new part, such as a code language or an image alt text.</summary>
    public IReadOnlyDictionary<string, object?>? Attributes { get; init; }

    /// <inheritdoc />
    public override string Type => "part.started";
}

/// <summary>
/// A part is finished and will receive no further deltas.
/// </summary>
/// <remarks>
/// Renderers defer expensive work until this arrives, so emitting it promptly is what makes a code
/// block highlight the instant it closes rather than at the end of the whole message.
/// </remarks>
public sealed record BitAiPartCompletedEvent : BitAiPartScopedEvent
{
    /// <inheritdoc />
    public override string Type => "part.completed";
}

/// <summary>
/// More visible text for the message.
/// </summary>
public sealed record BitAiTextDeltaEvent : BitAiPartScopedEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="delta">The text to append. May be a single token or a whole paragraph.</param>
    public BitAiTextDeltaEvent(string delta)
    {
        ArgumentNullException.ThrowIfNull(delta);
        Delta = delta;
    }

    /// <summary>The text to append.</summary>
    public string Delta { get; }

    /// <inheritdoc />
    public override string Type => "text.delta";
}

/// <summary>
/// More reasoning text, rendered in a collapsed thinking panel rather than as the answer.
/// </summary>
public sealed record BitAiReasoningDeltaEvent : BitAiPartScopedEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="delta">The reasoning text to append.</param>
    public BitAiReasoningDeltaEvent(string delta)
    {
        ArgumentNullException.ThrowIfNull(delta);
        Delta = delta;
    }

    /// <summary>The reasoning text to append.</summary>
    public string Delta { get; }

    /// <summary>
    /// An opaque signature some providers require when reasoning is replayed on a later turn.
    /// </summary>
    public string? Signature { get; init; }

    /// <inheritdoc />
    public override string Type => "reasoning.delta";
}

/// <summary>
/// A short status line describing what is happening before output appears, for example
/// <c>Searching the web</c>.
/// </summary>
/// <remarks>
/// Transient by design: it replaces the previous status rather than accumulating, the content that
/// streams in after it supersedes it, and it is cleared when the message completes. The
/// <see cref="BitAiConversationStreamProcessor"/> clears it on the next event that streams the answer
/// itself - text, reasoning, a started part, an image, a tool call, structured data or a generated
/// component - so a status sent before the answer starts goes away as the answer appears, and one
/// sent between two pieces of it (a tool being run) shows until the answer moves on.
/// </remarks>
public sealed record BitAiStatusEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="status">The status text, or <see langword="null"/> to clear the current status.</param>
    public BitAiStatusEvent(string? status) => Status = status;

    /// <summary>The status text, or <see langword="null"/> to clear it.</summary>
    public string? Status { get; }

    /// <inheritdoc />
    public override string Type => "status";
}

/// <summary>
/// Updated token accounting. May arrive several times; later events replace earlier ones.
/// </summary>
/// <remarks>
/// <para>
/// The usage is cumulative for the stream that carries it: one call to
/// <see cref="IBitAiTransport.StreamAsync"/>. A later event in the same stream replaces an earlier one
/// rather than adding to it, so a transport can forward a provider's running totals as they come.
/// </para>
/// <para>
/// Separate streams add up. A turn that calls tools makes one request per round trip, and the
/// message it produces reports the sum of each stream's final usage, as does any agent run the
/// streams ran within.
/// </para>
/// </remarks>
public sealed record BitAiUsageUpdatedEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="usage">The usage of this stream so far.</param>
    public BitAiUsageUpdatedEvent(BitAiTokenUsage usage) => Usage = usage;

    /// <summary>The usage of this stream so far.</summary>
    public BitAiTokenUsage Usage { get; }

    /// <inheritdoc />
    public override string Type => "usage.updated";
}

/// <summary>
/// Generation metadata such as the resolved model id, merged into the message.
/// </summary>
public sealed record BitAiMetadataEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="metadata">The metadata to merge. Fields left null are ignored.</param>
    public BitAiMetadataEvent(BitAiGenerationMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        Metadata = metadata;
    }

    /// <summary>The metadata to merge.</summary>
    public BitAiGenerationMetadata Metadata { get; }

    /// <inheritdoc />
    public override string Type => "metadata";
}

/// <summary>
/// Something went wrong.
/// </summary>
/// <remarks>
/// A transport may emit a non-terminal error and keep streaming, for example when one tool of
/// several failed. A terminal error ends the message.
/// </remarks>
public sealed record BitAiErrorEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="error">The failure.</param>
    /// <param name="isTerminal">Whether generation is over.</param>
    public BitAiErrorEvent(BitAiError error, bool isTerminal = true)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
        IsTerminal = isTerminal;
    }

    /// <summary>The failure.</summary>
    public BitAiError Error { get; }

    /// <summary>Whether generation is over.</summary>
    public bool IsTerminal { get; }

    /// <inheritdoc />
    public override string Type => "error";
}

/// <summary>
/// An application-defined event, for transports carrying information the framework has no concept for.
/// </summary>
/// <remarks>
/// The reducer applies nothing for it: handle it through <see cref="BitAiConversationStreamProcessor.EventApplied"/>
/// (a session's <see cref="BitAiChatSession.Processor"/>), which is raised for every event, this one included.
/// </remarks>
public sealed record BitAiCustomEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="name">The event name, ideally namespaced to avoid collisions.</param>
    /// <param name="data">The payload.</param>
    public BitAiCustomEvent(string name, object? data = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Data = data;
    }

    /// <summary>The event name.</summary>
    public string Name { get; }

    /// <summary>The payload.</summary>
    public object? Data { get; }

    /// <inheritdoc />
    public override string Type => "custom";
}
