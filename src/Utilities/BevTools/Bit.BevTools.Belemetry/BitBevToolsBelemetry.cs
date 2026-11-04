using Bit.Belemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BevTools;

public static class BitBevToolsBelemetry
{
    /// <summary>
    /// Adds the Belemetry integration to BevTools: the Support panel (Belemetry's diagnostic timeline and exception
    /// groups, a jump from a failure to its component, support bundle preview, export and import), and BevTools' component
    /// tree and render counts in Belemetry's captures (<see cref="IBelemetryComponentContextProvider"/>), and BevTools' own
    /// UI events kept off Belemetry's timeline (<see cref="IBelemetryDiagnosticComponentFilter"/>). Call it next to
    /// <c>AddBitBelemetryServices</c> and <c>AddBitBevToolsServices</c>, in every project that hosts Blazor.
    /// </summary>
    /// <remarks>
    /// Belemetry keeps working without it, and BevTools without Belemetry: a production app that leaves BevTools out still
    /// makes support bundles, just without the component hierarchy.
    /// </remarks>
    public static IServiceCollection AddBitBevToolsBelemetry(this IServiceCollection services, Action<BitBevToolsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IBelemetryComponentContextProvider, BevBelemetryComponentContext>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBelemetryDiagnosticComponentFilter, BevBelemetryComponentFilter>());
        services.AddBitBevToolsServices(options =>
        {
            configure?.Invoke(options);
            if (options.Extensions.OfType<BevToolsBelemetryExtension>().Any() is false)
            {
                options.Extensions.Add(new BevToolsBelemetryExtension());
            }
        });

        return services;
    }
}
