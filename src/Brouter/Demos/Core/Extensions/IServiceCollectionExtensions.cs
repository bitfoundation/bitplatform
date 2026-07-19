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

            // Demo-only: run the full animations even when the OS reports prefers-reduced-motion.
            // Windows "Animation effects" is commonly off on VMs/remote desktops for performance,
            // which would otherwise reduce this showcase to plain crossfades. Real applications
            // should usually leave this at its default (true) and respect the user's preference.
            o.ViewTransitionRespectReducedMotion = false;

            // Custom route constraint, scoped to this DI container. Templates can now use
            // {value:slug} exactly like a built-in constraint - see the /constraints tester.
            o.Constraints.Register("slug",
                new BrouterTypeRouteConstraint<string>((string s, out string r) =>
                {
                    r = s;
                    return s.Length >= 3 && s.All(c => char.IsLetterOrDigit(c) || c == '-');
                }));
        });

        services.AddScoped<DemoState>();

        return services;
    }
}
