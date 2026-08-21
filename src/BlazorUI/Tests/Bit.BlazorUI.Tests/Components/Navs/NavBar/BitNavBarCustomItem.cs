using System.Collections.Generic;

namespace Bit.BlazorUI.Tests.Components.Navs.NavBar;

/// <summary>
/// A custom item type whose every property is named differently from the BitNavBarItem it maps onto, so a
/// NameSelectors test cannot pass by accidentally falling back to the built-in item class.
/// </summary>
public class BitNavBarCustomItem
{
    public string? Caption { get; set; }
    public string? Glyph { get; set; }
    public BitIconInfo? GlyphInfo { get; set; }
    public string? Link { get; set; }
    public IEnumerable<string>? ExtraLinks { get; set; }
    public BitNavMatch? Matching { get; set; }
    public string? Counter { get; set; }
    public bool Marker { get; set; }
    public bool Disabled { get; set; }
    public string? CssClass { get; set; }
    public string? CssStyle { get; set; }
    public string? Tooltip { get; set; }
    public string? Label { get; set; }
    public string? Window { get; set; }
    public string? Identifier { get; set; }
    public BitNavAriaCurrent? Current { get; set; }
}
