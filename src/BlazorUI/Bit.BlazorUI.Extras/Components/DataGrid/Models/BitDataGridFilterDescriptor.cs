namespace Bit.BlazorUI;

/// <summary>Describes a filter applied to a single column.</summary>
public sealed class BitDataGridFilterDescriptor
{
    public required string ColumnId { get; init; }
    public BitDataGridFilterOperator Operator { get; set; } = BitDataGridFilterOperator.Contains;
    public object? Value { get; set; }
}
