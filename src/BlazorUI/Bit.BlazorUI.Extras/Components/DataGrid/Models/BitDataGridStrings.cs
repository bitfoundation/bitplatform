namespace Bit.BlazorUI;

/// <summary>
/// All user-visible (and screen-reader-visible) strings rendered by <see cref="BitDataGrid{TItem}"/>.
/// Assign a customized instance to the grid's <c>Strings</c> parameter to localize it; every property
/// defaults to the English text. Properties whose name ends in <c>Format</c> are
/// <see cref="string.Format(string, object?[])"/> templates.
/// </summary>
public class BitDataGridStrings
{
    /// <summary>Shown when the grid has no rows to display.</summary>
    public string EmptyText { get; set; } = "No records to display.";

    /// <summary>Shown while <c>Loading</c> is true.</summary>
    public string LoadingText { get; set; } = "Loading…";

    /// <summary>Header of the command (Edit/Delete) column.</summary>
    public string ActionsText { get; set; } = "Actions";

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

    public string EditText { get; set; } = "Edit";
    public string DeleteText { get; set; } = "Delete";
    public string SaveText { get; set; } = "Save";
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

    /// <summary>Filter operator option texts.</summary>
    public string FilterOpContains { get; set; } = "Contains";
    public string FilterOpDoesNotContain { get; set; } = "Doesn't contain";
    public string FilterOpStartsWith { get; set; } = "Starts with";
    public string FilterOpEndsWith { get; set; } = "Ends with";
    public string FilterOpEquals { get; set; } = "=";
    public string FilterOpNotEquals { get; set; } = "≠";
    public string FilterOpGreaterThan { get; set; } = ">";
    public string FilterOpGreaterThanOrEqual { get; set; } = "≥";
    public string FilterOpLessThan { get; set; } = "<";
    public string FilterOpLessThanOrEqual { get; set; } = "≤";

    public string BooleanTrueText { get; set; } = "True";
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

    public string FirstPageLabel { get; set; } = "First page";
    public string PreviousPageLabel { get; set; } = "Previous page";
    public string NextPageLabel { get; set; } = "Next page";
    public string LastPageLabel { get; set; } = "Last page";

    /// <summary>Shown after the last batch in infinite-scrolling mode.</summary>
    public string EndOfResultsText { get; set; } = "— End of results —";

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

    /// <summary>Screen-reader announcements pushed to the grid's aria-live region. {0} = column title
    /// (or page number for <see cref="AnnouncementPage"/>).</summary>
    public string AnnouncementSortedAscending { get; set; } = "Sorted by {0}, ascending";
    public string AnnouncementSortedDescending { get; set; } = "Sorted by {0}, descending";
    public string AnnouncementSortCleared { get; set; } = "Sorting by {0} removed";
    public string AnnouncementFiltered { get; set; } = "Filter applied on {0}";
    public string AnnouncementFilterCleared { get; set; } = "Filter on {0} cleared";
    /// <summary>{0} = current page, {1} = total pages.</summary>
    public string AnnouncementPage { get; set; } = "Page {0} of {1}";
    /// <summary>Announced after a row is deleted (via the Delete button or the Delete key).</summary>
    public string AnnouncementRowDeleted { get; set; } = "Row deleted";

    /// <summary>Footer aggregate labels. {0} = the formatted aggregate value.</summary>
    public string AggregateSumFormat { get; set; } = "Σ {0}";
    public string AggregateAverageFormat { get; set; } = "avg {0}";
    public string AggregateCountFormat { get; set; } = "count {0}";
    public string AggregateMinFormat { get; set; } = "min {0}";
    public string AggregateMaxFormat { get; set; } = "max {0}";
}
