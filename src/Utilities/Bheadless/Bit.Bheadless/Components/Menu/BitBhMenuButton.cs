using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The trigger button that opens the menu. Renders as <c>&lt;button&gt;</c> with
/// <c>aria-haspopup="menu"</c>, <c>aria-expanded</c> and <c>aria-controls</c>.
/// Inside a <see cref="BitBhMenubar"/> it becomes a <c>role="menuitem"</c> that takes
/// part in the bar's roving focus.
/// </summary>
public class BitBhMenuButton : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhMenuContext? MenuContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhMenuButtonRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhMenuButtonRenderContext>? Template { get; set; }

    protected override string DefaultTag => "button";

    private bool IsOpen => MenuContext?.IsOpen ?? false;
    private bool IsDisabled => MenuContext?.Disabled ?? false;
    private BitBhMenubarContext? Menubar => MenuContext?.Menubar;

    private BitBhMenuButtonRenderContext RenderContext => new()
    {
        IsOpen = IsOpen,
        Disabled = IsDisabled,
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", MenuContext?.ButtonId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            On<MouseEventArgs>("onclick", HandleClick));
        builder.AddAttribute(31, "onkeydown",
            On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        if (Menubar is not null)
        {
            builder.AddAttribute(32, "onpointerenter",
                On<PointerEventArgs>("onpointerenter", HandlePointerEnter));
            builder.AddAttribute(33, "onfocus",
                On<FocusEventArgs>("onfocus", HandleFocus));
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, RenderContext));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var isButton = Tag.Equals("button", StringComparison.OrdinalIgnoreCase);

        if (isButton)
            attrs["type"] = "button";

        if (Menubar is not null && MenuContext is not null)
        {
            attrs["role"] = "menuitem";
            attrs["tabindex"] = Menubar.IsTabStop(MenuContext.Menu) ? "0" : "-1";
            attrs[BitBhKeyboard.KeysAttribute] = "Enter Space ArrowDown ArrowUp ArrowLeft ArrowRight Home End";
        }
        else
        {
            if (!isButton)
            {
                attrs["role"] = "button";
                attrs["tabindex"] = IsDisabled ? "-1" : "0";
            }
            attrs[BitBhKeyboard.KeysAttribute] = "Enter Space ArrowDown ArrowUp";
        }

        attrs["aria-haspopup"] = "menu";
        SetAriaBool(attrs, "aria-expanded", IsOpen);
        if (IsOpen && MenuContext is not null)
        {
            attrs["aria-controls"] = MenuContext.ItemsId;
            // An Escape pressed on the trigger of an open menu (opened without moving focus into it)
            // is the trigger's: the runtime leaves keys pressed inside a layer's branch to .NET.
            attrs[BitBhKeyboard.KeysAttribute] = $"{attrs[BitBhKeyboard.KeysAttribute]} Escape";
        }

        if (IsDisabled)
        {
            // A disabled item of a menubar stays focusable (APG), so it is never natively disabled.
            if (isButton && Menubar is null) attrs["disabled"] = true;
            else attrs["aria-disabled"] = "true";
        }

        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", IsDisabled);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs args)
    {
        if (IsDisabled || MenuContext is null) return Task.CompletedTask;
        // Enter / Space are handled on keydown (their default click is suppressed
        // through data-bit-bh-keys), so a click here comes from a pointer.
        return MenuContext.ToggleAsync(BitBhMenuFocus.Container);
    }

    private async Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (MenuContext is null) return;
        if (IsDisabled)
        {
            // A disabled menubar item is focusable and still moves focus along the menubar; it never opens.
            if (Menubar is not null) await Menubar.HandleTriggerKeyDownAsync(MenuContext.Menu, args);
            return;
        }

        switch (args.Key)
        {
            case "Escape":
                if (MenuContext.IsOpen) await MenuContext.CloseAsync();
                break;
            case "Enter":
            case " ":
                if (MenuContext.IsOpen) await MenuContext.CloseAsync();
                else await MenuContext.OpenAsync(BitBhMenuFocus.First);
                break;
            case "ArrowDown":
                await MenuContext.OpenAsync(BitBhMenuFocus.First);
                break;
            case "ArrowUp":
                await MenuContext.OpenAsync(BitBhMenuFocus.Last);
                break;
            default:
                if (Menubar is not null)
                    await Menubar.HandleTriggerKeyDownAsync(MenuContext.Menu, args);
                break;
        }
    }

    private Task HandlePointerEnter(PointerEventArgs args)
        => Menubar is not null && MenuContext is not null
            ? Menubar.OnTriggerPointerEnterAsync(MenuContext.Menu)
            : Task.CompletedTask;

    private void HandleFocus(FocusEventArgs args)
    {
        if (Menubar is not null && MenuContext is not null)
            Menubar.SetTabStop(MenuContext.Menu);
    }
}
