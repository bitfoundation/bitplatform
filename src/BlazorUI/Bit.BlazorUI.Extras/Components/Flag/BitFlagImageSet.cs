namespace Bit.BlazorUI;

/// <summary>
/// The image sets of the Bit.BlazorUI.Assets package a <see cref="BitFlag"/> can be drawn out of.
/// </summary>
/// <remarks>
/// Every set draws every flag at 16, 24, 32, 48 and 64 pixels, which is what lets a flag stay sharp
/// past the icon sizes of the theme and on a screen of any density.
/// </remarks>
public enum BitFlagImageSet
{
    /// <summary>
    /// The flat artwork, in the same style as the packaged 16 pixel image of the Extras package.
    /// </summary>
    Flat,

    /// <summary>
    /// The shiny artwork, with a gloss and a soft edge over the flag.
    /// </summary>
    Shiny
}
