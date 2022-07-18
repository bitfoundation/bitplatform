using System;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// This attribute can be applied on either fields or properties to generate the rest of the required codes using Bit.SourceGenerators.
/// The Bit.SourceGenerators for this attribute generates an appropriate property with the [Inject] attribute for Blazor components and constructor injection for other classes.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class AutoInjectAttribute : Attribute
{
}
