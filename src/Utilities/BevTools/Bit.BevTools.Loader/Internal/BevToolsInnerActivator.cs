using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

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
