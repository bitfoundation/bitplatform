using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Groups related form controls under a <see cref="BitBhLegend"/>. Renders a native
/// <c>&lt;fieldset&gt;</c> by default; disabling it disables every
/// <see cref="BitBhField"/> and BlazorHeadlessUI control inside.
///
/// <para>When rendered as another element (<c>As="div"</c>) it becomes
/// <c>role="group"</c> labelled by its legend.</para>
/// </summary>
public class BitBhFieldset : BitBhComponentBase
{
    private bool _hasLegend;

    /// <summary>Disables every control in the fieldset.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>A legend and fields.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "fieldset";

    private string LegendId => $"{ComponentId}-legend";

    private bool IsNative => Tag.Equals("fieldset", StringComparison.OrdinalIgnoreCase);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BitBhFieldsetContext(LegendId, Disabled, RegisterLegend);

        builder.OpenComponent<CascadingValue<BitBhFieldsetContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());
            inner.AddElementReferenceCapture(30, CaptureElement);
            inner.AddContent(40, ChildContent);
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (!IsNative)
            attrs["role"] = "group";
        if (_hasLegend)
            attrs["aria-labelledby"] = LegendId;
        if (Disabled)
        {
            if (IsNative) attrs["disabled"] = true;
            else attrs["aria-disabled"] = "true";
        }
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    private void RegisterLegend(bool present)
    {
        if (_hasLegend == present) return;
        _hasLegend = present;
        StateHasChanged();
    }
}
