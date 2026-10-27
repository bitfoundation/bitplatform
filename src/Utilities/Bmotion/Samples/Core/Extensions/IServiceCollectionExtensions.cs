using Bit.Bmotion;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Registered by every container a sample page is created in: the server's (prerendering, and
        // Interactive Server circuits), the browser's (Interactive WebAssembly) and the MAUI app's.
        services.AddBitBmotionServices(options =>
        {
            // The web hosts prerender every page. Animate serves each element at rest with its enter
            // animation compiled to CSS, which _content/Bit.Bmotion/bit-bmotion.css (linked by each web
            // host's App.razor) plays as soon as the HTML arrives, so nothing waits invisible at its
            // Initial state while the runtime starts. The runtime then takes each element over instead
            // of playing its enter a second time. The MAUI app never prerenders, so it has no effect there.
            options.Prerender = BmPrerenderMode.Animate;

            // Follow the operating system's reduced-motion setting everywhere, as the library
            // recommends for new apps: moves snap, fades still fade.
            options.ReducedMotion = BmReducedMotionMode.User;
        });

        return services;
    }
}
