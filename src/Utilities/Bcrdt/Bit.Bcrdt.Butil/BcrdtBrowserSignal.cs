namespace Bit.Bcrdt;

/// <summary>Something the browser reported about the page or the network.</summary>
internal enum BcrdtBrowserSignal
{
    /// <summary><c>online</c>: the browser believes it regained connectivity.</summary>
    Online,

    /// <summary><c>offline</c>: the browser believes it lost connectivity.</summary>
    Offline,

    /// <summary><c>visibilitychange</c> to hidden: another tab, a minimized window, a backgrounded app.</summary>
    Hidden,

    /// <summary><c>visibilitychange</c> to visible.</summary>
    Visible,

    /// <summary><c>pagehide</c>: the page is being unloaded, or put in the back/forward cache.</summary>
    PageHide,

    /// <summary><c>pageshow</c>: the page was loaded, or restored from the back/forward cache.</summary>
    PageShow,

    /// <summary><c>freeze</c>: the browser is about to suspend the page's tasks, and may discard it.</summary>
    Freeze,

    /// <summary><c>resume</c>: a frozen page runs again.</summary>
    Resume,
}
