using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Bit.Bonboarding;

/// <summary>
/// The services the components work with: the ones <see cref="BitBonboarding.AddBitBonboardingServices"/> registered,
/// otherwise ones Bonboarding creates itself, once per browser connection.
/// </summary>
/// <remarks>
/// The second case is what lets a WebAssembly app lazy load Bit.Bonboarding: the assembly is not there yet when
/// <c>Program.cs</c> runs, so nothing can be registered. The choice is made once for every service rather than per
/// service, so a partial registration never mixes the application's services with Bonboarding's defaults; options
/// configured for <see cref="BonboardingOptions"/>, registered observers, the clock and the application's logging are
/// still honoured. What a tour's hooks, a checklist's handlers and <see cref="BonboardingOptions.StorageScope"/> reach
/// through their context still comes from the application's container.
/// </remarks>
internal sealed class BonboardingRuntime
{
    // Keyed weakly by the connection's IJSRuntime, so a closed Blazor Server circuit takes its services with it. Plain
    // objects rather than a service provider, and never disposed: they live as long as the connection, like a scope,
    // and what disposal would add - detaching from the browser and releasing the module held there - has no page left
    // to do it in by then.
    private static readonly ConditionalWeakTable<IJSRuntime, BonboardingRuntime> _own = new();

    // The logger factories already told that the services are not registered: one warning per application rather than
    // one per circuit.
    private static readonly ConditionalWeakTable<ILoggerFactory, object?> _warned = new();

    private BonboardingRuntime(
        IBonboardingService service,
        BonboardingGuidanceRegistry guidance,
        BonboardingHostRegistry hosts,
        IBonboardingInterop interop,
        IBonboardingLocalizer localizer,
        TimeProvider time)
    {
        Service = service;
        Guidance = guidance;
        Hosts = hosts;
        Interop = interop;
        Localizer = localizer;
        Time = time;
    }

    public IBonboardingService Service { get; }

    public BonboardingGuidanceRegistry Guidance { get; }

    public BonboardingHostRegistry Hosts { get; }

    public IBonboardingInterop Interop { get; }

    public IBonboardingLocalizer Localizer { get; }

    public TimeProvider Time { get; }

    /// <summary>The services of the circuit or WebAssembly runtime that <paramref name="services"/> belongs to.</summary>
    /// <param name="services">The services of the component asking, as injected through <see cref="IServiceProvider"/>.</param>
    public static BonboardingRuntime Of(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Only AddBitBonboardingServices registers the host registry, so it stands for the whole registration.
        if (services.GetService<BonboardingHostRegistry>() is { } hosts)
        {
            return new(services.GetRequiredService<IBonboardingService>(),
                       services.GetRequiredService<BonboardingGuidanceRegistry>(),
                       hosts,
                       services.GetRequiredService<IBonboardingInterop>(),
                       services.GetRequiredService<IBonboardingLocalizer>(),
                       services.GetRequiredService<TimeProvider>());
        }

        var js = services.GetService<IJSRuntime>() ?? throw new InvalidOperationException(
            "Bit.Bonboarding's services are not registered: call builder.Services.AddBitBonboardingServices() in Program.cs. " +
            "Without them Bonboarding creates its own for each browser connection, which needs the IJSRuntime of a Blazor app, and this container has none.");

        return _own.TryGetValue(js, out var own) ? own : Create(services, js);
    }

    private static BonboardingRuntime Create(IServiceProvider services, IJSRuntime js)
    {
        var loggers = services.GetService<ILoggerFactory>();

        if (loggers is not null && _warned.TryAdd(loggers, null))
        {
            loggers.CreateLogger(typeof(BitBonboarding).FullName!).LogWarning(
                "Bit.Bonboarding's services are not registered, so the components create their own: they run on the default BonboardingOptions, and progress goes to the browser's localStorage. " +
                "Call builder.Services.AddBitBonboardingServices() in Program.cs - in a Blazor Web App, in both the server and the client project. " +
                "An app that lazy loads Bit.Bonboarding on WebAssembly cannot, and can ignore this warning.");
        }

        loggers ??= NullLoggerFactory.Instance;

        // AddBitBonboardingServices roots the interop payloads for the trimmer; an app that never calls it needs them too.
        BitBonboarding.PreserveInteropContracts();

        var options = services.GetService<IOptions<BonboardingOptions>>() ?? Microsoft.Extensions.Options.Options.Create(new BonboardingOptions());
        var time = services.GetService<TimeProvider>() ?? TimeProvider.System;
        var interop = new BonboardingInterop(js, options, new Logger<BonboardingInterop>(loggers));
        var store = new BonboardingBrowserStore(interop, options, new Logger<BonboardingBrowserStore>(loggers), services);
        var localizer = new BonboardingDefaultLocalizer(options);
        var guidance = new BonboardingGuidanceRegistry();

        // The application's container, not one of Bonboarding's: it is what a tour's hooks reach through their context.
        var service = new BonboardingService(options, interop, store, localizer, services, loggers,
                                             services.GetServices<IBonboardingObserver>(), guidance,
                                             services.GetService<NavigationManager>(), time);

        // Two components of one connection that get here together end up with the same services: the first one added.
        return _own.GetOrAdd(js, new BonboardingRuntime(service, guidance, new BonboardingHostRegistry(), interop, localizer, time));
    }
}
