namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.DataGrid;

public partial class BitDataGridDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new() { Name = "Items", Type = "IEnumerable<TItem>?", DefaultValue = "null", Description = "The data source bound to the grid for client-side processing." },
        new() { Name = "OnRead", Type = "Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TItem>>>?", DefaultValue = "null", Description = "Server-side data callback. When set, the grid delegates sort/filter/page/group to the caller.", LinkType = LinkType.Link, Href = "#BitDataGridReadRequest" },
        new() { Name = "OnLoadMore", Type = "Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TItem>>>?", DefaultValue = "null", Description = "Infinite-scrolling data callback. Loads rows in batches and appends the next batch as the user scrolls toward the end.", LinkType = LinkType.Link, Href = "#BitDataGridReadRequest" },
        new() { Name = "LoadMoreBatchSize", Type = "int", DefaultValue = "50", Description = "Number of rows fetched per batch in infinite-scrolling mode." },
        new() { Name = "ChildContent", Type = "RenderFragment?", DefaultValue = "null", Description = "Column definitions and other declarative children." },
        new() { Name = "Loading", Type = "bool", DefaultValue = "false", Description = "Shows a loading overlay while data is being fetched." },
        new() { Name = "KeyField", Type = "Func<TItem, object>?", DefaultValue = "null", Description = "Optional key selector used for selection/edit identity. Defaults to reference equality." },
        new() { Name = "ChildrenSelector", Type = "Func<TItem, IEnumerable<TItem>?>?", DefaultValue = "null", Description = "Child selector that turns the grid into a hierarchical tree grid." },
        new() { Name = "TreeInitiallyExpanded", Type = "bool", DefaultValue = "false", Description = "When tree mode is active, controls whether nodes start expanded." },
        new() { Name = "Class", Type = "string?", DefaultValue = "null", Description = "Custom CSS class for the root element." },
        new() { Name = "Style", Type = "string?", DefaultValue = "null", Description = "Custom inline style for the root element." },
        new() { Name = "Height", Type = "string?", DefaultValue = "null", Description = "Height of the scroll viewport, e.g. \"480px\". Required for virtualization and infinite scrolling." },
        new() { Name = "Striped", Type = "bool", DefaultValue = "true", Description = "Renders alternate-row striping." },
        new() { Name = "Hoverable", Type = "bool", DefaultValue = "true", Description = "Highlights the row under the pointer." },
        new() { Name = "Bordered", Type = "bool", DefaultValue = "true", Description = "Renders cell borders." },
        new() { Name = "ShowHeader", Type = "bool", DefaultValue = "true", Description = "Renders the header row." },
        new() { Name = "ShowFooter", Type = "bool", DefaultValue = "false", Description = "Renders the footer/aggregate row." },
        new() { Name = "Direction", Type = "BitDir", DefaultValue = "BitDir.Ltr", Description = "Text direction (LTR/RTL).", LinkType = LinkType.Link, Href = "#BitDir" },
        new() { Name = "Sortable", Type = "bool", DefaultValue = "true", Description = "Enables column sorting by clicking headers." },
        new() { Name = "MultiSort", Type = "bool", DefaultValue = "true", Description = "Enables multi-column sorting via Ctrl/⌘+click with priority badges." },
        new() { Name = "Filterable", Type = "bool", DefaultValue = "false", Description = "Renders a per-column quick-filter row." },
        new() { Name = "Resizable", Type = "bool", DefaultValue = "false", Description = "Enables column resizing by dragging header edges." },
        new() { Name = "Reorderable", Type = "bool", DefaultValue = "false", Description = "Enables column reordering via native drag-and-drop." },
        new() { Name = "Groupable", Type = "bool", DefaultValue = "false", Description = "Enables grouping via a header button on groupable columns." },
        new() { Name = "ShowToolbar", Type = "bool", DefaultValue = "false", Description = "Renders the toolbar area." },
        new() { Name = "ShowColumnChooser", Type = "bool", DefaultValue = "false", Description = "Renders a column show/hide chooser in the toolbar." },
        new() { Name = "ShowCsvExport", Type = "bool", DefaultValue = "false", Description = "Renders a CSV export button for the current view." },
        new() { Name = "CellNavigation", Type = "bool", DefaultValue = "false", Description = "Enables keyboard cell navigation with a roving tabindex." },
        new() { Name = "RowReorderable", Type = "bool", DefaultValue = "false", Description = "Enables drag-and-drop row reordering." },
        new() { Name = "OnRowReorder", Type = "EventCallback<BitDataGridRowReorderEventArgs<TItem>>", DefaultValue = "", Description = "Raised when a row is dropped onto another row during reordering.", LinkType = LinkType.Link, Href = "#BitDataGridRowReorderEventArgs" },
        new() { Name = "SelectionMode", Type = "BitDataGridSelectionMode", DefaultValue = "BitDataGridSelectionMode.None", Description = "How rows can be selected (None/Single/Multiple).", LinkType = LinkType.Link, Href = "#BitDataGridSelectionMode" },
        new() { Name = "SelectedItems", Type = "IReadOnlyList<TItem>?", DefaultValue = "null", Description = "The selected items (supports two-way binding)." },
        new() { Name = "SelectedItemsChanged", Type = "EventCallback<IReadOnlyList<TItem>>", DefaultValue = "", Description = "Raised when the selection changes." },
        new() { Name = "OnRowClick", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when a row is clicked." },
        new() { Name = "OnCellClick", Type = "EventCallback<BitDataGridCellEventArgs<TItem>>", DefaultValue = "", Description = "Raised when a data cell is clicked.", LinkType = LinkType.Link, Href = "#BitDataGridCellEventArgs" },
        new() { Name = "OnCellDoubleClick", Type = "EventCallback<BitDataGridCellEventArgs<TItem>>", DefaultValue = "", Description = "Raised when a data cell is double-clicked.", LinkType = LinkType.Link, Href = "#BitDataGridCellEventArgs" },
        new() { Name = "OnCellContextMenu", Type = "EventCallback<BitDataGridCellEventArgs<TItem>>", DefaultValue = "", Description = "Raised when a data cell is right-clicked.", LinkType = LinkType.Link, Href = "#BitDataGridCellEventArgs" },
        new() { Name = "IsRowSelectionDisabled", Type = "Func<TItem, bool>?", DefaultValue = "null", Description = "Predicate returning true when a given row may not be selected." },
        new() { Name = "Pageable", Type = "bool", DefaultValue = "false", Description = "Enables paging with a pager UI." },
        new() { Name = "PageSize", Type = "int", DefaultValue = "20", Description = "The number of rows per page." },
        new() { Name = "PageSizeOptions", Type = "int[]", DefaultValue = "{ 10, 20, 50, 100 }", Description = "The page-size options offered in the pager dropdown." },
        new() { Name = "PagerPosition", Type = "BitDataGridPagerPosition", DefaultValue = "BitDataGridPagerPosition.Bottom", Description = "Where the pager renders relative to the grid.", LinkType = LinkType.Link, Href = "#BitDataGridPagerPosition" },
        new() { Name = "Virtualize", Type = "bool", DefaultValue = "false", Description = "Renders only the visible rows for large datasets. Requires a fixed Height and RowHeight." },
        new() { Name = "RowHeight", Type = "float", DefaultValue = "36", Description = "Uniform row height in pixels (required when virtualizing)." },
        new() { Name = "RowHeightSelector", Type = "Func<TItem, float>?", DefaultValue = "null", Description = "Optional per-row height selector (ignored while virtualizing)." },
        new() { Name = "Editable", Type = "bool", DefaultValue = "false", Description = "Enables inline editing with a command column." },
        new() { Name = "NewItemFactory", Type = "Func<TItem>?", DefaultValue = "null", Description = "Factory used by the toolbar Add button to create a new row." },
        new() { Name = "OnRowSave", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when an edited row is saved." },
        new() { Name = "OnRowCancel", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when an edit is cancelled." },
        new() { Name = "OnRowDelete", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when a row is deleted." },
        new() { Name = "OnRowCreate", Type = "EventCallback<TItem>", DefaultValue = "", Description = "Raised when a new row is created." },
        new() { Name = "EmptyTemplate", Type = "RenderFragment?", DefaultValue = "null", Description = "Custom content rendered when there is no data." },
        new() { Name = "ToolbarTemplate", Type = "RenderFragment?", DefaultValue = "null", Description = "Custom content rendered in the toolbar's start area." },
        new() { Name = "DetailTemplate", Type = "RenderFragment<TItem>?", DefaultValue = "null", Description = "Expandable master-detail content rendered under a row." },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new() { Name = "RefreshAsync", Type = "Task", DefaultValue = "", Description = "Recomputes the data view (filter → sort → group → page) and re-renders the grid." },
        new() { Name = "ClearFiltersAsync", Type = "Task", DefaultValue = "", Description = "Clears all active column filters and refreshes." },
        new() { Name = "ClearGroupsAsync", Type = "Task", DefaultValue = "", Description = "Removes all active groupings and refreshes." },
        new() { Name = "ExpandAllAsync", Type = "Task", DefaultValue = "", Description = "Expands every node in the tree. No-op outside tree mode." },
        new() { Name = "CollapseAllAsync", Type = "Task", DefaultValue = "", Description = "Collapses every node in the tree. No-op outside tree mode." },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "BitDataGridColumn",
            Title = "BitDataGridColumn",
            Description = "Defines a column inside a BitDataGrid. Place these as child content of the grid.",
            Parameters =
            [
                new() { Name = "Field", Type = "string?", DefaultValue = "null", Description = "Name of the property this column is bound to. Supports nested paths (\"Address.City\")." },
                new() { Name = "ColumnId", Type = "string?", DefaultValue = "null", Description = "Stable identifier for the column. Defaults to Field." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "Header text. Defaults to a humanized Field." },
                new() { Name = "Width", Type = "string?", DefaultValue = "null", Description = "CSS width, e.g. \"120px\" or \"20%\". When null the column shares remaining space." },
                new() { Name = "MinWidth", Type = "int", DefaultValue = "60", Description = "Minimum width in pixels the column can be resized to." },
                new() { Name = "MaxWidth", Type = "int?", DefaultValue = "null", Description = "Maximum width in pixels the column can be resized to." },
                new() { Name = "Sortable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level Sortable for this column." },
                new() { Name = "SortDescendingFirst", Type = "bool", DefaultValue = "false", Description = "When true, the first click on the header sorts descending instead of ascending." },
                new() { Name = "Filterable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level Filterable for this column." },
                new() { Name = "Resizable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level Resizable for this column." },
                new() { Name = "Reorderable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level Reorderable for this column." },
                new() { Name = "Editable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level Editable for this column." },
                new() { Name = "Groupable", Type = "bool?", DefaultValue = "null", Description = "Overrides the grid-level Groupable for this column." },
                new() { Name = "Frozen", Type = "bool", DefaultValue = "false", Description = "Pins the column to the start edge so it stays visible while scrolling horizontally." },
                new() { Name = "Group", Type = "string?", DefaultValue = "null", Description = "Optional header group name. Consecutive columns sharing the same value render under a single spanning header cell." },
                new() { Name = "ColSpan", Type = "Func<TItem, int?>?", DefaultValue = "null", Description = "Optional per-row column span." },
                new() { Name = "Visible", Type = "bool", DefaultValue = "true", Description = "Whether the column is visible." },
                new() { Name = "Align", Type = "BitDataGridColumnAlign", DefaultValue = "BitDataGridColumnAlign.Left", Description = "Horizontal alignment of cell content.", LinkType = LinkType.Link, Href = "#BitDataGridColumnAlign" },
                new() { Name = "Format", Type = "string?", DefaultValue = "null", Description = "A .NET format string applied to the value (e.g. \"C2\", \"yyyy-MM-dd\")." },
                new() { Name = "DataType", Type = "BitDataGridColumnDataType", DefaultValue = "BitDataGridColumnDataType.Auto", Description = "The data type used to pick the editor/filter.", LinkType = LinkType.Link, Href = "#BitDataGridColumnDataType" },
                new() { Name = "Aggregate", Type = "BitDataGridAggregateType", DefaultValue = "BitDataGridAggregateType.None", Description = "The footer/group aggregate function.", LinkType = LinkType.Link, Href = "#BitDataGridAggregateType" },
                new() { Name = "AggregateFormat", Type = "string?", DefaultValue = "null", Description = "Format string for the aggregate value. Falls back to Format." },
                new() { Name = "HeaderClass", Type = "string?", DefaultValue = "null", Description = "Custom CSS class applied to the header cell." },
                new() { Name = "CellClass", Type = "string?", DefaultValue = "null", Description = "Custom CSS class applied to each data cell." },
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
                new() { Name = "Operator", Type = "BitDataGridFilterOperator", DefaultValue = "BitDataGridFilterOperator.Contains", Description = "The comparison operator applied to the value.", LinkType = LinkType.Link, Href = "#BitDataGridFilterOperator" },
                new() { Name = "Value", Type = "object?", DefaultValue = "null", Description = "The value compared against the column's cell value." },
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
            Id = "BitDir",
            Name = "BitDir",
            Description = "Determines the component's direction (Ltr/Rtl/Auto).",
            Items =
            [
                new() { Name = "Ltr", Value = "0" },
                new() { Name = "Rtl", Value = "1" },
                new() { Name = "Auto", Value = "2" },
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
            ]
        },
    ];
}
