namespace Bit.BlazorUI;

/// <summary>
/// A color offered as a swatch by the text and highlight color panels of the <see cref="BitRichTextEditor"/>.
/// </summary>
/// <param name="Value">
/// The color applied, as a hex value (<c>#2563eb</c>). A hex value is also what lets the swatch show it is the color
/// already under the caret.
/// </param>
/// <param name="Name">
/// What the swatch is called - its accessible name and tooltip ("Brand blue"). Without it the value is read out, which
/// a screen reader spells character by character.
/// </param>
public sealed record BitRichTextEditorColor(string Value, string? Name = null);
