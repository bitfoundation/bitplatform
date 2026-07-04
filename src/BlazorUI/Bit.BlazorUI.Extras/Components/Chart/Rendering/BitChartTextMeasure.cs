namespace Bit.BlazorUI;

/// <summary>
/// Approximate text measurement without a canvas. Uses a per-character advance-width table
/// (fraction of font size) calibrated for typical sans-serif fonts. This is far more accurate
/// than a flat per-character heuristic and keeps axis/label/legend layout tight.
/// </summary>
public static class BitChartTextMeasure
{
    // Advance widths as a fraction of em (font size) for Helvetica/Arial-like fonts.
    // Values are approximate but good enough for layout reservation.
    private const double Default = 0.55;

    private static readonly Dictionary<char, double> Widths = new()
    {
        [' '] = 0.278, ['!'] = 0.278, ['"'] = 0.355, ['#'] = 0.556, ['$'] = 0.556, ['%'] = 0.889,
        ['&'] = 0.667, ['\''] = 0.191, ['('] = 0.333, [')'] = 0.333, ['*'] = 0.389, ['+'] = 0.584,
        [','] = 0.278, ['-'] = 0.333, ['.'] = 0.278, ['/'] = 0.278,
        ['0'] = 0.556, ['1'] = 0.556, ['2'] = 0.556, ['3'] = 0.556, ['4'] = 0.556, ['5'] = 0.556,
        ['6'] = 0.556, ['7'] = 0.556, ['8'] = 0.556, ['9'] = 0.556,
        [':'] = 0.278, [';'] = 0.278, ['<'] = 0.584, ['='] = 0.584, ['>'] = 0.584, ['?'] = 0.556, ['@'] = 1.015,
        ['A'] = 0.667, ['B'] = 0.667, ['C'] = 0.722, ['D'] = 0.722, ['E'] = 0.667, ['F'] = 0.611,
        ['G'] = 0.778, ['H'] = 0.722, ['I'] = 0.278, ['J'] = 0.5, ['K'] = 0.667, ['L'] = 0.556,
        ['M'] = 0.833, ['N'] = 0.722, ['O'] = 0.778, ['P'] = 0.667, ['Q'] = 0.778, ['R'] = 0.722,
        ['S'] = 0.667, ['T'] = 0.611, ['U'] = 0.722, ['V'] = 0.667, ['W'] = 0.944, ['X'] = 0.667,
        ['Y'] = 0.667, ['Z'] = 0.611,
        ['['] = 0.278, ['\\'] = 0.278, [']'] = 0.278, ['^'] = 0.469, ['_'] = 0.556, ['`'] = 0.333,
        ['a'] = 0.556, ['b'] = 0.556, ['c'] = 0.5, ['d'] = 0.556, ['e'] = 0.556, ['f'] = 0.278,
        ['g'] = 0.556, ['h'] = 0.556, ['i'] = 0.222, ['j'] = 0.222, ['k'] = 0.5, ['l'] = 0.222,
        ['m'] = 0.833, ['n'] = 0.556, ['o'] = 0.556, ['p'] = 0.556, ['q'] = 0.556, ['r'] = 0.333,
        ['s'] = 0.5, ['t'] = 0.278, ['u'] = 0.556, ['v'] = 0.5, ['w'] = 0.722, ['x'] = 0.5,
        ['y'] = 0.5, ['z'] = 0.5,
        ['{'] = 0.334, ['|'] = 0.26, ['}'] = 0.334, ['~'] = 0.584
    };

    /// <summary>Estimated width in pixels of a single line of text at the given font size and weight.</summary>
    public static double Width(string? text, double fontSize, string weight = "normal")
    {
        if (string.IsNullOrEmpty(text)) return 0;
        double em = 0;
        foreach (char c in text)
            em += Widths.TryGetValue(c, out var w) ? w : Default;
        // Bold text is a touch wider.
        bool bold = weight is "bold" or "600" or "700" or "800" or "900";
        if (bold) em *= 1.06;
        return em * fontSize;
    }

    /// <summary>Width of the widest line in a multi-line string.</summary>
    public static double MultilineWidth(string? text, double fontSize, string weight = "normal")
    {
        if (string.IsNullOrEmpty(text)) return 0;
        double max = 0;
        foreach (var line in text.Split('\n'))
            max = Math.Max(max, Width(line, fontSize, weight));
        return max;
    }
}
