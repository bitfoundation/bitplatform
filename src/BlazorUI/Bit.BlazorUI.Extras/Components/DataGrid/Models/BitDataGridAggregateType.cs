namespace Bit.BlazorUI;

/// <summary>Built-in aggregate functions for summary/footer rows.</summary>
public enum BitDataGridAggregateType
{
    None = 0,
    Sum,
    Average,
    Count,
    Min,
    Max,

    /// <summary>The value was produced by the column's custom AggregateBy delegate rather than a
    /// built-in function, so footer consumers can tell custom aggregation apart from no aggregation.</summary>
    Custom
}
