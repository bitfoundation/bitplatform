using Bit.Bink.Demo.Client.Shared;

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
        // BinkCanvas needs no registration of its own. The demo adds one helper: saving bytes the app
        // produced (a serialized document) as a download. It only touches JS when it is used, so
        // resolving it during prerendering is harmless.
        services.AddScoped<FileDownloads>();

        return services;
    }
}
