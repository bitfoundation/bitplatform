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

    /// <summary>Whether <see cref="TryMergeWith"/> would absorb <paramref name="right"/>. Changes nothing.</summary>
    internal virtual bool CanMergeWith(BcrdtAbstractStruct right) => false;

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
