namespace Bit.BlazorUI;

/// <summary>
/// Reports what took a <see cref="BitSnackBarItem"/> off the screen.
/// </summary>
/// <remarks>
/// The reason is written to <see cref="BitSnackBarItem.DismissReason"/> before the dismiss callbacks run, so a
/// handler can tell a notification the user acted on apart from one that simply ran out of time - which is the
/// difference between "the undo was offered and declined" and "the undo was never seen".
/// </remarks>
public enum BitSnackBarDismissReason
{
    /// <summary>
    /// The dismiss button of the item was activated.
    /// </summary>
    DismissButton,

    /// <summary>
    /// The Escape key was pressed while the focus was inside the item.
    /// </summary>
    Escape,

    /// <summary>
    /// The item was clicked while its host had <see cref="BitSnackBar.DismissOnClick"/> enabled.
    /// </summary>
    Click,

    /// <summary>
    /// The item was swiped away.
    /// </summary>
    Swipe,

    /// <summary>
    /// The auto-dismiss countdown of the item ran out.
    /// </summary>
    Timeout,

    /// <summary>
    /// <see cref="BitSnackBar.Close(BitSnackBarItem)"/> was called for the item.
    /// </summary>
    Close,

    /// <summary>
    /// <see cref="BitSnackBar.Clear"/> was called on the host.
    /// </summary>
    Clear,

    /// <summary>
    /// The item was the oldest one on screen when a newer one needed the room <see cref="BitSnackBar.MaxItems"/> caps.
    /// </summary>
    MaxItems,
}
