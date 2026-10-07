namespace Bit.BlazorUI;

/// <summary>Sort direction for a column.</summary>
public enum BitDataGridSortDirection
{
    /// <summary>The column is not sorted. Passed to SortByAsync it removes the column's sort, and on a group
    /// descriptor it keeps the groups in the order their keys are first met.</summary>
    None = 0,

    /// <summary>Sorts from the smallest value to the largest.</summary>
    Ascending = 1,

    /// <summary>Sorts from the largest value to the smallest.</summary>
    Descending = 2
}
