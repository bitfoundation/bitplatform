using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Decodes Code 128 (ISO/IEC 15417), including the GS1-128 application identifier variant.
/// </summary>
/// <remarks>
/// Code 128 characters are eleven modules wide and always split into three bars and three
/// spaces, so a character is recognised by matching six run widths against the 107 entry
/// character table. The three code sets are handled as a small state machine, because a single
/// symbol can switch between them and even shift into another set for exactly one character.
/// </remarks>
public sealed class BbCode128Reader : IBbRowDecoder
{
    private const int CodeShift = 98;
    private const int CodeCodeC = 99;
    private const int CodeCodeB = 100;
    private const int CodeCodeA = 101;
    private const int CodeFnc1 = 102;
    private const int CodeFnc2 = 97;
    private const int CodeFnc3 = 96;
    private const int CodeFnc4A = 101;
    private const int CodeFnc4B = 100;
    private const int CodeStartA = 103;
    private const int CodeStartB = 104;
    private const int CodeStartC = 105;
    private const int CodeStop = 106;

    /// <summary>
    /// The Code 128 character table. Each entry lists six element widths in modules,
    /// bar first; the stop character is the one exception and carries a seventh element.
    /// </summary>
    private static readonly int[][] CodePatterns =
    [
        [2, 1, 2, 2, 2, 2], [2, 2, 2, 1, 2, 2], [2, 2, 2, 2, 2, 1], [1, 2, 1, 2, 2, 3], [1, 2, 1, 3, 2, 2],
        [1, 3, 1, 2, 2, 2], [1, 2, 2, 2, 1, 3], [1, 2, 2, 3, 1, 2], [1, 3, 2, 2, 1, 2], [2, 2, 1, 2, 1, 3],
        [2, 2, 1, 3, 1, 2], [2, 3, 1, 2, 1, 2], [1, 1, 2, 2, 3, 2], [1, 2, 2, 1, 3, 2], [1, 2, 2, 2, 3, 1],
        [1, 1, 3, 2, 2, 2], [1, 2, 3, 1, 2, 2], [1, 2, 3, 2, 2, 1], [2, 2, 3, 2, 1, 1], [2, 2, 1, 1, 3, 2],
        [2, 2, 1, 2, 3, 1], [2, 1, 3, 2, 1, 2], [2, 2, 3, 1, 1, 2], [3, 1, 2, 1, 3, 1], [3, 1, 1, 2, 2, 2],
        [3, 2, 1, 1, 2, 2], [3, 2, 1, 2, 2, 1], [3, 1, 2, 2, 1, 2], [3, 2, 2, 1, 1, 2], [3, 2, 2, 2, 1, 1],
        [2, 1, 2, 1, 2, 3], [2, 1, 2, 3, 2, 1], [2, 3, 2, 1, 2, 1], [1, 1, 1, 3, 2, 3], [1, 3, 1, 1, 2, 3],
        [1, 3, 1, 3, 2, 1], [1, 1, 2, 3, 1, 3], [1, 3, 2, 1, 1, 3], [1, 3, 2, 3, 1, 1], [2, 1, 1, 3, 1, 3],
        [2, 3, 1, 1, 1, 3], [2, 3, 1, 3, 1, 1], [1, 1, 2, 1, 3, 3], [1, 1, 2, 3, 3, 1], [1, 3, 2, 1, 3, 1],
        [1, 1, 3, 1, 2, 3], [1, 1, 3, 3, 2, 1], [1, 3, 3, 1, 2, 1], [3, 1, 3, 1, 2, 1], [2, 1, 1, 3, 3, 1],
        [2, 3, 1, 1, 3, 1], [2, 1, 3, 1, 1, 3], [2, 1, 3, 3, 1, 1], [2, 1, 3, 1, 3, 1], [3, 1, 1, 1, 2, 3],
        [3, 1, 1, 3, 2, 1], [3, 3, 1, 1, 2, 1], [3, 1, 2, 1, 1, 3], [3, 1, 2, 3, 1, 1], [3, 3, 2, 1, 1, 1],
        [3, 1, 4, 1, 1, 1], [2, 2, 1, 4, 1, 1], [4, 3, 1, 1, 1, 1], [1, 1, 1, 2, 2, 4], [1, 1, 1, 4, 2, 2],
        [1, 2, 1, 1, 2, 4], [1, 2, 1, 4, 2, 1], [1, 4, 1, 1, 2, 2], [1, 4, 1, 2, 2, 1], [1, 1, 2, 2, 1, 4],
        [1, 1, 2, 4, 1, 2], [1, 2, 2, 1, 1, 4], [1, 2, 2, 4, 1, 1], [1, 4, 2, 1, 1, 2], [1, 4, 2, 2, 1, 1],
        [2, 4, 1, 2, 1, 1], [2, 2, 1, 1, 1, 4], [4, 1, 3, 1, 1, 1], [2, 4, 1, 1, 1, 2], [1, 3, 4, 1, 1, 1],
        [1, 1, 1, 2, 4, 2], [1, 2, 1, 1, 4, 2], [1, 2, 1, 2, 4, 1], [1, 1, 4, 2, 1, 2], [1, 2, 4, 1, 1, 2],
        [1, 2, 4, 2, 1, 1], [4, 1, 1, 2, 1, 2], [4, 2, 1, 1, 1, 2], [4, 2, 1, 2, 1, 1], [2, 1, 2, 1, 4, 1],
        [2, 1, 4, 1, 2, 1], [4, 1, 2, 1, 2, 1], [1, 1, 1, 1, 4, 3], [1, 1, 1, 3, 4, 1], [1, 3, 1, 1, 4, 1],
        [1, 1, 4, 1, 1, 3], [1, 1, 4, 3, 1, 1], [4, 1, 1, 1, 1, 3], [4, 1, 1, 3, 1, 1], [1, 1, 3, 1, 4, 1],
        [1, 1, 4, 1, 3, 1], [3, 1, 1, 1, 4, 1], [4, 1, 1, 1, 3, 1], [2, 1, 1, 4, 1, 2], [2, 1, 1, 2, 1, 4],
        [2, 1, 1, 2, 3, 2], [2, 3, 3, 1, 1, 1, 2],
    ];

    private readonly int[] _counters = new int[6];
    private readonly StringBuilder _builder = new(32);

    /// <inheritdoc />
    public BbBarcodeFormat Formats => BbBarcodeFormat.Code128;

    /// <inheritdoc />
    public BbSymbolDecodeResult? DecodeRow(int rowNumber, BbBitRow row, BbBarcodeFormat enabledFormats)
    {
        if ((enabledFormats & BbBarcodeFormat.Code128) == 0)
        {
            return null;
        }

        if (!TryFindStartPattern(row, out var patternStart, out var patternEnd, out var startCode))
        {
            return null;
        }

        var codeSet = startCode switch
        {
            CodeStartA => CodeCodeA,
            CodeStartB => CodeCodeB,
            _ => CodeCodeC,
        };

        var result = _builder;
        result.Clear();

        var counters = _counters;
        var lastStart = patternStart;
        var nextStart = patternEnd;

        var checksumTotal = startCode;
        var multiplier = 0;
        var code = 0;
        var lastCode = 0;
        var isNextShifted = false;
        var lastCharacterWasPrintable = true;
        var upperMode = false;
        var shiftUpperMode = false;
        var isGs1 = false;
        var done = false;

        while (!done)
        {
            var unshift = isNextShifted;
            isNextShifted = false;
            lastCode = code;

            if (!TryDecodeCode(row, counters, nextStart, out code))
            {
                return null;
            }

            if (code != CodeStop)
            {
                lastCharacterWasPrintable = true;
                multiplier++;
                checksumTotal += multiplier * code;
            }

            lastStart = nextStart;
            foreach (var counter in counters)
            {
                nextStart += counter;
            }

            // A start character in the middle of a symbol means the scan line crossed two symbols.
            if (code is CodeStartA or CodeStartB or CodeStartC)
            {
                return null;
            }

            switch (codeSet)
            {
                case CodeCodeA:
                    if (code < 64)
                    {
                        result.Append((char)((shiftUpperMode == upperMode ? ' ' : ' ' + 128) + code));
                        shiftUpperMode = false;
                    }
                    else if (code < 96)
                    {
                        result.Append((char)(shiftUpperMode == upperMode ? code - 64 : code + 64));
                        shiftUpperMode = false;
                    }
                    else
                    {
                        if (code != CodeStop)
                        {
                            lastCharacterWasPrintable = false;
                        }

                        switch (code)
                        {
                            case CodeFnc1:
                                if (result.Length == 0)
                                {
                                    isGs1 = true;
                                }
                                else
                                {
                                    result.Append((char)29);
                                }

                                break;
                            case CodeFnc2:
                            case CodeFnc3:
                                break;
                            case CodeFnc4A:
                                if (!upperMode && shiftUpperMode)
                                {
                                    upperMode = true;
                                    shiftUpperMode = false;
                                }
                                else if (upperMode && shiftUpperMode)
                                {
                                    upperMode = false;
                                    shiftUpperMode = false;
                                }
                                else
                                {
                                    shiftUpperMode = true;
                                }

                                break;
                            case CodeShift:
                                isNextShifted = true;
                                codeSet = CodeCodeB;
                                break;
                            case CodeCodeB:
                                codeSet = CodeCodeB;
                                break;
                            case CodeCodeC:
                                codeSet = CodeCodeC;
                                break;
                            case CodeStop:
                                done = true;
                                break;
                        }
                    }

                    break;

                case CodeCodeB:
                    if (code < 96)
                    {
                        result.Append((char)((shiftUpperMode == upperMode ? ' ' : ' ' + 128) + code));
                        shiftUpperMode = false;
                    }
                    else
                    {
                        if (code != CodeStop)
                        {
                            lastCharacterWasPrintable = false;
                        }

                        switch (code)
                        {
                            case CodeFnc1:
                                if (result.Length == 0)
                                {
                                    isGs1 = true;
                                }
                                else
                                {
                                    result.Append((char)29);
                                }

                                break;
                            case CodeFnc2:
                            case CodeFnc3:
                                break;
                            case CodeFnc4B:
                                if (!upperMode && shiftUpperMode)
                                {
                                    upperMode = true;
                                    shiftUpperMode = false;
                                }
                                else if (upperMode && shiftUpperMode)
                                {
                                    upperMode = false;
                                    shiftUpperMode = false;
                                }
                                else
                                {
                                    shiftUpperMode = true;
                                }

                                break;
                            case CodeShift:
                                isNextShifted = true;
                                codeSet = CodeCodeA;
                                break;
                            case CodeCodeA:
                                codeSet = CodeCodeA;
                                break;
                            case CodeCodeC:
                                codeSet = CodeCodeC;
                                break;
                            case CodeStop:
                                done = true;
                                break;
                        }
                    }

                    break;

                case CodeCodeC:
                    if (code < 100)
                    {
                        if (code < 10)
                        {
                            result.Append('0');
                        }

                        result.Append(code);
                    }
                    else
                    {
                        if (code != CodeStop)
                        {
                            lastCharacterWasPrintable = false;
                        }

                        switch (code)
                        {
                            case CodeFnc1:
                                if (result.Length == 0)
                                {
                                    isGs1 = true;
                                }
                                else
                                {
                                    result.Append((char)29);
                                }

                                break;
                            case CodeCodeA:
                                codeSet = CodeCodeA;
                                break;
                            case CodeCodeB:
                                codeSet = CodeCodeB;
                                break;
                            case CodeStop:
                                done = true;
                                break;
                        }
                    }

                    break;
            }

            if (unshift)
            {
                codeSet = codeSet == CodeCodeA ? CodeCodeB : CodeCodeA;
            }
        }

        var lastPatternSize = 0;
        foreach (var counter in counters)
        {
            lastPatternSize += counter;
        }

        // The stop character has seven elements rather than six, so the loop above stopped one
        // element short: the final element is a two module bar. Check it, then require the
        // usual quiet zone after it.
        var quietStart = row.GetNextUnset(nextStart);
        var module = lastPatternSize / 11.0f;
        var terminationBar = quietStart - nextStart;
        if (terminationBar < module * 1.2f || terminationBar > module * 3.2f)
        {
            return null;
        }

        // The quiet zone has to lie inside the frame: a symbol cut off by the edge of the image
        // may be missing characters, and the check character alone is a one in a hundred guard.
        var quietEnd = quietStart + ((quietStart - lastStart) / 2);
        if (quietEnd > row.Size || !row.IsRange(quietStart, quietEnd, false))
        {
            return null;
        }

        // The penultimate code is the modulo 103 check character, so remove its contribution.
        checksumTotal -= multiplier * lastCode;
        if (checksumTotal % 103 != lastCode)
        {
            return null;
        }

        // The check character was decoded into the result like any other; drop it again.
        if (lastCharacterWasPrintable)
        {
            var checkCharacterLength = codeSet == CodeCodeC ? 2 : 1;
            if (result.Length < checkCharacterLength)
            {
                return null;
            }

            result.Length -= checkCharacterLength;
        }
        else if (lastCode == CodeFnc1 && result.Length > 0 && result[^1] == (char)29)
        {
            // A check character of value 102 went through the FNC1 branch and appended a group
            // separator; the check character is never data, so that has to come out too. The
            // other function codes leave nothing behind once the symbol has ended.
            result.Length--;
        }

        if (result.Length == 0)
        {
            return null;
        }

        var text = result.ToString();
        var payload = new BbDecoderResult(Encoding.Latin1.GetBytes(text), text)
        {
            IsGs1 = isGs1,
        };

        var left = (patternStart + patternEnd) / 2.0f;
        var right = lastStart + (lastPatternSize / 2.0f);
        return new BbSymbolDecodeResult(
            payload,
            BbBarcodeFormat.Code128,
            [new BbScanPoint(left, rowNumber), new BbScanPoint(right, rowNumber)]);
    }

    private static bool TryFindStartPattern(BbBitRow row, out int patternStart, out int patternEnd, out int startCode)
    {
        patternStart = 0;
        patternEnd = 0;
        startCode = -1;

        var width = row.Size;
        var rowOffset = row.GetNextSet(0);

        Span<int> counters = stackalloc int[6];
        var counterPosition = 0;
        var start = rowOffset;
        var isWhite = false;

        for (var i = rowOffset; i < width; i++)
        {
            if (row[i] != isWhite)
            {
                counters[counterPosition]++;
                continue;
            }

            if (counterPosition == 5)
            {
                var bestVariance = BbLinearPatterns.MaxAvgVariance;
                var bestMatch = -1;
                for (var candidate = CodeStartA; candidate <= CodeStartC; candidate++)
                {
                    var variance = BbLinearPatterns.PatternMatchVariance(
                        counters, CodePatterns[candidate], BbLinearPatterns.MaxIndividualVariance);
                    if (variance < bestVariance)
                    {
                        bestVariance = variance;
                        bestMatch = candidate;
                    }
                }

                // Require a quiet zone of at least half the start pattern width before the symbol,
                // and require it to lie inside the frame: a start pattern flush against the edge
                // of the image is as likely to be the middle of a cropped symbol as its start.
                var quietStart = start - ((i - start) / 2);
                if (bestMatch >= 0 && quietStart >= 0 && row.IsRange(quietStart, start, false))
                {
                    patternStart = start;
                    patternEnd = i;
                    startCode = bestMatch;
                    return true;
                }

                start += counters[0] + counters[1];
                counters[2..6].CopyTo(counters[..4]);
                counters[4] = 0;
                counters[5] = 0;
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

    private static bool TryDecodeCode(BbBitRow row, int[] counters, int rowOffset, out int code)
    {
        code = -1;
        if (!BbLinearPatterns.RecordPattern(row, rowOffset, counters))
        {
            return false;
        }

        var bestVariance = BbLinearPatterns.MaxAvgVariance;
        for (var d = 0; d < CodePatterns.Length; d++)
        {
            var pattern = CodePatterns[d];
            if (pattern.Length != counters.Length)
            {
                // The stop character carries a seventh element; it is matched on its first six.
                var variance = BbLinearPatterns.PatternMatchVariance(
                    counters, pattern.AsSpan(0, counters.Length), BbLinearPatterns.MaxIndividualVariance);
                if (variance < bestVariance)
                {
                    bestVariance = variance;
                    code = d;
                }

                continue;
            }

            var v = BbLinearPatterns.PatternMatchVariance(counters, pattern, BbLinearPatterns.MaxIndividualVariance);
            if (v < bestVariance)
            {
                bestVariance = v;
                code = d;
            }
        }

        return code >= 0;
    }
}
