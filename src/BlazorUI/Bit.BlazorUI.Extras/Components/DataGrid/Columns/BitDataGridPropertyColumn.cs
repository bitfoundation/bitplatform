﻿using System.Linq.Expressions;

namespace Bit.BlazorUI;

/// <summary>
/// Represents a <see cref="BitDataGrid{TGridItem}"/> column whose cells display a single value.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
/// <typeparam name="TProp">The type of the value being displayed in the column's cells.</typeparam>
public class BitDataGridPropertyColumn<TGridItem, TProp> : BitDataGridColumnBase<TGridItem>, IBitDataGridSortBuilderColumn<TGridItem>
{
    private Expression<Func<TGridItem, TProp>>? _lastAssignedProperty;
    private Func<TGridItem, string?>? _cellTextFunc;
    private BitDataGridSort<TGridItem>? _sortBuilder;

    /// <summary>
    /// Defines the value to be displayed in this column's cells.
    /// </summary>
    [Parameter, EditorRequired] public Expression<Func<TGridItem, TProp>> Property { get; set; } = default!;

    /// <summary>
    /// Optionally specifies a format string for the value.
    ///
    /// Using this requires the <typeparamref name="TProp"/> type to implement <see cref="IFormattable" />.
    /// </summary>
    [Parameter] public string? Format { get; set; }

    BitDataGridSort<TGridItem>? IBitDataGridSortBuilderColumn<TGridItem>.SortBuilder => _sortBuilder;


    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // We have to do a bit of pre-processing on the lambda expression. Only do that if it's new or changed.
        if (_lastAssignedProperty != Property)
        {
            _lastAssignedProperty = Property;
            var compiledPropertyExpression = Property.Compile();

            if (Format.HasValue())
            {
                if (typeof(IFormattable).IsAssignableFrom(typeof(TProp)))
                {
                    _cellTextFunc = item => ((IFormattable?)compiledPropertyExpression!(item))?.ToString(Format, null);

                }
                else
                {
                    throw new InvalidOperationException($"A '{nameof(Format)}' parameter was supplied, but the type '{typeof(TProp)}' does not implement '{typeof(IFormattable)}'.");
                }
            }
            else
            {
                _cellTextFunc = item => compiledPropertyExpression!(item)?.ToString();
            }

            _sortBuilder = BitDataGridSort<TGridItem>.ByAscending(Property);
        }

        if (Title is null && Property.Body is MemberExpression memberExpression)
        {
            Title = memberExpression.Member.Name;
        }
    }

    /// <inheritdoc />
    protected internal override void CellContent(RenderTreeBuilder builder, TGridItem item)
        => builder.AddContent(0, _cellTextFunc!(item));
}
