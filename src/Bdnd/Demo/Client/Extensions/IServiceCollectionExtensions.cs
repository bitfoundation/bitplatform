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
        // The draggables and drop targets inject BdndInterop, and so does the sortable card,
        // which measures the hovered card through it.
        services.AddBitBdndServices();

        return services;
    }
}
