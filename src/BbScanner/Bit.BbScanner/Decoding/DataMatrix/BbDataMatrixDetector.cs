namespace Bit.BbScanner;

/// <summary>
/// Finds a Data Matrix symbol in a binarised image and samples its module grid.
/// </summary>
/// <remarks>
/// <para>
/// Data Matrix is bounded on two adjacent sides by a solid "L" and on the other two by a dashed
/// timing pattern, and that is all there is to find. The detector locates every patch of ink in
/// the frame, fits a quadrilateral to each, and identifies the L as the pair of adjacent edges
/// with the fewest colour transitions. The number of transitions along the dashed edges then
/// gives the module count directly, which is how a symbol of unknown size is measured.
/// </para>
/// <para>
/// Corners are not taken from single pixels. Each edge is refined by fitting a straight line
/// through dozens of boundary samples, and the corners are the intersections of those lines,
/// which is what makes small, rotated and perspective distorted symbols sample onto the right
/// modules. The symbol may be anywhere in the frame and at any angle.
/// </para>
/// <para>
/// When no patch yields a symbol the detector falls back to growing a rectangle out of the
/// image centre, which copes with frames so noisy that the ink patches merge into one.
/// </para>
/// </remarks>
public sealed class BbDataMatrixDetector
{
    private const int MinModules = 8;
    private const int MaxModules = 144;

    /// <summary>
    /// The depth of the first scan along every edge, in pixels. Shallow enough to stay inside
    /// the outermost row of modules at two pixels per module, so that no scan ever has to guess
    /// the module size before one has been measured.
    /// </summary>
    private const float ShallowInset = 1.5f;

    private BbBitMatrix _image;
    private readonly BbDarkRegionFinder _regionFinder;
    private readonly List<BbScanPoint> _samples;
    private readonly List<BbScanPoint> _hull;
    private readonly List<BbScanPoint> _edgeSamples;

    /// <summary>Creates a detector over a binarised image.</summary>
    /// <param name="image">The binarised image.</param>
    public BbDataMatrixDetector(BbBitMatrix image)
        : this(image, new BbDarkRegionFinder(), new BbDetectorScratch())
    {
    }

    internal BbDataMatrixDetector(BbBitMatrix image, BbDarkRegionFinder regionFinder, BbDetectorScratch scratch)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(regionFinder);
        ArgumentNullException.ThrowIfNull(scratch);
        _image = image;
        _regionFinder = regionFinder;
        _samples = scratch.Samples;
        _hull = scratch.Hull;
        _edgeSamples = scratch.EdgeSamples;
    }

    /// <summary>
    /// Points an existing detector at another image, so that continuous scanning reuses one
    /// instance rather than allocating one per frame.
    /// </summary>
    /// <param name="image">The binarised image.</param>
    public void Reset(BbBitMatrix image)
    {
        ArgumentNullException.ThrowIfNull(image);
        _image = image;
    }

    /// <summary>Detects and samples the first symbol found.</summary>
    /// <returns>The sampled grid and its corner points, or <see langword="null"/> when no symbol is found.</returns>
    public BbDetectorResult? Detect() => Detect(tryHarder: false, static _ => true);

    /// <summary>
    /// Detects and samples symbols until <paramref name="accept"/> takes one.
    /// </summary>
    /// <param name="tryHarder">Examine more candidate patches, for still images.</param>
    /// <param name="accept">Called with each sampled grid; returning <see langword="true"/> ends the search.</param>
    /// <returns>The accepted result, or <see langword="null"/>. Rejected results are disposed.</returns>
    public BbDetectorResult? Detect(bool tryHarder, Func<BbDetectorResult, bool> accept)
    {
        ArgumentNullException.ThrowIfNull(accept);

        var regions = _regionFinder.Find(_image, minCells: 4);
        var maxCandidates = tryHarder ? 8 : 3;
        var centerX = _image.Width / 2.0f;
        var centerY = _image.Height / 2.0f;

        // Candidates nearest the centre go first: that is where an aiming user puts the symbol.
        var examined = 0;
        var used = 0L;
        while (examined < maxCandidates)
        {
            var best = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < regions.Count && i < 64; i++)
            {
                if ((used & (1L << i)) != 0 || !IsPlausibleRegion(regions[i]))
                {
                    continue;
                }

                var dx = regions[i].CenterX - centerX;
                var dy = regions[i].CenterY - centerY;
                var distance = (dx * dx) + (dy * dy);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            if (best < 0)
            {
                break;
            }

            used |= 1L << best;
            examined++;

            var result = DetectInRegion(regions[best], accept);
            if (result is not null)
            {
                return result;
            }
        }

        return DetectFromCenter(accept);
    }

    private static bool IsPlausibleRegion(in BbDarkRegion region)
    {
        var width = region.Width;
        var height = region.Height;
        if (width < 10 || height < 10)
        {
            return false;
        }

        // The most elongated Data Matrix shapes are 8 by 32 and 16 by 48; anything much thinner
        // is a line of text or a linear symbol. The region is measured in whole cells, which can
        // round a 4:1 symbol past 4:1, hence the slack.
        var longer = Math.Max(width, height);
        var shorter = Math.Min(width, height);
        return longer <= shorter * 5;
    }

    private BbDetectorResult? DetectInRegion(in BbDarkRegion region, Func<BbDetectorResult, bool> accept)
    {
        var left = Math.Max(0, region.Left - 1);
        var top = Math.Max(0, region.Top - 1);
        var right = Math.Min(_image.Width - 1, region.Right + 1);
        var bottom = Math.Min(_image.Height - 1, region.Bottom + 1);

        CollectBoundarySamples(left, top, right, bottom);
        if (_samples.Count < 8)
        {
            return null;
        }

        ConvexHull(_samples, _hull);
        if (_hull.Count < 4)
        {
            return null;
        }

        Span<BbScanPoint> corners = stackalloc BbScanPoint[4];
        if (!PickCorners(_hull, corners))
        {
            return null;
        }

        return SampleQuadrilateral(corners, accept);
    }

    /// <summary>
    /// Orients, refines and samples a rough quadrilateral around a symbol, offering each
    /// plausible module count to <paramref name="accept"/> in turn.
    /// </summary>
    private BbDetectorResult? SampleQuadrilateral(Span<BbScanPoint> corners, Func<BbDetectorResult, bool> accept)
    {
        // Corners are ordered clockwise on screen: TL, TR, BR, BL for an upright symbol.
        var centroidX = (corners[0].X + corners[1].X + corners[2].X + corners[3].X) / 4;
        var centroidY = (corners[0].Y + corners[1].Y + corners[2].Y + corners[3].Y) / 4;

        // A first, shallow scan of every edge: the busiest edge is a dashed one and its
        // transition count gives a first estimate of the module pitch.
        Span<int> transitions = stackalloc int[4];
        Span<float> lengths = stackalloc float[4];
        var pitch = float.MaxValue;
        for (var i = 0; i < 4; i++)
        {
            var a = corners[i];
            var b = corners[(i + 1) & 3];
            lengths[i] = BbScanPoint.Distance(a, b);
            if (lengths[i] < 8)
            {
                return null;
            }

            transitions[i] = TransitionsAlongEdge(a, b, centroidX, centroidY, ShallowInset);
            pitch = Math.Min(pitch, lengths[i] / (transitions[i] + 1));
        }

        // Scan again half a module in, where the timing pattern is read most reliably, but never
        // deeper than four pixels: the corners are still rough and a deep scan along an
        // inaccurate edge would wander into the data region. Half the pitch keeps the scan out
        // of the data region whatever the module size.
        var inset = Math.Clamp(pitch / 2, ShallowInset, 4f);
        if (inset > ShallowInset)
        {
            for (var i = 0; i < 4; i++)
            {
                transitions[i] = TransitionsAlongEdge(corners[i], corners[(i + 1) & 3], centroidX, centroidY, inset);
            }
        }

        // The L is the pair of adjacent edges with the fewest transitions.
        var solidStart = -1;
        var solidScore = int.MaxValue;
        for (var i = 0; i < 4; i++)
        {
            var score = transitions[i] + transitions[(i + 1) & 3];
            if (score < solidScore)
            {
                solidScore = score;
                solidStart = i;
            }
        }

        var dashedA = transitions[(solidStart + 2) & 3];
        var dashedB = transitions[(solidStart + 3) & 3];
        var solidA = transitions[solidStart];
        var solidB = transitions[(solidStart + 1) & 3];
        if (Math.Max(solidA, solidB) > 3 || Math.Min(dashedA, dashedB) < 4 ||
            Math.Max(solidA, solidB) * 3 > Math.Min(dashedA, dashedB))
        {
            return null;
        }

        // Refine each edge into a line through its boundary samples, then intersect.
        Span<Line> lines = stackalloc Line[4];
        for (var i = 0; i < 4; i++)
        {
            var dashed = i != solidStart && i != ((solidStart + 1) & 3);
            if (!FitEdge(corners[i], corners[(i + 1) & 3], centroidX, centroidY, dashed, out lines[i]))
            {
                return null;
            }
        }

        Span<BbScanPoint> refined = stackalloc BbScanPoint[4];
        for (var i = 0; i < 4; i++)
        {
            if (!Line.TryIntersect(lines[(i + 3) & 3], lines[i], out refined[i]) || !IsInside(refined[i], 2))
            {
                return null;
            }
        }

        // The corner shared by the solid edges is the bottom-left of the symbol; the rest follow
        // clockwise.
        var bottomLeft = refined[(solidStart + 1) & 3];
        var topLeft = refined[(solidStart + 2) & 3];
        var topRight = refined[(solidStart + 3) & 3];
        var bottomRight = refined[solidStart];

        // Only the two dashed edges carry module information: the solid arms of the L have no
        // transitions to count, so scanning them would measure noise rather than modules.
        var columns = MeasureModules(topLeft, topRight, centroidX, centroidY);
        var rows = MeasureModules(topRight, bottomRight, centroidX, centroidY);
        if (columns < MinModules || rows < MinModules || columns > MaxModules || rows > MaxModules)
        {
            return null;
        }

        // Both edges were measured on the same symbol, so their module pitches must agree. A
        // pitch twice the other means one scan ran through the data region and counted half the
        // modules, whereas even a 45 degree tilt foreshortens one axis by under 30 percent.
        var pitchAcross = BbScanPoint.Distance(topLeft, topRight) / columns;
        var pitchDown = BbScanPoint.Distance(topRight, bottomRight) / rows;
        if (Math.Max(pitchAcross, pitchDown) > 1.4f * Math.Min(pitchAcross, pitchDown))
        {
            return null;
        }

        // Aspect ratios between 2:3 and 3:2 can only come from a square symbol measured
        // imprecisely, because no rectangular shape is that close to square.
        if (4 * columns < 6 * rows && 4 * rows < 6 * columns)
        {
            columns = rows = Math.Max(columns, rows);
        }

        Span<(int Rows, int Columns)> sizes = stackalloc (int, int)[MaxSizeCandidates];
        var sizeCount = LegalSizesNear(rows, columns, pitchDown, pitchAcross, sizes);
        for (var i = 0; i < sizeCount; i++)
        {
            (rows, columns) = sizes[i];
            var transform = BbPerspectiveTransform.QuadrilateralToQuadrilateral(
                0, 0,
                columns, 0,
                columns, rows,
                0, rows,
                topLeft.X, topLeft.Y,
                topRight.X, topRight.Y,
                bottomRight.X, bottomRight.Y,
                bottomLeft.X, bottomLeft.Y);

            var bits = BbGridSampler.Sample(_image, columns, rows, transform);
            if (bits is null)
            {
                continue;
            }

            var result = new BbDetectorResult(bits, [topLeft, bottomLeft, bottomRight, topRight]);
            if (accept(result))
            {
                return result;
            }

            result.Dispose();
        }

        return null;
    }

    /// <summary>The eight sizes two modules away from a measurement in either dimension.</summary>
    private const int MaxSizeCandidates = 8;

    /// <summary>
    /// Lists the legal symbol sizes the measured one could be, nearest first; a measured size
    /// that is itself legal is the only candidate.
    /// </summary>
    /// <remarks>
    /// Timing patterns are counted to within a module or two, which can leave the measurement
    /// between two legal sizes: 14 by 36 is as close to 12 by 36 as to 16 by 36. Equally near
    /// sizes are ordered by how well their module pitch agrees between the two measured edges,
    /// and every one of them is offered for decoding.
    /// </remarks>
    private static int LegalSizesNear(int rows, int columns, float lengthDownPerRow, float lengthAcrossPerColumn, Span<(int Rows, int Columns)> sizes)
    {
        if (BbDataMatrixVersion.GetVersionForDimensions(rows, columns) is not null)
        {
            sizes[0] = (rows, columns);
            return 1;
        }

        var lengthDown = lengthDownPerRow * rows;
        var lengthAcross = lengthAcrossPerColumn * columns;

        Span<float> costs = stackalloc float[MaxSizeCandidates];
        var count = 0;
        for (var dr = -2; dr <= 2; dr += 2)
        {
            for (var dc = -2; dc <= 2; dc += 2)
            {
                var r = rows + dr;
                var c = columns + dc;
                if (BbDataMatrixVersion.GetVersionForDimensions(r, c) is null)
                {
                    continue;
                }

                // Whole modules of error first, pitch disagreement (always under one) as the
                // tie breaker. Insertion sort keeps the list ordered as it grows.
                var pitchDown = lengthDown / r;
                var pitchAcross = lengthAcross / c;
                var cost = Math.Abs(dr) + Math.Abs(dc) + (Math.Abs(pitchDown - pitchAcross) / Math.Max(pitchDown, pitchAcross));

                var at = count++;
                while (at > 0 && costs[at - 1] > cost)
                {
                    costs[at] = costs[at - 1];
                    sizes[at] = sizes[at - 1];
                    at--;
                }

                costs[at] = cost;
                sizes[at] = (r, c);
            }
        }

        // Only the nearest sizes are worth sampling; a whole module further out is a different
        // symbol.
        var limit = 0;
        while (limit < count && costs[limit] - costs[0] < 1)
        {
            limit++;
        }

        return limit;
    }

    /// <summary>
    /// Counts the modules along a dashed edge: one more than the number of transitions seen half
    /// a module inside the symbol. The module size is unknown at first, so a shallow pass
    /// estimates it and further passes at the depths that estimate implies measure it.
    /// </summary>
    /// <remarks>
    /// The timing pattern alternates every module, so no row of the symbol has more transitions
    /// than it: the largest count any pass sees is the right one. That is what makes the passes
    /// safe to chain. A pass that rides the edge or strays into the data region undercounts,
    /// which overestimates the pitch and would send a single follow-up pass deeper still; taking
    /// the maximum instead means every pass can only shrink the pitch estimate, and an
    /// intermediate depth is tried so that a first estimate twice too large is still caught.
    /// </remarks>
    private int MeasureModules(BbScanPoint from, BbScanPoint to, float centroidX, float centroidY)
    {
        var length = BbScanPoint.Distance(from, to);
        var transitions = TransitionsAlongEdge(from, to, centroidX, centroidY, ShallowInset);
        if (transitions < 1)
        {
            return 0;
        }

        // Half a module in is the middle of the timing pattern's own row of modules: shallower
        // and the scan rides the symbol's edge, deeper and it enters the data region and counts
        // transitions that mean nothing.
        // A quarter of the estimated pitch first, in case the estimate is twice too large, then
        // half of it, and half again once the count has settled.
        var deepest = ShallowInset;
        for (var pass = 0; pass < 3; pass++)
        {
            var pitch = length / (transitions + 1);
            var inset = pass == 0 ? pitch / 4 : pitch / 2;
            if (inset < deepest + 0.5f)
            {
                continue;
            }

            deepest = inset;
            transitions = Math.Max(transitions, TransitionsAlongEdge(from, to, centroidX, centroidY, inset));
        }

        // Every Data Matrix dimension is even.
        var count = transitions + 1;
        return (count + 1) & ~1;
    }

    private int TransitionsAlongEdge(BbScanPoint a, BbScanPoint b, float centroidX, float centroidY, float inset)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var length = MathF.Sqrt((dx * dx) + (dy * dy));
        if (length <= 0)
        {
            return 0;
        }

        // Inward normal: the perpendicular pointing towards the centroid.
        var nx = -dy / length;
        var ny = dx / length;
        var midX = (a.X + b.X) / 2;
        var midY = (a.Y + b.Y) / 2;
        if (((centroidX - midX) * nx) + ((centroidY - midY) * ny) < 0)
        {
            nx = -nx;
            ny = -ny;
        }

        // Keep clear of the corners, where the neighbouring edge would add transitions.
        var trim = Math.Min(inset, length / 10);
        var ux = dx / length;
        var uy = dy / length;
        var start = new BbScanPoint(a.X + (nx * inset) + (ux * trim), a.Y + (ny * inset) + (uy * trim));
        var end = new BbScanPoint(b.X + (nx * inset) - (ux * trim), b.Y + (ny * inset) - (uy * trim));
        return TransitionsBetween(start, end);
    }

    /// <summary>
    /// Fits a line through the outer boundary of one edge of the symbol.
    /// </summary>
    /// <remarks>
    /// Along a dashed edge half of the boundary samples land on white modules and therefore one
    /// module further in. Only the outer cluster is kept, which is exactly the set that lies on
    /// the true edge.
    /// </remarks>
    private bool FitEdge(BbScanPoint a, BbScanPoint b, float centroidX, float centroidY, bool dashed, out Line line)
    {
        line = default;

        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var length = MathF.Sqrt((dx * dx) + (dy * dy));
        var ux = dx / length;
        var uy = dy / length;

        // Outward normal.
        var nx = -dy / length;
        var ny = dx / length;
        var midX = (a.X + b.X) / 2;
        var midY = (a.Y + b.Y) / 2;
        if (((centroidX - midX) * nx) + ((centroidY - midY) * ny) > 0)
        {
            nx = -nx;
            ny = -ny;
        }

        var samples = _edgeSamples;
        samples.Clear();

        var count = Math.Clamp((int)(length / 2), 8, 96);
        var outward = 4f;
        var depth = Math.Max(6f, length / 4);
        for (var i = 0; i < count; i++)
        {
            var t = 0.06f + (0.88f * i / (count - 1));
            var px = a.X + (ux * length * t);
            var py = a.Y + (uy * length * t);

            // March from outside the symbol inwards until ink is hit.
            for (var d = -outward; d <= depth; d += 0.5f)
            {
                var x = (int)MathF.Round(px - (nx * d));
                var y = (int)MathF.Round(py - (ny * d));
                if (_image.GetSafe(x, y))
                {
                    samples.Add(new BbScanPoint(x + 0.5f, y + 0.5f));
                    break;
                }
            }
        }

        if (samples.Count < 6)
        {
            return false;
        }

        if (!Line.TryFit(samples, out line))
        {
            return false;
        }

        if (dashed)
        {
            // Keep the outer cluster of the samples, measured against the first fit.
            KeepOuterCluster(samples, line, nx, ny);
            if (samples.Count < 4 || !Line.TryFit(samples, out line))
            {
                return false;
            }
        }

        // Discard outliers and refit once.
        var rms = 0f;
        foreach (var sample in samples)
        {
            var r = line.DistanceTo(sample);
            rms += r * r;
        }

        rms = MathF.Sqrt(rms / samples.Count);
        var limit = Math.Max(1f, 2.5f * rms);
        for (var i = samples.Count - 1; i >= 0; i--)
        {
            if (Math.Abs(line.DistanceTo(samples[i])) > limit)
            {
                samples.RemoveAt(i);
            }
        }

        if (samples.Count < 4 || !Line.TryFit(samples, out line))
        {
            return false;
        }

        // Samples are pixel centres; the edge itself lies half a pixel further out.
        line = line.Shift(nx * 0.5f, ny * 0.5f);
        return true;
    }

    /// <summary>
    /// Drops the samples that fell one module deep on the light modules of a dashed edge.
    /// </summary>
    /// <remarks>
    /// The samples form two clusters a module apart, each holding about half of them, so the
    /// median is a coin toss between the clusters: landing in the deep one keeps the wrong half
    /// and drags the fitted edge a module inwards. The upper quartile lies in the outer cluster
    /// whichever way the halves fall, and everything within a pixel of it is kept.
    /// </remarks>
    private static void KeepOuterCluster(List<BbScanPoint> samples, Line line, float nx, float ny)
    {
        // Signed distance along the outward normal, positive is further out.
        Span<float> distances = stackalloc float[samples.Count];
        for (var i = 0; i < samples.Count; i++)
        {
            distances[i] = ((samples[i].X - line.X) * nx) + ((samples[i].Y - line.Y) * ny);
        }

        Span<float> sorted = stackalloc float[samples.Count];
        distances.CopyTo(sorted);
        sorted.Sort();
        var upperQuartile = sorted[sorted.Length * 3 / 4];

        for (var i = samples.Count - 1; i >= 0; i--)
        {
            if (distances[i] < upperQuartile - 0.75f)
            {
                samples.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Collects the outermost ink of every row and column of the region, which is the outline
    /// the quadrilateral is fitted to.
    /// </summary>
    /// <remarks>
    /// A single dark pixel is not accepted as the edge: on a noisy frame the outermost ink of a
    /// row is far more often a speckle than the symbol, and one such sample pulls the convex
    /// hull, and with it a corner, several modules out of place. Requiring two adjacent dark
    /// pixels costs nothing and removes essentially all of them.
    /// </remarks>
    private void CollectBoundarySamples(int left, int top, int right, int bottom)
    {
        var samples = _samples;
        samples.Clear();

        for (var y = top; y <= bottom; y++)
        {
            var first = FirstRunHorizontal(left, right, y, forward: true);
            if (first < 0)
            {
                continue;
            }

            var last = FirstRunHorizontal(left, right, y, forward: false);
            samples.Add(new BbScanPoint(first, y));
            samples.Add(new BbScanPoint(first, y + 1));
            samples.Add(new BbScanPoint(last + 1, y));
            samples.Add(new BbScanPoint(last + 1, y + 1));
        }

        for (var x = left; x <= right; x++)
        {
            var first = FirstRunVertical(top, bottom, x, forward: true);
            if (first < 0)
            {
                continue;
            }

            var last = FirstRunVertical(top, bottom, x, forward: false);
            samples.Add(new BbScanPoint(x, first));
            samples.Add(new BbScanPoint(x + 1, first));
            samples.Add(new BbScanPoint(x, last + 1));
            samples.Add(new BbScanPoint(x + 1, last + 1));
        }
    }

    private int FirstRunHorizontal(int left, int right, int y, bool forward)
    {
        var step = forward ? 1 : -1;
        var from = forward ? left : right;
        var to = forward ? right : left;
        for (var x = from; x != to; x += step)
        {
            if (_image[x, y] && _image[x + step, y])
            {
                return x;
            }
        }

        return -1;
    }

    private int FirstRunVertical(int top, int bottom, int x, bool forward)
    {
        var step = forward ? 1 : -1;
        var from = forward ? top : bottom;
        var to = forward ? bottom : top;
        for (var y = from; y != to; y += step)
        {
            if (_image[x, y] && _image[x, y + step])
            {
                return y;
            }
        }

        return -1;
    }

    /// <summary>Andrew's monotone chain, producing the hull in counter clockwise order on screen.</summary>
    private static void ConvexHull(List<BbScanPoint> points, List<BbScanPoint> hull)
    {
        hull.Clear();
        points.Sort(static (p, q) => p.X != q.X ? p.X.CompareTo(q.X) : p.Y.CompareTo(q.Y));

        var n = points.Count;
        if (n < 3)
        {
            hull.AddRange(points);
            return;
        }

        // Lower hull.
        for (var i = 0; i < n; i++)
        {
            while (hull.Count >= 2 && Cross(hull[^2], hull[^1], points[i]) <= 0)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(points[i]);
        }

        // Upper hull.
        var lowerCount = hull.Count + 1;
        for (var i = n - 2; i >= 0; i--)
        {
            while (hull.Count >= lowerCount && Cross(hull[^2], hull[^1], points[i]) <= 0)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(points[i]);
        }

        hull.RemoveAt(hull.Count - 1);
    }

    private static float Cross(BbScanPoint o, BbScanPoint a, BbScanPoint b) =>
        ((a.X - o.X) * (b.Y - o.Y)) - ((a.Y - o.Y) * (b.X - o.X));

    /// <summary>
    /// Picks the four hull vertices that best describe the symbol as a quadrilateral: the vertex
    /// furthest from the centroid, the vertex furthest from that, and the vertices furthest from
    /// the diagonal between them on either side.
    /// </summary>
    private static bool PickCorners(List<BbScanPoint> hull, Span<BbScanPoint> corners)
    {
        var count = hull.Count;
        var cx = 0f;
        var cy = 0f;
        foreach (var p in hull)
        {
            cx += p.X;
            cy += p.Y;
        }

        cx /= count;
        cy /= count;

        var first = 0;
        var firstDistance = -1f;
        for (var i = 0; i < count; i++)
        {
            var d = BbMathUtils.DistanceSquared(hull[i].X, hull[i].Y, cx, cy);
            if (d > firstDistance)
            {
                firstDistance = d;
                first = i;
            }
        }

        var third = first;
        var thirdDistance = -1f;
        for (var i = 0; i < count; i++)
        {
            var d = BbMathUtils.DistanceSquared(hull[i].X, hull[i].Y, hull[first].X, hull[first].Y);
            if (d > thirdDistance)
            {
                thirdDistance = d;
                third = i;
            }
        }

        if (third == first)
        {
            return false;
        }

        var second = -1;
        var fourth = -1;
        var secondCross = 0f;
        var fourthCross = 0f;
        for (var i = 0; i < count; i++)
        {
            var cross = Cross(hull[first], hull[third], hull[i]);
            if (cross > secondCross)
            {
                secondCross = cross;
                second = i;
            }
            else if (cross < fourthCross)
            {
                fourthCross = cross;
                fourth = i;
            }
        }

        if (second < 0 || fourth < 0)
        {
            return false;
        }

        // The distance of each side corner from the diagonal must be substantial, otherwise the
        // shape is a sliver rather than a symbol.
        var diagonal = MathF.Sqrt(thirdDistance);
        if (secondCross / diagonal < 3 || -fourthCross / diagonal < 3)
        {
            return false;
        }

        corners[0] = hull[first];
        corners[1] = hull[second];
        corners[2] = hull[third];
        corners[3] = hull[fourth];

        // Order clockwise on screen (positive shoelace sum with y pointing down).
        var area = 0f;
        for (var i = 0; i < 4; i++)
        {
            var p = corners[i];
            var q = corners[(i + 1) & 3];
            area += (p.X * q.Y) - (q.X * p.Y);
        }

        if (area < 0)
        {
            (corners[1], corners[3]) = (corners[3], corners[1]);
        }

        return true;
    }

    private bool IsInside(BbScanPoint point, float margin) =>
        point.X >= -margin && point.Y >= -margin &&
        point.X <= _image.Width - 1 + margin && point.Y <= _image.Height - 1 + margin;

    /// <summary>Counts colour changes along the line between two points.</summary>
    private int TransitionsBetween(BbScanPoint from, BbScanPoint to)
    {
        var fromX = (int)from.X;
        var fromY = (int)from.Y;
        var toX = (int)to.X;
        var toY = (int)to.Y;

        var steep = Math.Abs(toY - fromY) > Math.Abs(toX - fromX);
        if (steep)
        {
            (fromX, fromY) = (fromY, fromX);
            (toX, toY) = (toY, toX);
        }

        var dx = Math.Abs(toX - fromX);
        var dy = Math.Abs(toY - fromY);
        var error = -dx / 2;
        var xStep = fromX < toX ? 1 : -1;
        var yStep = fromY < toY ? 1 : -1;

        var transitions = 0;
        var inBlack = _image.GetSafe(steep ? fromY : fromX, steep ? fromX : fromY);

        var y = fromY;
        for (var x = fromX; x != toX; x += xStep)
        {
            var isBlack = _image.GetSafe(steep ? y : x, steep ? x : y);
            if (isBlack != inBlack)
            {
                transitions++;
                inBlack = isBlack;
            }

            error += dy;
            if (error > 0)
            {
                if (y == toY)
                {
                    break;
                }

                y += yStep;
                error -= dx;
            }
        }

        return transitions;
    }

    /// <summary>A line through a point with a unit direction.</summary>
    private readonly record struct Line(float X, float Y, float DirectionX, float DirectionY)
    {
        /// <summary>Total least squares fit through a set of points.</summary>
        public static bool TryFit(List<BbScanPoint> points, out Line line)
        {
            line = default;
            var n = points.Count;
            if (n < 2)
            {
                return false;
            }

            var mx = 0f;
            var my = 0f;
            foreach (var p in points)
            {
                mx += p.X;
                my += p.Y;
            }

            mx /= n;
            my /= n;

            var sxx = 0f;
            var sxy = 0f;
            var syy = 0f;
            foreach (var p in points)
            {
                var dx = p.X - mx;
                var dy = p.Y - my;
                sxx += dx * dx;
                sxy += dx * dy;
                syy += dy * dy;
            }

            // Principal axis of the covariance matrix.
            var theta = 0.5f * MathF.Atan2(2 * sxy, sxx - syy);
            var dirX = MathF.Cos(theta);
            var dirY = MathF.Sin(theta);
            var spread = (sxx * dirX * dirX) + (2 * sxy * dirX * dirY) + (syy * dirY * dirY);
            if (spread <= 1e-3f)
            {
                return false;
            }

            line = new Line(mx, my, dirX, dirY);
            return true;
        }

        /// <summary>Perpendicular distance from the line, signed.</summary>
        public float DistanceTo(BbScanPoint p) => ((p.X - X) * -DirectionY) + ((p.Y - Y) * DirectionX);

        public Line Shift(float dx, float dy) => this with { X = X + dx, Y = Y + dy };

        public static bool TryIntersect(Line a, Line b, out BbScanPoint point)
        {
            point = default;
            var denominator = (a.DirectionX * b.DirectionY) - (a.DirectionY * b.DirectionX);
            if (Math.Abs(denominator) < 1e-4f)
            {
                return false;
            }

            var t = (((b.X - a.X) * b.DirectionY) - ((b.Y - a.Y) * b.DirectionX)) / denominator;
            point = new BbScanPoint(a.X + (a.DirectionX * t), a.Y + (a.DirectionY * t));
            return true;
        }
    }

    /// <summary>
    /// Fallback: grow a rectangle out of the image centre until it is clear of ink, then read the
    /// enclosed shape's corners off its diagonals.
    /// </summary>
    private BbDetectorResult? DetectFromCenter(Func<BbDetectorResult, bool> accept)
    {
        var rectangleDetector = BbWhiteRectangleDetector.Create(_image);
        var cornerPoints = rectangleDetector?.Detect();
        if (cornerPoints is null)
        {
            return null;
        }

        // The rectangle detector reports its points in the order 0, 2 across the top and 1, 3
        // across the bottom; relabel them into a cycle.
        Span<BbScanPoint> corners = stackalloc BbScanPoint[4];
        corners[0] = cornerPoints[0];
        corners[1] = cornerPoints[2];
        corners[2] = cornerPoints[3];
        corners[3] = cornerPoints[1];

        var area = 0f;
        for (var i = 0; i < 4; i++)
        {
            var p = corners[i];
            var q = corners[(i + 1) & 3];
            area += (p.X * q.Y) - (q.X * p.Y);
        }

        if (area < 0)
        {
            (corners[1], corners[3]) = (corners[3], corners[1]);
        }

        return SampleQuadrilateral(corners, accept);
    }
}

/// <summary>Scratch lists shared between detections so that a frame allocates nothing.</summary>
internal sealed class BbDetectorScratch
{
    public List<BbScanPoint> Samples { get; } = new(512);

    public List<BbScanPoint> Hull { get; } = new(64);

    public List<BbScanPoint> EdgeSamples { get; } = new(128);
}
