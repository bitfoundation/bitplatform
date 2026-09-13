namespace Bit.BlazorUI;

/// <summary>
/// What closed the last showing of a BitDialog.
/// </summary>
/// <remarks>
/// <see cref="BitDialogResult"/> reports the answer a showing was given; this reports the gesture that
/// ended it, which is what tells an Escape apart from a click on the overlay when neither leaves an answer.
/// </remarks>
public enum BitDialogDismissReason
{
    /// <summary>
    /// The Ok button ended the showing.
    /// </summary>
    OkButton,

    /// <summary>
    /// The Cancel button ended the showing.
    /// </summary>
    CancelButton,

    /// <summary>
    /// The close button in the header ended the showing.
    /// </summary>
    CloseButton,

    /// <summary>
    /// A click on the overlay ended the showing.
    /// </summary>
    OverlayClick,

    /// <summary>
    /// The Escape key ended the showing.
    /// </summary>
    Escape,

    /// <summary>
    /// The page closed the Dialog itself, by setting IsOpen or by calling Close or Toggle.
    /// </summary>
    Programmatic
}
