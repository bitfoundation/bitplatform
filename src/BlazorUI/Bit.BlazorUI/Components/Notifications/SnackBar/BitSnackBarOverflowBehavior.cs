namespace Bit.BlazorUI;

/// <summary>
/// Determines what a <see cref="BitSnackBar"/> does with a new item that arrives while
/// <see cref="BitSnackBar.MaxItems"/> is already reached.
/// </summary>
public enum BitSnackBarOverflowBehavior
{
    /// <summary>
    /// Dismisses the oldest item on screen to make room for the new one. This is the default.
    /// </summary>
    DismissOldest,

    /// <summary>
    /// Holds the new item back until a slot frees up, then shows it in the order it arrived.
    /// </summary>
    Queue,

    /// <summary>
    /// Drops the new item, leaving what is already on screen untouched.
    /// </summary>
    Skip,
}
