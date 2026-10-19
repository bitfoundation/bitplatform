using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Decodes the EAN/UPC family: EAN-13, EAN-8, UPC-A and UPC-E.
/// </summary>
/// <remarks>
/// <para>
/// The four symbologies share a start guard, a module width and a digit table, so they are read
/// by one decoder that locates the guard once and then tries each enabled layout against it.
/// Doing this in one pass rather than four independent readers roughly quarters the cost of the
/// retail symbology group, which matters because these are the codes most often scanned
/// continuously.
/// </para>
/// <para>
/// UPC-A is not a separate encoding: it is an EAN-13 symbol whose first digit is zero. UPC-E is
/// a genuinely different, six digit layout that carries its number system and check digit in the
/// parity of the digits rather than as modules.
/// </para>
/// </remarks>
public sealed class BbEanUpcReader : IBbRowDecoder
{
    private static readonly int[] StartEndPattern = [1, 1, 1];
    private static readonly int[] MiddlePattern = [1, 1, 1, 1, 1];
    private static readonly int[] UpcEEndPattern = [1, 1, 1, 1, 1, 1];

    /// <summary>Element widths of the left-hand, odd parity digit encodings.</summary>
    private static readonly int[][] LPatterns =
    [
        [3, 2, 1, 1], [2, 2, 2, 1], [2, 1, 2, 2], [1, 4, 1, 1], [1, 1, 3, 2],
        [1, 2, 3, 1], [1, 1, 1, 4], [1, 3, 1, 2], [1, 2, 1, 3], [3, 1, 1, 2],
    ];

    /// <summary>
    /// The odd parity encodings followed by the even parity ones, which are simply the odd
    /// encodings read backwards. Indices 10 to 19 therefore mean "digit i - 10, even parity".
    /// </summary>
    private static readonly int[][] LAndGPatterns =
    [
        [3, 2, 1, 1], [2, 2, 2, 1], [2, 1, 2, 2], [1, 4, 1, 1], [1, 1, 3, 2],
        [1, 2, 3, 1], [1, 1, 1, 4], [1, 3, 1, 2], [1, 2, 1, 3], [3, 1, 1, 2],
        [1, 1, 2, 3], [1, 2, 2, 2], [2, 2, 1, 2], [1, 1, 4, 1], [2, 3, 1, 1],
        [1, 3, 2, 1], [4, 1, 1, 1], [2, 1, 3, 1], [3, 1, 2, 1], [2, 1, 1, 3],
    ];

    /// <summary>Parity of the six left-hand digits encodes the first digit of an EAN-13.</summary>
    private static readonly int[] FirstDigitEncodings =
        [0x00, 0x0B, 0x0D, 0x0E, 0x13, 0x19, 0x1C, 0x15, 0x16, 0x1A];

    /// <summary>Parity of the six digits of a UPC-E encodes its number system and check digit.</summary>
    private static readonly int[][] UpcENumberSystemPatterns =
    [
        [0x38, 0x34, 0x32, 0x31, 0x2C, 0x26, 0x23, 0x2A, 0x29, 0x25],
        [0x07, 0x0B, 0x0D, 0x0E, 0x13, 0x19, 0x1C, 0x15, 0x16, 0x1A],
    ];

    private readonly int[] _counters = new int[4];
    private readonly int[] _guardCounters = new int[6];
    private readonly StringBuilder _builder = new(16);

    /// <inheritdoc />
    public BbBarcodeFormat Formats =>
        BbBarcodeFormat.Ean13 | BbBarcodeFormat.Ean8 | BbBarcodeFormat.UpcA | BbBarcodeFormat.UpcE;

    /// <inheritdoc />
    public BbSymbolDecodeResult? DecodeRow(int rowNumber, BbBitRow row, BbBarcodeFormat enabledFormats)
    {
        var wanted = enabledFormats & Formats;
        if (wanted == 0)
        {
            return null;
        }

        var searchFrom = 0;
        // A row can contain several candidate guards; try each in turn rather than giving up on
        // the first one, which is what makes the reader work when the frame contains packaging
        // artwork with guard-like patterns next to the real symbol.
        for (var attempt = 0; attempt < 4; attempt++)
        {
            if (!TryFindStartGuard(row, searchFrom, out var guardStart, out var guardEnd))
            {
                return null;
            }

            searchFrom = guardEnd;

            if ((wanted & (BbBarcodeFormat.Ean13 | BbBarcodeFormat.UpcA)) != 0)
            {
                var result = TryDecodeEan13(rowNumber, row, guardStart, guardEnd, wanted);
                if (result is not null)
                {
                    return result;
                }
            }

            if ((wanted & BbBarcodeFormat.Ean8) != 0)
            {
                var result = TryDecodeEan8(rowNumber, row, guardStart, guardEnd);
                if (result is not null)
                {
                    return result;
                }
            }

            if ((wanted & BbBarcodeFormat.UpcE) != 0)
            {
                var result = TryDecodeUpcE(rowNumber, row, guardStart, guardEnd);
                if (result is not null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    private BbSymbolDecodeResult? TryDecodeEan13(int rowNumber, BbBitRow row, int guardStart, int guardEnd, BbBarcodeFormat wanted)
    {
        var result = _builder;
        result.Clear();

        var counters = _counters;
        var rowOffset = guardEnd;
        var end = row.Size;
        var moduleWidth = (guardEnd - guardStart) / 3.0f;
        var lgPatternFound = 0;

        for (var x = 0; x < 6; x++)
        {
            if (rowOffset >= end ||
                !TryDecodeDigit(row, counters, rowOffset, LAndGPatterns, out var match) ||
                !IsDigitWidthPlausible(counters, moduleWidth))
            {
                return null;
            }

            result.Append((char)('0' + (match % 10)));
            rowOffset += counters[0] + counters[1] + counters[2] + counters[3];
            if (match >= 10)
            {
                lgPatternFound |= 1 << (5 - x);
            }
        }

        var firstDigit = Array.IndexOf(FirstDigitEncodings, lgPatternFound);
        if (firstDigit < 0)
        {
            return null;
        }

        result.Insert(0, (char)('0' + firstDigit));

        if (!TryMatchGuardAt(row, rowOffset, whiteFirst: true, MiddlePattern, out var middleEnd))
        {
            return null;
        }

        rowOffset = middleEnd;
        for (var x = 0; x < 6; x++)
        {
            if (rowOffset >= end ||
                !TryDecodeDigit(row, counters, rowOffset, LPatterns, out var match) ||
                !IsDigitWidthPlausible(counters, moduleWidth))
            {
                return null;
            }

            result.Append((char)('0' + match));
            rowOffset += counters[0] + counters[1] + counters[2] + counters[3];
        }

        if (!TryVerifyEndGuard(row, rowOffset, whiteFirst: false, StartEndPattern, out var symbolEnd))
        {
            return null;
        }

        if (result.Length != 13 || !IsChecksumValid(result))
        {
            return null;
        }

        var text = result.ToString();

        // A UPC-A symbol is an EAN-13 whose first digit is zero; report whichever of the two the
        // caller asked for, preferring UPC-A because that is the more specific reading.
        if (text[0] == '0' && (wanted & BbBarcodeFormat.UpcA) != 0)
        {
            return Build(rowNumber, text[1..], BbBarcodeFormat.UpcA, guardStart, guardEnd, symbolEnd);
        }

        if ((wanted & BbBarcodeFormat.Ean13) == 0)
        {
            return null;
        }

        return Build(rowNumber, text, BbBarcodeFormat.Ean13, guardStart, guardEnd, symbolEnd);
    }

    private BbSymbolDecodeResult? TryDecodeEan8(int rowNumber, BbBitRow row, int guardStart, int guardEnd)
    {
        var result = _builder;
        result.Clear();

        var counters = _counters;
        var rowOffset = guardEnd;
        var end = row.Size;
        var moduleWidth = (guardEnd - guardStart) / 3.0f;

        for (var x = 0; x < 4; x++)
        {
            if (rowOffset >= end ||
                !TryDecodeDigit(row, counters, rowOffset, LPatterns, out var match) ||
                !IsDigitWidthPlausible(counters, moduleWidth))
            {
                return null;
            }

            result.Append((char)('0' + match));
            rowOffset += counters[0] + counters[1] + counters[2] + counters[3];
        }

        if (!TryMatchGuardAt(row, rowOffset, whiteFirst: true, MiddlePattern, out var middleEnd))
        {
            return null;
        }

        rowOffset = middleEnd;
        for (var x = 0; x < 4; x++)
        {
            if (rowOffset >= end ||
                !TryDecodeDigit(row, counters, rowOffset, LPatterns, out var match) ||
                !IsDigitWidthPlausible(counters, moduleWidth))
            {
                return null;
            }

            result.Append((char)('0' + match));
            rowOffset += counters[0] + counters[1] + counters[2] + counters[3];
        }

        if (!TryVerifyEndGuard(row, rowOffset, whiteFirst: false, StartEndPattern, out var symbolEnd))
        {
            return null;
        }

        if (result.Length != 8 || !IsChecksumValid(result))
        {
            return null;
        }

        return Build(rowNumber, result.ToString(), BbBarcodeFormat.Ean8, guardStart, guardEnd, symbolEnd);
    }

    private BbSymbolDecodeResult? TryDecodeUpcE(int rowNumber, BbBitRow row, int guardStart, int guardEnd)
    {
        var result = _builder;
        result.Clear();

        var counters = _counters;
        var rowOffset = guardEnd;
        var end = row.Size;
        var moduleWidth = (guardEnd - guardStart) / 3.0f;
        var lgPatternFound = 0;

        for (var x = 0; x < 6; x++)
        {
            if (rowOffset >= end ||
                !TryDecodeDigit(row, counters, rowOffset, LAndGPatterns, out var match) ||
                !IsDigitWidthPlausible(counters, moduleWidth))
            {
                return null;
            }

            result.Append((char)('0' + (match % 10)));
            rowOffset += counters[0] + counters[1] + counters[2] + counters[3];
            if (match >= 10)
            {
                lgPatternFound |= 1 << (5 - x);
            }
        }

        var found = false;
        for (var numberSystem = 0; numberSystem <= 1 && !found; numberSystem++)
        {
            var checkDigit = Array.IndexOf(UpcENumberSystemPatterns[numberSystem], lgPatternFound);
            if (checkDigit < 0)
            {
                continue;
            }

            result.Insert(0, (char)('0' + numberSystem));
            result.Append((char)('0' + checkDigit));
            found = true;
        }

        if (!found)
        {
            return null;
        }

        // UPC-E ends with a six element guard rather than the three element guard of the others.
        if (!TryVerifyEndGuard(row, rowOffset, whiteFirst: true, UpcEEndPattern, out var symbolEnd))
        {
            return null;
        }

        var text = result.ToString();
        var expanded = ExpandUpcEToUpcA(text);
        if (expanded is null)
        {
            return null;
        }

        var expandedBuilder = new StringBuilder(expanded);
        if (!IsChecksumValid(expandedBuilder))
        {
            return null;
        }

        return Build(rowNumber, text, BbBarcodeFormat.UpcE, guardStart, guardEnd, symbolEnd);
    }

    /// <summary>
    /// Expands the eight character UPC-E representation into the twelve character UPC-A it
    /// stands for. The seventh digit selects which of the five compression rules was used.
    /// </summary>
    /// <param name="upce">An eight character UPC-E value, including number system and check digit.</param>
    /// <returns>The equivalent UPC-A value, or <see langword="null"/> when the input is malformed.</returns>
    public static string? ExpandUpcEToUpcA(string upce)
    {
        ArgumentNullException.ThrowIfNull(upce);
        if (upce.Length != 8)
        {
            return null;
        }

        var lastChar = upce[6];
        var result = new StringBuilder(12);
        result.Append(upce[0]);

        switch (lastChar)
        {
            case '0':
            case '1':
            case '2':
                result.Append(upce, 1, 2).Append(lastChar).Append("0000").Append(upce, 3, 3);
                break;
            case '3':
                result.Append(upce, 1, 3).Append("00000").Append(upce, 4, 2);
                break;
            case '4':
                result.Append(upce, 1, 4).Append("00000").Append(upce, 5, 1);
                break;
            default:
                if (lastChar is < '5' or > '9')
                {
                    return null;
                }

                result.Append(upce, 1, 5).Append("0000").Append(lastChar);
                break;
        }

        result.Append(upce[7]);
        return result.ToString();
    }

    private BbSymbolDecodeResult Build(
        int rowNumber, string text, BbBarcodeFormat format, int guardStart, int guardEnd, int symbolEnd)
    {
        var payload = new BbDecoderResult(Encoding.ASCII.GetBytes(text), text);
        var left = (guardStart + guardEnd) / 2.0f;
        return new BbSymbolDecodeResult(
            payload,
            format,
            [new BbScanPoint(left, rowNumber), new BbScanPoint(symbolEnd, rowNumber)]);
    }

    private bool TryVerifyEndGuard(BbBitRow row, int rowOffset, bool whiteFirst, int[] pattern, out int symbolEnd)
    {
        symbolEnd = 0;
        if (!TryMatchGuardAt(row, rowOffset, whiteFirst, pattern, out var end))
        {
            return false;
        }

        // The symbol must be followed by a quiet zone at least as wide as the end guard itself,
        // and for UPC-E the seven modules its specification demands: the middle guard of an
        // EAN-13 followed by the first bar of its seventh digit is the same six elements as a
        // UPC-E end guard, so a damaged EAN-13 is otherwise a UPC-E candidate.
        var guardWidth = end - rowOffset;
        var quietWidth = pattern.Length == UpcEEndPattern.Length
            ? (int)MathF.Ceiling(guardWidth * 7f / pattern.Length)
            : guardWidth;
        var quietEnd = end + quietWidth;
        if (quietEnd > row.Size || !row.IsRange(end, quietEnd, false))
        {
            return false;
        }

        symbolEnd = end;
        return true;
    }

    /// <summary>
    /// Matches a guard pattern that must begin exactly at <paramref name="rowOffset"/>.
    /// </summary>
    /// <remarks>
    /// Searching forward for a guard, as the start guard search does, is right when nothing is
    /// known about where the symbol is. Once digits have been read the next guard has to be the
    /// very next run; accepting one further along lets a row of noise assemble a "symbol" out of
    /// unrelated fragments, which is where nearly every EAN/UPC false positive comes from.
    /// </remarks>
    private bool TryMatchGuardAt(BbBitRow row, int rowOffset, bool whiteFirst, int[] pattern, out int end)
    {
        end = 0;
        var counters = _guardCounters.AsSpan(0, pattern.Length);
        if (rowOffset >= row.Size || row[rowOffset] == whiteFirst || !BbLinearPatterns.RecordPattern(row, rowOffset, counters))
        {
            return false;
        }

        if (BbLinearPatterns.PatternMatchVariance(counters, pattern, BbLinearPatterns.MaxIndividualVariance) >= BbLinearPatterns.MaxAvgVariance)
        {
            return false;
        }

        var total = 0;
        foreach (var counter in counters)
        {
            total += counter;
        }

        end = rowOffset + total;
        return true;
    }

    /// <summary>
    /// Rejects a digit whose total width is inconsistent with the module width established by
    /// the start guard. Every EAN/UPC digit is exactly seven modules wide.
    /// </summary>
    private static bool IsDigitWidthPlausible(ReadOnlySpan<int> counters, float moduleWidth)
    {
        var width = counters[0] + counters[1] + counters[2] + counters[3];
        var expected = 7 * moduleWidth;
        var tolerance = Math.Max(2.5f, expected * 0.45f);
        return Math.Abs(width - expected) <= tolerance;
    }

    private bool TryFindStartGuard(BbBitRow row, int from, out int guardStart, out int guardEnd)
    {
        guardStart = 0;
        guardEnd = 0;

        var offset = from;
        while (BbLinearPatterns.FindGuardPattern(row, offset, false, StartEndPattern, _guardCounters, out var start, out var end))
        {
            // Require a leading quiet zone as wide as the guard pattern.
            var quietStart = start - (end - start);
            if (quietStart >= 0 && row.IsRange(quietStart, start, false))
            {
                guardStart = start;
                guardEnd = end;
                return true;
            }

            offset = end;
        }

        return false;
    }

    private static bool TryDecodeDigit(BbBitRow row, int[] counters, int rowOffset, int[][] patterns, out int bestMatch)
    {
        bestMatch = -1;
        if (!BbLinearPatterns.RecordPattern(row, rowOffset, counters))
        {
            return false;
        }

        var bestVariance = BbLinearPatterns.MaxAvgVariance;
        for (var i = 0; i < patterns.Length; i++)
        {
            var variance = BbLinearPatterns.PatternMatchVariance(
                counters, patterns[i], BbLinearPatterns.MaxIndividualVariance);
            if (variance < bestVariance)
            {
                bestVariance = variance;
                bestMatch = i;
            }
        }

        return bestMatch >= 0;
    }

    /// <summary>Verifies the standard EAN/UPC modulo 10 check digit.</summary>
    /// <param name="digits">The full value, check digit last.</param>
    private static bool IsChecksumValid(StringBuilder digits)
    {
        var length = digits.Length;
        if (length == 0)
        {
            return false;
        }

        var sum = 0;
        for (var i = length - 2; i >= 0; i--)
        {
            var digit = digits[i] - '0';
            if ((uint)digit > 9)
            {
                return false;
            }

            sum += ((length - i) % 2 == 0) ? digit * 3 : digit;
        }

        return digits[length - 1] - '0' == (1000 - sum) % 10;
    }
}
