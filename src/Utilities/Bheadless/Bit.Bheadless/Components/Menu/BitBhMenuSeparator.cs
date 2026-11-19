using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>A visual divider between menu items (<c>role="separator"</c>).</summary>
public class BitBhMenuSeparator : BitBhComponentBase
{
    protected override string DefaultTag => "div";

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
        attrs["role"] = "separator";
        attrs["aria-orientation"] = "horizontal";
        return attrs;
    }
}
