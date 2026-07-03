namespace Bit.BlazorUI;

using System.Runtime.CompilerServices;

/// <summary>Null-safe comparer that orders nulls first and falls back to string comparison.</summary>
internal sealed class BitDataGridValueComparer : IComparer<object?>
{
    public static readonly BitDataGridValueComparer Instance = new();

    // object.ToString() returns the type's full name for every instance unless the type overrides it,
    // so using ToString() as a fallback ordering/equality key would collapse unrelated instances of
    // such a type into one value. ValueType.ToString() (the default for structs) has the same problem:
    // it yields the type name unless the struct overrides ToString itself. Only treat ToString() as
    // meaningful when the runtime type actually overrides it past object/ValueType.
    internal static bool HasMeaningfulToString(Type type)
    {
        var declaringType = type.GetMethod(nameof(ToString), Type.EmptyTypes)?.DeclaringType;
        return declaringType != typeof(object) && declaringType != typeof(ValueType);
    }

    // The IComparable fast path (CompareTo == 0 ⇒ equal) is only safe for types where a zero comparison
    // also guarantees an equal GetHashCode, i.e. the BCL scalar/value types whose CompareTo and
    // GetHashCode are defined consistently. An arbitrary custom IComparable may report CompareTo == 0 for
    // two instances while their (un-overridden) GetHashCode differ, which would group/sort them as equal
    // yet hash them apart — an Equals/GetHashCode contract violation. For those types we fall back to the
    // same normalized projection (meaningful ToString, else the canonical key) that GetHashCode uses, so
    // comparison and hashing always agree.
    internal static bool IsTrustedComparable(Type type)
    {
        if (type.IsPrimitive || type.IsEnum) return true;
        return type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || type == typeof(Version);
    }

    // Canonical ordering/identity key for reference values that are neither IComparable nor expose a
    // meaningful ToString() override. RuntimeHelpers.GetHashCode (the previous fallback) can collide:
    // two distinct instances may share an identity hash, which would make Compare return 0 for unequal
    // values and disagree with hash-based equality. A per-instance monotonic key, assigned the first
    // time the comparer sees an object and stable for that object's lifetime, is collision-free, so
    // ordering and hashing both derive from this same key and stay consistent. The table uses weak
    // keys, so it never keeps the compared values alive.
    private static readonly ConditionalWeakTable<object, object> _canonicalKeys = new();
    private static long _nextCanonicalKey;

    internal static long GetCanonicalKey(object obj)
        => (long)_canonicalKeys.GetValue(obj, static _ => (object)Interlocked.Increment(ref _nextCanonicalKey));

    public int Compare(object? x, object? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        // Strings are ordered with the same case-insensitive ordinal rule as the mixed-type fallback
        // below, so the comparer applies one consistent ordering rule for every code path and stays
        // transitive (a culture-sensitive CompareTo here could disagree with the fallback and break
        // the IComparer<T> contract when string and non-string values are mixed in the same column).
        if (x is string sx && y is string sy)
            return string.Compare(sx, sy, StringComparison.OrdinalIgnoreCase);

        // Only take the IComparable shortcut for trusted BCL scalar types, whose CompareTo == 0 also
        // implies an equal GetHashCode. Custom comparables fall through to the same projection-based
        // path GetHashCode uses, so comparison and hashing stay consistent (see IsTrustedComparable).
        if (x is IComparable cx && x.GetType() == y.GetType() && IsTrustedComparable(x.GetType()))
            return cx.CompareTo(y);

        // Mixed types: order first by a stable type discriminator (the full type name) so the ordering
        // is a total order and stays transitive across the whole column. Without this, same-type values
        // ordered via CompareTo and cross-type values ordered via string could disagree (e.g. ints 2 and
        // 10 sort numerically, but 2 vs the string "100" sorting by text would place 2 after it, breaking
        // transitivity and the IComparer<T> contract).
        var tx = x.GetType().FullName ?? x.GetType().Name;
        var ty = y.GetType().FullName ?? y.GetType().Name;
        var typeOrder = string.Compare(tx, ty, StringComparison.Ordinal);
        if (typeOrder != 0) return typeOrder;

        // Same type, not IComparable. Only fall back to ToString() when the type provides a meaningful
        // override; otherwise every instance stringifies to the type name and unrelated rows would be
        // ranked equal (and collapsed when grouping). For such types order by the collision-free
        // canonical key instead, so distinct instances stay distinct while preserving a stable,
        // transitive total order that GetHashCode mirrors exactly.
        if (HasMeaningfulToString(x.GetType()))
            return string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);

        return GetCanonicalKey(x).CompareTo(GetCanonicalKey(y));
    }
}

/// <summary>
/// Equality comparer that mirrors <see cref="BitDataGridValueComparer"/>'s ordering semantics
/// (two values are equal when the comparer ranks them as equal), so grouping keys collapse the same
/// way sorting and Equals-based filtering treat them — e.g. strings group case-insensitively.
/// </summary>
internal sealed class BitDataGridValueEqualityComparer : IEqualityComparer<object?>
{
    public static readonly BitDataGridValueEqualityComparer Instance = new();

    public new bool Equals(object? x, object? y) => BitDataGridValueComparer.Instance.Compare(x, y) == 0;

    // Must stay consistent with Equals: values the comparer treats as equal have to hash alike.
    // Strings compare case-insensitively, so hash them that way. Trusted BCL comparables fall back to
    // their own hash code (where CompareTo == 0 implies an equal hash). For any other value — including
    // a custom IComparable that the comparer does NOT shortcut — the comparer ranks two instances equal
    // only when the type has a meaningful ToString() override and their text matches; hash on that same
    // canonical string. When ToString() is not overridden the comparer keeps distinct instances distinct
    // via the same collision-free canonical key it orders them by, so hash on that key too — keeping
    // Equals/GetHashCode consistent without the identity-hash collisions a raw RuntimeHelpers.GetHashCode
    // could introduce. Null hashes to 0.
    public int GetHashCode(object? obj) => obj switch
    {
        null => 0,
        string s => StringComparer.OrdinalIgnoreCase.GetHashCode(s),
        IComparable when BitDataGridValueComparer.IsTrustedComparable(obj.GetType()) => obj.GetHashCode(),
        _ => BitDataGridValueComparer.HasMeaningfulToString(obj.GetType())
                ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ToString() ?? string.Empty)
                : BitDataGridValueComparer.GetCanonicalKey(obj).GetHashCode()
    };
}
