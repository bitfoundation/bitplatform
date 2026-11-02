using System.Globalization;
using System.Numerics;
using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Decodes GS1 DataBar Omnidirectional and its shorter Truncated form, the codes on loose fruit and vegetables and
/// small retail items (ISO/IEC 24724), and the stacked forms of the two, Stacked Omnidirectional and Stacked, which
/// split the symbol over two rows. All four carry a GTIN-14 and are read the same way.
/// </summary>
/// <remarks>
/// <para>
/// A symbol is 96 modules: a guard, an outside character, a finder pattern, an inside character, then the same three
/// again mirrored, and a guard. Each character is eight bars and spaces of one to eight modules - sixteen modules for
/// an outside character, fifteen for an inside one - and stands for a number through the combinatorics the
/// specification defines: how many modules its odd and its even elements share out, and in which arrangement.
/// </para>
/// <para>
/// One check guards the data. The finder patterns are not fixed: the pair of them encodes a checksum, modulo 79, of
/// every element width of the four characters. The GTIN's check digit is not in the symbol at all - it is computed
/// from the other thirteen digits - so it checks nothing. Beyond the checksum, each character's widths must be one of
/// the arrangements the specification allows, and a character whose odd and even elements add up to the wrong parity
/// is only put right where the rounding of its runs was in doubt. Even so, a random set of widths passes the checksum
/// one time in 79, and a noisy frame offers hundreds of positions to try - among them the wrong alignments inside a
/// real symbol, whose runs are DataBar elements too. So the pipeline reports a DataBar symbol only once a second scan
/// line has read the same value. What a second line cannot catch is a fault every line shares. A run narrower than
/// <see cref="BbDataBarElements.NarrowestRun"/> of a module is not taken for an element at all: it is what a binarizer
/// makes of a narrow bar that blur has washed out. And bars printed or seen wider or narrower than their modules - ink
/// spread, dot gain, exposure - put every element's rounding in doubt by the same amount: that bias is measured on the
/// guards and finder patterns, whose widths are known once they are matched, and taken out of the characters before
/// they are rounded. A symbol biased by more than <see cref="MaxBarBias"/> of a module is not read.
/// </para>
/// <para>
/// The text is the GS1 element string the symbol stands for, <c>01</c> and the GTIN-14, and
/// <see cref="BbBarcodeResultMetadata.IsGs1"/> is set.
/// </para>
/// <para>
/// A symbol no single line crosses whole is pieced together from its halves - a guard, an outside character, a finder
/// pattern and an inside character each - read on different lines of the same image: the two rows of DataBar Stacked
/// and Stacked Omnidirectional, which no line crosses both of, and a short symbol turned further off a line than the
/// line can cross it. Where the lines start a half traces its leading edge, square to the symbol's own axis, so a few
/// lines give the axis however far off it they ran. Two halves are only put together when they read the same way at
/// the same module size along that axis, lie where one symbol's halves lie - end to end, or one row under the other -
/// and satisfy the checksum between them; each half has to have been read on two lines, so that a half misread once is
/// not put together with its partner, and two pairings that would give different values give none. The result is
/// confirmed on a second line like any other. A half read on its own is handed back to the pipeline, which reads more
/// lines about it: see <see cref="IBbLineAwareRowDecoder.Partial"/>.
/// </para>
/// </remarks>
public sealed class BbDataBarReader : IBbRowDecoder, IBbImageHolder, IBbLineAwareRowDecoder
{
    /// <summary>
    /// Outside characters, sixteen modules, by group: the largest value, the group's first value, the modules of the
    /// odd and of the even elements, the widest odd and widest even element, and how many arrangements the odd and
    /// the even elements have.
    /// </summary>
    private static readonly int[][] Outside =
    [
        [160, 0, 12, 4, 8, 1, 161, 1],
        [960, 161, 10, 6, 6, 3, 80, 10],
        [2014, 961, 8, 8, 4, 5, 31, 34],
        [2714, 2015, 6, 10, 3, 6, 10, 70],
        [2840, 2715, 4, 12, 1, 8, 1, 126],
    ];

    /// <summary>Inside characters, fifteen modules, laid out as <see cref="Outside"/>.</summary>
    private static readonly int[][] Inside =
    [
        [335, 0, 5, 10, 2, 7, 4, 84],
        [1035, 336, 7, 8, 4, 5, 20, 35],
        [1515, 1036, 9, 6, 6, 3, 48, 10],
        [1596, 1516, 11, 4, 8, 1, 81, 1],
    ];

    /// <summary>
    /// The weight of every element width in the checksum: the left outside and inside characters, then the right outside
    /// and inside ones, each character's widths as drawn.
    /// </summary>
    private static readonly int[] CheckWeights =
    [
        1, 3, 9, 27, 2, 6, 18, 54, 58, 72, 24, 8, 29, 36, 12, 4,
        74, 51, 17, 32, 37, 65, 48, 16, 64, 34, 23, 69, 49, 68, 46, 59,
    ];

    /// <summary>The nine finder patterns, as the left one is drawn; the right one is drawn mirrored.</summary>
    private static readonly int[][] Finders =
    [
        [3, 8, 2, 1, 1], [3, 5, 5, 1, 1], [3, 3, 7, 1, 1], [3, 1, 9, 1, 1], [2, 7, 4, 1, 1],
        [2, 5, 6, 1, 1], [2, 3, 8, 1, 1], [1, 5, 7, 1, 1], [1, 3, 9, 1, 1],
    ];

    /// <summary>
    /// The modules a symbol spans from the left guard's bar to the right guard's bar, 45 runs: everything but the left
    /// guard's space, which merges with the light before it.
    /// </summary>
    private const int SymbolModules = 95;

    /// <summary>The light, in modules, there has to be beyond each of the two outermost bars.</summary>
    private const float MinOutsideLight = 1.5f;

    /// <summary>How much wider or narrower than its modules, in modules, every bar may measure.</summary>
    private const float MaxBarBias = 0.6f;

    private const float MaxFinderVariance = 0.25f;
    private const float MaxFinderElementVariance = 0.7f;

    private readonly List<int> _runs = new(256);
    private readonly List<int> _runStarts = new(256);
    private readonly int[] _counters = new int[8];
    private readonly int[] _widths = new int[32];
    private int[] _prefix = new int[257];

    /// <summary>The halves read on the lines of the current image, kept to piece symbols together from.</summary>
    private readonly List<Half> _halves = new(MaxHalves);

    /// <summary>The most halves kept from one image.</summary>
    private const int MaxHalves = 48;

    /// <summary>Set by a line that read a half no symbol could be made of yet; see <see cref="IBbLineAwareRowDecoder.Partial"/>.</summary>
    private BbPartialRead? _partial;

    /// <inheritdoc />
    BbPartialRead? IBbLineAwareRowDecoder.Partial => _partial;

    private float _lineX0;
    private float _lineY0;
    private float _lineDX;
    private float _lineDY;
    private int _lineLength;
    private bool _lineReversed;
    private bool _lineKnown;

    /// <summary>
    /// A half of a symbol: its outside and inside characters' value, <c>1597 outside + inside</c>; its finder pattern's
    /// value; the share of the checksum its characters contribute; its module size along the line; where it starts and
    /// ends in the image, read the way its characters run, on the first line it was read on; on how many lines, the
    /// last of them which, it was read; and where it started on each of them.
    /// </summary>
    private readonly record struct Half(bool Left, int Value, int Finder, int Checksum, float Module, Vector2 Start, Vector2 End, int Seen = 1, int LastLine = 0, BbEdgeFit Edge = default);

    /// <summary>Counts the lines of the image, so that a half read twice on one line is counted once.</summary>
    private int _lineSerial;

    /// <inheritdoc />
    void IBbLineAwareRowDecoder.BeginImage()
    {
        _halves.Clear();
        _lineKnown = false;
    }

    /// <inheritdoc />
    void IBbLineAwareRowDecoder.SetLine(float x0, float y0, float dx, float dy, int length, bool reversed, bool inverted)
    {
        (_lineX0, _lineY0, _lineDX, _lineDY, _lineLength, _lineReversed, _lineKnown) = (x0, y0, dx, dy, length, reversed, true);

        // The same line read the other way round is the same line: what it asked to have followed up still stands.
        if (!reversed)
        {
            _lineSerial++;
            _partial = null;
        }
    }

    /// <inheritdoc />
    void IBbImageHolder.ForgetImage()
    {
        BbLinearPatterns.Forget(_runs);
        BbLinearPatterns.Forget(_runStarts);
        Array.Clear(_prefix);
        Array.Clear(_widths);
        Array.Clear(_counters);
        _halves.Clear();
    }

    /// <inheritdoc />
    public BbBarcodeFormat Formats => BbBarcodeFormat.DataBar;

    /// <inheritdoc />
    public BbSymbolDecodeResult? DecodeRow(int rowNumber, BbBitRow row, BbBarcodeFormat enabledFormats)
    {
        // A half - a guard, two characters and a finder, 22 runs or 23 - and the light either side is the least a line
        // has to hold; a whole symbol needs twice that, which the loop below sees to.
        var runs = _runs;
        if ((enabledFormats & BbBarcodeFormat.DataBar) == 0 || !BbLinearPatterns.ReadRuns(row, runs, _runStarts) || runs.Count < 25)
        {
            return null;
        }

        // The left finder pattern starts on a space, nine runs after the left guard's bar; the guard's own space, the
        // run before that bar, must exist, so the first candidate is ten runs in. The right guard's bar is 35 runs after
        // the finder, and the light beyond it has to exist too.
        var firstBar = row[_runStarts[0]] ? 0 : 1;
        var firstSpace = 1 - firstBar;
        var prefix = Prefix(runs);
        for (var finder = 10 + ((firstSpace + 10) & 1); finder + 37 <= runs.Count; finder += 2)
        {
            if (!IsProportioned(prefix, finder) ||
                Finder(finder, mirrored: false) is not { } leftFinder || Finder(finder + 21, mirrored: true) is not { } rightFinder)
            {
                continue;
            }

            if (TryRead(rowNumber, row, finder, leftFinder, rightFinder) is { } result)
            {
                return result;
            }
        }

        return _lineKnown ? ReadHalves(rowNumber, row, prefix) : null;
    }

    /// <summary>
    /// Reads the halves on a line no whole symbol was read on, keeps them, and puts a symbol together from a left and a
    /// right half when the two fit.
    /// </summary>
    private BbSymbolDecodeResult? ReadHalves(int rowNumber, BbBitRow row, int[] prefix)
    {
        var runs = _runs;
        Span<int> widths = stackalloc int[16];
        Span<int> read = stackalloc int[16];
        var count = 0;
        Half? last = null;
        var lastKept = -1;
        for (var at = 9; at + 13 <= runs.Count; at++)
        {
            var startsOnBar = row[_runStarts[at]];
            var finderWidth = prefix[at + 5] - prefix[at];

            if (!startsOnBar && at >= 10 && Near(prefix[at] - prefix[at - 8], 16, finderWidth) && Near(prefix[at + 13] - prefix[at + 5], 15, finderWidth) &&
                Finder(at, mirrored: false) is { } leftFinder && TryReadLeftHalf(at, leftFinder, widths) is { } left)
            {
                (last, lastKept) = (left, Keep(left));
                count = Note(lastKept, read, count);
            }
            else if (startsOnBar && at >= 8 && at + 16 <= runs.Count && Near(prefix[at] - prefix[at - 8], 15, finderWidth) &&
                     Near(prefix[at + 13] - prefix[at + 5], 16, finderWidth) &&
                     Finder(at, mirrored: true) is { } rightFinder && TryReadRightHalf(at, rightFinder, widths) is { } right)
            {
                (last, lastKept) = (right, Keep(right));
                count = Note(lastKept, read, count);
            }
        }

        if (last is not { } half)
        {
            return null;
        }

        if (count > 0 && Combine(rowNumber, read[..count]) is { } combined)
        {
            return combined;
        }

        // Part of a symbol and nothing whole to make of it yet: the rest - the other row of a stacked symbol, at most 36
        // modules away, the other end of a short one, or this half again to count a second sighting of - lies on lines
        // either side, which the pipeline can read next - along the symbol's own axis, once the lines that read this half
        // show it.
        var middle = (half.Start + half.End) / 2;
        var axis = lastKept >= 0 && _halves[lastKept].Edge.Axis(default, half.Module, Vector2.Normalize(half.End - half.Start)) is { } fitted
            ? fitted
            : Vector2.Zero;
        _partial = new BbPartialRead(middle.X, middle.Y, 2 * half.Module, 40 * half.Module, axis.X, axis.Y);
        return null;

        static int Note(int index, Span<int> read, int count)
        {
            if (index >= 0 && count < read.Length && !read[..count].Contains(index))
            {
                read[count++] = index;
            }

            return count;
        }

        static bool Near(int width, int modules, int finderWidth) =>
            Math.Abs((width * 15) - (modules * finderWidth)) * 3 <= modules * finderWidth;
    }

    /// <summary>A left half: the outer guard's bar, the left outside character, the left finder and the left inside character.</summary>
    private Half? TryReadLeftHalf(int finder, int finderValue, Span<int> widths)
    {
        var runs = _runs;
        var guard = finder - 9;
        var last = finder + 12;
        var total = 0;
        for (var i = guard; i <= last; i++)
        {
            total += runs[i];
        }

        var module = total / 47f;
        if (module < 0.8f || MathF.Abs(runs[guard] - module) > module * 0.6f + 0.5f || runs[guard - 1] < module * MinOutsideLight)
        {
            return null;
        }

        var bias = ((runs[guard] - module) + FinderBias(finder, Finders[finderValue], mirrored: false)) / 6;
        if (MathF.Abs(bias) > module * MaxBarBias ||
            !Quantize(finder - 8, 16, oddFirst: true, firstIsBar: false, bias, widths[..8]) ||
            !Quantize(finder + 5, 15, oddFirst: false, firstIsBar: true, bias, widths[8..16]) ||
            Character(widths[..8], outside: true, oddFirst: true) is not { } outside ||
            Character(widths[8..16], outside: false, oddFirst: false) is not { } inside)
        {
            return null;
        }

        var checksum = 0;
        for (var i = 0; i < 16; i++)
        {
            checksum += widths[i] * CheckWeights[i];
        }

        return new Half(true, (1597 * outside) + inside, finderValue, checksum % 79, module, ToImage(_runStarts[guard]), ToImage(_runStarts[last] + runs[last]));
    }

    /// <summary>A right half: the right inside character, the right finder, the right outside character and the outer guard.</summary>
    private Half? TryReadRightHalf(int finder, int finderValue, Span<int> widths)
    {
        var runs = _runs;
        var first = finder - 8;
        var end = finder + 14;
        var total = 0;
        for (var i = first; i <= end; i++)
        {
            total += runs[i];
        }

        var module = total / 48f;
        if (module < 0.8f ||
            MathF.Abs(runs[end - 1] - module) > module * 0.6f + 0.5f ||
            MathF.Abs(runs[end] - module) > module * 0.6f + 0.5f ||
            runs[end + 1] < module * MinOutsideLight)
        {
            return null;
        }

        var bias = (FinderBias(finder, Finders[finderValue], mirrored: true) - (runs[end - 1] - module) + (runs[end] - module)) / 7;

        // The right outside character first and the right inside one second, the order the checksum weighs them in.
        if (MathF.Abs(bias) > module * MaxBarBias ||
            !Quantize(finder + 5, 16, oddFirst: false, firstIsBar: false, bias, widths[..8]) ||
            !Quantize(first, 15, oddFirst: true, firstIsBar: true, bias, widths[8..16]) ||
            Character(widths[..8], outside: true, oddFirst: false) is not { } outside ||
            Character(widths[8..16], outside: false, oddFirst: true) is not { } inside)
        {
            return null;
        }

        var checksum = 0;
        for (var i = 0; i < 16; i++)
        {
            checksum += widths[i] * CheckWeights[16 + i];
        }

        return new Half(false, (1597 * outside) + inside, finderValue, checksum % 79, module, ToImage(_runStarts[first]), ToImage(_runStarts[end] + runs[end]));
    }

    /// <summary>Where sample <paramref name="position"/> of the current line lies in the image.</summary>
    private Vector2 ToImage(float position)
    {
        var index = _lineReversed ? _lineLength - 1 - position : position;
        return new Vector2(_lineX0 + (_lineDX * index), _lineY0 + (_lineDY * index));
    }

    /// <summary>
    /// Keeps a half - or finds it kept already, read on another line - and gives its place among those kept; -1 when
    /// there is no room for it. A half read again is the same half wherever along its edge, and at whatever angle, the
    /// line crossed it: as far as a stacked symbol's edge is long from where it was first read.
    /// </summary>
    private int Keep(Half half)
    {
        for (var i = 0; i < _halves.Count; i++)
        {
            var kept = _halves[i];
            if (kept.Left != half.Left || kept.Value != half.Value || kept.Finder != half.Finder ||
                Vector2.Distance(kept.Start, half.Start) > 48 * half.Module ||
                Vector2.Dot(Vector2.Normalize(kept.End - kept.Start), Vector2.Normalize(half.End - half.Start)) < 0.5f)
            {
                continue;
            }

            if (kept.LastLine != _lineSerial)
            {
                _halves[i] = kept with { Seen = kept.Seen + 1, LastLine = _lineSerial, Edge = kept.Edge.Add(half.Start - kept.Start) };
            }

            return i;
        }

        if (_halves.Count >= MaxHalves)
        {
            return -1;
        }

        _halves.Add(half with { LastLine = _lineSerial, Edge = BbEdgeFit.First });
        return _halves.Count - 1;
    }

    /// <summary>
    /// The symbol a left and a right half kept from this image make, the closest pair that fits: read the same way at
    /// the same module size, the right half starting where a left half ends (one row) or one module in from where it
    /// starts, a row further down (stacked), and the checksum between them right. One of the two has to have been read on
    /// the current line: a line confirming a symbol has to have read some of it itself.
    /// </summary>
    private BbSymbolDecodeResult? Combine(int rowNumber, ReadOnlySpan<int> readHere)
    {
        Half? bestLeft = null;
        Half? bestRight = null;
        var bestAcross = float.MaxValue;
        var stacked = false;
        long? otherValue = null;
        for (var li = 0; li < _halves.Count; li++)
        {
            var left = _halves[li];
            if (!left.Left || !Dominates(left))
            {
                continue;
            }

            for (var ri = 0; ri < _halves.Count; ri++)
            {
                var right = _halves[ri];
                if (right.Left || (!readHere.Contains(li) && !readHere.Contains(ri)) || !Dominates(right) ||
                    !TryMeasure(left, right, out var along, out var across))
                {
                    continue;
                }

                var oneRow = MathF.Abs(along - 47) <= 3 && across <= 36;
                var twoRows = MathF.Abs(along - 1) <= 3 && across >= 1 && across <= 72;
                if ((!oneRow && !twoRows) || !ChecksumAgrees(left, right))
                {
                    continue;
                }

                // Another pairing that fits as well and stands for something else makes the image ambiguous.
                var pairValue = (4537077L * left.Value) + right.Value;
                if (bestLeft is { } previousLeft && bestRight is { } previousRight &&
                    (4537077L * previousLeft.Value) + previousRight.Value != pairValue)
                {
                    otherValue = pairValue;
                }

                if (across >= bestAcross)
                {
                    continue;
                }

                (bestLeft, bestRight, bestAcross, stacked) = (left, right, across, twoRows && !oneRow);
            }
        }

        if (bestLeft is not { } l || bestRight is not { } r || otherValue is not null)
        {
            return null;
        }

        var value = (4537077L * l.Value) + r.Value;
        if (value >= 10_000_000_000_000L)
        {
            return null;
        }

        var digits = value.ToString("D13", CultureInfo.InvariantCulture);
        var text = $"01{digits}{BbDataBarElements.CheckDigit(digits)}";
        var payload = new BbDecoderResult(Encoding.ASCII.GetBytes(text), text)
        {
            IsGs1 = true,
            SymbolVersion = stacked ? "Stacked" : null,
        };

        // The pipeline places a result along the line it came from: the symbol's two ends, as far along this line as
        // they lie.
        return new BbSymbolDecodeResult(
            payload,
            BbBarcodeFormat.DataBar,
            [new BbScanPoint(AlongLine(l.Start), rowNumber), new BbScanPoint(AlongLine(r.End), rowNumber)]);
    }

    /// <summary>
    /// Whether a half is what its place reads: seen on two lines at least, and more than twice as often as every other
    /// value read on the same side within a stacked symbol's height of it put together. A real half reads the same on
    /// nearly every line that crosses it whole; lines that cross a place too far off its axis to read it read something
    /// different nearly every time, and one of those, seen twice by chance, passes the checksum with its partner one
    /// time in 79.
    /// </summary>
    private bool Dominates(in Half half)
    {
        if (half.Seen < 2)
        {
            return false;
        }

        var others = 0;
        foreach (var other in _halves)
        {
            if (other.Left == half.Left && other.Value != half.Value &&
                Vector2.Distance(other.Start, half.Start) <= 40 * half.Module)
            {
                others += other.Seen;
            }
        }

        return half.Seen > 2 * others;
    }

    /// <summary>
    /// Where a right half's start lies from a left half's, along the symbol's axis and across it, in modules of the
    /// same size. The axis is the one their starts trace, when they trace one: the halves may then have been read on
    /// lines at different angles, each of which measured the module stretched by how far it ran off the axis. Otherwise
    /// it is the lines' own, and they have to have run the same way.
    /// </summary>
    private static bool TryMeasure(in Half left, in Half right, out float along, out float across)
    {
        (along, across) = (0, 0);
        var leftDirection = Vector2.Normalize(left.End - left.Start);
        var rightDirection = Vector2.Normalize(right.End - right.Start);
        var parallel = Vector2.Dot(leftDirection, rightDirection);
        if (parallel < 0.5f)
        {
            return false;
        }

        var lines = Vector2.Normalize(leftDirection + rightDirection);
        Vector2 axis;
        float leftModule, rightModule;
        if (left.Edge.Axis(right.Edge, (left.Module + right.Module) / 2, lines) is { } fitted)
        {
            // A line more than 45 degrees off the axis crosses no half whole: a fit that says otherwise is wrong.
            var (leftCos, rightCos) = (Vector2.Dot(leftDirection, fitted), Vector2.Dot(rightDirection, fitted));
            if (leftCos < 0.7f || rightCos < 0.7f)
            {
                return false;
            }

            (axis, leftModule, rightModule) = (fitted, left.Module * leftCos, right.Module * rightCos);
        }
        else if (parallel >= 0.978f)
        {
            (axis, leftModule, rightModule) = (lines, left.Module, right.Module);
        }
        else
        {
            return false;
        }

        if (MathF.Max(leftModule, rightModule) > MathF.Min(leftModule, rightModule) * 1.25f)
        {
            return false;
        }

        var module = (leftModule + rightModule) / 2;
        var offset = right.Start - left.Start;
        along = Vector2.Dot(offset, axis) / module;
        across = MathF.Abs((offset.X * axis.Y) - (offset.Y * axis.X)) / module;
        return true;
    }

    private bool ChecksumAgrees(in Half left, in Half right)
    {
        var checksum = (left.Checksum + right.Checksum) % 79;
        if (checksum >= 8)
        {
            checksum++;
        }

        if (checksum >= 72)
        {
            checksum++;
        }

        return checksum / 9 == left.Finder && checksum % 9 == right.Finder;
    }

    /// <summary>How far along the current line, in its own samples, a point of the image lies.</summary>
    private float AlongLine(Vector2 point)
    {
        var index = ((point.X - _lineX0) * _lineDX) + ((point.Y - _lineY0) * _lineDY);
        return _lineReversed ? _lineLength - 1 - index : index;
    }

    private BbSymbolDecodeResult? TryRead(int rowNumber, BbBitRow row, int finder, int leftFinder, int rightFinder)
    {
        var runs = _runs;
        var guard = finder - 9;
        var end = finder + 35;

        // One module size for the whole symbol; the left guard's bar, and the right guard's space and bar, a module of
        // it each. A module of light beside a module of bar is also what the inside of a symbol is made of - a wrong
        // alignment within a real symbol, or a bar that noise has split, looks just like a guard - so the light beyond
        // each of the two outermost bars has to be a module and a half at least. DataBar needs no quiet zone of its own;
        // a label has that much margin, and a symbol printed right up against other ink is missed rather than risked.
        var total = 0;
        for (var i = guard; i <= end; i++)
        {
            total += runs[i];
        }

        var module = total / (float)SymbolModules;
        if (module < 0.8f ||
            MathF.Abs(runs[guard] - module) > module * 0.6f + 0.5f ||
            MathF.Abs(runs[end - 1] - module) > module * 0.6f + 0.5f ||
            MathF.Abs(runs[end] - module) > module * 0.6f + 0.5f ||
            runs[guard - 1] < module * MinOutsideLight ||
            runs[end + 1] < module * MinOutsideLight)
        {
            return null;
        }

        var bias = BarBias(guard, finder, end, leftFinder, rightFinder, module);
        if (MathF.Abs(bias) > module * MaxBarBias)
        {
            return null;
        }

        // The four characters in the order the checksum weighs them: the left outside and inside characters, then the
        // right outside and inside ones. Runs alternate from the left finder's first, a space.
        var widths = _widths;
        if (!Quantize(finder - 8, 16, oddFirst: true, firstIsBar: false, bias, widths.AsSpan(0, 8)) ||
            !Quantize(finder + 5, 15, oddFirst: false, firstIsBar: true, bias, widths.AsSpan(8, 8)) ||
            !Quantize(finder + 26, 16, oddFirst: false, firstIsBar: false, bias, widths.AsSpan(16, 8)) ||
            !Quantize(finder + 13, 15, oddFirst: true, firstIsBar: true, bias, widths.AsSpan(24, 8)))
        {
            return null;
        }

        // The checksum the finder patterns carry, with the two values that would make them identical skipped.
        var checksum = 0;
        for (var i = 0; i < 32; i++)
        {
            checksum += widths[i] * CheckWeights[i];
        }

        checksum %= 79;
        if (checksum >= 8)
        {
            checksum++;
        }

        if (checksum >= 72)
        {
            checksum++;
        }

        if (checksum / 9 != leftFinder || checksum % 9 != rightFinder)
        {
            return null;
        }

        // The left outside and the right outside character are drawn odd element first, the two inside characters
        // the other way round.
        if (Character(widths.AsSpan(0, 8), outside: true, oddFirst: true) is not { } d1 ||
            Character(widths.AsSpan(8, 8), outside: false, oddFirst: false) is not { } d2 ||
            Character(widths.AsSpan(16, 8), outside: true, oddFirst: false) is not { } d3 ||
            Character(widths.AsSpan(24, 8), outside: false, oddFirst: true) is not { } d4)
        {
            return null;
        }

        var value = (4537077L * ((1597L * d1) + d2)) + (1597L * d3) + d4;
        if (value >= 10_000_000_000_000L)
        {
            return null;
        }

        var digits = value.ToString("D13", CultureInfo.InvariantCulture);
        var text = $"01{digits}{BbDataBarElements.CheckDigit(digits)}";
        var payload = new BbDecoderResult(Encoding.ASCII.GetBytes(text), text) { IsGs1 = true };
        var left = _runStarts[guard];
        var right = _runStarts[end] + runs[end];
        return new BbSymbolDecodeResult(
            payload,
            BbBarcodeFormat.DataBar,
            [new BbScanPoint(left, rowNumber), new BbScanPoint(right, rowNumber)]);
    }

    /// <summary>
    /// How many pixels wider than its modules every bar measures, and every space narrower: the mean departure of the
    /// thirteen elements whose widths are known - the left guard's bar, the right guard's space and bar, and the two
    /// finder patterns as matched, each finder at its own module size, which perspective can make differ from the
    /// symbol's - with the spaces' sign turned.
    /// </summary>
    private float BarBias(int guard, int finder, int end, int leftFinder, int rightFinder, float module)
    {
        var runs = _runs;
        var sum = (runs[guard] - module) - (runs[end - 1] - module) + (runs[end] - module);
        sum += FinderBias(finder, Finders[leftFinder], mirrored: false);
        sum += FinderBias(finder + 21, Finders[rightFinder], mirrored: true);
        return sum / 13;

    }

    /// <summary>
    /// The finder pattern's share of <see cref="BarBias"/>: the departures of its five runs from the matched pattern at
    /// its own module size, the spaces' sign turned. The left finder runs space, bar, space, bar, space; the right one,
    /// mirrored, bar first.
    /// </summary>
    private float FinderBias(int start, int[] pattern, bool mirrored)
    {
        var runs = _runs;
        var total = 0;
        for (var i = 0; i < 5; i++)
        {
            total += runs[start + i];
        }

        var finderModule = total / 15f;
        var bias = 0f;
        for (var i = 0; i < 5; i++)
        {
            var departure = runs[start + i] - (pattern[mirrored ? 4 - i : i] * finderModule);
            bias += ((i & 1) == 1) != mirrored ? departure : -departure;
        }

        return bias;
    }

    /// <summary>Running totals of the runs, so that the width of any span of them is one subtraction.</summary>
    private int[] Prefix(List<int> runs)
    {
        if (_prefix.Length <= runs.Count)
        {
            _prefix = new int[runs.Count * 2];
        }

        var prefix = _prefix;
        prefix[0] = 0;
        for (var i = 0; i < runs.Count; i++)
        {
            prefix[i + 1] = prefix[i] + runs[i];
        }

        return prefix;
    }

    /// <summary>
    /// Whether the six blocks between the guards - character, finder, character, character, finder, character - are
    /// in the proportions 16, 15, 15, 15, 15 and 16 modules. Six subtractions turn away almost every position of a row
    /// that crosses something else before a finder pattern is matched against it. Each character is held to the
    /// finder beside it and the two finders to each other, loosely, so that a symbol seen at an angle, whose modules
    /// shrink from one end to the other, still passes.
    /// </summary>
    private static bool IsProportioned(int[] prefix, int finder)
    {
        var leftFinder = prefix[finder + 5] - prefix[finder];
        var rightFinder = prefix[finder + 26] - prefix[finder + 21];
        return Near(prefix[finder] - prefix[finder - 8], 16, leftFinder) &&
               Near(prefix[finder + 13] - prefix[finder + 5], 15, leftFinder) &&
               Near(prefix[finder + 21] - prefix[finder + 13], 15, rightFinder) &&
               Near(prefix[finder + 34] - prefix[finder + 26], 16, rightFinder) &&
               Near(rightFinder, 15, leftFinder);

        // Within a third of the width a block of that many modules has beside a fifteen module finder.
        static bool Near(int width, int modules, int finderWidth) =>
            Math.Abs((width * 15) - (modules * finderWidth)) * 3 <= modules * finderWidth;
    }

    /// <summary>
    /// The value of the finder pattern whose five runs start at <paramref name="run"/>, read mirrored for the right
    /// one, or <see langword="null"/>.
    /// </summary>
    private int? Finder(int run, bool mirrored)
    {
        var counters = _counters.AsSpan(0, 5);
        for (var i = 0; i < 5; i++)
        {
            counters[i] = _runs[mirrored ? run + 4 - i : run + i];
        }

        // Every pattern is a short element, two that between them take ten to twelve of its fifteen modules, and two of
        // a module each: cheap tests that turn away nearly every position before the nine patterns are matched.
        var total = 0;
        foreach (var counter in counters)
        {
            total += counter;
        }

        if ((counters[3] + counters[4]) * 15 > total * 4 || (counters[1] + counters[2]) * 15 < total * 9)
        {
            return null;
        }

        var best = -1;
        var bestVariance = MaxFinderVariance;
        for (var value = 0; value < Finders.Length; value++)
        {
            var variance = BbLinearPatterns.PatternMatchVariance(counters, Finders[value], MaxFinderElementVariance);
            if (variance < bestVariance)
            {
                bestVariance = variance;
                best = value;
            }
        }

        return best < 0 ? null : best;
    }

    /// <summary>
    /// Turns the eight runs of a character into whole module widths that add up to <paramref name="modules"/>; see
    /// <see cref="BbDataBarElements.Quantize"/>. An outside character's odd elements add up to an even number of modules
    /// and an inside character's to an odd one, and the even elements always add up to an even number.
    /// </summary>
    private bool Quantize(int run, int modules, bool oddFirst, bool firstIsBar, float bias, Span<int> widths) =>
        BbDataBarElements.Quantize(_runs, run, 8, modules, 8, modules == 16 ? 0 : 1, 0, oddFirst, firstIsBar, bias, widths);

    /// <summary>
    /// The value of one character from its widths as drawn. Its odd elements are the first, third, fifth and seventh
    /// when <paramref name="oddFirst"/>, and otherwise the eighth, sixth, fourth and second; the group their modules
    /// add up to says how the odd and even arrangements combine.
    /// </summary>
    private static int? Character(ReadOnlySpan<int> widths, bool outside, bool oddFirst)
    {
        Span<int> odd = stackalloc int[4];
        Span<int> even = stackalloc int[4];
        int oddSum = 0, evenSum = 0;
        for (var i = 0; i < 4; i++)
        {
            odd[i] = oddFirst ? widths[2 * i] : widths[7 - (2 * i)];
            even[i] = oddFirst ? widths[(2 * i) + 1] : widths[6 - (2 * i)];
            oddSum += odd[i];
            evenSum += even[i];
        }

        foreach (var group in outside ? Outside : Inside)
        {
            if (group[2] != oddSum || group[3] != evenSum)
            {
                continue;
            }

            // The outside characters' even elements, and the inside characters' odd ones, may not all be wide.
            var vOdd = BbDataBarElements.RssValue(odd, group[4], noNarrow: !outside);
            var vEven = BbDataBarElements.RssValue(even, group[5], noNarrow: outside);
            if (vOdd < 0 || vEven < 0 || vOdd >= group[6] || vEven >= group[7])
            {
                return null;
            }

            var value = outside ? group[1] + (vOdd * group[7]) + vEven : group[1] + (vEven * group[6]) + vOdd;
            return value <= group[0] ? value : null;
        }

        return null;
    }
}
