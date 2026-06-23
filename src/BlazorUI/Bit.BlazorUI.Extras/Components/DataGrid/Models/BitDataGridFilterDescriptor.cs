namespace Bit.BlazorUI;

/// <summary>Describes a filter applied to a single column.</summary>
public sealed class BitDataGridFilterDescriptor
{
    /// <summary>The identifier of the column being filtered. Immutable once the descriptor is created.</summary>
    public required string ColumnId { get; init; }

    /// <summary>The filter operation to apply. Defaults to <see cref="BitDataGridFilterOperator.Contains"/>.</summary>
    public BitDataGridFilterOperator Operator { get; set; } = BitDataGridFilterOperator.Contains;

    /// <summary>
    /// The value to filter by. Its meaning depends on the selected <see cref="Operator"/> and it is
    /// unused for value-less operators such as <see cref="BitDataGridFilterOperator.IsEmpty"/> and
    /// <see cref="BitDataGridFilterOperator.IsNotEmpty"/>.
    /// </summary>
    public object? Value { get; set; }
}
