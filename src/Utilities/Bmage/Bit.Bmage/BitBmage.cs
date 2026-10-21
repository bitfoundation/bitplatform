using System.Runtime.CompilerServices;
using Bit.Bmage.Capabilities;
using Bit.Bmage.Input;
using Bit.Bmage.Interop;
using Bit.Bmage.Output;
using Bit.Bmage.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;

namespace Bit.Bmage;

/// <summary>Registers Bmage with the dependency injection container.</summary>
public static class BitBmage
{
    // One container per browser connection for applications that never called AddBitBmageServices. Keyed weakly, so a
    // closed Blazor Server circuit takes its container with it.
    private static readonly ConditionalWeakTable<IJSRuntime, IServiceProvider> _ownServices = new();

    /// <summary>
    /// Adds the Bmage services: capability detection, image loading, processing and export.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Services are registered as scoped, which is correct for both hosting models: in WebAssembly a scope lives for the
    /// whole application, and in Server one scope exists per circuit, so browser state is never shared between users.
    /// </para>
    /// <para>
    /// In a Blazor Web App, register this in the client project for WebAssembly and Auto render modes, and in the server
    /// project as well if any component uses Interactive Server.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddBitBmageServices(options =>
    /// {
    ///     options.Limits = ImageLimits.Default with { MaxPixels = 40_000_000 };
    ///     options.MaxSourceBytes = 32 * 1024 * 1024;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddBitBmageServices(this IServiceCollection services, Action<BitBmageOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new BitBmageOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        services.TryAddScoped<BrowserImageInterop>();
        services.TryAddScoped<BrowserCapabilityProvider>();
        services.TryAddScoped<IImageCapabilityProvider>(sp => sp.GetRequiredService<BrowserCapabilityProvider>());
        services.TryAddScoped(sp => new ImageProcessorFactory(
            sp.GetRequiredService<BrowserImageInterop>(),
            sp.GetRequiredService<IImageCapabilityProvider>(),
            sp.GetRequiredService<BitBmageOptions>()));
        services.TryAddScoped(sp =>
        {
            var loader = new ImageLoader(
                sp.GetRequiredService<ImageProcessorFactory>(),
                sp.GetRequiredService<BrowserImageInterop>(),
                sp.GetRequiredService<IImageCapabilityProvider>());
            loader.MaxSourceBytes = sp.GetRequiredService<BitBmageOptions>().MaxSourceBytes;
            return loader;
        });
        services.TryAddScoped<ImageExporter>();
        services.TryAddScoped<BrowserTextRasterizer>();
        services.TryAddScoped<FontTextRasterizer>();
        return services;
    }

    /// <summary>
    /// Adds Bmage without any browser integration, for server-side or console use. Only the managed codecs
    /// (PNG, JPEG, BMP and GIF decoding) are available.
    /// </summary>
    public static IServiceCollection AddBitBmageHeadlessServices(this IServiceCollection services, Action<BitBmageOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new BitBmageOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);
        services.TryAddSingleton<IImageCapabilityProvider>(ManagedCapabilityProvider.Instance);
        services.TryAddSingleton(sp =>
        {
            var o = sp.GetRequiredService<BitBmageOptions>();
            return new Pipeline.ImageProcessor(Codecs.ImageCodecRegistry.CreateDefault(), o.Allocator, o.Limits);
        });
        services.TryAddSingleton<FontTextRasterizer>();
        return services;
    }

    /// <summary>
    /// The container the editor components take their services from: the application's own when
    /// <see cref="AddBitBmageServices"/> registered them, otherwise one Bmage builds once per browser connection.
    /// </summary>
    /// <remarks>
    /// The second case is what lets a WebAssembly app lazy load Bit.Bmage: the assembly is not there yet when
    /// <c>Program.cs</c> runs, so it cannot be registered. The choice is made once for every service rather than per
    /// service, so a partial registration (for example <see cref="AddBitBmageHeadlessServices"/> alone) never mixes the
    /// application's services with Bmage's defaults; its <see cref="BitBmageOptions"/> are still honoured.
    /// </remarks>
    internal static IServiceProvider GetEditorServices(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (services.GetService<ImageLoader>() is not null) return services;

        var jsRuntime = services.GetRequiredService<IJSRuntime>();
        var options = services.GetService<BitBmageOptions>();
        return _ownServices.GetValue(jsRuntime, js =>
        {
            var own = new ServiceCollection().AddSingleton(js);
            if (options is not null) own.AddSingleton(options);
            return own.AddBitBmageServices().BuildServiceProvider();
        });
    }
}
