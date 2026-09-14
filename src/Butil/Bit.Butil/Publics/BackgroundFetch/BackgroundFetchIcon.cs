namespace Bit.Butil;

/// <summary>
/// One icon offered to the browser for the download's own UI - the notification and the download
/// shelf it shows while a background fetch runs.
/// </summary>
public class BackgroundFetchIcon
{
    /// <summary>The image URL. Must be fetchable by the browser; one that isn't fails the whole fetch call.</summary>
    public string Src { get; set; } = string.Empty;

    /// <summary>The size as <c>"WIDTHxHEIGHT"</c>, e.g. <c>"192x192"</c>. Optional.</summary>
    public string? Sizes { get; set; }

    /// <summary>The MIME type, e.g. <c>"image/png"</c>. Optional.</summary>
    public string? Type { get; set; }

    /// <summary>Accessible label for the image. Optional.</summary>
    public string? Label { get; set; }
}
