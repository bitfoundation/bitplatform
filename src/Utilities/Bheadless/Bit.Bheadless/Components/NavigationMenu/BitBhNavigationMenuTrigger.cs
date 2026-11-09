using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The disclosure button of a <see cref="BitBhNavigationMenuItem"/>: a <c>&lt;button&gt;</c> with
/// <c>aria-expanded</c> (and <c>aria-controls</c> while open) that shows the item's
/// <see cref="BitBhNavigationMenuContent"/>.
///
/// <para><b>Opens</b> on hover (after the menu's <c>DelayDuration</c>; mouse only), on click / Enter / Space (which
/// toggle), and on ArrowDown - ArrowRight (ArrowLeft in RTL) when the menu is vertical - which also moves focus
/// into the content. <b>Escape</b> closes it. Left / Right (Up / Down when vertical), Home and End move to the
/// other top-level triggers and links.</para>
///
/// <para><b>Styling hooks:</b> <c>data-state="open|closed"</c>, <c>data-disabled</c>, <c>data-orientation</c>.</para>
/// </summary>
public class BitBhNavigationMenuTrigger : BitBhComponentBase, IBitBhCollectionItem, IDisposable
{
    private bool _pointerDown;

    [CascadingParameter]
    private BitBhNavigationMenuContext? MenuContext { get; set; }

    [CascadingParameter]
    private BitBhNavigationMenuItemContext? ItemContext { get; set; }

    [CascadingParameter]
    private BitBhNavigationMenuFocusGroup? Group { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhNavigationMenuRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhNavigationMenuRenderContext>? Template { get; set; }

    /// <summary>Disables the trigger: it never opens and arrow keys skip it.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    protected override string DefaultTag => "button";

    private bool IsOpen => ItemContext?.IsOpen ?? false;

    private string TriggerId => ItemContext?.TriggerId ?? ComponentId;

    /// <inheritdoc />
    string IBitBhCollectionItem.ItemId => TriggerId;

    /// <inheritdoc />
    bool IBitBhCollectionItem.IsItemDisabled => Disabled;

    /// <inheritdoc />
    string? IBitBhCollectionItem.ItemText => null;

    protected override void OnInitialized() => Group?.Items.Register(this);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Group is not null)
            await Group.Items.EnsureOrderAsync(Interop);
    }

    private BitBhNavigationMenuRenderContext RenderContext => new()
    {
        IsOpen = IsOpen,
        Close = () => _ = MenuContext?.Root.CloseAsync(),
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", TriggerId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", HandleClick));
        builder.AddAttribute(31, "onkeydown", On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        // An open submenu's trigger keeps its Escape from closing the content around it too.
        builder.AddEventStopPropagationAttribute(32, "onkeydown", IsOpen);
        builder.AddAttribute(33, "onpointerenter", On<PointerEventArgs>("onpointerenter", HandlePointerEnter));
        builder.AddAttribute(34, "onpointerleave", On<PointerEventArgs>("onpointerleave", HandlePointerLeave));
        builder.AddAttribute(35, "onpointerdown", On<PointerEventArgs>("onpointerdown", HandlePointerDown));
        builder.AddAttribute(36, "onfocus", On<FocusEventArgs>("onfocus", HandleFocus));
        builder.AddAttribute(37, "onblur", On<FocusEventArgs>("onblur", HandleBlur));
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, RenderContext));
        builder.CloseElement();

        // Content shown in a viewport elsewhere in the page still reads as following its trigger.
        if (IsOpen && ItemContext is { HasContent: true } && MenuContext?.ViewportId is not null)
        {
            builder.OpenElement(60, "span");
            builder.AddAttribute(61, "aria-owns", ItemContext.ContentId);
            builder.CloseElement();
        }
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var isButton = Tag.Equals("button", StringComparison.OrdinalIgnoreCase);
        if (isButton)
        {
            attrs["type"] = "button";
            if (Disabled) attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            attrs["tabindex"] = Disabled ? "-1" : "0";
            if (Disabled) attrs["aria-disabled"] = "true";
        }

        SetAriaBool(attrs, "aria-expanded", IsOpen);
        var keys = Group is null ? "" : $"{Group.KeysAttribute} {Group.EntryKey}";
        if (!isButton) keys += " Enter Space";
        if (IsOpen && ItemContext is { HasContent: true })
        {
            attrs["aria-controls"] = ItemContext.ContentId;
            // Tab moves into the content wherever it is shown.
            attrs["data-bit-bh-tab-into"] = ItemContext.ContentId;
        }
        // An Escape on the trigger of an open item is the trigger's (the runtime leaves keys pressed in a layer's branch to .NET).
        if (IsOpen) keys += " Escape";
        if (keys.Trim().Length > 0) attrs[BitBhKeyboard.KeysAttribute] = keys.Trim();

        if (Group is not null) attrs["data-bit-bh-item"] = Group.Items.Key;
        if (MenuContext is not null)
        {
            // What the menu's indicator follows.
            attrs["data-bit-bh-navigation-menu-trigger"] = MenuContext.BaseId;
            SetDataValue(attrs, "orientation", MenuContext.Orientation.ToAttribute());
        }
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs args)
        => Disabled || MenuContext is null || ItemContext is null
            ? Task.CompletedTask
            : MenuContext.Root.TriggerClickAsync(ItemContext.Value);

    private async Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (Disabled || MenuContext is null || ItemContext is null) return;
        if (args.Key == "Escape")
        {
            if (IsOpen) await MenuContext.Root.CloseAsync();
            return;
        }
        if (!Tag.Equals("button", StringComparison.OrdinalIgnoreCase) && args.Key is "Enter" or " ")
        {
            await MenuContext.Root.TriggerClickAsync(ItemContext.Value);
            return;
        }
        if (Group is not null && args.Key == Group.EntryKey && !args.CtrlKey && !args.MetaKey && !args.AltKey)
        {
            if (ItemContext.HasContent) await MenuContext.Root.EnterContentAsync(ItemContext);
            return;
        }
        if (Group is not null) await Group.HandleKeyDownAsync(this, args);
    }

    private Task HandlePointerEnter(PointerEventArgs args)
        => args.PointerType != "mouse" || Disabled || MenuContext is null || ItemContext is not { HasContent: true }
            ? Task.CompletedTask
            : MenuContext.Root.TriggerEnterAsync(ItemContext.Value);

    private void HandlePointerLeave(PointerEventArgs args)
    {
        _pointerDown = false;
        if (args.PointerType == "mouse") MenuContext?.Root.TriggerLeave();
    }

    private void HandlePointerDown(PointerEventArgs args) => _pointerDown = true;

    private Task HandleFocus(FocusEventArgs args)
    {
        // Focus from a press is followed by the click, which decides; focus from the keyboard leaves the open content.
        if (_pointerDown || MenuContext is null || ItemContext is null || Group is not { IsList: true }) return Task.CompletedTask;
        return MenuContext.Root.FocusMovedToAsync(ItemContext.Value);
    }

    private void HandleBlur(FocusEventArgs args) => _pointerDown = false;

    public void Dispose() => Group?.Items.Unregister(this);
}
