namespace Bit.BlazorUI;

// Keyboard shortcuts and paste behavior.
public partial class BitRichTextEditor
{
    /// <summary>When true, pasted content is inserted as plain text.</summary>
    [Parameter] public bool PasteAsPlainText { get; set; }

    /// <summary>
    /// Custom key-combo → command map, merged over the built-in defaults. Keys use the form
    /// "ctrl+b", "ctrl+shift+k" (use "ctrl" for the primary modifier on all platforms).
    /// </summary>
    [Parameter] public IReadOnlyDictionary<string, string>? KeyboardShortcuts { get; set; }

    private static readonly Dictionary<string, string> DefaultShortcuts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ctrl+b"] = "bold",
        ["ctrl+i"] = "italic",
        ["ctrl+u"] = "underline",
        ["ctrl+z"] = "undo",
        ["ctrl+y"] = "redo",
        ["ctrl+shift+z"] = "redo"
    };

    /// <summary>
    /// Invoked by the JS bridge for Ctrl/Cmd keystrokes. Returns true when handled so the
    /// bridge can suppress the browser default.
    /// </summary>
    [JSInvokable("OnShortcut")]
    public async Task<bool> _OnShortcut(string key, bool ctrl, bool shift, bool alt)
    {
        if (ReadOnly) return false;

        var combo = BuildComboKey(key, ctrl, shift, alt);
        string? command = null;
        if (KeyboardShortcuts is not null && KeyboardShortcuts.TryGetValue(combo, out var custom))
            command = custom;                                   // custom wins
        else if (DefaultShortcuts.TryGetValue(combo, out var def))
            command = def;

        if (command is null) return false;

        if (IsKnownCommand(command) is false)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("unknown-shortcut", $"Shortcut command '{command}' is not recognized."));
            return false;
        }

        await ExecAsync(command);
        return true;
    }

    private static string BuildComboKey(string key, bool ctrl, bool shift, bool alt)
    {
        var parts = new List<string>();
        if (ctrl) parts.Add("ctrl");
        if (shift) parts.Add("shift");
        if (alt) parts.Add("alt");
        parts.Add(key.ToLowerInvariant());
        return string.Join('+', parts);
    }

    private static readonly HashSet<string> KnownCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "bold", "italic", "underline", "strikeThrough", "undo", "redo",
        "insertOrderedList", "insertUnorderedList", "justifyLeft", "justifyCenter",
        "justifyRight", "justifyFull", "indent", "outdent", "subscript", "superscript",
        "removeFormat", "unlink", "insertHorizontalRule"
    };

    private static bool IsKnownCommand(string command) => KnownCommands.Contains(command);

    /// <summary>
    /// The set of owned key combos (built-in defaults merged with any custom shortcuts),
    /// sent to the JS bridge so it can suppress the browser default synchronously - before
    /// the async OnShortcut callback - for combos that overlap native browser behavior.
    /// </summary>
    private string[] BuildOwnedShortcutCombos()
    {
        var combos = new HashSet<string>(DefaultShortcuts.Keys, StringComparer.OrdinalIgnoreCase);
        if (KeyboardShortcuts is not null)
        {
            foreach (var key in KeyboardShortcuts.Keys)
                combos.Add(key);
        }
        return combos.Select(c => c.ToLowerInvariant()).ToArray();
    }
}
