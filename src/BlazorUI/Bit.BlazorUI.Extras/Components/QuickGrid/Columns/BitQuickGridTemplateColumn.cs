namespace Bit.BlazorUI;

/// <summary>
/// Represents a <see cref="BitQuickGrid{TGridItem}"/> column whose cells render a supplied template.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
public class BitQuickGridTemplateColumn<TGridItem> : BitQuickGridColumnBase<TGridItem>, IBitQuickGridSortBuilderColumn<TGridItem>
{
    private readonly static RenderFragment<TGridItem> EmptyChildContent = _ => builder => { };

    /// <summary>
    /// Specifies the content to be rendered for each row in the table.
    /// </summary>
    [Parameter] public RenderFragment<TGridItem> ChildContent { get; set; } = EmptyChildContent;

    /// <summary>
    /// Optionally specifies sorting rules for this column.
    /// </summary>
    [Parameter] public BitQuickGridSort<TGridItem>? SortBy { get; set; }

    BitQuickGridSort<TGridItem>? IBitQuickGridSortBuilderColumn<TGridItem>.SortBuilder => SortBy;

    /// <inheritdoc />
    protected internal override void CellContent(RenderTreeBuilder builder, TGridItem item)
        => builder.AddContent(0, ChildContent(item));

    /// <inheritdoc />
    protected override bool IsSortableByDefault()
        => SortBy is not null;
}
