using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A progress bar (<c>role="progressbar"</c>) for determinate or indeterminate
/// (<c>Value = null</c>) tasks. Exposes <c>--bit-bh-progress</c> (0–100, as a number)
/// and <c>data-state="loading|complete|indeterminate"</c>; style an inner
/// <see cref="BitBhProgressIndicator"/> with it. Give it an accessible name with
/// <c>aria-label</c> / <c>aria-labelledby</c>.
///
/// <code>
/// &lt;BitBhProgress Value="@uploaded" Max="100" aria-label="Upload" class="progress"&gt;
///     &lt;BitBhProgressIndicator class="bar" style="width: calc(var(--bit-bh-progress) * 1%)" /&gt;
/// &lt;/BitBhProgress&gt;
/// </code>
/// </summary>
public class BitBhProgress : BitBhComponentBase
{
    /// <summary>The current value, or null for indeterminate progress.</summary>
    [Parameter]
    public double? Value { get; set; }

    /// <summary>The minimum value. Default: 0.</summary>
    [Parameter]
    public double Min { get; set; }

    /// <summary>The maximum value. Default: 100.</summary>
    [Parameter]
    public double Max { get; set; } = 100;

    /// <summary>Formats <c>aria-valuetext</c> (e.g. "3 of 10 files"). Defaults to a percentage.</summary>
    [Parameter]
    public Func<double, double, string>? ValueText { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>double?</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<double?>? Template { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The completion percentage (0–100), or null when indeterminate.</summary>
    public double? Percentage => Value is { } v && Max > Min ? Math.Clamp((v - Min) / (Max - Min) * 100, 0, 100) : null;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhProgress>>(30);
        builder.AddComponentParameter(31, "Value", this);
        builder.AddComponentParameter(32, "ChildContent", BitBhContent.Pick(Template, ChildContent, Percentage));
        builder.CloseComponent();
        builder.CloseElement();
    }

    internal string State => Value is null ? "indeterminate" : Percentage >= 100 ? "complete" : "loading";

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "progressbar";
        attrs["aria-valuemin"] = Min.ToString(CultureInfo.InvariantCulture);
        attrs["aria-valuemax"] = Max.ToString(CultureInfo.InvariantCulture);
        if (Value is { } value)
        {
            attrs["aria-valuenow"] = value.ToString(CultureInfo.InvariantCulture);
            attrs["aria-valuetext"] = ValueText?.Invoke(value, Max)
                ?? $"{Math.Round(Percentage ?? 0).ToString(CultureInfo.CurrentCulture)}%";
            attrs["style"] = $"--bit-bh-progress:{(Percentage ?? 0).ToString("0.##", CultureInfo.InvariantCulture)}";
        }
        attrs["data-state"] = State;
        attrs["data-max"] = Max.ToString(CultureInfo.InvariantCulture);
        if (Value is { } v) attrs["data-value"] = v.ToString(CultureInfo.InvariantCulture);
        return attrs;
    }
}
