using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>The button that copies a <see cref="BitBhClipboard"/>'s value (named "Copy" when it has no text of its own).</summary>
public class BitBhClipboardTrigger : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhClipboard? Clipboard { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Content that receives whether the value was just copied.</summary>
    [Parameter]
    public RenderFragment<bool>? Template { get; set; }

    /// <summary>Names the button "Copy" (<see cref="BitBhStrings.Copy"/>) - for an icon-only button. Default: true when it has no content.</summary>
    [Parameter]
    public bool? UseDefaultLabel { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", On<MouseEventArgs>("onclick", () => Clipboard?.CopyAsync() ?? Task.CompletedTask));
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, BitBhContent.Pick(Template, ChildContent, Clipboard?.IsCopied ?? false));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "button";
        if (UseDefaultLabel ?? (ChildContent is null && Template is null)) attrs["aria-label"] = Strings.Copy;
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "copied", Clipboard?.IsCopied ?? false);
        return attrs;
    }
}
