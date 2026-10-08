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
    public BitPlacement? PagerPlacement { get; set; }

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

        if (AllowUnsorted.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(AllowUnsorted), AllowUnsorted.Value, static d => d.AllowUnsorted, static (d, v) => d.AllowUnsorted = v);
        }

        if (Bordered.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(Bordered), Bordered.Value, static d => d.Bordered, static (d, v) => d.Bordered = v);
        }

        if (CellNavigation.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(CellNavigation), CellNavigation.Value, static d => d.CellNavigation, static (d, v) => d.CellNavigation = v);
        }

        if (Classes is not null)
        {
            bitDataGrid.TakeFromCascade(nameof(Classes), Classes, static d => d.Classes, static (d, v) => d.Classes = v);
        }

        if (ClipboardCopy.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(ClipboardCopy), ClipboardCopy.Value, static d => d.ClipboardCopy, static (d, v) => d.ClipboardCopy = v);
        }

        if (EmptyTemplate is not null)
        {
            bitDataGrid.TakeFromCascade(nameof(EmptyTemplate), EmptyTemplate, static d => d.EmptyTemplate, static (d, v) => d.EmptyTemplate = v);
        }

        if (ExcelExportStyled.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(ExcelExportStyled), ExcelExportStyled.Value, static d => d.ExcelExportStyled, static (d, v) => d.ExcelExportStyled = v);
        }

        if (ExpandDetailOnRowClick.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(ExpandDetailOnRowClick), ExpandDetailOnRowClick.Value, static d => d.ExpandDetailOnRowClick, static (d, v) => d.ExpandDetailOnRowClick = v);
        }

        if (FilterDebounce.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(FilterDebounce), FilterDebounce.Value, static d => d.FilterDebounce, static (d, v) => d.FilterDebounce = v);
        }

        if (FilterOperators.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(FilterOperators), FilterOperators.Value, static d => d.FilterOperators, static (d, v) => d.FilterOperators = v);
        }

        if (Filterable.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(Filterable), Filterable.Value, static d => d.Filterable, static (d, v) => d.Filterable = v);
        }

        if (Groupable.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(Groupable), Groupable.Value, static d => d.Groupable, static (d, v) => d.Groupable = v);
        }

        if (GroupsInitiallyCollapsed.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(GroupsInitiallyCollapsed), GroupsInitiallyCollapsed.Value, static d => d.GroupsInitiallyCollapsed, static (d, v) => d.GroupsInitiallyCollapsed = v);
        }

        if (Height.HasValue())
        {
            bitDataGrid.TakeFromCascade(nameof(Height), Height, static d => d.Height, static (d, v) => d.Height = v);
        }

        if (Hoverable.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(Hoverable), Hoverable.Value, static d => d.Hoverable, static (d, v) => d.Hoverable = v);
        }

        if (LoadMoreBatchSize.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(LoadMoreBatchSize), LoadMoreBatchSize.Value, static d => d.LoadMoreBatchSize, static (d, v) => d.LoadMoreBatchSize = v);
        }

        if (LoadingTemplate is not null)
        {
            bitDataGrid.TakeFromCascade(nameof(LoadingTemplate), LoadingTemplate, static d => d.LoadingTemplate, static (d, v) => d.LoadingTemplate = v);
        }

        if (MultiSort.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(MultiSort), MultiSort.Value, static d => d.MultiSort, static (d, v) => d.MultiSort = v);
        }

        if (PageSize.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(PageSize), PageSize.Value, static d => d.PageSize, static (d, v) => d.PageSize = v);
        }

        if (PageSizeOptions is not null)
        {
            bitDataGrid.TakeFromCascade(nameof(PageSizeOptions), PageSizeOptions, static d => d.PageSizeOptions, static (d, v) => d.PageSizeOptions = v);
        }

        if (Pageable.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(Pageable), Pageable.Value, static d => d.Pageable, static (d, v) => d.Pageable = v);
        }

        if (PagerPlacement.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(PagerPlacement), PagerPlacement.Value, static d => d.PagerPlacement, static (d, v) => d.PagerPlacement = v);
        }

        if (Reorderable.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(Reorderable), Reorderable.Value, static d => d.Reorderable, static (d, v) => d.Reorderable = v);
        }

        if (Resizable.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(Resizable), Resizable.Value, static d => d.Resizable, static (d, v) => d.Resizable = v);
        }

        if (RowHeight.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(RowHeight), RowHeight.Value, static d => d.RowHeight, static (d, v) => d.RowHeight = v);
        }

        if (SearchDebounce.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(SearchDebounce), SearchDebounce.Value, static d => d.SearchDebounce, static (d, v) => d.SearchDebounce = v);
        }

        if (ShowCellTooltips.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(ShowCellTooltips), ShowCellTooltips.Value, static d => d.ShowCellTooltips, static (d, v) => d.ShowCellTooltips = v);
        }

        if (ShowColumnChooser.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(ShowColumnChooser), ShowColumnChooser.Value, static d => d.ShowColumnChooser, static (d, v) => d.ShowColumnChooser = v);
        }

        if (ShowCsvExport.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(ShowCsvExport), ShowCsvExport.Value, static d => d.ShowCsvExport, static (d, v) => d.ShowCsvExport = v);
        }

        if (ShowDetailToggle.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(ShowDetailToggle), ShowDetailToggle.Value, static d => d.ShowDetailToggle, static (d, v) => d.ShowDetailToggle = v);
        }

        if (ShowExcelExport.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(ShowExcelExport), ShowExcelExport.Value, static d => d.ShowExcelExport, static (d, v) => d.ShowExcelExport = v);
        }

        if (ShowFooter.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(ShowFooter), ShowFooter.Value, static d => d.ShowFooter, static (d, v) => d.ShowFooter = v);
        }

        if (ShowHeader.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(ShowHeader), ShowHeader.Value, static d => d.ShowHeader, static (d, v) => d.ShowHeader = v);
        }

        if (ShowRowNumbers.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(ShowRowNumbers), ShowRowNumbers.Value, static d => d.ShowRowNumbers, static (d, v) => d.ShowRowNumbers = v);
        }

        if (ShowSearchBox.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(ShowSearchBox), ShowSearchBox.Value, static d => d.ShowSearchBox, static (d, v) => d.ShowSearchBox = v);
        }

        if (ShowToolbar.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(ShowToolbar), ShowToolbar.Value, static d => d.ShowToolbar, static (d, v) => d.ShowToolbar = v);
        }

        if (Sortable.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(Sortable), Sortable.Value, static d => d.Sortable, static (d, v) => d.Sortable = v);
        }

        if (Strings is not null)
        {
            bitDataGrid.TakeFromCascade(nameof(Strings), Strings, static d => d.Strings, static (d, v) => d.Strings = v);
        }

        if (Striped.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(Striped), Striped.Value, static d => d.Striped, static (d, v) => d.Striped = v);
        }

        if (Styles is not null)
        {
            bitDataGrid.TakeFromCascade(nameof(Styles), Styles, static d => d.Styles, static (d, v) => d.Styles = v);
        }

        if (TreeInitiallyExpanded.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(TreeInitiallyExpanded), TreeInitiallyExpanded.Value, static d => d.TreeInitiallyExpanded, static (d, v) => d.TreeInitiallyExpanded = v);
        }

        if (WrapCellText.HasValue)
        {
            bitDataGrid.TakeFromCascade(nameof(WrapCellText), WrapCellText.Value, static d => d.WrapCellText, static (d, v) => d.WrapCellText = v);
        }
    }
}
