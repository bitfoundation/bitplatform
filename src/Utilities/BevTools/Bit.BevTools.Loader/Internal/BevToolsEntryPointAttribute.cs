using System.Diagnostics.CodeAnalysis;

namespace Bit.BevTools;

/// <summary>
/// Names the type in Bit.BevTools that implements <see cref="IBevToolsEntryPoint"/>. The loader reads it from the
/// assembly once that is loaded: the one place the two meet, since the loader must not reference Bit.BevTools.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
internal sealed class BevToolsEntryPointAttribute(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type entryPointType) : Attribute
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type EntryPointType { get; } = entryPointType;
}
