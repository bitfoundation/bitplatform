namespace Bit.BevTools;

/// <summary>
/// One WebSocket of the page as the browser bridge observed it: the Blazor Server circuit, an application's SignalR hub
/// connection, any other socket. Only sizes, counts and message kinds (the SignalR invocation target, never its
/// arguments) cross the bridge; the URL is redacted in the browser.
/// </summary>
public sealed class BevToolsSocketStats
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

    public BevToolsMessageStat Sent { get; set; } = new();

    public BevToolsMessageStat Received { get; set; } = new();

    /// <summary>BevTools' own reports over the circuit, counted apart so they are not mistaken for the app's traffic.</summary>
    public BevToolsMessageStat Own { get; set; } = new();

    /// <summary>By direction and kind: "← JS.RenderBatch", "→ BeginInvokeDotNetFromJS", "← ping".</summary>
    public Dictionary<string, BevToolsMessageStat> Messages { get; set; } = [];

    public BevToolsRenderBatchTraffic RenderBatches { get; set; } = new();

    public List<BevToolsLargeMessage> Largest { get; set; } = [];

    public bool IsClosed => ClosedAt is not null;
}
