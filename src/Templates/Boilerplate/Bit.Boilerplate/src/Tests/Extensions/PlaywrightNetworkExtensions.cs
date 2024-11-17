using System.Text.RegularExpressions;

namespace Boilerplate.Tests.Extensions;

public static class PlaywrightNetworkExtensions
{
    public static async Task<PlaywrightNetworkSession> OpenNetworkSession(this IPage page)
    {
        // Enable CDP session to access network details
        var client = await page.Context.NewCDPSessionAsync(page);
        var networkSession = new PlaywrightNetworkSession(client);
        await networkSession.Enable();
        return networkSession;
    }
}

public class PlaywrightNetworkSession : IAsyncDisposable
{
    private readonly ICDPSession client;
    private readonly ICDPSessionEvent responseReceived;
    private readonly ICDPSessionEvent loadingFinished;

    public List<DownloadedResponse> DownloadedResponses { get; } = [];
    public int TotalDownloaded => DownloadedResponses.Sum(x => x.EncodedDataLength);

    public PlaywrightNetworkSession(ICDPSession client)
    {
        this.client = client;

        // Chrome DevTools Protocol
        // https://chromedevtools.github.io/devtools-protocol/1-3/Network/

        // Listen to the 'Network.responseReceived' events
        responseReceived = client.Event("Network.responseReceived");
        responseReceived.OnEvent += OnResponseReceived;

        // Listen to the 'Network.loadingFinished' events
        loadingFinished = client.Event("Network.loadingFinished");
        loadingFinished.OnEvent += OnLoadingFinished;
    }

    /// <summary>
    /// Enables network tracking, network events will now be delivered to the client.
    /// </summary>
    public async Task Enable() => client.SendAsync("Network.enable");

    /// <summary>
    /// Disables network tracking, prevents network events from being sent to the client.
    /// </summary>
    public async Task Disable() => client.SendAsync("Network.disable");

    private async void OnResponseReceived(object? sender, JsonElement? arg)
    {
        try
        {
            var data = arg!.Value.Deserialize<ResponseReceivedData>()!;
            data.Response.RequestId = data.RequestId;
            DownloadedResponses.Add(data.Response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to retrieve response data: {ex.Message}");
        }
    }

    private async void OnLoadingFinished(object? sender, JsonElement? arg)
    {
        try
        {
            var data = arg!.Value.Deserialize<LoadingFinishedData>()!;
            var response = DownloadedResponses.Find(x => x.RequestId == data.RequestId)!;
            response.EncodedDataLength = data.EncodedDataLength;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to retrieve encoded size: {ex.Message}");
        }
    }

    #region Other Commands

    public Task ClearBrowserCache() => client.SendAsync("Network.clearBrowserCache");

    public Task ClearBrowserCookies() => client.SendAsync("Network.clearBrowserCookies");

    /// <summary>
    /// Toggles ignoring cache for each request. If true, cache will not be used.
    /// </summary>
    public Task SetCacheDisabled(bool disabled) => client.SendAsync("Network.setCacheDisabled", new() { ["cacheDisabled"] = disabled });

    /// <summary>
    /// Activates emulation of network conditions.
    /// </summary>
    public Task EmulateNetworkConditions(NetworkCondition networkCondition) =>
        client.SendAsync("Network.emulateNetworkConditions", new()
        {
            ["offline"] = networkCondition.Offline,
            ["latency"] = networkCondition.Latency,
            ["downloadThroughput"] = networkCondition.DownloadThroughput,
            ["uploadThroughput"] = networkCondition.UploadThroughput,
            ["connectionType"] = networkCondition.ConnectionType
        });

    /// <summary>
    /// Blocks URLs from loading.
    /// </summary>
    /// <param name="urls">URL patterns to block. Wildcards ('*') are allowed.</param>
    public Task SetBlockedUrls(string[] urls) => client.SendAsync("Network.setBlockedURLs", new() { ["urls"] = urls });

    #endregion

    public List<DownloadedResponse> GetResponses(Regex regex) => DownloadedResponses.Where(x => regex.IsMatch(x.Url)).ToList();

    public List<DownloadedResponse> GetResponses(string url) => DownloadedResponses.Where(x => url.Contains(x.Url)).ToList();

    public bool ContainsResponse(Regex regex) => DownloadedResponses.Exists(x => regex.IsMatch(x.Url));

    public bool ContainsResponse(string url) => DownloadedResponses.Exists(x => url.Contains(x.Url));

    public async ValueTask DisposeAsync()
    {
        responseReceived.OnEvent -= OnResponseReceived;
        loadingFinished.OnEvent -= OnLoadingFinished;
        await client.DisposeAsync();
    }

    private class LoadingFinishedData
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("timestamp")]
        public double Timestamp { get; set; }

        [JsonPropertyName("encodedDataLength")]
        public int EncodedDataLength { get; set; }
    }

    private class ResponseReceivedData
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("loaderId")]
        public string LoaderId { get; set; }

        [JsonPropertyName("timestamp")]
        public double Timestamp { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("response")]
        public DownloadedResponse Response { get; set; }
    }
}

public class DownloadedResponse : IEquatable<DownloadedResponse>
{
    public string RequestId { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("statusText")]
    public string StatusText { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string> Headers { get; set; }

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; }

    [JsonPropertyName("charset")]
    public string Charset { get; set; }

    [JsonPropertyName("remoteIPAddress")]
    public string RemoteIPAddress { get; set; }

    [JsonPropertyName("remotePort")]
    public int RemotePort { get; set; }

    [JsonPropertyName("fromDiskCache")]
    public bool FromDiskCache { get; set; }

    [JsonPropertyName("fromServiceWorker")]
    public bool FromServiceWorker { get; set; }

    [JsonPropertyName("fromPrefetchCache")]
    public bool FromPrefetchCache { get; set; }

    [JsonPropertyName("encodedDataLength")]
    public int EncodedDataLength { get; set; }

    [JsonPropertyName("responseTime")]
    public double ResponseTime { get; set; }

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; }

    public override int GetHashCode() => RequestId.GetHashCode();
    public override bool Equals(object? obj) => obj is DownloadedResponse other && Equals(other);
    public bool Equals(DownloadedResponse? other) => other is not null && RequestId == other.RequestId;
}

public class NetworkCondition
{
    /// <summary>
    /// True to emulate Internet disconnection.
    /// </summary>
    public bool Offline { get; set; }

    /// <summary>
    /// Minimum latency from request sent to response headers received(ms).
    /// </summary>
    public int Latency { get; set; }

    /// <summary>
    /// Maximal aggregated download throughput(bytes/sec). -1 disables download throttling.
    /// </summary>
    public int DownloadThroughput { get; set; }

    /// <summary>
    /// Maximal aggregated upload throughput(bytes/sec). -1 disables upload throttling.
    /// </summary>
    public int UploadThroughput { get; set; }

    /// <summary>
    /// The underlying connection technology that the browser is supposedly using.
    /// </summary>
    public ConnectionType ConnectionType { get; set; }

    public static readonly NetworkCondition IsOffline = new()
    {
        Offline = true,
        DownloadThroughput = 0,
        UploadThroughput = 0,
        Latency = 0,
        ConnectionType = ConnectionType.None
    };

    public static readonly NetworkCondition NoThrottle = new()
    {
        Offline = false,
        DownloadThroughput = -1,
        UploadThroughput = -1,
        Latency = 0,
        ConnectionType = ConnectionType.None
    };

    public static readonly NetworkCondition Regular2G = new()
    {
        Offline = false,
        DownloadThroughput = (250 * 1024) / 8,
        UploadThroughput = (50 * 1024) / 8,
        Latency = 300,
        ConnectionType = ConnectionType.Cellular2G
    };

    public static readonly NetworkCondition Fast2G = new()
    {
        Offline = false,
        DownloadThroughput = (450 * 1024) / 8,
        UploadThroughput = (150 * 1024) / 8,
        Latency = 150,
        ConnectionType = ConnectionType.Cellular2G
    };

    public static readonly NetworkCondition Regular3G = new()
    {
        Offline = false,
        DownloadThroughput = (750 * 1024) / 8,
        UploadThroughput = (250 * 1024) / 8,
        Latency = 100,
        ConnectionType = ConnectionType.Cellular3G
    };

    public static readonly NetworkCondition Good3G = new()
    {
        Offline = false,
        DownloadThroughput = (1500 * 1024) / 8,
        UploadThroughput = (750 * 1024) / 8,
        Latency = 40,
        ConnectionType = ConnectionType.Cellular3G
    };

    public static readonly NetworkCondition Regular4G = new()
    {
        Offline = false,
        DownloadThroughput = (4 * 1024 * 1024) / 8,
        UploadThroughput = (3 * 1024 * 1024) / 8,
        Latency = 20,
        ConnectionType = ConnectionType.Cellular4G
    };

    public static readonly NetworkCondition WiFi = new()
    {
        Offline = false,
        DownloadThroughput = (30 * 1024 * 1024) / 8,
        UploadThroughput = (15 * 1024 * 1024) / 8,
        Latency = 2,
        ConnectionType = ConnectionType.Wifi
    };
}

public enum ConnectionType
{
    None,
    Cellular2G,
    Cellular3G,
    Cellular4G,
    Bluetooth,
    Ethernet,
    Wifi,
    Wimax,
    Other
}
