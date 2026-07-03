namespace Bit.BlazorUI;

/// <summary>
/// An allowlist sanitization policy. Only the listed tags, attributes, and URI schemes are
/// retained; everything else is removed. Supply via <c>SanitizationPolicy</c> to override the
/// secure <see cref="Default"/>.
/// </summary>
public sealed class BitRichTextEditorSanitizationPolicy
{
    /// <summary>Permitted (lowercase) element/tag names.</summary>
    public required ISet<string> AllowedTags { get; init; }

    /// <summary>Permitted attributes per tag name. Use the key "*" for attributes allowed on any tag.</summary>
    public required IDictionary<string, ISet<string>> AllowedAttributes { get; init; }

    /// <summary>Permitted URI schemes for href/src attributes (e.g. http, https, mailto).</summary>
    public required ISet<string> AllowedUriSchemes { get; init; }

    /// <summary>Whether <c>data:</c> image URIs are permitted in image sources.</summary>
    public bool AllowDataImageUris { get; init; } = true;

    /// <summary>
    /// A secure default policy covering the editor's standard formatting output. Returns a
    /// fresh instance on each access so callers can mutate it without affecting other editors.
    /// </summary>
    /// <remarks>
    /// iframe is intentionally excluded: the general sanitize pass does not host-restrict iframe
    /// sources (only the media-insert path enforces the YouTube/Vimeo host allowlist), so allowing
    /// iframe here would permit arbitrary embeds. Media embeds are therefore opt-in - add the
    /// iframe tag and its attributes to a custom policy if such embeds must round-trip.
    /// </remarks>
    public static BitRichTextEditorSanitizationPolicy Default => new()
    {
        AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "p", "br", "span", "div",
            "h1", "h2", "h3", "h4", "h5", "h6",
            "strong", "b", "em", "i", "u", "s", "strike", "sub", "sup",
            "ul", "ol", "li",
            "blockquote", "pre", "code",
            "a", "img", "hr",
            "table", "thead", "tbody", "tr", "th", "td",
            "audio", "video", "source"
        },
        AllowedAttributes = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["*"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "class", "dir" },
            ["a"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "title", "target", "rel" },
            ["img"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "src", "alt", "width", "height" },
            ["td"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "colspan", "rowspan" },
            ["th"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "colspan", "rowspan" },
            ["audio"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "src", "controls" },
            ["video"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "src", "controls", "width", "height" },
            ["source"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "src", "type" }
        },
        AllowedUriSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "http", "https", "mailto", "tel"
        }
    };
}
