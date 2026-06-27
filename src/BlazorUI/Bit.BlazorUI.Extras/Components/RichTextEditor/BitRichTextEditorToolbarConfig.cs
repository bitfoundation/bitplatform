namespace Bit.BlazorUI;

/// <summary>
/// Configures toolbar ordering and custom items. Provide via the <c>ToolbarConfig</c> parameter.
/// </summary>
public sealed class BitRichTextEditorToolbarConfig
{
    /// <summary>
    /// Explicit ordering of toolbar entry ids (built-in group ids and custom item ids).
    /// Unknown ids are skipped; omitted enabled entries are appended in default order.
    /// Built-in group ids: history, blockformat, font, inline, color, script, lists, indent,
    /// blocks, link, media, image, table, rule, alignment, direction, emoji, find, source,
    /// fullscreen, clear.
    /// </summary>
    public IReadOnlyList<string>? Order { get; init; }

    /// <summary>Custom toolbar items (max 50 are rendered).</summary>
    public IReadOnlyList<BitRichTextEditorToolbarItem>? CustomItems { get; init; }
}
