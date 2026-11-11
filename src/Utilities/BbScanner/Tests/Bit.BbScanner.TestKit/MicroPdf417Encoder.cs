namespace Bit.BbScanner.TestKit;

/// <summary>
/// A MicroPDF417 encoder used to generate test and sample symbols.
/// </summary>
/// <remarks>
/// It writes with the same size and row address pattern tables the reader reads with (<c>BbMicroPdf417Tables</c>), and
/// with PDF417's symbol character table and compaction: a round trip checks the reader's geometry, its error correction
/// and the bit stream, not the tables. Those are checked against the symbols BWIPP writes, in the reference fixtures.
/// </remarks>
public static class MicroPdf417Encoder
{
    private const int PadCodeword = 900;

    /// <summary>Encodes text into a MicroPDF417 module grid, its rows two modules tall, with no quiet zone.</summary>
    /// <param name="content">The text; uppercase letters and spaces use text compaction, anything else byte compaction.</param>
    /// <param name="columns">The data columns, 1 to 4, or 0 for the narrowest symbol the content fits.</param>
    /// <param name="minimumRows">The fewest rows, to choose a taller symbol than the content needs.</param>
    /// <param name="macro">Writes a Macro PDF417 control block after the data, as <see cref="Pdf417Encoder.Encode"/> does.</param>
    public static BbBitMatrix Encode(
        string content,
        int columns = 0,
        int minimumRows = 0,
        (int SegmentIndex, int[] FileId, int? SegmentCount, bool Last)? macro = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        // PDF417's data codewords without its symbol length descriptor, which MicroPDF417 does not have.
        var data = Pdf417Encoder.BuildDataCodewords(content);
        data.RemoveAt(0);
        if (macro is { } block)
        {
            Pdf417Encoder.AppendMacro(data, block);
        }

        return EncodeCodewords(data, columns, minimumRows);
    }

    /// <summary>
    /// Writes the given data codewords into a MicroPDF417 module grid as they are - for a stream the text and byte
    /// compaction of <see cref="Encode"/> does not write, such as one opening with a Code 128 emulation codeword.
    /// </summary>
    /// <param name="dataCodewords">The data codewords, without a symbol length descriptor; padded to the symbol's capacity.</param>
    /// <param name="columns">The data columns, 1 to 4, or 0 for the narrowest symbol the codewords fit.</param>
    /// <param name="minimumRows">The fewest rows, to choose a taller symbol than the codewords need.</param>
    public static BbBitMatrix EncodeCodewords(IEnumerable<int> dataCodewords, int columns = 0, int minimumRows = 0)
    {
        ArgumentNullException.ThrowIfNull(dataCodewords);
        ArgumentOutOfRangeException.ThrowIfNegative(columns);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(columns, 4);

        var data = new List<int>(dataCodewords);

        BbMicroPdf417Tables.Variant? chosen = null;
        foreach (var variant in BbMicroPdf417Tables.Variants)
        {
            if ((columns == 0 || variant.Columns == columns) &&
                variant.Rows >= minimumRows &&
                (variant.Columns * variant.Rows) - variant.EcCodewords >= data.Count &&
                (chosen is null || (variant.Columns * variant.Rows) < (chosen.Value.Columns * chosen.Value.Rows)))
            {
                chosen = variant;
            }
        }

        var size = chosen ?? throw new ArgumentException("The codewords do not fit in any MicroPDF417 symbol of that shape.", nameof(dataCodewords));
        var dataCount = (size.Columns * size.Rows) - size.EcCodewords;
        while (data.Count < dataCount)
        {
            data.Add(PadCodeword);
        }

        var codewords = new List<int>(data);
        codewords.AddRange(Pdf417Encoder.GenerateErrorCorrection(data, size.EcCodewords));

        var width = BbMicroPdf417Tables.RowModules(size.Columns);
        var matrix = new BbBitMatrix(width, size.Rows * 2);
        var table = BbPdf417SymbolTable.Specification;
        for (var row = 0; row < size.Rows; row++)
        {
            var x = 0;
            void Put(int pattern, int modules)
            {
                for (var bit = modules - 1; bit >= 0; bit--, x++)
                {
                    if (((pattern >> bit) & 1) != 0)
                    {
                        matrix[x, row * 2] = true;
                        matrix[x, (row * 2) + 1] = true;
                    }
                }
            }

            void Codeword(int column)
            {
                table.TryGetPattern(size.Cluster(row), codewords[(row * size.Columns) + column], out var pattern);
                Put(pattern, BbPdf417SymbolTable.ModulesPerCodeword);
            }

            void Rap(BbMicroPdf417RapKind kind)
            {
                var patterns = kind == BbMicroPdf417RapKind.Centre ? BbMicroPdf417Tables.CentrePatterns : BbMicroPdf417Tables.SidePatterns;
                Put(patterns[size.RapIndex(kind, row)], BbMicroPdf417Tables.RapModules);
            }

            Rap(BbMicroPdf417RapKind.Left);
            for (var column = 0; column < size.Columns; column++)
            {
                // Three and four column symbols carry a centre pattern after their first and second column.
                if (column == size.Columns - 2 && size.Columns >= 3)
                {
                    Rap(BbMicroPdf417RapKind.Centre);
                }

                Codeword(column);
            }

            Rap(BbMicroPdf417RapKind.Right);
            Put(1, 1);
        }

        return matrix;
    }
}
