using System.Drawing;
using System.Numerics;
using Bit.Bmage.Geometry;

namespace Bit.Bmage.Operations.Transforms;

/// <summary>
/// Changes the canvas around the image without scaling it: grows it, filling the new area with
/// <see cref="Background"/>, or shrinks it, cutting the edges the anchor leaves outside. A border, a square canvas for a
/// product photo, or room for a caption.
/// </summary>
/// <remarks>
/// The canvas is given either as an absolute size with an <see cref="Anchor"/> saying where the image sits in it, or
/// as margins added to each side (<see cref="Margins(int, int, int, int, Rgba32)"/>), negative margins cutting into the
/// image. Pixels are copied, never resampled, so the image itself is unchanged.
/// </remarks>
/// <example>
/// <code>
/// // A 40 pixel white border.
/// pipeline.Apply(CanvasSizeOperation.Margins(40, Rgba32.White));
///
/// // A square canvas with the photo at the top.
/// pipeline.CanvasSize(1200, 1200, Anchor.Top, Rgba32.White);
/// </code>
/// </example>
public sealed class CanvasSizeOperation : ImageOperation, IGeometricOperation
{
    /// <summary>A canvas of an absolute size, with the image placed in it by <paramref name="anchor"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The size is not positive.</exception>
    public CanvasSizeOperation(int width, int height, Anchor anchor = Anchor.Center, Rgba32 background = default)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "The canvas must be at least one pixel wide.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "The canvas must be at least one pixel high.");
        Size = new Size(width, height);
        Anchor = anchor;
        Background = background;
    }

    private CanvasSizeOperation(int left, int top, int right, int bottom, Rgba32 background)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Background = background;
    }

    /// <summary>
    /// A canvas grown by the given margins on each side, or shrunk where a margin is negative. The image keeps its
    /// position relative to the margins, so <c>Margins(10, 0, 0, 0)</c> adds a ten pixel strip on the left.
    /// </summary>
    public static CanvasSizeOperation Margins(int left, int top, int right, int bottom, Rgba32 background = default)
        => new(left, top, right, bottom, background);

    /// <summary>A canvas grown by <paramref name="all"/> pixels on every side: a border.</summary>
    public static CanvasSizeOperation Margins(int all, Rgba32 background = default) => new(all, all, all, all, background);

    /// <summary>The absolute size of the canvas, or null when it is given by margins.</summary>
    public Size? Size { get; }

    /// <summary>Where the image sits in a canvas of an absolute <see cref="Size"/>. Unused for margins.</summary>
    public Anchor Anchor { get; }

    /// <summary>Pixels added to the left, or cut from it when negative. Zero for an absolute size.</summary>
    public int Left { get; }

    /// <summary>Pixels added to the top, or cut from it when negative. Zero for an absolute size.</summary>
    public int Top { get; }

    /// <summary>Pixels added to the right, or cut from it when negative. Zero for an absolute size.</summary>
    public int Right { get; }

    /// <summary>Pixels added to the bottom, or cut from it when negative. Zero for an absolute size.</summary>
    public int Bottom { get; }

    /// <summary>The colour of the area the canvas gains. Transparent by default.</summary>
    public Rgba32 Background { get; }

    public override string Name => "Canvas size";

    public override bool IsIdentity => Size is null && Left == 0 && Top == 0 && Right == 0 && Bottom == 0;

    /// <summary>
    /// Where the image lands on the canvas for an input of <paramref name="inputSize"/>, and the canvas's size. The
    /// placement may reach outside the canvas, where the canvas is smaller than the image.
    /// </summary>
    /// <exception cref="ArgumentException">The margins leave no canvas.</exception>
    public (Rectangle Placement, Size Canvas) GetPlacement(Size inputSize)
    {
        if (Size is { } size) return (Anchor.Place(inputSize, size), size);
        var width = (long)inputSize.Width + Left + Right;
        var height = (long)inputSize.Height + Top + Bottom;
        if (width <= 0 || height <= 0)
            throw new ArgumentException($"Margins of {Left}, {Top}, {Right} and {Bottom} leave no canvas of an image of {inputSize.Width}x{inputSize.Height}.");
        if (width > int.MaxValue || height > int.MaxValue)
            throw new ArgumentException($"Margins of {Left}, {Top}, {Right} and {Bottom} make a canvas too large to address.");
        return (new Rectangle(Left, Top, inputSize.Width, inputSize.Height), new Size((int)width, (int)height));
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">The margins leave no canvas.</exception>
    public override Size GetOutputSize(Size inputSize) => GetPlacement(inputSize).Canvas;

    public Matrix3x2? GetTransform(Size inputSize)
    {
        var placement = GetPlacement(inputSize).Placement;
        return Matrix3x2.CreateTranslation(placement.X, placement.Y);
    }

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var (placement, canvas) = GetPlacement(source.Size);
        if (canvas == source.Size && placement.Location == Point.Empty)
            return context.CanMutateSource ? source : source.Clone(context.Allocator);
        var output = context.Allocate(canvas.Width, canvas.Height, clear: Background.A == 0);
        output.Metadata = source.Metadata;
        try
        {
            if (Background.A != 0) output.Fill(Background);
            var visible = placement;
            visible.Intersect(new Rectangle(Point.Empty, canvas));
            for (var y = visible.Top; y < visible.Bottom; y++)
            {
                if (((y - visible.Top) & 63) == 0) context.ThrowIfCancellationRequested();
                source.GetRow(y - placement.Y).Slice(visible.Left - placement.X, visible.Width).CopyTo(output.GetRow(y).Slice(visible.Left, visible.Width));
            }
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    public override IImageOperation ForScale(double scale)
    {
        if (scale == 1) return this;
        static int Scale(int value, double scale) => (int)Math.Round(value * scale);
        if (Size is { } size)
            return new CanvasSizeOperation(Math.Max(1, Scale(size.Width, scale)), Math.Max(1, Scale(size.Height, scale)), Anchor, Background);
        return new CanvasSizeOperation(Scale(Left, scale), Scale(Top, scale), Scale(Right, scale), Scale(Bottom, scale), Background);
    }

    public override string ToString() => Size is { } s
        ? $"CanvasSize({s.Width}x{s.Height}, {Anchor})"
        : $"CanvasSize(margins {Left},{Top},{Right},{Bottom})";
}

/// <summary>
/// Levels a tilted photo: rotates it by a small angle about its centre and crops to the largest rectangle of the
/// photo's own aspect ratio that the rotated photo covers, so the result has no empty corners. A horizon, a scanned
/// page, a building's edge.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="RotateOperation"/>, which keeps or grows the canvas and fills the corners with a background, every
/// output pixel here comes from the photo, so an opaque photo stays opaque. Output pixels are sampled at their centres,
/// half a pixel inside the crop, which puts every sample at least half a pixel inside the rotated photo: a bilinear
/// sample's taps all land on it, and the only bicubic taps beyond it carry no weight or a negative one.
/// </para>
/// <para>
/// The crop is a fraction of the image, the same at every resolution, so a straighten previewed on a proxy shows the
/// same framing as the full-resolution export, to within a pixel of rounding.
/// </para>
/// </remarks>
public sealed class StraightenOperation : ImageOperation, IGeometricOperation
{
    /// <summary>The largest tilt a straighten corrects, either way. Larger turns are a rotation by quarter turns first.</summary>
    public const double MaxDegrees = 45;

    /// <param name="degrees">Clockwise rotation, between -45 and 45 degrees. Negative turns anticlockwise.</param>
    /// <param name="sampling">How the rotated photo is sampled.</param>
    /// <exception cref="ArgumentOutOfRangeException">The angle is not a finite number between -45 and 45.</exception>
    public StraightenOperation(double degrees, WarpSampling sampling = WarpSampling.Bicubic)
    {
        if (!double.IsFinite(degrees) || Math.Abs(degrees) > MaxDegrees)
            throw new ArgumentOutOfRangeException(nameof(degrees), degrees, $"A straighten turns by at most {MaxDegrees} degrees either way.");
        Degrees = degrees;
        Sampling = sampling;
    }

    /// <summary>Clockwise rotation in degrees, between -45 and 45.</summary>
    public double Degrees { get; }

    public WarpSampling Sampling { get; }

    public override string Name => "Straighten";

    public override bool IsIdentity => Degrees == 0;

    /// <summary>
    /// The fraction of each dimension the straightened image keeps: the largest scale at which a rectangle of the
    /// image's aspect ratio, centred, fits inside the image rotated by <paramref name="degrees"/>.
    /// </summary>
    public static double GetCropScale(Size inputSize, double degrees)
    {
        if (inputSize.Width <= 0 || inputSize.Height <= 0) throw new ArgumentOutOfRangeException(nameof(inputSize));
        var radians = Math.Abs(degrees) * Math.PI / 180;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        double w = inputSize.Width, h = inputSize.Height;
        // A centred w*k x h*k rectangle, turned back by the angle, must stay within the image: its half-extents along
        // the image's axes are (w*k*cos + h*k*sin)/2 and (w*k*sin + h*k*cos)/2.
        return Math.Min(1, Math.Min(w / (w * cos + h * sin), h / (w * sin + h * cos)));
    }

    public override Size GetOutputSize(Size inputSize)
    {
        if (IsIdentity) return inputSize;
        var k = GetCropScale(inputSize, Degrees);
        // Rounded down, with a hair of slack for an exact fit, so the crop never reaches past the rotated photo.
        return new Size(Math.Max(1, (int)Math.Floor(inputSize.Width * k + 1e-6)), Math.Max(1, (int)Math.Floor(inputSize.Height * k + 1e-6)));
    }

    public Matrix3x2? GetTransform(Size inputSize) => IsIdentity ? Matrix3x2.Identity : Forward(inputSize, GetOutputSize(inputSize));

    /// <summary>Rotates about the input's centre and moves that centre to the output's.</summary>
    private Matrix3x2 Forward(Size input, Size output)
    {
        var centre = new Vector2(input.Width / 2f, input.Height / 2f);
        return Matrix3x2.CreateRotation((float)(Degrees * Math.PI / 180), centre)
            * Matrix3x2.CreateTranslation(output.Width / 2f - centre.X, output.Height / 2f - centre.Y);
    }

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        if (IsIdentity) return context.CanMutateSource ? source : source.Clone(context.Allocator);
        var size = GetOutputSize(source.Size);
        Matrix3x2.Invert(Forward(source.Size, size), out var inverse);
        var output = context.Allocate(size.Width, size.Height, clear: false);
        output.Metadata = source.Metadata;
        try
        {
            Warp.RunAffine(source, output, inverse, Sampling, default, context);
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    public override string ToString() => $"Straighten({Degrees:0.##}°)";
}
