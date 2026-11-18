using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>Coordinates the menus of a <see cref="BitBhMenubar"/>.</summary>
public sealed class BitBhMenubarContext
{
    private readonly List<BitBhMenu> _menus = new();
    private readonly BitBhInterop _interop;
    private BitBhMenu? _tabStop;

    internal BitBhMenubarContext(BitBhInterop interop) => _interop = interop;

    internal bool Loop { get; set; } = true;
    internal BitBhDirection Dir { get; set; }

    internal void Register(BitBhMenu menu)
    {
        if (!_menus.Contains(menu)) _menus.Add(menu);
    }

    internal void Unregister(BitBhMenu menu)
    {
        _menus.Remove(menu);
        if (_tabStop == menu) _tabStop = null;
    }

    internal bool IsTabStop(BitBhMenu menu)
        => _tabStop is not null ? _tabStop == menu : _menus.FirstOrDefault(m => !m.IsDisabled) == menu;

    internal void SetTabStop(BitBhMenu menu)
    {
        if (_tabStop == menu) return;
        var previous = _tabStop ?? _menus.FirstOrDefault(m => !m.IsDisabled);
        _tabStop = menu;
        previous?.Refresh();
        menu.Refresh();
    }

    private BitBhMenu? OpenMenu => _menus.FirstOrDefault(m => m.IsOpen);

    internal async Task OnMenuOpenedAsync(BitBhMenu menu, bool byHover = false)
    {
        // A hover leaves the focus - and so the bar's tab stop - where it is.
        if (!byHover) SetTabStop(menu);
        foreach (var other in _menus.Where(m => m != menu && m.IsOpen).ToArray())
            await other.CloseAsync();
    }

    internal async Task OnTriggerPointerEnterAsync(BitBhMenu menu)
    {
        if (OpenMenu is { } open && open != menu && !menu.IsDisabled)
        {
            // A menu the pointer opened hands over to the next one the same way, whether that one opens on hover or
            // not: the focus stays where it is (a hover never moves it), and the next one closes when the pointer leaves.
            if (open.OpenedByHover) await menu.OpenByHoverAsync();
            else await menu.OpenAsync(BitBhMenuFocus.Container);
        }
    }

    internal async Task HandleTriggerKeyDownAsync(BitBhMenu menu, KeyboardEventArgs e)
    {
        var next = Dir == BitBhDirection.Rtl ? "ArrowLeft" : "ArrowRight";
        var prev = Dir == BitBhDirection.Rtl ? "ArrowRight" : "ArrowLeft";
        BitBhMenu? target = null;
        // Disabled items stay reachable (APG): they take focus and announce themselves, they just never open.
        if (e.Key == next) target = Adjacent(menu, +1);
        else if (e.Key == prev) target = Adjacent(menu, -1);
        else if (e.Key == "Home") target = _menus.FirstOrDefault();
        else if (e.Key == "End") target = _menus.LastOrDefault();
        if (target is null) return;
        SetTabStop(target);
        await target.FocusButtonAsync();
    }

    internal async Task MoveFromMenuAsync(IBitBhMenuRoot root, int delta)
    {
        if (root is not BitBhMenu menu) return;
        var target = Adjacent(menu, delta);
        if (target is null || target == menu) return;
        await menu.CloseAsync();
        SetTabStop(target);
        await target.FocusButtonAsync();
        if (target.IsDisabled is false)
            await target.OpenAsync(BitBhMenuFocus.First);
    }

    private BitBhMenu? Adjacent(BitBhMenu menu, int delta)
    {
        var index = _menus.IndexOf(menu);
        if (index < 0) return null;
        var j = index + delta;
        if (j < 0 || j >= _menus.Count)
        {
            if (!Loop) return null;
            j = ((j % _menus.Count) + _menus.Count) % _menus.Count;
        }
        return _menus[j];
    }
}
