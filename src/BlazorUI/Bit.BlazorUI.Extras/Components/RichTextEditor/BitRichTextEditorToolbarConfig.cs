namespace Bit.BlazorUI;

/// <summary>
/// Configures toolbar ordering and custom items. Provide via the <c>ToolbarConfig</c> parameter.
/// </summary>
public sealed class BitRichTextEditorToolbarConfig
{
    /// <summary>
    /// Stable identifiers for the built-in toolbar groups. Use these when building
    /// <see cref="Order"/> instead of copying the string literals from the documentation.
    /// </summary>
    public static class GroupIds
    {
        public const string History = "history";
        public const string BlockFormat = "blockformat";
        public const string Font = "font";
        public const string Inline = "inline";
        public const string Color = "color";
        public const string Script = "script";
        public const string Lists = "lists";
        public const string Indent = "indent";
        public const string Blocks = "blocks";
        public const string Link = "link";
        public const string Media = "media";
        public const string Image = "image";
        public const string Table = "table";
        public const string Rule = "rule";
        public const string Alignment = "alignment";
        public const string Direction = "direction";
        public const string Emoji = "emoji";
        public const string Find = "find";
        public const string Source = "source";
        public const string FullScreen = "fullscreen";
        public const string Clear = "clear";
    }

    /// <summary>
    /// Explicit ordering of toolbar entry ids (built-in group ids and custom item ids).
    /// Unknown ids are skipped; omitted enabled entries are appended in default order.
    /// Use <see cref="GroupIds"/> for the built-in group ids: history, blockformat, font,
    /// inline, color, script, lists, indent, blocks, link, media, image, table, rule,
    /// alignment, direction, emoji, find, source, fullscreen, clear.
    /// </summary>
    public IReadOnlyList<string>? Order { get; init; }

    /// <summary>Custom toolbar items (max 50 are rendered).</summary>
    public IReadOnlyList<BitRichTextEditorToolbarItem>? CustomItems { get; init; }
}
