namespace Bit.BevTools;

/// <summary>A service worker registration: the script that runs, and one installed and waiting to take over, if any.</summary>
public sealed class BevToolsServiceWorkerInfo
{
    public string Scope { get; set; } = "";

    public string? Active { get; set; }

    public string? ActiveState { get; set; }

    /// <summary>A new version installed and waiting: the page keeps running the old one until every tab of it closes, or the worker calls <c>skipWaiting</c>.</summary>
    public string? Waiting { get; set; }

    public string? Installing { get; set; }
}
