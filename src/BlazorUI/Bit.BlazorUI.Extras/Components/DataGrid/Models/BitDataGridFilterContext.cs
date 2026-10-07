namespace Bit.BlazorUI;

/// <summary>
/// What a column's <c>FilterTemplate</c> renders its own filter editor from: the filter currently
/// applied to the column, and the calls that change it. Applying goes through the same pipeline as the
/// built-in editors, so the view, paging, <c>OnFilterChange</c>, <c>OnStateChange</c>, the toolbar's
/// <i>Clear filters</i> button and the screen-reader announcement all follow.
/// </summary>
public sealed class BitDataGridFilterContext
{
    private readonly Func<BitDataGridFilterOperator, object?, Task> _apply;
    private readonly Func<object?, object?, Task> _applyRange;
    private readonly Func<Task> _clear;

    internal BitDataGridFilterContext(
        string columnId,
        string title,
        string label,
        Type? valueType,
        bool disabled,
        IReadOnlyList<BitDataGridFilterDescriptor> filters,
        Func<BitDataGridFilterOperator, object?, Task> apply,
        Func<object?, object?, Task> applyRange,
        Func<Task> clear)
    {
        ColumnId = columnId;
        Title = title;
        Label = label;
        ValueType = valueType;
        Disabled = disabled;
        Filters = filters;
        _apply = apply;
        _applyRange = applyRange;
        _clear = clear;
    }

    /// <summary>The identifier of the column being filtered.</summary>
    public string ColumnId { get; }

    /// <summary>The column's header text.</summary>
    public string Title { get; }

    /// <summary>The accessible name the editor should carry (<c>Strings.FilterByFormat</c>, e.g. "Filter by Category").
    /// A filter cell has no visible label of its own, so put it on the control as its <c>aria-label</c>.</summary>
    public string Label { get; }

    /// <summary>The type of the column's bound member, with any <see cref="Nullable{T}"/> unwrapped.</summary>
    public Type? ValueType { get; }

    /// <summary>Whether the grid is disabled; a disabled grid's editor should be disabled too.</summary>
    public bool Disabled { get; }

    /// <summary>The descriptors applied to this column: none, one, or the two halves of a range.</summary>
    public IReadOnlyList<BitDataGridFilterDescriptor> Filters { get; }

    /// <summary>Whether any filter is applied to the column.</summary>
    public bool IsActive => Filters.Count > 0;

    /// <summary>The operator of the column's (first) filter, or <see cref="BitDataGridFilterOperator.Unspecified"/> when none is applied.</summary>
    public BitDataGridFilterOperator Operator => Filters.Count > 0 ? Filters[0].Operator : BitDataGridFilterOperator.Unspecified;

    /// <summary>The value of the column's (first) filter, or <c>null</c> when none is applied.</summary>
    public object? Value => Filters.Count > 0 ? Filters[0].Value : null;

    /// <summary>Replaces the column's filter. A <c>null</c>, blank or empty-set value clears it, except for the value-less operators.</summary>
    public Task ApplyAsync(BitDataGridFilterOperator op, object? value) => _apply(op, value);

    /// <summary>Replaces the column's filter with a half-open range (<c>&gt;= from</c> AND <c>&lt; toExclusive</c>); a <c>null</c> bound clears it.</summary>
    public Task ApplyRangeAsync(object? from, object? toExclusive) => _applyRange(from, toExclusive);

    /// <summary>Removes the column's filter.</summary>
    public Task ClearAsync() => _clear();
}
