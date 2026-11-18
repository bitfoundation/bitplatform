using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The state of a roving-tabindex group: exactly one item is in the tab order
/// (<c>tabindex="0"</c>) and arrow keys move focus between items. Cascaded by
/// <see cref="BitBhRovingFocusGroup"/>, <see cref="BitBhToolbar"/> and
/// <see cref="BitBhToggleGroup"/>; items derive from <see cref="BitBhRovingItemBase"/>.
/// </summary>
public sealed class BitBhRovingFocusContext
{
    private readonly BitBhInterop _interop;
    private BitBhRovingItemBase? _current;

    internal BitBhRovingFocusContext(BitBhInterop interop, string key)
    {
        _interop = interop;
        Items = new BitBhCollection<BitBhRovingItemBase>(key);
        Items.Changed += RefreshTabStops;
    }

    /// <summary>The registered items in document order.</summary>
    public BitBhCollection<BitBhRovingItemBase> Items { get; }

    /// <summary>Which arrow keys move focus.</summary>
    public BitBhRovingOrientation Orientation { get; internal set; } = BitBhRovingOrientation.Horizontal;

    /// <summary>Whether navigation wraps at the ends.</summary>
    public bool Loop { get; internal set; } = true;

    /// <summary>Reading direction (mirrors Left / Right).</summary>
    public BitBhDirection Dir { get; internal set; }

    /// <summary>Whether a <see cref="BitBhToolbar"/> owns the group.</summary>
    internal bool IsToolbar { get; set; }

    /// <summary>The item that is currently the group's tab stop.</summary>
    public BitBhRovingItemBase? TabStop
    {
        get
        {
            if (_current is not null && Items.IndexOf(_current) >= 0 && !_current.IsItemDisabled) return _current;
            return Items.Items.FirstOrDefault(i => i.PreferredTabStop && !i.IsItemDisabled) ?? Items.First();
        }
    }

    internal string KeysAttribute => Orientation switch
    {
        BitBhRovingOrientation.Vertical => "ArrowUp ArrowDown Home End",
        BitBhRovingOrientation.Both => "ArrowUp ArrowDown ArrowLeft ArrowRight Home End",
        _ => "ArrowLeft ArrowRight Home End",
    };

    // An item's tabindex is decided when it renders, but the tab stop moves without that item rendering: an item
    // that registers later can take the first place, and EnsureOrderAsync re-sorts the items into document order
    // once they are on the page. Re-render whichever item now disagrees, so the group keeps exactly one tab stop.
    private void RefreshTabStops()
    {
        var stop = TabStop;
        foreach (var item in Items.Items.ToArray())
        {
            if (item.RenderedAsTabStop != ReferenceEquals(item, stop))
                item.Refresh();
        }
    }

    internal void SetCurrent(BitBhRovingItemBase item)
    {
        if (ReferenceEquals(_current, item)) return;
        var previous = TabStop;
        _current = item;
        previous?.Refresh();
        item.Refresh();
    }

    internal async Task<bool> HandleKeyDownAsync(BitBhRovingItemBase item, KeyboardEventArgs e)
    {
        if (e.CtrlKey || e.MetaKey || e.AltKey) return false;
        var rtl = Dir == BitBhDirection.Rtl;
        var horizontal = Orientation != BitBhRovingOrientation.Vertical;
        var vertical = Orientation != BitBhRovingOrientation.Horizontal;

        var moves = e.Key switch
        {
            "ArrowRight" or "ArrowLeft" => horizontal,
            "ArrowDown" or "ArrowUp" => vertical,
            "Home" or "End" => true,
            _ => false,
        };
        if (moves is false) return false;

        // The target is picked in document order: items that joined since the last key are in place first.
        await Items.EnsureOrderAsync(_interop);
        BitBhRovingItemBase? target = e.Key switch
        {
            "ArrowRight" => rtl ? Items.Previous(item, Loop) : Items.Next(item, Loop),
            "ArrowLeft" => rtl ? Items.Next(item, Loop) : Items.Previous(item, Loop),
            "ArrowDown" => Items.Next(item, Loop),
            "ArrowUp" => Items.Previous(item, Loop),
            "Home" => Items.First(),
            _ => Items.Last(),
        };
        if (target is null || ReferenceEquals(target, item)) return false;

        SetCurrent(target);
        await _interop.FocusAsync(target.ItemId);
        await target.OnRovedToAsync();
        return true;
    }
}
