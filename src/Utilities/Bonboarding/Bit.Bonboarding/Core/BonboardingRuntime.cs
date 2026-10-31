using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Bit.Bonboarding;

/// <summary>
/// The services the components work with: the ones the application registered, otherwise - in an app that lazy loads
/// Bit.Bonboarding on WebAssembly, and turned <see cref="LazyLoadSwitch"/> on to say so - ones Bonboarding creates itself,
/// once per browser connection.
/// </summary>
/// <remarks>
/// A registered <see cref="IBonboardingService"/> decides it, for the components and for
/// <see cref="BitBonboarding.GetBonboardingService"/> alike, so app code and the components never end up with two
/// different services. Where it is registered, every service comes from the application's container - a test double of
/// the service, registered alone, serves the components that need nothing else. Where it is not, the application's
/// options, store, localizer, observers and clock are still honoured, and anything it did not register is Bonboarding's
/// default, as <see cref="BitBonboarding.AddBitBonboardingServices"/> would have registered it.
/// </remarks>
internal sealed class BonboardingRuntime
{
    /// <summary>
    /// The runtime switch an app that lazy loads Bit.Bonboarding turns on, since it cannot call
    /// <see cref="BitBonboarding.AddBitBonboardingServices"/>: in its .csproj,
    /// <c>&lt;RuntimeHostConfigurationOption Include="Bit.Bonboarding.LazyLoad" Value="true" /&gt;</c>. Without it a
    /// missing registration fails, as it should in every other app.
    /// </summary>
    public const string LazyLoadSwitch = "Bit.Bonboarding.LazyLoad";

    private const string NotRegistered =
        "Bit.Bonboarding's services are not registered: call builder.Services.AddBitBonboardingServices() in Program.cs - in a Blazor Web App, in both the server and the client project. " +
        "A WebAssembly app that lazy loads Bit.Bonboarding, and so cannot register them, turns on the '" + LazyLoadSwitch + "' runtime switch instead: " +
        "<RuntimeHostConfigurationOption Include=\"" + LazyLoadSwitch + "\" Value=\"true\" /> in its .csproj.";

    // Where the services are registered: one runtime per scope, which resolves each service the first time a component
    // of the scope needs it. Keyed weakly, so a closed circuit takes its runtime with it.
    private static readonly ConditionalWeakTable<IServiceProvider, BonboardingRuntime> _registered = new();

    // Where they are not: one runtime per browser connection, keyed weakly by its IJSRuntime for the same reason. Plain
    // objects rather than a service provider, and never disposed: they live as long as the connection, like a scope,
    // and what disposal would add - detaching from the browser and releasing the module held there - has no page left
    // to do it in by then.
    private static readonly ConditionalWeakTable<IJSRuntime, BonboardingRuntime> _own = new();

    private readonly IServiceProvider _services;

    private IBonboardingService? _service;
    private BonboardingGuidanceRegistry? _guidance;
    private BonboardingHostRegistry? _hosts;
    private IBonboardingInterop? _interop;
    private IBonboardingLocalizer? _localizer;
    private TimeProvider? _time;

    private BonboardingRuntime(IServiceProvider services)
    {
        _services = services;
    }

    private static bool IsLazyLoaded => AppContext.TryGetSwitch(LazyLoadSwitch, out var on) && on;

    public IBonboardingService Service => _service ??= _services.GetRequiredService<IBonboardingService>();

    public BonboardingGuidanceRegistry Guidance => _guidance ??= _services.GetRequiredService<BonboardingGuidanceRegistry>();

    public BonboardingHostRegistry Hosts => _hosts ??= _services.GetRequiredService<BonboardingHostRegistry>();

    public IBonboardingInterop Interop => _interop ??= _services.GetRequiredService<IBonboardingInterop>();

    public IBonboardingLocalizer Localizer => _localizer ??= _services.GetRequiredService<IBonboardingLocalizer>();

    public TimeProvider Time => _time ??= _services.GetRequiredService<TimeProvider>();

    /// <summary>The services of the circuit or WebAssembly runtime that <paramref name="services"/> belongs to.</summary>
    /// <param name="services">The services of the code asking - a component's, or a scope app code made.</param>
    public static BonboardingRuntime Of(IServiceProvider services) => Of(services, component: false);

    private static BonboardingRuntime Of(IServiceProvider services, bool component)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (_registered.TryGetValue(services, out var registered)) return registered;

        if (services.GetService<IBonboardingService>() is { } service)
        {
            return _registered.GetOrAdd(services, new BonboardingRuntime(services) { _service = service });
        }

        if (IsLazyLoaded is false) throw new InvalidOperationException(NotRegistered);

        var js = services.GetService<IJSRuntime>() ?? throw new InvalidOperationException(
            $"Bit.Bonboarding's services are not registered, and the '{LazyLoadSwitch}' switch has Bonboarding create its own for each browser connection - " +
            "which needs the IJSRuntime of a Blazor app, and this container has none. Call builder.Services.AddBitBonboardingServices() in Program.cs.");

        // Two components of one connection that get here together end up with the same services: the first one added.
        var own = _own.TryGetValue(js, out var existing) ? existing : _own.GetOrAdd(js, Create(new ComponentServices(services), js));

        if (component && own._services is ComponentServices app) app.UseComponents(services);

        return own;
    }

    private static BonboardingRuntime Create(ComponentServices app, IJSRuntime js)
    {
        // What the application registered wins, as it does over AddBitBonboardingServices' TryAdds; the rest is built
        // the way the container would build it, so a dependency added to one of these types reaches this path as well.
        var options = app.GetService<IOptions<BonboardingOptions>>() ?? Microsoft.Extensions.Options.Options.Create(new BonboardingOptions());
        var loggers = app.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
        var time = app.GetService<TimeProvider>() ?? TimeProvider.System;
        var guidance = new BonboardingGuidanceRegistry();

        var interop = app.GetService<IBonboardingInterop>()
                      ?? ActivatorUtilities.CreateInstance<BonboardingInterop>(app, js, options, new Logger<BonboardingInterop>(loggers));
        var store = app.GetService<IBonboardingStore>()
                    ?? ActivatorUtilities.CreateInstance<BonboardingBrowserStore>(app, interop, options, new Logger<BonboardingBrowserStore>(loggers), app);
        var localizer = app.GetService<IBonboardingLocalizer>()
                        ?? ActivatorUtilities.CreateInstance<BonboardingDefaultLocalizer>(app, options);

        // The application's container, not one of Bonboarding's: it is what a tour's hooks reach through their context.
        var service = ActivatorUtilities.CreateInstance<BonboardingService>(app, options, interop, store, localizer, app, loggers, guidance, time);

        return new BonboardingRuntime(app)
        {
            _service = service,
            _guidance = guidance,
            _hosts = new BonboardingHostRegistry(),
            _interop = interop,
            _localizer = localizer,
            _time = time,
        };
    }

    /// <summary>Resolves now what a component is about to use, so it either has all of it or none.</summary>
    private BonboardingRuntime Resolve(Needs needs)
    {
        if (needs.HasFlag(Needs.Service)) _ = Service;
        if (needs.HasFlag(Needs.Guidance)) _ = Guidance;
        if (needs.HasFlag(Needs.Hosts)) _ = Hosts;
        if (needs.HasFlag(Needs.Interop)) _ = Interop;
        if (needs.HasFlag(Needs.Localizer)) _ = Localizer;
        if (needs.HasFlag(Needs.Time)) _ = Time;

        return this;
    }

    /// <summary>The services a component uses.</summary>
    [Flags]
    public enum Needs
    {
        Service = 1,
        Guidance = 2,
        Hosts = 4,
        Interop = 8,
        Localizer = 16,
        Time = 32,
    }

    /// <summary>
    /// A component's hold on its runtime: resolved the first time the component needs it, and from then on the same
    /// runtime - or the same failure, rethrown rather than tried again. Once the component is disposed it hands out only
    /// what it already resolved, so nothing is resolved, or created, for a component that is gone.
    /// </summary>
    /// <param name="component">The component's type, which a use after disposal names.</param>
    /// <param name="needs">What the component uses: resolved together, so a failure leaves it holding nothing to undo.</param>
    public sealed class Handle(Type component, Needs needs)
    {
        private BonboardingRuntime? _runtime;
        private ExceptionDispatchInfo? _failure;
        private bool _closed;

        public BonboardingRuntime Get(IServiceProvider services)
        {
            if (_runtime is not null) return _runtime;

            _failure?.Throw();
            ObjectDisposedException.ThrowIf(_closed, component);

            try
            {
                return _runtime = Of(services, component: true).Resolve(needs);
            }
            catch (Exception ex)
            {
                _failure = ExceptionDispatchInfo.Capture(ex);
                throw;
            }
        }

        /// <summary>Called when the component is disposed: what it resolved, which is what it has to undo - null for nothing.</summary>
        public BonboardingRuntime? Close()
        {
            _closed = true;
            return _runtime;
        }
    }

    /// <summary>
    /// The application's services as the services Bonboarding creates itself reach them - a tour's hooks,
    /// <see cref="BonboardingOptions.StorageScope"/>. They are created by whoever asks first, which may be app code
    /// holding a scope of its own, such as an <c>OwningComponentBase</c>'s <c>ScopedServices</c>; such a scope is
    /// disposed long before the connection closes. So the first component to ask takes its place: the container
    /// the components are given lives as long as the connection.
    /// </summary>
    private sealed class ComponentServices(IServiceProvider services) : IServiceProvider, ISupportRequiredService, IKeyedServiceProvider
    {
        private IServiceProvider _services = services;
        private bool _fromComponent;

        public void UseComponents(IServiceProvider services)
        {
            if (_fromComponent) return;

            _services = services;
            _fromComponent = true;
        }

        public object? GetService(Type serviceType) => _services.GetService(serviceType);

        public object GetRequiredService(Type serviceType) => _services.GetRequiredService(serviceType);

        public object? GetKeyedService(Type serviceType, object? serviceKey) => Keyed.GetKeyedService(serviceType, serviceKey);

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey) => Keyed.GetRequiredKeyedService(serviceType, serviceKey);

        private IKeyedServiceProvider Keyed => _services as IKeyedServiceProvider
            ?? throw new InvalidOperationException("This service provider doesn't support keyed services.");
    }
}
