using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A link of a <see cref="BitBhNavigationMenu"/> - a top-level item of its list, or one of the links of a
/// content. Renders an <c>&lt;a&gt;</c>: give it an <c>href</c>. Choosing it closes the menu (and the menus it is
/// a submenu of); arrow keys move to the neighbouring links.
///
/// <para><b>Styling hooks:</b> <c>data-active</c> and <c>aria-current="page"</c> when <see cref="Active"/>.</para>
/// </summary>
public class BitBhNavigationMenuLink : BitBhComponentBase, IBitBhCollectionItem, IDisposable
{
    private bool _pointerDown;

    [CascadingParameter]
    private BitBhNavigationMenuContext? MenuContext { get; set; }

    [CascadingParameter]
    private BitBhNavigationMenuFocusGroup? Group { get; set; }

    /// <summary>Marks the link of the current page (<c>aria-current="page"</c>, <c>data-active</c>).</summary>
    [Parameter]
    public bool Active { get; set; }

    /// <summary>Whether choosing the link closes the menu. Default: true.</summary>
    [Parameter]
    public bool CloseOnSelect { get; set; } = true;

    /// <summary>Fires when the link is chosen (clicked, or Enter), before the menu closes.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnSelect { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "a";

    /// <inheritdoc />
    string IBitBhCollectionItem.ItemId => ComponentId;

    /// <inheritdoc />
    bool IBitBhCollectionItem.IsItemDisabled => false;

    /// <inheritdoc />
    string? IBitBhCollectionItem.ItemText => null;

    protected override void OnInitialized() => Group?.Items.Register(this);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Group is not null)
            await Group.Items.EnsureOrderAsync(Interop);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", HandleClick));
        if (Group is not null)
        {
            builder.AddAttribute(31, "onkeydown", On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
            builder.AddAttribute(32, "onpointerdown", On<PointerEventArgs>("onpointerdown", HandlePointerDown));
            builder.AddAttribute(33, "onfocus", On<FocusEventArgs>("onfocus", HandleFocus));
            builder.AddAttribute(34, "onblur", On<FocusEventArgs>("onblur", HandleBlur));
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Active) attrs["aria-current"] = "page";
        SetDataFlag(attrs, "active", Active);
        if (Group is not null)
        {
            attrs["data-bit-bh-item"] = Group.Items.Key;
            attrs[BitBhKeyboard.KeysAttribute] = Group.KeysAttribute;
        }
        return attrs;
    }

    private async Task HandleClick(MouseEventArgs args)
    {
        await OnSelect.InvokeAsync(args);
        // A click the keyboard made (Enter: detail 0) hides the focused link: focus goes back to the trigger.
        if (CloseOnSelect && MenuContext is not null)
            await MenuContext.Root.CloseAllAsync(restoreFocus: args.Detail == 0 && Group is { IsList: false });
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
        => Group?.HandleKeyDownAsync(this, args) ?? Task.CompletedTask;

    private void HandlePointerDown(PointerEventArgs args) => _pointerDown = true;

    private Task HandleFocus(FocusEventArgs args)
    {
        // A top-level link reached from the keyboard leaves the open content.
        if (_pointerDown || MenuContext is null || Group is not { IsList: true }) return Task.CompletedTask;
        return MenuContext.Root.FocusMovedToAsync(null);
    }

    private void HandleBlur(FocusEventArgs args) => _pointerDown = false;

    public void Dispose() => Group?.Items.Unregister(this);
}
