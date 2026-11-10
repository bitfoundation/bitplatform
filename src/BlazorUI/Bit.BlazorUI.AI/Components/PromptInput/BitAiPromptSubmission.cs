namespace Bit.BlazorUI;

/// <summary>
/// What the <see cref="BitAiPromptInput"/> hands over when the user submits.
/// </summary>
/// <param name="Text">The trimmed text.</param>
/// <param name="Attachments">The files staged alongside it.</param>
/// <remarks>
/// The submission owns the attachments from then on: the composer no longer releases their browser streams or previews
/// when it is discarded - unless the submission is turned down (<see cref="Reject"/>), which hands them back to it.
/// </remarks>
public sealed record BitAiPromptSubmission(string Text, IReadOnlyList<BitAiAttachment> Attachments)
{
    /// <summary>
    /// Whether the handler of the submission turned it down (<see cref="Reject"/>).
    /// </summary>
    public bool IsRejected { get; private set; }

    /// <summary>
    /// Turns the submission down, for a handler that could not send it - an upload or a request that failed: the composer
    /// puts the text and the attachments back once the handler returns, so the user can send them again rather than type
    /// them anew.
    /// </summary>
    /// <remarks>
    /// Read once the <c>OnSubmit</c> handler of the composer (or of a <see cref="BitAiChat"/>) has completed. Text typed
    /// in the meantime is kept, after the text put back.
    /// </remarks>
    public void Reject() => IsRejected = true;
}
