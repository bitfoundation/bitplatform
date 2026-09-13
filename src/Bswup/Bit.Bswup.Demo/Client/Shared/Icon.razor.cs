using Microsoft.AspNetCore.Components;

namespace Bit.Bswup.Demo.Client.Shared;

/// <summary>
/// The site's icon family. See the comment at the top of Icon.razor for the drawing rules and
/// for how a caller decides whether a glyph is decorative or meaningful.
/// </summary>
public partial class Icon
{
    /// <summary>One of the names in <see cref="Geometries"/>. An unknown name renders nothing.</summary>
    [Parameter, EditorRequired] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Set only when the icon carries meaning that no neighbouring text already carries. Leaving
    /// it null marks the glyph decorative and hides it from assistive technology, which is the
    /// right answer for an icon beside a label or inside an already-named button.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Rendered as the width/height attributes; a CSS rule still wins over them.</summary>
    [Parameter] public string Size { get; set; } = "24";

    [Parameter] public string? Class { get; set; }

    private string? _geometry;

    protected override void OnParametersSet()
    {
        _geometry = Geometries.GetValueOrDefault(Name);

        base.OnParametersSet();
    }

    // The geometry only - every wrapper attribute is on the <svg> in the markup, so adding a
    // glyph is one line here and nothing else. Ordered by where the icons are used, because that
    // is how someone looking for "the one on the feature cards" will search this file.
    private static readonly Dictionary<string, string> Geometries = new(StringComparer.OrdinalIgnoreCase)
    {
        // ---- chrome
        ["menu"] = """<path d="M3.5 6.5h17M3.5 12h17M3.5 17.5h17"/>""",
        ["dismiss"] = """<path d="M6 6l12 12M18 6L6 18"/>""",
        ["sun"] = """<circle cx="12" cy="12" r="4"/><path d="M12 2.5v2M12 19.5v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M2.5 12h2M19.5 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4"/>""",
        ["moon"] = """<path d="M20.5 13.2A8.5 8.5 0 1 1 10.8 3.5a6.6 6.6 0 0 0 9.7 9.7z"/>""",
        ["search"] = """<circle cx="11" cy="11" r="7"/><path d="M20.5 20.5l-4.2-4.2"/>""",
        ["arrow-up"] = """<path d="M12 19.5v-15M5.5 11L12 4.5 18.5 11"/>""",
        ["arrow-right"] = """<path d="M4.5 12h15M13 5.5l6.5 6.5-6.5 6.5"/>""",
        ["arrow-left"] = """<path d="M19.5 12h-15M11 5.5L4.5 12 11 18.5"/>""",
        ["chevron-right"] = """<path d="M9 4.5l7.5 7.5L9 19.5"/>""",
        ["link"] = """<path d="M10 13.8a4 4 0 0 0 5.7 0l3-3a4 4 0 1 0-5.7-5.7l-1.3 1.3"/><path d="M14 10.2a4 4 0 0 0-5.7 0l-3 3a4 4 0 1 0 5.7 5.7l1.3-1.3"/>""",
        ["copy"] = """<rect x="8.5" y="8.5" width="11" height="11" rx="2"/><path d="M15.5 5.5a2 2 0 0 0-2-2h-7a3 3 0 0 0-3 3v7a2 2 0 0 0 2 2"/>""",
        ["checkmark"] = """<path d="M4.5 12.5l5 5 10-11"/>""",

        // ---- brands and destinations
        ["github"] = """<path d="M9.2 19a4.5 4.5 0 0 1-4.4-2.2m10.6 4.7v-3a3 3 0 0 0-.8-2.3c2.7-.3 5.5-1.3 5.5-5.9a4.6 4.6 0 0 0-1.3-3.2 4.3 4.3 0 0 0-.1-3.2s-1-.3-3.4 1.3a11.7 11.7 0 0 0-6.2 0C6.7 3.6 5.7 3.9 5.7 3.9a4.3 4.3 0 0 0-.1 3.2 4.6 4.6 0 0 0-1.3 3.2c0 4.6 2.8 5.6 5.5 5.9a3 3 0 0 0-.8 2.3v3"/>""",
        ["package"] = """<path d="M20.5 8.3v7.4a1.6 1.6 0 0 1-.8 1.4l-6.9 3.9a1.6 1.6 0 0 1-1.6 0l-6.9-3.9a1.6 1.6 0 0 1-.8-1.4V8.3a1.6 1.6 0 0 1 .8-1.4l6.9-3.9a1.6 1.6 0 0 1 1.6 0l6.9 3.9a1.6 1.6 0 0 1 .8 1.4z"/><path d="M3.8 7.4L12 12l8.2-4.6M12 21V12"/>""",
        ["book"] = """<path d="M4 4.5A1.5 1.5 0 0 1 5.5 3H10a3 3 0 0 1 3 3v14a2.5 2.5 0 0 0-2.5-2.5H5.5A1.5 1.5 0 0 1 4 17z"/><path d="M20 4.5A1.5 1.5 0 0 0 18.5 3H14a3 3 0 0 0-3 3v14a2.5 2.5 0 0 1 2.5-2.5h5A1.5 1.5 0 0 0 20 17z"/>""",
        ["play"] = """<circle cx="12" cy="12" r="9"/><path d="M10 8.5l6 3.5-6 3.5z"/>""",
        ["sparkle"] = """<path d="M12 3l1.9 5.1L19 10l-5.1 1.9L12 17l-1.9-5.1L5 10l5.1-1.9z"/><path d="M18.4 15.4l.7 1.9 1.9.7-1.9.7-.7 1.9-.7-1.9-1.9-.7 1.9-.7z"/>""",

        // ---- message bar intents
        ["info"] = """<circle cx="12" cy="12" r="9"/><path d="M12 11v5.5M12 7.8v.4"/>""",
        ["lightbulb"] = """<path d="M9.5 18h5M10 21h4"/><path d="M12 3a6 6 0 0 0-3.5 10.9c.6.4 1 1.1 1 1.9v.2h5v-.2c0-.8.4-1.5 1-1.9A6 6 0 0 0 12 3z"/>""",
        ["warning"] = """<path d="M10.6 4.1L3.2 17a1.6 1.6 0 0 0 1.4 2.4h14.8A1.6 1.6 0 0 0 20.8 17L13.4 4.1a1.6 1.6 0 0 0-2.8 0z"/><path d="M12 9.5v4M12 16.6v.2"/>""",
        ["error"] = """<circle cx="12" cy="12" r="9"/><path d="M12 7.5v5M12 15.9v.2"/>""",

        // ---- product capabilities (the home page's feature cards)
        ["arrow-download"] = """<path d="M12 3.5v11M7.5 10.5L12 15l4.5-4.5"/><path d="M4 16.5v2A2.5 2.5 0 0 0 6.5 21h11a2.5 2.5 0 0 0 2.5-2.5v-2"/>""",
        ["arrow-sync"] = """<path d="M20 12a8 8 0 0 1-13.7 5.6L4 15.3"/><path d="M4 12a8 8 0 0 1 13.7-5.6L20 8.7"/><path d="M20 3.5v5.2h-5.2M4 20.5v-5.2h5.2"/>""",
        ["cloud-off"] = """<path d="M17.4 17.5H7a4.5 4.5 0 0 1-.7-8.9"/><path d="M8.6 5.6A5.5 5.5 0 0 1 17.5 10a4 4 0 0 1 2.9 6.3"/><path d="M3.5 3.5l17 17"/>""",
        ["shield"] = """<path d="M12 3.2l7 2.7v5.4c0 4.3-2.9 8.2-7 9.5-4.1-1.3-7-5.2-7-9.5V5.9z"/><path d="M9.2 12.2l1.9 1.9 3.7-3.9"/>""",
        ["compass"] = """<circle cx="12" cy="12" r="9"/><path d="M15.4 8.6l-1.8 5-5 1.8 1.8-5z"/>""",
        ["settings"] = """<circle cx="12" cy="12" r="3.2"/><path d="M18.8 14a1.5 1.5 0 0 0 .3 1.7l.1.1a1.9 1.9 0 1 1-2.7 2.7l-.1-.1a1.5 1.5 0 0 0-2.6 1.1v.2a1.9 1.9 0 0 1-3.8 0v-.1a1.5 1.5 0 0 0-2.6-1.1l-.1.1a1.9 1.9 0 1 1-2.7-2.7l.1-.1A1.5 1.5 0 0 0 3.5 13h-.2a1.9 1.9 0 0 1 0-3.8h.1a1.5 1.5 0 0 0 1.1-2.6l-.1-.1a1.9 1.9 0 1 1 2.7-2.7l.1.1a1.5 1.5 0 0 0 2.6-1.1v-.2a1.9 1.9 0 0 1 3.8 0v.1a1.5 1.5 0 0 0 2.6 1.1l.1-.1a1.9 1.9 0 1 1 2.7 2.7l-.1.1a1.5 1.5 0 0 0 1.1 2.6h.2a1.9 1.9 0 0 1 0 3.8h-.1a1.5 1.5 0 0 0-1.3.9z"/>""",
        ["database"] = """<ellipse cx="12" cy="6" rx="7.5" ry="3"/><path d="M4.5 6v12c0 1.7 3.4 3 7.5 3s7.5-1.3 7.5-3V6"/><path d="M4.5 12c0 1.7 3.4 3 7.5 3s7.5-1.3 7.5-3"/>""",
        ["broom"] = """<path d="M14.5 3.5l6 6"/><path d="M13.7 6.3L9.5 10.5a2 2 0 0 0 0 2.8l1.2 1.2a2 2 0 0 0 2.8 0l4.2-4.2z"/><path d="M9.8 13.6L4.5 18.9a1.5 1.5 0 0 0 0 2.1 1.5 1.5 0 0 0 2.1 0l5.3-5.3"/>""",

        // ---- the playground and the MCP explorer
        ["pulse"] = """<path d="M2.5 12h4l2-5.5 4 12 2.5-6.5h6.5"/>""",
        ["plug"] = """<path d="M9 3v5M15 3v5"/><path d="M6 8h12v3a6 6 0 0 1-12 0z"/><path d="M12 17v4"/>""",
        ["code"] = """<path d="M9 17.5L3.5 12 9 6.5M15 6.5l5.5 5.5-5.5 5.5"/>""",
        ["bot"] = """<rect x="4" y="8" width="16" height="12" rx="3"/><path d="M12 3v5M9 14v.2M15 14v.2M9.5 17.2a4 4 0 0 0 5 0"/>""",
        ["list"] = """<path d="M9 6.5h11M9 12h11M9 17.5h11M4.5 6.5h.2M4.5 12h.2M4.5 17.5h.2"/>""",
        ["wrench"] = """<path d="M20 6.2a5 5 0 0 1-6.6 6.6l-7 7a2.1 2.1 0 0 1-3-3l7-7A5 5 0 0 1 17 3.2l-3 3 2.9 2.9 3-3z"/>""",
    };
}
