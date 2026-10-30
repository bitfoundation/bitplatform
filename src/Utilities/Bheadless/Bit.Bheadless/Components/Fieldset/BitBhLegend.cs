using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The caption of a <see cref="BitBhFieldset"/>. Renders a native <c>&lt;legend&gt;</c>
/// by default (which names the fieldset natively); with another element it names
/// the fieldset through <c>aria-labelledby</c>.
/// </summary>
public class BitBhLegend : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    /// <summary>Legend content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "legend";

    protected override void OnInitialized() => FieldsetContext?.RegisterLegend(true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", FieldsetContext?.LegendId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataFlag(attrs, "disabled", FieldsetContext?.Disabled ?? false);
        return attrs;
    }

    public void Dispose() => FieldsetContext?.RegisterLegend(false);
}
