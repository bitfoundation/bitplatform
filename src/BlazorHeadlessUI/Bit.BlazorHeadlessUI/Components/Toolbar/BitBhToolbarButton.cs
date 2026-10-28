using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>A button inside a <see cref="BitBhToolbar"/> (roving tab stop).</summary>
public class BitBhToolbarButton : BitBhRovingItemBase
{
    /// <summary>Disables the button (it stays focusable so it remains discoverable).</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Click handler.</summary>
    [Parameter]
    public EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs> OnClick { get; set; }

    /// <summary>Button content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    /// <inheritdoc />
    public override bool IsItemDisabled => false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(25, "onclick",
            On<Microsoft.AspNetCore.Components.Web.MouseEventArgs>("onclick", e => Disabled ? Task.CompletedTask : OnClick.InvokeAsync(e)));
        AddRovingHandlers(builder, 30);
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "button";
        // aria-disabled (not disabled) keeps the control focusable inside the toolbar (APG).
        if (Disabled) attrs["aria-disabled"] = "true";
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }
}
