namespace Bit.BlazorUI;

/// <summary>
/// All user-visible (and screen-reader-visible) strings rendered by <see cref="BitDataGrid{TItem}"/>.
/// Assign a customized instance to the grid's <c>Strings</c> parameter to localize it; every property
/// defaults to the English text. Properties whose name ends in <c>Format</c> are
/// <see cref="string.Format(string, object?[])"/> templates.
/// </summary>
public class BitDataGridStrings
{
    /// <summary>
    /// Default accessible name of the grid itself (its <c>role="grid"</c>, <c>"treegrid"</c> or <c>"table"</c> element), used when no
    /// <c>AriaLabel</c> is given. A page with several grids should name each one through
    /// <c>AriaLabel</c> instead, so screen-reader users can tell them apart.
    /// </summary>
    public string GridLabel { get; set; } = "Data grid";

    /// <summary>Shown when the grid has no rows to display. Defaults to <see cref="BitStrings.NoItemsFound"/>, read
    /// from <see cref="BitStrings.Current"/> each time it is shown until a text of its own is set.</summary>
    public string EmptyText { get => _emptyText ?? BitStrings.Current.NoItemsFound; set => _emptyText = value; }
    private string? _emptyText;

    /// <summary>Shown instead of <see cref="EmptyText"/> when there are rows, but the filters or the search exclude them all.
    /// Defaults to <see cref="BitStrings.NoResultsFound"/>, read from <see cref="BitStrings.Current"/> each time it is
    /// shown until a text of its own is set.</summary>
    public string NoMatchesText { get => _noMatchesText ?? BitStrings.Current.NoResultsFound; set => _noMatchesText = value; }
    private string? _noMatchesText;

    /// <summary>Shown while <c>Loading</c> is true, and while a server grid (<c>OnRead</c>) with no rows yet reads its
    /// first ones or reads them again after a failure.</summary>
    public string LoadingText { get; set; } = "Loading…";

    /// <summary>Shown in place of the rows when a read through <c>OnRead</c> failed and the app handles that
    /// (<c>OnReadError</c>), and told to screen readers then.</summary>
    public string ReadErrorText { get; set; } = "The records could not be loaded.";

    /// <summary>The button beside <see cref="ReadErrorText"/> that reads the rows again.</summary>
    public string RetryText { get; set; } = "Retry";

    /// <summary>Header of the command (Edit/Delete) column.</summary>
    public string ActionsText { get; set; } = "Actions";

    /// <summary>Visible header glyph of the row-number column.</summary>
    public string RowNumberHeader { get; set; } = "#";

    /// <summary>Accessible name of the row-number column's header (the "#" glyph reads as nothing).</summary>
    public string RowNumberLabel { get; set; } = "Row number";

    /// <summary>Toolbar button that begins adding a new row.</summary>
    public string AddRowText { get; set; } = "+ Add";

    /// <summary>Toolbar button that clears all active filters.</summary>
    public string ClearFiltersText { get; set; } = "Clear filters";

    /// <summary>Toolbar button that downloads the CSV export.</summary>
    public string ExportCsvText { get; set; } = "Export CSV";

    /// <summary>Toolbar button that downloads the Excel (.xlsx) export.</summary>
    public string ExportExcelText { get; set; } = "Export Excel";

    /// <summary>Toolbar button that opens the column chooser.</summary>
    public string ColumnsText { get; set; } = "Columns";

    /// <summary>Accessible label of the column-chooser panel.</summary>
    public string ColumnChooserLabel { get; set; } = "Choose columns";

    /// <summary>Placeholder of the toolbar's quick-search box.</summary>
    public string SearchPlaceholder { get; set; } = "Search…";

    /// <summary>Accessible label of the toolbar's quick-search box.</summary>
    public string SearchLabel { get; set; } = "Search all columns";

    /// <summary>Accessible label of the button that clears the quick search.</summary>
    public string ClearSearchLabel { get; set; } = "Clear search";

    /// <summary>Tooltip of a resizable column's drag handle.</summary>
    public string ResizeColumnTitle { get; set; } = "Drag or use the arrow keys to resize; double-click or press Enter to fit the content";

    /// <summary>Accessible label of a resizable column's resize handle. {0} = column title.</summary>
    public string ResizeColumnFormat { get; set; } = "Resize {0}";

    /// <summary>Accessible label of the column chooser button that moves a column one place toward the start. {0} = column title.</summary>
    public string MoveColumnEarlierFormat { get; set; } = "Move {0} earlier";

    /// <summary>Accessible label of the column chooser button that moves a column one place toward the end. {0} = column title.</summary>
    public string MoveColumnLaterFormat { get; set; } = "Move {0} later";

    /// <summary>Spoken after a column header in a multi-column sort. {0} = the column's place in the sort order.</summary>
    public string SortPriorityFormat { get; set; } = "sort priority {0}";

    /// <summary>Accessible name of the header of the row drag handle column.</summary>
    public string ReorderColumnHeaderLabel { get; set; } = "Reorder";

    /// <summary>Accessible name of the header of the detail toggle column.</summary>
    public string DetailsColumnHeaderLabel { get; set; } = "Details";

    /// <summary>Command column button that starts editing the row.</summary>
    public string EditText { get; set; } = "Edit";

    /// <summary>Command column button that deletes the row.</summary>
    public string DeleteText { get; set; } = "Delete";

    /// <summary>Command column button that saves the row being edited.</summary>
    public string SaveText { get; set; } = "Save";

    /// <summary>Command column button that drops the changes to the row being edited.</summary>
    public string CancelText { get; set; } = "Cancel";

    /// <summary>Accessible label of the select-all checkbox.</summary>
    public string SelectAllLabel { get; set; } = "Select all";

    /// <summary>Accessible label of a row checkbox when no row context is available.</summary>
    public string SelectRowLabel { get; set; } = "Select row";

    /// <summary>Accessible label of a row checkbox. {0} = the row's first visible cell text.</summary>
    public string SelectRowFormat { get; set; } = "Select row: {0}";

    /// <summary>Placeholder of the text filter input.</summary>
    public string FilterPlaceholder { get; set; } = "Filter…";

    /// <summary>Accessible label of a column filter editor. {0} = column title.</summary>
    public string FilterByFormat { get; set; } = "Filter by {0}";

    /// <summary>"All" option of boolean/enum filter selects.</summary>
    public string FilterAllText { get; set; } = "All";

    /// <summary>Accessible label of a column's filter-operator dropdown. {0} = column title.</summary>
    public string FilterOperatorLabel { get; set; } = "Filter operator for {0}";

    /// <summary>The <c>Contains</c> option of a column's filter-operator dropdown.</summary>
    public string FilterOpContains { get; set; } = "Contains";

    /// <summary>The <c>DoesNotContain</c> option of a column's filter-operator dropdown.</summary>
    public string FilterOpDoesNotContain { get; set; } = "Doesn't contain";

    /// <summary>The <c>StartsWith</c> option of a column's filter-operator dropdown.</summary>
    public string FilterOpStartsWith { get; set; } = "Starts with";

    /// <summary>The <c>EndsWith</c> option of a column's filter-operator dropdown.</summary>
    public string FilterOpEndsWith { get; set; } = "Ends with";

    /// <summary>The <c>Equals</c> option of a column's filter-operator dropdown.</summary>
    public string FilterOpEquals { get; set; } = "=";

    /// <summary>The <c>NotEquals</c> option of a column's filter-operator dropdown.</summary>
    public string FilterOpNotEquals { get; set; } = "≠";

    /// <summary>The <c>GreaterThan</c> option of a column's filter-operator dropdown.</summary>
    public string FilterOpGreaterThan { get; set; } = ">";

    /// <summary>The <c>GreaterThanOrEqual</c> option of a column's filter-operator dropdown.</summary>
    public string FilterOpGreaterThanOrEqual { get; set; } = "≥";

    /// <summary>The <c>LessThan</c> option of a column's filter-operator dropdown.</summary>
    public string FilterOpLessThan { get; set; } = "<";

    /// <summary>The <c>LessThanOrEqual</c> option of a column's filter-operator dropdown.</summary>
    public string FilterOpLessThanOrEqual { get; set; } = "≤";

    /// <summary>The <c>IsEmpty</c> option of a column's filter-operator dropdown.</summary>
    public string FilterOpIsEmpty { get; set; } = "Is blank";

    /// <summary>The <c>IsNotEmpty</c> option of a column's filter-operator dropdown.</summary>
    public string FilterOpIsNotEmpty { get; set; } = "Is not blank";

    /// <summary>The <c>In</c> option of a column's filter-operator dropdown.</summary>
    public string FilterOpIn { get; set; } = "Is any of";

    /// <summary>The <c>NotIn</c> option of a column's filter-operator dropdown.</summary>
    public string FilterOpNotIn { get; set; } = "Is none of";

    /// <summary>Placeholder of the filter input while an "is any of" / "is none of" operator is selected.</summary>
    public string FilterListPlaceholder { get; set; } = "a, b, c…";

    /// <summary>The <c>true</c> option of a boolean column's filter and editor, unless the column's <c>ValueText</c>
    /// writes one of its own.</summary>
    public string BooleanTrueText { get; set; } = "True";

    /// <summary>The <c>false</c> option of a boolean column's filter and editor, unless the column's <c>ValueText</c>
    /// writes one of its own.</summary>
    public string BooleanFalseText { get; set; } = "False";

    /// <summary>Group-by header button label. {0} = column title.</summary>
    public string GroupByFormat { get; set; } = "Group by {0}";

    /// <summary>Ungroup header button label. {0} = column title.</summary>
    public string UngroupByFormat { get; set; } = "Ungroup by {0}";

    /// <summary>Accessible label of a collapsed group's toggle. {0} = column title, {1} = group key.</summary>
    public string ExpandGroupFormat { get; set; } = "Expand group {0}: {1}";

    /// <summary>Accessible label of an expanded group's toggle. {0} = column title, {1} = group key.</summary>
    public string CollapseGroupFormat { get; set; } = "Collapse group {0}: {1}";

    /// <summary>Pager range summary. {0} = first row number, {1} = last row number, {2} = total rows.</summary>
    public string PagerRangeFormat { get; set; } = "{0}–{1} of {2}";

    /// <summary>Pager page summary. {0} = current page, {1} = total pages.</summary>
    public string PagerPageFormat { get; set; } = "Page {0} of {1}";

    /// <summary>Page-size option text. {0} = the page size.</summary>
    public string PerPageFormat { get; set; } = "{0} / page";

    /// <summary>Accessible label of the page-size select.</summary>
    public string RowsPerPageLabel { get; set; } = "Rows per page";

    /// <summary>Accessible label of the pager button that goes to the first page.</summary>
    public string FirstPageLabel { get; set; } = "First page";

    /// <summary>Accessible label of the pager button that goes to the previous page.</summary>
    public string PreviousPageLabel { get; set; } = "Previous page";

    /// <summary>Accessible label of the pager button that goes to the next page.</summary>
    public string NextPageLabel { get; set; } = "Next page";

    /// <summary>Accessible label of the pager button that goes to the last page.</summary>
    public string LastPageLabel { get; set; } = "Last page";

    /// <summary>Shown after the last batch in infinite-scrolling mode.</summary>
    public string EndOfResultsText { get; set; } = "- End of results -";

    /// <summary>Accessible label of the detail-row toggle.</summary>
    public string ToggleDetailsLabel { get; set; } = "Toggle details";

    /// <summary>Accessible label of a tree node's expand/collapse toggle.</summary>
    public string ToggleChildrenLabel { get; set; } = "Toggle children";

    /// <summary>Tooltip of the row drag handle.</summary>
    public string ReorderRowTitle { get; set; } = "Drag to reorder, or focus and use the arrow keys to move this row";

    /// <summary>Accessible label of the row drag handle.</summary>
    public string ReorderRowLabel { get; set; } = "Reorder row. Press Arrow Up or Arrow Down to move this row.";

    /// <summary>Error shown when an edited value can't be converted to the column's type. {0} = column title.</summary>
    public string InvalidValueError { get; set; } = "Invalid value for {0}.";

    // The Announcement* texts are what the grid tells screen readers through its aria-live region.

    /// <summary>Announced when a column is sorted ascending. {0} = column title.</summary>
    public string AnnouncementSortedAscending { get; set; } = "Sorted by {0}, ascending";

    /// <summary>Announced when a column is sorted descending. {0} = column title.</summary>
    public string AnnouncementSortedDescending { get; set; } = "Sorted by {0}, descending";

    /// <summary>Announced when a column's sort is removed. {0} = column title.</summary>
    public string AnnouncementSortCleared { get; set; } = "Sorting by {0} removed";

    /// <summary>Announced when a column's filter is applied, with the number of rows it leaves
    /// (<see cref="AnnouncementResultsFormat"/>) unless the grid is a tree or scrolls infinitely. {0} = column
    /// title.</summary>
    public string AnnouncementFiltered { get; set; } = "Filter applied on {0}";

    /// <summary>Announced when a column's filter is cleared, with the number of rows there are then
    /// (<see cref="AnnouncementResultsFormat"/>) unless the grid is a tree or scrolls infinitely. {0} = column
    /// title.</summary>
    public string AnnouncementFilterCleared { get; set; } = "Filter on {0} cleared";

    /// <summary>Announced when every column filter is cleared at once (the toolbar's Clear filters
    /// button, or ClearFiltersAsync).</summary>
    public string AnnouncementFiltersCleared { get; set; } = "All filters cleared";
    /// <summary>Announced when every sort is removed at once (ClearSortsAsync).</summary>
    public string AnnouncementSortsCleared { get; set; } = "Sorting cleared";
    /// <summary>Announced when every grouping is removed at once (ClearGroupsAsync).</summary>
    public string AnnouncementGroupsCleared { get; set; } = "Grouping cleared";
    /// <summary>Announced after a bulk selection change. {0} = the number of selected rows.</summary>
    public string AnnouncementRowsSelected { get; set; } = "{0} rows selected";
    /// <summary>Announced when the selection is cleared.</summary>
    public string AnnouncementSelectionCleared { get; set; } = "Selection cleared";
    /// <summary>{0} = current page, {1} = total pages.</summary>
    public string AnnouncementPage { get; set; } = "Page {0} of {1}";
    /// <summary>Announced after a row is deleted (via the Delete button or the Delete key).</summary>
    public string AnnouncementRowDeleted { get; set; } = "Row deleted";
    /// <summary>{0} = the search term.</summary>
    public string AnnouncementSearched { get; set; } = "Searching for {0}";

    /// <summary>Announced when the quick search is cleared.</summary>
    public string AnnouncementSearchCleared { get; set; } = "Search cleared";

    /// <summary>A filter or search announcement with the number of rows it leaves. {0} = the announcement, {1} = row count.</summary>
    public string AnnouncementResultsFormat { get; set; } = "{0}. Rows: {1}";
    /// <summary>{0} = column title.</summary>
    public string AnnouncementGrouped { get; set; } = "Grouped by {0}";
    /// <summary>{0} = column title.</summary>
    public string AnnouncementUngrouped { get; set; } = "Grouping by {0} removed";
    /// <summary>Announced after a clipboard copy. {0} = the number of rows copied.</summary>
    public string AnnouncementRowsCopied { get; set; } = "{0} rows copied to the clipboard";
    /// <summary>Announced after a column is moved from the keyboard or the column chooser. {0} = column title, {1} = its new 1-based position, {2} = the number of columns.</summary>
    public string AnnouncementColumnMoved { get; set; } = "{0} moved to position {1} of {2}";

    /// <summary>Footer label of a column's sum. {0} = the formatted value.</summary>
    public string AggregateSumFormat { get; set; } = "Σ {0}";

    /// <summary>Footer label of a column's average. {0} = the formatted value.</summary>
    public string AggregateAverageFormat { get; set; } = "avg {0}";

    /// <summary>Footer label of a column's count. {0} = the formatted value.</summary>
    public string AggregateCountFormat { get; set; } = "count {0}";

    /// <summary>Footer label of a column's minimum. {0} = the formatted value.</summary>
    public string AggregateMinFormat { get; set; } = "min {0}";

    /// <summary>Footer label of a column's maximum. {0} = the formatted value.</summary>
    public string AggregateMaxFormat { get; set; } = "max {0}";
}
