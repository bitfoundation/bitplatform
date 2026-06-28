namespace Bit.BlazorUI;

/// <summary>Describes a filter applied to a single column.</summary>
public sealed class BitDataGridFilterDescriptor
{
    /// <summary>The identifier of the column being filtered. Immutable once the descriptor is created.</summary>
    public required string ColumnId { get; init; }

    /// <summary>
    /// The filter operation to apply. Has no default: an omitted value stays
    /// <see cref="BitDataGridFilterOperator.Unspecified"/> so a descriptor created without an explicit
    /// operator is treated as invalid/omitted rather than silently filtering as "contains".
    /// </summary>
    public BitDataGridFilterOperator Operator { get; set; }

    /// <summary>
    /// The value to filter by. Its meaning depends on the selected <see cref="Operator"/> and it is
    /// unused for value-less operators such as <see cref="BitDataGridFilterOperator.IsEmpty"/> and
    /// <see cref="BitDataGridFilterOperator.IsNotEmpty"/>.
    /// </summary>
    public object? Value { get; set; }
}
