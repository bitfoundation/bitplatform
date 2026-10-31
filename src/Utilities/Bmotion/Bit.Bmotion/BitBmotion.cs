using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

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
/// motion values - degrade to instant state changes.
/// </para>
/// <para>
/// <b>Prerendering / static SSR:</b> no browser is attached, so the engine doesn't run. By default
/// elements render their <c>Initial</c> state and animate once interactive;
/// <see cref="BitBmotionOptions.Prerender"/> can instead render them at rest
/// (<see cref="BmPrerenderMode.Final"/>) or play the enter animation in pure CSS before the runtime
/// loads (<see cref="BmPrerenderMode.Animate"/>, which needs <c>_content/Bit.Bmotion/bit-bmotion.css</c>
/// linked in the page head).
/// </para>
/// </remarks>
public static class BitBmotion
{
    // One container per WebAssembly runtime for applications that lazy load Bit.Bmotion and so never call
    // AddBitBmotionServices. Keyed weakly by the runtime's IJSRuntime - on WebAssembly there is one for the life of the
    // tab - so that every runtime in a process, as in the tests, has its own. Never disposed, and nothing needs it to be:
    // it lives as long as the tab, like the runtime's one scope, and what disposal would add - taking the engine off the
    // frame loop and releasing the module reference held in the browser - has no page left to do it in by then.
    private static readonly ConditionalWeakTable<IJSRuntime, IServiceProvider> _ownServices = new();

    /// <summary>
    /// Registers all Bit.Bmotion services.
    /// Call this in <c>Program.cs</c> before <c>builder.Build()</c>:
    /// <code>builder.Services.AddBitBmotionServices();</code>
    /// <para>
    /// Fully supported on Blazor WebAssembly; on Blazor Server, compositor-eligible animations
    /// play and the rest degrade to instant sets - see the remarks on <see cref="BitBmotion"/>.
    /// </para>
    /// <para>
    /// A WebAssembly app that lazy loads Bit.Bmotion leaves it out: the components then create these
    /// services themselves, once per WebAssembly runtime, <see cref="GetBmotionService{T}"/> hands the
    /// app's own code the same ones and <see cref="ConfigureBmotionServices"/> sets their options.
    /// Everywhere else - Blazor Server, Blazor Hybrid, the server of a Blazor Web App - it is required.
    /// </para>
    /// </summary>
    public static IServiceCollection AddBitBmotionServices(this IServiceCollection services)
        => services.AddBitBmotionServices(null);

    /// <summary>
    /// Registers all Bit.Bmotion services with library-wide options, e.g. the reduced-motion policy:
    /// <code>builder.Services.AddBitBmotionServices(o =&gt; o.ReducedMotion = BmReducedMotionMode.User);</code>
    /// </summary>
    public static IServiceCollection AddBitBmotionServices(this IServiceCollection services, Action<BitBmotionOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new BitBmotionOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        // Keep the JSON-marshaled interop DTOs trim/AOT-safe. These types cross the JS↔.NET
        // boundary via reflection-based System.Text.Json (InvokeAsync<T>, [JSInvokable] params),
        // so the trimmer can't see their members as used and would otherwise strip them - leading
        // to empty/failed deserialization in published WebAssembly builds.
        PreserveInteropContracts();

        // Slim browser-API interop bridge - one instance per DI scope. Registered behind the
        // IBmotionInterop seam so the engine and components can be unit-tested against a fake.
        services.AddScoped<IBmotionInterop, BmotionInterop>();

        // C# animation engine - drives all animation math in WebAssembly
        services.AddScoped<BmotionAnimationEngine>();

        // Queryable render-mode capability flags (WASM frame loop vs Server compositor-only).
        services.AddScoped(sp => new BmotionCapabilities(sp.GetRequiredService<IBmotionInterop>()));

        // Shared-element (LayoutId) rect registry - one per scope, like the engine
        services.AddScoped<BmotionLayoutRegistry>();

        // Carries "prerendered at rest" from the prerendering pass to the runtime that replaces it.
        services.AddScoped<BmotionPrerenderState>();

        // Higher-level services
        // BmotionScrollTracker is owned and disposed by the consuming component (like
        // Framer Motion's per-component useScroll), so it must be transient.
        // A scoped (app-lifetime in WASM) instance would be disposed by the first
        // component to unmount, leaving its DotNetObjectReference disposed and
        // causing ObjectDisposedException when another component re-observes.
        services.AddTransient<BmotionScrollTracker>();
        services.AddScoped<BmotionAnimateService>();

        // Native View Transitions API wrapper (opt-in helper).
        services.AddScoped<BmotionViewTransition>();

        return services;
    }

    /// <summary>
    /// The Bmotion service of type <typeparamref name="T"/> - <see cref="BmotionAnimateService"/>,
    /// <see cref="BmotionScrollTracker"/>, <see cref="BmotionViewTransition"/>, <see cref="BmotionCapabilities"/> -
    /// that the animated elements of this circuit or WebAssembly runtime work with: the one
    /// <see cref="AddBitBmotionServices(IServiceCollection)"/> registered, otherwise - on WebAssembly only - one of
    /// the services Bmotion creates itself, once per runtime. Inject the service instead wherever the services are
    /// registered; this is for an app that lazy loads Bit.Bmotion on WebAssembly and so cannot register them:
    /// <code>
    /// @inject IServiceProvider Services
    ///
    /// private BmotionAnimateService Motion => field ??= Services.GetBmotionService&lt;BmotionAnimateService&gt;();
    ///
    /// await Motion.AnimateAsync("#box", Bm.To(x: 100));
    /// </code>
    /// A transient service - <see cref="BmotionScrollTracker"/> - is a new instance on every call, as it is on every
    /// injection.
    /// </summary>
    /// <typeparam name="T">The service: one of those <see cref="AddBitBmotionServices(IServiceCollection)"/> registers.</typeparam>
    /// <param name="services">The services of the component asking, as injected through <see cref="IServiceProvider"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// The services are not registered, and <paramref name="services"/> do not belong to a WebAssembly runtime; or
    /// there is no service of type <typeparamref name="T"/> where they come from.
    /// </exception>
    public static T GetBmotionService<T>(this IServiceProvider services) where T : notnull
        => GetBmotionServices(services, warn: false).GetRequiredService<T>();

    /// <summary>
    /// Sets the <see cref="BitBmotionOptions"/> of the services Bmotion creates itself in a WebAssembly app that lazy
    /// loads Bit.Bmotion, and so cannot hand them to <see cref="AddBitBmotionServices(IServiceCollection, Action{BitBmotionOptions}?)"/>.
    /// Call it in <c>OnInitialized</c> of the page or layout that holds the animated elements, which runs before theirs:
    /// <code>
    /// @inject IServiceProvider Services
    ///
    /// protected override void OnInitialized() => Services.ConfigureBmotionServices(o =&gt;
    /// {
    ///     o.ReducedMotion = BmReducedMotionMode.User;
    ///     o.Prerender = BmPrerenderMode.Animate;
    /// });
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Where the services are registered it does nothing - their options are the ones given to
    /// <c>AddBitBmotionServices</c> - so code that has to work both ways, such as a layout of a Blazor Web App that is
    /// prerendered on a server that registers them and runs in a browser that lazy loads them, can always call it. Such
    /// an app sets the same <see cref="BitBmotionOptions.Prerender"/> here as on the server: the browser takes an element
    /// that was prerendered at rest over at rest only when it agrees on how the element was prerendered.
    /// </para>
    /// <para>
    /// The options are shared by every animated element of the runtime, which read them as they mount and as they
    /// animate, so every call applies <paramref name="configure"/> again and the last one wins: configure them in one
    /// place, the same way every time.
    /// </para>
    /// </remarks>
    /// <param name="services">The services of the component asking, as injected through <see cref="IServiceProvider"/>.</param>
    /// <param name="configure">Sets the options, as for <c>AddBitBmotionServices</c>.</param>
    /// <exception cref="InvalidOperationException">
    /// The services are not registered, and <paramref name="services"/> do not belong to a WebAssembly runtime.
    /// </exception>
    public static void ConfigureBmotionServices(this IServiceProvider services, Action<BitBmotionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Registered services were configured where they were registered.
        if (IsRegistered(services)) return;

        configure(GetBmotionServices(services, warn: false).GetRequiredService<BitBmotionOptions>());
    }

    /// <summary>
    /// The container the components take their services from: the application's own when
    /// <see cref="AddBitBmotionServices(IServiceCollection)"/> registered them, otherwise - on WebAssembly only - one
    /// Bmotion builds once per runtime.
    /// </summary>
    /// <remarks>
    /// The second case is what lets a WebAssembly app lazy load Bit.Bmotion: the assembly is not there yet when
    /// <c>Program.cs</c> runs, so nothing can be registered. That only happens on WebAssembly - everywhere else
    /// Bit.Bmotion is loaded with the application - so everywhere else a missing registration is a mistake, reported where
    /// it happens. The choice is made once for every service rather than per service, so a partial registration never
    /// mixes the application's services with Bmotion's own; the application's logging, and the state its prerendering
    /// server persisted, still reach the ones built here.
    /// </remarks>
    internal static IServiceProvider GetBmotionServices(IServiceProvider services) => GetBmotionServices(services, warn: true);

    // warn: whether building the services is worth a warning - when a component asks first, which is all a client project
    // that forgot the registration does. App code asking through GetBmotionService or ConfigureBmotionServices has chosen
    // to go without it.
    private static IServiceProvider GetBmotionServices(IServiceProvider services, bool warn)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (IsRegistered(services)) return services;

        // The JS runtime tells WebAssembly apart: only there is it in-process. Blazor Server's and Blazor Hybrid's reach
        // the browser over a connection, and a prerendering server has none that works.
        if (services.GetService<IJSRuntime>() is not IJSInProcessRuntime jsRuntime) throw new InvalidOperationException(
            "Bit.Bmotion's services are not registered: call builder.Services.AddBitBmotionServices() in Program.cs - in a Blazor Web App, in both the server and the client project. " +
            "Only a WebAssembly app that lazy loads Bit.Bmotion goes without, and takes its services from IServiceProvider.GetBmotionService<T>() instead.");

        return _ownServices.TryGetValue(jsRuntime, out var own) ? own : _ownServices.GetValue(jsRuntime, js => BuildOwnServices(services, js, warn));
    }

    // The engine stands for the whole registration: AddBitBmotionServices registers it with everything else.
    private static bool IsRegistered(IServiceProvider services) => services.GetService<BmotionAnimationEngine>() is not null;

    private static ServiceProvider BuildOwnServices(IServiceProvider services, IJSRuntime js, bool warn)
    {
        var own = new ServiceCollection().AddSingleton(js);
        var loggers = services.GetService<ILoggerFactory>();

        // Once per runtime, so once per app: what tells the client project of a Blazor Web App that forgot the
        // registration why the options it meant to register are not the ones its elements run on.
        if (warn) loggers?.CreateLogger(typeof(BitBmotion).FullName!).LogWarning(
            "Bit.Bmotion's services are not registered, so the animated elements create their own, on the default BitBmotionOptions. " +
            "Call builder.Services.AddBitBmotionServices() in Program.cs - in a Blazor Web App, in both the server and the client project. " +
            "An app that lazy loads Bit.Bmotion on WebAssembly cannot, and sets the options with IServiceProvider.ConfigureBmotionServices(...) before its elements mount, which also keeps this warning away.");

        // What the application's container holds that Bmotion's services use: its logging, and the state its prerendering
        // server persisted for the elements it served at rest, which is how the elements tell they may take over at rest.
        if (loggers is not null) own.AddSingleton(loggers).AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        if (services.GetService<PersistentComponentState>() is { } state) own.AddSingleton(state);

        return own.AddBitBmotionServices().BuildServiceProvider();
    }

    // Roots the public properties + parameterless constructors of every type that is (de)serialized
    // across JS interop so the trimmer preserves them. The [DynamicDependency] attributes take
    // effect because this method is reachable from AddBitBmotionServices (an app entry point),
    // which GetBmotionServices also calls for an app that never does.
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmotionBoundingRect))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmScrollInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmotionXY))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmViewport))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmDragConstraints))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmotionStyleRead))]
    private static void PreserveInteropContracts() { }
}
