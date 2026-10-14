using System.Net.Http.Json;

namespace Bit.Belemetry.Demo.Client.Services;

/// <summary>The browser's <see cref="IApiReference"/>: the server's GET mirror of the MCP API tool.</summary>
public sealed class HttpApiReference(HttpClient http) : IApiReference
{
    private sealed record Result(ApiTypeInfo? Details, string? Message);

    public async Task<ApiTypeInfo?> GetAsync(string typeName)
    {
        if (http.BaseAddress is null) return null;

        try
        {
            var result = await http.GetFromJsonAsync<Result>($"api/mcp/GetBelemetryApiDetails?typeName={Uri.EscapeDataString(typeName)}");
            return result?.Details;
        }
        catch
        {
            // A table that cannot load says so on the page; it is reference material, not the demo.
            return null;
        }
    }
}
