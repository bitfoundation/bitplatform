using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The button that opens a <see cref="BitBhDrawer"/> (and closes a non-modal one). Renders a <c>&lt;button&gt;</c>
/// with <c>aria-haspopup="dialog"</c>, <c>aria-expanded</c> and, while open, <c>aria-controls</c>; focus returns to
/// it when the drawer closes.
/// </summary>
public class BitBhDrawerTrigger : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhDrawerContext? DrawerContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhDrawerRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhDrawerRenderContext>? Template { get; set; }

    /// <summary>Disables the trigger.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    protected override string DefaultTag => "button";

    private bool IsOpen => DrawerContext?.IsOpen ?? false;

    protected override void OnInitialized() => DrawerContext?.Register(BitBhDrawerPart.Trigger, true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", DrawerContext?.TriggerId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", HandleClick));
        if (!Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
            builder.AddAttribute(31, "onkeydown", On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, new BitBhDrawerRenderContext
            {
                IsOpen = IsOpen,
                Close = () => _ = DrawerContext?.CloseAsync(),
            }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        BitBhToggleAttributes.ApplyTrigger(attrs, Tag, IsOpen, Disabled, DrawerContext is { IsPresent: true } drawer ? drawer.PanelId : null);
        attrs["aria-haspopup"] = "dialog";
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", Disabled);
        if (DrawerContext is not null) SetDataValue(attrs, "side", DrawerContext.Side);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
    {
        if (Disabled || DrawerContext is null) return Task.CompletedTask;
        return DrawerContext.IsOpen ? DrawerContext.CloseAsync() : DrawerContext.OpenAsync();
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
        => args.Key is "Enter" or " " ? HandleClick(new MouseEventArgs()) : Task.CompletedTask;

    public void Dispose() => DrawerContext?.Register(BitBhDrawerPart.Trigger, false);
}
