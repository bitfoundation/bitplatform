using System.Text;

namespace Bit.BlazorUI;

/// <summary>A single key press with modifiers.</summary>
public sealed record BitCommandPaletteKeyChord(string Key, bool Ctrl = false, bool Shift = false, bool Alt = false, bool Meta = false, bool Mod = false)
{
    private static readonly Dictionary<string, string> KeyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["esc"] = "escape",
        ["return"] = "enter",
        // Spelled out: a literal space separates the chords of a sequence in Normalized.
        ["spacebar"] = "space",
        ["del"] = "delete",
        ["ins"] = "insert",
        ["up"] = "arrowup",
        ["down"] = "arrowdown",
        ["left"] = "arrowleft",
        ["right"] = "arrowright",
        ["plus"] = "+",
        ["comma"] = ",",
        ["period"] = ".",
        ["slash"] = "/",
        ["backslash"] = "\\",
        ["minus"] = "-",
        ["pgup"] = "pageup",
        ["pgdn"] = "pagedown",
    };

    /// <summary>Parses one chord such as <c>Ctrl+Shift+P</c>; throws a <see cref="FormatException"/> without a key or with two.</summary>
    public static BitCommandPaletteKeyChord Parse(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        bool ctrl = false, shift = false, alt = false, meta = false, mod = false;
        string? key = null;

        IEnumerable<string> parts = text.EndsWith("++", StringComparison.Ordinal)
            ? text[..^2].Split('+', StringSplitOptions.RemoveEmptyEntries).Append("+")
            : text.Split('+', StringSplitOptions.RemoveEmptyEntries);

        foreach (var raw in parts)
        {
            var p = raw.Trim();
            switch (p.ToLowerInvariant())
            {
                case "ctrl" or "control" or "⌃": ctrl = true; break;
                case "shift" or "⇧": shift = true; break;
                case "alt" or "option" or "opt" or "⌥": alt = true; break;
                case "meta" or "cmd" or "command" or "win" or "super" or "⌘": meta = true; break;
                case "mod" or "cmdorctrl" or "commandorcontrol": mod = true; break;
                default:
                    if (key is not null)
                    {
                        throw new FormatException($"Chord '{text}' has more than one non-modifier key.");
                    }

                    key = KeyAliases.TryGetValue(p, out var alias) ? alias : p.ToLowerInvariant();
                    break;
            }
        }

        if (key is null)
        {
            throw new FormatException($"Chord '{text}' has no key.");
        }

        return new BitCommandPaletteKeyChord(key, ctrl, shift, alt, meta, mod);
    }

    /// <summary>Canonical lower-case form, modifiers in fixed order: mod, ctrl, alt, shift, meta.</summary>
    public string Normalized
    {
        get
        {
            var sb = new StringBuilder();
            if (Mod) sb.Append("mod+");
            if (Ctrl) sb.Append("ctrl+");
            if (Alt) sb.Append("alt+");
            if (Shift) sb.Append("shift+");
            if (Meta) sb.Append("meta+");
            sb.Append(Key);
            return sb.ToString();
        }
    }

    /// <summary>The key caps to render, e.g. <c>⌘ K</c> on macOS and <c>Ctrl K</c> elsewhere.</summary>
    public IReadOnlyList<string> DisplayTokens(bool isMac)
    {
        var tokens = new List<string>(5);
        if (Mod) tokens.Add(isMac ? "⌘" : "Ctrl");
        if (Ctrl) tokens.Add(isMac ? "⌃" : "Ctrl");
        if (Alt) tokens.Add(isMac ? "⌥" : "Alt");
        if (Shift) tokens.Add(isMac ? "⇧" : "Shift");
        if (Meta) tokens.Add(isMac ? "⌘" : "Win");
        tokens.Add(DisplayKey(Key, isMac));
        return tokens;
    }

    private static string DisplayKey(string key, bool isMac) => key switch
    {
        "enter" => isMac ? "↩" : "Enter",
        "escape" => "Esc",
        "backspace" => isMac ? "⌫" : "Backspace",
        "delete" => isMac ? "⌦" : "Del",
        "tab" => isMac ? "⇥" : "Tab",
        "space" => "Space",
        "arrowup" => "↑",
        "arrowdown" => "↓",
        "arrowleft" => "←",
        "arrowright" => "→",
        "pageup" => "PgUp",
        "pagedown" => "PgDn",
        _ when key.Length == 1 => key.ToUpperInvariant(),
        _ => char.ToUpperInvariant(key[0]) + key[1..],
    };
}
