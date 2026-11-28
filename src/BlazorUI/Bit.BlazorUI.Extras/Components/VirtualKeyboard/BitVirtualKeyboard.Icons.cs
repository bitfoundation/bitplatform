namespace Bit.BlazorUI;

public partial class BitVirtualKeyboard
{
    // The icons of the built-in keys that show one, drawn inline (24x24, stroked) so no icon font has to carry them.
    private static readonly Dictionary<string, MarkupString> Icons = new(StringComparer.Ordinal)
    {
        ["bksp"] = new("<path d=\"M9 5h11v14H9l-6-7z\" /><path d=\"M12 9l5 6M17 9l-5 6\" />"),
        ["del"] = new("<path d=\"M15 5H4v14h11l6-7z\" /><path d=\"M7 9l5 6M12 9l-5 6\" />"),
        ["enter"] = new("<path d=\"M19 5v7a3 3 0 0 1-3 3H5\" /><path d=\"M9 11l-4 4 4 4\" />"),
        ["tab"] = new("<path d=\"M4 12h14M14 8l4 4-4 4M20 6v12\" />"),
        ["shifttab"] = new("<path d=\"M20 12H6M10 8l-4 4 4 4M4 6v12\" />"),
        ["shift"] = new("<path d=\"M12 4l8 8h-4v7H8v-7H4z\" />"),
        ["caps"] = new("<path d=\"M12 3l8 8h-4v5H8v-5H4z\" /><path d=\"M8 20h8\" />"),
        ["hide"] = new("<rect x=\"3\" y=\"4\" width=\"18\" height=\"11\" rx=\"2\" /><path d=\"M7 8h1M11 8h1M15 8h1M8 11h8M9 18l3 3 3-3\" />"),
        ["lang"] = new("<circle cx=\"12\" cy=\"12\" r=\"8\" /><path d=\"M4 12h16M12 4c3 3 3 13 0 16M12 4c-3 3-3 13 0 16\" />"),
        ["left"] = new("<path d=\"M15 6l-6 6 6 6\" />"),
        ["right"] = new("<path d=\"M9 6l6 6-6 6\" />"),
        ["up"] = new("<path d=\"M6 15l6-6 6 6\" />"),
        ["down"] = new("<path d=\"M6 9l6 6 6-6\" />"),
    };
}