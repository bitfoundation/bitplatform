using Bit.Bortable;

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
        // The drag coordinator the lists share. Without it in the server's container too, the first
        // prerendered list would throw "Bit.Bortable's services are not registered".
        services.AddBitBortableServices();

        return services;
    }
}
