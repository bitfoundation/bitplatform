using System.Drawing;

namespace Bit.Bmage;

/// <summary>
/// A mask taken from an image, stretched over the whole image it is applied to: the output of a segmentation or
/// background removal model, a matte painted elsewhere, or a mask the editor captured. Its resolution need not match.
/// </summary>
/// <param name="Image">The mask's pixels. Not owned by the mask: the caller keeps it alive for as long as the mask is used.</param>
public sealed record BmageBitmapMask(BmageImageBuffer Image) : BmageMask
{
    public BmageImageBuffer Image { get; init; } = Image ?? throw new ArgumentNullException(nameof(Image));

    /// <summary>Which channel holds the coverage.</summary>
    public BmageMaskChannel Channel { get; init; }

    public override string Kind => "Bitmap";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(BmageMaskBuffer target, BmageMaskRenderContext context)
    {
        if (Image.IsDisposed) throw new ObjectDisposedException(nameof(Image), "The image this mask reads has been disposed.");
        using var values = Channel == BmageMaskChannel.Luminance
            ? BmageMaskBuffer.FromLuminance(Image, context.Operation.Allocator)
            : BmageMaskBuffer.FromAlpha(Image, context.Operation.Allocator);
        if (values.Size == context.Size)
        {
            CopyWindow(values, target, context.Window);
            return;
        }
        using var resized = values.Resize(context.Size.Width, context.Size.Height, context.Operation.Allocator);
        CopyWindow(resized, target, context.Window);
    }

    private static void CopyWindow(BmageMaskBuffer whole, BmageMaskBuffer target, Rectangle window)
    {
        for (var y = 0; y < window.Height; y++) whole.GetRow(window.Top + y).Slice(window.Left, window.Width).CopyTo(target.GetRow(y));
    }
}
