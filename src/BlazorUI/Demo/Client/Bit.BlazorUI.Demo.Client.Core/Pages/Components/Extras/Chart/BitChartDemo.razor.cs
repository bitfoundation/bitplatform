namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class BitChartDemo
{
    [CascadingParameter(Name = nameof(RenderForMcpClient))] public bool RenderForMcpClient { get; set; }

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitChartClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the parts of the chart: root, title, subtitle, legend, legend items, plot, tooltip, empty state and loading state.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Config",
            Type = "BitChartConfig?",
            DefaultValue = "null",
            Description = "Full configuration (type + data + options). Takes precedence over Type/Data/Options when set.",
            LinkType = LinkType.Link,
            Href = "#chart-config"
        },
        new()
        {
            Name = "Data",
            Type = "BitChartData?",
            DefaultValue = "null",
            Description = "The chart data: labels and datasets.",
            LinkType = LinkType.Link,
            Href = "#chart-data"
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "A visually hidden summary of what the chart shows - its trend or takeaway - that the plot is described by: the long description a complex image needs beside its name and its data table."
        },
        new()
        {
            Name = "GenerateTable",
            Type = "bool",
            DefaultValue = "true",
            Description = "Renders a visually hidden data table right after the chart, which a screen reader can browse cell by cell."
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "Optional CSS height of the chart container. When null the height follows the aspect ratio."
        },
        new()
        {
            Name = "IsLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows a loading veil with a spinner over the plot, marks the chart aria-busy and holds back the empty state, so a chart still waiting for its data does not claim it has none."
        },
        new()
        {
            Name = "LoadingLabel",
            Type = "string?",
            DefaultValue = "\"Loading\"",
            Description = "The text shown under the spinner, and announced, while IsLoading is set."
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom content shown in place of the default spinner while IsLoading is set."
        },
        new()
        {
            Name = "MaxTableColumns",
            Type = "int",
            DefaultValue = "100",
            Description = "Upper bound on the columns the screen-reader table renders. A value series is one row with a cell per category, so a long one is wide rather than tall and the row cap alone would not contain it. Ignored for scatter and bubble data, whose table is three fixed columns."
        },
        new()
        {
            Name = "MaxTableRows",
            Type = "int",
            DefaultValue = "500",
            Description = "Upper bound on the rows the screen-reader table renders, so a long series does not put tens of thousands of hidden nodes in the DOM. Past the limit the caption says how many rows were left out."
        },
        new()
        {
            Name = "NavigationHint",
            Type = "string?",
            DefaultValue = "\"Interactive chart. Use the left and right arrow keys...\"",
            Description = "A visually hidden sentence telling a screen-reader user how to walk the data, pointed at by aria-describedby. Only rendered while there is data to navigate; set it to null to leave it out."
        },
        new()
        {
            Name = "NoDataTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom content shown in place of the plot when there is nothing to draw. Takes precedence over NoDataText."
        },
        new()
        {
            Name = "NoDataText",
            Type = "string",
            DefaultValue = "No data to display",
            Description = "Message shown in place of the plot when the configuration produces nothing to draw."
        },
        new()
        {
            Name = "OnElementClick",
            Type = "EventCallback<(int DatasetIndex, int DataIndex)>",
            Description = "Callback raised when a data element (point, bar, arc, ...) is clicked, by pointer or with Enter/Space while it is focused."
        },
        new()
        {
            Name = "OnElementHover",
            Type = "EventCallback<BitChartTooltipContext?>",
            Description = "Callback raised when the active (hovered or keyboard-focused) element set changes. The context is null once nothing is active."
        },
        new()
        {
            Name = "OnLegendItemClick",
            Type = "EventCallback<BitChartLegendItemModel>",
            Description = "Callback raised when a legend item is clicked, before the default visibility toggle runs."
        },
        new()
        {
            Name = "OnZoomChange",
            Type = "EventCallback",
            Description = "Callback raised after zoom or pan changes the visible axis ranges."
        },
        new()
        {
            Name = "Options",
            Type = "BitChartOptions?",
            DefaultValue = "null",
            Description = "The chart options: scales, plugins (title, legend, tooltip, data labels), interaction, animation, culture and zoom.",
            LinkType = LinkType.Link,
            Href = "#chart-options"
        },
        new()
        {
            Name = "Styles",
            Type = "BitChartClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the parts of the chart: root, title, subtitle, legend, legend items, plot, tooltip, empty state and loading state.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Texts",
            Type = "BitChartTexts?",
            DefaultValue = "null",
            Description = "The texts written for assistive technologies and into the CSV export: the default accessible name, the legend's name, the keyboard announcements, the table headers and captions. English by default; override them to localize.",
            LinkType = LinkType.Link,
            Href = "#chart-texts"
        },
        new()
        {
            Name = "TooltipTemplate",
            Type = "RenderFragment<BitChartTooltipContext>?",
            DefaultValue = "null",
            Description = "Optional custom tooltip template. When set it replaces the default tooltip body."
        },
        new()
        {
            Name = "Type",
            Type = "BitChartType",
            DefaultValue = "BitChartType.Line",
            Description = "The chart type: Line, Bar, Radar, Pie, Doughnut, PolarArea, Bubble or Scatter."
        },
        new()
        {
            Name = "Width",
            Type = "string",
            DefaultValue = "100%",
            Description = "CSS width of the chart container."
        },
        new()
        {
            Name = "ZoomHint",
            Type = "string?",
            DefaultValue = "\"Press plus or minus to zoom, and 0 to reset the zoom.\"",
            Description = "Appended to NavigationHint while zoom is enabled, naming the zoom keys. Set it to null to leave it out."
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Chart-font-family",
            DefaultValue = "--bit-tpg-font-family",
            Description = "Typeface of every text the chart draws: title, legend, ticks, labels and tooltip.",
        },
        new()
        {
            Name = "--bit-Chart-series-color-1 ... --bit-Chart-series-color-10",
            DefaultValue = "The Chart.js palette (#36a2eb, #ff6384, #4bc0c0, ...)",
            Description = "The default palette, one color per series - or per slice of a pie, doughnut or polar area. Only used where the dataset names no color; its translucent area and hover shades are derived from it.",
        },
        new()
        {
            Name = "--bit-Chart-title-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Color of the title.",
        },
        new()
        {
            Name = "--bit-Chart-subtitle-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the subtitle.",
        },
        new()
        {
            Name = "--bit-Chart-legend-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the legend labels.",
        },
        new()
        {
            Name = "--bit-Chart-tick-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the axis tick labels and the radar / polar point labels.",
        },
        new()
        {
            Name = "--bit-Chart-axis-title-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the axis titles.",
        },
        new()
        {
            Name = "--bit-Chart-axis-color",
            DefaultValue = "--bit-clr-brd-pri",
            Description = "Color of the axis border lines.",
        },
        new()
        {
            Name = "--bit-Chart-grid-color",
            DefaultValue = "--bit-clr-brd-sec",
            Description = "Color of the grid lines, the tick marks and the radar angle lines. Minor grid lines draw it at 40%.",
        },
        new()
        {
            Name = "--bit-Chart-data-label-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Color of the data labels.",
        },
        new()
        {
            Name = "--bit-Chart-crosshair-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the crosshair through the hovered index.",
        },
        new()
        {
            Name = "--bit-Chart-annotation-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Default color of an annotation's line or outline, its translucent fill and its label pill. BitChartAnnotation.Color overrides it.",
        },
        new()
        {
            Name = "--bit-Chart-annotation-label-color",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Default text color of an annotation's label. BitChartAnnotation.LabelColor overrides it.",
        },
        new()
        {
            Name = "--bit-Chart-center-text-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Color of the text BitChartCenterTextPlugin draws in a doughnut's cutout.",
        },
        new()
        {
            Name = "--bit-Chart-surface-color",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "The surface the chart sits on: the border that separates pie and doughnut slices, and the backdrop behind radial tick labels. Set it to the card color when the chart sits on a card.",
        },
        new()
        {
            Name = "--bit-Chart-tooltip-background",
            DefaultValue = "--bit-clr-tooltip-bg",
            Description = "Fill of the tooltip and its caret, and of the crosshair's axis chip.",
        },
        new()
        {
            Name = "--bit-Chart-tooltip-color",
            DefaultValue = "--bit-clr-tooltip-fg",
            Description = "Text color of the tooltip and of the crosshair's axis chip.",
        },
        new()
        {
            Name = "--bit-Chart-tooltip-radius",
            DefaultValue = "--bit-shp-radius-popup",
            Description = "Corner radius of the tooltip. Tooltip.CornerRadius in the options overrides it.",
        },
        new()
        {
            Name = "--bit-Chart-tooltip-shadow",
            DefaultValue = "--bit-shd-tooltip",
            Description = "Elevation of the tooltip.",
        },
        new()
        {
            Name = "--bit-Chart-focus-color",
            DefaultValue = "--bit-clr-pri-focus",
            Description = "Focus ring of the plot and the legend items, and the outline of the element the keyboard is on.",
        },
        new()
        {
            Name = "--bit-Chart-zoom-box-background",
            DefaultValue = "--bit-clr-pri at 20%",
            Description = "Fill of the drag-to-zoom selection box.",
        },
        new()
        {
            Name = "--bit-Chart-zoom-box-border-color",
            DefaultValue = "--bit-clr-pri",
            Description = "Border of the drag-to-zoom selection box.",
        },
        new()
        {
            Name = "--bit-Chart-inactive-opacity",
            DefaultValue = "0.2",
            Description = "Opacity of the series faded behind a legend item that is hovered or focused (Legend.HighlightOnHover).",
        },
        new()
        {
            Name = "--bit-Chart-no-data-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the empty-state message.",
        },
        new()
        {
            Name = "--bit-Chart-loading-background",
            DefaultValue = "--bit-clr-bg-pri at 70%",
            Description = "The veil laid over the plot while IsLoading is set; translucent, so the chart behind it shows through.",
        },
        new()
        {
            Name = "--bit-Chart-loading-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the loading label.",
        },
        new()
        {
            Name = "--bit-Chart-loading-spinner-color",
            DefaultValue = "--bit-clr-pri",
            Description = "Color of the turning arc of the loading spinner.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Refresh",
            Type = "void Refresh()",
            Description = "Rebuilds and redraws the chart from its current data and options. Blazor only re-renders on a parameter change it can see, so mutating the same BitChartData in place - appending to a live series, editing a value - needs this call. The counterpart of Chart.js's chart.update()."
        },
        new()
        {
            Name = "IsDatasetVisible",
            Type = "bool IsDatasetVisible(int datasetIndex)",
            Description = "Whether a dataset is currently drawn, i.e. hidden neither through the legend nor by BitChartDataset.Hidden."
        },
        new()
        {
            Name = "SetDatasetVisible",
            Type = "void SetDatasetVisible(int datasetIndex, bool visible)",
            Description = "Shows or hides a dataset, exactly as clicking its legend entry would; the axes re-scale around what is left."
        },
        new()
        {
            Name = "ToggleDataset",
            Type = "void ToggleDataset(int datasetIndex)",
            Description = "Flips a dataset between shown and hidden."
        },
        new()
        {
            Name = "IsDataIndexVisible",
            Type = "bool IsDataIndexVisible(int dataIndex)",
            Description = "Whether a data index - a pie, doughnut or polar-area slice - is currently drawn."
        },
        new()
        {
            Name = "SetDataIndexVisible",
            Type = "void SetDataIndexVisible(int dataIndex, bool visible)",
            Description = "Shows or hides one data index across the chart, the slice-level counterpart of SetDatasetVisible."
        },
        new()
        {
            Name = "ToggleDataIndex",
            Type = "void ToggleDataIndex(int dataIndex)",
            Description = "Flips one data index between shown and hidden."
        },
        new()
        {
            Name = "ResetVisibility",
            Type = "void ResetVisibility()",
            Description = "Brings back every dataset and data index hidden through the legend or the API."
        },
        new()
        {
            Name = "ResetZoom",
            Type = "void ResetZoom()",
            Description = "Clears every zoom/pan override and returns the chart to the full data range."
        },
        new()
        {
            Name = "ZoomTo",
            Type = "void ZoomTo(string axisId, double? min, double? max)",
            Description = "Zooms an axis to an explicit value range, honoring the configured zoom limits. Pass null bounds to clear that axis's override."
        },
        new()
        {
            Name = "GetAxisRange",
            Type = "(double Min, double Max)? GetAxisRange(string axisId)",
            Description = "The currently visible range of an axis: its zoomed range when zoomed, otherwise the full data range."
        },
        new()
        {
            Name = "ExportSvgAsync",
            Type = "Task<bool> ExportSvgAsync(string? fileName = null, string? backgroundColor = null)",
            Description = "Downloads the chart as a standalone .svg file, with the theme tokens it references resolved into the file."
        },
        new()
        {
            Name = "ExportPngAsync",
            Type = "Task<bool> ExportPngAsync(string? fileName = null, double scale = 2, string? backgroundColor = SurfaceBackground)",
            Description = "Downloads the chart as a .png image rasterized from the live SVG at the given pixel ratio, on the surface the chart sits on (so a dark theme exports dark); pass a color of your own, or null for a transparent image."
        },
        new()
        {
            Name = "ExportCsvAsync",
            Type = "Task<bool> ExportCsvAsync(string? fileName = null)",
            Description = "Downloads the chart's data as a .csv file, formatted with the chart's culture."
        },
        new()
        {
            Name = "ToCsv",
            Type = "string ToCsv()",
            Description = "Returns the chart's data as CSV text: one row per series for value datasets, one row per point for scatter and bubble datasets."
        },
        new()
        {
            Name = "ToSvgStringAsync",
            Type = "Task<string?> ToSvgStringAsync(string? backgroundColor = null)",
            Description = "Returns the chart as standalone SVG markup instead of downloading it, with the theme tokens it references resolved into the markup. Null when the chart has not been rendered in a browser yet."
        },
        new()
        {
            Name = "ToBase64ImageAsync",
            Type = "Task<string?> ToBase64ImageAsync(string mimeType = \"image/png\", double scale = 2, string? backgroundColor = SurfaceBackground)",
            Description = "Returns the rasterized chart as a data: URL - the same picture ExportPngAsync downloads - ready for an img src or a PDF. Mirrors Chart.js's toBase64Image."
        },
        new()
        {
            Name = "SurfaceBackground",
            Type = "const string",
            DefaultValue = "\"var(--bit-Chart-surface-color, var(--bit-clr-bg-pri))\"",
            Description = "The default background of ExportPngAsync and ToBase64ImageAsync: the surface the chart sits on, resolved in the browser."
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitChartClassStyles",
            Description = "Defines per-part CSS class/style values for BitChart.",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the root element." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the title." },
                new() { Name = "Subtitle", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the subtitle." },
                new() { Name = "Legend", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the legend container." },
                new() { Name = "LegendItem", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to each legend item." },
                new() { Name = "Plot", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the plot container that holds the SVG, the tooltip and the empty state." },
                new() { Name = "Tooltip", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the tooltip box, the default one and a custom template's alike." },
                new() { Name = "NoData", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the empty state shown when there is nothing to draw." },
                new() { Name = "Loading", Type = "string?", DefaultValue = "null", Description = "Custom class or style applied to the loading state shown over the plot while the chart is loading." },
            ]
        },
        new()
        {
            Id = "chart-texts",
            Title = "BitChartTexts",
            Description = "The texts BitChart writes for assistive technologies and into its CSV export. All default to English; the ...Format ones are composite format strings filled in the chart's culture.",
            Parameters =
            [
                new() { Name = "DefaultAriaLabelFormat", Type = "string", DefaultValue = "\"{0} chart with {1} data series.\"", Description = "Accessible name of a chart with neither an AriaLabel nor a displayed title. {0} is the chart type, {1} the number of series." },
                new() { Name = "RoleDescription", Type = "string", DefaultValue = "\"chart\"", Description = "What a screen reader calls the plot in place of its role (aria-roledescription)." },
                new() { Name = "TypeNames", Type = "Dictionary<BitChartType, string>", DefaultValue = "\"Line\", \"Bar\", ... \"Polar area\"", Description = "The name of each chart type, filled into {0} of DefaultAriaLabelFormat." },
                new() { Name = "NotesFormat", Type = "string", DefaultValue = "\"Marked on the chart: {0}.\"", Description = "The hidden sentence the plot is described by that names what the plugins drew - annotations, labeled trend lines, a center text. {0} is the list of them." },
                new() { Name = "LegendAriaLabel", Type = "string", DefaultValue = "\"Chart legend\"", Description = "Accessible name of a legend that has no title of its own." },
                new() { Name = "PositionFormat", Type = "string", DefaultValue = "\"{0} of {1}\"", Description = "Announced after the keyboard-focused value: its position within its series." },
                new() { Name = "SeriesPositionFormat", Type = "string", DefaultValue = "\"series {0} of {1}\"", Description = "Announced when the chart has more than one series: which one the focused value belongs to." },
                new() { Name = "DatasetLabelFormat", Type = "string", DefaultValue = "\"Dataset {0}\"", Description = "Name of a dataset without a Label, in the legend, the table and the CSV alike. {0} is the 1-based dataset number." },
                new() { Name = "Series", Type = "string", DefaultValue = "\"Series\"", Description = "Header of the series column of the table and the CSV." },
                new() { Name = "X / Y", Type = "string", DefaultValue = "\"X\" / \"Y\"", Description = "Headers of the point columns of scatter and bubble data, used when the axis shows no title of its own." },
                new() { Name = "Radius", Type = "string", DefaultValue = "\"R\"", Description = "Header of the radius column of bubble data." },
                new() { Name = "RowsTruncatedFormat / ColumnsTruncatedFormat", Type = "string", DefaultValue = "\"Showing the first {0} of {1} rows.\" / \"... columns.\"", Description = "Appended to the table's caption when MaxTableRows / MaxTableColumns cut it short." },
            ]
        },
        new()
        {
            Id = "chart-config",
            Title = "BitChartConfig",
            Description = "A complete chart configuration bundling the type, data and options.",
            Parameters =
            [
                new()
                {
                    Name = "Type",
                    Type = "BitChartType",
                    DefaultValue = "BitChartType.Line",
                    Description = "The chart type."
                },
                new()
                {
                    Name = "Data",
                    Type = "BitChartData",
                    DefaultValue = "new()",
                    Description = "The labels and datasets."
                },
                new()
                {
                    Name = "Options",
                    Type = "BitChartOptions",
                    DefaultValue = "new()",
                    Description = "The scales, plugins, interaction, animation and zoom options."
                },
            ]
        },
        new()
        {
            Id = "chart-data",
            Title = "BitChartData",
            Description = "The chart data, mirroring Chart.js data: labels + datasets.",
            Parameters =
            [
                new()
                {
                    Name = "Labels",
                    Type = "List<string>",
                    DefaultValue = "new()",
                    Description = "The category labels shared by the datasets (used by cartesian, radar, pie and polar charts)."
                },
                new()
                {
                    Name = "Datasets",
                    Type = "List<BitChartDataset>",
                    DefaultValue = "new()",
                    Description = "The datasets to render. Each dataset carries either a list of values (Data) or points (Points).",
                    LinkType = LinkType.Link,
                    Href = "#chart-dataset"
                },
            ]
        },
        new()
        {
            Id = "chart-dataset",
            Title = "BitChartDataset",
            Description = "A single dataset, mirroring Chart.js dataset configuration. Colors, radii and styles marked *Fn are scriptable: they receive a BitChartScriptableContext per element and take precedence over the constant beside them.",
            Parameters =
            [
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Dataset label shown in legends and tooltips."
                },
                new()
                {
                    Name = "Data",
                    Type = "List<double?>",
                    DefaultValue = "new()",
                    Description = "Per-index values (line, bar, radar, pie, doughnut, polarArea). A null is a gap, not a zero."
                },
                new()
                {
                    Name = "Points",
                    Type = "List<BitChartDataPoint>?",
                    DefaultValue = "null",
                    Description = "Point data (x, y[, r]) for scatter, bubble and time-based line charts. When set, takes precedence over Data."
                },
                new()
                {
                    Name = "RangeData",
                    Type = "List<(double Low, double High)?>?",
                    DefaultValue = "null",
                    Description = "Floating-bar ranges per index. When set, bars span low to high instead of growing from the base."
                },
                new()
                {
                    Name = "Type",
                    Type = "BitChartType?",
                    DefaultValue = "null",
                    Description = "Optional per-dataset type override, used to build mixed charts."
                },
                new()
                {
                    Name = "BackgroundColor",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The fill color of the dataset: bars, arcs, points and the area fill of a filled line."
                },
                new()
                {
                    Name = "BackgroundColors",
                    Type = "List<string>?",
                    DefaultValue = "null",
                    Description = "One fill color per data index, cycled when shorter than the data."
                },
                new()
                {
                    Name = "BorderColor",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The line/border color. Bars and arcs fall back to their own fill color rather than an unrelated palette entry."
                },
                new()
                {
                    Name = "BorderWidth",
                    Type = "double?",
                    DefaultValue = "null",
                    Description = "Border/line thickness. When null a per-type default applies: 3 for lines and radar, 2 for arcs, and 0 for bars unless a border color was given."
                },
                new()
                {
                    Name = "Fill",
                    Type = "BitChartFillMode",
                    DefaultValue = "BitChartFillMode.None",
                    Description = "Area fill mode for line/radar datasets (None, Origin, Start, End, Stack, Dataset, Value)."
                },
                new()
                {
                    Name = "FillGradient",
                    Type = "BitChartGradientBase?",
                    DefaultValue = "null",
                    Description = "A linear or radial gradient used for the area fill, taking precedence over FillColor and BackgroundColor."
                },
                new()
                {
                    Name = "BackgroundPattern",
                    Type = "BitChartFillPattern?",
                    DefaultValue = "null",
                    Description = "A repeating hatch/grid/dot texture used instead of a solid fill; keeps series distinguishable in print and greyscale."
                },
                new()
                {
                    Name = "Tension",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "Bezier curve tension for line datasets (0 = straight lines)."
                },
                new()
                {
                    Name = "Stepped",
                    Type = "BitChartSteppedLine",
                    DefaultValue = "BitChartSteppedLine.False",
                    Description = "Draws the line as steps (Before, After or Middle) instead of interpolating."
                },
                new()
                {
                    Name = "SpanGaps",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Bridges null values instead of breaking the line at them."
                },
                new()
                {
                    Name = "Segment",
                    Type = "BitChartLineSegmentStyle?",
                    DefaultValue = "null",
                    Description = "Per-segment color, width and dash callbacks, evaluated from the two endpoints of each segment."
                },
                new()
                {
                    Name = "PointRadius",
                    Type = "double",
                    DefaultValue = "3",
                    Description = "Marker radius. Zero hides the marker but keeps the point hoverable."
                },
                new()
                {
                    Name = "PointStyle",
                    Type = "BitChartPointStyle",
                    DefaultValue = "BitChartPointStyle.Circle",
                    Description = "Marker shape. BitChartPointStyle.None removes the markers - and their hit targets - entirely."
                },
                new()
                {
                    Name = "Stack",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Stack group id. Datasets sharing an id accumulate together; each group gets its own column."
                },
                new()
                {
                    Name = "Grouped",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "When false the bar dataset leaves the side-by-side layout and keeps the whole category band, so it can sit behind the others."
                },
                new()
                {
                    Name = "SkipNull",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Lets the remaining bars of a category widen over the datasets that have no value there, instead of leaving a hole."
                },
                new()
                {
                    Name = "MinBarLength",
                    Type = "double?",
                    DefaultValue = "null",
                    Description = "Minimum bar length in pixels, so near-zero values stay visible."
                },
                new()
                {
                    Name = "Base",
                    Type = "double?",
                    DefaultValue = "null",
                    Description = "The value bars grow from. Defaults to zero clamped into the axis range."
                },
                new()
                {
                    Name = "BorderRadius",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "Corner radius. On a bar only the corners away from the skipped (baseline) edge are rounded, and BorderRadiusCorners overrides each corner; on a pie, doughnut or polar-area arc it rounds the arc's own corners, clamped to half the ring's thickness."
                },
                new()
                {
                    Name = "Offset / SpacingArc / HoverOffset",
                    Type = "double",
                    DefaultValue = "0 / 0 / 6",
                    Description = "Arc geometry: how far every slice sits from the center, the gap left between neighbouring slices, and the extra distance the hovered slice pops out."
                },
                new()
                {
                    Name = "Weight",
                    Type = "double",
                    DefaultValue = "1",
                    Description = "Relative thickness of this dataset's ring in a multi-dataset pie or doughnut. The available radius is shared out in proportion to the weights."
                },
                new()
                {
                    Name = "ErrorData",
                    Type = "List<BitChartErrorBar?>?",
                    DefaultValue = "null",
                    Description = "Per-index uncertainty, drawn as a capped whisker through the value and named in the tooltip. A BitChartErrorBar comes from one number (symmetric) or two (asymmetric); a null entry leaves that value bare. Cartesian charts only."
                },
                new()
                {
                    Name = "ErrorBarColor / ErrorBarWidth / ErrorBarCapWidth",
                    Type = "string? / double / double",
                    DefaultValue = "null / 1.5 / 8",
                    Description = "Error-bar styling. A null color follows the primary foreground token; a zero cap width draws a bare whisker."
                },
                new()
                {
                    Name = "HoverBackgroundColor / HoverBorderColor / HoverBorderWidth",
                    Type = "string? / string? / double?",
                    DefaultValue = "null",
                    Description = "Styling used while a bar or arc is hovered or keyboard-focused."
                },
                new()
                {
                    Name = "XAxisID / YAxisID / RAxisID",
                    Type = "string",
                    DefaultValue = "x / y / r",
                    Description = "The scales this dataset is bound to. Naming a scale that does not exist yet creates a linear one."
                },
                new()
                {
                    Name = "Order",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "Draw order across datasets; lower draws first. Bars are always drawn before lines and points."
                },
                new()
                {
                    Name = "Hidden",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Hides the dataset without removing it, and marks its legend entry as toggled off."
                },
            ]
        },
        new()
        {
            Id = "chart-options",
            Title = "BitChartOptions",
            Description = "Top-level chart options, mirroring Chart.js options. The same instance can safely be shared between charts: the renderer completes the missing scales locally instead of writing them back.",
            Parameters =
            [
                new()
                {
                    Name = "Responsive",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Observes the container and renders at real device pixels, which keeps font sizes constant at any width."
                },
                new()
                {
                    Name = "MaintainAspectRatio / AspectRatio",
                    Type = "bool / double?",
                    DefaultValue = "true / null",
                    Description = "Whether the height follows the width, and the ratio to use. Defaults to 2 for cartesian charts and 1 for circular and radar ones."
                },
                new()
                {
                    Name = "IndexAxis",
                    Type = "BitChartIndexAxis",
                    DefaultValue = "BitChartIndexAxis.X",
                    Description = "The axis the data index runs along: X for vertical bars, Y for horizontal ones."
                },
                new()
                {
                    Name = "Sparkline",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Draws the chart as a sparkline: axes, grid, tick labels, legend, title and subtitle are all dropped so the series fills the box. A presentation switch only - tooltips, keyboard navigation and the screen-reader table still describe the full series."
                },
                new()
                {
                    Name = "Scales",
                    Type = "Dictionary<string, BitChartScaleOptions>",
                    DefaultValue = "new()",
                    Description = "Named scales keyed by id (x, y, r, y2, ...): type, min/max, grid, ticks, title, stacking, time unit and radial options."
                },
                new()
                {
                    Name = "Interaction",
                    Type = "BitChartInteractionOptions",
                    DefaultValue = "new()",
                    Description = "Mode (Nearest, Index, Dataset, ...) and Intersect. With Intersect false - the default - hit bands make the whole plot hoverable, marked by a crosshair and an axis chip (Crosshair / CrosshairLabel / CrosshairColor). The tooltip inherits Mode and Intersect unless it overrides them."
                },
                new()
                {
                    Name = "Plugins",
                    Type = "BitChartPluginOptions",
                    DefaultValue = "new()",
                    Description = "Title, Subtitle, Legend, Tooltip, DataLabels and Decimation options, plus Custom for your own IBitChartPlugin drawing plugins."
                },
                new()
                {
                    Name = "Animation",
                    Type = "BitChartAnimationOptions",
                    DefaultValue = "new()",
                    Description = "Duration, easing, per-element stagger (DelayBetween) and the progressive draw-on for line charts."
                },
                new()
                {
                    Name = "Elements",
                    Type = "BitChartElementOptions",
                    DefaultValue = "new()",
                    Description = "Per-type defaults used whenever a dataset leaves the matching property unset."
                },
                new()
                {
                    Name = "Layout",
                    Type = "BitChartLayoutOptions",
                    DefaultValue = "new()",
                    Description = "Padding around the whole chart."
                },
                new()
                {
                    Name = "Zoom",
                    Type = "BitChartZoomOptions",
                    DefaultValue = "new()",
                    Description = "Wheel zoom, drag pan, drag-to-zoom box, axis mode, speed, and the limits that keep the view inside the data."
                },
                new()
                {
                    Name = "Culture",
                    Type = "CultureInfo?",
                    DefaultValue = "null",
                    Description = "Culture used for every number and date the chart prints - ticks, tooltips, data labels and the CSV export. Null means the invariant culture."
                },
                new()
                {
                    Name = "CutoutPercentage / CircumferenceDegrees / RotationDegrees",
                    Type = "double",
                    DefaultValue = "50 / 360 / -90",
                    Description = "Doughnut hole size, sweep and starting angle. A 180 degree sweep turns a doughnut into a gauge."
                },
            ]
        },
        new()
        {
            Id = "chart-annotation",
            Title = "BitChartAnnotation",
            Description = "One line, box, ellipse, polygon, point or label drawn in data coordinates by BitChartAnnotationPlugin, which is registered through Options.Plugins.Custom. Cartesian charts only.",
            Parameters =
            [
                new()
                {
                    Name = "Kind",
                    Type = "BitChartAnnotationKind",
                    DefaultValue = "Line",
                    Description = "The shape: Line, Box, Point, Label, Ellipse or Polygon."
                },
                new()
                {
                    Name = "Orientation / Value / AxisId",
                    Type = "BitChartLineOrientation / double / string",
                    DefaultValue = "Horizontal / 0 / \"y\"",
                    Description = "Where a line is drawn: a y value for a horizontal line, an x value (or category index with XIsIndex) for a vertical one."
                },
                new()
                {
                    Name = "XMin / XMax / YMin / YMax / XIsIndex",
                    Type = "double? / bool",
                    DefaultValue = "null / false",
                    Description = "The bounds of a box or an ellipse in data coordinates; a null bound runs to the edge of the plot. XIsIndex reads the X values as category indices."
                },
                new()
                {
                    Name = "Sides / Radius / Rotation",
                    Type = "int / double? / double",
                    DefaultValue = "3 / null / 0",
                    Description = "The shape of a polygon, and the pixel radius of a polygon or a point."
                },
                new()
                {
                    Name = "Color / FillColor / LineWidth / Dash",
                    Type = "string / string? / double / List<double>?",
                    DefaultValue = "--bit-Chart-annotation-color / null / 2 / null",
                    Description = "Styling. A null fill is the color made translucent."
                },
                new()
                {
                    Name = "Label / LabelColor / LabelBackground / LabelFont",
                    Type = "string? / string / string? / BitChartFont",
                    DefaultValue = "null / --bit-Chart-annotation-label-color / null / 11px bold",
                    Description = "An optional pill beside the shape. A null background follows Color."
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "What a screen reader is told about the annotation, in place of its label and value. The drawing is hidden from assistive technologies, so an annotation with neither a Label nor a Description is treated as decoration."
                },
                new()
                {
                    Name = "DrawBehindDatasets",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Draws the annotation under the datasets rather than over them."
                },
            ]
        },
        new()
        {
            Id = "chart-trendline",
            Title = "BitChartTrendline",
            Description = "One fitted line drawn over a dataset by BitChartTrendlinePlugin, which is registered through Options.Plugins.Custom. Cartesian charts only.",
            Parameters =
            [
                new()
                {
                    Name = "DatasetIndex",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "Index of the dataset the line is fitted to. A dataset hidden through the legend takes its trend line with it."
                },
                new()
                {
                    Name = "Kind",
                    Type = "BitChartTrendlineKind",
                    DefaultValue = "BitChartTrendlineKind.Linear",
                    Description = "Linear for a least-squares regression, MovingAverage for a trailing average over Period points, or Average for a flat line at the series mean."
                },
                new()
                {
                    Name = "Period",
                    Type = "int",
                    DefaultValue = "5",
                    Description = "Window of the trailing moving average. Ignored by the other kinds."
                },
                new()
                {
                    Name = "Extend",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Projects a straight fit out to both edges of the plot instead of stopping at the first and last data point. Ignored by MovingAverage, which has no meaning outside the data."
                },
                new()
                {
                    Name = "Color / LineWidth / Dash",
                    Type = "string? / double / List<double>?",
                    DefaultValue = "null / 2 / [6, 4]",
                    Description = "Line styling. A null color follows the dataset's own border color; the dash is what keeps the fit from reading as another measured series - set it to null for a solid line."
                },
                new()
                {
                    Name = "Label / LabelColor / LabelBackground / LabelFont",
                    Type = "string? / string / string? / BitChartFont",
                    DefaultValue = "null / #fff / null / 11px bold",
                    Description = "An optional pill drawn at the end of the line, pinned inside the plot so it stays readable at the edge. A labeled line is also named to screen readers."
                },
                new()
                {
                    Name = "DrawBehindDatasets",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Draws the line under the datasets rather than over them."
                },
            ]
        },
    ];
}
