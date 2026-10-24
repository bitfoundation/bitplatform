using System.Globalization;
using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Decodes GS1 DataBar Omnidirectional and its shorter Truncated form, the codes on loose fruit and vegetables and
/// small retail items (ISO/IEC 24724). Both carry a GTIN-14 and are read the same way; the stacked forms, which split
/// the symbol over two rows, are not read.
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
/// <see cref="NarrowestRun"/> of a module is not taken for an element at all: it is what a binarizer makes of a narrow
/// bar that blur has washed out. And bars printed or seen wider or narrower than their modules - ink spread, dot gain,
/// exposure - put every element's rounding in doubt by the same amount: that bias is measured on the guards and finder
/// patterns, whose widths are known once they are matched, and taken out of the characters before they are rounded.
/// A symbol biased by more than <see cref="MaxBarBias"/> of a module is not read.
/// </para>
/// <para>
/// The text is the GS1 element string the symbol stands for, <c>01</c> and the GTIN-14, and
/// <see cref="BbBarcodeResultMetadata.IsGs1"/> is set.
/// </para>
/// </remarks>
public sealed class BbDataBarReader : IBbRowDecoder, IBbImageHolder
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
    /// Runs in a symbol from the left guard's bar to the right guard's bar, and the modules they span: everything but the
    /// left guard's space, which merges with the light before it.
    /// </summary>
    private const int SymbolRuns = 45;
    private const int SymbolModules = 95;

    /// <summary>The narrowest run, in modules of its character, that is taken for a printed element.</summary>
    private const float NarrowestRun = 0.4f;

    /// <summary>The light, in modules, there has to be beyond each of the two outermost bars.</summary>
    private const float MinOutsideLight = 1.5f;

    /// <summary>How much wider or narrower than its modules, in modules, every bar may measure.</summary>
    private const float MaxBarBias = 0.6f;

    /// <summary>How many modules the rounding of one character may move on a doubt before it is given up.</summary>
    private const int MaxCorrections = 2;

    private const float MaxFinderVariance = 0.25f;
    private const float MaxFinderElementVariance = 0.7f;

    private readonly List<int> _runs = new(256);
    private readonly List<int> _runStarts = new(256);
    private readonly int[] _counters = new int[8];
    private readonly int[] _widths = new int[32];
    private int[] _prefix = new int[257];

    /// <inheritdoc />
    void IBbImageHolder.ForgetImage()
    {
        BbLinearPatterns.Forget(_runs);
        BbLinearPatterns.Forget(_runStarts);
        Array.Clear(_prefix);
        Array.Clear(_widths);
        Array.Clear(_counters);
    }

    /// <inheritdoc />
    public BbBarcodeFormat Formats => BbBarcodeFormat.DataBar;

    /// <inheritdoc />
    public BbSymbolDecodeResult? DecodeRow(int rowNumber, BbBitRow row, BbBarcodeFormat enabledFormats)
    {
        var runs = _runs;
        if ((enabledFormats & BbBarcodeFormat.DataBar) == 0 || !BbLinearPatterns.ReadRuns(row, runs, _runStarts) ||
            runs.Count < SymbolRuns + 2)
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

        return null;
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
        var text = $"01{digits}{CheckDigit(digits)}";
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

        // The left finder runs space, bar, space, bar, space; the right one, mirrored, bar first.
        float FinderBias(int start, int[] pattern, bool mirrored)
        {
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
    /// Turns the eight runs of a character into whole module widths that add up to <paramref name="modules"/>: each run
    /// rounded at the character's own module size, then put right one module at a time on the runs whose rounding was
    /// furthest off.
    /// </summary>
    /// <remarks>
    /// The total alone does not say which run to put right. At two pixels a module an edge a pixel out of place leaves
    /// two neighbouring runs a module and a half wide each, and taking the module from the wrong one is a misread
    /// character. The specification settles it: an outside character's odd elements add up to an even number of
    /// modules and an inside character's to an odd one, and the even elements always add up to an even number. So
    /// whichever set has the wrong parity is the one that gives or takes the module, and when both are wrong one module
    /// moves from one set to the other. That is only done where the rounding was in doubt: a run measured a whole
    /// number of modules wide that breaks the parity is a damaged character, and taking a module from it would leave
    /// the checksum alone to catch the misread. And it is only done twice a character: rounding that needs more guesses
    /// than that is a guess.
    /// </remarks>
    private bool Quantize(int run, int modules, bool oddFirst, bool firstIsBar, float bias, Span<int> widths)
    {
        const int Widest = 8;

        // How far from a whole number of modules a run has to measure for its rounding to be in doubt.
        const float Doubt = 0.2f;
        var total = 0;
        for (var i = 0; i < 8; i++)
        {
            total += _runs[run + i];
        }

        if (total < modules)
        {
            return false;
        }

        // The odd elements as drawn are the even positions when the character starts with one, the odd ones otherwise.
        var oddPosition = oddFirst ? 0 : 1;
        var oddParity = modules == 16 ? 0 : 1;
        Span<float> errors = stackalloc float[8];
        int oddSum = 0, evenSum = 0;
        for (var i = 0; i < 8; i++)
        {
            // The bias is taken out, bars narrowed and spaces widened by it; four of each, so the total stays.
            var isBar = ((i & 1) == 0) == firstIsBar;
            var exact = (_runs[run + i] - (isBar ? bias : -bias)) * modules / total;
            if (exact < NarrowestRun)
            {
                return false;
            }

            widths[i] = Math.Clamp((int)MathF.Round(exact), 1, Widest);
            errors[i] = exact - widths[i];
            if ((i & 1) == oddPosition)
            {
                oddSum += widths[i];
            }
            else
            {
                evenSum += widths[i];
            }
        }

        for (var step = 0; ; step++)
        {
            var mismatch = oddSum + evenSum - modules;
            var oddBad = (oddSum & 1) != oddParity;
            var evenBad = (evenSum & 1) != 0;
            if (mismatch == 0 && !oddBad && !evenBad)
            {
                return true;
            }

            if (step == MaxCorrections)
            {
                return false;
            }

            if (mismatch == 0)
            {
                // Both sets are a module out, in opposite directions: move it to whichever side costs the rounding least.
                var growOdd = Pick(widths, errors, oddPosition, grow: true);
                var shrinkEven = Pick(widths, errors, 1 - oddPosition, grow: false);
                var shrinkOdd = Pick(widths, errors, oddPosition, grow: false);
                var growEven = Pick(widths, errors, 1 - oddPosition, grow: true);
                // Only a move both of whose runs were in doubt; the cheaper of the two when both qualify.
                var intoOdd = InDoubt(errors, growOdd, shrinkEven) ? errors[shrinkEven] - errors[growOdd] : float.MaxValue;
                var intoEven = InDoubt(errors, growEven, shrinkOdd) ? errors[shrinkOdd] - errors[growEven] : float.MaxValue;
                if (intoOdd == float.MaxValue && intoEven == float.MaxValue)
                {
                    return false;
                }

                var (grown, shrunk) = intoOdd <= intoEven ? (growOdd, shrinkEven) : (growEven, shrinkOdd);
                widths[grown]++;
                errors[grown]--;
                widths[shrunk]--;
                errors[shrunk]++;
                oddSum += intoOdd <= intoEven ? 1 : -1;
                evenSum += intoOdd <= intoEven ? -1 : 1;
                continue;
            }

            // A module too many or too few: from the set whose parity is wrong, or from either when neither is.
            var growing = mismatch < 0;
            var byParity = oddBad != evenBad;
            var pick = byParity
                ? Pick(widths, errors, oddBad ? oddPosition : 1 - oddPosition, growing)
                : Better(Pick(widths, errors, 0, growing), Pick(widths, errors, 1, growing), errors, growing);
            if (pick < 0 || (byParity && (growing ? errors[pick] < Doubt : errors[pick] > -Doubt)))
            {
                return false;
            }

            widths[pick] += growing ? 1 : -1;
            errors[pick] += growing ? -1 : 1;
            if ((pick & 1) == oddPosition)
            {
                oddSum += growing ? 1 : -1;
            }
            else
            {
                evenSum += growing ? 1 : -1;
            }
        }

        static bool InDoubt(Span<float> errors, int grown, int shrunk) =>
            grown >= 0 && shrunk >= 0 && errors[grown] >= Doubt && errors[shrunk] <= -Doubt;

        // The element of one parity whose rounding was furthest off in the direction of the change; -1 for none.
        static int Pick(Span<int> widths, Span<float> errors, int position, bool grow)
        {
            var pick = -1;
            for (var i = position; i < 8; i += 2)
            {
                if ((grow && widths[i] < Widest && (pick < 0 || errors[i] > errors[pick])) ||
                    (!grow && widths[i] > 1 && (pick < 0 || errors[i] < errors[pick])))
                {
                    pick = i;
                }
            }

            return pick;
        }

        static int Better(int a, int b, Span<float> errors, bool grow)
        {
            if (a < 0 || b < 0)
            {
                return Math.Max(a, b);
            }

            return grow == errors[a] > errors[b] ? a : b;
        }
    }

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
            var vOdd = RssValue(odd, group[4], noNarrow: !outside);
            var vEven = RssValue(even, group[5], noNarrow: outside);
            if (vOdd < 0 || vEven < 0 || vOdd >= group[6] || vEven >= group[7])
            {
                return null;
            }

            var value = outside ? group[1] + (vOdd * group[7]) + vEven : group[1] + (vEven * group[6]) + vOdd;
            return value <= group[0] ? value : null;
        }

        return null;
    }

    /// <summary>
    /// The number of the arrangement <paramref name="widths"/> is among all arrangements of as many modules over as many
    /// elements with none wider than <paramref name="maxWidth"/> - and, when <paramref name="noNarrow"/>, at least one
    /// a single module wide - in the order the specification enumerates them; -1 for widths that break the rules.
    /// </summary>
    internal static int RssValue(ReadOnlySpan<int> widths, int maxWidth, bool noNarrow)
    {
        var elements = widths.Length;
        var n = 0;
        foreach (var width in widths)
        {
            if (width < 1 || width > maxWidth)
            {
                return -1;
            }

            n += width;
        }

        var value = 0;
        var narrowMask = 0;
        for (var bar = 0; bar < elements - 1; bar++)
        {
            int width;
            for (width = 1, narrowMask |= 1 << bar; width < widths[bar]; width++, narrowMask &= ~(1 << bar))
            {
                var sub = Combinations(n - width - 1, elements - bar - 2);
                if (noNarrow && narrowMask == 0 && n - width - (elements - bar - 1) >= elements - bar - 1)
                {
                    sub -= Combinations(n - width - (elements - bar), elements - bar - 2);
                }

                if (elements - bar - 1 > 1)
                {
                    var less = 0;
                    for (var widest = n - width - (elements - bar - 2); widest > maxWidth; widest--)
                    {
                        less += Combinations(n - width - widest - 1, elements - bar - 3);
                    }

                    sub -= less * (elements - 1 - bar);
                }
                else if (n - width > maxWidth)
                {
                    sub--;
                }

                value += sub;
            }

            n -= width;
        }

        // The last element takes what is left, which must be a width the symbol could have drawn.
        return noNarrow && narrowMask == 0 && widths[^1] > 1 ? -1 : value;
    }

    private static int Combinations(int n, int r)
    {
        if (r < 0 || n < r)
        {
            return 0;
        }

        long value = 1;
        r = Math.Min(r, n - r);
        for (var i = 1; i <= r; i++)
        {
            value = value * (n - r + i) / i;
        }

        return (int)value;
    }

    /// <summary>The GTIN check digit of thirteen digits: weights three and one from the right.</summary>
    private static char CheckDigit(string digits)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            var weight = ((digits.Length - i) & 1) == 1 ? 3 : 1;
            sum += (digits[i] - '0') * weight;
        }

        return (char)('0' + ((10 - (sum % 10)) % 10));
    }
}
