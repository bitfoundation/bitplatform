using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The rotation control of an autoplaying <see cref="BitBhCarousel"/> (APG): "Stop automatic slide show" while rotating,
/// "Start automatic slide show" while not. Put it first in the carousel's tab order.
/// </summary>
public class BitBhCarouselAutoplayToggle : BitBhCarouselButtonBase
{
    /// <summary>Content that receives whether the carousel is rotating.</summary>
    [Parameter]
    public RenderFragment<bool>? Template { get; set; }

    protected override bool IsDisabled => false;

    protected override Task ActivateAsync(BitBhCarousel owner)
    {
        owner.SetPlaying(!owner.IsPlaying);
        return Task.CompletedTask;
    }

    protected override string Label => Owner?.IsPlaying == true ? Strings.StopAutoplay : Strings.StartAutoplay;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", On<MouseEventArgs>("onclick", () => Owner is { } owner ? ActivateAsync(owner) : Task.CompletedTask));
        // Focus on the rotation control is not "focus entering the carousel": pressing it would otherwise stop the
        // rotation on the focus and start it again on the click.
        builder.AddAttribute(21, "onfocusin", On<FocusEventArgs>("onfocusin", static () => { }));
        builder.AddEventStopPropagationAttribute(22, "onfocusin", true);
        builder.AddElementReferenceCapture(30, CaptureElement);
        if (Template is not null) builder.AddContent(40, Template(Owner?.IsPlaying ?? false));
        else builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs.Remove("aria-controls");
        SetDataFlag(attrs, "playing", Owner?.IsPlaying ?? false);
        return attrs;
    }
}
