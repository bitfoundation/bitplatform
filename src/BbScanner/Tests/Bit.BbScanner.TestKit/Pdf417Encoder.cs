
namespace Bit.BbScanner.TestKit;

/// <summary>
/// A PDF417 encoder used to generate test symbols.
/// </summary>
/// <remarks>
/// It encodes with the same <see cref="BbPdf417SymbolTable"/> the decoder reads with, so a round
/// trip validates the detector, the row indicators, the GF(929) error correction and the
/// compaction modes. It does not validate the symbol character table itself, which is why the
/// library is explicit that the shipped table is a placeholder.
/// </remarks>
public static class Pdf417Encoder
{
    private const int TextCompactionLatch = 900;
    private const int ByteCompactionLatch = 901;
    private const int PadCodeword = 900;

    /// <summary>Encodes text into a PDF417 module grid.</summary>
    /// <param name="content">The text to encode; uppercase letters and spaces use text compaction, anything else uses byte compaction.</param>
    /// <param name="columns">Number of data columns, 1 to 30.</param>
    /// <param name="errorCorrectionLevel">Security level, 0 to 8.</param>
    /// <param name="rowHeightModules">Height of each symbol row in modules.</param>
    /// <param name="quietZoneModules">Light margin on each side, as a real symbol carries.</param>
    public static BbBitMatrix Encode(
        string content, int columns = 4, int errorCorrectionLevel = 2, int rowHeightModules = 3, int quietZoneModules = 2)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(columns, 30);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rowHeightModules);

        var data = BuildDataCodewords(content);

        var numEc = BbPdf417ErrorCorrection.GetErrorCorrectionCodewordCount(errorCorrectionLevel);
        var rows = (int)Math.Ceiling((data.Count + numEc) / (double)columns);
        rows = Math.Clamp(rows, 3, 90);

        var capacity = (rows * columns) - numEc;
        if (data.Count > capacity)
        {
            throw new ArgumentException(
                "The content does not fit; use more columns or a lower security level.", nameof(content));
        }

        while (data.Count < capacity)
        {
            data.Add(PadCodeword);
        }

        // The first codeword counts the data codewords, itself included.
        data[0] = data.Count;

        var all = new int[rows * columns];
        for (var i = 0; i < data.Count; i++)
        {
            all[i] = data[i];
        }

        var ec = GenerateErrorCorrection(data, numEc);
        for (var i = 0; i < numEc; i++)
        {
            all[data.Count + i] = ec[i];
        }

        return Render(all, rows, columns, errorCorrectionLevel, rowHeightModules, quietZoneModules);
    }

    private static List<int> BuildDataCodewords(string content)
    {
        var codewords = new List<int> { 0 };

        var textOnly = content.Length > 0;
        foreach (var c in content)
        {
            if (c is not ((>= 'A' and <= 'Z') or ' '))
            {
                textOnly = false;
                break;
            }
        }

        if (textOnly)
        {
            codewords.Add(TextCompactionLatch);

            var values = new List<int>(content.Length + 1);
            foreach (var c in content)
            {
                values.Add(c == ' ' ? 26 : c - 'A');
            }

            // Text compaction packs two values per codeword; an odd count is padded with the
            // punctuation shift, which the decoder discards at the end of the stream.
            if (values.Count % 2 != 0)
            {
                values.Add(29);
            }

            for (var i = 0; i < values.Count; i += 2)
            {
                codewords.Add((values[i] * 30) + values[i + 1]);
            }

            return codewords;
        }

        codewords.Add(ByteCompactionLatch);

        var bytes = System.Text.Encoding.Latin1.GetBytes(content);
        var index = 0;
        while (index + 6 <= bytes.Length)
        {
            long value = 0;
            for (var i = 0; i < 6; i++)
            {
                value = (value << 8) | bytes[index + i];
            }

            var group = new int[5];
            for (var i = 4; i >= 0; i--)
            {
                group[i] = (int)(value % 900);
                value /= 900;
            }

            codewords.AddRange(group);
            index += 6;
        }

        // Any remaining bytes are emitted one per codeword.
        while (index < bytes.Length)
        {
            codewords.Add(bytes[index++]);
        }

        return codewords;
    }

    /// <summary>
    /// Computes the error correction codewords as the negated remainder of the data polynomial
    /// divided by the generator, which is what makes every syndrome vanish.
    /// </summary>
    private static int[] GenerateErrorCorrection(List<int> data, int numEc)
    {
        var field = BbModulusGF.Pdf417;

        // The generator polynomial has roots at the first numEc powers of the field generator.
        var generator = field.One;
        for (var i = 1; i <= numEc; i++)
        {
            generator = generator.Multiply(new BbModulusPoly(field, [1, field.Subtract(0, field.Exp(i))]));
        }

        var remainder = new BbModulusPoly(field, [.. data]).MultiplyByMonomial(numEc, 1);
        var divisorLeadingInverse = field.Inverse(generator.GetCoefficient(generator.Degree));

        while (remainder.Degree >= generator.Degree && !remainder.IsZero)
        {
            var degreeDifference = remainder.Degree - generator.Degree;
            var scale = field.Multiply(remainder.GetCoefficient(remainder.Degree), divisorLeadingInverse);
            remainder = remainder.Subtract(generator.MultiplyByMonomial(degreeDifference, scale));
        }

        var result = new int[numEc];
        for (var i = 0; i < numEc; i++)
        {
            result[numEc - 1 - i] = field.Subtract(0, remainder.GetCoefficient(i));
        }

        return result;
    }

    private static BbBitMatrix Render(
        int[] codewords, int rows, int columns, int errorCorrectionLevel, int rowHeight, int quietZone)
    {
        var table = BbPdf417SymbolTable.Current;

        int[] startPattern = [8, 1, 1, 1, 1, 1, 1, 3];
        int[] stopPattern = [7, 1, 1, 3, 1, 1, 1, 2, 1];

        var symbolWidth = 17 + (17 * (columns + 2)) + 18;
        var width = symbolWidth + (2 * quietZone);
        var matrix = new BbBitMatrix(width, (rows * rowHeight) + (2 * quietZone));

        for (var row = 0; row < rows; row++)
        {
            var cluster = (row % 3) * 3;
            var modules = new List<bool>(width);

            AppendWidths(modules, startPattern, startWithBar: true);
            AppendCodeword(modules, table, cluster, LeftIndicator(row, rows, columns, errorCorrectionLevel));

            for (var column = 0; column < columns; column++)
            {
                AppendCodeword(modules, table, cluster, codewords[(row * columns) + column]);
            }

            AppendCodeword(modules, table, cluster, RightIndicator(row, rows, columns, errorCorrectionLevel));
            AppendWidths(modules, stopPattern, startWithBar: true);

            for (var y = 0; y < rowHeight; y++)
            {
                var targetY = (row * rowHeight) + y + quietZone;
                for (var x = 0; x < modules.Count && x < symbolWidth; x++)
                {
                    if (modules[x])
                    {
                        matrix[x + quietZone, targetY] = true;
                    }
                }
            }
        }

        return matrix;
    }

    /// <summary>The left row indicator carries one third of the symbol structure, chosen by the row number.</summary>
    private static int LeftIndicator(int row, int rows, int columns, int errorCorrectionLevel) => (row % 3) switch
    {
        0 => (30 * (row / 3)) + ((rows - 1) / 3),
        1 => (30 * (row / 3)) + (errorCorrectionLevel * 3) + ((rows - 1) % 3),
        _ => (30 * (row / 3)) + (columns - 1),
    };

    /// <summary>The right row indicator carries the other two thirds, rotated by one.</summary>
    private static int RightIndicator(int row, int rows, int columns, int errorCorrectionLevel) => (row % 3) switch
    {
        0 => (30 * (row / 3)) + (columns - 1),
        1 => (30 * (row / 3)) + ((rows - 1) / 3),
        _ => (30 * (row / 3)) + (errorCorrectionLevel * 3) + ((rows - 1) % 3),
    };

    private static void AppendCodeword(List<bool> modules, BbPdf417SymbolTable table, int cluster, int codeword)
    {
        if (!table.TryGetPattern(cluster, codeword, out var pattern))
        {
            throw new InvalidOperationException($"Codeword {codeword} has no pattern in cluster {cluster}.");
        }

        for (var bit = BbPdf417SymbolTable.ModulesPerCodeword - 1; bit >= 0; bit--)
        {
            modules.Add(((pattern >> bit) & 1) != 0);
        }
    }

    private static void AppendWidths(List<bool> modules, ReadOnlySpan<int> widths, bool startWithBar)
    {
        var bar = startWithBar;
        foreach (var width in widths)
        {
            for (var i = 0; i < width; i++)
            {
                modules.Add(bar);
            }

            bar = !bar;
        }
    }
}
