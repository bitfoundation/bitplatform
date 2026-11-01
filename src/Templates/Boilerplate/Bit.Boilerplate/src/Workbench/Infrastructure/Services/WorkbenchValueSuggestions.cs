namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Values the component panel suggests for a CSS variable or a text parameter, picked by the words in its name, like
/// margin or line-height; they are a click away under the field's arrow, and any other value can still be typed.
/// </summary>
public static class WorkbenchValueSuggestions
{
    private static readonly string[] lengths = ["0", "0.25rem", "0.5rem", "0.75rem", "1rem", "1.5rem", "2rem"];

    /// <summary>Each kind's name, the words that pick it, matched without dashes and case, and its values; earlier kinds win.</summary>
    public static readonly (string Kind, string[] Words, string[] Values)[] Kinds =
    [
        ("text-transform", ["texttransform"], ["none", "uppercase", "lowercase", "capitalize"]),
        ("font-weight", ["fontweight", "weight"], ["300", "400", "500", "600", "700", "normal", "bold"]),
        ("font-size", ["fontsize", "textsize"], ["0.75rem", "0.875rem", "1rem", "1.125rem", "1.25rem", "1.5rem", "2rem"]),
        ("font-family", ["fontfamily", "family"], ["inherit", "system-ui, sans-serif", "Georgia, serif", "ui-monospace, monospace"]),
        ("line-height", ["lineheight"], ["normal", "1", "1.2", "1.4", "1.5", "1.75", "2"]),
        ("letter-spacing", ["letterspacing", "tracking"], ["normal", "-0.02em", "-0.01em", "0.02em", "0.05em", "0.1em"]),
        ("radius", ["radius", "corner"], ["0", "0.25rem", "0.5rem", "0.75rem", "1rem", "9999px"]),
        ("shadow", ["shadow", "elevation"], ["none", "0 1px 2px rgb(0 0 0 / 20%)", "0 4px 12px rgb(0 0 0 / 20%)", "0 12px 32px rgb(0 0 0 / 25%)"]),
        ("opacity", ["opacity"], ["0", "0.25", "0.5", "0.75", "1"]),
        ("duration", ["duration", "transition", "delay"], ["0s", "100ms", "150ms", "200ms", "300ms", "500ms"]),
        ("border-width", ["borderwidth", "outlinewidth", "strokewidth"], ["0", "1px", "2px", "3px"]),
        ("margin", ["margin"], ["auto", .. lengths]),
        ("spacing", ["padding", "gap", "spacing", "offset", "inset"], lengths),
        ("size", ["width", "height", "size"], ["auto", "100%", "fit-content", "1rem", "1.5rem", "2rem", "2.5rem", "3rem"]),
        ("z-index", ["zindex"], ["0", "1", "10", "100", "1000"]),
        ("display", ["display"], ["none", "block", "inline-block", "flex", "inline-flex", "grid"]),
        ("align", ["align", "justify"], ["start", "center", "end", "stretch", "space-between"]),
        ("white-space", ["whitespace"], ["normal", "nowrap", "pre-wrap"]),
        ("cursor", ["cursor"], ["default", "pointer", "not-allowed", "text"]),
        ("overflow", ["overflow"], ["visible", "hidden", "auto", "scroll"]),
        ("target", ["target"], ["_self", "_blank", "_parent", "_top"])
    ];

    /// <summary>The kind whose words a name contains; null when none does.</summary>
    public static string? KindOf(string name) => Find(name).Kind;

    /// <summary>The values to suggest for a name; none when no kind's words are in it.</summary>
    public static string[] Of(string name) => Find(name).Values ?? [];

    private static (string Kind, string[] Words, string[] Values) Find(string name)
    {
        var compact = name.Replace("-", string.Empty).ToLowerInvariant();

        return Kinds.FirstOrDefault(kind => kind.Words.Any(word => compact.Contains(word, StringComparison.Ordinal)));
    }
}
