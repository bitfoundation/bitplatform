namespace Bit.Brouter.Demos.Core;

/// <summary>One tile/hero of the View Transitions gallery demo (see GalleryPage / GalleryItemPage).</summary>
public sealed record GalleryItem(int Id, string Emoji, string Name, string Gradient, string Blurb);

/// <summary>
/// Static catalog backing the View Transitions gallery. The gradient doubles as the shared visual
/// that makes the tile-to-hero morph read clearly.
/// </summary>
public static class GalleryCatalog
{
    public static readonly GalleryItem[] Items =
    [
        new(1, "🌋", "Volcano", "linear-gradient(135deg, #f83600, #f9d423)",
            "The tile you clicked kept its view-transition-name on this page, so the browser morphed it into this hero - position, size and gradient interpolate in one motion."),
        new(2, "🌊", "Ocean", "linear-gradient(135deg, #2193b0, #6dd5ed)",
            "No JavaScript animates this: the CSS view-transition-name on the tile and this hero is the entire wiring; the browser does the rest."),
        new(3, "🌸", "Blossom", "linear-gradient(135deg, #ee9ca7, #ffdde1)",
            "Everything without a matching name (the header, this text) simply cross-fades underneath the morph."),
        new(4, "🌌", "Nebula", "linear-gradient(135deg, #41295a, #7b2ff7)",
            "Use the previous/next links below to hop between items - each hop morphs this hero into the next one directly."),
        new(5, "🍃", "Meadow", "linear-gradient(135deg, #11998e, #38ef7d)",
            "The timing/easing lives in app.css under ::view-transition-group - tune it there, ship no extra code."),
        new(6, "🏜️", "Dunes", "linear-gradient(135deg, #c79081, #dfa579)",
            "On browsers without the View Transitions API this page still works identically - navigation just doesn't animate."),
    ];

    public static GalleryItem? Find(int id) => Array.Find(Items, i => i.Id == id);
}
