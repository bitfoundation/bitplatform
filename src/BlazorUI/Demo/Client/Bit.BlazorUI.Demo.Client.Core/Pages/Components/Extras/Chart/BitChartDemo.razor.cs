namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class BitChartDemo
{
    [CascadingParameter(Name = nameof(RenderForMcpClient))] public bool RenderForMcpClient { get; set; }

    private readonly List<ComponentParameter> componentParameters =
    [
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
            Name = "Type",
            Type = "BitChartType",
            DefaultValue = "BitChartType.Line",
            Description = "The chart type: Line, Bar, Radar, Pie, Doughnut, PolarArea, Bubble or Scatter."
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
            Name = "Options",
            Type = "BitChartOptions?",
            DefaultValue = "null",
            Description = "The chart options: scales, plugins (title, legend, tooltip, data labels), interaction, animation, culture and zoom.",
            LinkType = LinkType.Link,
            Href = "#chart-options"
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
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "Optional CSS height of the chart container. When null the height follows the aspect ratio."
        },
        new()
        {
            Name = "Class",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom CSS class applied to the root element of the chart."
        },
        new()
        {
            Name = "Style",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom CSS style applied to the root element of the chart."
        },
        new()
        {
            Name = "Id",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the root element of the chart."
        },
        new()
        {
            Name = "Dir",
            Type = "BitDir?",
            DefaultValue = "null",
            Description = "Text direction of the chrome around the plot (title, legend, tooltip and the screen-reader table). The plot keeps its own coordinates; mirror it by setting Reverse on the index scale."
        },
        new()
        {
            Name = "HtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new()",
            Description = "Additional HTML attributes applied to the root element."
        },
        new()
        {
            Name = "AriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Accessible label for the chart. When null it falls back to the chart title, then to a generated summary."
        },
        new()
        {
            Name = "GenerateTable",
            Type = "bool",
            DefaultValue = "true",
            Description = "Renders a visually-hidden data table for screen readers and points the chart's aria-describedby at it."
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
            Name = "NoDataTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom content shown in place of the plot when there is nothing to draw. Takes precedence over NoDataText."
        },
        new()
        {
            Name = "RespectReducedMotion",
            Type = "bool",
            DefaultValue = "true",
            Description = "When true, animations are disabled for users who requested reduced motion (prefers-reduced-motion: reduce). Set to false to always animate regardless of the OS setting."
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
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
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
            Type = "Task<bool> ExportPngAsync(string? fileName = null, double scale = 2, string? backgroundColor = \"#ffffff\")",
            Description = "Downloads the chart as a .png image rasterized from the live SVG at the given pixel ratio."
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
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
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
                    Description = "Bar corner radius. Only the corners away from the skipped (baseline) edge are rounded; BorderRadiusCorners overrides each corner."
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
    ];
}
