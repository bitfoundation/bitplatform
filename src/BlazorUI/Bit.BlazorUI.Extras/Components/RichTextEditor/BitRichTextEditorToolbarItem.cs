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

    /// <summary>Non-empty accessible label / tooltip.</summary>
    public required string AriaLabel { get; init; }

    /// <summary>Action invoked when the item is activated; receives the editor instance.</summary>
    public required Func<BitRichTextEditor, Task> OnActivate { get; init; }
}
