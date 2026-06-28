namespace Bit.BlazorUI;

/// <summary>Holds the computed aggregate value for a column footer or group.</summary>
public sealed class BitDataGridAggregateResult
{
    /// <summary>The identifier of the column this aggregate was computed for.</summary>
    public required string ColumnId { get; init; }

    /// <summary>The kind of aggregation performed (e.g. Sum, Average, Count). Required so an aggregate result cannot be created without declaring its aggregation kind.</summary>
    public required BitDataGridAggregateType Type { get; init; }

    /// <summary>The raw computed aggregate value, or <c>null</c> when no value applies.</summary>
    public object? Value { get; init; }

    /// <summary>The display-ready string produced by formatting <see cref="Value"/>.</summary>
    public required string FormattedValue { get; init; }
}
