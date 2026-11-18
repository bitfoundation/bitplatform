namespace Bit.BlazorUI;

/// <summary>Describes a grouping applied to a column.</summary>
public sealed class BitDataGridGroupDescriptor
{
    /// <summary>The id of the column whose values the rows are grouped by.</summary>
    public required string ColumnId { get; init; }

    /// <summary>The order of the groups by their key: <see cref="BitDataGridSortDirection.Ascending"/> (the default),
    /// <see cref="BitDataGridSortDirection.Descending"/>, or <see cref="BitDataGridSortDirection.None"/> for the order
    /// the keys first appear in the rows.</summary>
    public BitDataGridSortDirection Direction { get; set; } = BitDataGridSortDirection.Ascending;
}
