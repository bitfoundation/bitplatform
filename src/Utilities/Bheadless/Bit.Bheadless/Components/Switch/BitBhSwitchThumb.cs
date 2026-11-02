using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The movable knob of a <see cref="BitBhSwitch"/>. Renders a decorative <c>&lt;span&gt;</c>
/// with <c>data-state="checked|unchecked"</c> (and <c>data-disabled</c>) for styling.
/// </summary>
public class BitBhSwitchThumb : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhSwitchRenderContext? SwitchContext { get; set; }

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        SetDataState(attrs, SwitchContext?.IsChecked ?? false, "checked", "unchecked");
        SetDataFlag(attrs, "disabled", SwitchContext?.Disabled ?? false);
        return attrs;
    }
}
