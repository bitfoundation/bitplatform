using Bit.BevTools;
using Bit.BevTools.Samples.Core;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// The services of the shared pages. BevTools itself is registered by each host's Program.cs, so that the setup of
    /// every hosting model stays visible in one place.
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services, Action<SampleSettings>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var settings = new SampleSettings();
        configure?.Invoke(settings);
        services.AddSingleton(settings);

        // Through IHttpClientFactory, so BevTools records every request with no change to the calling code.
        services.AddHttpClient("samples");

        services.AddScoped<SampleSession>();

        // Shown in the State panel; harmless when BevTools is off.
        services.AddBevToolsStateProvider<CartState>();

        return services;
    }
}
