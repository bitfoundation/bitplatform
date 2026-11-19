namespace Bit.Bmage;

/// <summary>How a <see cref="BmageProject"/> is written and read.</summary>
public sealed record BmageProjectOptions
{
    /// <summary>The defaults: no custom codecs, the default image limits, metadata removed, at most 1 GB unpacked.</summary>
    public static BmageProjectOptions Default { get; } = new();

    /// <summary>
    /// Codecs for custom operations, annotations, layers and masks. Their image handling is not used: a project stores
    /// its images itself. Null for the built-in types only.
    /// </summary>
    public BmageJsonOptions? Json { get; init; }

    /// <summary>The largest image a project may hold, checked before an image is decoded.</summary>
    public BmageImageLimits Limits { get; init; } = BmageImageLimits.Default;

    /// <summary>
    /// The most pixels a project read may decode, its original and the images its document uses together, checked
    /// before each is decoded; null for four times <see cref="BmageImageLimits.MaxPixels"/>. A PNG of one colour compresses
    /// to almost nothing, so without this a small file could ask for many large images.
    /// </summary>
    public long? MaxTotalPixels
    {
        get;
        init => field = value is null or > 0 ? value : throw new ArgumentOutOfRangeException(nameof(MaxTotalPixels));
    }

    internal long TotalPixelLimit => MaxTotalPixels ?? (Limits.MaxPixels > long.MaxValue / 4 ? long.MaxValue : Limits.MaxPixels * 4);

    /// <summary>Where the decoded images' pixels come from, or null for the pooled allocator.</summary>
    public IBmagePixelAllocator? Allocator { get; init; }

    /// <summary>
    /// What of the original's metadata the project keeps: removed by default, as an export removes it, so a project
    /// passed on does not carry the photo's location.
    /// </summary>
    public BmageMetadataPolicy OriginalMetadata { get; init; } = BmageMetadataPolicy.Remove;

    /// <summary>The most files a project read may hold.</summary>
    public int MaxEntries
    {
        get;
        init => field = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(MaxEntries));
    } = 1024;

    /// <summary>The most bytes the files a project read holds may unpack to, together.</summary>
    public long MaxUnpackedBytes
    {
        get;
        init => field = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(MaxUnpackedBytes));
    } = 1L << 30;

    /// <summary>The PNG compression level the images are written with, 0 to 9.</summary>
    public int PngCompressionLevel
    {
        get;
        init => field = value is >= 0 and <= 9 ? value : throw new ArgumentOutOfRangeException(nameof(PngCompressionLevel));
    } = 6;
}
