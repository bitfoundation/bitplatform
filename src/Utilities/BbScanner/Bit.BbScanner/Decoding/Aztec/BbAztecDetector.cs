using System.Numerics;

namespace Bit.BbScanner;

/// <summary>
/// Finds Aztec Code symbols in a binarised image by their bullseye and samples their module grid.
/// </summary>
/// <remarks>
/// <para>
/// The bullseye is a set of concentric square rings round a dark centre module - two dark rings on a compact symbol,
/// three on a full range one - and any line through its centre crosses them in runs of one module each, whatever
/// way up the symbol is. Rows are scanned for seven such runs between two dark ones; the column, the row again and at
/// least one diagonal through the middle of them must show the same before a candidate is believed.
/// </para>
/// <para>
/// Rays are then cast from the centre in every direction. Along each, the ring edges fall at half a module, one and
/// a half, two and a half and so on, scaled by how obliquely the ray meets the square, which tells a full range
/// bullseye from a compact one and gives points round the middle of the outermost light ring, which no data module
/// touches. Lines fitted to those points make a square, and its corners an affine transform: enough to read the mode
/// message ring, whose orientation modules say which corner is the top left and whose content says how big the
/// symbol is. The symbol's perspective is then fitted from its reference grid where it is large enough to have one,
/// and from its outline otherwise, and the whole grid read through it.
/// </para>
/// </remarks>
internal sealed class BbAztecDetector
{
    /// <summary>Rays cast from a candidate centre.</summary>
    private const int RayCount = 128;

    /// <summary>The ray step, in pixels.</summary>
    private const float RayStep = 0.5f;

    /// <summary>Smallest module, in pixels, a bullseye run may have.</summary>
    private const float MinModuleSize = 1.5f;

    /// <summary>Candidates tried per frame live, and on a still image.</summary>
    private const int LiveCandidates = 4;
    private const int ThoroughCandidates = 16;

    /// <summary>
    /// The most confirmed centres collected from one image. Every sighting is checked against those already found, so
    /// an image tiled with bullseyes - which no photograph of real symbols is, but a file anyone can pick may be -
    /// would otherwise cost the square of their number; once this many are found, the search stops.
    /// </summary>
    private const int MaxCandidates = 64;

    private readonly List<Candidate> _candidates = [];
    private readonly float[] _transitions = new float[RayCount * 6];
    private readonly int[] _transitionCounts = new int[RayCount];
    private readonly bool[] _rayFits = new bool[RayCount];
    private readonly float[] _xs = new float[RayCount];
    private readonly float[] _ys = new float[RayCount];

    /// <summary>A confirmed centre, how many rows led to it, and whether both diagonals through it, not just one, look right.</summary>
    private readonly record struct Candidate(float X, float Y, float ModuleSize, int Votes, bool BothDiagonals);

    /// <summary>Finds and decodes the first readable symbol.</summary>
    /// <param name="image">The binarised image, dark modules set.</param>
    /// <param name="thorough">Scan every row and try more candidates, for a still image.</param>
    /// <returns>The symbol, or <see langword="null"/>.</returns>
    public BbSymbolDecodeResult? Read(BbBitMatrix image, bool thorough)
    {
        FindBullseyes(image, thorough ? 1 : 2);
        var limit = Math.Min(_candidates.Count, thorough ? ThoroughCandidates : LiveCandidates);
        for (var i = 0; i < limit; i++)
        {
            var result = TryCandidate(image, _candidates[i]);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private void FindBullseyes(BbBitMatrix image, int stride)
    {
        _candidates.Clear();
        // The last runs of the row, in a ring: shifting a window along by one for every run costs more, on a noisy
        // frame, than the rest of the search.
        Span<int> runs = stackalloc int[16];
        Span<int> starts = stackalloc int[16];
        var width = image.Width;

        for (var y = stride / 2; y < image.Height && _candidates.Count < MaxCandidates; y += stride)
        {
            var words = image.GetRowWords(y);
            var x = 0;
            var dark = image[0, y];
            var count = 0;
            while (x < width)
            {
                var next = NextChange(words, width, x, dark);

                var slot = count & 15;
                runs[slot] = next - x;
                starts[slot] = x;
                count++;

                // Newest first: a dark run, seven alternating from light, and the dark run they started after.
                if (dark && count >= 9 && LooksLikeBullseye(runs, slot, out var module))
                {
                    var middle = (slot - 4) & 15;
                    Consider(image, starts[middle] + (runs[middle] / 2f), y + 0.5f, module);
                }

                x = next;
                dark = !dark;
            }
        }

        // The likeliest first: a centre both diagonals agree on, then the one most rows led to.
        _candidates.Sort((a, b) => a.BothDiagonals != b.BothDiagonals ? (a.BothDiagonals ? -1 : 1) : b.Votes.CompareTo(a.Votes));
    }

    /// <summary>
    /// Whether the nine runs back from the ring slot <paramref name="newest"/> - the newest dark - are a bullseye's:
    /// seven inner runs of one module each and a dark run either side, which may be wider, merging with dark modules
    /// beyond the bullseye.
    /// </summary>
    private static bool LooksLikeBullseye(ReadOnlySpan<int> runs, int newest, out float module)
    {
        var inner = 0;
        for (var i = 1; i <= 7; i++)
        {
            inner += runs[(newest - i) & 15];
        }

        module = inner / 7f;
        if (module < MinModuleSize)
        {
            return false;
        }

        var tolerance = MathF.Max(module * 0.5f, 1f);
        for (var i = 1; i <= 7; i++)
        {
            if (MathF.Abs(runs[(newest - i) & 15] - module) > tolerance)
            {
                return false;
            }
        }

        return runs[newest] >= module * 0.5f && runs[(newest - 8) & 15] >= module * 0.5f;
    }

    /// <summary>
    /// Confirms a centre found on a row: along the column, along the row again, and along at least one diagonal.
    /// </summary>
    private void Consider(BbBitMatrix image, float x, float y, float module)
    {
        // A row a pixel from one that already led to a confirmed centre crosses the same bullseye.
        if (_candidates.Count >= MaxCandidates || Vote(x, y, module * 0.75f))
        {
            return;
        }

        if (!CrossCheck(image, x, y, 0, 1, module, out var centreY, out var vertical) ||
            !CrossCheck(image, x, centreY, 1, 0, module, out var centreX, out var horizontal))
        {
            return;
        }

        // Turned near a diagonal, a bullseye's rings look like diamonds to rows and columns, and points a few
        // modules off its centre pass both checks; a diagonal through one of those does not. One diagonal is asked
        // for, not both: on a small symbol turned a few degrees, stepping a diagonal pixel by pixel can misjudge a
        // ring corner.
        var falling = CrossCheck(image, centreX, centreY, 1, 1, module, out _, out _);
        var rising = CrossCheck(image, centreX, centreY, 1, -1, module, out _, out _);
        if (!falling && !rising)
        {
            return;
        }

        if (!Vote(centreX, centreY, module))
        {
            _candidates.Add(new Candidate(centreX, centreY, (vertical + horizontal) / 2, 1, falling && rising));
        }
    }

    /// <summary>Counts another sighting of a confirmed centre within <paramref name="distance"/> of a point.</summary>
    private bool Vote(float x, float y, float distance)
    {
        for (var i = 0; i < _candidates.Count; i++)
        {
            var existing = _candidates[i];
            if (MathF.Abs(existing.X - x) < distance && MathF.Abs(existing.Y - y) < distance)
            {
                _candidates[i] = existing with { Votes = existing.Votes + 1 };
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Walks a line through a supposed centre, in steps of one pixel along x, y or a diagonal, measuring the dark
    /// centre run and the three runs either side of it, which must be equal and followed by dark. Gives back the
    /// centre of the middle run, in the coordinate the line moves in (y for a column, x otherwise), and the run width
    /// along the line in pixels.
    /// </summary>
    private static bool CrossCheck(BbBitMatrix image, float x, float y, int dx, int dy, float expected, out float centre, out float module)
    {
        centre = 0;
        module = 0;
        var px = (int)x;
        var py = (int)y;
        if (!image.GetSafe(px, py))
        {
            return false;
        }

        var limit = (int)MathF.Ceiling(expected * 2.5f) + 2;
        Span<int> forward = stackalloc int[4];
        Span<int> backward = stackalloc int[4];
        if (!Walk(image, px, py, dx, dy, limit, forward) || !Walk(image, px, py, -dx, -dy, limit, backward))
        {
            return false;
        }

        // forward[0] and backward[0] both count the centre pixel.
        var middle = forward[0] + backward[0] - 1;
        var total = middle + forward[1] + forward[2] + forward[3] + backward[1] + backward[2] + backward[3];
        module = total / 7f;
        var tolerance = MathF.Max(module * 0.6f, 1.5f);
        if (MathF.Abs(middle - module) > tolerance)
        {
            return false;
        }

        for (var i = 1; i < 4; i++)
        {
            if (MathF.Abs(forward[i] - module) > tolerance || MathF.Abs(backward[i] - module) > tolerance)
            {
                return false;
            }
        }

        // The diagonal steps are longer than a pixel; only the centre's position is used from them.
        var start = dx != 0 ? px : py;
        var step = dx != 0 ? dx : dy;
        centre = start + (step * (forward[0] - backward[0]) / 2f) + 0.5f;
        if (dx != 0 && dy != 0)
        {
            module *= MathF.Sqrt(2);
        }

        return MathF.Abs(module - expected) <= expected * 0.7f + 1f;
    }

    /// <summary>
    /// Counts, from a dark pixel outward, the dark run it is in, then a light, a dark and a light run, and checks that
    /// dark follows.
    /// </summary>
    private static bool Walk(BbBitMatrix image, int x, int y, int dx, int dy, int limit, Span<int> lengths)
    {
        var dark = true;
        for (var run = 0; run < 4; run++)
        {
            var length = 0;
            while (image.GetSafe(x, y) == dark && (uint)x < (uint)image.Width && (uint)y < (uint)image.Height)
            {
                length++;
                if (length > limit)
                {
                    return false;
                }

                x += dx;
                y += dy;
            }

            if (length == 0 || (uint)x >= (uint)image.Width || (uint)y >= (uint)image.Height)
            {
                return false;
            }

            lengths[run] = length;
            dark = !dark;
        }

        // The fourth ring out is dark.
        return image.GetSafe(x, y);
    }

    private BbSymbolDecodeResult? TryCandidate(BbBitMatrix image, Candidate candidate)
    {
        var (full, compact) = CastRays(image, candidate);

        // A full range bullseye also passes as a compact one, so it is tried first when the rays say it may be one;
        // a compact symbol only passes the full range test by accident, which its mode message then refuses. Each is
        // read as printed and then, failing that, as its mirror image: a symbol printed on the back of a transparent
        // label, or seen through one, whose orientation modules then match no turn of the upright pattern.
        foreach (var mirrored in (ReadOnlySpan<bool>)[false, true])
        {
            if (full >= RayCount * 0.7f && TryRead(image, candidate, compact: false, mirrored) is { } fullRange)
            {
                return fullRange;
            }

            if (compact >= RayCount * 0.6f && TryRead(image, candidate, compact: true, mirrored) is { } small)
            {
                return small;
            }
        }

        return null;
    }

    /// <summary>
    /// Casts the rays, recording up to six ring edges along each, and counts the rays whose edges sit where a compact
    /// bullseye's four, and a full range bullseye's six, should.
    /// </summary>
    private (int Full, int Compact) CastRays(BbBitMatrix image, Candidate candidate)
    {
        var reach = candidate.ModuleSize * 7f * 2f;
        var full = 0;
        var compact = 0;
        for (var ray = 0; ray < RayCount; ray++)
        {
            var angle = ray * 2 * MathF.PI / RayCount;
            var (sin, cos) = MathF.SinCos(angle);
            var count = 0;
            var dark = true;
            for (var r = RayStep; r < reach && count < 6; r += RayStep)
            {
                var sample = image.GetSafe((int)(candidate.X + (r * cos)), (int)(candidate.Y + (r * sin)));
                if (sample != dark)
                {
                    _transitions[(ray * 6) + count++] = r - (RayStep / 2);
                    dark = sample;
                }
            }

            _transitionCounts[ray] = count;
            if (Fits(ray, 4))
            {
                compact++;
                if (Fits(ray, 6))
                {
                    full++;
                }
            }
        }

        return (full, compact);
    }

    /// <summary>
    /// Whether the first <paramref name="edges"/> edges of a ray lie at half a module, one and a half and so on, for
    /// one scale that fits them all.
    /// </summary>
    private bool Fits(int ray, int edges)
    {
        if (_transitionCounts[ray] < edges)
        {
            return false;
        }

        float weighted = 0, squares = 0;
        for (var k = 0; k < edges; k++)
        {
            var expected = k + 0.5f;
            weighted += _transitions[(ray * 6) + k] * expected;
            squares += expected * expected;
        }

        var scale = weighted / squares;
        for (var k = 0; k < edges; k++)
        {
            if (MathF.Abs(_transitions[(ray * 6) + k] - (scale * (k + 0.5f))) > MathF.Max(scale * 0.4f, 0.8f))
            {
                return false;
            }
        }

        return true;
    }

    private BbSymbolDecodeResult? TryRead(BbBitMatrix image, Candidate candidate, bool compact, bool mirrored)
    {
        // The middle of the outermost light ring, which no data module touches: the third on a full range symbol, the
        // second on a compact one. A middle, not an edge, because binarisation that fattens dark modules moves the
        // ring's two edges by as much in opposite directions and leaves its middle where it was.
        var edge = compact ? 4 : 6;
        var half = edge - 1f;
        Span<BbScanPoint> square = stackalloc BbScanPoint[4];
        if (!FitSquare(candidate, edge, square))
        {
            return null;
        }

        // The corners in the opposite order are the mirror image's: every fit and sample below then reads the symbol
        // the right way round, and only the corners reported need putting back in the image's own order.
        if (mirrored)
        {
            square.Reverse();
        }

        // Affine, not projective: four corners a few modules apart carry noise that a projective fit would read as
        // perspective, and extrapolate quadratically to the edge of the symbol. Real perspective is fitted below, from
        // the reference grid or the outline.
        var affine = Projection.Affine(square, half);
        bool Module(int u, int v) => affine.IsDark(image, u, v);

        Span<int> sides = stackalloc int[4];
        var turns = BbAztecDecoder.FindRotation(BbAztecDecoder.ReadModeRing(Module, compact, sides));
        if (turns < 0)
        {
            return null;
        }

        Span<int> upright = stackalloc int[4];
        for (var i = 0; i < 4; i++)
        {
            upright[i] = sides[(turns + i) % 4];
        }

        if (!BbAztecDecoder.TryReadModeMessage(upright, compact, out var layers, out var dataCodewords, out var modeErrors))
        {
            return compact ? TryReadRune(image, affine, upright, turns, mirrored) : null;
        }

        var size = BbAztecDecoder.Size(compact, layers);
        var best = affine;
        var gridFitted = false;
        if (!compact && size / 2 >= 16 && RefineWithGrid(image, affine, square, half, size) is { } refined)
        {
            if (TrySample(image, refined, compact, layers, dataCodewords, modeErrors, turns, size, mirrored) is { } gridded)
            {
                return gridded;
            }

            best = refined;
            gridFitted = true;
        }
        else if (TrySample(image, affine, compact, layers, dataCodewords, modeErrors, turns, size, mirrored) is { } flat)
        {
            return flat;
        }

        // Seen at an angle, a symbol without enough reference grid to fit its perspective from needs it from
        // somewhere else, and its outline is the one other thing whose geometry is known.
        if (RefineWithOutline(image, best, square, half, size) is { } outlined &&
            TrySample(image, outlined, compact, layers, dataCodewords, modeErrors, turns, size, mirrored) is { } perspective)
        {
            return perspective;
        }

        return gridFitted ? TrySample(image, affine, compact, layers, dataCodewords, modeErrors, turns, size, mirrored) : null;
    }

    /// <summary>
    /// Fits the symbol's perspective from its outline: along each side, the outer edge of every dark module in the
    /// outermost row, where it meets the quiet zone, is found and a line fitted through those edges; where the lines
    /// cross are the corners, which with the bullseye's fix the transform over the whole symbol. A symbol printed
    /// without a quiet zone, which the specification allows, has no outline to find, and then nothing is returned.
    /// </summary>
    private static Projection? RefineWithOutline(BbBitMatrix image, Projection start, ReadOnlySpan<BbScanPoint> square, float half, int size)
    {
        var edge = size / 2.0;
        Span<Line> lines = stackalloc Line[4];
        Span<float> xs = stackalloc float[size];
        Span<float> ys = stackalloc float[size];
        for (var side = 0; side < 4; side++)
        {
            var count = 0;
            for (var i = 0; i < size; i++)
            {
                // Along the side, and outward from it: side 0 is the top, then the right, the bottom and the left.
                var along = i + 0.5 - edge;
                var (u, v, nu, nv) = side switch
                {
                    0 => (along, -edge, 0.0, -1.0),
                    1 => (edge, along, 1.0, 0.0),
                    2 => (-along, edge, 0.0, 1.0),
                    _ => (-edge, -along, -1.0, 0.0),
                };

                if (OuterEdge(image, start, u, v, nu, nv) is { } point)
                {
                    xs[count] = point.X;
                    ys[count] = point.Y;
                    count++;
                }
            }

            // The side's outward direction in the image, a module long.
            var middle = start.Map(side switch { 0 => (0, -edge), 1 => (edge, 0), 2 => (0, edge), _ => (-edge, 0) });
            var beyond = start.Map(side switch { 0 => (0, -edge - 1), 1 => (edge + 1, 0), 2 => (0, edge + 1), _ => (-edge - 1, 0) });

            // Half the outermost row is dark on average; a side with too few edges is no side at all.
            if (count < Math.Max(4, size / 4) ||
                FitLine(xs[..count], ys[..count], beyond.X - middle.X, beyond.Y - middle.Y) is not { } line)
            {
                return null;
            }

            lines[side] = line;
        }

        var points = new List<(double U, double V, double X, double Y)>
        {
            (half, -half, square[0].X, square[0].Y),
            (half, half, square[1].X, square[1].Y),
            (-half, half, square[2].X, square[2].Y),
            (-half, -half, square[3].X, square[3].Y),
        };

        // Corner k is between side k - 1 and side k: top left, top right, bottom right, bottom left.
        ReadOnlySpan<int> cu = [-1, 1, 1, -1];
        ReadOnlySpan<int> cv = [-1, -1, 1, 1];
        for (var corner = 0; corner < 4; corner++)
        {
            if (Intersect(lines[(corner + 3) % 4], lines[corner]) is not { } at)
            {
                return null;
            }

            // An outline that puts a corner more than two modules from where the bullseye does belongs to something
            // else - a border round the symbol, or a neighbour.
            var predicted = start.Map(cu[corner] * edge, cv[corner] * edge);
            var module = Distance(start.Map(0, 0), start.Map(1, 0));
            if (Distance(predicted, at) > 2.5 * module * (1 + (size / 40.0)))
            {
                return null;
            }

            points.Add((cu[corner] * edge, cv[corner] * edge, at.X, at.Y));
        }

        return Projection.Fit(points);
    }

    /// <summary>
    /// Where the symbol ends across the point (<paramref name="u"/>, <paramref name="v"/>) of its predicted edge, whose
    /// outward normal is (<paramref name="nu"/>, <paramref name="nv"/>): the last dark pixel before at least a module
    /// and a half of light, searched from a module and a half inside the predicted edge to three outside it, which
    /// covers the drift of a prediction made without perspective.
    /// </summary>
    private static BbScanPoint? OuterEdge(BbBitMatrix image, Projection projection, double u, double v, double nu, double nv)
    {
        var inner = projection.Map(u - (1.5 * nu), v - (1.5 * nv));
        var outer = projection.Map(u + (3 * nu), v + (3 * nv));
        var dx = outer.X - inner.X;
        var dy = outer.Y - inner.Y;
        var length = MathF.Sqrt((dx * dx) + (dy * dy));
        if (length < 2)
        {
            return null;
        }

        var steps = (int)(length * 2);
        var module = length / 4.5f;
        var lastDark = -1f;
        var lightRun = 0f;
        for (var s = 0; s <= steps; s++)
        {
            var t = s / (float)steps;
            var x = inner.X + (t * dx);
            var y = inner.Y + (t * dy);
            if (x >= 0 && y >= 0 && image.GetSafe((int)x, (int)y))
            {
                lastDark = t;
                lightRun = 0;
            }
            else
            {
                lightRun += length / steps;
            }
        }

        // The edge must be inside the search, and followed by quiet zone to the end of it.
        if (lastDark <= 0 || lightRun < module * 1.5f)
        {
            return null;
        }

        var edgeT = lastDark + (0.5f / steps);
        return new BbScanPoint(inner.X + (edgeT * dx), inner.Y + (edgeT * dy));
    }

    /// <summary>
    /// The line along the outer edge of a set of edge points. A column whose outermost module is light gives the edge
    /// of a module further in, so after a first fit through every point the line is refitted, twice, through those
    /// no more than half a module inside it along the outward direction (<paramref name="ox"/>, <paramref name="oy"/>),
    /// which is a module long: it settles on the outermost points, which are the outline.
    /// </summary>
    private static Line? FitLine(ReadOnlySpan<float> xs, ReadOnlySpan<float> ys, float ox, float oy)
    {
        var module = MathF.Sqrt((ox * ox) + (oy * oy));
        if (module < 1e-3f)
        {
            return null;
        }

        ox /= module;
        oy /= module;
        Line? line = null;
        Span<bool> keep = stackalloc bool[xs.Length];
        keep.Fill(true);
        for (var pass = 0; pass < 3; pass++)
        {
            float sx = 0, sy = 0;
            var n = 0;
            for (var i = 0; i < xs.Length; i++)
            {
                if (keep[i])
                {
                    sx += xs[i];
                    sy += ys[i];
                    n++;
                }
            }

            if (n < 3)
            {
                return line;
            }

            var mx = sx / n;
            var my = sy / n;
            float sxx = 0, syy = 0, sxy = 0;
            for (var i = 0; i < xs.Length; i++)
            {
                if (keep[i])
                {
                    sxx += (xs[i] - mx) * (xs[i] - mx);
                    syy += (ys[i] - my) * (ys[i] - my);
                    sxy += (xs[i] - mx) * (ys[i] - my);
                }
            }

            var theta = 0.5f * MathF.Atan2(2 * sxy, sxx - syy);
            line = new Line(mx, my, MathF.Cos(theta), MathF.Sin(theta));
            for (var i = 0; i < xs.Length; i++)
            {
                // How far outward of the line the point is.
                var outward = ((xs[i] - mx) * ox) + ((ys[i] - my) * oy);
                keep[i] = outward >= -0.5f * module;
            }
        }

        return line;
    }

    private static float Distance(BbScanPoint a, BbScanPoint b) =>
        MathF.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    /// <summary>
    /// Reads the compact core as an Aztec Rune. A rune has no data layers, so the two rings just outside the core are
    /// its quiet zone, where a compact symbol has its first layer of data: they have to be light, which keeps a damaged
    /// compact mode message from passing as a rune.
    /// </summary>
    private static BbSymbolDecodeResult? TryReadRune(BbBitMatrix image, Projection projection, ReadOnlySpan<int> upright, int turns, bool mirrored)
    {
        if (!BbAztecDecoder.TryReadRune(upright, out var value, out var errors))
        {
            return null;
        }

        var dark = 0;
        var total = 0;
        for (var ring = 6; ring <= 7; ring++)
        {
            for (var i = -ring; i < ring; i++)
            {
                foreach (var (u, v) in (ReadOnlySpan<(int, int)>)[(i, -ring), (ring, i), (-i, ring), (-ring, -i)])
                {
                    total++;
                    if (projection.IsDark(image, u, v))
                    {
                        dark++;
                    }
                }
            }
        }

        if (dark * 10 > total)
        {
            return null;
        }

        var payload = new BbDecoderResult([(byte)value], value.ToString(System.Globalization.CultureInfo.InvariantCulture))
        {
            SymbolVersion = "Rune",
            ErrorsCorrected = errors,
        };

        return new BbSymbolDecodeResult(payload, BbBarcodeFormat.Aztec, Corners(projection, 11, turns, mirrored));
    }

    /// <summary>
    /// The symbol's corners in the image, clockwise from its own top left - which, for a symbol read mirrored, are the
    /// projection's in the other direction.
    /// </summary>
    private static BbScanPoint[] Corners(Projection projection, int size, int turns, bool mirrored)
    {
        var halfSize = size / 2f;
        var corners = new BbScanPoint[4];
        corners[0] = projection.Map(Turn(-halfSize, -halfSize, turns));
        corners[1] = projection.Map(Turn(halfSize, -halfSize, turns));
        corners[2] = projection.Map(Turn(halfSize, halfSize, turns));
        corners[3] = projection.Map(Turn(-halfSize, halfSize, turns));
        return mirrored ? [corners[0], corners[3], corners[2], corners[1]] : corners;
    }

    private static BbSymbolDecodeResult? TrySample(
        BbBitMatrix image, Projection projection, bool compact, int layers, int dataCodewords, int modeErrors, int turns, int size, bool mirrored)
    {
        var halfSize = size / 2f;
        var corners = new BbScanPoint[4];
        corners[0] = projection.Map(Turn(-halfSize, -halfSize, turns));
        corners[1] = projection.Map(Turn(halfSize, -halfSize, turns));
        corners[2] = projection.Map(Turn(halfSize, halfSize, turns));
        corners[3] = projection.Map(Turn(-halfSize, halfSize, turns));

        // Four points determine a perspective transform exactly, so this is the projection itself, in grid units.
        var sampling = BbPerspectiveTransform.QuadrilateralToQuadrilateral(
            0, 0, size, 0, size, size, 0, size,
            corners[0].X, corners[0].Y, corners[1].X, corners[1].Y, corners[2].X, corners[2].Y, corners[3].X, corners[3].Y);
        using var grid = BbGridSampler.Sample(image, size, size, sampling);
        if (grid is null || BbAztecDecoder.Decode(grid, compact, layers, dataCodewords, modeErrors) is not { } payload)
        {
            return null;
        }

        return new BbSymbolDecodeResult(payload, BbBarcodeFormat.Aztec, mirrored ? [corners[0], corners[3], corners[2], corners[1]] : corners);
    }

    /// <summary>
    /// Fits the symbol's perspective from its reference grid: the lines of alternating modules through the centre
    /// and every sixteen modules out from it, where a module is dark exactly when its coordinate along the line is
    /// even. Each crossing of two lines is looked for close to where the current fit puts it, a ring of crossings at
    /// a time outward, and the fit is redone from everything found so far before the next ring is predicted.
    /// </summary>
    /// <returns>The refined projection, or <see langword="null"/> when no crossing could be placed.</returns>
    private static Projection? RefineWithGrid(BbBitMatrix image, Projection start, ReadOnlySpan<BbScanPoint> square, float half, int size)
    {
        var points = new List<(double U, double V, double X, double Y)>
        {
            (half, -half, square[0].X, square[0].Y),
            (half, half, square[1].X, square[1].Y),
            (-half, half, square[2].X, square[2].Y),
            (-half, -half, square[3].X, square[3].Y),
        };

        var reach = (size - 1) / 2;
        var current = start;
        var found = 0;
        for (var ring = 16; ring <= reach; ring += 16)
        {
            for (var gy = -ring; gy <= ring; gy += 16)
            {
                for (var gx = -ring; gx <= ring; gx += 16)
                {
                    if (Math.Max(Math.Abs(gx), Math.Abs(gy)) != ring)
                    {
                        continue;
                    }

                    if (LocateCrossing(image, current, gx, gy, reach) is { } at)
                    {
                        points.Add((gx, gy, at.X, at.Y));
                        found++;
                    }
                }
            }

            if (Projection.Fit(points) is { } fitted)
            {
                current = fitted;
            }
        }

        return found > 0 ? current : null;
    }

    /// <summary>
    /// Finds where the reference grid crossing at (<paramref name="gx"/>, <paramref name="gy"/>) really is: the offset,
    /// within three quarters of a module of the prediction, at which the modules of both lines either side of it read
    /// as they should. Every offset that reads best is averaged, which puts the estimate in the middle of the range of
    /// positions that sample the right modules.
    /// </summary>
    private static BbScanPoint? LocateCrossing(BbBitMatrix image, Projection projection, int gx, int gy, int reach)
    {
        const int Arm = 4;
        var best = -1;
        double sumU = 0, sumV = 0;
        var ties = 0;
        var total = 0;
        for (var du = -0.75; du <= 0.751; du += 0.25)
        {
            for (var dv = -0.75; dv <= 0.751; dv += 0.25)
            {
                var matches = 0;
                total = 0;
                for (var k = -Arm; k <= Arm; k++)
                {
                    var expected = (k & 1) == 0;
                    if (Math.Abs(gx + k) <= reach)
                    {
                        total++;
                        matches += projection.IsDark(image, gx + k + du, gy + dv) == expected ? 1 : 0;
                    }

                    if (k != 0 && Math.Abs(gy + k) <= reach)
                    {
                        total++;
                        matches += projection.IsDark(image, gx + du, gy + k + dv) == expected ? 1 : 0;
                    }
                }

                if (matches > best)
                {
                    best = matches;
                    sumU = du;
                    sumV = dv;
                    ties = 1;
                }
                else if (matches == best)
                {
                    sumU += du;
                    sumV += dv;
                    ties++;
                }
            }
        }

        // Two misread modules are allowed for; more and the crossing is not where it was looked for.
        if (best < total - 2)
        {
            return null;
        }

        return projection.Map(gx + (sumU / ties), gy + (sumV / ties));
    }

    /// <summary>Where a point of the upright symbol is in the coordinates the square was fitted in.</summary>
    private static BbScanPoint Turn(float u, float v, int turns) => turns switch
    {
        0 => new BbScanPoint(u, v),
        1 => new BbScanPoint(-v, u),
        2 => new BbScanPoint(-u, -v),
        _ => new BbScanPoint(v, -u),
    };

    /// <summary>
    /// Fits the square halfway between the edges before and at the <paramref name="edge"/>th along every ray, and
    /// gives its corners in the order (+half, -half), (+half, +half), (-half, +half), (-half, -half) of module
    /// coordinates about the centre: the first side's normal is the angle the fit settled on, the second a quarter
    /// turn clockwise from it, and so on.
    /// </summary>
    private bool FitSquare(Candidate candidate, int edge, Span<BbScanPoint> corners)
    {
        // Edge points, and the scale of each ray: a square seen square on is crossed at its module size along its
        // axes and at the square root of two times that along its diagonals.
        Span<float> scales = stackalloc float[RayCount];
        var points = 0;
        for (var ray = 0; ray < RayCount; ray++)
        {
            _rayFits[ray] = Fits(ray, edge);
            if (!_rayFits[ray])
            {
                continue;
            }

            var radius = (_transitions[(ray * 6) + edge - 2] + _transitions[(ray * 6) + edge - 1]) / 2;
            var angle = ray * 2 * MathF.PI / RayCount;
            var (sin, cos) = MathF.SinCos(angle);
            _xs[ray] = candidate.X + (radius * cos);
            _ys[ray] = candidate.Y + (radius * sin);
            scales[ray] = radius;
            points++;
        }

        if (points < RayCount / 2)
        {
            return false;
        }

        // The square's rotation: the angle at which scaling each ray back by its obliquity agrees best.
        var bestAngle = 0f;
        var bestSpread = float.MaxValue;
        for (var step = 0; step < 90; step++)
        {
            var phi = step * MathF.PI / 180;
            float sum = 0, sumSquares = 0;
            var n = 0;
            for (var ray = 0; ray < RayCount; ray++)
            {
                if (!_rayFits[ray])
                {
                    continue;
                }

                var relative = (ray * 2 * MathF.PI / RayCount) - phi;
                var value = scales[ray] * MathF.Max(MathF.Abs(MathF.Cos(relative)), MathF.Abs(MathF.Sin(relative)));
                sum += value;
                sumSquares += value * value;
                n++;
            }

            var mean = sum / n;
            var spread = (sumSquares / n) - (mean * mean);
            if (spread < bestSpread)
            {
                bestSpread = spread;
                bestAngle = phi;
            }
        }

        // Each side's points, leaving out those near a corner, where a ray may meet either side.
        Span<Line> lines = stackalloc Line[4];
        for (var side = 0; side < 4; side++)
        {
            var sideAngle = bestAngle + (side * MathF.PI / 2);
            float sx = 0, sy = 0;
            var n = 0;
            for (var pass = 0; pass < 2; pass++)
            {
                float sxx = 0, syy = 0, sxy = 0;
                for (var ray = 0; ray < RayCount; ray++)
                {
                    if (!_rayFits[ray] || !OnSide(ray, sideAngle))
                    {
                        continue;
                    }

                    if (pass == 0)
                    {
                        sx += _xs[ray];
                        sy += _ys[ray];
                        n++;
                    }
                    else
                    {
                        var ex = _xs[ray] - (sx / n);
                        var ey = _ys[ray] - (sy / n);
                        sxx += ex * ex;
                        syy += ey * ey;
                        sxy += ex * ey;
                    }
                }

                if (n < 3)
                {
                    return false;
                }

                if (pass == 1)
                {
                    // The principal axis of the points is the side's direction.
                    var theta = 0.5f * MathF.Atan2(2 * sxy, sxx - syy);
                    lines[side] = new Line(sx / n, sy / n, MathF.Cos(theta), MathF.Sin(theta));
                }
            }
        }

        // The corner between sides 3 and 0 is the square's (+half, -half), and so round.
        for (var side = 0; side < 4; side++)
        {
            if (Intersect(lines[(side + 3) % 4], lines[side]) is not { } corner)
            {
                return false;
            }

            corners[side] = corner;
        }

        return true;
    }

    /// <summary>Whether a ray points well inside the side facing <paramref name="sideAngle"/>.</summary>
    private static bool OnSide(int ray, float sideAngle)
    {
        var relative = (ray * 2 * MathF.PI / RayCount) - sideAngle;
        relative = MathF.IEEERemainder(relative, 2 * MathF.PI);
        return MathF.Abs(relative) < 33 * MathF.PI / 180;
    }

    private readonly record struct Line(float X, float Y, float Dx, float Dy);

    private static BbScanPoint? Intersect(Line a, Line b)
    {
        var denominator = (a.Dx * b.Dy) - (a.Dy * b.Dx);
        if (MathF.Abs(denominator) < 1e-4f)
        {
            return null;
        }

        var t = (((b.X - a.X) * b.Dy) - ((b.Y - a.Y) * b.Dx)) / denominator;
        return new BbScanPoint(a.X + (t * a.Dx), a.Y + (t * a.Dy));
    }

    /// <summary>
    /// A perspective transform from module coordinates about the symbol's centre to image pixels, in double precision:
    /// a full range symbol reaches 75 modules from its centre, and a fit is only as good as its arithmetic there.
    /// </summary>
    private readonly struct Projection
    {
        private readonly double _a, _b, _c, _d, _e, _f, _g, _h;

        private Projection(double a, double b, double c, double d, double e, double f, double g, double h)
        {
            (_a, _b, _c, _d, _e, _f, _g, _h) = (a, b, c, d, e, f, g, h);
        }

        public BbScanPoint Map(double u, double v)
        {
            var w = (_g * u) + (_h * v) + 1;
            return new BbScanPoint((float)(((_a * u) + (_b * v) + _c) / w), (float)(((_d * u) + (_e * v) + _f) / w));
        }

        public BbScanPoint Map(BbScanPoint point) => Map(point.X, point.Y);

        public BbScanPoint Map((double U, double V) point) => Map(point.U, point.V);

        /// <summary>Whether the module centred at (<paramref name="u"/>, <paramref name="v"/>) is dark.</summary>
        public bool IsDark(BbBitMatrix image, double u, double v)
        {
            var point = Map(u, v);
            return point.X >= 0 && point.Y >= 0 && image.GetSafe((int)point.X, (int)point.Y);
        }

        /// <summary>
        /// The least squares affine transform taking the square of half size <paramref name="half"/> onto
        /// <paramref name="corners"/>, given in the order <see cref="FitSquare"/> gives them.
        /// </summary>
        public static Projection Affine(ReadOnlySpan<BbScanPoint> corners, float half)
        {
            // Corner i sits at (su[i], sv[i]) * half; with the corners symmetric about the centre the normal
            // equations separate, and each axis is the signed sum of the corners.
            ReadOnlySpan<int> su = [1, 1, -1, -1];
            ReadOnlySpan<int> sv = [-1, 1, 1, -1];
            double cx = 0, cy = 0, ux = 0, uy = 0, vx = 0, vy = 0;
            for (var i = 0; i < 4; i++)
            {
                cx += corners[i].X;
                cy += corners[i].Y;
                ux += su[i] * corners[i].X;
                uy += su[i] * corners[i].Y;
                vx += sv[i] * corners[i].X;
                vy += sv[i] * corners[i].Y;
            }

            var scale = 4 * half;
            return new Projection(ux / scale, vx / scale, cx / 4, uy / scale, vy / scale, cy / 4, 0, 0);
        }

        /// <summary>
        /// The least squares perspective transform through module and image point pairs, or <see langword="null"/>
        /// when they do not determine one. Both sides are centred and scaled first, which keeps the normal equations
        /// well conditioned whatever the size of the image.
        /// </summary>
        public static Projection? Fit(List<(double U, double V, double X, double Y)> points)
        {
            if (points.Count < 5)
            {
                return null;
            }

            double mu = 0, mv = 0, mx = 0, my = 0;
            foreach (var p in points)
            {
                mu += p.U;
                mv += p.V;
                mx += p.X;
                my += p.Y;
            }

            mu /= points.Count;
            mv /= points.Count;
            mx /= points.Count;
            my /= points.Count;
            double su = 0, sx = 0;
            foreach (var p in points)
            {
                su += Math.Sqrt(((p.U - mu) * (p.U - mu)) + ((p.V - mv) * (p.V - mv)));
                sx += Math.Sqrt(((p.X - mx) * (p.X - mx)) + ((p.Y - my) * (p.Y - my)));
            }

            su = su > 0 ? points.Count / su : 1;
            sx = sx > 0 ? points.Count / sx : 1;

            // Unknowns a..h of x = (a u + b v + c) / (g u + h v + 1), and likewise y with d, e, f.
            Span<double> normal = stackalloc double[8 * 9];
            normal.Clear();
            Span<double> row = stackalloc double[9];
            foreach (var p in points)
            {
                var u = (p.U - mu) * su;
                var v = (p.V - mv) * su;
                var x = (p.X - mx) * sx;
                var y = (p.Y - my) * sx;
                for (var equation = 0; equation < 2; equation++)
                {
                    row.Clear();
                    var target = equation == 0 ? x : y;
                    var offset = equation * 3;
                    row[offset] = u;
                    row[offset + 1] = v;
                    row[offset + 2] = 1;
                    row[6] = -u * target;
                    row[7] = -v * target;
                    row[8] = target;
                    for (var i = 0; i < 8; i++)
                    {
                        for (var j = 0; j < 9; j++)
                        {
                            normal[(i * 9) + j] += row[i] * row[j];
                        }
                    }
                }
            }

            Span<double> solution = stackalloc double[8];
            if (!Solve(normal, solution))
            {
                return null;
            }

            // Undo the normalisation: H = Tx^-1 * Hn * Tu, with Tu and Tx the centring and scaling of each side.
            var (a, b, c, d, e, f, g, h) = (solution[0], solution[1], solution[2], solution[3], solution[4], solution[5], solution[6], solution[7]);
            var g2 = g * su;
            var h2 = h * su;
            var w0 = 1 - (g * su * mu) - (h * su * mv);
            var a2 = ((a * su) / sx) + (mx * g2);
            var b2 = ((b * su) / sx) + (mx * h2);
            var c2 = ((c - (a * su * mu) - (b * su * mv)) / sx) + (mx * w0);
            var d2 = ((d * su) / sx) + (my * g2);
            var e2 = ((e * su) / sx) + (my * h2);
            var f2 = ((f - (d * su * mu) - (e * su * mv)) / sx) + (my * w0);
            if (Math.Abs(w0) < 1e-12)
            {
                return null;
            }

            return new Projection(a2 / w0, b2 / w0, c2 / w0, d2 / w0, e2 / w0, f2 / w0, g2 / w0, h2 / w0);
        }

        /// <summary>Gaussian elimination with partial pivoting on an 8 by 9 augmented matrix.</summary>
        private static bool Solve(Span<double> m, Span<double> solution)
        {
            for (var column = 0; column < 8; column++)
            {
                var pivot = column;
                for (var r = column + 1; r < 8; r++)
                {
                    if (Math.Abs(m[(r * 9) + column]) > Math.Abs(m[(pivot * 9) + column]))
                    {
                        pivot = r;
                    }
                }

                if (Math.Abs(m[(pivot * 9) + column]) < 1e-12)
                {
                    return false;
                }

                if (pivot != column)
                {
                    for (var j = 0; j < 9; j++)
                    {
                        (m[(column * 9) + j], m[(pivot * 9) + j]) = (m[(pivot * 9) + j], m[(column * 9) + j]);
                    }
                }

                for (var r = column + 1; r < 8; r++)
                {
                    var factor = m[(r * 9) + column] / m[(column * 9) + column];
                    for (var j = column; j < 9; j++)
                    {
                        m[(r * 9) + j] -= factor * m[(column * 9) + j];
                    }
                }
            }

            for (var r = 7; r >= 0; r--)
            {
                var value = m[(r * 9) + 8];
                for (var j = r + 1; j < 8; j++)
                {
                    value -= m[(r * 9) + j] * solution[j];
                }

                solution[r] = value / m[(r * 9) + r];
            }

            return true;
        }
    }

    /// <summary>The first pixel at or after <paramref name="from"/> whose colour is not <paramref name="dark"/>.</summary>
    private static int NextChange(ReadOnlySpan<uint> words, int width, int from, bool dark)
    {
        var word = from >> 5;
        var current = (dark ? ~words[word] : words[word]) & ~((1u << (from & 31)) - 1);
        while (current == 0)
        {
            if (++word == words.Length)
            {
                return width;
            }

            current = dark ? ~words[word] : words[word];
        }

        return Math.Min(width, (word << 5) + BitOperations.TrailingZeroCount(current));
    }
}
