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

        if (services.Any(s => s.ServiceType == typeof(BrouterOptions)))
        {
            throw new InvalidOperationException(
                $"{nameof(AddBitBrouterServices)} has already been called on this service collection. " +
                "Bit.Brouter services must only be registered once to avoid silently discarding configuration.");
        }

        var options = new BrouterOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.TryAddScoped<BrouterService>();
        services.TryAddScoped<IBrouter>(sp => sp.GetRequiredService<BrouterService>());
        return services;
    }
}
