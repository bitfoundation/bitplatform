using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bmotion;

/// <summary>
/// Extension methods to register Bit.Bmotion services in the DI container.
/// </summary>
/// <remarks>
/// <para>
/// <b>Platform support:</b> on <b>Blazor WebAssembly</b> everything works: compositor-eligible
/// animations (tweens and zero-velocity springs on transform/opacity) offload to the browser's
/// Web Animations API, and the C# rAF engine drives the rest over synchronous JS interop.
/// On <b>Blazor Server</b> the compositor path still works (it needs only async interop), so
/// enter/exit/hover/tap/variant animations on transform + opacity play normally; features that
/// require the per-frame loop - inertia, color/dimension interpolation, keyframe arrays, drag,
/// motion values - degrade to instant state changes. The library is inert during server-side
/// prerendering (animations start once the circuit/runtime is interactive).
/// </para>
/// </remarks>
public static class BitBmotion
{
    /// <summary>
    /// Registers all Bit.Bmotion services.
    /// Call this in <c>Program.cs</c> before <c>builder.Build()</c>:
    /// <code>builder.Services.AddBitBmotionServices();</code>
    /// <para>
    /// Fully supported on Blazor WebAssembly; on Blazor Server, compositor-eligible animations
    /// play and the rest degrade to instant sets - see the remarks on <see cref="BitBmotion"/>.
    /// </para>
    /// </summary>
    public static IServiceCollection AddBitBmotionServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Keep the JSON-marshaled interop DTOs trim/AOT-safe. These types cross the JS↔.NET
        // boundary via reflection-based System.Text.Json (InvokeAsync<T>, [JSInvokable] params),
        // so the trimmer can't see their members as used and would otherwise strip them - leading
        // to empty/failed deserialization in published WebAssembly builds.
        PreserveInteropContracts();

        // Slim browser-API interop bridge - one instance per DI scope
        services.AddScoped<BmotionInterop>();

        // C# animation engine - drives all animation math in WebAssembly
        services.AddScoped<BmotionAnimationEngine>();

        // Shared-element (LayoutId) rect registry - one per scope, like the engine
        services.AddScoped<BmotionLayoutRegistry>();

        // Higher-level services
        // BmotionScrollTracker is owned and disposed by the consuming component (like
        // Framer Motion's per-component useScroll), so it must be transient.
        // A scoped (app-lifetime in WASM) instance would be disposed by the first
        // component to unmount, leaving its DotNetObjectReference disposed and
        // causing ObjectDisposedException when another component re-observes.
        services.AddTransient<BmotionScrollTracker>();
        services.AddScoped<BmotionAnimateService>();

        return services;
    }

    // Roots the public properties + parameterless constructors of every type that is (de)serialized
    // across JS interop so the trimmer preserves them. The [DynamicDependency] attributes take
    // effect because this method is reachable from AddBitBmotionServices (an app entry point).
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmotionBoundingRect))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmScrollInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmotionXY))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmViewport))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmDragConstraints))]
    private static void PreserveInteropContracts() { }
}
