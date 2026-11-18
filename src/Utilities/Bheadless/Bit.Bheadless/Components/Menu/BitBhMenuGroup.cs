using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// Groups related menu items (<c>role="group"</c>). A <see cref="BitBhMenuLabel"/>
/// inside the group names it automatically through <c>aria-labelledby</c>.
/// </summary>
public class BitBhMenuGroup : BitBhComponentBase
{
    private bool _hasLabel;

    /// <summary>The group's items and optional label.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private string LabelId => $"{ComponentId}-label";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhMenuGroupContext>>(40);
        builder.AddComponentParameter(41, "Value", new BitBhMenuGroupContext(LabelId, RegisterLabel));
        builder.AddComponentParameter(42, "IsFixed", true);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "group";
        if (_hasLabel)
            attrs["aria-labelledby"] = LabelId;
        return attrs;
    }

    private void RegisterLabel(bool present)
    {
        if (_hasLabel == present) return;
        _hasLabel = present;
        StateHasChanged();
    }
}
