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
    /// Raw inline markup rendered inside the button: an SVG, or the element of an external icon font
    /// (<c>&lt;i class="fa-solid fa-floppy-disk"&gt;&lt;/i&gt;</c>). Takes precedence over <see cref="IconName"/>.
    /// </summary>
    public string Icon { get; init; } = string.Empty;

    /// <summary>
    /// The name of a built-in Fabric MDL2 icon (e.g. <c>BitIconName.Save</c>) drawn inside the button
    /// when <see cref="Icon"/> is empty.
    /// </summary>
    public string? IconName { get; init; }

    /// <summary>
    /// How the item behaves when activated.
    /// </summary>
    public BitMarkdownEditorToolbarItemType Type { get; init; } = BitMarkdownEditorToolbarItemType.Command;

    /// <summary>
    /// The text command to run when <see cref="Type"/> is <see cref="BitMarkdownEditorToolbarItemType.Command"/>.
    /// </summary>
    public BitMarkdownEditorCommand? Command { get; init; }

    /// <summary>
    /// The keyboard shortcut of the item, e.g. <c>"Ctrl+B"</c>: shown in its tooltip and announced through
    /// <c>aria-keyshortcuts</c>. On a <see cref="BitMarkdownEditorToolbarItemType.Command"/> or a
    /// <see cref="BitMarkdownEditorToolbarItemType.Custom"/> item it is also bound - pressed in the editor, it runs the
    /// item, ahead of a built-in shortcut on the same keys - and a custom one is listed in the shortcuts panel.
    /// A bound shortcut needs Ctrl/Cmd or Alt unless it is a function key (<c>"F2"</c>), so it cannot swallow typing.
    /// </summary>
    public string? Shortcut { get; init; }

    /// <summary>
    /// Callback used when <see cref="Type"/> is <see cref="BitMarkdownEditorToolbarItemType.Custom"/>, handed the editor.
    /// A handler written in a component is run as that component's event handler, so it re-renders afterwards.
    /// </summary>
    public Func<BitMarkdownEditor, Task>? OnClick { get; init; }

    /// <summary>
    /// Child items shown in the menu when <see cref="Type"/> is
    /// <see cref="BitMarkdownEditorToolbarItemType.Dropdown"/>.
    /// </summary>
    public IReadOnlyList<BitMarkdownEditorToolbarItem>? Children { get; init; }

    /// <summary>
    /// A short text rendered beside the icon: the label of a menu item, or of a toolbar button that is worth
    /// spelling out (a "Save" or a "Publish"). An item named like one of the default toolbar shows its localized title
    /// (see <see cref="BitMarkdownEditorTexts"/>) instead, so what is on screen is also what is read out.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Keeps a <see cref="BitMarkdownEditorToolbarItemType.Custom"/> item enabled while the
    /// editor is read-only. Editing items are disabled in read-only mode because they would
    /// change the text; a custom item that only reads it (export, copy, save, ...) sets this
    /// to stay clickable. Has no effect on the built-in editing items, which refuse to run
    /// against a read-only editor either way, nor while the whole component is disabled.
    /// </summary>
    public bool AlwaysEnabled { get; init; }

    /// <summary>
    /// Convenience instance for a toolbar separator.
    /// </summary>
    public static BitMarkdownEditorToolbarItem Separator { get; } = new() { Type = BitMarkdownEditorToolbarItemType.Separator, Name = "separator" };
}
