namespace Bit.BlazorUI;

/// <summary>The opacity tokens (<c>--bit-opa-*</c>).</summary>
public class BitThemeOpacities
{
    /// <summary>
    /// The alpha of an element that is disabled but must keep its own colors - an image, a color
    /// swatch (<c>--bit-opa-dis</c>). Text-bearing controls express disabled through the
    /// <c>--bit-clr-*-dis</c> color tokens instead, never through alpha.
    /// </summary>
    public string? Disabled { get; set; }
}
