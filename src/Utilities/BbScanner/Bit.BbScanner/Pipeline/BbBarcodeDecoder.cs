using System.Buffers;
using System.Diagnostics;

namespace Bit.BbScanner;

/// <summary>
/// The decoding pipeline: takes a frame of pixels and returns the barcode it contains.
/// </summary>
/// <remarks>
/// <para>
/// This is the whole library minus the browser. It has no dependency on Blazor, JavaScript or a
/// camera, which is what lets it be unit tested, benchmarked and reused from a server or a
/// desktop application.
/// </para>
/// <para>
/// One instance owns all the scratch state a frame needs: the luminance plane, the packed scan
/// line, the detectors and the per-symbology decoders. After the first frame, decoding a frame
/// that contains no barcode allocates nothing at all on the linear path, and on the matrix path
/// only the bit plane's wrapper object, whose buffer is pooled. Nothing allocated per frame
/// scales with the work done, which is what keeps continuous scanning free of garbage collection
/// pauses. An instance is therefore not thread safe; use one per scanning session.
/// </para>
/// </remarks>
public sealed class BbBarcodeDecoder : IDisposable
{
    private readonly BbScannerOptions _options;
    private readonly List<IBbRowDecoder> _rowDecoders = [];
    private readonly List<IBbMatrixDecoder> _matrixDecoders = [];
    private readonly BbHybridBinarizer _matrixBinarizer = new();
    private readonly BbAdaptiveRowBinarizer _rowBinarizer = new();
    private readonly BbDuplicateFilter _duplicateFilter;

    private readonly List<BbSymbolDecodeResult> _symbols = [];

    private BbLuminanceBuffer? _grayscale;
    private BbLuminanceBuffer? _masked;
    private BbBitRow? _scanLine;
    private BbBitRow? _confirmLine;
    private BbBitRow? _followLine;

    /// <summary>How many times this image's lines have been followed up; see <see cref="FollowUp"/>.</summary>
    private int _followUps;

    /// <summary>
    /// The most follow-ups an image gets: each is up to a hundred lines - forty parallel, sixteen turned and forty along
    /// the axis those find - and noise can ask for them too.
    /// </summary>
    private const int MaxFollowUps = 4;

    /// <summary>
    /// How far, in degrees, a follow-up turns its lines about a part's middle either way, and in what steps: a half of a
    /// DataBar Stacked row, five modules tall, is crossed whole within six degrees of its axis through its middle.
    /// </summary>
    private const int MaxFollowTurn = 16;

    private const int FollowTurnStep = 2;
    private byte[] _lineBuffer = [];
    private bool _disposed;

    /// <summary>Creates a decoder.</summary>
    /// <param name="options">Tuning; a copy is taken, so later edits to the instance are ignored.</param>
    public BbBarcodeDecoder(BbScannerOptions? options = null)
    {
        options ??= new BbScannerOptions();
        options.Validate();
        _options = options.Clone();
        _duplicateFilter = new BbDuplicateFilter(_options.DuplicateSuppressionWindow);

        // Only the decoders that can produce a requested format are instantiated, so a scanner
        // configured for a single symbology carries no cost for the others.
        // One reader for QR Code and Micro QR: they share the finder pattern search.
        if ((_options.Formats & (BbBarcodeFormat.QrCode | BbBarcodeFormat.MicroQrCode)) != 0)
        {
            _matrixDecoders.Add(new BbQrCodeReader(_options.Formats));
        }

        if ((_options.Formats & BbBarcodeFormat.DataMatrix) != 0)
        {
            _matrixDecoders.Add(new BbDataMatrixReader());
        }

        // The specification symbol character table ships with the library; an application that
        // registers a table of its own that does not claim conformance turns PDF417 off rather
        // than reading symbols with it.
        if ((_options.Formats & BbBarcodeFormat.Pdf417) != 0 && BbPdf417SymbolTable.Current.IsSpecificationTable)
        {
            _matrixDecoders.Add(new BbPdf417Reader());
        }

        if ((_options.Formats & BbBarcodeFormat.Aztec) != 0)
        {
            _matrixDecoders.Add(new BbAztecReader());
        }

        if ((_options.Formats & BbBarcodeFormat.MaxiCode) != 0)
        {
            _matrixDecoders.Add(new BbMaxiCodeReader());
        }

        if ((_options.Formats & BbBarcodeFormat.Code128) != 0)
        {
            _rowDecoders.Add(new BbCode128Reader());
        }

        if ((_options.Formats & BbBarcodeFormat.Code39) != 0)
        {
            _rowDecoders.Add(new BbCode39Reader(_options.Code39CheckDigit, _options.Code39ExtendedMode));
        }

        if ((_options.Formats & (BbBarcodeFormat.Ean13 | BbBarcodeFormat.Ean8 | BbBarcodeFormat.UpcA | BbBarcodeFormat.UpcE)) != 0)
        {
            _rowDecoders.Add(new BbEanUpcReader(_options.RequireEanExtension));
        }

        if ((_options.Formats & BbBarcodeFormat.Itf) != 0)
        {
            _rowDecoders.Add(new BbItfReader(_options.ItfLengths));
        }

        if ((_options.Formats & BbBarcodeFormat.Code93) != 0)
        {
            _rowDecoders.Add(new BbCode93Reader());
        }

        if ((_options.Formats & BbBarcodeFormat.Codabar) != 0)
        {
            _rowDecoders.Add(new BbCodabarReader(_options.CodabarStartStop));
        }

        if ((_options.Formats & BbBarcodeFormat.DataBar) != 0)
        {
            _rowDecoders.Add(new BbDataBarReader());
        }

        if ((_options.Formats & BbBarcodeFormat.DataBarLimited) != 0)
        {
            _rowDecoders.Add(new BbDataBarLimitedReader());
        }

        if ((_options.Formats & BbBarcodeFormat.DataBarExpanded) != 0)
        {
            _rowDecoders.Add(new BbDataBarExpandedReader());
        }
    }

    /// <summary>The options this decoder was created with.</summary>
    public BbScannerOptions Options => _options;

    /// <summary>
    /// How long the same value is suppressed after being reported. Can be changed while running.
    /// </summary>
    public TimeSpan DuplicateSuppressionWindow
    {
        get => _duplicateFilter.Window;
        set
        {
            if (value != Timeout.InfiniteTimeSpan)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value.Ticks);
            }

            _options.DuplicateSuppressionWindow = value;
            _duplicateFilter.Window = value;
        }
    }

    /// <summary>Timings from the most recently decoded frame.</summary>
    public BbScanDiagnostics LastFrame { get; private set; }

    /// <summary>Number of frames decoded since the decoder was created.</summary>
    public long FramesDecoded { get; private set; }

    /// <summary>Number of frames that produced a result, before duplicate suppression.</summary>
    public long FramesWithSymbol { get; private set; }

    /// <summary>Forgets the reported values so that scanning one again reports immediately.</summary>
    public void ResetDuplicateFilter() => _duplicateFilter.Reset();

    /// <summary>
    /// Decodes a frame of packed pixels.
    /// </summary>
    /// <param name="pixels">The frame, row major and tightly packed.</param>
    /// <param name="width">Frame width in pixels.</param>
    /// <param name="height">Frame height in pixels.</param>
    /// <param name="format">Layout of <paramref name="pixels"/>.</param>
    /// <param name="suppressDuplicates">When <see langword="false"/>, the duplicate filter is bypassed.</param>
    /// <returns>The decoded barcode, or <see langword="null"/>.</returns>
    public BbBarcodeResult? Decode(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        BbPixelFormat format,
        bool suppressDuplicates = true)
    {
        var start = Stopwatch.GetTimestamp();
        var view = Prepare(pixels, width, height, format, out var cropX, out var cropY, out var step);
        var afterGrayscale = Stopwatch.GetTimestamp();

        return DecodeCore(view, cropX, cropY, step, start, afterGrayscale, width, height, suppressDuplicates, maxSymbols: 1, results: null);
    }

    /// <summary>
    /// Decodes every symbol in a frame of packed pixels, up to <paramref name="maxSymbols"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A matrix symbol, once read, is painted out of a working copy of the frame and the frame is
    /// searched again, so each pass finds the next one; linear symbols are collected across every
    /// scan line of a single pass, as distinct values. Two copies of the same code in different
    /// places are reported as two results.
    /// </para>
    /// <para>
    /// A frame holding one symbol costs one more matrix pass than <see cref="Decode(ReadOnlySpan{byte}, int, int, BbPixelFormat, bool)"/>,
    /// the pass that establishes there is nothing else; a frame holding none costs the same.
    /// </para>
    /// </remarks>
    /// <param name="pixels">The frame, row major and tightly packed.</param>
    /// <param name="width">Frame width in pixels.</param>
    /// <param name="height">Frame height in pixels.</param>
    /// <param name="format">Layout of <paramref name="pixels"/>.</param>
    /// <param name="maxSymbols">The most symbols to report, at least one.</param>
    /// <param name="suppressDuplicates">When <see langword="false"/>, the duplicate filter is bypassed.</param>
    /// <returns>The symbols found, matrix symbols first, in the order they were found; empty when there are none.</returns>
    public IReadOnlyList<BbBarcodeResult> DecodeAll(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        BbPixelFormat format,
        int maxSymbols = 16,
        bool suppressDuplicates = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSymbols);

        var start = Stopwatch.GetTimestamp();
        var view = Prepare(pixels, width, height, format, out var cropX, out var cropY, out var step);
        var afterGrayscale = Stopwatch.GetTimestamp();

        var results = new List<BbBarcodeResult>();
        DecodeCore(view, cropX, cropY, step, start, afterGrayscale, width, height, suppressDuplicates, maxSymbols, results);
        return results;
    }

    /// <summary>
    /// Converts the searched region of a frame of packed pixels into the luminance plane every
    /// later stage reads, cropped and subsampled.
    /// </summary>
    private BbLuminanceView Prepare(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        BbPixelFormat format,
        out int cropX,
        out int cropY,
        out int step)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        if (width < 8 || height < 8)
        {
            throw new ArgumentException("The frame is too small to decode.", nameof(pixels));
        }

        // The region is cut out before the conversion rather than after it: converting is the one
        // stage that touches every pixel, so a region a quarter of the frame makes it a quarter of
        // the work too.
        (cropX, cropY, var cropWidth, var cropHeight) = _options.Region.ToPixels(width, height);

        // A factor the frame, or the region, is too small for would leave nothing to decode, so
        // it is lowered to the largest one that still leaves eight pixels a side.
        step = Math.Clamp(Math.Min(cropWidth, cropHeight) / 8, 1, _options.DownsampleFactor);
        var targetWidth = cropWidth / step;
        var targetHeight = cropHeight / step;

        if (_grayscale is null)
        {
            _grayscale = BbLuminanceBuffer.Rent(targetWidth, targetHeight);
        }
        else
        {
            _grayscale.Resize(targetWidth, targetHeight);
        }

        var bpp = BbPixelConverter.BytesPerPixel(format);
        var stride = width * bpp;
        BbPixelConverter.ToGrayscaleDownsampled(
            pixels[((cropY * stride) + (cropX * bpp))..],
            _grayscale.Pixels,
            cropWidth,
            cropHeight,
            format,
            step,
            stride);

        return _grayscale.View;
    }

    /// <summary>
    /// Decodes a frame that is already 8 bit grayscale.
    /// </summary>
    /// <param name="view">The grayscale frame.</param>
    /// <param name="suppressDuplicates">When <see langword="false"/>, the duplicate filter is bypassed.</param>
    public BbBarcodeResult? Decode(in BbLuminanceView view, bool suppressDuplicates = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var start = Stopwatch.GetTimestamp();
        var (x, y, width, height) = _options.Region.ToPixels(view.Width, view.Height);
        var cropped = x == 0 && y == 0 && width == view.Width && height == view.Height ? view : view.Crop(x, y, width, height);
        return DecodeCore(cropped, x, y, 1, start, start, view.Width, view.Height, suppressDuplicates, maxSymbols: 1, results: null);
    }

    /// <summary>
    /// Decodes every symbol in a frame that is already 8 bit grayscale, up to
    /// <paramref name="maxSymbols"/>; see <see cref="DecodeAll(ReadOnlySpan{byte}, int, int, BbPixelFormat, int, bool)"/>.
    /// </summary>
    /// <param name="view">The grayscale frame.</param>
    /// <param name="maxSymbols">The most symbols to report, at least one.</param>
    /// <param name="suppressDuplicates">When <see langword="false"/>, the duplicate filter is bypassed.</param>
    /// <returns>The symbols found, matrix symbols first, in the order they were found; empty when there are none.</returns>
    public IReadOnlyList<BbBarcodeResult> DecodeAll(in BbLuminanceView view, int maxSymbols = 16, bool suppressDuplicates = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSymbols);

        var start = Stopwatch.GetTimestamp();
        var (x, y, width, height) = _options.Region.ToPixels(view.Width, view.Height);
        var cropped = x == 0 && y == 0 && width == view.Width && height == view.Height ? view : view.Crop(x, y, width, height);
        var results = new List<BbBarcodeResult>();
        DecodeCore(cropped, x, y, 1, start, start, view.Width, view.Height, suppressDuplicates, maxSymbols, results);
        return results;
    }

    /// <summary>
    /// Decodes the searched region, already cropped and downsampled into <paramref name="view"/>,
    /// whose top-left corner sits at <paramref name="offsetX"/>, <paramref name="offsetY"/> of the
    /// caller's frame and whose every pixel spans <paramref name="step"/> frame pixels.
    /// </summary>
    /// <param name="view">The searched region, cropped and downsampled.</param>
    /// <param name="offsetX">Where the region's left edge sits in the caller's frame.</param>
    /// <param name="offsetY">Where the region's top edge sits in the caller's frame.</param>
    /// <param name="step">Frame pixels per region pixel.</param>
    /// <param name="start">When decoding of the frame began.</param>
    /// <param name="afterGrayscale">When the luminance plane was ready.</param>
    /// <param name="frameWidth">The caller's frame width.</param>
    /// <param name="frameHeight">The caller's frame height.</param>
    /// <param name="suppressDuplicates">Whether the duplicate filter applies.</param>
    /// <param name="maxSymbols">The most symbols to collect into <paramref name="results"/>.</param>
    /// <param name="results">
    /// <see langword="null"/> to stop at the first symbol and return it; otherwise every symbol
    /// found is added here, up to <paramref name="maxSymbols"/>, and the first is returned too.
    /// </param>
    private BbBarcodeResult? DecodeCore(
        in BbLuminanceView view,
        int offsetX,
        int offsetY,
        int step,
        long start,
        long afterGrayscale,
        int frameWidth,
        int frameHeight,
        bool suppressDuplicates,
        int maxSymbols,
        List<BbBarcodeResult>? results)
    {
        FramesDecoded++;

        var binarizeTicks = 0L;
        var matrixTicks = 0L;
        var linearTicks = 0L;
        var rowsScanned = 0;

        // Every symbol of the frame, in the coordinates of the searched region. A single symbol
        // is the common case and needs no list at all.
        BbSymbolDecodeResult? first = null;
        var symbols = results is null ? null : _symbols;
        symbols?.Clear();
        var searched = view;

        if (_matrixDecoders.Count > 0 && view.Width >= 16 && view.Height >= 16)
        {
            if (symbols is null)
            {
                first = DecodeMatrix(view, ref binarizeTicks, ref matrixTicks);
            }
            else
            {
                // Each symbol found is painted out of a working copy of the frame, so the next
                // pass finds the next symbol rather than the same one again. A pass that does
                // find the same one - a symbol painting did not fully cover - still counts, so
                // the loop ends whatever the frame holds.
                var working = CopyForMasking(view);
                for (var pass = 0; pass < maxSymbols + 2 && symbols.Count < maxSymbols; pass++)
                {
                    var symbol = DecodeMatrix(working, ref binarizeTicks, ref matrixTicks);
                    if (symbol is null)
                    {
                        break;
                    }

                    if (!IsRepeat(symbols, symbol))
                    {
                        symbols.Add(symbol);
                    }

                    PaintOut(working, symbol.Points);
                }

                searched = working;
            }
        }

        if (_rowDecoders.Count > 0 && (symbols is null ? first is null : symbols.Count < maxSymbols))
        {
            var beforeLinear = Stopwatch.GetTimestamp();
            var symbol = ScanRows(searched, out rowsScanned, symbols, maxSymbols);
            if (symbols is null)
            {
                first = symbol;
            }

            linearTicks = Stopwatch.GetTimestamp() - beforeLinear;
        }

        var found = symbols is null ? (first is null ? 0 : 1) : symbols.Count;
        var total = Stopwatch.GetTimestamp() - start;
        LastFrame = new BbScanDiagnostics
        {
            DecodedWidth = searched.Width,
            DecodedHeight = searched.Height,
            Total = Stopwatch.GetElapsedTime(0, total),
            Grayscale = Stopwatch.GetElapsedTime(start, afterGrayscale),
            Binarize = Stopwatch.GetElapsedTime(0, binarizeTicks),
            Matrix = Stopwatch.GetElapsedTime(0, matrixTicks),
            Linear = Stopwatch.GetElapsedTime(0, linearTicks),
            RowsScanned = rowsScanned,
            Decoded = found > 0,
        };

        if (found == 0)
        {
            return null;
        }

        FramesWithSymbol++;

        var timestamp = Environment.TickCount64;
        if (symbols is null)
        {
            return suppressDuplicates && !_duplicateFilter.ShouldReport(first!.Payload.Text, first.Format, timestamp)
                ? null
                : ToResult(first!, offsetX, offsetY, step, frameWidth, frameHeight);
        }

        foreach (var symbol in symbols)
        {
            if (!suppressDuplicates || _duplicateFilter.ShouldReport(symbol.Payload.Text, symbol.Format, timestamp))
            {
                results!.Add(ToResult(symbol, offsetX, offsetY, step, frameWidth, frameHeight));
            }
        }

        symbols.Clear();
        return results!.Count > 0 ? results[0] : null;
    }

    /// <summary>Binarises the view and runs the matrix decoders on it until one reads a symbol.</summary>
    private BbSymbolDecodeResult? DecodeMatrix(in BbLuminanceView view, ref long binarizeTicks, ref long matrixTicks)
    {
        var beforeBinarize = Stopwatch.GetTimestamp();
        using var binary = _matrixBinarizer.GetBlackMatrix(view);
        var afterBinarize = Stopwatch.GetTimestamp();
        binarizeTicks += afterBinarize - beforeBinarize;

        BbSymbolDecodeResult? symbol = null;
        if (binary is not null)
        {
            var matrixOptions = new BbMatrixDecodeOptions(_options.TryHarder, _options.AllowInverted);
            foreach (var decoder in _matrixDecoders)
            {
                symbol = decoder is IBbLuminanceMatrixDecoder resampling
                    ? resampling.Decode(binary, view, matrixOptions)
                    : decoder.Decode(binary, matrixOptions);
                if (symbol is not null)
                {
                    break;
                }
            }
        }

        matrixTicks += Stopwatch.GetTimestamp() - afterBinarize;
        return symbol ?? DecodeEnlarged(view, ref binarizeTicks, ref matrixTicks);
    }

    /// <summary>The symbologies a second look at an enlarged copy of the frame helps; see <see cref="DecodeEnlarged"/>.</summary>
    private const BbBarcodeFormat EnlargedFormats = BbBarcodeFormat.QrCode | BbBarcodeFormat.MicroQrCode | BbBarcodeFormat.DataMatrix | BbBarcodeFormat.Aztec;

    /// <summary>The most pixels of a still image, before doubling, its second look takes: the middle of a larger one.</summary>
    private const int MaxEnlargedStillArea = 1_000_000;

    /// <summary>The enlarged copy of part of the frame; see <see cref="DecodeEnlarged"/>.</summary>
    private BbLuminanceBuffer? _enlarged;

    /// <summary>Counts the live frames whose matrix stage found nothing, for <see cref="DecodeEnlarged"/>.</summary>
    private int _enlargeTurn;

    /// <summary>
    /// Tries the QR Code, Data Matrix and Aztec readers again on part of the frame enlarged twice, when the frame itself
    /// gave them nothing: all of a still image up to a megapixel, and the middle of a larger one; on a live frame, every
    /// fourth one, the middle quarter of it.
    /// </summary>
    /// <remarks>
    /// At two pixels a module, turned, a finder pattern's runs come out of the binariser a pixel too wide or too narrow
    /// on nearly every line, and the finders, which take a run for a module only within half a module of its width,
    /// find nothing. Enlarged by interpolation, the same symbol is four pixels a module with edges the binariser places
    /// within a pixel: every QR reference symbol up to 57 modules reads at every one of 51 angles that way, where some
    /// fifth of the angles failed before. PDF417 resamples the luminance of a candidate itself, and the linear readers
    /// have their own lines, so they are not tried again. A live frame looks again only every fourth time, and only
    /// where a user aims, so that the second look costs a frame with nothing in it a quarter of a frame's matrix stage.
    /// </remarks>
    private BbSymbolDecodeResult? DecodeEnlarged(in BbLuminanceView view, ref long binarizeTicks, ref long matrixTicks)
    {
        if ((_options.Formats & EnlargedFormats) == 0 || (!_options.TryHarder && (_enlargeTurn++ & 3) != 3))
        {
            return null;
        }

        var (width, height) = _options.TryHarder
            ? Fit(view.Width, view.Height, MaxEnlargedStillArea)
            : (view.Width / 2, view.Height / 2);
        if (width < 16 || height < 16)
        {
            return null;
        }

        var (left, top) = ((view.Width - width) / 2, (view.Height - height) / 2);
        if (_enlarged is null || _enlarged.Width != 2 * width || _enlarged.Height != 2 * height)
        {
            _enlarged?.Dispose();
            _enlarged = BbLuminanceBuffer.Rent(2 * width, 2 * height);
        }

        var beforeBinarize = Stopwatch.GetTimestamp();
        Enlarge(view, left, top, width, height, _enlarged);
        var enlarged = _enlarged.View;
        BbSymbolDecodeResult? symbol = null;
        try
        {
            using var binary = _matrixBinarizer.GetBlackMatrix(enlarged);
            var afterBinarize = Stopwatch.GetTimestamp();
            binarizeTicks += afterBinarize - beforeBinarize;
            if (binary is not null)
            {
                var matrixOptions = new BbMatrixDecodeOptions(_options.TryHarder, _options.AllowInverted);
                foreach (var decoder in _matrixDecoders)
                {
                    if ((decoder.Formats & EnlargedFormats) == 0)
                    {
                        continue;
                    }

                    symbol = decoder is IBbLuminanceMatrixDecoder resampling
                        ? resampling.Decode(binary, enlarged, matrixOptions)
                        : decoder.Decode(binary, matrixOptions);
                    if (symbol is not null)
                    {
                        break;
                    }
                }
            }

            matrixTicks += Stopwatch.GetTimestamp() - afterBinarize;
        }
        finally
        {
            // What the copy holds is the frame's.
            _enlarged.Clear();
        }

        if (symbol is null)
        {
            return null;
        }

        // A point of the enlarged copy is twice as far from the part's corner as the same point of the frame.
        var points = new BbScanPoint[symbol.Points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            points[i] = new BbScanPoint(left + (symbol.Points[i].X / 2), top + (symbol.Points[i].Y / 2));
        }

        return symbol with { Points = points };

        // The largest part of the frame, its proportions kept, with no more than area pixels.
        static (int Width, int Height) Fit(int width, int height, int area)
        {
            if ((long)width * height <= area)
            {
                return (width, height);
            }

            var shrink = MathF.Sqrt(area / ((float)width * height));
            return ((int)(width * shrink), (int)(height * shrink));
        }
    }

    /// <summary>
    /// Writes the <paramref name="width"/> by <paramref name="height"/> part of <paramref name="view"/> whose top-left is
    /// at (<paramref name="left"/>, <paramref name="top"/>) into <paramref name="target"/>, twice as wide and twice as
    /// tall, interpolated: each new pixel three parts of the old one it falls in and one of the neighbour it is nearer,
    /// across and down, which is bilinear interpolation at exactly twice the size.
    /// </summary>
    private static void Enlarge(in BbLuminanceView view, int left, int top, int width, int height, BbLuminanceBuffer target)
    {
        var doubled = 2 * width;
        var rows = ArrayPool<int>.Shared.Rent(3 * doubled);
        try
        {
            // Three rows doubled across in turn - the one above, this one and the one below - the edges repeating.
            var all = rows.AsSpan(0, 3 * doubled);
            var output = target.Pixels;
            var (above, at, below) = (0, doubled, 2 * doubled);
            Across(view, left, top, width, all.Slice(at, doubled));
            all.Slice(at, doubled).CopyTo(all.Slice(above, doubled));
            for (var y = 0; y < height; y++)
            {
                Across(view, left, top + Math.Min(y + 1, height - 1), width, all.Slice(below, doubled));
                var up = all.Slice(above, doubled);
                var here = all.Slice(at, doubled);
                var down = all.Slice(below, doubled);
                var upper = output.Slice(2 * y * doubled, doubled);
                var lower = output.Slice(((2 * y) + 1) * doubled, doubled);
                for (var x = 0; x < doubled; x++)
                {
                    upper[x] = (byte)(((3 * here[x]) + up[x] + 8) >> 4);
                    lower[x] = (byte)(((3 * here[x]) + down[x] + 8) >> 4);
                }

                (above, at, below) = (at, below, above);
            }
        }
        finally
        {
            // What the rows hold is the frame's.
            ArrayPool<int>.Shared.Return(rows, clearArray: true);
        }

        // One row doubled across, each value four times the pixel it stands for, so that the second pass divides once.
        static void Across(in BbLuminanceView view, int left, int y, int width, Span<int> doubled)
        {
            var row = view.GetRow(y).Slice(left, width);
            for (var x = 0; x < width; x++)
            {
                var (before, here, after) = (row[Math.Max(x - 1, 0)], row[x], row[Math.Min(x + 1, width - 1)]);
                doubled[2 * x] = (3 * here) + before;
                doubled[(2 * x) + 1] = (3 * here) + after;
            }
        }
    }

    /// <summary>
    /// Whether a symbol is one already found again: the same value from the same symbology,
    /// centred inside the outline of the earlier one.
    /// </summary>
    private static bool IsRepeat(List<BbSymbolDecodeResult> found, BbSymbolDecodeResult symbol)
    {
        var center = BbScanRect.FromPoints(symbol.Points).Center;
        foreach (var earlier in found)
        {
            if (earlier.Format == symbol.Format &&
                string.Equals(earlier.Payload.Text, symbol.Payload.Text, StringComparison.Ordinal) &&
                (earlier.Points.Length < 3 || IsInsideConvex(center, earlier.Points)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Copies the searched region into the scratch plane symbols are painted out of.</summary>
    private BbLuminanceView CopyForMasking(in BbLuminanceView view)
    {
        if (_masked is null)
        {
            _masked = BbLuminanceBuffer.Rent(view.Width, view.Height);
        }
        else
        {
            _masked.Resize(view.Width, view.Height);
        }

        var target = _masked.Pixels;
        for (var y = 0; y < view.Height; y++)
        {
            view.GetRow(y).CopyTo(target.Slice(y * view.Width, view.Width));
        }

        return _masked.View;
    }

    /// <summary>
    /// Paints a matrix symbol out of the working plane: its outline, grown about its centre by an
    /// eighth and a few pixels so that the outermost modules and the edges the binariser would
    /// still see go with it, is filled with white.
    /// </summary>
    private void PaintOut(in BbLuminanceView working, BbScanPoint[] corners)
    {
        if (corners.Length < 3)
        {
            return;
        }

        var center = BbScanRect.FromPoints(corners).Center;
        Span<BbScanPoint> grown = stackalloc BbScanPoint[corners.Length];
        for (var i = 0; i < corners.Length; i++)
        {
            var dx = corners[i].X - center.X;
            var dy = corners[i].Y - center.Y;
            var length = MathF.Sqrt((dx * dx) + (dy * dy));
            var scale = length > 0 ? (1.125f + (3f / length)) : 1;
            grown[i] = new BbScanPoint(center.X + (dx * scale), center.Y + (dy * scale));
        }

        var box = BbScanRect.FromPoints(grown);
        var left = Math.Max(0, (int)MathF.Floor(box.X));
        var top = Math.Max(0, (int)MathF.Floor(box.Y));
        var right = Math.Min(working.Width - 1, (int)MathF.Ceiling(box.Right));
        var bottom = Math.Min(working.Height - 1, (int)MathF.Ceiling(box.Bottom));
        var pixels = _masked!.Pixels;
        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                if (IsInsideConvex(new BbScanPoint(x + 0.5f, y + 0.5f), grown))
                {
                    pixels[(y * working.Width) + x] = 255;
                }
            }
        }
    }

    /// <summary>Whether a point lies inside a convex polygon whose vertices go round in either direction.</summary>
    private static bool IsInsideConvex(BbScanPoint point, ReadOnlySpan<BbScanPoint> polygon)
    {
        var sign = 0;
        for (var i = 0; i < polygon.Length; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Length];
            var cross = ((b.X - a.X) * (point.Y - a.Y)) - ((b.Y - a.Y) * (point.X - a.X));
            if (cross == 0)
            {
                continue;
            }

            var side = cross > 0 ? 1 : -1;
            if (sign != 0 && side != sign)
            {
                return false;
            }

            sign = side;
        }

        return true;
    }

    /// <summary>
    /// Maps a symbol from the coordinates of the searched region - cropped and downsampled - back
    /// into the coordinates of the frame the caller handed in.
    /// </summary>
    private static BbBarcodeResult ToResult(BbSymbolDecodeResult symbol, int offsetX, int offsetY, int step, int frameWidth, int frameHeight)
    {
        var points = new BbScanPoint[symbol.Points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            points[i] = new BbScanPoint(
                (symbol.Points[i].X * step) + offsetX,
                (symbol.Points[i].Y * step) + offsetY);
        }

        return new BbBarcodeResult(
            symbol.Payload.Text,
            symbol.Payload.RawBytes,
            symbol.Format,
            points,
            symbol.Payload.ToMetadata(),
            DateTimeOffset.UtcNow)
        {
            FrameWidth = frameWidth,
            FrameHeight = frameHeight,
        };
    }

    /// <summary>
    /// Scans a spread of lines for linear symbols, working outwards from the middle of the
    /// region because that is where an aiming user puts the symbol.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A linear symbol is only readable along a line that crosses every bar without leaving the
    /// symbol through its top or bottom edge. A retail barcode is several times wider than it is
    /// tall, so a horizontal scan stops working at about ten degrees of rotation, which is well
    /// inside the range a hand holding a phone actually produces. Scanning at a spread of angles
    /// is what makes a tilted symbol readable at all.
    /// </para>
    /// <para>
    /// The angles are not free, so the budget is spent where it pays: the aimed orientations get
    /// the full <see cref="BbScannerOptions.MaxScanLines"/>, and the tilted ones get a third of it,
    /// which is enough to cross a symbol the user is roughly pointing at.
    /// </para>
    /// </remarks>
    /// <param name="view">The searched region.</param>
    /// <param name="rowsScanned">How many lines were read.</param>
    /// <param name="collect">
    /// <see langword="null"/> to stop at the first symbol and return it; otherwise every distinct
    /// symbol is added here, scanning on until <paramref name="limit"/> is reached.
    /// </param>
    /// <param name="limit">The size <paramref name="collect"/> stops at.</param>
    private BbSymbolDecodeResult? ScanRows(in BbLuminanceView view, out int rowsScanned, List<BbSymbolDecodeResult>? collect = null, int limit = 1)
    {
        rowsScanned = 0;

        var width = view.Width;
        var height = view.Height;
        _scanLine ??= new BbBitRow(width + height);

        // Decoders that piece a symbol together from several lines start the image with nothing pieced.
        _followUps = 0;
        foreach (var decoder in _rowDecoders)
        {
            (decoder as IBbLineAwareRowDecoder)?.BeginImage();
        }

        // A live frame also tries one of the angles between the ones it always tries, a different one each frame, so a
        // symbol held at any angle at all is crossed within four frames - a tenth of a second at thirty a second - for
        // the cost of one more angle a frame rather than four. And it shifts its lines across by a fraction of their
        // spacing, a different one each frame, so that a symbol thinner than that spacing, which one frame's lines can
        // all miss, is crossed within four frames too; the in-between angles go through the fractions over sixteen.
        var angles = _options.TryHarder ? ThoroughAngles : LiveAngles;
        var count = angles.Length + (_options.TryHarder ? 0 : 1);
        var frame = _options.TryHarder ? 0 : _liveFrame++;
        var phase = LinePhases[frame & 3];
        for (var a = 0; a < count; a++)
        {
            if (a == angles.Length)
            {
                phase = LinePhases[(frame + (frame >> 2)) & 3];
            }

            var angle = a < angles.Length ? angles[a] : InBetweenAngles[frame & 3];
            if (!_options.TryVerticalLines && angle != 0f)
            {
                continue;
            }

            // The aimed orientations get the whole budget; the tilted ones a share of it, scaled
            // by how much further their lines have to spread so that the spacing between lines,
            // and therefore the smallest symbol that can be crossed, stays comparable.
            var budget = angle is 0f or 90f
                ? _options.MaxScanLines
                : ScaledBudget(view, angle, _options.MaxScanLines / (_options.TryHarder ? 2 : 3));

            var result = ScanParallelLines(view, angle, budget, phase, ref rowsScanned, collect, limit);
            if (result is not null && collect is null)
            {
                return result;
            }

            if (collect is not null && collect.Count >= limit)
            {
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Chooses how many lines to scan at a tilted angle.
    /// </summary>
    /// <remarks>
    /// A fixed count is the wrong control here. Lines at a diagonal have to spread across the
    /// frame's diagonal extent, so the same count leaves a wider gap between them, and a symbol
    /// thinner than that gap is crossed by no line at all: whether it decodes becomes a matter
    /// of where it happens to sit. The budget is therefore whichever is larger of the caller's
    /// share and the count a spacing bound demands, that bound being one tenth of the frame's
    /// shorter side, about the height of a retail barcode filling a third of the view. At the
    /// default settings that works out at around ninety lines a frame across all six
    /// orientations, most of which are rejected by the contrast probe before they are read.
    /// </remarks>
    private static int ScaledBudget(in BbLuminanceView view, float angleDegrees, int baseBudget)
    {
        var radians = angleDegrees * MathF.PI / 180f;
        var perpendicular = (view.Width * MathF.Abs(MathF.Sin(radians))) +
                            (view.Height * MathF.Abs(MathF.Cos(radians)));
        var maximumSpacing = Math.Max(4f, Math.Min(view.Width, view.Height) / 10f);
        var needed = (int)MathF.Ceiling(perpendicular / maximumSpacing);
        return Math.Clamp(Math.Max(baseBudget, needed), 5, 64);
    }

    /// <summary>
    /// Orientations tried on a live camera frame: upright, sideways, and the tilts a hand
    /// actually produces - up to 30 degrees off upright, and 15 off sideways, which a phone held
    /// across a label produces as readily. The sideways tilts come last: they are only scanned on
    /// a frame that holds nothing nearer upright, and on one of pure noise they add about a third
    /// to the linear stage.
    /// </summary>
    private static readonly float[] LiveAngles = [0f, 90f, 15f, 165f, 30f, 150f, 75f, 105f];

    /// <summary>
    /// The live angles' gaps: the diagonals and the angles either side of them. One of them is tried each frame, in
    /// turn; see <see cref="ScanRows"/>.
    /// </summary>
    private static readonly float[] InBetweenAngles = [45f, 135f, 60f, 120f];

    /// <summary>
    /// How far across, in units of their spacing, a live frame shifts its lines: none, half, and a quarter either way,
    /// so that four frames between them leave no gap wider than a quarter of the spacing.
    /// </summary>
    private static readonly float[] LinePhases = [0f, 0.5f, -0.25f, 0.25f];

    /// <summary>Counts the live frames scanned, for <see cref="InBetweenAngles"/> and <see cref="LinePhases"/>.</summary>
    private int _liveFrame;

    /// <summary>
    /// Orientations tried on a still image: every ten degrees and both diagonals, ordered so that
    /// the most likely ones are reached first.
    /// </summary>
    /// <remarks>
    /// A wide linear symbol tolerates only a few degrees of misalignment before a scan line
    /// leaves it through the top or bottom edge, so the step has to be small. A still image is
    /// scanned once, on demand, so it can afford the sweep; a live frame cannot, which is why
    /// the two sets differ.
    /// </remarks>
    private static readonly float[] ThoroughAngles = BuildThoroughAngles();

    private static float[] BuildThoroughAngles()
    {
        // 0 and 90 first, because a symbol is usually roughly aligned, then outwards from the
        // horizontal in ten degree steps, and finally the two exact diagonals, which are the
        // only orientations the ten degree grid leaves five degrees away from a scan line.
        var angles = new List<float> { 0f, 90f };
        for (var offset = 10; offset < 90; offset += 10)
        {
            angles.Add(offset);
            angles.Add(180 - offset);
        }

        angles.Add(45f);
        angles.Add(135f);
        return [.. angles];
    }

    /// <summary>
    /// Reads a spread of parallel lines at one angle, outwards from the middle, shifted across by
    /// <paramref name="phase"/> of their spacing. Returns the first symbol read, or - collecting -
    /// adds each distinct one to <paramref name="collect"/> and returns a symbol only once
    /// <paramref name="limit"/> is reached.
    /// </summary>
    private BbSymbolDecodeResult? ScanParallelLines(
        in BbLuminanceView view, float angleDegrees, int maxScanLines, float phase, ref int linesScanned,
        List<BbSymbolDecodeResult>? collect, int limit)
    {
        var width = view.Width;
        var height = view.Height;
        var radians = angleDegrees * MathF.PI / 180f;
        var dx = MathF.Cos(radians);
        var dy = MathF.Sin(radians);
        if (Math.Abs(dx) < 1e-4f) dx = 0;
        if (Math.Abs(dy) < 1e-4f) dy = 0;

        // Perpendicular, along which the lines are spread.
        var nx = -dy;
        var ny = dx;
        // The lines are spread along the perpendicular, so the span to cover is the frame's
        // extent in that direction. Using the shorter side instead, as is tempting, leaves the
        // corners of the frame unscanned at every diagonal angle: a symbol away from the middle
        // would then never be crossed by any line.
        var extent = (int)MathF.Ceiling((width * MathF.Abs(nx)) + (height * MathF.Abs(ny)));

        var maxLines = Math.Min(maxScanLines, extent);
        var spacing = Math.Max(1f, (float)extent / (maxLines + 1));
        float centerX = width / 2;
        float centerY = height / 2;
        var scanLine = _scanLine!;

        for (var attempt = 0; attempt < maxLines; attempt++)
        {
            // 0, +1, -1, +2, -2 ... in units of spacing.
            var delta = ((attempt + 1) / 2) * spacing;
            var offset = ((attempt & 1) == 0 ? delta : -delta) + (phase * spacing);
            var px = centerX + (nx * offset);
            var py = centerY + (ny * offset);
            if (dy == 0)
            {
                py = MathF.Round(py);
            }
            else if (dx == 0)
            {
                px = MathF.Round(px);
            }

            if (!TryClipLine(px, py, dx, dy, width, height, out var startT, out var endT))
            {
                continue;
            }

            var geometry = new LineGeometry(px + (dx * startT), py + (dy * startT), dx, dy, (int)(endT - startT) + 1);
            if (geometry.Length < 8)
            {
                continue;
            }

            linesScanned++;

            if (!TryBinarizeLine(view, geometry, scanLine, inverted: false))
            {
                continue;
            }

            var result = DecodeLine(scanLine, geometry, inverted: false);
            if (result is not null && IsConfirmed(view, result, px, py, nx, ny, dx, dy, inverted: false))
            {
                if (Take(result, collect, limit))
                {
                    return result;
                }

                continue;
            }

            if (result is null && FollowUp(view, dx, dy, inverted: false) is { } pieced)
            {
                if (Take(pieced, collect, limit))
                {
                    return pieced;
                }

                continue;
            }

            if (!_options.AllowInverted)
            {
                continue;
            }

            // A light-on-dark symbol needs the threshold applied the other way round.
            if (!TryBinarizeLine(view, geometry, scanLine, inverted: true))
            {
                continue;
            }

            result = DecodeLine(scanLine, geometry, inverted: true);
            if (result is not null && IsConfirmed(view, result, px, py, nx, ny, dx, dy, inverted: true) &&
                Take(result, collect, limit))
            {
                return result;
            }

            if (result is null && FollowUp(view, dx, dy, inverted: true) is { } invertedPiece &&
                Take(invertedPiece, collect, limit))
            {
                return invertedPiece;
            }
        }

        return null;

        // Whether the scan is over: always, for the first symbol of a single result scan; once
        // the collection is full, for one that gathers every distinct value the lines cross.
        static bool Take(BbSymbolDecodeResult result, List<BbSymbolDecodeResult>? collect, int limit)
        {
            if (collect is null)
            {
                return true;
            }

            if (!IsRepeat(collect, result))
            {
                collect.Add(result);
            }

            return collect.Count >= limit;
        }
    }

    /// <summary>
    /// Symbologies without a check character are re-read on a neighbouring, parallel line and
    /// only accepted when both lines agree. A scan line that leaves a symbol through its edge
    /// can assemble a shorter, internally valid value; the same accident almost never repeats a
    /// few pixels away, whereas a genuine symbol reads identically on every line that crosses it.
    /// UPC-E is included because the left half of a damaged EAN-13 reads as one, and GS1 DataBar
    /// because its only check is a modulo 79 checksum - DataBar Limited's, modulo 89 - which the
    /// hundreds of candidate positions of a noisy frame pass by chance often enough to matter.
    /// DataBar Expanded checks far more, but a symbol it puts together from several lines is held
    /// to the same rule as the rest of the family.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every parallel line is read, and one that reads the same symbology as something else vetoes
    /// the read: a spot or a scratch that one line runs through can make it read a different
    /// value that passes every check, and the lines either side, clear of it, read the right one.
    /// </para>
    /// <para>
    /// A short DataBar symbol turned nearly as far off a line as the line can be and still cross
    /// it whole is crossed whole by a band of lines a few pixels wide, so the parallel lines can
    /// all miss it. Only then is it tried on lines through the middle of the read, turned four
    /// degrees either way: one of them turns towards the symbol's own axis and still crosses it
    /// whole. They share the first line's pixels only within a few modules of the middle, so they
    /// are the weaker check, and are not used when a parallel line has a say.
    /// </para>
    /// </remarks>
    private bool IsConfirmed(
        in BbLuminanceView view, BbSymbolDecodeResult result,
        float px, float py, float nx, float ny, float dx, float dy, bool inverted)
    {
        var needsConfirmation = result.Format is BbBarcodeFormat.Itf or BbBarcodeFormat.UpcE or BbBarcodeFormat.Codabar or
                                    BbBarcodeFormat.DataBar or BbBarcodeFormat.DataBarLimited or BbBarcodeFormat.DataBarExpanded ||
                                (result.Format == BbBarcodeFormat.Code39 && !_options.Code39CheckDigit);
        if (!needsConfirmation)
        {
            return true;
        }

        _confirmLine ??= new BbBitRow(view.Width + view.Height);
        var agreed = false;
        Span<float> offsets = [3f, -3f, 6f, -6f];
        foreach (var offset in offsets)
        {
            var cx = px + (nx * offset);
            var cy = py + (ny * offset);
            if (dy == 0)
            {
                cy = MathF.Round(cy);
            }
            else if (dx == 0)
            {
                cx = MathF.Round(cx);
            }

            switch (ReadAgain(view, result, cx, cy, dx, dy, inverted))
            {
                case Agreement.Differs:
                    return false;
                case Agreement.Same:
                    agreed = true;
                    break;
            }
        }

        if (agreed || result.Format is not (BbBarcodeFormat.DataBar or BbBarcodeFormat.DataBarLimited) || result.Points is not [var start, var end])
        {
            return agreed;
        }

        var mx = (start.X + end.X) / 2;
        var my = (start.Y + end.Y) / 2;
        Span<float> turns = [4f, -4f];
        foreach (var turn in turns)
        {
            var (tdx, tdy) = Turn(dx, dy, turn);
            switch (ReadAgain(view, result, mx, my, tdx, tdy, inverted))
            {
                case Agreement.Differs:
                    return false;
                case Agreement.Same:
                    agreed = true;
                    break;
            }
        }

        return agreed;
    }

    /// <summary>What a second line made of a symbol another line read.</summary>
    private enum Agreement
    {
        /// <summary>It read nothing of that symbology.</summary>
        Silent,

        /// <summary>It read the same value.</summary>
        Same,

        /// <summary>It read the same symbology as a different value.</summary>
        Differs,
    }

    /// <summary>What the line through <c>(px, py)</c> along <c>(dx, dy)</c> makes of the symbol <paramref name="result"/> was read from.</summary>
    private Agreement ReadAgain(in BbLuminanceView view, BbSymbolDecodeResult result, float px, float py, float dx, float dy, bool inverted)
    {
        if (!TryClipLine(px, py, dx, dy, view.Width, view.Height, out var startT, out var endT))
        {
            return Agreement.Silent;
        }

        var geometry = new LineGeometry(px + (dx * startT), py + (dy * startT), dx, dy, (int)(endT - startT) + 1);
        if (geometry.Length < 8 || !TryBinarizeLine(view, geometry, _confirmLine!, inverted))
        {
            return Agreement.Silent;
        }

        if (DecodeLine(_confirmLine!, geometry, inverted, result.Format) is not { } again || again.Format != result.Format)
        {
            return Agreement.Silent;
        }

        return string.Equals(again.Payload.Text, result.Payload.Text, StringComparison.Ordinal) ? Agreement.Same : Agreement.Differs;
    }

    /// <summary>
    /// Reads more lines about one that read part of a symbol but not all of it, when a decoder asks. First the lines
    /// either side, parallel to it: the other row of a stacked symbol, which the frame's own lines - a few tens of
    /// pixels apart - may all miss, or the other end of a short symbol turned too far for one line to cross whole. Then,
    /// when those piece nothing together, lines turned about the part's middle: the turns that read part of the symbol
    /// again bracket the symbol's own axis, and lines along that axis, either side of the middle, cross rows too short
    /// for a line at any of the frame's angles to cross whole. Whatever they read is confirmed like any other read.
    /// </summary>
    private BbSymbolDecodeResult? FollowUp(in BbLuminanceView view, float dx, float dy, bool inverted)
    {
        if (_followUps >= MaxFollowUps || Partial() is not { } part)
        {
            return null;
        }

        _followUps++;
        _followLine ??= new BbBitRow(view.Width + view.Height);
        if (ReadAcross(view, part, dx, dy, inverted, middle: false) is { } result)
        {
            return result;
        }

        // Where the turned lines start the part traces its edge, which gives the decoder the axis; failing that, the
        // turns that read it at all bracket the axis.
        var (lowest, highest) = (0, 0);
        var aim = part;
        for (var turn = -MaxFollowTurn; turn <= MaxFollowTurn; turn += FollowTurnStep)
        {
            if (turn == 0)
            {
                continue;
            }

            var (tdx, tdy) = Turn(dx, dy, turn);
            if (ReadFollowLine(view, part.X, part.Y, tdx, tdy, inverted) is { } turned)
            {
                return turned;
            }

            if (Partial() is { } again)
            {
                (lowest, highest) = (Math.Min(lowest, turn), Math.Max(highest, turn));
                aim = again.HasAxis ? again : aim;
            }
        }

        var (adx, ady) = aim.HasAxis ? (aim.AxisX, aim.AxisY) : Turn(dx, dy, (lowest + highest) / 2f);

        // Along the line already: its parallels have been read.
        if ((adx * dx) + (ady * dy) > MathF.Cos(FollowTurnStep * MathF.PI / 180f))
        {
            return null;
        }

        return ReadAcross(view, part, adx, ady, inverted, middle: true);
    }

    /// <summary>The part of a symbol the last line read, as the first decoder that pieces symbols together saw it.</summary>
    private BbPartialRead? Partial()
    {
        foreach (var decoder in _rowDecoders)
        {
            if (decoder is IBbLineAwareRowDecoder { Partial: { } part })
            {
                return part;
            }
        }

        return null;
    }

    /// <summary>
    /// Reads the lines along <c>(dx, dy)</c> either side of <paramref name="part"/>'s middle, out to its reach, and the
    /// one through the middle too when <paramref name="middle"/> is set.
    /// </summary>
    private BbSymbolDecodeResult? ReadAcross(in BbLuminanceView view, BbPartialRead part, float dx, float dy, bool inverted, bool middle)
    {
        for (var step = middle ? 0 : 1; step * part.Spacing <= part.Reach; step++)
        {
            foreach (var sign in (ReadOnlySpan<float>)[1f, -1f])
            {
                if (step == 0 && sign < 0)
                {
                    continue;
                }

                var offset = sign * step * part.Spacing;
                if (ReadFollowLine(view, part.X - (dy * offset), part.Y + (dx * offset), dx, dy, inverted) is { } result)
                {
                    return result;
                }
            }
        }

        return null;
    }

    /// <summary>Reads the line through <c>(cx, cy)</c> along <c>(dx, dy)</c>, and confirms what it reads.</summary>
    private BbSymbolDecodeResult? ReadFollowLine(in BbLuminanceView view, float cx, float cy, float dx, float dy, bool inverted)
    {
        if (!TryClipLine(cx, cy, dx, dy, view.Width, view.Height, out var startT, out var endT))
        {
            return null;
        }

        var geometry = new LineGeometry(cx + (dx * startT), cy + (dy * startT), dx, dy, (int)(endT - startT) + 1);
        if (geometry.Length < 8 || !TryBinarizeLine(view, geometry, _followLine!, inverted))
        {
            return null;
        }

        return DecodeLine(_followLine!, geometry, inverted) is { } result && IsConfirmed(view, result, cx, cy, -dy, dx, dx, dy, inverted) ? result : null;
    }

    /// <summary>The direction <c>(dx, dy)</c> turned by <paramref name="degrees"/>.</summary>
    private static (float DX, float DY) Turn(float dx, float dy, float degrees)
    {
        var (sin, cos) = MathF.SinCos(degrees * MathF.PI / 180f);
        return ((dx * cos) - (dy * sin), (dx * sin) + (dy * cos));
    }

    /// <summary>Liang-Barsky clip of the line <c>(px, py) + t (dx, dy)</c> against the image rectangle.</summary>
    private static bool TryClipLine(float px, float py, float dx, float dy, int width, int height, out float startT, out float endT)
    {
        var t0 = float.NegativeInfinity;
        var t1 = float.PositiveInfinity;
        startT = 0;
        endT = 0;

        if (!Clip(-dx, px, ref t0, ref t1) ||
            !Clip(dx, width - 1 - px, ref t0, ref t1) ||
            !Clip(-dy, py, ref t0, ref t1) ||
            !Clip(dy, height - 1 - py, ref t0, ref t1))
        {
            return false;
        }

        startT = MathF.Ceiling(t0);
        endT = MathF.Floor(t1);
        return endT >= startT;

        static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (p == 0)
            {
                return q >= 0;
            }

            var t = q / p;
            if (p < 0)
            {
                if (t > t1)
                {
                    return false;
                }

                if (t > t0)
                {
                    t0 = t;
                }
            }
            else
            {
                if (t < t0)
                {
                    return false;
                }

                if (t < t1)
                {
                    t1 = t;
                }
            }

            return true;
        }
    }

    private bool TryBinarizeLine(in BbLuminanceView view, in LineGeometry geometry, BbBitRow scanLine, bool inverted)
    {
        if (geometry.IsRow)
        {
            return _rowBinarizer.TryGetBlackRow(view, (int)geometry.Y0, scanLine, inverted);
        }

        // Columns and diagonals are gathered into a contiguous scratch row so the same
        // binariser serves every orientation. The gather touches one byte per pixel of the
        // line, which is far cheaper than binarising a rotated copy of the frame.
        //
        // Most lines of most frames cross nothing but background, and gathering them in full
        // only to have the binariser reject them for want of contrast is the single largest
        // avoidable cost of a scan. A sparse probe first settles that question for a quarter of
        // the work.
        if (!HasContrastAlong(view, geometry))
        {
            return false;
        }

        var length = geometry.Length;
        if (_lineBuffer.Length < length)
        {
            _lineBuffer = new byte[Math.Max(length, view.Width + view.Height)];
        }

        var line = _lineBuffer;
        if (geometry.IsColumn)
        {
            var x = (int)geometry.X0;
            for (var i = 0; i < length; i++)
            {
                line[i] = view[x, i];
            }
        }
        else
        {
            // Bilinear rather than nearest neighbour. A diagonal walk rounded to whole pixels
            // wanders half a pixel either side of the true line, and that wander lands on the
            // wrong side of a module edge often enough to corrupt the run lengths the decoders
            // measure: the symptom is a symbol that reads at one angle but not at the angle it
            // is actually printed at. Interpolating only across the line is not enough, because
            // the rounding along it distorts the run lengths in its own right.
            var maxX = view.Width - 1;
            var maxY = view.Height - 1;
            for (var i = 0; i < length; i++)
            {
                var fx = Math.Clamp(geometry.X0 + (geometry.DX * i), 0, maxX);
                var fy = Math.Clamp(geometry.Y0 + (geometry.DY * i), 0, maxY);
                var x0 = (int)fx;
                var y0 = (int)fy;
                var x1 = Math.Min(x0 + 1, maxX);
                var y1 = Math.Min(y0 + 1, maxY);
                var tx = fx - x0;
                var ty = fy - y0;

                var top = (view[x0, y0] * (1 - tx)) + (view[x1, y0] * tx);
                var bottom = (view[x0, y1] * (1 - tx)) + (view[x1, y1] * tx);
                line[i] = (byte)((top * (1 - ty)) + (bottom * ty) + 0.5f);
            }
        }

        var lineView = new BbLuminanceView(line, 0, length, length, 1);
        return _rowBinarizer.TryGetBlackRow(lineView, 0, scanLine, inverted);
    }

    /// <summary>
    /// Samples every fourth point of a line and reports whether it spans enough luminance to
    /// hold a symbol at all.
    /// </summary>
    /// <remarks>
    /// The stride is short enough that a symbol wide enough to decode, which crosses dozens of
    /// bars, cannot hide between the samples, and the span is measured over the whole line
    /// against a threshold below the one the row binariser applies within each of its segments,
    /// so this rejects strictly less than the binariser would.
    /// </remarks>
    private static bool HasContrastAlong(in BbLuminanceView view, in LineGeometry geometry)
    {
        const int Stride = 4;
        const int MinimumSpan = 24;

        var maxX = view.Width - 1;
        var maxY = view.Height - 1;
        var min = 255;
        var max = 0;

        for (var i = 0; i < geometry.Length; i += Stride)
        {
            var x = Math.Clamp((int)(geometry.X0 + (geometry.DX * i)), 0, maxX);
            var y = Math.Clamp((int)(geometry.Y0 + (geometry.DY * i)), 0, maxY);
            int sample = view[x, y];
            if (sample < min)
            {
                min = sample;
            }

            if (sample > max)
            {
                max = sample;
            }

            if (max - min >= MinimumSpan)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Tries every row decoder on a binarised line, forwards and, when enabled, backwards, and
    /// maps the reported end points back into image coordinates.
    /// </summary>
    /// <param name="scanLine">The binarised line.</param>
    /// <param name="geometry">Where the line runs in the image.</param>
    /// <param name="inverted">Whether the line was binarised the other way round, for a light symbol on a dark ground.</param>
    /// <param name="decoders">
    /// Only the decoders that can produce one of these: a line confirming a read must not be won
    /// by another symbol it also crosses. Each decoder still gets every enabled format, so what it
    /// reports does not change.
    /// </param>
    private BbSymbolDecodeResult? DecodeLine(BbBitRow scanLine, in LineGeometry geometry, bool inverted, BbBarcodeFormat decoders = BbBarcodeFormat.All)
    {
        foreach (var decoder in _rowDecoders)
        {
            if ((decoder.Formats & decoders) == 0)
            {
                continue;
            }

            (decoder as IBbLineAwareRowDecoder)?.SetLine(geometry.X0, geometry.Y0, geometry.DX, geometry.DY, geometry.Length, reversed: false, inverted);
            var result = decoder.DecodeRow(0, scanLine, _options.Formats);
            if (result is not null)
            {
                return MapPoints(result, geometry, reversed: false);
            }
        }

        if (!_options.TryReversedRows)
        {
            return null;
        }

        // Retry the same line read right to left; several symbologies are only recognisable in
        // one direction and this costs nothing compared to binarising another line.
        scanLine.Reverse();
        try
        {
            foreach (var decoder in _rowDecoders)
            {
                if ((decoder.Formats & decoders) == 0)
                {
                    continue;
                }

                (decoder as IBbLineAwareRowDecoder)?.SetLine(geometry.X0, geometry.Y0, geometry.DX, geometry.DY, geometry.Length, reversed: true, inverted);
                var result = decoder.DecodeRow(0, scanLine, _options.Formats);
                if (result is not null)
                {
                    return MapPoints(result, geometry, reversed: true);
                }
            }
        }
        finally
        {
            // Leave the line as it was read, so a later inverted retry sees a known orientation.
            scanLine.Reverse();
        }

        return null;
    }

    private static BbSymbolDecodeResult MapPoints(BbSymbolDecodeResult result, in LineGeometry geometry, bool reversed)
    {
        var points = new BbScanPoint[result.Points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            var index = result.Points[i].X;
            if (reversed)
            {
                index = geometry.Length - 1 - index;
            }

            points[i] = new BbScanPoint(geometry.X0 + (geometry.DX * index), geometry.Y0 + (geometry.DY * index));
        }

        return result with { Points = points };
    }

    /// <summary>A scan line: the image point of sample <c>i</c> is <c>(X0 + i DX, Y0 + i DY)</c>.</summary>
    private readonly record struct LineGeometry(float X0, float Y0, float DX, float DY, int Length)
    {
        public bool IsRow => DY == 0 && DX == 1;

        public bool IsColumn => DX == 0 && DY == 1;
    }

    /// <summary>
    /// Zeroes everything the decoder keeps from the last image - its grayscale and masked copies, the binarizers' block
    /// statistics, the luminance and bits of the last scan lines, and what each symbology's reader kept of it - while
    /// keeping the buffers for the next one. The scanner component calls this whenever its camera closes, so that
    /// nothing goes on holding the last thing the camera saw.
    /// </summary>
    internal void ForgetImage()
    {
        _grayscale?.Clear();
        _masked?.Clear();
        _scanLine?.Clear();
        _confirmLine?.Clear();
        _followLine?.Clear();
        _enlarged?.Clear();
        Array.Clear(_lineBuffer);
        _matrixBinarizer.ForgetImage();
        _rowBinarizer.ForgetImage();
        foreach (var decoder in _matrixDecoders)
        {
            (decoder as IBbImageHolder)?.ForgetImage();
        }

        foreach (var decoder in _rowDecoders)
        {
            (decoder as IBbImageHolder)?.ForgetImage();
        }
    }

    /// <inheritdoc />
    /// <remarks>Everything kept from the last image is zeroed first; buffers go back to the shared pool cleared.</remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ForgetImage();
        _disposed = true;
        _grayscale?.Dispose();
        _grayscale = null;
        _masked?.Dispose();
        _masked = null;
        _enlarged?.Dispose();
        _enlarged = null;
        _scanLine = null;
        _confirmLine = null;
        _lineBuffer = [];
    }
}
