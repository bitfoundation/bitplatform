using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

/// <summary>
/// The component activator a renderer takes when it is created, installed at startup so BevTools can see the
/// components created after it attaches. Until then it creates components exactly as the framework would, through the
/// activator registered before it if there was one; a renderer never asks for its activator again, so this is the only
/// way in that needs no reflection over the renderer.
/// </summary>
internal sealed class BevToolsActivatorHook(IServiceProvider services, IComponentActivator? inner) : IComponentActivator
{
    // What IComponentActivator.CreateInstance declares; an implementation has to repeat it exactly.
    private const DynamicallyAccessedMemberTypes Component = DynamicallyAccessedMemberTypes.PublicConstructors;
#if NET9_0_OR_GREATER
    private static readonly ConcurrentDictionary<Type, ObjectFactory> Factories = new();
#endif
    private IComponentActivator? _interceptor;
    private IComponentActivator? _untracked;

    /// <summary>Set by BevTools when it attaches to the renderer this scope serves; it creates components through <see cref="Untracked"/>.</summary>
    public IComponentActivator? Interceptor
    {
        get => Volatile.Read(ref _interceptor);
        set => Volatile.Write(ref _interceptor, value);
    }

    /// <summary>Creates components the way the app would without BevTools.</summary>
    public IComponentActivator Untracked => _untracked ??= new UntrackedActivator(this);

    public IComponent CreateInstance([DynamicallyAccessedMembers(Component)] Type componentType) =>
        Interceptor is { } interceptor ? interceptor.CreateInstance(componentType) : CreateUntracked(componentType);

    private IComponent CreateUntracked([DynamicallyAccessedMembers(Component)] Type componentType)
    {
        if (inner is not null)
        {
            return inner.CreateInstance(componentType);
        }

        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(componentType));
        }

        // What the framework's own DefaultComponentActivator does on each runtime: constructor injection arrived in .NET 9.
#if NET9_0_OR_GREATER
        // Not GetOrAdd: the annotation does not flow into its callback (IL2111). Building an entry twice is harmless.
        if (!Factories.TryGetValue(componentType, out var factory))
        {
            factory = ActivatorUtilities.CreateFactory(componentType, Type.EmptyTypes);
            Factories.TryAdd(componentType, factory);
        }

        return (IComponent)factory(services, []);
#else
        _ = services;
        return (IComponent)Activator.CreateInstance(componentType)!;
#endif
    }

    /// <summary>
    /// Registers the hook as the scope's <see cref="IComponentActivator"/>. An activator registered before it keeps
    /// its lifetime and keeps creating the components.
    /// </summary>
    internal static void Register(IServiceCollection services)
    {
        if (services.Any(d => d.ServiceType == typeof(BevToolsActivatorHook)))
        {
            return;
        }

        var existing = services.LastOrDefault(d => d.ServiceType == typeof(IComponentActivator) && !d.IsKeyedService);
        if (existing is not null)
        {
            services.Remove(existing);
            services.Add(existing switch
            {
                { ImplementationInstance: IComponentActivator instance } => ServiceDescriptor.Singleton(new InnerActivator(instance)),
                { ImplementationFactory: { } factory } => new ServiceDescriptor(typeof(InnerActivator), sp => new InnerActivator((IComponentActivator)factory(sp)), existing.Lifetime),
                { ImplementationType: { } type } => new ServiceDescriptor(typeof(InnerActivator), sp => new InnerActivator((IComponentActivator)ActivatorUtilities.CreateInstance(sp, type)), existing.Lifetime),
                _ => existing,
            });
        }

        services.AddScoped(sp => new BevToolsActivatorHook(sp, sp.GetService<InnerActivator>()?.Activator));
        services.AddScoped<IComponentActivator>(sp => sp.GetRequiredService<BevToolsActivatorHook>());
    }

    private sealed class InnerActivator(IComponentActivator activator)
    {
        public IComponentActivator Activator { get; } = activator;
    }

    private sealed class UntrackedActivator(BevToolsActivatorHook hook) : IComponentActivator
    {
        public IComponent CreateInstance([DynamicallyAccessedMembers(Component)] Type componentType) => hook.CreateUntracked(componentType);
    }
}
