using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.JSInterop;

namespace Bit.BevTools;

public static class BitBevToolsLoader
{
    /// <summary>
    /// Lets the app open BevTools on demand without referencing Bit.BevTools at startup, so Blazor WebAssembly can
    /// lazy load it (<c>&lt;BlazorWebAssemblyLazyLoad Include="Bit.BevTools.wasm" /&gt;</c>).
    /// <para>
    /// When Bit.BevTools is among the assemblies the app starts with and should run from the first render
    /// (<see cref="BitBevToolsLoaderOptions.RegisterAtStartup"/>; Development by default), this registers it exactly as
    /// <c>AddBitBevToolsServices</c> would. Otherwise it installs the hooks a later attach needs, which cost next to
    /// nothing until then: a pass-through component activator, pass-through handlers in <c>IHttpClientFactory</c>
    /// pipelines, the framework's ActivitySource and meters, and the service collection for the Services panel.
    /// </para>
    /// <para>
    /// Inject <see cref="BevToolsLoader"/> to open it, and render <c>&lt;BevToolsLoaderPanel /&gt;</c> in the layout.
    /// Calling this again configures the existing options; what the first call decided stays.
    /// </para>
    /// </summary>
    public static IServiceCollection AddBitBevToolsLoaderServices(this IServiceCollection services, Action<BitBevToolsLoaderOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.FirstOrDefault(d => d.ServiceType == typeof(BevToolsLoaderContext))?.ImplementationInstance is BevToolsLoaderContext existing)
        {
            configure?.Invoke(existing.Options);
            return services;
        }

        var options = new BitBevToolsLoaderOptions();
        configure?.Invoke(options);
        var context = new BevToolsLoaderContext(options, services);
        services.AddSingleton(context);
        services.TryAddScoped(sp => new BevToolsLoader(sp.GetRequiredService<BevToolsLoaderContext>(), sp, sp.GetRequiredService<IJSRuntime>()));

        // A lazy loaded Bit.BevTools fails this, quickly and without downloading anything, so it is not even tried
        // when the app said BevTools must wait.
        string reason;
        if (options.RegisterAtStartup is false)
        {
            context.Deferred(null, "BitBevToolsLoaderOptions.RegisterAtStartup was set to false.");
        }
        else if (context.TryLoadFromApp(out var error) is not { } entryPoint)
        {
            context.Deferred(null, error + " It is attached on demand instead.");
        }
        else if (context.TryAddServices(entryPoint, services, out reason))
        {
            context.Registered(entryPoint, reason);
            return services;
        }
        else
        {
            context.Deferred(entryPoint, reason);
        }

#if NET10_0_OR_GREATER
        // Both are idempotent, and both are read by a renderer when it is created, which is before any attach.
        if (options.UseFrameworkInstrumentation)
        {
            ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(services);
            ComponentsMetricsServiceCollectionExtensions.AddComponentsMetrics(services);
        }
#endif

        BevToolsActivatorHook.Register(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, BevToolsHttpHookFilter>());
        return services;
    }
}
