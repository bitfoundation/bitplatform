using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// Renders its content only while the enclosing <see cref="BitBhMenuCheckboxItem"/> or
/// <see cref="BitBhMenuRadioItem"/> is checked — typically a check mark or a dot.
/// Hidden from assistive tech (the item already exposes <c>aria-checked</c>).
/// </summary>
public class BitBhMenuItemIndicator : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhMenuItemIndicatorContext? IndicatorContext { get; set; }

    /// <summary>Always render (with <c>data-state</c>) so the indicator can be animated or keep its space.</summary>
    [Parameter]
    public bool ForceMount { get; set; }

    /// <summary>The indicator content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    private bool IsChecked => IndicatorContext?.Checked ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!IsChecked && !ForceMount) return;
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        SetDataState(attrs, IsChecked, "checked", "unchecked");
        return attrs;
    }
}
