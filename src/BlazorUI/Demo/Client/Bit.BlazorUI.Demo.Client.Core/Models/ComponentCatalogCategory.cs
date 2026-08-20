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
}
