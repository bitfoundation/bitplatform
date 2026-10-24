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
/// <b>Conformance.</b> This reader is only as correct as the symbol character table it is given,
/// which is <see cref="BbPdf417SymbolTable.Current"/>: the specification table unless an
/// application registered another. See <see cref="BbPdf417SymbolTable"/> for why the table is
/// data rather than a rule.
/// </para>
/// </remarks>
public sealed class BbPdf417Reader : IBbMatrixDecoder, IBbLuminanceMatrixDecoder, IBbImageHolder
{
    private readonly BbPdf417Detector _detector = new();
    private readonly BbPdf417ErrorCorrection _errorCorrection = new();
    private readonly BbPdf417Locator _locator = new();

    /// <inheritdoc />
    void IBbImageHolder.ForgetImage() => _locator.ForgetImage();

    /// <inheritdoc />
    public BbBarcodeFormat Formats => BbBarcodeFormat.Pdf417;

    /// <inheritdoc />
    /// <remarks>
    /// Without the luminance plane the binarised image came from, a rotated symbol is resampled
    /// from the binarised image itself, which reads slightly smaller modules less reliably.
    /// </remarks>
    public BbSymbolDecodeResult? Decode(BbBitMatrix image, BbMatrixDecodeOptions options) => Decode(image, default, options);

    /// <inheritdoc />
    BbSymbolDecodeResult? IBbLuminanceMatrixDecoder.Decode(BbBitMatrix image, in BbLuminanceView luminance, BbMatrixDecodeOptions options) =>
        Decode(image, luminance, options);

    private BbSymbolDecodeResult? Decode(BbBitMatrix image, in BbLuminanceView luminance, BbMatrixDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);

        var result = DecodeAtAnyOrientation(image, luminance, options.TryHarder, inverted: false);
        if (result is not null || !options.AllowInverted)
        {
            return result;
        }

        image.Invert();
        try
        {
            return DecodeAtAnyOrientation(image, luminance, options.TryHarder, inverted: true);
        }
        finally
        {
            image.Invert();
        }
    }

    /// <summary>
    /// Locates the symbols in the frame, resamples each one upright and reads it, mapping the
    /// corners it reports back onto the frame.
    /// </summary>
    private BbSymbolDecodeResult? DecodeAtAnyOrientation(BbBitMatrix image, in BbLuminanceView luminance, bool thorough, bool inverted)
    {
        // Resampling a candidate upright and reading every row of it costs about a hundred nanoseconds a pixel, and
        // a pair of start and stop shaped stripes - a poster, a crafted file - is a candidate like any real symbol. So
        // the candidates of a frame share a budget: a frame and a half of pixels live, which bounds a frame's PDF417
        // work to tens of milliseconds whatever it shows, and eight frames' worth for a still image. A small image
        // still gets the half megapixel - or four, still - that resampling a small symbol at three pixels a module
        // can take, since small modules are exactly what the resampling is for.
        var budget = thorough
            ? Math.Max((long)image.Width * image.Height * 8, 4L * 1024 * 1024)
            : Math.Max((long)image.Width * image.Height * 3 / 2, 512L * 1024);
        foreach (var candidate in _locator.Locate(image, thorough))
        {
            if (budget <= 0)
            {
                break;
            }

            using var upright = _locator.Rectify(candidate, luminance, image, inverted, budget, out var toImage);
            if (upright is null)
            {
                continue;
            }

            budget -= (long)upright.Width * upright.Height;

            var result = TryDecode(upright);
            if (result is null)
            {
                continue;
            }

            var points = new BbScanPoint[result.Points.Length];
            for (var i = 0; i < points.Length; i++)
            {
                points[i] = toImage(result.Points[i]);
            }

            return result with { Points = points };
        }

        // A still image is worth one more look: an upright symbol whose start pattern is too
        // damaged to locate can still have enough intact rows to read where it lies.
        return thorough ? TryDecode(image) : null;
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
    /// sides of every row gives each value many times over, and every reading votes: a single
    /// indicator misread as another valid codeword is outvoted by the rest instead of deciding
    /// the symbol's shape. An indicator the detector could not read is skipped.
    /// </remarks>
    private static bool TryReadStructure(
        IReadOnlyList<BbPdf417Detector.SymbolRow> rows, out int rowCount, out int columnCount, out int errorCorrectionLevel)
    {
        rowCount = 0;
        columnCount = 0;
        errorCorrectionLevel = -1;

        // One tally per field, each over the thirty values an indicator's remainder can take.
        Span<int> votes = stackalloc int[3 * 30];
        votes.Clear();

        foreach (var row in rows)
        {
            var phase = row.RowNumber % 3;
            var left = row.Codewords[0];
            var right = row.Codewords[^1];

            // The left indicator of phase p carries field p; the right indicator carries the
            // field two rows along, so between them each row holds two of the three fields.
            if (left != BbPdf417Detector.Erasure)
            {
                votes[(phase * 30) + (left % 30)]++;
            }

            if (right != BbPdf417Detector.Erasure)
            {
                votes[(((phase + 2) % 3) * 30) + (right % 30)]++;
            }
        }

        var rowsDiv3 = Winner(votes[..30]);
        var levelAndRowsMod3 = Winner(votes.Slice(30, 30));
        var columns = Winner(votes.Slice(60, 30));
        if (rowsDiv3 < 0 || levelAndRowsMod3 < 0 || columns < 0)
        {
            return false;
        }

        rowCount = (3 * rowsDiv3) + (levelAndRowsMod3 % 3) + 1;
        columnCount = columns + 1;
        errorCorrectionLevel = levelAndRowsMod3 / 3;

        return rowCount is >= 3 and <= 90
            && columnCount is >= 1 and <= 30
            && errorCorrectionLevel is >= 0 and <= 8;

        static int Winner(ReadOnlySpan<int> tally)
        {
            var best = -1;
            var bestCount = 0;
            for (var value = 0; value < tally.Length; value++)
            {
                if (tally[value] > bestCount)
                {
                    best = value;
                    bestCount = tally[value];
                }
            }

            return best;
        }
    }
}
