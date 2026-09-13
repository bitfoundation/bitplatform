namespace Bit.BlazorUI;

/// <summary>
/// Determines how the coin of a BitPersona is decorated while the persona is active.
/// </summary>
public enum BitPersonaActiveAppearance
{
    /// <summary>
    /// Draws a ring around the coin in the coin color, separated from it by a gap in the page background color.
    /// </summary>
    Ring,

    /// <summary>
    /// Lifts the coin with an elevation shadow.
    /// </summary>
    Shadow,

    /// <summary>
    /// Combines the ring and the elevation shadow.
    /// </summary>
    RingShadow
}
