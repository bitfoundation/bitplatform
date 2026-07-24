namespace Bit.BlazorUI;

/// <summary>
/// The result of applying a <see cref="BitMarkdownEditorCommand"/> to a piece of text.
/// Describes the new text content together with the selection range that should
/// be restored in the textarea afterwards.
/// </summary>
/// <param name="Handled">Whether the command produced a change.</param>
/// <param name="Text">The full new text content of the editor.</param>
/// <param name="SelectionStart">The caret/selection start to restore (char index).</param>
/// <param name="SelectionEnd">The caret/selection end to restore (char index).</param>
public readonly record struct BitMarkdownEditorEditResult(bool Handled, string Text, int SelectionStart, int SelectionEnd)
{
    /// <summary>
    /// A no-op result that leaves the supplied text unchanged.
    /// </summary>
    public static BitMarkdownEditorEditResult NotHandled(string text, int start, int end) => new(false, text, start, end);
}
