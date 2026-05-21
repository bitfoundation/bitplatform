using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Brouter;

public static class BitBrouter
{
    /// <summary>
    /// Registers the services required by Bit.Brouter, including <see cref="IBrouter"/> for
    /// programmatic navigation, named-route resolution and global navigation hooks.
    /// </summary>
    public static IServiceCollection AddBitBrouterServices(this IServiceCollection services,
                                                           Action<BrouterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new BrouterOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddScoped<BrouterService>();
        services.TryAddScoped<IBrouter>(sp => sp.GetRequiredService<BrouterService>());
        return services;
    }
}
