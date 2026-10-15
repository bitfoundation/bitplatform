using System.Net.Http.Json;
using System.Text.Json;
using Bit.Belemetry.Tests.Harness.Web.Collector;

namespace Bit.Belemetry.Tests.E2E.Infrastructure;

/// <summary>Reads the capturing receiver (<c>/test-collector/received</c>) of a web harness host.</summary>
public sealed class CollectorClient(string origin)
{
    /// <summary>
    /// Long enough for a WebAssembly boot in Debug plus a flush interval (500 ms) and a retry, on a slow CI runner.
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(45);

    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public string Origin { get; } = origin.TrimEnd('/');

    public async Task<CollectedTelemetry> ReceivedAsync(string? session = null, string? marker = null)
    {
        var query = new List<string>();
        if (session is not null) query.Add("session=" + Uri.EscapeDataString(session));
        if (marker is not null) query.Add("marker=" + Uri.EscapeDataString(marker));

        var url = $"{Origin}/test-collector/received{(query.Count > 0 ? "?" + string.Join('&', query) : string.Empty)}";
        return await _http.GetFromJsonAsync<CollectedTelemetry>(url, _json) ?? new CollectedTelemetry();
    }

    /// <summary>
    /// Polls until <paramref name="pick"/> finds something in what <paramref name="session"/> /
    /// <paramref name="marker"/> received, and fails the test with what did arrive when nothing does.
    /// </summary>
    public async Task<T> WaitForAsync<T>(Func<CollectedTelemetry, T?> pick, string what, string? session = null, string? marker = null, TimeSpan? timeout = null) where T : class
    {
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        CollectedTelemetry received;
        while (true)
        {
            received = await ReceivedAsync(session, marker);
            if (pick(received) is { } found) return found;
            if (DateTime.UtcNow > deadline) break;
            await Task.Delay(250);
        }

        Assert.Fail($"The receiver never got {what} (session {session ?? "any"}, marker {marker ?? "any"}). {Describe(received)}");
        throw new InvalidOperationException("Unreachable.");
    }

    public static string Describe(CollectedTelemetry received) =>
        $"It holds {received.Logs.Count} log(s) [{string.Join(", ", received.Logs.Select(l => l.EventName ?? l.Body).Distinct().Take(30))}], " +
        $"{received.Spans.Count} span(s) [{string.Join(", ", received.Spans.Select(s => s.Name).Distinct().Take(30))}], " +
        $"{received.Envelopes.Count} envelope(s) [{string.Join(", ", received.Envelopes.Select(e => e.Name).Distinct())}].";
}
