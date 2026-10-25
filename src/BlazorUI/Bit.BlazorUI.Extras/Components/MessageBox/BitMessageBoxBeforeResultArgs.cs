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
    /// <remarks>
    /// Read-only: this is what was pressed, and the guard's say over it is <see cref="Cancel"/> - to let
    /// the answer through or not. A message box that answers something other than the button that was
    /// pressed is one whose buttons say the wrong thing, and <see cref="BitMessageBox.AnswerAsync"/> is
    /// how an answer no button stands for is given.
    /// </remarks>
    public BitMessageBoxResult Result { get; init; }

    /// <summary>
    /// Set to <c>true</c> to keep the message box open and hand over no answer.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>
    /// Cancelled when the answer is taken back while the guard is still working it out: the close button or the Cancel
    /// button pressed in the meantime, which is never kept waiting on a guard the user has given up on.
    /// </summary>
    /// <remarks>
    /// Hand it to whatever slow work the guard does - a server call - so that work stops with the answer. An answer that
    /// was taken back is never handed over, whatever the guard decides, and an <see cref="OperationCanceledException"/>
    /// of this token thrown out of the guard is not an error.
    /// </remarks>
    public CancellationToken CancellationToken { get; init; }
}
