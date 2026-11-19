namespace Bit.BevTools;

/// <summary>Excludes a component type, or a field/property from inspection.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
public sealed class BevToolsIgnoreAttribute : Attribute
{
}
