namespace Bit.Belemetry.Demo.Client.Services;

/// <summary>
/// Where the API tables get their rows. The server answers from the reflected catalog directly
/// while prerendering; the browser asks the server's <c>/api/mcp</c> mirror.
/// </summary>
public interface IApiReference
{
    /// <summary>The reflected reference of one public Bit.Belemetry type, or null when it cannot be had.</summary>
    Task<ApiTypeInfo?> GetAsync(string typeName);
}
