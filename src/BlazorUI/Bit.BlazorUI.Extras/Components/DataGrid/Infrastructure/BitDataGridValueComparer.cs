namespace Bit.BlazorUI;

/// <summary>Null-safe comparer that orders nulls first and falls back to string comparison.</summary>
internal sealed class BitDataGridValueComparer : IComparer<object?>
{
    public static readonly BitDataGridValueComparer Instance = new();

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

        if (x is IComparable cx && x.GetType() == y.GetType())
            return cx.CompareTo(y);

        // Mixed types: order first by a stable type discriminator (the full type name) so the ordering
        // is a total order and stays transitive across the whole column. Without this, same-type values
        // ordered via CompareTo and cross-type values ordered via string could disagree (e.g. ints 2 and
        // 10 sort numerically, but 2 vs the string "100" sorting by text would place 2 after it, breaking
        // transitivity and the IComparer<T> contract). Within the same type name we then fall back to a
        // symmetric, case-insensitive string comparison.
        var tx = x.GetType().FullName ?? x.GetType().Name;
        var ty = y.GetType().FullName ?? y.GetType().Name;
        var typeOrder = string.Compare(tx, ty, StringComparison.Ordinal);
        if (typeOrder != 0) return typeOrder;

        return string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
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
    // Strings compare case-insensitively, so hash them that way. IComparable values fall back to their
    // own hash code (where CompareTo == 0 implies an equal hash for well-behaved types). Any other
    // (non-IComparable) value is ranked equal by Compare only when its ToString() matches - the
    // comparer's final fallback - so hash it on that same canonical string rather than obj.GetHashCode(),
    // otherwise two values the comparer calls equal could hash differently and break grouping/lookups.
    // Null hashes to 0.
    public int GetHashCode(object? obj) => obj switch
    {
        null => 0,
        string s => StringComparer.OrdinalIgnoreCase.GetHashCode(s),
        IComparable => obj.GetHashCode(),
        _ => StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ToString() ?? string.Empty)
    };
}
