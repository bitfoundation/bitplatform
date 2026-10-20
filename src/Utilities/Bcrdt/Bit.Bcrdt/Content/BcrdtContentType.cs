namespace Bit.Bcrdt;

/// <summary>
/// A nested collaborative type stored inside another one.
/// </summary>
/// <remarks>
/// Nesting is what turns four primitives into an arbitrary document model: a map of arrays of maps,
/// a list of rich-text fields, a whiteboard whose shapes are maps. The nested type is an ordinary
/// block in its parent, so it participates in ordering, deletion, undo and synchronization exactly
/// like a scalar would; its own contents are separate blocks that name it as their parent.
/// </remarks>
/// <param name="type">The nested type instance.</param>
public sealed class BcrdtContentType(BcrdtAbstractType type) : IBcrdtContent
{
    /// <summary>The nested type.</summary>
    public BcrdtAbstractType Type { get; } = type;

    /// <inheritdoc/>
    public int Length => 1;

    /// <inheritdoc/>
    public bool Countable => true;

    /// <inheritdoc/>
    public BcrdtContentRef Ref => BcrdtContentRef.Type;

    /// <inheritdoc/>
    public IReadOnlyList<object?> GetContent() => [Type];

    /// <summary>
    /// Produces an <i>empty</i> type of the same kind.
    /// </summary>
    /// <remarks>
    /// Copying is only used when undo re-inserts deleted content. The nested contents are separate
    /// blocks with their own undo records, so copying them here would restore them twice.
    /// </remarks>
    public IBcrdtContent Copy() => new BcrdtContentType(Type.CreateInstance());

    /// <inheritdoc/>
    public IBcrdtContent Splice(int offset) => throw new InvalidOperationException("A nested type is indivisible.");

    /// <inheritdoc/>
    public bool TryMergeWith(IBcrdtContent right) => false;

    /// <inheritdoc/>
    public void Integrate(BcrdtTransaction transaction, BcrdtItem item) => Type.Integrate(transaction.Doc, item);

    /// <inheritdoc/>
    public void Delete(BcrdtTransaction transaction)
    {
        // Deleting a container deletes everything in it. Blocks that were already deleted before
        // this transaction become merge candidates, because with the container gone there is no
        // longer anything that could need to address them individually.
        for (BcrdtItem? item = Type.Start; item is not null; item = item.Right)
        {
            if (!item.Deleted) item.Delete(transaction);
            else if (item.Id.Clock < transaction.BeforeState.Get(item.Id.Client)) transaction.MergeStructs.Add(item);
        }

        foreach (BcrdtItem head in Type.KeyIndex.Values)
        {
            for (BcrdtItem? item = head; item is not null; item = item.Left)
            {
                if (!item.Deleted) item.Delete(transaction);
                else if (item.Id.Clock < transaction.BeforeState.Get(item.Id.Client)) transaction.MergeStructs.Add(item);
            }
        }

        // No point raising a change event for a type that no longer exists.
        transaction.Changed.Remove(Type);
    }

    /// <inheritdoc/>
    public void Collect(BcrdtStructStore store)
    {
        for (BcrdtItem? item = Type.Start; item is not null; item = item.Right)
        {
            item.Collect(store, parentCollected: true);
        }
        Type.Start = null;

        foreach (BcrdtItem head in Type.KeyIndex.Values)
        {
            for (BcrdtItem? item = head; item is not null; item = item.Left)
            {
                item.Collect(store, parentCollected: true);
            }
        }
        Type.KeyIndex.Clear();
    }

    /// <inheritdoc/>
    public void Write(ref BcrdtBufferWriter writer, int offset) => writer.WriteVarUInt((ulong)Type.Kind);

    /// <summary>Reads a payload written by <see cref="Write"/>.</summary>
    public static BcrdtContentType Read(ref BcrdtBufferReader reader)
    {
        ulong kind = reader.ReadVarUInt();

        // Only roots can be untyped: a nested type's kind always travels with it. And a value wider
        // than the enum must not be truncated into a valid kind.
        if (kind >= (ulong)BcrdtTypeKind.Untyped)
        {
            throw BcrdtFormatException.UnknownTag("collaborative type", (int)Math.Min(kind, int.MaxValue));
        }
        return new BcrdtContentType(BcrdtTypeFactory.Create((BcrdtTypeKind)kind));
    }
}

/// <summary>Creates collaborative type instances from their wire discriminator.</summary>
public static class BcrdtTypeFactory
{
    /// <summary>Creates an empty instance of the given kind.</summary>
    /// <exception cref="BcrdtFormatException">The kind is not recognised.</exception>
    public static BcrdtAbstractType Create(BcrdtTypeKind kind) => kind switch
    {
        BcrdtTypeKind.Map => new BcrdtMap(),
        BcrdtTypeKind.Array => new BcrdtArray(),
        BcrdtTypeKind.Text => new BcrdtText(),
        BcrdtTypeKind.Set => new BcrdtSet(),
        BcrdtTypeKind.Untyped => new BcrdtUntypedType(),
        _ => throw BcrdtFormatException.UnknownTag("collaborative type", (int)kind),
    };
}
