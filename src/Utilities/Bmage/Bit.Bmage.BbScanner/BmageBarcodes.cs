using Bit.BbScanner;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.BbScanner;

/// <summary>A way <see cref="BmageBarcodes.Read"/> prepares an image before decoding it again.</summary>
public enum BarcodePreprocessing
{
    /// <summary>The image as it is.</summary>
    None = 0,

    /// <summary>Sharpened with an unsharp mask: a code photographed slightly out of focus.</summary>
    Sharpen,

    /// <summary>
    /// Enlarged to <see cref="BarcodeReadOptions.MinimumSize"/> pixels across with bicubic interpolation: a soft code
    /// whose modules are only a few pixels wide, from a thumbnail or a distant shot, gets room for its edges. Skipped for
    /// an image that is already that large.
    /// </summary>
    Enlarge,
}

/// <summary>How <see cref="BmageBarcodes.Read"/> looks for codes.</summary>
public sealed record BarcodeReadOptions
{
    /// <summary>The decoders' options; null for <see cref="BbScannerOptions.ForStillImages"/>.</summary>
    public BbScannerOptions? Scanner { get; init; }

    /// <summary>The most codes to return.</summary>
    public int MaxSymbols
    {
        get;
        init => field = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(MaxSymbols), value, "At least one code must be asked for.");
    } = 16;

    /// <summary>
    /// What to try, in order, until one gives a code: the first entry is usually <see cref="BarcodePreprocessing.None"/>,
    /// so an image that reads as it is costs one decode. Defaults to as it is, sharpened, then enlarged: cheapest first.
    /// </summary>
    public IReadOnlyList<BarcodePreprocessing> Preprocessing { get; init; } =
        [BarcodePreprocessing.None, BarcodePreprocessing.Sharpen, BarcodePreprocessing.Enlarge];

    /// <summary>The longer side <see cref="BarcodePreprocessing.Enlarge"/> enlarges a smaller image to; larger images are skipped by it.</summary>
    public int MinimumSize
    {
        get;
        init => field = value is >= 64 and <= 4096 ? value : throw new ArgumentOutOfRangeException(nameof(MinimumSize), value, "The size must be between 64 and 4096 pixels.");
    } = 1200;
}

/// <summary>What <see cref="BmageBarcodes.Read"/> found, and which preparation found it.</summary>
/// <param name="Barcodes">The codes; empty when none was found.</param>
/// <param name="Preprocessing">The preparation that found them, or null when none did.</param>
/// <param name="ScaleX">
/// How much wider than the image given the image the codes were found in was: above 1 after
/// <see cref="BarcodePreprocessing.Enlarge"/>, else 1. <see cref="CornersOf"/> maps a code's corners back.
/// </param>
/// <param name="ScaleY">
/// How much taller than the image given the image the codes were found in was. Rounding each side to whole pixels can
/// leave it a little different from <paramref name="ScaleX"/>.
/// </param>
public sealed record BarcodeReadResult(IReadOnlyList<BbBarcodeResult> Barcodes, BarcodePreprocessing? Preprocessing, double ScaleX = 1, double ScaleY = 1)
{
    /// <summary>True when at least one code was found.</summary>
    public bool Found => Barcodes.Count > 0;

    /// <summary>The corners of <paramref name="barcode"/> in the image as it was given.</summary>
    public IReadOnlyList<BbScanPoint> CornersOf(BbBarcodeResult barcode)
    {
        ArgumentNullException.ThrowIfNull(barcode);
        return ScaleX == 1 && ScaleY == 1 ? barcode.Corners : [.. barcode.Corners.Select(p => new BbScanPoint((float)(p.X / ScaleX), (float)(p.Y / ScaleY)))];
    }
}

/// <summary>
/// Reads barcodes and QR codes from Bmage images with BbScanner's managed decoders: in the browser without a canvas
/// round trip, or on a server from an uploaded photo.
/// </summary>
/// <remarks>
/// <para>
/// A Bmage image is straight RGBA, the layout BbScanner calls <see cref="BbPixelFormat.Rgba32"/>, so its pixels go to the
/// decoder as they are, without a copy. <see cref="Decode"/> is one attempt; <see cref="Read"/> retries a photo that does
/// not read through Bmage's preprocessing, each a pipeline on a copy, so the image given is never changed.
/// </para>
/// <para>
/// Only preprocessing that turned failures into reads when it was measured is offered: sharpening a soft code, and
/// enlarging a soft one only a few pixels a module. Two others were measured and left out because they rescued
/// nothing: thresholding against the surroundings, since the decoders already binarise locally and so cope with
/// shadows and uneven light, and enlarging and sharpening together, which read nothing either alone did not. Rotation
/// needs none: the decoders find QR codes and other matrix codes at any angle, and linear codes along rows and columns.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var photo = await loader.LoadAsync(file);
/// var read = photo.ReadBarcodes();
/// if (read.Found) Console.WriteLine($"{read.Barcodes[0].Text} (prepared: {read.Preprocessing})");
/// </code>
/// </example>
public static class BmageBarcodes
{
    /// <summary>Decodes the codes in <paramref name="image"/> as it is.</summary>
    /// <param name="image">The image.</param>
    /// <param name="options">The decoders' options; null for <see cref="BbScannerOptions.ForStillImages"/>.</param>
    /// <param name="maxSymbols">The most codes to return.</param>
    public static IReadOnlyList<BbBarcodeResult> Decode(this ImageBuffer image, BbScannerOptions? options = null, int maxSymbols = 16)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSymbols, 1);
        using var decoder = new BbBarcodeDecoder(options ?? BbScannerOptions.ForStillImages());
        return decoder.DecodeAll(image.Bytes, image.Width, image.Height, BbPixelFormat.Rgba32, maxSymbols, suppressDuplicates: false);
    }

    /// <summary>
    /// Decodes the codes in <paramref name="image"/>, trying each of <see cref="BarcodeReadOptions.Preprocessing"/> in
    /// turn until one finds a code.
    /// </summary>
    public static BarcodeReadResult ReadBarcodes(this ImageBuffer image, BarcodeReadOptions? options = null, CancellationToken cancellationToken = default)
        => Read(image, options, cancellationToken);

    /// <inheritdoc cref="ReadBarcodes"/>
    public static BarcodeReadResult Read(ImageBuffer image, BarcodeReadOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= new BarcodeReadOptions();
        var scanner = options.Scanner ?? BbScannerOptions.ForStillImages();
        using var decoder = new BbBarcodeDecoder(scanner);
        foreach (var step in options.Preprocessing.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (step == BarcodePreprocessing.None)
            {
                var found = decoder.DecodeAll(image.Bytes, image.Width, image.Height, BbPixelFormat.Rgba32, options.MaxSymbols, suppressDuplicates: false);
                if (found.Count > 0) return new BarcodeReadResult(found, step);
                continue;
            }
            if (Prepare(image, step, options) is not { } prepared) continue;
            using var processed = prepared.Execute(image, new PipelineExecutionOptions { CancellationToken = cancellationToken });
            var results = decoder.DecodeAll(processed.Bytes, processed.Width, processed.Height, BbPixelFormat.Rgba32, options.MaxSymbols, suppressDuplicates: false);
            // Each side's own scale, from the sizes themselves: rounding them to whole pixels can make the two differ.
            if (results.Count > 0) return new BarcodeReadResult(results, step, processed.Width / (double)image.Width, processed.Height / (double)image.Height);
        }
        return new BarcodeReadResult([], null);
    }

    /// <summary>The pipeline for a step, or null when the step does not apply.</summary>
    private static ImagePipeline? Prepare(ImageBuffer image, BarcodePreprocessing step, BarcodeReadOptions options)
    {
        var longer = Math.Max(image.Width, image.Height);
        switch (step)
        {
            case BarcodePreprocessing.Enlarge:
            {
                if (longer >= options.MinimumSize) return null;
                var scale = options.MinimumSize / (double)longer;
                var width = (int)Math.Round(image.Width * scale);
                var height = (int)Math.Round(image.Height * scale);
                return ImagePipeline.Create().Add(new ResizeOperation(width, height, ResizeMode.Stretch, ResamplingFilter.Bicubic));
            }
            case BarcodePreprocessing.Sharpen:
                return ImagePipeline.Create().Sharpen(1.5f, Math.Max(1f, longer / 800f));
            default:
                throw new ArgumentOutOfRangeException(nameof(step), step, "Unknown preprocessing.");
        }
    }
}
