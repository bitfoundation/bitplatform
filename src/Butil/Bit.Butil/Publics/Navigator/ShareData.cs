namespace Bit.Butil;

/// <summary>
/// What the native share sheet is handed. At least one of the three has to be set, and a URL that
/// is not a valid absolute URL makes the whole call fail rather than being dropped.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/share">Navigator.share()</see>
/// </summary>
public class ShareData
{
    // files?: File[];

    /// <summary>The body text to share.</summary>
    public string? Text { get; set; }

    /// <summary>
    /// The title to share. Lower-cased because it is the JSON member name the browser reads;
    /// renaming it would break callers.
    /// </summary>
    public string? title { get; set; }

    /// <summary>
    /// The URL to share, absolute. Lower-cased because it is the JSON member name the browser
    /// reads; renaming it would break callers.
    /// </summary>
    public string? url { get; set; }
}
