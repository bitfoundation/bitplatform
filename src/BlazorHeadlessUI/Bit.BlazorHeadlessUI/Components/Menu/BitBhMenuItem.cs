using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// An action item inside a menu panel (<see cref="BitBhMenuItems"/>,
/// <see cref="BitBhMenuSubContent"/> or <see cref="BitBhContextMenuContent"/>).
/// Renders as <c>&lt;li role="menuitem"&gt;</c> by default.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Click / Enter / Space invokes <see cref="OnClick"/> and closes the menu (unless <see cref="CloseOnSelect"/> is false).</item>
///   <item>Pointer hover highlights it (<c>data-active</c> / <c>data-highlighted</c>).</item>
///   <item>Render as a link with <c>As="a" href="…"</c>; keyboard activation follows the link.</item>
/// </list>
/// </summary>
public class BitBhMenuItem : BitBhMenuItemBase
{
    /// <summary>The action to invoke when this item is activated (click, Enter or Space).</summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>Whether activating the item closes the menu. Default: true.</summary>
    [Parameter]
    public bool CloseOnSelect { get; set; } = true;

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhMenuItemRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhMenuItemRenderContext>? Template { get; set; }

    internal override async Task SelectAsync(bool viaKeyboard)
    {
        if (viaKeyboard && IsLink)
        {
            // Let the browser follow the link; the resulting click runs this again.
            await ClickElementAsync();
            return;
        }

        if (CloseOnSelect)
            await CloseMenuAsync();
        await OnClick.InvokeAsync();
    }

    protected override void RenderContent(RenderTreeBuilder builder, int sequence)
    {
        if (Template is not null || ChildContent is not null)
            builder.AddContent(sequence, BitBhContent.Pick(Template, ChildContent, new BitBhMenuItemRenderContext { IsActive = IsActive, Disabled = IsItemDisabled }));
    }
}
