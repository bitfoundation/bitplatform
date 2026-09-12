namespace Bit.BlazorUI;

/// <summary>
/// How a showing of a <see cref="BitMessageBox"/> was answered.
/// </summary>
public enum BitMessageBoxResult
{
    /// <summary>
    /// The message box was dismissed rather than answered: its close button, or - for one shown through
    /// the <see cref="BitMessageBoxService"/> - the Escape key, a click on the overlay, or the page
    /// closing the modal itself.
    /// </summary>
    /// <remarks>
    /// This is the default value, which is what tells a walked-away-from message box apart from one whose
    /// Cancel button was pressed. Where the two mean the same thing, treat anything that is not
    /// <see cref="Ok"/> or <see cref="Yes"/> as a refusal.
    /// </remarks>
    None,

    /// <summary>
    /// The Ok button ended the showing.
    /// </summary>
    Ok,

    /// <summary>
    /// The Cancel button ended the showing.
    /// </summary>
    Cancel,

    /// <summary>
    /// The Yes button ended the showing.
    /// </summary>
    Yes,

    /// <summary>
    /// The No button ended the showing.
    /// </summary>
    No
}
