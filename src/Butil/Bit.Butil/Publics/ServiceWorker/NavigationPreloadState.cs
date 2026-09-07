namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/NavigationPreloadManager/getState">NavigationPreloadManager.getState()</see>.
/// </summary>
public class NavigationPreloadState
{
    /// <summary>
    /// False when the browser has no <c>navigationPreload</c> at all, which is how "not supported"
    /// is told apart from "supported and switched off".
    /// </summary>
    public bool IsSupported { get; set; }

    /// <summary>True when the browser starts the navigation request in parallel with booting the worker.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// What the browser sends as the <c>Service-Worker-Navigation-Preload</c> header on a preload
    /// request. <c>"true"</c> unless changed.
    /// </summary>
    public string HeaderValue { get; set; } = string.Empty;
}
