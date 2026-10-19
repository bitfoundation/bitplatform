using Bit.Belemetry.Samples.Core;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// The services of the shared pages. Belemetry itself is registered by each host's Program.cs, so that the setup
    /// of every hosting model stays visible in one place.
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services, Action<SampleSettings>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var settings = new SampleSettings();
        configure?.Invoke(settings);
        services.AddSingleton(settings);

        return services;
    }
}
