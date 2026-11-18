using Bit.Blex;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

public static class BitBevToolsBlex
{
    /// <summary>
    /// Shows bit Blex in BevTools' State panel: every store's state, a change per action, and restoring an earlier state
    /// (where BevTools allows restoring: Development, or a process that serves one user). Call after
    /// <c>AddBitBlexServices</c> and <c>AddBitBevToolsServices</c>.
    /// </summary>
    public static IServiceCollection AddBevToolsBlex(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IBevToolsStateProvider>(sp => new BevToolsBlexStateProvider(
            sp.GetRequiredService<BlexManager>(),
            sp.GetService<BevToolsOptions>()?.MaxStateChangesPerProvider ?? 100));
        return services;
    }
}
