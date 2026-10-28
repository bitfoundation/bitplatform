using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The button that toggles a <see cref="BitBhAccordionItem"/>. Renders a native
/// <c>&lt;button&gt;</c> with <c>aria-expanded</c> / <c>aria-controls</c>, and moves
/// focus between triggers with the arrow keys, Home and End.
/// </summary>
public class BitBhAccordionTrigger : BitBhComponentBase, IBitBhCollectionItem, IDisposable
{
    [CascadingParameter]
    private BitBhAccordionItemContext? ItemContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhAccordionTriggerContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhAccordionTriggerContext>? Template { get; set; }

    protected override string DefaultTag => "button";

    private bool IsOpen => ItemContext?.IsOpen ?? false;
    private bool IsDisabled => ItemContext?.Disabled ?? false;
    private BitBhCollection<BitBhAccordionTrigger>? Triggers => ItemContext?.Accordion?.Triggers;

    /// <inheritdoc />
    public string ItemId => ItemContext?.TriggerId ?? ComponentId;

    /// <inheritdoc />
    public bool IsItemDisabled => IsDisabled;

    /// <inheritdoc />
    public string? ItemText => null;

    protected override void OnInitialized() => Triggers?.Register(this);

    public void Dispose() => Triggers?.Unregister(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            On<MouseEventArgs>("onclick", HandleClick));
        builder.AddAttribute(31, "onkeydown",
            On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, new BitBhAccordionTriggerContext { IsOpen = IsOpen, Disabled = IsDisabled }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        BitBhToggleAttributes.ApplyTrigger(attrs, Tag, IsOpen, IsDisabled, ItemContext?.ContentId);
        // APG: the trigger of an open panel that can't be collapsed reports aria-disabled.
        if (!IsDisabled && IsOpen && ItemContext?.Accordion is { Type: BitBhAccordionType.Single, Collapsible: false })
            attrs["aria-disabled"] = "true";
        // A disabled trigger is still focusable for navigation in Radix; native
        // disabled buttons are skipped, which matches the APG recommendation.
        if (Triggers is not null)
            attrs["data-bit-bh-item"] = Triggers.Key;
        var vertical = (ItemContext?.Accordion?.Orientation ?? BitBhOrientation.Vertical) == BitBhOrientation.Vertical;
        var nav = vertical ? "ArrowDown ArrowUp Home End" : "ArrowLeft ArrowRight Home End";
        attrs[BitBhKeyboard.KeysAttribute] = Tag.Equals("button", StringComparison.OrdinalIgnoreCase) ? nav : $"{nav} Enter Space";
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", IsDisabled);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
    {
        if (IsDisabled || ItemContext is null) return Task.CompletedTask;
        if (ItemContext.Accordion is { } accordion) return accordion.ToggleAsync(ItemContext.Value);
        ItemContext.Toggle();
        return Task.CompletedTask;
    }

    private Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is "Enter" or " " && !Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
            return HandleClick(new MouseEventArgs());
        return ItemContext?.Accordion?.Accordion?.HandleTriggerKeyDownAsync(this, e) ?? Task.CompletedTask;
    }
}
