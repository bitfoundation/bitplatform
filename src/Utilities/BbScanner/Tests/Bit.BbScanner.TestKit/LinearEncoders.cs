namespace Bit.BbScanner.TestKit;

/// <summary>
/// Encoders for the linear symbologies, producing module arrays where <see langword="true"/>
/// is a bar.
/// </summary>
/// <remarks>
/// The character tables are repeated here rather than shared with the decoders. That is
/// deliberate: a shared table would let a transcription error cancel itself out in a round trip
/// test and go unnoticed.
/// </remarks>
public static class LinearEncoders
{
    private static readonly int[][] Code128Patterns =
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

    private const string Code39Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";

    private static readonly int[] Code39Encodings =
    [
        0x034, 0x121, 0x061, 0x160, 0x031, 0x130, 0x070, 0x025, 0x124, 0x064,
        0x109, 0x049, 0x148, 0x019, 0x118, 0x058, 0x00D, 0x10C, 0x04C, 0x01C,
        0x103, 0x043, 0x142, 0x013, 0x112, 0x052, 0x007, 0x106, 0x046, 0x016,
        0x181, 0x0C1, 0x1C0, 0x091, 0x190, 0x0D0, 0x085, 0x184, 0x0C4, 0x0A8,
        0x0A2, 0x08A, 0x02A,
    ];

    private const int Code39Asterisk = 0x094;

    private static readonly int[][] EanLPatterns =
    [
        [3, 2, 1, 1], [2, 2, 2, 1], [2, 1, 2, 2], [1, 4, 1, 1], [1, 1, 3, 2],
        [1, 2, 3, 1], [1, 1, 1, 4], [1, 3, 1, 2], [1, 2, 1, 3], [3, 1, 1, 2],
    ];

    private static readonly int[] EanFirstDigitParity =
        [0x00, 0x0B, 0x0D, 0x0E, 0x13, 0x19, 0x1C, 0x15, 0x16, 0x1A];

    private static readonly int[][] UpcEParity =
    [
        [0x38, 0x34, 0x32, 0x31, 0x2C, 0x26, 0x23, 0x2A, 0x29, 0x25],
        [0x07, 0x0B, 0x0D, 0x0E, 0x13, 0x19, 0x1C, 0x15, 0x16, 0x1A],
    ];

    private static readonly int[][] ItfPatterns =
    [
        [1, 1, 2, 2, 1], [2, 1, 1, 1, 2], [1, 2, 1, 1, 2], [2, 2, 1, 1, 1], [1, 1, 2, 1, 2],
        [2, 1, 2, 1, 1], [1, 2, 2, 1, 1], [1, 1, 1, 2, 2], [2, 1, 1, 2, 1], [1, 2, 1, 2, 1],
    ];

    private const string CodabarAlphabet = "0123456789-$:/.+ABCD";

    /// <summary>Codabar characters: bit <c>6 - i</c> is set when element <c>i</c> is wide, bar first.</summary>
    private static readonly int[] CodabarEncodings =
    [
        0x03, 0x06, 0x09, 0x60, 0x12, 0x42, 0x21, 0x24, 0x30, 0x48,
        0x0C, 0x18, 0x45, 0x51, 0x54, 0x15, 0x1A, 0x29, 0x0B, 0x0E,
    ];

    private const string Code93Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";

    /// <summary>
    /// Code 93 characters as bar and space widths, bar first: the alphabet, the four shifts
    /// ($), (%), (/), (+), then the start and stop character.
    /// </summary>
    private static readonly string[] Code93Encodings =
    [
        "131112", "111213", "111312", "111411", "121113", "121212", "121311", "111114", "131211", "141111",
        "211113", "211212", "211311", "221112", "221211", "231111", "112113", "112212", "112311", "122112",
        "132111", "111123", "111222", "111321", "121122", "131121", "212112", "212211", "211122", "211221",
        "221121", "222111", "112122", "112221", "122121", "123111", "121131", "311112", "311211", "321111",
        "112131", "113121", "211131", "121221", "312111", "311121", "122211", "111141",
    ];

    /// <summary>Encodes Codabar data between a start and a stop character.</summary>
    /// <param name="data">Digits and <c>- $ : / . +</c>.</param>
    /// <param name="start">The start character, <c>A</c> to <c>D</c>.</param>
    /// <param name="stop">The stop character, <c>A</c> to <c>D</c>.</param>
    /// <param name="wideRatio">Width of a wide element in narrow modules.</param>
    public static bool[] Codabar(string data, char start = 'A', char stop = 'B', int wideRatio = 3)
    {
        ArgumentNullException.ThrowIfNull(data);

        var widths = new List<int>();
        var text = $"{start}{data}{stop}";
        for (var c = 0; c < text.Length; c++)
        {
            var index = CodabarAlphabet.IndexOf(char.ToUpperInvariant(text[c]));
            if (index < 0 || (index >= 16) != (c == 0 || c == text.Length - 1))
            {
                throw new ArgumentException($"'{text[c]}' cannot appear there in a Codabar symbol.", nameof(data));
            }

            if (c > 0)
            {
                widths.Add(1); // Inter-character gap.
            }

            for (var i = 0; i < 7; i++)
            {
                widths.Add(((CodabarEncodings[index] >> (6 - i)) & 1) != 0 ? wideRatio : 1);
            }
        }

        return SyntheticImage.ExpandWidths(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(widths));
    }

    /// <summary>
    /// Encodes ASCII text as Code 93, with the two check characters and, for characters outside
    /// its alphabet, the full ASCII shift pairs.
    /// </summary>
    /// <param name="content">Any ASCII text.</param>
    public static bool[] Code93(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var values = new List<int>();
        foreach (var c in content)
        {
            var direct = Code93Alphabet.IndexOf(c);
            if (direct >= 0)
            {
                values.Add(direct);
                continue;
            }

            var (shift, letter) = c switch
            {
                >= 'a' and <= 'z' => (46, (char)(c - 32)),
                >= (char)1 and <= (char)26 => (43, (char)(c + 64)),
                (char)0 => (44, 'U'),
                >= (char)27 and <= (char)31 => (44, (char)(c + 38)),
                >= ';' and <= '?' => (44, (char)(c + 11)),
                >= '[' and <= '_' => (44, (char)(c - 16)),
                >= '{' and <= (char)127 => (44, (char)(c - 43)),
                '@' => (44, 'V'),
                '`' => (44, 'W'),
                >= '!' and <= ',' => (45, (char)(c + 32)),
                ':' => (45, 'Z'),
                _ => throw new ArgumentException($"'{c}' is not ASCII.", nameof(content)),
            };

            values.Add(shift);
            values.Add(Code93Alphabet.IndexOf(letter));
        }

        values.Add(Code93Check(values, 20));
        values.Add(Code93Check(values, 15));

        var widths = new List<int>();
        Append(47);
        foreach (var value in values)
        {
            Append(value);
        }

        Append(47);
        widths.Add(1); // Termination bar.
        return SyntheticImage.ExpandWidths(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(widths));

        void Append(int value)
        {
            foreach (var digit in Code93Encodings[value])
            {
                widths.Add(digit - '0');
            }
        }

        static int Code93Check(List<int> values, int maxWeight)
        {
            var sum = 0;
            var weight = 1;
            for (var i = values.Count - 1; i >= 0; i--)
            {
                sum += values[i] * weight;
                weight = weight == maxWeight ? 1 : weight + 1;
            }

            return sum % 47;
        }
    }

    /// <summary>Encodes text as Code 128, choosing code set B or C automatically.</summary>
    /// <param name="content">ASCII text, or digits for the compact numeric code set.</param>
    public static bool[] Code128(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var allDigits = content.Length > 0 && content.Length % 2 == 0;
        foreach (var c in content)
        {
            if (c is < '0' or > '9')
            {
                allDigits = false;
                break;
            }
        }

        var values = new List<int>();
        if (allDigits)
        {
            values.Add(105);
            for (var i = 0; i < content.Length; i += 2)
            {
                values.Add(int.Parse(content.AsSpan(i, 2), System.Globalization.CultureInfo.InvariantCulture));
            }
        }
        else
        {
            values.Add(104);
            foreach (var c in content)
            {
                if (c is < ' ' or > '~')
                {
                    throw new ArgumentException("Only printable ASCII is supported.", nameof(content));
                }

                values.Add(c - ' ');
            }
        }

        var checksum = values[0];
        for (var i = 1; i < values.Count; i++)
        {
            checksum += values[i] * i;
        }

        values.Add(checksum % 103);
        values.Add(106);

        var widths = new List<int>();
        foreach (var value in values)
        {
            widths.AddRange(Code128Patterns[value]);
        }

        return SyntheticImage.ExpandWidths(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(widths));
    }

    /// <summary>Encodes text as Code 39, with the surrounding asterisk delimiters.</summary>
    /// <param name="content">Text drawn from the Code 39 alphabet.</param>
    /// <param name="wideRatio">Width of a wide element in narrow modules.</param>
    public static bool[] Code39(string content, int wideRatio = 3)
    {
        ArgumentNullException.ThrowIfNull(content);

        var widths = new List<int>();
        AppendCode39Character(widths, Code39Asterisk, wideRatio);

        foreach (var c in content)
        {
            var index = Code39Alphabet.IndexOf(char.ToUpperInvariant(c));
            if (index < 0)
            {
                throw new ArgumentException($"'{c}' is not in the Code 39 alphabet.", nameof(content));
            }

            widths.Add(1); // Inter-character gap.
            AppendCode39Character(widths, Code39Encodings[index], wideRatio);
        }

        widths.Add(1);
        AppendCode39Character(widths, Code39Asterisk, wideRatio);

        return SyntheticImage.ExpandWidths(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(widths));
    }

    private static void AppendCode39Character(List<int> widths, int encoding, int wideRatio)
    {
        for (var i = 0; i < 9; i++)
        {
            var wide = ((encoding >> (8 - i)) & 1) != 0;
            widths.Add(wide ? wideRatio : 1);
        }
    }

    /// <summary>Encodes a thirteen digit EAN-13 value, including its check digit.</summary>
    /// <param name="digits">Thirteen digits.</param>
    public static bool[] Ean13(string digits)
    {
        ArgumentNullException.ThrowIfNull(digits);
        if (digits.Length != 13)
        {
            throw new ArgumentException("EAN-13 needs thirteen digits.", nameof(digits));
        }

        var modules = new List<bool>(95);
        AppendGuard(modules);

        var parity = EanFirstDigitParity[digits[0] - '0'];
        for (var i = 1; i <= 6; i++)
        {
            var even = ((parity >> (6 - i)) & 1) != 0;
            AppendEanDigit(modules, digits[i] - '0', even ? EanDigitStyle.EvenLeft : EanDigitStyle.OddLeft);
        }

        AppendMiddleGuard(modules);

        for (var i = 7; i <= 12; i++)
        {
            AppendEanDigit(modules, digits[i] - '0', EanDigitStyle.Right);
        }

        AppendGuard(modules);
        return [.. modules];
    }

    /// <summary>Encodes an eight digit EAN-8 value, including its check digit.</summary>
    /// <param name="digits">Eight digits.</param>
    public static bool[] Ean8(string digits)
    {
        ArgumentNullException.ThrowIfNull(digits);
        if (digits.Length != 8)
        {
            throw new ArgumentException("EAN-8 needs eight digits.", nameof(digits));
        }

        var modules = new List<bool>(67);
        AppendGuard(modules);

        for (var i = 0; i < 4; i++)
        {
            AppendEanDigit(modules, digits[i] - '0', EanDigitStyle.OddLeft);
        }

        AppendMiddleGuard(modules);

        for (var i = 4; i < 8; i++)
        {
            AppendEanDigit(modules, digits[i] - '0', EanDigitStyle.Right);
        }

        AppendGuard(modules);
        return [.. modules];
    }

    /// <summary>Encodes a twelve digit UPC-A value as the EAN-13 symbol it is.</summary>
    /// <param name="digits">Twelve digits.</param>
    public static bool[] UpcA(string digits)
    {
        ArgumentNullException.ThrowIfNull(digits);
        if (digits.Length != 12)
        {
            throw new ArgumentException("UPC-A needs twelve digits.", nameof(digits));
        }

        return Ean13("0" + digits);
    }

    /// <summary>Encodes an eight character UPC-E value, including number system and check digit.</summary>
    /// <param name="digits">Eight digits; the first must be 0 or 1.</param>
    public static bool[] UpcE(string digits)
    {
        ArgumentNullException.ThrowIfNull(digits);
        if (digits.Length != 8)
        {
            throw new ArgumentException("UPC-E needs eight digits.", nameof(digits));
        }

        var numberSystem = digits[0] - '0';
        if (numberSystem is not (0 or 1))
        {
            throw new ArgumentException("The UPC-E number system must be 0 or 1.", nameof(digits));
        }

        var parity = UpcEParity[numberSystem][digits[7] - '0'];

        var modules = new List<bool>(51);
        AppendGuard(modules);

        for (var i = 1; i <= 6; i++)
        {
            var even = ((parity >> (6 - i)) & 1) != 0;
            AppendEanDigit(modules, digits[i] - '0', even ? EanDigitStyle.EvenLeft : EanDigitStyle.OddLeft);
        }

        // UPC-E closes with a six element guard instead of the usual three.
        for (var i = 0; i < 3; i++)
        {
            modules.Add(false);
            modules.Add(true);
        }

        return [.. modules];
    }

    private enum EanDigitStyle
    {
        OddLeft,
        EvenLeft,
        Right,
    }

    private static void AppendGuard(List<bool> modules)
    {
        modules.Add(true);
        modules.Add(false);
        modules.Add(true);
    }

    private static void AppendMiddleGuard(List<bool> modules)
    {
        modules.Add(false);
        modules.Add(true);
        modules.Add(false);
        modules.Add(true);
        modules.Add(false);
    }

    private static void AppendEanDigit(List<bool> modules, int digit, EanDigitStyle style)
    {
        var widths = EanLPatterns[digit];

        switch (style)
        {
            case EanDigitStyle.OddLeft:
                // Left-hand odd parity: space first.
                AppendRun(modules, widths, startWithBar: false, reverse: false);
                break;
            case EanDigitStyle.EvenLeft:
                // Left-hand even parity is the odd pattern read backwards.
                AppendRun(modules, widths, startWithBar: false, reverse: true);
                break;
            default:
                // Right-hand digits are the colour inverse, which is the same widths starting dark.
                AppendRun(modules, widths, startWithBar: true, reverse: false);
                break;
        }
    }

    private static void AppendRun(List<bool> modules, int[] widths, bool startWithBar, bool reverse)
    {
        var bar = startWithBar;
        for (var i = 0; i < widths.Length; i++)
        {
            var width = reverse ? widths[widths.Length - 1 - i] : widths[i];
            for (var j = 0; j < width; j++)
            {
                modules.Add(bar);
            }

            bar = !bar;
        }
    }

    /// <summary>Encodes an even number of digits as Interleaved 2 of 5.</summary>
    /// <param name="digits">An even number of digits.</param>
    /// <param name="wideRatio">Width of a wide element in narrow modules.</param>
    public static bool[] Itf(string digits, int wideRatio = 2)
    {
        ArgumentNullException.ThrowIfNull(digits);
        if (digits.Length % 2 != 0 || digits.Length == 0)
        {
            throw new ArgumentException("ITF encodes digits in pairs.", nameof(digits));
        }

        var widths = new List<int> { 1, 1, 1, 1 };

        for (var i = 0; i < digits.Length; i += 2)
        {
            var bars = ItfPatterns[digits[i] - '0'];
            var spaces = ItfPatterns[digits[i + 1] - '0'];
            for (var k = 0; k < 5; k++)
            {
                widths.Add(bars[k] == 1 ? 1 : wideRatio);
                widths.Add(spaces[k] == 1 ? 1 : wideRatio);
            }
        }

        widths.Add(wideRatio);
        widths.Add(1);
        widths.Add(1);

        return SyntheticImage.ExpandWidths(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(widths));
    }

    /// <summary>
    /// Encodes GS1 DataBar Omnidirectional: a GTIN-14, with or without its check digit, which the symbol does not
    /// carry. Truncated DataBar is the same modules, drawn shorter.
    /// </summary>
    /// <param name="gtin">Thirteen or fourteen digits.</param>
    public static bool[] DataBar(string gtin)
    {
        ArgumentNullException.ThrowIfNull(gtin);
        if (gtin.Length is not (13 or 14) || !gtin.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("A GTIN-14 is thirteen digits and a check digit.", nameof(gtin));
        }

        var value = long.Parse(gtin[..13], System.Globalization.CultureInfo.InvariantCulture);
        var left = value / 4537077;
        var right = value % 4537077;
        var d1w = DataBarCharacter((int)(left / 1597), outside: true, oddFirst: true);
        var d2w = DataBarCharacter((int)(left % 1597), outside: false, oddFirst: false);
        var d3w = DataBarCharacter((int)(right / 1597), outside: true, oddFirst: false);
        var d4w = DataBarCharacter((int)(right % 1597), outside: false, oddFirst: true);

        int[] weights =
        [
            1, 3, 9, 27, 2, 6, 18, 54, 58, 72, 24, 8, 29, 36, 12, 4,
            74, 51, 17, 32, 37, 65, 48, 16, 64, 34, 23, 69, 49, 68, 46, 59,
        ];
        int[][] finders =
        [
            [3, 8, 2, 1, 1], [3, 5, 5, 1, 1], [3, 3, 7, 1, 1], [3, 1, 9, 1, 1], [2, 7, 4, 1, 1],
            [2, 5, 6, 1, 1], [2, 3, 8, 1, 1], [1, 5, 7, 1, 1], [1, 3, 9, 1, 1],
        ];

        var checksum = 0;
        var all = d1w.Concat(d2w).Concat(d3w).Concat(d4w).ToArray();
        for (var i = 0; i < 32; i++)
        {
            checksum += all[i] * weights[i];
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

        // Bar first: the left guard's bar (its space is the quiet zone), then the characters and finders as drawn,
        // the right finder mirrored, and the right guard's space and bar.
        var widths = new List<int> { 1 };
        widths.AddRange(d1w);
        widths.AddRange(finders[checksum / 9]);
        widths.AddRange(d2w);
        widths.AddRange(d4w);
        widths.AddRange(finders[checksum % 9].Reverse());
        widths.AddRange(d3w);
        widths.AddRange([1, 1]);
        return SyntheticImage.ExpandWidths(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(widths));
    }

    private static int[] DataBarCharacter(int value, bool outside, bool oddFirst)
    {
        // Per group: the largest value, the first, odd and even modules, widest odd and even, odd and even arrangements.
        int[][] groups = outside
            ? [[160, 0, 12, 4, 8, 1, 161, 1], [960, 161, 10, 6, 6, 3, 80, 10], [2014, 961, 8, 8, 4, 5, 31, 34], [2714, 2015, 6, 10, 3, 6, 10, 70], [2840, 2715, 4, 12, 1, 8, 1, 126]]
            : [[335, 0, 5, 10, 2, 7, 4, 84], [1035, 336, 7, 8, 4, 5, 20, 35], [1515, 1036, 9, 6, 6, 3, 48, 10], [1596, 1516, 11, 4, 8, 1, 81, 1]];
        var group = groups.First(g => value <= g[0]);
        var offset = value - group[1];
        var (vOdd, vEven) = outside ? (offset / group[7], offset % group[7]) : (offset % group[6], offset / group[6]);
        var odd = RssWidths(vOdd, group[2], group[4], noNarrow: !outside);
        var even = RssWidths(vEven, group[3], group[5], noNarrow: outside);

        var widths = new int[8];
        for (var i = 0; i < 4; i++)
        {
            if (oddFirst)
            {
                widths[2 * i] = odd[i];
                widths[(2 * i) + 1] = even[i];
            }
            else
            {
                widths[7 - (2 * i)] = odd[i];
                widths[6 - (2 * i)] = even[i];
            }
        }

        return widths;
    }

    /// <summary>The widths of the arrangement numbered <paramref name="value"/> - the inverse of the decoder's value.</summary>
    private static int[] RssWidths(int value, int modules, int maxWidth, bool noNarrow, int elements = 4)
    {
        var widths = new int[elements];
        var mask = 0;
        for (var bar = 0; bar < elements - 1; bar++)
        {
            var width = 1;
            mask |= 1 << bar;
            int sub;
            while (true)
            {
                sub = Combinations(modules - width - 1, elements - bar - 2);
                if (noNarrow && mask == 0 && modules - width - (elements * 2) + (bar * 2) >= -2)
                {
                    sub -= Combinations(modules - width - elements + bar, elements - bar - 2);
                }

                if (elements - bar > 2)
                {
                    var less = 0;
                    for (var widest = modules - width - elements + bar + 2; widest >= maxWidth + 1; widest--)
                    {
                        less += Combinations(modules - widest - width - 1, elements - bar - 3);
                    }

                    sub -= less * (elements - bar - 1);
                }
                else if (modules - width > maxWidth)
                {
                    sub--;
                }

                value -= sub;
                if (value < 0)
                {
                    break;
                }

                width++;
                mask &= ~(1 << bar);
            }

            value += sub;
            modules -= width;
            widths[bar] = width;
        }

        widths[elements - 1] = modules;
        return widths;
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

    /// <summary>Computes the modulo 10 check digit used by the EAN and UPC family.</summary>
    /// <param name="digitsWithoutCheck">The value without its trailing check digit.</param>
    public static char EanCheckDigit(string digitsWithoutCheck)
    {
        ArgumentNullException.ThrowIfNull(digitsWithoutCheck);

        var sum = 0;
        var length = digitsWithoutCheck.Length;
        for (var i = length - 1; i >= 0; i--)
        {
            var digit = digitsWithoutCheck[i] - '0';
            sum += ((length - i) % 2 == 1) ? digit * 3 : digit;
        }

        return (char)('0' + ((1000 - sum) % 10));
    }
}
