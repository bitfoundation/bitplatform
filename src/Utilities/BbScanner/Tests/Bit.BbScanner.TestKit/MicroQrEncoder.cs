using System.Text;

namespace Bit.BbScanner.TestKit;

/// <summary>
/// A minimal Micro QR encoder (ISO/IEC 18004), M1 to M4: one segment - numeric, alphanumeric or UTF-8 bytes, whichever
/// the content fits - in the smallest symbol that holds it at the requested error correction level.
/// </summary>
public static class MicroQrEncoder
{
    private const string AlphanumericChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    /// <summary>The symbols, by symbol number: version, level ('D' for M1's detection only), codewords in all and data codewords.</summary>
    private static readonly (int Version, char Level, int Total, int Data)[] Symbols =
    [
        (1, 'D', 5, 3), (2, 'L', 10, 5), (2, 'M', 10, 4), (3, 'L', 17, 11),
        (3, 'M', 17, 9), (4, 'L', 24, 16), (4, 'M', 24, 14), (4, 'Q', 24, 10),
    ];

    /// <summary>Encodes <paramref name="content"/>.</summary>
    /// <param name="content">Digits, QR alphanumeric characters, or any text, which is written as UTF-8 bytes.</param>
    /// <param name="level">'L', 'M' or 'Q'; M1, which only detects errors, is used for 'L' when the content fits it.</param>
    /// <param name="maskPattern">0 to 3, or <see langword="null"/> for the one the specification's evaluation prefers.</param>
    public static BbBitMatrix Encode(string content, char level = 'L', int? maskPattern = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        var mode = content.All(char.IsAsciiDigit) ? 0 : content.All(c => AlphanumericChars.Contains(c, StringComparison.Ordinal)) ? 1 : 2;
        var bytes = Encoding.UTF8.GetBytes(content);
        var count = mode == 2 ? bytes.Length : content.Length;
        int[] countBits = mode switch { 0 => [3, 4, 5, 6], 1 => [0, 3, 4, 5], _ => [0, 0, 4, 5] };

        for (var number = 0; number < Symbols.Length; number++)
        {
            var symbol = Symbols[number];
            var width = countBits[symbol.Version - 1];
            if ((symbol.Level != level && !(symbol.Level == 'D' && level == 'L')) || width == 0 || count >= 1 << width)
            {
                continue;
            }

            var bits = new List<bool>();
            Append(bits, mode, symbol.Version - 1);
            Append(bits, count, width);
            switch (mode)
            {
                case 0:
                    for (var i = 0; i < content.Length; i += 3)
                    {
                        var group = content.Substring(i, Math.Min(3, content.Length - i));
                        Append(bits, int.Parse(group, System.Globalization.CultureInfo.InvariantCulture), (3 * group.Length) + 1);
                    }

                    break;
                case 1:
                    for (var i = 0; i < content.Length; i += 2)
                    {
                        var first = AlphanumericChars.IndexOf(content[i], StringComparison.Ordinal);
                        if (i + 1 < content.Length)
                        {
                            Append(bits, (first * 45) + AlphanumericChars.IndexOf(content[i + 1], StringComparison.Ordinal), 11);
                        }
                        else
                        {
                            Append(bits, first, 6);
                        }
                    }

                    break;
                default:
                    foreach (var value in bytes)
                    {
                        Append(bits, value, 8);
                    }

                    break;
            }

            var shortLast = symbol.Version is 1 or 3;
            var capacity = (8 * symbol.Data) - (shortLast ? 4 : 0);
            if (bits.Count > capacity)
            {
                continue;
            }

            return Build(number, Pad(bits, symbol.Version, symbol.Data, shortLast, capacity), maskPattern);
        }

        throw new ArgumentException($"'{content}' does not fit a Micro QR symbol at level {level}.", nameof(content));
    }

    /// <summary>
    /// The data codewords: the terminator, as much of it as fits, zeros to the codeword, then the pad codewords 0xEC and
    /// 0x11 in turn - the four bit last codeword of M1 and M3 zero.
    /// </summary>
    private static int[] Pad(List<bool> bits, int version, int dataCodewords, bool shortLast, int capacity)
    {
        bits.AddRange(Enumerable.Repeat(false, Math.Min((2 * version) + 1, capacity - bits.Count)));
        while (bits.Count < capacity && bits.Count % 8 != 0)
        {
            bits.Add(false);
        }

        var codewords = new int[dataCodewords];
        var at = 0;
        for (var i = 0; i < dataCodewords; i++)
        {
            var length = shortLast && i == dataCodewords - 1 ? 4 : 8;
            if (at >= bits.Count)
            {
                codewords[i] = length == 4 ? 0 : ((i - (bits.Count / 8)) % 2 == 0 ? 0xEC : 0x11);
                continue;
            }

            var value = 0;
            for (var b = 0; b < length; b++)
            {
                value = (value << 1) | (at < bits.Count && bits[at++] ? 1 : 0);
            }

            codewords[i] = length == 4 ? value << 4 : value;
        }

        return codewords;
    }

    private static BbBitMatrix Build(int number, int[] data, int? maskPattern)
    {
        var symbol = Symbols[number];
        var codewords = new int[symbol.Total];
        data.CopyTo(codewords, 0);
        new ReedSolomonEncoder(BbGenericGF.QrCodeField256).Encode(codewords, symbol.Total - symbol.Data);

        // The bits in placement order: the data codewords, four bits only of a short last one, then the check codewords.
        var stream = new List<bool>();
        for (var i = 0; i < codewords.Length; i++)
        {
            if (i == symbol.Data - 1 && symbol.Version is 1 or 3)
            {
                Append(stream, codewords[i] >> 4, 4);
            }
            else
            {
                Append(stream, codewords[i], 8);
            }
        }

        var dimension = 9 + (2 * symbol.Version);
        var best = (Matrix: (BbBitMatrix?)null, Score: -1);
        for (var mask = 0; mask < 4; mask++)
        {
            if (maskPattern is { } wanted && wanted != mask)
            {
                continue;
            }

            var matrix = Place(dimension, stream, mask, (number << 2) | mask);
            var score = Evaluate(matrix);
            if (score > best.Score)
            {
                best.Matrix?.Dispose();
                best = (matrix, score);
            }
            else
            {
                matrix.Dispose();
            }
        }

        return best.Matrix!;
    }

    private static BbBitMatrix Place(int dimension, List<bool> stream, int mask, int format)
    {
        var matrix = new BbBitMatrix(dimension, dimension);

        // The finder pattern, its separator light, and the timing patterns along the top row and left column.
        for (var y = 0; y < 7; y++)
        {
            for (var x = 0; x < 7; x++)
            {
                if (x is 0 or 6 || y is 0 or 6 || (x is >= 2 and <= 4 && y is >= 2 and <= 4))
                {
                    matrix[x, y] = true;
                }
            }
        }

        for (var i = 8; i < dimension; i += 2)
        {
            matrix[i, 0] = true;
            matrix[0, i] = true;
        }

        // The data, two columns at a time from the bottom right, up and down in turn.
        var at = 0;
        var upward = true;
        for (var x = dimension - 1; x > 0; x -= 2)
        {
            for (var step = 0; step < dimension; step++)
            {
                var y = upward ? dimension - 1 - step : step;
                for (var column = 0; column < 2; column++)
                {
                    var xx = x - column;
                    if ((xx < 9 && y < 9) || xx == 0 || y == 0)
                    {
                        continue;
                    }

                    var dark = at < stream.Count && stream[at];
                    at++;
                    if (dark != Masked(mask, xx, y))
                    {
                        matrix[xx, y] = true;
                    }
                }
            }

            upward = !upward;
        }

        // The format information: row 8 from column 1 to 8, then column 8 from row 7 up to 1, the most significant first.
        var word = FormatWord(format);
        var bit = 14;
        for (var x = 1; x <= 8; x++, bit--)
        {
            matrix[x, 8] = ((word >> bit) & 1) == 1;
        }

        for (var y = 7; y >= 1; y--, bit--)
        {
            matrix[8, y] = ((word >> bit) & 1) == 1;
        }

        return matrix;
    }

    private static int FormatWord(int data)
    {
        var remainder = data << 10;
        for (var b = 14; b >= 10; b--)
        {
            if ((remainder & (1 << b)) != 0)
            {
                remainder ^= 0x537 << (b - 10);
            }
        }

        return ((data << 10) | remainder) ^ 0x4445;
    }

    private static bool Masked(int mask, int x, int y) => mask switch
    {
        0 => y % 2 == 0,
        1 => ((y / 2) + (x / 3)) % 2 == 0,
        2 => (((y * x) % 2) + ((y * x) % 3)) % 2 == 0,
        _ => (((y + x) % 2) + ((y * x) % 3)) % 2 == 0,
    };

    /// <summary>The specification's evaluation: the darker the right column and bottom row, the lighter of them sixteen times over.</summary>
    private static int Evaluate(BbBitMatrix matrix)
    {
        var last = matrix.Width - 1;
        var (right, bottom) = (0, 0);
        for (var i = 1; i <= last; i++)
        {
            right += matrix[last, i] ? 1 : 0;
            bottom += matrix[i, last] ? 1 : 0;
        }

        return right <= bottom ? (right * 16) + bottom : (bottom * 16) + right;
    }

    private static void Append(List<bool> bits, int value, int count)
    {
        for (var i = count - 1; i >= 0; i--)
        {
            bits.Add(((value >> i) & 1) != 0);
        }
    }
}
