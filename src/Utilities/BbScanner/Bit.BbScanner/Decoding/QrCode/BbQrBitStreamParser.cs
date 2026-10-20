using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Turns the corrected codewords of a QR symbol into the payload it encodes.
/// </summary>
/// <remarks>
/// A QR payload is a sequence of segments, each with its own compaction mode, and a symbol may
/// mix them freely: a URL is commonly stored as a byte segment followed by a numeric segment
/// because that is smaller than either alone. The parser therefore loops over segments rather
/// than assuming a single mode.
/// </remarks>
public static class BbQrBitStreamParser
{
    private const string AlphanumericChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    private const int ModeTerminator = 0x00;
    private const int ModeNumeric = 0x01;
    private const int ModeAlphanumeric = 0x02;
    private const int ModeStructuredAppend = 0x03;
    private const int ModeByte = 0x04;
    private const int ModeFnc1FirstPosition = 0x05;
    private const int ModeEci = 0x07;
    private const int ModeKanji = 0x08;
    private const int ModeFnc1SecondPosition = 0x09;
    private const int ModeHanzi = 0x0D;

    /// <summary>Decodes the payload.</summary>
    /// <param name="bytes">Corrected data codewords, concatenated across blocks.</param>
    /// <param name="version">The symbol version, which sets the width of the character count fields.</param>
    /// <param name="level">The error correction level, reported back in the result.</param>
    /// <param name="errorsCorrected">Number of codewords the error correction stage repaired.</param>
    /// <returns>The payload, or <see langword="null"/> when the bit stream is malformed.</returns>
    public static BbDecoderResult? Decode(
        byte[] bytes,
        BbQrVersion version,
        BbQrErrorCorrectionLevel level,
        int errorsCorrected)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(version);

        var bits = new BbBitSource(bytes);
        var text = new StringBuilder(bytes.Length);
        var raw = new List<byte>(bytes.Length);

        var currentEncoding = BbCharacterSetEci.Latin1;
        int? eci = null;
        var isGs1 = false;
        int? structuredAppendIndex = null;
        int? structuredAppendCount = null;
        int? structuredAppendParity = null;
        var sawExplicitEncoding = false;

        while (true)
        {
            if (bits.Available < 4)
            {
                // A stream that simply runs out is a legal, if unterminated, symbol.
                break;
            }

            var mode = bits.ReadBits(4);
            if (mode == ModeTerminator)
            {
                break;
            }

            switch (mode)
            {
                case ModeFnc1FirstPosition:
                case ModeFnc1SecondPosition:
                    isGs1 = true;
                    if (mode == ModeFnc1SecondPosition)
                    {
                        if (bits.Available < 8)
                        {
                            return null;
                        }

                        // The application indicator is not part of the payload.
                        bits.ReadBits(8);
                    }

                    break;

                case ModeStructuredAppend:
                    if (bits.Available < 16)
                    {
                        return null;
                    }

                    var sequence = bits.ReadBits(8);
                    structuredAppendIndex = sequence >> 4;
                    structuredAppendCount = (sequence & 0x0F) + 1;
                    structuredAppendParity = bits.ReadBits(8);
                    break;

                case ModeEci:
                    if (!TryReadEci(ref bits, out var value))
                    {
                        return null;
                    }

                    eci = value;
                    currentEncoding = BbCharacterSetEci.FromEci(value);
                    sawExplicitEncoding = true;
                    break;

                case ModeNumeric:
                {
                    var count = ReadCount(ref bits, version, mode);
                    if (count < 0 || !DecodeNumeric(ref bits, count, text, raw))
                    {
                        return null;
                    }

                    break;
                }

                case ModeAlphanumeric:
                {
                    var count = ReadCount(ref bits, version, mode);
                    if (count < 0 || !DecodeAlphanumeric(ref bits, count, text, raw, isGs1))
                    {
                        return null;
                    }

                    break;
                }

                case ModeByte:
                {
                    var count = ReadCount(ref bits, version, mode);
                    if (count < 0 || !DecodeByte(ref bits, count, text, raw, currentEncoding, sawExplicitEncoding))
                    {
                        return null;
                    }

                    break;
                }

                case ModeKanji:
                case ModeHanzi:
                {
                    if (mode == ModeHanzi)
                    {
                        if (bits.Available < 4)
                        {
                            return null;
                        }

                        // Subset selector; only GB2312 is defined and it carries no payload.
                        bits.ReadBits(4);
                    }

                    var count = ReadCount(ref bits, version, mode);
                    if (count < 0 || !DecodeDoubleByte(ref bits, count, text, raw, mode == ModeKanji))
                    {
                        return null;
                    }

                    break;
                }

                default:
                    return null;
            }
        }

        var rawArray = raw.ToArray();
        return new BbDecoderResult(rawArray, text.ToString())
        {
            ErrorCorrectionLevel = level.ToLetter(),
            SymbolVersion = version.VersionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ErrorsCorrected = errorsCorrected,
            Eci = eci,
            IsGs1 = isGs1,
            StructuredAppendIndex = structuredAppendIndex,
            StructuredAppendCount = structuredAppendCount,
            StructuredAppendParity = structuredAppendParity,
            Rows = version.DimensionForVersion,
            Columns = version.DimensionForVersion,
        };
    }

    /// <summary>Number of bits the character count field uses for a mode at a given version.</summary>
    /// <param name="version">The symbol version.</param>
    /// <param name="mode">The segment mode.</param>
    public static int GetCharacterCountBits(BbQrVersion version, int mode)
    {
        var number = version.VersionNumber;
        var offset = number switch
        {
            <= 9 => 0,
            <= 26 => 1,
            _ => 2,
        };

        return mode switch
        {
            ModeNumeric => offset switch { 0 => 10, 1 => 12, _ => 14 },
            ModeAlphanumeric => offset switch { 0 => 9, 1 => 11, _ => 13 },
            ModeByte => offset switch { 0 => 8, _ => 16 },
            ModeKanji or ModeHanzi => offset switch { 0 => 8, 1 => 10, _ => 12 },
            _ => -1,
        };
    }

    private static int ReadCount(ref BbBitSource bits, BbQrVersion version, int mode)
    {
        var countBits = GetCharacterCountBits(version, mode);
        if (countBits < 0 || bits.Available < countBits)
        {
            return -1;
        }

        return bits.ReadBits(countBits);
    }

    private static bool TryReadEci(ref BbBitSource bits, out int value)
    {
        value = 0;
        if (bits.Available < 8)
        {
            return false;
        }

        var firstByte = bits.ReadBits(8);
        if ((firstByte & 0x80) == 0)
        {
            value = firstByte & 0x7F;
            return true;
        }

        if ((firstByte & 0xC0) == 0x80)
        {
            if (bits.Available < 8)
            {
                return false;
            }

            value = ((firstByte & 0x3F) << 8) | bits.ReadBits(8);
            return true;
        }

        if ((firstByte & 0xE0) == 0xC0)
        {
            if (bits.Available < 16)
            {
                return false;
            }

            value = ((firstByte & 0x1F) << 16) | bits.ReadBits(16);
            return true;
        }

        return false;
    }

    private static bool DecodeNumeric(ref BbBitSource bits, int count, StringBuilder text, List<byte> raw)
    {
        // Digits are packed three to ten bits, with four or seven bit tails.
        while (count >= 3)
        {
            if (bits.Available < 10)
            {
                return false;
            }

            var threeDigits = bits.ReadBits(10);
            if (threeDigits >= 1000)
            {
                return false;
            }

            AppendDigit(text, raw, threeDigits / 100);
            AppendDigit(text, raw, (threeDigits / 10) % 10);
            AppendDigit(text, raw, threeDigits % 10);
            count -= 3;
        }

        if (count == 2)
        {
            if (bits.Available < 7)
            {
                return false;
            }

            var twoDigits = bits.ReadBits(7);
            if (twoDigits >= 100)
            {
                return false;
            }

            AppendDigit(text, raw, twoDigits / 10);
            AppendDigit(text, raw, twoDigits % 10);
        }
        else if (count == 1)
        {
            if (bits.Available < 4)
            {
                return false;
            }

            var digit = bits.ReadBits(4);
            if (digit >= 10)
            {
                return false;
            }

            AppendDigit(text, raw, digit);
        }

        return true;

        static void AppendDigit(StringBuilder text, List<byte> raw, int digit)
        {
            var c = (char)('0' + digit);
            text.Append(c);
            raw.Add((byte)c);
        }
    }

    private static bool DecodeAlphanumeric(ref BbBitSource bits, int count, StringBuilder text, List<byte> raw, bool isGs1)
    {
        var start = text.Length;

        // Characters are packed two to eleven bits, base 45.
        while (count > 1)
        {
            if (bits.Available < 11)
            {
                return false;
            }

            var nextTwo = bits.ReadBits(11);
            var first = nextTwo / 45;
            var second = nextTwo % 45;
            if (first >= AlphanumericChars.Length)
            {
                return false;
            }

            text.Append(AlphanumericChars[first]);
            text.Append(AlphanumericChars[second]);
            count -= 2;
        }

        if (count == 1)
        {
            if (bits.Available < 6)
            {
                return false;
            }

            var index = bits.ReadBits(6);
            if (index >= AlphanumericChars.Length)
            {
                return false;
            }

            text.Append(AlphanumericChars[index]);
        }

        if (isGs1)
        {
            // In a GS1 payload a doubled percent sign stands for a literal one, and a single
            // percent sign is the field separator.
            for (var i = start; i < text.Length; i++)
            {
                if (text[i] != '%')
                {
                    continue;
                }

                if (i + 1 < text.Length && text[i + 1] == '%')
                {
                    text.Remove(i + 1, 1);
                }
                else
                {
                    text[i] = (char)29;
                }
            }
        }

        for (var i = start; i < text.Length; i++)
        {
            raw.Add((byte)text[i]);
        }

        return true;
    }

    private static bool DecodeByte(
        ref BbBitSource bits,
        int count,
        StringBuilder text,
        List<byte> raw,
        Encoding encoding,
        bool sawExplicitEncoding)
    {
        if (count * 8 > bits.Available)
        {
            return false;
        }

        var segment = new byte[count];
        for (var i = 0; i < count; i++)
        {
            segment[i] = (byte)bits.ReadBits(8);
        }

        raw.AddRange(segment);

        // Without an ECI declaration the specification says ISO-8859-1, but a large share of
        // real encoders emit UTF-8 regardless, so the payload is sniffed instead.
        var effective = sawExplicitEncoding ? encoding : BbCharacterSetEci.GuessEncoding(segment);
        text.Append(effective.GetString(segment));
        return true;
    }

    private static bool DecodeDoubleByte(ref BbBitSource bits, int count, StringBuilder text, List<byte> raw, bool kanji)
    {
        if (count * 13 > bits.Available)
        {
            return false;
        }

        // Kanji and Hanzi segments are packed as two byte codes in Shift JIS or GB 2312. Those
        // code pages are not part of the trimmed base class library, so the bytes are always
        // recovered exactly into the raw payload; the text gets the characters when the host
        // application has registered a code page provider, and the replacement character
        // otherwise, so that RawBytes can still be re-decoded.
        var start = raw.Count;
        for (var i = 0; i < count; i++)
        {
            var twoBytes = bits.ReadBits(13);
            var assembled = ((twoBytes / 0x0C0) << 8) | (twoBytes % 0x0C0);
            if (kanji)
            {
                assembled += assembled < 0x01F00 ? 0x08140 : 0x0C140;
            }
            else
            {
                assembled += assembled < 0x00A00 ? 0x0A1A1 : 0x0A6A1;
            }

            raw.Add((byte)(assembled >> 8));
            raw.Add((byte)(assembled & 0xFF));
        }

        var encoding = kanji ? BbCharacterSetEci.ShiftJis : BbCharacterSetEci.Gb2312;
        if (encoding is null)
        {
            text.Append('�', count);
        }
        else
        {
            text.Append(encoding.GetString(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(raw)[start..]));
        }

        return true;
    }
}
