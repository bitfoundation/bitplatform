namespace Bit.BlazorUI;

/// <summary>
/// Arguments for the OnDismissing callback of BitDialog.
/// Set <see cref="Cancel"/> to true to keep the Dialog open.
/// </summary>
public class BitDialogDismissArgs
{
    /// <summary>
    /// Creates a new instance of <see cref="BitDialogDismissArgs"/>.
    /// </summary>
    /// <param name="reason">
    /// What is about to close the Dialog.
    /// </param>
    public BitDialogDismissArgs(BitDialogDismissReason reason)
    {
        Reason = reason;
    }

    /// <summary>
    /// What is about to close the Dialog: one of its three buttons, a click on the overlay, the Escape key,
    /// or a call to one of its Close and Toggle methods.
    /// </summary>
    public BitDialogDismissReason Reason { get; }

    /// <summary>
    /// Set to true to refuse the closing and leave the Dialog where it is.
    /// </summary>
    /// <remarks>
    /// A refused closing is played back the same way a refused dismissal is - the surface shakes once and
    /// <see cref="BitDialog.OnDismissPrevented"/> is raised with the same reason - so the two read alike to
    /// whoever made the gesture.
    /// </remarks>
    public bool Cancel { get; set; }
}
