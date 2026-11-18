namespace Bit.BlazorUI;

/// <summary>Describes the sort state applied to a single column.</summary>
public sealed class BitDataGridSortDescriptor
{
    /// <summary>The id of the column the rows are sorted by.</summary>
    public required string ColumnId { get; init; }

    /// <summary>The direction of the sort; <see cref="BitDataGridSortDirection.None"/> leaves the column out of
    /// it.</summary>
    public BitDataGridSortDirection Direction { get; set; } = BitDataGridSortDirection.Ascending;

    /// <summary>Priority for multi-column sorting (1 = primary). Defaults to <see cref="int.MaxValue"/> so that
    /// explicitly assigned priorities always take precedence over descriptors left unset.</summary>
    public int Priority { get; set; } = int.MaxValue;
}
