namespace Bit.BlazorUI;

/// <summary>
/// The glyphs of the toolbar buttons: stroked SVGs drawn in <c>currentColor</c>, so a button takes its color from the
/// theme (and the --bit-RichTextEditor-button-* variables) the way its text would, which an emoji or a Unicode symbol
/// - painted in its own colors, and differently on every platform - never did. They are decorative: every button is
/// named by its aria-label.
/// </summary>
internal static class BitRichTextEditorIcons
{
    private const string Open = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\">";
    private const string Close = "</svg>";

    // The glyphs that point along the line (history, indentation) are mirrored in a right-to-left layout.
    private const string OpenDirectional = "<svg class=\"bit-rte-flip\" xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\">";

    private static MarkupString S(string body) => new(Open + body + Close);

    private static MarkupString D(string body) => new(OpenDirectional + body + Close);

    private const string TableFrame = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"16\" rx=\"1\"/>";

    public static readonly MarkupString Undo = D("<path d=\"M9 7L4 12l5 5\"/><path d=\"M4 12h11a5 5 0 0 1 0 10h-1\"/>");
    public static readonly MarkupString Redo = D("<path d=\"M15 7l5 5-5 5\"/><path d=\"M20 12H9a5 5 0 0 0 0 10h1\"/>");

    public static readonly MarkupString Bold = S("<path d=\"M6 4h7a4 4 0 0 1 0 8H6z\"/><path d=\"M6 12h8a4 4 0 0 1 0 8H6z\"/>");
    public static readonly MarkupString Italic = S("<line x1=\"19\" y1=\"4\" x2=\"10\" y2=\"4\"/><line x1=\"14\" y1=\"20\" x2=\"5\" y2=\"20\"/><line x1=\"15\" y1=\"4\" x2=\"9\" y2=\"20\"/>");
    public static readonly MarkupString Underline = S("<path d=\"M6 4v6a6 6 0 0 0 12 0V4\"/><line x1=\"4\" y1=\"21\" x2=\"20\" y2=\"21\"/>");
    public static readonly MarkupString Strikethrough = S("<path d=\"M16 4H9a3 3 0 0 0-2.83 4\"/><path d=\"M14 12a4 4 0 0 1 0 8H6\"/><line x1=\"4\" y1=\"12\" x2=\"20\" y2=\"12\"/>");
    public static readonly MarkupString InlineCode = S("<polyline points=\"16 18 22 12 16 6\"/><polyline points=\"8 6 2 12 8 18\"/>");
    public static readonly MarkupString Subscript = S("<path d=\"M4 5l8 10M12 5L4 15\"/><path d=\"M17 15.5a1.5 1.5 0 1 1 3 0c0 1-3 2-3 3.5h3\"/>");
    public static readonly MarkupString Superscript = S("<path d=\"M4 9l8 10M12 9L4 19\"/><path d=\"M17 6a1.5 1.5 0 1 1 3 0c0 1-3 2-3 3.5h3\"/>");

    public static readonly MarkupString TextColor = S("<path d=\"M6 16L12 3l6 13\"/><path d=\"M8.5 11h7\"/><line x1=\"4\" y1=\"21\" x2=\"20\" y2=\"21\"/>");
    public static readonly MarkupString RemoveTextColor = S("<path d=\"M6 16L12 3l6 13\"/><path d=\"M8.5 11h7\"/><line x1=\"3\" y1=\"3\" x2=\"21\" y2=\"21\"/>");
    public static readonly MarkupString Highlight = S("<path d=\"M9 11l-6 6v3h9l3-3\"/><path d=\"M22 12l-4.6 4.6a2 2 0 0 1-2.8 0l-5.2-5.2a2 2 0 0 1 0-2.8L14 4\"/>");
    public static readonly MarkupString RemoveHighlight = S("<path d=\"M9 11l-6 6v3h9l3-3\"/><path d=\"M22 12l-4.6 4.6a2 2 0 0 1-2.8 0l-5.2-5.2a2 2 0 0 1 0-2.8L14 4\"/><line x1=\"3\" y1=\"3\" x2=\"21\" y2=\"21\"/>");

    public static readonly MarkupString UnorderedList = S("<line x1=\"8\" y1=\"6\" x2=\"21\" y2=\"6\"/><line x1=\"8\" y1=\"12\" x2=\"21\" y2=\"12\"/><line x1=\"8\" y1=\"18\" x2=\"21\" y2=\"18\"/><circle cx=\"3.5\" cy=\"6\" r=\"1\"/><circle cx=\"3.5\" cy=\"12\" r=\"1\"/><circle cx=\"3.5\" cy=\"18\" r=\"1\"/>");
    public static readonly MarkupString OrderedList = S("<line x1=\"10\" y1=\"6\" x2=\"21\" y2=\"6\"/><line x1=\"10\" y1=\"12\" x2=\"21\" y2=\"12\"/><line x1=\"10\" y1=\"18\" x2=\"21\" y2=\"18\"/><path d=\"M4 6h1v4M4 10h2\"/><path d=\"M4 14h2v1.5L4 17v1h2\"/>");
    public static readonly MarkupString TaskList = S("<path d=\"M3 6l1.5 1.5L7 5\"/><path d=\"M3 13l1.5 1.5L7 12\"/><line x1=\"11\" y1=\"6\" x2=\"21\" y2=\"6\"/><line x1=\"11\" y1=\"13\" x2=\"21\" y2=\"13\"/><line x1=\"11\" y1=\"19\" x2=\"21\" y2=\"19\"/>");
    public static readonly MarkupString Outdent = D("<line x1=\"21\" y1=\"5\" x2=\"11\" y2=\"5\"/><line x1=\"21\" y1=\"12\" x2=\"11\" y2=\"12\"/><line x1=\"21\" y1=\"19\" x2=\"11\" y2=\"19\"/><polyline points=\"7 8 3 12 7 16\"/>");
    public static readonly MarkupString Indent = D("<line x1=\"21\" y1=\"5\" x2=\"11\" y2=\"5\"/><line x1=\"21\" y1=\"12\" x2=\"11\" y2=\"12\"/><line x1=\"21\" y1=\"19\" x2=\"11\" y2=\"19\"/><polyline points=\"3 8 7 12 3 16\"/>");

    public static readonly MarkupString Quote = S("<path d=\"M6 17h3l2-4V7H5v6h3zM14 17h3l2-4V7h-6v6h3z\"/>");
    public static readonly MarkupString CodeBlock = S("<rect x=\"3\" y=\"4\" width=\"18\" height=\"16\" rx=\"2\"/><polyline points=\"9 9 7 12 9 15\"/><polyline points=\"15 9 17 12 15 15\"/>");

    public static readonly MarkupString Link = S("<path d=\"M10 13a5 5 0 0 0 7 0l2-2a5 5 0 0 0-7-7l-1 1\"/><path d=\"M14 11a5 5 0 0 0-7 0l-2 2a5 5 0 0 0 7 7l1-1\"/>");
    public static readonly MarkupString Unlink = S("<path d=\"M18.8 12.3l1.7-1.8a5 5 0 0 0-7-7l-1.8 1.7\"/><path d=\"M5.2 11.7l-1.7 1.8a5 5 0 0 0 7 7l1.8-1.7\"/><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"5\"/><line x1=\"2\" y1=\"8\" x2=\"5\" y2=\"8\"/><line x1=\"16\" y1=\"19\" x2=\"16\" y2=\"22\"/><line x1=\"19\" y1=\"16\" x2=\"22\" y2=\"16\"/>");
    public static readonly MarkupString Media = S("<rect x=\"2\" y=\"4\" width=\"20\" height=\"16\" rx=\"2\"/><path d=\"M10 9l5 3-5 3z\"/>");

    public static readonly MarkupString Image = S("<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"2\"/><circle cx=\"8.5\" cy=\"8.5\" r=\"1.5\"/><path d=\"M21 15l-5-5L5 21\"/>");
    public static readonly MarkupString ImageLeft = S("<rect x=\"3\" y=\"5\" width=\"9\" height=\"8\" rx=\"1\"/><line x1=\"15\" y1=\"6\" x2=\"21\" y2=\"6\"/><line x1=\"15\" y1=\"12\" x2=\"21\" y2=\"12\"/><line x1=\"3\" y1=\"18\" x2=\"21\" y2=\"18\"/>");
    public static readonly MarkupString ImageCenter = S("<rect x=\"7\" y=\"4\" width=\"10\" height=\"9\" rx=\"1\"/><line x1=\"3\" y1=\"17\" x2=\"21\" y2=\"17\"/><line x1=\"6\" y1=\"21\" x2=\"18\" y2=\"21\"/>");
    public static readonly MarkupString ImageRight = S("<rect x=\"12\" y=\"5\" width=\"9\" height=\"8\" rx=\"1\"/><line x1=\"3\" y1=\"6\" x2=\"9\" y2=\"6\"/><line x1=\"3\" y1=\"12\" x2=\"9\" y2=\"12\"/><line x1=\"3\" y1=\"18\" x2=\"21\" y2=\"18\"/>");

    public static readonly MarkupString Table = S(TableFrame + "<line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/><line x1=\"3\" y1=\"15\" x2=\"21\" y2=\"15\"/><line x1=\"12\" y1=\"4\" x2=\"12\" y2=\"20\"/>");
    public static readonly MarkupString TableRowAbove = S("<rect x=\"3\" y=\"11\" width=\"18\" height=\"10\" rx=\"1\"/><line x1=\"3\" y1=\"16\" x2=\"21\" y2=\"16\"/><path d=\"M12 2v7M8.5 5.5h7\"/>");
    public static readonly MarkupString TableRowBelow = S("<rect x=\"3\" y=\"3\" width=\"18\" height=\"10\" rx=\"1\"/><line x1=\"3\" y1=\"8\" x2=\"21\" y2=\"8\"/><path d=\"M12 15v7M8.5 18.5h7\"/>");
    public static readonly MarkupString TableColumnBefore = S("<rect x=\"10\" y=\"3\" width=\"11\" height=\"18\" rx=\"1\"/><line x1=\"15.5\" y1=\"3\" x2=\"15.5\" y2=\"21\"/><path d=\"M4.5 8.5v7M1 12h7\"/>");
    public static readonly MarkupString TableColumnAfter = S("<rect x=\"3\" y=\"3\" width=\"11\" height=\"18\" rx=\"1\"/><line x1=\"8.5\" y1=\"3\" x2=\"8.5\" y2=\"21\"/><path d=\"M19.5 8.5v7M16 12h7\"/>");
    public static readonly MarkupString TableRowDelete = S("<rect x=\"3\" y=\"10\" width=\"18\" height=\"7\" rx=\"1\"/><path d=\"M9 2l5 5M14 2l-5 5\"/>");
    public static readonly MarkupString TableColumnDelete = S("<rect x=\"9\" y=\"3\" width=\"8\" height=\"18\" rx=\"1\"/><path d=\"M2 8l5 5M7 8l-5 5\"/>");
    public static readonly MarkupString TableHeaderRow = S(TableFrame + "<line x1=\"3\" y1=\"9\" x2=\"21\" y2=\"9\"/><line x1=\"3\" y1=\"6.5\" x2=\"21\" y2=\"6.5\"/><line x1=\"12\" y1=\"9\" x2=\"12\" y2=\"20\"/>");
    public static readonly MarkupString TableMerge = S(TableFrame + "<path d=\"M3 12h5M6 9.5L8.5 12 6 14.5M21 12h-5M18 9.5L15.5 12l2.5 2.5\"/>");
    public static readonly MarkupString TableSplit = S(TableFrame + "<line x1=\"12\" y1=\"4\" x2=\"12\" y2=\"20\"/><path d=\"M9 12H5M7 10l-2 2 2 2M15 12h4M17 10l2 2-2 2\"/>");
    public static readonly MarkupString TableDelete = S(TableFrame + "<path d=\"M9 9l6 6M15 9l-6 6\"/>");

    public static readonly MarkupString HorizontalRule = S("<line x1=\"3\" y1=\"12\" x2=\"21\" y2=\"12\"/><line x1=\"3\" y1=\"6\" x2=\"9\" y2=\"6\" opacity=\".4\"/><line x1=\"3\" y1=\"18\" x2=\"9\" y2=\"18\" opacity=\".4\"/>");

    public static readonly MarkupString AlignLeft = S("<line x1=\"3\" y1=\"6\" x2=\"21\" y2=\"6\"/><line x1=\"3\" y1=\"12\" x2=\"13\" y2=\"12\"/><line x1=\"3\" y1=\"18\" x2=\"17\" y2=\"18\"/>");
    public static readonly MarkupString AlignCenter = S("<line x1=\"3\" y1=\"6\" x2=\"21\" y2=\"6\"/><line x1=\"7\" y1=\"12\" x2=\"17\" y2=\"12\"/><line x1=\"5\" y1=\"18\" x2=\"19\" y2=\"18\"/>");
    public static readonly MarkupString AlignRight = S("<line x1=\"3\" y1=\"6\" x2=\"21\" y2=\"6\"/><line x1=\"11\" y1=\"12\" x2=\"21\" y2=\"12\"/><line x1=\"7\" y1=\"18\" x2=\"21\" y2=\"18\"/>");
    public static readonly MarkupString AlignJustify = S("<line x1=\"3\" y1=\"6\" x2=\"21\" y2=\"6\"/><line x1=\"3\" y1=\"12\" x2=\"21\" y2=\"12\"/><line x1=\"3\" y1=\"18\" x2=\"21\" y2=\"18\"/>");

    public static readonly MarkupString LeftToRight = S("<path d=\"M10 3v9M14 3v9M16 3H9.5a3 3 0 0 0 0 6H10\"/><path d=\"M4 17h16M17 14l3 3-3 3\"/>");
    public static readonly MarkupString RightToLeft = S("<path d=\"M10 3v9M14 3v9M16 3H9.5a3 3 0 0 0 0 6H10\"/><path d=\"M20 17H4M7 14l-3 3 3 3\"/>");

    public static readonly MarkupString Emoji = S("<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M8 14s1.5 2 4 2 4-2 4-2\"/><line x1=\"9\" y1=\"9\" x2=\"9.01\" y2=\"9\"/><line x1=\"15\" y1=\"9\" x2=\"15.01\" y2=\"9\"/>");
    public static readonly MarkupString Find = S("<circle cx=\"11\" cy=\"11\" r=\"7\"/><line x1=\"16\" y1=\"16\" x2=\"21\" y2=\"21\"/>");
    public static readonly MarkupString Previous = S("<polyline points=\"18 15 12 9 6 15\"/>");
    public static readonly MarkupString Next = S("<polyline points=\"6 9 12 15 18 9\"/>");
    public static readonly MarkupString Source = S("<polyline points=\"16 18 22 12 16 6\"/><polyline points=\"8 6 2 12 8 18\"/><line x1=\"14\" y1=\"4\" x2=\"10\" y2=\"20\"/>");
    public static readonly MarkupString FullScreen = S("<path d=\"M8 3H5a2 2 0 0 0-2 2v3M16 3h3a2 2 0 0 1 2 2v3M16 21h3a2 2 0 0 0 2-2v-3M8 21H5a2 2 0 0 1-2-2v-3\"/>");
    public static readonly MarkupString ExitFullScreen = S("<path d=\"M8 3v3a2 2 0 0 1-2 2H3M21 8h-3a2 2 0 0 1-2-2V3M3 16h3a2 2 0 0 1 2 2v3M16 21v-3a2 2 0 0 1 2-2h3\"/>");
    public static readonly MarkupString ClearFormatting = S("<path d=\"M7 6h11M10 6l-2 12M4 20h6\"/><path d=\"M15 14l6 6M21 14l-6 6\"/>");
}
