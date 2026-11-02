namespace Bit.BlazorUI;

/// <summary>
/// One heterogeneous piece of a message.
/// </summary>
/// <remarks>
/// <para>
/// A message is an ordered list of parts rather than a string. Text, reasoning, code, images,
/// tool calls, citations, artifacts and application-defined content all implement this interface,
/// which is what lets the same <see cref="BitAiMessageList"/> render a mixed response without special-casing.
/// </para>
/// <para>
/// Implement this interface (or derive from <see cref="BitAiMessagePart"/>) to add a
/// custom part type, then register a renderer for its <see cref="Kind"/> with the part-renderer
/// registry so the UI knows how to draw it.
/// </para>
/// </remarks>
public interface IBitAiMessagePart
{
    /// <summary>
    /// Stable identifier for this part, unique within its message.
    /// </summary>
    /// <remarks>
    /// Streaming events address parts by this id, and the UI uses it as the diffing key, so it must
    /// not change once assigned.
    /// </remarks>
    string PartId { get; }

    /// <summary>
    /// The discriminator used to select a renderer and to round-trip the part through JSON.
    /// </summary>
    /// <remarks>
    /// Use a lowercase token for built-in kinds (<c>text</c>, <c>tool-call</c>) and a namespaced
    /// token for application kinds (<c>acme:invoice</c>) to avoid collisions.
    /// </remarks>
    string Kind { get; }
}

/// <summary>
/// A part that can contribute to the plain-text projection of a message.
/// </summary>
/// <remarks>
/// Copy-to-clipboard, export, search, and the text sent back to a provider on a follow-up turn all
/// use this. Parts that are purely visual should not implement it.
/// </remarks>
public interface IBitAiTextualPart : IBitAiMessagePart
{
    /// <summary>
    /// The plain-text projection of this part, or <see langword="null"/> to contribute nothing.
    /// </summary>
    string? Text { get; }
}

/// <summary>
/// A part that can still be growing while the message streams.
/// </summary>
public interface IBitAiStreamablePart : IBitAiMessagePart
{
    /// <summary>
    /// <see langword="true"/> while more deltas are expected for this part.
    /// </summary>
    /// <remarks>
    /// Renderers use this to suppress expensive work (syntax highlighting a half-written code
    /// block, laying out a partial table) and to show a caret or shimmer.
    /// </remarks>
    bool IsStreaming { get; }
}
