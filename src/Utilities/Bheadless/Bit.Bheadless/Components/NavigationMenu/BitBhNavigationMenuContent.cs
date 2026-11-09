using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The panel a <see cref="BitBhNavigationMenuTrigger"/> discloses. Rendered in place - right after the trigger,
/// for you to position with CSS (or with <see cref="BitBhFloatingContentBase.Anchor"/>) - or, when the menu has a
/// <see cref="BitBhNavigationMenuViewport"/>, moved into the viewport while open. Hidden while closed (kept in
/// the DOM, so its links stay crawlable), visible while an exit animation runs.
///
/// <para><b>Behaviour:</b> Tab from the open trigger moves into it and Tab from its last element moves on to what
/// follows the trigger, wherever it is shown; every arrow key moves between its links; Escape closes it and
/// returns focus to the trigger; a press outside the menu or focus leaving the menu closes it; the pointer
/// leaving it (and the trigger) closes it after the menu's <c>CloseDelay</c>, unless focus is inside it.</para>
///
/// <para><b>Styling hooks:</b> <c>data-state="open|closed"</c>, <c>data-orientation</c> and
/// <c>data-motion="from-start|from-end|to-start|to-end"</c>: the content being opened comes from the side of the
/// item that was open before it (<c>from-end</c>: that item is on the left, so slide in from the right), the one
/// being closed for it goes to the other side. Start is left and end is right in both reading directions; a
/// content opened or closed with no other involved has no <c>data-motion</c>.</para>
/// </summary>
public class BitBhNavigationMenuContent : BitBhFloatingContentBase
{
    private BitBhNavigationMenuFocusGroup? _group;
    private string? _motion;
    private string? _measuredIn;

    [CascadingParameter]
    private BitBhNavigationMenuContext? MenuContext { get; set; }

    [CascadingParameter]
    private BitBhNavigationMenuItemContext? ItemContext { get; set; }

    /// <summary>The content: links, submenus, anything.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhNavigationMenuRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhNavigationMenuRenderContext>? Template { get; set; }

    protected override string DefaultTag => "div";

    protected override bool IsOpen => ItemContext?.IsOpen ?? false;

    private string ContentId => ItemContext?.ContentId ?? ComponentId;

    private string? ViewportId => MenuContext?.ViewportId;

    // In a viewport the content is moved there; otherwise it is wherever it was rendered, or anchored.
    protected override bool EffectivePortal => ViewportId is not null || Portal;

    protected override BitBhAnchorOptions? EffectiveAnchor => ViewportId is null ? Anchor : null;

    private BitBhNavigationMenuFocusGroup Group => _group ??= new BitBhNavigationMenuFocusGroup(Interop, isList: false);

    protected override void OnInitialized() => ItemContext?.Item.RegisterContent(true);

    protected override void OnParametersSet() => Group.Menu = MenuContext;

    protected override BitBhFloatingRequest CreateFloatingRequest()
    {
        var layer = new BitBhLayerOptions
        {
            FocusOutside = true,
            ParentHandle = ParentLayerHandle,
        };
        // Presses on the other triggers are theirs (a click switches items), and focus moving to them is
        // judged by them (FocusMovedToAsync): the list is part of the layer, and so is the viewport around it.
        layer.BranchIds.Add(ItemContext?.TriggerId);
        layer.BranchIds.Add(MenuContext?.ListId);
        layer.BranchIds.Add(ViewportId);

        var focus = ItemContext is not null && MenuContext is not null && MenuContext.Root.TakeFocusOnOpen(ItemContext.Value);
        return new BitBhFloatingRequest
        {
            ContentId = ContentId,
            ReferenceId = ItemContext?.TriggerId,
            Anchor = EffectiveAnchor,
            ArrowId = ArrowId,
            Portal = EffectivePortal,
            PortalTarget = ViewportId ?? PortalTarget,
            Layer = layer,
            // Focus moves in only when the keyboard entered; it is given back by the trigger's own handling.
            Focus = new BitBhFocusScopeOptions { AutoFocus = focus ? "first" : "none", RestoreFocus = false },
        };
    }

    protected override Task OnDismissAsync(BitBhDismissReason reason)
        => MenuContext is not null && ItemContext is not null
            ? MenuContext.Root.DismissAsync(ItemContext.Value, reason)
            : Task.CompletedTask;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!IsOpen)
        {
            _measuredIn = null;
            return;
        }
        // Shown in the viewport: the viewport takes this content's size.
        if (ViewportId is { } viewport && Floating.IsActive && _measuredIn != viewport)
        {
            _measuredIn = viewport;
            await Interop.NavigationMenuMeasureAsync(viewport, ContentId);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!ShouldRenderContent) return;

        Scope.Owner = ContentId;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ContentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (IsHidden)
            builder.AddAttribute(25, "hidden", true);
        builder.AddAttribute(30, "onkeydown", On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        // While open, keep Escape from also closing an enclosing content / popover / dialog.
        builder.AddEventStopPropagationAttribute(31, "onkeydown", IsOpen);
        if (ViewportId is null)
        {
            // In a viewport, the viewport tracks the pointer (it holds every content shown in it).
            builder.AddAttribute(32, "onpointerenter", On<PointerEventArgs>("onpointerenter", HandlePointerEnter));
            builder.AddAttribute(33, "onpointerleave", On<PointerEventArgs>("onpointerleave", HandlePointerLeave));
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhNavigationMenuFocusGroup>>(50);
        builder.AddComponentParameter(51, "Value", Group);
        builder.AddComponentParameter(52, "IsFixed", true);
        builder.AddComponentParameter(53, "ChildContent", (RenderFragment)(inner =>
        {
            if (Template is not null || ChildContent is not null)
                BitBhFloatingScope.Render(inner, 0, Scope, BitBhContent.Pick(Template, ChildContent, new BitBhNavigationMenuRenderContext
                {
                    IsOpen = IsOpen,
                    Close = () => _ = MenuContext?.Root.CloseAsync(),
                }));
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);
        if (Motion() is { } motion) SetDataValue(attrs, "motion", motion);
        if (MenuContext is not null) SetDataValue(attrs, "orientation", MenuContext.Orientation.ToAttribute());
        // Shift+Tab on the first element goes back to the trigger, Tab on the last to what follows it.
        if (IsOpen && ItemContext is not null) attrs["data-bit-bh-tab-out"] = ItemContext.TriggerId;
        return attrs;
    }

    // Where the newly open item is relative to the one open before it. Left as it was for a content that is
    // neither (another pair switching), so its finished animation does not run again.
    private string? Motion()
    {
        if (MenuContext is null || ItemContext is null) return null;
        var value = ItemContext.Value;
        var isSelected = MenuContext.Value == value;
        var wasSelected = MenuContext.PreviousValue == value;
        if (!isSelected && !wasSelected) return _motion;

        var values = MenuContext.Root.ItemValues.ToList();
        // Start / end are screen sides: in RTL the first item is on the right.
        if (MenuContext.Dir == BitBhDirection.Rtl && MenuContext.Orientation == BitBhOrientation.Horizontal) values.Reverse();
        var index = MenuContext.Value is null ? -1 : values.IndexOf(MenuContext.Value);
        var previous = MenuContext.PreviousValue is null ? -1 : values.IndexOf(MenuContext.PreviousValue);

        string? motion = null;
        if (index != previous)
        {
            if (isSelected && previous != -1) motion = index > previous ? "from-end" : "from-start";
            else if (wasSelected && index != -1) motion = index > previous ? "to-start" : "to-end";
        }
        return _motion = motion;
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
        => args.Key == "Escape" && IsOpen && MenuContext is not null && ItemContext is not null
            ? MenuContext.Root.EscapeFromContentAsync(ItemContext)
            : Task.CompletedTask;

    private void HandlePointerEnter(PointerEventArgs args)
    {
        if (args.PointerType == "mouse") MenuContext?.Root.ContentEnter();
    }

    private void HandlePointerLeave(PointerEventArgs args)
    {
        if (args.PointerType == "mouse") MenuContext?.Root.ContentLeave();
    }

    public override ValueTask DisposeAsync()
    {
        ItemContext?.Item.RegisterContent(false);
        return base.DisposeAsync();
    }
}
