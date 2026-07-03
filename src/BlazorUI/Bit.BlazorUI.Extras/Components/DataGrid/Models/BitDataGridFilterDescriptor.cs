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
    /// <para>
    /// Server-side note for <c>OnRead</c> consumers: when an <see cref="BitDataGridFilterOperator.Equals"/>
    /// (or <see cref="BitDataGridFilterOperator.NotEquals"/>) filter targets a <see cref="System.DateTime"/>
    /// or <see cref="System.DateTimeOffset"/> column, the value is emitted as the selected calendar day at
    /// midnight (its time-of-day is zero). It is intended as a day-level match against each row's own
    /// calendar date, not an exact-instant comparison, so a remote consumer should compare on the date
    /// component (e.g. <c>row.Date == value.Date</c>) rather than the full timestamp.
    /// </para>
    /// </summary>
    public object? Value { get; set; }
}
