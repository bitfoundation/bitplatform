using Bit.Belemetry.Demo.Client.Services;

namespace Bit.Belemetry.Demo.Server.Services;

/// <summary>
/// The prerendering host's <see cref="IApiReference"/>: the reflected catalog itself, rather than an HTTP
/// call from this server to its own <c>/api/mcp</c> mirror. The rows then reach the browser in the
/// persisted component state, so the table is in the served HTML and not fetched again on hydration.
/// </summary>
public sealed class ServerApiReference : IApiReference
{
    public Task<ApiTypeInfo?> GetAsync(string typeName) => Task.FromResult(BelemetryApiCatalog.Find(typeName));
}
