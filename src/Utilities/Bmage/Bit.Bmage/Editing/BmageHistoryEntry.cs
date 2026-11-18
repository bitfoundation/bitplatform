namespace Bit.Bmage;

/// <summary>One entry in the edit history.</summary>
public sealed record BmageHistoryEntry(BmageImageDocument Document, string Label, DateTimeOffset Timestamp)
{
    /// <summary>
    /// Rough memory cost of this entry: documents are descriptions, not pixels, so this is small. The pixels an image
    /// layer or a bitmap mask refers to are shared between entries and are not counted; painted strokes are.
    /// </summary>
    public int EstimatedSize
    {
        get
        {
            var size = 128 + Document.Pipeline.Count * 64L + Document.Annotations.Count * 256L + SizeOf(Document.Layers);
            return (int)Math.Min(int.MaxValue, size);
        }
    }

    private static long SizeOf(IReadOnlyList<BmageLayer> layers)
    {
        long size = 0;
        foreach (var layer in layers)
        {
            size += 256;
            if (layer is BmagePaintLayer paint) size += paint.Strokes.Sum(s => 96L + s.Points.Count * 12L);
            else if (layer is BmageGroupLayer group) size += SizeOf(group.Layers);
        }
        return size;
    }
}
