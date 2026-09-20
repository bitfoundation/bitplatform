using System;

namespace Bit.Butil.Build;

/// <summary>One <c>TypeDef</c> row, in the terms the module map needs.</summary>
public readonly struct TypeDefinition(int row, TypeName name, MetadataToken extends, int fieldListStart, int methodListStart)
{
    public int Row { get; } = row;

    public TypeName Name { get; } = name;

    /// <summary>The base type. Nil for <c>System.Object</c> and for interfaces.</summary>
    public MetadataToken Extends { get; } = extends;

    /// <summary>The one-based <c>Field</c> row this type's fields start at.</summary>
    public int FieldListStart { get; } = fieldListStart;

    /// <summary>The one-based <c>MethodDef</c> row this type's methods start at.</summary>
    public int MethodListStart { get; } = methodListStart;
}
