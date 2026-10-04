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

        if (Classes is not null && bitChart.HasNotBeenSet(nameof(Classes)))
        {
            bitChart.Classes = Classes;

            bitChart.ClassBuilder.Reset();
        }

        if (GenerateTable.HasValue && bitChart.HasNotBeenSet(nameof(GenerateTable)))
        {
            bitChart.GenerateTable = GenerateTable.Value;
        }

        if (Height.HasValue() && bitChart.HasNotBeenSet(nameof(Height)))
        {
            bitChart.Height = Height;

            bitChart.StyleBuilder.Reset();
        }

        if (LoadingLabel.HasValue() && bitChart.HasNotBeenSet(nameof(LoadingLabel)))
        {
            bitChart.LoadingLabel = LoadingLabel;
        }

        if (LoadingTemplate is not null && bitChart.HasNotBeenSet(nameof(LoadingTemplate)))
        {
            bitChart.LoadingTemplate = LoadingTemplate;
        }

        if (MaxTableColumns.HasValue && bitChart.HasNotBeenSet(nameof(MaxTableColumns)))
        {
            bitChart.MaxTableColumns = MaxTableColumns.Value;
        }

        if (MaxTableRows.HasValue && bitChart.HasNotBeenSet(nameof(MaxTableRows)))
        {
            bitChart.MaxTableRows = MaxTableRows.Value;
        }

        if (NavigationHint.HasValue() && bitChart.HasNotBeenSet(nameof(NavigationHint)))
        {
            bitChart.NavigationHint = NavigationHint;
        }

        if (NoDataTemplate is not null && bitChart.HasNotBeenSet(nameof(NoDataTemplate)))
        {
            bitChart.NoDataTemplate = NoDataTemplate;
        }

        if (NoDataText.HasValue() && bitChart.HasNotBeenSet(nameof(NoDataText)))
        {
            bitChart.NoDataText = NoDataText!;
        }

        if (Options is not null && bitChart.HasNotBeenSet(nameof(Options)))
        {
            bitChart.Options = Options;
        }

        if (Styles is not null && bitChart.HasNotBeenSet(nameof(Styles)))
        {
            bitChart.Styles = Styles;

            bitChart.StyleBuilder.Reset();
        }

        if (Texts is not null && bitChart.HasNotBeenSet(nameof(Texts)))
        {
            bitChart.Texts = Texts;
        }

        if (TooltipTemplate is not null && bitChart.HasNotBeenSet(nameof(TooltipTemplate)))
        {
            bitChart.TooltipTemplate = TooltipTemplate;
        }

        if (Type.HasValue && bitChart.HasNotBeenSet(nameof(Type)))
        {
            bitChart.Type = Type.Value;
        }

        if (Width.HasValue() && bitChart.HasNotBeenSet(nameof(Width)))
        {
            bitChart.Width = Width!;

            bitChart.StyleBuilder.Reset();
        }

        if (ZoomHint.HasValue() && bitChart.HasNotBeenSet(nameof(ZoomHint)))
        {
            bitChart.ZoomHint = ZoomHint;
        }
    }
}
