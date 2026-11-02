using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A horizontal application menu bar (<c>role="menubar"</c>) implementing the
/// WAI-ARIA Menubar pattern. Place <see cref="BitBhMenu"/> components inside it; their
/// <see cref="BitBhMenuButton"/>s become menubar items with a single tab stop.
///
/// <para><b>Keyboard:</b> ArrowLeft / ArrowRight move between menus (opening the
/// neighbour when a menu is already open), Home / End jump to the ends, Enter /
/// Space / ArrowDown open the focused menu, ArrowRight / ArrowLeft inside an open
/// menu move to the adjacent one (unless the item opens a submenu).</para>
/// <para><b>Pointer:</b> once a menu is open, hovering another trigger switches to it.</para>
///
/// <code>
/// &lt;BitBhMenubar&gt;
///     &lt;BitBhMenu&gt;
///         &lt;BitBhMenuButton&gt;File&lt;/BitBhMenuButton&gt;
///         &lt;BitBhMenuItems Anchor="@("bottom start")"&gt;…&lt;/BitBhMenuItems&gt;
///     &lt;/BitBhMenu&gt;
///     &lt;BitBhMenu&gt;
///         &lt;BitBhMenuButton&gt;Edit&lt;/BitBhMenuButton&gt;
///         &lt;BitBhMenuItems Anchor="@("bottom start")"&gt;…&lt;/BitBhMenuItems&gt;
///     &lt;/BitBhMenu&gt;
/// &lt;/BitBhMenubar&gt;
/// </code>
/// </summary>
public class BitBhMenubar : BitBhComponentBase
{
    private BitBhMenubarContext? _context;

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>Whether arrow keys wrap from the last menu to the first. Default: true.</summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    /// <summary>Reading direction. Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>The menus.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnParametersSet()
    {
        _context ??= new BitBhMenubarContext(Interop);
        _context.Loop = Loop;
        _context.Dir = Dir ?? CascadedDir;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhMenubarContext>>(40);
        builder.AddComponentParameter(41, "Value", _context);
        builder.AddComponentParameter(42, "IsFixed", true);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "menubar";
        attrs["aria-orientation"] = "horizontal";
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        return attrs;
    }
}

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

    internal async Task OnMenuOpenedAsync(BitBhMenu menu)
    {
        SetTabStop(menu);
        foreach (var other in _menus.Where(m => m != menu && m.IsOpen).ToArray())
            await other.CloseAsync();
    }

    internal async Task OnTriggerPointerEnterAsync(BitBhMenu menu)
    {
        if (OpenMenu is { } open && open != menu && !menu.IsDisabled)
        {
            await menu.OpenAsync(BitBhMenuFocus.Container);
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
