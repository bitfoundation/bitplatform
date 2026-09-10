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

    /// <summary>
    /// Permitted attributes per tag name. Use the key "*" for attributes allowed on any tag.
    /// </summary>
    /// <remarks>
    /// Allowing <c>style</c> does not allow arbitrary CSS: surviving style attributes are further
    /// filtered down to a presentational property allowlist (color, font, alignment, spacing,
    /// sizing, borders) and any declaration whose value references a URL, a script, or a legacy
    /// browser behavior is dropped.
    /// </remarks>
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
    /// iframe is included, but only because the built-in policy is enforced together with a host
    /// allowlist: an iframe survives the sanitize pass only when its source is an https URL on one
    /// of the approved embed hosts (YouTube, YouTube-nocookie, Vimeo). That host restriction is
    /// tied to this default policy - a custom policy that lists the iframe tag is taken at its
    /// word and permits any source its scheme allowlist accepts, so only list it deliberately.
    /// </remarks>
    public static BitRichTextEditorSanitizationPolicy Default => new()
    {
        AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "p", "br", "span", "div",
            "h1", "h2", "h3", "h4", "h5", "h6",
            "strong", "b", "em", "i", "u", "s", "strike", "sub", "sup", "mark",
            "ul", "ol", "li",
            "blockquote", "pre", "code",
            "a", "img", "hr",
            "table", "caption", "colgroup", "col", "thead", "tbody", "tfoot", "tr", "th", "td",
            "audio", "video", "source", "iframe"
        },
        AllowedAttributes = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            // style/align carry the output of the formatting commands themselves (colors, fonts,
            // sizes, alignment, indentation), so dropping them would leave the editor showing
            // formatting the persisted value no longer has. Style values are still filtered down
            // to a presentational CSS property allowlist.
            ["*"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "class", "dir", "style", "align", "title" },
            ["a"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "title", "target", "rel" },
            // A picked mention is a span carrying the host's own id, so it must round-trip.
            ["span"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "data-mention-id" },
            ["img"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "src", "alt", "width", "height", "loading" },
            ["ol"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "start", "type", "reversed" },
            ["ul"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "type" },
            ["li"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "value", "data-checked" },
            ["table"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "border", "cellpadding", "cellspacing", "width", "height" },
            ["col"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "span", "width" },
            ["colgroup"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "span" },
            ["td"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "colspan", "rowspan", "width", "height", "valign", "headers" },
            ["th"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "colspan", "rowspan", "width", "height", "valign", "scope", "abbr" },
            ["audio"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "src", "controls" },
            ["video"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "src", "controls", "width", "height", "poster" },
            ["source"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "src", "type" },
            ["iframe"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "src", "width", "height", "allow", "allowfullscreen", "frameborder", "loading", "referrerpolicy" }
        },
        AllowedUriSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "http", "https", "mailto", "tel"
        }
    };
}
