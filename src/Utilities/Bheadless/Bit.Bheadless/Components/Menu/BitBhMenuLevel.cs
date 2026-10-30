using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>How a menu should place its highlight when it opens.</summary>
public enum BitBhMenuFocus
{
    /// <summary>Don't move focus into the menu (pointer-opened submenus).</summary>
    None,

    /// <summary>Focus the menu without highlighting an item (pointer-opened menus).</summary>
    Container,

    /// <summary>Highlight the first enabled item (Enter / Space / ArrowDown).</summary>
    First,

    /// <summary>Highlight the last enabled item (ArrowUp).</summary>
    Last,
}

/// <summary>A component that owns a menu tree: dropdown menu, context menu, menubar menu.</summary>
internal interface IBitBhMenuRoot
{
    bool IsOpen { get; }
    bool Disabled { get; }
    BitBhDirection Dir { get; }
    string? TriggerId { get; }
    BitBhMenubarContext? Menubar { get; }
    Task CloseAsync();
}

/// <summary>
/// One level of a menu tree — the root menu panel or a submenu panel. Owns the
/// item collection, the highlighted ("active") item and the keyboard model.
/// Cascaded (fixed) from the panel to its items; state changes refresh only the
/// affected items instead of re-rendering the whole menu.
/// </summary>
public sealed class BitBhMenuLevel
{
    private readonly BitBhInterop _interop;
    private readonly Action _refreshContent;

    internal BitBhMenuLevel(IBitBhMenuRoot root, BitBhMenuLevel? parent, BitBhMenuSub? sub, string contentId, BitBhInterop interop, Action refreshContent)
    {
        Root = root;
        Parent = parent;
        Sub = sub;
        ContentId = contentId;
        _interop = interop;
        _refreshContent = refreshContent;
        Items = new BitBhCollection<BitBhMenuItemBase>(contentId);
    }

    /// <summary>The items registered at this level, in document order.</summary>
    public BitBhCollection<BitBhMenuItemBase> Items { get; }

    /// <summary>The highlighted item, or null.</summary>
    public BitBhMenuItemBase? ActiveItem { get; private set; }

    /// <summary>The id of the panel element (<c>role="menu"</c>).</summary>
    public string ContentId { get; }

    /// <summary>Whether this level is open.</summary>
    public bool IsOpen => Sub?.IsOpen ?? Root.IsOpen;

    internal IBitBhMenuRoot Root { get; }
    internal BitBhMenuLevel? Parent { get; }
    internal BitBhMenuSub? Sub { get; }
    internal BitBhMenuSub? OpenSub { get; set; }
    internal bool Loop { get; set; } = true;
    internal bool HasFocus { get; set; }
    internal BitBhTypeahead Typeahead { get; } = new();

    /// <summary>Space-separated keys whose browser default is suppressed while the panel has focus.</summary>
    internal string KeysAttribute => "ArrowDown ArrowUp Home End PageUp PageDown Enter Space Tab ArrowLeft ArrowRight";

    internal void SetActive(BitBhMenuItemBase? item, bool viaPointer = false)
    {
        if (!ReferenceEquals(item, ActiveItem))
        {
            var previous = ActiveItem;
            ActiveItem = item;
            previous?.Refresh();
            item?.Refresh();
            _refreshContent();
            if (item is not null && !viaPointer)
                _ = _interop.ScrollIntoViewAsync(item.ItemId);
        }

        // Moving the highlight away from an open submenu's trigger closes it —
        // immediately for the keyboard, after a grace period for the pointer so
        // diagonal movement towards the submenu doesn't close it.
        if (OpenSub is { } sub && !ReferenceEquals(sub.TriggerItem, item))
        {
            if (viaPointer) sub.ScheduleClose();
            else _ = sub.CloseAsync(focusParent: false);
        }
    }

    internal Task FocusAsync() => _interop.FocusAsync(ContentId).AsTask();

    internal void Reset()
    {
        if (ActiveItem is not null)
        {
            var previous = ActiveItem;
            ActiveItem = null;
            previous.Refresh();
        }
        Typeahead.Reset();
    }

    internal async Task ActivateAsync(BitBhMenuItemBase item, bool viaKeyboard)
    {
        if (item.IsItemDisabled) return;
        await item.SelectAsync(viaKeyboard);
    }

    internal void PointerEnter(BitBhMenuItemBase item)
    {
        if (item.IsItemDisabled)
        {
            SetActive(null, viaPointer: true);
            return;
        }
        SetActive(item, viaPointer: true);
        if (!HasFocus && IsOpen)
            _ = FocusAsync();
    }

    internal void PointerLeave(BitBhMenuItemBase item)
    {
        if (ReferenceEquals(ActiveItem, item) && OpenSub is null)
            SetActive(null, viaPointer: true);
    }

    internal async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (!IsOpen) return;

        var rtl = Root.Dir == BitBhDirection.Rtl;
        var openKey = rtl ? "ArrowLeft" : "ArrowRight";
        var closeKey = rtl ? "ArrowRight" : "ArrowLeft";

        if (e.Key == " " && Typeahead.IsActive)
        {
            SearchTypeahead(" ");
            return;
        }

        switch (e.Key)
        {
            case "ArrowDown":
                SetActive(ActiveItem is null ? Items.First() : Items.Next(ActiveItem, Loop) ?? ActiveItem);
                break;

            case "ArrowUp":
                SetActive(ActiveItem is null ? Items.Last() : Items.Previous(ActiveItem, Loop) ?? ActiveItem);
                break;

            case "Home":
            case "PageUp":
                SetActive(Items.First());
                break;

            case "End":
            case "PageDown":
                SetActive(Items.Last());
                break;

            case "Enter":
            case " ":
                if (ActiveItem is not null)
                    await ActivateAsync(ActiveItem, viaKeyboard: true);
                break;

            case "Escape":
                if (Sub is not null) await Sub.CloseAsync(focusParent: true);
                else await Root.CloseAsync();
                break;

            case "Tab":
                // Leave the menu: move focus to the element after (or before) the
                // trigger, then close. Focus is no longer "lost", so it isn't restored.
                if (Root.TriggerId is not null)
                    await _interop.FocusAdjacentAsync(Root.TriggerId, e.ShiftKey);
                await Root.CloseAsync();
                break;

            default:
                if (e.Key == openKey)
                {
                    if (ActiveItem is { IsSubTrigger: true } trigger)
                        await trigger.SelectAsync(viaKeyboard: true);
                    else if (Root.Menubar is { } bar)
                        await bar.MoveFromMenuAsync(Root, +1);
                }
                else if (e.Key == closeKey)
                {
                    if (Sub is not null) await Sub.CloseAsync(focusParent: true);
                    else if (Root.Menubar is { } bar) await bar.MoveFromMenuAsync(Root, -1);
                }
                else if (BitBhTypeahead.IsTypeaheadKey(e.Key, e.CtrlKey || e.MetaKey, e.AltKey))
                {
                    SearchTypeahead(e.Key);
                }
                break;
        }
    }

    private void SearchTypeahead(string key)
    {
        var match = Typeahead.Search(Items, key, ActiveItem);
        if (match is not null) SetActive(match);
    }
}
