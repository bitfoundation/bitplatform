using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

public static class BitBevTools
{
    /// <summary>
    /// Registers BevTools. Enabled only in Development unless <see cref="BitBevToolsOptions.Enabled"/> is set.
    /// Place <c>&lt;BevToolsPanel /&gt;</c> inside an interactive component (typically the layout) to show the UI.
    /// <para>
    /// Calling this more than once (for example <c>AddBitBevToolsServices()</c> and then <c>AddBitBevToolsServerServices()</c>)
    /// applies the additional configuration to the existing options instead of being ignored.
    /// </para>
    /// </summary>
    public static IServiceCollection AddBitBevToolsServices(this IServiceCollection services, Action<BitBevToolsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Second and later calls configure the options that are already registered, so extensions and thresholds
        // contributed by different packages accumulate instead of silently overwriting each other.
        if (services.FirstOrDefault(d => d.ServiceType == typeof(BitBevToolsOptions))?.ImplementationInstance is BitBevToolsOptions existing)
        {
            configure?.Invoke(existing);
            var (mergedEnabled, mergedReason) = ResolveEnabled(services, existing);
            existing.ResolvedEnabled = mergedEnabled;
            ReplaceRegistry(services, existing, mergedEnabled, mergedReason);
            if (mergedEnabled)
            {
                RegisterInstrumentation(services, existing);
            }

            ApplyServiceRegistrations(services, mergedEnabled);
            return services;
        }

        var options = new BitBevToolsOptions();
        configure?.Invoke(options);
        var (enabled, reason) = ResolveEnabled(services, options);
        options.ResolvedEnabled = enabled;

        services.AddSingleton(options);
        ReplaceRegistry(services, options, enabled, reason);

        services.AddScoped<BevToolsSession>();
        services.AddScoped<IBevToolsTimeline>(sp => sp.GetRequiredService<BevToolsSession>().TimelineApi);
        services.AddScoped<IBevStateProviderRegistry>(sp => sp.GetRequiredService<BevToolsSession>().StateProviders);
        ApplyServiceRegistrations(services, enabled);

        if (!enabled)
        {
            return services;
        }

        RegisterInstrumentation(services, options);

        return services;
    }

    /// <summary>Registers an extension. Safe to combine with other <c>AddBitBevToolsServices</c> calls.</summary>
    public static IServiceCollection AddBitBevToolsServicesExtension<TExtension>(this IServiceCollection services, Action<BitBevToolsOptions>? configure = null) where TExtension : IBevToolsExtension, new()
        => services.AddBitBevToolsServices(o =>
        {
            o.Extensions.Add(new TExtension());
            configure?.Invoke(o);
        });

    /// <summary>Registers a state provider so the State panel shows it. The provider type must also be resolvable (it is registered as scoped if not already).</summary>
    public static IServiceCollection AddBevToolsStateProvider<TProvider>(this IServiceCollection services) where TProvider : class, IBevStateProvider
    {
        services.TryAddScoped<TProvider>();
        services.AddScoped<IBevStateProvider>(sp => sp.GetRequiredService<TProvider>());
        return services;
    }

    /// <summary>Exposes an already registered service as a state provider through an adapter.</summary>
    public static IServiceCollection AddBevToolsStateProvider<TService>(this IServiceCollection services, string name, Func<TService, object?> snapshot, Action<TService, Action>? subscribe = null) where TService : class
    {
        var serviceLifetime = services.LastOrDefault(d => d.ServiceType == typeof(TService) && !d.IsKeyedService)?.Lifetime;
        var adapterLifetime = serviceLifetime == ServiceLifetime.Singleton ? ServiceLifetime.Singleton : ServiceLifetime.Scoped;
        services.Add(ServiceDescriptor.Describe(
            typeof(IBevStateProvider),
            sp => new DelegateStateProvider<TService>(sp.GetRequiredService<TService>(), name, snapshot, subscribe),
            adapterLifetime));
        return services;
    }

    /// <summary>
    /// Exposes an already registered service as a state provider through an adapter whose subscription is released
    /// with the BevTools scope. Prefer this overload when the observed service can outlive the scope.
    /// </summary>
    public static IServiceCollection AddBevToolsStateProviderWithSubscription<TService>(this IServiceCollection services, string name, Func<TService, object?> snapshot, Func<TService, Action, IDisposable> subscribe) where TService : class
    {
        ArgumentNullException.ThrowIfNull(subscribe);
        services.AddScoped<IBevStateProvider>(sp => new DelegateStateProvider<TService>(sp.GetRequiredService<TService>(), name, snapshot, subscribe));
        return services;
    }

    private static void ReplaceRegistry(IServiceCollection services, BitBevToolsOptions options, bool enabled, string reason)
    {
        var previous = services.FirstOrDefault(d => d.ServiceType == typeof(BevToolsRegistry));
        if (previous is not null)
        {
            services.Remove(previous);
        }

        services.AddSingleton(sp => new BevToolsRegistry(options, enabled, reason, services));
    }

    private static void RegisterInstrumentation(IServiceCollection services, BitBevToolsOptions options)
    {
#if NET10_0_OR_GREATER
        // The framework only registers its own ActivitySource/meters for server-side rendering. Registering them here
        // is what makes "why did this render?" work in WebAssembly and in any other host; both calls are public and
        // idempotent. Both arrived in .NET 10: on .NET 8 and 9 there is nothing to register, and the capability report
        // says the event-driven render causes need runtime support.
        if (options.UseFrameworkInstrumentation)
        {
            ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(services);
            ComponentsMetricsServiceCollectionExtensions.AddComponentsMetrics(services);
        }
#endif

        if (!services.Any(d => d.ServiceType == typeof(IComponentActivator) && d.ImplementationType == typeof(BevToolsComponentActivator)))
        {
            WrapExistingActivator(services);
            services.AddScoped<IComponentActivator, BevToolsComponentActivator>();
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, BevToolsHttpFilter>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, BevToolsLoggerProvider>());
    }

    private static void ApplyServiceRegistrations(IServiceCollection services, bool enabled)
    {
        foreach (var registration in services
            .Where(descriptor => descriptor.ServiceType == typeof(IBevToolsServiceRegistration))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<IBevToolsServiceRegistration>()
            .ToArray())
        {
            registration.Apply(services, enabled);
        }
    }

    private static void WrapExistingActivator(IServiceCollection services)
    {
        var existing = services.LastOrDefault(d => d.ServiceType == typeof(IComponentActivator) && !d.IsKeyedService);
        if (existing is null)
        {
            return;
        }

        services.Remove(existing);
        ServiceDescriptor holder;
        if (existing.ImplementationInstance is IComponentActivator instance)
        {
            holder = new ServiceDescriptor(typeof(InnerComponentActivator), new InnerComponentActivator(instance));
        }
        else if (existing.ImplementationFactory is { } factory)
        {
            holder = new ServiceDescriptor(typeof(InnerComponentActivator), sp => new InnerComponentActivator((IComponentActivator)factory(sp)), existing.Lifetime);
        }
        else if (existing.ImplementationType is { } type)
        {
            holder = new ServiceDescriptor(typeof(InnerComponentActivator), sp => new InnerComponentActivator((IComponentActivator)ActivatorUtilities.CreateInstance(sp, type)), existing.Lifetime);
        }
        else
        {
            return;
        }

        services.Add(holder);
    }

    /// <summary>
    /// Decides whether BevTools runs, and records why. The reason is shown in the About panel so a developer who
    /// expected BevTools and does not see it can tell immediately what the host reported.
    /// </summary>
    private static (bool Enabled, string Reason) ResolveEnabled(IServiceCollection services, BitBevToolsOptions options)
    {
        if (options.Enabled is { } explicitValue)
        {
            return (explicitValue, "BitBevToolsOptions.Enabled was set to " + explicitValue.ToString().ToLowerInvariant() + ".");
        }

        foreach (var descriptor in services)
        {
            if (descriptor.ImplementationInstance is null)
            {
                continue;
            }

            if (descriptor.ServiceType == typeof(IHostEnvironment) && descriptor.ImplementationInstance is IHostEnvironment env)
            {
                return (env.IsDevelopment(), $"IHostEnvironment reports environment '{env.EnvironmentName}'.");
            }

            if (descriptor.ServiceType.FullName == "Microsoft.AspNetCore.Components.WebAssembly.Hosting.IWebAssemblyHostEnvironment")
            {
                var wasm = descriptor.ImplementationInstance;
                var environment = wasm.GetType().GetProperty("Environment")?.GetValue(wasm) as string;
                return (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase),
                    $"IWebAssemblyHostEnvironment reports environment '{environment ?? "unknown"}'.");
            }
        }

        var variable = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        if (!string.IsNullOrEmpty(variable))
        {
            return (string.Equals(variable, "Development", StringComparison.OrdinalIgnoreCase), $"Environment variable reports '{variable}'.");
        }

        return (false, "No host environment information was available at registration time, so BevTools stayed off. Set BitBevToolsOptions.Enabled explicitly.");
    }

    private sealed class DelegateStateProvider<TService> : IBevStateProvider, IDisposable where TService : class
    {
        private readonly TService _service;
        private readonly Func<TService, object?> _snapshot;
        private IDisposable? _subscription;
        private int _disposed;

        public DelegateStateProvider(TService service, string name, Func<TService, object?> snapshot, Action<TService, Action>? subscribe)
        {
            _service = service;
            _snapshot = snapshot;
            Name = name;
            subscribe?.Invoke(service, Raise);
        }

        public DelegateStateProvider(TService service, string name, Func<TService, object?> snapshot, Func<TService, Action, IDisposable> subscribe)
        {
            _service = service;
            _snapshot = snapshot;
            Name = name;
            _subscription = subscribe(service, Raise);
        }

        public string Name { get; }

        public string? Description => TypeNames.Short(typeof(TService));

        public object? GetSnapshot() => Volatile.Read(ref _disposed) != 0 ? null : _snapshot(_service);

        private void Raise()
        {
            if (Volatile.Read(ref _disposed) == 0)
            {
                Changed?.Invoke(BevStateChangeInfo.Empty);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            Interlocked.Exchange(ref _subscription, null)?.Dispose();
            Changed = null;
        }

        public event Action<BevStateChangeInfo>? Changed;
    }
}

internal interface IBevToolsServiceRegistration
{
    void Apply(IServiceCollection services, bool enabled);
}
