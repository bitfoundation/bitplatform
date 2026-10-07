namespace Bit.BlazorUI;

/// <summary>Built-in aggregate functions for summary/footer rows.</summary>
public enum BitDataGridAggregateType
{
    /// <summary>No aggregate: the column shows nothing in the footer and group rows, unless it has an
    /// AggregateBy delegate of its own.</summary>
    None = 0,

    /// <summary>The total of the values that convert to a number; the rest, nulls included, are skipped.</summary>
    Sum,

    /// <summary>The mean of the values that convert to a number, or 0 when there are none; the rest, nulls
    /// included, are skipped.</summary>
    Average,

    /// <summary>The number of rows, whatever their values.</summary>
    Count,

    /// <summary>The smallest non-null value.</summary>
    Min,

    /// <summary>The largest non-null value.</summary>
    Max,

    /// <summary>The value was produced by the column's custom AggregateBy delegate rather than a
    /// built-in function, so footer consumers can tell custom aggregation apart from no aggregation.</summary>
    Custom
}
