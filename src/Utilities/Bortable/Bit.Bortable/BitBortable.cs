using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Bortable;

/// <summary>
/// Extension methods to register the Bit.Bortable services in the DI container.
/// </summary>
public static class BitBortable
{
    /// <summary>
    /// Registers the coordinator that every <see cref="Bortable{TItem}"/> needs to track the active drag across
    /// lists. It is scoped, which is per circuit on Blazor Server and per application on WebAssembly, so two users
    /// never share a drag. In a Blazor Web App, call it in both the server and the client project; calling it more
    /// than once is harmless:
    /// <code>builder.Services.AddBitBortableServices();</code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection AddBitBortableServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<BortableService>();
        return services;
    }

    /// <summary>
    /// Registers the services (see <see cref="AddBitBortableServices(IServiceCollection)"/>) and sets app-wide
    /// defaults for every list - a slower animation, a longer long press, the texts in the app's language:
    /// <code>builder.Services.AddBitBortableServices(options =&gt; options.Messages = German);</code>
    /// A list that sets a parameter itself keeps its own value. Several calls add up, in order.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Sets the defaults.</param>
    public static IServiceCollection AddBitBortableServices(this IServiceCollection services, Action<BortableOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.AddBitBortableServices();
        services.AddSingleton(new BortableConfiguration(configure));
        return services;
    }
}
