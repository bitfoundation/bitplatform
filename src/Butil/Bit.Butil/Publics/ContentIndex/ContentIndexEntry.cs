namespace Bit.Butil;

/// <summary>
/// One piece of already-cached content registered with the browser through the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Content_Index_API">Content Index API</see>.
/// </summary>
public class ContentIndexEntry
{
    /// <summary>
    /// Your identifier for this entry, unique within the service worker registration. Registering
    /// the same id twice replaces the earlier entry rather than adding a second one.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The headline the browser shows. Required.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>The line under the title. Required.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Where to go when the user picks the entry. Must be inside the service worker's scope, and
    /// should be a URL your worker can already serve offline - the entry is a promise that it can.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// What kind of content this is: <c>"article"</c>, <c>"audio"</c>, <c>"video"</c>, or empty for
    /// unspecified. Browsers may group entries by it.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Icons for the browser's UI. At least one is required in Chromium, which fetches each of them
    /// while registering the entry.
    /// </summary>
    public ContentIndexIcon[] Icons { get; set; } = [];
}
