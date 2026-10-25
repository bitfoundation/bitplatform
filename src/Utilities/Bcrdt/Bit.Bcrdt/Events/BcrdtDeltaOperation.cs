namespace Bit.Bcrdt;

/// <summary>The three things a delta operation can do to a sequence.</summary>
public enum BcrdtDeltaKind
{
    /// <summary>Advance the cursor, leaving content untouched.</summary>
    Retain,
    /// <summary>Insert new content at the cursor.</summary>
    Insert,
    /// <summary>Remove content at the cursor.</summary>
    Delete,
}

/// <summary>
/// One step in a description of how a sequence changed.
/// </summary>
/// <remarks>
/// <para>
/// Deltas are what a view layer actually needs. Telling a component "the document changed" forces
/// it to re-read and re-render everything; telling it "retain 240, insert 'hello', delete 3" lets it
/// patch exactly what moved. For a large document under high-frequency editing that difference is
/// the whole rendering budget.
/// </para>
/// <para>
/// The shape follows the widely used retain/insert/delete convention, so bridging to an existing
/// editor's change model is mechanical rather than a translation exercise.
/// </para>
/// </remarks>
public sealed class BcrdtDeltaOperation
{
    private BcrdtDeltaOperation(
        BcrdtDeltaKind kind,
        int length,
        object? content,
        IReadOnlyDictionary<string, object?>? attributes,
        bool isEmbed = false)
    {
        Kind = kind;
        Length = length;
        Content = content;
        Attributes = attributes;
        IsEmbed = isEmbed;
    }

    /// <summary>
    /// Whether this insert step carries one embedded value - an image, a mention, a nested type -
    /// rather than text.
    /// </summary>
    /// <remarks>
    /// A string can be embedded too, so the type of <see cref="Content"/> alone cannot tell the two
    /// apart. Text deltas produced by the engine set this on every embed; consumers that project
    /// text should test it rather than <c>Content is string</c>, or an embedded string would be
    /// counted as its characters instead of as the single position it occupies.
    /// </remarks>
    public bool IsEmbed { get; }

    /// <summary>What this step does.</summary>
    public BcrdtDeltaKind Kind { get; }

    /// <summary>How many logical units this step covers.</summary>
    public int Length { get; internal set; }

    /// <summary>
    /// The inserted content: a <see cref="string"/> for text, a list of values for arrays, or an
    /// embedded value. <see langword="null"/> for retains and deletes.
    /// </summary>
    public object? Content { get; internal set; }

    /// <summary>Formatting attributes carried by this step, if any.</summary>
    public IReadOnlyDictionary<string, object?>? Attributes { get; internal set; }

    /// <summary>Creates a retain step.</summary>
    public static BcrdtDeltaOperation Retain(int length, IReadOnlyDictionary<string, object?>? attributes = null) =>
        new(BcrdtDeltaKind.Retain, length, null, attributes);

    /// <summary>Creates a delete step.</summary>
    public static BcrdtDeltaOperation Delete(int length) => new(BcrdtDeltaKind.Delete, length, null, null);

    /// <summary>Creates an insert step.</summary>
    public static BcrdtDeltaOperation Insert(
        object? content,
        int length,
        IReadOnlyDictionary<string, object?>? attributes = null) =>
        new(BcrdtDeltaKind.Insert, length, content, attributes);

    /// <summary>Creates an insert step for text.</summary>
    public static BcrdtDeltaOperation InsertText(string text, IReadOnlyDictionary<string, object?>? attributes = null) =>
        new(BcrdtDeltaKind.Insert, text.Length, text, attributes);

    /// <summary>
    /// Creates an insert step for a single embedded value, which occupies one position whatever it is
    /// - including a string.
    /// </summary>
    public static BcrdtDeltaOperation InsertEmbed(object value, IReadOnlyDictionary<string, object?>? attributes = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(BcrdtDeltaKind.Insert, 1, value, attributes, isEmbed: true);
    }

    /// <inheritdoc/>
    public override string ToString() => Kind switch
    {
        BcrdtDeltaKind.Retain => $"retain {Length}",
        BcrdtDeltaKind.Delete => $"delete {Length}",
        _ => $"insert {(Content is string s ? $"\"{s}\"" : Content?.ToString() ?? "null")}",
    };
}
