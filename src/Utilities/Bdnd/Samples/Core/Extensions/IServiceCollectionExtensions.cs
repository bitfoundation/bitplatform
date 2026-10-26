using Bit.Bdnd;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Registered by every container a sample page is created in: the server's (prerendering, and
        // Interactive Server circuits), the browser's (Interactive WebAssembly) and the MAUI app's. Only the
        // interop service is registered: each BdndProvider creates the drag state of its own subtree, and no
        // browser API is called before a page is interactive, so prerendering is safe.
        services.AddBitBdndServices();

        return services;
    }
}
