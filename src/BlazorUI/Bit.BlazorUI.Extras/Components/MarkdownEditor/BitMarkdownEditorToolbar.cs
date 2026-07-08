namespace Bit.BlazorUI;

/// <summary>
/// Provides the default toolbar layout and the inline SVG icons used by it.
/// All icons are hand-authored 24x24 stroke glyphs so the component needs no
/// external icon font or library.
/// </summary>
public static class BitMarkdownEditorToolbar
{
    /// <summary>
    /// The standard, full-featured toolbar used when none is supplied.
    /// </summary>
    public static IReadOnlyList<BitMarkdownEditorToolbarItem> Default { get; } = BuildDefault();

    private static List<BitMarkdownEditorToolbarItem> BuildDefault() =>
    [
        new() { Name = "undo", Title = "Undo", Type = BitMarkdownEditorToolbarItemType.Undo, Icon = Icons.Undo, Shortcut = "Ctrl+Z" },
        new() { Name = "redo", Title = "Redo", Type = BitMarkdownEditorToolbarItemType.Redo, Icon = Icons.Redo, Shortcut = "Ctrl+Y" },
        BitMarkdownEditorToolbarItem.Separator,
        Cmd("bold", "Bold", BitMarkdownEditorCommand.Bold, Icons.Bold, "Ctrl+B"),
        Cmd("italic", "Italic", BitMarkdownEditorCommand.Italic, Icons.Italic, "Ctrl+I"),
        Cmd("strikethrough", "Strikethrough", BitMarkdownEditorCommand.Strikethrough, Icons.Strikethrough, "Ctrl+Shift+S"),
        BitMarkdownEditorToolbarItem.Separator,
        new()
        {
            Name = "heading", Title = "Heading", Type = BitMarkdownEditorToolbarItemType.Dropdown, Icon = Icons.Heading,
            Children =
            [
                HeadingItem("h1", "Heading 1", BitMarkdownEditorCommand.Heading1, Icons.H1),
                HeadingItem("h2", "Heading 2", BitMarkdownEditorCommand.Heading2, Icons.H2),
                HeadingItem("h3", "Heading 3", BitMarkdownEditorCommand.Heading3, Icons.H3),
                HeadingItem("h4", "Heading 4", BitMarkdownEditorCommand.Heading4, Icons.H4),
                HeadingItem("h5", "Heading 5", BitMarkdownEditorCommand.Heading5, Icons.H5),
                HeadingItem("h6", "Heading 6", BitMarkdownEditorCommand.Heading6, Icons.H6),
            ]
        },
        Cmd("quote", "Blockquote", BitMarkdownEditorCommand.Quote, Icons.Quote),
        BitMarkdownEditorToolbarItem.Separator,
        Cmd("ul", "Bullet list", BitMarkdownEditorCommand.UnorderedList, Icons.UnorderedList),
        Cmd("ol", "Numbered list", BitMarkdownEditorCommand.OrderedList, Icons.OrderedList),
        Cmd("task", "Task list", BitMarkdownEditorCommand.TaskList, Icons.TaskList),
        BitMarkdownEditorToolbarItem.Separator,
        Cmd("link", "Link", BitMarkdownEditorCommand.Link, Icons.Link, "Ctrl+K"),
        Cmd("image", "Image", BitMarkdownEditorCommand.Image, Icons.Image),
        Cmd("code", "Inline code", BitMarkdownEditorCommand.InlineCode, Icons.Code),
        Cmd("codeblock", "Code block", BitMarkdownEditorCommand.CodeBlock, Icons.CodeBlock),
        Cmd("table", "Table", BitMarkdownEditorCommand.Table, Icons.Table),
        Cmd("hr", "Horizontal rule", BitMarkdownEditorCommand.HorizontalRule, Icons.HorizontalRule),
        Cmd("clear", "Clear formatting", BitMarkdownEditorCommand.ClearFormatting, Icons.ClearFormatting),
        BitMarkdownEditorToolbarItem.Separator,
        new() { Name = "find", Title = "Find & replace", Type = BitMarkdownEditorToolbarItemType.Find, Icon = Icons.Find, Shortcut = "Ctrl+F" },
        new() { Name = "preview", Title = "Toggle preview mode", Type = BitMarkdownEditorToolbarItemType.TogglePreview, Icon = Icons.Preview },
        new() { Name = "fullscreen", Title = "Toggle full-screen", Type = BitMarkdownEditorToolbarItemType.ToggleFullScreen, Icon = Icons.FullScreen },
        new() { Name = "help", Title = "Keyboard shortcuts", Type = BitMarkdownEditorToolbarItemType.Help, Icon = Icons.Help },
    ];

    private static BitMarkdownEditorToolbarItem Cmd(string name, string title, BitMarkdownEditorCommand command, string icon, string? shortcut = null) =>
        new() { Name = name, Title = title, Command = command, Icon = icon, Shortcut = shortcut, Type = BitMarkdownEditorToolbarItemType.Command };

    private static BitMarkdownEditorToolbarItem HeadingItem(string name, string title, BitMarkdownEditorCommand command, string icon) =>
        new() { Name = name, Title = title, Text = title, Command = command, Icon = icon, Type = BitMarkdownEditorToolbarItemType.Command };

    /// <summary>
    /// Inline SVG glyphs (no fill, currentColor stroke) used by the default toolbar.
    /// </summary>
    public static class Icons
    {
        private const string Open = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">";
        private const string Close = "</svg>";
        private static string S(string body) => Open + body + Close;

        public static readonly string Undo = S("<path d=\"M9 7L4 12l5 5\"/><path d=\"M4 12h11a5 5 0 0 1 0 10h-1\"/>");
        public static readonly string Redo = S("<path d=\"M15 7l5 5-5 5\"/><path d=\"M20 12H9a5 5 0 0 0 0 10h1\"/>");
        public static readonly string Bold = S("<path d=\"M6 4h7a4 4 0 0 1 0 8H6z\"/><path d=\"M6 12h8a4 4 0 0 1 0 8H6z\"/>");
        public static readonly string Italic = S("<line x1=\"19\" y1=\"4\" x2=\"10\" y2=\"4\"/><line x1=\"14\" y1=\"20\" x2=\"5\" y2=\"20\"/><line x1=\"15\" y1=\"4\" x2=\"9\" y2=\"20\"/>");
        public static readonly string Strikethrough = S("<path d=\"M16 4H9a3 3 0 0 0-2.83 4\"/><path d=\"M14 12a4 4 0 0 1 0 8H6\"/><line x1=\"4\" y1=\"12\" x2=\"20\" y2=\"12\"/>");
        public static readonly string Heading = S("<path d=\"M6 4v16M18 4v16M6 12h12\"/>");
        public static readonly string H1 = S("<path d=\"M4 6v12M12 6v12M4 12h8\"/><path d=\"M17 10l3-2v10\"/>");
        public static readonly string H2 = S("<path d=\"M4 6v12M12 6v12M4 12h8\"/><path d=\"M17 10a2 2 0 1 1 4 0c0 1.5-2 2.5-4 5h4\"/>");
        public static readonly string H3 = S("<path d=\"M4 6v12M12 6v12M4 12h8\"/><path d=\"M17 9a2 2 0 1 1 3 1.7A2 2 0 1 1 17 16\"/>");
        public static readonly string H4 = S("<path d=\"M4 6v12M12 6v12M4 12h8\"/><path d=\"M20 8v6h-4l4-6v10\"/>");
        public static readonly string H5 = S("<path d=\"M4 6v12M12 6v12M4 12h8\"/><path d=\"M21 8h-4v4h2a2 2 0 1 1-2 2\"/>");
        public static readonly string H6 = S("<path d=\"M4 6v12M12 6v12M4 12h8\"/><path d=\"M21 9a2 2 0 0 0-4 0v5a2 2 0 1 0 4 0 2 2 0 0 0-4 0\"/>");
        public static readonly string Superscript = S("<path d=\"M4 7l8 10M12 7L4 17\"/><path d=\"M17 8a1.5 1.5 0 1 1 3 0c0 1-3 2-3 3.5h3\"/>");
        public static readonly string Subscript = S("<path d=\"M4 7l8 10M12 7L4 17\"/><path d=\"M17 15.5a1.5 1.5 0 1 1 3 0c0 1-3 2-3 3.5h3\"/>");
        public static readonly string ClearFormatting = S("<path d=\"M7 6h11M10 6l-2 12M4 20h6\"/><path d=\"M15 14l6 6M21 14l-6 6\"/>");
        public static readonly string Find = S("<circle cx=\"11\" cy=\"11\" r=\"7\"/><line x1=\"16\" y1=\"16\" x2=\"21\" y2=\"21\"/>");
        public static readonly string Quote = S("<path d=\"M6 17h3l2-4V7H5v6h3zM14 17h3l2-4V7h-6v6h3z\"/>");
        public static readonly string UnorderedList = S("<line x1=\"8\" y1=\"6\" x2=\"21\" y2=\"6\"/><line x1=\"8\" y1=\"12\" x2=\"21\" y2=\"12\"/><line x1=\"8\" y1=\"18\" x2=\"21\" y2=\"18\"/><circle cx=\"3.5\" cy=\"6\" r=\"1\"/><circle cx=\"3.5\" cy=\"12\" r=\"1\"/><circle cx=\"3.5\" cy=\"18\" r=\"1\"/>");
        public static readonly string OrderedList = S("<line x1=\"10\" y1=\"6\" x2=\"21\" y2=\"6\"/><line x1=\"10\" y1=\"12\" x2=\"21\" y2=\"12\"/><line x1=\"10\" y1=\"18\" x2=\"21\" y2=\"18\"/><path d=\"M4 6h1v4M4 10h2\"/><path d=\"M4 14h2v1.5L4 17v1h2\"/>");
        public static readonly string TaskList = S("<path d=\"M3 6l1.5 1.5L7 5\"/><path d=\"M3 13l1.5 1.5L7 12\"/><line x1=\"11\" y1=\"6\" x2=\"21\" y2=\"6\"/><line x1=\"11\" y1=\"13\" x2=\"21\" y2=\"13\"/><line x1=\"11\" y1=\"19\" x2=\"21\" y2=\"19\"/>");
        public static readonly string Link = S("<path d=\"M10 13a5 5 0 0 0 7 0l2-2a5 5 0 0 0-7-7l-1 1\"/><path d=\"M14 11a5 5 0 0 0-7 0l-2 2a5 5 0 0 0 7 7l1-1\"/>");
        public static readonly string Image = S("<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"2\"/><circle cx=\"8.5\" cy=\"8.5\" r=\"1.5\"/><path d=\"M21 15l-5-5L5 21\"/>");
        public static readonly string Code = S("<polyline points=\"16 18 22 12 16 6\"/><polyline points=\"8 6 2 12 8 18\"/>");
        public static readonly string CodeBlock = S("<rect x=\"3\" y=\"4\" width=\"18\" height=\"16\" rx=\"2\"/><polyline points=\"9 9 7 12 9 15\"/><polyline points=\"15 9 17 12 15 15\"/>");
        public static readonly string Table = S("<rect x=\"3\" y=\"4\" width=\"18\" height=\"16\" rx=\"1\"/><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/><line x1=\"3\" y1=\"15\" x2=\"21\" y2=\"15\"/><line x1=\"12\" y1=\"4\" x2=\"12\" y2=\"20\"/>");
        public static readonly string HorizontalRule = S("<line x1=\"3\" y1=\"12\" x2=\"21\" y2=\"12\"/>");
        public static readonly string Preview = S("<path d=\"M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>");
        public static readonly string FullScreen = S("<path d=\"M8 3H5a2 2 0 0 0-2 2v3M16 3h3a2 2 0 0 1 2 2v3M16 21h3a2 2 0 0 0 2-2v-3M8 21H5a2 2 0 0 1-2-2v-3\"/>");
        public static readonly string Help = S("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M9.1 9a3 3 0 0 1 5.8 1c0 2-3 3-3 3\"/><line x1=\"12\" y1=\"17\" x2=\"12\" y2=\"17\"/>");
    }
}
