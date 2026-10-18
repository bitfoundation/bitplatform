using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Turns the corrected codewords of a Data Matrix symbol into the payload it encodes.
/// </summary>
/// <remarks>
/// Data Matrix has six compaction modes and a symbol switches between them with latch and
/// unlatch codewords. ASCII is the base mode and the only one that can start a symbol; the
/// others compress specific alphabets, so a mixed payload alternates between them repeatedly.
/// </remarks>
public static class BbDataMatrixBitStreamParser
{
    private const int PadCodeword = 129;
    private const int LatchC40 = 230;
    private const int LatchBase256 = 231;
    private const int Fnc1 = 232;
    private const int StructuredAppend = 233;
    private const int ReaderProgramming = 234;
    private const int UpperShift = 235;
    private const int Macro05 = 236;
    private const int Macro06 = 237;
    private const int LatchX12 = 238;
    private const int LatchText = 239;
    private const int LatchEdifact = 240;
    private const int Eci = 241;

    private static readonly char[] C40BasicSet =
    [
        '*', '*', '*', ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
        'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
    ];

    private static readonly char[] C40Shift2Set =
    [
        '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.',
        '/', ':', ';', '<', '=', '>', '?', '@', '[', '\\', ']', '^', '_',
    ];

    private static readonly char[] TextBasicSet =
    [
        '*', '*', '*', ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
        'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
    ];

    private static readonly char[] TextShift3Set =
    [
        '`', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
        'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        '{', '|', '}', '~', (char)127,
    ];

    private enum Mode
    {
        Ascii,
        C40,
        Text,
        X12,
        Edifact,
        Base256,
        Pad,
        Invalid,
    }

    /// <summary>Decodes the payload.</summary>
    /// <param name="bytes">Corrected data codewords, concatenated across blocks.</param>
    /// <param name="version">The symbol shape, reported back in the result.</param>
    /// <param name="errorsCorrected">Number of codewords the error correction stage repaired.</param>
    /// <returns>The payload, or <see langword="null"/> when the codeword stream is malformed.</returns>
    public static BbDecoderResult? Decode(byte[] bytes, BbDataMatrixVersion version, int errorsCorrected)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(version);

        var bits = new BbBitSource(bytes);
        var result = new StringBuilder(bytes.Length * 2);
        var trailer = new StringBuilder(0);

        var mode = Mode.Ascii;
        var isGs1 = false;
        int? structuredAppendIndex = null;
        int? structuredAppendCount = null;
        int? structuredAppendParity = null;
        int? eci = null;

        try
        {
            do
            {
                if (mode == Mode.Ascii)
                {
                    mode = DecodeAscii(
                        ref bits, result, trailer,
                        ref isGs1, ref structuredAppendIndex, ref structuredAppendCount,
                        ref structuredAppendParity, ref eci);
                    if (mode == Mode.Pad)
                    {
                        break;
                    }

                    if (mode == Mode.Invalid)
                    {
                        return null;
                    }

                    continue;
                }

                var ok = mode switch
                {
                    Mode.C40 => DecodeC40OrText(ref bits, result, text: false),
                    Mode.Text => DecodeC40OrText(ref bits, result, text: true),
                    Mode.X12 => DecodeX12(ref bits, result),
                    Mode.Edifact => DecodeEdifact(ref bits, result),
                    Mode.Base256 => DecodeBase256(ref bits, result),
                    _ => false,
                };

                if (!ok)
                {
                    return null;
                }

                mode = Mode.Ascii;
            }
            while (bits.Available > 0);
        }
        catch (ArgumentOutOfRangeException)
        {
            // A truncated bit stream is a decode failure, not an error condition worth throwing.
            return null;
        }

        if (trailer.Length > 0)
        {
            result.Append(trailer);
        }

        // Every character appended so far stands for one payload byte, so the Latin-1 round trip
        // recovers the raw bytes exactly; the text is then interpreted in the declared or
        // guessed character set.
        var rawBytes = BbCharacterSetEci.Latin1.GetBytes(result.ToString());
        var encoding = eci is int declared ? BbCharacterSetEci.FromEci(declared) : BbCharacterSetEci.GuessEncoding(rawBytes);
        var text = encoding.GetString(rawBytes);
        return new BbDecoderResult(rawBytes, text)
        {
            SymbolVersion = version.ToString(),
            ErrorsCorrected = errorsCorrected,
            Rows = version.SymbolSizeRows,
            Columns = version.SymbolSizeColumns,
            IsGs1 = isGs1,
            Eci = eci,
            StructuredAppendIndex = structuredAppendIndex,
            StructuredAppendCount = structuredAppendCount,
            StructuredAppendParity = structuredAppendParity,
        };
    }

    private static Mode DecodeAscii(
        ref BbBitSource bits,
        StringBuilder result,
        StringBuilder trailer,
        ref bool isGs1,
        ref int? structuredAppendIndex,
        ref int? structuredAppendCount,
        ref int? structuredAppendParity,
        ref int? eci)
    {
        var upperShift = false;
        var codewordsRead = 0;
        var previousWasStructuredAppend = false;

        do
        {
            var value = bits.ReadBits(8);
            codewordsRead++;
            var wasFirst = codewordsRead == 1 && result.Length == 0;
            var afterStructuredAppend = previousWasStructuredAppend;
            previousWasStructuredAppend = false;

            if (value == 0)
            {
                // Codeword 0 is not defined by the specification.
                return Mode.Invalid;
            }

            if (value <= 128)
            {
                // Plain ASCII, stored as the character value plus one.
                if (upperShift)
                {
                    value += 128;
                    upperShift = false;
                }

                result.Append((char)(value - 1));
                return Mode.Ascii;
            }

            if (value == PadCodeword)
            {
                return Mode.Pad;
            }

            if (value <= 229)
            {
                // A single codeword carries two digits.
                var digits = value - 130;
                if (digits < 10)
                {
                    result.Append('0');
                }

                result.Append(digits);
                continue;
            }

            switch (value)
            {
                case LatchC40:
                    return Mode.C40;
                case LatchBase256:
                    return Mode.Base256;
                case LatchX12:
                    return Mode.X12;
                case LatchText:
                    return Mode.Text;
                case LatchEdifact:
                    return Mode.Edifact;
                case Fnc1:
                    // FNC1 in the first position (or second, after a structured append header)
                    // declares a GS1 stream; anywhere else it is a group separator.
                    if (wasFirst || afterStructuredAppend)
                    {
                        isGs1 = true;
                    }
                    else
                    {
                        result.Append((char)29);
                    }

                    break;
                case StructuredAppend:
                    if (bits.Available < 24)
                    {
                        return Mode.Invalid;
                    }

                    {
                        var sequence = bits.ReadBits(8);
                        structuredAppendIndex = (sequence >> 4) + 1;
                        structuredAppendCount = 17 - (sequence & 0x0F);
                        structuredAppendParity = bits.ReadBits(16);
                        previousWasStructuredAppend = true;
                    }

                    break;
                case ReaderProgramming:
                    break;
                case UpperShift:
                    upperShift = true;
                    break;
                case Macro05:
                case Macro06:
                    // The macro codewords stand for a fixed header and trailer around the payload.
                    result.Append(value == Macro05 ? "[)>\u001E05\u001D" : "[)>\u001E06\u001D");
                    trailer.Insert(0, "\u001E\u0004");
                    break;
                case Eci:
                    if (!TryReadEci(ref bits, out var assignment))
                    {
                        return Mode.Invalid;
                    }

                    eci = assignment;
                    break;
                case 254:
                    // The unlatch codeword is only legal as the very last one.
                    if (bits.Available != 0)
                    {
                        return Mode.Invalid;
                    }

                    break;
                default:
                    // 242 to 253 and 255 are reserved; a stream that contains them is corrupt.
                    return Mode.Invalid;
            }
        }
        while (bits.Available > 0);

        return Mode.Ascii;
    }

    /// <summary>
    /// Reads an ECI assignment number, which spans one, two or three codewords depending on its
    /// magnitude (ISO/IEC 16022 5.2.4.11).
    /// </summary>
    private static bool TryReadEci(ref BbBitSource bits, out int eci)
    {
        eci = 0;
        if (bits.Available < 8)
        {
            return false;
        }

        // Codeword value 0 is not defined in any position of an ECI, and the continuation
        // codewords run from 1 to 254; anything else is a corrupted stream.
        var first = bits.ReadBits(8);
        if (first == 0)
        {
            return false;
        }

        if (first <= 127)
        {
            eci = first - 1;
            return true;
        }

        if (first <= 191)
        {
            if (bits.Available < 8)
            {
                return false;
            }

            var continuation = bits.ReadBits(8);
            if (continuation is < 1 or > 254)
            {
                return false;
            }

            eci = ((first - 128) * 254) + continuation - 1 + 127;
            return true;
        }

        if (bits.Available < 16)
        {
            return false;
        }

        var second = bits.ReadBits(8);
        var third = bits.ReadBits(8);
        if (second is < 1 or > 254 || third is < 1 or > 254)
        {
            return false;
        }

        eci = ((first - 192) * 64516) + ((second - 1) * 254) + third - 1 + 16383;
        return true;
    }

    private static bool DecodeC40OrText(ref BbBitSource bits, StringBuilder result, bool text)
    {
        var upperShift = false;
        var shift = 0;
        Span<int> values = stackalloc int[3];

        var basicSet = text ? TextBasicSet : C40BasicSet;

        do
        {
            if (bits.Available == 8)
            {
                return true;
            }

            var firstByte = bits.ReadBits(8);
            if (firstByte == 254)
            {
                return true;
            }

            if (bits.Available < 8)
            {
                return false;
            }

            ParseTwoBytes(firstByte, bits.ReadBits(8), values);

            for (var i = 0; i < 3; i++)
            {
                var value = values[i];
                switch (shift)
                {
                    case 0:
                        if (value < 3)
                        {
                            shift = value + 1;
                        }
                        else if (value < basicSet.Length)
                        {
                            var c = basicSet[value];
                            result.Append(upperShift ? (char)(c + 128) : c);
                            upperShift = false;
                        }
                        else
                        {
                            return false;
                        }

                        break;

                    case 1:
                        result.Append(upperShift ? (char)(value + 128) : (char)value);
                        upperShift = false;
                        shift = 0;
                        break;

                    case 2:
                        if (value < C40Shift2Set.Length)
                        {
                            var c = C40Shift2Set[value];
                            result.Append(upperShift ? (char)(c + 128) : c);
                            upperShift = false;
                        }
                        else if (value == 27)
                        {
                            result.Append((char)29);
                        }
                        else if (value == 30)
                        {
                            upperShift = true;
                        }
                        else
                        {
                            return false;
                        }

                        shift = 0;
                        break;

                    case 3:
                        if (text)
                        {
                            if (value >= TextShift3Set.Length)
                            {
                                return false;
                            }

                            var c = TextShift3Set[value];
                            result.Append(upperShift ? (char)(c + 128) : c);
                        }
                        else
                        {
                            // The C40 shift 3 set holds the thirty two characters from the
                            // grave accent to DEL; the remaining values are undefined.
                            if (value >= 32)
                            {
                                return false;
                            }

                            result.Append(upperShift ? (char)(value + 224) : (char)(value + 96));
                        }

                        upperShift = false;
                        shift = 0;
                        break;

                    default:
                        return false;
                }
            }
        }
        while (bits.Available > 0);

        return true;
    }

    private static bool DecodeX12(ref BbBitSource bits, StringBuilder result)
    {
        Span<int> values = stackalloc int[3];

        do
        {
            if (bits.Available == 8)
            {
                return true;
            }

            var firstByte = bits.ReadBits(8);
            if (firstByte == 254)
            {
                return true;
            }

            if (bits.Available < 8)
            {
                return false;
            }

            ParseTwoBytes(firstByte, bits.ReadBits(8), values);

            for (var i = 0; i < 3; i++)
            {
                var value = values[i];
                switch (value)
                {
                    case 0:
                        result.Append('\r');
                        break;
                    case 1:
                        result.Append('*');
                        break;
                    case 2:
                        result.Append('>');
                        break;
                    case 3:
                        result.Append(' ');
                        break;
                    case < 14:
                        result.Append((char)(value + 44));
                        break;
                    case < 40:
                        result.Append((char)(value + 51));
                        break;
                    default:
                        return false;
                }
            }
        }
        while (bits.Available > 0);

        return true;
    }

    private static bool DecodeEdifact(ref BbBitSource bits, StringBuilder result)
    {
        do
        {
            // Fewer than two bytes left means the remainder is padding, not a value.
            if (bits.Available <= 16)
            {
                return true;
            }

            for (var i = 0; i < 4; i++)
            {
                var value = bits.ReadBits(6);
                if (value == 0x1F)
                {
                    // Unlatch, then discard the rest of the current byte.
                    var bitsLeft = 8 - bits.BitOffset;
                    if (bitsLeft != 8)
                    {
                        bits.ReadBits(bitsLeft);
                    }

                    return true;
                }

                // Six bit values map onto ASCII 32 to 94, with the top bit reconstructed.
                if ((value & 0x20) == 0)
                {
                    value |= 0x40;
                }

                result.Append((char)value);
            }
        }
        while (bits.Available > 0);

        return true;
    }

    private static bool DecodeBase256(ref BbBitSource bits, StringBuilder result)
    {
        // Codeword positions are one based in the de-randomisation formula.
        var position = 1 + bits.ByteOffset;
        var d1 = Unrandomize(bits.ReadBits(8), position++);

        int count;
        if (d1 == 0)
        {
            count = bits.Available / 8;
        }
        else if (d1 < 250)
        {
            count = d1;
        }
        else
        {
            if (bits.Available < 8)
            {
                return false;
            }

            count = (250 * (d1 - 249)) + Unrandomize(bits.ReadBits(8), position++);
        }

        if (count < 0 || count * 8 > bits.Available)
        {
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            result.Append((char)Unrandomize(bits.ReadBits(8), position++));
        }

        return true;
    }

    /// <summary>
    /// Reverses the 255-state randomisation the specification applies to Base 256 data so that
    /// long runs of the same byte do not produce large blocks of identical modules.
    /// </summary>
    private static int Unrandomize(int codeword, int position)
    {
        var pseudoRandom = ((149 * position) % 255) + 1;
        var value = codeword - pseudoRandom;
        return value >= 0 ? value : value + 256;
    }

    private static void ParseTwoBytes(int firstByte, int secondByte, Span<int> values)
    {
        var full = (firstByte << 8) + secondByte - 1;
        var temp = full / 1600;
        values[0] = temp;
        full -= temp * 1600;
        temp = full / 40;
        values[1] = temp;
        values[2] = full - (temp * 40);
    }
}
