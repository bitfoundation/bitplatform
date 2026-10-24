using Bit.Bmage;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Registered by every container a sample page is created in: the server's (prerendering, and
        // Interactive Server circuits) and the browser's (Interactive WebAssembly). The services only
        // reach for the browser when a page asks them to do something, so prerendering is safe.
        services.AddBitBmageServices(options =>
        {
            options.MaxSourceBytes = 32 * 1024 * 1024;
            options.Limits = ImageLimits.Default with { MaxPixels = 40_000_000 };
        });

        return services;
    }
}
