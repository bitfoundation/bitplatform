using Bit.Bmage;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    /// <param name="configure">What a host adds to the shared options, such as the threads it can give an image.</param>
    public static IServiceCollection AddCoreServices(this IServiceCollection services, Action<BitBmageOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Registered by every container a sample page is created in: the server's (prerendering, and
        // Interactive Server circuits), the browser's (Interactive WebAssembly) and the MAUI app's. The
        // services only reach for the browser when a page asks them to do something, so prerendering is safe.
        services.AddBitBmageServices(options =>
        {
            options.MaxSourceBytes = 32 * 1024 * 1024;
            options.Limits = ImageLimits.Default with { MaxPixels = 40_000_000 };
            configure?.Invoke(options);
        });

        return services;
    }
}
