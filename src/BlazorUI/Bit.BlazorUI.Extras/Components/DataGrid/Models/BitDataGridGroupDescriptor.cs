namespace Bit.BlazorUI;

/// <summary>Describes a grouping applied to a column.</summary>
public sealed class BitDataGridGroupDescriptor
{
    public required string ColumnId { get; init; }
    public BitDataGridSortDirection Direction { get; set; } = BitDataGridSortDirection.Ascending;
}
