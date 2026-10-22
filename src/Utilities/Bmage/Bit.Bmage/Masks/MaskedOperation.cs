using System.Buffers;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Masks;

/// <summary>
/// Applies an operation only where a mask covers the image, fading with the mask's coverage: a selective adjustment.
/// Where the mask is 0 the pixels are untouched, where it is 255 they are exactly what the operation alone produces.
/// </summary>
/// <remarks>
/// <para>
/// The operation must keep the image's size; anything that does not (a crop, a resize) has no pixel-for-pixel
/// relationship with the original to blend by, and is refused.
/// </para>
/// <para>
/// A per-pixel operation, or a pipeline of them, runs in a single pass and only across the columns and rows the mask
/// can reach, so darkening a sky through a gradient never touches the ground. Anything else, such as a blur, needs its
/// neighbours: it runs over the whole image and the result is blended back by the mask.
/// </para>
/// <para>
/// The mask is rendered for the image the operation receives, so a mask selecting by content (a luminance or colour
/// range) selects from the pixels before the edit.
/// </para>
/// </remarks>
public sealed class MaskedOperation : ImageOperation
{
    public MaskedOperation(IImageOperation operation, Mask mask)
    {
        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        Mask = mask ?? throw new ArgumentNullException(nameof(mask));
    }

    /// <summary>The operation applied through the mask.</summary>
    public IImageOperation Operation { get; }

    /// <summary>Where, and how strongly, the operation applies.</summary>
    public Mask Mask { get; }

    public override string Name => Operation.Name + " (masked)";

    public override bool IsIdentity => Operation.IsIdentity || Mask.IsEmpty;

    /// <inheritdoc />
    /// <exception cref="ArgumentException">The operation changes the image size.</exception>
    public override Size GetOutputSize(Size inputSize)
    {
        var output = Operation.GetOutputSize(inputSize);
        if (output != inputSize)
            throw new ArgumentException($"'{Operation.Name}' turns a {inputSize.Width}x{inputSize.Height} image into {output.Width}x{output.Height}, and only operations that keep the size can be applied through a mask.");
        return inputSize;
    }

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        GetOutputSize(source.Size);
        var affected = Mask.GetAffectedBounds(source.Size);
        var operation = IsIdentity || affected.IsEmptyArea() ? null : Simplify(Operation, source.Size);
        if (operation is null) return GetMutableCopy(source, context);

        // Only the part of the mask that can affect anything is drawn: its rows are the affected area's.
        using var mask = Mask.RenderWindow(source.Size, affected, source, context.WithProgress(null));
        if (operation is PointOperation point) return ApplyPerPixel(source, mask, point, affected, context);

        var processed = operation.Apply(source, context.WithMutation(false));
        try
        {
            if (processed.Size != source.Size)
                throw new InvalidOperationException($"'{operation.Name}' reported keeping the size but produced a {processed.Width}x{processed.Height} image.");
            // The operation has reported the progress; blending back is the last moment of it.
            BlendBack(source, processed, mask, affected, context.WithProgress(null));
            return processed;
        }
        catch
        {
            processed.Dispose();
            throw;
        }
    }

    /// <summary>
    /// What actually runs: a nested pipeline is optimised for the size it receives, so a run of adjustments becomes one
    /// per-pixel operation. Null when nothing is left to do.
    /// </summary>
    private static IImageOperation? Simplify(IImageOperation operation, Size size)
    {
        if (operation is not PipelineOperation nested) return operation.IsIdentity ? null : operation;
        var steps = nested.Pipeline.Optimize(size);
        return steps.Count switch
        {
            0 => null,
            1 => steps[0],
            _ => new PipelineOperation(steps, nested.Name),
        };
    }

    /// <summary>
    /// One pass over the rows the mask reaches: each row is mapped exactly as the operation alone maps it, and mixed with
    /// the original by the mask, which holds just the <paramref name="affected"/> area. Rows and columns outside its
    /// reach are copied, or left alone in place.
    /// </summary>
    private static ImageBuffer ApplyPerPixel(ImageBuffer source, MaskBuffer mask, PointOperation operation, Rectangle affected, OperationContext context)
    {
        var output = GetOutputBuffer(source, context, source.Width, source.Height);
        try
        {
            using var processor = new PointOperationExecutor.RowProcessor(operation);
            var inPlace = ReferenceEquals(output, source);
            if (!inPlace) CopyRowsOutside(source, output, affected);
            ParallelRows.For(processor.Context(context), affected.Top, affected.Bottom, affected.Width, (start, end, band) =>
            {
                var scratch = processor.UsesTablesOnly ? null : ArrayPool<Vector4>.Shared.Rent(affected.Width);
                var mapped = ArrayPool<Rgba32>.Shared.Rent(affected.Width);
                try
                {
                    for (var y = start; y < end; y++)
                    {
                        if (((y - start) & 15) == 0)
                        {
                            band.ThrowIfCancellationRequested();
                            band.ReportProgress((y - affected.Top) / (double)affected.Height);
                        }
                        var src = source.GetRow(y);
                        var dst = output.GetRow(y);
                        if (!inPlace)
                        {
                            // The columns outside the mask's reach are the original's.
                            src[..affected.Left].CopyTo(dst);
                            src[affected.Right..].CopyTo(dst[affected.Right..]);
                        }
                        var coverage = mask.GetRow(y - affected.Top);
                        var original = src.Slice(affected.Left, affected.Width);
                        var target = dst.Slice(affected.Left, affected.Width);
                        if (coverage.IndexOfAnyExcept((byte)0) < 0)
                        {
                            if (!inPlace) original.CopyTo(target);
                            continue;
                        }
                        var result = mapped.AsSpan(0, affected.Width);
                        processor.Process(original, result, scratch.AsSpan(0, scratch is null ? 0 : affected.Width));
                        Mix(original, result, coverage, target);
                    }
                }
                finally
                {
                    if (scratch is not null) ArrayPool<Vector4>.Shared.Return(scratch);
                    ArrayPool<Rgba32>.Shared.Return(mapped);
                }
            });
            context.ReportProgress(1);
            return output;
        }
        catch
        {
            if (!ReferenceEquals(output, source)) output.Dispose();
            throw;
        }
    }

    /// <summary>Puts the original back wherever the mask is not fully covering, mixing where it is partly.</summary>
    private static void BlendBack(ImageBuffer source, ImageBuffer processed, MaskBuffer mask, Rectangle affected, OperationContext context)
    {
        CopyRowsOutside(source, processed, affected);
        ParallelRows.For(context, affected.Top, affected.Bottom, affected.Width, (start, end, band) =>
        {
            for (var y = start; y < end; y++)
            {
                if (((y - start) & 31) == 0) band.ThrowIfCancellationRequested();
                var src = source.GetRow(y);
                var dst = processed.GetRow(y);
                src[..affected.Left].CopyTo(dst);
                src[affected.Right..].CopyTo(dst[affected.Right..]);
                var target = dst.Slice(affected.Left, affected.Width);
                Mix(src.Slice(affected.Left, affected.Width), target, mask.GetRow(y - affected.Top), target);
            }
        });
    }

    private static void CopyRowsOutside(ImageBuffer source, ImageBuffer destination, Rectangle affected)
    {
        for (var y = 0; y < affected.Top; y++) source.GetRow(y).CopyTo(destination.GetRow(y));
        for (var y = affected.Bottom; y < source.Height; y++) source.GetRow(y).CopyTo(destination.GetRow(y));
    }

    /// <summary>
    /// Mixes each original pixel towards its edited value by the coverage. <paramref name="target"/> may be either input.
    /// </summary>
    internal static void Mix(ReadOnlySpan<Rgba32> original, ReadOnlySpan<Rgba32> edited, ReadOnlySpan<byte> coverage, Span<Rgba32> target)
    {
        for (var x = 0; x < coverage.Length; x++) target[x] = Mix(original[x], edited[x], coverage[x]);
    }

    /// <summary>
    /// <paramref name="original"/> moved towards <paramref name="edited"/> by <paramref name="coverage"/> / 255, in
    /// premultiplied alpha whenever the two alphas differ, so a pixel the edit made transparent does not drag its old
    /// colour into the mix.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Rgba32 Mix(Rgba32 original, Rgba32 edited, int coverage)
    {
        if (coverage == 0) return original;
        if (coverage == 255) return edited;
        var keep = 255 - coverage;
        if (original.A == edited.A)
        {
            return new Rgba32(
                (byte)((original.R * keep + edited.R * coverage + 127) / 255),
                (byte)((original.G * keep + edited.G * coverage + 127) / 255),
                (byte)((original.B * keep + edited.B * coverage + 127) / 255),
                original.A);
        }
        var wo = original.A * keep;
        var we = edited.A * coverage;
        var total = wo + we;
        if (total == 0) return Rgba32.Transparent;
        var half = total / 2;
        return new Rgba32(
            (byte)((original.R * wo + edited.R * we + half) / total),
            (byte)((original.G * wo + edited.G * we + half) / total),
            (byte)((original.B * wo + edited.B * we + half) / total),
            (byte)((total + 127) / 255));
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new MaskedOperation(Operation.ForScale(scale), Mask.ForScale(scale));

    public override string ToString() => $"Masked({Mask.Kind}: {Operation})";
}

/// <summary>
/// Multiplies the image's alpha by a mask, so everything outside it becomes transparent: a round avatar, a shaped
/// cut-out, or a background removed by a segmentation model's matte through a <see cref="BitmapMask"/>.
/// </summary>
/// <remarks>Exporting to a format without alpha, such as JPEG, fills the cleared area with the export background.</remarks>
public sealed class AlphaMaskOperation : ImageOperation
{
    public AlphaMaskOperation(Mask mask) => Mask = mask ?? throw new ArgumentNullException(nameof(mask));

    /// <summary>What stays visible.</summary>
    public Mask Mask { get; }

    public override string Name => "Mask";

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        var target = GetMutableCopy(source, context);
        try
        {
            using var mask = Mask.Render(source.Size, source, context.WithProgress(null));
            mask.ApplyToAlpha(target);
            context.ReportProgress(1);
            return target;
        }
        catch
        {
            if (!ReferenceEquals(target, source)) target.Dispose();
            throw;
        }
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new AlphaMaskOperation(Mask.ForScale(scale));

    public override string ToString() => $"Mask({Mask.Kind})";
}
