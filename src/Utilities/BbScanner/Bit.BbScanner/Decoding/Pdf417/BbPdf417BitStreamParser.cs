using System.Globalization;
using System.Numerics;
using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Turns the corrected codewords of a PDF417 symbol into the payload it encodes.
/// </summary>
/// <remarks>
/// <para>
/// PDF417 has three compaction modes and the payload latches between them. Text compaction packs
/// two characters into every codeword through four sub-alphabets, byte compaction packs six bytes
/// into every five codewords, and numeric compaction treats runs of digits as one large base 900
/// number.
/// </para>
/// <para>
/// The payload is assembled as bytes and interpreted as text afterwards, per ECI segment, the
/// way the QR and Data Matrix parsers do: text and numeric compaction produce ASCII, so only
/// byte compaction is affected by the character set, but a symbol that declares UTF-8 and then
/// carries accented characters as bytes has to come out as the characters it meant.
/// </para>
/// </remarks>
public static class BbPdf417BitStreamParser
{
    private const int TextCompaction = 900;
    private const int ByteCompaction = 901;
    private const int NumericCompaction = 902;
    private const int ShiftToByte = 913;
    private const int MacroTerminator = 922;
    private const int MacroOptionalField = 923;
    private const int ByteCompactionSixes = 924;
    private const int EciUserDefined = 925;
    private const int EciGeneralPurpose = 926;
    private const int EciCharset = 927;
    private const int MacroControlBlock = 928;

    private static readonly char[] PunctuationChars =
    [
        ';', '<', '>', '@', '[', '\\', ']', '_', '`', '~', '!', '\r', '\t', ',', ':',
        '\n', '-', '.', '$', '/', '"', '|', '*', '(', ')', '?', '{', '}', '\'',
    ];

    private static readonly char[] MixedChars =
    [
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '&', '\r', '\t', ',', ':',
        '#', '-', '.', '$', '/', '+', '%', '*', '=', '^',
    ];

    private enum TextMode
    {
        Alpha,
        Lower,
        Mixed,
        Punctuation,
        AlphaShift,
        PunctuationShift,
    }

    /// <summary>
    /// The payload under construction: the raw bytes, plus the text decoded so far, which is
    /// built one ECI segment at a time because a change of character set applies only to the
    /// bytes that follow it.
    /// </summary>
    private sealed class Output
    {
        private readonly StringBuilder _text = new();
        private readonly List<byte> _segment = [];
        private int? _segmentEci;

        public List<byte> Raw { get; } = [];

        /// <summary>The last ECI declared by the symbol, as reported in the metadata.</summary>
        public int? Eci { get; private set; }

        public void Append(byte value)
        {
            Raw.Add(value);
            _segment.Add(value);
        }

        public void Append(char value) => Append((byte)value);

        public void SetEci(int eci)
        {
            Flush();
            Eci = eci;

            // Only the character set range of ECI carries an interpretation for bytes; the user
            // defined and general purpose ranges are reported but do not change the text.
            _segmentEci = eci <= 899 ? eci : null;
        }

        public string Text
        {
            get
            {
                Flush();
                return _text.ToString();
            }
        }

        private void Flush()
        {
            if (_segment.Count == 0)
            {
                return;
            }

            var bytes = _segment.ToArray();
            var encoding = _segmentEci is int eci
                ? BbCharacterSetEci.FromEci(eci)
                : BbCharacterSetEci.GuessEncoding(bytes);
            _text.Append(encoding.GetString(bytes));
            _segment.Clear();
        }
    }

    /// <summary>Decodes the payload.</summary>
    /// <param name="codewords">Corrected codewords; index 0 is the symbol length descriptor.</param>
    /// <param name="rows">Number of symbol rows, reported back in the result.</param>
    /// <param name="columns">Number of data columns, reported back in the result.</param>
    /// <param name="errorCorrectionLevel">Security level, reported back in the result.</param>
    /// <param name="errorsCorrected">Number of codewords the error correction stage repaired.</param>
    /// <returns>The payload, or <see langword="null"/> when the codeword stream is malformed.</returns>
    public static BbDecoderResult? Decode(
        int[] codewords, int rows, int columns, int errorCorrectionLevel, int errorsCorrected)
    {
        ArgumentNullException.ThrowIfNull(codewords);

        if (codewords.Length == 0)
        {
            return null;
        }

        var length = codewords[0];
        if (length < 1 || length > codewords.Length)
        {
            return null;
        }

        var output = new Output();

        int? segmentIndex = null;
        int? segmentCount = null;

        var index = 1;
        while (index < length)
        {
            var codeword = codewords[index++];

            switch (codeword)
            {
                case TextCompaction:
                    index = DecodeText(codewords, index, length, output);
                    break;

                case ByteCompaction:
                case ByteCompactionSixes:
                    index = DecodeBytes(codewords, index, length, output, codeword == ByteCompactionSixes);
                    break;

                case NumericCompaction:
                    index = DecodeNumeric(codewords, index, length, output);
                    break;

                case ShiftToByte:
                    if (index >= length)
                    {
                        return null;
                    }

                    output.Append((byte)(codewords[index++] & 0xFF));
                    break;

                case EciCharset:
                case EciUserDefined:
                case EciGeneralPurpose:
                    index = DecodeEci(codewords, index - 1, length, output);
                    break;

                case MacroControlBlock:
                    index = DecodeMacro(codewords, index, length, ref segmentIndex, ref segmentCount);
                    break;

                case MacroOptionalField:
                case MacroTerminator:
                    // A stray optional field or terminator outside a control block carries
                    // metadata that is not part of the payload.
                    index = length;
                    break;

                default:
                    if (codeword >= TextCompaction)
                    {
                        // An unknown high codeword means the stream is not something this parser
                        // understands; stopping is safer than emitting rubbish.
                        return null;
                    }

                    // A stream that starts without an explicit latch is in text compaction.
                    index = DecodeText(codewords, index - 1, length, output);
                    break;
            }

            if (index < 0)
            {
                return null;
            }
        }

        return new BbDecoderResult([.. output.Raw], output.Text)
        {
            ErrorCorrectionLevel = errorCorrectionLevel.ToString(CultureInfo.InvariantCulture),
            SymbolVersion = $"{rows}x{columns}",
            ErrorsCorrected = errorsCorrected,
            Rows = rows,
            Columns = columns,
            Eci = output.Eci,
            StructuredAppendIndex = segmentIndex,
            StructuredAppendCount = segmentCount,
        };
    }

    /// <summary>
    /// Reads an ECI designator and the one or two codewords that follow it. The three
    /// designators cover disjoint ranges of the assignment number space.
    /// </summary>
    /// <returns>The index after the ECI, or -1 when the stream ends inside it.</returns>
    private static int DecodeEci(int[] codewords, int index, int length, Output output)
    {
        var designator = codewords[index++];
        switch (designator)
        {
            case EciCharset:
                if (index >= length || codewords[index] >= TextCompaction)
                {
                    return -1;
                }

                output.SetEci(codewords[index++]);
                return index;

            case EciGeneralPurpose:
                if (index + 1 >= length || codewords[index] >= TextCompaction || codewords[index + 1] >= TextCompaction)
                {
                    return -1;
                }

                output.SetEci(900 + (codewords[index] * 900) + codewords[index + 1]);
                return index + 2;

            default:
                if (index >= length || codewords[index] >= TextCompaction)
                {
                    return -1;
                }

                output.SetEci(810900 + codewords[index++]);
                return index;
        }
    }

    /// <summary>
    /// Decodes a run of text compaction codewords.
    /// </summary>
    /// <remarks>
    /// A byte shift, a text latch and an ECI designator may all appear inside a text run without
    /// ending it: the shift emits one byte and leaves the sub-mode as it was, the latch resets
    /// the sub-mode to Alpha, and the ECI changes only the character set. Any other mode codeword
    /// ends the run.
    /// </remarks>
    private static int DecodeText(int[] codewords, int index, int length, Output output)
    {
        var mode = TextMode.Alpha;
        var priorToShift = TextMode.Alpha;

        Span<int> values = stackalloc int[2];

        while (index < length)
        {
            var codeword = codewords[index];
            if (codeword >= TextCompaction)
            {
                if (codeword == ShiftToByte)
                {
                    if (index + 1 >= length)
                    {
                        return -1;
                    }

                    output.Append((byte)(codewords[index + 1] & 0xFF));
                    index += 2;
                    continue;
                }

                if (codeword == TextCompaction)
                {
                    mode = TextMode.Alpha;
                    priorToShift = TextMode.Alpha;
                    index++;
                    continue;
                }

                if (codeword is EciCharset or EciGeneralPurpose or EciUserDefined)
                {
                    index = DecodeEci(codewords, index, length, output);
                    if (index < 0)
                    {
                        return -1;
                    }

                    continue;
                }

                break;
            }

            index++;
            values[0] = codeword / 30;
            values[1] = codeword % 30;

            foreach (var value in values)
            {
                switch (mode)
                {
                    case TextMode.Alpha:
                    case TextMode.AlphaShift:
                        if (value < 26)
                        {
                            output.Append((char)('A' + value));
                        }
                        else if (value == 26)
                        {
                            output.Append(' ');
                        }
                        else if (value == 27)
                        {
                            mode = mode == TextMode.AlphaShift ? priorToShift : TextMode.Lower;
                            continue;
                        }
                        else if (value == 28)
                        {
                            mode = TextMode.Mixed;
                            continue;
                        }
                        else if (value == 29)
                        {
                            priorToShift = mode == TextMode.AlphaShift ? priorToShift : mode;
                            mode = TextMode.PunctuationShift;
                            continue;
                        }

                        if (mode == TextMode.AlphaShift)
                        {
                            mode = priorToShift;
                        }

                        break;

                    case TextMode.Lower:
                        if (value < 26)
                        {
                            output.Append((char)('a' + value));
                        }
                        else if (value == 26)
                        {
                            output.Append(' ');
                        }
                        else if (value == 27)
                        {
                            priorToShift = mode;
                            mode = TextMode.AlphaShift;
                        }
                        else if (value == 28)
                        {
                            mode = TextMode.Mixed;
                        }
                        else if (value == 29)
                        {
                            priorToShift = mode;
                            mode = TextMode.PunctuationShift;
                        }

                        break;

                    case TextMode.Mixed:
                        if (value < MixedChars.Length)
                        {
                            output.Append(MixedChars[value]);
                        }
                        else if (value == 25)
                        {
                            mode = TextMode.Punctuation;
                        }
                        else if (value == 26)
                        {
                            output.Append(' ');
                        }
                        else if (value == 27)
                        {
                            mode = TextMode.Lower;
                        }
                        else if (value == 28)
                        {
                            mode = TextMode.Alpha;
                        }
                        else if (value == 29)
                        {
                            priorToShift = mode;
                            mode = TextMode.PunctuationShift;
                        }

                        break;

                    case TextMode.Punctuation:
                        if (value < PunctuationChars.Length)
                        {
                            output.Append(PunctuationChars[value]);
                        }
                        else if (value == 29)
                        {
                            mode = TextMode.Alpha;
                        }

                        break;

                    case TextMode.PunctuationShift:
                        if (value < PunctuationChars.Length)
                        {
                            output.Append(PunctuationChars[value]);
                        }
                        else if (value == 29)
                        {
                            mode = TextMode.Alpha;
                            continue;
                        }

                        mode = priorToShift;
                        break;
                }
            }
        }

        return index;
    }

    private static int DecodeBytes(int[] codewords, int index, int length, Output output, bool sixes)
    {
        var group = new List<int>(5);

        while (index < length)
        {
            var codeword = codewords[index];
            if (codeword >= TextCompaction)
            {
                break;
            }

            index++;
            group.Add(codeword);

            if (group.Count != 5)
            {
                continue;
            }

            // Five codewords carry six bytes as one base 900 number.
            long value = 0;
            foreach (var item in group)
            {
                value = (value * 900) + item;
            }

            for (var i = 5; i >= 0; i--)
            {
                output.Append((byte)((value >> (8 * i)) & 0xFF));
            }

            group.Clear();
        }

        // A trailing partial group carries one byte per codeword. Mode 924 promises a multiple of
        // six bytes and therefore should never leave one behind.
        if (group.Count > 0)
        {
            if (sixes)
            {
                return -1;
            }

            foreach (var item in group)
            {
                output.Append((byte)(item & 0xFF));
            }
        }

        return index;
    }

    private static int DecodeNumeric(int[] codewords, int index, int length, Output output)
    {
        var group = new List<int>(15);

        while (index < length)
        {
            var codeword = codewords[index];
            if (codeword >= TextCompaction)
            {
                break;
            }

            index++;
            group.Add(codeword);

            if (group.Count == 15)
            {
                if (!EmitNumericGroup(group, output))
                {
                    return -1;
                }

                group.Clear();
            }
        }

        if (group.Count > 0 && !EmitNumericGroup(group, output))
        {
            return -1;
        }

        return index;
    }

    /// <summary>
    /// Converts a group of base 900 codewords into digits.
    /// </summary>
    /// <remarks>
    /// The encoder prefixes the digits with a 1 so that leading zeros survive the conversion to a
    /// number, so that digit is dropped again here.
    /// </remarks>
    private static bool EmitNumericGroup(List<int> group, Output output)
    {
        BigInteger value = 0;
        foreach (var codeword in group)
        {
            value = (value * 900) + codeword;
        }

        var digits = value.ToString(CultureInfo.InvariantCulture);
        if (digits.Length < 2 || digits[0] != '1')
        {
            return false;
        }

        foreach (var digit in digits.AsSpan(1))
        {
            output.Append(digit);
        }

        return true;
    }

    /// <summary>
    /// Reads a Macro PDF417 control block: the segment index, the file identifier, and the
    /// optional fields, of which only the segment count is reported.
    /// </summary>
    /// <remarks>
    /// The block sits at the end of the data: a two codeword base 900 segment index, the file
    /// identifier as text compaction codewords, then optional fields each introduced by 923 and
    /// a field designator, and finally 922 on the last segment of the file. The segment index
    /// and count are numeric compaction values, so their digits carry the leading 1 that keeps
    /// zeros intact.
    /// </remarks>
    private static int DecodeMacro(int[] codewords, int index, int length, ref int? segmentIndex, ref int? segmentCount)
    {
        if (index + 2 > length)
        {
            return length;
        }

        if (!TryReadMacroNumber(codewords.AsSpan(index, 2), out var parsedIndex))
        {
            return -1;
        }

        segmentIndex = parsedIndex;
        index += 2;

        // The file identifier: text compaction codewords up to the first optional field or the
        // terminator. It names the file, not the payload, so it is skipped.
        while (index < length && codewords[index] < TextCompaction)
        {
            index++;
        }

        while (index < length && codewords[index] == MacroOptionalField)
        {
            index++;
            if (index >= length)
            {
                return length;
            }

            var designator = codewords[index++];
            var fieldStart = index;
            while (index < length && codewords[index] < TextCompaction)
            {
                index++;
            }

            // Field 1 is the segment count; the others (file name, time stamp, sender,
            // addressee, file size, checksum) are metadata this library does not surface.
            if (designator == 1 && index - fieldStart >= 2 &&
                TryReadMacroNumber(codewords.AsSpan(fieldStart, 2), out var parsedCount))
            {
                segmentCount = parsedCount;
            }
        }

        if (index < length && codewords[index] == MacroTerminator)
        {
            index++;
        }

        return index;
    }

    /// <summary>Reads a two codeword numeric compaction value: base 900 digits with a leading 1.</summary>
    private static bool TryReadMacroNumber(ReadOnlySpan<int> codewords, out int value)
    {
        value = 0;
        if (codewords.Length != 2 || codewords[0] >= TextCompaction || codewords[1] >= TextCompaction)
        {
            return false;
        }

        var number = (codewords[0] * 900) + codewords[1];
        var digits = number.ToString(CultureInfo.InvariantCulture);
        return digits.Length > 1 && digits[0] == '1' && int.TryParse(digits.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

}
