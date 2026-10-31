using System.Numerics;

namespace Bit.BbScanner;

/// <summary>
/// The MaxiCode entry point (ISO/IEC 16023): the fixed size hexagonal symbol on parcel labels, found by its bullseye.
/// </summary>
/// <remarks>
/// <para>
/// A MaxiCode symbol is 33 rows of 30 hexagonal modules, every odd row half a module to the right, round a bullseye of
/// three dark rings. The rings are what is looked for: a line through their centre, at any angle, crosses eleven runs -
/// dark, light, dark, light, dark, the light centre half again as wide, and the same again - in proportions no other
/// symbology has. A row of the binarised image that crosses them is checked again down the column and both diagonals
/// through the centre it gives; the four diameters it measures are those of an ellipse when the symbol is seen at an
/// angle, which gives the shape of the module grid, all but its rotation.
/// </para>
/// <para>
/// The rotation comes from the orientation patterns: six clusters of three modules round the bullseye whose dark and
/// light modules no other rotation of the grid matches. Every rotation is tried a degree at a time, and only one that
/// matches at least sixteen of the eighteen modules is read: as they lie close to the centre, a few degrees either side
/// match them as well, so the middle of the run that does is taken. The 864 data modules are sampled at their centres
/// - from the luminance the image was binarised from, when the pipeline has it - there and a degree or few either side,
/// and handed to <see cref="BbMaxiCodeDecoder"/>, whose Reed-Solomon correction is the judge of which is right.
/// </para>
/// </remarks>
public sealed class BbMaxiCodeReader : IBbMatrixDecoder, IBbLuminanceMatrixDecoder
{
    /// <summary>The radii of the six circles bounding the bullseye's rings, in module widths, from the centre out.</summary>
    private static readonly float[] RingRadii = [0.5774f, 1.3359f, 2.1058f, 2.8644f, 3.6229f, 4.3814f];

    /// <summary>
    /// The width of the nine inner runs across the bullseye - all but the outer ring either side - in units of one
    /// ring's width.
    /// </summary>
    private const float InnerRings = 9.52f;

    /// <summary>
    /// The orientation modules, as <c>row * 30 + column</c>, and whether each is dark: six clusters of three round the
    /// bullseye.
    /// </summary>
    private static readonly (short Module, bool Dark)[] Orientation =
    [
        (280, true), (281, true), (311, true),
        (287, false), (317, false), (318, false),
        (457, true), (487, false), (488, true),
        (500, true), (501, false), (530, true),
        (670, true), (671, false), (700, true),
        (677, true), (706, false), (707, true),
    ];

    /// <summary>The fewest of the eighteen orientation modules a rotation has to match to be read.</summary>
    private const int MinOrientationMatches = 16;

    /// <summary>Most bullseyes followed up in one image.</summary>
    private const int MaxCandidates = 4;

    private readonly List<Bullseye> _candidates = new(16);
    private readonly int[] _runs = new int[11];
    private int _rowPhase;

    /// <summary>A bullseye: its centre, and the radius of its outer ring in four directions - across, down and the two diagonals.</summary>
    private readonly record struct Bullseye(float X, float Y, float Across, float Down, float Falling, float Rising, int Seen = 1);

    /// <inheritdoc />
    public BbBarcodeFormat Formats => BbBarcodeFormat.MaxiCode;

    /// <inheritdoc />
    BbSymbolDecodeResult? IBbLuminanceMatrixDecoder.Decode(BbBitMatrix image, in BbLuminanceView luminance, BbMatrixDecodeOptions options)
        => Decode(image, luminance, options);

    /// <inheritdoc />
    public BbSymbolDecodeResult? Decode(BbBitMatrix image, BbMatrixDecodeOptions options) => Decode(image, default, options);

    /// <summary>
    /// Decodes with the luminance plane the image was binarised from, which the measures and the samples are taken
    /// from when it is not empty. It is handed down, never kept: the reader holds no reference to a frame once read.
    /// </summary>
    private BbSymbolDecodeResult? Decode(BbBitMatrix image, in BbLuminanceView luminance, BbMatrixDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);

        var result = TryDecode(image, luminance, options.TryHarder, inverted: false);
        if (result is not null || !options.AllowInverted)
        {
            return result;
        }

        image.Invert();
        try
        {
            return TryDecode(image, luminance, options.TryHarder, inverted: true);
        }
        finally
        {
            image.Invert();
        }
    }

    private BbSymbolDecodeResult? TryDecode(BbBitMatrix image, in BbLuminanceView luminance, bool thorough, bool inverted)
    {
        FindBullseyes(image, luminance, thorough);
        var tried = 0;
        foreach (var bullseye in _candidates)
        {
            if (tried++ >= MaxCandidates)
            {
                break;
            }

            if (TryRead(image, luminance, bullseye, inverted) is { } result)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Scans rows for the bullseye's eleven runs, confirms each down its column and both diagonals, and keeps the
    /// distinct centres, the most often seen first.
    /// </summary>
    private void FindBullseyes(BbBitMatrix image, in BbLuminanceView luminance, bool thorough)
    {
        _candidates.Clear();
        // A live frame skips rows, each search starting a row on from the last: the light centre is only a module and a
        // bit across, and the next frames of a symbol held still cross it at the rows this one missed.
        var step = thorough ? 1 : Math.Max(1, image.Height / 160);
        _rowPhase = (_rowPhase + 1) % step;
        var runs = _runs;
        for (var y = _rowPhase; y < image.Height; y += step)
        {
            // The last eleven runs of the row, the oldest first; count says how many there are yet.
            var count = 0;
            var x = 0;
            var width = image.Width;
            while (x < width)
            {
                var dark = image[x, y];
                var end = x + 1;
                while (end < width && image[end, y] == dark)
                {
                    end++;
                }

                if (count == 11)
                {
                    Array.Copy(runs, 1, runs, 0, 10);
                    count = 10;
                }

                runs[count++] = end - x;

                // Eleven runs ending in a dark one start with a dark one.
                if (count == 11 && dark && IsBullseye(runs, MinRing))
                {
                    var centreStart = end - Sum(runs, 5, 6);
                    var centreX = centreStart + (runs[5] / 2f);
                    Confirm(image, luminance, centreX, y + 0.5f, Sum(runs, 1, 9));
                }

                x = end;
            }
        }

        _candidates.Sort(static (a, b) => b.Seen.CompareTo(a.Seen));

        static int Sum(int[] runs, int from, int count)
        {
            var sum = 0;
            for (var i = from; i < from + count; i++)
            {
                sum += runs[i];
            }

            return sum;
        }
    }

    /// <summary>The narrowest ring looked for, in pixels: a module of two.</summary>
    private const float MinRing = 1.5f;

    /// <summary>
    /// Whether eleven runs, dark first, are in the bullseye's proportions: ten rings of one width, the light centre
    /// half as wide again. Each ring within half its width, the centre within its own; and the rings at least
    /// <paramref name="minRing"/> steps wide.
    /// </summary>
    /// <remarks>
    /// The outer ring has data modules a tenth of a module beyond it, which run into it once blurred - one, or along a
    /// column two or three - so its runs only have to be there, and the ring width is taken from the nine inside them.
    /// </remarks>
    private static bool IsBullseye(ReadOnlySpan<int> runs, float minRing)
    {
        var total = 0;
        for (var i = 1; i < 10; i++)
        {
            total += runs[i];
        }

        var ring = total / InnerRings;
        if (ring < minRing)
        {
            return false;
        }

        for (var i = 1; i < 10; i++)
        {
            var expected = i == 5 ? 1.52f * ring : ring;
            if (MathF.Abs(runs[i] - expected) > (i == 5 ? expected : 0.5f * expected) + 0.5f)
            {
                return false;
            }
        }

        return runs[0] >= (0.5f * ring) - 0.5f && runs[10] >= (0.5f * ring) - 0.5f;
    }

    /// <summary>
    /// Confirms a bullseye a row found at (<paramref name="x"/>, <paramref name="y"/>): the same eleven runs down the
    /// column and along both diagonals through its centre, the centre refined to the middle of each.
    /// </summary>
    private void Confirm(BbBitMatrix image, in BbLuminanceView luminance, float x, float y, int rowInner)
    {
        // No run inside the outer ring is much longer than the nine inner runs the row found together: the measure across
        // has to agree with them to three tenths, and no diameter may be more than 1.6 times another. A run twice as long
        // is refused below whatever else it measures, so a walk gives up on one there rather than following it - down a
        // light column through every centre of an image of stripes, which costs the square of its height, or along a
        // light line through every centre of a lattice.
        var limit = 2 * rowInner;
        if (Measure(image, luminance, x, y, 0, 1, limit) is not ({ } down, var downShift))
        {
            return;
        }

        y += downShift;
        if (Measure(image, luminance, x, y, 1, 0, limit) is not ({ } across, var acrossShift))
        {
            return;
        }

        x += acrossShift;
        if (Measure(image, luminance, x, y, 1, 1, limit) is not ({ } falling, _) || Measure(image, luminance, x, y, 1, -1, limit) is not ({ } rising, _))
        {
            return;
        }

        // A circle seen at an angle is an ellipse, but not one whose diameters differ by more than half.
        var (shortest, longest) = (MathF.Min(MathF.Min(across, down), MathF.Min(falling, rising)), MathF.Max(MathF.Max(across, down), MathF.Max(falling, rising)));
        // And the row that found it agrees with the measure across: the nine inner runs reach the outer ring's inner edge.
        if (longest > 1.6f * shortest || MathF.Abs((2 * across * RingRadii[4] / RingRadii[5]) - rowInner) > rowInner * 0.3f)
        {
            return;
        }

        // The same centre found again, to within half a module: a line through rings near the bullseye's but not its own
        // can be taken for it, and must not take its place.
        for (var i = 0; i < _candidates.Count; i++)
        {
            var known = _candidates[i];
            if (MathF.Abs(known.X - x) < known.Across / 10 && MathF.Abs(known.Y - y) < known.Down / 10)
            {
                _candidates[i] = known with { Seen = known.Seen + 1 };
                return;
            }
        }

        if (_candidates.Count < _candidates.Capacity)
        {
            _candidates.Add(new Bullseye(x, y, across, down, falling, rising));
        }
    }

    /// <summary>
    /// Walks out from (<paramref name="x"/>, <paramref name="y"/>) both ways along (<paramref name="dx"/>,
    /// <paramref name="dy"/>) across the bullseye's eleven runs, and gives the outer ring's radius in pixels and how
    /// far along the direction the middle of the centre lies from the starting point; <see langword="null"/> when the
    /// runs are not the bullseye's.
    /// </summary>
    /// <remarks>
    /// Both come from ten edges of the rings, not the outer two alone, by least squares - which matters, as the
    /// outermost modules are four times as far from the centre as the outer ring. The outer ring's own outer edge is
    /// left out: data modules run into it. In the binarised image an edge is where it is to within half a step; from
    /// the luminance, where it crosses halfway between the rings' dark and light, to a fraction of that. A fit the
    /// edges stray from by more than a third of a ring is not the bullseye's: a line through the rings off their
    /// centre can cross runs near enough the right widths. No run is followed further than <paramref name="limit"/> steps.
    /// </remarks>
    private static (float? Radius, float Shift) Measure(BbBitMatrix image, in BbLuminanceView luminance, float x, float y, int dx, int dy, int limit)
    {
        Span<int> forward = stackalloc int[6];
        Span<int> backward = stackalloc int[6];
        var (cx, cy) = ((int)x, (int)y);
        if (!Inside(image, cx, cy) || image[cx, cy] || !Walk(image, cx, cy, dx, dy, limit, forward) || !Walk(image, cx, cy, -dx, -dy, limit, backward))
        {
            return (null, 0);
        }

        // The centre run is counted from the starting pixel both ways, so it is once too many.
        Span<int> runs = stackalloc int[11];
        for (var i = 0; i < 5; i++)
        {
            runs[i] = backward[5 - i];
            runs[10 - i] = forward[5 - i];
        }

        runs[5] = forward[0] + backward[0] - 1;
        var length = MathF.Sqrt((dx * dx) + (dy * dy));
        if (!IsBullseye(runs, MinRing / length))
        {
            return (null, 0);
        }

        // Edge k, in steps from the starting pixel, forward and back: between the last pixel of the runs to it and the
        // next, half a step short of them in the binarised image.
        var subPixel = !luminance.IsEmpty && luminance.Width == image.Width && luminance.Height == image.Height;
        var (threshold, lightIsBrighter) = subPixel ? LineThreshold(luminance, cx, cy, dx, dy, forward, backward) : (0, true);
        Span<float> ahead = stackalloc float[5];
        Span<float> behind = stackalloc float[5];
        var (front, back) = (0, 0);
        for (var k = 0; k < 5; k++)
        {
            front += forward[k];
            back += backward[k];
            // Outwards from run k to run k + 1, which is the light of the rings when k is odd.
            var rises = (k % 2 == 1) == lightIsBrighter;
            ahead[k] = subPixel ? Crossing(luminance, cx, cy, dx, dy, front - 1, 1, threshold, rises) : front - 0.5f;
            behind[k] = subPixel ? Crossing(luminance, cx, cy, dx, dy, 1 - back, -1, threshold, rises) : 0.5f - back;
        }

        // At c + s r(k) forward and c - s r(k) back, for the centre c and the scale s.
        float reach = 0, spread = 0, middle = 0;
        for (var k = 0; k < 5; k++)
        {
            spread += (ahead[k] - behind[k]) * RingRadii[k];
            middle += (ahead[k] + behind[k]) / 2;
            reach += RingRadii[k] * RingRadii[k];
        }

        var (scale, centre) = (spread / (2 * reach), middle / 5);
        float residual = 0;
        for (var k = 0; k < 5; k++)
        {
            var (along, against) = (ahead[k] - centre - (scale * RingRadii[k]), behind[k] - centre + (scale * RingRadii[k]));
            residual += (along * along) + (against * against);
        }

        var ring = scale * (RingRadii[1] - RingRadii[0]);
        return residual / 10 > MaxStray * MaxStray * ring * ring ? (null, 0) : (scale * RingRadii[^1] * length, centre * length);

        static bool Walk(BbBitMatrix image, int x, int y, int dx, int dy, int limit, Span<int> runs)
        {
            // The centre's light, then five runs of the rings: dark, light, dark, light, dark. The outer ring is only
            // asked to be there, so one longer than the limit is cut short rather than refused.
            for (var run = 0; run < 6; run++)
            {
                var dark = run % 2 == 1;
                var length = 0;
                while (Inside(image, x, y) && image[x, y] == dark && length <= limit)
                {
                    length++;
                    (x, y) = (x + dx, y + dy);
                }

                if (length == 0 || (run < 5 && (length > limit || !Inside(image, x, y))))
                {
                    return false;
                }

                runs[run] = length;
            }

            return true;
        }
    }

    /// <summary>How far, in ring widths, the edges may stray from the radii fitted to them, as a root mean square.</summary>
    private const float MaxStray = 1 / 3f;

    /// <summary>
    /// Halfway between the luminance of the rings' dark and of their light, along a line through the bullseye, from the
    /// middle of each run on it but the outer ring's; and whether the light is the brighter, which it is but in an
    /// inverted symbol.
    /// </summary>
    private static (float Threshold, bool LightIsBrighter) LineThreshold(
        in BbLuminanceView luminance, int x, int y, int dx, int dy, ReadOnlySpan<int> forward, ReadOnlySpan<int> backward)
    {
        float dark = 0, light = Pixel(luminance, x + ((forward[0] - backward[0]) / 2 * dx), y + ((forward[0] - backward[0]) / 2 * dy));
        var (front, back) = (forward[0], backward[0]);
        for (var run = 1; run < 5; run++)
        {
            var (ahead, behind) = (front + (forward[run] / 2), back + (backward[run] / 2));
            var sum = Pixel(luminance, x + (ahead * dx), y + (ahead * dy)) + Pixel(luminance, x - (behind * dx), y - (behind * dy));
            if (run % 2 == 1)
            {
                dark += sum;
            }
            else
            {
                light += sum;
            }

            (front, back) = (front + forward[run], back + backward[run]);
        }

        return (((dark / 4) + (light / 5)) / 2, light / 5 > dark / 4);
    }

    /// <summary>
    /// Where, in steps from (<paramref name="x"/>, <paramref name="y"/>), the luminance crosses
    /// <paramref name="threshold"/> next to step <paramref name="from"/> going <paramref name="direction"/>, upwards
    /// when it <paramref name="rises"/>: between it and the next if it crosses there, else a step either side - the
    /// other edges of rings a step or two wide cross the other way - and half a step on when it crosses nowhere near.
    /// </summary>
    private static float Crossing(in BbLuminanceView luminance, int x, int y, int dx, int dy, int from, int direction, float threshold, bool rises)
    {
        foreach (var shift in (ReadOnlySpan<int>)[0, -1, 1])
        {
            var near = from + (direction * shift);
            var far = near + direction;
            var (a, b) = (Pixel(luminance, x + (near * dx), y + (near * dy)), Pixel(luminance, x + (far * dx), y + (far * dy)));
            if (a != b && (b > a) == rises && (a - threshold) * (b - threshold) <= 0)
            {
                return near + (direction * (threshold - a) / (b - a));
            }
        }

        return from + (direction * 0.5f);
    }

    /// <summary>The luminance of a pixel, the nearest inside the image for one outside it.</summary>
    private static float Pixel(in BbLuminanceView luminance, int x, int y)
        => luminance[Math.Clamp(x, 0, luminance.Width - 1), Math.Clamp(y, 0, luminance.Height - 1)];

    private static bool Inside(BbBitMatrix image, int x, int y) => (uint)x < (uint)image.Width && (uint)y < (uint)image.Height;

    /// <summary>
    /// Reads the symbol round a bullseye: the ellipse its four diameters make gives the module grid but its rotation,
    /// the orientation patterns the rotation, and then the data modules are sampled and decoded.
    /// </summary>
    private static BbSymbolDecodeResult? TryRead(BbBitMatrix image, in BbLuminanceView luminance, Bullseye bullseye, bool inverted)
    {
        if (Shape(bullseye) is not { } shape)
        {
            return null;
        }

        var centre = new Vector2(bullseye.X, bullseye.Y);
        var threshold = luminance.IsEmpty ? 0 : Threshold(luminance, centre, shape);

        // Every rotation a degree at a time. The orientation modules lie within six modules of the centre, so a few
        // degrees either side of the true rotation match them as well: the middle of the widest run of best scores is
        // taken, and the grid read there and a degree or two either side of it, the Reed-Solomon check the judge of
        // which is right.
        Span<int> scores = stackalloc int[360];
        var bestScore = 0;
        for (var degrees = 0; degrees < 360; degrees++)
        {
            scores[degrees] = OrientationScore(image, luminance, centre, shape, degrees, threshold, inverted);
            bestScore = Math.Max(bestScore, scores[degrees]);
        }

        if (bestScore < MinOrientationMatches || Plateau(scores, bestScore) is not { } middle)
        {
            return null;
        }

        // The rings give the scale to a per cent or two, and the outermost modules are seventeen from the centre: the grid
        // is read a little larger and smaller too.
        BbDecoderResult? payload = null;
        var (grid, perspective) = (Matrix3x2.Identity, Vector2.Zero);
        Span<int> codewords = stackalloc int[BbMaxiCodeDecoder.Codewords];
        foreach (var scale in Scales)
        {
            foreach (var offset in AngleOffsets)
            {
                grid = Matrix3x2.CreateScale(scale) * Grid(shape, middle + offset);
                if (Read(image, luminance, centre, grid, perspective, threshold, inverted, codewords) && BbMaxiCodeDecoder.Decode(codewords) is { } read)
                {
                    payload = read;
                    break;
                }
            }

            if (payload is not null)
            {
                break;
            }
        }

        if (payload is null && !luminance.IsEmpty)
        {
            var radius = (bullseye.Across + bullseye.Down + bullseye.Falling + bullseye.Rising) / 4;
            (payload, grid, perspective) = ReadForeshortened(image, luminance, centre, Grid(shape, middle), threshold, inverted, radius, codewords);
        }

        if (payload is null)
        {
            return null;
        }

        // The grid's own corners, clockwise from its top-left: half a module beside the outermost modules' centres, and
        // a hexagon's corner above and below them.
        var (top, bottom) = (-((16 * Pitch) + HexRadius), (16 * Pitch) + HexRadius);
        BbScanPoint Corner(float u, float v)
        {
            var point = Map(centre, grid, perspective, new Vector2(u, v));
            return new BbScanPoint(point.X, point.Y);
        }

        return new BbSymbolDecodeResult(
            payload,
            BbBarcodeFormat.MaxiCode,
            [Corner(-14.5f, top), Corner(16f, top), Corner(16f, bottom), Corner(-14.5f, bottom)]);
    }

    /// <summary>The rotations, from the middle of the best orientation scores, the grid is read at, in degrees.</summary>
    private static readonly float[] AngleOffsets = [0, 1, -1, 2, -2, 3, -3];

    /// <summary>The scales, of the one the rings give, the grid is read at.</summary>
    private static readonly float[] Scales = [1, 0.98f, 1.02f, 0.96f, 1.04f];

    /// <summary>
    /// The middle of the widest run of rotations that score <paramref name="best"/>, the run taken round from 359
    /// degrees to 0; <see langword="null"/> when there are two runs as wide, which is a symbol no rotation fits.
    /// </summary>
    private static float? Plateau(ReadOnlySpan<int> scores, int best)
    {
        // Start the walk where the score is not the best, so that no run is split across 0 degrees.
        var start = scores.IndexOfAnyExcept(best);
        if (start < 0)
        {
            return null;
        }

        var (widest, widestStart, tie, run) = (0, 0, false, 0);
        for (var i = 1; i <= 360; i++)
        {
            var degrees = (start + i) % 360;
            if (scores[degrees] == best)
            {
                run++;
                continue;
            }

            if (run > widest)
            {
                (widest, widestStart, tie) = (run, degrees - run, false);
            }
            else if (run == widest && run > 0)
            {
                tie = true;
            }

            run = 0;
        }

        return tie ? null : (widestStart + ((widest - 1) / 2f) + 360) % 360;
    }

    /// <summary>The most a symbol seen at an angle is foreshortened: how much larger or smaller than at the centre its outermost modules are.</summary>
    private const float MaxForeshortening = 0.32f;

    /// <summary>The steps either side of none the foreshortening is searched in, along each axis.</summary>
    private const int ForeshorteningSteps = 4;

    /// <summary>
    /// Reads a symbol seen at an angle steep enough for its modules' size to change across it. The rings give the grid
    /// where they are; how it is foreshortened away from them is searched for, and the grid read there. A grid that
    /// lies over the modules' centres samples each well inside its dark or its light, so the foreshortening taken is
    /// the one whose samples lie furthest from the threshold in all: on a coarse lattice, then a finer one round the
    /// best.
    /// </summary>
    private static (BbDecoderResult? Payload, Matrix3x2 Grid, Vector2 Perspective) ReadForeshortened(
        BbBitMatrix image, in BbLuminanceView luminance, Vector2 centre, Matrix3x2 grid, float threshold, bool inverted, float radius, Span<int> codewords)
    {
        // The outermost modules are about three and a half outer ring radii from the centre.
        var step = MaxForeshortening / (3.5f * radius * ForeshorteningSteps);
        var (best, bestContrast) = (Vector2.Zero, Contrast(luminance, centre, grid, Vector2.Zero, threshold));
        foreach (var (around, spacing, steps) in (ReadOnlySpan<(bool, float, int)>)[(false, step, ForeshorteningSteps), (true, step / 4, 3)])
        {
            var middle = around ? best : Vector2.Zero;
            for (var i = -steps; i <= steps; i++)
            {
                for (var j = -steps; j <= steps; j++)
                {
                    var perspective = middle + new Vector2(i * spacing, j * spacing);
                    var contrast = Contrast(luminance, centre, grid, perspective, threshold);
                    if (contrast > bestContrast)
                    {
                        (best, bestContrast) = (perspective, contrast);
                    }
                }
            }
        }

        foreach (var scale in (ReadOnlySpan<float>)[1, 0.98f, 1.02f])
        {
            foreach (var offset in (ReadOnlySpan<float>)[0, 1, -1])
            {
                var turned = Matrix3x2.CreateScale(scale) * Matrix3x2.CreateRotation(offset * MathF.PI / 180) * grid;
                if (Read(image, luminance, centre, turned, best, threshold, inverted, codewords) && BbMaxiCodeDecoder.Decode(codewords) is { } read)
                {
                    return (read, turned, best);
                }
            }
        }

        return (null, grid, best);
    }

    /// <summary>
    /// How far, in all, the luminance at the data modules' centres lies from <paramref name="threshold"/>; a grid that
    /// runs off the image scores nothing.
    /// </summary>
    private static float Contrast(in BbLuminanceView luminance, Vector2 centre, Matrix3x2 grid, Vector2 perspective, float threshold)
    {
        float sum = 0;
        var map = BbMaxiCodeDecoder.ModuleMap;
        for (var bit = 0; bit < map.Length; bit++)
        {
            var point = Map(centre, grid, perspective, Module(map[bit]));
            if (point.X < 0 || point.Y < 0 || point.X >= luminance.Width - 1 || point.Y >= luminance.Height - 1)
            {
                return 0;
            }

            sum += MathF.Abs(Luminance(luminance, point) - threshold);
        }

        return sum;
    }

    /// <summary>
    /// Where the point <paramref name="module"/> of the grid, in module widths from the bullseye's centre, lies in the
    /// image: the grid about the centre, a point <c>x</c> pixels from it then foreshortened to <c>x / (1 + w.x)</c>.
    /// </summary>
    private static Vector2 Map(Vector2 centre, Matrix3x2 grid, Vector2 perspective, Vector2 module)
    {
        var offset = Vector2.Transform(module, grid);
        return centre + (offset / (1 + Vector2.Dot(perspective, offset)));
    }

    /// <summary>
    /// Samples the 864 data modules of the grid into <paramref name="codewords"/>, six bits each; <see langword="false"/>
    /// when the grid runs off the image.
    /// </summary>
    private static bool Read(
        BbBitMatrix image, in BbLuminanceView luminance, Vector2 centre, Matrix3x2 grid, Vector2 perspective, float threshold, bool inverted, Span<int> codewords)
    {
        codewords.Clear();
        var map = BbMaxiCodeDecoder.ModuleMap;
        for (var bit = 0; bit < map.Length; bit++)
        {
            if (Sample(image, luminance, Map(centre, grid, perspective, Module(map[bit])), threshold, inverted) is not { } dark)
            {
                return false;
            }

            if (dark)
            {
                codewords[bit / 6] |= 1 << (5 - (bit % 6));
            }
        }

        return true;
    }

    /// <summary>How far apart, in module widths, the rows' centres are: the height of a row of nested hexagons.</summary>
    private const float Pitch = 0.8660254f;

    /// <summary>How far a hexagon a module wide reaches from its centre to its top and bottom corners.</summary>
    private const float HexRadius = 0.5773503f;

    /// <summary>
    /// The centre of module <paramref name="module"/> from the bullseye's, in module widths: the bullseye sits on the
    /// place of the module at row 16, column 14.
    /// </summary>
    private static Vector2 Module(int module)
    {
        var (row, column) = (module / BbMaxiCodeDecoder.Columns, module % BbMaxiCodeDecoder.Columns);
        return new Vector2(column - 14 + ((row & 1) == 1 ? 0.5f : 0), (row - 16) * Pitch);
    }

    /// <summary>
    /// The shape of the module grid in the image but for its rotation: the matrix that takes the outer ring's circle,
    /// in module widths, onto the ellipse the four measured radii lie on. <see langword="null"/> for radii no ellipse
    /// fits.
    /// </summary>
    private static Matrix3x2? Shape(Bullseye bullseye)
    {
        // The ellipse x^T M x = 1, from its radius in four directions: across, down, and the two diagonals.
        var a = 1 / (bullseye.Across * bullseye.Across);
        var c = 1 / (bullseye.Down * bullseye.Down);
        var b = ((1 / (bullseye.Falling * bullseye.Falling)) - (1 / (bullseye.Rising * bullseye.Rising))) / 2;
        var determinant = (a * c) - (b * b);
        if (a <= 0 || c <= 0 || determinant <= 0)
        {
            return null;
        }

        // M to the power -1/2, through its eigenvalues, scaled so the outer ring's radius in modules lands on it.
        var mean = (a + c) / 2;
        var spread = MathF.Sqrt((((a - c) / 2) * ((a - c) / 2)) + (b * b));
        var (large, small) = (mean + spread, mean - spread);
        if (small <= 0)
        {
            return null;
        }

        var angle = MathF.Atan2(2 * b, a - c) / 2;
        var (sin, cos) = MathF.SinCos(angle);
        var (alongLarge, alongSmall) = (1 / MathF.Sqrt(large) / RingRadii[^1], 1 / MathF.Sqrt(small) / RingRadii[^1]);

        // R diag(1/sqrt(large), 1/sqrt(small)) R^T, rows as System.Numerics transforms them.
        var m11 = (cos * cos * alongLarge) + (sin * sin * alongSmall);
        var m22 = (sin * sin * alongLarge) + (cos * cos * alongSmall);
        var m12 = cos * sin * (alongLarge - alongSmall);
        return new Matrix3x2(m11, m12, m12, m22, 0, 0);
    }

    /// <summary>The module grid turned by <paramref name="degrees"/>, then given the shape.</summary>
    private static Matrix3x2 Grid(Matrix3x2 shape, float degrees) => Matrix3x2.CreateRotation(degrees * MathF.PI / 180) * shape;

    private static int OrientationScore(
        BbBitMatrix image, in BbLuminanceView luminance, Vector2 centre, Matrix3x2 shape, float degrees, float threshold, bool inverted)
    {
        var grid = Grid(shape, degrees);
        var score = 0;
        foreach (var (module, dark) in Orientation)
        {
            if (Sample(image, luminance, centre + Vector2.Transform(Module(module), grid), threshold, inverted) == dark)
            {
                score++;
            }
        }

        return score;
    }

    /// <summary>
    /// The luminance halfway between the bullseye's dark rings and the light between them, sampled round the middle of
    /// each, for sampling modules from luminance.
    /// </summary>
    private static float Threshold(in BbLuminanceView luminance, Vector2 centre, Matrix3x2 shape)
    {
        float dark = 0, light = 0;
        for (var ring = 0; ring < 5; ring++)
        {
            var radius = (RingRadii[ring] + RingRadii[ring + 1]) / 2;
            for (var step = 0; step < 8; step++)
            {
                var (sin, cos) = MathF.SinCos(step * MathF.PI / 4);
                var point = centre + Vector2.Transform(new Vector2(cos * radius, sin * radius), shape);
                var value = Luminance(luminance, point);
                if (ring % 2 == 0)
                {
                    dark += value;
                }
                else
                {
                    light += value;
                }
            }
        }

        return ((dark / 24) + (light / 16)) / 2;
    }

    /// <summary>Whether the module centred at <paramref name="point"/> is dark; <see langword="null"/> outside the image.</summary>
    private static bool? Sample(BbBitMatrix image, in BbLuminanceView luminance, Vector2 point, float threshold, bool inverted)
    {
        if (point.X < 0 || point.Y < 0 || point.X >= image.Width - 1 || point.Y >= image.Height - 1)
        {
            return null;
        }

        if (luminance.IsEmpty)
        {
            return image[(int)point.X, (int)point.Y];
        }

        return Luminance(luminance, point) < threshold != inverted;
    }

    /// <summary>
    /// The luminance at <paramref name="point"/>, interpolated between the four pixels round it: the point is measured
    /// from the pixels' edges, so a pixel's centre is half a pixel in.
    /// </summary>
    private static float Luminance(in BbLuminanceView luminance, Vector2 point) => luminance.Interpolate(point.X - 0.5f, point.Y - 0.5f);
}
