namespace Bit.BlazorUI;

/// <summary>
/// Defines the font weights of the typography ramp available in the bit BlazorUI.
/// </summary>
/// <remarks>
/// The members name the steps of the theme's weight scale rather than the numbers behind them, so that a preset
/// which draws its light or its bold somewhere else than the Fluent default re-skins every component that reads
/// the scale. A component reading this scale never writes a literal weight of its own.
/// </remarks>
public enum BitFontWeight
{
    /// <summary>
    /// The lightest step of the weight scale.
    /// </summary>
    Light,

    /// <summary>
    /// The weight of body copy, and the default of nearly every typography variant.
    /// </summary>
    Regular,

    /// <summary>
    /// The step between the body copy and the titles.
    /// </summary>
    Medium,

    /// <summary>
    /// The weight of the titles and of the labels of the interactive controls.
    /// </summary>
    Semibold,

    /// <summary>
    /// The heaviest step of the weight scale.
    /// </summary>
    Bold
}
