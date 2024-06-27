using System.Text;

namespace Bit.BlazorUI;

public partial class BitProgressBar
{
    private string _labelId = string.Empty;
    private string _descriptionId = string.Empty;


    /// <summary>
    /// Text alternative of the progress status, used by screen readers for reading the value of the progress.
    /// </summary>
    [Parameter] public string? AriaValueText { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitProgressBar.
    /// </summary>
    [Parameter] public BitProgressBarClassStyles? Classes { get; set; }

    /// <summary>
    /// Color of the BitProgressBar.
    /// </summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>
    /// Text describing or supplementing the operation.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Custom template for describing or supplementing the operation.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Height of the BitProgressBar.
    /// </summary>
    [Parameter] public int Height { get; set; } = 2;

    /// <summary>
    /// Whether or not to show indeterminate progress animation.
    /// </summary>
    [Parameter] public bool Indeterminate { get; set; }

    /// <summary>
    /// Label to display above the BitProgressBar.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom label template to display above the BitProgressBar.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Percentage of the operation's completeness, numerically between 0 and 100.
    /// </summary>
    [Parameter] public double Percent { get; set; }

    /// <summary>
    /// The format of the percent number in percentage display.
    /// </summary>
    [Parameter] public string PercentNumberFormat { get; set; } = "{0:F0} %";

    /// <summary>
    /// Whether or not to percentage display.
    /// </summary>
    [Parameter] public bool ShowPercentNumber { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitProgressBar.
    /// </summary>
    [Parameter] public BitProgressBarClassStyles? Styles { get; set; }


    protected override string RootElementClass => "bit-prb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override Task OnInitializedAsync()
    {
        _labelId = $"BitProgressBar-{UniqueId}-label";
        _descriptionId = $"BitProgressBar-{UniqueId}-description";

        return base.OnInitializedAsync();
    }

    private static double Normalize(double? value) => Math.Clamp(value.GetValueOrDefault(), 0, 100);

    private string GetProgressBarStyle()
    {
        StringBuilder sb = new();

        sb.Append($"--bit-prb-bar-color:{(Color.HasValue() ? Color : "var(--bit-clr-primary-main)")};");

        sb.Append(Styles?.Bar);

        if (Indeterminate is false)
        {
            sb.Append($"width: {Normalize(Percent)}%;");
        }

        return sb.ToString();
    }
}
