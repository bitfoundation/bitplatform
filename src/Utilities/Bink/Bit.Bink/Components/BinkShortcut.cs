namespace Bit.Bink;

/// <summary>
/// Keyboard chords as text, the way <see cref="BinkCanvas.DisabledShortcuts"/> takes them: modifiers, then the key,
/// joined with <c>+</c>, such as <c>"Ctrl+Shift+Z"</c>, <c>"Delete"</c> or <c>"Shift+H"</c>.
/// </summary>
/// <remarks>
/// Ctrl stands for Command on macOS too, as everywhere in the canvas. The key is the browser's
/// <c>KeyboardEvent.key</c> value; a letter may be written in either case. <c>Cmd</c>, <c>Meta</c> and <c>Control</c>
/// mean Ctrl, <c>Option</c> means Alt, and <c>Esc</c> and <c>Del</c> mean Escape and Delete.
/// </remarks>
public static class BinkShortcut
{
    /// <summary>
    /// The canonical form of a chord: Ctrl, Alt and Shift in that order, then the key, a single character upper-cased.
    /// Null when the text names no key.
    /// </summary>
    public static string? Normalize(string? chord)
    {
        if (string.IsNullOrWhiteSpace(chord)) return null;
        bool ctrl = false, alt = false, shift = false;
        string? key = null;
        // A trailing "+" is the plus key itself, as in "Ctrl++".
        string text = chord.Trim();
        bool plus = text.EndsWith("++", StringComparison.Ordinal) || text == "+";
        if (plus) text = text[..^1];
        foreach (string raw in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (raw.ToLowerInvariant())
            {
                case "ctrl" or "control" or "cmd" or "command" or "meta": ctrl = true; break;
                case "alt" or "option": alt = true; break;
                case "shift": shift = true; break;
                default: key = raw; break;
            }
        }
        if (plus) key = "+";
        return key is null ? null : Compose(KeyName(key), ctrl, alt, shift);
    }

    /// <summary>The canonical chord of a key press, as the canvas receives it.</summary>
    internal static string FromEvent(string key, bool ctrl, bool shift, bool alt) => Compose(KeyName(key), ctrl, alt, shift);

    /// <summary>
    /// The same key press as typed without Shift on a common layout: Shift turns <c>]</c> into <c>}</c> and
    /// <c>=</c> into <c>+</c>, and a chord written with the unshifted key should still match.
    /// </summary>
    internal static string? Unshifted(string key, bool ctrl, bool shift, bool alt)
    {
        if (!shift || key.Length != 1) return null;
        char unshifted = key[0] switch
        {
            '}' => ']',
            '{' => '[',
            '+' => '=',
            '_' => '-',
            '!' => '1',
            '@' => '2',
            '#' => '3',
            '$' => '4',
            '%' => '5',
            '^' => '6',
            '&' => '7',
            '*' => '8',
            '(' => '9',
            ')' => '0',
            _ => '\0',
        };
        return unshifted == '\0' ? null : Compose(unshifted.ToString(), ctrl, alt, shift);
    }

    private static string KeyName(string key) => key.ToLowerInvariant() switch
    {
        "esc" or "escape" => "Escape",
        "del" or "delete" => "Delete",
        "backspace" => "Backspace",
        "enter" or "return" => "Enter",
        "tab" => "Tab",
        "space" or "spacebar" => " ",
        "arrowleft" or "left" => "ArrowLeft",
        "arrowright" or "right" => "ArrowRight",
        "arrowup" or "up" => "ArrowUp",
        "arrowdown" or "down" => "ArrowDown",
        "home" => "Home",
        "end" => "End",
        "pageup" => "PageUp",
        "pagedown" => "PageDown",
        "insert" => "Insert",
        _ when key.Length == 1 => key.ToUpperInvariant(),
        // Function keys as the browser names them, F1 to F24, whatever case they are written in.
        _ when key.Length is 2 or 3 && key[0] is 'f' or 'F' && int.TryParse(key.AsSpan(1), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int n) && n is >= 1 and <= 24 => "F" + n.ToString(System.Globalization.CultureInfo.InvariantCulture),
        // Anything else the browser names, as it names it.
        _ => key,
    };

    private static string Compose(string key, bool ctrl, bool alt, bool shift)
        => (ctrl ? "Ctrl+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty) + key;
}
