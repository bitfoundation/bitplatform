using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Decodes Code 93, including its full ASCII form.
/// </summary>
/// <remarks>
/// <para>
/// Every Code 93 character is nine modules wide: three bars and three spaces of one to four
/// modules each, with no gap between characters. Two check characters, C and K, close every
/// symbol, each the sum of the characters before it weighted by position modulo 47, which makes
/// it far harder to misread than Code 39 or Codabar: a scan line that drops or mangles a
/// character leaves both checks to fail.
/// </para>
/// <para>
/// Four of the 47 characters are shifts that, paired with a following letter, stand for the rest
/// of ASCII. They exist for nothing else, so they are always decoded.
/// </para>
/// </remarks>
public sealed class BbCode93Reader : IBbRowDecoder, IBbImageHolder
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";

    /// <summary>The value of the first shift character, ($); (%), (/) and (+) follow it.</summary>
    private const int FirstShift = 43;

    /// <summary>The value the start and stop character decodes to.</summary>
    private const int StartStop = 47;

    /// <summary>
    /// Bar and space widths of every character, bar first, indexed by value: the 43 characters of
    /// <see cref="Alphabet"/>, the four shifts and the start/stop character.
    /// </summary>
    private static readonly int[][] Characters =
    [
        [1, 3, 1, 1, 1, 2], [1, 1, 1, 2, 1, 3], [1, 1, 1, 3, 1, 2], [1, 1, 1, 4, 1, 1], [1, 2, 1, 1, 1, 3], // 0-4
        [1, 2, 1, 2, 1, 2], [1, 2, 1, 3, 1, 1], [1, 1, 1, 1, 1, 4], [1, 3, 1, 2, 1, 1], [1, 4, 1, 1, 1, 1], // 5-9
        [2, 1, 1, 1, 1, 3], [2, 1, 1, 2, 1, 2], [2, 1, 1, 3, 1, 1], [2, 2, 1, 1, 1, 2], [2, 2, 1, 2, 1, 1], // A-E
        [2, 3, 1, 1, 1, 1], [1, 1, 2, 1, 1, 3], [1, 1, 2, 2, 1, 2], [1, 1, 2, 3, 1, 1], [1, 2, 2, 1, 1, 2], // F-J
        [1, 3, 2, 1, 1, 1], [1, 1, 1, 1, 2, 3], [1, 1, 1, 2, 2, 2], [1, 1, 1, 3, 2, 1], [1, 2, 1, 1, 2, 2], // K-O
        [1, 3, 1, 1, 2, 1], [2, 1, 2, 1, 1, 2], [2, 1, 2, 2, 1, 1], [2, 1, 1, 1, 2, 2], [2, 1, 1, 2, 2, 1], // P-T
        [2, 2, 1, 1, 2, 1], [2, 2, 2, 1, 1, 1], [1, 1, 2, 1, 2, 2], [1, 1, 2, 2, 2, 1], [1, 2, 2, 1, 2, 1], // U-Y
        [1, 2, 3, 1, 1, 1], [1, 2, 1, 1, 3, 1], [3, 1, 1, 1, 1, 2], [3, 1, 1, 2, 1, 1], [3, 2, 1, 1, 1, 1], // Z - . space $
        [1, 1, 2, 1, 3, 1], [1, 1, 3, 1, 2, 1], [2, 1, 1, 1, 3, 1], [1, 2, 1, 2, 2, 1], [3, 1, 2, 1, 1, 1], // / + % ($) (%)
        [3, 1, 1, 1, 2, 1], [1, 2, 2, 2, 1, 1], [1, 1, 1, 1, 4, 1],                                           // (/) (+) start/stop
    ];

    /// <summary>Character value by packed module widths, two bits a width less one; -1 for none.</summary>
    private static readonly int[] ValueByPattern = BuildLookup();

    private const float MaxAvgVariance = 0.3f;
    private const float MaxIndividualVariance = 0.8f;

    private readonly int[] _counters = new int[6];
    private readonly List<int> _runs = new(256);
    private readonly List<int> _runStarts = new(256);
    private readonly List<int> _values = new(32);

    /// <inheritdoc />
    void IBbImageHolder.ForgetImage()
    {
        BbLinearPatterns.Forget(_runs);
        BbLinearPatterns.Forget(_runStarts);
        BbLinearPatterns.Forget(_values);
    }
    private readonly StringBuilder _builder = new(32);

    /// <inheritdoc />
    public BbBarcodeFormat Formats => BbBarcodeFormat.Code93;

    /// <inheritdoc />
    public BbSymbolDecodeResult? DecodeRow(int rowNumber, BbBitRow row, BbBarcodeFormat enabledFormats)
    {
        if ((enabledFormats & BbBarcodeFormat.Code93) == 0)
        {
            return null;
        }

        // A start character, a data character, the two checks, a stop and its termination bar.
        var runs = _runs;
        if (!BbLinearPatterns.ReadRuns(row, runs, _runStarts) || runs.Count < 31)
        {
            return null;
        }

        // The start character is found among the runs, not the pixels. Runs alternate colour, so
        // its six begin at every other one, and the quiet zone it needs, five modules of the nine
        // it is wide, is the single white run before it: one comparison that turns away nearly
        // every bar on a line through anything that is not Code 93 before it is classified.
        Span<int> candidate = stackalloc int[6];
        var firstBar = row[_runStarts[0]] ? 0 : 1;
        var attempts = 0;
        for (var i = firstBar == 0 ? 2 : 1; i + 6 <= runs.Count && attempts < 4; i += 2)
        {
            var width = 0;
            for (var e = 0; e < 6; e++)
            {
                candidate[e] = runs[i + e];
                width += candidate[e];
            }

            if (runs[i - 1] * 9 < width * 5 || Classify(candidate) != StartStop)
            {
                continue;
            }

            // A row can hold several start-shaped runs; try each until one opens a whole symbol.
            var start = _runStarts[i];
            var result = TryDecodeFrom(rowNumber, row, start, start + width);
            if (result is not null)
            {
                return result;
            }

            attempts++;
            i += 4;
        }

        return null;
    }

    private BbSymbolDecodeResult? TryDecodeFrom(int rowNumber, BbBitRow row, int start, int afterStart)
    {
        var counters = _counters;
        var values = _values;
        values.Clear();

        var startWidth = afterStart - start;
        var position = afterStart;
        while (true)
        {
            if (position >= row.Size || !row[position] || !BbLinearPatterns.RecordPattern(row, position, counters))
            {
                return null;
            }

            // Every character is nine modules; one far from the start character's width is a
            // different symbol, or none, that the scan line has run into.
            var width = BbMathUtils.Sum(counters);
            if (width * 2 < startWidth || width > startWidth * 2)
            {
                return null;
            }

            var value = Classify(counters);
            if (value < 0)
            {
                return null;
            }

            position += width;
            if (value == StartStop)
            {
                break;
            }

            values.Add(value);
            if (values.Count > 80)
            {
                return null;
            }
        }

        // The stop character is closed by a single module termination bar and the quiet zone.
        var module = startWidth / 9f;
        if (position >= row.Size || !row[position])
        {
            return null;
        }

        var barEnd = row.GetNextUnset(position);
        var bar = barEnd - position;
        if (bar < module * 0.4f || bar > module * 2.2f)
        {
            return null;
        }

        var quietEnd = (int)MathF.Ceiling(barEnd + (module * 5));
        if (quietEnd > row.Size || !row.IsRange(barEnd, quietEnd, false))
        {
            return null;
        }

        // At least one data character and the two checks.
        if (values.Count < 3 || !ChecksAreValid(values))
        {
            return null;
        }

        values.RemoveRange(values.Count - 2, 2);
        var text = DecodeFullAscii(values);
        if (text is null)
        {
            return null;
        }

        var payload = new BbDecoderResult(Encoding.ASCII.GetBytes(text), text);
        return new BbSymbolDecodeResult(
            payload,
            BbBarcodeFormat.Code93,
            [new BbScanPoint(start, rowNumber), new BbScanPoint(barEnd, rowNumber)]);
    }

    /// <summary>
    /// The value of the character six measured runs make up, or -1. The runs are scaled to nine
    /// modules with the rounding error handed to the elements rounded furthest, so a character
    /// whose module is not a whole number of pixels still sums to nine.
    /// </summary>
    private static int Classify(ReadOnlySpan<int> counters)
    {
        var total = 0;
        foreach (var counter in counters)
        {
            if (counter <= 0)
            {
                return -1;
            }

            total += counter;
        }

        if (total < 9)
        {
            return -1;
        }

        Span<int> widths = stackalloc int[6];
        Span<float> errors = stackalloc float[6];
        var sum = 0;
        for (var i = 0; i < 6; i++)
        {
            var exact = counters[i] * 9f / total;
            widths[i] = Math.Clamp((int)MathF.Round(exact), 1, 4);
            errors[i] = exact - widths[i];
            sum += widths[i];
        }

        while (sum != 9)
        {
            var grow = sum < 9;
            var best = -1;
            for (var i = 0; i < 6; i++)
            {
                if ((grow && widths[i] < 4 && (best < 0 || errors[i] > errors[best])) ||
                    (!grow && widths[i] > 1 && (best < 0 || errors[i] < errors[best])))
                {
                    best = i;
                }
            }

            if (best < 0)
            {
                return -1;
            }

            widths[best] += grow ? 1 : -1;
            errors[best] += grow ? -1 : 1;
            sum += grow ? 1 : -1;
        }

        var key = 0;
        foreach (var width in widths)
        {
            key = (key << 2) | (width - 1);
        }

        var value = ValueByPattern[key];
        if (value < 0)
        {
            return -1;
        }

        // The scaled widths name a character; the measured ones still have to look like it.
        return BbLinearPatterns.PatternMatchVariance(counters, Characters[value], MaxIndividualVariance) < MaxAvgVariance
            ? value
            : -1;
    }

    /// <summary>
    /// Verifies the C and K check characters: each is the sum of the values before it, weighted
    /// from one at the right and wrapping after twenty for C and fifteen for K, modulo 47.
    /// </summary>
    private static bool ChecksAreValid(List<int> values)
    {
        return Check(values, values.Count - 2, 20) == values[^2] &&
               Check(values, values.Count - 1, 15) == values[^1];

        static int Check(List<int> values, int count, int maxWeight)
        {
            var sum = 0;
            var weight = 1;
            for (var i = count - 1; i >= 0; i--)
            {
                sum += values[i] * weight;
                if (++weight > maxWeight)
                {
                    weight = 1;
                }
            }

            return sum % 47;
        }
    }

    /// <summary>
    /// Folds the shift pairs back into ASCII: ($) with a letter is a control character, (%) the
    /// rest of the punctuation and controls, (/) the punctuation of Code 39's alphabet and (+) a
    /// lower case letter. Returns <see langword="null"/> for a shift with nothing valid after it.
    /// </summary>
    private string? DecodeFullAscii(List<int> values)
    {
        var text = _builder;
        text.Clear();
        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (value < FirstShift)
            {
                text.Append(Alphabet[value]);
                continue;
            }

            if (++i >= values.Count || values[i] is < 10 or > 35)
            {
                return null;
            }

            var next = Alphabet[values[i]];
            char? decoded = (value - FirstShift) switch
            {
                0 => (char)(next - 64),
                1 => next switch
                {
                    >= 'A' and <= 'E' => (char)(next - 38),
                    >= 'F' and <= 'J' => (char)(next - 11),
                    >= 'K' and <= 'O' => (char)(next + 16),
                    >= 'P' and <= 'T' => (char)(next + 43),
                    'U' => '\0',
                    'V' => '@',
                    'W' => '`',
                    _ => (char)127,
                },
                2 => next switch
                {
                    >= 'A' and <= 'O' => (char)(next - 32),
                    'Z' => ':',
                    _ => null,
                },
                _ => (char)(next + 32),
            };

            if (decoded is null)
            {
                return null;
            }

            text.Append(decoded.Value);
        }

        return text.ToString();
    }

    private static int[] BuildLookup()
    {
        var lookup = new int[1 << 12];
        Array.Fill(lookup, -1);
        for (var value = 0; value < Characters.Length; value++)
        {
            var key = 0;
            foreach (var width in Characters[value])
            {
                key = (key << 2) | (width - 1);
            }

            lookup[key] = value;
        }

        return lookup;
    }
}
