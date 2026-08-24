namespace Bit.BlazorUI;

/// <summary>
/// What made the message dismiss, handed to the OnDismissing callback of BitMessage.
/// </summary>
public enum BitMessageDismissReason
{
    /// <summary>
    /// The dismiss button of the message was pressed.
    /// </summary>
    Button,

    /// <summary>
    /// The Escape key was pressed while the focus was inside the message.
    /// </summary>
    Escape,

    /// <summary>
    /// The AutoDismissTime countdown of the message ran out.
    /// </summary>
    AutoDismiss,

    /// <summary>
    /// The DismissAsync method of the message was called.
    /// </summary>
    Programmatic
}
