namespace Bit.BlazorUI;

/// <summary>
/// Which navigations <see cref="BitAppShell.PersistScroll"/> puts the reader back where they left a page on.
/// </summary>
public enum BitAppShellScrollRestoration
{
    /// <summary>
    /// Every navigation to a url that was left scrolled puts the reader back there, however it was navigated to -
    /// what the tabs of a mobile app do, each remembering its own place.
    /// </summary>
    Url,

    /// <summary>
    /// Only the browser's back and forward buttons put the reader back; every other navigation opens the page at its
    /// top - what a browser does for a page that scrolls the document.
    /// </summary>
    History
}
