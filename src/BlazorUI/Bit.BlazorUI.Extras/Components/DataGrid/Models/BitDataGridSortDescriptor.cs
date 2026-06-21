namespace Bit.BlazorUI;

/// <summary>Describes the sort state applied to a single column.</summary>
public sealed class BitDataGridSortDescriptor
{
    public required string ColumnId { get; init; }
    public BitDataGridSortDirection Direction { get; set; } = BitDataGridSortDirection.Ascending;
    /// <summary>Priority for multi-column sorting (1 = primary).</summary>
    public int Priority { get; set; }
}
