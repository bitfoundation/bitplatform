using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>Replaces blocks of pixels with their average colour (mosaic). Commonly used for redaction.</summary>
public sealed class BmagePixelateFilter : BmageRegionOperation
{
    public BmagePixelateFilter(int blockSize, Rectangle? region = null) : base(region)
    {
        if (blockSize < 1) throw new ArgumentOutOfRangeException(nameof(blockSize));
        BlockSize = blockSize;
    }

    public int BlockSize { get; }
    public override string Name => "Pixelate";
    public override bool IsIdentity => BlockSize == 1;

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        var target = GetMutableCopy(source, context);
        var region = GetRegion(source.Size);
        if (region.IsEmptyArea() || IsIdentity) return target;
        var bs = BlockSize;
        for (var by = region.Top; by < region.Bottom; by += bs)
        {
            context.ThrowIfCancellationRequested();
            var bh = Math.Min(bs, region.Bottom - by);
            for (var bx = region.Left; bx < region.Right; bx += bs)
            {
                var bw = Math.Min(bs, region.Right - bx);
                long r = 0, g = 0, b = 0, a = 0;
                for (var y = 0; y < bh; y++)
                {
                    var row = target.GetRow(by + y).Slice(bx, bw);
                    foreach (var p in row) { r += p.R * p.A; g += p.G * p.A; b += p.B * p.A; a += p.A; }
                }
                var n = bw * bh;
                BmageRgba32 avg = a == 0 ? BmageRgba32.Transparent : new BmageRgba32((byte)(r / a), (byte)(g / a), (byte)(b / a), (byte)(a / n));
                for (var y = 0; y < bh; y++) target.GetRow(by + y).Slice(bx, bw).Fill(avg);
            }
        }
        return target;
    }

    public override IBmageImageOperation ForScale(double scale) => scale == 1 ? this : new BmagePixelateFilter(Math.Max(1, (int)Math.Round(BlockSize * scale)), ScaleRegion(Region, scale));
}
