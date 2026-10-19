using System.Globalization;

namespace Bit.Bink;

/// <summary>
/// Formats coordinates for SVG output: fixed-point with at most six decimals, invariant culture, trailing zeros
/// trimmed, and "-0" written as "0". Exporting a large drawing formats millions of numbers, so the common case
/// avoids the general float formatter entirely while producing exactly the same characters.
/// </summary>
internal static class BinkSvgNumber
{
    /// <summary>Longest output: float.MaxValue with six decimals and a sign is 47 characters.</summary>
    public const int MaxLength = 64;

    private static readonly string[] Formats = ["F0", "F1", "F2", "F3", "F4", "F5", "F6"];
    private static readonly long[] Pow10 = [1, 10, 100, 1_000, 10_000, 100_000, 1_000_000];

    /// <summary>Writes <paramref name="value"/> with <paramref name="decimals"/> (clamped to 0..6) decimals.</summary>
    public static void Write(TextWriter writer, float value, int decimals)
    {
        Span<char> buffer = stackalloc char[MaxLength];
        writer.Write(buffer[..Format(value, decimals, buffer)]);
    }

    /// <summary>Formats into <paramref name="buffer"/> (at least <see cref="MaxLength"/> chars) and returns the length.</summary>
    public static int Format(float value, int decimals, Span<char> buffer)
    {
        decimals = Math.Clamp(decimals, 0, 6);
        if (!float.IsFinite(value)) value = 0;
        if (MathF.Abs(value) < 1e9f) return FormatFixed(value, decimals, buffer);

        value.TryFormat(buffer, out int written, Formats[decimals], CultureInfo.InvariantCulture);
        ReadOnlySpan<char> s = buffer[..written];
        if (s.IndexOf('.') >= 0)
        {
            s = s.TrimEnd('0');
            if (s.Length > 0 && s[^1] == '.') s = s[..^1];
        }
        if (s.IsEmpty || s.SequenceEqual("-0"))
        {
            buffer[0] = '0';
            return 1;
        }
        return s.Length;
    }

    /// <summary>
    /// Exact fixed-point formatting. A float is <c>m * 2^e</c> with a 24-bit <c>m</c>, so for magnitudes below 1e9
    /// the value times 10^decimals is a quotient of integers that fit in a long. Rounding that quotient to nearest,
    /// ties to even, is what the "F" format does with the exact binary value (0.125 with two decimals is "0.12").
    /// </summary>
    private static int FormatFixed(float value, int decimals, Span<char> buffer)
    {
        int bits = BitConverter.SingleToInt32Bits(value);
        bool negative = bits < 0;
        int exponentField = (bits >> 23) & 0xFF;
        long mantissa = bits & 0x7FFFFF;
        int exponent;
        if (exponentField == 0)
        {
            exponent = -149;
        }
        else
        {
            mantissa |= 1L << 23;
            exponent = exponentField - 150;
        }

        long scaled = mantissa * Pow10[decimals];
        long rounded;
        if (exponent >= 0)
        {
            rounded = scaled << exponent;
        }
        else
        {
            int shift = -exponent;
            // scaled is below 2^44, so beyond this shift the quotient is 0 and the remainder under one half.
            if (shift >= 62)
            {
                rounded = 0;
            }
            else
            {
                rounded = scaled >> shift;
                long remainder = scaled - (rounded << shift);
                long half = 1L << (shift - 1);
                if (remainder > half || (remainder == half && (rounded & 1) != 0)) rounded++;
            }
        }

        if (rounded == 0)
        {
            buffer[0] = '0';
            return 1;
        }

        int length = 0;
        if (negative) buffer[length++] = '-';
        long integerPart = Math.DivRem(rounded, Pow10[decimals], out long fraction);
        integerPart.TryFormat(buffer[length..], out int written, default, CultureInfo.InvariantCulture);
        length += written;
        if (fraction != 0)
        {
            buffer[length++] = '.';
            for (int i = decimals - 1; i >= 0; i--)
            {
                buffer[length + i] = (char)('0' + (int)(fraction % 10));
                fraction /= 10;
            }
            length += decimals;
            while (buffer[length - 1] == '0') length--;
        }
        return length;
    }
}
