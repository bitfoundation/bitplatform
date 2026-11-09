using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Bheadless;

/// <summary>
/// Service-collection extensions for registering Bheadless's runtime services.
/// </summary>
public static class BitBhServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services used by Bheadless components: the JS runtime
    /// wrapper (<see cref="BitBhInterop"/>), the live-region announcer
    /// (<see cref="IBitBhAnnouncer"/>) and the toast queue (<see cref="BitBhToaster"/>).
    /// Safe to call more than once.
    ///
    /// <para>
    /// Register once during application startup (in both the server and the
    /// client project of a Blazor Web App that uses WebAssembly rendering):
    /// <code>builder.Services.AddBitBheadlessServices();</code>
    /// </para>
    /// </summary>
    public static IServiceCollection AddBitBheadlessServices(this IServiceCollection services)
        => services.AddBitBheadlessServices(configureStrings: null);

    /// <summary>
    /// Registers the services used by Bheadless components (see <see cref="AddBitBheadlessServices(IServiceCollection)"/>)
    /// and the text the components write themselves (<see cref="BitBhStrings"/>): accessible names of parts without
    /// visible text and announcements, English unless <paramref name="configureStrings"/> changes them.
    /// </summary>
    public static IServiceCollection AddBitBheadlessServices(this IServiceCollection services, Action<BitBhStrings>? configureStrings)
    {
        if (configureStrings is not null)
        {
            var strings = new BitBhStrings();
            configureStrings(strings);
            services.AddSingleton(strings);
        }
        services.TryAddScoped<BitBhInterop>();
        services.TryAddScoped<IBitBhAnnouncer, BitBhAnnouncer>();
        services.TryAddScoped<BitBhToaster>();
        return services;
    }
}
