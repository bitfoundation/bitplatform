using System.Linq.Expressions;

namespace Bit.BlazorUI;

/// <summary>
/// Represents a <see cref="BitQuickGrid{TGridItem}"/> column whose cells display a single value.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
/// <typeparam name="TProp">The type of the value being displayed in the column's cells.</typeparam>
public class BitQuickGridPropertyColumn<TGridItem, TProp> : BitQuickGridColumnBase<TGridItem>, IBitQuickGridSortBuilderColumn<TGridItem>
{
    private Expression<Func<TGridItem, TProp>>? _lastAssignedProperty;
    private string? _lastAssignedFormat;
    private string? _autoTitle;
    private Func<TGridItem, string?>? _cellTextFunc;
    private BitQuickGridSort<TGridItem>? _sortBuilder;

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

    BitQuickGridSort<TGridItem>? IBitQuickGridSortBuilderColumn<TGridItem>.SortBuilder => _sortBuilder;


    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // We have to do a bit of pre-processing on the lambda expression. Only do that if the Property
        // or the Format has changed, so a Format-only change still rebuilds the cell formatter.
        if (_lastAssignedProperty != Property || _lastAssignedFormat != Format)
        {
            var compiledPropertyExpression = Property.Compile();
            Func<TGridItem, string?> cellTextFunc;

            if (Format.HasValue())
            {
                // For a nullable value type (e.g. int?, DateTime?) Nullable<T> itself does not implement
                // IFormattable, but its underlying type does and a boxed non-null value formats correctly.
                // Check the underlying type so Format is allowed on nullable columns too.
                var formattableType = Nullable.GetUnderlyingType(typeof(TProp)) ?? typeof(TProp);
                if (typeof(IFormattable).IsAssignableFrom(formattableType))
                {
                    cellTextFunc = item => ((IFormattable?)compiledPropertyExpression!(item))?.ToString(Format, null);
                }
                else
                {
                    throw new InvalidOperationException($"A '{nameof(Format)}' parameter was supplied, but the type '{typeof(TProp)}' does not implement '{typeof(IFormattable)}'.");
                }
            }
            else
            {
                cellTextFunc = item => compiledPropertyExpression!(item)?.ToString();
            }

            _cellTextFunc = cellTextFunc;
            _sortBuilder = BitQuickGridSort<TGridItem>.ByAscending(Property);

            // Only record the assignments after the formatter has been built and validated, so a failed
            // Format/TProp validation above doesn't suppress a retry on the next parameters set (which
            // would leave _cellTextFunc in a stale or null state).
            _lastAssignedProperty = Property;
            _lastAssignedFormat = Format;
        }

        if (Property.Body is MemberExpression memberExpression)
        {
            // Auto-derive the header from the member name unless the consumer set Title explicitly. A Title
            // still equal to the previously derived name is treated as auto-managed, so a changed Property
            // replaces the old member name instead of leaving a stale header.
            var derived = memberExpression.Member.Name;
            if (Title is null || Title == _autoTitle)
            {
                Title = derived;
            }
            _autoTitle = derived;
        }
    }

    /// <inheritdoc />
    protected internal override void CellContent(RenderTreeBuilder builder, TGridItem item)
        => builder.AddContent(0, _cellTextFunc!(item));
}
