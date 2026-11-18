using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Bit.Borm;

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
