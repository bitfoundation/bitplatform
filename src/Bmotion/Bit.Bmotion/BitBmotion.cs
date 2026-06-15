using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bmotion;

/// <summary>
/// Extension methods to register Bit.Bmotion services in the DI container.
/// </summary>
public static class BitBmotion
{
    /// <summary>
    /// Registers all Bit.Bmotion services.
    /// Call this in <c>Program.cs</c> before <c>builder.Build()</c>:
    /// <code>builder.Services.AddBitBmotionServices();</code>
    /// </summary>
    public static IServiceCollection AddBitBmotionServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Slim browser-API interop bridge - one instance per DI scope
        services.AddScoped<BmotionInterop>();

        // C# animation engine - drives all animation math in WebAssembly
        services.AddScoped<BmotionAnimationEngine>();

        // Higher-level services
        // BmotionScrollTracker is owned and disposed by the consuming component (like
        // Framer Motion's per-component useScroll), so it must be transient.
        // A scoped (app-lifetime in WASM) instance would be disposed by the first
        // component to unmount, leaving its DotNetObjectReference disposed and
        // causing ObjectDisposedException when another component re-observes.
        services.AddTransient<BmotionScrollTracker>();
        services.AddTransient<BmotionAnimationController>();
        services.AddScoped<BmotionAnimateService>();

        return services;
    }
}
