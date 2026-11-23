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
    public static IServiceCollection AddBitBevToolsBelemetry(this IServiceCollection services, Action<BevToolsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBitBevToolsServices(options =>
        {
            configure?.Invoke(options);
            options.AddBelemetry();
        });

        return services;
    }

    /// <summary>
    /// The Belemetry integration as an option, for the configuration an app gives <c>Bit.BevTools.Loader</c>
    /// (<c>BevToolsLoaderOptions.ConfigureBevTools</c>), which serves a BevTools registered at startup and one attached on
    /// demand alike: <c>o.ConfigureBevTools(static options =&gt; ((BevToolsOptions)options).AddBelemetry())</c>, with
    /// <c>"Bit.BevTools.Belemetry"</c> in <c>BevToolsLoaderOptions.AdditionalAssemblies</c> and the assembly marked
    /// <c>BlazorWebAssemblyLazyLoad</c> wherever Bit.BevTools is. It adds the Support panel, and - where BevTools registers
    /// at startup - the services Belemetry asks BevTools through (<see cref="IBelemetryComponentContextProvider"/>,
    /// <see cref="IBelemetryDiagnosticComponentFilter"/>). A session attached later has the Support panel only: the
    /// container Belemetry resolves those services from was built before Bit.BevTools was loaded, so its captures carry
    /// no component hierarchy and its timeline does not tell BevTools' own UI events apart. Calling it twice adds nothing.
    /// </summary>
    public static BevToolsOptions AddBelemetry(this BevToolsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Extensions.OfType<BevToolsBelemetryExtension>().Any() is false)
        {
            options.Extensions.Add(new BevToolsBelemetryExtension());
        }

        options.ServiceRegistrations.Add(static services =>
        {
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IBelemetryComponentContextProvider, BevToolsBelemetryComponentContext>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IBelemetryDiagnosticComponentFilter, BevToolsBelemetryComponentFilter>());
        });

        return options;
    }
}
