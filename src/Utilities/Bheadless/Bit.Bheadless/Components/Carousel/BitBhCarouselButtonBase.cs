using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>Shared behaviour of the carousel's buttons.</summary>
public abstract class BitBhCarouselButtonBase : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhCarouselContext? Context { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The owning carousel.</summary>
    protected BitBhCarousel? Owner => Context?.Owner;

    protected override string DefaultTag => "button";

    /// <summary>Whether the button can't do anything now.</summary>
    protected abstract bool IsDisabled { get; }

    /// <summary>What a press does.</summary>
    protected abstract Task ActivateAsync(BitBhCarousel owner);

    /// <summary>The accessible name (an <c>aria-label</c> you set replaces it).</summary>
    protected abstract string Label { get; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", On<MouseEventArgs>("onclick", () => Owner is { } owner && !IsDisabled ? ActivateAsync(owner) : Task.CompletedTask));
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var disabled = Owner is null || IsDisabled;
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (disabled) attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            attrs["tabindex"] = disabled ? "-1" : "0";
            attrs[BitBhKeyboard.KeysAttribute] = "Enter Space";
            if (disabled) attrs["aria-disabled"] = "true";
        }
        attrs["aria-label"] = Label;
        if (Owner is { } owner) attrs["aria-controls"] = owner.SlidesId;
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "disabled", disabled);
        return attrs;
    }
}
