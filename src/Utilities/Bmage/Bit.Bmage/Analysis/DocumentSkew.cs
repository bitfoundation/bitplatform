using System.Drawing;

namespace Bit.Bmage.Analysis;

/// <summary>How far a document's lines are tilted, and how sure the estimate is.</summary>
/// <param name="Angle">
/// The clockwise rotation in degrees that levels the lines: pass it to <see cref="Pipeline.ImagePipeline.Straighten"/>
/// or <see cref="Pipeline.ImagePipeline.Rotate"/> as it is.
/// </param>
/// <param name="Confidence">
/// How clearly one angle stood out from the others, in [0,1]: about 0.9 for lines of text, and below 0.3 for scattered
/// marks with no lines among them. An estimate below about 0.3 is better left unapplied. Texture with a grain along the
/// pixel grid can score higher, but then the angle found is within a fraction of a degree of level.
/// </param>
public readonly record struct SkewEstimate(double Angle, double Confidence);

/// <summary>
/// Measures how far the lines of a scanned or photographed document are tilted, to level them before reading the
/// page, cropping it or decoding a barcode on it.
/// </summary>
/// <remarks>
/// <para>
/// The image is reduced to at most 1024 pixels across, and its ink (the darker of the two tones Otsu's method finds,
/// or the lighter where that is rarer, as with light text on a dark page) is projected onto lines at each candidate
/// angle. Lines of text pile ink into a few rows and leave the gaps between them empty only when the angle matches
/// theirs, so the angle whose profile changes most sharply from row to row is the tilt. Candidates are tried half a
/// degree apart, then a twentieth of a degree apart around the best.
/// </para>
/// <para>
/// Like trimming, this depends on the pixels, so it is an analysis and not an operation: measure once, then record
/// the rotation it gives, which replays exactly on a proxy, a server or another copy of the image.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var scan = await loader.LoadAsync(file);
/// var pipeline = ImagePipeline.Create();
/// if (DocumentSkew.Estimate(scan) is { Confidence: > 0.3 } skew)
///     pipeline = pipeline.Straighten(skew.Angle);
/// using var page = pipeline.AdaptiveThreshold(radius: 20).Execute(scan);
/// </code>
/// </example>
public static class DocumentSkew
{
    /// <summary>The largest tilt that can be searched for, in degrees either way.</summary>
    public const double MaxSearchAngle = 45;

    private const int AnalysisSize = 1024;
    private const int MaxInkPoints = 250_000;

    /// <summary>
    /// Estimates the tilt of the lines in <paramref name="image"/>, or returns null when there is too little ink to
    /// tell (a blank page, or an image of one tone).
    /// </summary>
    /// <param name="image">The page.</param>
    /// <param name="maxAngle">The largest tilt to consider, in degrees either way, up to 45.</param>
    /// <param name="region">The part of the image to measure, or null for all of it.</param>
    /// <param name="cancellationToken">Cancels the measurement.</param>
    public static SkewEstimate? Estimate(ImageBuffer image, double maxAngle = 15, Rectangle? region = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (!double.IsFinite(maxAngle) || maxAngle <= 0 || maxAngle > MaxSearchAngle)
            throw new ArgumentOutOfRangeException(nameof(maxAngle), maxAngle, $"The angle must be more than 0 and at most {MaxSearchAngle} degrees.");
        var area = region ?? image.Bounds;
        area.Intersect(image.Bounds);
        if (area.Width < 16 || area.Height < 16) return null;

        var plane = LuminancePlane.Create(image, area, AnalysisSize, cancellationToken);
        if (FindInk(plane, cancellationToken) is not var (xs, ys) || xs.Length < 64) return null;
        var weights = Window(xs, ys, plane.Width / 2f, plane.Height / 2f);

        var profile = new float[(int)Math.Ceiling(Math.Sqrt((double)plane.Width * plane.Width + (double)plane.Height * plane.Height)) + 4];
        double Score(double degrees)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var radians = degrees * Math.PI / 180;
            float sin = (float)Math.Sin(radians), cos = (float)Math.Cos(radians);
            Array.Clear(profile);
            var middle = profile.Length / 2f;
            // Where a point lands once the image is turned clockwise by the candidate angle: on screen, with rows
            // growing downwards, that rotation takes (x, y) to (x cos - y sin, x sin + y cos). Each point is shared
            // between the two rows of the profile it falls between, so no angle gains from lining the profile's rows up
            // with the pixels'.
            for (var i = 0; i < xs.Length; i++)
            {
                var position = xs[i] * sin + ys[i] * cos + middle;
                var row = (int)position;
                var share = position - row;
                var weight = weights[i];
                profile[row] += weight * (1 - share);
                profile[row + 1] += weight * share;
            }
            double score = 0;
            for (var i = 1; i < profile.Length; i++)
            {
                double d = profile[i] - profile[i - 1];
                score += d * d;
            }
            return score;
        }

        // Coarse, then fine around the best coarse angle.
        double best = 0, bestScore = double.MinValue, total = 0;
        var coarse = 0;
        for (var a = -Math.Floor(maxAngle * 2) / 2; a <= maxAngle + 1e-9; a += 0.5)
        {
            var s = Score(a);
            total += s;
            coarse++;
            if (s > bestScore || (s == bestScore && Math.Abs(a) < Math.Abs(best))) (best, bestScore) = (a, s);
        }
        var mean = total / coarse;
        var centre = best;
        for (var a = centre - 0.5; a <= centre + 0.5 + 1e-9; a += 0.05)
        {
            if (Math.Abs(a) > maxAngle + 1e-9) continue;
            var s = Score(a);
            if (s > bestScore) (best, bestScore) = (a, s);
        }

        var confidence = bestScore <= 0 ? 0 : Math.Clamp(1 - mean / bestScore, 0, 1);
        return new SkewEstimate(Math.Round(best, 2) + 0.0, confidence);
    }

    /// <summary>
    /// How much each point counts: fully inside the ellipse the plane's edges enclose, fading to nothing over its outer
    /// fifth and beyond it. The edges of a rectangle project to a sharp step only at the angles square to them, which
    /// would make any image with ink up to its borders, a photo or a scan of a dark desk, look level; an ellipse with a
    /// soft rim projects to a smooth hump at every angle.
    /// </summary>
    private static float[] Window(float[] xs, float[] ys, float halfWidth, float halfHeight)
    {
        var weights = new float[xs.Length];
        for (var i = 0; i < xs.Length; i++)
        {
            float u = xs[i] / halfWidth, v = ys[i] / halfHeight;
            var r = MathF.Sqrt(u * u + v * v);
            weights[i] = Math.Clamp((1 - r) / 0.2f, 0, 1);
        }
        return weights;
    }

    /// <summary>
    /// The points of the rarer tone, relative to the plane's centre, at most <see cref="MaxInkPoints"/> of them taken
    /// evenly; null when either tone is almost absent.
    /// </summary>
    private static (float[] Xs, float[] Ys)? FindInk(LuminancePlane plane, CancellationToken cancellationToken)
    {
        var values = plane.Values;
        var histogram = new int[256];
        foreach (var v in values) histogram[v]++;
        var level = OtsuLevel(histogram, values.Length);
        var dark = 0;
        for (var i = 0; i <= level; i++) dark += histogram[i];
        var light = values.Length - dark;
        var inkIsDark = dark <= light;
        var ink = inkIsDark ? dark : light;
        // Too little of either tone (under a thousandth of the page) is a blank page, not a document.
        if (ink < values.Length / 1000 || ink == 0) return null;

        var step = Math.Max(1, (ink + MaxInkPoints - 1) / MaxInkPoints);
        var count = (ink + step - 1) / step;
        var xs = new float[count];
        var ys = new float[count];
        float cx = plane.Width / 2f, cy = plane.Height / 2f;
        int seen = 0, taken = 0;
        for (var y = 0; y < plane.Height; y++)
        {
            if ((y & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            var row = values.AsSpan(y * plane.Width, plane.Width);
            for (var x = 0; x < row.Length; x++)
            {
                if ((row[x] <= level) != inkIsDark) continue;
                if (seen++ % step != 0 || taken == count) continue;
                // Spread over the pixel by a fixed hash of its position, so the points do not sit on a grid that some
                // angles line up with and others do not.
                var hash = (uint)(x * 73856093) ^ (uint)(y * 19349663);
                hash *= 0x9E3779B1;
                xs[taken] = x + (hash >> 16) / 65536f - cx;
                ys[taken] = y + (hash & 0xFFFF) / 65536f - cy;
                taken++;
            }
        }
        return taken == count ? (xs, ys) : (xs[..taken], ys[..taken]);
    }

    /// <summary>The level that best separates the histogram into two tones (Otsu's method): values at or below it are dark.</summary>
    internal static int OtsuLevel(ReadOnlySpan<int> histogram, long total)
    {
        double sum = 0;
        for (var i = 0; i < 256; i++) sum += i * (double)histogram[i];
        double sumBelow = 0, best = -1;
        long below = 0;
        var level = 127;
        for (var t = 0; t < 255; t++)
        {
            below += histogram[t];
            if (below == 0) continue;
            var above = total - below;
            if (above == 0) break;
            sumBelow += t * (double)histogram[t];
            var meanBelow = sumBelow / below;
            var meanAbove = (sum - sumBelow) / above;
            var between = (double)below * above * (meanBelow - meanAbove) * (meanBelow - meanAbove);
            if (between > best) (best, level) = (between, t);
        }
        return level;
    }
}
