using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI;

public partial class BitProgressIndicator
{
    private double? percentComplete;

    /// <summary>
    /// Label to display above the component
    /// </summary>
    [Parameter] public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Custom label template to display above the component
    /// </summary>
    [Parameter] public RenderFragment? LabelFragment { get; set; }

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
    [Parameter] public RenderFragment? DescriptionFragment { get; set; }

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

    public string? LabelId { get; set; } = string.Empty;
    public string? DescriptionId { get; set; } = string.Empty;

    protected override async Task OnParametersSetAsync()
    {
        LabelId = Label.HasValue() ? $"progress-indicator{UniqueId}-label" : null;
        DescriptionId = Description.HasValue() ? $"progress-indicator{UniqueId}-description" : null;

        await base.OnParametersSetAsync();
    }

    protected override string RootElementClass => "bit-pi";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => PercentComplete is not null ? string.Empty
                                            : $"{RootElementClass}-indeterminate-{VisualClassRegistrar()}");
    }

    private static double Normalize(double? value) => Math.Clamp(value ?? 0, 0, 100);
}
