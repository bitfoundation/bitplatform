namespace Bit.BlazorUI;

internal class BitRichTextEditorSetupOptions
{
    public int Debounce { get; set; }
    public BitRichTextEditorPolicyPayload? Policy { get; set; }
    public bool HasUpload { get; set; }
    public bool PlainTextPaste { get; set; }
    public int? MaxLength { get; set; }
    public string[]? ShortcutKeys { get; set; }

    /// <summary>
    /// Mirrors the effective read-only state (ReadOnly or IsEnabled=false). The bridge refuses
    /// every mutating path while set, so paste, drop, typing, and shortcuts cannot change content
    /// the component considers locked.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>Whether typing a URL followed by a space turns it into a link.</summary>
    public bool AutoLink { get; set; } = true;

    /// <summary>
    /// Whether the floating selection toolbar is enabled. The bridge only measures the selection
    /// rectangle - a layout-forcing read on every selection change - when it is.
    /// </summary>
    public bool QuickToolbar { get; set; }

    /// <summary>
    /// Whether the bridge watches for the mention trigger. Off unless the host supplies a lookup,
    /// so an editor without mentions pays nothing for the detection.
    /// </summary>
    public bool Mentions { get; set; }
}
