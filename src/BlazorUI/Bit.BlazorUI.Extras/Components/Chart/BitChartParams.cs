namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitChart"/> component.
/// </summary>
public class BitChartParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitChart"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitChart value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitChart)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the chart.
    /// </summary>
    public BitChartClassStyles? Classes { get; set; }

    /// <summary>
    /// Renders a visually-hidden data table for screen readers.
    /// </summary>
    public bool? GenerateTable { get; set; }

    /// <summary>
    /// The CSS height of the chart container.
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// The text shown under the spinner, and announced, while the chart is loading.
    /// </summary>
    public string? LoadingLabel { get; set; }

    /// <summary>
    /// Custom content shown in place of the default spinner while the chart is loading.
    /// </summary>
    public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Upper bound on the columns the screen-reader table renders.
    /// </summary>
    public int? MaxTableColumns { get; set; }

    /// <summary>
    /// Upper bound on the rows the screen-reader table renders.
    /// </summary>
    public int? MaxTableRows { get; set; }

    /// <summary>
    /// The visually hidden sentence telling a screen-reader user how to walk the data.
    /// </summary>
    public string? NavigationHint { get; set; }

    /// <summary>
    /// Custom content shown in place of the plot when there is nothing to draw.
    /// </summary>
    public RenderFragment? NoDataTemplate { get; set; }

    /// <summary>
    /// Message shown in place of the plot when there is nothing to draw.
    /// </summary>
    public string? NoDataText { get; set; }

    /// <summary>
    /// The chart options: scales, plugins, interaction, animation, culture and zoom. One instance can be shared
    /// by every chart under the <see cref="BitParams"/>, since the renderer never writes back into it.
    /// </summary>
    public BitChartOptions? Options { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the chart.
    /// </summary>
    public BitChartClassStyles? Styles { get; set; }

    /// <summary>
    /// The texts the chart writes for assistive technologies and into its CSV export, which is what localizes every
    /// chart under the <see cref="BitParams"/> at once.
    /// </summary>
    public BitChartTexts? Texts { get; set; }

    /// <summary>
    /// The custom tooltip template that replaces the default tooltip body.
    /// </summary>
    public RenderFragment<BitChartTooltipContext>? TooltipTemplate { get; set; }

    /// <summary>
    /// The chart type. Ignored by a chart that is given a <see cref="BitChart.Config"/>.
    /// </summary>
    public BitChartType? Type { get; set; }

    /// <summary>
    /// The CSS width of the chart container.
    /// </summary>
    public string? Width { get; set; }

    /// <summary>
    /// The sentence added to the navigation hint while zoom is enabled, naming the zoom keys.
    /// </summary>
    public string? ZoomHint { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitChart"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitChart"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitChart"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitChart"/>.
    /// </remarks>
    /// <param name="bitChart">
    /// The <see cref="BitChart"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitChart bitChart)
    {
        if (bitChart is null) return;

        UpdateBaseParameters(bitChart);

        if (Classes is not null)
        {
            bitChart.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (GenerateTable.HasValue)
        {
            bitChart.TakeFromCascade(nameof(GenerateTable), GenerateTable.Value, static c => c.GenerateTable, static (c, v) => c.GenerateTable = v);
        }

        if (Height.HasValue())
        {
            bitChart.TakeFromCascade(nameof(Height), Height, static c => c.Height, static (c, v) => c.Height = v);
        }

        if (LoadingLabel.HasValue())
        {
            bitChart.TakeFromCascade(nameof(LoadingLabel), LoadingLabel, static c => c.LoadingLabel, static (c, v) => c.LoadingLabel = v);
        }

        if (LoadingTemplate is not null)
        {
            bitChart.TakeFromCascade(nameof(LoadingTemplate), LoadingTemplate, static c => c.LoadingTemplate, static (c, v) => c.LoadingTemplate = v);
        }

        if (MaxTableColumns.HasValue)
        {
            bitChart.TakeFromCascade(nameof(MaxTableColumns), MaxTableColumns.Value, static c => c.MaxTableColumns, static (c, v) => c.MaxTableColumns = v);
        }

        if (MaxTableRows.HasValue)
        {
            bitChart.TakeFromCascade(nameof(MaxTableRows), MaxTableRows.Value, static c => c.MaxTableRows, static (c, v) => c.MaxTableRows = v);
        }

        if (NavigationHint.HasValue())
        {
            bitChart.TakeFromCascade(nameof(NavigationHint), NavigationHint, static c => c.NavigationHint, static (c, v) => c.NavigationHint = v);
        }

        if (NoDataTemplate is not null)
        {
            bitChart.TakeFromCascade(nameof(NoDataTemplate), NoDataTemplate, static c => c.NoDataTemplate, static (c, v) => c.NoDataTemplate = v);
        }

        if (NoDataText.HasValue())
        {
            bitChart.TakeFromCascade(nameof(NoDataText), NoDataText!, static c => c.NoDataText, static (c, v) => c.NoDataText = v);
        }

        if (Options is not null)
        {
            bitChart.TakeFromCascade(nameof(Options), Options, static c => c.Options, static (c, v) => c.Options = v);
        }

        if (Styles is not null)
        {
            bitChart.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }

        if (Texts is not null)
        {
            bitChart.TakeFromCascade(nameof(Texts), Texts, static c => c.Texts, static (c, v) => c.Texts = v);
        }

        if (TooltipTemplate is not null)
        {
            bitChart.TakeFromCascade(nameof(TooltipTemplate), TooltipTemplate, static c => c.TooltipTemplate, static (c, v) => c.TooltipTemplate = v);
        }

        if (Type.HasValue)
        {
            bitChart.TakeFromCascade(nameof(Type), Type.Value, static c => c.Type, static (c, v) => c.Type = v);
        }

        if (Width.HasValue())
        {
            bitChart.TakeFromCascade(nameof(Width), Width!, static c => c.Width, static (c, v) => c.Width = v);
        }

        if (ZoomHint.HasValue())
        {
            bitChart.TakeFromCascade(nameof(ZoomHint), ZoomHint, static c => c.ZoomHint, static (c, v) => c.ZoomHint = v);
        }
    }
}
