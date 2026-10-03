namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitDataGrid{TItem}"/> component.
/// </summary>
/// <remarks>
/// The grid is generic over its row type, but the parameters worth sharing between the grids of an app are not: the data,
/// the key and children selectors, the selection, the templates typed over TItem and the event callbacks stay on the
/// instance, which is what keeps this object usable from a single non-generic <see cref="BitParams"/> list. So do the
/// choices that belong to one data set rather than to the app - Editable, the selection mode, row reordering,
/// virtualization, the toolbar template and the export file name.
/// </remarks>
public class BitDataGridParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitDataGrid{TItem}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitDataGrid value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitDataGrid<object>)}";



    public string Name => ParamName;



    /// <summary>
    /// Whether a third click on a sortable header removes its sort (ascending, descending, unsorted) instead of cycling between the two directions.
    /// </summary>
    public bool? AllowUnsorted { get; set; }

    /// <summary>
    /// Draws the outer border and the lines between columns.
    /// </summary>
    public bool? Bordered { get; set; }

    /// <summary>
    /// Makes the data cells one roving tab stop that the arrow, Home/End and Page keys move between (the APG grid pattern).
    /// </summary>
    public bool? CellNavigation { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the grid.
    /// </summary>
    public BitDataGridClassStyles? Classes { get; set; }

    /// <summary>
    /// Lets Ctrl/Cmd+C on a focused cell copy the selected rows (or the focused one) as tab-separated text.
    /// </summary>
    public bool? ClipboardCopy { get; set; }

    /// <summary>
    /// The content shown in place of the rows when there are none.
    /// </summary>
    public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    /// Bakes the grid's rendered colors and fonts into the Excel export.
    /// </summary>
    public bool? ExcelExportStyled { get; set; }

    /// <summary>
    /// Lets a click anywhere on a row toggle its detail content.
    /// </summary>
    public bool? ExpandDetailOnRowClick { get; set; }

    /// <summary>
    /// How long, in milliseconds, a text or number filter box waits after the last keystroke before it applies.
    /// </summary>
    public int? FilterDebounce { get; set; }

    /// <summary>
    /// Adds an operator dropdown beside each column filter.
    /// </summary>
    public bool? FilterOperators { get; set; }

    /// <summary>
    /// Renders the filter row under the column headers.
    /// </summary>
    public bool? Filterable { get; set; }

    /// <summary>
    /// Adds a group-by toggle to the column headers.
    /// </summary>
    public bool? Groupable { get; set; }

    /// <summary>
    /// Whether groups start collapsed.
    /// </summary>
    public bool? GroupsInitiallyCollapsed { get; set; }

    /// <summary>
    /// The height of the scroll viewport, e.g. "480px".
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// Highlights the row under the pointer.
    /// </summary>
    public bool? Hoverable { get; set; }

    /// <summary>
    /// The number of rows fetched per batch in infinite-scrolling mode.
    /// </summary>
    public int? LoadMoreBatchSize { get; set; }

    /// <summary>
    /// The content shown in place of the rows while the grid is loading.
    /// </summary>
    public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Lets Ctrl/Cmd or Shift+click add a column to the sort instead of replacing it.
    /// </summary>
    public bool? MultiSort { get; set; }

    /// <summary>
    /// The number of rows per page.
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// The page sizes the pager's dropdown offers.
    /// </summary>
    public int[]? PageSizeOptions { get; set; }

    /// <summary>
    /// Splits the rows into pages and renders the pager.
    /// </summary>
    public bool? Pageable { get; set; }

    /// <summary>
    /// Where the pager renders: above, below or on both sides of the rows.
    /// </summary>
    public BitDataGridPagerPosition? PagerPosition { get; set; }

    /// <summary>
    /// Lets the columns be reordered by dragging their headers.
    /// </summary>
    public bool? Reorderable { get; set; }

    /// <summary>
    /// Lets the columns be resized by dragging the edge of their headers.
    /// </summary>
    public bool? Resizable { get; set; }

    /// <summary>
    /// The height of a row in pixels: its minimum height, and the exact one virtualization lays the rows out by.
    /// </summary>
    public float? RowHeight { get; set; }

    /// <summary>
    /// How long, in milliseconds, the search box waits after the last keystroke before it applies the term.
    /// </summary>
    public int? SearchDebounce { get; set; }

    /// <summary>
    /// Gives every data cell a native tooltip with its full text.
    /// </summary>
    public bool? ShowCellTooltips { get; set; }

    /// <summary>
    /// Renders the toolbar button that opens the column chooser.
    /// </summary>
    public bool? ShowColumnChooser { get; set; }

    /// <summary>
    /// Renders the toolbar button that exports the rows to CSV.
    /// </summary>
    public bool? ShowCsvExport { get; set; }

    /// <summary>
    /// Renders the leading column of detail toggles when a DetailTemplate is set.
    /// </summary>
    public bool? ShowDetailToggle { get; set; }

    /// <summary>
    /// Renders the toolbar button that exports the rows to Excel.
    /// </summary>
    public bool? ShowExcelExport { get; set; }

    /// <summary>
    /// Renders the footer row of column aggregates.
    /// </summary>
    public bool? ShowFooter { get; set; }

    /// <summary>
    /// Renders the header rows.
    /// </summary>
    public bool? ShowHeader { get; set; }

    /// <summary>
    /// Renders a leading column numbering the rows by their position in the whole dataset.
    /// </summary>
    public bool? ShowRowNumbers { get; set; }

    /// <summary>
    /// Renders the quick-search box in the toolbar.
    /// </summary>
    public bool? ShowSearchBox { get; set; }

    /// <summary>
    /// Renders the toolbar, which hosts the Clear filters button while a filter is on.
    /// </summary>
    public bool? ShowToolbar { get; set; }

    /// <summary>
    /// Lets the columns be sorted by clicking their headers.
    /// </summary>
    public bool? Sortable { get; set; }

    /// <summary>
    /// Every user-visible string the grid renders, which is how an app localizes all its grids at once.
    /// </summary>
    public BitDataGridStrings? Strings { get; set; }

    /// <summary>
    /// Shades every other row.
    /// </summary>
    public bool? Striped { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the grid.
    /// </summary>
    public BitDataGridClassStyles? Styles { get; set; }

    /// <summary>
    /// Whether the nodes of a tree grid start expanded.
    /// </summary>
    public bool? TreeInitiallyExpanded { get; set; }

    /// <summary>
    /// Lets long cell values wrap onto several lines instead of being clipped to one.
    /// </summary>
    public bool? WrapCellText { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitDataGrid{TItem}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitDataGrid{TItem}"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitDataGrid"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitDataGrid"/>.
    /// </remarks>
    /// <param name="bitDataGrid">
    /// The <see cref="BitDataGrid{TItem}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TItem>(BitDataGrid<TItem> bitDataGrid)
    {
        if (bitDataGrid is null) return;

        UpdateBaseParameters(bitDataGrid);

        if (AllowUnsorted.HasValue && bitDataGrid.HasNotBeenSet(nameof(AllowUnsorted)))
        {
            bitDataGrid.AllowUnsorted = AllowUnsorted.Value;
        }

        if (Bordered.HasValue && bitDataGrid.HasNotBeenSet(nameof(Bordered)))
        {
            bitDataGrid.Bordered = Bordered.Value;

            bitDataGrid.ClassBuilder.Reset();
        }

        if (CellNavigation.HasValue && bitDataGrid.HasNotBeenSet(nameof(CellNavigation)))
        {
            bitDataGrid.CellNavigation = CellNavigation.Value;
        }

        if (Classes is not null && bitDataGrid.HasNotBeenSet(nameof(Classes)))
        {
            bitDataGrid.Classes = Classes;

            bitDataGrid.ClassBuilder.Reset();
        }

        if (ClipboardCopy.HasValue && bitDataGrid.HasNotBeenSet(nameof(ClipboardCopy)))
        {
            bitDataGrid.ClipboardCopy = ClipboardCopy.Value;
        }

        if (EmptyTemplate is not null && bitDataGrid.HasNotBeenSet(nameof(EmptyTemplate)))
        {
            bitDataGrid.EmptyTemplate = EmptyTemplate;
        }

        if (ExcelExportStyled.HasValue && bitDataGrid.HasNotBeenSet(nameof(ExcelExportStyled)))
        {
            bitDataGrid.ExcelExportStyled = ExcelExportStyled.Value;
        }

        if (ExpandDetailOnRowClick.HasValue && bitDataGrid.HasNotBeenSet(nameof(ExpandDetailOnRowClick)))
        {
            bitDataGrid.ExpandDetailOnRowClick = ExpandDetailOnRowClick.Value;
        }

        if (FilterDebounce.HasValue && bitDataGrid.HasNotBeenSet(nameof(FilterDebounce)))
        {
            bitDataGrid.FilterDebounce = FilterDebounce.Value;
        }

        if (FilterOperators.HasValue && bitDataGrid.HasNotBeenSet(nameof(FilterOperators)))
        {
            bitDataGrid.FilterOperators = FilterOperators.Value;
        }

        if (Filterable.HasValue && bitDataGrid.HasNotBeenSet(nameof(Filterable)))
        {
            bitDataGrid.Filterable = Filterable.Value;
        }

        if (Groupable.HasValue && bitDataGrid.HasNotBeenSet(nameof(Groupable)))
        {
            bitDataGrid.Groupable = Groupable.Value;
        }

        if (GroupsInitiallyCollapsed.HasValue && bitDataGrid.HasNotBeenSet(nameof(GroupsInitiallyCollapsed)))
        {
            bitDataGrid.GroupsInitiallyCollapsed = GroupsInitiallyCollapsed.Value;
        }

        if (Height.HasValue() && bitDataGrid.HasNotBeenSet(nameof(Height)))
        {
            bitDataGrid.Height = Height;
        }

        if (Hoverable.HasValue && bitDataGrid.HasNotBeenSet(nameof(Hoverable)))
        {
            bitDataGrid.Hoverable = Hoverable.Value;

            bitDataGrid.ClassBuilder.Reset();
        }

        if (LoadMoreBatchSize.HasValue && bitDataGrid.HasNotBeenSet(nameof(LoadMoreBatchSize)))
        {
            bitDataGrid.LoadMoreBatchSize = LoadMoreBatchSize.Value;
        }

        if (LoadingTemplate is not null && bitDataGrid.HasNotBeenSet(nameof(LoadingTemplate)))
        {
            bitDataGrid.LoadingTemplate = LoadingTemplate;
        }

        if (MultiSort.HasValue && bitDataGrid.HasNotBeenSet(nameof(MultiSort)))
        {
            bitDataGrid.MultiSort = MultiSort.Value;
        }

        if (PageSize.HasValue && bitDataGrid.HasNotBeenSet(nameof(PageSize)))
        {
            bitDataGrid.PageSize = PageSize.Value;
        }

        if (PageSizeOptions is not null && bitDataGrid.HasNotBeenSet(nameof(PageSizeOptions)))
        {
            bitDataGrid.PageSizeOptions = PageSizeOptions;
        }

        if (Pageable.HasValue && bitDataGrid.HasNotBeenSet(nameof(Pageable)))
        {
            bitDataGrid.Pageable = Pageable.Value;
        }

        if (PagerPosition.HasValue && bitDataGrid.HasNotBeenSet(nameof(PagerPosition)))
        {
            bitDataGrid.PagerPosition = PagerPosition.Value;
        }

        if (Reorderable.HasValue && bitDataGrid.HasNotBeenSet(nameof(Reorderable)))
        {
            bitDataGrid.Reorderable = Reorderable.Value;
        }

        if (Resizable.HasValue && bitDataGrid.HasNotBeenSet(nameof(Resizable)))
        {
            bitDataGrid.Resizable = Resizable.Value;
        }

        if (RowHeight.HasValue && bitDataGrid.HasNotBeenSet(nameof(RowHeight)))
        {
            bitDataGrid.RowHeight = RowHeight.Value;
        }

        if (SearchDebounce.HasValue && bitDataGrid.HasNotBeenSet(nameof(SearchDebounce)))
        {
            bitDataGrid.SearchDebounce = SearchDebounce.Value;
        }

        if (ShowCellTooltips.HasValue && bitDataGrid.HasNotBeenSet(nameof(ShowCellTooltips)))
        {
            bitDataGrid.ShowCellTooltips = ShowCellTooltips.Value;
        }

        if (ShowColumnChooser.HasValue && bitDataGrid.HasNotBeenSet(nameof(ShowColumnChooser)))
        {
            bitDataGrid.ShowColumnChooser = ShowColumnChooser.Value;
        }

        if (ShowCsvExport.HasValue && bitDataGrid.HasNotBeenSet(nameof(ShowCsvExport)))
        {
            bitDataGrid.ShowCsvExport = ShowCsvExport.Value;
        }

        if (ShowDetailToggle.HasValue && bitDataGrid.HasNotBeenSet(nameof(ShowDetailToggle)))
        {
            bitDataGrid.ShowDetailToggle = ShowDetailToggle.Value;
        }

        if (ShowExcelExport.HasValue && bitDataGrid.HasNotBeenSet(nameof(ShowExcelExport)))
        {
            bitDataGrid.ShowExcelExport = ShowExcelExport.Value;
        }

        if (ShowFooter.HasValue && bitDataGrid.HasNotBeenSet(nameof(ShowFooter)))
        {
            bitDataGrid.ShowFooter = ShowFooter.Value;
        }

        if (ShowHeader.HasValue && bitDataGrid.HasNotBeenSet(nameof(ShowHeader)))
        {
            bitDataGrid.ShowHeader = ShowHeader.Value;
        }

        if (ShowRowNumbers.HasValue && bitDataGrid.HasNotBeenSet(nameof(ShowRowNumbers)))
        {
            bitDataGrid.ShowRowNumbers = ShowRowNumbers.Value;
        }

        if (ShowSearchBox.HasValue && bitDataGrid.HasNotBeenSet(nameof(ShowSearchBox)))
        {
            bitDataGrid.ShowSearchBox = ShowSearchBox.Value;
        }

        if (ShowToolbar.HasValue && bitDataGrid.HasNotBeenSet(nameof(ShowToolbar)))
        {
            bitDataGrid.ShowToolbar = ShowToolbar.Value;
        }

        if (Sortable.HasValue && bitDataGrid.HasNotBeenSet(nameof(Sortable)))
        {
            bitDataGrid.Sortable = Sortable.Value;
        }

        if (Strings is not null && bitDataGrid.HasNotBeenSet(nameof(Strings)))
        {
            bitDataGrid.Strings = Strings;
        }

        if (Striped.HasValue && bitDataGrid.HasNotBeenSet(nameof(Striped)))
        {
            bitDataGrid.Striped = Striped.Value;

            bitDataGrid.ClassBuilder.Reset();
        }

        if (Styles is not null && bitDataGrid.HasNotBeenSet(nameof(Styles)))
        {
            bitDataGrid.Styles = Styles;

            bitDataGrid.StyleBuilder.Reset();
        }

        if (TreeInitiallyExpanded.HasValue && bitDataGrid.HasNotBeenSet(nameof(TreeInitiallyExpanded)))
        {
            bitDataGrid.TreeInitiallyExpanded = TreeInitiallyExpanded.Value;
        }

        if (WrapCellText.HasValue && bitDataGrid.HasNotBeenSet(nameof(WrapCellText)))
        {
            bitDataGrid.WrapCellText = WrapCellText.Value;
        }
    }
}
