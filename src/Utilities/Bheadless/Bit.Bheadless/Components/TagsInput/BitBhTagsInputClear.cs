using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>A button that removes every tag of a <see cref="BitBhTagsInput"/> (named "Clear all tags", out of the tab order).</summary>
public class BitBhTagsInputClear : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhTagsInputContext? Context { get; set; }

    /// <summary>The content (an icon, "Clear").</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", () => Context?.ClearAsync() ?? Task.CompletedTask));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var disabled = Context?.Owner is not { CanEdit: true } owner || owner.CurrentValues.Count == 0;
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (disabled) attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            if (disabled) attrs["aria-disabled"] = "true";
        }
        attrs["tabindex"] = "-1";
        attrs["aria-label"] = Strings.ClearTags;
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "disabled", disabled);
        return attrs;
    }
}
