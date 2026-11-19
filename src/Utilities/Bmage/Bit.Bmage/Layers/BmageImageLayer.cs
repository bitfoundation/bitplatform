using System.Buffers;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Bit.Bmage;

/// <summary>
/// Pixels placed on the composition: a photo, a texture, a logo, a sticker. The image is drawn into
/// <see cref="Bounds"/>, rotated about its centre, after its own <see cref="Pipeline"/> has been applied to it.
/// </summary>
public sealed record BmageImageLayer : BmageLayer
{
    /// <summary>The layer's pixels. Not owned by the layer: the caller keeps it alive while the layer is used.</summary>
    public required BmageImageBuffer Image
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(Image));
    }

    /// <summary>Where the image is drawn, in pixels of the image the layer is composited onto.</summary>
    public required RectangleF Bounds { get; init; }

    /// <summary>Clockwise rotation in degrees about the centre of <see cref="Bounds"/>.</summary>
    public float Rotation { get; init; }

    /// <summary>
    /// Edits applied to the layer's own pixels before it is placed: desaturating a texture, blurring a backdrop. Run at
    /// the resolution the image is drawn at when that is smaller than the image, the way a proxy preview runs an edit.
    /// </summary>
    public BmageImagePipeline Pipeline
    {
        get;
        init => field = value ?? BmageImagePipeline.Empty;
    } = BmageImagePipeline.Empty;

    /// <summary>How the image is resampled into <see cref="Bounds"/>.</summary>
    public BmageResamplingFilter Filter { get; init; } = BmageResamplingFilter.Auto;

    public override string Kind => "Image";

    public override bool IsNoOp => base.IsNoOp || Bounds.Width <= 0 || Bounds.Height <= 0;

    /// <summary>The area the layer covers on an image, after rotation.</summary>
    public RectangleF GetRotatedBounds() => Rotation == 0 ? Bounds : BmageMaskRasterizer.BoundsOf(RotatedCorners());

    /// <summary>The corners of <see cref="Bounds"/> turned by <see cref="Rotation"/> about its centre, from the top left clockwise.</summary>
    private Vector2[] RotatedCorners()
    {
        var m = Matrix3x2.CreateRotation(Rotation * MathF.PI / 180f, new Vector2(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2));
        return
        [
            Vector2.Transform(new Vector2(Bounds.Left, Bounds.Top), m),
            Vector2.Transform(new Vector2(Bounds.Right, Bounds.Top), m),
            Vector2.Transform(new Vector2(Bounds.Right, Bounds.Bottom), m),
            Vector2.Transform(new Vector2(Bounds.Left, Bounds.Bottom), m),
        ];
    }

    protected internal override void CompositeCore(BmageImageBuffer target, BmageOperationContext context) => Composite(target, context, cache: null);

    /// <summary>Blends the layer onto the target, taking its prepared image from <paramref name="cache"/> when there is one.</summary>
    internal void Composite(BmageImageBuffer target, BmageOperationContext context, BmageLayerImageCache? cache)
    {
        if (IsNoOp) return;
        if (Image.IsDisposed) throw new ObjectDisposedException(nameof(Image), "The image this layer draws has been disposed.");
        var bounds = GetRotatedBounds();
        if (!float.IsFinite(bounds.Left) || !float.IsFinite(bounds.Top) || !float.IsFinite(bounds.Right) || !float.IsFinite(bounds.Bottom)) return;
        // A layer this far out cannot be placed in whole pixels at all.
        const float far = 1 << 24;
        if (!(bounds.Left > -far && bounds.Top > -far && bounds.Right < far && bounds.Bottom < far)) return;
        // An upright layer lands on whole pixels, rounded exactly as the canvas rounds a destination; a rotated one is
        // sampled at every pixel its outline reaches.
        var device = Rotation == 0
            ? Rectangle.FromLTRB((int)MathF.Round(Bounds.Left), (int)MathF.Round(Bounds.Top), (int)MathF.Round(Bounds.Right), (int)MathF.Round(Bounds.Bottom))
            : Rectangle.FromLTRB((int)MathF.Floor(bounds.Left), (int)MathF.Floor(bounds.Top), (int)MathF.Ceiling(bounds.Right), (int)MathF.Ceiling(bounds.Bottom));
        if (device.Width <= 0 || device.Height <= 0) return;
        var area = device;
        area.Intersect(target.Bounds);
        if (area.IsEmptyArea()) return;

        // The canvas draws with the layer's allocator, limits, threads and cancellation, and reports nothing itself.
        var drawing = context.WithProgress(null);
        var pixels = Pipeline.IsEmpty ? Image : cache is null ? PrepareImage(drawing) : cache.GetOrAdd(this, drawing);
        try
        {
            if (Mask is null && Rotation == 0)
            {
                // Without a mask the canvas blends the image straight onto the target, with the same arithmetic.
                using var direct = BmageImageCanvas.FromBuffer(target, drawing);
                direct.Opacity = Opacity;
                direct.DrawImage(pixels, device, BlendMode, Filter);
                return;
            }
            // The layer drawn on its own, over transparency, across the part of the target it covers. Shifting by whole
            // pixels keeps every sample where drawing it on the target would put it.
            using var raster = BmageImageBuffer.Create(area.Width, area.Height, context.Allocator, clear: true, BmageImageLimits.Unrestricted);
            using (var canvas = BmageImageCanvas.FromBuffer(raster, drawing))
            {
                if (Rotation == 0)
                {
                    canvas.DrawImage(pixels, new RectangleF(device.X - area.X, device.Y - area.Y, device.Width, device.Height), BmageBlendMode.SourceOver, Filter);
                }
                else
                {
                    canvas.Translate(-area.X, -area.Y);
                    canvas.Rotate(Rotation, new Vector2(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2));
                    canvas.DrawImage(pixels, Bounds, BmageBlendMode.SourceOver, Filter);
                }
            }
            // Copy replaces what is below, so a rotated layer must stop at its own outline: the raster's corners around
            // it are empty, not part of the layer.
            var layer = BlendMode == BmageBlendMode.Copy && Rotation != 0
                ? this with { Mask = Mask is null ? Outline() : Mask.Intersect(Outline()) }
                : this;
            layer.BlendRows(target, area, context, y => raster.GetRow(y - area.Y));
        }
        finally
        {
            // A cached image stays with the cache for the next composite.
            if (!ReferenceEquals(pixels, Image) && cache is null) pixels.Dispose();
        }
    }

    /// <summary>The layer's rotated rectangle, in pixels.</summary>
    private BmagePolygonMask Outline() => new(RotatedCorners());

    /// <summary>
    /// How far the image is shrunk before its pipeline runs: to the size the pipeline's result is drawn at, when that is
    /// smaller. A crop or a resize in the pipeline changes what lands in the bounds, so the scale compares the bounds with
    /// the pipeline's result, not with the image.
    /// </summary>
    internal double PrepareScale()
    {
        var output = Pipeline.TryGetOutputSize(Image.Size, out var size) ? size : Image.Size;
        return Math.Min(1.0, Math.Min(Bounds.Width / Math.Max(1, output.Width), Bounds.Height / Math.Max(1, output.Height)));
    }

    /// <summary>
    /// The image with its pipeline applied: first shrunk to the size it is drawn at when that is smaller, so a
    /// full-resolution layer previewed on a proxy costs what the proxy does. The caller owns the result.
    /// </summary>
    internal BmageImageBuffer PrepareImage(BmageOperationContext context)
    {
        var scale = PrepareScale();
        var options = new BmagePipelineExecutionOptions
        {
            Allocator = context.Allocator,
            Limits = context.Limits,
            CancellationToken = context.CancellationToken,
            MaxDegreeOfParallelism = context.MaxDegreeOfParallelism,
        };
        if (scale >= 0.999) return Pipeline.Execute(Image, options);
        var width = Math.Max(1, (int)Math.Round(Image.Width * scale));
        var height = Math.Max(1, (int)Math.Round(Image.Height * scale));
        // Never in place: the image is the caller's, and a resize to its own size would hand it straight back.
        using var reduced = new BmageResizeOperation(width, height, BmageResizeMode.Stretch, Filter).Apply(Image, context.WithProgress(null).WithMutation(false));
        return Pipeline.ForScale(scale).Execute(reduced, options);
    }

    protected override BmageLayer ScaleCore(double scale) => this with
    {
        Bounds = new RectangleF((float)(Bounds.X * scale), (float)(Bounds.Y * scale), (float)(Bounds.Width * scale), (float)(Bounds.Height * scale)),
    };

    /// <remarks>
    /// A move or a scale maps the bounds exactly, the two axes scaling apart if the image's do. Otherwise the layer's
    /// centre follows the image, its size scales with it and it turns with it; a mirror mirrors its pixels too, by a flip
    /// added to its pipeline, so a photo placed on the image stays a faithful part of it.
    /// </remarks>
    protected override BmageLayer TransformCore(Matrix3x2 matrix)
    {
        if (Rotation == 0 && BmageAffineParts.IsScaleAndTranslation(matrix)) return this with { Bounds = BmageAffineParts.MapRectangle(Bounds, matrix) };
        var parts = BmageAffineParts.Of(matrix);
        var centre = Vector2.Transform(new Vector2(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2), matrix);
        var size = new SizeF(Bounds.Width * parts.Scale, Bounds.Height * parts.Scale);
        return this with
        {
            Bounds = new RectangleF(centre.X - size.Width / 2, centre.Y - size.Height / 2, size.Width, size.Height),
            // A layer turned by r and then mirrored is the mirrored layer turned by -r.
            Rotation = BmageAffineParts.Normalize(parts.Mirrored ? parts.Angle - Rotation : Rotation + parts.Angle),
            Pipeline = parts.Mirrored ? Pipeline.FlipHorizontal() : Pipeline,
        };
    }
}
