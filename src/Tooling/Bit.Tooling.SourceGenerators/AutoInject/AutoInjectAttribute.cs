using System;

namespace Microsoft.Extensions.DependencyInjection.Abstractions;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class AutoInjectAttribute : Attribute
{
}
