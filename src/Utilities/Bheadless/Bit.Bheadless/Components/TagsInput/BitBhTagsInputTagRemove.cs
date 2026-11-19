using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The remove button of a <see cref="BitBhTagsInputTag"/>: named "Remove {tag}", out of the tab order (the
/// focused tag's Backspace / Delete do the same from the keyboard).
/// </summary>
public class BitBhTagsInputTagRemove : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhTagsInputContext? Context { get; set; }

    [CascadingParameter]
    private BitBhTagsInputTag? TagPart { get; set; }

    /// <summary>The content (an icon, "×").</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", RemoveAsync));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var disabled = Context?.Owner is not { CanEdit: true };
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
        attrs["aria-label"] = Strings.RemoveTag(TagPart?.TagValue ?? string.Empty).Trim();
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "disabled", disabled);
        return attrs;
    }

    private async Task RemoveAsync()
    {
        if (Context?.Owner is not { CanEdit: true } owner || TagPart is null) return;
        await owner.RemoveAtAsync(TagPart.TagIndex);
        await Interop.FocusAsync(owner.InputId);
    }
}
