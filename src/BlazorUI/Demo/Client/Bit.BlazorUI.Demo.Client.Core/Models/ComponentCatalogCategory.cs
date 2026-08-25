namespace Bit.BlazorUI.Demo.Client.Core.Models;

/// <summary>
/// A group of components as the nav groups them. The gallery renders one section per category and
/// one filter chip per category, both from this.
/// </summary>
public sealed class ComponentCatalogCategory
{
    public required string Name { get; init; }

    /// <summary>
    /// A Fabric glyph rather than one of the old per-category SVG assets: the glyph takes the
    /// theme's colors, so it follows the active preset and the accent the switcher applies, where a
    /// two-color asset with a white plate behind it could only ever be right in one scheme.
    /// </summary>
    public required string IconName { get; init; }

    /// <summary>What the category covers, shown under its heading in the gallery.</summary>
    public string Summary { get; init; } = string.Empty;

    public required IReadOnlyList<ComponentCatalogItem> Items { get; init; }

    /// <summary>
    /// The id the gallery's section for this category carries, and so the anchor its heading offers
    /// and the "on this page" rail scrolls to. Derived from the name rather than written out, since
    /// the categories are derived from the nav; prefixed so that a category can never collide with
    /// an anchor a prose page owns.
    /// </summary>
    public string Anchor => $"category-{Slugify(Name)}";

    private static string Slugify(string name)
    {
        var slug = new char[name.Length];
        var length = 0;

        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                slug[length++] = char.ToLowerInvariant(c);
            }
            // One separator per run of them, and never a trailing one.
            else if (length > 0 && slug[length - 1] != '-')
            {
                slug[length++] = '-';
            }
        }

        while (length > 0 && slug[length - 1] == '-') length--;

        return new string(slug, 0, length);
    }
}
