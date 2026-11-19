namespace Bit.Bcrdt;

/// <summary>A cached "index N is at this block" association.</summary>
internal sealed class BcrdtSearchMarker(BcrdtItem item, int index)
{
    public BcrdtItem Item = item;
    public int Index = index;
    public long Timestamp = BcrdtSearchMarkerCache.NextTimestamp();
}
