using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.BevTools;

public static class BitBevToolsLoader
{
    /// <summary>
    /// Lets the app open BevTools on demand without referencing Bit.BevTools at startup, so Blazor WebAssembly can
    /// lazy load it (<c>&lt;BlazorWebAssemblyLazyLoad Include="Bit.BevTools.wasm" /&gt;</c>).
    /// <para>
    /// When Bit.BevTools is among the assemblies the app starts with and should run from the first render
    /// (<see cref="BevToolsLoaderOptions.RegisterAtStartup"/>; Development by default), this registers it exactly as
    /// <c>AddBitBevToolsServices</c> would. Otherwise it installs the hooks a later attach needs, which cost next to
    /// nothing until then: a pass-through component activator, pass-through handlers in <c>IHttpClientFactory</c>
    /// pipelines, the framework's ActivitySource and meters, and the service collection for the Services panel. The
    /// parts named in <see cref="BevToolsLoaderOptions.AutoStart"/> (<see cref="BevToolsAutoStart.Logs"/>) start right
    /// away, on their own, and the session attached later takes over what they collected.
    /// </para>
    /// <para>
    /// Inject <see cref="BevToolsLoader"/> to open it, and render <c>&lt;BevToolsLoaderPanel /&gt;</c> in the layout.
    /// Calling this again configures the existing options; what the first call decided stays, and a BevTools the first
    /// call registered takes the <see cref="BevToolsLoaderOptions.ConfigureBevTools"/> callbacks a later one adds.
    /// </para>
    /// </summary>
    public static IServiceCollection AddBitBevToolsLoaderServices(this IServiceCollection services, Action<BevToolsLoaderOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.FirstOrDefault(d => d.ServiceType == typeof(BevToolsLoaderContext))?.ImplementationInstance is BevToolsLoaderContext existing)
        {
            configure?.Invoke(existing.Options);
            if (existing.RegisteredAtStartup && existing.EntryPoint is { } registered)
            {
                // A BevTools registered by the first call takes the ConfigureBevTools callbacks this one added; one
                // attached later reads them all when it attaches.
                existing.TryAddServices(registered, services, out _);
            }

            return services;
        }

        var options = new BevToolsLoaderOptions();
        configure?.Invoke(options);
        var context = new BevToolsLoaderContext(options, services);
        services.AddSingleton(context);
        services.TryAddScoped(sp => new BevToolsLoader(sp.GetRequiredService<BevToolsLoaderContext>(), sp, sp.GetRequiredService<IJSRuntime>()));

        // A lazy loaded Bit.BevTools fails this, quickly and without downloading anything, so it is not even tried
        // when the app said BevTools must wait.
        string reason;
        if (options.RegisterAtStartup is false)
        {
            context.Deferred(null, "BevToolsLoaderOptions.RegisterAtStartup was set to false.");
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

        // Both are idempotent, and both are read by a renderer when it is created, which is before any attach.
        if (options.UseFrameworkInstrumentation)
        {
            ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(services);
            ComponentsMetricsServiceCollectionExtensions.AddComponentsMetrics(services);
        }

        // Before the activator hook, which takes the auto-started logs from the context when a scope creates it.
        StartAutoStartParts(services, context);
        BevToolsActivatorHook.Register(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, BevToolsHttpHookFilter>());
        return services;
    }

    /// <summary>
    /// Starts the parts of BevTools the app asked to run from startup (<see cref="BevToolsLoaderOptions.AutoStart"/>),
    /// each on its own and inside the loader, while the rest of BevTools waits for an attach. Only reached while BevTools
    /// is not registered at startup: one that is runs whole from the first render.
    /// </summary>
    private static void StartAutoStartParts(IServiceCollection services, BevToolsLoaderContext context)
    {
        if ((context.Options.AutoStart & BevToolsAutoStart.Logs) != 0)
        {
            var logs = context.StartAutoStartLogs();
            // A provider of the container, not one added to the factory later: the factory takes it when it is built,
            // so it sees the app's first entry, and the host's logging configuration filters it like any other.
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, BevToolsAutoStartLogProvider>(sp =>
            {
                logs.Resolve(sp);
                logs.ProviderCreated();
                return new BevToolsAutoStartLogProvider(logs);
            }));
        }
    }
}
