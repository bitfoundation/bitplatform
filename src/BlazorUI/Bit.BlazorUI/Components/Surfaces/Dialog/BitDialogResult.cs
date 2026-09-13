namespace Bit.BlazorUI;

/// <summary>
/// How a showing of a BitDialog was answered.
/// </summary>
/// <remarks>
/// A Dialog that was dismissed rather than answered - by its close button, by a click on the overlay, by
/// the Escape key, or by the page closing it - reports no result at all rather than one of these, which is
/// why both <see cref="BitDialog.Result"/> and the task <see cref="BitDialog.Show"/> hands back are nullable.
/// </remarks>
public enum BitDialogResult
{
    /// <summary>
    /// The Ok button ended the showing.
    /// </summary>
    Ok,

    /// <summary>
    /// The Cancel button ended the showing.
    /// </summary>
    Cancel
}
