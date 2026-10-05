namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.DataGrid;

public partial class BitDataGridDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new() { Name = "Items", Type = "IEnumerable<TItem>?", DefaultValue = "null", Description = "The data source bound to the grid for client-side processing. An IQueryable<T> (e.g. an EF Core DbSet) gets filtering/sorting/paging translated into expression trees the provider executes at the source, materializing only the current page." },
        new() { Name = "OnRead", Type = "Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TItem>>>?", DefaultValue = "null", Description = "Server-side data callback. When set, the grid delegates sort/filter/page/group to the caller.", LinkType = LinkType.Link, Href = "#BitDataGridReadRequest" },
        new() { Name = "OnLoadMore", Type = "Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TItem>>>?", DefaultValue = "null", Description = "Infinite-scrolling data callback. Loads rows in batches and appends the next batch as the user scrolls toward the end. CSV/Excel exports issue a single request with Take = null (\"all rows\"), so the handler should honor a null Take.", LinkType = LinkType.Link, Href = "#BitDataGridReadRequest" },
        new() { Name = "LoadMoreBatchSize", Type = "int", DefaultValue = "50", Description = "Number of rows fetched per batch in infinite-scrolling mode." },
        new() { Name = "ChildContent", Type = "RenderFragment?", DefaultValue = "null", Description = "Column definitions and other declarative children." },
        new() { Name = "Columns", Type = "RenderFragment?", DefaultValue = "null", Description = "Alias of ChildContent, letting column definitions read declaratively as <Columns>...</Columns>. Both fragments are rendered when both are set." },
        new() { Name = "Loading", Type = "bool", DefaultValue = "false", Description = "Replaces the grid body with a loading row (LoadingTemplate, or a spinner and the localized loading text) while data is being fetched, and marks the grid aria-busy. Rows are hidden until loading ends." },
        new() { Name = "ShowSearchBox", Type = "bool", DefaultValue = "false", Description = "Renders a quick-search box in the toolbar that filters rows across every searchable column at once, matching case-insensitively against the text each column renders. Forwarded as BitDataGridReadRequest.Search in server/infinite modes; translated into an OR of Contains predicates over the string columns for an IQueryable source; a tree grid is pruned to the branches containing a match. Suppressed on a lazily-loaded tree (ChildrenProvider), whose unloaded children cannot be examined." },
        new() { Name = "SearchText", Type = "string?", DefaultValue = "null", Description = "The quick-search term (supports two-way binding), so the search can also be driven from an external field." },
        new() { Name = "SearchTextChanged", Type = "EventCallback<string?>", DefaultValue = "", Description = "Raised whenever the quick-search term changes." },
        new() { Name = "SearchDebounce", Type = "int", DefaultValue = "300", Description = "How long (ms) the search box waits after the last keystroke before applying the term, so the grid searches as the user types without re-querying on every character. 0 applies each keystroke immediately." },
        new() { Name = "KeyField", Type = "Func<TItem, object>?", DefaultValue = "null", Description = "Optional key selector used for selection/edit identity. Defaults to reference equality." },
        new() { Name = "ChildrenSelector", Type = "Func<TItem, IEnumerable<TItem>?>?", DefaultValue = "null", Description = "Child selector that turns the grid into a hierarchical tree grid." },
        new() { Name = "ChildrenProvider", Type = "Func<TItem, Task<IEnumerable<TItem>?>>?", DefaultValue = "null", Description = "Async children provider for a lazily-loaded tree grid: children are fetched on a node's first expand (e.g. from a backend) and cached. Mutually exclusive with ChildrenSelector; pair with HasChildrenSelector." },
        new() { Name = "HasChildrenSelector", Type = "Func<TItem, bool>?", DefaultValue = "null", Description = "Tells whether a node can have children before they are loaded, so unloaded lazy nodes render an expand toggle. Only used with ChildrenProvider." },
        new() { Name = "TreeInitiallyExpanded", Type = "bool", DefaultValue = "false", Description = "When tree mode is active, controls whether nodes start expanded. Ignored in lazy mode (ChildrenProvider)." },
        new() { Name = "Height", Type = "string?", DefaultValue = "null", Description = "Height of the scroll viewport, e.g. \"480px\". Required for virtualization and infinite scrolling." },
        new() { Name = "Classes", Type = "BitDataGridClassStyles?", DefaultValue = "null", Description = "Custom CSS classes for the different parts of the grid.", LinkType = LinkType.Link, Href = "#class-styles" },
        new() { Name = "Styles", Type = "BitDataGridClassStyles?", DefaultValue = "null", Description = "Custom CSS styles for the different parts of the grid.", LinkType = LinkType.Link, Href = "#class-styles" },
        new() { Name = "Striped", Type = "bool", DefaultValue = "true", Description = "Renders alternate-row striping." },
        new() { Name = "Hoverable", Type = "bool", DefaultValue = "true", Description = "Highlights the row under the pointer." },
        new() { Name = "Bordered", Type = "bool", DefaultValue = "true", Description = "Draws the outer border and the lines between columns; the lines between rows are always drawn." },
        new() { Name = "ShowHeader", Type = "bool", DefaultValue = "true", Description = "Renders the header rows (column titles, header groups and the filter row)." },
        new() { Name = "AriaLabelledBy", Type = "string?", DefaultValue = "null", Description = "The id of the element - a visible heading, say - that names the grid, in place of AriaLabel. Lands on the element with the grid role." },
        new() { Name = "AriaDescribedBy", Type = "string?", DefaultValue = "null", Description = "The id of the element that describes the grid - a caption, or how its keyboard works." },
        new() { Name = "ShowFooter", Type = "bool", DefaultValue = "false", Description = "Renders the footer/aggregate row." },
        new() { Name = "ShowRowNumbers", Type = "bool", DefaultValue = "false", Description = "Renders a narrow leading gutter numbering the rows by their position in the whole dataset, so the count continues across pages, virtualized windows and infinite-scroll batches. It is chrome, not data: exports and clipboard copies never carry it." },
        new() { Name = "ShowCellTooltips", Type = "bool", DefaultValue = "false", Description = "Gives every value cell a native tooltip with its full text, so a value the column is too narrow to show stays readable on hover. Overridable per column with ShowTooltip; cells rendered by a Template are excluded." },
        new() { Name = "WrapCellText", Type = "bool", DefaultValue = "false", Description = "Lets long headers and cell values wrap onto several lines instead of clipping to one, with each row growing to fit its tallest cell. Overridable per column with WrapText; ignored while Virtualize is on, which requires a uniform row height." },
        new() { Name = "RowClass", Type = "Func<TItem, string?>?", DefaultValue = "null", Description = "Per-row CSS class selector, appended after the grid's own row classes - the conditional row styling counterpart of AG Grid's rowClassRules." },
        new() { Name = "RowStyle", Type = "Func<TItem, string?>?", DefaultValue = "null", Description = "Per-row inline style selector, appended after the row's layout style." },
        new() { Name = "Sortable", Type = "bool", DefaultValue = "true", Description = "Enables column sorting by clicking headers." },
        new() { Name = "MultiSort", Type = "bool", DefaultValue = "true", Description = "Enables multi-column sorting via Ctrl/⌘+click with priority badges." },
        new() { Name = "AllowUnsorted", Type = "bool", DefaultValue = "true", Description = "Whether a third header click returns the column to its unsorted state (ascending → descending → unsorted). Set false to cycle between ascending and descending only. Overridable per column with AllowUnsorted." },
        new() { Name = "Filterable", Type = "bool", DefaultValue = "false", Description = "Renders a per-column quick-filter row. A tree grid filters by pruning the hierarchy to the branches containing a match; the row is suppressed on a lazily-loaded tree (ChildrenProvider)." },
        new() { Name = "FilterOperators", Type = "bool", DefaultValue = "false", Description = "Shows an operator dropdown next to text/number/date filter editors so users pick the comparison (contains/starts with/=/≠/>/≥/</≤/is blank, and for text and numbers is any of/is none of over a comma-separated list) instead of the fixed default." },
        new() { Name = "FilterDebounce", Type = "int", DefaultValue = "300", Description = "How long (ms) a text or number filter box waits after the last keystroke before applying, so the grid filters as the user types. 0 applies each keystroke; a negative value applies only on Enter or blur." },
        new() { Name = "Strings", Type = "BitDataGridStrings", DefaultValue = "new()", Description = "All user-visible strings rendered by the grid; assign a customized instance to localize the UI.", LinkType = LinkType.Link, Href = "#BitDataGridStrings" },
        new() { Name = "Resizable", Type = "bool", DefaultValue = "false", Description = "Lets the columns be resized: drag a header's edge, double-click it to fit the content, or focus it (a separator) and use Left/Right, Home/End (MinWidth/MaxWidth) and Enter (fit)." },
        new() { Name = "Reorderable", Type = "bool", DefaultValue = "false", Description = "Lets the columns be reordered: drag a header (mouse, touch or pen), press Ctrl+Left/Right on it, or use the column chooser's move buttons." },
        new() { Name = "Groupable", Type = "bool", DefaultValue = "false", Description = "Enables grouping via a header button on groupable columns." },
        new() { Name = "GroupsInitiallyCollapsed", Type = "bool", DefaultValue = "false", Description = "Groups start collapsed instead of expanded, so a grouped grid opens as a compact list of group headers. Flipped at runtime by ExpandAllGroupsAsync/CollapseAllGroupsAsync." },
        new() { Name = "ShowToolbar", Type = "bool", DefaultValue = "false", Description = "Renders the toolbar, which hosts the Clear filters button while a filter is on. Search, export, the column chooser, Add and ToolbarTemplate show it on their own; an empty bar takes no room." },
        new() { Name = "ShowColumnChooser", Type = "bool", DefaultValue = "false", Description = "Renders the toolbar button that opens the column chooser, which shows and hides columns and, with Reorderable, moves them (the single-pointer and keyboard alternative to dragging a header)." },
        new() { Name = "ShowCsvExport", Type = "bool", DefaultValue = "false", Description = "Renders a CSV export button. The export covers all matching rows in every data mode, not just the rendered ones." },
        new() { Name = "ShowExcelExport", Type = "bool", DefaultValue = "false", Description = "Renders an Excel (.xlsx) export button. The workbook is generated in-process with no external dependency, covers all matching rows in every data mode, and mirrors the grid's layout: bold frozen header row, column widths, leading frozen columns as a freeze pane and ColSpan cells as merged cells." },
        new() { Name = "ExcelExportStyled", Type = "bool", DefaultValue = "false", Description = "When true, Excel exports also carry the grid's current visual theme: the rendered header/row colors, striped alternating rows, border color and bold/italic fonts are sampled from the live DOM at export time (so the active theme - including dark mode - lands in the workbook). Falls back to the plain bold-header styling when JS is unavailable (prerendering)." },
        new() { Name = "ExportFileName", Type = "string?", DefaultValue = "null", Description = "Base name (without extension) of the downloaded export files, e.g. \"orders\" for orders.csv / orders.xlsx. Defaults to \"export\"." },
        new() { Name = "CellNavigation", Type = "bool", DefaultValue = "false", Description = "Makes the data cells the grid's one tab stop, with a roving tabindex: the arrows, Home/End and PageUp/PageDown move; Enter/F2 edit, and typing into a text or number cell edits it with what was typed (Enter otherwise toggles the row's detail); Esc cancels; Space toggles the row's selection (Shift+Space a range) and Ctrl+A selects all; Delete deletes the row when Editable; in a tree, the arrows open and close a node from its first cell. The per-row checkboxes, toggles and command buttons leave the tab order." },
        new() { Name = "ClipboardCopy", Type = "bool", DefaultValue = "false", Description = "Enables copying to the system clipboard with Ctrl/⌘+C on a focused cell (requires CellNavigation) and through CopyToClipboardAsync. Copies the selected rows - or the focused one - as tab-separated text with a header line, so it pastes into a spreadsheet as columns." },
        new() { Name = "RowReorderable", Type = "bool", DefaultValue = "false", Description = "Enables drag-and-drop row reordering (mouse, touch and pen; plus keyboard via the drag handle's arrow keys)." },
        new() { Name = "OnRowReorder", Type = "EventCallback<BitDataGridRowReorderEventArgs<TItem>>", DefaultValue = "", Description = "Raised when a row is dropped onto another row during reordering.", LinkType = LinkType.Link, Href = "#BitDataGridRowReorderEventArgs" },
        new() { Name = "SelectionMode", Type = "BitDataGridSelectionMode", DefaultValue = "BitDataGridSelectionMode.None", Description = "How rows can be selected: None, Single (a row click, or Space on a focused cell - Single turns CellNavigation on) or Multiple (a checkbox column with a select-all box).", LinkType = LinkType.Link, Href = "#BitDataGridSelectionMode" },
        new() { Name = "SelectedItems", Type = "IReadOnlyList<TItem>?", DefaultValue = "null", Description = "The selected items (supports two-way binding)." },
        new() { Name = "SelectedItemsChanged", Type = "EventCallback<IReadOnlyList<TItem>>", DefaultValue = "", Description = "Raised when the selection changes." },
        new() { Name = "OnRowClick", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when a row is clicked." },
        new() { Name = "OnRowDoubleClick", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when a row is double-clicked - the usual hook for \"open this record\", or for starting an inline edit with BeginEdit." },
        new() { Name = "OnCellClick", Type = "EventCallback<BitDataGridCellEventArgs<TItem>>", DefaultValue = "", Description = "Raised when a data cell is clicked.", LinkType = LinkType.Link, Href = "#BitDataGridCellEventArgs" },
        new() { Name = "OnCellDoubleClick", Type = "EventCallback<BitDataGridCellEventArgs<TItem>>", DefaultValue = "", Description = "Raised when a data cell is double-clicked.", LinkType = LinkType.Link, Href = "#BitDataGridCellEventArgs" },
        new() { Name = "OnCellContextMenu", Type = "EventCallback<BitDataGridCellEventArgs<TItem>>", DefaultValue = "", Description = "Raised when a data cell is right-clicked.", LinkType = LinkType.Link, Href = "#BitDataGridCellEventArgs" },
        new() { Name = "IsRowSelectionDisabled", Type = "Func<TItem, bool>?", DefaultValue = "null", Description = "Predicate returning true when a given row may not be selected." },
        new() { Name = "Pageable", Type = "bool", DefaultValue = "false", Description = "Enables paging with a pager UI." },
        new() { Name = "PageSize", Type = "int", DefaultValue = "20", Description = "The number of rows per page." },
        new() { Name = "PageSizeOptions", Type = "int[]", DefaultValue = "{ 10, 20, 50, 100 }", Description = "The page-size options offered in the pager dropdown." },
        new() { Name = "PagerPosition", Type = "BitDataGridPagerPosition", DefaultValue = "BitDataGridPagerPosition.Bottom", Description = "Where the pager renders relative to the grid.", LinkType = LinkType.Link, Href = "#BitDataGridPagerPosition" },
        new() { Name = "Virtualize", Type = "bool", DefaultValue = "false", Description = "Renders only the visible rows for large datasets. Requires a fixed Height and RowHeight. In server mode (OnRead) with paging off, row windows are fetched on demand as the user scrolls; with OnLoadMore, the accumulated batches are virtualized so the DOM stays bounded. Requires a uniform row height, so RowHeightSelector and WrapCellText are ignored while it is on and expanded DetailTemplate rows are not accounted for - pair master-detail with paging instead." },
        new() { Name = "RowHeight", Type = "float", DefaultValue = "36", Description = "Uniform row height in pixels (required when virtualizing)." },
        new() { Name = "RowHeightSelector", Type = "Func<TItem, float>?", DefaultValue = "null", Description = "Optional per-row height selector (ignored while virtualizing)." },
        new() { Name = "VirtualizeColumns", Type = "bool", DefaultValue = "false", Description = "Renders only the columns in (and near) the horizontal viewport, replacing scrolled-out runs with spacers - for grids with very many columns. Requires explicit px column widths; not applied with column header groups or ColSpans." },
        new() { Name = "Editable", Type = "bool", DefaultValue = "false", Description = "Enables inline editing with a command column." },
        new() { Name = "EditMode", Type = "BitDataGridEditMode", DefaultValue = "BitDataGridEditMode.Row", Description = "Row edits the whole row with Save/Cancel; Cell edits one cell at a time - Enter, F2, a double-click or typing into it (Backspace: empty) opens it, Enter, Tab (opening the next cell) or moving the focus out commits it (raising OnRowSave), Escape cancels. Cell mode makes the cells keyboard-navigable.", LinkType = LinkType.Link, Href = "#BitDataGridEditMode" },
        new() { Name = "NewItemFactory", Type = "Func<TItem>?", DefaultValue = "null", Description = "Factory used by the toolbar Add button to create a new row." },
        new() { Name = "OnRowSave", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when an edited row is saved." },
        new() { Name = "OnRowCancel", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when an edit is cancelled." },
        new() { Name = "OnRowDelete", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when a row is deleted (via the command column's Delete button or the Delete key in cell navigation)." },
        new() { Name = "OnRowCreate", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when a new row is created." },
        new() { Name = "EmptyTemplate", Type = "RenderFragment?", DefaultValue = "null", Description = "Custom content rendered when there is no data." },
        new() { Name = "LoadingTemplate", Type = "RenderFragment?", DefaultValue = "null", Description = "Custom content rendered in place of the built-in spinner while Loading is true." },
        new() { Name = "ToolbarTemplate", Type = "RenderFragment?", DefaultValue = "null", Description = "Custom content rendered in the toolbar's start area." },
        new() { Name = "DetailTemplate", Type = "RenderFragment<TItem>?", DefaultValue = "null", Description = "Expandable master-detail content rendered under a row." },
        new() { Name = "ShowDetailToggle", Type = "bool", DefaultValue = "true", Description = "Renders the built-in expand/collapse toggle column while a DetailTemplate is set. Turn it off to drive the detail rows from row clicks (ExpandDetailOnRowClick) or from code; only the toggle column disappears, the detail rows still render." },
        new() { Name = "ExpandDetailOnRowClick", Type = "bool", DefaultValue = "false", Description = "Expands (and collapses) a row's detail content when the row itself is clicked. Combines with SelectionMode: a click both selects the row and toggles its detail. Clicks inside the reorder/select/command cells are excluded." },
        new() { Name = "ExpandedDetailItems", Type = "IReadOnlyList<TItem>?", DefaultValue = "null", Description = "Rows whose detail content is expanded, as a two-way bindable list. Binding it takes control of the expanded state, letting a parent expand or collapse details declaratively." },
        new() { Name = "ExpandedDetailItemsChanged", Type = "EventCallback<IReadOnlyList<TItem>>", DefaultValue = "", Description = "Raised with the new set of expanded rows whenever a detail row is expanded or collapsed." },
        new() { Name = "OnDetailToggle", Type = "EventCallback<BitDataGridDetailEventArgs<TItem>>", DefaultValue = "", Description = "Raised when a single row's detail content is expanded or collapsed.", LinkType = LinkType.Link, Href = "#BitDataGridDetailEventArgs" },
        new() { Name = "OnSortChange", Type = "EventCallback<IReadOnlyList<BitDataGridSortDescriptor>>", DefaultValue = "", Description = "Raised with the new sort descriptors whenever the sorting changes, by header click or through the programmatic API.", LinkType = LinkType.Link, Href = "#BitDataGridSortDescriptor" },
        new() { Name = "OnFilterChange", Type = "EventCallback<IReadOnlyList<BitDataGridFilterDescriptor>>", DefaultValue = "", Description = "Raised with the new filter descriptors whenever the filtering changes.", LinkType = LinkType.Link, Href = "#BitDataGridFilterDescriptor" },
        new() { Name = "OnGroupChange", Type = "EventCallback<IReadOnlyList<BitDataGridGroupDescriptor>>", DefaultValue = "", Description = "Raised with the new group descriptors whenever the grouping changes.", LinkType = LinkType.Link, Href = "#BitDataGridGroupDescriptor" },
        new() { Name = "OnPageChange", Type = "EventCallback<int>", DefaultValue = "", Description = "Raised with the new 1-based page number whenever the page or the page size changes." },
        new() { Name = "OnStateChange", Type = "EventCallback<BitDataGridState>", DefaultValue = "", Description = "Raised with a GetState snapshot once the grid has re-rendered after anything it captures changed - sorts, filters, search, groups and their expansion, page, page size, and the columns' order, widths and visibility - by the user or through the API. The hook for persisting the view; ApplyStateAsync does not raise it.", LinkType = LinkType.Link, Href = "#BitDataGridState" },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new() { Name = "RefreshAsync", Type = "Task", DefaultValue = "", Description = "Recomputes the data view (filter → sort → group → page) and re-renders the grid." },
        new() { Name = "SortByAsync", Type = "Task", DefaultValue = "", Description = "SortByAsync(columnId, direction, additive) - programmatically sorts by a column; BitDataGridSortDirection.None removes the sort." },
        new() { Name = "ClearSortsAsync", Type = "Task", DefaultValue = "", Description = "Removes all active sorts and refreshes." },
        new() { Name = "ApplyFilterAsync", Type = "Task", DefaultValue = "", Description = "ApplyFilterAsync(columnId, operator, value) - programmatically applies a filter, replacing any existing one on the column." },
        new() { Name = "ApplyRangeFilterAsync", Type = "Task", DefaultValue = "", Description = "ApplyRangeFilterAsync(columnId, from, toExclusive) - applies a half-open range filter (>= from AND < toExclusive), the shape a \"between\" criterion takes; emitted as two ordinary comparison descriptors so remote and queryable sources need no special handling." },
        new() { Name = "ClearFilterAsync", Type = "Task", DefaultValue = "", Description = "ClearFilterAsync(columnId) - removes the filter(s) applied to a column." },
        new() { Name = "ClearFiltersAsync", Type = "Task", DefaultValue = "", Description = "Clears all active column filters and refreshes." },
        new() { Name = "GroupByAsync", Type = "Task", DefaultValue = "", Description = "GroupByAsync(columnId) - adds the column as the next (nested) grouping level." },
        new() { Name = "UngroupAsync", Type = "Task", DefaultValue = "", Description = "UngroupAsync(columnId) - removes the column's grouping level." },
        new() { Name = "ClearGroupsAsync", Type = "Task", DefaultValue = "", Description = "Removes all active groupings and refreshes." },
        new() { Name = "ExpandAllGroupsAsync", Type = "Task", DefaultValue = "", Description = "Expands every group at every nesting level, and makes newly-built groups expanded (so the choice survives a regrouping or a data refresh)." },
        new() { Name = "CollapseAllGroupsAsync", Type = "Task", DefaultValue = "", Description = "Collapses every group at every nesting level, and makes newly-built groups collapsed." },
        new() { Name = "SearchAsync", Type = "Task", DefaultValue = "", Description = "SearchAsync(text) - applies the grid-wide quick-search term and resets to the first page; null or empty clears it. Raises SearchTextChanged." },
        new() { Name = "ActiveSearch", Type = "string?", DefaultValue = "null", Description = "The active quick-search term, or null when no search is applied." },
        new() { Name = "SelectAllAsync", Type = "Task", DefaultValue = "", Description = "Selects every selectable row of the current view (the whole filtered set locally; the loaded rows in server, queryable and infinite modes). Requires SelectionMode.Multiple." },
        new() { Name = "ClearSelectionAsync", Type = "Task", DefaultValue = "", Description = "Clears the row selection." },
        new() { Name = "CopyToClipboardAsync", Type = "Task<int>", DefaultValue = "", Description = "CopyToClipboardAsync() / CopyToClipboardAsync(fallbackRow) - copies the selected rows (or, in the second overload, the given row when nothing is selected) to the system clipboard as tab-separated text with a header line, and returns how many rows were copied; 0 when the clipboard is unavailable." },
        new() { Name = "AutoFitColumnAsync", Type = "Task", DefaultValue = "", Description = "AutoFitColumnAsync(columnId) - sizes a column to its widest rendered content, the same result as double-clicking its resize handle." },
        new() { Name = "AutoFitAllColumnsAsync", Type = "Task", DefaultValue = "", Description = "Auto-fits every visible column to its widest rendered content." },
        new() { Name = "GoToPageAsync", Type = "Task", DefaultValue = "", Description = "GoToPageAsync(page) - navigates to the given 1-based page (clamped to the valid range)." },
        new() { Name = "MoveColumnAsync", Type = "Task", DefaultValue = "", Description = "MoveColumnAsync(columnId, index) - moves a column to a 0-based position among all the columns (hidden ones included), whatever its Reorderable says. Raises OnStateChange." },
        new() { Name = "SetPageSizeAsync", Type = "Task", DefaultValue = "", Description = "SetPageSizeAsync(size) - changes the page size and resets to the first page (without mutating the PageSize parameter)." },
        new() { Name = "GetState", Type = "BitDataGridState", DefaultValue = "", Description = "Captures the user-adjustable state (page, page size, quick search, sorts, filters, groups with their expand/collapse state, and column layout) as a serializable snapshot.", LinkType = LinkType.Link, Href = "#BitDataGridState" },
        new() { Name = "ApplyStateAsync", Type = "Task", DefaultValue = "", Description = "ApplyStateAsync(state) - restores a state snapshot captured by GetState.", LinkType = LinkType.Link, Href = "#BitDataGridState" },
        new() { Name = "ExportCsvAsync", Type = "Task", DefaultValue = "", Description = "ExportCsvAsync(selectedOnly) - generates the full (filtered/sorted) data as CSV and triggers a client-side download. Covers all matching rows in every data mode - server/infinite modes fetch them through OnRead/OnLoadMore, tree mode includes collapsed branches - or only the selected rows when selectedOnly is true." },
        new() { Name = "ToCsv", Type = "string", DefaultValue = "", Description = "ToCsv(selectedOnly) - builds a CSV string of the full dataset synchronously - tree mode includes collapsed branches, queryable mode covers all pages. Only server/infinite modes are limited to the loaded rows (their providers are async); use ToCsvAsync there." },
        new() { Name = "ToCsvAsync", Type = "Task<string>", DefaultValue = "", Description = "ToCsvAsync(selectedOnly) - builds a CSV string of the full dataset (or only the selected rows) - server/infinite modes issue an OnRead/OnLoadMore request with no paging (Take = null), tree mode includes collapsed branches." },
        new() { Name = "ToExcelAsync", Type = "Task<byte[]>", DefaultValue = "", Description = "ToExcelAsync(selectedOnly) - generates the full (filtered/sorted) dataset, or only the selected rows, as an Excel workbook (.xlsx). Numbers, booleans and dates land as native cell types, so the sheet can sort, filter and compute on them. The workbook mirrors the grid's layout: a bold frozen header row carrying Excel's own AutoFilter, column widths, leading frozen columns as a freeze pane and ColSpan cells as merged cells; with ExcelExportStyled it also carries the grid's rendered theme (colors, striping, borders, fonts)." },
        new() { Name = "ExportExcelAsync", Type = "Task", DefaultValue = "", Description = "ExportExcelAsync(selectedOnly) - generates the .xlsx workbook and triggers a client-side download." },
        new() { Name = "ActiveSorts", Type = "IReadOnlyList<BitDataGridSortDescriptor>", DefaultValue = "[]", Description = "The active sort descriptors, in priority order." },
        new() { Name = "ActiveFilters", Type = "IReadOnlyList<BitDataGridFilterDescriptor>", DefaultValue = "[]", Description = "The active filter descriptors." },
        new() { Name = "ActiveGroups", Type = "IReadOnlyList<BitDataGridGroupDescriptor>", DefaultValue = "[]", Description = "The active group descriptors, in nesting order." },
        new() { Name = "TotalCount", Type = "int", DefaultValue = "0", Description = "Total number of rows in the current (filtered) view; the server-reported total in server mode." },
        new() { Name = "TotalPages", Type = "int", DefaultValue = "1", Description = "Total number of pages while paging is active." },
        new() { Name = "CurrentPage", Type = "int", DefaultValue = "1", Description = "The 1-based current page." },
        new() { Name = "ExpandAllAsync", Type = "Task", DefaultValue = "", Description = "Expands every node in the tree. No-op outside tree mode." },
        new() { Name = "CollapseAllAsync", Type = "Task", DefaultValue = "", Description = "Collapses every node in the tree. No-op outside tree mode." },
        new() { Name = "IsDetailExpanded", Type = "bool", DefaultValue = "", Description = "IsDetailExpanded(item) - true when the given row's DetailTemplate content is currently expanded." },
        new() { Name = "ExpandDetailAsync", Type = "Task", DefaultValue = "", Description = "ExpandDetailAsync(item) - expands the given row's detail content. No-op without a DetailTemplate." },
        new() { Name = "CollapseDetailAsync", Type = "Task", DefaultValue = "", Description = "CollapseDetailAsync(item) - collapses the given row's detail content." },
        new() { Name = "ToggleDetailAsync", Type = "Task", DefaultValue = "", Description = "ToggleDetailAsync(item) - expands the given row's detail content when collapsed, and collapses it otherwise." },
        new() { Name = "SetDetailExpandedAsync", Type = "Task", DefaultValue = "", Description = "SetDetailExpandedAsync(item, expanded) - expands or collapses a row's detail content." },
        new() { Name = "ExpandAllDetailsAsync", Type = "Task", DefaultValue = "", Description = "Expands the detail content of every row of the current view - in local mode every row matching the active filters, not only the rendered page; in server, queryable and infinite modes only the rows loaded so far. Raises OnDetailToggle once per newly expanded row." },
        new() { Name = "CollapseAllDetailsAsync", Type = "Task", DefaultValue = "", Description = "Collapses every expanded detail row, raising OnDetailToggle once per row." },
        new() { Name = "EditingItem", Type = "TItem?", DefaultValue = "null", Description = "The row currently in inline-edit mode, or null when no edit is open." },
        new() { Name = "EditingColumnId", Type = "string?", DefaultValue = "null", Description = "The column of the cell open for editing in Cell mode, or null." },
        new() { Name = "BeginEditAsync", Type = "Task", DefaultValue = "", Description = "BeginEditAsync(item, columnId) - opens an edit at a cell and moves the focus into its editor (the whole row in Row mode; in Cell mode only that cell, after committing any other)." },
        new() { Name = "BeginEdit", Type = "void", DefaultValue = "", Description = "BeginEdit(item) - puts a row into inline-edit mode from code, exactly as its Edit button (or Enter/F2 on a navigable cell) does, snapshotting its values so CancelEditAsync can restore them." },
        new() { Name = "CommitEditAsync", Type = "Task", DefaultValue = "", Description = "Commits the open inline edit (writing the buffered values to the row and raising OnRowSave). Refuses while any editor holds an invalid value." },
        new() { Name = "CancelEditAsync", Type = "Task", DefaultValue = "", Description = "Abandons the open inline edit, restoring the row to the values it had when the edit began, and raises OnRowCancel." },
        new() { Name = "AddNewRowAsync", Type = "Task", DefaultValue = "", Description = "Appends a blank row built by NewItemFactory above the view and opens it for editing, exactly as the toolbar Add button does." },
        new() { Name = "DeleteRowAsync", Type = "Task", DefaultValue = "", Description = "DeleteRowAsync(item) - drops the row from the selection and raises OnRowDelete so the caller can remove it from the data source, then refreshes." },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-DataGrid-background", DefaultValue = "--bit-clr-bg-pri", Description = "Background of the grid and its rows." },
        new() { Name = "--bit-DataGrid-color", DefaultValue = "--bit-clr-fg-pri", Description = "Text color." },
        new() { Name = "--bit-DataGrid-font-family", DefaultValue = "--bit-tpg-font-family", Description = "Font of the whole grid." },
        new() { Name = "--bit-DataGrid-font-size", DefaultValue = "--bit-tpg-fs-sm", Description = "Font size of the whole grid." },
        new() { Name = "--bit-DataGrid-line-height", DefaultValue = "1.4", Description = "Line height of the cells." },
        new() { Name = "--bit-DataGrid-border-color", DefaultValue = "--bit-clr-brd-ter", Description = "Color of the outer border and of every line between rows and columns." },
        new() { Name = "--bit-DataGrid-border-radius", DefaultValue = "--bit-shp-radius-surface", Description = "Corner radius of the grid." },
        new() { Name = "--bit-DataGrid-cell-padding", DefaultValue = "spacing(1) spacing(1.25)", Description = "Padding of every cell; with RowHeight, the density of the grid." },
        new() { Name = "--bit-DataGrid-header-background", DefaultValue = "--bit-clr-bg-sec", Description = "Background of the header rows and the column chooser." },
        new() { Name = "--bit-DataGrid-header-color", DefaultValue = "The grid's color", Description = "Text color of the header rows." },
        new() { Name = "--bit-DataGrid-header-font-weight", DefaultValue = "--bit-tpg-fw-semibold", Description = "Font weight of the column titles." },
        new() { Name = "--bit-DataGrid-stripe-background", DefaultValue = "--bit-clr-bg-sec", Description = "Background of every other row while Striped." },
        new() { Name = "--bit-DataGrid-hover-background", DefaultValue = "--bit-clr-bg-pri-hover", Description = "Background of the row under the pointer while Hoverable." },
        new() { Name = "--bit-DataGrid-selected-background", DefaultValue = "--bit-clr-pri-tint", Description = "Wash laid over a selected row; the row stays opaque underneath, so frozen cells still cover what scrolls by." },
        new() { Name = "--bit-DataGrid-selected-color", DefaultValue = "The grid's color", Description = "Text color of a selected row." },
        new() { Name = "--bit-DataGrid-editing-background", DefaultValue = "--bit-clr-wrn-tint", Description = "Wash laid over the row being edited (in Cell mode, over the cell)." },
        new() { Name = "--bit-DataGrid-accent-color", DefaultValue = "--bit-clr-pri", Description = "Sort arrows and priority badges, the active group toggle, the group bar, the checkboxes and the resize handle." },
        new() { Name = "--bit-DataGrid-focus-color", DefaultValue = "--bit-clr-pri-focus", Description = "Focus indicator of the cells, the rows and the controls in the grid." },
        new() { Name = "--bit-DataGrid-group-background", DefaultValue = "--bit-clr-bg-ter", Description = "Background of the group header rows." },
        new() { Name = "--bit-DataGrid-group-indent", DefaultValue = "spacing(2.5)", Description = "Indent of each nested group level." },
        new() { Name = "--bit-DataGrid-tree-indent", DefaultValue = "spacing(2.25)", Description = "Indent of each tree level." },
        new() { Name = "--bit-DataGrid-detail-background", DefaultValue = "--bit-clr-bg-sec", Description = "Background of the expanded detail rows." },
        new() { Name = "--bit-DataGrid-footer-background", DefaultValue = "--bit-clr-bg-sec", Description = "Background of the footer row." },
        new() { Name = "--bit-DataGrid-disabled-color", DefaultValue = "--bit-clr-fg-dis", Description = "Text color of a disabled grid." },
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
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the root element." },
                new() { Name = "Toolbar", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the toolbar above the grid." },
                new() { Name = "ColumnChooser", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the column chooser panel." },
                new() { Name = "Viewport", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the scrolling viewport that holds the rows." },
                new() { Name = "HeaderRow", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the row of column titles." },
                new() { Name = "HeaderCell", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to each column header cell (the column's HeaderClass comes after it)." },
                new() { Name = "FilterRow", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the row of column filters." },
                new() { Name = "Row", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to each data row (RowClass / RowStyle come after it)." },
                new() { Name = "SelectedRow", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to each selected data row, after Row." },
                new() { Name = "Cell", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to each data cell (the column's CellClass comes after it)." },
                new() { Name = "GroupRow", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the cell of each group header row." },
                new() { Name = "DetailRow", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the content of each expanded detail row." },
                new() { Name = "FooterRow", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the footer row of aggregates." },
                new() { Name = "Pager", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the pager." },
                new() { Name = "Empty", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the cell that shows the empty message." },
                new() { Name = "Loading", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the cell that shows the loading indicator." },
            ],
        },
        new()
        {
            Id = "BitDataGridColumn",
            Title = "BitDataGridColumn",
            Description = "Defines a column inside a BitDataGrid. Place these as child content of the grid.",
            Parameters =
            [
                new() { Name = "Field", Type = "string?", DefaultValue = "null", Description = "Name of the property this column is bound to. Supports nested paths (\"Address.City\"). Prefer Property for a strongly typed, refactor-safe alternative." },
                new() { Name = "Property", Type = "Expression<Func<TItem, object?>>?", DefaultValue = "null", Description = "Typed selector of the property this column is bound to, e.g. Property=\"p => p.Name\". A strongly typed, refactor-safe alternative to Field that supports nested member chains (p => p.Address.City). Takes precedence over Field when both are set." },
                new() { Name = "ColumnId", Type = "string?", DefaultValue = "null", Description = "Stable identifier for the column. Defaults to the resolved Property/Field path." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "Header text. Defaults to a humanized Property/Field name." },
                new() { Name = "Width", Type = "string?", DefaultValue = "null", Description = "CSS width, e.g. \"120px\" or \"20%\". When null the column shares remaining space." },
                new() { Name = "MinWidth", Type = "int", DefaultValue = "60", Description = "Minimum width in pixels the column can be resized to." },
                new() { Name = "MaxWidth", Type = "int?", DefaultValue = "null", Description = "Maximum width in pixels the column can be resized to." },
                new() { Name = "Sortable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level Sortable for this column." },
                new() { Name = "SortBy", Type = "Func<TItem, object?>?", DefaultValue = "null", Description = "Custom sort key selector. Enables sorting for template-only columns and overrides the field value as the sort key (client mode)." },
                new() { Name = "SortDescendingFirst", Type = "bool", DefaultValue = "false", Description = "When true, the first click on the header sorts descending instead of ascending." },
                new() { Name = "AllowUnsorted", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level AllowUnsorted: whether a third header click returns this column to its unsorted state." },
                new() { Name = "Comparer", Type = "IComparer<object?>?", DefaultValue = "null", Description = "Custom comparer applied to this column's sort keys, for orderings the default null-safe value comparer cannot express. Client-side only (server and queryable sources sort at the source)." },
                new() { Name = "Validate", Type = "Func<TItem, object?, string?>?", DefaultValue = "null", Description = "Validator for inline edits: receives the row and the proposed value, returns an error message to reject it (blocking Save) or null to accept." },
                new() { Name = "Filterable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level Filterable for this column." },
                new() { Name = "FilterOperators", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level FilterOperators (the operator dropdown next to this column's filter editor)." },
                new() { Name = "FilterTemplate", Type = "RenderFragment<BitDataGridFilterContext>?", DefaultValue = "null", Description = "Replaces this column's built-in filter editor with custom markup (e.g. a multi-select applying In). The context carries the current filter and ApplyAsync/ApplyRangeAsync/ClearAsync.", LinkType = LinkType.Link, Href = "#BitDataGridFilterContext" },
                new() { Name = "Resizable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level Resizable for this column." },
                new() { Name = "Reorderable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level Reorderable for this column." },
                new() { Name = "Editable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level Editable for this column." },
                new() { Name = "Groupable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level Groupable for this column." },
                new() { Name = "Searchable", Type = "bool?", DefaultValue = "null", Description = "Whether the grid's quick-search box searches this column. Defaults to every field-bound (or ExportValue-backed) column." },
                new() { Name = "Exportable", Type = "bool?", DefaultValue = "null", Description = "Whether the column is included in CSV/Excel exports. Defaults to every column that has a value to write (a bound field or an ExportValue selector)." },
                new() { Name = "ExportValue", Type = "Func<TItem, object?>?", DefaultValue = "null", Description = "Value selector used by exports and the clipboard instead of the bound field - the export counterpart of Template. Gives a template-only column a real exported value (numbers and booleans still land in Excel as native cell types)." },
                new() { Name = "ShowTooltip", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level ShowCellTooltips for this column's cells." },
                new() { Name = "WrapText", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level WrapCellText: lets this column's header and cells wrap onto several lines instead of clipping to one, with the row growing to fit." },
                new() { Name = "Frozen", Type = "bool", DefaultValue = "false", Description = "Pins the column to the start edge so it stays visible while scrolling horizontally." },
                new() { Name = "FrozenEnd", Type = "bool", DefaultValue = "false", Description = "Pins the column to the end edge (right in LTR, left in RTL). Typical for action/status columns." },
                new() { Name = "Group", Type = "string?", DefaultValue = "null", Description = "Optional header group name. Consecutive columns sharing the same value render under a single spanning header cell." },
                new() { Name = "ColSpan", Type = "Func<TItem, int?>?", DefaultValue = "null", Description = "Optional per-row column span." },
                new() { Name = "Visible", Type = "bool", DefaultValue = "true", Description = "Whether the column is visible." },
                new() { Name = "RowHeader", Type = "bool", DefaultValue = "false", Description = "Makes the column's cells the headers of their rows (role rowheader) - the column that names a row. Screen readers announce it as the focus moves between rows, and it names the row's selection checkbox." },
                new() { Name = "Align", Type = "BitDataGridColumnAlign", DefaultValue = "BitDataGridColumnAlign.Left", Description = "Horizontal alignment of cell content.", LinkType = LinkType.Link, Href = "#BitDataGridColumnAlign" },
                new() { Name = "Format", Type = "string?", DefaultValue = "null", Description = "A .NET format string applied to the value (e.g. \"C2\", \"yyyy-MM-dd\")." },
                new() { Name = "DataType", Type = "BitDataGridColumnDataType", DefaultValue = "BitDataGridColumnDataType.Auto", Description = "The data type used to pick the editor/filter.", LinkType = LinkType.Link, Href = "#BitDataGridColumnDataType" },
                new() { Name = "Aggregate", Type = "BitDataGridAggregateType", DefaultValue = "BitDataGridAggregateType.None", Description = "The footer/group aggregate function.", LinkType = LinkType.Link, Href = "#BitDataGridAggregateType" },
                new() { Name = "AggregateBy", Type = "Func<IReadOnlyList<TItem>, object?>?", DefaultValue = "null", Description = "Custom aggregate function for computations beyond the built-ins (e.g. distinct count). Takes precedence over Aggregate." },
                new() { Name = "AggregateFormat", Type = "string?", DefaultValue = "null", Description = "Format string for the aggregate value. Falls back to Format." },
                new() { Name = "HeaderClass", Type = "string?", DefaultValue = "null", Description = "Custom CSS class applied to the header cell." },
                new() { Name = "CellClass", Type = "string?", DefaultValue = "null", Description = "Custom CSS class applied to each data cell." },
                new() { Name = "CellClassSelector", Type = "Func<TItem, string?>?", DefaultValue = "null", Description = "Per-row CSS class for this column's cells, from the row's data (added after CellClass)." },
                new() { Name = "CellStyleSelector", Type = "Func<TItem, string?>?", DefaultValue = "null", Description = "Per-row inline style for this column's cells, applied last. Prefer color/font over background, which would hide the selection and hover states." },
                new() { Name = "Template", Type = "RenderFragment<TItem>?", DefaultValue = "null", Description = "Custom rendering for a data cell." },
                new() { Name = "HeaderTemplate", Type = "RenderFragment?", DefaultValue = "null", Description = "Custom rendering for the header cell content." },
                new() { Name = "EditTemplate", Type = "RenderFragment<TItem>?", DefaultValue = "null", Description = "Custom editor rendered when the row/cell is in edit mode." },
                new() { Name = "FooterTemplate", Type = "RenderFragment<BitDataGridAggregateResult>?", DefaultValue = "null", Description = "Custom rendering for the footer/aggregate cell.", LinkType = LinkType.Link, Href = "#BitDataGridAggregateResult" },
            ],
        },
        new()
        {
            Id = "BitDataGridReadRequest",
            Title = "BitDataGridReadRequest",
            Description = "Describes the data the grid needs from a server-side/infinite source (passed to OnRead/OnLoadMore).",
            Parameters =
            [
                new() { Name = "Skip", Type = "int", DefaultValue = "0", Description = "Zero-based number of items to skip." },
                new() { Name = "Take", Type = "int?", DefaultValue = "null", Description = "Maximum number of items to return (null means all)." },
                new() { Name = "Sorts", Type = "IReadOnlyList<BitDataGridSortDescriptor>", DefaultValue = "[]", Description = "The active sort descriptors ordered by priority.", LinkType = LinkType.Link, Href = "#BitDataGridSortDescriptor" },
                new() { Name = "Filters", Type = "IReadOnlyList<BitDataGridFilterDescriptor>", DefaultValue = "[]", Description = "The active filter descriptors.", LinkType = LinkType.Link, Href = "#BitDataGridFilterDescriptor" },
                new() { Name = "Groups", Type = "IReadOnlyList<BitDataGridGroupDescriptor>", DefaultValue = "[]", Description = "The active group descriptors in nesting order, letting a server-side handler reconstruct the grouping. Empty when no grouping is active." },
                new() { Name = "Search", Type = "string?", DefaultValue = "null", Description = "The grid-wide quick-search term, or null when no search is active. A free-text term to match across the columns the handler considers searchable, in addition to the per-column Filters." },
                new() { Name = "CancellationToken", Type = "CancellationToken", DefaultValue = "", Description = "A token that is cancelled when the request is superseded by a newer one." },
            ],
        },
        new()
        {
            Id = "BitDataGridReadResult",
            Title = "BitDataGridReadResult<TItem>",
            Description = "Result returned from a grid's OnRead/OnLoadMore callback.",
            Parameters =
            [
                new() { Name = "Items", Type = "IReadOnlyList<TItem>", DefaultValue = "", Description = "The items for the current page/window." },
                new() { Name = "TotalCount", Type = "int", DefaultValue = "", Description = "The total number of items matching the current filters (ignored in infinite mode)." },
                new() { Name = "Aggregates", Type = "IReadOnlyList<BitDataGridAggregateResult>?", DefaultValue = "null", Description = "Optional aggregates computed by the data source over the whole filtered dataset; when provided, the footer shows these instead of aggregating the current page locally.", LinkType = LinkType.Link, Href = "#BitDataGridAggregateResult" },
            ],
        },
        new()
        {
            Id = "BitDataGridCellEventArgs",
            Title = "BitDataGridCellEventArgs<TItem>",
            Description = "Arguments passed to cell-level event callbacks.",
            Parameters =
            [
                new() { Name = "Item", Type = "TItem", DefaultValue = "", Description = "The row item." },
                new() { Name = "Column", Type = "BitDataGridColumn<TItem>", DefaultValue = "", Description = "The column the cell belongs to.", LinkType = LinkType.Link, Href = "#BitDataGridColumn" },
                new() { Name = "ColumnId", Type = "string", DefaultValue = "", Description = "The column field/identifier." },
                new() { Name = "ColumnTitle", Type = "string", DefaultValue = "", Description = "The column's display title." },
                new() { Name = "Value", Type = "object?", DefaultValue = "null", Description = "The raw value of the cell." },
                new() { Name = "Mouse", Type = "MouseEventArgs", DefaultValue = "", Description = "The underlying browser mouse event." },
            ],
        },
        new()
        {
            Id = "BitDataGridDetailEventArgs",
            Title = "BitDataGridDetailEventArgs<TItem>",
            Description = "Arguments raised when a row's master-detail content is expanded or collapsed.",
            Parameters =
            [
                new() { Name = "Item", Type = "TItem", DefaultValue = "", Description = "The row whose detail content was toggled." },
                new() { Name = "Expanded", Type = "bool", DefaultValue = "", Description = "True when the detail content was expanded, false when it was collapsed." },
            ],
        },
        new()
        {
            Id = "BitDataGridRowReorderEventArgs",
            Title = "BitDataGridRowReorderEventArgs<TItem>",
            Description = "Arguments raised when a row is reordered via drag-and-drop.",
            Parameters =
            [
                new() { Name = "DraggedItem", Type = "TItem", DefaultValue = "", Description = "The dragged row item." },
                new() { Name = "TargetItem", Type = "TItem", DefaultValue = "", Description = "The drop-target row item." },
                new() { Name = "FromIndex", Type = "int?", DefaultValue = "", Description = "The original index of the dragged item, or null when the bound Items is not an indexable list." },
                new() { Name = "ToIndex", Type = "int?", DefaultValue = "", Description = "The destination index, or null when the bound Items is not an indexable list." },
            ],
        },
        new()
        {
            Id = "BitDataGridSortDescriptor",
            Title = "BitDataGridSortDescriptor",
            Description = "Describes the sort state applied to a single column (found on BitDataGridReadRequest.Sorts).",
            Parameters =
            [
                new() { Name = "ColumnId", Type = "string", DefaultValue = "", Description = "The identifier of the column being sorted." },
                new() { Name = "Direction", Type = "BitDataGridSortDirection", DefaultValue = "BitDataGridSortDirection.Ascending", Description = "The sort direction.", LinkType = LinkType.Link, Href = "#BitDataGridSortDirection" },
                new() { Name = "Priority", Type = "int", DefaultValue = "int.MaxValue", Description = "Priority for multi-column sorting (1 = primary)." },
            ],
        },
        new()
        {
            Id = "BitDataGridFilterDescriptor",
            Title = "BitDataGridFilterDescriptor",
            Description = "Describes a filter applied to a single column (found on BitDataGridReadRequest.Filters).",
            Parameters =
            [
                new() { Name = "ColumnId", Type = "string", DefaultValue = "", Description = "The identifier of the column being filtered." },
                new() { Name = "Operator", Type = "BitDataGridFilterOperator", DefaultValue = "BitDataGridFilterOperator.Unspecified", Description = "The comparison operator applied to the value. Unspecified applies no filter.", LinkType = LinkType.Link, Href = "#BitDataGridFilterOperator" },
                new() { Name = "Value", Type = "object?", DefaultValue = "null", Description = "The value compared against the column's cell value; a collection for In/NotIn." },
            ],
        },
        new()
        {
            Id = "BitDataGridFilterContext",
            Title = "BitDataGridFilterContext",
            Description = "The context of a column's FilterTemplate: the column's current filter and the calls that change it, through the same pipeline as the built-in editors.",
            Parameters =
            [
                new() { Name = "ColumnId", Type = "string", DefaultValue = "", Description = "The identifier of the column being filtered." },
                new() { Name = "Title", Type = "string", DefaultValue = "", Description = "The column's header text." },
                new() { Name = "Label", Type = "string", DefaultValue = "", Description = "The accessible name for the editor (\"Filter by {Title}\"); put it on the control's aria-label." },
                new() { Name = "ValueType", Type = "Type?", DefaultValue = "null", Description = "The type of the column's bound member, Nullable<T> unwrapped." },
                new() { Name = "Disabled", Type = "bool", DefaultValue = "", Description = "Whether the grid is disabled; disable the editor when it is." },
                new() { Name = "Filters", Type = "IReadOnlyList<BitDataGridFilterDescriptor>", DefaultValue = "", Description = "The descriptors applied to the column: none, one, or the two halves of a range.", LinkType = LinkType.Link, Href = "#BitDataGridFilterDescriptor" },
                new() { Name = "IsActive", Type = "bool", DefaultValue = "", Description = "Whether any filter is applied to the column." },
                new() { Name = "Operator", Type = "BitDataGridFilterOperator", DefaultValue = "", Description = "The operator of the column's (first) filter, or Unspecified.", LinkType = LinkType.Link, Href = "#BitDataGridFilterOperator" },
                new() { Name = "Value", Type = "object?", DefaultValue = "", Description = "The value of the column's (first) filter, or null." },
                new() { Name = "ApplyAsync", Type = "Task (BitDataGridFilterOperator, object?)", DefaultValue = "", Description = "Replaces the column's filter; a null, blank or empty-set value clears it." },
                new() { Name = "ApplyRangeAsync", Type = "Task (object?, object?)", DefaultValue = "", Description = "Replaces the column's filter with a half-open range (>= from AND < toExclusive)." },
                new() { Name = "ClearAsync", Type = "Task ()", DefaultValue = "", Description = "Removes the column's filter." },
            ],
        },
        new()
        {
            Id = "BitDataGridGroupDescriptor",
            Title = "BitDataGridGroupDescriptor",
            Description = "Describes a grouping applied to a column.",
            Parameters =
            [
                new() { Name = "ColumnId", Type = "string", DefaultValue = "", Description = "The identifier of the column being grouped." },
                new() { Name = "Direction", Type = "BitDataGridSortDirection", DefaultValue = "BitDataGridSortDirection.Ascending", Description = "The sort direction applied to the group keys.", LinkType = LinkType.Link, Href = "#BitDataGridSortDirection" },
            ],
        },
        new()
        {
            Id = "BitDataGridAggregateResult",
            Title = "BitDataGridAggregateResult",
            Description = "Holds the computed aggregate value for a column footer or group (passed to a column's FooterTemplate).",
            Parameters =
            [
                new() { Name = "ColumnId", Type = "string", DefaultValue = "", Description = "The identifier of the aggregated column." },
                new() { Name = "Type", Type = "BitDataGridAggregateType", DefaultValue = "", Description = "The aggregate function that produced the value.", LinkType = LinkType.Link, Href = "#BitDataGridAggregateType" },
                new() { Name = "Value", Type = "object?", DefaultValue = "null", Description = "The raw aggregate value." },
                new() { Name = "FormattedValue", Type = "string", DefaultValue = "string.Empty", Description = "The aggregate value formatted using the column's AggregateFormat/Format." },
            ],
        },
        new()
        {
            Id = "BitDataGridState",
            Title = "BitDataGridState",
            Description = "A serializable snapshot of the grid's user-adjustable state, captured with GetState() and restored with ApplyStateAsync(). Enables persisting grid state across sessions.",
            Parameters =
            [
                new() { Name = "CurrentPage", Type = "int", DefaultValue = "1", Description = "The 1-based current page." },
                new() { Name = "PageSize", Type = "int?", DefaultValue = "null", Description = "The user-selected page size, or null when the grid's PageSize parameter applies." },
                new() { Name = "Search", Type = "string?", DefaultValue = "null", Description = "The grid-wide quick-search term, or null when no search was active." },
                new() { Name = "Sorts", Type = "List<BitDataGridSortDescriptor>", DefaultValue = "[]", Description = "The active sort descriptors.", LinkType = LinkType.Link, Href = "#BitDataGridSortDescriptor" },
                new() { Name = "Filters", Type = "List<BitDataGridFilterDescriptor>", DefaultValue = "[]", Description = "The active filter descriptors.", LinkType = LinkType.Link, Href = "#BitDataGridFilterDescriptor" },
                new() { Name = "Groups", Type = "List<BitDataGridGroupDescriptor>", DefaultValue = "[]", Description = "The active group descriptors." },
                new() { Name = "GroupsCollapsed", Type = "bool", DefaultValue = "false", Description = "Whether groups were collapsed by default when the snapshot was taken." },
                new() { Name = "GroupExpansionOverrides", Type = "List<string>", DefaultValue = "[]", Description = "The groups whose expanded state differed from GroupsCollapsed, by their stable paths - so a restored view opens exactly the groups the user had open. Tree-node expansion is not captured (a tree key is not guaranteed to be serializable)." },
                new() { Name = "Columns", Type = "List<BitDataGridColumnState>", DefaultValue = "[]", Description = "Per-column layout state.", LinkType = LinkType.Link, Href = "#BitDataGridColumnState" },
            ],
        },
        new()
        {
            Id = "BitDataGridColumnState",
            Title = "BitDataGridColumnState",
            Description = "A per-column layout entry inside BitDataGridState.Columns.",
            Parameters =
            [
                new() { Name = "ColumnId", Type = "string", DefaultValue = "string.Empty", Description = "The column's stable identifier (ColumnId or Field)." },
                new() { Name = "Visible", Type = "bool", DefaultValue = "true", Description = "Whether the column is shown (column-chooser state)." },
                new() { Name = "Width", Type = "double?", DefaultValue = "null", Description = "The resized width in pixels, or null when the column was never resized." },
                new() { Name = "Order", Type = "int", DefaultValue = "0", Description = "The display position among all columns." },
            ],
        },
        new()
        {
            Id = "BitDataGridStrings",
            Title = "BitDataGridStrings",
            Description = "All user-visible (and screen-reader) strings rendered by the grid, defaulting to English. Assign a customized instance to the Strings parameter to localize - including empty/loading texts, pager texts, filter placeholders and operators, edit buttons, aggregate labels, aria-labels and live-region announcements.",
            Parameters =
            [
                new() { Name = "GridLabel", Type = "string", DefaultValue = "\"Data grid\"", Description = "Default accessible name of the grid element, used when no AriaLabel is given." },
                new() { Name = "EmptyText", Type = "string", DefaultValue = "\"No records to display.\"", Description = "Shown when the grid has no rows to display." },
                new() { Name = "LoadingText", Type = "string", DefaultValue = "\"Loading…\"", Description = "Shown while Loading is true." },
                new() { Name = "PagerRangeFormat", Type = "string", DefaultValue = "\"{0}–{1} of {2}\"", Description = "Pager range summary format." },
                new() { Name = "PagerPageFormat", Type = "string", DefaultValue = "\"Page {0} of {1}\"", Description = "Pager page summary format." },
                new() { Name = "InvalidValueError", Type = "string", DefaultValue = "\"Invalid value for {0}.\"", Description = "Error shown when an edited value can't be converted to the column's type." },
                new() { Name = "SearchPlaceholder", Type = "string", DefaultValue = "\"Search…\"", Description = "Placeholder of the toolbar's quick-search box." },
                new() { Name = "…", Type = "string", DefaultValue = "", Description = "Plus 86 more: toolbar/edit button texts, search and column-chooser labels, the no-matches text, filter placeholders and operator labels, boolean/enum option texts, the names of the special columns, group/detail/tree/reorder/resize/move labels and tooltips, the sort priority, aggregate label formats and aria-live announcements." },
            ],
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "BitDataGridColumnAlign",
            Name = "BitDataGridColumnAlign",
            Description = "Horizontal alignment of cell content.",
            Items =
            [
                new() { Name = "Left", Value = "0" },
                new() { Name = "Center", Value = "1" },
                new() { Name = "Right", Value = "2" },
            ]
        },
        new()
        {
            Id = "BitDataGridSortDirection",
            Name = "BitDataGridSortDirection",
            Description = "Sort direction for a column.",
            Items =
            [
                new() { Name = "None", Value = "0" },
                new() { Name = "Ascending", Value = "1" },
                new() { Name = "Descending", Value = "2" },
            ]
        },
        new()
        {
            Id = "BitDataGridEditMode",
            Name = "BitDataGridEditMode",
            Description = "How much of a row an inline edit opens.",
            Items =
            [
                new() { Name = "Row", Value = "0", Description = "The whole row opens at once, with Save/Cancel in the command column." },
                new() { Name = "Cell", Value = "1", Description = "One cell opens at a time; Enter, Tab (opening the next) or moving the focus out commits it, Escape cancels." },
            ]
        },
        new()
        {
            Id = "BitDataGridSelectionMode",
            Name = "BitDataGridSelectionMode",
            Description = "How rows can be selected in the grid.",
            Items =
            [
                new() { Name = "None", Value = "0" },
                new() { Name = "Single", Value = "1" },
                new() { Name = "Multiple", Value = "2" },
            ]
        },
        new()
        {
            Id = "BitDataGridAggregateType",
            Name = "BitDataGridAggregateType",
            Description = "Built-in aggregate functions for summary/footer rows.",
            Items =
            [
                new() { Name = "None", Value = "0" },
                new() { Name = "Sum", Value = "1" },
                new() { Name = "Average", Value = "2" },
                new() { Name = "Count", Value = "3" },
                new() { Name = "Min", Value = "4" },
                new() { Name = "Max", Value = "5" },
                new() { Name = "Custom", Value = "6", Description = "The value was produced by the column's custom AggregateBy delegate rather than a built-in function." },
            ]
        },
        new()
        {
            Id = "BitDataGridPagerPosition",
            Name = "BitDataGridPagerPosition",
            Description = "Where the pager is rendered relative to the grid.",
            Items =
            [
                new() { Name = "Bottom", Value = "0" },
                new() { Name = "Top", Value = "1" },
                new() { Name = "TopAndBottom", Value = "2" },
            ]
        },
        new()
        {
            Id = "BitDataGridColumnDataType",
            Name = "BitDataGridColumnDataType",
            Description = "The kind of editor/filter rendered for a column based on its data type.",
            Items =
            [
                new() { Name = "Auto", Value = "0" },
                new() { Name = "Text", Value = "1" },
                new() { Name = "Number", Value = "2" },
                new() { Name = "Boolean", Value = "3" },
                new() { Name = "Date", Value = "4" },
                new() { Name = "DateTime", Value = "5" },
                new() { Name = "DateTimeOffset", Value = "6" },
                new() { Name = "Enum", Value = "7" },
            ]
        },
        new()
        {
            Id = "BitDataGridFilterOperator",
            Name = "BitDataGridFilterOperator",
            Description = "Comparison operators available for column filtering.",
            Items =
            [
                new() { Name = "Unspecified", Value = "0" },
                new() { Name = "Contains", Value = "1" },
                new() { Name = "DoesNotContain", Value = "2" },
                new() { Name = "StartsWith", Value = "3" },
                new() { Name = "EndsWith", Value = "4" },
                new() { Name = "Equals", Value = "5" },
                new() { Name = "NotEquals", Value = "6" },
                new() { Name = "GreaterThan", Value = "7" },
                new() { Name = "GreaterThanOrEqual", Value = "8" },
                new() { Name = "LessThan", Value = "9" },
                new() { Name = "LessThanOrEqual", Value = "10" },
                new() { Name = "IsEmpty", Value = "11" },
                new() { Name = "IsNotEmpty", Value = "12" },
                new() { Name = "In", Value = "13", Description = "Equals any member of the collection in Value." },
                new() { Name = "NotIn", Value = "14", Description = "Equals no member of the collection in Value." },
            ]
        },
    ];
}
