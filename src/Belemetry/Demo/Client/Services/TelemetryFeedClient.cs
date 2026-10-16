using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Bit.Belemetry.Demo.Client.Services;

/// <summary>
/// Reads this reader's own telemetry back from the demo server: the browser session id from the
/// script (<c>BitBelemetry.getContext()</c>), then <c>GET /api/telemetry?sessionId=...</c>.
/// </summary>
public sealed class TelemetryFeedClient(HttpClient http, IJSRuntime js)
{
    /// <summary>What <c>BitBelemetry.getContext()</c> returns.</summary>
    public sealed record BrowserContext(int Consent, bool Decided, string? SessionId, string? PseudoId, string? UserId);

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    /// <summary>The browser script's context, or null while the script has not started (or outside a browser).</summary>
    public async Task<BrowserContext?> GetContextAsync()
    {
        try
        {
            // window.BitBelemetry is set by the module as soon as it loads; until then this throws.
            var element = await js.InvokeAsync<JsonElement>("BitBelemetry.getContext");
            return element.Deserialize<BrowserContext>(_json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>The records of <paramref name="sessionId"/> newer than <paramref name="after"/>.</summary>
    public async Task<TelemetryFeed?> GetAsync(string sessionId, long after, bool includeShared)
    {
        if (http.BaseAddress is null) return null;

        try
        {
            return await http.GetFromJsonAsync<TelemetryFeed>(
                $"api/telemetry?sessionId={Uri.EscapeDataString(sessionId)}&after={after}&shared={(includeShared ? "true" : "false")}", _json);
        }
        catch
        {
            return null;
        }
    }
}
