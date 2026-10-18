using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Decodes Code 39 (ISO/IEC 16388), optionally with the modulo 43 check character and the
/// full ASCII extension.
/// </summary>
/// <remarks>
/// Code 39 is a two-width symbology: every character is nine elements of which exactly three
/// are wide, and there is no fixed module width. The reader therefore derives the narrow/wide
/// boundary from the measured elements themselves, which is what makes it tolerant of the
/// printing variation these symbols are usually produced with.
/// </remarks>
public sealed class BbCode39Reader : IBbRowDecoder
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";
    private const int AsteriskEncoding = 0x094;

    /// <summary>
    /// One entry per character of <see cref="Alphabet"/>. Bit <c>8 - i</c> is set when element
    /// <c>i</c> of the character is wide; elements alternate bar, space, starting with a bar.
    /// </summary>
    private static readonly int[] CharacterEncodings =
    [
        0x034, 0x121, 0x061, 0x160, 0x031, 0x130, 0x070, 0x025, 0x124, 0x064, // 0-9
        0x109, 0x049, 0x148, 0x019, 0x118, 0x058, 0x00D, 0x10C, 0x04C, 0x01C, // A-J
        0x103, 0x043, 0x142, 0x013, 0x112, 0x052, 0x007, 0x106, 0x046, 0x016, // K-T
        0x181, 0x0C1, 0x1C0, 0x091, 0x190, 0x0D0, 0x085, 0x184, 0x0C4, 0x0A8, // U-Z, -, ., space, $
        0x0A2, 0x08A, 0x02A,                                                  // /, +, %
    ];

    private readonly int[] _counters = new int[9];
    private readonly StringBuilder _builder = new(24);

    /// <summary>Creates a reader.</summary>
    /// <param name="validateCheckCharacter">
    /// When <see langword="true"/> the last character is treated as a modulo 43 check character,
    /// verified and removed from the result.
    /// </param>
    /// <param name="extendedMode">
    /// When <see langword="true"/> the two-character escape sequences of Code 39 Extended are
    /// folded back into the full ASCII range.
    /// </param>
    public BbCode39Reader(bool validateCheckCharacter = false, bool extendedMode = false)
    {
        ValidateCheckCharacter = validateCheckCharacter;
        ExtendedMode = extendedMode;
    }

    /// <summary>Whether the modulo 43 check character is verified.</summary>
    public bool ValidateCheckCharacter { get; }

    /// <summary>Whether Code 39 Extended escape sequences are decoded.</summary>
    public bool ExtendedMode { get; }

    /// <inheritdoc />
    public BbBarcodeFormat Formats => BbBarcodeFormat.Code39;

    /// <inheritdoc />
    public BbSymbolDecodeResult? DecodeRow(int rowNumber, BbBitRow row, BbBarcodeFormat enabledFormats)
    {
        if ((enabledFormats & BbBarcodeFormat.Code39) == 0)
        {
            return null;
        }

        var counters = _counters;
        if (!TryFindAsteriskPattern(row, counters, out var patternStart, out var patternEnd))
        {
            return null;
        }

        var result = _builder;
        result.Clear();

        // The asterisk fixes the narrow element width; every later character must agree with it
        // and the gaps between characters may not exceed a few narrow elements.
        var narrowReference = NarrowWidth(counters, AsteriskEncoding);
        var maxGap = (int)(narrowReference * 6) + 1;

        var nextStart = row.GetNextSet(patternEnd);
        if (nextStart - patternEnd > maxGap)
        {
            return null;
        }

        var end = row.Size;
        var lastStart = patternStart;

        while (true)
        {
            if (!BbLinearPatterns.RecordPattern(row, nextStart, counters))
            {
                return null;
            }

            var pattern = ToNarrowWidePattern(counters);
            if (pattern < 0)
            {
                return null;
            }

            var narrow = NarrowWidth(counters, pattern);
            if (narrow * 2 < narrowReference || narrow > narrowReference * 2)
            {
                return null;
            }

            var isAsterisk = pattern == AsteriskEncoding;
            if (!isAsterisk)
            {
                var index = Array.IndexOf(CharacterEncodings, pattern);
                if (index < 0)
                {
                    return null;
                }

                result.Append(Alphabet[index]);
            }

            lastStart = nextStart;
            foreach (var counter in counters)
            {
                nextStart += counter;
            }

            var gapStart = nextStart;
            nextStart = row.GetNextSet(nextStart);

            if (isAsterisk)
            {
                // The closing delimiter marks the end of the symbol.
                break;
            }

            if (nextStart - gapStart > maxGap)
            {
                return null;
            }

            // Guard against a runaway scan on a noisy row.
            if (result.Length > 80)
            {
                return null;
            }
        }

        return Finish();

        BbSymbolDecodeResult? Finish()
        {
            // Require a quiet zone after the closing asterisk, inside the frame: a symbol that
            // runs off the edge of the image cannot be trusted.
            var lastPatternSize = BbMathUtils.Sum(counters);
            var whiteSpaceAfterEnd = nextStart - lastStart - lastPatternSize;
            if ((whiteSpaceAfterEnd * 2) < lastPatternSize)
            {
                return null;
            }

            if (ValidateCheckCharacter)
            {
                if (result.Length == 0)
                {
                    return null;
                }

                var total = 0;
                for (var i = 0; i < result.Length - 1; i++)
                {
                    total += Alphabet.IndexOf(result[i], StringComparison.Ordinal);
                }

                if (result[^1] != Alphabet[total % 43])
                {
                    return null;
                }

                result.Length--;
            }

            if (result.Length == 0)
            {
                return null;
            }

            var text = ExtendedMode ? DecodeExtended(result) : result.ToString();
            if (text is null)
            {
                return null;
            }

            var payload = new BbDecoderResult(Encoding.ASCII.GetBytes(text), text);
            var left = (patternStart + patternEnd) / 2.0f;
            var right = lastStart + (lastPatternSize / 2.0f);
            return new BbSymbolDecodeResult(
                payload,
                BbBarcodeFormat.Code39,
                [new BbScanPoint(left, rowNumber), new BbScanPoint(right, rowNumber)]);
        }
    }

    private static bool TryFindAsteriskPattern(BbBitRow row, int[] counters, out int patternStart, out int patternEnd)
    {
        patternStart = 0;
        patternEnd = 0;

        var width = row.Size;
        var rowOffset = row.GetNextSet(0);
        Array.Clear(counters);

        var counterPosition = 0;
        var start = rowOffset;
        var isWhite = false;
        const int PatternLength = 9;

        for (var i = rowOffset; i < width; i++)
        {
            if (row[i] != isWhite)
            {
                counters[counterPosition]++;
                continue;
            }

            if (counterPosition == PatternLength - 1)
            {
                // Require a quiet zone of at least half the start pattern width before the
                // symbol, and require it to lie inside the frame.
                var quietStart = start - ((i - start) / 2);
                if (quietStart >= 0 &&
                    ToNarrowWidePattern(counters) == AsteriskEncoding &&
                    row.IsRange(quietStart, start, false))
                {
                    patternStart = start;
                    patternEnd = i;
                    return true;
                }

                start += counters[0] + counters[1];
                Array.Copy(counters, 2, counters, 0, PatternLength - 2);
                counters[PatternLength - 2] = 0;
                counters[PatternLength - 1] = 0;
                counterPosition--;
            }
            else
            {
                counterPosition++;
            }

            counters[counterPosition] = 1;
            isWhite = !isWhite;
        }

        return false;
    }

    /// <summary>
    /// Classifies the measured elements into narrow and wide, returning a bit pattern where
    /// bit <c>8 - i</c> is set when element <c>i</c> is wide, or -1 when the classification is
    /// not consistent with a Code 39 character.
    /// </summary>
    private static int ToNarrowWidePattern(int[] counters)
    {
        var numCounters = counters.Length;
        var maxNarrowCounter = 0;
        int wideCounters;

        do
        {
            var minCounter = int.MaxValue;
            foreach (var counter in counters)
            {
                if (counter < minCounter && counter > maxNarrowCounter)
                {
                    minCounter = counter;
                }
            }

            maxNarrowCounter = minCounter;
            wideCounters = 0;
            var totalWideCountersWidth = 0;
            var pattern = 0;
            for (var i = 0; i < numCounters; i++)
            {
                var counter = counters[i];
                if (counter > maxNarrowCounter)
                {
                    pattern |= 1 << (numCounters - 1 - i);
                    wideCounters++;
                    totalWideCountersWidth += counter;
                }
            }

            if (wideCounters == 3)
            {
                // Three wide elements is the right count, but they also have to be similar in
                // width; a single element that is more than half the total is a mis-measurement.
                var totalNarrowWidth = 0;
                for (var i = 0; i < numCounters; i++)
                {
                    var counter = counters[i];
                    if (counter <= maxNarrowCounter)
                    {
                        totalNarrowWidth += counter;
                        continue;
                    }

                    if (counter * 2 >= totalWideCountersWidth)
                    {
                        return -1;
                    }
                }

                // The specification allows wide to narrow ratios from 2:1 to 3:1. Anything close
                // to 1:1 is a run of noise whose widths happen to sort into two groups.
                if (totalWideCountersWidth * 6 < totalNarrowWidth * 3 * 16 / 10)
                {
                    return -1;
                }

                return pattern;
            }
        }
        while (wideCounters > 3);

        return -1;
    }

    /// <summary>Average width of the six narrow elements of a character.</summary>
    private static float NarrowWidth(int[] counters, int pattern)
    {
        var total = 0;
        var count = 0;
        for (var i = 0; i < counters.Length; i++)
        {
            if ((pattern & (1 << (counters.Length - 1 - i))) == 0)
            {
                total += counters[i];
                count++;
            }
        }

        return count == 0 ? 0 : (float)total / count;
    }

    private static string? DecodeExtended(StringBuilder encoded)
    {
        var length = encoded.Length;
        var decoded = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            var c = encoded[i];
            if (c is not ('+' or '$' or '%' or '/'))
            {
                decoded.Append(c);
                continue;
            }

            if (i + 1 >= length)
            {
                return null;
            }

            var next = encoded[i + 1];
            char decodedChar;
            switch (c)
            {
                case '+':
                    if (next is < 'A' or > 'Z')
                    {
                        return null;
                    }

                    decodedChar = (char)(next + 32);
                    break;
                case '$':
                    if (next is < 'A' or > 'Z')
                    {
                        return null;
                    }

                    decodedChar = (char)(next - 64);
                    break;
                case '%':
                    if (next is >= 'A' and <= 'E')
                    {
                        decodedChar = (char)(next - 38);
                    }
                    else if (next is >= 'F' and <= 'J')
                    {
                        decodedChar = (char)(next - 11);
                    }
                    else if (next is >= 'K' and <= 'O')
                    {
                        decodedChar = (char)(next + 16);
                    }
                    else if (next is >= 'P' and <= 'T')
                    {
                        decodedChar = (char)(next + 43);
                    }
                    else if (next is 'U')
                    {
                        decodedChar = (char)0;
                    }
                    else if (next is 'V')
                    {
                        decodedChar = '@';
                    }
                    else if (next is 'W')
                    {
                        decodedChar = '`';
                    }
                    else if (next is 'X' or 'Y' or 'Z')
                    {
                        decodedChar = (char)127;
                    }
                    else
                    {
                        return null;
                    }

                    break;
                default:
                    if (next is < 'A' or > 'O')
                    {
                        if (next == 'Z')
                        {
                            decodedChar = ':';
                            break;
                        }

                        return null;
                    }

                    decodedChar = (char)(next - 32);
                    break;
            }

            decoded.Append(decodedChar);
            i++;
        }

        return decoded.ToString();
    }
}
