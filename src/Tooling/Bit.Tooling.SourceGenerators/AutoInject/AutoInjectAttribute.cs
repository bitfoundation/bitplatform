using System;

namespace Bit.Tooling.SourceGenerators;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class AutoInjectAttribute : Attribute
{
}
