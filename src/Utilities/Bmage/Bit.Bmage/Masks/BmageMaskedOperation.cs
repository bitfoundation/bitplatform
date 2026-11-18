using System.Buffers;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bit.Bmage;

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
public sealed class BmageMaskedOperation : BmageImageOperation
{
    public BmageMaskedOperation(IBmageImageOperation operation, BmageMask mask)
    {
        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        Mask = mask ?? throw new ArgumentNullException(nameof(mask));
    }

    /// <summary>The operation applied through the mask.</summary>
    public IBmageImageOperation Operation { get; }

    /// <summary>Where, and how strongly, the operation applies.</summary>
    public BmageMask Mask { get; }

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

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        GetOutputSize(source.Size);
        var affected = Mask.GetAffectedBounds(source.Size);
        var operation = IsIdentity || affected.IsEmptyArea() ? null : Simplify(Operation, source.Size);
        if (operation is null) return GetMutableCopy(source, context);

        // Only the part of the mask that can affect anything is drawn: its rows are the affected area's.
        using var mask = Mask.RenderWindow(source.Size, affected, source, context.WithProgress(null));
        if (operation is BmagePointOperation point) return ApplyPerPixel(source, mask, point, affected, context);

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
    private static IBmageImageOperation? Simplify(IBmageImageOperation operation, Size size)
    {
        if (operation is not BmagePipelineOperation nested) return operation.IsIdentity ? null : operation;
        var steps = nested.Pipeline.Optimize(size);
        return steps.Count switch
        {
            0 => null,
            1 => steps[0],
            _ => new BmagePipelineOperation(steps, nested.Name),
        };
    }

    /// <summary>
    /// One pass over the rows the mask reaches: each row is mapped exactly as the operation alone maps it, and mixed with
    /// the original by the mask, which holds just the <paramref name="affected"/> area. Rows and columns outside its
    /// reach are copied, or left alone in place.
    /// </summary>
    private static BmageImageBuffer ApplyPerPixel(BmageImageBuffer source, BmageMaskBuffer mask, BmagePointOperation operation, Rectangle affected, BmageOperationContext context)
    {
        var output = GetOutputBuffer(source, context, source.Width, source.Height);
        try
        {
            using var processor = new BmagePointOperationExecutor.RowProcessor(operation);
            var inPlace = ReferenceEquals(output, source);
            if (!inPlace) CopyRowsOutside(source, output, affected);
            BmageParallelRows.For(processor.Context(context), affected.Top, affected.Bottom, affected.Width, (start, end, band) =>
            {
                var scratch = processor.UsesTablesOnly ? null : ArrayPool<Vector4>.Shared.Rent(affected.Width);
                var mapped = ArrayPool<BmageRgba32>.Shared.Rent(affected.Width);
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
                    ArrayPool<BmageRgba32>.Shared.Return(mapped);
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
    private static void BlendBack(BmageImageBuffer source, BmageImageBuffer processed, BmageMaskBuffer mask, Rectangle affected, BmageOperationContext context)
    {
        CopyRowsOutside(source, processed, affected);
        BmageParallelRows.For(context, affected.Top, affected.Bottom, affected.Width, (start, end, band) =>
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

    private static void CopyRowsOutside(BmageImageBuffer source, BmageImageBuffer destination, Rectangle affected)
    {
        for (var y = 0; y < affected.Top; y++) source.GetRow(y).CopyTo(destination.GetRow(y));
        for (var y = affected.Bottom; y < source.Height; y++) source.GetRow(y).CopyTo(destination.GetRow(y));
    }

    /// <summary>
    /// Mixes each original pixel towards its edited value by the coverage. <paramref name="target"/> may be either input.
    /// </summary>
    internal static void Mix(ReadOnlySpan<BmageRgba32> original, ReadOnlySpan<BmageRgba32> edited, ReadOnlySpan<byte> coverage, Span<BmageRgba32> target)
    {
        for (var x = 0; x < coverage.Length; x++) target[x] = Mix(original[x], edited[x], coverage[x]);
    }

    /// <summary>
    /// <paramref name="original"/> moved towards <paramref name="edited"/> by <paramref name="coverage"/> / 255, in
    /// premultiplied alpha whenever the two alphas differ, so a pixel the edit made transparent does not drag its old
    /// colour into the mix.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static BmageRgba32 Mix(BmageRgba32 original, BmageRgba32 edited, int coverage)
    {
        if (coverage == 0) return original;
        if (coverage == 255) return edited;
        var keep = 255 - coverage;
        if (original.A == edited.A)
        {
            return new BmageRgba32(
                (byte)((original.R * keep + edited.R * coverage + 127) / 255),
                (byte)((original.G * keep + edited.G * coverage + 127) / 255),
                (byte)((original.B * keep + edited.B * coverage + 127) / 255),
                original.A);
        }
        var wo = original.A * keep;
        var we = edited.A * coverage;
        var total = wo + we;
        if (total == 0) return BmageRgba32.Transparent;
        var half = total / 2;
        return new BmageRgba32(
            (byte)((original.R * wo + edited.R * we + half) / total),
            (byte)((original.G * wo + edited.G * we + half) / total),
            (byte)((original.B * wo + edited.B * we + half) / total),
            (byte)((total + 127) / 255));
    }

    public override IBmageImageOperation ForScale(double scale) => scale == 1 ? this : new BmageMaskedOperation(Operation.ForScale(scale), Mask.ForScale(scale));

    public override string ToString() => $"Masked({Mask.Kind}: {Operation})";
}
