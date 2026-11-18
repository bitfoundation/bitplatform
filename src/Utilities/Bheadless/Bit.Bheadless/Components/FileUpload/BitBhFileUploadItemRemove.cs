using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>The button that removes a <see cref="BitBhFileUploadItem"/>'s file ("Remove report.pdf").</summary>
public class BitBhFileUploadItemRemove : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhFileUploadContext? Context { get; set; }

    [CascadingParameter]
    private BitBhFileUploadItem? Item { get; set; }

    /// <summary>The content (an icon).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", On<MouseEventArgs>("onclick", () => Item is not null && Context is not null ? Context.RemoveAsync(Item.File) : Task.CompletedTask));
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
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
        if (Item is not null) attrs["aria-label"] = Strings.RemoveFile(Item.File.Name);
        attrs["data-bit-bh-interactive"] = "";
        return attrs;
    }
}
