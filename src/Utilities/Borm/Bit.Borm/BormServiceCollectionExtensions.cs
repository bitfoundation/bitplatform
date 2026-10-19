using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Borm;

/// <summary>DI registration for Borm's Blazor rendering services.</summary>
public static class BormServiceCollectionExtensions
{
    /// <summary>
    /// Registers Borm services, including the field renderer registry. Call
    /// <paramref name="configure"/> to override built-in renderers or register custom ones.
    /// </summary>
    /// <remarks>
    /// Calling this twice configures the registry that is already there rather than quietly throwing
    /// the second call's renderers away. The registration is <c>TryAdd</c>, so a second
    /// <c>AddBorm(r =&gt; r.RegisterCustom("rating", …))</c> — after a component library or a
    /// shared module had already called the parameterless overload — used to build a registry,
    /// configure it, discard it, and leave the application with a custom renderer key that resolved to
    /// nothing and a field that threw at render time naming a key the developer could see themselves
    /// registering.
    /// </remarks>
    public static IServiceCollection AddBorm(
        this IServiceCollection services,
        Action<IBormFieldRendererRegistry>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (Registered(services) is { } existing)
        {
            configure?.Invoke(existing);
            return services;
        }

        var registry = new BormFieldRendererRegistry();
        configure?.Invoke(registry);
        services.TryAddSingleton<IBormFieldRendererRegistry>(registry);
        return services;
    }

    /// <summary>
    /// The registry a previous call already put in the collection, when it was registered as an
    /// instance. A registry registered by type or by factory cannot be configured from here — it does
    /// not exist yet — so the caller gets the normal <c>TryAdd</c> behaviour.
    /// </summary>
    private static IBormFieldRendererRegistry? Registered(IServiceCollection services)
    {
        for (var i = 0; i < services.Count; i++)
        {
            if (services[i].ServiceType == typeof(IBormFieldRendererRegistry)
                && services[i].ImplementationInstance is IBormFieldRendererRegistry registry)
                return registry;
        }
        return null;
    }

    /// <summary>
    /// Replaces the English text of the built-in validation messages. Register your own
    /// <typeparamref name="TProvider"/> — typically one wrapping <c>IStringLocalizer</c> — to localise
    /// the whole library without rewriting any rules.
    /// </summary>
    public static IServiceCollection AddBormMessages<TProvider>(this IServiceCollection services)
        where TProvider : class, IBormMessageProvider
    {
        services.TryAddScoped<IBormMessageProvider, TProvider>();
        return services;
    }

    /// <summary>Registers a message provider instance (useful for a simple dictionary of overrides).</summary>
    public static IServiceCollection AddBormMessages(this IServiceCollection services, IBormMessageProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        services.TryAddSingleton(provider);
        return services;
    }
}
