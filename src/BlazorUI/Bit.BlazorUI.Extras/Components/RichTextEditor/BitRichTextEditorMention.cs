namespace Bit.BlazorUI;

/// <summary>
/// A single suggestion offered by the mention menu.
/// </summary>
/// <param name="Id">
/// Stable identifier written into the inserted markup as <c>data-mention-id</c>, so the host can
/// resolve the mention back to a record when the HTML is read.
/// </param>
/// <param name="Display">The text shown in the menu and inserted after the trigger character.</param>
/// <param name="Description">Optional secondary line shown under the display text in the menu.</param>
public sealed record BitRichTextEditorMention(string Id, string Display, string? Description = null);
