namespace Bit.Bcrdt;

/// <summary>
/// A cursor into a text sequence: where it is, and what formatting is in force there.
/// </summary>
/// <remarks>
/// Text operations cannot work from an index alone, because inserting at a position also has to
/// decide which formatting the new text inherits and which marks have to be re-established after
/// it. Carrying the attribute state along with the position is what lets a single left-to-right
/// pass answer both questions.
/// </remarks>
internal sealed class BcrdtTextCursor(BcrdtItem? left, BcrdtItem? right, int index, Dictionary<string, object?> attributes)
{
    public BcrdtItem? Left = left;
    public BcrdtItem? Right = right;
    public int Index = index;
    public readonly Dictionary<string, object?> Attributes = attributes;

    /// <summary>Steps one block to the right, folding any mark it passes into the attribute state.</summary>
    public void Forward()
    {
        if (Right is null) throw new InvalidOperationException("Cannot advance a cursor past the end of the text.");

        if (Right.Content is BcrdtContentFormat format)
        {
            if (!Right.Deleted) BcrdtAttributeComparer.Apply(Attributes, format);
        }
        else if (!Right.Deleted)
        {
            Index += Right.Length;
        }

        Left = Right;
        Right = Right.Right;
    }
}
