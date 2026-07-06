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
            Description = "The chart options: scales, plugins (title, legend, tooltip), interaction, animation and zoom."
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
            Name = "AriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Accessible label for the chart. When null a summary is generated from the title and datasets."
        },
        new()
        {
            Name = "GenerateTable",
            Type = "bool",
            DefaultValue = "true",
            Description = "Renders a visually-hidden data table for screen readers."
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
            Description = "Callback raised when a data element (point, bar, arc, ...) is clicked."
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
            Description = "A single dataset, mirroring Chart.js dataset configuration.",
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
                    Description = "Per-index values (line, bar, radar, pie, doughnut, polarArea)."
                },
                new()
                {
                    Name = "Points",
                    Type = "List<BitChartDataPoint>?",
                    DefaultValue = "null",
                    Description = "Point data (x, y[, r]) for scatter/bubble charts. When set, takes precedence over Data."
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
                    Description = "The fill color of the dataset (bars, arcs, points and area fills)."
                },
                new()
                {
                    Name = "BorderColor",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The line/border color of the dataset."
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
                    Name = "Tension",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "Bezier curve tension for line datasets (0 = straight lines)."
                },
            ]
        },
    ];
}
