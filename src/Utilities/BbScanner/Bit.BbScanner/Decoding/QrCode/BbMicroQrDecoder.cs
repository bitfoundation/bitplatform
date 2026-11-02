namespace Bit.BbScanner;

/// <summary>
/// Turns the module grid of a Micro QR symbol (ISO/IEC 18004), M1 to M4, read upright, into its payload.
/// </summary>
/// <remarks>
/// <para>
/// A Micro QR symbol is 11, 13, 15 or 17 modules square: one finder pattern in the top left corner, timing patterns
/// along the whole of the top row and left column, and fifteen bits of format information beside the finder - the
/// symbol number, which is the version and error correction level together, and which of four masks the data was
/// written with, protected by a BCH code and masked with 0x4445.
/// </para>
/// <para>
/// The codewords are laid out as in QR Code, two columns at a time from the bottom right, up and down in turn, around
/// the function patterns; but in M1 and M3 the last data codeword is four bits long. One Reed-Solomon block, in QR
/// Code's field, holds them all, and is only trusted to repair as many codewords as the specification gives each
/// variant - none in M1, whose two check codewords only detect errors.
/// </para>
/// </remarks>
internal static class BbMicroQrDecoder
{
    private static readonly BbReedSolomonDecoder Correction = new(BbGenericGF.QrCodeField256);

    /// <summary>
    /// The eight symbols the symbol number stands for: version, error correction level, codewords in all, data
    /// codewords, and how many codewords the check codewords may repair.
    /// </summary>
    private static readonly (int Version, string Level, int Total, int Data, int Repairable)[] Symbols =
    [
        (1, "Detection", 5, 3, 0),
        (2, "L", 10, 5, 2),
        (2, "M", 10, 4, 3),
        (3, "L", 17, 11, 2),
        (3, "M", 17, 9, 4),
        (4, "L", 24, 16, 3),
        (4, "M", 24, 14, 5),
        (4, "Q", 24, 10, 7),
    ];

    /// <summary>Decodes a grid read upright, 11 to 17 modules square; <see langword="null"/> when it does not decode.</summary>
    public static BbDecoderResult? Decode(BbBitMatrix grid)
    {
        var dimension = grid.Width;
        if (grid.Height != dimension || dimension is not (11 or 13 or 15 or 17) || ReadFormat(grid) is not { } format)
        {
            return null;
        }

        var (number, mask) = format;
        var symbol = Symbols[number];
        if (symbol.Version != (dimension - 9) / 2)
        {
            return null;
        }

        var codewords = new int[symbol.Total];
        if (!ReadCodewords(grid, mask, symbol.Data, symbol.Version is 1 or 3, codewords) ||
            !Correction.TryDecode(codewords, symbol.Total - symbol.Data, out var corrected) || corrected > symbol.Repairable)
        {
            return null;
        }

        var data = new byte[symbol.Data];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (byte)codewords[i];
        }

        return BbQrBitStreamParser.DecodeMicro(data, symbol.Version, symbol.Level, corrected);
    }

    /// <summary>
    /// The format information beside the finder: row 8 from column 1 to 8, then column 8 from row 7 up to 1, the most
    /// significant bit first. The nearest of the 32 valid words, three bits off at most, gives the symbol number and
    /// the mask.
    /// </summary>
    private static (int Number, int Mask)? ReadFormat(BbBitMatrix grid)
    {
        var read = 0;
        for (var x = 1; x <= 8; x++)
        {
            read = (read << 1) | (grid[x, 8] ? 1 : 0);
        }

        for (var y = 7; y >= 1; y--)
        {
            read = (read << 1) | (grid[8, y] ? 1 : 0);
        }

        var (best, bestDistance) = (-1, 4);
        for (var data = 0; data < 32; data++)
        {
            var distance = System.Numerics.BitOperations.PopCount((uint)(read ^ FormatWord(data)));
            if (distance < bestDistance)
            {
                (best, bestDistance) = (data, distance);
            }
        }

        return best < 0 ? null : (best >> 2, best & 3);
    }

    /// <summary>The fifteen bit format word of five data bits: their BCH code, generator 0x537, masked with 0x4445.</summary>
    private static int FormatWord(int data)
    {
        var remainder = data << 10;
        for (var bit = 14; bit >= 10; bit--)
        {
            if ((remainder & (1 << bit)) != 0)
            {
                remainder ^= 0x537 << (bit - 10);
            }
        }

        return ((data << 10) | remainder) ^ 0x4445;
    }

    /// <summary>
    /// Reads the codewords two columns at a time from the bottom right, up and down in turn, skipping the finder, its
    /// separator and the format information in the top left nine by nine, and the timing patterns along the top row and
    /// left column; each bit unmasked. In M1 and M3 the last data codeword is four bits, kept as its high four.
    /// </summary>
    private static bool ReadCodewords(BbBitMatrix grid, int mask, int dataCodewords, bool shortLast, int[] codewords)
    {
        var dimension = grid.Width;
        var (count, current, bitsRead) = (0, 0, 0);
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

                    current = (current << 1) | (grid[xx, y] != Masked(mask, xx, y) ? 1 : 0);
                    bitsRead++;
                    if (bitsRead == 8 || (bitsRead == 4 && shortLast && count == dataCodewords - 1))
                    {
                        if (count == codewords.Length)
                        {
                            return false;
                        }

                        codewords[count++] = bitsRead == 4 ? current << 4 : current;
                        (current, bitsRead) = (0, 0);
                    }
                }
            }

            upward = !upward;
        }

        return count == codewords.Length;
    }

    /// <summary>Whether mask <paramref name="mask"/> darkens the module in column <paramref name="x"/>, row <paramref name="y"/>.</summary>
    private static bool Masked(int mask, int x, int y) => mask switch
    {
        0 => y % 2 == 0,
        1 => ((y / 2) + (x / 3)) % 2 == 0,
        2 => (((y * x) % 2) + ((y * x) % 3)) % 2 == 0,
        _ => (((y + x) % 2) + ((y * x) % 3)) % 2 == 0,
    };
}
