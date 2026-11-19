namespace Bit.Blow;

/// <summary>
/// Decides, per DOM event, whether the browser's own handling of it (scrolling the page, mostly)
/// should be cancelled. Only the DOM can cancel an event, and only synchronously, so the rules
/// have to be handed to JS up front.
/// </summary>
/// <param name="PreventScrolling">Whether wheel events the flow acts on are kept from scrolling the page.</param>
/// <param name="PanOnScroll">Whether a plain wheel pans the flow (in which case the flow always acts on it).</param>
/// <param name="ZoomOnScroll">Whether a plain wheel zooms the flow at all. When it does not, the wheel passes to the page.</param>
/// <param name="ZoomKey">
/// The <c>KeyboardEvent</c> flag (<c>"ctrlKey"</c>, <c>"metaKey"</c>, ...) that must be set for a plain
/// wheel to zoom, or null when the wheel zooms unconditionally. Without it the wheel passes to the page.
/// </param>
/// <param name="PanKey">The key that, held, turns a drag into a pan; it must not also scroll the page.</param>
/// <param name="KeyboardA11y">Whether the arrow keys move the selection, and so must not also scroll the page.</param>
/// <param name="SelectAllKey">Whether Ctrl/Cmd+A selects the graph, and so must not also select the page's text.</param>
/// <param name="ClipboardKeys">Whether Ctrl/Cmd+C, X, V and D are the flow's, and so must not also reach the browser (Ctrl+D bookmarks).</param>
/// <param name="HistoryKeys">Whether Ctrl/Cmd+Z and Y undo and redo in the flow, and so must not also reach the browser.</param>
/// <param name="DeleteKeys">
/// The keys that delete the selection, one per line, or null when none does. They must not also reach the browser:
/// Backspace goes back a page in some (WebKit among them), and the graph with it.
/// </param>
internal sealed record BlowInputPolicy(
    bool PreventScrolling, bool PanOnScroll, bool ZoomOnScroll, string? ZoomKey, string? PanKey, bool KeyboardA11y,
    bool SelectAllKey, bool ClipboardKeys, bool HistoryKeys, string? DeleteKeys);
