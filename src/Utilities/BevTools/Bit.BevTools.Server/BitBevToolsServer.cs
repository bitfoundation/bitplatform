using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BevTools;

public static class BitBevToolsServer
{
    /// <summary>
    /// Registers BevTools plus Blazor Server circuit instrumentation (circuit lifecycle, connection state,
    /// inbound message processing, active circuit overview). Call this in the server project; Interactive Auto apps
    /// must also call <c>AddBitBevToolsServices</c> in the client project so the WebAssembly render path is instrumented.
    /// </summary>
    public static IServiceCollection AddBitBevToolsServerServices(this IServiceCollection services, Action<BitBevToolsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<BevCircuitRegistry>();
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(IBevToolsServiceRegistration)
            && descriptor.ImplementationInstance is ServerServiceRegistration))
        {
            services.AddSingleton<IBevToolsServiceRegistration>(ServerServiceRegistration.Instance);
        }

        services.AddBitBevToolsServices(options =>
        {
            configure?.Invoke(options);
            if (!options.Extensions.OfType<BevToolsServerExtension>().Any())
            {
                options.Extensions.Add(new BevToolsServerExtension());
            }
        });

        return services;
    }

    private sealed class ServerServiceRegistration : IBevToolsServiceRegistration
    {
        public static readonly ServerServiceRegistration Instance = new();

        public void Apply(IServiceCollection services, bool enabled)
        {
            var existing = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(CircuitHandler)
                && descriptor.ImplementationType == typeof(BevToolsCircuitHandler));
            if (enabled && existing is null)
            {
                services.AddScoped<CircuitHandler, BevToolsCircuitHandler>();
            }
            else if (!enabled && existing is not null)
            {
                services.Remove(existing);
            }
        }
    }
}
