using Bit.Bortable;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// The services of the shared pages, which every host calls: Bortable's drag coordinator, which every list
    /// injects. The web hosts call it in their server project as well as in their WebAssembly client, because the
    /// server prerenders the same pages and must resolve what they inject.
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Services registered in this class can be injected in client side (Web, Android, iOS, Windows, macOS)

        // Scoped: one per circuit on Blazor Server, one per app on WebAssembly and in a BlazorWebView, so two users
        // never share a drag. Without it the first list that renders throws, unable to resolve BortableService.
        services.AddBitBortableServices();

        return services;
    }
}
