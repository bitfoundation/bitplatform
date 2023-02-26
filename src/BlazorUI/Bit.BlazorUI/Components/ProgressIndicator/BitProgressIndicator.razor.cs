namespace Bit.BlazorUI;

public partial class BitProgressIndicator
{
    protected override bool UseVisual => false;


    private double? percentComplete;


    /// <summary>
    /// Label to display above the component
    /// </summary>
    [Parameter] public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Custom label template to display above the component
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Height of the ProgressIndicator
    /// </summary>
    [Parameter] public int BarHeight { get; set; } = 2;

    /// <summary>
    /// Percentage of the operation's completeness, numerically between 0 and 100. If this is not set, the indeterminate progress animation will be shown instead
    /// </summary>
    [Parameter]
    public double? PercentComplete
    {
        get => percentComplete;
        set
        {
            percentComplete = value is not null ? Normalize(value) : null;
        }
    }

    /// <summary>
    /// Text describing or supplementing the operation
    /// </summary>
    [Parameter] public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Custom template for describing or supplementing the operation
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Text alternative of the progress status, used by screen readers for reading the value of the progress
    /// </summary>
    [Parameter] public string AriaValueText { get; set; } = string.Empty;

    /// <summary>
    /// Whether or not to hide the progress state
    /// </summary>
    [Parameter] public bool IsProgressHidden { get; set; }

    /// <summary>
    /// A custom template for progress track
    /// </summary>
    [Parameter] public RenderFragment<BitProgressIndicator>? ProgressTemplate { get; set; }

    private string? LabelId => Label.HasValue() ? $"ProgressIndicator-{UniqueId}-Label" : null;
    private string? DescriptionId => Description.HasValue() ? $"ProgressIndicator-{UniqueId}-Description" : null;

    protected override string RootElementClass => "bit-pin";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => PercentComplete is not null  ? string.Empty : $"indeterminate");
    }

    private static double Normalize(double? value) => Math.Clamp(value ?? 0, 0, 100);
}
