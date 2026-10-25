namespace Bit.Bcrdt;

/// <summary>
/// Anything that occupies a range of one client's clock space.
/// </summary>
/// <remarks>
/// A struct covers the half-open clock range <c>[Id.Clock, Id.Clock + Length)</c>. Within a client,
/// structs are stored contiguously and in order, so struct <c>n+1</c> begins exactly where struct
/// <c>n</c> ends. That invariant turns "do I have this content?" into one integer comparison and
/// turns "where is this identifier?" into a binary search, and it is relied on everywhere.
/// </remarks>
public abstract class BcrdtAbstractStruct
{
    /// <summary>The address of the first logical unit this struct covers.</summary>
    public BcrdtItemId Id;

    /// <summary>How many logical units this struct covers.</summary>
    public int Length;

    /// <summary>Creates a struct covering a clock range.</summary>
    protected BcrdtAbstractStruct(BcrdtItemId id, int length)
    {
        Id = id;
        Length = length;
    }

    /// <summary>The address of the last logical unit this struct covers.</summary>
    public BcrdtItemId LastId => new(Id.Client, Id.Clock + Length - 1);

    /// <summary>Whether this struct's content has been deleted.</summary>
    public abstract bool Deleted { get; }

    /// <summary>Absorbs the struct immediately to the right if the two can be represented as one.</summary>
    public abstract bool TryMergeWith(BcrdtAbstractStruct right);

    /// <summary>
    /// Whether two adjacent ranges fit in one struct, whose length is an <see cref="int"/>.
    /// </summary>
    /// <remarks>
    /// Clocks are 64-bit, lengths are not, and an update can legitimately describe two neighbouring
    /// ranges each close to the limit. Merging them anyway would wrap the length negative, after which
    /// the replica's state vector, and everything it encodes, is garbage to every peer - from a
    /// well-formed message. Two blocks that do not fit simply stay two blocks.
    /// </remarks>
    internal static bool FitsOneBlock(int left, int right) => (long)left + right <= int.MaxValue;

    /// <summary>Writes this struct, skipping its first <paramref name="offset"/> logical units.</summary>
    public abstract void Write(ref BcrdtBufferWriter writer, int offset);

    /// <summary>Places this struct into the document, skipping units the document already has.</summary>
    public abstract void Integrate(BcrdtTransaction transaction, int offset);
}

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
        if (right is not BcrdtGarbageCollected || !FitsOneBlock(Length, right.Length)) return false;
        Length += right.Length;
        return true;
    }

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

/// <summary>
/// A range the sender deliberately omitted from an update.
/// </summary>
/// <remarks>
/// Skips appear only on the wire, never in a document. They exist so that a peer can send a
/// non-contiguous slice of its history - for instance when a client's early operations were
/// compacted away - without the receiver mistaking the gap for missing causal dependencies it
/// should wait for. On receipt a skip is folded into the pending-structs bookkeeping and discarded.
/// </remarks>
public sealed class BcrdtSkip(BcrdtItemId id, int length) : BcrdtAbstractStruct(id, length)
{
    /// <inheritdoc/>
    public override bool Deleted => true;

    /// <inheritdoc/>
    public override bool TryMergeWith(BcrdtAbstractStruct right)
    {
        if (right is not BcrdtSkip || !FitsOneBlock(Length, right.Length)) return false;
        Length += right.Length;
        return true;
    }

    /// <inheritdoc/>
    public override void Write(ref BcrdtBufferWriter writer, int offset)
    {
        writer.WriteByte((byte)BcrdtContentRef.Skip);
        writer.WriteVarUInt((ulong)(Length - offset));
    }

    /// <inheritdoc/>
    public override void Integrate(BcrdtTransaction transaction, int offset) =>
        throw new InvalidOperationException("A skip marker must never be integrated into a document.");
}
