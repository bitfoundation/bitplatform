using System.Collections.Generic;

namespace Bit.Brouter.Generators;

/// <summary>
/// A route discovered in a .razor file, reduced to what URL generation needs:
/// <c>Template</c> is the full normalized template ("users/{id}/edit", no surrounding slashes),
/// <c>Name</c> the explicit route name (from a Broute <c>Name="..."</c>) or null, and
/// <c>Segments</c> the parsed segments in order. Records with a value-equatable segment list so
/// the incremental pipeline can cache on equality.
/// </summary>
internal sealed record RouteModel(string Template, string? Name, EquatableArray<RouteSegment> Segments);

/// <summary>
/// One template segment: <c>Value</c> is the literal text or parameter name; <c>ClrType</c> the C#
/// type keyword the (last) constraint maps to ("string" when unconstrained).
/// </summary>
internal sealed record RouteSegment(SegmentKind Kind, string Value, string ClrType, bool IsOptional);

internal enum SegmentKind
{
    Literal,
    Parameter,
    CatchAll,
    /// <summary>A literal '*' or '**' wildcard - the template can't be resolved into a URL.</summary>
    Wildcard,
}

/// <summary>
/// Minimal immutable array with structural equality, so records holding lists stay cacheable by
/// the incremental generator infrastructure (plain arrays/ImmutableArray compare by reference).
/// </summary>
internal readonly struct EquatableArray<T> : System.IEquatable<EquatableArray<T>>
    where T : notnull
{
    private static readonly T[] _empty = new T[0];

    private readonly T[]? _items;

    public EquatableArray(T[] items) => _items = items;

    public IReadOnlyList<T> Items => _items ?? _empty;

    public int Count => _items?.Length ?? 0;

    public T this[int index] => _items![index];

    public bool Equals(EquatableArray<T> other)
    {
        var a = _items ?? _empty;
        var b = other._items ?? _empty;
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
        {
            if (EqualityComparer<T>.Default.Equals(a[i], b[i]) is false) return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var item in _items ?? _empty)
        {
            hash = unchecked(hash * 31 + item.GetHashCode());
        }
        return hash;
    }
}
