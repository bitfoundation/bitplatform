using System.Drawing;
using System.Numerics;
using Bit.Bmage.Geometry;

namespace Bit.Bmage.Operations.Transforms;

/// <summary>
/// Applies one of the eight lossless orientations (rotations by multiples of 90° and mirrors). Consecutive
/// orientation operations combine into a single one.
/// </summary>
public sealed class OrientationOperation : ImageOperation, IGeometricOperation
{
    public OrientationOperation(Orientation orientation)
    {
        Orientation = orientation == Orientation.Unspecified ? Orientation.Normal : orientation;
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(orientation));
    }

    /// <summary>Rotate 90° clockwise.</summary>
    public static OrientationOperation Rotate90 { get; } = new(Orientation.Rotate90);
    /// <summary>Rotate 180°.</summary>
    public static OrientationOperation Rotate180 { get; } = new(Orientation.Rotate180);
    /// <summary>Rotate 270° clockwise (90° counter-clockwise).</summary>
    public static OrientationOperation Rotate270 { get; } = new(Orientation.Rotate270);
    /// <summary>Mirror horizontally.</summary>
    public static OrientationOperation FlipHorizontal { get; } = new(Orientation.FlipHorizontal);
    /// <summary>Mirror vertically.</summary>
    public static OrientationOperation FlipVertical { get; } = new(Orientation.FlipVertical);

    /// <summary>Creates an operation for a clockwise rotation that is a multiple of 90°.</summary>
    public static OrientationOperation FromRotation(int clockwiseDegrees) => new(OrientationMath.FromRotation(clockwiseDegrees));

    public Orientation Orientation { get; }

    public override string Name => Orientation switch
    {
        Orientation.Rotate90 => "Rotate 90°",
        Orientation.Rotate180 => "Rotate 180°",
        Orientation.Rotate270 => "Rotate 270°",
        Orientation.FlipHorizontal => "Flip horizontal",
        Orientation.FlipVertical => "Flip vertical",
        Orientation.Transpose => "Transpose",
        Orientation.Transverse => "Transverse",
        _ => "Orientation",
    };

    public override bool IsIdentity => Orientation == Orientation.Normal;

    public override Size GetOutputSize(Size inputSize) => Orientation.Transform(inputSize);

    public Matrix3x2? GetTransform(Size inputSize) => Orientation.ToMatrix(inputSize);

    /// <summary>Combines this orientation with a following one.</summary>
    public OrientationOperation Then(OrientationOperation next) => new(Orientation.Then(next.Orientation));

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var o = Orientation;
        if (o == Orientation.Normal) return context.CanMutateSource ? source : source.Clone(context.Allocator);

        int w = source.Width, h = source.Height;
        var outSize = o.Transform(source.Size);

        // In-place fast paths for mirrors and 180° when we own the buffer.
        if (context.CanMutateSource && !o.SwapsDimensions())
        {
            ApplyInPlace(source, o, context);
            return source;
        }

        var output = context.Allocate(outSize.Width, outSize.Height);
        output.Metadata = source.Metadata;
        try
        {
            var src = source.Pixels;
            var dst = output.Pixels;
            var ow = outSize.Width;
            for (var y = 0; y < h; y++)
            {
                if ((y & 63) == 0) { context.ThrowIfCancellationRequested(); context.ReportProgress(y / (double)h); }
                var row = src.Slice(y * w, w);
                switch (o)
                {
                    case Orientation.FlipHorizontal:
                    {
                        var d = dst.Slice(y * ow, ow);
                        for (var x = 0; x < w; x++) d[w - 1 - x] = row[x];
                        break;
                    }
                    case Orientation.FlipVertical:
                        row.CopyTo(dst.Slice((h - 1 - y) * ow, ow));
                        break;
                    case Orientation.Rotate180:
                    {
                        var d = dst.Slice((h - 1 - y) * ow, ow);
                        for (var x = 0; x < w; x++) d[w - 1 - x] = row[x];
                        break;
                    }
                    case Orientation.Rotate90:
                        // (x,y) -> (h-1-y, x)
                        for (var x = 0; x < w; x++) dst[x * ow + (h - 1 - y)] = row[x];
                        break;
                    case Orientation.Rotate270:
                        // (x,y) -> (y, w-1-x)
                        for (var x = 0; x < w; x++) dst[(w - 1 - x) * ow + y] = row[x];
                        break;
                    case Orientation.Transpose:
                        // (x,y) -> (y, x)
                        for (var x = 0; x < w; x++) dst[x * ow + y] = row[x];
                        break;
                    case Orientation.Transverse:
                        // (x,y) -> (h-1-y, w-1-x)
                        for (var x = 0; x < w; x++) dst[(w - 1 - x) * ow + (h - 1 - y)] = row[x];
                        break;
                }
            }
            context.ReportProgress(1);
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    private static void ApplyInPlace(ImageBuffer buffer, Orientation o, OperationContext context)
    {
        int w = buffer.Width, h = buffer.Height;
        var px = buffer.Pixels;
        switch (o)
        {
            case Orientation.FlipHorizontal:
                for (var y = 0; y < h; y++) px.Slice(y * w, w).Reverse();
                break;
            case Orientation.FlipVertical:
            {
                Span<Rgba32> tmp = w <= 4096 ? stackalloc Rgba32[w] : new Rgba32[w];
                for (var y = 0; y < h / 2; y++)
                {
                    var a = px.Slice(y * w, w);
                    var b = px.Slice((h - 1 - y) * w, w);
                    a.CopyTo(tmp); b.CopyTo(a); tmp.CopyTo(b);
                }
                break;
            }
            case Orientation.Rotate180:
                px.Reverse();
                break;
        }
        context.ReportProgress(1);
    }

    public override string ToString() => Name;
}

/// <summary>
/// Applies the EXIF orientation recorded in the buffer's metadata so the pixels appear upright, then resets the stored
/// orientation to <see cref="Orientation.Normal"/>. A no-op when there is no orientation metadata.
/// </summary>
public sealed class AutoOrientOperation : ImageOperation
{
    public static AutoOrientOperation Instance { get; } = new();

    public override string Name => "Auto orient";

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var orientation = source.Metadata?.Exif?.Orientation ?? Orientation.Normal;
        if (orientation is Orientation.Normal or Orientation.Unspecified)
            return context.CanMutateSource ? source : source.Clone(context.Allocator);
        var result = new OrientationOperation(orientation).Apply(source, context);
        if (result.Metadata is { } md) result.Metadata = md.WithOrientation(Orientation.Normal);
        return result;
    }
}
