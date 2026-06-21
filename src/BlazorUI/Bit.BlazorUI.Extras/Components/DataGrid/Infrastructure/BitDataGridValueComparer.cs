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

        if (x is IComparable cx2)
        {
            try { return cx2.CompareTo(Convert.ChangeType(y, x.GetType())); }
            catch { /* fall through */ }
        }

        return string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
