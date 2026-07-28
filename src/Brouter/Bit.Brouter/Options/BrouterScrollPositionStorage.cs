namespace Bit.Brouter;

/// <summary>
/// Where saved scroll positions are kept when <see cref="BrouterOptions.RestoreScrollPosition"/> is
/// enabled. Only affects how long the positions live; the restore behavior itself is identical.
/// </summary>
public enum BrouterScrollPositionStorage
{
    /// <summary>
    /// Keep positions in memory only. They are lost on a full page reload (or when navigating away to
    /// another origin and back). Cheapest and leaks nothing to disk. This is the default.
    /// </summary>
    Memory = 0,

    /// <summary>
    /// Persist positions in the browser's <c>sessionStorage</c>: they survive a full reload within the
    /// same tab and are cleared automatically when the tab is closed, and are never shared with other
    /// tabs. This is the recommended choice for scroll restoration (it mirrors how React Router's
    /// <c>ScrollRestoration</c> stores positions).
    /// </summary>
    SessionStorage = 1,

    /// <summary>
    /// Persist positions in the browser's <c>localStorage</c>: they survive reloads and browser
    /// restarts and are shared across every tab of the same origin. Because tabs share one store, tabs
    /// can overwrite each other's saved positions for the same URL, and positions are never cleared
    /// automatically. Prefer <see cref="SessionStorage"/> unless you specifically need cross-restart
    /// persistence.
    /// </summary>
    LocalStorage = 2
}
