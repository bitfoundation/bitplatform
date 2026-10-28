using Bit.Balette;
using Bit.Balette.Samples.Core;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Registered by every container a sample page is created in: the server's (prerendering, and
        // Interactive Server circuits), the browser's (Interactive WebAssembly) and the MAUI app's. The
        // palette only reaches for the browser once it is interactive, so prerendering is safe.
        services.AddBitBaletteServices(options =>
        {
            // Groups appear in this order on the empty query; the ones not listed follow alphabetically.
            options.GroupOrder.AddRange(["Navigation", "Counter", "Flows", "Preferences", "Help"]);

            // A mode registered at startup. The "#" and "@" modes are registered at runtime by the
            // sources page instead, and leave with it.
            options.Modes.Add(new BaletteCommandMode
            {
                Prefix = ">",
                Name = "Commands",
                Description = "Run a command",
                Placeholder = "Run a command…",
                Groups = ["Counter", "Flows", "Preferences"],
            });
        });

        services.AddScoped<SampleState>();

        return services;
    }
}
