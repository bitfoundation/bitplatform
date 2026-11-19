namespace Bit.BevTools;

public sealed class BevToolsUiOptions
{
    public bool OpenByDefault { get; set; }

    public BevToolsDock Dock { get; set; } = BevToolsDock.Bottom;

    public BevToolsTheme Theme { get; set; } = BevToolsTheme.Auto;

    /// <summary>How often the UI checks for new activity while open, in milliseconds. The UI never re-renders without changes.</summary>
    public int RefreshIntervalMs { get; set; } = 250;

    /// <summary>
    /// When true (default) the open tab re-reads captured data as it arrives. Turn off on a very busy app and use
    /// the Refresh button; capture itself keeps running either way.
    /// </summary>
    public bool LiveUpdates { get; set; } = true;

    /// <summary>Keyboard shortcut that toggles the panel. Format: modifiers + key, e.g. "Ctrl+Shift+D".</summary>
    public string ToggleShortcut { get; set; } = "Ctrl+Shift+D";

    /// <summary>Show a small floating badge when the panel is closed.</summary>
    public bool ShowBadge { get; set; } = true;

    /// <summary>
    /// The link that opens a source location in an editor, with <c>{path}</c>, <c>{line}</c> and <c>{column}</c> filled in:
    /// VS Code by default; <c>vscode-insiders://file/{path}:{line}:{column}</c>, or null for no links (paths are still
    /// shown). Locations come from the app's symbols (PDB), so there are none in a build without them.
    /// </summary>
    public string? EditorUrlTemplate { get; set; } = "vscode://file/{path}:{line}:{column}";
}
