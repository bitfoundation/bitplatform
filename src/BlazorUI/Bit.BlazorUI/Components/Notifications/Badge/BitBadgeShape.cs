namespace Bit.BlazorUI;

/// <summary>
/// Determines the corner shape of the <see cref="BitBadge"/>.
/// </summary>
public enum BitBadgeShape
{
    /// <summary>
    /// Fully rounded corners, so a counter reads as a circle and a longer label as a pill.
    /// </summary>
    Circular,

    /// <summary>
    /// The corner radius the current theme gives to its controls.
    /// </summary>
    Rounded,

    /// <summary>
    /// Square corners with no radius at all.
    /// </summary>
    Square
}
