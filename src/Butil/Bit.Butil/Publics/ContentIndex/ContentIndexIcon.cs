namespace Bit.Butil;

/// <summary>
/// One icon the browser shows next to an indexed piece of offline content in its own UI (in
/// Chromium: <em>Downloads, Articles for you</em>).
/// </summary>
public class ContentIndexIcon
{
    /// <summary>
    /// The image URL. The browser fetches it while registering the entry, so one it cannot load
    /// fails the whole <see cref="ContentIndex.Add"/> call.
    /// </summary>
    public string Src { get; set; } = string.Empty;

    /// <summary>The size as <c>"WIDTHxHEIGHT"</c>, e.g. <c>"192x192"</c>.</summary>
    public string Sizes { get; set; } = string.Empty;

    /// <summary>The MIME type, e.g. <c>"image/png"</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Accessible label for the image.</summary>
    public string Label { get; set; } = string.Empty;
}
