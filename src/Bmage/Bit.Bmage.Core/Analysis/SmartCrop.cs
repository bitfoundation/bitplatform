using System.Drawing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Transforms;

namespace Bit.Bmage.Analysis;

/// <summary>Settings for <see cref="SmartCrop"/>.</summary>
public sealed record SmartCropOptions
{
    public static SmartCropOptions Default { get; } = new();

    /// <summary>
    /// Resolution the analysis runs at. Saliency does not need detail, and working small makes this fast enough to run
    /// on every upload. 100 is ample.
    /// </summary>
    public int AnalysisSize { get; init; } = 100;

    /// <summary>Weight given to local detail, which is where the subject usually is.</summary>
    public double DetailWeight { get; init; } = 1.0;

    /// <summary>Weight given to saturated colour, which draws the eye and is rarely background.</summary>
    public double SaturationWeight { get; init; } = 0.3;

    /// <summary>Weight given to skin-like colour, which keeps faces in frame without running face detection.</summary>
    public double SkinWeight { get; init; } = 1.8;

    /// <summary>
    /// How strongly to prefer a crop whose interesting content sits near a rule-of-thirds intersection rather than dead
    /// centre. Set to 0 to ignore composition.
    /// </summary>
    public double CompositionWeight { get; init; } = 0.25;

    /// <summary>How strongly to penalise cutting through a busy area at the crop border.</summary>
    public double EdgePenalty { get; init; } = 0.4;

    /// <summary>Number of candidate positions searched along each axis.</summary>
    public int SearchSteps { get; init; } = 24;
}

/// <summary>The chosen crop and how it scored.</summary>
public readonly record struct SmartCropResult(Rectangle Rectangle, double Score)
{
    /// <summary>Builds a crop operation for this result.</summary>
    public CropOperation ToOperation() => new(Rectangle);
}

/// <summary>
/// Chooses where to crop, rather than assuming the middle.
/// </summary>
/// <remarks>
/// <para>
/// A fixed anchor is wrong most of the time: centre-cropping a portrait to a square routinely cuts off the top of a
/// head, and centre-cropping a product photo can trim the product. This scores candidate positions on a small
/// downscaled copy using three cheap signals that correlate well with where a person looks, then picks the best.
/// </para>
/// <para>
/// The signals are local detail (edges and texture), colour saturation, and skin-like hue. Together they find faces and
/// subjects well enough for avatars, thumbnails and product tiles without shipping a face detection model. This is a
/// heuristic, not recognition; when you need a guaranteed face, run a detector and pass its rectangle to
/// <see cref="FindCrop(ImageBuffer, AspectRatio, Rectangle, SmartCropOptions?)"/>.
/// </para>
/// </remarks>
public static class SmartCrop
{
    /// <summary>Finds the best crop with the given aspect ratio.</summary>
    public static SmartCropResult FindCrop(ImageBuffer image, AspectRatio ratio, SmartCropOptions? options = null)
        => FindCrop(image, ratio, Rectangle.Empty, options);

    /// <summary>
    /// Finds the best crop with the given aspect ratio, biased to include <paramref name="mustInclude"/>. Pass a face
    /// rectangle from a detector, or the product's bounding box, when you have one.
    /// </summary>
    public static SmartCropResult FindCrop(ImageBuffer image, AspectRatio ratio, Rectangle mustInclude, SmartCropOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= SmartCropOptions.Default;
        if (ratio.Width <= 0 || ratio.Height <= 0) throw new ArgumentOutOfRangeException(nameof(ratio));

        var target = ratio.FitInside(image.Bounds);
        if (target.Width >= image.Width && target.Height >= image.Height)
            return new SmartCropResult(image.Bounds, 0);

        // Analyse a small copy: saliency is a low-frequency judgement and this keeps the cost independent of input size.
        var scale = Math.Min(1.0, options.AnalysisSize / (double)Math.Max(image.Width, image.Height));
        var analysisWidth = Math.Max(8, (int)Math.Round(image.Width * scale));
        var analysisHeight = Math.Max(8, (int)Math.Round(image.Height * scale));

        ImageBuffer small;
        var ownsSmall = false;
        if (analysisWidth >= image.Width && analysisHeight >= image.Height)
        {
            small = image;
        }
        else
        {
            small = new ResizeOperation(analysisWidth, analysisHeight, ResizeMode.Stretch, ResamplingFilter.Box)
                .Apply(image, OperationContext.Default);
            ownsSmall = true;
        }

        try
        {
            var saliency = BuildSaliencyMap(small, options);
            var integral = BuildIntegral(saliency, small.Width, small.Height);

            var sx = small.Width / (double)image.Width;
            var sy = small.Height / (double)image.Height;

            // The crop size in analysis space.
            var cropW = Math.Max(1, (int)Math.Round(target.Width * sx));
            var cropH = Math.Max(1, (int)Math.Round(target.Height * sy));
            if (cropW > small.Width) cropW = small.Width;
            if (cropH > small.Height) cropH = small.Height;

            var required = mustInclude.IsEmpty ? Rectangle.Empty : Rectangle.FromLTRB(
                (int)Math.Floor(mustInclude.Left * sx), (int)Math.Floor(mustInclude.Top * sy),
                (int)Math.Ceiling(mustInclude.Right * sx), (int)Math.Ceiling(mustInclude.Bottom * sy));

            var bestScore = double.NegativeInfinity;
            var bestX = (small.Width - cropW) / 2;
            var bestY = (small.Height - cropH) / 2;

            var stepX = Math.Max(1, (small.Width - cropW) / Math.Max(1, options.SearchSteps));
            var stepY = Math.Max(1, (small.Height - cropH) / Math.Max(1, options.SearchSteps));

            for (var y = 0; y <= small.Height - cropH; y += stepY)
            {
                for (var x = 0; x <= small.Width - cropW; x += stepX)
                {
                    var candidate = new Rectangle(x, y, cropW, cropH);
                    if (!required.IsEmpty && !candidate.Contains(required)) continue;

                    var score = Score(candidate, integral, saliency, small.Width, small.Height, options);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestX = x;
                        bestY = y;
                    }
                }
            }

            // If nothing satisfied the required region, fall back to a crop centred on it.
            if (double.IsNegativeInfinity(bestScore) && !required.IsEmpty)
            {
                bestX = Math.Clamp(required.X + required.Width / 2 - cropW / 2, 0, small.Width - cropW);
                bestY = Math.Clamp(required.Y + required.Height / 2 - cropH / 2, 0, small.Height - cropH);
                bestScore = 0;
            }

            // Map back to full resolution, clamped so rounding cannot push it outside the image.
            var finalX = Math.Clamp((int)Math.Round(bestX / sx), 0, image.Width - target.Width);
            var finalY = Math.Clamp((int)Math.Round(bestY / sy), 0, image.Height - target.Height);
            return new SmartCropResult(new Rectangle(finalX, finalY, target.Width, target.Height), bestScore);
        }
        finally
        {
            if (ownsSmall) small.Dispose();
        }
    }

    /// <summary>
    /// Scores a rectangle: the saliency it contains, a bonus for content near the thirds, and a penalty for cutting
    /// through busy pixels at the border.
    /// </summary>
    private static double Score(Rectangle candidate, double[] integral, double[] saliency, int width, int height, SmartCropOptions options)
    {
        var area = (double)candidate.Width * candidate.Height;
        if (area <= 0) return double.NegativeInfinity;

        var contained = SumRegion(integral, width, candidate) / area;

        double composition = 0;
        if (options.CompositionWeight > 0)
        {
            // Reward saliency concentrated near the four rule-of-thirds intersections.
            foreach (var (fx, fy) in new[] { (1 / 3.0, 1 / 3.0), (2 / 3.0, 1 / 3.0), (1 / 3.0, 2 / 3.0), (2 / 3.0, 2 / 3.0) })
            {
                var px = candidate.X + candidate.Width * fx;
                var py = candidate.Y + candidate.Height * fy;
                var radius = Math.Max(2, Math.Min(candidate.Width, candidate.Height) / 6);
                var region = Rectangle.FromLTRB(
                    Math.Max(0, (int)(px - radius)), Math.Max(0, (int)(py - radius)),
                    Math.Min(width, (int)(px + radius)), Math.Min(height, (int)(py + radius)));
                if (region.Width <= 0 || region.Height <= 0) continue;
                composition += SumRegion(integral, width, region) / (region.Width * (double)region.Height);
            }
            composition /= 4;
        }

        double edge = 0;
        if (options.EdgePenalty > 0)
        {
            // Cutting through detail looks like a mistake; prefer borders that fall on quiet pixels.
            edge = BorderEnergy(saliency, width, height, candidate);
        }

        return contained
             + composition * options.CompositionWeight
             - edge * options.EdgePenalty;
    }

    private static double BorderEnergy(double[] saliency, int width, int height, Rectangle r)
    {
        double sum = 0;
        var count = 0;
        // Only edges that actually cut the image count; an edge on the image boundary cuts nothing.
        if (r.Left > 0)
            for (var y = r.Top; y < r.Bottom; y++) { sum += saliency[y * width + r.Left]; count++; }
        if (r.Right < width)
            for (var y = r.Top; y < r.Bottom; y++) { sum += saliency[y * width + r.Right - 1]; count++; }
        if (r.Top > 0)
            for (var x = r.Left; x < r.Right; x++) { sum += saliency[r.Top * width + x]; count++; }
        if (r.Bottom < height)
            for (var x = r.Left; x < r.Right; x++) { sum += saliency[(r.Bottom - 1) * width + x]; count++; }
        return count == 0 ? 0 : sum / count;
    }

    /// <summary>Builds a per-pixel interest map from detail, saturation and skin likeness.</summary>
    private static double[] BuildSaliencyMap(ImageBuffer image, SmartCropOptions options)
    {
        int w = image.Width, h = image.Height;
        var map = new double[w * h];
        var luminance = new double[w * h];

        for (var y = 0; y < h; y++)
        {
            var row = image.GetRow(y);
            for (var x = 0; x < w; x++)
            {
                var p = row[x];
                luminance[y * w + x] = (0.2126 * p.R + 0.7152 * p.G + 0.0722 * p.B) / 255.0;
            }
        }

        for (var y = 0; y < h; y++)
        {
            var row = image.GetRow(y);
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                var p = row[x];

                // Detail: the gradient magnitude, which peaks on edges and texture rather than flat background.
                var left = luminance[y * w + Math.Max(0, x - 1)];
                var right = luminance[y * w + Math.Min(w - 1, x + 1)];
                var up = luminance[Math.Max(0, y - 1) * w + x];
                var down = luminance[Math.Min(h - 1, y + 1) * w + x];
                var detail = Math.Sqrt((right - left) * (right - left) + (down - up) * (down - up));

                // Saturation: vivid colour is rarely background.
                var max = Math.Max(p.R, Math.Max(p.G, p.B)) / 255.0;
                var min = Math.Min(p.R, Math.Min(p.G, p.B)) / 255.0;
                var saturation = max <= 0 ? 0 : (max - min) / max;

                var score = detail * options.DetailWeight + saturation * options.SaturationWeight;
                if (IsSkinLike(p)) score += options.SkinWeight;

                // Transparent pixels are not content.
                map[i] = p.A == 0 ? 0 : score * (p.A / 255.0);
            }
        }
        return map;
    }

    /// <summary>
    /// A skin-tone hint in normalised RGB, which is reasonably robust across lighting and skin tones. It pulls the crop
    /// towards people; it is not a classifier and does not pretend to be one.
    /// </summary>
    /// <remarks>
    /// The red dominance thresholds matter more than they look. A neutral grey sits at the exact centre of the
    /// normalised red/green plane, so a test that only bounds a region around skin will happily classify every grey wall
    /// and white background as a face, and the crop will then chase the background instead of the subject.
    /// </remarks>
    private static bool IsSkinLike(Rgba32 p)
    {
        double r = p.R, g = p.G, b = p.B;
        var sum = r + g + b;
        if (sum < 90 || sum > 720) return false;   // too dark or too close to blown out to judge

        var nr = r / sum;
        var ng = g / sum;

        return nr is > 0.36 and < 0.50       // clearly red dominant, but not a saturated red
            && ng is > 0.26 and < 0.36
            && nr - ng > 0.045               // rejects neutral greys, where nr and ng are both about a third
            && r - g > 14                    // and rejects near-neutral colours in absolute terms
            && g >= b;                       // skin is never blue dominant
    }

    /// <summary>Builds a summed-area table so any rectangle's total is four lookups regardless of its size.</summary>
    private static double[] BuildIntegral(double[] values, int width, int height)
    {
        var integral = new double[(width + 1) * (height + 1)];
        for (var y = 0; y < height; y++)
        {
            double rowSum = 0;
            for (var x = 0; x < width; x++)
            {
                rowSum += values[y * width + x];
                integral[(y + 1) * (width + 1) + x + 1] = integral[y * (width + 1) + x + 1] + rowSum;
            }
        }
        return integral;
    }

    private static double SumRegion(double[] integral, int width, Rectangle r)
    {
        var stride = width + 1;
        var a = integral[r.Top * stride + r.Left];
        var b = integral[r.Top * stride + r.Right];
        var c = integral[r.Bottom * stride + r.Left];
        var d = integral[r.Bottom * stride + r.Right];
        return d - b - c + a;
    }
}

/// <summary>
/// Crops to an aspect ratio at the position <see cref="SmartCrop"/> considers most interesting, instead of a fixed
/// anchor. The right default for avatars, thumbnails and product tiles.
/// </summary>
public sealed class SmartCropOperation : ImageOperation
{
    public SmartCropOperation(AspectRatio ratio, SmartCropOptions? options = null)
    {
        if (ratio.Width <= 0 || ratio.Height <= 0) throw new ArgumentOutOfRangeException(nameof(ratio));
        Ratio = ratio;
        Options = options ?? SmartCropOptions.Default;
    }

    public AspectRatio Ratio { get; }
    public SmartCropOptions Options { get; }

    public override string Name => "Smart crop";

    public override Size GetOutputSize(Size inputSize) => Ratio.FitInside(new Rectangle(Point.Empty, inputSize)).Size;

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var result = SmartCrop.FindCrop(source, Ratio, Options);
        context.ThrowIfCancellationRequested();
        return result.ToOperation().Apply(source, context);
    }

    public override string ToString() => $"SmartCrop({Ratio})";
}
