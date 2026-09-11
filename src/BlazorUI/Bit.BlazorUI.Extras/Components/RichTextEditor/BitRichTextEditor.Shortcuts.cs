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
        ["ctrl+shift+z"] = "redo",
        ["ctrl+shift+x"] = "strikeThrough",
        ["ctrl+shift+7"] = "insertOrderedList",
        ["ctrl+shift+8"] = "insertUnorderedList",
        ["ctrl+shift+9"] = "insertTaskList",
        ["ctrl+e"] = "inlineCode",
        ["ctrl+k"] = LinkCommand,
        ["ctrl+shift+k"] = "unlink",
        ["ctrl+\\"] = "removeFormat"
    };

    /// <summary>
    /// The one shortcut command that is not an editing command: it opens the link panel instead of
    /// mutating content, because a link needs a URL the user has not supplied yet.
    /// </summary>
    private const string LinkCommand = "link";

    /// <summary>
    /// Invoked by the JS bridge for Ctrl/Cmd keystrokes. Returns true when handled so the
    /// bridge can suppress the browser default.
    /// </summary>
    [JSInvokable("OnShortcut")]
    public async Task<bool> _OnShortcut(string key, bool ctrl, bool shift, bool alt)
    {
        // Source view (and ReadOnly) disable command execution: ExecAsync no-ops when
        // ControlsDisabled, so report the shortcut as unhandled instead of suppressing the
        // browser default for a command that will not run.
        if (ControlsDisabled) return false;

        var combo = BuildComboKey(key, ctrl, shift, alt);
        string? command = null;
        // Custom shortcut keys are advertised to the JS bridge lowercased (see
        // BuildOwnedShortcutCombos), so probe the user-supplied map case-insensitively to keep
        // matching consistent regardless of the casing used in the KeyboardShortcuts keys.
        if (KeyboardShortcuts is not null)
        {
            foreach (var (k, v) in KeyboardShortcuts)
            {
                if (string.Equals(k, combo, StringComparison.OrdinalIgnoreCase))
                {
                    command = v;                                // custom wins
                    break;
                }
            }
        }
        if (command is null && DefaultShortcuts.TryGetValue(combo, out var def))
            command = def;

        if (command is null) return false;

        // "link" opens the link panel rather than running a command; the panel then applies the
        // link through the normal validated path once a URL has been entered.
        if (string.Equals(command, LinkCommand, StringComparison.OrdinalIgnoreCase))
        {
            // The panel stands on its own (it opens without the toolbar), but the link group still
            // gates it: claiming the keystroke with links turned off would swallow the browser
            // default and show nothing.
            if (Has(BitRichTextEditorToolbar.Link) is false) return false;

            if (_showLinkInput is false) await ToggleLinkInput();
            StateHasChanged();
            return true;
        }

        // A block name maps to the paragraph format rather than to an editing command, so the
        // same map that binds "bold" to a chord can bind "h1" or "blockquote" to one. Applying it
        // toggles, matching the toolbar's own block buttons: pressing it on the block it already
        // produces goes back to a normal paragraph.
        if (BlockCommands.Contains(command))
        {
            await FormatBlockToggleAsync(command.ToLowerInvariant());
            return true;
        }

        if (IsKnownCommand(command) is false)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("unknown-shortcut",
                string.Format(Label("unknown-shortcut", "Shortcut command '{0}' is not recognized."), command)));
            return false;
        }

        await ExecAsync(command);
        return true;
    }

    /// <summary>
    /// The combo currently bound to a command, written the way <c>aria-keyshortcuts</c> wants it
    /// ("Control+Shift+X"), or null when nothing reaches that command. Read from the effective
    /// map - a custom binding first, then the built-in default unless the host has rebound that
    /// key - so what a screen reader announces is what actually works.
    /// </summary>
    private string? ShortcutFor(string command)
    {
        if (KeyboardShortcuts is not null)
        {
            foreach (var (key, mapped) in KeyboardShortcuts)
            {
                if (string.Equals(mapped, command, StringComparison.OrdinalIgnoreCase))
                    return ToAriaKeyShortcut(key);
            }
        }

        foreach (var (key, mapped) in DefaultShortcuts)
        {
            if (string.Equals(mapped, command, StringComparison.OrdinalIgnoreCase) is false) continue;
            // A default whose key the host has taken over no longer runs this command.
            if (KeyboardShortcuts is not null
                && KeyboardShortcuts.Any(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase))) continue;
            return ToAriaKeyShortcut(key);
        }

        return null;
    }

    private static string ToAriaKeyShortcut(string combo)
        => string.Join('+', combo.Split('+', StringSplitOptions.RemoveEmptyEntries).Select(part => part.ToLowerInvariant() switch
        {
            "ctrl" => "Control",
            "shift" => "Shift",
            "alt" => "Alt",
            "meta" => "Meta",
            var other => other.Length == 1 ? other.ToUpperInvariant() : char.ToUpperInvariant(other[0]) + other[1..]
        }));

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
        "insertOrderedList", "insertUnorderedList", "insertTaskList", "inlineCode",
        "justifyLeft", "justifyCenter", "justifyRight", "justifyFull",
        "indent", "outdent", "subscript", "superscript",
        "removeFormat", "unlink", "insertHorizontalRule",
        LinkCommand
    };

    /// <summary>
    /// The paragraph formats a shortcut may be bound to. They are block names, not editing
    /// commands, so they run through the block path (like the toolbar's format selector) instead
    /// of the command path.
    /// </summary>
    private static readonly HashSet<string> BlockCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "h1", "h2", "h3", "h4", "h5", "h6", "blockquote", "pre"
    };

    private static bool IsKnownCommand(string command)
        => KnownCommands.Contains(command) || BlockCommands.Contains(command);

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
            // Custom shortcuts win over the built-in defaults (see _OnShortcut). Only advertise a
            // combo as owned when its effective command can actually be executed; if a custom
            // override maps a key (including one that shadows a default) to an unknown command,
            // drop it so the bridge does not suppress an otherwise-handled browser shortcut that
            // _OnShortcut would later reject.
            foreach (var (key, command) in KeyboardShortcuts)
            {
                if (IsKnownCommand(command))
                    combos.Add(key);
                else
                    combos.Remove(key);
            }
        }
        // Sort into a stable order so SerializeSetupOptions() produces a deterministic snapshot;
        // the underlying HashSet has no guaranteed iteration order, which would otherwise let the
        // same logical shortcuts serialize differently and retrigger BitRichTextEditorUpdateOptions.
        return combos.Select(c => c.ToLowerInvariant()).OrderBy(c => c, StringComparer.Ordinal).ToArray();
    }
}
