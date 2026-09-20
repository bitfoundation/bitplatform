namespace Bit.BlazorUI;

/// <summary>
/// A selection range inside the <see cref="BitMarkdownEditor"/> textarea.
/// </summary>
/// <param name="Start">The selection start (char index).</param>
/// <param name="End">The selection end (char index).</param>
/// <param name="Text">The selected text, empty when the selection is a caret.</param>
public readonly record struct BitMarkdownEditorSelection(int Start, int End, string Text)
{
    /// <summary>
    /// True when nothing is selected and the range is a plain caret position.
    /// </summary>
    public bool IsEmpty => Start == End;
}
