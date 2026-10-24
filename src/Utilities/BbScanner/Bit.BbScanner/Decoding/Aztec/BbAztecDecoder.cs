using System.Globalization;
using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Reads the data out of an Aztec Code module grid: the mode message around the bullseye, the codewords spiralling
/// out from it, their Reed-Solomon correction and the character modes they encode (ISO/IEC 24778).
/// </summary>
/// <remarks>
/// <para>
/// The grid is upright - the corner of the mode message ring with three dark orientation modules is at the top left -
/// and covers the whole symbol, reference grid included. Coordinates below are measured from the centre module,
/// right and down.
/// </para>
/// <para>
/// A compact symbol has a bullseye of two dark rings and 1 to 4 data layers; a full range one has three dark rings,
/// 1 to 32 layers and a reference grid - lines of alternating modules through the centre and every sixteen modules
/// out from it - that the data flows around. The mode message gives the layer count and how many codewords carry
/// data; the rest of the codewords are error correction, over a field whose size depends on the layer count.
/// </para>
/// </remarks>
internal static class BbAztecDecoder
{
    private static readonly BbReedSolomonDecoder ModeMessageCorrection = new(new BbGenericGF(0x13, 16, 1));
    private static readonly BbReedSolomonDecoder SixBitCorrection = new(new BbGenericGF(0x43, 64, 1));
    private static readonly BbReedSolomonDecoder TenBitCorrection = new(new BbGenericGF(0x409, 1024, 1));
    private static readonly BbReedSolomonDecoder TwelveBitCorrection = new(new BbGenericGF(0x1069, 4096, 1));

    /// <summary>
    /// The orientation modules of the mode message ring, three to a corner, read clockwise from the top left corner:
    /// for each corner the module before it on the ring, the corner itself and the module after it.
    /// </summary>
    internal const int UprightOrientation = 0b111_011_100_000;

    /// <summary>
    /// The fewest error correction codewords a symbol may carry. Every encoder writes several - the specification
    /// recommends a quarter of the symbol plus three - and a symbol with none would accept whatever a misread grid
    /// produced.
    /// </summary>
    private const int MinErrorCorrectionCodewords = 2;

    /// <summary>
    /// The largest chance, as a power of two, that a block damaged beyond repair is "corrected" into a wrong one that
    /// is accepted: a correction is only trusted when that chance is smaller.
    /// </summary>
    private const int MaxMiscorrectionLog2 = -14;

    /// <summary>The number of modules across a symbol, reference grid included.</summary>
    /// <param name="compact">Whether the symbol is a compact one.</param>
    /// <param name="layers">The number of data layers.</param>
    internal static int Size(bool compact, int layers)
    {
        if (compact)
        {
            return 11 + (4 * layers);
        }

        // The data layers, the bullseye and the mode message come to 14 + 4 * layers modules; the reference grid adds
        // the centre line and a line every sixteen modules out from it on each side.
        var withoutGrid = 14 + (4 * layers);
        return withoutGrid + 1 + (2 * (((withoutGrid / 2) - 1) / 15));
    }

    /// <summary>How far from the centre module the mode message ring lies.</summary>
    /// <param name="compact">Whether the symbol is a compact one.</param>
    internal static int ModeRingDistance(bool compact) => compact ? 5 : 7;

    /// <summary>
    /// Reads the mode message ring: the side values, each sample a bit with the first the most significant, going
    /// clockwise from the top left corner, and from them the orientation modules.
    /// </summary>
    /// <param name="module">Whether the module at a centre relative coordinate is dark.</param>
    /// <param name="compact">Whether the symbol is a compact one.</param>
    /// <param name="sides">Receives the four side values.</param>
    /// <returns>The twelve orientation bits, in the order of <see cref="UprightOrientation"/>.</returns>
    internal static int ReadModeRing(Func<int, int, bool> module, bool compact, Span<int> sides)
    {
        var d = ModeRingDistance(compact);
        var length = 2 * d;
        for (var side = 0; side < 4; side++)
        {
            var value = 0;
            for (var k = 0; k < length; k++)
            {
                var (u, v) = side switch
                {
                    0 => (-d + k, -d),
                    1 => (d, -d + k),
                    2 => (d - k, d),
                    _ => (-d, d - k),
                };

                value = (value << 1) | (module(u, v) ? 1 : 0);
            }

            sides[side] = value;
        }

        // Each side starts at its corner, so a corner's three modules are the last sample of the side before it, then
        // the first two of its own.
        var orientation = 0;
        for (var corner = 0; corner < 4; corner++)
        {
            var before = sides[(corner + 3) % 4] & 1;
            var own = sides[corner] >> (length - 2);
            orientation = (orientation << 3) | (before << 2) | own;
        }

        return orientation;
    }

    /// <summary>
    /// Finds how far a symbol read as <paramref name="orientation"/> is turned: the number of quarter turns
    /// clockwise from upright, or -1 when the orientation modules match no turn closely enough.
    /// </summary>
    /// <param name="orientation">Orientation bits from <see cref="ReadModeRing"/>.</param>
    internal static int FindRotation(int orientation)
    {
        // Reading the ring from a different corner rotates the twelve bits by three places at a time. The four
        // rotations of the upright pattern are eight bits apart from one another, so two misread modules are safe.
        for (var turns = 0; turns < 4; turns++)
        {
            var shift = 3 * turns;
            var rotated = ((UprightOrientation >> shift) | (UprightOrientation << (12 - shift))) & 0xFFF;
            if (int.PopCount(orientation ^ rotated) <= 2)
            {
                return turns;
            }
        }

        return -1;
    }

    /// <summary>
    /// Corrects and reads the mode message: the number of layers and of data codewords.
    /// </summary>
    /// <param name="sides">The four side values of an upright symbol, from <see cref="ReadModeRing"/>.</param>
    /// <param name="compact">Whether the symbol is a compact one.</param>
    /// <param name="layers">Receives the number of data layers.</param>
    /// <param name="dataCodewords">Receives the number of data codewords.</param>
    /// <param name="errorsCorrected">Receives the number of mode message words repaired.</param>
    internal static bool TryReadModeMessage(ReadOnlySpan<int> sides, bool compact, out int layers, out int dataCodewords, out int errorsCorrected)
    {
        layers = 0;
        dataCodewords = 0;

        // Seven bits a side on a compact symbol, the two orientation modules either end excluded; ten on a full range
        // one, which also skips the reference grid module in the middle of each side.
        long bits = 0;
        foreach (var side in sides)
        {
            bits = compact
                ? (bits << 7) | (uint)((side >> 1) & 0x7F)
                : (bits << 10) | (uint)(((side >> 2) & (0x1F << 5)) | ((side >> 1) & 0x1F));
        }

        var words = compact ? 7 : 10;
        var data = compact ? 2 : 4;
        var received = new int[words];
        for (var i = words - 1; i >= 0; i--)
        {
            received[i] = (int)(bits & 0xF);
            bits >>= 4;
        }

        if (!ModeMessageCorrection.TryDecode(received, words - data, out errorsCorrected))
        {
            return false;
        }

        var value = 0;
        for (var i = 0; i < data; i++)
        {
            value = (value << 4) | received[i];
        }

        if (compact)
        {
            layers = (value >> 6) + 1;
            dataCodewords = (value & 0x3F) + 1;
        }
        else
        {
            layers = (value >> 11) + 1;
            dataCodewords = (value & 0x7FF) + 1;
        }

        return true;
    }

    /// <summary>Decodes the data of an upright module grid.</summary>
    /// <param name="grid">The whole symbol, upright.</param>
    /// <param name="compact">Whether the symbol is a compact one.</param>
    /// <param name="layers">The number of data layers, from the mode message.</param>
    /// <param name="dataCodewords">The number of data codewords, from the mode message.</param>
    /// <param name="modeErrors">Mode message words already repaired, added to the count reported.</param>
    /// <returns>The payload, or <see langword="null"/> when the codewords cannot be corrected or do not parse.</returns>
    internal static BbDecoderResult? Decode(BbBitMatrix grid, bool compact, int layers, int dataCodewords, int modeErrors = 0)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (layers < 1 || layers > (compact ? 4 : 32) || grid.Width != Size(compact, layers) || grid.Height != grid.Width)
        {
            return null;
        }

        var raw = ExtractBits(grid, compact, layers);
        var (wordSize, correction) = layers switch
        {
            <= 2 => (6, SixBitCorrection),
            <= 8 => (8, BbReedSolomonDecoder.DataMatrix),
            <= 22 => (10, TenBitCorrection),
            _ => (12, TwelveBitCorrection),
        };

        var codewords = raw.Length / wordSize;
        if (dataCodewords > codewords - MinErrorCorrectionCodewords)
        {
            return null;
        }

        // Codewords end flush with the outside of the symbol; whatever does not fill a whole one is at the start.
        var words = new int[codewords];
        var offset = raw.Length % wordSize;
        for (var i = 0; i < codewords; i++, offset += wordSize)
        {
            var word = 0;
            for (var b = 0; b < wordSize; b++)
            {
                word = (word << 1) | (raw[offset + b] ? 1 : 0);
            }

            words[i] = word;
        }

        var checkCodewords = codewords - dataCodewords;
        if (!correction.TryDecode(words, checkCodewords, out var errorsCorrected) ||
            !IsTrustworthy(codewords, checkCodewords, wordSize, errorsCorrected))
        {
            return null;
        }

        // A mode message that had to be repaired may have been repaired into the wrong data codeword count. An
        // overstated one still decodes - the real check codewords then satisfy a subset of their own roots - and they
        // are read as characters, so the next root is asked for too: a real block has it only by chance.
        if (modeErrors > 0 && correction.HasNextRoot(words, checkCodewords))
        {
            return null;
        }

        if (Unstuff(words.AsSpan(0, dataCodewords), wordSize) is not { } bits)
        {
            return null;
        }

        var output = ReadCharacters(bits);
        if (output is null)
        {
            return null;
        }

        return new BbDecoderResult(output.Raw.ToArray(), output.Text)
        {
            SymbolVersion = string.Create(
                CultureInfo.InvariantCulture,
                $"{(compact ? "Compact" : "Full range")}, {layers} layer{(layers == 1 ? "" : "s")}"),
            ErrorCorrectionLevel = string.Create(
                CultureInfo.InvariantCulture,
                $"{100 * (codewords - dataCodewords) / codewords}%"),
            ErrorsCorrected = errorsCorrected + modeErrors,
            Eci = output.Eci,
            IsGs1 = output.IsGs1,
        };
    }

    /// <summary>
    /// Whether a block corrected by <paramref name="errors"/> codewords can be believed. Reed-Solomon corrects as many
    /// errors as half its check codewords, but a block damaged beyond that lands within reach of some other codeword
    /// with a chance of the number of words within that many errors of one over the number of possible check values:
    /// <c>sum C(n, i) (q - 1)^i / q^ec</c>. With the big fields and the error correction an encoder writes by default
    /// that is negligible, but a small symbol in six bit codewords with a minimal level has hardly any to spare - two
    /// check codewords correcting one error accept a wrong block more often than not - so corrections are only
    /// trusted, and an uncorrected block only accepted, while the chance stays below 2^<see cref="MaxMiscorrectionLog2"/>.
    /// </summary>
    private static bool IsTrustworthy(int codewords, int checkCodewords, int wordSize, int errors)
    {
        var q = Math.Pow(2, wordSize);
        var within = 0.0;
        for (var i = 0; i <= errors; i++)
        {
            within += Math.Exp(LogCombinations(codewords, i) + (i * Math.Log(q - 1)));
        }

        return (Math.Log2(within) - (checkCodewords * wordSize)) < MaxMiscorrectionLog2;

        static double LogCombinations(int n, int k)
        {
            var log = 0.0;
            for (var i = 0; i < k; i++)
            {
                log += Math.Log(n - i) - Math.Log(i + 1);
            }

            return log;
        }
    }

    /// <summary>
    /// Reads the data layers in order, outermost first: each layer is two modules deep and read a side at a time,
    /// down the left, along the bottom, up the right and back along the top, two modules across at each step.
    /// </summary>
    private static bool[] ExtractBits(BbBitMatrix grid, bool compact, int layers)
    {
        // Positions are worked out on the symbol as it would be without its reference grid, then mapped past the
        // grid lines.
        var withoutGrid = (compact ? 11 : 14) + (4 * layers);
        var map = new int[withoutGrid];
        if (compact)
        {
            for (var i = 0; i < map.Length; i++)
            {
                map[i] = i;
            }
        }
        else
        {
            var half = withoutGrid / 2;
            var centre = grid.Width / 2;
            for (var i = 0; i < half; i++)
            {
                // One grid line every fifteen data modules out from the centre line.
                var outward = i + (i / 15);
                map[half - i - 1] = centre - outward - 1;
                map[half + i] = centre + outward + 1;
            }
        }

        var bits = new bool[((compact ? 88 : 112) + (16 * layers)) * layers];
        var at = 0;
        for (var layer = 0; layer < layers; layer++)
        {
            var length = ((layers - layer) * 4) + (compact ? 9 : 12);
            var low = layer * 2;
            var high = withoutGrid - 1 - low;
            for (var j = 0; j < length; j++)
            {
                for (var k = 0; k < 2; k++)
                {
                    var step = (j * 2) + k;
                    bits[at + step] = grid[map[low + k], map[low + j]];
                    bits[at + (2 * length) + step] = grid[map[low + j], map[high - k]];
                    bits[at + (4 * length) + step] = grid[map[high - k], map[high - j]];
                    bits[at + (6 * length) + step] = grid[map[high - j], map[low + k]];
                }
            }

            at += length * 8;
        }

        return bits;
    }

    /// <summary>
    /// Undoes bit stuffing. An encoder may not write a codeword of all zeros or all ones, so one whose leading bits
    /// are all the same carries a final bit that is not data; a codeword that is all zeros or all ones is invalid.
    /// </summary>
    private static bool[]? Unstuff(ReadOnlySpan<int> words, int wordSize)
    {
        var all = (1 << wordSize) - 1;
        var count = 0;
        foreach (var word in words)
        {
            if (word == 0 || word == all)
            {
                return null;
            }

            count += word == 1 || word == all - 1 ? wordSize - 1 : wordSize;
        }

        var bits = new bool[count];
        var at = 0;
        foreach (var word in words)
        {
            if (word == 1 || word == all - 1)
            {
                bits.AsSpan(at, wordSize - 1).Fill(word > 1);
                at += wordSize - 1;
                continue;
            }

            for (var b = wordSize - 1; b >= 0; b--)
            {
                bits[at++] = ((word >> b) & 1) != 0;
            }
        }

        return bits;
    }

    private enum Mode
    {
        Upper,
        Lower,
        Mixed,
        Punct,
        Digit,
    }

    private enum Control
    {
        None,
        PunctShift,
        UpperShift,
        LatchLower,
        LatchMixed,
        LatchDigit,
        LatchUpper,
        LatchPunct,
        BinaryShift,
        Flag,
    }

    /// <summary>
    /// Reads the character modes. Upper, lower, mixed and punctuation characters are five bits and digits four; a
    /// shift changes the mode for one character and a latch until the next latch, binary shift reads a run of bytes,
    /// and the punctuation mode's first code is a flag: FNC1, or an ECI.
    /// </summary>
    private static Output? ReadCharacters(bool[] bits)
    {
        var output = new Output();
        var latched = Mode.Upper;
        var current = Mode.Upper;
        var at = 0;
        var end = bits.Length;
        var sawData = false;

        while (at < end)
        {
            // What is left may be the ones that pad out the last codeword - as many as eleven of them in a twelve bit
            // codeword - which in upper, lower or mixed mode would read as a binary shift of a length the symbol cannot
            // hold. A run of ones carries no character in any mode, so it is padding.
            if (end - at < 12 && bits.AsSpan(at, end - at).IndexOf(false) < 0)
            {
                break;
            }

            var size = current == Mode.Digit ? 4 : 5;
            if (end - at < size)
            {
                break;
            }

            var code = Read(bits, ref at, size);
            var control = Character(current, code, out var literal);
            switch (control)
            {
                case Control.None:
                    output.Append(literal!);
                    sawData = true;
                    current = latched;
                    break;

                case Control.BinaryShift:
                {
                    // The run ends in the mode it was invoked from, even when that was itself a shift: a byte run
                    // after an upper shift out of digits goes back to upper, not to digits.
                    latched = current;
                    if (end - at < 5)
                    {
                        at = end;
                        break;
                    }

                    var length = Read(bits, ref at, 5);
                    if (length == 0)
                    {
                        if (end - at < 11)
                        {
                            at = end;
                            break;
                        }

                        length = Read(bits, ref at, 11) + 31;
                    }

                    for (var i = 0; i < length; i++)
                    {
                        if (end - at < 8)
                        {
                            // A run that claims more bytes than the symbol holds was not written by an encoder.
                            return null;
                        }

                        output.Append((byte)Read(bits, ref at, 8));
                    }

                    sawData = true;
                    current = latched;
                    break;
                }

                case Control.Flag:
                {
                    if (end - at < 3)
                    {
                        at = end;
                        break;
                    }

                    var n = Read(bits, ref at, 3);
                    if (n == 0)
                    {
                        // FNC1: in first position it marks GS1 data, anywhere after that it separates elements.
                        if (sawData)
                        {
                            output.Append((byte)0x1D);
                        }
                        else
                        {
                            output.IsGs1 = true;
                        }
                    }
                    else if (n == 7)
                    {
                        // Reserved.
                        return null;
                    }
                    else
                    {
                        // An ECI of n digits, each a four bit digit mode code.
                        if (end - at < 4 * n)
                        {
                            return null;
                        }

                        var eci = 0;
                        for (var i = 0; i < n; i++)
                        {
                            var digit = Read(bits, ref at, 4);
                            if (digit is < 2 or > 11)
                            {
                                return null;
                            }

                            eci = (eci * 10) + (digit - 2);
                        }

                        output.SetEci(eci);
                    }

                    current = latched;
                    break;
                }

                default:
                {
                    // A shift is taken from the mode in use - a shift itself, if that is how it was reached - and a
                    // latch also becomes the mode returned to.
                    var next = control switch
                    {
                        Control.PunctShift or Control.LatchPunct => Mode.Punct,
                        Control.UpperShift or Control.LatchUpper => Mode.Upper,
                        Control.LatchLower => Mode.Lower,
                        Control.LatchMixed => Mode.Mixed,
                        _ => Mode.Digit,
                    };

                    latched = current;
                    current = next;
                    if (control is not (Control.PunctShift or Control.UpperShift))
                    {
                        latched = next;
                    }

                    break;
                }
            }
        }

        return output;
    }

    /// <summary>The character a code stands for in a mode, or the control it is.</summary>
    private static Control Character(Mode mode, int code, out string? literal)
    {
        literal = null;
        switch (mode)
        {
            case Mode.Upper:
            case Mode.Lower:
                switch (code)
                {
                    case 0: return Control.PunctShift;
                    case 1: literal = " "; return Control.None;
                    case 28: return mode == Mode.Upper ? Control.LatchLower : Control.UpperShift;
                    case 29: return Control.LatchMixed;
                    case 30: return Control.LatchDigit;
                    case 31: return Control.BinaryShift;
                    default:
                        literal = ((char)((mode == Mode.Upper ? 'A' : 'a') + code - 2)).ToString();
                        return Control.None;
                }

            case Mode.Mixed:
                switch (code)
                {
                    case 0: return Control.PunctShift;
                    case 1: literal = " "; return Control.None;
                    case <= 14: literal = ((char)(code - 1)).ToString(); return Control.None;
                    case <= 19: literal = ((char)(code + 12)).ToString(); return Control.None;
                    case 28: return Control.LatchLower;
                    case 29: return Control.LatchUpper;
                    case 30: return Control.LatchPunct;
                    case 31: return Control.BinaryShift;
                    default:
                        literal = MixedSymbols[code - 20].ToString();
                        return Control.None;
                }

            case Mode.Punct:
                switch (code)
                {
                    case 0: return Control.Flag;
                    case 31: return Control.LatchUpper;
                    default:
                        literal = Punctuation[code - 1];
                        return Control.None;
                }

            default:
                switch (code)
                {
                    case 0: return Control.PunctShift;
                    case 1: literal = " "; return Control.None;
                    case <= 11: literal = ((char)('0' + code - 2)).ToString(); return Control.None;
                    case 12: literal = ","; return Control.None;
                    case 13: literal = "."; return Control.None;
                    case 14: return Control.LatchUpper;
                    default: return Control.UpperShift;
                }
        }
    }

    /// <summary>Mixed mode codes 20 to 27.</summary>
    private const string MixedSymbols = "@\\^_`|~\u007F";

    /// <summary>Punctuation mode codes 1 to 30; four of them stand for two characters.</summary>
    private static readonly string[] Punctuation =
    [
        "\r", "\r\n", ". ", ", ", ": ", "!", "\"", "#", "$", "%", "&", "'", "(", ")", "*",
        "+", ",", "-", ".", "/", ":", ";", "<", "=", ">", "?", "[", "]", "{", "}",
    ];

    private static int Read(bool[] bits, ref int at, int count)
    {
        var value = 0;
        for (var i = 0; i < count; i++)
        {
            value = (value << 1) | (bits[at++] ? 1 : 0);
        }

        return value;
    }

    /// <summary>
    /// The decoded bytes, and the text they make in the character set in force where each was written: ISO-8859-1
    /// unless an ECI says otherwise, or UTF-8 when the bytes are valid UTF-8 and no ECI was given.
    /// </summary>
    private sealed class Output
    {
        private readonly StringBuilder _text = new();
        private readonly List<byte> _segment = [];
        private int? _segmentEci;

        public List<byte> Raw { get; } = [];

        public int? Eci { get; private set; }

        public bool IsGs1 { get; set; }

        public string Text
        {
            get
            {
                Flush();
                return _text.ToString();
            }
        }

        public void Append(byte value)
        {
            Raw.Add(value);
            _segment.Add(value);
        }

        public void Append(string characters)
        {
            foreach (var character in characters)
            {
                Append((byte)character);
            }
        }

        public void SetEci(int eci)
        {
            Flush();
            Eci ??= eci;
            _segmentEci = eci <= 899 ? eci : null;
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
}
