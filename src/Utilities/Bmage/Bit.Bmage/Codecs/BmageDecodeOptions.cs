using System.Drawing;

namespace Bit.Bmage;

/// <summary>Options controlling decoding.</summary>
public sealed record BmageDecodeOptions
{
    public static BmageDecodeOptions Default { get; } = new();

    /// <summary>Safety limits. Decoders reject images exceeding them before allocating pixel memory.</summary>
    public BmageImageLimits Limits { get; init; } = BmageImageLimits.Default;

    /// <summary>Apply the EXIF orientation so pixels are upright (recommended). The stored orientation is then reset to normal.</summary>
    public bool AutoOrient { get; init; } = true;

    /// <summary>Read EXIF/ICC/XMP metadata into <see cref="BmageImageBuffer.Metadata"/>.</summary>
    public bool ReadMetadata { get; init; } = true;

    /// <summary>
    /// Optional hint: the decoder may produce an image no larger than this (keeping aspect ratio) when it can do so cheaply
    /// (e.g. browser decoders, JPEG DCT scaling). The result may still be larger; callers must resize if exactness matters.
    /// </summary>
    public Size? MaxSize { get; init; }

    /// <summary>File name of the source, stored in metadata for convenience.</summary>
    public string? FileName { get; init; }

    /// <summary>
    /// The most threads the managed decoders may spread the work after decoding over, such as turning a photo upright,
    /// or null for <see cref="BmageOperationContext.DefaultMaxDegreeOfParallelism"/>. An
    /// <see cref="BmageImageProcessor"/> fills it in from its own <see cref="BmageImageProcessor.MaxDegreeOfParallelism"/>.
    /// </summary>
    public int? MaxDegreeOfParallelism
    {
        get;
        init => field = value is < 1 ? throw new ArgumentOutOfRangeException(nameof(MaxDegreeOfParallelism), "At least one thread is needed.") : value;
    }
}
