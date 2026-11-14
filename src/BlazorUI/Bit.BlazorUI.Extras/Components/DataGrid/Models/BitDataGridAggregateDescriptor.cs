namespace Bit.BlazorUI;

/// <summary>Describes an aggregate a column shows in the footer (in <see cref="BitDataGridReadRequest.Aggregates"/>).</summary>
public sealed class BitDataGridAggregateDescriptor
{
    /// <summary>The identifier of the column the aggregate is for.</summary>
    public required string ColumnId { get; init; }

    /// <summary>
    /// The function the column aggregates with. <see cref="BitDataGridAggregateType.Custom"/> means the column has an
    /// <c>AggregateBy</c> delegate of its own, which the grid cannot send: compute that column's value your own way.
    /// </summary>
    public required BitDataGridAggregateType Type { get; init; }
}
