using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Bit.Borm;

/// <summary>
/// Resolves the Blazor component used to render each <see cref="BormFieldType"/>. Applications can
/// override built-in mappings or register renderers for custom field keys, giving the same
/// extensibility as JSON Forms' renderer set or react-jsonschema-form widgets.
/// </summary>
/// <remarks>
/// The component types are annotated as Blazor annotates its own, so a trimmed app keeps the constructor and the
/// parameters of every component registered here.
/// </remarks>
public interface IBormFieldRendererRegistry
{
    /// <summary>Maps a field type to the component that renders its input control.</summary>
    void Register(BormFieldType type, [DynamicallyAccessedMembers(BormFieldRendererRegistry.Component)] Type componentType);

    /// <summary>Maps a custom renderer key (see <see cref="BormFieldDefinition.CustomRenderer"/>) to a component.</summary>
    void RegisterCustom(string key, [DynamicallyAccessedMembers(BormFieldRendererRegistry.Component)] Type componentType);

    /// <summary>
    /// Resolves the component type for a field, honouring custom renderer keys first.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The field is <see cref="BormFieldType.Custom"/> but its
    /// <see cref="BormFieldDefinition.CustomRenderer"/> key has no registered component. Failing
    /// loudly beats silently rendering a text box the developer never asked for.
    /// </exception>
    [return: DynamicallyAccessedMembers(BormFieldRendererRegistry.Component)]
    Type Resolve(BormFieldDefinition field);

    /// <summary>Resolves the component type for a field, returning false instead of throwing when none is registered.</summary>
    bool TryResolve(BormFieldDefinition field, [DynamicallyAccessedMembers(BormFieldRendererRegistry.Component)] out Type componentType);
}

/// <summary>Strongly-typed conveniences over <see cref="IBormFieldRendererRegistry"/>.</summary>
public static class BormFieldRendererRegistryExtensions
{
    /// <summary>Maps a field type to <typeparamref name="TComponent"/>.</summary>
    public static IBormFieldRendererRegistry Register<[DynamicallyAccessedMembers(BormFieldRendererRegistry.Component)] TComponent>(
        this IBormFieldRendererRegistry registry, BormFieldType type)
        where TComponent : IComponent
    {
        registry.Register(type, typeof(TComponent));
        return registry;
    }

    /// <summary>Maps a custom renderer key to <typeparamref name="TComponent"/>.</summary>
    public static IBormFieldRendererRegistry RegisterCustom<[DynamicallyAccessedMembers(BormFieldRendererRegistry.Component)] TComponent>(
        this IBormFieldRendererRegistry registry, string key)
        where TComponent : IComponent
    {
        registry.RegisterCustom(key, typeof(TComponent));
        return registry;
    }
}
