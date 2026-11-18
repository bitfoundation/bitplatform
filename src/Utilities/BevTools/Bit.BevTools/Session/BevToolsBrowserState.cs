namespace Bit.BevTools;

/// <summary>Browser-side facts reported by the JS bridge. Only available when the bridge is enabled and the session is interactive.</summary>
public sealed class BevToolsBrowserState
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

    /// <summary>
    /// The calls in <see cref="JsToDotNetCalls"/> the page hub counted without naming them: more kinds of call in one
    /// report window than it sends an entry each for (calls on hundreds of objects, whose identifiers name the object).
    /// </summary>
    public int JsToDotNetUnlisted { get; internal set; }

    public IReadOnlyList<BevToolsStorageEntry>? LocalStorage { get; internal set; }

    public IReadOnlyList<BevToolsStorageEntry>? SessionStorage { get; internal set; }

    public DateTimeOffset? StorageReadAt { get; internal set; }

    /// <summary>What the last accessibility check of the page found; null before the first check.</summary>
    public IReadOnlyList<BevToolsA11yFinding>? Accessibility { get; internal set; }

    public DateTimeOffset? AccessibilityCheckedAt { get; internal set; }

    /// <summary>Why the last accessibility check failed, if it did.</summary>
    public string? AccessibilityError { get; internal set; }

    /// <summary>Why the last storage read failed, if it did. Shown in the Browser panel instead of failing silently.</summary>
    public string? StorageError { get; internal set; }

    /// <summary>True while a storage read is in flight, so the button never looks like it did nothing.</summary>
    public bool StorageReading { get; internal set; }

    /// <summary>The id the page hub gave this runtime when its panel attached ("t1"); null before that.</summary>
    public string? TargetId { get; internal set; }

    /// <summary>Every BevTools runtime on the page, this one included: a Web App can run a circuit and WebAssembly at once.</summary>
    public IReadOnlyList<BevToolsTargetInfo> Targets { get; internal set; } = [];

    /// <summary>The page's WebSockets as the hub last reported them: sizes and message kinds, never payloads.</summary>
    public IReadOnlyList<BevToolsSocketStats> Sockets { get; internal set; } = [];

    public DateTimeOffset? SocketsReportedAt { get; internal set; }

    /// <summary>Page updates a Blazor Web App made without a reload (enhanced navigation), as the browser reported them.</summary>
    public int EnhancedNavigations { get; internal set; }

    public int ResourceLoadErrors { get; internal set; }

    public int CspViolations { get; internal set; }

    /// <summary>Long frames and interactions the page's browser reported (Event Timing, long animation frames).</summary>
    public BevToolsResponsiveness Responsiveness { get; } = new();

    /// <summary>The page's cookies, IndexedDB databases, caches, service workers and quota at the last storage read.</summary>
    public BevToolsAppStorage? AppStorage { get; internal set; }
}
