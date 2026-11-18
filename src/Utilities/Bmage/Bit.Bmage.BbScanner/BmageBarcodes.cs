using Bit.BbScanner;

namespace Bit.Bmage;

/// <summary>
/// Reads barcodes and QR codes from Bmage images with BbScanner's managed decoders: in the browser without a canvas
/// round trip, or on a server from an uploaded photo.
/// </summary>
/// <remarks>
/// <para>
/// A Bmage image is straight RGBA, the layout BbScanner calls <see cref="BbScannerPixelFormat.Rgba32"/>, so its pixels go to the
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
    public static IReadOnlyList<BbScannerBarcodeResult> Decode(this BmageImageBuffer image, BbScannerOptions? options = null, int maxSymbols = 16)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSymbols, 1);
        using var decoder = new BbScannerBarcodeDecoder(options ?? BbScannerOptions.ForStillImages());
        return decoder.DecodeAll(image.Bytes, image.Width, image.Height, BbScannerPixelFormat.Rgba32, maxSymbols, suppressDuplicates: false);
    }

    /// <summary>
    /// Decodes the codes in <paramref name="image"/>, trying each of <see cref="BmageBarcodeReadOptions.Preprocessing"/> in
    /// turn until one finds a code.
    /// </summary>
    public static BmageBarcodeReadResult ReadBarcodes(this BmageImageBuffer image, BmageBarcodeReadOptions? options = null, CancellationToken cancellationToken = default)
        => Read(image, options, cancellationToken);

    /// <inheritdoc cref="ReadBarcodes"/>
    public static BmageBarcodeReadResult Read(BmageImageBuffer image, BmageBarcodeReadOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= new BmageBarcodeReadOptions();
        var scanner = options.Scanner ?? BbScannerOptions.ForStillImages();
        using var decoder = new BbScannerBarcodeDecoder(scanner);
        foreach (var step in options.Preprocessing.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (step == BmageBarcodePreprocessing.None)
            {
                var found = decoder.DecodeAll(image.Bytes, image.Width, image.Height, BbScannerPixelFormat.Rgba32, options.MaxSymbols, suppressDuplicates: false);
                if (found.Count > 0) return new BmageBarcodeReadResult(found, step);
                continue;
            }
            if (Prepare(image, step, options) is not { } prepared) continue;
            using var processed = prepared.Execute(image, new BmagePipelineExecutionOptions { CancellationToken = cancellationToken });
            var results = decoder.DecodeAll(processed.Bytes, processed.Width, processed.Height, BbScannerPixelFormat.Rgba32, options.MaxSymbols, suppressDuplicates: false);
            // Each side's own scale, from the sizes themselves: rounding them to whole pixels can make the two differ.
            if (results.Count > 0) return new BmageBarcodeReadResult(results, step, processed.Width / (double)image.Width, processed.Height / (double)image.Height);
        }
        return new BmageBarcodeReadResult([], null);
    }

    /// <summary>The pipeline for a step, or null when the step does not apply.</summary>
    private static BmageImagePipeline? Prepare(BmageImageBuffer image, BmageBarcodePreprocessing step, BmageBarcodeReadOptions options)
    {
        var longer = Math.Max(image.Width, image.Height);
        switch (step)
        {
            case BmageBarcodePreprocessing.Enlarge:
            {
                if (longer >= options.MinimumSize) return null;
                var scale = options.MinimumSize / (double)longer;
                var width = (int)Math.Round(image.Width * scale);
                var height = (int)Math.Round(image.Height * scale);
                return BmageImagePipeline.Create().Add(new BmageResizeOperation(width, height, BmageResizeMode.Stretch, BmageResamplingFilter.Bicubic));
            }
            case BmageBarcodePreprocessing.Sharpen:
                return BmageImagePipeline.Create().Sharpen(1.5f, Math.Max(1f, longer / 800f));
            default:
                throw new ArgumentOutOfRangeException(nameof(step), step, "Unknown preprocessing.");
        }
    }
}
