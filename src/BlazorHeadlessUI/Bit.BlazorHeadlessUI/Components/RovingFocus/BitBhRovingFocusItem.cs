using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>A generic item of a <see cref="BitBhRovingFocusGroup"/>. Renders a <c>&lt;button&gt;</c> by default.</summary>
public class BitBhRovingFocusItem : BitBhRovingItemBase
{
    /// <summary>Removes the item from keyboard navigation.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Make this the group's initial tab stop.</summary>
    [Parameter]
    public bool TabStop { get; set; }

    /// <summary>The item content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    /// <inheritdoc />
    public override bool IsItemDisabled => Disabled;

    protected internal override bool PreferredTabStop => TabStop;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        AddRovingHandlers(builder, 30);
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (Disabled) attrs["disabled"] = true;
        }
        else if (Disabled)
        {
            attrs["aria-disabled"] = "true";
        }
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }
}
