using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Brouter;

public static class BitBrouter
{
    /// <summary>
    /// Registers the services required by Bit.Brouter, including <see cref="IBrouter"/> for
    /// programmatic navigation, named-route resolution and global navigation hooks.
    /// </summary>
    /// <remarks>
    /// This method is idempotent, matching the convention every <c>Add*Services</c> extension in
    /// the .NET ecosystem follows: calling it more than once on the same
    /// <paramref name="services"/> instance never produces duplicate registrations, so a library
    /// and the host app can each call it without coordinating. The service registrations are made
    /// with <c>TryAdd</c>, which also means a caller that registered its own
    /// <see cref="IBrouter"/> / <see cref="BrouterService"/> beforehand keeps it.
    /// <para>
    /// The optional <paramref name="configure"/> callback follows the standard Options pattern and
    /// is <i>additive</i>: every callback passed across all calls runs, in call order, on the same
    /// <see cref="BrouterOptions"/> instance. Nothing is silently discarded, and later calls win on
    /// any property they set. Note that <see cref="BrouterConstraintRegistry.Register"/> rejects a
    /// duplicate constraint name, so if two call sites register the same custom constraint name,
    /// prefer registering it from a single place (or guard it) rather than relying on the last one
    /// winning.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddBitBrouterServices(this IServiceCollection services,
                                                           Action<BrouterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Use the standard Options pattern so callers can compose configuration from multiple
        // sources (e.g. appsettings binding + a Configure callback), use IOptionsMonitor for
        // change notifications in long-running processes, and integrate with PostConfigure.
        // AddOptions itself is idempotent, and Configure is additive by design.
        var optionsBuilder = services.AddOptions<BrouterOptions>();
        if (configure is not null) optionsBuilder.Configure(configure);

        services.TryAddScoped<BrouterService>();
        services.TryAddScoped<IBrouter>(sp => sp.GetRequiredService<BrouterService>());
        return services;
    }
}
