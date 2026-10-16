namespace Bit.BevTools;

/// <summary>One browser storage entry. The preview is redacted in the browser, so a secret never reaches BevTools.</summary>
public sealed record BevStorageEntry(string Key, int Size, string Preview, bool Redacted = false);

/// <summary>Browser-side facts reported by the JS bridge. Only available when the bridge is enabled and the session is interactive.</summary>
public sealed class BevBrowserState
{
    public bool BridgeAttached { get; internal set; }

    public string? BridgeError { get; internal set; }

    public bool? Online { get; internal set; }

    public string? Visibility { get; internal set; }

    public string? UserAgent { get; internal set; }

    public string? ReconnectState { get; internal set; }

    public int? ReconnectAttempt { get; internal set; }

    public DateTimeOffset? ReconnectStateChangedAt { get; internal set; }

    public int ReconnectCount { get; internal set; }

    public double? JsHeapUsedMb { get; internal set; }

    public double? JsHeapLimitMb { get; internal set; }

    public double? NavigationLoadMs { get; internal set; }

    public double? DomContentLoadedMs { get; internal set; }

    public int? ResourceCount { get; internal set; }

    public int JsErrorCount { get; internal set; }

    public int JsToDotNetCalls { get; internal set; }

    public IReadOnlyList<BevStorageEntry>? LocalStorage { get; internal set; }

    public IReadOnlyList<BevStorageEntry>? SessionStorage { get; internal set; }

    public DateTimeOffset? StorageReadAt { get; internal set; }

    /// <summary>Why the last storage read failed, if it did. Shown in the Browser panel instead of failing silently.</summary>
    public string? StorageError { get; internal set; }

    /// <summary>True while a storage read is in flight, so the button never looks like it did nothing.</summary>
    public bool StorageReading { get; internal set; }
}
