using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;

namespace Bit.Bmage;

/// <summary>Registers Bmage's browser integration with the dependency injection container.</summary>
public static class BitBmageBlazor
{
    // One container per browser connection for applications that never called AddBitBmageServices. Keyed weakly, so a
    // closed Blazor Server circuit takes its container with it. Never disposed, and nothing needs it to be: it lives as
    // long as the connection, like a scope, and the one thing disposal would add, releasing the module reference held
    // in the browser, has no page left to do it in by then.
    private static readonly ConditionalWeakTable<IJSRuntime, IServiceProvider> _ownServices = new();

    /// <summary>
    /// Adds the Bmage services: capability detection, image loading, processing and export.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Services are registered as scoped, which is correct for every hosting model: in WebAssembly a scope lives for the
    /// whole application, in Server one scope exists per circuit, so browser state is never shared between users, and in
    /// Blazor Hybrid one exists per BlazorWebView.
    /// </para>
    /// <para>
    /// In a Blazor Web App, register this in the client project for WebAssembly and Auto render modes, and in the server
    /// project as well if any component uses Interactive Server. In a Blazor Hybrid app (MAUI, WPF, Windows Forms),
    /// register it once, next to <c>AddMauiBlazorWebView</c> or its equivalent.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddBitBmageServices(options =>
    /// {
    ///     options.Limits = BmageImageLimits.Default with { MaxPixels = 40_000_000 };
    ///     options.MaxSourceBytes = 32 * 1024 * 1024;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddBitBmageServices(this IServiceCollection services, Action<BmageOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new BmageOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        services.TryAddScoped<BmageBrowserImageInterop>();
        services.TryAddScoped<BmageBrowserCapabilityProvider>();
        services.TryAddScoped<IBmageImageCapabilityProvider>(sp => sp.GetRequiredService<BmageBrowserCapabilityProvider>());
        services.TryAddScoped(sp => new BmageImageProcessorFactory(
            sp.GetRequiredService<BmageBrowserImageInterop>(),
            sp.GetRequiredService<IBmageImageCapabilityProvider>(),
            sp.GetRequiredService<BmageOptions>()));
        services.TryAddScoped(sp =>
        {
            var loader = new BmageImageLoader(
                sp.GetRequiredService<BmageImageProcessorFactory>(),
                sp.GetRequiredService<BmageBrowserImageInterop>(),
                sp.GetRequiredService<IBmageImageCapabilityProvider>());
            loader.MaxSourceBytes = sp.GetRequiredService<BmageOptions>().MaxSourceBytes;
            return loader;
        });
        services.TryAddScoped<BmageImageExporter>();
        services.TryAddScoped(sp => new BmageBrowserTextRasterizer(sp.GetRequiredService<BmageBrowserImageInterop>())
        {
            Limits = sp.GetRequiredService<BmageOptions>().Limits,
        });
        services.TryAddScoped<BmageFontTextRasterizer>();
        return services;
    }

    /// <summary>
    /// The container the editor components take their services from: the application's own when
    /// <see cref="AddBitBmageServices"/> registered them, otherwise one Bmage builds once per browser connection.
    /// </summary>
    /// <remarks>
    /// The second case is what lets a WebAssembly app lazy load Bit.Bmage and Bit.Bmage.Blazor: the assemblies are not
    /// there yet when <c>Program.cs</c> runs, so they cannot be registered. The choice is made once for every service
    /// rather than per service, so a partial registration (for example <see cref="BitBmage.AddBitBmageHeadlessServices"/>
    /// alone) never mixes the application's services with Bmage's defaults; its <see cref="BmageOptions"/> are still
    /// honoured.
    /// </remarks>
    internal static IServiceProvider GetEditorServices(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (services.GetService<BmageImageLoader>() is not null) return services;

        var jsRuntime = services.GetRequiredService<IJSRuntime>();
        var options = services.GetService<BmageOptions>();
        return _ownServices.GetValue(jsRuntime, js =>
        {
            var own = new ServiceCollection().AddSingleton(js);
            if (options is not null) own.AddSingleton(options);
            return own.AddBitBmageServices().BuildServiceProvider();
        });
    }
}
