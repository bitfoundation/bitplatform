using Bit.Bheadless;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The services shared by the WebAssembly client and the server.
/// Both containers have to register the same set: every page is instantiated once on the server for
/// the prerender pass (and again for Blazor Server rendering) and once more in the browser, so anything
/// a page injects - IBitBhAnnouncer, BitBhToaster - must resolve in both places.
/// </summary>
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDemoClientServices(this IServiceCollection services)
    {
        services.AddBitBheadlessServices();

        return services;
    }
}
