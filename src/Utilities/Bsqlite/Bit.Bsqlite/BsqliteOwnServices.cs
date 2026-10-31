using System.Diagnostics.CodeAnalysis;
using Bit.Bsqlite.Interop;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bsqlite;

/// <summary>
/// The services <see cref="BitBsqlite.GetBitBsqliteServices"/> builds for an application that did not register
/// Bsqlite's: what its <c>configure</c> registered, in a container of their own, in front of the application's.
/// </summary>
/// <remarks>
/// <para>
/// Everything that container builds is built through this provider rather than through the container itself, so the
/// constructors and factories it registered, the <c>BeforeFirstOpen</c> steps and the contexts' constructors reach the
/// application's services - its <c>IJSRuntime</c>, its logging, its <c>LazyAssemblyLoader</c> - as they would if the
/// same calls had been made in <c>Program.cs</c>. A service both have is the container's own.
/// </para>
/// <para>
/// Everything is resolved from one scope, created when the container is built: the whole application on WebAssembly,
/// as the application's own scope is. Neither is ever disposed, and nothing needs them to be: they live as long as the
/// browser connection they were built for, and a page that is gone has no worker left to terminate.
/// </para>
/// </remarks>
internal sealed class BsqliteOwnServices : IKeyedServiceProvider, IServiceProviderIsKeyedService
{
    private readonly IServiceProvider _application;
    private IServiceProvider _own = default!;

    private BsqliteOwnServices(IServiceProvider application) => _application = application;

    /// <summary>
    /// Runs <paramref name="configure"/> on a collection of its own and builds it in front of
    /// <paramref name="application"/>.
    /// </summary>
    public static BsqliteOwnServices Build(IServiceProvider application, Action<IServiceCollection> configure)
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

        var services = new BsqliteOwnServices(application);
        IServiceCollection own = new ServiceCollection();
        foreach (var service in registered)
        {
            own.Add(services.InFront(service));
        }

        services._own = own.BuildServiceProvider().CreateScope().ServiceProvider;
        return services;
    }

    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceType == typeof(IServiceProvider)
            || serviceType == typeof(IKeyedServiceProvider)
            || serviceType == typeof(IServiceProviderIsService)
            || serviceType == typeof(IServiceProviderIsKeyedService))
        {
            return this;
        }

        return OrElse(_own.GetService(serviceType), () => _application.GetService(serviceType));
    }

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        return OrElse(((IKeyedServiceProvider)_own).GetKeyedService(serviceType, serviceKey),
            () => (_application as IKeyedServiceProvider)?.GetKeyedService(serviceType, serviceKey));
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
        => GetKeyedService(serviceType, serviceKey)
            ?? throw new InvalidOperationException($"No service for type '{serviceType}' has been registered with the key '{serviceKey}'.");

    // What ActivatorUtilities asks to pick the constructor it can fill.
    public bool IsService(Type serviceType)
        => serviceType == typeof(IServiceProvider)
            || _own.GetRequiredService<IServiceProviderIsService>().IsService(serviceType)
            || _application.GetService<IServiceProviderIsService>()?.IsService(serviceType) is true;

    public bool IsKeyedService(Type serviceType, object? serviceKey)
        => _own.GetRequiredService<IServiceProviderIsKeyedService>().IsKeyedService(serviceType, serviceKey)
            || _application.GetService<IServiceProviderIsKeyedService>()?.IsKeyedService(serviceType, serviceKey) is true;

    // A service the container lacks, or an IEnumerable<T> it has none of - which it answers with an empty array.
    private static object? OrElse(object? own, Func<object?> application)
        => own is null or Array { Length: 0 } ? application() : own;

    /// <summary>
    /// <paramref name="service"/>, built through this provider: a factory is handed it instead of the container, and an
    /// implementation type is constructed from it. An instance needs nothing, and an open generic, which only the
    /// container can close, is left to it.
    /// </summary>
    private ServiceDescriptor InFront(ServiceDescriptor service)
    {
        if (service.IsKeyedService)
        {
            if (service.KeyedImplementationFactory is { } keyedFactory)
            {
                return new(service.ServiceType, service.ServiceKey, (_, key) => keyedFactory(this, key), service.Lifetime);
            }

            return service.KeyedImplementationType is { IsGenericTypeDefinition: false }
                ? new(service.ServiceType, service.ServiceKey, new Activation(this, service.KeyedImplementationType).Create, service.Lifetime)
                : service;
        }

        if (service.ImplementationFactory is { } factory)
        {
            return new(service.ServiceType, _ => factory(this), service.Lifetime);
        }

        return service.ImplementationType is { IsGenericTypeDefinition: false }
            ? new(service.ServiceType, new Activation(this, service.ImplementationType).Create, service.Lifetime)
            : service;
    }

    // A class rather than a lambda, so the trim analyzers see the constructors kept for the type reach the activation.
    private sealed class Activation(BsqliteOwnServices services, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        private readonly Type _type = type;

        public object Create(IServiceProvider _) => ActivatorUtilities.CreateInstance(services, _type);

        public object Create(IServiceProvider _, object? __) => ActivatorUtilities.CreateInstance(services, _type);
    }
}
