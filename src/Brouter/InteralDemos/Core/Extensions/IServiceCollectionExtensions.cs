using Bit.Brouter;
using Bit.Brouter.Demos.Core;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Services registered in this class can be injected in client side (Web, Android, iOS, Windows, macOS)
        services.AddBitBrouterServices(o =>
        {
            // Scroll & focus management: new navigations land at the top, Back/Forward restores
            // where you left each page (persisted per-tab), and focus moves to the page heading
            // so assistive tech announces it.
            o.ScrollBehavior = BrouterScrollMode.ToTop;
            o.RestoreScrollPosition = true;
            o.ScrollPositionStorage = BrouterScrollPositionStorage.SessionStorage;
            o.FocusOnNavigateSelector = "h1";

            // Animate page changes with the browser's View Transitions API (inert where unsupported).
            o.ViewTransitions = true;
        });

        services.AddScoped<DemoState>();

        return services;
    }
}
