namespace Bit.Bink;

/// <summary>Raised when the text tool wants the host to show a text editor.</summary>
/// <param name="Element">The text element being edited (already in the document).</param>
/// <param name="ViewRect">Where to place the editor, in view (CSS pixel) coordinates.</param>
public readonly record struct BinkTextEditRequest(BinkTextElement Element, BinkRect ViewRect);
