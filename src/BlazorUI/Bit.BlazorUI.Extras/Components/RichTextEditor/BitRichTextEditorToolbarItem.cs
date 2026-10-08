namespace Bit.BlazorUI;

/// <summary>A custom toolbar button supplied by the host.</summary>
public sealed class BitRichTextEditorToolbarItem
{
    /// <summary>Unique id used for ordering and lookup.</summary>
    public required string Id { get; init; }

    /// <summary>Text label shown when no icon is provided.</summary>
    public string? Label { get; init; }

    /// <summary>Optional icon content.</summary>
    public RenderFragment? Icon { get; init; }

    /// <summary>
    /// Optional accessible label / tooltip. When omitted (or blank), <see cref="Label"/> serves
    /// as the accessible name; a bare <see cref="Id"/> is never used as the accessible name.
    /// </summary>
    public string? AriaLabel { get; init; }

    /// <summary>
    /// Whether the item shows as pressed for the formatting at the caret - the way the built-in Bold button does - read
    /// off the same snapshot the toolbar highlights itself from. Null makes a plain action button, with no pressed
    /// state to announce.
    /// </summary>
    public Func<BitRichTextEditorSelectionState, bool>? IsActive { get; init; }

    /// <summary>Action invoked when the item is activated; receives the editor instance.</summary>
    public required Func<BitRichTextEditor, Task> OnActivate { get; init; }
}
