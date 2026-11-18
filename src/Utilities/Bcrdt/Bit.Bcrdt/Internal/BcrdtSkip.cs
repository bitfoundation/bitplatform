namespace Bit.Bcrdt;

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
        if (!CanMergeWith(right)) return false;
        Length += right.Length;
        return true;
    }

    /// <inheritdoc/>
    internal override bool CanMergeWith(BcrdtAbstractStruct right) =>
        right is BcrdtSkip && FitsOneBlock(Length, right.Length);

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
