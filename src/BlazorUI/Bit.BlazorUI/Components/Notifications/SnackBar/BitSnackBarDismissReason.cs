namespace Bit.BlazorUI;

/// <summary>
/// Tells what took a <see cref="BitSnackBarItem"/> off the screen, reported through
/// <see cref="BitSnackBarItem.DismissReason"/> to the dismiss callbacks.
/// </summary>
public enum BitSnackBarDismissReason
{
    /// <summary>
    /// The code that opened the item closed it through <see cref="BitSnackBar.Close(BitSnackBarItem)"/>.
    /// </summary>
    Programmatic,

    /// <summary>
    /// The user pressed the dismiss button of the item.
    /// </summary>
    DismissButton,

    /// <summary>
    /// The user pressed the Escape key while the focus was inside the item.
    /// </summary>
    Escape,

    /// <summary>
    /// The user clicked the item while <see cref="BitSnackBar.DismissOnClick"/> was enabled.
    /// </summary>
    Click,

    /// <summary>
    /// The user swiped the item away while <see cref="BitSnackBar.SwipeToDismiss"/> was enabled.
    /// </summary>
    Swipe,

    /// <summary>
    /// The auto-dismiss countdown of the item ran out.
    /// </summary>
    Timeout,

    /// <summary>
    /// The item was taken away to make room for a newer one under <see cref="BitSnackBar.MaxItems"/>.
    /// </summary>
    Overflow,

    /// <summary>
    /// The host was emptied through <see cref="BitSnackBar.Clear"/>.
    /// </summary>
    Clear,
}
