using System.Numerics;

namespace Bit.BbScanner;

/// <summary>
/// Reads MicroPDF417 (ISO/IEC 24728) symbols.
/// </summary>
/// <remarks>
/// <para>
/// MicroPDF417 has nothing a detector can find from afar - no finder pattern, no start or stop pattern - so the reader
/// works from what every row does carry: its row address patterns (see <see cref="BbMicroPdf417Tables"/>). Lines are
/// scanned across the image at a set of angles, and wherever a line crosses a RAP the codewords next to it are read
/// along with it. A RAP names its row, given the symbol's size, and a codeword's position next to a RAP names its
/// column, so what many lines read - none of them needing to follow a whole row - is gathered into a grid of codewords,
/// the size chosen that explains every RAP seen. The grid then goes through Reed-Solomon over GF(929), which corrects
/// what was misread and fills in what was not read, and the bit stream is PDF417's.
/// </para>
/// <para>
/// A line only reads a codeword it crosses within one row, two modules tall in the smallest symbols, so the lines have
/// to run within a degree or so of the rows. Live frames try lines along the rows of a symbol held upright, sideways or
/// upside down, and up to six degrees either side within four frames; a still image sweeps every angle.
/// </para>
/// </remarks>
public sealed class BbMicroPdf417Reader : IBbMatrixDecoder
{
    /// <summary>
    /// Offsets from upright and sideways tried on live frames, in degrees, two a frame after upright itself: a row two
    /// modules tall and a hundred long has to be crossed within a degree of its slope, so the offsets are tried a degree
    /// and a half apart, and every one of them within four frames - a tenth of a second at thirty frames a second.
    /// </summary>
    private static readonly float[] LiveOffsets = [-1.5f, 1.5f, -3f, 3f, -4.5f, 4.5f, -6f, 6f];

    /// <summary>
    /// The least score a size needs before its grid is put together: a few RAPs confirmed by the codewords next to them,
    /// which the smallest symbol read from a handful of lines already gives, and stray reads in the bars of other
    /// symbols do not.
    /// </summary>
    private const int MinimumScore = 4;

    /// <summary>The spacing, in pixels, of the lines that look for a piece of a row before the rest are scanned.</summary>
    private const float ProbeSpacing = 6.5f;

    /// <summary>The step between angles a still image is swept with, in degrees.</summary>
    private const float ThoroughStep = 1.5f;

    private static readonly Dictionary<int, int> SideIndex = Index(BbMicroPdf417Tables.SidePatterns);
    private static readonly Dictionary<int, int> CentreIndex = Index(BbMicroPdf417Tables.CentrePatterns);

    private readonly BbPdf417ErrorCorrection _errorCorrection = new();
    private int _liveFrame;
    private readonly List<Segment> _forward = [];
    private readonly List<Segment> _backward = [];
    private List<Segment> _segments = [];
    private readonly List<Token> _tokens = [];
    private int[] _runs = new int[1024];

    // How many rows' edges the last symbol read was located from.
    private int _edgesRead;
    private float[] _starts = new float[1025];
    private int[] _reversedRuns = new int[1024];
    private float[] _reversedStarts = new float[1025];

    /// <inheritdoc />
    public BbBarcodeFormat Formats => BbBarcodeFormat.MicroPdf417;

    /// <inheritdoc />
    public BbSymbolDecodeResult? Decode(BbBitMatrix image, BbMatrixDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);

        var result = DecodeAtAnyAngle(image, options.TryHarder);
        if (result is not null || !options.AllowInverted)
        {
            return result;
        }

        image.Invert();
        try
        {
            return DecodeAtAnyAngle(image, options.TryHarder);
        }
        finally
        {
            image.Invert();
        }
    }

    private BbSymbolDecodeResult? DecodeAtAnyAngle(BbBitMatrix image, bool thorough)
    {
        if (thorough)
        {
            for (var angle = 0f; angle < 180f; angle += ThoroughStep)
            {
                if (DecodeAlong(image, angle, spacing: 1f) is not { } found)
                {
                    continue;
                }

                // A symbol reads from lines a degree or so off its rows, and its corners come out best from lines along
                // them, which read the most of its rows' edges: the angles either side of the one that read are tried
                // too, and the one that read the most edges kept.
                var best = found;
                var bestEdges = _edgesRead;
                foreach (var nudge in (ReadOnlySpan<float>)[-1f, -0.5f, 0.5f, 1f])
                {
                    if (DecodeAlong(image, angle + nudge, spacing: 1f) is { } nudged && _edgesRead > bestEdges)
                    {
                        best = nudged;
                        bestEdges = _edgesRead;
                    }
                }

                return best;
            }

            return null;
        }

        var frame = _liveFrame++ & 3;
        Span<float> offsets = [0f, LiveOffsets[frame * 2], LiveOffsets[(frame * 2) + 1]];
        foreach (var offset in offsets)
        {
            if (DecodeAlong(image, offset, spacing: 2f) is { } horizontal)
            {
                return horizontal;
            }

            if (DecodeAlong(image, 90f + offset, spacing: 2f) is { } vertical)
            {
                return vertical;
            }
        }

        return null;
    }

    /// <summary>
    /// Scans parallel lines at <paramref name="degrees"/>, each read both ways, and reads a symbol whose rows run along
    /// them. Lines <see cref="ProbeSpacing"/> apart are scanned first, and the rest only when one of those read a piece of a row: two RAPs
    /// naming the same row of some symbol size, with as many codewords of that row's cluster between them as the size
    /// has there. The bars of other symbols make a RAP or a codeword now and then; that, they practically never do.
    /// </summary>
    private BbSymbolDecodeResult? DecodeAlong(BbBitMatrix image, float degrees, float spacing)
    {
        var radians = degrees * MathF.PI / 180f;
        var direction = new Vector2(MathF.Cos(radians), MathF.Sin(radians));
        var normal = new Vector2(-direction.Y, direction.X);
        var centre = new Vector2((image.Width - 1) / 2f, (image.Height - 1) / 2f);
        var reach = MathF.Sqrt((image.Width * image.Width) + (image.Height * image.Height)) / 2f;

        // Rows are four pixels tall at the least, so these lines cross every other one of them; a spacing of no whole
        // number of pixels keeps them from all falling on the edges between rows as tall as it.
        Scan(image, centre, direction, normal, reach, ProbeSpacing);
        if (!HasRowPiece(_forward) && !HasRowPiece(_backward))
        {
            return null;
        }

        Scan(image, centre, direction, normal, reach, spacing);
        return (_forward.Count > 0 ? Assemble(_forward, direction) : null)
            ?? (_backward.Count > 0 ? Assemble(_backward, -direction) : null);
    }

    private void Scan(BbBitMatrix image, Vector2 centre, Vector2 direction, Vector2 normal, float reach, float spacing)
    {
        _forward.Clear();
        _backward.Clear();
        _tokens.Clear();
        for (var offset = -reach; offset <= reach; offset += spacing)
        {
            ScanLine(image, centre + (normal * offset), direction);
        }
    }

    private bool HasRowPiece(List<Segment> segments)
    {
        foreach (var segment in segments)
        {
            for (var i = 0; i < segment.Count; i++)
            {
                if (IsRowPiece(segment, i))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Whether the RAP at <paramref name="i"/>, the codewords after it and the RAP after them are a piece of a row of
    /// some symbol size: as many codewords as that size has between the two patterns, all of the cluster of the row
    /// both patterns name.
    /// </summary>
    private bool IsRowPiece(Segment segment, int i)
    {
        var first = _tokens[segment.Start + i];
        if (first.IsCodeword)
        {
            return false;
        }

        var j = i + 1;
        var cluster = -1;
        while (j < segment.Count && _tokens[segment.Start + j].IsCodeword)
        {
            var token = _tokens[segment.Start + j];
            if (cluster >= 0 && token.Cluster != cluster)
            {
                return false;
            }

            cluster = token.Cluster;
            j++;
        }

        var codewords = j - i - 1;
        if (codewords == 0 || j >= segment.Count)
        {
            return false;
        }

        var second = _tokens[segment.Start + j];
        var opening = first.Set == 1 ? BbMicroPdf417RapKind.Centre : BbMicroPdf417RapKind.Left;
        var closing = second.Set == 1 ? BbMicroPdf417RapKind.Centre : BbMicroPdf417RapKind.Right;
        if (opening == BbMicroPdf417RapKind.Centre && closing == BbMicroPdf417RapKind.Centre)
        {
            return false;
        }

        foreach (var variant in BbMicroPdf417Tables.Variants)
        {
            var expected = (opening, closing) switch
            {
                (BbMicroPdf417RapKind.Left, BbMicroPdf417RapKind.Right) => variant.Columns,
                (BbMicroPdf417RapKind.Left, _) => variant.Columns >= 3 ? variant.Columns - 2 : -1,
                _ => variant.Columns >= 3 ? 2 : -1,
            };

            if (codewords != expected)
            {
                continue;
            }

            var row = variant.RowOf(opening, first.Value);
            if (row >= 0 && variant.RowOf(closing, second.Value) == row && variant.Cluster(row) == cluster)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Reads the line through <paramref name="through"/> along <paramref name="direction"/> into runs, and finds the
    /// chains of RAPs and codewords on it read forwards, then backwards.
    /// </summary>
    private void ScanLine(BbBitMatrix image, Vector2 through, Vector2 direction)
    {
        if (!TryClip(through, direction, image.Width, image.Height, out var from, out var to))
        {
            return;
        }

        // Fixed point, sixteen bits of fraction: a step is added exactly, so the samples cannot drift off the line however
        // long it is, and each is clamped, so the last one cannot round past the image's edge.
        var maxX = image.Width - 1;
        var maxY = image.Height - 1;
        var fixedX = (long)MathF.Round((through.X + (direction.X * from) + 0.5f) * 65536f);
        var fixedY = (long)MathF.Round((through.Y + (direction.Y * from) + 0.5f) * 65536f);
        var stepX = (long)MathF.Round(direction.X * 65536f);
        var stepY = (long)MathF.Round(direction.Y * 65536f);
        var colour = image[Math.Clamp((int)(fixedX >> 16), 0, maxX), Math.Clamp((int)(fixedY >> 16), 0, maxY)];
        var firstDark = colour;
        var count = 0;
        _runs[0] = 0;
        _starts[0] = 0;
        var length = (int)(to - from);
        for (var t = 0; t <= length; t++, fixedX += stepX, fixedY += stepY)
        {
            var dark = image[Math.Clamp((int)(fixedX >> 16), 0, maxX), Math.Clamp((int)(fixedY >> 16), 0, maxY)];
            if (dark != colour)
            {
                if (++count >= _runs.Length - 1)
                {
                    Array.Resize(ref _runs, _runs.Length * 2);
                    Array.Resize(ref _starts, _starts.Length * 2);
                    Array.Resize(ref _reversedRuns, _runs.Length);
                    Array.Resize(ref _reversedStarts, _starts.Length);
                }

                _runs[count] = 0;
                _starts[count] = t;
                colour = dark;
            }

            _runs[count]++;
        }

        var runs = count + 1;
        _starts[runs] = _starts[count] + _runs[count];
        var origin = through + (direction * from);
        Parse(runs, firstDark, origin, direction, _forward);

        // The same runs backwards, from the far end of the line: a symbol upside down to this direction reads that way.
        var end = _starts[runs];
        for (var k = 0; k < runs; k++)
        {
            _reversedRuns[k] = _runs[runs - 1 - k];
            _reversedStarts[k] = end - _starts[runs - k];
        }

        _reversedStarts[runs] = end;
        (_runs, _reversedRuns) = (_reversedRuns, _runs);
        (_starts, _reversedStarts) = (_reversedStarts, _starts);
        Parse(runs, (runs & 1) == 1 ? firstDark : !firstDark, origin + (direction * end), -direction, _backward);
        (_runs, _reversedRuns) = (_reversedRuns, _runs);
        (_starts, _reversedStarts) = (_reversedStarts, _starts);
    }

    private void Parse(int runs, bool firstDark, Vector2 origin, Vector2 along, List<Segment> segments)
    {
        _segments = segments;
        for (var i = firstDark ? 0 : 1; i + 6 <= runs; i += 2)
        {
            if (TryRap(i, runs, out var set, out var index, out var module))
            {
                i = Chain(i, runs, set, index, module, origin, along) - 2;
            }
        }
    }

    /// <summary>Where a line through <paramref name="through"/> enters and leaves the image, as distances along it.</summary>
    private static bool TryClip(Vector2 through, Vector2 direction, int width, int height, out float from, out float to)
    {
        from = float.MinValue;
        to = float.MaxValue;
        if (!Clip(through.X, direction.X, width - 1, ref from, ref to) || !Clip(through.Y, direction.Y, height - 1, ref from, ref to))
        {
            return false;
        }

        from = MathF.Ceiling(from);
        to = MathF.Floor(to);
        return to - from >= 16;

        static bool Clip(float start, float step, float max, ref float from, ref float to)
        {
            if (MathF.Abs(step) < 1e-6f)
            {
                return start >= 0 && start <= max;
            }

            var a = (0 - start) / step;
            var b = (max - 0.001f - start) / step;
            from = Math.Max(from, Math.Min(a, b));
            to = Math.Min(to, Math.Max(a, b));
            return from <= to;
        }
    }

    /// <summary>
    /// Follows a chain of RAPs and codewords from the RAP at run <paramref name="first"/>, recording it as a segment.
    /// </summary>
    /// <returns>The run after the chain.</returns>
    private int Chain(int first, int runs, int set, int index, float module, Vector2 origin, Vector2 along)
    {
        var tokenStart = _tokens.Count;
        _tokens.Add(new Token(set, index, Point(origin, along, _starts[first])));
        var run = first + 6;
        var stop = false;
        var endPosition = _starts[run];

        while (run < runs)
        {
            if (run + 8 <= runs && TryCodeword(run, module, out var cluster, out var codeword, out var width))
            {
                _tokens.Add(new Token(-1 - cluster, codeword, Point(origin, along, _starts[run])));
                module = ((module * 3) + (width / 17f)) / 4f;
                run += 8;
                endPosition = _starts[run];
                continue;
            }

            if (run + 6 <= runs && TryRap(run, runs, out set, out index, out var rapModule) && MathF.Abs(rapModule - module) < module * 0.3f)
            {
                _tokens.Add(new Token(set, index, Point(origin, along, _starts[run])));
                module = ((module * 3) + rapModule) / 4f;
                run += 6;
                endPosition = _starts[run];
                continue;
            }

            // The bar closing every row: one module, then the quiet zone - a module at least - or the end of the line.
            var bar = _runs[run];
            if (bar > module * 0.4f && bar < module * 1.8f && (run + 1 >= runs || _runs[run + 1] > module * 0.8f))
            {
                stop = true;
                endPosition = _starts[run + 1];
            }

            break;
        }

        _segments.Add(new Segment(tokenStart, _tokens.Count - tokenStart, stop, Point(origin, along, endPosition), module));
        return run;
    }

    private static Vector2 Point(Vector2 origin, Vector2 along, float t) => origin + (along * t);

    /// <summary>Reads the six runs from <paramref name="run"/> as a row address pattern.</summary>
    private bool TryRap(int run, int runs, out int set, out int index, out float module)
    {
        set = 0;
        index = -1;
        var total = 0;
        for (var i = 0; i < 6; i++)
        {
            total += _runs[run + i];
        }

        module = total / (float)BbMicroPdf417Tables.RapModules;
        if (module < 0.8f)
        {
            return false;
        }

        // No element of a RAP is wider than five of its ten modules: anything wider is a bar or a gap of something else,
        // turned away before the rounding.
        var widest = module * 5.6f;
        for (var i = 0; i < 6; i++)
        {
            if (_runs[run + i] > widest)
            {
                return false;
            }
        }

        Span<int> widths = stackalloc int[6];
        if (!Normalize(_runs.AsSpan(run, 6), module, BbMicroPdf417Tables.RapModules, widths))
        {
            return false;
        }

        var pattern = 0;
        var bit = BbMicroPdf417Tables.RapModules - 1;
        for (var i = 0; i < 6; i++)
        {
            for (var m = 0; m < widths[i]; m++, bit--)
            {
                if ((i & 1) == 0)
                {
                    pattern |= 1 << bit;
                }
            }
        }

        if (SideIndex.TryGetValue(pattern, out index))
        {
            set = 0;
            return true;
        }

        if (CentreIndex.TryGetValue(pattern, out index))
        {
            set = 1;
            return true;
        }

        return false;
    }

    /// <summary>Reads the eight runs from <paramref name="run"/> as a PDF417 symbol character, whose cluster is in its widths.</summary>
    private bool TryCodeword(int run, float module, out int cluster, out int codeword, out int width)
    {
        cluster = -1;
        codeword = -1;
        width = 0;
        for (var i = 0; i < 8; i++)
        {
            width += _runs[run + i];
        }

        // A character is seventeen modules: one much wider or narrower than the chain's modules belongs to something else.
        var expected = module * BbPdf417SymbolTable.ModulesPerCodeword;
        if (width < expected * 0.75f || width > expected * 1.25f)
        {
            return false;
        }

        Span<int> widths = stackalloc int[8];
        if (!Normalize(_runs.AsSpan(run, 8), width / (float)BbPdf417SymbolTable.ModulesPerCodeword, BbPdf417SymbolTable.ModulesPerCodeword, widths))
        {
            return false;
        }

        cluster = BbPdf417SymbolTable.ClusterOf(widths);
        return cluster is 0 or 3 or 6 &&
            BbPdf417SymbolTable.Current.TryGetCodeword(cluster, BbPdf417SymbolTable.ToPattern(widths), out codeword);
    }

    /// <summary>
    /// Rounds run widths to whole modules summing to <paramref name="modules"/>, each at least one, moving the roundings
    /// that were closest to a half the other way when the sum is off. Fails when a run is far from a whole number.
    /// </summary>
    private static bool Normalize(ReadOnlySpan<int> runs, float module, int modules, Span<int> widths)
    {
        var sum = 0;
        Span<float> exact = stackalloc float[runs.Length];
        for (var i = 0; i < runs.Length; i++)
        {
            exact[i] = runs[i] / module;
            widths[i] = Math.Max(1, (int)MathF.Round(exact[i]));
            if (widths[i] > 6)
            {
                return false;
            }

            sum += widths[i];
        }

        while (sum != modules)
        {
            var step = sum < modules ? 1 : -1;
            var best = -1;
            var bestError = float.MaxValue;
            for (var i = 0; i < runs.Length; i++)
            {
                var candidate = widths[i] + step;
                if (candidate < 1 || candidate > 6)
                {
                    continue;
                }

                var error = MathF.Abs(exact[i] - candidate);
                if (error < bestError)
                {
                    bestError = error;
                    best = i;
                }
            }

            if (best < 0 || bestError > 1.1f)
            {
                return false;
            }

            widths[best] += step;
            sum += step;
        }

        return true;
    }

    /// <summary>
    /// Splits what the lines of one direction read into the symbols it came from - segments near each other across the
    /// lines and overlapping along them belong together - and assembles each, the one most was read of first. Two
    /// symbols of one size side by side or one above the other would otherwise vote into the same grid, and neither read.
    /// </summary>
    private BbSymbolDecodeResult? Assemble(List<Segment> segments, Vector2 along)
    {
        // Nothing is split, or allocated, for reads no symbol size explains - the stray rows bars of other symbols make.
        _segments = segments;
        var fits = false;
        foreach (var variant in BbMicroPdf417Tables.Variants)
        {
            if (Score(variant) >= MinimumScore)
            {
                fits = true;
                break;
            }
        }

        if (!fits)
        {
            return null;
        }

        var across = new Vector2(-along.Y, along.X);
        var count = segments.Count;
        var parent = new int[count];
        for (var i = 0; i < count; i++)
        {
            parent[i] = i;
        }

        int Root(int i)
        {
            while (parent[i] != i)
            {
                i = parent[i] = parent[parent[i]];
            }

            return i;
        }

        // Each segment's place: across the lines, and the stretch it covers along them.
        var order = new int[count];
        var offsets = new float[count];
        var starts = new float[count];
        var ends = new float[count];
        for (var i = 0; i < count; i++)
        {
            order[i] = i;
            offsets[i] = Vector2.Dot(_tokens[segments[i].Start].Position, across);
            starts[i] = Vector2.Dot(_tokens[segments[i].Start].Position, along);
            ends[i] = Vector2.Dot(segments[i].End, along);
        }

        Array.Sort(offsets.ToArray(), order);
        for (var a = 0; a < count; a++)
        {
            var i = order[a];
            var reach = segments[i].Module * 6;
            for (var b = a + 1; b < count && offsets[order[b]] - offsets[i] <= reach; b++)
            {
                var j = order[b];
                // Pieces of one row lie end to end, and lines across whole rows join the rest; the quiet zone between
                // two symbols, a module at the least, keeps them apart.
                var margin = segments[i].Module * 0.8f;
                if (starts[j] <= ends[i] + margin && starts[i] <= ends[j] + margin)
                {
                    parent[Root(i)] = Root(j);
                }
            }
        }

        var groups = new Dictionary<int, List<Segment>>();
        for (var i = 0; i < count; i++)
        {
            var root = Root(i);
            if (!groups.TryGetValue(root, out var group))
            {
                groups[root] = group = [];
            }

            group.Add(segments[i]);
        }

        if (groups.Count == 1)
        {
            return AssembleSymbol(segments, along);
        }

        foreach (var group in groups.Values.OrderByDescending(g => g.Count).Take(4))
        {
            if (AssembleSymbol(group, along) is { } result)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Fits the segments the lines of one direction read to every symbol size, and decodes the grid of the sizes that
    /// explain them best.
    /// </summary>
    private BbSymbolDecodeResult? AssembleSymbol(List<Segment> segments, Vector2 along)
    {
        _segments = segments;
        Span<int> scores = stackalloc int[BbMicroPdf417Tables.Variants.Length];
        var any = false;
        for (var v = 0; v < scores.Length; v++)
        {
            scores[v] = Score(BbMicroPdf417Tables.Variants[v]);
            any |= scores[v] >= MinimumScore;
        }

        if (!any)
        {
            return null;
        }


        // The sizes are tried best first; error correction is what decides between those that fit equally well.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var best = -1;
            for (var v = 0; v < scores.Length; v++)
            {
                if (scores[v] >= MinimumScore && (best < 0 || scores[v] > scores[best]))
                {
                    best = v;
                }
            }

            if (best < 0)
            {
                return null;
            }

            scores[best] = 0;
            if (TryDecode(BbMicroPdf417Tables.Variants[best], along) is { } result)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// How well a size explains what was read. A RAP on its own proves little - two in five ten module patterns of three
    /// bars are RAPs - so only a RAP next to a codeword counts: one point when the size has a row for it and the codeword
    /// is of that row's cluster, minus two when not. The number of codewords between a left pattern and the next one
    /// counts too: it is the column count's to say, and it is what tells apart three and four column sizes, whose
    /// patterns start alike.
    /// </summary>
    private int Score(BbMicroPdf417Tables.Variant variant)
    {
        var score = 0;
        foreach (var segment in _segments)
        {
            for (var i = 0; i < segment.Count; i++)
            {
                var token = _tokens[segment.Start + i];
                if (token.IsCodeword || Kind(segment, i) is not { } kind)
                {
                    continue;
                }

                var neighbour = kind == BbMicroPdf417RapKind.Right ? i - 1 : i + 1;
                if (neighbour < 0 || neighbour >= segment.Count || !_tokens[segment.Start + neighbour].IsCodeword)
                {
                    continue;
                }

                var row = kind == BbMicroPdf417RapKind.Centre && variant.Columns < 3 ? -1 : variant.RowOf(kind, token.Value);
                score += row >= 0 && _tokens[segment.Start + neighbour].Cluster == variant.Cluster(row) ? 1 : -2;

                if (kind == BbMicroPdf417RapKind.Left)
                {
                    var codewords = 0;
                    var next = i + 1;
                    while (next < segment.Count && _tokens[segment.Start + next].IsCodeword)
                    {
                        codewords++;
                        next++;
                    }

                    if (next < segment.Count && Kind(segment, next) is { } closing)
                    {
                        var expected = closing == BbMicroPdf417RapKind.Centre ? variant.Columns - 2 : variant.Columns;
                        score += codewords == expected && (closing != BbMicroPdf417RapKind.Centre || variant.Columns >= 3) ? 2 : -3;
                    }
                }
            }
        }

        return score;
    }

    /// <summary>
    /// Where a RAP of a segment sits in its row: a centre pattern says so itself; a side pattern followed by a codeword
    /// opens its row, and one the closing bar follows ends it.
    /// </summary>
    private BbMicroPdf417RapKind? Kind(Segment segment, int i)
    {
        var token = _tokens[segment.Start + i];
        if (token.Set == 1)
        {
            return BbMicroPdf417RapKind.Centre;
        }

        var last = i == segment.Count - 1;
        if (!last && _tokens[segment.Start + i + 1].IsCodeword)
        {
            return BbMicroPdf417RapKind.Left;
        }

        // A right pattern closes a row it has codewords in: a lone side pattern before something like the closing bar
        // may just as well be a left one whose first codeword did not read.
        if (last && segment.Stop && i > 0 && _tokens[segment.Start + i - 1].IsCodeword)
        {
            return BbMicroPdf417RapKind.Right;
        }

        return null;
    }

    private BbSymbolDecodeResult? TryDecode(BbMicroPdf417Tables.Variant variant, Vector2 along)
    {
        var columns = variant.Columns;
        var cells = columns * variant.Rows;
        var votes = new Dictionary<int, int>[cells];
        var leftEdges = new List<Vector2>?[variant.Rows];
        var rightEdges = new List<Vector2>?[variant.Rows];
        var moduleSum = 0f;
        var moduleCount = 0;

        foreach (var segment in _segments)
        {
            for (var i = 0; i < segment.Count; i++)
            {
                var token = _tokens[segment.Start + i];
                if (token.IsCodeword || Kind(segment, i) is not { } kind || (kind == BbMicroPdf417RapKind.Centre && columns < 3))
                {
                    continue;
                }

                var row = variant.RowOf(kind, token.Value);
                if (row < 0)
                {
                    continue;
                }

                // An edge is taken from a pattern whose codeword next to it is of its row's cluster, which a misread
                // pattern - naming another row - rarely is.
                var neighbour = kind == BbMicroPdf417RapKind.Right ? i - 1 : i + 1;
                var confirmed = neighbour >= 0 && neighbour < segment.Count &&
                    _tokens[segment.Start + neighbour].IsCodeword &&
                    _tokens[segment.Start + neighbour].Cluster == variant.Cluster(row);
                if (confirmed)
                {
                    moduleSum += segment.Module;
                    moduleCount++;
                    if (kind == BbMicroPdf417RapKind.Left)
                    {
                        (leftEdges[row] ??= []).Add(token.Position);
                    }
                    else if (kind == BbMicroPdf417RapKind.Right)
                    {
                        (rightEdges[row] ??= []).Add(segment.End);
                    }
                }

                // The codewords after a left or centre pattern, and before a right or centre one, up to the next pattern.
                var forwardFirst = kind switch
                {
                    BbMicroPdf417RapKind.Left => 0,
                    BbMicroPdf417RapKind.Centre => columns == 3 ? 1 : 2,
                    _ => -1,
                };
                var backwardLast = kind switch
                {
                    BbMicroPdf417RapKind.Right => columns - 1,
                    BbMicroPdf417RapKind.Centre => columns == 3 ? 0 : 1,
                    _ => -1,
                };

                var cluster = variant.Cluster(row);
                if (forwardFirst >= 0)
                {
                    for (var k = i + 1; k < segment.Count && _tokens[segment.Start + k].IsCodeword; k++)
                    {
                        Vote(votes, _tokens[segment.Start + k], cluster, row, forwardFirst + (k - i - 1), columns, kind, forward: true);
                    }
                }

                if (backwardLast >= 0)
                {
                    for (var k = i - 1; k >= 0 && _tokens[segment.Start + k].IsCodeword; k--)
                    {
                        Vote(votes, _tokens[segment.Start + k], cluster, row, backwardLast - (i - k - 1), columns, kind, forward: false);
                    }
                }
            }
        }

        var codewords = new int[cells];
        var erasures = new List<int>();
        for (var cell = 0; cell < cells; cell++)
        {
            var best = -1;
            var bestCount = 0;
            if (votes[cell] is { } candidates)
            {
                foreach (var (value, count) in candidates)
                {
                    if (count > bestCount)
                    {
                        best = value;
                        bestCount = count;
                    }
                }
            }

            if (best < 0)
            {
                erasures.Add(cell);
                codewords[cell] = 0;
            }
            else
            {
                codewords[cell] = best;
            }
        }

        // Unread codewords are erasures. A grid mostly unread is not worth correcting: no more than half the check
        // codewords' worth are let through, and what keeps a wrong grid - or a wrong size - from passing is that the
        // corrected codewords have to check out against every check codeword.
        if (erasures.Count > variant.EcCodewords / 2 ||
            !_errorCorrection.TryDecode(codewords, variant.EcCodewords, erasures, out var corrected))
        {
            return null;
        }

        var dataCount = cells - variant.EcCodewords;
        var stream = new int[dataCount + 1];
        stream[0] = dataCount + 1;
        Array.Copy(codewords, 0, stream, 1, dataCount);
        var payload = BbPdf417BitStreamParser.DecodeMicroPdf417(stream, variant.Rows, columns, corrected);
        if (payload is null)
        {
            return null;
        }

        var module = moduleCount > 0 ? moduleSum / moduleCount : 1f;
        _edgesRead = moduleCount;
        var corners = Corners(variant, along, module, leftEdges, rightEdges) ?? Outline(along, module);
        return new BbSymbolDecodeResult(payload, BbBarcodeFormat.MicroPdf417, corners);
    }

    private void Vote(Dictionary<int, int>[] votes, Token token, int cluster, int row, int column, int columns, BbMicroPdf417RapKind anchor, bool forward)
    {
        // A codeword of another cluster is from a neighbouring row the line drifted into, and one past where the
        // pattern's codewords end is from beyond the symbol.
        if (token.Cluster != cluster || column < 0 || column >= columns)
        {
            return;
        }

        // Forward from the left pattern of a three or four column symbol, the codewords stop at the centre one.
        if (forward && anchor == BbMicroPdf417RapKind.Left && columns >= 3 && column >= columns - 2)
        {
            return;
        }

        if (!forward && anchor == BbMicroPdf417RapKind.Right && columns >= 3 && column < columns - 2)
        {
            return;
        }

        var cell = (row * columns) + column;
        var counts = votes[cell] ??= [];
        counts[token.Value] = counts.GetValueOrDefault(token.Value) + 1;
    }

    /// <summary>
    /// The symbol's corners, clockwise from its own top-left, in the frame of the lines: a position along them, and the
    /// offset of the line across them. Each side is fitted on its own - its rows' edges along the lines, and across
    /// them the middle of each row, which is the middle of the lines that read the row there - so a symbol a little
    /// turned from the lines, or seen at an angle, still gets its own corners. The symbol runs half a row beyond its
    /// first and last row.
    /// </summary>
    private static BbScanPoint[]? Corners(
        BbMicroPdf417Tables.Variant variant, Vector2 along, float module, List<Vector2>?[] leftEdges, List<Vector2>?[] rightEdges)
    {
        var across = new Vector2(-along.Y, along.X);
        var width = BbMicroPdf417Tables.RowModules(variant.Columns) * module;

        var left = FitSide(leftEdges, along, across);
        var right = FitSide(rightEdges, along, across);
        if (left is null && right is null)
        {
            return null;
        }

        // A side read across too few rows to fit is placed from the other: a row is as wide as its size says.
        var l = left ?? right!.Value with { AlongAt = right.Value.AlongAt - width };
        var r = right ?? l with { AlongAt = l.AlongAt + width };

        BbScanPoint Corner(Side side, float row)
        {
            var point = (along * (side.AlongAt + (side.AlongStep * row))) + (across * (side.AcrossAt + (side.AcrossStep * row)));
            return new BbScanPoint(point.X, point.Y);
        }

        return
        [
            Corner(l, -0.5f),
            Corner(r, -0.5f),
            Corner(r, variant.Rows - 0.5f),
            Corner(l, variant.Rows - 0.5f),
        ];
    }

    private static Side? FitSide(List<Vector2>?[] edges, Vector2 along, Vector2 across)
    {
        // Across the lines, the middle of the outermost lines that read each row.
        var middles = new List<float>?[edges.Length];
        for (var row = 0; row < edges.Length; row++)
        {
            if (edges[row] is { Count: > 0 } points)
            {
                var low = float.MaxValue;
                var high = float.MinValue;
                foreach (var point in points)
                {
                    var offset = Vector2.Dot(point, across);
                    low = Math.Min(low, offset);
                    high = Math.Max(high, offset);
                }

                middles[row] = [(low + high) / 2f];
            }
        }

        var position = FitLine(Project(edges, along));
        var middle = FitLine(middles);
        return position is { } p && middle is { } m ? new Side(p.Origin, p.Step, m.Origin, m.Step) : null;
    }

    /// <summary>One side of the symbol: where row 0's edge is along and across the lines, and how far each row moves it.</summary>
    private readonly record struct Side(float AlongAt, float AlongStep, float AcrossAt, float AcrossStep);

    /// <summary>
    /// The box round everything the lines read, half a row beyond them across the lines, for a symbol whose rows' edges
    /// were read too seldom to fit: rougher than its corners, but enough to outline it and to paint it out of the frame
    /// before the next symbol is looked for.
    /// </summary>
    private BbScanPoint[] Outline(Vector2 along, float module)
    {
        var across = new Vector2(-along.Y, along.X);
        float minAlong = float.MaxValue, maxAlong = float.MinValue, minAcross = float.MaxValue, maxAcross = float.MinValue;
        foreach (var segment in _segments)
        {
            for (var i = 0; i < segment.Count; i++)
            {
                Include(_tokens[segment.Start + i].Position);
            }

            Include(segment.End);
        }

        void Include(Vector2 point)
        {
            var u = Vector2.Dot(point, along);
            var v = Vector2.Dot(point, across);
            minAlong = Math.Min(minAlong, u);
            maxAlong = Math.Max(maxAlong, u);
            minAcross = Math.Min(minAcross, v);
            maxAcross = Math.Max(maxAcross, v);
        }

        BbScanPoint Corner(float u, float v)
        {
            var point = (along * u) + (across * v);
            return new BbScanPoint(point.X, point.Y);
        }

        minAcross -= module;
        maxAcross += module;
        return [Corner(minAlong, minAcross), Corner(maxAlong, minAcross), Corner(maxAlong, maxAcross), Corner(minAlong, maxAcross)];
    }

    private static List<float>?[] Project(List<Vector2>?[] points, Vector2 axis)
    {
        var projected = new List<float>?[points.Length];
        for (var row = 0; row < points.Length; row++)
        {
            if (points[row] is { } list)
            {
                projected[row] = list.ConvertAll(point => Vector2.Dot(point, axis));
            }
        }

        return projected;
    }

    /// <summary>
    /// Fits value = origin + step × row through the rows' median values, the Theil-Sen way: the step is the median of
    /// the steps between every two rows, the origin the median of what each row then says it is. A row whose pattern
    /// was misread as another's - its values far off the rest - moves neither. Null when fewer than two rows have values.
    /// </summary>
    private static (float Origin, float Step)? FitLine(List<float>?[] values)
    {
        var rows = new List<(int Row, float Value)>();
        for (var row = 0; row < values.Length; row++)
        {
            if (values[row] is { Count: > 0 } list)
            {
                list.Sort();
                rows.Add((row, list[list.Count / 2]));
            }
        }

        if (rows.Count < 2)
        {
            return null;
        }

        var steps = new List<float>(rows.Count * (rows.Count - 1) / 2);
        for (var a = 0; a < rows.Count; a++)
        {
            for (var b = a + 1; b < rows.Count; b++)
            {
                steps.Add((rows[b].Value - rows[a].Value) / (rows[b].Row - rows[a].Row));
            }
        }

        steps.Sort();
        var step = steps[steps.Count / 2];
        var origins = rows.ConvertAll(r => r.Value - (step * r.Row));
        origins.Sort();
        return (origins[origins.Count / 2], step);
    }

    private static Dictionary<int, int> Index(ReadOnlySpan<int> patterns)
    {
        var index = new Dictionary<int, int>(patterns.Length);
        for (var i = 0; i < patterns.Length; i++)
        {
            index[patterns[i]] = i;
        }

        return index;
    }

    /// <summary>
    /// A RAP (<see cref="Set"/> 0 for a side pattern, 1 for a centre one, <see cref="Value"/> its index) or a codeword
    /// (<see cref="Set"/> minus one minus its cluster, <see cref="Value"/> the codeword), and where along its line it starts.
    /// </summary>
    private readonly record struct Token(int Set, int Value, Vector2 Position)
    {
        public bool IsCodeword => Set < 0;

        public int Cluster => -1 - Set;
    }

    /// <summary>A chain of tokens one line read, whether the closing bar ended it, where it ended and its module size.</summary>
    private readonly record struct Segment(int Start, int Count, bool Stop, Vector2 End, float Module);
}
