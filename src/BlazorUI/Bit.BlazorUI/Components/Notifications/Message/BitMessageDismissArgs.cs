namespace Bit.BlazorUI;

/// <summary>
/// Arguments for the OnDismissing callback of BitMessage.
/// Set <see cref="Cancel"/> to true to keep the message where it is.
/// </summary>
public class BitMessageDismissArgs
{
    /// <summary>
    /// Creates a new instance of <see cref="BitMessageDismissArgs"/>.
    /// </summary>
    /// <param name="reason">
    /// What made the message dismiss.
    /// </param>
    public BitMessageDismissArgs(BitMessageDismissReason reason)
    {
        Reason = reason;
    }

    /// <summary>
    /// What made the message dismiss: its dismiss button, the Escape key, the auto-dismiss countdown,
    /// or a call to the DismissAsync method.
    /// </summary>
    public BitMessageDismissReason Reason { get; }

    /// <summary>
    /// Set to true to cancel the dismissal and keep the message where it is.
    /// </summary>
    public bool Cancel { get; set; }
}
