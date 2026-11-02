using Bit.Borm;
using Bit.Borm.Samples.Core;
using Bit.Borm.Samples.Core.Renderers;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Registered by every container a sample page is created in: the server's (prerendering, static forms and
        // Interactive Server circuits), the browser's (Interactive WebAssembly) and the MAUI app's. Borm calls no
        // browser API, so a form prerenders as plain HTML and becomes interactive with the page.
        services.AddBorm(registry => registry
            // A custom renderer, asked for with CustomRenderer("rating"). In a container without this registration the
            // builder sample would throw at render time, naming the key - so every host makes it, through here.
            .RegisterCustom<RatingInput>("rating"));

        services.AddSingleton<SampleDirectory>();

        return services;
    }
}
