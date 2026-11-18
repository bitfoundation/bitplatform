using System.Buffers;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Bit.Bmage;

/// <summary>Composites layers.</summary>
public static class BmageLayerCompositor
{
    /// <summary>Blends <paramref name="layers"/>, bottom first, onto <paramref name="target"/> in place.</summary>
    public static void Composite(BmageImageBuffer target, IEnumerable<BmageLayer> layers, BmageOperationContext? context = null)
        => Composite(target, layers, context, cache: null);

    /// <summary>
    /// Blends the layers onto the target, keeping the images image layers are drawn with in <paramref name="cache"/>
    /// for the next composite of the same layers, and releasing those this one did not use.
    /// </summary>
    internal static void Composite(BmageImageBuffer target, IEnumerable<BmageLayer> layers, BmageOperationContext? context, BmageLayerImageCache? cache)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(layers);
        context ??= BmageOperationContext.Default;
        cache?.Begin();
        CompositeList(target, layers as IReadOnlyList<BmageLayer> ?? [.. layers], context, cache);
        cache?.End();
    }

    /// <summary>
    /// Blends a list of layers, bottom first, the layers clipped to one drawn with it; groups composite their own lists
    /// the same way. Reports progress by the share of the list done.
    /// </summary>
    internal static void CompositeList(BmageImageBuffer target, IReadOnlyList<BmageLayer> list, BmageOperationContext context, BmageLayerImageCache? cache)
    {
        for (var i = 0; i < list.Count;)
        {
            context.ThrowIfCancellationRequested();
            var end = i + 1;
            while (end < list.Count && list[end].ClipToLayerBelow) end++;
            // Each layer reports its progress within its own share of the whole.
            var progress = context.Progress is null ? null : new BmageScaledProgress(context.Progress, i / (double)list.Count, (end - i) / (double)list.Count);
            var part = context.WithProgress(progress);
            if (end == i + 1) CompositeOne(list[i], target, part, cache);
            else CompositeClipped(list[i], list, i + 1, end, target, part, cache);
            // A layer blended on one thread reports nothing along the way, so its end is reported here.
            context.ReportProgress(end / (double)list.Count);
            i = end;
        }
        if (list.Count == 0) context.ReportProgress(1);
    }

    private static void CompositeOne(BmageLayer layer, BmageImageBuffer target, BmageOperationContext context, BmageLayerImageCache? cache)
    {
        switch (layer)
        {
            case BmageImageLayer image when cache is not null:
                image.Composite(target, context, cache);
                break;
            case BmageGroupLayer group:
                group.Composite(target, context, cache);
                break;
            default:
                layer.Composite(target, context);
                break;
        }
    }

    /// <summary>
    /// A base layer and the layers clipped to it, <paramref name="list"/>[<paramref name="from"/>..<paramref name="to"/>):
    /// drawn together over transparency, the base at full strength, then blended with the base's mode and opacity.
    /// </summary>
    private static void CompositeClipped(BmageLayer bottom, IReadOnlyList<BmageLayer> list, int from, int to, BmageImageBuffer target, BmageOperationContext context, BmageLayerImageCache? cache)
    {
        // A hidden base hides what is clipped to it.
        if (bottom.IsNoOp) return;
        var inner = context.WithProgress(null);
        using var stack = BmageImageBuffer.Create(target.Width, target.Height, context.Allocator, clear: true, BmageImageLimits.Unrestricted);
        CompositeOne(bottom with { BlendMode = BmageBlendMode.SourceOver, Opacity = 1 }, stack, inner, cache);

        // The base's transparency, which the layers clipped to it are limited to, and the area it covers: outside it
        // the base is transparent, so nothing clipped to it can show there and nothing outside it is blended.
        int width = stack.Width, pixels = width * stack.Height;
        var bytes = MemoryMarshal.AsBytes(stack.Pixels);
        var alpha = ArrayPool<byte>.Shared.Rent(pixels);
        try
        {
            int minX = width, maxX = -1, minY = -1, maxY = -1;
            for (var y = 0; y < stack.Height; y++)
            {
                int start = y * width, first = -1, last = -1;
                for (var x = 0; x < width; x++)
                {
                    var a = alpha[start + x] = bytes[(start + x) * 4 + 3];
                    if (a == 0) continue;
                    if (first < 0) first = x;
                    last = x;
                }
                if (first < 0) continue;
                if (minY < 0) minY = y;
                maxY = y;
                minX = Math.Min(minX, first);
                maxX = Math.Max(maxX, last);
            }
            if (minY < 0) return;
            var area = Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);

            var opaque = false;
            var drawnOutside = false;
            for (var i = from; i < to; i++)
            {
                var layer = list[i];
                if (layer.IsNoOp) continue;
                // A layer of pixels blends onto the base as if it were opaque, which is what limiting the result to the
                // base's transparency afterwards needs; an edit sees the base's real transparency, so a blur in it does
                // not pull in the colour of what is transparent, nor what a layer drew outside the base.
                var wantOpaque = layer is not BmageAdjustmentLayer;
                if (wantOpaque && !opaque) SetAlpha(bytes, alpha, width, area, opaque: true);
                else if (opaque || drawnOutside) SetAlpha(bytes, alpha, width, drawnOutside ? stack.Bounds : area, opaque: false);
                opaque = wantOpaque;
                drawnOutside = wantOpaque;
                CompositeOne(layer, stack, inner, cache);
            }
            SetAlpha(bytes, alpha, width, area, opaque: false);
            new StackLayer(stack, area) { BlendMode = bottom.BlendMode, Opacity = bottom.Opacity }.Composite(target, context);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(alpha);
        }
    }

    /// <summary>Sets the alpha of every pixel in <paramref name="area"/> to opaque, or back to the base's.</summary>
    private static void SetAlpha(Span<byte> bytes, byte[] alpha, int width, Rectangle area, bool opaque)
    {
        for (var y = area.Top; y < area.Bottom; y++)
        {
            var start = y * width + area.Left;
            var end = start + area.Width;
            if (opaque) for (var p = start; p < end; p++) bytes[p * 4 + 3] = 255;
            else for (var p = start; p < end; p++) bytes[p * 4 + 3] = alpha[p];
        }
    }

    /// <summary>Layers already composited together, blended onto the target as one over <paramref name="Area"/>.</summary>
    private sealed record StackLayer(BmageImageBuffer Pixels, Rectangle Area) : BmageLayer
    {
        public override string Kind => "Stack";

        protected internal override void CompositeCore(BmageImageBuffer target, BmageOperationContext context)
            => BlendRows(target, Area, context, y => Pixels.GetRow(y)[Area.Left..]);

        protected override BmageLayer ScaleCore(double scale) => this;
    }

    /// <summary>
    /// A new image of <paramref name="size"/> filled with <paramref name="background"/> (transparent by default) with
    /// <paramref name="layers"/> composited onto it, bottom first. The caller owns the result.
    /// </summary>
    /// <example>
    /// <code>
    /// using var card = BmageLayerCompositor.Flatten(new Size(1200, 630), BmageRgba32.White,
    /// [
    ///     new BmageImageLayer { Image = photo, Bounds = new RectangleF(0, 0, 1200, 630) },
    ///     new BmageFillLayer(new BmageRgba32(10, 20, 60)) { BlendMode = BmageBlendMode.Multiply, Mask = BmageMask.Gradient(new(0, 1), new(0, 0.3f)) },
    ///     new BmageImageLayer { Image = logo, Bounds = new RectangleF(40, 40, 160, 160), Opacity = 0.9f },
    /// ]);
    /// </code>
    /// </example>
    public static BmageImageBuffer Flatten(Size size, BmageRgba32 background, IEnumerable<BmageLayer> layers, BmageOperationContext? context = null)
    {
        context ??= BmageOperationContext.Default;
        var image = BmageImageBuffer.Create(size.Width, size.Height, context.Allocator, clear: true, context.Limits);
        try
        {
            if (background.A != 0) image.Fill(background);
            Composite(image, layers, context);
            return image;
        }
        catch
        {
            image.Dispose();
            throw;
        }
    }
}
