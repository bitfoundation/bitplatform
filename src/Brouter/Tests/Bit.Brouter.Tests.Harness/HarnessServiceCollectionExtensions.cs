using Bit.Brouter;
using Bit.Brouter.Tests.Harness;

namespace Microsoft.Extensions.DependencyInjection;

public static class HarnessServiceCollectionExtensions
{
    /// <summary>
    /// Registers Brouter with every browser-facing feature switched on, plus the per-scope
    /// <see cref="HarnessScope"/>. Every host calls this, so the options under test are identical
    /// wherever the harness runs.
    /// </summary>
    public static IServiceCollection AddBrouterHarness(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBitBrouterServices(o =>
        {
            o.ScrollBehavior = BrouterScrollMode.ToTop;
            o.RestoreScrollPosition = true;
            o.ScrollPositionStorage = BrouterScrollPositionStorage.SessionStorage;
            o.FocusOnNavigateSelector = "h1";
            o.ViewTransitions = true;
            o.PersistLoaderState = true;
            o.LoaderStateTypeInfoResolver = HarnessJsonContext.Default;
        });

        services.AddScoped<HarnessScope>();

        return services;
    }
}
