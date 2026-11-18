using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The drop zone of a <see cref="BitBhFileUpload"/>: files dropped on it are added, and - unless <see cref="Clickable"/> is
/// off - it is a button (<c>role="button"</c>, Enter / Space) that opens the file picker. <c>data-dragging</c> while files are
/// dragged over it. Don't put a <see cref="BitBhFileUploadTrigger"/> inside a clickable drop zone (a button in a button).
/// </summary>
public class BitBhFileUploadDropzone : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhFileUploadContext? Context { get; set; }

    /// <summary>Whether the zone is itself a button that opens the picker. Default: true.</summary>
    [Parameter]
    public bool Clickable { get; set; } = true;

    /// <summary>The content (instructions, an icon).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Context?.Owner is not { } owner) return attrs;
        var disabled = owner.IsDisabled;
        if (!disabled) attrs["data-bit-bh-file-drop"] = owner.InputId;
        if (Clickable)
        {
            attrs["role"] = "button";
            attrs["tabindex"] = disabled ? "-1" : "0";
            if (disabled) attrs["aria-disabled"] = "true";
            else
            {
                attrs["data-bit-bh-file-trigger"] = owner.InputId;
                attrs[BitBhKeyboard.KeysAttribute] = "Enter Space";
            }
            if (owner.Field?.LabelledBy is { } label && GetAdditionalAttribute("aria-label") is null) attrs["aria-labelledby"] = $"{label} {ComponentId}";
            if (owner.Field?.DescribedBy is { } description) attrs["aria-describedby"] = description;
            attrs["data-bit-bh-interactive"] = "";
        }
        SetDataFlag(attrs, "disabled", disabled);
        SetDataFlag(attrs, "invalid", owner.IsInvalid);
        return attrs;
    }
}
