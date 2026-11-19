namespace Bit.Bcrdt;

/// <summary>A contiguous run of deleted clocks belonging to one client.</summary>
/// <param name="Clock">The first deleted clock.</param>
/// <param name="Length">How many consecutive clocks are deleted.</param>
public readonly record struct BcrdtDeleteRange(long Clock, long Length)
{
    /// <summary>The clock just past the end of the range.</summary>
    public long End => Clock + Length;
}
