using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>A button that opens the file picker of a <see cref="BitBhFileUpload"/> (React Aria FileTrigger).</summary>
public class BitBhFileUploadTrigger : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhFileUploadContext? Context { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

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
        var disabled = Context?.Owner is not { } owner || owner.IsDisabled;
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (disabled) attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            attrs["tabindex"] = disabled ? "-1" : "0";
            if (!disabled) attrs[BitBhKeyboard.KeysAttribute] = "Enter Space";
            if (disabled) attrs["aria-disabled"] = "true";
        }
        // The runtime opens the picker in the click itself: the browser only allows it with the user's activation.
        if (!disabled && Context?.Owner is { } o) attrs["data-bit-bh-file-trigger"] = o.InputId;
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "disabled", disabled);
        return attrs;
    }
}
