namespace Bit.Brouter.Samples.Core;

/// <summary>One tile/hero of the view-transitions sample (see GalleryPage / GalleryItemPage).</summary>
public sealed record GalleryItem(int Id, string Emoji, string Name, string Gradient);

/// <summary>
/// Static catalog backing the view-transitions sample. The gradient doubles as the shared visual
/// that makes the tile-to-hero morph read clearly.
/// </summary>
public static class GalleryCatalog
{
    public static readonly GalleryItem[] Items =
    [
        new(1, "🌋", "Volcano", "linear-gradient(135deg, #f83600, #f9d423)"),
        new(2, "🌊", "Ocean", "linear-gradient(135deg, #2193b0, #6dd5ed)"),
        new(3, "🌸", "Blossom", "linear-gradient(135deg, #ee9ca7, #ffdde1)"),
        new(4, "🌌", "Nebula", "linear-gradient(135deg, #41295a, #7b2ff7)"),
        new(5, "🍃", "Meadow", "linear-gradient(135deg, #11998e, #38ef7d)"),
        new(6, "🏜️", "Dunes", "linear-gradient(135deg, #c79081, #dfa579)"),
    ];

    public static GalleryItem? Find(int id) => Array.Find(Items, i => i.Id == id);
}
