namespace Bit.Bcrdt;

/// <summary>
/// A clock range whose content has been reclaimed entirely.
/// </summary>
/// <remarks>
/// Produced when garbage collection reclaims a deleted block whose parent was also deleted. Unlike
/// <see cref="BcrdtContentDeleted"/>, which keeps a block shell so its links stay walkable, this
/// discards the block altogether and keeps only the clock range, so nothing can be inserted
/// relative to it. That is safe precisely because everything around it is gone too.
/// </remarks>
public sealed class BcrdtGarbageCollected(BcrdtItemId id, int length) : BcrdtAbstractStruct(id, length)
{
    /// <inheritdoc/>
    public override bool Deleted => true;

    /// <inheritdoc/>
    public override bool TryMergeWith(BcrdtAbstractStruct right)
    {
        if (!CanMergeWith(right)) return false;
        Length += right.Length;
        return true;
    }

    /// <inheritdoc/>
    internal override bool CanMergeWith(BcrdtAbstractStruct right) =>
        right is BcrdtGarbageCollected && FitsOneBlock(Length, right.Length);

    /// <inheritdoc/>
    public override void Write(ref BcrdtBufferWriter writer, int offset)
    {
        writer.WriteByte((byte)BcrdtContentRef.GarbageCollected);
        writer.WriteVarUInt((ulong)(Length - offset));
    }

    /// <inheritdoc/>
    public override void Integrate(BcrdtTransaction transaction, int offset)
    {
        if (offset > 0)
        {
            Id = new BcrdtItemId(Id.Client, Id.Clock + offset);
            Length -= offset;
        }
        transaction.Doc.Store.AddStruct(this);
    }
}
