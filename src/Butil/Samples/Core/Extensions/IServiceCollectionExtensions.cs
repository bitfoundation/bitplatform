using Bit.Butil;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in every sample host (Web, Server, Wasm, Auto, Android,
        // iOS, Windows, macOS). A Blazor Web App calls this from both of its containers - the server's, which
        // prerenders the pages, and the WebAssembly client's, which then hydrates them.
        services.AddBitButilServices();

        return services;
    }
}
