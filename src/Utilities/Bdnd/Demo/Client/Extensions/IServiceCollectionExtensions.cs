using Bit.Bdnd;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The services shared by the WebAssembly client and the server host. Both containers have to
/// register the same set: every page is prerendered on the server before it runs in the browser,
/// so anything a page injects must resolve in both.
/// </summary>
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDemoServices(this IServiceCollection services)
    {
        // Required on the server, which runs the components of the Blazor Server page. In the browser the
        // draggables and drop targets would create BdndInterop themselves, but two pages inject it: the
        // sortable card, to measure the hovered card, and the custom drag layer.
        services.AddBitBdndServices();

        return services;
    }
}
