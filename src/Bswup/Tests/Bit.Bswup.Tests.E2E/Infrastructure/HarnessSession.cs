using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.Bswup.Tests.E2E.Infrastructure;

/// <summary>
/// One test's slice of the harness host, reached through the host's /_harness/ endpoints: an origin of its
/// own (<c>http://&lt;id&gt;.localhost:&lt;port&gt;</c>) and the deployment behind it - service-worker settings, the
/// live version, the network, faults and the request log.
/// </summary>
public sealed class HarnessSession
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly WebHarnessHost _host;

    public HarnessSession(WebHarnessHost host)
    {
        _host = host;
        // Host names are case-insensitive and the host keys sessions by them: lowercase letters and digits only.
        Id = "t" + Guid.NewGuid().ToString("N")[..12];
    }

    public string Id { get; }

    /// <summary>The origin the test's pages load from, without a trailing slash.</summary>
    public string Origin => $"http://{Id}.localhost:{_host.Port}";

    public string HostOutput => _host.RecentOutput;

    /// <summary>
    /// Replaces the session's options. <paramref name="options"/> is serialized with web (camelCase) naming;
    /// see HarnessSessionOptions in the host for the shape. Build RegExp settings with <see cref="Regex"/>.
    /// </summary>
    public Task SetOptionsAsync(object options) => SendAsync(HttpMethod.Put, "options", options);

    /// <summary>Publishes deployment <paramref name="version"/>: a new worker script and manifest version.</summary>
    public Task PublishVersionAsync(int version) => SendAsync(HttpMethod.Put, "version", new { version });

    public Task SetOfflineAsync(bool offline) => SendAsync(HttpMethod.Put, "offline", new { offline });

    /// <summary>Adds a fault: <c>new { path = "regex", status = 404, times = 1, delayMs = 0, abort = false, corrupt = false }</c>.</summary>
    public Task AddFaultAsync(object fault) => SendAsync(HttpMethod.Post, "faults", fault);

    public Task ClearFaultsAsync() => SendAsync(HttpMethod.Delete, "faults", null);

    public Task ClearRequestsAsync() => SendAsync(HttpMethod.Delete, "requests", null);

    public async Task<RequestRecord[]> RequestsAsync()
    {
        using var response = await _http.SendAsync(Request(HttpMethod.Get, "/_harness/requests"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RequestRecord[]>(_json))!;
    }

    /// <summary>The service-worker-assets.js the host currently serves for the app at <paramref name="appPath"/> ("/" or "/standalone/").</summary>
    public async Task<AssetsManifest> ManifestAsync(string appPath)
    {
        using var response = await _http.SendAsync(Request(HttpMethod.Get, appPath + "service-worker-assets.js"));
        response.EnsureSuccessStatusCode();

        // self.assetsManifest = { ... };
        var script = await response.Content.ReadAsStringAsync();
        var json = script[script.IndexOf('{')..(script.LastIndexOf('}') + 1)];
        return JsonSerializer.Deserialize<AssetsManifest>(json, _json)!;
    }

    /// <summary>A RegExp worker setting value.</summary>
    public static JsonObject Regex(string source, string flags = "") => new() { ["$regex"] = source, ["flags"] = flags };

    private async Task SendAsync(HttpMethod method, string command, object? body)
    {
        var request = Request(method, "/_harness/" + command);
        if (body is not null) request.Content = JsonContent.Create(body, options: _json);

        using var response = await _http.SendAsync(request);
        if (response.IsSuccessStatusCode is false)
            throw new InvalidOperationException($"{method} /_harness/{command} answered {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}{Environment.NewLine}{_host.RecentOutput}");
    }

    private HttpRequestMessage Request(HttpMethod method, string pathAndQuery)
    {
        // Sent to the loopback address with the session's host name: the host keys the session by it.
        var request = new HttpRequestMessage(method, $"http://127.0.0.1:{_host.Port}{pathAndQuery}");
        request.Headers.Host = $"{Id}.localhost:{_host.Port}";
        return request;
    }

    public sealed class RequestRecord
    {
        public string Method { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public string Query { get; set; } = string.Empty;

        public string Destination { get; set; } = string.Empty;

        public string FetchMode { get; set; } = string.Empty;

        public string ServiceWorker { get; set; } = string.Empty;

        public string Range { get; set; } = string.Empty;

        public long At { get; set; }

        public int Status { get; set; }

        public override string ToString() => $"{Method} {Path}{Query} [{Destination}] {Status}";
    }

    public sealed class AssetsManifest
    {
        public string Version { get; set; } = string.Empty;

        public List<Asset> Assets { get; set; } = [];

        public sealed class Asset
        {
            public string Url { get; set; } = string.Empty;

            public string? Hash { get; set; }
        }
    }
}
