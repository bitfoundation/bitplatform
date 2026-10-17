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

    private BbLuminanceBuffer? _grayscale;
    private BbBitRow? _scanLine;
    private BbBitRow? _confirmLine;
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
        if ((_options.Formats & BbBarcodeFormat.QrCode) != 0)
        {
            _matrixDecoders.Add(new BbQrCodeReader());
        }

        if ((_options.Formats & BbBarcodeFormat.DataMatrix) != 0)
        {
            _matrixDecoders.Add(new BbDataMatrixReader());
        }

        // PDF417 is only wired up once a specification symbol character table has been
        // installed; see BbPdf417SymbolTable for why the shipped table cannot be conformant.
        if ((_options.Formats & BbBarcodeFormat.Pdf417) != 0 && BbPdf417SymbolTable.Current.IsSpecificationTable)
        {
            _matrixDecoders.Add(new BbPdf417Reader());
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
            _rowDecoders.Add(new BbEanUpcReader());
        }

        if ((_options.Formats & BbBarcodeFormat.Itf) != 0)
        {
            _rowDecoders.Add(new BbItfReader(_options.ItfLengths));
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
            ArgumentOutOfRangeException.ThrowIfNegative(value.Ticks);
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

    /// <summary>Forgets the last reported value so that scanning it again reports immediately.</summary>
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var start = Stopwatch.GetTimestamp();

        var step = _options.DownsampleFactor;
        var targetWidth = width / step;
        var targetHeight = height / step;
        if (targetWidth < 8 || targetHeight < 8)
        {
            throw new ArgumentException(
                "The frame is too small to decode after downsampling.", nameof(pixels));
        }

        if (_grayscale is null)
        {
            _grayscale = BbLuminanceBuffer.Rent(targetWidth, targetHeight);
        }
        else
        {
            _grayscale.Resize(targetWidth, targetHeight);
        }

        BbPixelConverter.ToGrayscaleDownsampled(pixels, _grayscale.Pixels, width, height, format, step);
        var afterGrayscale = Stopwatch.GetTimestamp();

        var result = DecodeCore(_grayscale.View, start, afterGrayscale, width, height, step, suppressDuplicates);
        return result;
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
        return DecodeCore(view, start, start, view.Width, view.Height, 1, suppressDuplicates);
    }

    private BbBarcodeResult? DecodeCore(
        in BbLuminanceView source,
        long start,
        long afterGrayscale,
        int frameWidth,
        int frameHeight,
        int step,
        bool suppressDuplicates)
    {
        FramesDecoded++;

        var region = _options.Region;
        var view = region.IsFull ? source : source.Crop(region);
        var offsetX = ((view.Offset - source.Offset) % source.Stride) * step;
        var offsetY = ((view.Offset - source.Offset) / source.Stride) * step;

        var binarizeTicks = 0L;
        var matrixTicks = 0L;
        var linearTicks = 0L;
        var rowsScanned = 0;

        BbSymbolDecodeResult? symbol = null;

        if (_matrixDecoders.Count > 0 && view.Width >= 16 && view.Height >= 16)
        {
            var beforeBinarize = Stopwatch.GetTimestamp();
            using var binary = _matrixBinarizer.GetBlackMatrix(view);
            var afterBinarize = Stopwatch.GetTimestamp();
            binarizeTicks = afterBinarize - beforeBinarize;

            if (binary is not null)
            {
                var matrixOptions = new BbMatrixDecodeOptions(_options.TryHarder, _options.AllowInverted);
                foreach (var decoder in _matrixDecoders)
                {
                    symbol = decoder.Decode(binary, matrixOptions);
                    if (symbol is not null)
                    {
                        break;
                    }
                }
            }

            matrixTicks = Stopwatch.GetTimestamp() - afterBinarize;
        }

        if (symbol is null && _rowDecoders.Count > 0)
        {
            var beforeLinear = Stopwatch.GetTimestamp();
            symbol = ScanRows(view, out rowsScanned);
            linearTicks = Stopwatch.GetTimestamp() - beforeLinear;
        }

        var total = Stopwatch.GetTimestamp() - start;
        LastFrame = new BbScanDiagnostics
        {
            DecodedWidth = view.Width,
            DecodedHeight = view.Height,
            Total = Stopwatch.GetElapsedTime(0, total),
            Grayscale = Stopwatch.GetElapsedTime(start, afterGrayscale),
            Binarize = Stopwatch.GetElapsedTime(0, binarizeTicks),
            Matrix = Stopwatch.GetElapsedTime(0, matrixTicks),
            Linear = Stopwatch.GetElapsedTime(0, linearTicks),
            RowsScanned = rowsScanned,
            Decoded = symbol is not null,
        };

        if (symbol is null)
        {
            return null;
        }

        FramesWithSymbol++;

        var timestamp = Environment.TickCount64;
        if (suppressDuplicates && !_duplicateFilter.ShouldReport(symbol.Payload.Text, symbol.Format, timestamp))
        {
            return null;
        }

        // Decoder coordinates are relative to the cropped, downsampled image; map them back to
        // the coordinates of the frame the caller handed in.
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
    private BbSymbolDecodeResult? ScanRows(in BbLuminanceView view, out int rowsScanned)
    {
        rowsScanned = 0;

        var width = view.Width;
        var height = view.Height;
        _scanLine ??= new BbBitRow(width + height);

        var angles = _options.TryHarder ? ThoroughAngles : LiveAngles;
        foreach (var angle in angles)
        {
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

            var result = ScanParallelLines(view, angle, budget, ref rowsScanned);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Orientations tried on a live camera frame: upright, sideways, and the tilts a hand
    /// actually produces.
    /// </summary>
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

    private static readonly float[] LiveAngles = [0f, 90f, 15f, 165f, 30f, 150f];

    /// <summary>
    /// Orientations tried on a still image: every five degrees, ordered so that the most likely
    /// ones are reached first.
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

    private BbSymbolDecodeResult? ScanParallelLines(in BbLuminanceView view, float angleDegrees, int maxScanLines, ref int linesScanned)
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
            var offset = (attempt & 1) == 0 ? delta : -delta;
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

            var result = DecodeLine(scanLine, geometry);
            if (result is not null && IsConfirmed(view, result, px, py, nx, ny, dx, dy, inverted: false))
            {
                return result;
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

            result = DecodeLine(scanLine, geometry);
            if (result is not null && IsConfirmed(view, result, px, py, nx, ny, dx, dy, inverted: true))
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Symbologies without a check character are re-read on a neighbouring, parallel line and
    /// only accepted when both lines agree. A scan line that leaves a symbol through its edge
    /// can assemble a shorter, internally valid value; the same accident almost never repeats a
    /// few pixels away, whereas a genuine symbol reads identically on every line that crosses it.
    /// UPC-E is included because the left half of a damaged EAN-13 reads as one.
    /// </summary>
    private bool IsConfirmed(
        in BbLuminanceView view, BbSymbolDecodeResult result,
        float px, float py, float nx, float ny, float dx, float dy, bool inverted)
    {
        var needsConfirmation = result.Format is BbBarcodeFormat.Itf or BbBarcodeFormat.UpcE ||
                                (result.Format == BbBarcodeFormat.Code39 && !_options.Code39CheckDigit);
        if (!needsConfirmation)
        {
            return true;
        }

        _confirmLine ??= new BbBitRow(view.Width + view.Height);
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

            if (!TryClipLine(cx, cy, dx, dy, view.Width, view.Height, out var startT, out var endT))
            {
                continue;
            }

            var geometry = new LineGeometry(cx + (dx * startT), cy + (dy * startT), dx, dy, (int)(endT - startT) + 1);
            if (geometry.Length < 8 || !TryBinarizeLine(view, geometry, _confirmLine, inverted))
            {
                continue;
            }

            var again = DecodeLine(_confirmLine, geometry);
            if (again is not null && again.Format == result.Format &&
                string.Equals(again.Payload.Text, result.Payload.Text, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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
    private BbSymbolDecodeResult? DecodeLine(BbBitRow scanLine, in LineGeometry geometry)
    {
        foreach (var decoder in _rowDecoders)
        {
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _grayscale?.Dispose();
        _grayscale = null;
        _scanLine = null;
        _confirmLine = null;
        _lineBuffer = [];
    }
}
