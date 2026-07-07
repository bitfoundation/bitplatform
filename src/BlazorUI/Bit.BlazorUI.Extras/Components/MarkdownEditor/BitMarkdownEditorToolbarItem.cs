namespace Bit.BlazorUI;

/// <summary>
/// Describes a single button (or separator) in the BitMarkdownEditor toolbar.
/// The toolbar is fully data-driven, so consumers can reorder, remove,
/// or add items by supplying their own list to the Toolbar parameter.
/// </summary>
public class BitMarkdownEditorToolbarItem
{
    /// <summary>
    /// Stable identifier, handy for tests and custom styling.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Tooltip / accessible label shown to the user.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Raw inline SVG markup rendered inside the button.
    /// </summary>
    public string Icon { get; init; } = string.Empty;

    /// <summary>
    /// How the item behaves when activated.
    /// </summary>
    public BitMarkdownEditorToolbarItemType Type { get; init; } = BitMarkdownEditorToolbarItemType.Command;

    /// <summary>
    /// The text command to run when <see cref="Type"/> is <see cref="BitMarkdownEditorToolbarItemType.Command"/>.
    /// </summary>
    public BitMarkdownEditorCommand? Command { get; init; }

    /// <summary>
    /// Optional human readable shortcut hint, e.g. "Ctrl+B".
    /// </summary>
    public string? Shortcut { get; init; }

    /// <summary>
    /// Callback used when <see cref="Type"/> is <see cref="BitMarkdownEditorToolbarItemType.Custom"/>.
    /// </summary>
    public Func<BitMarkdownEditor, Task>? OnClick { get; init; }

    /// <summary>
    /// Convenience instance for a toolbar separator.
    /// </summary>
    public static BitMarkdownEditorToolbarItem Separator { get; } = new() { Type = BitMarkdownEditorToolbarItemType.Separator, Name = "separator" };
}
