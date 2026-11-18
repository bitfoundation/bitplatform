using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>A read-only input showing a <see cref="BitBhClipboard"/>'s value, selected on focus. Name it with <c>aria-label</c>.</summary>
public class BitBhClipboardInput : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhClipboard? Clipboard { get; set; }

    protected override string DefaultTag => "input";

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
        attrs["type"] = "text";
        attrs["readonly"] = true;
        attrs["value"] = Clipboard?.Value ?? string.Empty;
        attrs["data-bit-bh-select-on-focus"] = "";
        SetDataFlag(attrs, "copied", Clipboard?.IsCopied ?? false);
        return attrs;
    }
}
