namespace Bit.Bmage;

/// <summary>
/// Managed baseline JPEG encoder with 4:2:0 or 4:4:4 chroma subsampling, standard Huffman tables and quality-scaled
/// quantisation tables. Transparent pixels are composited over the export background colour.
/// </summary>
public sealed class BmageJpegEncoder : IBmageImageEncoder
{
    private static readonly BmageImageFormat[] SupportedFormats = [BmageImageFormat.Jpeg];
    public IReadOnlyCollection<BmageImageFormat> Formats => SupportedFormats;

    public ValueTask<BmageEncodedImage> EncodeAsync(BmageImageBuffer image, BmageImageExportOptions options, CancellationToken cancellationToken = default)
        => new(Encode(image, options, cancellationToken));

    public static BmageEncodedImage Encode(BmageImageBuffer image, BmageImageExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= BmageImageExportOptions.Jpeg;
        var quality = Math.Clamp(options.Quality, 0.01, 1.0);
        var subsample = quality < 0.9; // 4:2:0 below 90, 4:4:4 above (matches common encoder behaviour)
        var writer = new BmageJpegWriter(image, quality, subsample, options.Background, cancellationToken);
        var bytes = writer.Write();
        var metadata = image.Metadata?.Apply(options.Metadata, image.Width, image.Height);
        if (metadata is { IsEmpty: false }) bytes = BmageJpegContainer.WriteMetadata(bytes, metadata);
        return new BmageEncodedImage(bytes, BmageImageFormat.Jpeg, image.Width, image.Height, quality);
    }
}
