using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

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

/// <summary>Which arrow keys a roving-focus group responds to.</summary>
public enum BitBhRovingOrientation
{
    /// <summary>Left / Right.</summary>
    Horizontal,

    /// <summary>Up / Down.</summary>
    Vertical,

    /// <summary>All four arrows.</summary>
    Both,
}

/// <summary>
/// Base class for items of a roving-tabindex group (toolbar buttons, toggle-group
/// items, generic <see cref="BitBhRovingFocusItem"/>s). Handles registration,
/// <c>tabindex</c>, arrow-key navigation and tab-stop tracking.
/// </summary>
public abstract class BitBhRovingItemBase : BitBhComponentBase, IBitBhCollectionItem, IDisposable
{
    [CascadingParameter]
    private BitBhRovingFocusContext? RovingContext { get; set; }

    /// <summary>The group this item belongs to (null when used standalone).</summary>
    protected BitBhRovingFocusContext? Roving => RovingContext;

    /// <inheritdoc />
    public string ItemId => ComponentId;

    /// <inheritdoc />
    public abstract bool IsItemDisabled { get; }

    /// <inheritdoc />
    public virtual string? ItemText => null;

    /// <summary>Whether this item should be the initial tab stop (e.g. the pressed item of a single toggle group).</summary>
    protected internal virtual bool PreferredTabStop => false;

    internal void Refresh() => StateHasChanged();

    /// <summary>Called after arrow keys moved focus to this item (radio-like groups select it).</summary>
    protected internal virtual Task OnRovedToAsync() => Task.CompletedTask;

    protected override void OnInitialized() => RovingContext?.Items.Register(this);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && RovingContext is not null)
            await RovingContext.Items.EnsureOrderAsync(Interop);
    }

    /// <summary>Adds the roving behaviour (keydown + focus handlers) to the element being built.</summary>
    protected void AddRovingHandlers(RenderTreeBuilder builder, int sequence)
    {
        if (RovingContext is null) return;
        builder.AddAttribute(sequence, "onkeydown",
            On<KeyboardEventArgs>("onkeydown", HandleRovingKeyDown));
        builder.AddAttribute(sequence + 1, "onfocus",
            On<FocusEventArgs>("onfocus", () => RovingContext.SetCurrent(this)));
    }

    /// <summary>Additional key handling (Enter / Space for non-button elements …).</summary>
    protected virtual Task OnKeyDownAsync(KeyboardEventArgs e) => Task.CompletedTask;

    private async Task HandleRovingKeyDown(KeyboardEventArgs e)
    {
        if (RovingContext is not null && await RovingContext.HandleKeyDownAsync(this, e)) return;
        await OnKeyDownAsync(e);
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (RovingContext is not null)
        {
            attrs["tabindex"] = ReferenceEquals(RovingContext.TabStop, this) ? "0" : "-1";
            attrs["data-bit-bh-item"] = RovingContext.Items.Key;
            attrs[BitBhKeyboard.KeysAttribute] = RovingContext.KeysAttribute;
        }
        return attrs;
    }

    public virtual void Dispose() => RovingContext?.Items.Unregister(this);
}
