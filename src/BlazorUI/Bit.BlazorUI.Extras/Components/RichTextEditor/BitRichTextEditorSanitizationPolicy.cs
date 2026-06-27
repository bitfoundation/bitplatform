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

    /// <summary>A secure default policy covering the editor's standard formatting output.</summary>
    public static BitRichTextEditorSanitizationPolicy Default { get; } = new()
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
            "audio", "video", "iframe", "source"
        },
        AllowedAttributes = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["*"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "style", "class", "dir" },
            ["a"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "title", "target", "rel" },
            ["img"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "src", "alt", "width", "height" },
            ["td"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "colspan", "rowspan" },
            ["th"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "colspan", "rowspan" },
            ["audio"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "src", "controls" },
            ["video"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "src", "controls", "width", "height" },
            ["iframe"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "src", "width", "height", "allow", "allowfullscreen", "frameborder" },
            ["source"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "src", "type" }
        },
        AllowedUriSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "http", "https", "mailto", "tel", "data"
        }
    };
}
