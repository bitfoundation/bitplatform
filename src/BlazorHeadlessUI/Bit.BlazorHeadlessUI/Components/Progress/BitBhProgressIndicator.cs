using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The filled part of a <see cref="BitBhProgress"/>. Mirrors <c>data-state</c> and
/// sets <c>--bit-bh-progress</c> so CSS can size it (<c>width: calc(var(--bit-bh-progress) * 1%)</c>
/// or <c>transform: translateX(calc(var(--bit-bh-progress) * 1% - 100%))</c>).
/// </summary>
public class BitBhProgressIndicator : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhProgress? Progress { get; set; }

    /// <summary>Optional content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Progress is not null)
        {
            attrs["data-state"] = Progress.State;
            if (Progress.Percentage is { } p)
                attrs["style"] = $"--bit-bh-progress:{p.ToString("0.##", CultureInfo.InvariantCulture)}";
        }
        return attrs;
    }
}
