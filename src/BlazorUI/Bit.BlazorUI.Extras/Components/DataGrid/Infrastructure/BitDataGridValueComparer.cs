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

        if (x is IComparable cx && x.GetType() == y.GetType())
            return cx.CompareTo(y);

        // Mixed types: compare by string representation so the result is symmetric regardless of
        // argument order. A one-sided Convert.ChangeType (coercing y to x's type) could order the same
        // pair differently when the operands are swapped, breaking the IComparer<T> contract. When the
        // strings are equal we treat the values as equivalent for ordering (return 0) rather than
        // attempting an asymmetric type-specific tie-break.
        return string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
