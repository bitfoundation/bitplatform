namespace Bit.Bcrdt;

/// <summary>
/// Where a participant's cursor and selection are, anchored to content rather than to indices.
/// </summary>
/// <param name="Field">The root type the selection lies in.</param>
/// <param name="Anchor">Where the selection was started.</param>
/// <param name="Head">Where the selection currently ends. Equal to the anchor for a plain caret.</param>
public sealed record BcrdtCollaboratorSelection(string Field, BcrdtRelativePosition Anchor, BcrdtRelativePosition Head)
{
    /// <summary>Whether this is a caret rather than a range.</summary>
    public bool IsCaret => ReferenceEquals(Anchor, Head) || Anchor.Encode().AsSpan().SequenceEqual(Head.Encode());

    /// <summary>Resolves the selection to indices in the document as it stands now.</summary>
    /// <returns>
    /// The resolved range, or <see langword="null"/> if the anchored content has not arrived yet.
    /// </returns>
    public (int Start, int End)? Resolve(BcrdtDocument doc)
    {
        BcrdtAbsolutePosition? anchor = Anchor.Resolve(doc);
        BcrdtAbsolutePosition? head = Head.Resolve(doc);
        if (anchor is null || head is null) return null;

        int start = Math.Min(anchor.Value.Index, head.Value.Index);
        int end = Math.Max(anchor.Value.Index, head.Value.Index);
        return (start, end);
    }
}
