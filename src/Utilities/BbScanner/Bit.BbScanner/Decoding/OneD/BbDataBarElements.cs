namespace Bit.BbScanner;

/// <summary>
/// What the GS1 DataBar family shares (ISO/IEC 24724): characters whose elements are rounded to whole modules under
/// parity rules, and numbered through the same combinatorics.
/// </summary>
internal static class BbDataBarElements
{
    /// <summary>The narrowest run, in modules of its character, that is taken for a printed element.</summary>
    public const float NarrowestRun = 0.4f;

    /// <summary>How many modules the rounding of one character may move on a doubt before it is given up.</summary>
    private const int MaxCorrections = 2;

    /// <summary>How far from a whole number of modules a run has to measure for its rounding to be in doubt.</summary>
    private const float Doubt = 0.2f;

    /// <summary>
    /// Turns the runs of a character, starting at <paramref name="run"/>, into whole module widths that add up to
    /// <paramref name="modules"/>: each run rounded at the character's own module size, then put right one module at a
    /// time on the runs whose rounding was furthest off.
    /// </summary>
    /// <remarks>
    /// The total alone does not say which run to put right. At two pixels a module an edge a pixel out of place leaves
    /// two neighbouring runs a module and a half wide each, and taking the module from the wrong one is a misread
    /// character. The specification settles it: the odd elements of every character add up to a number of modules of a
    /// known parity, <paramref name="oddParity"/>, and so do the even ones, <paramref name="evenParity"/>. So whichever
    /// set has the wrong parity is the one that gives or takes the module, and when both are wrong one module moves from
    /// one set to the other. That is only done where the rounding was in doubt: a run measured a whole number of modules
    /// wide that breaks the parity is a damaged character, and taking a module from it would leave the checksum alone to
    /// catch the misread. And it is only done twice a character: rounding that needs more guesses than that is a guess.
    /// The odd elements as drawn are the first, third and so on when <paramref name="oddFirst"/>, and the second, fourth
    /// and so on otherwise; <paramref name="bias"/>, how many pixels wider than its modules every bar measures, is taken
    /// out first.
    /// </remarks>
    public static bool Quantize(
        List<int> runs, int run, int count, int modules, int widest, int oddParity, int evenParity,
        bool oddFirst, bool firstIsBar, float bias, Span<int> widths)
    {
        var total = 0;
        for (var i = 0; i < count; i++)
        {
            total += runs[run + i];
        }

        if (total < modules)
        {
            return false;
        }

        var oddPosition = oddFirst ? 0 : 1;
        Span<float> errors = stackalloc float[count];
        int oddSum = 0, evenSum = 0;
        for (var i = 0; i < count; i++)
        {
            // The bias is taken out, bars narrowed and spaces widened by it; as many of each, so the total stays.
            var isBar = ((i & 1) == 0) == firstIsBar;
            var exact = (runs[run + i] - (isBar ? bias : -bias)) * modules / total;
            if (exact < NarrowestRun)
            {
                return false;
            }

            widths[i] = Math.Clamp((int)MathF.Round(exact), 1, widest);
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
            var evenBad = (evenSum & 1) != evenParity;
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
                var growOdd = Pick(widths, errors, count, widest, oddPosition, grow: true);
                var shrinkEven = Pick(widths, errors, count, widest, 1 - oddPosition, grow: false);
                var shrinkOdd = Pick(widths, errors, count, widest, oddPosition, grow: false);
                var growEven = Pick(widths, errors, count, widest, 1 - oddPosition, grow: true);
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
                ? Pick(widths, errors, count, widest, oddBad ? oddPosition : 1 - oddPosition, growing)
                : Better(Pick(widths, errors, count, widest, 0, growing), Pick(widths, errors, count, widest, 1, growing), errors, growing);
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
        static int Pick(Span<int> widths, Span<float> errors, int count, int widest, int position, bool grow)
        {
            var pick = -1;
            for (var i = position; i < count; i += 2)
            {
                if ((grow && widths[i] < widest && (pick < 0 || errors[i] > errors[pick])) ||
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
    /// The number of the arrangement <paramref name="widths"/> is among all arrangements of as many modules over as many
    /// elements with none wider than <paramref name="maxWidth"/> - and, when <paramref name="noNarrow"/>, at least one
    /// a single module wide - in the order the specification enumerates them; -1 for widths that break the rules.
    /// </summary>
    public static int RssValue(ReadOnlySpan<int> widths, int maxWidth, bool noNarrow)
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
    public static char CheckDigit(string digits)
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
