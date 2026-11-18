namespace Bit.Bheadless;

/// <summary>Sort direction announced by a grid column header (<c>aria-sort</c>).</summary>
public enum BitBhSortDirection
{
    /// <summary>Not sorted (no <c>aria-sort</c>).</summary>
    None,

    /// <summary>Ascending.</summary>
    Ascending,

    /// <summary>Descending.</summary>
    Descending,
}
