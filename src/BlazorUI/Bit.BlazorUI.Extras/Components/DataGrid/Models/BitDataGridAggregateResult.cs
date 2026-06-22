namespace Bit.BlazorUI;

/// <summary>Holds the computed aggregate value for a column footer or group.</summary>
public sealed class BitDataGridAggregateResult
{
    public required string ColumnId { get; init; }
    public BitDataGridAggregateType Type { get; init; }
    public object? Value { get; init; }
    public required string FormattedValue { get; init; }
}
