namespace Bit.BlazorUI;

/// <summary>
/// The arguments of the <see cref="BitMessageBox.OnBeforeResult"/> callback, which is asked before a
/// <see cref="BitMessageBox"/> hands over the answer a button of its own was pressed for.
/// </summary>
/// <remarks>
/// Setting <see cref="Cancel"/> to <c>true</c> keeps the message box where it is: neither
/// <see cref="BitMessageBox.OnResult"/> nor the per-button callback nor
/// <see cref="BitMessageBox.OnClose"/> is raised, so a message box shown through the
/// <see cref="BitMessageBoxService"/> stays open and the caller keeps waiting for its answer.
/// <br/>
/// This is the guard for the buttons the message box draws. The ways the layer around it is dismissed -
/// the Escape key, a click on the overlay - are the modal's, and
/// <see cref="BitModalParameters.CanClose"/> is what guards those.
/// </remarks>
public class BitMessageBoxBeforeResultArgs
{
    /// <summary>
    /// The answer that is about to be handed over: the result of the button that was pressed, or
    /// <see cref="BitMessageBoxResult.None"/> for the close button.
    /// </summary>
    public BitMessageBoxResult Result { get; set; }

    /// <summary>
    /// Set to <c>true</c> to keep the message box open and hand over no answer.
    /// </summary>
    public bool Cancel { get; set; }
}
