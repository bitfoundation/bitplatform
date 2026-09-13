namespace Bit.Butil;

/// <summary>One cover image offered to the platform as part of a <see cref="MediaMetadata"/>.</summary>
public class MediaArtwork
{
    /// <summary>
    /// The image URL. Must be fetchable by the browser - a URL it cannot load rejects the whole
    /// metadata object, not just this entry.
    /// </summary>
    public string Src { get; set; } = string.Empty;

    /// <summary>The size as <c>"WIDTHxHEIGHT"</c>, e.g. <c>"512x512"</c>. Optional.</summary>
    public string? Sizes { get; set; }

    /// <summary>The MIME type, e.g. <c>"image/png"</c>. Optional.</summary>
    public string? Type { get; set; }
}
