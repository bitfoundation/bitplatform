using Bit.Bonboarding;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The services shared by the WebAssembly client and the server host. Both containers have to
/// register the same set: with InteractiveAuto every component may run on the server (prerendering
/// and interactive Server) or in the browser, so anything a page injects must resolve in both.
/// </summary>
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDemoServices(this IServiceCollection services)
    {
        // Everything is scoped, which means one set per circuit on the server and one per app in
        // the browser.
        services.AddBitBonboardingServices(options =>
        {
            options.Labels.Skip = "Skip";
            options.StorageKeyPrefix = "bit-bonboarding-demo:";
        });

        return services;
    }
}
