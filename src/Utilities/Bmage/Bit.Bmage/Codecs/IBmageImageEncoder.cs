namespace Bit.Bmage;

/// <summary>Encodes an <see cref="BmageImageBuffer"/> to bytes.</summary>
public interface IBmageImageEncoder
{
    /// <summary>The formats this encoder can produce.</summary>
    IReadOnlyCollection<BmageImageFormat> Formats { get; }

    /// <summary>Encodes the image. Metadata is written according to the options' policy.</summary>
    ValueTask<BmageEncodedImage> EncodeAsync(BmageImageBuffer image, BmageImageExportOptions options, CancellationToken cancellationToken = default);
}
