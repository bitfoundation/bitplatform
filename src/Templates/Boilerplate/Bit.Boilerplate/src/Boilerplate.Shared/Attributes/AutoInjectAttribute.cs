namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// By applying this attribute to either fields or properties, the bit source generator will automatically
/// generate the remaining code necessary to inject that field or property using dependency injection.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class AutoInjectAttribute : Attribute
{
}
