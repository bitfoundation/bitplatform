namespace Bit.BlazorUI;

/// <summary>
/// Toolbar button groups of the BitRichTextEditor. Combine with bitwise OR to choose which
/// groups appear, or use <see cref="All"/> for the default toolbar or <see cref="AllExtended"/>
/// for every available group.
/// </summary>
[Flags]
public enum BitRichTextEditorToolbar
{
    None = 0,
    History = 1 << 0,
    BlockFormat = 1 << 1,
    Inline = 1 << 2,
    Lists = 1 << 3,
    Blocks = 1 << 4,
    Link = 1 << 5,
    Alignment = 1 << 6,
    Clear = 1 << 7,

    // Extended groups (opt-in).
    Image = 1 << 8,
    Color = 1 << 9,
    Font = 1 << 10,
    Indent = 1 << 11,
    Script = 1 << 12,
    Source = 1 << 13,
    Table = 1 << 14,
    Media = 1 << 15,
    Rule = 1 << 16,
    Emoji = 1 << 17,
    Find = 1 << 18,
    FullScreen = 1 << 19,
    Direction = 1 << 20,

    /// <summary>The default toolbar groups.</summary>
    All = History | BlockFormat | Inline | Lists | Blocks | Link | Alignment | Clear,

    /// <summary>Every available toolbar group, including the extended ones.</summary>
    AllExtended = All | Image | Color | Font | Indent | Script | Source
                | Table | Media | Rule | Emoji | Find | FullScreen | Direction
}
