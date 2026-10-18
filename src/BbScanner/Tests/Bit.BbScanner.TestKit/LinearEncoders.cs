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
