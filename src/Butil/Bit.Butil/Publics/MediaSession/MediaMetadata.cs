namespace Bit.Butil;

/// <summary>
/// What the platform shows for the current track, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaMetadata">MediaMetadata</see>.
/// </summary>
public class MediaMetadata
{
    public string Title { get; set; } = string.Empty;

    public string Artist { get; set; } = string.Empty;

    public string Album { get; set; } = string.Empty;

    /// <summary>
    /// Cover images. Offering several sizes lets each surface pick what fits - a lock screen wants
    /// a large one, a system tray a small one.
    /// </summary>
    public MediaArtwork[] Artwork { get; set; } = [];
}
