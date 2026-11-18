namespace Bit.Bcrdt;

/// <summary>
/// A range whose payload has been reclaimed but whose identity must be preserved.
/// </summary>
/// <remarks>
/// When garbage collection reclaims deleted content it cannot simply remove the block: concurrent
/// operations still in flight may reference its identifier as an origin. This payload keeps the
/// clock range alive at a cost of a few bytes while releasing the actual data.
/// </remarks>
/// <param name="length">The number of logical units the reclaimed range covered.</param>
public sealed class BcrdtContentDeleted(int length) : IBcrdtContent
{
    /// <inheritdoc/>
    public int Length { get; private set; } = length;

    /// <inheritdoc/>
    public bool Countable => false;

    /// <inheritdoc/>
    public BcrdtContentRef Ref => BcrdtContentRef.Deleted;

    /// <inheritdoc/>
    public IReadOnlyList<object?> GetContent() => [];

    /// <inheritdoc/>
    public IBcrdtContent Copy() => new BcrdtContentDeleted(Length);

    /// <inheritdoc/>
    public IBcrdtContent Splice(int offset)
    {
        var right = new BcrdtContentDeleted(Length - offset);
        Length = offset;
        return right;
    }

    /// <inheritdoc/>
    public bool CanMergeWith(IBcrdtContent right) => right is BcrdtContentDeleted;

    /// <inheritdoc/>
    public bool TryMergeWith(IBcrdtContent right)
    {
        if (right is not BcrdtContentDeleted other) return false;
        Length += other.Length;
        return true;
    }

    /// <inheritdoc/>
    public void Integrate(BcrdtTransaction transaction, BcrdtItem item)
    {
        transaction.DeleteSet.Add(item.Id.Client, item.Id.Clock, Length);
        item.MarkDeleted();
    }

    /// <inheritdoc/>
    public void Delete(BcrdtTransaction transaction) { }

    /// <inheritdoc/>
    public void Collect(BcrdtStructStore store) { }

    /// <inheritdoc/>
    public void Write(ref BcrdtBufferWriter writer, int offset) => writer.WriteVarUInt((ulong)(Length - offset));

    /// <summary>Reads a payload written by <see cref="Write"/>.</summary>
    public static BcrdtContentDeleted Read(ref BcrdtBufferReader reader)
    {
        int length = reader.ReadVarUInt32();
        if (length <= 0) throw new BcrdtFormatException("Deleted range must have positive length.");
        return new BcrdtContentDeleted(length);
    }
}
