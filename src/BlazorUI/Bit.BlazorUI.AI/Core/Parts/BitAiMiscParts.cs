namespace Bit.BlazorUI;

/// <summary>
/// A reference to an artifact held by the conversation.
/// </summary>
/// <remarks>
/// The artifact itself lives on the conversation so that revisions across several turns update one
/// object. The part only says where it was introduced, which is what lets the message list show an
/// inline card that opens the shared artifact panel.
/// </remarks>
public sealed class BitAiArtifactPart : BitAiMessagePart
{
    /// <summary>Creates an artifact reference.</summary>
    /// <param name="artifactId">The artifact identifier.</param>
    /// <param name="partId">The identifier, or <see langword="null"/> to generate one.</param>
    public BitAiArtifactPart(string artifactId, string? partId = null) : base(partId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);
        ArtifactId = artifactId;
    }

    /// <inheritdoc />
    public override string Kind => BitAiMessagePartKinds.Artifact;

    /// <summary>The artifact identifier.</summary>
    public string ArtifactId { get; }

    /// <summary>
    /// The artifact version introduced here, so an older turn keeps showing what it produced.
    /// </summary>
    /// <remarks>
    /// Named distinctly from <see cref="BitAiMessagePart.Version"/>, which counts changes to the part
    /// itself and means something entirely different.
    /// </remarks>
    public int? ArtifactVersion { get; set; }
}

/// <summary>
/// A tool result carried on its own, for providers that emit results as separate tool messages.
/// </summary>
/// <remarks>
/// The framework reconciles these onto the originating <see cref="BitAiToolCallPart"/> when it can find
/// one, and renders them standalone when it cannot.
/// </remarks>
public sealed class BitAiToolResultPart : BitAiMessagePart
{
    /// <summary>Creates a tool result part.</summary>
    /// <param name="toolCallId">The call being answered.</param>
    /// <param name="outcome">The result.</param>
    /// <param name="partId">The identifier, or <see langword="null"/> to generate one.</param>
    public BitAiToolResultPart(string toolCallId, BitAiToolOutcome outcome, string? partId = null) : base(partId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolCallId);
        ArgumentNullException.ThrowIfNull(outcome);
        ToolCallId = toolCallId;
        Outcome = outcome;
    }

    /// <inheritdoc />
    public override string Kind => BitAiMessagePartKinds.ToolResult;

    /// <summary>The call being answered.</summary>
    public string ToolCallId { get; }

    /// <summary>The tool that produced the result, when known.</summary>
    public string? ToolName { get; set; }

    /// <summary>The result.</summary>
    public BitAiToolOutcome Outcome { get; }
}

/// <summary>
/// An error surfaced inside the message body rather than replacing the whole message.
/// </summary>
/// <remarks>
/// Used when part of a response failed but the rest is worth keeping, for example when one of
/// several parallel tool calls threw.
/// </remarks>
public sealed class BitAiErrorPart : BitAiMessagePart
{
    /// <summary>Creates an error part.</summary>
    /// <param name="error">The failure.</param>
    /// <param name="partId">The identifier, or <see langword="null"/> to generate one.</param>
    public BitAiErrorPart(BitAiError error, string? partId = null) : base(partId)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
    }

    /// <inheritdoc />
    public override string Kind => BitAiMessagePartKinds.Error;

    /// <summary>The failure.</summary>
    public BitAiError Error { get; }
}

/// <summary>
/// An application-defined part carrying a typed payload.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
/// <remarks>
/// The escape hatch for content the framework has no concept for. Register a renderer against the
/// same <see cref="BitAiMessagePart.Kind"/> string and it participates in message rendering, copying,
/// and virtualization like any built-in part.
/// </remarks>
/// <example>
/// <code>
/// var part = new BitAiCustomPart&lt;Invoice&gt;("acme:invoice", invoice);
/// message.AddPart(part);
/// </code>
/// </example>
public sealed class BitAiCustomPart<T> : BitAiMessagePart, IBitAiTextualPart
{
    private T _data;

    /// <summary>Creates a custom part.</summary>
    /// <param name="kind">The discriminator a renderer is registered against.</param>
    /// <param name="data">The payload.</param>
    /// <param name="partId">The identifier, or <see langword="null"/> to generate one.</param>
    public BitAiCustomPart(string kind, T data, string? partId = null) : base(partId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        Kind = kind;
        _data = data;
    }

    /// <inheritdoc />
    public override string Kind { get; }

    /// <summary>The payload. Setting it notifies observers.</summary>
    public T Data
    {
        get => _data;
        set
        {
            _data = value;
            NotifyChanged();
        }
    }

    /// <summary>
    /// The plain-text projection, used for copy and export. Defaults to nothing.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>Notifies observers after mutating <see cref="Data"/> in place.</summary>
    public void Refresh() => NotifyChanged();
}

/// <summary>
/// A loosely typed application-defined part, for payloads that arrive as JSON.
/// </summary>
public sealed class BitAiCustomJsonPart : BitAiMessagePart, IBitAiTextualPart
{
    /// <summary>Creates a custom JSON part.</summary>
    /// <param name="kind">The discriminator a renderer is registered against.</param>
    /// <param name="data">The payload.</param>
    /// <param name="partId">The identifier, or <see langword="null"/> to generate one.</param>
    public BitAiCustomJsonPart(string kind, JsonElement data, string? partId = null) : base(partId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        Kind = kind;
        Data = data;
    }

    /// <inheritdoc />
    public override string Kind { get; }

    /// <summary>The payload.</summary>
    public JsonElement Data { get; }

    /// <inheritdoc />
    public string? Text { get; set; }
}
