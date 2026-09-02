using System;

namespace Bit.Butil.Build;

/// <summary>A type's namespace and name, as the metadata spells them.</summary>
public readonly struct TypeName(string @namespace, string name) : IEquatable<TypeName>
{
    public string Namespace { get; } = @namespace ?? string.Empty;

    public string Name { get; } = name ?? string.Empty;

    public string FullName => Namespace.Length == 0 ? Name : Namespace + "." + Name;

    public bool Equals(TypeName other)
        => string.Equals(Namespace, other.Namespace, StringComparison.Ordinal) && string.Equals(Name, other.Name, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is TypeName other && Equals(other);

    public override int GetHashCode() => (Namespace.GetHashCode() * 397) ^ Name.GetHashCode();

    public override string ToString() => FullName;
}
