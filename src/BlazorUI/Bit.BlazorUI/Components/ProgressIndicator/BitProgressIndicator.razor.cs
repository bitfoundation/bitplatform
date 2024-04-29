using System.Text;

namespace Bit.BlazorUI;

public partial class BitProgressIndicator
{
    private double? percentComplete;

    private string _labelId = string.Empty;
    private string _descriptionId = string.Empty;


    /// <summary>
    /// Text alternative of the progress status, used by screen readers for reading the value of the progress.
    /// </summary>
    [Parameter] public string? AriaValueText { get; set; }

    /// <summary>
    /// Color of the BitProgressIndicator.
    /// </summary>
    [Parameter] public string? BarColor { get; set; }

    /// <summary>
    /// Height of the BitProgressIndicator.
    /// </summary>
    [Parameter] public int BarHeight { get; set; } = 2;

    /// <summary>
    /// Custom CSS classes for different parts of the BitProgressIndicator.
    /// </summary>
    [Parameter] public BitProgressIndicatorClassStyles? Classes { get; set; }

    /// <summary>
    /// Text describing or supplementing the operation.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Custom template for describing or supplementing the operation.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Whether or not to hide the progress state.
    /// </summary>
    [Parameter] public bool IsProgressHidden { get; set; }

    /// <summary>
    /// Label to display above the BitProgressIndicator.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom label template to display above the BitProgressIndicator.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Percentage of the operation's completeness, numerically between 0 and 100. If this is not set, the indeterminate progress animation will be shown instead.
    /// </summary>
    [Parameter]
    public double? PercentComplete
    {
        get => percentComplete;
        set
        {
            percentComplete = value.HasValue ? Normalize(value) : null;
        }
    }

    /// <summary>
    /// A custom template for progress track.
    /// </summary>
    [Parameter] public RenderFragment<BitProgressIndicator>? ProgressTemplate { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitProgressIndicator.
    /// </summary>
    [Parameter] public BitProgressIndicatorClassStyles? Styles { get; set; }


    protected override string RootElementClass => "bit-pin";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => PercentComplete.HasValue ? string.Empty : $"{RootElementClass}-ind");
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override Task OnInitializedAsync()
    {
        _labelId = $"BitProgressIndicator-{UniqueId}-label";
        _descriptionId = $"BitProgressIndicator-{UniqueId}-description";

        return base.OnInitializedAsync();
    }

    private static double Normalize(double? value) => Math.Clamp(value.GetValueOrDefault(), 0, 100);

    private string GetProgressBarStyle()
    {
        StringBuilder sb = new();

        if (PercentComplete.HasValue)
        {
            sb.Append($"width: {percentComplete}%;");
        }

        if (BarColor.HasValue())
        {
            if (PercentComplete.HasValue)
            {
                sb.Append($"background-color: {BarColor};");
            }
            else
            {
                sb.Append($"background: linear-gradient(to right, var(--bit-clr-bg-secondary) 0%, {BarColor} 50%, var(--bit-clr-bg-secondary) 100%);");
            }
        }

        return sb.ToString();
    }
}
