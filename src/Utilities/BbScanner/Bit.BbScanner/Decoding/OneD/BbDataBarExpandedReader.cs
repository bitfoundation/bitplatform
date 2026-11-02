using System.Globalization;
using System.Numerics;
using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Decodes GS1 DataBar Expanded and Expanded Stacked (ISO/IEC 24724): any GS1 element string of up to 74 digits or 41
/// letters - a GTIN and its weight, price, batch or best before date - on variable measure fresh foods and coupons.
/// </summary>
/// <remarks>
/// <para>
/// A symbol is a guard, up to eleven pairs of symbol characters with a finder pattern between the two of each pair,
/// and a guard. Every character is eight elements over seventeen modules and stands for a twelve bit number through the
/// combinatorics the other DataBar symbols use. The first character of all is a check character; the rest carry the
/// bits of the data, and the last pair may be one character short. Expanded Stacked is the same characters cut into
/// rows of two to twenty: a row is read like a short symbol, and every second row may be drawn right to left.
/// </para>
/// <para>
/// The finder patterns say which pair a pair is: six patterns, each drawn either way round, in one of ten sequences
/// that the number of characters chooses. And they say which way a line crosses them. A pair's finder pattern is drawn
/// the first way round when the pair is the first, third or so on, and then starts on a space; the other way round,
/// starting on a bar, otherwise. A line that crosses a pair backwards sees the pattern turned round and starting on
/// the same colour - so its own start says it is backwards, and the pair is not read that way.
/// </para>
/// <para>
/// So the characters are kept by the finder pattern they sit beside and the side they sit on, from every line of an
/// image, and a symbol is put together from them: the check character says how many characters there are, and so the
/// sequence of finder patterns and which character goes where. Its value has to match the sum, modulo 211, of every
/// other character's element widths weighted by where it sits. Then the data has to make sense to the last bit - its
/// encodation method, the count of characters it repeats, every field and every latch, and the padding. A symbol one
/// line read whole is reported like any other, once a second line reads it too; one put together from several lines
/// only from characters each read on two lines, so that a character misread once is not put into it, and each read
/// more than twice as often as anything else its place was read as - a line too far off a symbol's axis to read it
/// reads something different nearly every time, and one of those, seen twice by chance, is not a reading.
/// </para>
/// <para>
/// The text is the GS1 element string the symbol stands for, with a group separator after every variable length
/// element but the last, and <see cref="BbBarcodeResultMetadata.IsGs1"/> is set. A symbol whose linkage flag says it
/// is the linear part of a GS1 Composite symbol is not read: the two-dimensional part above it, which carries the rest
/// of its data, is not.
/// </para>
/// </remarks>
public sealed class BbDataBarExpandedReader : IBbRowDecoder, IBbImageHolder, IBbLineAwareRowDecoder
{
    /// <summary>
    /// The characters by group: the largest value, the group's first value, the modules of the odd and of the even
    /// elements, the widest odd and widest even element, and how many arrangements the odd and the even elements have.
    /// </summary>
    private static readonly int[][] Groups =
    [
        [347, 0, 12, 5, 7, 2, 87, 4],
        [1387, 348, 10, 7, 5, 4, 52, 20],
        [2947, 1388, 8, 9, 4, 5, 30, 52],
        [3987, 2948, 6, 11, 3, 6, 10, 104],
        [4191, 3988, 4, 13, 1, 8, 1, 204],
    ];

    /// <summary>The six finder patterns, A to F, as drawn the first way round; the second way round is the mirror image.</summary>
    private static readonly int[][] Finders =
    [
        [1, 8, 4, 1, 1], [3, 6, 4, 1, 1], [3, 4, 6, 1, 1], [3, 2, 8, 1, 1], [2, 6, 5, 1, 1], [2, 2, 9, 1, 1],
    ];

    /// <summary>
    /// The finder patterns of a symbol of two to eleven pairs, each twice its letter, plus one for the second way round:
    /// 0 is A drawn the first way round, 1 A the second, 2 B the first and so on.
    /// </summary>
    private static readonly int[][] Sequences =
    [
        [0, 1],
        [0, 3, 2],
        [0, 5, 2, 7],
        [0, 9, 2, 7, 4],
        [0, 9, 2, 7, 6, 11],
        [0, 9, 2, 7, 8, 11, 10],
        [0, 1, 2, 3, 4, 5, 6, 7],
        [0, 1, 2, 3, 4, 5, 6, 9, 8],
        [0, 1, 2, 3, 4, 5, 6, 9, 10, 11],
        [0, 1, 2, 3, 4, 7, 6, 9, 8, 11, 10],
    ];

    /// <summary>
    /// The weight of every element width in the checksum, sixteen to a finder pattern: its left character's eight and
    /// its right character's eight, each as drawn. The first pair's left character is the check character itself.
    /// </summary>
    private static readonly int[] CheckWeights =
    [
        -1, -1, -1, -1, -1, -1, -1, -1, 77, 96, 32, 81, 27, 9, 3, 1,
        20, 60, 180, 118, 143, 7, 21, 63, 205, 209, 140, 117, 39, 13, 145, 189,
        193, 157, 49, 147, 19, 57, 171, 91, 132, 44, 85, 169, 197, 136, 186, 62,
        185, 133, 188, 142, 4, 12, 36, 108, 50, 87, 29, 80, 97, 173, 128, 113,
        150, 28, 84, 41, 123, 158, 52, 156, 166, 196, 206, 139, 187, 203, 138, 46,
        76, 17, 51, 153, 37, 111, 122, 155, 146, 119, 110, 107, 106, 176, 129, 43,
        16, 48, 144, 10, 30, 90, 59, 177, 164, 125, 112, 178, 200, 137, 116, 109,
        70, 210, 208, 202, 184, 130, 179, 115, 190, 204, 68, 93, 31, 151, 191, 134,
        148, 22, 66, 198, 172, 94, 71, 2, 40, 154, 192, 64, 162, 54, 18, 6,
        120, 149, 25, 75, 14, 42, 126, 167, 175, 199, 207, 69, 23, 78, 26, 79,
        103, 98, 83, 38, 114, 131, 182, 124, 159, 53, 88, 170, 127, 183, 61, 161,
        55, 165, 73, 8, 24, 72, 5, 15, 89, 100, 174, 58, 160, 194, 135, 45,
    ];

    /// <summary>How much wider or narrower than its modules, in modules, every bar may measure.</summary>
    private const float MaxBarBias = 0.6f;

    /// <summary>
    /// The light, in modules, there has to be before the guard's bar: its own module and half a module of margin. The
    /// symbol needs no quiet zone, but a line that crosses a two-pair symbol backwards on the other polarity sees the
    /// far end's guard as one with exactly one module of light, and reads a check character from the far end's last
    /// character.
    /// </summary>
    private const float MinGuardLight = 1.5f;

    private const float MaxFinderVariance = 0.25f;
    private const float MaxFinderElementVariance = 0.7f;

    /// <summary>The most characters kept from one image.</summary>
    private const int MaxReads = 96;

    /// <summary>The most characters a symbol has: the check character and 21 data characters.</summary>
    private const int MaxCharacters = 22;

    private readonly List<int> _runs = new(256);
    private readonly List<int> _runStarts = new(256);
    private readonly int[] _widths = new int[8];
    private int[] _prefix = new int[257];

    /// <summary>The characters read on the lines of the current image, kept to put symbols together from.</summary>
    private readonly List<CharacterRead> _reads = new(MaxReads);

    /// <summary>
    /// Which kept character some line of the current image read straight after which: the one at place <c>b</c> after
    /// the one at place <c>a</c> when <c>[a * MaxReads + b]</c> is set.
    /// </summary>
    private readonly bool[] _follows = new bool[MaxReads * MaxReads];

    /// <summary>The same for the current line alone, as pairs of places.</summary>
    private readonly List<(int Previous, int Next)> _lineFollows = new(2 * MaxCharacters);

    /// <summary>
    /// The characters the current line read, in the order of the runs they start at, to string into chains once the
    /// whole line is read: a finder pattern matched where there is none can put a character between two that follow
    /// each other, and must not break the chain they make.
    /// </summary>
    private readonly List<LineRead> _lineReads = new(2 * MaxCharacters);

    private readonly record struct LineRead(int Run, bool Right, int Index, float Module);

    private bool _firstDark;

    /// <summary>Set by a line that read characters no symbol could be made of yet; see <see cref="IBbLineAwareRowDecoder.Partial"/>.</summary>
    private BbPartialRead? _partial;

    private float _lineX0;
    private float _lineY0;
    private float _lineDX;
    private float _lineDY;
    private int _lineLength;
    private bool _lineReversed;
    private bool _lineInverted;
    private bool _lineKnown;

    /// <summary>Counts the lines of the image, so that a character read twice on one line is counted once.</summary>
    private int _lineSerial;

    /// <summary>
    /// A character: the finder pattern it sits beside, as numbered in <see cref="Sequences"/>, and on which side; its
    /// value; its share of the checksum; its module size along the line; where it starts and ends in the image, read
    /// the way the symbol runs, on the first line it was read on; whether that line was binarised the other way round;
    /// for a check character, whether a guard and light came before it; on how many lines, the last of them which, it
    /// was read; and where it started on each of them.
    /// </summary>
    /// <remarks>
    /// Whether some line read it first after a guard, and whether some line read it last before one, say where a row of
    /// a stacked symbol may start and end.
    /// </remarks>
    private readonly record struct CharacterRead(
        int Finder, bool Right, int Value, int Checksum, float Module, Vector2 Start, Vector2 End, bool Inverted, bool Guarded,
        int Seen = 1, int LastLine = 0, BbEdgeFit Edge = default, bool StartsRow = false, bool EndsRow = false);

    /// <inheritdoc />
    public BbBarcodeFormat Formats => BbBarcodeFormat.DataBarExpanded;

    /// <inheritdoc />
    BbPartialRead? IBbLineAwareRowDecoder.Partial => _partial;

    /// <inheritdoc />
    void IBbLineAwareRowDecoder.BeginImage()
    {
        _reads.Clear();
        Array.Clear(_follows);
        _lineKnown = false;
    }

    /// <inheritdoc />
    void IBbLineAwareRowDecoder.SetLine(float x0, float y0, float dx, float dy, int length, bool reversed, bool inverted)
    {
        (_lineX0, _lineY0, _lineDX, _lineDY, _lineLength, _lineReversed, _lineKnown) = (x0, y0, dx, dy, length, reversed, true);
        _lineInverted = inverted;

        // The same line read the other way round is the same line.
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
        _reads.Clear();
        Array.Clear(_follows);
        _lineFollows.Clear();
    }

    /// <inheritdoc />
    public BbSymbolDecodeResult? DecodeRow(int rowNumber, BbBitRow row, BbBarcodeFormat enabledFormats)
    {
        // A finder pattern with a character either side of it is the least a line can read anything of.
        var runs = _runs;
        if ((enabledFormats & BbBarcodeFormat.DataBarExpanded) == 0 || !_lineKnown ||
            !BbLinearPatterns.ReadRuns(row, runs, _runStarts) || runs.Count < 23)
        {
            return null;
        }

        var prefix = Prefix(runs);
        _firstDark = row[_runStarts[0]];
        _lineReads.Clear();
        _lineFollows.Clear();
        Span<int> readHere = stackalloc int[2 * MaxCharacters];
        var count = 0;
        for (var finder = 8; finder + 5 <= runs.Count; finder++)
        {
            // The first way round starts on a space, the second on a bar: a pattern crossed backwards is not matched.
            var dark = IsDark(finder);
            if (Finder(finder, dark) is not { } value)
            {
                continue;
            }

            var module = (prefix[finder + 5] - prefix[finder]) / 15f;
            var bias = FinderBias(finder, value) / 5;
            if (module < 0.8f || MathF.Abs(bias) > module * MaxBarBias)
            {
                continue;
            }

            // The left character as drawn, its odd elements first; the right one mirrored, the colours alternating on.
            // The check character, the first pair's left one, follows the guard: a module of bar with light before it.
            if (Near(prefix[finder] - prefix[finder - 8], module) &&
                TryRead(finder - 8, value, right: false, firstIsBar: dark, bias) is { } left)
            {
                var index = Keep(left with { Guarded = value == 0 && IsLeftGuard(finder - 8, module) });
                count = Note(index, readHere, count);
                if (index >= 0 && _lineReads.Count < _lineReads.Capacity)
                {
                    _lineReads.Add(new LineRead(finder - 8, Right: false, index, module));
                }
            }

            if (finder + 13 <= runs.Count && Near(prefix[finder + 13] - prefix[finder + 5], module) &&
                TryRead(finder + 5, value, right: true, firstIsBar: !dark, bias) is { } right)
            {
                var index = Keep(right);
                count = Note(index, readHere, count);
                if (index >= 0 && _lineReads.Count < _lineReads.Capacity)
                {
                    _lineReads.Add(new LineRead(finder + 5, Right: true, index, module));
                }
            }
        }

        StringChains();
        if (count == 0)
        {
            return null;
        }

        if (Assemble(rowNumber, readHere[..count]) is { } result)
        {
            return result;
        }

        // Some of a symbol and not all of it: the rest lies on lines either side - the other rows of a stacked symbol,
        // 37 modules apart, or more of a long one turned off the line - which the pipeline can read next.
        var (first, last) = (_reads[readHere[0]], _reads[readHere[count - 1]]);
        var middle = (first.Start + last.End) / 2;
        _partial = new BbPartialRead(middle.X, middle.Y, 6 * first.Module, 120 * first.Module, 0, 0);
        return null;

        static int Note(int index, Span<int> read, int count)
        {
            if (index >= 0 && count < read.Length && !read[..count].Contains(index))
            {
                read[count++] = index;
            }

            return count;
        }

        // Within a third of the seventeen modules of a character.
        static bool Near(int width, float module) => MathF.Abs(width - (17 * module)) * 3 <= 17 * module;
    }

    /// <summary>Whether run <paramref name="run"/> of the current line is a bar.</summary>
    private bool IsDark(int run) => ((run & 1) == 0) == _firstDark;

    /// <summary>
    /// Strings the characters the line read into chains: a character follows another when it starts where the next of
    /// the symbol does - the right one of the same pair thirteen runs on, past the finder pattern, or the left one of
    /// the next pair eight runs on. A chain's first character is noted as starting a row when a guard came before it,
    /// and its last as ending one when a guard came after it: right after a right character, or past the finder pattern
    /// of a last pair that has no right character.
    /// </summary>
    private void StringChains()
    {
        var reads = _lineReads;
        for (var at = 0; at < reads.Count; at++)
        {
            var (run, right, index) = (reads[at].Run, reads[at].Right, reads[at].Index);
            var previous = -1;
            var next = -1;
            for (var i = 0; i < reads.Count; i++)
            {
                if (reads[i].Right == right)
                {
                    continue;
                }

                if (reads[i].Run + (right ? 13 : 8) == run)
                {
                    previous = i;
                }
                else if (reads[i].Run == run + (right ? 8 : 13))
                {
                    next = i;
                }
            }

            if (next >= 0)
            {
                _follows[(index * MaxReads) + reads[next].Index] = true;
                _lineFollows.Add((index, reads[next].Index));
            }

            var read = _reads[index];
            if (previous < 0 && !right && !read.StartsRow && IsLeftGuard(run, reads[at].Module))
            {
                _reads[index] = read with { StartsRow = true };
            }
            else if (next < 0 && !read.EndsRow && IsRightGuard(run + (right ? 8 : 13), reads[at].Module))
            {
                _reads[index] = read with { EndsRow = true };
            }
        }
    }

    /// <summary>
    /// Whether a guard comes before the character starting at <paramref name="run"/>: light and a module of bar before
    /// one that starts on a space, and light, a module of bar and one of space before one that starts on a bar.
    /// </summary>
    private bool IsLeftGuard(int run, float module)
    {
        var runs = _runs;
        if (IsDark(run))
        {
            return run >= 3 && IsModule(runs[run - 1], module) && IsModule(runs[run - 2], module) && runs[run - 3] >= module * MinGuardLight;
        }

        return run >= 2 && IsModule(runs[run - 1], module) && runs[run - 2] >= module * MinGuardLight;
    }

    /// <summary>
    /// Whether a guard starts at <paramref name="run"/>: a module of bar and the light after it, or a module of space, one
    /// of bar and the light after them.
    /// </summary>
    private bool IsRightGuard(int run, float module)
    {
        var runs = _runs;
        if (run + 1 >= runs.Count || !IsModule(runs[run], module))
        {
            return false;
        }

        return IsDark(run)
            ? runs[run + 1] >= module * MinGuardLight
            : run + 2 < runs.Count && IsModule(runs[run + 1], module) && runs[run + 2] >= module * MinGuardLight;
    }

    private static bool IsModule(int run, float module) => MathF.Abs(run - module) <= (module * 0.6f) + 0.5f;

    /// <summary>
    /// The finder pattern whose five runs start at <paramref name="run"/>, numbered as in <see cref="Sequences"/>:
    /// drawn the first way round when it starts on a space, the second when it starts on a bar.
    /// </summary>
    private int? Finder(int run, bool dark)
    {
        var runs = _runs;
        Span<int> counters = stackalloc int[5];
        var total = 0;
        for (var i = 0; i < 5; i++)
        {
            counters[i] = runs[dark ? run + 4 - i : run + i];
            total += counters[i];
        }

        // A short element, two that between them take ten to twelve of the fifteen modules, and two of a module each.
        if ((counters[3] + counters[4]) * 15 > total * 4 || (counters[1] + counters[2]) * 15 < total * 9)
        {
            return null;
        }

        var best = -1;
        var bestVariance = MaxFinderVariance;
        for (var letter = 0; letter < Finders.Length; letter++)
        {
            var variance = BbLinearPatterns.PatternMatchVariance(counters, Finders[letter], MaxFinderElementVariance);
            if (variance < bestVariance)
            {
                bestVariance = variance;
                best = letter;
            }
        }

        return best < 0 ? null : (2 * best) + (dark ? 1 : 0);
    }

    /// <summary>
    /// How many pixels wider than its modules every bar of the finder pattern starting at <paramref name="run"/>
    /// measures, summed over its five elements with the spaces' sign turned.
    /// </summary>
    private float FinderBias(int run, int value)
    {
        var runs = _runs;
        var pattern = Finders[value / 2];
        var mirrored = (value & 1) == 1;
        var total = 0;
        for (var i = 0; i < 5; i++)
        {
            total += runs[run + i];
        }

        var module = total / 15f;
        var bias = 0f;
        for (var i = 0; i < 5; i++)
        {
            // The first way round the pattern starts on a space, the second on a bar.
            var departure = runs[run + i] - (pattern[mirrored ? 4 - i : i] * module);
            bias += ((i & 1) == 1) != mirrored ? departure : -departure;
        }

        return bias;
    }

    /// <summary>
    /// The character whose eight runs start at <paramref name="run"/>, beside finder pattern <paramref name="finder"/>:
    /// the left one drawn odd element first, the right one mirrored.
    /// </summary>
    private CharacterRead? TryRead(int run, int finder, bool right, bool firstIsBar, float bias)
    {
        var widths = _widths.AsSpan();

        // The odd elements add up to an even number of modules, from four to twelve, and the even ones to an odd number.
        if (!BbDataBarElements.Quantize(_runs, run, 8, 17, 8, 0, 1, oddFirst: !right, firstIsBar, bias, widths) ||
            Character(widths, oddFirst: !right) is not { } value)
        {
            return null;
        }

        // The first pair's left character is the check character, which the checksum does not weigh.
        var checksum = 0;
        if (finder != 0 || right)
        {
            var weights = (finder * 16) + (right ? 8 : 0);
            for (var i = 0; i < 8; i++)
            {
                checksum += widths[i] * CheckWeights[weights + i];
            }
        }

        var start = _runStarts[run];
        var end = _runStarts[run + 7] + _runs[run + 7];
        return new CharacterRead(finder, right, value, checksum, (end - start) / 17f, ToImage(start), ToImage(end), _lineInverted, Guarded: false);
    }

    /// <summary>
    /// The value of a character from its widths as drawn. Its odd elements are the first, third, fifth and seventh when
    /// <paramref name="oddFirst"/>, and otherwise the eighth, sixth, fourth and second; the group their modules add up
    /// to says how the odd and even arrangements combine.
    /// </summary>
    private static int? Character(ReadOnlySpan<int> widths, bool oddFirst)
    {
        Span<int> odd = stackalloc int[4];
        Span<int> even = stackalloc int[4];
        var oddSum = 0;
        for (var i = 0; i < 4; i++)
        {
            odd[i] = oddFirst ? widths[2 * i] : widths[7 - (2 * i)];
            even[i] = oddFirst ? widths[(2 * i) + 1] : widths[6 - (2 * i)];
            oddSum += odd[i];
        }

        foreach (var group in Groups)
        {
            if (group[2] != oddSum)
            {
                continue;
            }

            // The odd elements may not all be wide.
            var vOdd = BbDataBarElements.RssValue(odd, group[4], noNarrow: true);
            var vEven = BbDataBarElements.RssValue(even, group[5], noNarrow: false);
            if (vOdd < 0 || vEven < 0 || vOdd >= group[6] || vEven >= group[7])
            {
                return null;
            }

            var value = group[1] + (vOdd * group[7]) + vEven;
            return value <= group[0] ? value : null;
        }

        return null;
    }

    /// <summary>Where sample <paramref name="position"/> of the current line lies in the image.</summary>
    private Vector2 ToImage(float position)
    {
        var index = _lineReversed ? _lineLength - 1 - position : position;
        return new Vector2(_lineX0 + (_lineDX * index), _lineY0 + (_lineDY * index));
    }

    /// <summary>How far along the current line, in its own samples, a point of the image lies.</summary>
    private float AlongLine(Vector2 point)
    {
        var index = ((point.X - _lineX0) * _lineDX) + ((point.Y - _lineY0) * _lineDY);
        return _lineReversed ? _lineLength - 1 - index : index;
    }

    /// <summary>
    /// Keeps a character - or finds it kept already, read on another line - and gives its place among those kept; -1
    /// when there is no room for it. A character read again lies along the same edge, as far as a row is tall from
    /// where it was first read, runs the same way, and was read on a line binarised the same way round.
    /// </summary>
    private int Keep(CharacterRead read)
    {
        for (var i = 0; i < _reads.Count; i++)
        {
            var kept = _reads[i];
            if (kept.Finder != read.Finder || kept.Right != read.Right || kept.Value != read.Value || kept.Inverted != read.Inverted ||
                Vector2.Distance(kept.Start, read.Start) > 40 * read.Module ||
                Vector2.Dot(Vector2.Normalize(kept.End - kept.Start), Vector2.Normalize(read.End - read.Start)) < 0.5f)
            {
                continue;
            }

            if (kept.LastLine != _lineSerial)
            {
                _reads[i] = kept with
                {
                    Seen = kept.Seen + 1,
                    LastLine = _lineSerial,
                    Guarded = kept.Guarded || read.Guarded,
                    Edge = kept.Edge.Add(read.Start - kept.Start),
                };
            }

            return i;
        }

        if (_reads.Count >= MaxReads)
        {
            return -1;
        }

        _reads.Add(read with { LastLine = _lineSerial, Edge = BbEdgeFit.First });
        return _reads.Count - 1;
    }

    /// <summary>
    /// The symbol the characters kept from this image make, around a check character: put together from this line's
    /// characters alone, or from characters each read on two lines, one of them read on this one. Two check characters
    /// within reach of each other that make different symbols make the image ambiguous, and give nothing.
    /// </summary>
    private BbSymbolDecodeResult? Assemble(int rowNumber, ReadOnlySpan<int> readHere)
    {
        Span<int> slots = stackalloc int[MaxCharacters];
        Span<int> values = stackalloc int[MaxCharacters - 1];
        string? text = null;
        var (first, last, reach) = (Vector2.Zero, Vector2.Zero, 0f);
        for (var c = 0; c < _reads.Count; c++)
        {
            var check = _reads[c];
            if (check.Finder != 0 || check.Right)
            {
                continue;
            }

            // The check character carries how many data characters follow, from three to 21, beyond its checksum.
            var characters = (check.Value / 211) + 4;
            if (characters > MaxCharacters)
            {
                continue;
            }

            var sequence = Sequences[((characters + 1) / 2) - 2];
            foreach (var alone in (ReadOnlySpan<bool>)[true, false])
            {
                if (!Place(c, characters, sequence, alone, readHere, slots))
                {
                    continue;
                }

                var checksum = 0;
                for (var i = 1; i < characters; i++)
                {
                    checksum += _reads[slots[i]].Checksum;
                    values[i - 1] = _reads[slots[i]].Value;
                }

                if (checksum % 211 != check.Value % 211 || Decode(values[..(characters - 1)]) is not { } decoded)
                {
                    continue;
                }

                if (text is not null)
                {
                    if (text != decoded && Vector2.Distance(first, check.Start) <= MathF.Max(reach, Reach(check, characters)))
                    {
                        return null;
                    }

                    break;
                }

                (text, first, last, reach) = (decoded, check.Start, _reads[slots[characters - 1]].End, Reach(check, characters));
                break;
            }
        }

        if (text is null)
        {
            return null;
        }

        var payload = new BbDecoderResult(Encoding.ASCII.GetBytes(text), text) { IsGs1 = true };

        // The pipeline places a result along the line it came from: the symbol's two ends, as far along this line as
        // they lie.
        return new BbSymbolDecodeResult(
            payload,
            BbBarcodeFormat.DataBarExpanded,
            [new BbScanPoint(AlongLine(first), rowNumber), new BbScanPoint(AlongLine(last), rowNumber)]);
    }

    /// <summary>
    /// Fills <paramref name="slots"/> with the kept character for every place of a symbol of
    /// <paramref name="characters"/> around check character <paramref name="check"/>, which has to have been read after
    /// its guard: the one beside each place's finder pattern, on its side, of the same symbol - read on a line binarised
    /// the same way round, at the same module size, running the same way or, on a row drawn right to left, the opposite
    /// way, and within the symbol's reach - and read straight after the place before it on some line. Only a row of a
    /// stacked symbol may start without that: when the row before ended at a guard, this one starts at one, and it lies
    /// a row below. <paramref name="alone"/> puts it together from the chains of this line alone; otherwise from
    /// characters each read on two lines and dominating their place - see <see cref="Dominates"/> - at least one of them
    /// read on this line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Following chains is what keeps two symbols apart. Labels of one product side by side, or one above another, carry
    /// the same GTIN, and so the same first characters at the same places: kept by place alone, the characters of one
    /// would fill the places of the other wherever its own were not read, and one time in 211 the checksum would pass.
    /// Strung together the way the lines read them, the two diverge where their data does, and a place two chains lead
    /// to with different values is read as neither.
    /// </para>
    /// <para>
    /// A line at the same angle as the check character's measures the same module, and a character it reads a tenth
    /// smaller is one a finder pattern matched where there is none put together from parts of two characters; a line at
    /// another angle measures the module stretched by how far it runs off the symbol's axis.
    /// </para>
    /// </remarks>
    private bool Place(int check, int characters, int[] sequence, bool alone, ReadOnlySpan<int> readHere, Span<int> slots)
    {
        var anchor = _reads[check];
        if (!anchor.Guarded || (!alone && !Dominates(check)) || (alone && anchor.LastLine != _lineSerial))
        {
            return false;
        }

        var direction = Vector2.Normalize(anchor.End - anchor.Start);
        var reach = Reach(anchor, characters);
        slots[0] = check;
        var here = readHere.Contains(check);
        var firstRowLast = -1;
        for (var place = 1; place < characters; place++)
        {
            var finder = sequence[place / 2];
            var right = (place & 1) == 1;
            var previous = slots[place - 1];
            var chosen = -1;
            for (var i = 0; i < _reads.Count; i++)
            {
                var read = _reads[i];
                var alignment = MathF.Abs(Vector2.Dot(Vector2.Normalize(read.End - read.Start), direction));
                if (read.Finder != finder || read.Right != right || read.Inverted != anchor.Inverted ||
                    (alone ? read.LastLine != _lineSerial : read.Seen < 2) ||
                    MathF.Max(read.Module, anchor.Module) > MathF.Min(read.Module, anchor.Module) * (alignment > 0.9995f ? 1.12f : 1.3f) ||
                    alignment < 0.7f || Vector2.Distance(read.Start, anchor.Start) > reach)
                {
                    continue;
                }

                // The first row's last character is the one before the first row to break.
                var follows = Follows(previous, i, alone);
                if (!follows && (alone || right || !StartsRowAfter(check, previous, i, firstRowLast >= 0 ? firstRowLast : previous)))
                {
                    continue;
                }

                if (!follows && firstRowLast < 0)
                {
                    firstRowLast = previous;
                }

                // Two values leading on from one place read neither: two symbols, or a misread.
                if (chosen >= 0 && _reads[chosen].Value != read.Value)
                {
                    return false;
                }

                if (chosen < 0 || read.Seen > _reads[chosen].Seen)
                {
                    chosen = i;
                }
            }

            if (chosen < 0 || (!alone && !Dominates(chosen)))
            {
                return false;
            }

            slots[place] = chosen;
            here |= readHere.Contains(chosen);
        }

        return here;
    }

    /// <summary>
    /// Whether some line - this one, when <paramref name="thisLine"/> - read the character kept at
    /// <paramref name="next"/> straight after the one kept at <paramref name="previous"/>.
    /// </summary>
    private bool Follows(int previous, int next, bool thisLine) =>
        thisLine ? _lineFollows.Contains((previous, next)) : _follows[(previous * MaxReads) + next];

    /// <summary>
    /// Whether the character kept at <paramref name="next"/> starts the next row of a stacked symbol after the one kept
    /// at <paramref name="previous"/>: some line read the row before ending at a guard after it, some line read this one
    /// starting at a guard before it, and it lies below the check character's row, a row - 34 modules and a separator
    /// of three - below the row before, where the rows of the symbol start: lined up with the check character, or, on a
    /// row drawn right to left, with the far end of the first row, whose last character is kept at
    /// <paramref name="firstRowLast"/>.
    /// </summary>
    private bool StartsRowAfter(int check, int previous, int next, int firstRowLast)
    {
        if (!_reads[previous].EndsRow || !_reads[next].StartsRow)
        {
            return false;
        }

        // The symbol's own frame: its rows run along its axis, and the rows after the first lie on the side down points
        // to. The axis is the one the first row's characters' starts trace; failing that, the check character's line,
        // which may run off the axis by as much as a line can and still cross a row whole, some eighteen degrees, and so
        // move a row below along it by a third of the drop. A symbol beside this one starts a whole row further along.
        var (anchor, above, below) = (_reads[check], _reads[previous], _reads[next]);
        var line = Vector2.Normalize(anchor.End - anchor.Start);
        var fitted = anchor.Edge.Axis(above.Edge, anchor.Module, line);
        var axis = fitted ?? line;
        var down = new Vector2(-axis.Y, axis.X);
        var module = anchor.Module * Vector2.Dot(line, axis);
        var drop = Vector2.Dot(below.Start - above.Start, down) / module;
        var along = Vector2.Dot(below.Start - anchor.Start, axis) / module;
        var mirrored = Vector2.Dot(Vector2.Normalize(below.End - below.Start), axis) < 0;
        var rowEnd = Vector2.Dot(_reads[firstRowLast].End - anchor.Start, axis) / module;
        return drop is >= 2 and <= 75 && MathF.Abs(along - (mirrored ? rowEnd : 0)) <= (fitted is null ? 4 + (0.3f * drop) : 4);
    }

    /// <summary>
    /// Whether a character is what its place reads: seen on two lines at least, and more than twice as often as every
    /// other value read beside the same finder pattern, on the same side, within a row's height of it, put together. A
    /// real character reads the same on nearly every line that crosses it whole; a line too far off its axis reads
    /// something different nearly every time.
    /// </summary>
    private bool Dominates(int index)
    {
        var read = _reads[index];
        if (read.Seen < 2)
        {
            return false;
        }

        var others = 0;
        foreach (var other in _reads)
        {
            if (other.Finder == read.Finder && other.Right == read.Right && other.Inverted == read.Inverted &&
                other.Value != read.Value && Vector2.Distance(other.Start, read.Start) <= 40 * read.Module)
            {
                others += other.Seen;
            }
        }

        return read.Seen > 2 * others;
    }

    /// <summary>
    /// How far from its check character, in pixels, a symbol of <paramref name="characters"/> can reach, generously:
    /// 25 modules a character, and 150 more for the rows of a stacked one, each 37 modules below the last.
    /// </summary>
    private static float Reach(in CharacterRead check, int characters) => ((25 * characters) + 150) * check.Module;

    /// <summary>
    /// The GS1 element string the data characters' bits stand for, or <see langword="null"/> when they make no sense to
    /// the last bit.
    /// </summary>
    private static string? Decode(ReadOnlySpan<int> data)
    {
        var bits = new Bits(data);

        // A linked symbol's other part is not read.
        if (bits.Read(0, 1) != 0)
        {
            return null;
        }

        var text = new StringBuilder(80);
        int position;
        if (bits.Read(1, 1) == 1)
        {
            // Method 1: a GTIN, its first digit in four bits, then the general purpose field.
            var firstDigit = bits.Read(4, 4);
            if (!LengthAgrees(bits, 2) || firstDigit > 9 || !AppendGtin(text, bits, 8, (char)('0' + firstDigit)))
            {
                return null;
            }

            position = 48;
        }
        else if (bits.Read(2, 1) == 0)
        {
            // Method 00: the general purpose field alone.
            if (!LengthAgrees(bits, 3))
            {
                return null;
            }

            position = 5;
        }
        else if (bits.Read(1, 4) is 4 or 5 && bits.Size == 60)
        {
            // Methods 0100 and 0101: a variable measure GTIN and its net weight in kilograms or pounds, in fifteen bits.
            var weight = bits.Read(45, 15);
            if (!AppendGtin(text, bits, 5, '9'))
            {
                return null;
            }

            var (ai, value) = bits.Read(1, 4) == 4 ? ("3103", weight) : weight < 10000 ? ("3202", weight) : ("3203", weight - 10000);
            text.Append(ai).Append(value.ToString("D6", CultureInfo.InvariantCulture));
            return text.ToString();
        }
        else if (bits.Read(1, 5) is 12 or 13)
        {
            // Methods 01100 and 01101: a variable measure GTIN and the price to pay, the second with its currency.
            if (!LengthAgrees(bits, 6) || bits.Size < 50 || !AppendGtin(text, bits, 8, '9'))
            {
                return null;
            }

            var decimals = bits.Read(48, 2);
            position = 50;
            if (bits.Read(1, 5) == 12)
            {
                text.Append("392").Append((char)('0' + decimals));
            }
            else
            {
                var currency = bits.Size >= 60 ? bits.Read(50, 10) : 1000;
                if (currency > 999)
                {
                    return null;
                }

                text.Append("393").Append((char)('0' + decimals)).Append(currency.ToString("D3", CultureInfo.InvariantCulture));
                position = 60;
            }
        }
        else if (bits.Read(1, 7) is >= 56 and <= 63 && bits.Size == 84)
        {
            // Methods 0111000 to 0111111: a variable measure GTIN, its net weight and a date, all of fixed size.
            var method = bits.Read(1, 7) - 56;
            var weight = bits.Read(48, 20);
            var date = bits.Read(68, 16);
            if (!AppendGtin(text, bits, 8, '9') || weight / 100000 > 9 || date > 38400)
            {
                return null;
            }

            text.Append((method & 1) == 0 ? "310" : "320").Append((char)('0' + (weight / 100000)))
                .Append((weight % 100000).ToString("D6", CultureInfo.InvariantCulture));

            // 38400 says there is no date; otherwise it is days, months and years, each counted from zero.
            if (date < 38400)
            {
                var (day, month, year) = (date % 32, (date / 32 % 12) + 1, date / 384);
                text.Append("1").Append((char)('1' + (2 * (method >> 1)))).Append(year.ToString("D2", CultureInfo.InvariantCulture))
                    .Append(month.ToString("D2", CultureInfo.InvariantCulture)).Append(day.ToString("D2", CultureInfo.InvariantCulture));
            }

            return text.ToString();
        }
        else
        {
            return null;
        }

        return DecodeGeneralPurpose(bits, position, text) ? text.ToString() : null;
    }

    /// <summary>
    /// Whether the two bits at <paramref name="at"/> - whether the symbol has an odd number of characters, and whether
    /// more than fourteen - say what the data characters do.
    /// </summary>
    private static bool LengthAgrees(in Bits bits, int at)
    {
        var characters = (bits.Size / 12) + 1;
        return bits.Read(at, 1) == (characters & 1) && bits.Read(at + 1, 1) == (characters > 14 ? 1 : 0);
    }

    /// <summary>
    /// Appends <c>01</c> and the GTIN-14 whose first digit is <paramref name="firstDigit"/> and whose next twelve are in
    /// four groups of three, ten bits each, from <paramref name="position"/>, with its check digit computed.
    /// </summary>
    private static bool AppendGtin(StringBuilder text, in Bits bits, int position, char firstDigit)
    {
        Span<char> digits = stackalloc char[13];
        digits[0] = firstDigit;
        for (var group = 0; group < 4; group++)
        {
            var value = bits.Read(position + (10 * group), 10);
            if (value > 999)
            {
                return false;
            }

            digits[1 + (3 * group)] = (char)('0' + (value / 100));
            digits[2 + (3 * group)] = (char)('0' + (value / 10 % 10));
            digits[3 + (3 * group)] = (char)('0' + (value % 10));
        }

        var gtin = new string(digits);
        text.Append("01").Append(gtin).Append(BbDataBarElements.CheckDigit(gtin));
        return true;
    }

    /// <summary>
    /// Decodes the general purpose field from <paramref name="position"/> to the last bit: numeric, alphanumeric and
    /// ISO/IEC 646 runs and the latches between them, the padding included, a function 1 character ending a variable
    /// length element turned into a group separator. Any bits that are none of these make the field nonsense.
    /// </summary>
    private static bool DecodeGeneralPurpose(in Bits bits, int position, StringBuilder text)
    {
        const char GroupSeparator = '\u001d';
        var size = bits.Size;
        var mode = 0; // 0 numeric, 1 alphanumeric, 2 ISO/IEC 646.
        while (position < size)
        {
            var before = position;
            if (mode == 0)
            {
                if (IsStillNumeric(bits, position))
                {
                    if (position + 7 > size)
                    {
                        // A last digit on its own, in four bits, as one more than itself.
                        var last = bits.Read(position, 4);
                        if (last is 0 or > 10)
                        {
                            return false;
                        }

                        text.Append((char)('0' + last - 1));
                        position = size;
                        break;
                    }

                    // Two digits in seven bits, eight more than eleven times the first plus the second; ten is function 1.
                    var pair = bits.Read(position, 7) - 8;
                    position += 7;
                    foreach (var digit in (ReadOnlySpan<int>)[pair / 11, pair % 11])
                    {
                        text.Append(digit == 10 ? GroupSeparator : (char)('0' + digit));
                    }
                }
                else if (Zeros(bits, position, 4))
                {
                    (mode, position) = (1, Math.Min(size, position + 4));
                }
            }
            else if (IsStillAlphanumeric(bits, position, mode))
            {
                var five = bits.Read(position, 5);
                if (five == 15)
                {
                    // Function 1 ends the element and goes back to numeric.
                    text.Append(GroupSeparator);
                    (mode, position) = (0, position + 5);
                }
                else if (five < 15)
                {
                    // Digits, five bits.
                    text.Append((char)('0' + five - 5));
                    position += 5;
                }
                else if (mode == 1)
                {
                    var six = bits.Read(position, 6);
                    text.Append(six < 58 ? (char)(six + 33) : "*,-./"[six - 58]);
                    position += 6;
                }
                else if (bits.Read(position, 7) is var seven and < 116)
                {
                    text.Append(seven < 90 ? (char)(seven + 1) : (char)(seven + 7));
                    position += 7;
                }
                else
                {
                    text.Append("!\"%&'()*+,-./:;<=>?_ "[bits.Read(position, 8) - 232]);
                    position += 8;
                }
            }
            else if (Zeros(bits, position, 3) && position + 3 <= size)
            {
                (mode, position) = (0, position + 3);
            }
            else if (IsLatchBetweenAlphanumericAndIso(bits, position))
            {
                (mode, position) = (3 - mode, Math.Min(size, position + 5));
            }

            if (position == before)
            {
                return false;
            }
        }

        // The last element is not followed by a separator; a single last digit is written with function 1 after it.
        if (text.Length > 0 && text[^1] == GroupSeparator)
        {
            text.Length--;
        }

        return text.Length > 0;

        static bool IsStillNumeric(in Bits bits, int position)
        {
            if (position + 7 > bits.Size)
            {
                return position + 4 <= bits.Size && !Zeros(bits, position, 4);
            }

            return bits.Read(position, 4) != 0;
        }

        static bool IsStillAlphanumeric(in Bits bits, int position, int mode)
        {
            if (position + 5 > bits.Size)
            {
                return false;
            }

            if (bits.Read(position, 5) is >= 5 and < 16)
            {
                return true;
            }

            if (mode == 1)
            {
                return position + 6 <= bits.Size && bits.Read(position, 6) is >= 32 and < 63;
            }

            if (position + 7 > bits.Size)
            {
                return false;
            }

            if (bits.Read(position, 7) is >= 64 and < 116)
            {
                return true;
            }

            return position + 8 <= bits.Size && bits.Read(position, 8) is >= 232 and < 253;
        }

        // The latch between alphanumeric and ISO/IEC 646 is 00100, and the padding repeats it; the last may be cut short.
        static bool IsLatchBetweenAlphanumericAndIso(in Bits bits, int position)
        {
            for (var i = 0; i < 5 && position + i < bits.Size; i++)
            {
                if (bits.Read(position + i, 1) != (i == 2 ? 1 : 0))
                {
                    return false;
                }
            }

            return true;
        }

        // Count bits from position are zero, or as many as there are.
        static bool Zeros(in Bits bits, int position, int count)
        {
            for (var i = 0; i < count && position + i < bits.Size; i++)
            {
                if (bits.Read(position + i, 1) != 0)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>The data characters' bits, twelve to a character, most significant first.</summary>
    private readonly ref struct Bits(ReadOnlySpan<int> data)
    {
        private readonly ReadOnlySpan<int> _data = data;

        public int Size => _data.Length * 12;

        /// <summary>The <paramref name="count"/> bits from <paramref name="position"/> as a number, those past the end zero.</summary>
        public int Read(int position, int count)
        {
            var value = 0;
            for (var i = position; i < position + count; i++)
            {
                var bit = i < Size ? (_data[i / 12] >> (11 - (i % 12))) & 1 : 0;
                value = (value << 1) | bit;
            }

            return value;
        }
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
}
