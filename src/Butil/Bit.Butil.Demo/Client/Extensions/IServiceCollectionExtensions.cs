using Bit.Butil;
using Bit.Butil.Demo.Client.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The services shared by the WebAssembly client and the prerendering host.
/// Both containers have to register the same set: every component is instantiated once on the
/// server for the prerender pass and again in the browser, so anything a page injects must
/// resolve in both places or prerendering fails with a missing-service exception.
/// </summary>
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDemoServices(this IServiceCollection services)
    {
        services.AddBitButilServices();
        services.AddScoped<ThemeService>();

        return services;
    }
}
