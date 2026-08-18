using Bit.Bmotion;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The services shared by the WebAssembly client and the prerendering host.
/// Both containers have to register the same set: every component is instantiated once on the
/// server for the prerender pass and again in the browser, so anything a page injects must
/// resolve in both places or prerendering fails with a missing-service exception.
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <param name="baseAddress">
    /// The app's own origin, for the <see cref="HttpClient"/> the MCP page calls the demo's
    /// /api/mcp endpoints with. The prerendering host passes nothing: it registers the client so
    /// the page's injection resolves, but the page only ever issues a request from a button, which
    /// cannot happen during prerendering.
    /// </param>
    public static IServiceCollection AddDemoServices(this IServiceCollection services, string? baseAddress = null)
    {
        services.AddBitBmotionServices();

        services.AddScoped(_ => baseAddress is null
            ? new HttpClient()
            : new HttpClient { BaseAddress = new Uri(baseAddress) });

        return services;
    }
}
