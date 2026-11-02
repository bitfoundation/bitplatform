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
    private static readonly ConcurrentDictionary<Type, ObjectFactory> Factories = new();
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

    private IComponent CreateUntracked([DynamicallyAccessedMembers(Component)] Type componentType) =>
        inner is not null ? inner.CreateInstance(componentType) : CreateDefault(services, componentType);

    /// <summary>
    /// Creates a component exactly as the framework's own DefaultComponentActivator does, constructor injection
    /// included. Bit.BevTools' activator creates components through it too, so neither one changes
    /// what an app can construct.
    /// </summary>
    internal static IComponent CreateDefault(IServiceProvider services, [DynamicallyAccessedMembers(Component)] Type componentType)
    {
        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(componentType));
        }

        // Not GetOrAdd: the annotation does not flow into its callback (IL2111). Building an entry twice is harmless.
        if (!Factories.TryGetValue(componentType, out var factory))
        {
            factory = ActivatorUtilities.CreateFactory(componentType, Type.EmptyTypes);
            Factories.TryAdd(componentType, factory);
        }

        return (IComponent)factory(services, []);
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

        BevToolsInnerActivator.MoveExisting(services);
        services.AddScoped(sp => new BevToolsActivatorHook(sp, sp.GetService<BevToolsInnerActivator>()?.Activator));
        services.AddScoped<IComponentActivator>(sp => sp.GetRequiredService<BevToolsActivatorHook>());
    }

    private sealed class UntrackedActivator(BevToolsActivatorHook hook) : IComponentActivator
    {
        public IComponent CreateInstance([DynamicallyAccessedMembers(Component)] Type componentType) => hook.CreateUntracked(componentType);
    }
}

/// <summary>
/// Holds the activator that was registered before BevTools (a custom activator, bUnit's factories), so it keeps
/// creating the components behind BevTools' own: the loader's hook, or Bit.BevTools' activator.
/// </summary>
internal sealed class BevToolsInnerActivator(IComponentActivator activator)
{
    public IComponentActivator Activator { get; } = activator;

    /// <summary>Moves the <see cref="IComponentActivator"/> registered last behind this holder, keeping its lifetime.</summary>
    internal static void MoveExisting(IServiceCollection services)
    {
        var existing = services.LastOrDefault(d => d.ServiceType == typeof(IComponentActivator) && !d.IsKeyedService);
        ServiceDescriptor? holder = existing switch
        {
            { ImplementationInstance: IComponentActivator instance } => ServiceDescriptor.Singleton(new BevToolsInnerActivator(instance)),
            { ImplementationFactory: { } factory } => new ServiceDescriptor(typeof(BevToolsInnerActivator), sp => new BevToolsInnerActivator((IComponentActivator)factory(sp)), existing.Lifetime),
            { ImplementationType: { } type } => new ServiceDescriptor(typeof(BevToolsInnerActivator), sp => new BevToolsInnerActivator((IComponentActivator)ActivatorUtilities.CreateInstance(sp, type)), existing.Lifetime),
            _ => null,
        };

        if (holder is not null)
        {
            services.Remove(existing!);
            services.Add(holder);
        }
    }
}
