using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// Applies one of the eight lossless orientations (rotations by multiples of 90° and mirrors). Consecutive
/// orientation operations combine into a single one.
/// </summary>
public sealed class OrientationOperation : ImageOperation, IGeometricOperation
{
    public OrientationOperation(BmageOrientation orientation)
    {
        Orientation = orientation == BmageOrientation.Unspecified ? BmageOrientation.Normal : orientation;
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(orientation));
    }

    /// <summary>Rotate 90° clockwise.</summary>
    public static OrientationOperation Rotate90 { get; } = new(BmageOrientation.Rotate90);
    /// <summary>Rotate 180°.</summary>
    public static OrientationOperation Rotate180 { get; } = new(BmageOrientation.Rotate180);
    /// <summary>Rotate 270° clockwise (90° counter-clockwise).</summary>
    public static OrientationOperation Rotate270 { get; } = new(BmageOrientation.Rotate270);
    /// <summary>Mirror horizontally.</summary>
    public static OrientationOperation FlipHorizontal { get; } = new(BmageOrientation.FlipHorizontal);
    /// <summary>Mirror vertically.</summary>
    public static OrientationOperation FlipVertical { get; } = new(BmageOrientation.FlipVertical);

    /// <summary>Creates an operation for a clockwise rotation that is a multiple of 90°.</summary>
    public static OrientationOperation FromRotation(int clockwiseDegrees) => new(OrientationMath.FromRotation(clockwiseDegrees));

    public BmageOrientation Orientation { get; }

    public override string Name => Orientation switch
    {
        BmageOrientation.Rotate90 => "Rotate 90°",
        BmageOrientation.Rotate180 => "Rotate 180°",
        BmageOrientation.Rotate270 => "Rotate 270°",
        BmageOrientation.FlipHorizontal => "Flip horizontal",
        BmageOrientation.FlipVertical => "Flip vertical",
        BmageOrientation.Transpose => "Transpose",
        BmageOrientation.Transverse => "Transverse",
        _ => "Orientation",
    };

    public override bool IsIdentity => Orientation == BmageOrientation.Normal;

    public override Size GetOutputSize(Size inputSize) => Orientation.Transform(inputSize);

    public Matrix3x2? GetTransform(Size inputSize) => Orientation.ToMatrix(inputSize);

    /// <summary>Combines this orientation with a following one.</summary>
    public OrientationOperation Then(OrientationOperation next) => new(Orientation.Then(next.Orientation));

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var o = Orientation;
        if (o == BmageOrientation.Normal) return context.CanMutateSource ? source : source.Clone(context.Allocator);

        int w = source.Width, h = source.Height;
        var outSize = o.Transform(source.Size);

        // In-place fast paths for mirrors and 180° when we own the buffer.
        if (context.CanMutateSource && !o.SwapsDimensions())
        {
            ApplyInPlace(source, o, context);
            return source;
        }

        // Every pixel is written below, so the buffer needs no clearing.
        var output = context.Allocate(outSize.Width, outSize.Height, clear: false);
        output.Metadata = source.Metadata;
        try
        {
            // Each source row lands in its own pixels of the output, so bands of source rows run side by side.
            ParallelRows.For(context, 0, h, w / 2, (start, end, band) => Reorient(source, output, o, start, end, band));
            context.ReportProgress(1);
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    /// <summary>Moves source rows <paramref name="start"/> to <paramref name="end"/> to where the orientation puts them.</summary>
    private static void Reorient(ImageBuffer source, ImageBuffer output, BmageOrientation o, int start, int end, OperationContext context)
    {
        int w = source.Width, h = source.Height;
        var src = source.Pixels;
        var dst = output.Pixels;
        var ow = output.Width;
        for (var y = start; y < end; y++)
        {
            if (((y - start) & 63) == 0) { context.ThrowIfCancellationRequested(); context.ReportProgress(y / (double)h); }
            var row = src.Slice(y * w, w);
            switch (o)
            {
                // Mirrors copy the row and reverse it in place, which the runtime vectorises.
                case BmageOrientation.FlipHorizontal:
                {
                    var d = dst.Slice(y * ow, ow);
                    row.CopyTo(d);
                    d.Reverse();
                    break;
                }
                case BmageOrientation.FlipVertical:
                    row.CopyTo(dst.Slice((h - 1 - y) * ow, ow));
                    break;
                case BmageOrientation.Rotate180:
                {
                    var d = dst.Slice((h - 1 - y) * ow, ow);
                    row.CopyTo(d);
                    d.Reverse();
                    break;
                }
                case BmageOrientation.Rotate90:
                    // (x,y) -> (h-1-y, x)
                    for (var x = 0; x < w; x++) dst[x * ow + (h - 1 - y)] = row[x];
                    break;
                case BmageOrientation.Rotate270:
                    // (x,y) -> (y, w-1-x)
                    for (var x = 0; x < w; x++) dst[(w - 1 - x) * ow + y] = row[x];
                    break;
                case BmageOrientation.Transpose:
                    // (x,y) -> (y, x)
                    for (var x = 0; x < w; x++) dst[x * ow + y] = row[x];
                    break;
                case BmageOrientation.Transverse:
                    // (x,y) -> (h-1-y, w-1-x)
                    for (var x = 0; x < w; x++) dst[(w - 1 - x) * ow + (h - 1 - y)] = row[x];
                    break;
            }
        }
    }

    private static void ApplyInPlace(ImageBuffer buffer, BmageOrientation o, OperationContext context)
    {
        int w = buffer.Width, h = buffer.Height;
        var px = buffer.Pixels;
        switch (o)
        {
            case BmageOrientation.FlipHorizontal:
                for (var y = 0; y < h; y++) px.Slice(y * w, w).Reverse();
                break;
            case BmageOrientation.FlipVertical:
            {
                var rented = w <= 4096 ? null : System.Buffers.ArrayPool<Rgba32>.Shared.Rent(w);
                Span<Rgba32> tmp = rented is null ? stackalloc Rgba32[w] : rented.AsSpan(0, w);
                for (var y = 0; y < h / 2; y++)
                {
                    var a = px.Slice(y * w, w);
                    var b = px.Slice((h - 1 - y) * w, w);
                    a.CopyTo(tmp); b.CopyTo(a); tmp.CopyTo(b);
                }
                if (rented is not null) System.Buffers.ArrayPool<Rgba32>.Shared.Return(rented);
                break;
            }
            case BmageOrientation.Rotate180:
                px.Reverse();
                break;
        }
        context.ReportProgress(1);
    }

    public override string ToString() => Name;
}

/// <summary>
/// Applies the EXIF orientation recorded in the buffer's metadata so the pixels appear upright, then resets the stored
/// orientation to <see cref="BmageOrientation.Normal"/>. A no-op when there is no orientation metadata.
/// </summary>
public sealed class AutoOrientOperation : ImageOperation
{
    public static AutoOrientOperation Instance { get; } = new();

    public override string Name => "Auto orient";

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var orientation = source.Metadata?.Exif?.Orientation ?? BmageOrientation.Normal;
        if (orientation is BmageOrientation.Normal or BmageOrientation.Unspecified)
            return context.CanMutateSource ? source : source.Clone(context.Allocator);
        var result = new OrientationOperation(orientation).Apply(source, context);
        if (result.Metadata is { } md) result.Metadata = md.WithOrientation(BmageOrientation.Normal);
        return result;
    }
}
