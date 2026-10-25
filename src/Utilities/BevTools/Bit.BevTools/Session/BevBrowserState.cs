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

    /// <summary>What the last accessibility check of the page found; null before the first check.</summary>
    public IReadOnlyList<BevA11yFinding>? Accessibility { get; internal set; }

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
    public IReadOnlyList<BevTargetInfo> Targets { get; internal set; } = [];

    /// <summary>The page's WebSockets as the hub last reported them: sizes and message kinds, never payloads.</summary>
    public IReadOnlyList<BevSocketStats> Sockets { get; internal set; } = [];

    public DateTimeOffset? SocketsReportedAt { get; internal set; }

    /// <summary>Page updates a Blazor Web App made without a reload (enhanced navigation), as the browser reported them.</summary>
    public int EnhancedNavigations { get; internal set; }

    public int ResourceLoadErrors { get; internal set; }

    public int CspViolations { get; internal set; }
}

/// <summary>One finding of the page's accessibility check (page hub), with the component that rendered the element.</summary>
public sealed class BevA11yFinding
{
    /// <summary>"control-name", "field-label", "image-alt", "hidden-focusable", "tabindex-positive", "duplicate-id", "heading-empty", "page-lang".</summary>
    public string Rule { get; set; } = "";

    /// <summary>The WCAG success criteria it is about ("4.1.2").</summary>
    public string Wcag { get; set; } = "";

    public string Message { get; set; } = "";

    /// <summary>The element, by tag, id or first class: never its content.</summary>
    public string Element { get; set; } = "";

    public string? TargetId { get; set; }

    public long? InstanceId { get; set; }

    public string? Component { get; set; }

    /// <summary>Why the component could not be found, when the element may be a component's (the lookup failed); null for static markup.</summary>
    public string? OwnerUnknown { get; set; }
}

/// <summary>One BevTools runtime on the page, as the page hub knows it.</summary>
public sealed class BevTargetInfo
{
    public string Id { get; set; } = "";

    public long? SessionId { get; set; }

    public string? Platform { get; set; }

    public string? RenderMode { get; set; }

    public string Label { get; set; } = "";

    public double AttachedAt { get; set; }
}

public sealed class BevMessageStat
{
    public int Count { get; set; }

    public long Bytes { get; set; }

    public long MaxBytes { get; set; }
}

/// <summary>Render batches a circuit sent to the browser, and how long the browser took to apply them.</summary>
public sealed class BevRenderBatchTraffic
{
    public int Count { get; set; }

    public long Bytes { get; set; }

    public long MaxBytes { get; set; }

    /// <summary>From a batch arriving to the browser acknowledging it: the DOM work on the browser's side, summed.</summary>
    public double ApplyMs { get; set; }

    public double MaxApplyMs { get; set; }

    /// <summary>Batches received and not acknowledged yet.</summary>
    public int Pending { get; set; }

    /// <summary>
    /// Batches that changed nothing but BevTools' own panel, told apart in the browser by where their DOM changes landed.
    /// They are not in the counts above; their bytes are in <see cref="BevSocketStats.Own"/>.
    /// </summary>
    public int BevTools { get; set; }
}

public sealed class BevLargeMessage
{
    /// <summary>"→" sent by the browser, "←" received.</summary>
    public string Direction { get; set; } = "";

    public string Kind { get; set; } = "";

    public long Bytes { get; set; }

    /// <summary>Milliseconds since the Unix epoch.</summary>
    public double At { get; set; }
}

/// <summary>
/// One WebSocket of the page as the browser bridge observed it: the Blazor Server circuit, an application's SignalR hub
/// connection, any other socket. Only sizes, counts and message kinds (the SignalR invocation target, never its
/// arguments) cross the bridge; the URL is redacted in the browser.
/// </summary>
public sealed class BevSocketStats
{
    public int Id { get; set; }

    public string Url { get; set; } = "";

    /// <summary>"blazor-circuit", "signalr" or "websocket".</summary>
    public string Kind { get; set; } = "";

    /// <summary>The SignalR hub protocol ("blazorpack", "json", "messagepack"), when it could be told.</summary>
    public string? Protocol { get; set; }

    /// <summary>When it opened, in Unix milliseconds; null for a socket that already existed when BevTools attached.</summary>
    public double? OpenedAt { get; set; }

    /// <summary>When BevTools first saw it: everything before is not counted.</summary>
    public double ObservedFrom { get; set; }

    public double? ClosedAt { get; set; }

    public int? CloseCode { get; set; }

    public string? CloseReason { get; set; }

    public BevMessageStat Sent { get; set; } = new();

    public BevMessageStat Received { get; set; } = new();

    /// <summary>BevTools' own reports over the circuit, counted apart so they are not mistaken for the app's traffic.</summary>
    public BevMessageStat Own { get; set; } = new();

    /// <summary>By direction and kind: "← JS.RenderBatch", "→ BeginInvokeDotNetFromJS", "← ping".</summary>
    public Dictionary<string, BevMessageStat> Messages { get; set; } = [];

    public BevRenderBatchTraffic RenderBatches { get; set; } = new();

    public List<BevLargeMessage> Largest { get; set; } = [];

    public bool IsClosed => ClosedAt is not null;
}
