namespace Bit.BlazorUI;

/// <summary>
/// Model reasoning, shown in a collapsed panel above the answer.
/// </summary>
/// <remarks>
/// Reasoning is a separate part rather than prefixed text so it can be collapsed by default,
/// excluded from copy-to-clipboard, and kept out of the plain-text projection that goes back to the
/// provider on the next turn.
/// </remarks>
public sealed class BitAiReasoningPart : BitAiTextPart
{
    /// <summary>Creates an empty part that expects more reasoning.</summary>
    public BitAiReasoningPart() : this(null, null) { }

    /// <summary>Creates a finished part with known content.</summary>
    /// <param name="text">The reasoning text.</param>
    public BitAiReasoningPart(string text) : this(text ?? throw new ArgumentNullException(nameof(text)), null) { }

    /// <summary>Creates a part with an explicit identifier.</summary>
    /// <param name="text">The reasoning text, or <see langword="null"/> for an empty part.</param>
    /// <param name="partId">The identifier, or <see langword="null"/> to generate one.</param>
    public BitAiReasoningPart(string? text, string? partId) : base(text, partId) =>
        StartedAt = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public override string Kind => BitAiMessagePartKinds.Reasoning;

    /// <summary>When reasoning began, used to show how long the model thought for.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>When reasoning finished.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>How long the model spent reasoning, once finished.</summary>
    public TimeSpan? Duration => CompletedAt is { } completed ? completed - StartedAt : null;

    /// <summary>
    /// An opaque signature some providers require when reasoning is replayed on a later turn.
    /// </summary>
    public string? Signature { get; set; }

    /// <inheritdoc />
    protected override void OnCompleted() => CompletedAt = DateTimeOffset.UtcNow;

    /// <summary>Restores persisted timings. Used by the conversation serializer.</summary>
    internal void RestoreTimings(DateTimeOffset startedAt, DateTimeOffset? completedAt)
    {
        StartedAt = startedAt;
        CompletedAt = completedAt;
    }
}

/// <summary>
/// A standalone code block with a language, distinct from a fenced block inside Markdown.
/// </summary>
/// <remarks>
/// Use this when the transport knows it is emitting code, for example a code-generation endpoint.
/// Fenced blocks inside a text part are handled by the Markdown renderer and do not need this type.
/// </remarks>
public sealed class BitAiCodePart : BitAiTextPart
{
    /// <summary>Creates an empty part that expects more code.</summary>
    public BitAiCodePart() : this(null, null, null) { }

    /// <summary>Creates a finished part with known content.</summary>
    /// <param name="code">The source.</param>
    /// <param name="language">The language identifier, for example <c>csharp</c>.</param>
    public BitAiCodePart(string code, string? language)
        : this(code ?? throw new ArgumentNullException(nameof(code)), language, null) { }

    /// <summary>Creates a part with an explicit identifier.</summary>
    /// <param name="code">The source, or <see langword="null"/> for an empty part.</param>
    /// <param name="language">The language identifier.</param>
    /// <param name="partId">The identifier, or <see langword="null"/> to generate one.</param>
    public BitAiCodePart(string? code, string? language, string? partId) : base(code, partId) =>
        Language = language;

    /// <inheritdoc />
    public override string Kind => BitAiMessagePartKinds.Code;

    /// <summary>The language identifier used to select a highlighter.</summary>
    public string? Language { get; set; }

    /// <summary>A file name shown in the block header, when the code belongs to a file.</summary>
    public string? FileName { get; set; }
}
