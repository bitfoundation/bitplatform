using System.Reflection;
using System.Globalization;
using Bit.BbScanner.TestKit;
using Bit.BbScanner.Demo.Server.Dtos;

namespace Bit.BbScanner.Demo.Server.Services;

/// <summary>
/// Renders a real symbol, puts it through the camera-like transforms asked for, and runs Bit.BbScanner's own
/// decoder over it - first with the live camera defaults, then with <see cref="BbScannerOptions.ForStillImages"/> -
/// so an agent sees the <see cref="BbBarcodeResult"/> the library actually produces for a payload: its text,
/// raw bytes, corners, metadata and timings.
/// <para>
/// The symbol is drawn with the encoders of the test kit, the same ones the unit tests round trip against and the
/// demo draws its sample barcodes with. The library itself only reads barcodes.
/// </para>
/// </summary>
public static class BbScannerDecodeLab
{
    /// <summary>The longest payload the lab encodes; anything past it is a pasted file rather than a barcode.</summary>
    private const int MaxTextLength = 1_000;

    private const int MaxRawBytes = 64;

    /// <summary>
    /// The largest frame the lab renders, after framing and rotation. The payload and module size limits alone do not
    /// bound it - a thousand character Code 128 at twelve pixels a module is 130,000 pixels wide - and every transform
    /// and both decodes scale with it, so without this one anonymous request could hold the server for minutes.
    /// Four megapixels holds the largest QR code, version 40, at ten pixels a module upright or seven rotated 45 degrees.
    /// </summary>
    private const int MaxFramePixels = 4 * 1024 * 1024;

    private const int MaxFrameEdge = 4096;

    private static readonly PropertyInfo[] _metadataProperties = typeof(BbBarcodeResultMetadata).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    public static BbScannerDecodeReportDto Inspect(string? format, string? text, int moduleSize, double rotation, double blur)
    {
        var dto = BbScannerFormatCatalog.Find(format);
        if (dto is null)
        {
            return new BbScannerDecodeReportDto
            {
                Format = format ?? string.Empty,
                Text = text ?? string.Empty,
                Encoded = false,
                Error = $"'{format}' is not a symbology Bit.BbScanner reads.",
                Notes = [$"Use one of: {string.Join(", ", BbScannerFormatCatalog.Formats.Select(f => f.Name))}."]
            };
        }

        var symbology = Enum.Parse<BbBarcodeFormat>(dto.Name);
        var payload = string.IsNullOrEmpty(text) ? dto.SampleText : text;

        if (payload.Length > MaxTextLength)
        {
            return Failed(dto.Name, payload[..40] + "...", $"The payload is {payload.Length} characters long; the lab encodes at most {MaxTextLength}.");
        }

        moduleSize = Math.Clamp(moduleSize, 1, 12);
        rotation = double.IsFinite(rotation) ? Math.Clamp(rotation, -360, 360) : 0;
        blur = double.IsFinite(blur) ? Math.Clamp(blur, 0, 5) : 0;

        BbBitMatrix? matrix;
        bool[]? modules;
        try
        {
            payload = Complete(symbology, payload);
            (matrix, modules) = Encode(symbology, payload);
        }
        catch (ArgumentException ex)
        {
            return Failed(dto.Name, payload, ex.Message);
        }

        // The size is worked out from the encoded modules before a single pixel is allocated.
        var (symbolWidth, symbolHeight) = SymbolSize(matrix, modules, moduleSize);
        var (frameWidth, frameHeight) = (Math.Max(640, symbolWidth + 80), Math.Max(480, symbolHeight + 80));
        var (outputWidth, outputHeight) = RotatedSize(frameWidth, frameHeight, rotation);
        if (outputWidth > MaxFrameEdge || outputHeight > MaxFrameEdge || (long)outputWidth * outputHeight > MaxFramePixels)
        {
            matrix?.Dispose();
            return TooLarge(dto.Name, payload, moduleSize, outputWidth, outputHeight);
        }

        using var symbol = Render(matrix, modules, moduleSize);

        // A camera frame rather than a tightly cropped symbol: the detectors look for a symbol, so the lab gives
        // them one to find, with background around it as a real frame has.
        using var framed = ImageTransforms.Frame(symbol, frameWidth, frameHeight);
        using var rotated = rotation == 0 ? null : ImageTransforms.Rotate(framed, rotation);
        using var blurred = blur == 0 ? null : ImageTransforms.Blur(rotated ?? framed, blur);
        var frame = blurred ?? rotated ?? framed;

        var live = new BbScannerOptions { Formats = symbology, DuplicateSuppressionWindow = TimeSpan.Zero };

        var still = BbScannerOptions.ForStillImages();
        still.Formats = symbology;

        BbScannerDecodeAttemptDto[] attempts = [Decode(frame, live, "live", payload), Decode(frame, still, "still", payload)];

        return new BbScannerDecodeReportDto
        {
            Format = dto.Name,
            Text = payload,
            Encoded = true,
            FrameWidth = frame.Width,
            FrameHeight = frame.Height,
            ModuleSize = moduleSize,
            Rotation = rotation,
            Blur = blur,
            Attempts = attempts,
            Notes = Notes(dto, symbology, payload, rotation, attempts)
        };
    }

    private static BbScannerDecodeReportDto Failed(string format, string text, string error) => new()
    {
        Format = format,
        Text = text,
        Encoded = false,
        Error = error,
        Notes = ["The payload could not be encoded, so nothing was decoded. The error is the encoder's: change the payload, not the scanner."]
    };

    private static BbScannerDecodeReportDto TooLarge(string format, string text, int moduleSize, int width, int height) => new()
    {
        Format = format,
        Text = text.Length > 40 ? text[..40] + "..." : text,
        Encoded = false,
        Error = $"At {moduleSize} pixels a module the frame would be {width}x{height} pixels; the lab renders at most " +
                $"{MaxFrameEdge} pixels a side and {MaxFramePixels / (1024 * 1024)} megapixels.",
        Notes = ["Nothing was rendered or decoded. Lower moduleSize, shorten the payload, or rotate by a multiple of 90 degrees, " +
                 "which keeps the frame from growing to hold the rotated symbol."]
    };

    /// <summary>
    /// Adds the check digit an EAN or UPC payload is usually written without, so "400638133393" works as well as
    /// "4006381333931". A payload that already has one is left alone, and a wrong one is the encoder's to reject.
    /// </summary>
    private static string Complete(BbBarcodeFormat format, string payload)
    {
        if (payload.All(char.IsAsciiDigit) is false) return payload;

        return format switch
        {
            BbBarcodeFormat.Ean13 when payload.Length == 12 => payload + LinearEncoders.EanCheckDigit(payload),
            BbBarcodeFormat.Ean8 when payload.Length == 7 => payload + LinearEncoders.EanCheckDigit(payload),
            BbBarcodeFormat.UpcA when payload.Length == 11 => payload + LinearEncoders.EanCheckDigit("0" + payload),
            _ => payload
        };
    }

    private static (BbBitMatrix? Matrix, bool[]? Modules) Encode(BbBarcodeFormat format, string payload) => format switch
    {
        BbBarcodeFormat.QrCode => (QrEncoder.Encode(payload, BbQrErrorCorrectionLevel.M, maskPattern: 2), null),
        BbBarcodeFormat.DataMatrix => (DataMatrixEncoder.Encode(payload), null),
        BbBarcodeFormat.Pdf417 => (Pdf417Encoder.Encode(payload, rowHeightModules: 4), null),
        BbBarcodeFormat.Code128 => (null, LinearEncoders.Code128(payload)),
        BbBarcodeFormat.Code39 => (null, LinearEncoders.Code39(payload)),
        BbBarcodeFormat.Ean13 => (null, LinearEncoders.Ean13(payload)),
        BbBarcodeFormat.Ean8 => (null, LinearEncoders.Ean8(payload)),
        BbBarcodeFormat.UpcA => (null, LinearEncoders.UpcA(payload)),
        BbBarcodeFormat.UpcE => (null, LinearEncoders.UpcE(payload)),
        BbBarcodeFormat.Itf => (null, LinearEncoders.Itf(payload)),
        _ => throw new ArgumentException($"The lab has no encoder for {format}.", nameof(format)),
    };

    // The quiet zones and the linear height match the defaults Render draws with.
    private static (int Width, int Height) SymbolSize(BbBitMatrix? matrix, bool[]? modules, int moduleSize) => matrix is not null
        ? ((matrix.Width + 8) * moduleSize, (matrix.Height + 8) * moduleSize)
        : ((modules!.Length + 24) * moduleSize, LinearHeight(moduleSize));

    private static int LinearHeight(int moduleSize) => Math.Max(60, moduleSize * 30);

    /// <summary>The size <see cref="ImageTransforms.Rotate"/> gives a frame so that the whole rotated frame fits.</summary>
    private static (int Width, int Height) RotatedSize(int width, int height, double degrees)
    {
        if (degrees == 0) return (width, height);

        var radians = degrees * Math.PI / 180.0;
        var (cos, sin) = (Math.Abs(Math.Cos(radians)), Math.Abs(Math.Sin(radians)));
        return ((int)Math.Ceiling((width * cos) + (height * sin)), (int)Math.Ceiling((width * sin) + (height * cos)));
    }

    private static BbLuminanceBuffer Render(BbBitMatrix? matrix, bool[]? modules, int moduleSize)
    {
        if (matrix is not null)
        {
            using (matrix)
            {
                return SyntheticImage.FromMatrix(matrix, scale: moduleSize);
            }
        }

        return SyntheticImage.FromLinear(modules!, scale: moduleSize, height: LinearHeight(moduleSize));
    }

    private static BbScannerDecodeAttemptDto Decode(BbLuminanceBuffer frame, BbScannerOptions options, string name, string payload)
    {
        using var decoder = new BbBarcodeDecoder(options);

        var result = decoder.Decode(frame.View, suppressDuplicates: false);
        var timings = decoder.LastFrame;

        var raw = result?.RawBytes.Span.ToArray() ?? [];

        return new BbScannerDecodeAttemptDto
        {
            Options = name,
            Decoded = result is not null,
            Text = result?.Text,
            Format = result?.Format.ToString(),
            Matches = result is null ? null : result.Text == payload,
            RawBytes = result is null ? null : Convert.ToHexString(raw.AsSpan(0, Math.Min(raw.Length, MaxRawBytes))) + (raw.Length > MaxRawBytes ? "..." : null),
            RawByteCount = result is null ? null : raw.Length,
            Corners = result is null ? null : [.. result.Corners.Select(point => new BbScannerPointDto { X = Round(point.X), Y = Round(point.Y) })],
            BoundingBox = result is null ? null : new BbScannerRectDto
            {
                X = Round(result.BoundingBox.X),
                Y = Round(result.BoundingBox.Y),
                Width = Round(result.BoundingBox.Width),
                Height = Round(result.BoundingBox.Height)
            },
            Metadata = result?.Metadata is { } metadata ? Metadata(metadata) : null,
            Timings = new BbScannerTimingsDto
            {
                TotalMilliseconds = Milliseconds(timings.Total),
                GrayscaleMilliseconds = Milliseconds(timings.Grayscale),
                BinarizeMilliseconds = Milliseconds(timings.Binarize),
                MatrixMilliseconds = Milliseconds(timings.Matrix),
                LinearMilliseconds = Milliseconds(timings.Linear),
                RowsScanned = timings.RowsScanned
            }
        };
    }

    private static Dictionary<string, string> Metadata(BbBarcodeResultMetadata metadata)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var property in _metadataProperties)
        {
            if (property.GetValue(metadata) is { } value)
            {
                values[property.Name] = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
        }

        return values;
    }

    private static string[] Notes(BbScannerFormatDto dto, BbBarcodeFormat format, string payload, double rotation, BbScannerDecodeAttemptDto[] attempts)
    {
        var notes = new List<string>();
        var (live, still) = (attempts[0], attempts[1]);

        if (dto.Active is false)
        {
            notes.Add("BbBarcodeDecoder does not create its PDF417 reader while the registered symbol character table is the generated " +
                      "placeholder, so neither attempt can read this symbol. That is deliberate - see GetBbScannerGuideSection(heading: " +
                      "\"PDF417 status\") for registering the ISO/IEC 15438 table.");
        }
        else if (live.Decoded && still.Decoded)
        {
            notes.Add("Both the live camera defaults and the still image defaults read it.");
        }
        else if (still.Decoded)
        {
            notes.Add("Only BbScannerOptions.ForStillImages() reads it: the live defaults give up early because a camera gets another frame " +
                      "a few tens of milliseconds later. For a still image use ForStillImages (BbScanner.DecodeImage and the built-in " +
                      "file picker already do); for live scanning, ask the user to straighten or move closer, or raise MaxScanLines / set TryHarder.");
        }
        else if (live.Decoded is false)
        {
            notes.Add("Neither attempt read it. Modules below about two pixels, blur wider than a module, and - while scanning live - a linear " +
                      "symbol rotated more than about 30 degrees are outside what the decoders read; see GetBbScannerGuideSection(heading: " +
                      "\"What it reads, and what it does not\").");
        }

        foreach (var attempt in attempts.Where(a => a.Decoded))
        {
            if (attempt.Matches is false)
            {
                notes.Add($"The {attempt.Options} attempt returned \"{attempt.Text}\" for the payload \"{payload}\".");
            }

            if (attempt.Format is { } reported && reported != dto.Name)
            {
                notes.Add($"The {attempt.Options} attempt reported the symbol as {reported}, not {dto.Name}.");
            }
        }

        // How far the bars are from upright or sideways, the two orientations live scanning sweeps around.
        var offAxis = Math.Abs(rotation % 90);
        if (dto.Family == "Linear" && Math.Min(offAxis, 90 - offAxis) > 20)
        {
            notes.Add("Live scanning reads a linear symbol within about 30 degrees of upright or sideways; ForStillImages sweeps every ten degrees.");
        }

        if (format == BbBarcodeFormat.Itf && new BbScannerOptions().ItfLengths is { Count: > 0 } lengths && lengths.Contains(payload.Length) is false)
        {
            notes.Add($"ITF results of {payload.Length} digits are rejected by the default ItfLengths [{string.Join(", ", lengths)}]: set " +
                      "BbScannerOptions.ItfLengths to the lengths the application uses (an empty collection accepts any even length).");
        }

        if (format == BbBarcodeFormat.Code39)
        {
            notes.Add("Code 39 has an optional modulo 43 check character and an optional full ASCII mode; both are off by default " +
                      "(Code39CheckDigit, Code39ExtendedMode), so the text is returned exactly as the bars spell it.");
        }

        if (attempts.Any(a => a.Decoded))
        {
            notes.Add("Corners and BoundingBox are in the pixel coordinates of the whole frame the decoder was given, whatever Region was searched. " +
                      "An application receives exactly this object as BbBarcodeResult from OnScan or BbBarcodeDecoder.Decode.");
        }

        return [.. notes];
    }

    private static float Round(double value) => (float)Math.Round(value, 1);

    private static double Milliseconds(TimeSpan span) => Math.Round(span.TotalMilliseconds, 3);
}
