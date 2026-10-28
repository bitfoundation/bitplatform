using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A scalar measurement within a known range (<c>role="meter"</c>) — disk usage,
/// password strength. Unlike <see cref="BitBhProgress"/> it's not about task
/// completion. Exposes <c>--bit-bh-meter</c> (0–100) and <c>data-level="low|optimum|high"</c>.
/// </summary>
public class BitBhMeter : BitBhComponentBase
{
    /// <summary>The current value.</summary>
    [Parameter]
    public double Value { get; set; }

    /// <summary>The minimum value. Default: 0.</summary>
    [Parameter]
    public double Min { get; set; }

    /// <summary>The maximum value. Default: 100.</summary>
    [Parameter]
    public double Max { get; set; } = 100;

    /// <summary>Values below this are "low".</summary>
    [Parameter]
    public double? Low { get; set; }

    /// <summary>Values above this are "high".</summary>
    [Parameter]
    public double? High { get; set; }

    /// <summary>Formats <c>aria-valuetext</c>.</summary>
    [Parameter]
    public Func<double, string>? ValueText { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>double</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<double>? Template { get; set; }

    protected override string DefaultTag => "div";

    private double Percentage => Max > Min ? Math.Clamp((Value - Min) / (Max - Min) * 100, 0, 100) : 0;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, BitBhContent.Pick(Template, ChildContent, Percentage));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "meter";
        attrs["aria-valuemin"] = Min.ToString(CultureInfo.InvariantCulture);
        attrs["aria-valuemax"] = Max.ToString(CultureInfo.InvariantCulture);
        attrs["aria-valuenow"] = Value.ToString(CultureInfo.InvariantCulture);
        if (ValueText is not null) attrs["aria-valuetext"] = ValueText(Value);
        attrs["style"] = $"--bit-bh-meter:{Percentage.ToString("0.##", CultureInfo.InvariantCulture)}";
        attrs["data-level"] = Low is { } low && Value < low ? "low" : High is { } high && Value > high ? "high" : "optimum";
        return attrs;
    }
}
