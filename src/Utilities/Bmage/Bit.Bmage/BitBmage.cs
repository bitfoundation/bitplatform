using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Bmage;

/// <summary>Registers Bmage with the dependency injection container.</summary>
/// <remarks>
/// The browser integration (capability detection, browser codecs, loading, export and the editor components) lives in
/// the Bit.Bmage.Blazor package, which registers it with <c>AddBitBmageServices</c>.
/// </remarks>
public static class BitBmage
{
    /// <summary>
    /// Adds Bmage without any browser integration, for server-side or console use. Only the managed codecs are
    /// available: PNG, JPEG, BMP and WebP, lossy and lossless, and GIF and AVIF decoding.
    /// </summary>
    public static IServiceCollection AddBitBmageHeadlessServices(this IServiceCollection services, Action<BmageOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new BmageOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);
        services.TryAddSingleton<IBmageImageCapabilityProvider>(BmageManagedCapabilityProvider.Instance);
        services.TryAddSingleton(sp =>
        {
            var o = sp.GetRequiredService<BmageOptions>();
            return new BmageImageProcessor(BmageImageCodecRegistry.CreateDefault(), o.Allocator, o.Limits) { MaxDegreeOfParallelism = o.MaxDegreeOfParallelism };
        });
        services.TryAddSingleton<BmageFontTextRasterizer>();
        return services;
    }
}
