using System.Diagnostics.CodeAnalysis;
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
    public static IServiceCollection AddBormMessages<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>(
        this IServiceCollection services)
        where TProvider : class, IBormMessageProvider
    {
        services.TryAddScoped<IBormMessageProvider, TProvider>();
        return services;
    }

    /// <summary>
    /// Translates the schema's own text — labels, help, placeholders, options, step titles — through
    /// <typeparamref name="TLocalizer"/>, typically one wrapping <c>IStringLocalizer</c>. See
    /// <see cref="IBormSchemaLocalizer"/> for the keys it is asked for.
    /// </summary>
    public static IServiceCollection AddBormSchemaLocalizer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TLocalizer>(
        this IServiceCollection services)
        where TLocalizer : class, IBormSchemaLocalizer
    {
        services.TryAddScoped<IBormSchemaLocalizer, TLocalizer>();
        return services;
    }

    /// <summary>
    /// Has the library speak German, Spanish, French, Italian, Dutch, Portuguese, Russian, Turkish, Persian, Arabic,
    /// Japanese and simplified Chinese - its validation messages, buttons, announcements and designer - in the
    /// form's <see cref="BormState.Language"/> when it has one, else in the current UI culture, and English otherwise.
    /// See <see cref="BormTranslatedMessageProvider"/>.
    /// </summary>
    /// <remarks>
    /// The schema's own words - labels, help, options - are the schema's to translate (<c>x-translations</c>,
    /// <see cref="IBormSchemaLocalizer"/>). A provider registered first, by <see cref="AddBormMessages{TProvider}"/>
    /// or an instance, is kept: this is a <c>TryAdd</c>.
    /// </remarks>
    public static IServiceCollection AddBormTranslations(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IBormMessageProvider>(BormTranslatedMessageProvider.Instance);
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
