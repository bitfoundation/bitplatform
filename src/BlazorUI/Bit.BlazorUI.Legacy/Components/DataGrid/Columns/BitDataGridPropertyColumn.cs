using System.Linq.Expressions;

namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents a <see cref="BitDataGrid{TGridItem}"/> column whose cells display a single value.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
/// <typeparam name="TProp">The type of the value being displayed in the column's cells.</typeparam>
public class BitDataGridPropertyColumn<TGridItem, TProp> : BitDataGridColumnBase<TGridItem>, IBitDataGridSortBuilderColumn<TGridItem>
{
    private Expression<Func<TGridItem, TProp>>? _lastAssignedProperty;
    private string? _lastAssignedFormat;
    private bool _titleWasExplicitlySet;
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
    public override Task SetParametersAsync(ParameterView parameters)
    {
        // Track whether Title was supplied explicitly by the consumer in *this* render's ParameterView
        // rather than inferring intent from value equality (which can't tell an auto-derived header apart
        // from an explicit one matching the member name). Recompute it every render instead of latching:
        // if a consumer later removes Title, explicitness must drop back to false so the auto-generated
        // header can return.
        _titleWasExplicitlySet = parameters.TryGetValue<string?>(nameof(Title), out _);

        // When the consumer stops passing Title, the incoming ParameterView no longer contains it, so
        // base.SetParametersAsync leaves the previously assigned (explicit) value in place. Clear it up
        // front so a removed Title can't linger as a stale header; OnParametersSet then re-derives the
        // auto-generated title from the Property when possible.
        if (!_titleWasExplicitlySet)
        {
            Title = null;
        }

        return base.SetParametersAsync(parameters);
    }


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
            _sortBuilder = BitDataGridSort<TGridItem>.ByAscending(Property);

            // Only record the assignments after the formatter has been built and validated, so a failed
            // Format/TProp validation above doesn't suppress a retry on the next parameters set (which
            // would leave _cellTextFunc in a stale or null state).
            _lastAssignedProperty = Property;
            _lastAssignedFormat = Format;
        }

        if (_titleWasExplicitlySet)
        {
            // The consumer supplied Title this render; base.SetParametersAsync already applied it, so
            // there is nothing more to do.
        }
        else if (Property.Body is MemberExpression memberExpression)
        {
            // No explicit Title this render, so derive the header from the member name. Recomputed every
            // render, this also lets the auto-generated header follow Property changes and reappear after
            // a previously explicit Title is removed.
            Title = memberExpression.Member.Name;
        }
        // else: Property is a method/cast expression (no member name to derive a header from) and no
        // explicit Title was supplied. SetParametersAsync already cleared any stale value, so the column
        // simply has no header.
    }

    /// <inheritdoc />
    protected internal override void CellContent(RenderTreeBuilder builder, TGridItem item)
        => builder.AddContent(0, _cellTextFunc!(item));
}
