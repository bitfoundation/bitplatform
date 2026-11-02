using System.Numerics;

namespace Bit.BbScanner;

/// <summary>
/// Finds PDF417 symbols at any orientation and resamples each one upright, so the row reader only
/// ever sees symbols whose rows run left to right from the start pattern to the stop pattern.
/// </summary>
/// <remarks>
/// <para>
/// The row reader needs a whole symbol row on one line of pixels. A PDF417 row is three modules
/// tall and often more than a hundred wide, so that holds within a degree or so of upright, which
/// a hand holding a phone never manages. Reading at an angle therefore starts with finding the
/// angle.
/// </para>
/// <para>
/// The start and stop patterns are what make that cheap. Every row carries the same ones, so
/// together they form solid bars running the full height of the symbol, and a scan line crossing
/// them at almost any angle sees their run widths in the right proportions, merely stretched. A
/// few lines at a handful of angles are therefore enough to land on a start pattern. From there
/// the start pattern's eight module bar is traced along its length: its edges are the left edge
/// of the symbol, its ends are the top and bottom left corners, and the direction it runs in is
/// the direction of the symbol's columns. A line along the rows then finds the stop pattern, whose
/// seven module bar is traced the same way for the right edge. The four corners that come out of
/// that are mapped onto an upright rectangle and resampled from the luminance image, which keeps
/// the sub-pixel edge information a binary image has already thrown away.
/// </para>
/// </remarks>
internal sealed class BbPdf417Locator
{
    private static readonly int[] StartPattern = [8, 1, 1, 1, 1, 1, 1, 3];
    private static readonly int[] StartPatternReversed = [3, 1, 1, 1, 1, 1, 1, 8];
    private static readonly int[] StopPattern = [7, 1, 1, 3, 1, 1, 1, 2, 1];

    private const float MaxAvgVariance = 0.42f;
    private const float MaxIndividualVariance = 0.8f;

    /// <summary>
    /// Orientations scanned on a live frame. A start pattern stays readable on a line up to about
    /// thirty degrees off its rows for a symbol of three rows, and further for a taller one, so
    /// lines every forty-five degrees, each read in both directions, cover every orientation.
    /// </summary>
    private static readonly float[] LiveAngles = [0f, 90f, 45f, 135f];

    /// <summary>Orientations scanned on a still image, where finding a short symbol matters more than time.</summary>
    private static readonly float[] ThoroughAngles = [0f, 90f, 45f, 135f, 15f, 30f, 60f, 75f, 105f, 120f, 150f, 165f];

    /// <summary>Most symbols located per frame.</summary>
    private const int MaxCandidates = 4;

    /// <summary>
    /// Most start patterns followed up per frame. Every line crossing a symbol sees its start
    /// pattern, so a symbol that cannot be resolved - damaged, or a start pattern with no stop -
    /// would otherwise be traced again on every one of them.
    /// </summary>
    private const int MaxAttempts = 12;

    private readonly BbHybridBinarizer _binarizer = new();
    private readonly List<Vector2> _outerEdges = [];
    private readonly List<Vector2> _innerEdges = [];
    private readonly List<Candidate> _candidates = [];

    /// <summary>
    /// Candidates a start pattern gave when no stop pattern could be found for it, as compact symbols. They are offered
    /// only after every other: a full symbol's start pattern seen where its stop pattern cannot be traced - the frame's
    /// edge, a crease - would be taken for a compact symbol too, and, offered first, hide the symbol it belongs to.
    /// </summary>
    private readonly List<Candidate> _compactCandidates = [];
    private readonly RunBuffer _scan = new();
    private readonly RunBuffer _row = new();
    private BbLuminanceBuffer? _upright;
    private int _attempts;

    /// <summary>The widest a symbol can be: 30 data columns of 17 modules and 69 of start, stop and indicators, and some slack.</summary>
    private const float MaxSymbolModules = 579 * 1.1f;


    /// <summary>
    /// Zeroes the straightened copy of the last candidate, what its binarizer measured of it, and the runs of the last
    /// lines scanned for start and stop patterns.
    /// </summary>
    internal void ForgetImage()
    {
        _upright?.Clear();
        _binarizer.ForgetImage();
        _scan.Forget();
        _row.Forget();
    }

    /// <summary>A located symbol: its corners in image coordinates and its module size in pixels.</summary>
    internal readonly record struct Candidate(Vector2 TopLeft, Vector2 TopRight, Vector2 BottomRight, Vector2 BottomLeft, float ModuleSize);

    /// <summary>
    /// Finds the symbols in a binarised image. The list is reused by the next call.
    /// </summary>
    /// <param name="image">The binarised frame.</param>
    /// <param name="thorough">Scan more orientations and more densely, for still images.</param>
    public IReadOnlyList<Candidate> Locate(BbBitMatrix image, bool thorough)
    {
        _candidates.Clear();
        _compactCandidates.Clear();
        _attempts = 0;

        var angles = thorough ? ThoroughAngles : LiveAngles;
        var spacing = thorough
            ? Math.Max(3f, Math.Min(image.Width, image.Height) / 64f)
            : Math.Max(6f, Math.Min(image.Width, image.Height) / 32f);

        foreach (var angle in angles)
        {
            var radians = angle * MathF.PI / 180f;
            var direction = new Vector2(MathF.Cos(radians), MathF.Sin(radians));
            if (MathF.Abs(direction.X) < 1e-4f) direction.X = 0;
            if (MathF.Abs(direction.Y) < 1e-4f) direction.Y = 0;

            var normal = new Vector2(-direction.Y, direction.X);
            var extent = (image.Width * MathF.Abs(normal.X)) + (image.Height * MathF.Abs(normal.Y));
            var center = new Vector2(image.Width / 2f, image.Height / 2f);
            var lines = (int)(extent / spacing);

            // Outwards from the middle, where an aiming user puts the symbol.
            for (var attempt = 0; attempt <= lines; attempt++)
            {
                var delta = ((attempt + 1) / 2) * spacing;
                var offset = (attempt & 1) == 0 ? delta : -delta;
                var origin = center + (normal * offset);

                if (ScanLine(image, origin, direction) && _candidates.Count >= MaxCandidates)
                {
                    return _candidates;
                }
            }
        }

        foreach (var compact in _compactCandidates)
        {
            if (_candidates.Count >= MaxCandidates)
            {
                break;
            }

            if (!IsInsideKnownCandidate(Vector2.Lerp(compact.TopLeft, compact.BottomRight, 0.5f)))
            {
                _candidates.Add(compact);
            }
        }

        return _candidates;
    }

    /// <summary>
    /// Resamples a located symbol upright: rows horizontal, start pattern on the left, about three
    /// pixels a module. The buffer is reused by the next call.
    /// </summary>
    /// <remarks>
    /// This, and reading every row of what it produces, is the expensive part of the search: about a hundred
    /// nanoseconds a pixel. A pair of start and stop shaped stripes with anything between them - a poster, a crafted
    /// file - is a candidate like any other, so the caller spends a budget of pixels across the candidates of a frame.
    /// </remarks>
    /// <param name="candidate">The symbol to resample.</param>
    /// <param name="luminance">The frame to sample from, or an empty view to sample the binarised image instead.</param>
    /// <param name="image">The binarised frame, sampled when there is no luminance.</param>
    /// <param name="inverted">Whether the symbol is light on dark.</param>
    /// <param name="maxPixels">
    /// The most pixels to resample into: a candidate that would need more is resampled more coarsely, and one that
    /// would need more even at the frame's own scale is given up.
    /// </param>
    /// <param name="toImage">Maps a point of the upright image back to the frame.</param>
    public BbBitMatrix? Rectify(
        in Candidate candidate, in BbLuminanceView luminance, BbBitMatrix image, bool inverted, long maxPixels, out Func<BbScanPoint, BbScanPoint> toImage)
    {
        var widthPixels = (Vector2.Distance(candidate.TopLeft, candidate.TopRight) + Vector2.Distance(candidate.BottomLeft, candidate.BottomRight)) / 2;
        var heightPixels = (Vector2.Distance(candidate.TopLeft, candidate.BottomLeft) + Vector2.Distance(candidate.TopRight, candidate.BottomRight)) / 2;

        // Upsampling a small symbol gives the binariser interpolated edges to threshold, which
        // places them more accurately than whole source pixels would.
        var scale = Math.Clamp(3f / Math.Max(candidate.ModuleSize, 0.5f), 1f, 4f);
        const int MarginX = 6;
        const int MarginY = 2;

        // Coarser, a step at a time, until the copy - margins included - fits what the caller can spend on it.
        int innerWidth, innerHeight;
        while (true)
        {
            innerWidth = Math.Max(1, (int)MathF.Ceiling(widthPixels * scale));
            innerHeight = Math.Max(1, (int)MathF.Ceiling(heightPixels * scale));
            if ((long)(innerWidth + (2 * MarginX)) * (innerHeight + (2 * MarginY)) <= maxPixels)
            {
                break;
            }

            if (scale <= 1f)
            {
                toImage = static p => p;
                return null;
            }

            scale = MathF.Max(1f, scale * 0.95f);
        }

        var width = innerWidth + (2 * MarginX);
        var height = innerHeight + (2 * MarginY);

        // A symbol is at most 30 data columns of 17 modules, plus 69 modules of start, stop and
        // indicators, and at most 90 rows; anything far beyond that is not a PDF417 symbol, and it
        // would cost seconds to resample and read.
        if (width > 4096 || height > 4096)
        {
            toImage = static p => p;
            return null;
        }

        var transform = BbPerspectiveTransform.SquareToQuadrilateral(
            candidate.TopLeft.X, candidate.TopLeft.Y,
            candidate.TopRight.X, candidate.TopRight.Y,
            candidate.BottomRight.X, candidate.BottomRight.Y,
            candidate.BottomLeft.X, candidate.BottomLeft.Y);

        if (_upright is null)
        {
            _upright = BbLuminanceBuffer.Rent(width, height);
        }
        else
        {
            _upright.Resize(width, height);
        }

        var pixels = _upright.Pixels;
        var hasLuminance = !luminance.IsEmpty;
        for (var y = 0; y < height; y++)
        {
            var v = (y - MarginY + 0.5f) / innerHeight;
            for (var x = 0; x < width; x++)
            {
                var u = (x - MarginX + 0.5f) / innerWidth;
                var source = transform.Transform(new BbScanPoint(u, v));
                var sample = hasLuminance
                    ? SampleLuminance(luminance, source.X - 0.5f, source.Y - 0.5f)
                    : SampleBits(image, source.X - 0.5f, source.Y - 0.5f);
                // Luminance is the frame as it is, so a light-on-dark symbol is turned around here; the binarised
                // image has been inverted already by the reader, and turning it again would undo that.
                pixels[(y * width) + x] = inverted && hasLuminance ? (byte)(255 - sample) : sample;
            }
        }

        toImage = point => transform.Transform(new BbScanPoint(
            (point.X - MarginX) / innerWidth,
            (point.Y - MarginY) / innerHeight));
        return _binarizer.GetBlackMatrix(_upright.View);
    }

    /// <summary>Reads one scan line in both directions and follows up every start pattern on it.</summary>
    /// <returns><see langword="true"/> when a new candidate was added.</returns>
    private bool ScanLine(BbBitMatrix image, Vector2 origin, Vector2 direction)
    {
        // The step between samples: a pixel along the line, or for a line on the pixel lattice -
        // every live scan line - a whole pixel along each axis it moves on, so a diagonal steps
        // the square root of two.
        Vector2 step;
        float from;
        int runCount;
        bool firstBlack;
        if (TryLattice(direction, out var dx, out var dy))
        {
            origin = new Vector2(MathF.Round(origin.X), MathF.Round(origin.Y));
            step = new Vector2(dx, dy);
            if (!TryClip(origin, step, image.Width, image.Height, out from, out var to))
            {
                return false;
            }

            runCount = _scan.ReadLattice(image, (int)(origin.X + (dx * from)), (int)(origin.Y + (dy * from)), dx, dy, (int)(to - from) + 1, out firstBlack);
        }
        else
        {
            step = direction;
            if (!TryClip(origin, direction, image.Width, image.Height, out from, out var to))
            {
                return false;
            }

            runCount = _scan.Read(image, origin + (direction * from), direction, (int)(to - from) + 1, out firstBlack);
        }

        var added = false;

        // The start pattern's wide bar is eight of its seventeen modules. Most windows of eight runs
        // on a frame have no run anywhere near that dominant, and a running sum rejects them for the
        // price of two additions, before the variance test looks at all eight.
        var sum = 0;
        for (var i = 0; i < 8 && i < runCount; i++)
        {
            sum += _scan.Runs[i];
        }

        for (var i = 0; i + 8 <= runCount; sum += (i + 8 < runCount ? _scan.Runs[i + 8] : 0) - _scan.Runs[i], i++)
        {
            var black = ((i & 1) == 0) == firstBlack;
            var wide = black ? _scan.Runs[i] : _scan.Runs[i + 7];
            if (wide * 17 < sum * 6)
            {
                continue;
            }

            var window = _scan.Runs.AsSpan(i, 8);

            Vector2 outer;
            Vector2 inner;
            if (black && Matches(window, StartPattern))
            {
                // Read left to right: the eight module bar comes first.
                outer = origin + (step * (from + _scan.Starts[i]));
                inner = origin + (step * (from + _scan.Starts[i + 8]));
            }
            else if (!black && Matches(window, StartPatternReversed))
            {
                // Read right to left: the symbol lies behind the line's direction of travel.
                outer = origin + (step * (from + _scan.Starts[i + 8]));
                inner = origin + (step * (from + _scan.Starts[i]));
            }
            else
            {
                continue;
            }

            if (IsInsideKnownCandidate(outer))
            {
                continue;
            }

            if (++_attempts > MaxAttempts)
            {
                return added;
            }

            if (TryResolve(image, outer, inner, out var candidate))
            {
                _candidates.Add(candidate);
                added = true;
                if (_candidates.Count >= MaxCandidates)
                {
                    return true;
                }
            }
        }

        return added;
    }

    /// <summary>
    /// Turns a start pattern seen on one scan line into the symbol's four corners, or fails when
    /// what was seen does not hold up as a PDF417 symbol.
    /// </summary>
    /// <param name="image">The binarised frame.</param>
    /// <param name="outer">Where the scan line entered the start pattern's eight module bar.</param>
    /// <param name="inner">Where the scan line left the start pattern.</param>
    /// <param name="candidate">The symbol's corners and module size.</param>
    private bool TryResolve(BbBitMatrix image, Vector2 outer, Vector2 inner, out Candidate candidate)
    {
        candidate = default;

        var across = inner - outer;
        var crossing = across.Length();
        if (crossing < BbPdf417SymbolTable.ModulesPerCodeword)
        {
            return false;
        }

        var u = across / crossing;
        var crossingModule = crossing / BbPdf417SymbolTable.ModulesPerCodeword;

        // A first trace across the line's own direction finds which way the bar runs.
        if (!TraceBar(image, outer + (u * (4 * crossingModule)), u, 8 * crossingModule, crossingModule, out var columns, out var top, out var bottom))
        {
            return false;
        }

        // The columns run along the traced bar; the rows run across it, into the symbol.
        var rows = new Vector2(columns.Y, -columns.X);
        if (Vector2.Dot(rows, u) < 0)
        {
            rows = -rows;
            columns = -columns;
            (top, bottom) = (bottom, top);
        }

        // That trace measured the bar obliquely, and near the bar's ends an oblique cross-section
        // runs off the symbol, comes up short and ends the trace a few modules early. Measured
        // again straight across - along the rows - the cross-section stays whole to the very end,
        // which is what puts the corners where they belong. The middle of the first trace is
        // reliable, so the second starts there.
        var module = crossingModule * Vector2.Dot(u, rows);
        var middle = Vector2.Lerp(top, bottom, 0.5f) + (rows * (4 * module));
        if (!TraceBar(image, middle, rows, 8 * module, module, out var refined, out top, out bottom))
        {
            return false;
        }

        if (Vector2.Dot(refined, columns) < 0)
        {
            refined = -refined;
            (top, bottom) = (bottom, top);
        }

        columns = refined;
        rows = new Vector2(columns.Y, -columns.X);

        var height = Vector2.Distance(top, bottom);
        if (height < 3 * module)
        {
            return false;
        }

        // Now that the rows' direction is known, a line along a row reads straight across the
        // symbol to its stop pattern. The middle row is tried first, then two others in case it
        // is damaged.
        Span<float> fractions = [0.5f, 0.3f, 0.7f];
        Span<int> stops = stackalloc int[8];
        foreach (var fraction in fractions)
        {
            var origin = Vector2.Lerp(top, bottom, fraction) - (rows * (2 * module));
            if (!TryClip(origin, rows, image.Width, image.Height, out var from, out var to))
            {
                continue;
            }

            var found = FindStops(image, origin + (rows * from), rows, (int)(to - from) + 1, stops);

            // A line that is a fraction of a degree off the rows can still drift across them on
            // a wide symbol and assemble something stop-shaped in the middle of it. The real stop
            // pattern is the last one on the line, and it is as tall as the start pattern - unless
            // the last one is a neighbouring symbol's, printed beside this one a little higher or
            // lower: paired with that, the quad comes out sheared and reads as neither. A stop
            // spans the same rows as its own start pattern, give or take what perspective takes
            // off one side, so an earlier stop that is clearly better aligned is preferred.
            var best = default(Candidate);
            var bestMisalignment = float.MaxValue;
            for (var s = found - 1; s >= 0; s--)
            {
                var stopInner = origin + (rows * (from + _row.Starts[stops[s]]));
                var stopModule = (_row.Starts[stops[s] + 9] - _row.Starts[stops[s]]) / 18f;

                // The right edge: trace the stop pattern's seven module bar. The traced edge is
                // the one facing the start, where the stop pattern begins, so the symbol itself
                // ends eighteen modules further on.
                if (!TraceBar(image, stopInner + (rows * (3.5f * stopModule)), rows, 7 * stopModule, stopModule, out _, out var stopTop, out var stopBottom))
                {
                    continue;
                }

                var stopHeight = Vector2.Distance(stopTop, stopBottom);
                if (stopHeight < height * 0.7f || stopHeight > height * 1.4f)
                {
                    continue;
                }

                var (rightTop, rightBottom) = Vector2.Dot(stopBottom - stopTop, columns) >= 0 ? (stopTop, stopBottom) : (stopBottom, stopTop);
                var misalignment = MathF.Max(
                    MathF.Abs(Vector2.Dot(rightTop - top, columns)),
                    MathF.Abs(Vector2.Dot(rightBottom - bottom, columns))) - (MathF.Abs(stopHeight - height) / 2);
                if (misalignment > 0.5f * height || misalignment > bestMisalignment - (0.1f * height))
                {
                    continue;
                }

                var stopWidth = rows * (18 * stopModule);
                var paired = new Candidate(top, rightTop + stopWidth, rightBottom + stopWidth, bottom, (module + stopModule) / 2);

                // Start, two row indicators, one to thirty data columns and stop: 86 to 579 modules.
                var symbolModules = Vector2.Distance(paired.TopLeft, paired.TopRight) / paired.ModuleSize;
                if (symbolModules >= 60 && symbolModules <= MaxSymbolModules)
                {
                    best = paired;
                    bestMisalignment = misalignment;
                }
            }

            if (bestMisalignment < float.MaxValue)
            {
                candidate = best;
                return true;
            }
        }

        // No stop pattern: a compact symbol's is a single bar. Kept aside, once, until every other candidate is in.
        if (TryResolveCompact(image, top, bottom, rows, module, out var compact))
        {
            var known = false;
            foreach (var other in _compactCandidates)
            {
                known |= IsInside(Vector2.Lerp(compact.TopLeft, compact.BottomLeft, 0.5f), other.TopLeft, other.TopRight, other.BottomRight, other.BottomLeft, other.ModuleSize * 4);
            }

            if (!known)
            {
                _compactCandidates.Add(compact);
            }
        }

        candidate = default;
        return false;
    }

    /// <summary>
    /// Finds the right edge of a compact symbol, one whose start pattern has no stop pattern on its rows: compact PDF417
    /// drops the right row indicator and cuts the stop pattern to a single bar. Its rows end at their last bar before
    /// light wider than any space in a codeword - seven modules - or light that runs on to the edge of the frame, and the
    /// right edge is the line through where three rows end, which have to agree to within a module and a half.
    /// </summary>
    private bool TryResolveCompact(BbBitMatrix image, Vector2 top, Vector2 bottom, Vector2 rows, float module, out Candidate candidate)
    {
        candidate = default;
        Span<Vector2> ends = stackalloc Vector2[3];
        ReadOnlySpan<float> fractions = [0.25f, 0.5f, 0.75f];
        var found = 0;
        foreach (var fraction in fractions)
        {
            var left = Vector2.Lerp(top, bottom, fraction);
            var origin = left - (rows * (2 * module));
            if (!TryClip(origin, rows, image.Width, image.Height, out var from, out var to))
            {
                continue;
            }

            var runCount = _row.Read(image, origin + (rows * from), rows, (int)(to - from) + 1, out var firstBlack);
            var startAt = -1;
            for (var i = 0; i + 8 <= runCount && i < 4; i++)
            {
                if (((i & 1) == 0) == firstBlack && Matches(_row.Runs.AsSpan(i, 8), StartPattern))
                {
                    startAt = i;
                    break;
                }
            }

            if (startAt < 0)
            {
                continue;
            }

            // The end: the start of the first light run after the start pattern that is seven modules wide or runs on to
            // the edge of the frame - a quiet zone, which need only be two modules wide - or the line's end.
            var end = _row.Starts[runCount];
            for (var i = startAt + 8; i < runCount; i++)
            {
                if (((i & 1) == 0) != firstBlack && (_row.Runs[i] >= 7 * module || (i == runCount - 1 && _row.Runs[i] >= 2 * module)))
                {
                    end = _row.Starts[i];
                    break;
                }
            }

            ends[found++] = origin + (rows * (from + end));
        }

        if (found < 2)
        {
            return false;
        }

        // The right edge runs from the first end to the last; any end between has to lie on it.
        var (first, last) = (ends[0], ends[found - 1]);
        var edge = Vector2.Normalize(last - first);
        for (var i = 1; i < found - 1; i++)
        {
            var off = ends[i] - first;
            if (MathF.Abs((off.X * edge.Y) - (off.Y * edge.X)) > 1.5f * module)
            {
                return false;
            }
        }

        // Where the edge meets the top and bottom rows, which run along rows from the left corners.
        if (!Meet(top, rows, first, edge, out var rightTop) || !Meet(bottom, rows, first, edge, out var rightBottom))
        {
            return false;
        }

        // Start, left row indicator, one to thirty data columns and the stop bar: 52 to 545 modules.
        var symbolModules = Vector2.Distance(top, rightTop) / module;
        if (symbolModules < 50 || symbolModules > MaxSymbolModules || Vector2.Distance(bottom, rightBottom) / module < 50)
        {
            return false;
        }

        candidate = new Candidate(top, rightTop, rightBottom, bottom, module);
        return true;

        static bool Meet(Vector2 origin, Vector2 direction, Vector2 point, Vector2 along, out Vector2 meeting)
        {
            var cross = (direction.X * along.Y) - (direction.Y * along.X);
            if (MathF.Abs(cross) < 0.3f)
            {
                meeting = default;
                return false;
            }

            var between = point - origin;
            meeting = origin + (direction * (((between.X * along.Y) - (between.Y * along.X)) / cross));
            return true;
        }
    }

    /// <summary>
    /// Follows a bar along its length in both directions from a point near its middle, measuring
    /// its outer edge as it goes.
    /// </summary>
    /// <param name="image">The binarised frame.</param>
    /// <param name="center">A point inside the bar.</param>
    /// <param name="across">The direction that crosses the bar, from its outer edge to its inner one.</param>
    /// <param name="width">The bar's width measured along <paramref name="across"/>.</param>
    /// <param name="module">The module size, which sets the step.</param>
    /// <param name="along">The fitted direction the bar runs in.</param>
    /// <param name="first">The bar's outer edge at one end, on the fitted line.</param>
    /// <param name="last">The bar's outer edge at the other end, on the fitted line.</param>
    private bool TraceBar(
        BbBitMatrix image, Vector2 center, Vector2 across, float width, float module,
        out Vector2 along, out Vector2 first, out Vector2 last)
    {
        along = default;
        first = default;
        last = default;

        _outerEdges.Clear();
        _innerEdges.Clear();

        if (!MeasureBar(image, center, across, width, width * 0.5f, width * 1.6f, out var outer, out var inner))
        {
            return false;
        }

        _outerEdges.Add(outer);
        _innerEdges.Add(inner);

        // A straight bar measures the same width all along a fixed direction, so every later
        // cross-section is held to the first. What that rejects is the bar's end, where the
        // cross-section is clipped by the edge of the symbol: measured anyway, it puts edge
        // points a module or more off the line, and at the far ends of the fit those few points
        // carry enough leverage to tip its angle the wrong way.
        var reference = Vector2.Distance(outer, inner);
        var normal = new Vector2(-across.Y, across.X);
        var step = Math.Max(1f, module * 0.75f);
        var limit = image.Width + image.Height;
        var middle = (outer + inner) / 2;

        for (var sign = -1; sign <= 1; sign += 2)
        {
            // Each measurement re-centres the next one on the bar, so the trace follows it
            // whatever its angle to the step direction.
            var current = middle;
            for (var i = 0; i < limit; i++)
            {
                var next = current + (normal * (sign * step));
                if (!MeasureBar(image, next, across, width, reference * 0.75f, reference * 1.3f, out var edge, out var opposite))
                {
                    break;
                }

                _outerEdges.Add(edge);
                _innerEdges.Add(opposite);
                current = (edge + opposite) / 2;
            }
        }

        if (_outerEdges.Count < 3)
        {
            return false;
        }

        // Fit the bar's direction to both of its edges at once: they are parallel, and between
        // them they double the evidence. A second fit leaves out the cross-sections the first
        // one puts more than half a module off either edge.
        Fit(Vector2.Zero, float.PositiveInfinity, out var mean, out along);
        var count = Fit(along, Math.Max(1f, module * 0.5f), out mean, out along);
        if (count < 3)
        {
            return false;
        }

        var min = float.PositiveInfinity;
        var max = float.NegativeInfinity;
        foreach (var point in _outerEdges)
        {
            var t = Vector2.Dot(point - mean, along);
            min = Math.Min(min, t);
            max = Math.Max(max, t);
        }

        // The last measurement either way is up to a step short of the bar's real end.
        first = mean + (along * (min - (step / 2)));
        last = mean + (along * (max + (step / 2)));
        return true;
    }

    /// <summary>
    /// Fits a direction to the traced edges, keeping only cross-sections whose two edge points
    /// both lie within <paramref name="tolerance"/> of the lines an earlier fit along
    /// <paramref name="previous"/> describes; the rest are removed from the lists.
    /// </summary>
    /// <returns>How many cross-sections the fit used.</returns>
    private int Fit(Vector2 previous, float tolerance, out Vector2 outerMean, out Vector2 along)
    {
        if (!float.IsPositiveInfinity(tolerance))
        {
            var previousOuter = Mean(_outerEdges);
            var previousInner = Mean(_innerEdges);
            for (var i = _outerEdges.Count - 1; i >= 0; i--)
            {
                if (Distance(_outerEdges[i], previousOuter, previous) > tolerance ||
                    Distance(_innerEdges[i], previousInner, previous) > tolerance)
                {
                    _outerEdges.RemoveAt(i);
                    _innerEdges.RemoveAt(i);
                }
            }
        }

        outerMean = Mean(_outerEdges);
        var innerMean = Mean(_innerEdges);

        float sxx = 0, sxy = 0, syy = 0;
        for (var i = 0; i < _outerEdges.Count; i++)
        {
            var d = _outerEdges[i] - outerMean;
            sxx += d.X * d.X;
            sxy += d.X * d.Y;
            syy += d.Y * d.Y;

            d = _innerEdges[i] - innerMean;
            sxx += d.X * d.X;
            sxy += d.X * d.Y;
            syy += d.Y * d.Y;
        }

        var theta = 0.5f * MathF.Atan2(2 * sxy, sxx - syy);
        along = new Vector2(MathF.Cos(theta), MathF.Sin(theta));
        return _outerEdges.Count;

        static Vector2 Mean(List<Vector2> points)
        {
            var sum = Vector2.Zero;
            foreach (var point in points)
            {
                sum += point;
            }

            return points.Count == 0 ? sum : sum / points.Count;
        }

        // Perpendicular distance from a point to the line through origin along direction.
        static float Distance(Vector2 point, Vector2 origin, Vector2 direction)
        {
            var d = point - origin;
            return MathF.Abs((d.X * direction.Y) - (d.Y * direction.X));
        }
    }

    /// <summary>Measures one cross-section of a bar: where it begins and ends along <paramref name="across"/>.</summary>
    private static bool MeasureBar(
        BbBitMatrix image, Vector2 center, Vector2 across, float width, float minWidth, float maxWidth, out Vector2 outer, out Vector2 inner)
    {
        outer = default;
        inner = default;

        // The traced point drifts off the bar near its ends, where the edge is ragged; looking a
        // little either side keeps the trace on it rather than ending it early.
        if (!IsBlack(image, center))
        {
            var found = false;
            for (var probe = 0.5f; probe <= width / 2 && !found; probe += 0.5f)
            {
                if (IsBlack(image, center + (across * probe)))
                {
                    center += across * probe;
                    found = true;
                }
                else if (IsBlack(image, center - (across * probe)))
                {
                    center -= across * probe;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        var maximum = maxWidth;
        var back = 0f;
        while (back < maximum && IsBlack(image, center - (across * (back + 0.5f))))
        {
            back += 0.5f;
        }

        var ahead = 0f;
        while (ahead < maximum && IsBlack(image, center + (across * (ahead + 0.5f))))
        {
            ahead += 0.5f;
        }

        var measured = back + ahead + 0.5f;
        if (measured < minWidth || measured > maxWidth)
        {
            return false;
        }

        outer = center - (across * (back + 0.25f));
        inner = center + (across * (ahead + 0.25f));
        return true;
    }

    /// <summary>
    /// Reads along a row from just before the start pattern and records the runs at which a stop
    /// pattern begins, nearest first. A symbol against the edge of the frame puts the line's
    /// origin outside it, so the caller starts the line where it enters the frame.
    /// </summary>
    /// <returns>How many stop patterns were recorded; none when the line does not begin with a start pattern.</returns>
    private int FindStops(BbBitMatrix image, Vector2 start, Vector2 direction, int length, Span<int> stops)
    {
        var runCount = _row.Read(image, start, direction, length, out var firstBlack);

        // The start pattern has to be the first thing on the line, or the line is not a row.
        var startAt = -1;
        for (var i = 0; i + 8 <= runCount && i < 4; i++)
        {
            if (((i & 1) == 0) == firstBlack && Matches(_row.Runs.AsSpan(i, 8), StartPattern))
            {
                startAt = i;
                break;
            }
        }

        if (startAt < 0)
        {
            return 0;
        }

        var found = 0;
        for (var i = startAt + 8; i + 9 <= runCount && found < stops.Length; i++)
        {
            if (((i & 1) == 0) == firstBlack && Matches(_row.Runs.AsSpan(i, 9), StopPattern))
            {
                stops[found++] = i;
            }
        }

        return found;
    }

    /// <summary>
    /// The runs of one sampled line. The scan lines and the row lines a candidate is checked with
    /// each have their own, because a candidate is checked in the middle of reading a scan line.
    /// </summary>
    private sealed class RunBuffer
    {
        /// <summary>Run widths, in samples.</summary>
        public int[] Runs { get; private set; } = new int[512];

        /// <summary>Where each run begins, in pixels along the line; one entry more than there are runs.</summary>
        public float[] Starts { get; private set; } = new float[513];

        /// <summary>Zeroes the runs of the last line read.</summary>
        public void Forget()
        {
            Array.Clear(Runs);
            Array.Clear(Starts);
        }

        /// <summary>
        /// Samples a line that steps one whole pixel at a time along each axis it moves on, which is
        /// every live scan line: such a line needs no interpolation, no rounding and no clamping,
        /// only a walk through the packed bits.
        /// </summary>
        /// <param name="image">The binarised image.</param>
        /// <param name="x">Column of the first sample, inside the image.</param>
        /// <param name="y">Row of the first sample, inside the image.</param>
        /// <param name="dx">Columns to step per sample: -1, 0 or 1.</param>
        /// <param name="dy">Rows to step per sample: 0 or 1.</param>
        /// <param name="length">Samples to take, all of which must lie inside the image.</param>
        /// <param name="firstBlack">Whether the first run is ink.</param>
        /// <returns>The number of runs; <see cref="Starts"/> is in samples.</returns>
        public int ReadLattice(BbBitMatrix image, int x, int y, int dx, int dy, int length, out bool firstBlack)
        {
            var bits = image.Bits;
            var rowWords = image.RowWords;
            var row = y * rowWords;
            var rowStep = dy * rowWords;
            var runs = Runs;
            var starts = Starts;

            var previous = ((bits[row + (x >> 5)] >> (x & 31)) & 1) != 0;
            firstBlack = previous;
            runs[0] = 1;
            starts[0] = 0;
            var count = 1;

            for (var i = 1; i < length; i++)
            {
                x += dx;
                row += rowStep;
                var black = ((bits[row + (x >> 5)] >> (x & 31)) & 1) != 0;
                if (black == previous)
                {
                    runs[count - 1]++;
                    continue;
                }

                if (count == runs.Length)
                {
                    Array.Resize(ref runs, runs.Length * 2);
                    Array.Resize(ref starts, runs.Length + 1);
                    Runs = runs;
                    Starts = starts;
                }

                runs[count] = 1;
                starts[count] = i;
                count++;
                previous = black;
            }

            starts[count] = length;
            return count;
        }

        /// <summary>Samples the binarised image along a line and records its runs.</summary>
        /// <returns>The number of runs; <see cref="Starts"/> is in samples, which are a pixel apart.</returns>
        public int Read(BbBitMatrix image, Vector2 start, Vector2 direction, int length, out bool firstBlack)
        {
            var maxX = image.Width - 1;
            var maxY = image.Height - 1;
            var runs = Runs;
            var starts = Starts;
            var count = 0;
            var previous = false;
            firstBlack = false;

            // Positions are worked out in double precision. A line fitted to a symbol's edge often
            // starts close to a pixel boundary, and in single precision the fraction of start + i
            // rounds away as i grows, so samples would hop between neighbouring pixels along the
            // line - reading one twice, skipping the next - which at a pixel a module drops whole
            // modules and at larger sizes adds a pixel of error to random runs.
            var startX = (double)start.X + 0.5;
            var startY = (double)start.Y + 0.5;
            for (var i = 0; i < length; i++)
            {
                var x = Math.Clamp((int)(startX + (direction.X * (double)i)), 0, maxX);
                var y = Math.Clamp((int)(startY + (direction.Y * (double)i)), 0, maxY);
                var black = image[x, y];

                if (i == 0)
                {
                    firstBlack = black;
                    previous = black;
                    runs[0] = 1;
                    starts[0] = 0;
                    count = 1;
                    continue;
                }

                if (black == previous)
                {
                    runs[count - 1]++;
                    continue;
                }

                if (count == runs.Length)
                {
                    Array.Resize(ref runs, runs.Length * 2);
                    Array.Resize(ref starts, runs.Length + 1);
                    Runs = runs;
                    Starts = starts;
                }

                runs[count] = 1;
                starts[count] = i;
                count++;
                previous = black;
            }

            starts[count] = length;
            return count;
        }
    }

    private bool IsInsideKnownCandidate(Vector2 point)
    {
        foreach (var candidate in _candidates)
        {
            if (IsInside(point, candidate.TopLeft, candidate.TopRight, candidate.BottomRight, candidate.BottomLeft, candidate.ModuleSize * 4))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether a point lies inside a convex quadrilateral grown by <paramref name="slack"/>.</summary>
    private static bool IsInside(Vector2 p, Vector2 a, Vector2 b, Vector2 c, Vector2 d, float slack)
    {
        var center = (a + b + c + d) / 4;
        return SameSide(p, a, b, center, slack) && SameSide(p, b, c, center, slack) &&
               SameSide(p, c, d, center, slack) && SameSide(p, d, a, center, slack);

        static bool SameSide(Vector2 p, Vector2 from, Vector2 to, Vector2 inside, float slack)
        {
            var edge = to - from;
            var length = edge.Length();
            if (length < 1e-3f)
            {
                return true;
            }

            var normal = new Vector2(-edge.Y, edge.X) / length;
            var side = Vector2.Dot(inside - from, normal) >= 0 ? 1 : -1;
            return Vector2.Dot(p - from, normal) * side >= -slack;
        }
    }

    private static bool Matches(ReadOnlySpan<int> runs, ReadOnlySpan<int> pattern) =>
        BbLinearPatterns.PatternMatchVariance(runs, pattern, MaxIndividualVariance) < MaxAvgVariance;

    private static bool IsBlack(BbBitMatrix image, Vector2 point)
    {
        var x = (int)MathF.Floor(point.X + 0.5f);
        var y = (int)MathF.Floor(point.Y + 0.5f);
        return image.GetSafe(x, y);
    }

    /// <summary>
    /// Whether a unit direction runs along a row, a column or an exact diagonal, and so can be
    /// walked a whole pixel per axis at a time; if so, the step it walks.
    /// </summary>
    private static bool TryLattice(Vector2 direction, out int dx, out int dy)
    {
        dx = MathF.Abs(direction.X) < 1e-3f ? 0 : Math.Sign(direction.X);
        dy = MathF.Abs(direction.Y) < 1e-3f ? 0 : Math.Sign(direction.Y);
        if (dx == 0 || dy == 0)
        {
            return dx != 0 || dy != 0;
        }

        return MathF.Abs(MathF.Abs(direction.X) - MathF.Abs(direction.Y)) < 1e-3f;
    }

    /// <summary>Clips the line <c>origin + t direction</c> to the image, Liang-Barsky style.</summary>
    private static bool TryClip(Vector2 origin, Vector2 direction, int width, int height, out float from, out float to)
    {
        var t0 = float.NegativeInfinity;
        var t1 = float.PositiveInfinity;
        from = 0;
        to = 0;

        if (!Clip(-direction.X, origin.X, ref t0, ref t1) ||
            !Clip(direction.X, width - 1 - origin.X, ref t0, ref t1) ||
            !Clip(-direction.Y, origin.Y, ref t0, ref t1) ||
            !Clip(direction.Y, height - 1 - origin.Y, ref t0, ref t1))
        {
            return false;
        }

        from = MathF.Ceiling(t0);
        to = MathF.Floor(t1);
        return to - from >= 16;

        static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (p == 0)
            {
                return q >= 0;
            }

            var t = q / p;
            if (p < 0)
            {
                if (t > t1)
                {
                    return false;
                }

                t0 = Math.Max(t0, t);
            }
            else
            {
                if (t < t0)
                {
                    return false;
                }

                t1 = Math.Min(t1, t);
            }

            return true;
        }
    }

    /// <summary>Bilinear sample of the frame; anything outside it reads as paper.</summary>
    private static byte SampleLuminance(in BbLuminanceView view, float x, float y)
    {
        return x < 0 || y < 0 || x > view.Width - 1 || y > view.Height - 1
            ? (byte)255
            : (byte)(view.Interpolate(x, y) + 0.5f);
    }

    /// <summary>Bilinear sample of the binarised frame, ink as black and everything else as paper.</summary>
    private static byte SampleBits(BbBitMatrix image, float x, float y)
    {
        var x0 = (int)MathF.Floor(x);
        var y0 = (int)MathF.Floor(y);
        var tx = x - x0;
        var ty = y - y0;
        var top = (Paper(image, x0, y0) * (1 - tx)) + (Paper(image, x0 + 1, y0) * tx);
        var bottom = (Paper(image, x0, y0 + 1) * (1 - tx)) + (Paper(image, x0 + 1, y0 + 1) * tx);
        return (byte)((top * (1 - ty)) + (bottom * ty) + 0.5f);

        static float Paper(BbBitMatrix image, int x, int y) => image.GetSafe(x, y) ? 0f : 255f;
    }
}
