using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Brouter;

public static class BitBrouter
{
    /// <summary>
    /// Marker singleton registered exactly once by <see cref="AddBitBrouterServices"/> so the
    /// "called more than once" check is unambiguous. Probing for <see cref="BrouterOptions"/>
    /// is brittle: any other library that happens to register a <c>BrouterOptions</c> service
    /// (or a future change that swaps the registration shape) would falsely trip the guard.
    /// A dedicated marker type avoids both of those failure modes.
    /// </summary>
    private sealed class BitBrouterMarker { }

    /// <summary>
    /// Registers the services required by Bit.Brouter, including <see cref="IBrouter"/> for
    /// programmatic navigation, named-route resolution and global navigation hooks.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this method has already been called on the same <paramref name="services"/>
    /// instance. Calling it twice would either silently discard the new <paramref name="configure"/>
    /// callback (because the second <c>AddSingleton(BrouterOptions)</c> would lose to the first
    /// via <c>TryAdd</c>) or replace previously-registered configuration; both are surprising,
    /// so we fail loud instead.
    /// </exception>
    public static IServiceCollection AddBitBrouterServices(this IServiceCollection services,
                                                           Action<BrouterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Use a dedicated marker rather than probing for BrouterOptions so we don't
        // misidentify foreign registrations of the same type as a duplicate call.
        for (int i = 0; i < services.Count; i++)
        {
            if (services[i].ServiceType == typeof(BitBrouterMarker))
            {
                throw new InvalidOperationException(
                    $"{nameof(AddBitBrouterServices)} has already been called on this service collection. " +
                    "Bit.Brouter services must only be registered once to avoid silently discarding configuration.");
            }
        }

        var options = new BrouterOptions();
        configure?.Invoke(options);

        services.AddSingleton(new BitBrouterMarker());
        services.AddSingleton(options);
        services.TryAddScoped<BrouterService>();
        services.TryAddScoped<IBrouter>(sp => sp.GetRequiredService<BrouterService>());
        return services;
    }
}
