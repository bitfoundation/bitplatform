using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The region that opens the <see cref="BitBhContextMenu"/> on <c>contextmenu</c>
/// (right click, long-press on platforms that emit it, the Menu key, Shift+F10).
/// Renders as <c>&lt;span&gt;</c> by default.
/// </summary>
public class BitBhContextMenuTrigger : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhContextMenuContext? MenuContext { get; set; }

    /// <summary>Disables this trigger (the native browser menu shows instead).</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhContextMenuTriggerRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhContextMenuTriggerRenderContext>? Template { get; set; }

    protected override string DefaultTag => "span";

    private bool IsDisabled => Disabled || (MenuContext?.Disabled ?? false);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", MenuContext?.TriggerId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "oncontextmenu",
            On<MouseEventArgs>("oncontextmenu", HandleContextMenu));
        // Suppress the native browser menu unless this trigger is disabled.
        builder.AddEventPreventDefaultAttribute(31, "oncontextmenu", !IsDisabled);
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, new BitBhContextMenuTriggerRenderContext
            {
                IsOpen = MenuContext?.IsOpen ?? false,
                Disabled = IsDisabled,
            }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, MenuContext?.IsOpen ?? false);
        SetDataFlag(attrs, "disabled", IsDisabled);
        return attrs;
    }

    private async Task HandleContextMenu(MouseEventArgs args)
    {
        if (IsDisabled || MenuContext is null) return;

        var x = args.ClientX;
        var y = args.ClientY;
        var viaKeyboard = args.Detail == 0 && x == 0 && y == 0;
        if (viaKeyboard)
        {
            // Keyboard-invoked context menus carry no pointer position: open at
            // the trigger's top-left corner instead.
            var rect = await MenuContext.Menu.GetRectAsync(MenuContext.TriggerId);
            if (rect is not null)
            {
                x = rect.X + Math.Min(rect.Width, 16);
                y = rect.Y + Math.Min(rect.Height, 16);
            }
        }
        await MenuContext.Menu.OpenAtAsync(x, y, viaKeyboard);
    }
}
