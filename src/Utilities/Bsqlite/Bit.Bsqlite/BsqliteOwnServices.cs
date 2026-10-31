using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Bit.Bsqlite.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Bsqlite;

/// <summary>
/// The services <see cref="BitBsqlite.GetBitBsqliteServices"/> builds for a WebAssembly application that did not
/// register Bsqlite's: what its <c>configure</c> registered, in a container of their own, in front of the application's.
/// </summary>
/// <remarks>
/// <para>
/// Only WebAssembly gets them. It is the one host Bit.Bsqlite can be lazy loaded in - everywhere else it is loaded with
/// the application, so a missing registration is a mistake - and the one where nothing has to dispose them: built once
/// per WebAssembly runtime, they live as long as the tab, as the application's own scope does. A Blazor Hybrid web view
/// or a server circuit ends long before its process does, and would leave its databases open.
/// </para>
/// <para>
/// Everything the container builds is built through a <see cref="Provider"/> rather than through the container itself,
/// so the constructors and factories <c>configure</c> registered, the <c>BeforeFirstOpen</c> steps and the contexts'
/// constructors reach the application's services - its <c>IJSRuntime</c>, its logging, its <c>LazyAssemblyLoader</c> -
/// as they would if the same calls had been made in <c>Program.cs</c>. A service both have is the container's own.
/// </para>
/// <para>
/// Each scope of the container has a provider of its own, in front of a scope of the application's created with it and
/// disposed with it: the root's is the one handed out, and lives as long as the container. So nothing the container
/// keeps depends on a scope a caller may dispose, nothing resolves the application's scoped services from its root,
/// and a scope created through a provider is a scope of both.
/// </para>
/// </remarks>
internal sealed class BsqliteOwnServices
{
    // Keyed weakly by the WebAssembly runtime's IJSRuntime: there is one for the life of the tab, and tests run several
    // in one process.
    private static readonly ConditionalWeakTable<IJSRuntime, BsqliteOwnServices> _built = new();

    // Building under a lock, rather than keeping whichever of two racing callers finishes first, is what runs configure
    // exactly once.
    private static readonly Lock _gate = new();

    // The configures already checked against the container of an application that registered Bsqlite's services.
    private static readonly ConditionalWeakTable<IServiceProviderIsService, ConcurrentDictionary<MethodInfo, bool>> _checked = new();

    private readonly MethodInfo _configure;
    private readonly IServiceScopeFactory _applicationScopes;
    private readonly ServiceProvider _container;
    private readonly ScopeServices _root;

    // The open generics configure registered, which a provider closes itself, and the closed services it registered,
    // which win over them as they would in any container - each with its key, null for a service without one.
    private readonly Dictionary<(Type Type, object? Key), ServiceDescriptor> _openGenerics = [];
    private readonly HashSet<(Type Type, object? Key)> _closed = [];
    private readonly ConcurrentDictionary<(Type Type, ServiceDescriptor Open), Closing?> _closings = new();

    // What an IEnumerable of a closing holds, in the order configure registered it: the open generics without a key,
    // which the container is not given - it would answer such an IEnumerable with closings built from itself alone - and
    // the closed generic services it builds.
    private readonly List<ServiceDescriptor> _enumerated = [];

    private BsqliteOwnServices(IServiceProvider application, MethodInfo configure, ServiceCollection registered)
    {
        _configure = configure;
        _applicationScopes = application.GetRequiredService<IServiceScopeFactory>();

        IServiceCollection own = new ServiceCollection();
        foreach (var service in registered)
        {
            var identity = (service.ServiceType, service.IsKeyedService ? service.ServiceKey : null);
            if (!service.ServiceType.IsGenericTypeDefinition)
            {
                _closed.Add(identity);
            }
            else if (IsClosable(service))
            {
                _openGenerics[identity] = service;
                if (!service.IsKeyedService)
                {
                    _enumerated.Add(service);
                    continue;
                }
            }

            if (!service.IsKeyedService && service.ServiceType.IsConstructedGenericType)
            {
                _enumerated.Add(service);
            }

            own.Add(InFront(service));
        }

        own.AddScoped(sp => new ScopeServices(this, sp));
        _container = own.BuildServiceProvider();
        _root = _container.GetRequiredService<ScopeServices>();
    }

    /// <summary>What <see cref="BitBsqlite.GetBitBsqliteServices"/> hands back.</summary>
    public static IServiceProvider For(IServiceProvider services, Action<IServiceCollection> configure)
    {
        if (services is Provider provider)
        {
            provider.Connection.EnsureBuiltBy(configure);
            return provider;
        }

        if (IsRegistered(services))
        {
            Check(services, configure);
            return services;
        }

        if (services.GetService<IJSRuntime>() is not IJSInProcessRuntime js)
        {
            throw new InvalidOperationException(
                "Bit.Bsqlite's services are not registered: call builder.Services.AddBitBsqliteServices(...) - and "
                + "AddBitBsqliteDbContext<TContext>() for each context - in Program.cs, or in MauiProgram.cs; calling the method "
                + "given to GetBitBsqliteServices there registers the same. Only a WebAssembly app that lazy loads Bit.Bsqlite "
                + "goes without, and GetBitBsqliteServices builds them in the browser. A page a Blazor Web App prerenders asks "
                + "for them only once RendererInfo.IsInteractive is true, as it opens the database then, unless the server "
                + "project registers them too.");
        }

        lock (_gate)
        {
            if (_built.TryGetValue(js, out var built))
            {
                built.EnsureBuiltBy(configure);
                return built._root.Provider;
            }

            built = Build(services, configure);
            _built.Add(js, built);
            return built._root.Provider;
        }
    }

    /// <summary>
    /// Disposes the services built for <paramref name="js"/>'s runtime, closing their databases, and forgets them, so
    /// the next call builds them anew. A WebAssembly app never needs to - its services go with the tab - but tests run
    /// many runtimes in one process.
    /// </summary>
    internal static async ValueTask ReleaseAsync(IJSRuntime js)
    {
        BsqliteOwnServices? built;
        lock (_gate)
        {
            if (!_built.TryGetValue(js, out built))
            {
                return;
            }

            _built.Remove(js);
        }

        await built._container.DisposeAsync().ConfigureAwait(false);
    }

    // Asked rather than resolved: resolving the session factory would build its whole graph - the resolver, every
    // storage provider, the binding store - to answer a yes or no.
    private static bool IsRegistered(IServiceProvider services)
        => services.GetService<IServiceProviderIsService>() is { } isService
            ? isService.IsService(typeof(IBsqliteSessionFactory))
            : services.GetService<IBsqliteSessionFactory>() is not null;

    /// <summary>
    /// Makes sure an application that registered Bsqlite's services, and so gets its own container back, registered
    /// what <paramref name="configure"/> does as well - a context, a database, a service of its own - rather than
    /// failing later on whichever of them a page asks for. Once per container and method.
    /// </summary>
    private static void Check(IServiceProvider services, Action<IServiceCollection> configure)
    {
        if (services.GetService<IServiceProviderIsService>() is not { } isService)
        {
            return;
        }

        var checkedConfigures = _checked.GetOrCreateValue(isService);
        if (checkedConfigures.ContainsKey(configure.Method))
        {
            return;
        }

        var registered = new ServiceCollection();
        configure(registered);

        var isKeyedService = services.GetService<IServiceProviderIsKeyedService>();
        var missing = registered
            .Where(service => service.IsKeyedService
                ? !service.ServiceType.IsGenericTypeDefinition && isKeyedService?.IsKeyedService(service.ServiceType, service.ServiceKey) is false
                // An open generic is asked for closed over object, which a container answers from the definition.
                : (service.ServiceType.IsGenericTypeDefinition ? ClosedOverObject(service.ServiceType) : service.ServiceType) is { } type
                    && !isService.IsService(type))
            .Select(Describe)
            .Distinct()
            .ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "The application registers Bit.Bsqlite's services, so GetBitBsqliteServices hands back its container and builds "
                + $"nothing from configure - but that container lacks what configure registers: {string.Join(", ", missing)}. "
                + "Register them where AddBitBsqliteServices is called - Program.cs, or MauiProgram.cs - by calling the method "
                + "given to GetBitBsqliteServices there instead.");
        }

        checkedConfigures.TryAdd(configure.Method, true);
    }

    private static BsqliteOwnServices Build(IServiceProvider application, Action<IServiceCollection> configure)
    {
        var registered = new ServiceCollection();
        configure(registered);

        if (!registered.Any(service => service.ServiceType == typeof(IBsqliteSessionFactory)))
        {
            throw new InvalidOperationException(
                "The configure GetBitBsqliteServices was given registers none of Bsqlite's services: call "
                + "services.AddBitBsqliteServices(sqlite => ...) in it - and AddBitBsqliteDbContext<TContext>() for each "
                + "context - as Program.cs would.");
        }

        // An open generic of configure's would answer for every closing of it, hiding the application's: AddLogging,
        // AddOptions, and whatever calls them, would leave Bsqlite's logging and the application's options to an empty
        // second set.
        var isService = application.GetService<IServiceProviderIsService>();
        var hiding = registered
            .Where(service => !service.IsKeyedService && service.ServiceType.IsGenericTypeDefinition)
            .Select(service => service.ServiceType)
            .Distinct()
            .Where(type => ClosedOverObject(type) is { } closed && isService?.IsService(closed) is true)
            .Select(Name)
            .ToArray();
        if (hiding.Length > 0)
        {
            throw new InvalidOperationException(
                $"The configure GetBitBsqliteServices was given registers {string.Join(", ", hiding)} - through AddLogging, "
                + "AddOptions, AddMemoryCache or AddHttpClient, say - which the application's container has as well. In the "
                + "container built from configure they would answer for the application's, losing its logging and its options: "
                + "make those calls in Program.cs - and do not lazy load an assembly they need - or register in configure the "
                + "closed services they build, such as services.AddSingleton<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions())).");
        }

        return new(application, configure.Method, registered);
    }

    // Only to ask a container whether it has the open generic - which it answers from the definition - never to build it.
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "The closed type is only looked up, never instantiated.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Closed over object, a reference type, whose code is shared; and never instantiated.")]
    private static Type? ClosedOverObject(Type openGeneric)
    {
        try
        {
            return openGeneric.MakeGenericType([.. openGeneric.GetGenericArguments().Select(_ => typeof(object))]);
        }
        catch (ArgumentException)
        {
            // A constraint object does not meet, which none of logging's, options' or caching's has.
            return null;
        }
    }

    // The services of one browser connection are built once, so a second configure's registrations would be missing
    // from them with nothing to tell why.
    private void EnsureBuiltBy(Action<IServiceCollection> configure)
    {
        if (!configure.Method.Equals(_configure))
        {
            throw new InvalidOperationException(
                $"GetBitBsqliteServices built the services from {Name(_configure)}, and is now given {Name(configure.Method)}: "
                + "it builds them once, from the first, so the registrations of any other would be missing. Keep every "
                + "registration - each database, each context, each service of yours - in one method, and pass that method "
                + "to every call.");
        }
    }

    /// <summary>
    /// <paramref name="service"/>, built through the provider of the scope it is built in: a factory is handed that
    /// provider instead of the container, and an implementation type is constructed from it. An instance needs nothing,
    /// and an open generic, which only a container can close, is left to it - a provider closes the ones asked of it
    /// itself, see <see cref="Provider.GetService"/>.
    /// </summary>
    private static ServiceDescriptor InFront(ServiceDescriptor service)
    {
        if (service.IsKeyedService)
        {
            if (service.KeyedImplementationFactory is { } keyedFactory)
            {
                return new(service.ServiceType, service.ServiceKey, (sp, key) => keyedFactory(Provider.Of(sp), key), service.Lifetime);
            }

            return service.KeyedImplementationType is { IsGenericTypeDefinition: false } keyedType
                ? new(service.ServiceType, service.ServiceKey, new Activation(keyedType).Create, service.Lifetime)
                : service;
        }

        if (service.ImplementationFactory is { } factory)
        {
            return new(service.ServiceType, sp => factory(Provider.Of(sp)), service.Lifetime);
        }

        return service.ImplementationType is { IsGenericTypeDefinition: false } type
            ? new(service.ServiceType, new Activation(type).Create, service.Lifetime)
            : service;
    }

    /// <summary>
    /// How to build <paramref name="serviceType"/>, asked for with <paramref name="key"/>, when it closes an open
    /// generic <c>configure</c> registered without registering the closed type itself - with that key, or with
    /// <see cref="KeyedService.AnyKey"/>; otherwise - or when the implementation's constraints rule the closing out,
    /// which a container passes over too - null.
    /// </summary>
    private Closing? ClosingOf(Type serviceType, object? key)
    {
        if (_openGenerics.Count == 0 || !serviceType.IsConstructedGenericType || _closed.Contains((serviceType, key))
            || key is not null && _closed.Contains((serviceType, KeyedService.AnyKey)))
        {
            return null;
        }

        var definition = serviceType.GetGenericTypeDefinition();
        return _openGenerics.TryGetValue((definition, key), out var open)
            || key is not null && _openGenerics.TryGetValue((definition, KeyedService.AnyKey), out open)
            ? ClosingBy(serviceType, open)
            : null;
    }

    private Closing? ClosingBy(Type serviceType, ServiceDescriptor open)
        => _closings.GetOrAdd((serviceType, open), static closing => Close(closing.Open, closing.Type));

    // What a container would close: an open generic implementation, with as many type parameters, that it can construct.
    // The container is given any other, and refuses it as it is built.
    private static bool IsClosable(ServiceDescriptor open)
        => (open.IsKeyedService ? open.KeyedImplementationType : open.ImplementationType) is { IsGenericTypeDefinition: true, IsAbstract: false } type
            && type.GetGenericArguments().Length == open.ServiceType.GetGenericArguments().Length;

    // The implementation type kept its public constructors when it was registered - ServiceDescriptor asks for them -
    // and closing it keeps them; and Blazor WebAssembly, the only host this runs in, keeps the interpreter for code it
    // cannot have compiled ahead of time.
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "The open generic's public constructors are kept by its registration.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Runs on Blazor WebAssembly only, which keeps the interpreter.")]
    private static Closing? Close(ServiceDescriptor open, Type serviceType)
    {
        try
        {
            var implementation = open.IsKeyedService ? open.KeyedImplementationType! : open.ImplementationType!;
            return new(new Activation(implementation.MakeGenericType(serviceType.GenericTypeArguments)), open.Lifetime);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string Describe(ServiceDescriptor service)
        => service.IsKeyedService ? $"{Name(service.ServiceType)} with the key '{service.ServiceKey}'" : Name(service.ServiceType);

    private static string Name(MethodInfo method) => $"{(method.DeclaringType is { } type ? Name(type) + "." : "")}{method.Name}";

    private static string Name(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var tick = type.Name.IndexOf('`', StringComparison.Ordinal);
        var name = tick < 0 ? type.Name : type.Name[..tick];
        return type.IsGenericTypeDefinition
            ? $"{name}<{new string(',', type.GetGenericArguments().Length - 1)}>"
            : $"{name}<{string.Join(", ", type.GenericTypeArguments.Select(Name))}>";
    }

    private sealed record Closing(Activation Activation, ServiceLifetime Lifetime);

    /// <summary>
    /// The services of one scope of the container - its root's for the whole connection - in front of a scope of the
    /// application's.
    /// </summary>
    internal sealed class Provider : IKeyedServiceProvider, IServiceProviderIsService, IServiceProviderIsKeyedService, IServiceScopeFactory
    {
        private readonly IServiceProvider _own;
        private readonly IServiceProvider _application;
        private readonly ScopeServices _scope;
        private readonly IServiceProviderIsService _ownIsService;
        private readonly IServiceProviderIsService? _applicationIsService;

        public Provider(BsqliteOwnServices connection, IServiceProvider own, IServiceProvider application, ScopeServices scope)
        {
            Connection = connection;
            _own = own;
            _application = application;
            _scope = scope;
            _ownIsService = own.GetRequiredService<IServiceProviderIsService>();
            _applicationIsService = application.GetService<IServiceProviderIsService>();
        }

        public BsqliteOwnServices Connection { get; }

        /// <summary>The provider of the scope of the container <paramref name="own"/> belongs to.</summary>
        public static Provider Of(IServiceProvider own) => own.GetRequiredService<ScopeServices>().Provider;

        public object? GetService(Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            if (serviceType == typeof(IServiceProvider)
                || serviceType == typeof(IKeyedServiceProvider)
                || serviceType == typeof(IServiceProviderIsService)
                || serviceType == typeof(IServiceProviderIsKeyedService)
                || serviceType == typeof(IServiceScopeFactory))
            {
                return this;
            }

            // The container closes an open generic too, but would build what it takes from itself alone.
            var own = Connection.ClosingOf(serviceType, null) is { } closing
                ? Build(null, closing)
                : All(serviceType) ?? _own.GetService(serviceType);

            // A service the container lacks, or an IEnumerable<T> it has none of - which it answers with an empty array.
            return own is null or Array { Length: 0 } ? _application.GetService(serviceType) : own;
        }

        public object? GetKeyedService(Type serviceType, object? serviceKey)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            if (serviceKey is null)
            {
                return GetService(serviceType);
            }

            var own = Connection.ClosingOf(serviceType, serviceKey) is { } closing
                ? Build(serviceKey, closing)
                : ((IKeyedServiceProvider)_own).GetKeyedService(serviceType, serviceKey);
            return own is null or Array { Length: 0 }
                ? (_application as IKeyedServiceProvider)?.GetKeyedService(serviceType, serviceKey)
                : own;
        }

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
            => GetKeyedService(serviceType, serviceKey)
                ?? throw new InvalidOperationException($"No service for type '{serviceType}' has been registered with the key '{serviceKey}'.");

        // What an activation asks to pick the constructor it can fill. An application container that cannot tell is
        // asked by resolving, as ActivatorUtilities asks one.
        public bool IsService(Type serviceType)
            => serviceType == typeof(IKeyedServiceProvider)
                || _ownIsService.IsService(serviceType)
                || Connection.ClosingOf(serviceType, null) is not null
                || (_applicationIsService is { } applicationIsService
                    ? applicationIsService.IsService(serviceType)
                    : _application.GetService(serviceType) is not null);

        public bool IsKeyedService(Type serviceType, object? serviceKey)
            => serviceKey is null
                ? IsService(serviceType)
                : ((IServiceProviderIsKeyedService)_ownIsService).IsKeyedService(serviceType, serviceKey)
                    || (_applicationIsService as IServiceProviderIsKeyedService)?.IsKeyedService(serviceType, serviceKey) is true;

        /// <summary>A scope of the container's, in front of a scope of the application's that it disposes with it.</summary>
        public IServiceScope CreateScope()
        {
            var own = Connection._container.CreateAsyncScope();
            return new Scope(own, Of(own.ServiceProvider));
        }

        private object Build(object? key, Closing closing) => closing.Lifetime switch
        {
            ServiceLifetime.Singleton => Connection._root.Keep(closing.Activation, key),
            ServiceLifetime.Scoped => _scope.Keep(closing.Activation, key),
            _ => _scope.Track(closing.Activation.Create(this, key)),
        };

        /// <summary>
        /// <paramref name="serviceType"/>, when it is an IEnumerable of a closing of an open generic <c>configure</c>
        /// registered without a key: the closed services the container builds and the closings this provider builds, in
        /// the order <c>configure</c> registered them, as the container would; otherwise null.
        /// </summary>
        private Array? All(Type serviceType)
        {
            if (!serviceType.IsConstructedGenericType || serviceType.GetGenericTypeDefinition() != typeof(IEnumerable<>)
                || serviceType.GenericTypeArguments[0] is not { IsConstructedGenericType: true } itemType
                || !Connection._openGenerics.ContainsKey((itemType.GetGenericTypeDefinition(), null)))
            {
                return null;
            }

            var closed = (Array)_own.GetService(serviceType)!;
            var definition = itemType.GetGenericTypeDefinition();
            var items = new List<object>();
            var next = 0;
            foreach (var service in Connection._enumerated)
            {
                if (service.ServiceType == itemType)
                {
                    items.Add(closed.GetValue(next++)!);
                }
                else if (service.ServiceType == definition && Connection.ClosingBy(itemType, service) is { } closing)
                {
                    items.Add(Build(null, closing));
                }
            }

            var all = Array.CreateInstanceFromArrayType(closed.GetType(), items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                all.SetValue(items[i], i);
            }

            return all;
        }
    }

    /// <summary>
    /// A scope of the container: the provider it is resolved through, the scope of the application's that provider
    /// falls back to, and the closings of open generics built in it - all disposed when the scope is, after what the
    /// container built in it, since this is the first thing a scope builds.
    /// </summary>
    internal sealed class ScopeServices : IDisposable, IAsyncDisposable
    {
        private readonly AsyncServiceScope _application;
        private readonly Lock _lock = new();
        private readonly Dictionary<(Activation Activation, object? Key), object> _kept = [];
        private readonly List<object> _disposables = [];

        public ScopeServices(BsqliteOwnServices connection, IServiceProvider own)
        {
            _application = connection._applicationScopes.CreateAsyncScope();
            Provider = new(connection, own, _application.ServiceProvider, this);
        }

        public Provider Provider { get; }

        public object Keep(Activation activation, object? key)
        {
            lock (_lock)
            {
                if (!_kept.TryGetValue((activation, key), out var kept))
                {
                    _kept.Add((activation, key), kept = Track(activation.Create(Provider, key)));
                }

                return kept;
            }
        }

        public object Track(object service)
        {
            if (service is IDisposable or IAsyncDisposable)
            {
                lock (_lock)
                {
                    _disposables.Add(service);
                }
            }

            return service;
        }

        // The application's scope is disposed even when a service of this one throws.
        public void Dispose()
        {
            try
            {
                foreach (var service in TakeDisposables())
                {
                    if (service is not IDisposable disposable)
                    {
                        throw new InvalidOperationException($"'{service.GetType()}' type only implements IAsyncDisposable. Use DisposeAsync to dispose the container.");
                    }

                    disposable.Dispose();
                }
            }
            finally
            {
                _application.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                foreach (var service in TakeDisposables())
                {
                    if (service is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        ((IDisposable)service).Dispose();
                    }
                }
            }
            finally
            {
                await _application.DisposeAsync().ConfigureAwait(false);
            }
        }

        // Last built, first disposed, as a container does.
        private object[] TakeDisposables()
        {
            lock (_lock)
            {
                var disposables = _disposables.ToArray();
                _disposables.Clear();
                Array.Reverse(disposables);
                return disposables;
            }
        }
    }

    private sealed class Scope(AsyncServiceScope own, Provider services) : IServiceScope, IAsyncDisposable
    {
        public IServiceProvider ServiceProvider => services;

        public void Dispose() => own.Dispose();

        public ValueTask DisposeAsync() => own.DisposeAsync();
    }

    /// <summary>
    /// Constructs an implementation type through a <see cref="Provider"/>, choosing its constructor as the container
    /// would - the longest one it can fill, <c>[FromKeyedServices]</c> and <c>[ServiceKey]</c> included. The choice is
    /// made once, per key for a keyed service, since what either container has never changes once it is built.
    /// </summary>
    internal sealed class Activation([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        private readonly Type _type = type;

        // The types this thread is constructing, the innermost last. The container sees only factories, so a constructor
        // that leads back to one of them would recurse until the stack overflows, rather than be refused as a container
        // refuses it.
        [ThreadStatic]
        private static List<(Type Type, object? Key)>? _constructing;

        private Plan? _plan;
        private ConcurrentDictionary<object, Plan>? _keyedPlans;

        public object Create(IServiceProvider own) => Create(Provider.Of(own), null);

        public object Create(IServiceProvider own, object? key) => Create(Provider.Of(own), key);

        public object Create(Provider services, object? key)
        {
            var constructing = _constructing ??= [];
            if (constructing.IndexOf((_type, key)) is var cycle and >= 0)
            {
                throw new InvalidOperationException(
                    $"A circular dependency was detected for the service of type '{_type}'.{Environment.NewLine}"
                    + string.Join(" -> ", constructing.Skip(cycle).Select(entry => entry.Type).Append(_type)));
            }

            constructing.Add((_type, key));
            try
            {
                var plan = key is null
                    ? _plan ??= Choose(services, null)
                    : (_keyedPlans ??= new()).GetOrAdd(key, static (key, state) => state.Activation.Choose(state.Services, key), (Activation: this, Services: services));

                var arguments = new object?[plan.Arguments.Length];
                for (var i = 0; i < arguments.Length; i++)
                {
                    var argument = plan.Arguments[i];
                    arguments[i] = argument.Source switch
                    {
                        Source.ServiceKey => key,
                        Source.Default => argument.Default,
                        _ => (argument.Key is null ? services.GetService(argument.Type) : services.GetKeyedService(argument.Type, argument.Key))
                            ?? (argument.Source is Source.ServiceOrDefault
                                ? argument.Default
                                : throw new InvalidOperationException($"Unable to resolve service for type '{argument.Type}' while attempting to activate '{_type}'.")),
                    };
                }

                return plan.Constructor.Invoke(arguments);
            }
            finally
            {
                constructing.RemoveAt(constructing.Count - 1);
            }
        }

        private Plan Choose(Provider services, object? key)
        {
            var constructors = _type.GetConstructors();
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"A suitable constructor for type '{_type}' could not be located. Ensure the type is concrete and all "
                    + "parameters of a public constructor are either registered as services or passed as arguments.");
            }

            if (constructors.Length == 1)
            {
                return new(constructors[0], Arguments(constructors[0], services, key, required: true)!);
            }

            Plan? best = null;
            HashSet<Type>? bestParameterTypes = null;
            foreach (var constructor in constructors.OrderByDescending(constructor => constructor.GetParameters().Length))
            {
                if (Arguments(constructor, services, key, required: false) is not { } arguments)
                {
                    continue;
                }

                if (best is null)
                {
                    best = new(constructor, arguments);
                    continue;
                }

                bestParameterTypes ??= [.. best.Constructor.GetParameters().Select(parameter => parameter.ParameterType)];
                if (!constructor.GetParameters().All(parameter => bestParameterTypes.Contains(parameter.ParameterType)))
                {
                    throw new InvalidOperationException(
                        $"Unable to activate type '{_type}'. The following constructors are ambiguous:{Environment.NewLine}{best.Constructor}{Environment.NewLine}{constructor}");
                }
            }

            return best ?? throw new InvalidOperationException(
                $"No constructor for type '{_type}' can be instantiated using services from the service container and default values.");
        }

        // How each parameter of the constructor is filled; null when one cannot be and required is false.
        private Argument[]? Arguments(ConstructorInfo constructor, Provider services, object? key, bool required)
        {
            var parameters = constructor.GetParameters();
            var arguments = new Argument[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var type = parameter.ParameterType;

                if (key is not null && parameter.IsDefined(typeof(ServiceKeyAttribute), inherit: false))
                {
                    if (type != typeof(object) && type != key.GetType())
                    {
                        throw new InvalidOperationException(
                            "The type of the key used for lookup doesn't match the type in the constructor parameter with the ServiceKey attribute.");
                    }

                    arguments[i] = new(type, Source.ServiceKey, null, null);
                    continue;
                }

                var lookup = parameter.GetCustomAttribute<FromKeyedServicesAttribute>(inherit: false) is { } keyed
                    ? keyed.LookupMode switch
                    {
                        ServiceKeyLookupMode.InheritKey => key,
                        ServiceKeyLookupMode.NullKey => null,
                        _ => keyed.Key,
                    }
                    : null;

                var isService = lookup is null ? services.IsService(type) : services.IsKeyedService(type, lookup);
                var fallback = parameter.HasDefaultValue ? DefaultOf(parameter) : null;
                if (isService)
                {
                    arguments[i] = new(type, parameter.HasDefaultValue ? Source.ServiceOrDefault : Source.Service, lookup, fallback);
                }
                else if (parameter.HasDefaultValue)
                {
                    arguments[i] = new(type, Source.Default, null, fallback);
                }
                else if (required)
                {
                    throw new InvalidOperationException($"Unable to resolve service for type '{type}' while attempting to activate '{_type}'.");
                }
                else
                {
                    return null;
                }
            }

            return arguments;
        }

        // A default an enum parameter declares comes back as its underlying value; one a value type declares as default
        // comes back as null, which a constructor invocation turns into that default.
        private static object? DefaultOf(ParameterInfo parameter)
        {
            var value = parameter.DefaultValue;
            var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
            return value is not null && type.IsEnum ? Enum.ToObject(type, value) : value;
        }

        private sealed record Plan(ConstructorInfo Constructor, Argument[] Arguments);

        private readonly record struct Argument(Type Type, Source Source, object? Key, object? Default);

        private enum Source
        {
            Service,
            ServiceOrDefault,
            Default,
            ServiceKey,
        }
    }
}
