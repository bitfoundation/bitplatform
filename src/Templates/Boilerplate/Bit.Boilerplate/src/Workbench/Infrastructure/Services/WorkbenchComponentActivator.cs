using System.Diagnostics.CodeAnalysis;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Creates the components of a circuit as Blazor does, constructor injection included, and records each bit BlazorUI one in
/// <see cref="WorkbenchComponentInstances"/>.
/// </summary>
public sealed class WorkbenchComponentActivator(IServiceProvider services, WorkbenchComponentInstances instances) : IComponentActivator
{
    private static readonly ConcurrentDictionary<Type, ObjectFactory> factories = new();

    public IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
    {
        var factory = factories.GetOrAdd(componentType, type => ActivatorUtilities.CreateFactory(type, Type.EmptyTypes));
        var component = (IComponent)factory(services, []);

        if (component is BitComponentBase bitComponent)
        {
            instances.Add(bitComponent);
        }

        return component;
    }
}
