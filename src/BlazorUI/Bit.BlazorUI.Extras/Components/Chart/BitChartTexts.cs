namespace Bit.BlazorUI;

/// <summary>
/// The texts <see cref="BitChart"/> writes for assistive technologies and into its exports. All strings default to
/// English; override individual properties to localize the chart. The <c>...Format</c> ones are composite format
/// strings, filled with <see cref="string.Format(IFormatProvider, string, object[])"/> in the chart's culture.
/// </summary>
public class BitChartTexts
{
    /// <summary>
    /// The accessible name of a chart that has neither an <c>AriaLabel</c> nor a displayed title.
    /// {0} is the chart type and {1} the number of data series.
    /// </summary>
    public string DefaultAriaLabelFormat { get; set; } = "{0} chart with {1} data series.";

    /// <summary>
    /// What a screen reader calls the plot in place of its role (<c>aria-roledescription</c>), so it is announced as a
    /// chart rather than as an application or an image.
    /// </summary>
    public string RoleDescription { get; set; } = "chart";

    /// <summary>
    /// The name of each chart type, filled into {0} of <see cref="DefaultAriaLabelFormat"/>. A type missing from the map
    /// is named by its enum member.
    /// </summary>
    public Dictionary<BitChartType, string> TypeNames { get; set; } = new()
    {
        [BitChartType.Line] = "Line",
        [BitChartType.Bar] = "Bar",
        [BitChartType.Radar] = "Radar",
        [BitChartType.Pie] = "Pie",
        [BitChartType.Doughnut] = "Doughnut",
        [BitChartType.PolarArea] = "Polar area",
        [BitChartType.Bubble] = "Bubble",
        [BitChartType.Scatter] = "Scatter"
    };

    /// <summary>
    /// The hidden sentence naming what the plugins drew over the data - annotations, labeled trend lines, the text in a
    /// doughnut's cutout - which the plot is described by. {0} is the list of them, separated by semicolons.
    /// </summary>
    public string NotesFormat { get; set; } = "Marked on the chart: {0}.";

    /// <summary>The accessible name of the legend, used when the legend has no title of its own.</summary>
    public string LegendAriaLabel { get; set; } = "Chart legend";

    /// <summary>
    /// The position of the keyboard-focused value within its series, announced after the value.
    /// {0} is the 1-based position and {1} the number of values in the series.
    /// </summary>
    public string PositionFormat { get; set; } = "{0} of {1}";

    /// <summary>
    /// Which series the keyboard-focused value belongs to, announced when the chart has more than one.
    /// {0} is the 1-based series number and {1} the number of series.
    /// </summary>
    public string SeriesPositionFormat { get; set; } = "series {0} of {1}";

    /// <summary>
    /// The name of a dataset that has no <see cref="BitChartDataset.Label"/>, in the legend, the screen-reader table
    /// and the CSV export alike. {0} is the 1-based dataset number.
    /// </summary>
    public string DatasetLabelFormat { get; set; } = "Dataset {0}";

    /// <summary>The header of the series column of the screen-reader table and the CSV export.</summary>
    public string Series { get; set; } = "Series";

    /// <summary>The header of the X column of point (scatter/bubble) data, used when the x scale has no displayed title.</summary>
    public string X { get; set; } = "X";

    /// <summary>The header of the Y column of point (scatter/bubble) data, used when the y scale has no displayed title.</summary>
    public string Y { get; set; } = "Y";

    /// <summary>The header of the radius column of bubble data.</summary>
    public string Radius { get; set; } = "R";

    /// <summary>
    /// Appended to the screen-reader table's caption when <c>MaxTableRows</c> cuts it short.
    /// {0} is the number of rows shown and {1} the number there are.
    /// </summary>
    public string RowsTruncatedFormat { get; set; } = "Showing the first {0} of {1} rows.";

    /// <summary>
    /// Appended to the screen-reader table's caption when <c>MaxTableColumns</c> cuts it short.
    /// {0} is the number of columns shown and {1} the number there are.
    /// </summary>
    public string ColumnsTruncatedFormat { get; set; } = "Showing the first {0} of {1} columns.";



    /// <summary>The English texts, used by a chart that is given none.</summary>
    internal static BitChartTexts Default { get; } = new();

    /// <summary>The name of a chart type, or its enum member when the map has none.</summary>
    internal string TypeName(BitChartType type)
        => TypeNames is not null && TypeNames.TryGetValue(type, out var name) && string.IsNullOrWhiteSpace(name) is false ? name : type.ToString();

    /// <summary>The name of the unlabeled dataset at the given 0-based index.</summary>
    internal string DatasetLabel(int datasetIndex, IFormatProvider? culture)
        => Format(culture, DatasetLabelFormat, datasetIndex + 1);

    /// <summary>
    /// Fills a format string, falling back to the string itself when it is not a valid format - a stray brace in a
    /// translation is a typo, not a reason for the chart to throw out of its render.
    /// </summary>
    internal static string Format(IFormatProvider? culture, string format, params object[] args)
    {
        try
        {
            return string.Format(culture ?? System.Globalization.CultureInfo.InvariantCulture, format, args);
        }
        catch (FormatException)
        {
            return format;
        }
    }
}
