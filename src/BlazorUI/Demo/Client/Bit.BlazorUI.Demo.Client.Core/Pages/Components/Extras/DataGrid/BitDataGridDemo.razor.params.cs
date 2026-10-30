namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.DataGrid;

public partial class BitDataGridDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new() { Name = "Items", Type = "IEnumerable<TItem>?", DefaultValue = "null", Description = "The client-side data source. An IQueryable<T> (e.g. an EF Core DbSet) has its filtering, sorting and paging executed at the source." },
        new() { Name = "OnRead", Type = "Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TItem>>>?", DefaultValue = "null", Description = "Server-side data callback; the grid delegates sort/filter/page/group to it.", LinkType = LinkType.Link, Href = "#BitDataGridReadRequest" },
        new() { Name = "OnLoadMore", Type = "Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TItem>>>?", DefaultValue = "null", Description = "Infinite-scrolling callback, appending a batch as the user scrolls to the end. Exports request all rows with Take = null, so honor a null Take.", LinkType = LinkType.Link, Href = "#BitDataGridReadRequest" },
        new() { Name = "LoadMoreBatchSize", Type = "int", DefaultValue = "50", Description = "Rows fetched per batch in infinite-scrolling mode." },
        new() { Name = "ChildContent", Type = "RenderFragment?", DefaultValue = "null", Description = "Column definitions and other children." },
        new() { Name = "Columns", Type = "RenderFragment?", DefaultValue = "null", Description = "Alias of ChildContent for the column definitions (<Columns>...</Columns>)." },
        new() { Name = "Loading", Type = "bool", DefaultValue = "false", Description = "Shows a loading row (LoadingTemplate, or a spinner) in place of the rows and marks the grid aria-busy." },
        new() { Name = "ShowSearchBox", Type = "bool", DefaultValue = "false", Description = "Renders a toolbar quick-search box filtering the rows across every searchable column, ignoring case. Sent as BitDataGridReadRequest.Search in server/infinite modes; not available on a lazy tree (ChildrenProvider)." },
        new() { Name = "SearchText", Type = "string?", DefaultValue = "null", Description = "The quick-search term (two-way bindable)." },
        new() { Name = "SearchTextChanged", Type = "EventCallback<string?>", DefaultValue = "", Description = "Raised when the quick-search term changes." },
        new() { Name = "SearchDebounce", Type = "int", DefaultValue = "300", Description = "Delay (ms) after the last keystroke before the search applies; 0 applies each keystroke." },
        new() { Name = "KeyField", Type = "Func<TItem, object>?", DefaultValue = "null", Description = "Key selector for selection/edit identity. Defaults to reference equality." },
        new() { Name = "ChildrenSelector", Type = "Func<TItem, IEnumerable<TItem>?>?", DefaultValue = "null", Description = "Child selector that turns the grid into a tree grid." },
        new() { Name = "ChildrenProvider", Type = "Func<TItem, Task<IEnumerable<TItem>?>>?", DefaultValue = "null", Description = "Async children loader of a lazy tree grid: a node's children load on its first expand and are cached. Use instead of ChildrenSelector, with HasChildrenSelector." },
        new() { Name = "HasChildrenSelector", Type = "Func<TItem, bool>?", DefaultValue = "null", Description = "Whether an unloaded lazy node can have children, so it renders an expand toggle. ChildrenProvider only." },
        new() { Name = "TreeInitiallyExpanded", Type = "bool", DefaultValue = "false", Description = "Whether the tree nodes start expanded. Ignored in lazy mode." },
        new() { Name = "Height", Type = "string?", DefaultValue = "null", Description = "Height of the scroll viewport, e.g. \"480px\". Required for virtualization and infinite scrolling." },
        new() { Name = "Classes", Type = "BitDataGridClassStyles?", DefaultValue = "null", Description = "Custom CSS classes for the parts of the grid.", LinkType = LinkType.Link, Href = "#class-styles" },
        new() { Name = "Styles", Type = "BitDataGridClassStyles?", DefaultValue = "null", Description = "Custom CSS styles for the parts of the grid.", LinkType = LinkType.Link, Href = "#class-styles" },
        new() { Name = "Striped", Type = "bool", DefaultValue = "true", Description = "Renders alternate-row striping." },
        new() { Name = "Hoverable", Type = "bool", DefaultValue = "true", Description = "Highlights the row under the pointer." },
        new() { Name = "Bordered", Type = "bool", DefaultValue = "true", Description = "Draws the outer border and the column lines; the row lines are always drawn." },
        new() { Name = "ShowHeader", Type = "bool", DefaultValue = "true", Description = "Renders the header rows (titles, header groups and the filter row)." },
        new() { Name = "AriaLabelledBy", Type = "string?", DefaultValue = "null", Description = "Id of the element that names the grid, in place of AriaLabel." },
        new() { Name = "AriaDescribedBy", Type = "string?", DefaultValue = "null", Description = "Id of the element that describes the grid." },
        new() { Name = "ShowFooter", Type = "bool", DefaultValue = "false", Description = "Renders the footer/aggregate row." },
        new() { Name = "ShowRowNumbers", Type = "bool", DefaultValue = "false", Description = "Renders a leading gutter numbering the rows by their position in the whole dataset, across pages and batches. Never exported or copied." },
        new() { Name = "ShowCellTooltips", Type = "bool", DefaultValue = "false", Description = "Gives each value cell a native tooltip with its full text. Overridable per column (ShowTooltip); Template cells are excluded." },
        new() { Name = "WrapCellText", Type = "bool", DefaultValue = "false", Description = "Wraps long headers and values onto several lines instead of clipping them. Overridable per column (WrapText); ignored while virtualizing." },
        new() { Name = "RowClass", Type = "Func<TItem, string?>?", DefaultValue = "null", Description = "Per-row CSS class selector, appended after the grid's own row classes." },
        new() { Name = "RowStyle", Type = "Func<TItem, string?>?", DefaultValue = "null", Description = "Per-row inline style selector." },
        new() { Name = "Sortable", Type = "bool", DefaultValue = "true", Description = "Enables column sorting by clicking headers." },
        new() { Name = "MultiSort", Type = "bool", DefaultValue = "true", Description = "Enables multi-column sorting via Ctrl/⌘+click." },
        new() { Name = "AllowUnsorted", Type = "bool", DefaultValue = "true", Description = "Whether a third header click removes the sort (ascending → descending → unsorted). Overridable per column." },
        new() { Name = "Filterable", Type = "bool", DefaultValue = "false", Description = "Renders a per-column filter row. A tree is pruned to the matching branches; not available on a lazy tree." },
        new() { Name = "FilterOperators", Type = "bool", DefaultValue = "false", Description = "Shows an operator dropdown next to the text/number/date filters (contains, starts with, =, ≠, >, <, is blank, is any of, ...)." },
        new() { Name = "FilterDebounce", Type = "int", DefaultValue = "300", Description = "Delay (ms) before a text/number filter applies; 0 applies each keystroke, a negative value only Enter or blur." },
        new() { Name = "Strings", Type = "BitDataGridStrings", DefaultValue = "new()", Description = "All user-visible strings; assign a customized instance to localize.", LinkType = LinkType.Link, Href = "#BitDataGridStrings" },
        new() { Name = "Resizable", Type = "bool", DefaultValue = "false", Description = "Lets the columns be resized by dragging a header's edge, double-clicking it (fit), or with the keyboard on it." },
        new() { Name = "Reorderable", Type = "bool", DefaultValue = "false", Description = "Lets the columns be reordered by dragging a header, Ctrl+Left/Right, or the column chooser." },
        new() { Name = "Groupable", Type = "bool", DefaultValue = "false", Description = "Enables grouping via a header button on groupable columns." },
        new() { Name = "GroupsInitiallyCollapsed", Type = "bool", DefaultValue = "false", Description = "Groups start collapsed instead of expanded." },
        new() { Name = "ShowToolbar", Type = "bool", DefaultValue = "false", Description = "Renders the toolbar with its Clear filters button. Search, export, the column chooser, Add and ToolbarTemplate show it on their own." },
        new() { Name = "ShowColumnChooser", Type = "bool", DefaultValue = "false", Description = "Renders the column chooser button, to show, hide and (with Reorderable) move columns." },
        new() { Name = "ShowCsvExport", Type = "bool", DefaultValue = "false", Description = "Renders a CSV export button, exporting all matching rows in every data mode." },
        new() { Name = "ShowExcelExport", Type = "bool", DefaultValue = "false", Description = "Renders an Excel (.xlsx) export button. Built in-process, exporting all matching rows with the grid's layout (frozen header and columns, widths, merged ColSpan cells)." },
        new() { Name = "ExcelExportStyled", Type = "bool", DefaultValue = "false", Description = "Excel exports also carry the grid's rendered theme (colors, striping, borders, fonts), read from the DOM; plain styling without JS." },
        new() { Name = "ExportFileName", Type = "string?", DefaultValue = "null", Description = "Base name of the export files, e.g. \"orders\" for orders.csv. Defaults to \"export\"." },
        new() { Name = "CellNavigation", Type = "bool", DefaultValue = "false", Description = "Makes the data cells one tab stop: the arrows, Home/End and PageUp/PageDown move; Enter/F2 or typing edits; Esc cancels; Space selects (Shift+Space a range); Ctrl+A selects all; Delete deletes when Editable." },
        new() { Name = "ClipboardCopy", Type = "bool", DefaultValue = "false", Description = "Enables Ctrl/⌘+C on a focused cell (with CellNavigation) and CopyToClipboardAsync, copying the selected or focused rows as tab-separated text with a header." },
        new() { Name = "RowReorderable", Type = "bool", DefaultValue = "false", Description = "Enables drag-and-drop row reordering (pointer, or the drag handle's arrow keys)." },
        new() { Name = "OnRowReorder", Type = "EventCallback<BitDataGridRowReorderEventArgs<TItem>>", DefaultValue = "", Description = "Raised when a row is dropped onto another row.", LinkType = LinkType.Link, Href = "#BitDataGridRowReorderEventArgs" },
        new() { Name = "SelectionMode", Type = "BitSelectionMode", DefaultValue = "BitSelectionMode.None", Description = "None, Single (a row click or Space; turns CellNavigation on) or Multiple (a checkbox column).", LinkType = LinkType.Link, Href = "#selection-mode-enum" },
        new() { Name = "SelectedItems", Type = "IReadOnlyList<TItem>?", DefaultValue = "null", Description = "The selected items (two-way bindable)." },
        new() { Name = "SelectedItemsChanged", Type = "EventCallback<IReadOnlyList<TItem>>", DefaultValue = "", Description = "Raised when the selection changes." },
        new() { Name = "OnRowClick", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when a row is clicked. A click on a control in a cell (a button, link, field, ARIA widget, focusable or data-bit-interactive element, or anything in a dialog or grid in the cell) raises neither this nor OnCellClick, and selects or expands nothing. data-bit-interactive=\"false\" opts a control out." },
        new() { Name = "OnRowDoubleClick", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when a row is double-clicked, e.g. to open the record or BeginEdit; not on a control in a cell." },
        new() { Name = "OnCellClick", Type = "EventCallback<BitDataGridCellEventArgs<TItem>>", DefaultValue = "", Description = "Raised when a data cell is clicked; not on a control in it (see OnRowClick).", LinkType = LinkType.Link, Href = "#BitDataGridCellEventArgs" },
        new() { Name = "OnCellDoubleClick", Type = "EventCallback<BitDataGridCellEventArgs<TItem>>", DefaultValue = "", Description = "Raised when a data cell is double-clicked; not on a control in it.", LinkType = LinkType.Link, Href = "#BitDataGridCellEventArgs" },
        new() { Name = "OnCellContextMenu", Type = "EventCallback<BitDataGridCellEventArgs<TItem>>", DefaultValue = "", Description = "Raised when a data cell is right-clicked, suppressing the browser's menu; not on a control in it.", LinkType = LinkType.Link, Href = "#BitDataGridCellEventArgs" },
        new() { Name = "IsRowSelectionDisabled", Type = "Func<TItem, bool>?", DefaultValue = "null", Description = "Returns true for the rows that may not be selected." },
        new() { Name = "Pageable", Type = "bool", DefaultValue = "false", Description = "Enables paging with a pager UI." },
        new() { Name = "PageSize", Type = "int", DefaultValue = "20", Description = "The number of rows per page." },
        new() { Name = "PageSizeOptions", Type = "int[]", DefaultValue = "{ 10, 20, 50, 100 }", Description = "The page sizes offered in the pager." },
        new() { Name = "PagerPlacement", Type = "BitPlacement", DefaultValue = "BitPlacement.Bottom", Description = "Where the pager renders: Top, Bottom or TopAndBottom.", LinkType = LinkType.Link, Href = "#placement-enum" },
        new() { Name = "Virtualize", Type = "bool", DefaultValue = "false", Description = "Renders only the visible rows. Requires Height and RowHeight; server mode without paging fetches the rows as they scroll in. Ignores RowHeightSelector, WrapCellText and the height of expanded detail rows." },
        new() { Name = "RowHeight", Type = "float", DefaultValue = "36", Description = "Uniform row height in px (required when virtualizing)." },
        new() { Name = "RowHeightSelector", Type = "Func<TItem, float>?", DefaultValue = "null", Description = "Per-row height selector (ignored while virtualizing)." },
        new() { Name = "VirtualizeColumns", Type = "bool", DefaultValue = "false", Description = "Renders only the columns near the horizontal viewport. Requires px column widths; not applied with header groups or ColSpans." },
        new() { Name = "Editable", Type = "bool", DefaultValue = "false", Description = "Enables inline editing with a command column." },
        new() { Name = "EditMode", Type = "BitDataGridEditMode", DefaultValue = "BitDataGridEditMode.Row", Description = "Row edits the whole row with Save/Cancel; Cell edits one cell at a time (Enter, F2, double-click or typing opens; Enter, Tab or blur commits; Escape cancels).", LinkType = LinkType.Link, Href = "#BitDataGridEditMode" },
        new() { Name = "NewItemFactory", Type = "Func<TItem>?", DefaultValue = "null", Description = "Creates the new row of the toolbar Add button." },
        new() { Name = "OnRowSave", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when an edited row is saved." },
        new() { Name = "OnRowCancel", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when an edit is cancelled." },
        new() { Name = "OnRowDelete", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when a row is deleted (Delete button or Delete key)." },
        new() { Name = "OnRowCreate", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when a new row is created." },
        new() { Name = "EmptyTemplate", Type = "RenderFragment?", DefaultValue = "null", Description = "Custom content rendered when there is no data." },
        new() { Name = "LoadingTemplate", Type = "RenderFragment?", DefaultValue = "null", Description = "Custom content replacing the spinner while Loading." },
        new() { Name = "ToolbarTemplate", Type = "RenderFragment?", DefaultValue = "null", Description = "Custom content at the toolbar's start." },
        new() { Name = "DetailTemplate", Type = "RenderFragment<TItem>?", DefaultValue = "null", Description = "Expandable master-detail content rendered under a row." },
        new() { Name = "ShowDetailToggle", Type = "bool", DefaultValue = "true", Description = "Renders the expand/collapse column while a DetailTemplate is set. Turn it off to drive the details from row clicks or code." },
        new() { Name = "ExpandDetailOnRowClick", Type = "bool", DefaultValue = "false", Description = "Toggles a row's detail when the row is clicked (selecting it too with SelectionMode). Clicks on the grid's own cells or on a control in a cell are excluded." },
        new() { Name = "ExpandedDetailItems", Type = "IReadOnlyList<TItem>?", DefaultValue = "null", Description = "The rows whose detail is expanded (two-way bindable)." },
        new() { Name = "ExpandedDetailItemsChanged", Type = "EventCallback<IReadOnlyList<TItem>>", DefaultValue = "", Description = "Raised when the expanded detail rows change." },
        new() { Name = "OnDetailToggle", Type = "EventCallback<BitDataGridDetailEventArgs<TItem>>", DefaultValue = "", Description = "Raised when a row's detail is expanded or collapsed.", LinkType = LinkType.Link, Href = "#BitDataGridDetailEventArgs" },
        new() { Name = "OnSortChange", Type = "EventCallback<IReadOnlyList<BitDataGridSortDescriptor>>", DefaultValue = "", Description = "Raised with the new sorts whenever the sorting changes.", LinkType = LinkType.Link, Href = "#BitDataGridSortDescriptor" },
        new() { Name = "OnFilterChange", Type = "EventCallback<IReadOnlyList<BitDataGridFilterDescriptor>>", DefaultValue = "", Description = "Raised with the new filters whenever the filtering changes.", LinkType = LinkType.Link, Href = "#BitDataGridFilterDescriptor" },
        new() { Name = "OnGroupChange", Type = "EventCallback<IReadOnlyList<BitDataGridGroupDescriptor>>", DefaultValue = "", Description = "Raised with the new groups whenever the grouping changes.", LinkType = LinkType.Link, Href = "#BitDataGridGroupDescriptor" },
        new() { Name = "OnPageChange", Type = "EventCallback<int>", DefaultValue = "", Description = "Raised with the new 1-based page when the page or the page size changes." },
        new() { Name = "OnStateChange", Type = "EventCallback<BitDataGridState>", DefaultValue = "", Description = "Raised with a GetState snapshot after any state it captures changes (sorts, filters, search, groups, page, column layout), for persisting the view. Not raised by ApplyStateAsync.", LinkType = LinkType.Link, Href = "#BitDataGridState" },
        new() { Name = "OnViewChange", Type = "EventCallback<IReadOnlyList<TItem>>", DefaultValue = "", Description = "Raised with the rows of the view across every page (see GetViewItems) when the search, filters, sorts or Items change them, to keep a chart or a map in step. Rows are matched by KeyField. List, tree and IQueryable sources only; use GetViewItemsAsync in server/infinite modes." },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new() { Name = "RefreshAsync", Type = "Task", DefaultValue = "", Description = "Recomputes the view (filter, sort, group, page) and re-renders." },
        new() { Name = "SortByAsync", Type = "Task", DefaultValue = "", Description = "SortByAsync(columnId, direction, additive) - sorts by a column; BitDataGridSortDirection.None removes the sort." },
        new() { Name = "ClearSortsAsync", Type = "Task", DefaultValue = "", Description = "Removes all the sorts." },
        new() { Name = "ApplyFilterAsync", Type = "Task", DefaultValue = "", Description = "ApplyFilterAsync(columnId, operator, value) - applies a filter, replacing the column's existing one." },
        new() { Name = "ApplyRangeFilterAsync", Type = "Task", DefaultValue = "", Description = "ApplyRangeFilterAsync(columnId, from, toExclusive) - applies a half-open range filter (>= from AND < toExclusive) as two comparison filters." },
        new() { Name = "ClearFilterAsync", Type = "Task", DefaultValue = "", Description = "ClearFilterAsync(columnId) - removes a column's filters." },
        new() { Name = "ClearFiltersAsync", Type = "Task", DefaultValue = "", Description = "Removes all the column filters." },
        new() { Name = "GroupByAsync", Type = "Task", DefaultValue = "", Description = "GroupByAsync(columnId) - adds the column as the next grouping level." },
        new() { Name = "UngroupAsync", Type = "Task", DefaultValue = "", Description = "UngroupAsync(columnId) - removes the column's grouping level." },
        new() { Name = "ClearGroupsAsync", Type = "Task", DefaultValue = "", Description = "Removes all the groupings." },
        new() { Name = "ExpandAllGroupsAsync", Type = "Task", DefaultValue = "", Description = "Expands every group, including the ones built later." },
        new() { Name = "CollapseAllGroupsAsync", Type = "Task", DefaultValue = "", Description = "Collapses every group, including the ones built later." },
        new() { Name = "SearchAsync", Type = "Task", DefaultValue = "", Description = "SearchAsync(text) - applies the quick-search term (null clears it) and goes to the first page." },
        new() { Name = "ActiveSearch", Type = "string?", DefaultValue = "null", Description = "The active quick-search term, or null." },
        new() { Name = "SelectAllAsync", Type = "Task", DefaultValue = "", Description = "Selects every selectable row of the view (only the loaded ones in server, queryable and infinite modes). Multiple mode only." },
        new() { Name = "ClearSelectionAsync", Type = "Task", DefaultValue = "", Description = "Clears the row selection." },
        new() { Name = "CopyToClipboardAsync", Type = "Task<int>", DefaultValue = "", Description = "CopyToClipboardAsync() / CopyToClipboardAsync(fallbackRow) - copies the selected rows (or the fallback row) as tab-separated text and returns how many were copied; 0 without a clipboard." },
        new() { Name = "AutoFitColumnAsync", Type = "Task", DefaultValue = "", Description = "AutoFitColumnAsync(columnId) - sizes a column to its widest content." },
        new() { Name = "AutoFitAllColumnsAsync", Type = "Task", DefaultValue = "", Description = "Auto-fits every visible column." },
        new() { Name = "GoToPageAsync", Type = "Task", DefaultValue = "", Description = "GoToPageAsync(page) - goes to a 1-based page (clamped)." },
        new() { Name = "MoveColumnAsync", Type = "Task", DefaultValue = "", Description = "MoveColumnAsync(columnId, index) - moves a column to a 0-based position among all the columns." },
        new() { Name = "SetPageSizeAsync", Type = "Task", DefaultValue = "", Description = "SetPageSizeAsync(size) - changes the page size and goes to the first page." },
        new() { Name = "GetViewItems", Type = "IReadOnlyList<TItem>", DefaultValue = "", Description = "The rows of the view across every page, in display order. Server/infinite modes return only the loaded rows; use GetViewItemsAsync there." },
        new() { Name = "GetViewItemsAsync", Type = "Task<IReadOnlyList<TItem>>", DefaultValue = "", Description = "GetViewItemsAsync(cancellationToken) - the rows of the view across every page in every mode; server/infinite modes read them with an unpaged request (Take = null)." },
        new() { Name = "RevealItemAsync", Type = "Task<bool>", DefaultValue = "", Description = "RevealItemAsync(item) - pages to, expands to and scrolls to a row of the view; false when the row is not in it." },
        new() { Name = "GetState", Type = "BitDataGridState", DefaultValue = "", Description = "Captures the user-adjustable state (page, search, sorts, filters, groups, column layout) as a serializable snapshot.", LinkType = LinkType.Link, Href = "#BitDataGridState" },
        new() { Name = "ApplyStateAsync", Type = "Task", DefaultValue = "", Description = "ApplyStateAsync(state) - restores a GetState snapshot.", LinkType = LinkType.Link, Href = "#BitDataGridState" },
        new() { Name = "ExportCsvAsync", Type = "Task", DefaultValue = "", Description = "ExportCsvAsync(selectedOnly) - downloads all the matching rows (or the selected ones) as CSV, in every data mode." },
        new() { Name = "ToCsv", Type = "string", DefaultValue = "", Description = "ToCsv(selectedOnly) - builds the CSV synchronously; server/infinite modes cover only the loaded rows (use ToCsvAsync)." },
        new() { Name = "ToCsvAsync", Type = "Task<string>", DefaultValue = "", Description = "ToCsvAsync(selectedOnly) - builds the CSV of all the matching (or selected) rows in every mode." },
        new() { Name = "ToExcelAsync", Type = "Task<byte[]>", DefaultValue = "", Description = "ToExcelAsync(selectedOnly) - builds an .xlsx of all the matching (or selected) rows with native cell types and the grid's layout (and theme, with ExcelExportStyled)." },
        new() { Name = "ExportExcelAsync", Type = "Task", DefaultValue = "", Description = "ExportExcelAsync(selectedOnly) - builds the .xlsx and downloads it." },
        new() { Name = "ActiveSorts", Type = "IReadOnlyList<BitDataGridSortDescriptor>", DefaultValue = "[]", Description = "The active sorts, in priority order." },
        new() { Name = "ActiveFilters", Type = "IReadOnlyList<BitDataGridFilterDescriptor>", DefaultValue = "[]", Description = "The active filters." },
        new() { Name = "ActiveGroups", Type = "IReadOnlyList<BitDataGridGroupDescriptor>", DefaultValue = "[]", Description = "The active groups, in nesting order." },
        new() { Name = "TotalCount", Type = "int", DefaultValue = "0", Description = "The rows of the filtered view; the server's total in server mode." },
        new() { Name = "TotalPages", Type = "int", DefaultValue = "1", Description = "The number of pages while paging." },
        new() { Name = "CurrentPage", Type = "int", DefaultValue = "1", Description = "The 1-based current page." },
        new() { Name = "ExpandAllAsync", Type = "Task", DefaultValue = "", Description = "Expands every tree node (tree mode only)." },
        new() { Name = "CollapseAllAsync", Type = "Task", DefaultValue = "", Description = "Collapses every tree node (tree mode only)." },
        new() { Name = "IsDetailExpanded", Type = "bool", DefaultValue = "", Description = "IsDetailExpanded(item) - whether the row's detail is expanded." },
        new() { Name = "ExpandDetailAsync", Type = "Task", DefaultValue = "", Description = "ExpandDetailAsync(item) - expands the row's detail." },
        new() { Name = "CollapseDetailAsync", Type = "Task", DefaultValue = "", Description = "CollapseDetailAsync(item) - collapses the row's detail." },
        new() { Name = "ToggleDetailAsync", Type = "Task", DefaultValue = "", Description = "ToggleDetailAsync(item) - toggles the row's detail." },
        new() { Name = "SetDetailExpandedAsync", Type = "Task", DefaultValue = "", Description = "SetDetailExpandedAsync(item, expanded) - expands or collapses the row's detail." },
        new() { Name = "ExpandAllDetailsAsync", Type = "Task", DefaultValue = "", Description = "Expands the detail of every row of the view (only the loaded rows in server, queryable and infinite modes)." },
        new() { Name = "CollapseAllDetailsAsync", Type = "Task", DefaultValue = "", Description = "Collapses every expanded detail." },
        new() { Name = "EditingItem", Type = "TItem?", DefaultValue = "null", Description = "The row being edited, or null." },
        new() { Name = "EditingColumnId", Type = "string?", DefaultValue = "null", Description = "The column of the cell being edited in Cell mode, or null." },
        new() { Name = "BeginEditAsync", Type = "Task", DefaultValue = "", Description = "BeginEditAsync(item, columnId) - opens an edit at a cell and focuses its editor." },
        new() { Name = "BeginEdit", Type = "void", DefaultValue = "", Description = "BeginEdit(item) - puts a row into edit mode, as its Edit button does." },
        new() { Name = "CommitEditAsync", Type = "Task", DefaultValue = "", Description = "Saves the open edit and raises OnRowSave; refused while a value is invalid." },
        new() { Name = "CancelEditAsync", Type = "Task", DefaultValue = "", Description = "Abandons the open edit, restoring the row, and raises OnRowCancel." },
        new() { Name = "AddNewRowAsync", Type = "Task", DefaultValue = "", Description = "Adds a row from NewItemFactory and opens it for editing, as the toolbar Add button does." },
        new() { Name = "DeleteRowAsync", Type = "Task", DefaultValue = "", Description = "DeleteRowAsync(item) - deselects the row and raises OnRowDelete for the caller to remove it." },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-DataGrid-background", DefaultValue = "--bit-clr-bg-pri", Description = "Background of the grid and its rows." },
        new() { Name = "--bit-DataGrid-color", DefaultValue = "--bit-clr-fg-pri", Description = "Text color." },
        new() { Name = "--bit-DataGrid-font-family", DefaultValue = "--bit-tpg-font-family", Description = "Font of the grid." },
        new() { Name = "--bit-DataGrid-font-size", DefaultValue = "--bit-tpg-fs-sm", Description = "Font size of the grid." },
        new() { Name = "--bit-DataGrid-line-height", DefaultValue = "1.4", Description = "Line height of the cells." },
        new() { Name = "--bit-DataGrid-border-color", DefaultValue = "--bit-clr-brd-ter", Description = "Color of the border and of the row and column lines." },
        new() { Name = "--bit-DataGrid-border-radius", DefaultValue = "--bit-shp-radius-surface", Description = "Corner radius of the grid." },
        new() { Name = "--bit-DataGrid-cell-padding", DefaultValue = "spacing(1) spacing(1.25)", Description = "Padding of the cells." },
        new() { Name = "--bit-DataGrid-header-background", DefaultValue = "--bit-clr-bg-sec", Description = "Background of the header rows and the column chooser." },
        new() { Name = "--bit-DataGrid-header-color", DefaultValue = "The grid's color", Description = "Text color of the header rows." },
        new() { Name = "--bit-DataGrid-header-font-weight", DefaultValue = "--bit-tpg-fw-semibold", Description = "Font weight of the column titles." },
        new() { Name = "--bit-DataGrid-stripe-background", DefaultValue = "--bit-clr-bg-sec", Description = "Background of the striped rows." },
        new() { Name = "--bit-DataGrid-hover-background", DefaultValue = "--bit-clr-bg-pri-hover", Description = "Background of the hovered row." },
        new() { Name = "--bit-DataGrid-selected-background", DefaultValue = "--bit-clr-pri-tint", Description = "Wash over a selected row." },
        new() { Name = "--bit-DataGrid-selected-color", DefaultValue = "The grid's color", Description = "Text color of a selected row." },
        new() { Name = "--bit-DataGrid-editing-background", DefaultValue = "--bit-clr-wrn-tint", Description = "Wash over the row (or cell) being edited." },
        new() { Name = "--bit-DataGrid-accent-color", DefaultValue = "--bit-clr-pri", Description = "Sort arrows, badges, group toggle and bar, checkboxes and resize handle." },
        new() { Name = "--bit-DataGrid-focus-color", DefaultValue = "--bit-clr-pri-focus", Description = "Focus indicator of the cells, rows and controls." },
        new() { Name = "--bit-DataGrid-group-background", DefaultValue = "--bit-clr-bg-ter", Description = "Background of the group header rows." },
        new() { Name = "--bit-DataGrid-group-indent", DefaultValue = "spacing(2.5)", Description = "Indent of each group level." },
        new() { Name = "--bit-DataGrid-tree-indent", DefaultValue = "spacing(2.25)", Description = "Indent of each tree level." },
        new() { Name = "--bit-DataGrid-detail-background", DefaultValue = "--bit-clr-bg-sec", Description = "Background of the detail rows." },
        new() { Name = "--bit-DataGrid-footer-background", DefaultValue = "--bit-clr-bg-sec", Description = "Background of the footer row." },
        new() { Name = "--bit-DataGrid-disabled-color", DefaultValue = "--bit-clr-fg-dis", Description = "Text color of a disabled grid." },
        new() { Name = "--bit-DataGrid-placeholder-color", DefaultValue = "--bit-clr-fg-sec", Description = "Placeholder of the search and filter boxes." },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitDataGridClassStyles",
            Description = "Defines per-part CSS class/style values for BitDataGrid.",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom class or style of the root element." },
                new() { Name = "Toolbar", Type = "string?", DefaultValue = "null", Description = "Custom class or style of the toolbar." },
                new() { Name = "ColumnChooser", Type = "string?", DefaultValue = "null", Description = "Custom class or style of the column chooser." },
                new() { Name = "Viewport", Type = "string?", DefaultValue = "null", Description = "Custom class or style of the scrolling viewport." },
                new() { Name = "HeaderRow", Type = "string?", DefaultValue = "null", Description = "Custom class or style of the column titles row." },
                new() { Name = "HeaderCell", Type = "string?", DefaultValue = "null", Description = "Custom class or style of each header cell, before the column's HeaderClass." },
                new() { Name = "FilterRow", Type = "string?", DefaultValue = "null", Description = "Custom class or style of the filter row." },
                new() { Name = "Row", Type = "string?", DefaultValue = "null", Description = "Custom class or style of each data row, before RowClass/RowStyle." },
                new() { Name = "SelectedRow", Type = "string?", DefaultValue = "null", Description = "Custom class or style of each selected row, after Row." },
                new() { Name = "Cell", Type = "string?", DefaultValue = "null", Description = "Custom class or style of each data cell, before the column's CellClass." },
                new() { Name = "GroupRow", Type = "string?", DefaultValue = "null", Description = "Custom class or style of each group header cell." },
                new() { Name = "DetailRow", Type = "string?", DefaultValue = "null", Description = "Custom class or style of each detail row's content." },
                new() { Name = "FooterRow", Type = "string?", DefaultValue = "null", Description = "Custom class or style of the footer row." },
                new() { Name = "Pager", Type = "string?", DefaultValue = "null", Description = "Custom class or style of the pager." },
                new() { Name = "Empty", Type = "string?", DefaultValue = "null", Description = "Custom class or style of the empty message cell." },
                new() { Name = "Loading", Type = "string?", DefaultValue = "null", Description = "Custom class or style of the loading indicator cell." },
            ],
        },
        new()
        {
            Id = "BitDataGridColumn",
            Title = "BitDataGridColumn",
            Description = "A column of a BitDataGrid, placed in its child content.",
            Parameters =
            [
                new() { Name = "Field", Type = "string?", DefaultValue = "null", Description = "Name of the bound property; supports nested paths (\"Address.City\"). Prefer Property." },
                new() { Name = "Property", Type = "Expression<Func<TItem, object?>>?", DefaultValue = "null", Description = "Typed selector of the bound property, e.g. p => p.Address.City. Refactor-safe; takes precedence over Field." },
                new() { Name = "ColumnId", Type = "string?", DefaultValue = "null", Description = "Stable column identifier. Defaults to the Property/Field path." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "Header text. Defaults to the humanized Property/Field name." },
                new() { Name = "Width", Type = "string?", DefaultValue = "null", Description = "CSS width, e.g. \"120px\" or \"20%\"; null shares the remaining space." },
                new() { Name = "MinWidth", Type = "int", DefaultValue = "60", Description = "Minimum resize width in px." },
                new() { Name = "MaxWidth", Type = "int?", DefaultValue = "null", Description = "Maximum resize width in px." },
                new() { Name = "Sortable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid's Sortable. A template-only column sorts only through SortBy (in-memory sources) or ServerSortable (server/infinite modes)." },
                new() { Name = "SortBy", Type = "Func<TItem, object?>?", DefaultValue = "null", Description = "Custom sort key, making a template-only column sortable; in-memory sources only." },
                new() { Name = "ServerSortable", Type = "bool", DefaultValue = "false", Description = "Lets a template-only column sort in server/infinite modes, sending its ColumnId (required) in the request." },
                new() { Name = "SortDescendingFirst", Type = "bool", DefaultValue = "false", Description = "The first header click sorts descending." },
                new() { Name = "AllowUnsorted", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid's AllowUnsorted." },
                new() { Name = "Comparer", Type = "IComparer<object?>?", DefaultValue = "null", Description = "Custom comparer of the column's sort keys; client-side only." },
                new() { Name = "Validate", Type = "Func<TItem, object?, string?>?", DefaultValue = "null", Description = "Inline-edit validator: returns an error message to reject the value, or null to accept it." },
                new() { Name = "Filterable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid's Filterable." },
                new() { Name = "FilterOperators", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid's FilterOperators." },
                new() { Name = "FilterTemplate", Type = "RenderFragment<BitDataGridFilterContext>?", DefaultValue = "null", Description = "Custom filter editor; its context carries the filter and ApplyAsync/ApplyRangeAsync/ClearAsync.", LinkType = LinkType.Link, Href = "#BitDataGridFilterContext" },
                new() { Name = "Resizable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid's Resizable." },
                new() { Name = "Reorderable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid's Reorderable." },
                new() { Name = "Editable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid's Editable." },
                new() { Name = "Groupable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid's Groupable." },
                new() { Name = "Searchable", Type = "bool?", DefaultValue = "null", Description = "Whether the quick search covers the column. Defaults to the bound (or ExportValue) columns." },
                new() { Name = "Exportable", Type = "bool?", DefaultValue = "null", Description = "Whether exports include the column. Defaults to the columns with a value (a bound field or ExportValue)." },
                new() { Name = "ExportValue", Type = "Func<TItem, object?>?", DefaultValue = "null", Description = "The value exports and the clipboard use instead of the bound field." },
                new() { Name = "ShowTooltip", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid's ShowCellTooltips." },
                new() { Name = "WrapText", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid's WrapCellText." },
                new() { Name = "Frozen", Type = "bool", DefaultValue = "false", Description = "Pins the column to the start edge while scrolling horizontally." },
                new() { Name = "FrozenEnd", Type = "bool", DefaultValue = "false", Description = "Pins the column to the end edge." },
                new() { Name = "Group", Type = "string?", DefaultValue = "null", Description = "Header group name; consecutive columns sharing it render under one spanning header." },
                new() { Name = "ColSpan", Type = "Func<TItem, int?>?", DefaultValue = "null", Description = "Per-row column span." },
                new() { Name = "Visible", Type = "bool", DefaultValue = "true", Description = "Whether the column is visible." },
                new() { Name = "RowHeader", Type = "bool", DefaultValue = "false", Description = "Makes the cells the headers of their rows (role rowheader), naming the row for screen readers." },
                new() { Name = "Align", Type = "BitTextAlign", DefaultValue = "BitTextAlign.Start", Description = "Horizontal alignment of the cells: Start, Center, End, Left or Right; any other value is Start.", LinkType = LinkType.Link, Href = "#BitTextAlign" },
                new() { Name = "Format", Type = "string?", DefaultValue = "null", Description = "A .NET format string, e.g. \"C2\" or \"yyyy-MM-dd\"." },
                new() { Name = "DataType", Type = "BitDataGridColumnDataType", DefaultValue = "BitDataGridColumnDataType.Auto", Description = "The data type picking the editor and filter.", LinkType = LinkType.Link, Href = "#BitDataGridColumnDataType" },
                new() { Name = "Aggregate", Type = "BitDataGridAggregateType", DefaultValue = "BitDataGridAggregateType.None", Description = "The footer/group aggregate function.", LinkType = LinkType.Link, Href = "#BitDataGridAggregateType" },
                new() { Name = "AggregateBy", Type = "Func<IReadOnlyList<TItem>, object?>?", DefaultValue = "null", Description = "Custom aggregate function (e.g. a distinct count); takes precedence over Aggregate." },
                new() { Name = "AggregateFormat", Type = "string?", DefaultValue = "null", Description = "Format of the aggregate value; falls back to Format." },
                new() { Name = "HeaderClass", Type = "string?", DefaultValue = "null", Description = "CSS class of the header cell." },
                new() { Name = "CellClass", Type = "string?", DefaultValue = "null", Description = "CSS class of each data cell." },
                new() { Name = "CellClassSelector", Type = "Func<TItem, string?>?", DefaultValue = "null", Description = "Per-row CSS class of the cells, after CellClass." },
                new() { Name = "CellStyleSelector", Type = "Func<TItem, string?>?", DefaultValue = "null", Description = "Per-row inline style of the cells. Prefer color/font to background, which hides selection and hover." },
                new() { Name = "Template", Type = "RenderFragment<TItem>?", DefaultValue = "null", Description = "Custom data cell content." },
                new() { Name = "HeaderTemplate", Type = "RenderFragment?", DefaultValue = "null", Description = "Custom header cell content." },
                new() { Name = "EditTemplate", Type = "RenderFragment<TItem>?", DefaultValue = "null", Description = "Custom editor in edit mode." },
                new() { Name = "FooterTemplate", Type = "RenderFragment<BitDataGridAggregateResult>?", DefaultValue = "null", Description = "Custom footer/aggregate cell content.", LinkType = LinkType.Link, Href = "#BitDataGridAggregateResult" },
            ],
        },
        new()
        {
            Id = "BitDataGridReadRequest",
            Title = "BitDataGridReadRequest",
            Description = "What the grid asks of OnRead/OnLoadMore.",
            Parameters =
            [
                new() { Name = "Skip", Type = "int", DefaultValue = "0", Description = "Zero-based number of items to skip." },
                new() { Name = "Take", Type = "int?", DefaultValue = "null", Description = "Maximum number of items to return; null means all." },
                new() { Name = "Sorts", Type = "IReadOnlyList<BitDataGridSortDescriptor>", DefaultValue = "[]", Description = "The active sorts, by priority.", LinkType = LinkType.Link, Href = "#BitDataGridSortDescriptor" },
                new() { Name = "Filters", Type = "IReadOnlyList<BitDataGridFilterDescriptor>", DefaultValue = "[]", Description = "The active filters.", LinkType = LinkType.Link, Href = "#BitDataGridFilterDescriptor" },
                new() { Name = "Groups", Type = "IReadOnlyList<BitDataGridGroupDescriptor>", DefaultValue = "[]", Description = "The active groups, in nesting order." },
                new() { Name = "Search", Type = "string?", DefaultValue = "null", Description = "The quick-search term to match across the searchable columns, or null." },
                new() { Name = "CancellationToken", Type = "CancellationToken", DefaultValue = "", Description = "Cancelled when a newer request supersedes this one." },
            ],
        },
        new()
        {
            Id = "BitDataGridReadResult",
            Title = "BitDataGridReadResult<TItem>",
            Description = "The result of OnRead/OnLoadMore.",
            Parameters =
            [
                new() { Name = "Items", Type = "IReadOnlyList<TItem>", DefaultValue = "", Description = "The items of the current page/window." },
                new() { Name = "TotalCount", Type = "int", DefaultValue = "", Description = "The total of the matching items (ignored in infinite mode)." },
                new() { Name = "Aggregates", Type = "IReadOnlyList<BitDataGridAggregateResult>?", DefaultValue = "null", Description = "Aggregates over the whole filtered data, shown in the footer instead of the page's own.", LinkType = LinkType.Link, Href = "#BitDataGridAggregateResult" },
            ],
        },
        new()
        {
            Id = "BitDataGridCellEventArgs",
            Title = "BitDataGridCellEventArgs<TItem>",
            Description = "Arguments of the cell events.",
            Parameters =
            [
                new() { Name = "Item", Type = "TItem", DefaultValue = "", Description = "The row item." },
                new() { Name = "Column", Type = "BitDataGridColumn<TItem>", DefaultValue = "", Description = "The cell's column.", LinkType = LinkType.Link, Href = "#BitDataGridColumn" },
                new() { Name = "ColumnId", Type = "string", DefaultValue = "", Description = "The column's identifier." },
                new() { Name = "ColumnTitle", Type = "string", DefaultValue = "", Description = "The column's title." },
                new() { Name = "Value", Type = "object?", DefaultValue = "null", Description = "The cell's raw value." },
                new() { Name = "Mouse", Type = "MouseEventArgs", DefaultValue = "", Description = "The browser mouse event." },
            ],
        },
        new()
        {
            Id = "BitDataGridDetailEventArgs",
            Title = "BitDataGridDetailEventArgs<TItem>",
            Description = "Arguments of a row's detail toggle.",
            Parameters =
            [
                new() { Name = "Item", Type = "TItem", DefaultValue = "", Description = "The toggled row." },
                new() { Name = "Expanded", Type = "bool", DefaultValue = "", Description = "Whether the detail was expanded." },
            ],
        },
        new()
        {
            Id = "BitDataGridRowReorderEventArgs",
            Title = "BitDataGridRowReorderEventArgs<TItem>",
            Description = "Arguments of a row drop.",
            Parameters =
            [
                new() { Name = "DraggedItem", Type = "TItem", DefaultValue = "", Description = "The dragged row." },
                new() { Name = "TargetItem", Type = "TItem", DefaultValue = "", Description = "The drop-target row." },
                new() { Name = "FromIndex", Type = "int?", DefaultValue = "", Description = "The original index, or null when Items is not an indexable list." },
                new() { Name = "ToIndex", Type = "int?", DefaultValue = "", Description = "The destination index, or null when Items is not an indexable list." },
            ],
        },
        new()
        {
            Id = "BitDataGridSortDescriptor",
            Title = "BitDataGridSortDescriptor",
            Description = "A column's sort (in BitDataGridReadRequest.Sorts).",
            Parameters =
            [
                new() { Name = "ColumnId", Type = "string", DefaultValue = "", Description = "The sorted column's identifier." },
                new() { Name = "Direction", Type = "BitDataGridSortDirection", DefaultValue = "BitDataGridSortDirection.Ascending", Description = "The sort direction.", LinkType = LinkType.Link, Href = "#BitDataGridSortDirection" },
                new() { Name = "Priority", Type = "int", DefaultValue = "int.MaxValue", Description = "Multi-sort priority (1 = primary)." },
            ],
        },
        new()
        {
            Id = "BitDataGridFilterDescriptor",
            Title = "BitDataGridFilterDescriptor",
            Description = "A column's filter (in BitDataGridReadRequest.Filters).",
            Parameters =
            [
                new() { Name = "ColumnId", Type = "string", DefaultValue = "", Description = "The filtered column's identifier." },
                new() { Name = "Operator", Type = "BitDataGridFilterOperator", DefaultValue = "BitDataGridFilterOperator.Unspecified", Description = "The comparison operator; Unspecified applies no filter.", LinkType = LinkType.Link, Href = "#BitDataGridFilterOperator" },
                new() { Name = "Value", Type = "object?", DefaultValue = "null", Description = "The operand; a collection for In/NotIn." },
            ],
        },
        new()
        {
            Id = "BitDataGridFilterContext",
            Title = "BitDataGridFilterContext",
            Description = "The context of a column's FilterTemplate: its filter and the calls that change it.",
            Parameters =
            [
                new() { Name = "ColumnId", Type = "string", DefaultValue = "", Description = "The filtered column's identifier." },
                new() { Name = "Title", Type = "string", DefaultValue = "", Description = "The column's header text." },
                new() { Name = "Label", Type = "string", DefaultValue = "", Description = "The editor's accessible name (\"Filter by {Title}\")." },
                new() { Name = "ValueType", Type = "Type?", DefaultValue = "null", Description = "The bound member's type, Nullable<T> unwrapped." },
                new() { Name = "Disabled", Type = "bool", DefaultValue = "", Description = "Whether the grid is disabled." },
                new() { Name = "Filters", Type = "IReadOnlyList<BitDataGridFilterDescriptor>", DefaultValue = "", Description = "The column's filters: none, one, or a range's two halves.", LinkType = LinkType.Link, Href = "#BitDataGridFilterDescriptor" },
                new() { Name = "IsActive", Type = "bool", DefaultValue = "", Description = "Whether the column is filtered." },
                new() { Name = "Operator", Type = "BitDataGridFilterOperator", DefaultValue = "", Description = "The first filter's operator, or Unspecified.", LinkType = LinkType.Link, Href = "#BitDataGridFilterOperator" },
                new() { Name = "Value", Type = "object?", DefaultValue = "", Description = "The first filter's value, or null." },
                new() { Name = "ApplyAsync", Type = "Task (BitDataGridFilterOperator, object?)", DefaultValue = "", Description = "Replaces the filter; an empty value clears it." },
                new() { Name = "ApplyRangeAsync", Type = "Task (object?, object?)", DefaultValue = "", Description = "Replaces the filter with a half-open range (>= from AND < toExclusive)." },
                new() { Name = "ClearAsync", Type = "Task ()", DefaultValue = "", Description = "Removes the filter." },
            ],
        },
        new()
        {
            Id = "BitDataGridGroupDescriptor",
            Title = "BitDataGridGroupDescriptor",
            Description = "A column's grouping.",
            Parameters =
            [
                new() { Name = "ColumnId", Type = "string", DefaultValue = "", Description = "The grouped column's identifier." },
                new() { Name = "Direction", Type = "BitDataGridSortDirection", DefaultValue = "BitDataGridSortDirection.Ascending", Description = "The sort direction of the group keys.", LinkType = LinkType.Link, Href = "#BitDataGridSortDirection" },
            ],
        },
        new()
        {
            Id = "BitDataGridAggregateResult",
            Title = "BitDataGridAggregateResult",
            Description = "A computed footer or group aggregate (passed to FooterTemplate).",
            Parameters =
            [
                new() { Name = "ColumnId", Type = "string", DefaultValue = "", Description = "The aggregated column's identifier." },
                new() { Name = "Type", Type = "BitDataGridAggregateType", DefaultValue = "", Description = "The aggregate function.", LinkType = LinkType.Link, Href = "#BitDataGridAggregateType" },
                new() { Name = "Value", Type = "object?", DefaultValue = "null", Description = "The raw value." },
                new() { Name = "FormattedValue", Type = "string", DefaultValue = "string.Empty", Description = "The value formatted with AggregateFormat/Format." },
            ],
        },
        new()
        {
            Id = "BitDataGridState",
            Title = "BitDataGridState",
            Description = "A serializable snapshot of the user-adjustable state, from GetState() for ApplyStateAsync().",
            Parameters =
            [
                new() { Name = "CurrentPage", Type = "int", DefaultValue = "1", Description = "The 1-based current page." },
                new() { Name = "PageSize", Type = "int?", DefaultValue = "null", Description = "The user-chosen page size, or null for the PageSize parameter." },
                new() { Name = "Search", Type = "string?", DefaultValue = "null", Description = "The quick-search term, or null." },
                new() { Name = "Sorts", Type = "List<BitDataGridSortDescriptor>", DefaultValue = "[]", Description = "The active sorts.", LinkType = LinkType.Link, Href = "#BitDataGridSortDescriptor" },
                new() { Name = "Filters", Type = "List<BitDataGridFilterDescriptor>", DefaultValue = "[]", Description = "The active filters.", LinkType = LinkType.Link, Href = "#BitDataGridFilterDescriptor" },
                new() { Name = "Groups", Type = "List<BitDataGridGroupDescriptor>", DefaultValue = "[]", Description = "The active groups." },
                new() { Name = "GroupsCollapsed", Type = "bool", DefaultValue = "false", Description = "Whether the groups were collapsed by default." },
                new() { Name = "GroupExpansionOverrides", Type = "List<string>", DefaultValue = "[]", Description = "The paths of the groups whose expansion differs from GroupsCollapsed. Tree expansion is not captured." },
                new() { Name = "Columns", Type = "List<BitDataGridColumnState>", DefaultValue = "[]", Description = "The column layout.", LinkType = LinkType.Link, Href = "#BitDataGridColumnState" },
            ],
        },
        new()
        {
            Id = "BitDataGridColumnState",
            Title = "BitDataGridColumnState",
            Description = "A column's layout in BitDataGridState.Columns.",
            Parameters =
            [
                new() { Name = "ColumnId", Type = "string", DefaultValue = "string.Empty", Description = "The column's identifier." },
                new() { Name = "Visible", Type = "bool", DefaultValue = "true", Description = "Whether the column is shown." },
                new() { Name = "Width", Type = "double?", DefaultValue = "null", Description = "The resized width in px, or null." },
                new() { Name = "Order", Type = "int", DefaultValue = "0", Description = "The display position among all the columns." },
            ],
        },
        new()
        {
            Id = "BitDataGridStrings",
            Title = "BitDataGridStrings",
            Description = "All the user-visible and screen-reader strings of the grid, in English by default; assign a customized instance to Strings to localize.",
            Parameters =
            [
                new() { Name = "GridLabel", Type = "string", DefaultValue = "\"Data grid\"", Description = "The grid's accessible name when no AriaLabel is given." },
                new() { Name = "EmptyText", Type = "string", DefaultValue = "\"No records to display.\"", Description = "Shown when there are no rows." },
                new() { Name = "LoadingText", Type = "string", DefaultValue = "\"Loading…\"", Description = "Shown while Loading is true." },
                new() { Name = "PagerRangeFormat", Type = "string", DefaultValue = "\"{0}–{1} of {2}\"", Description = "Pager range summary format." },
                new() { Name = "PagerPageFormat", Type = "string", DefaultValue = "\"Page {0} of {1}\"", Description = "Pager page summary format." },
                new() { Name = "InvalidValueError", Type = "string", DefaultValue = "\"Invalid value for {0}.\"", Description = "Shown when an edited value can't be converted." },
                new() { Name = "SearchPlaceholder", Type = "string", DefaultValue = "\"Search…\"", Description = "The quick-search placeholder." },
                new() { Name = "…", Type = "string", DefaultValue = "", Description = "Plus 86 more: button texts, labels, placeholders, operator and option texts, tooltips, aggregate formats and live announcements." },
            ],
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "BitTextAlign",
            Name = "BitTextAlign",
            Description = "Horizontal alignment of cell content.",
            Items =
            [
                new() { Name = "Start", Description = "The leading edge, whatever the text direction.", Value = "0" },
                new() { Name = "End", Description = "The trailing edge, whatever the text direction.", Value = "1" },
                new() { Name = "Left", Description = "The left edge, whatever the text direction.", Value = "2" },
                new() { Name = "Right", Description = "The right edge, whatever the text direction.", Value = "3" },
                new() { Name = "Center", Description = "Centers the lines.", Value = "4" },
                new() { Name = "Justify", Description = "Lines up both edges of every line but the last.", Value = "5" },
                new() { Name = "JustifyAll", Description = "Justifies the last line too. Not implemented by browsers yet.", Value = "6" },
                new() { Name = "MatchParent", Description = "Inherits, resolving start/end against the parent's direction.", Value = "7" },
                new() { Name = "Inherit", Description = "Takes the parent's alignment.", Value = "8" },
                new() { Name = "Initial", Description = "Takes the initial value.", Value = "9" },
                new() { Name = "Revert", Description = "Reverts to the user agent or user stylesheet value.", Value = "10" },
                new() { Name = "RevertLayer", Description = "Reverts to the previous cascade layer's value.", Value = "11" },
                new() { Name = "Unset", Description = "Inherits, or takes the initial value where not inherited.", Value = "12" },
            ]
        },
        new()
        {
            Id = "BitDataGridSortDirection",
            Name = "BitDataGridSortDirection",
            Description = "Sort direction for a column.",
            Items =
            [
                new() { Name = "None", Value = "0", Description = "Not sorted. SortByAsync removes the sort with it; a group keeps its keys in first-met order." },
                new() { Name = "Ascending", Value = "1", Description = "Smallest to largest." },
                new() { Name = "Descending", Value = "2", Description = "Largest to smallest." },
            ]
        },
        new()
        {
            Id = "BitDataGridEditMode",
            Name = "BitDataGridEditMode",
            Description = "How much of a row an inline edit opens.",
            Items =
            [
                new() { Name = "Row", Value = "0", Description = "The whole row, with Save/Cancel." },
                new() { Name = "Cell", Value = "1", Description = "One cell at a time; Enter, Tab or blur commits, Escape cancels." },
            ]
        },
        DemoSharedEnums.BitSelectionMode(),
        new()
        {
            Id = "BitDataGridAggregateType",
            Name = "BitDataGridAggregateType",
            Description = "Built-in footer/group aggregates.",
            Items =
            [
                new() { Name = "None", Value = "0", Description = "No aggregate, unless the column has an AggregateBy." },
                new() { Name = "Sum", Value = "1", Description = "The total of the numeric values." },
                new() { Name = "Average", Value = "2", Description = "The mean of the numeric values, or 0." },
                new() { Name = "Count", Value = "3", Description = "The number of rows." },
                new() { Name = "Min", Value = "4", Description = "The smallest non-null value." },
                new() { Name = "Max", Value = "5", Description = "The largest non-null value." },
                new() { Name = "Custom", Value = "6", Description = "Produced by the column's AggregateBy." },
            ]
        },
        DemoSharedEnums.BitPlacement(),
        new()
        {
            Id = "BitDataGridColumnDataType",
            Name = "BitDataGridColumnDataType",
            Description = "The editor/filter kind of a column.",
            Items =
            [
                new() { Name = "Auto", Value = "0", Description = "Inferred from the property type (bool, enum, date, numeric; otherwise Text)." },
                new() { Name = "Text", Value = "1", Description = "A text box with the text operators." },
                new() { Name = "Number", Value = "2", Description = "A numeric text box with the comparison operators." },
                new() { Name = "Boolean", Value = "3", Description = "A true/false dropdown." },
                new() { Name = "Date", Value = "4", Description = "A date picker, without a time of day." },
                new() { Name = "DateTime", Value = "5", Description = "A date picker; equality matches the whole day." },
                new() { Name = "DateTimeOffset", Value = "6", Description = "A date picker; equality matches the whole day." },
                new() { Name = "Enum", Value = "7", Description = "A dropdown of the enum's names." },
            ]
        },
        new()
        {
            Id = "BitDataGridFilterOperator",
            Name = "BitDataGridFilterOperator",
            Description = "Column filter comparison operators.",
            Items =
            [
                new() { Name = "Unspecified", Value = "0", Description = "No operator; the filter is ignored." },
                new() { Name = "Contains", Value = "1", Description = "The text contains the term, ignoring case." },
                new() { Name = "DoesNotContain", Value = "2", Description = "The text does not contain the term, ignoring case." },
                new() { Name = "StartsWith", Value = "3", Description = "The text starts with the term, ignoring case." },
                new() { Name = "EndsWith", Value = "4", Description = "The text ends with the term, ignoring case." },
                new() { Name = "Equals", Value = "5", Description = "Equals the operand; a null operand matches nulls." },
                new() { Name = "NotEquals", Value = "6", Description = "Does not equal the operand." },
                new() { Name = "GreaterThan", Value = "7", Description = "Greater than the operand; nulls never match." },
                new() { Name = "GreaterThanOrEqual", Value = "8", Description = "Greater than or equal to the operand; nulls never match." },
                new() { Name = "LessThan", Value = "9", Description = "Less than the operand; nulls never match." },
                new() { Name = "LessThanOrEqual", Value = "10", Description = "Less than or equal to the operand; nulls never match." },
                new() { Name = "IsEmpty", Value = "11", Description = "Null or empty text; takes no operand." },
                new() { Name = "IsNotEmpty", Value = "12", Description = "Neither null nor empty text; takes no operand." },
                new() { Name = "In", Value = "13", Description = "Equals a member of the Value collection." },
                new() { Name = "NotIn", Value = "14", Description = "Equals no member of the Value collection." },
            ]
        },
    ];
}
