using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A button that closes its <see cref="BitBhDrawer"/> - "Done", "Cancel", an × in the corner. Renders a
/// <c>&lt;button&gt;</c>; with <c>As</c> another element it gets <c>role="button"</c>, a tab stop and Enter / Space.
/// </summary>
public class BitBhDrawerClose : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhDrawerContext? DrawerContext { get; set; }

    /// <summary>Optional callback invoked before the drawer closes.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Disables the button.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Button content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private bool IsButton => Tag.Equals("button", StringComparison.OrdinalIgnoreCase);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", HandleClick));
        if (!IsButton)
            builder.AddAttribute(31, "onkeydown", On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (IsButton)
        {
            attrs["type"] = "button";
            if (Disabled) attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            attrs["tabindex"] = Disabled ? "-1" : "0";
            attrs[BitBhKeyboard.KeysAttribute] = "Enter Space";
            if (Disabled) attrs["aria-disabled"] = "true";
        }
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    private async Task HandleClick(MouseEventArgs args)
    {
        if (Disabled) return;
        await OnClick.InvokeAsync(args);
        if (DrawerContext is not null)
            await DrawerContext.CloseAsync();
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
        => args.Key is "Enter" or " " ? HandleClick(new MouseEventArgs()) : Task.CompletedTask;
}
