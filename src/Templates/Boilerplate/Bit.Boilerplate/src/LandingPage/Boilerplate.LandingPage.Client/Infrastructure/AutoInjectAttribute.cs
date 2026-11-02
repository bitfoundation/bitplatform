namespace Microsoft.Extensions.DependencyInjection;

// [mirror] src/Shared/Infrastructure/Attributes/AutoInjectAttribute.cs
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed partial class AutoInjectAttribute : Attribute
{
}
