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
    // AddBitBmotionServices, created by ConfigureBmotionServices. Keyed weakly by the runtime - in a browser tab the
    // process itself - so that every runtime the tests stand in for has its own. Never disposed, and nothing needs it to
    // be: it lives as long as the tab, like the runtime's one scope, and what disposal would add - taking the engine off
    // the frame loop and releasing the module reference held in the browser - has no page left to do it in by then.
    private static readonly ConditionalWeakTable<object, IServiceProvider> _ownServices = new();
    private static readonly Lock _ownServicesLock = new();
    private static readonly object _thisRuntime = new();

    /// <summary>
    /// The WebAssembly runtime the services belong to, or <c>null</c> where they belong to none: Blazor Server, Blazor
    /// Hybrid, a prerendering server. A browser tab runs one, the process itself; settable only so the tests, which run
    /// on .NET, can stand in for several.
    /// </summary>
    internal static Func<IServiceProvider, object?> WebAssemblyRuntimeOf { get; set; } = static _ => OperatingSystem.IsBrowser() ? _thisRuntime : null;

    /// <summary>
    /// Registers all Bit.Bmotion services.
    /// Call this in <c>Program.cs</c> before <c>builder.Build()</c>:
    /// <code>builder.Services.AddBitBmotionServices();</code>
    /// <para>
    /// Fully supported on Blazor WebAssembly; on Blazor Server, compositor-eligible animations
    /// play and the rest degrade to instant sets - see the remarks on <see cref="BitBmotion"/>.
    /// </para>
    /// <para>
    /// A WebAssembly app that lazy loads Bit.Bmotion leaves it out: it calls
    /// <see cref="ConfigureBmotionServices"/> instead, which creates these services once per WebAssembly
    /// runtime, and its own code takes them from <see cref="GetBmotionService{T}"/>. Everywhere else -
    /// Blazor Server, Blazor Hybrid, the server of a Blazor Web App - it is required.
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

        return Register(services, options);
    }

    private static IServiceCollection Register(IServiceCollection services, BitBmotionOptions options)
    {
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
    /// the services <see cref="ConfigureBmotionServices"/> created for the runtime. Inject the service instead wherever
    /// the services are registered; this is for an app that lazy loads Bit.Bmotion on WebAssembly and so cannot
    /// register them:
    /// <code>
    /// @inject IServiceProvider Services
    ///
    /// private BmotionAnimateService Motion => field ??= Services.GetBmotionService&lt;BmotionAnimateService&gt;();
    ///
    /// await Motion.AnimateAsync("#box", Bm.To(x: 100));
    /// </code>
    /// A transient service - <see cref="BmotionScrollTracker"/> - is a new instance on every call, as it is on every
    /// injection, and the caller's to dispose.
    /// </summary>
    /// <typeparam name="T">The service: one of those <see cref="AddBitBmotionServices(IServiceCollection)"/> registers.</typeparam>
    /// <param name="services">The services of the component asking, as injected through <see cref="IServiceProvider"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// The services are not registered, and <see cref="ConfigureBmotionServices"/> has not created them for the
    /// WebAssembly runtime <paramref name="services"/> belong to; or there is no service of type
    /// <typeparamref name="T"/> where they come from.
    /// </exception>
    public static T GetBmotionService<T>(this IServiceProvider services) where T : notnull
    {
        var bmotion = GetBmotionServices(services);

        // Bmotion's own container is never disposed, so it must not hold on to what it hands out: the one transient
        // service is created outside it, the caller's alone to dispose.
        if (typeof(T) == typeof(BmotionScrollTracker) && ReferenceEquals(bmotion, services) is false)
        {
            return (T)(object)new BmotionScrollTracker(bmotion.GetRequiredService<IBmotionInterop>(), bmotion.GetService<ILogger<BmotionScrollTracker>>());
        }

        return bmotion.GetRequiredService<T>();
    }

    /// <summary>
    /// Creates the services of a WebAssembly app that lazy loads Bit.Bmotion, and so cannot register them with
    /// <see cref="AddBitBmotionServices(IServiceCollection, Action{BitBmotionOptions}?)"/>, with the
    /// <see cref="BitBmotionOptions"/> <paramref name="configure"/> sets. Call it in <c>OnInitialized</c> of every page
    /// or layout that holds animated elements, which runs before theirs:
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
    /// The services are created once per runtime, and so are their options, shared by every animated element. Every
    /// later call - and every call where the services are registered, as on the server of a Blazor Web App that
    /// prerenders the same layout - checks that the options it sets are the ones in effect, and throws if not. So a
    /// layout of a Blazor Web App, prerendered on a server that registers the services and run in a browser that lazy
    /// loads them, sets the options given to <c>AddBitBmotionServices</c> on the server: the browser takes an element
    /// that was prerendered at rest over at rest only when it agrees on how the element was prerendered.
    /// </para>
    /// <para>
    /// Until it has run, an animated element without the registration throws as it is created, as it does wherever the
    /// registration is missing: the call is what tells a lazy-loading app apart from one that forgot to register.
    /// </para>
    /// </remarks>
    /// <param name="services">The services of the component asking, as injected through <see cref="IServiceProvider"/>.</param>
    /// <param name="configure">Sets the options, as for <c>AddBitBmotionServices</c>.</param>
    /// <exception cref="InvalidOperationException">
    /// The services are not registered, and <paramref name="services"/> do not belong to a WebAssembly runtime; or the
    /// options are already set, to other values.
    /// </exception>
    public static void ConfigureBmotionServices(this IServiceProvider services, Action<BitBmotionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new BitBmotionOptions();
        configure(options);

        if (IsRegistered(services))
        {
            EnsureSameOptions(services.GetRequiredService<BitBmotionOptions>(), options);
            return;
        }

        var runtime = WebAssemblyRuntimeOf(services) ?? throw NotRegistered();

        lock (_ownServicesLock)
        {
            if (_ownServices.TryGetValue(runtime, out var own))
            {
                EnsureSameOptions(own.GetRequiredService<BitBmotionOptions>(), options);
            }
            else
            {
                _ownServices.Add(runtime, BuildOwnServices(services, options));
            }
        }
    }

    /// <summary>
    /// The container the components take their services from: the application's own when
    /// <see cref="AddBitBmotionServices(IServiceCollection)"/> registered them, otherwise - on WebAssembly only - the
    /// one <see cref="ConfigureBmotionServices"/> built for the runtime.
    /// </summary>
    /// <remarks>
    /// The second case is what lets a WebAssembly app lazy load Bit.Bmotion: the assembly is not there yet when
    /// <c>Program.cs</c> runs, so nothing can be registered. Anywhere else a missing registration is a mistake, reported
    /// where it happens - and so it is on WebAssembly too, unless the app said it lazy loads by configuring the services.
    /// The choice is made once for every service rather than per service, so a partial registration never mixes the
    /// application's services with Bmotion's own; the application's logging, and the state its prerendering server
    /// persisted, still reach the ones built here.
    /// </remarks>
    internal static IServiceProvider GetBmotionServices(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (IsRegistered(services))
        {
            return services;
        }

        if (WebAssemblyRuntimeOf(services) is { } runtime && _ownServices.TryGetValue(runtime, out var own))
        {
            return own;
        }

        throw NotRegistered();
    }

    // The engine stands for the whole registration: AddBitBmotionServices registers it with everything else. The container
    // is asked rather than the engine resolved: the engine is scoped, and the container may be a root that validates scopes.
    private static bool IsRegistered(IServiceProvider services)
        => services.GetService<IServiceProviderIsService>() is { } isService
            ? isService.IsService(typeof(BmotionAnimationEngine))
            : services.GetService<BmotionAnimationEngine>() is not null;

    private static InvalidOperationException NotRegistered() => new(
        "Bit.Bmotion's services are not registered: call builder.Services.AddBitBmotionServices() in Program.cs - in a Blazor Web App, in both the server and the client project. " +
        "Only a WebAssembly app that lazy loads Bit.Bmotion goes without: it calls IServiceProvider.ConfigureBmotionServices(...) before its first animated element is created, and takes its services from IServiceProvider.GetBmotionService<T>().");

    private static void EnsureSameOptions(BitBmotionOptions current, BitBmotionOptions requested)
    {
        var differences = current.DifferencesFrom(requested).ToArray();

        if (differences.Length > 0)
        {
            throw new InvalidOperationException(
                $"Bit.Bmotion's options are already set, and ConfigureBmotionServices sets {string.Join(", ", differences)} differently. " +
                "They are set once - by AddBitBmotionServices where the services are registered, otherwise by the first ConfigureBmotionServices - so call it the same way everywhere, " +
                "with the options AddBitBmotionServices is given on the server of a Blazor Web App. To animate one part of a page differently, set the element's Prerender or wrap it in a <BmotionConfig>.");
        }
    }

    private static ServiceProvider BuildOwnServices(IServiceProvider services, BitBmotionOptions options)
    {
        var own = new ServiceCollection().AddSingleton(services.GetRequiredService<IJSRuntime>());

        // What the application's container holds that Bmotion's services use: its logging, and the state its prerendering
        // server persisted for the elements it served at rest, which is how the elements tell they may take over at rest.
        if (services.GetService<ILoggerFactory>() is { } loggers)
        {
            own.AddSingleton(loggers).AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        }

        if (services.GetService<PersistentComponentState>() is { } state)
        {
            own.AddSingleton(state);
        }

        return Register(own, options).BuildServiceProvider();
    }

    // Roots the public properties + parameterless constructors of every type that is (de)serialized
    // across JS interop so the trimmer preserves them. The [DynamicDependency] attributes take
    // effect because this method is reachable from AddBitBmotionServices (an app entry point),
    // and from ConfigureBmotionServices for an app that never calls it.
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmotionBoundingRect))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmScrollInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmotionXY))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmViewport))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmDragConstraints))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(BmotionStyleRead))]
    private static void PreserveInteropContracts() { }
}
