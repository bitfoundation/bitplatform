using Microsoft.AspNetCore.Components;

namespace Bit.Borm;

/// <summary>
/// Resolves the Blazor component used to render each <see cref="BormFieldType"/>. Applications can
/// override built-in mappings or register renderers for custom field keys, giving the same
/// extensibility as JSON Forms' renderer set or react-jsonschema-form widgets.
/// </summary>
public interface IBormFieldRendererRegistry
{
    /// <summary>Maps a field type to the component that renders its input control.</summary>
    void Register(BormFieldType type, Type componentType);

    /// <summary>Maps a custom renderer key (see <see cref="BormFieldDefinition.CustomRenderer"/>) to a component.</summary>
    void RegisterCustom(string key, Type componentType);

    /// <summary>
    /// Resolves the component type for a field, honouring custom renderer keys first.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The field is <see cref="BormFieldType.Custom"/> but its
    /// <see cref="BormFieldDefinition.CustomRenderer"/> key has no registered component. Failing
    /// loudly beats silently rendering a text box the developer never asked for.
    /// </exception>
    Type Resolve(BormFieldDefinition field);

    /// <summary>Resolves the component type for a field, returning false instead of throwing when none is registered.</summary>
    bool TryResolve(BormFieldDefinition field, out Type componentType);
}

/// <summary>Strongly-typed conveniences over <see cref="IBormFieldRendererRegistry"/>.</summary>
public static class BormFieldRendererRegistryExtensions
{
    /// <summary>Maps a field type to <typeparamref name="TComponent"/>.</summary>
    public static IBormFieldRendererRegistry Register<TComponent>(
        this IBormFieldRendererRegistry registry, BormFieldType type)
        where TComponent : IComponent
    {
        registry.Register(type, typeof(TComponent));
        return registry;
    }

    /// <summary>Maps a custom renderer key to <typeparamref name="TComponent"/>.</summary>
    public static IBormFieldRendererRegistry RegisterCustom<TComponent>(
        this IBormFieldRendererRegistry registry, string key)
        where TComponent : IComponent
    {
        registry.RegisterCustom(key, typeof(TComponent));
        return registry;
    }
}
