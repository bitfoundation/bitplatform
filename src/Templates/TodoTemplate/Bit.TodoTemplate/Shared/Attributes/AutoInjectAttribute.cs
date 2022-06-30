namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// You can apply this attribute on either fields or properties and it generated the rest of required codes using Bit.SourceGenerators.
/// It generates a contructor and accepts parameter of that field or property type and assigns the parameter's value to the field or property.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class AutoInjectAttribute : Attribute
{
}
