using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Bortable;

/// <summary>
/// Extension methods to register the Bit.Bortable services in the DI container.
/// </summary>
public static class BitBortable
{
    /// <summary>
    /// Registers the coordinator that every <see cref="Bortable{TItem}"/> needs to track the active drag
    /// across lists. It is scoped, which is per circuit on Blazor Server and per application on
    /// WebAssembly, so two users never share a drag. In a Blazor Web App, call it in both the server and
    /// the client project; calling it more than once is harmless:
    /// <code>builder.Services.AddBitBortableServices();</code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection AddBitBortableServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<BortableService>();
        return services;
    }
}
