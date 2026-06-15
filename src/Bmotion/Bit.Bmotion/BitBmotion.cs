using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bmotion;

/// <summary>
/// Extension methods to register Bit.Bmotion services in the DI container.
/// </summary>
/// <remarks>
/// <para>
/// <b>Platform support:</b> Bit.Bmotion runs its animation loop over <b>synchronous</b> JS interop
/// and is therefore supported on <b>Blazor WebAssembly only</b>. It does <b>not</b> work on Blazor
/// Server, and it is inert during server-side prerendering (animations start once the WASM runtime
/// is interactive). Attempting to start the animation loop on a non-WebAssembly host throws
/// <see cref="PlatformNotSupportedException"/>.
/// </para>
/// </remarks>
public static class BitBmotion
{
    /// <summary>
    /// Registers all Bit.Bmotion services.
    /// Call this in <c>Program.cs</c> before <c>builder.Build()</c>:
    /// <code>builder.Services.AddBitBmotionServices();</code>
    /// <para>
    /// <b>Blazor WebAssembly only</b> - see the remarks on <see cref="BitBmotion"/> for details.
    /// </para>
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
