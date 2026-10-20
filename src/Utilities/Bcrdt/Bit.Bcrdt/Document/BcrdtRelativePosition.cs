namespace Bit.Bcrdt;

/// <summary>A position resolved against the document as it stands right now.</summary>
/// <param name="Type">The sequence the position lies in.</param>
/// <param name="Index">The character or element index.</param>
/// <param name="Association">Which side of the position new text attaches to.</param>
public readonly record struct BcrdtAbsolutePosition(BcrdtAbstractType Type, int Index, int Association);

/// <summary>
/// A position anchored to content rather than to an index.
/// </summary>
/// <remarks>
/// <para>
/// An index is the wrong way to remember a cursor in a collaborative document. "Character 40" means
/// something different the instant somebody types at character 10, so a remote cursor stored as an
/// index drifts away from the word it was actually on, and a selection stored as two indices
/// silently comes to cover different text.
/// </para>
/// <para>
/// A relative position instead names the specific character the cursor sits beside. That anchor is
/// stable no matter what anyone inserts or deletes elsewhere, and it survives the anchor itself
/// being deleted - the position resolves to where that character used to be. This is what makes
/// remote cursors, selections, comments and annotations behave correctly rather than approximately.
/// </para>
/// <para>
/// The association records which side new text should attach to. A cursor at the end of a word
/// normally wants text typed there to land before it, and one at the start of a word wants it to
/// land after; without that distinction, two people typing at the same point push each other around.
/// </para>
/// </remarks>
public sealed class BcrdtRelativePosition
{
    private BcrdtRelativePosition(BcrdtItemId? item, BcrdtItemId? typeItem, string? rootName, int association)
    {
        Item = item;
        TypeItem = typeItem;
        RootName = rootName;
        Association = association;
    }

    /// <summary>The content the position is anchored to, or null when anchored to a boundary.</summary>
    public BcrdtItemId? Item { get; }

    /// <summary>The block holding the containing type, when the position is at a boundary.</summary>
    public BcrdtItemId? TypeItem { get; }

    /// <summary>The root name of the containing type, when the position is at a boundary of a root.</summary>
    public string? RootName { get; }

    /// <summary>
    /// Which side new content attaches to: negative binds to the character on the left, zero or
    /// positive binds to the character on the right.
    /// </summary>
    public int Association { get; }

    /// <summary>
    /// Captures an index as a stable anchor.
    /// </summary>
    /// <param name="type">The sequence the index refers to.</param>
    /// <param name="index">The index to capture.</param>
    /// <param name="association">
    /// Negative to bind to the character to the left, zero or positive to bind to the right.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    public static BcrdtRelativePosition FromIndex(BcrdtAbstractType type, int index, int association = 0)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        BcrdtItem? cursor = type.Start;

        if (association < 0)
        {
            if (index == 0) return AtBoundary(type, association);
            index--;
        }

        while (cursor is not null)
        {
            if (!cursor.Deleted && cursor.Countable)
            {
                if (cursor.Length > index)
                {
                    return new BcrdtRelativePosition(
                        new BcrdtItemId(cursor.Id.Client, cursor.Id.Clock + index), null, null, association);
                }
                index -= cursor.Length;
            }

            if (cursor.Right is null && association < 0)
            {
                return new BcrdtRelativePosition(cursor.LastId, null, null, association);
            }
            cursor = cursor.Right;
        }

        return AtBoundary(type, association);
    }

    private static BcrdtRelativePosition AtBoundary(BcrdtAbstractType type, int association) =>
        type.ContainingItem is { } item
            ? new BcrdtRelativePosition(null, item.Id, null, association)
            : new BcrdtRelativePosition(null, null, type.FindRootKey(), association);

    /// <summary>
    /// Resolves the anchor against a document.
    /// </summary>
    /// <returns>
    /// The current index, or <see langword="null"/> if the document has not yet received the content
    /// the position is anchored to - which is a normal, temporary state during synchronization
    /// rather than an error.
    /// </returns>
    public BcrdtAbsolutePosition? Resolve(BcrdtDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);

        if (Item is not { } anchor)
        {
            // An anchor-less position is a boundary. Binding rightward, there was no character to
            // the right to bind to, so it is the end; binding leftward, there was none to the left,
            // so it is the start.
            BcrdtAbstractType? type = ResolveBoundaryType(doc);
            if (type is null) return null;
            return new BcrdtAbsolutePosition(type, Association >= 0 ? type.Length : 0, Association);
        }

        // The anchor is content this replica has not received yet.
        if (doc.Store.GetState(anchor.Client) <= anchor.Clock) return null;
        if (!doc.Store.TryFind(anchor, out BcrdtAbstractStruct? found) || found is not BcrdtItem item) return null;
        if (item.Parent is not BcrdtAbstractType parent) return null;
        if (parent.ContainingItem is { Deleted: true }) return new BcrdtAbsolutePosition(parent, 0, Association);

        // Content that has since been deleted resolves to where it used to be, which is what keeps a
        // cursor sensible when somebody deletes the word it was sitting on.
        long offsetIntoItem = anchor.Clock - item.Id.Clock;
        int index = item.Deleted || !item.Countable
            ? 0
            : (int)offsetIntoItem + (Association >= 0 ? 0 : 1);

        for (BcrdtItem? left = item.Left; left is not null; left = left.Left)
        {
            if (!left.Deleted && left.Countable) index += left.Length;
        }

        return new BcrdtAbsolutePosition(parent, index, Association);
    }

    private BcrdtAbstractType? ResolveBoundaryType(BcrdtDocument doc)
    {
        if (RootName is not null)
        {
            return doc.RootTypes.GetValueOrDefault(RootName);
        }

        if (TypeItem is { } typeItem &&
            doc.Store.TryFind(typeItem, out BcrdtAbstractStruct? found) &&
            found is BcrdtItem { Content: BcrdtContentType nested })
        {
            return nested.Type;
        }

        return null;
    }

    /// <summary>Encodes the position to a compact byte array, for putting in an awareness update.</summary>
    public byte[] Encode()
    {
        var writer = new BcrdtBufferWriter(24);
        try
        {
            if (Item is { } item)
            {
                writer.WriteByte(0);
                item.Write(ref writer);
            }
            else if (RootName is not null)
            {
                writer.WriteByte(1);
                writer.WriteString(RootName);
            }
            else if (TypeItem is { } typeItem)
            {
                writer.WriteByte(2);
                typeItem.Write(ref writer);
            }
            else
            {
                throw new InvalidOperationException("Relative position has no anchor.");
            }

            writer.WriteVarInt(Association);
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>Decodes a position produced by <see cref="Encode"/>.</summary>
    public static BcrdtRelativePosition Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new BcrdtBufferReader(data);
        byte tag = reader.ReadByte();

        BcrdtRelativePosition position = tag switch
        {
            0 => new BcrdtRelativePosition(BcrdtItemId.Read(ref reader), null, null, 0),
            1 => new BcrdtRelativePosition(null, null, reader.ReadString(), 0),
            2 => new BcrdtRelativePosition(null, BcrdtItemId.Read(ref reader), null, 0),
            _ => throw BcrdtFormatException.UnknownTag("relative position", tag),
        };

        int association = (int)reader.ReadVarInt();
        return new BcrdtRelativePosition(position.Item, position.TypeItem, position.RootName, association);
    }

    /// <inheritdoc/>
    public override string ToString() =>
        Item is { } item ? $"@{item}({Association})" : $"@{RootName ?? TypeItem?.ToString()}({Association})";
}
