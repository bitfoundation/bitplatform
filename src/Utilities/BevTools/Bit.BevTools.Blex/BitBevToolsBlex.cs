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

    /// <summary>
    /// Shows bit Blex in BevTools' State panel as <see cref="AddBevToolsBlex"/> does, as an option: for the
    /// configuration an app gives <c>Bit.BevTools.Loader</c> (<c>BevToolsLoaderOptions.ConfigureBevTools</c>), which serves a
    /// BevTools registered at startup and one attached on demand alike -
    /// <c>o.ConfigureBevTools(static options =&gt; ((BevToolsOptions)options).AddBlex())</c>, with
    /// <c>"Bit.BevTools.Blex"</c> in <c>BevToolsLoaderOptions.AdditionalAssemblies</c> and the assembly marked
    /// <c>BlazorWebAssemblyLazyLoad</c> wherever Bit.BevTools is. Needs <c>AddBitBlexServices</c>. Calling it twice, or next
    /// to <see cref="AddBevToolsBlex"/>, shows Blex once.
    /// </summary>
    public static BevToolsOptions AddBlex(this BevToolsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Extensions.OfType<BevToolsBlexExtension>().Any() is false)
        {
            options.Extensions.Add(new BevToolsBlexExtension(options));
        }

        return options;
    }
}
