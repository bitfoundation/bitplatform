namespace Bit.BbScanner;

/// <summary>
/// The PDF417 entry point: row detection, row indicator interpretation, error correction and
/// decoding.
/// </summary>
/// <remarks>
/// <para>
/// The structure of a PDF417 symbol is carried by its row indicators rather than by a header: the
/// first three rows' left indicators between them encode the row count, the security level and
/// the column count. Everything else follows from those.
/// </para>
/// <para>
/// <b>Conformance.</b> This reader is only as correct as the symbol character table it is given.
/// The table that ships with the library is a generated placeholder, so
/// <see cref="BbBarcodeDecoder"/> leaves PDF417 disabled until a specification table is
/// installed through <see cref="BbPdf417SymbolTable.Register"/>. See
/// <see cref="BbPdf417SymbolTable"/> for why the table cannot be derived.
/// </para>
/// </remarks>
public sealed class BbPdf417Reader : IBbMatrixDecoder
{
    private readonly BbPdf417Detector _detector = new();
    private readonly BbPdf417ErrorCorrection _errorCorrection = new();

    /// <inheritdoc />
    public BbBarcodeFormat Formats => BbBarcodeFormat.Pdf417;

    /// <inheritdoc />
    public BbSymbolDecodeResult? Decode(BbBitMatrix image, BbMatrixDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);

        var result = TryDecode(image);
        if (result is not null || !options.AllowInverted)
        {
            return result;
        }

        image.Invert();
        try
        {
            return TryDecode(image);
        }
        finally
        {
            image.Invert();
        }
    }

    private BbSymbolDecodeResult? TryDecode(BbBitMatrix image)
    {
        var table = BbPdf417SymbolTable.Current;
        var rows = _detector.Detect(image, table);

        // Three rows is the smallest legal symbol and is also the minimum needed to read the
        // three pieces of structure the row indicators carry.
        if (rows.Count < 3)
        {
            return null;
        }

        if (!TryReadStructure(rows, out var rowCount, out var columnCount, out var errorCorrectionLevel))
        {
            return null;
        }

        // Rows the detector never saw, and rows it saw with the wrong number of characters,
        // become erasures: the error correction stage decides whether the symbol survives them,
        // which is what the security level is for.
        var codewords = new int[rowCount * columnCount];
        Array.Fill(codewords, BbPdf417Detector.Erasure);
        foreach (var row in rows)
        {
            if (row.RowNumber >= rowCount || row.Codewords.Length != columnCount + 2)
            {
                continue;
            }

            var offset = row.RowNumber * columnCount;
            for (var i = 1; i <= columnCount; i++)
            {
                codewords[offset + i - 1] = row.Codewords[i];
            }
        }

        var numEcCodewords = BbPdf417ErrorCorrection.GetErrorCorrectionCodewordCount(errorCorrectionLevel);
        if (numEcCodewords >= codewords.Length)
        {
            return null;
        }

        var erasures = new List<int>();
        for (var i = 0; i < codewords.Length; i++)
        {
            if (codewords[i] == BbPdf417Detector.Erasure)
            {
                erasures.Add(i);
            }
        }

        if (!_errorCorrection.TryDecode(codewords, numEcCodewords, erasures, out var errorsCorrected))
        {
            return null;
        }

        var dataCodewords = codewords[..(codewords.Length - numEcCodewords)];
        var payload = BbPdf417BitStreamParser.Decode(
            dataCodewords, rowCount, columnCount, errorCorrectionLevel, errorsCorrected);

        if (payload is null)
        {
            return null;
        }

        var first = rows[0];
        var last = rows[^1];
        BbScanPoint[] points =
        [
            new(first.Left, first.Top),
            new(first.Right, first.Top),
            new(last.Right, last.Bottom),
            new(last.Left, last.Bottom),
        ];

        return new BbSymbolDecodeResult(payload, BbBarcodeFormat.Pdf417, points);
    }

    /// <summary>
    /// Reads the row count, column count and security level out of the row indicators.
    /// </summary>
    /// <remarks>
    /// Each row indicator carries one third of the structure, chosen by the row number modulo
    /// three, and the left and right indicators of a row carry different thirds. Reading both
    /// sides of every row gives each value many times over, which is what makes this robust on a
    /// symbol whose edge is partly damaged; an indicator the detector could not read is skipped.
    /// </remarks>
    private static bool TryReadStructure(
        IReadOnlyList<BbPdf417Detector.SymbolRow> rows, out int rowCount, out int columnCount, out int errorCorrectionLevel)
    {
        rowCount = 0;
        columnCount = 0;
        errorCorrectionLevel = -1;

        int? rowsDiv3 = null;
        int? rowsMod3 = null;
        int? level = null;
        int? columns = null;

        foreach (var row in rows)
        {
            var phase = row.RowNumber % 3;
            var left = row.Codewords[0];
            var right = row.Codewords[^1];

            if (left != BbPdf417Detector.Erasure)
            {
                Apply(phase, left % 30, isLeft: true);
            }

            if (right != BbPdf417Detector.Erasure)
            {
                Apply(phase, right % 30, isLeft: false);
            }

            if (rowsDiv3 is not null && rowsMod3 is not null && level is not null && columns is not null)
            {
                break;
            }
        }

        if (rowsDiv3 is null || rowsMod3 is null || level is null || columns is null)
        {
            return false;
        }

        rowCount = (3 * rowsDiv3.Value) + rowsMod3.Value + 1;
        columnCount = columns.Value + 1;
        errorCorrectionLevel = level.Value;

        return rowCount is >= 3 and <= 90
            && columnCount is >= 1 and <= 30
            && errorCorrectionLevel is >= 0 and <= 8;

        // The left indicator of phase p carries field p; the right indicator carries the field
        // two rows along, so between them each row holds two of the three fields.
        void Apply(int phase, int value, bool isLeft)
        {
            var field = isLeft ? phase : (phase + 2) % 3;
            switch (field)
            {
                case 0:
                    rowsDiv3 ??= value;
                    break;
                case 1:
                    level ??= value / 3;
                    rowsMod3 ??= value % 3;
                    break;
                default:
                    columns ??= value;
                    break;
            }
        }
    }
}
