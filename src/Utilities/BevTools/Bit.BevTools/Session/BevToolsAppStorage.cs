namespace Bit.BevTools;

/// <summary>
/// The page's other storage, as the page hub read it on request: structure and sizes only. Cookie values are never
/// read (a cookie is where a session id lives), IndexedDB databases are listed, not opened, and caches are counted,
/// never their responses read.
/// </summary>
public sealed class BevToolsAppStorage
{
    /// <summary>Cookies script can read: HttpOnly cookies (an ASP.NET Core authentication cookie) are invisible to the page.</summary>
    public BevToolsCookieInfo[] Cookies { get; set; } = [];

    /// <summary>Null where the browser cannot list its databases.</summary>
    public BevToolsIndexedDbInfo[]? IndexedDb { get; set; }

    public BevToolsCacheInfo[]? Caches { get; set; }

    public BevToolsServiceWorkerInfo[]? ServiceWorkers { get; set; }

    /// <summary>The script of the service worker that controls this page, null when none does.</summary>
    public string? Controller { get; set; }

    public double? UsageBytes { get; set; }

    public double? QuotaBytes { get; set; }

    /// <summary>Usage per kind of storage (Chromium): indexedDB, caches, serviceWorkerRegistrations, fileSystem.</summary>
    public Dictionary<string, double>? UsageDetails { get; set; }

    /// <summary>Whether the browser keeps this origin's storage under pressure (<c>navigator.storage.persist()</c> was granted).</summary>
    public bool? Persisted { get; set; }

    /// <summary>What could not be read, and why.</summary>
    public string[] Errors { get; set; } = [];
}
