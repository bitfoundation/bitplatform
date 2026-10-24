using System.Numerics;

namespace Bit.BbScanner;

/// <summary>
/// Finds a Micro QR symbol round one of the finder patterns the QR Code finder search has found, and samples its grid.
/// </summary>
/// <remarks>
/// <para>
/// A Micro QR symbol has one finder pattern, QR Code's, in its top left corner; so the centres the QR Code search found
/// - which a QR symbol needs three of - are where one is looked for when no QR symbol was read. A finder pattern is
/// seven modules square, and rays cast from its centre leave its outer dark ring along the sides of that square: the
/// distances at which they do give its size and its angle, to within a quarter turn.
/// </para>
/// <para>
/// The quarter turn, and the symbol's size, come from the timing patterns: in a Micro QR symbol they run along the
/// outer edge of its top row and its left column, from the finder pattern to the symbol's edge, dark and light in
/// turn. Of the four ways the grid can lie round the finder, only one has both of them alternate; and where they stop
/// alternating - the symbol's edge, then its quiet zone - says whether it is 11, 13, 15 or 17 modules. The module size
/// the finder gave is put right within a few per cent by how well the timing patterns line up, and the grid is then
/// sampled and handed on to be decoded.
/// </para>
/// <para>
/// A symbol seen at an angle is not square in the image, and the grid is not laid by the square: lines are fitted to
/// where the rays leave each of the finder pattern's four sides, and where they meet are its corners - four points of
/// the grid, which give the perspective it is seen in. The square is the fallback when a side has too few rays.
/// </para>
/// </remarks>
internal sealed class BbMicroQrDetector
{
    /// <summary>Most finder patterns tried in one image.</summary>
    private const int MaxFinders = 6;

    /// <summary>How many rays are cast from a finder pattern's centre.</summary>
    private const int Rays = 120;

    /// <summary>Rays within this of a corner of the finder pattern, in radians, could lie on either side and are not fitted to one.</summary>
    private const float NearCorner = (MathF.PI / 4) - (10 * MathF.PI / 180);

    private readonly float[] _distances = new float[Rays];
    private readonly bool[] _inliers = new bool[Rays];
    private readonly Vector2[] _corners = new Vector2[4];

    /// <summary>
    /// How the grid lies in the image: by the perspective the finder pattern's corners give, or, without them, by the
    /// square the rays fit - an angle and a module size. Either is scaled by <see cref="Scale"/> and turned by
    /// <see cref="Turn"/> radians about the finder pattern's centre, the grid's (3.5, 3.5), to put right the size and
    /// the angle the finder pattern gave: at two pixels a module its edges are straight in the binarised image a few
    /// degrees either side of their true angle.
    /// </summary>
    private readonly record struct Placement(BbPerspectiveTransform? Finder, Vector2 Center, float Angle, float Module, float Scale, float Turn = 0)
    {
        /// <summary>Where the point (<paramref name="u"/>, <paramref name="v"/>) of the grid, in modules from its top left corner, lies in the image.</summary>
        public Vector2 ToImage(float u, float v)
        {
            var (turnSin, turnCos) = MathF.SinCos(Turn);
            var (du, dv) = ((u - 3.5f) * Scale, (v - 3.5f) * Scale);
            var (su, sv) = (3.5f + (du * turnCos) - (dv * turnSin), 3.5f + (du * turnSin) + (dv * turnCos));
            if (Finder is not null)
            {
                var point = Finder.Transform(new BbScanPoint(su, sv));
                return new Vector2(point.X, point.Y);
            }

            var (sin, cos) = MathF.SinCos(Angle);
            var (x, y) = ((su - 3.5f) * Module, (sv - 3.5f) * Module);
            return new Vector2(Center.X + (x * cos) - (y * sin), Center.Y + (x * sin) + (y * cos));
        }
    }

    /// <summary>
    /// Offers the grid of every Micro QR symbol found round <paramref name="centers"/> to <paramref name="accept"/>,
    /// sampled from <paramref name="image"/> and, failing that, from <paramref name="luminance"/> when there is one.
    /// </summary>
    public BbDetectorResult? Detect(
        BbBitMatrix image, ReadOnlySpan<BbQrFinderPattern> centers, Func<BbDetectorResult, bool> accept,
        in BbLuminanceView luminance, bool hasLuminance, bool inverted)
    {
        var tried = 0;
        foreach (var center in centers)
        {
            if (tried++ >= MaxFinders)
            {
                break;
            }

            if (Square(image, center) is not { } square)
            {
                continue;
            }

            // The perspective the corners give first, at each quarter turn, then the square if the rays fit one.
            var hasCorners = Corners(center, square.Angle, square.Module);
            foreach (var perspective in (ReadOnlySpan<bool>)[true, false])
            {
                if (perspective ? !hasCorners : !square.Fits)
                {
                    continue;
                }

                for (var turn = 0; turn < 4; turn++)
                {
                    var finder = perspective
                        ? BbPerspectiveTransform.QuadrilateralToQuadrilateral(
                            0, 0, 7, 0, 7, 7, 0, 7,
                            _corners[turn].X, _corners[turn].Y, _corners[(turn + 1) % 4].X, _corners[(turn + 1) % 4].Y,
                            _corners[(turn + 2) % 4].X, _corners[(turn + 2) % 4].Y, _corners[(turn + 3) % 4].X, _corners[(turn + 3) % 4].Y)
                        : null;
                    var placement = new Placement(finder, new Vector2(center.X, center.Y), square.Angle + (turn * MathF.PI / 2), square.Module, 1);
                    if (Timing(image, placement) is not { } timing)
                    {
                        continue;
                    }

                    if (Sample(image, timing.Placement, timing.Dimension, accept, luminance, hasLuminance, inverted) is { } result)
                    {
                        return result;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// The angle, within a quarter turn, and the module size of the finder pattern round <paramref name="center"/>:
    /// the square whose sides best fit where rays from its centre leave its outer dark ring; and whether they fit it
    /// well enough to lay the grid by, which a finder pattern seen at a steep angle does not.
    /// </summary>
    private (float Angle, float Module, bool Fits)? Square(BbBitMatrix image, BbQrFinderPattern center)
    {
        var limit = 8 * center.EstimatedModuleSize;
        for (var ray = 0; ray < Rays; ray++)
        {
            var (sin, cos) = MathF.SinCos(ray * 2 * MathF.PI / Rays);
            if (Leave(image, center.X, center.Y, cos, sin, limit) is not { } distance)
            {
                return null;
            }

            _distances[ray] = distance;
            _inliers[ray] = true;
        }

        // A ray that slips out of the ring early - through a corner blurred light - is left out of a second fit, as long
        // as few do: those more than four tenths of a module off the first.
        var (angle, half, error, count) = Fit();
        var outliers = 0;
        for (var ray = 0; ray < Rays; ray++)
        {
            if (MathF.Abs(_distances[ray] - (half * Shape(ray, angle))) > MathF.Max(0.4f * half / 3.5f, 1.5f))
            {
                _inliers[ray] = false;
                outliers++;
            }
        }

        if (outliers > 0 && outliers <= Rays / 8)
        {
            (angle, half, error, count) = Fit();
        }

        // A finder pattern is seven modules across, the outer edge of its dark ring three and a half from the centre.
        // Rays leave the ring within a fifth of a module of a square's sides - or within a pixel at small sizes.
        var module = half / 3.5f;
        var fits = outliers <= Rays / 8 && error / count <= MathF.Max(module * module * 0.04f, 0.5f);
        return module < 1 ? null : (angle * MathF.PI / 180, module, fits);
    }

    /// <summary>
    /// Fills <see cref="_corners"/> with the finder pattern's four outer corners, clockwise from the top left of the
    /// square at <paramref name="angle"/>: where lines fitted to the points the rays left each side at meet. Each line
    /// is fitted twice, the second time without the points more than a third of a module off the first - rays that
    /// slipped out early. <see langword="false"/> when a side has too few rays, or the corners are not near the
    /// square's.
    /// </summary>
    private bool Corners(BbQrFinderPattern center, float angle, float module)
    {
        Span<Vector2> through = stackalloc Vector2[4];
        Span<Vector2> along = stackalloc Vector2[4];

        // For each side - right, bottom, left and top of the square - the sums a line is fitted from.
        Span<float> sums = stackalloc float[4 * 6];
        for (var pass = 0; pass < 2; pass++)
        {
            sums.Clear();
            for (var ray = 0; ray < Rays; ray++)
            {
                if (Point(ray) is not var (point, side))
                {
                    continue;
                }

                if (pass == 1)
                {
                    var off = point - through[side];
                    if (MathF.Abs((off.X * along[side].Y) - (off.Y * along[side].X)) > MathF.Max(module / 3, 1))
                    {
                        continue;
                    }
                }

                var sum = sums.Slice(side * 6, 6);
                sum[0]++;
                sum[1] += point.X;
                sum[2] += point.Y;
                sum[3] += point.X * point.X;
                sum[4] += point.X * point.Y;
                sum[5] += point.Y * point.Y;
            }

            // Each side's line through the mean of its points, along their principal direction.
            for (var side = 0; side < 4; side++)
            {
                var sum = sums.Slice(side * 6, 6);
                if (sum[0] < 6)
                {
                    return false;
                }

                var (mx, my) = (sum[1] / sum[0], sum[2] / sum[0]);
                var (xx, xy, yy) = ((sum[3] / sum[0]) - (mx * mx), (sum[4] / sum[0]) - (mx * my), (sum[5] / sum[0]) - (my * my));
                var (sin, cos) = MathF.SinCos(MathF.Atan2(2 * xy, xx - yy) / 2);
                (through[side], along[side]) = (new Vector2(mx, my), new Vector2(cos, sin));
            }
        }

        // Top and left, top and right, right and bottom, bottom and left.
        var reach = 3.5f * MathF.Sqrt(2) * module;
        var centre = new Vector2(center.X, center.Y);
        ReadOnlySpan<(int, int)> meetings = [(3, 2), (3, 0), (0, 1), (1, 2)];
        for (var corner = 0; corner < 4; corner++)
        {
            var (a, b) = meetings[corner];
            var cross = (along[a].X * along[b].Y) - (along[a].Y * along[b].X);
            if (MathF.Abs(cross) < 0.5f)
            {
                return false;
            }

            var between = through[b] - through[a];
            var t = ((between.X * along[b].Y) - (between.Y * along[b].X)) / cross;
            _corners[corner] = through[a] + (t * along[a]);
            var distance = Vector2.Distance(_corners[corner], centre);
            if (distance < 0.6f * reach || distance > 1.5f * reach)
            {
                return false;
            }
        }

        return true;

        // Where ray r left the ring, and which side of the square that is; nothing for a ray within ten degrees of a
        // corner, which could be on either.
        (Vector2 Point, int Side)? Point(int ray)
        {
            var direction = ray * 2 * MathF.PI / Rays;
            var relative = direction - angle;
            relative -= MathF.Floor(relative / (2 * MathF.PI)) * 2 * MathF.PI;
            var quarter = MathF.Round(relative / (MathF.PI / 2));
            if (MathF.Abs(relative - (quarter * MathF.PI / 2)) > NearCorner)
            {
                return null;
            }

            var (sin, cos) = MathF.SinCos(direction);
            return (new Vector2(center.X + (_distances[ray] * cos), center.Y + (_distances[ray] * sin)), (int)quarter % 4);
        }
    }

    /// <summary>
    /// The square that best fits the rays kept: a square of half side h at angle a is h / max(|cos|, |sin|) from its
    /// centre in every direction, the best a found a degree at a time, then a tenth either side, h the least squares
    /// fit at each. Its angle in degrees, half side, summed squared error and the rays it was fitted to.
    /// </summary>
    private (float Angle, float Half, float Error, int Count) Fit()
    {
        var best = (Angle: 0f, Half: 0f, Error: float.MaxValue, Count: 0);
        for (var tenth = 0; tenth < 900; tenth += 10)
        {
            Try(tenth / 10f);
        }

        var coarse = best.Angle;
        for (var tenth = -10; tenth <= 10; tenth++)
        {
            Try(coarse + (tenth / 10f));
        }

        return best;

        void Try(float degrees)
        {
            float sumDistance = 0, sumShapeSquared = 0, sumDistanceSquared = 0;
            var count = 0;
            for (var ray = 0; ray < Rays; ray++)
            {
                if (!_inliers[ray])
                {
                    continue;
                }

                var shape = Shape(ray, degrees);
                sumDistance += _distances[ray] * shape;
                sumShapeSquared += shape * shape;
                sumDistanceSquared += _distances[ray] * _distances[ray];
                count++;
            }

            var half = sumDistance / sumShapeSquared;
            var error = sumDistanceSquared - (2 * half * sumDistance) + (half * half * sumShapeSquared);
            if (error < best.Error)
            {
                best = (degrees, half, error, count);
            }
        }
    }

    /// <summary>How far from its centre, in half sides, a square at <paramref name="degrees"/> reaches along ray <paramref name="ray"/>.</summary>
    private static float Shape(int ray, float degrees)
    {
        var (sin, cos) = MathF.SinCos(((ray * 360f / Rays) - degrees) * MathF.PI / 180);
        return 1 / MathF.Max(MathF.Abs(cos), MathF.Abs(sin));
    }

    /// <summary>
    /// How far along (<paramref name="dx"/>, <paramref name="dy"/>) from (<paramref name="x"/>, <paramref name="y"/>)
    /// the finder pattern's outer dark ring ends: past the dark centre, the light ring and the dark ring.
    /// </summary>
    private static float? Leave(BbBitMatrix image, float x, float y, float dx, float dy, float limit)
    {
        var changes = 0;
        var dark = true;
        for (var distance = 0f; distance < limit; distance += 0.5f)
        {
            var (px, py) = ((int)(x + (dx * distance)), (int)(y + (dy * distance)));
            if ((uint)px >= (uint)image.Width || (uint)py >= (uint)image.Height)
            {
                return null;
            }

            if (image[px, py] != dark)
            {
                dark = !dark;
                if (++changes == 3)
                {
                    return distance;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// The symbol's size, and the placement of its grid, when it lies as <paramref name="placement"/> has it round the
    /// finder pattern: the size whose timing patterns, along the top row and left column, alternate best, with the
    /// quiet zone beyond them, at the module size within six per cent of the finder's and the angle within three
    /// degrees that line them up best; <see langword="null"/> when no size has both match nine modules in ten.
    /// </summary>
    private static (int Dimension, Placement Placement)? Timing(BbBitMatrix image, Placement placement)
    {
        var (best, bestScore, bestPlacement) = (0, 0f, placement);
        foreach (var (scale, degrees) in TimingSearch)
        {
            var scaled = placement with { Scale = scale, Turn = degrees * MathF.PI / 180 };
            foreach (var dimension in (ReadOnlySpan<int>)[11, 13, 15, 17])
            {
                // Modules 8 to the last of the top row and left column alternate, the even ones dark, and the two
                // beyond the symbol's edge are its quiet zone. A size whose modules would lie outside the image is not
                // this symbol's; a quiet zone cut off by the edge of the image cannot be checked, and is not.
                var (matches, total) = (0, 0);
                for (var i = 8; i < dimension + 2 && total >= 0; i++)
                {
                    var dark = i < dimension && i % 2 == 0;
                    foreach (var alongRow in (ReadOnlySpan<bool>)[true, false])
                    {
                        var (column, row) = alongRow ? (i, 0) : (0, i);
                        if (At(image, scaled, column, row) is not { } seen)
                        {
                            total = i < dimension ? -1 : total;
                            break;
                        }

                        matches += seen == dark ? 1 : 0;
                        total++;
                    }
                }

                if (total <= 0)
                {
                    continue;
                }

                var score = matches / (float)total;
                if (score > bestScore)
                {
                    (best, bestScore, bestPlacement) = (dimension, score, scaled);
                }
            }
        }

        return bestScore >= 0.9f ? (best, bestPlacement) : null;
    }

    /// <summary>The scales and turns, in degrees, the timing patterns are looked for at: the finder's own first.</summary>
    private static readonly (float Scale, float Degrees)[] TimingSearch =
    [
        (1, 0), (0.97f, 0), (1.03f, 0), (0.94f, 0), (1.06f, 0),
        (1, 1.5f), (1, -1.5f), (1, 3), (1, -3),
        (0.97f, 1.5f), (0.97f, -1.5f), (1.03f, 1.5f), (1.03f, -1.5f),
    ];

    /// <summary>Whether the module in <paramref name="column"/> and <paramref name="row"/> of the grid is dark; <see langword="null"/> outside the image.</summary>
    private static bool? At(BbBitMatrix image, Placement placement, int column, int row)
    {
        var point = placement.ToImage(column + 0.5f, row + 0.5f);
        var (x, y) = ((int)point.X, (int)point.Y);
        return (uint)x < (uint)image.Width && (uint)y < (uint)image.Height ? image[x, y] : null;
    }

    /// <summary>
    /// Samples the grid at its centres and offers it to <paramref name="accept"/>: from the binarised image, and then
    /// from the luminance when there is one.
    /// </summary>
    private static BbDetectorResult? Sample(
        BbBitMatrix image, Placement placement, int dimension, Func<BbDetectorResult, bool> accept,
        in BbLuminanceView luminance, bool hasLuminance, bool inverted)
    {
        var corners = new[]
        {
            placement.ToImage(0, 0),
            placement.ToImage(dimension, 0),
            placement.ToImage(dimension, dimension),
            placement.ToImage(0, dimension),
        };
        var transform = BbPerspectiveTransform.QuadrilateralToQuadrilateral(
            0, 0, dimension, 0, dimension, dimension, 0, dimension,
            corners[0].X, corners[0].Y, corners[1].X, corners[1].Y, corners[2].X, corners[2].Y, corners[3].X, corners[3].Y);
        var points = new BbScanPoint[4];
        for (var i = 0; i < 4; i++)
        {
            points[i] = new BbScanPoint(corners[i].X, corners[i].Y);
        }

        if (Offer(BbGridSampler.Sample(image, dimension, dimension, transform), points, accept) is { } result)
        {
            return result;
        }

        return hasLuminance
            ? Offer(BbGridSampler.SampleLuminance(luminance, dimension, dimension, transform, inverted), points, accept)
            : null;

        static BbDetectorResult? Offer(BbBitMatrix? bits, BbScanPoint[] points, Func<BbDetectorResult, bool> accept)
        {
            if (bits is null)
            {
                return null;
            }

            var result = new BbDetectorResult(bits, points);
            if (accept(result))
            {
                return result;
            }

            result.Dispose();
            return null;
        }
    }
}
