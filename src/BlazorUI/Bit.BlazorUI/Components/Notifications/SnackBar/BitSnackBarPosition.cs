namespace Bit.BlazorUI;

/// <summary>
/// Determines the corner or edge of the screen the snack bars of a <see cref="BitSnackBar"/> are stacked at.
/// </summary>
/// <remarks>
/// The start/end naming follows the text direction rather than the screen, so a stack keeps to the same side of the
/// reading order in both LTR and RTL. The enter animation of an item always slides out of the edge its stack is
/// pinned to.
/// </remarks>
public enum BitSnackBarPosition
{
    /// <summary>
    /// Top of the screen, at the inline start.
    /// </summary>
    TopStart,

    /// <summary>
    /// Top of the screen, centered.
    /// </summary>
    TopCenter,

    /// <summary>
    /// Top of the screen, at the inline end.
    /// </summary>
    TopEnd,

    /// <summary>
    /// Bottom of the screen, at the inline start.
    /// </summary>
    BottomStart,

    /// <summary>
    /// Bottom of the screen, centered.
    /// </summary>
    BottomCenter,

    /// <summary>
    /// Bottom of the screen, at the inline end. This is the default.
    /// </summary>
    BottomEnd,
}
