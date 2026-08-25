namespace Bit.BlazorUI;

/// <summary>
/// Determines what a <see cref="BitSnackBar"/> does with a new item that arrives while
/// <see cref="BitSnackBar.MaxItems"/> is already reached.
/// </summary>
public enum BitSnackBarOverflow
{
    /// <summary>
    /// Dismisses the oldest item on screen to make room for the new one. This is the default.
    /// </summary>
    /// <remarks>
    /// The newest notification is the one that matters most in a burst of them, which is what this keeps.
    /// </remarks>
    DismissOldest,

    /// <summary>
    /// Holds the new item back and shows it as soon as one of the items on screen leaves.
    /// </summary>
    /// <remarks>
    /// Nothing is lost this way, at the price of a notification arriving later than what it reports. Reach for it
    /// where every notification has to be seen, and keep the auto-dismiss countdown short enough that the queue
    /// drains - a queue behind persistent items never does.
    /// </remarks>
    Queue,
}
