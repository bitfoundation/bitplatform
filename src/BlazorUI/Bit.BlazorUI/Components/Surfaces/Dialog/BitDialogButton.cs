namespace Bit.BlazorUI;

/// <summary>
/// One of the three buttons a BitDialog renders of its own.
/// </summary>
public enum BitDialogButton
{
    /// <summary>
    /// The Ok button, which answers the Dialog with <see cref="BitDialogResult.Ok"/>.
    /// </summary>
    Ok,

    /// <summary>
    /// The Cancel button, which answers the Dialog with <see cref="BitDialogResult.Cancel"/>.
    /// </summary>
    Cancel,

    /// <summary>
    /// The close button in the header, which dismisses the Dialog without an answer.
    /// </summary>
    Close
}
