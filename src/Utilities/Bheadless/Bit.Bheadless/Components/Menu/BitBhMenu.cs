using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A headless, accessible dropdown Menu implementing the WAI-ARIA Menu Button pattern.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Rich items</b> — <see cref="BitBhMenuItem"/>, <see cref="BitBhMenuCheckboxItem"/>, <see cref="BitBhMenuRadioGroup"/> / <see cref="BitBhMenuRadioItem"/>, groups, labels, separators.</item>
///   <item><b>Nested submenus</b> — <see cref="BitBhMenuSub"/>, <see cref="BitBhMenuSubTrigger"/>, <see cref="BitBhMenuSubContent"/> with hover intent and keyboard support.</item>
///   <item><b>Full keyboard support</b> — Enter / Space / ArrowDown / ArrowUp open; arrows, Home / End, typeahead navigate; Enter / Space activate; ArrowRight / ArrowLeft enter and leave submenus; Escape and Tab close.</item>
///   <item><b>Focus management</b> — focus moves into the menu (items are announced via <c>aria-activedescendant</c>) and returns to the button on close.</item>
///   <item><b>Dismissal</b> — pointer-down outside or Escape closes it; no invisible overlays, so the first outside click still reaches its target.</item>
///   <item><b>Menubar ready</b> — place menus inside a <see cref="BitBhMenubar"/> for an application menu bar.</item>
///   <item><b>Open on hover</b> — <see cref="OpenOnHover"/> opens it when the pointer rests on the button (menubar-style navigation) without taking the focus.</item>
///   <item><b>Data attributes</b> — <c>data-state="open|closed"</c>; <c>data-active</c> / <c>data-highlighted</c>, <c>data-disabled</c>, <c>data-state="checked|unchecked"</c> on items.</item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BitBhMenu&gt;
///     &lt;BitBhMenuButton&gt;Options ▾&lt;/BitBhMenuButton&gt;
///     &lt;BitBhMenuItems Anchor="@("bottom start")"&gt;
///         &lt;BitBhMenuItem OnClick="Edit"&gt;Edit&lt;/BitBhMenuItem&gt;
///         &lt;BitBhMenuItem OnClick="Delete" Disabled="true"&gt;Delete&lt;/BitBhMenuItem&gt;
///     &lt;/BitBhMenuItems&gt;
/// &lt;/BitBhMenu&gt;
/// </code>
/// </summary>
public class BitBhMenu : BitBhComponentBase, IBitBhMenuRoot, IDisposable
{
    private bool _open;
    private BitBhMenuFocus _focusIntent;
    private bool _openedByHover;
    private BitBhDelayedAction? _hoverTimer;
    private readonly HashSet<string> _hoveredParts = new(StringComparer.Ordinal);
    private bool _disposed;

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    [CascadingParameter]
    private BitBhMenubarContext? MenubarContext { get; set; }

    /// <summary>Whether the menu is open. Supplying it makes the menu controlled (use <c>@bind-Open</c>).</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Initial open state when uncontrolled.</summary>
    [Parameter]
    public bool DefaultOpen { get; set; }

    /// <summary>Two-way binding callback for <see cref="Open"/>.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Fires whenever the open state changes.</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>Disables the entire menu.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Reading direction (flips submenu keys). Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>
    /// Opens the menu when the pointer rests on its button (after <see cref="HoverOpenDelay"/>) and closes it when the
    /// pointer has left the button and every panel of the menu (after <see cref="HoverCloseDelay"/>) - Base UI's
    /// <c>openOnHover</c>, for menubar-style navigation. A hover-opened menu leaves the focus where it is: the pointer
    /// highlights its items without focusing it, and closing it gives no focus back. Enter, Space, ↓ or ↑ on the button -
    /// or, with nothing focused, one of the menu's own keys - moves the focus into it, and a click on the button keeps it
    /// open (it no longer closes when the pointer leaves). Only a mouse hovers: a touch or pen press is a click. The keyboard opens it as always.
    /// </summary>
    [Parameter]
    public bool OpenOnHover { get; set; }

    /// <summary>How long the pointer rests on the button before a hover opens the menu (ms). Default: 100.</summary>
    [Parameter]
    public int HoverOpenDelay { get; set; } = 100;

    /// <summary>
    /// How long after the pointer left the button and the menu a hover-opened menu closes (ms): the grace period that
    /// lets the pointer cross the gap between them, diagonally too. Default: 300.
    /// </summary>
    [Parameter]
    public int HoverCloseDelay { get; set; } = 300;

    /// <summary>Child content. Should contain an <see cref="BitBhMenuButton"/> and an <see cref="BitBhMenuItems"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>Whether the menu is open.</summary>
    public bool IsOpen => IsProvided(nameof(Open)) ? Open : _open;

    /// <summary>
    /// Whether the menu is open because the pointer rested on its button, and has been neither clicked nor focused
    /// since (<see cref="OpenOnHover"/>): it closes when the pointer leaves it.
    /// </summary>
    public bool OpenedByHover => IsOpen && _openedByHover;

    bool IBitBhMenuRoot.Disabled => Disabled;
    BitBhDirection IBitBhMenuRoot.Dir => Dir ?? CascadedDir;
    string? IBitBhMenuRoot.TriggerId => ButtonId;
    BitBhMenubarContext? IBitBhMenuRoot.Menubar => MenubarContext;
    bool IBitBhMenuRoot.OpenedByHover => OpenedByHover;
    bool IBitBhMenuRoot.TracksHover => TracksHover;
    void IBitBhMenuRoot.PanelHoverChanged(string contentId, bool inside)
    {
        if (inside) HoverEnter(contentId, canOpen: false);
        else HoverLeave(contentId);
    }
    void IBitBhMenuRoot.FocusEntered() => TakeOver();

    // A menu follows the pointer while it opens on hover, or while a hover opened it (a menubar hands a hover-opened menu
    // over to the next one the pointer rests on the same way, OpenOnHover or not).
    private bool TracksHover => OpenOnHover || OpenedByHover;

    private string ButtonId => $"{ComponentId}-button";
    private string ItemsId => $"{ComponentId}-items";

    private BitBhDelayedAction HoverTimer => _hoverTimer ??= new BitBhDelayedAction(a => InvokeAsync(a));

    /// <summary>The root panel, which the keyboard enters when it takes over a hover-opened menu.</summary>
    internal BitBhMenuItems? Items { get; set; }

    protected override void OnInitialized()
    {
        _open = DefaultOpen;
        MenubarContext?.Register(this);
    }

    protected override void OnParametersSet()
    {
        // Closed from outside (a controlled Open): the next open is not a hover's unless a hover makes it, and the panels
        // that went away with it (no pointerleave) are no longer counted as hovered.
        if (!IsOpen) ResetHover();
    }

    public void Dispose()
    {
        _disposed = true;
        _hoverTimer?.Dispose();
        MenubarContext?.Unregister(this);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BitBhMenuContext(this, IsOpen, Disabled, ComponentId, ButtonId, ItemsId, MenubarContext);

        builder.OpenComponent<CascadingValue<IBitBhCloseContext>>(0);
        builder.AddComponentParameter(1, "Value", (IBitBhCloseContext)context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(closeOuter =>
        {
            closeOuter.OpenComponent<CascadingValue<BitBhMenuContext>>(0);
            closeOuter.AddComponentParameter(1, "Value", context);
            closeOuter.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenElement(0, Tag);
                inner.AddAttribute(10, "id", ComponentId);
                inner.AddMultipleAttributes(20, GetFinalAttributes());
                inner.AddElementReferenceCapture(30, CaptureElement);
                inner.AddContent(40, ChildContent);
                inner.CloseElement();
            }));
            closeOuter.CloseComponent();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    internal BitBhMenuFocus ConsumeFocusIntent()
    {
        var intent = _focusIntent;
        _focusIntent = BitBhMenuFocus.Container;
        return intent;
    }

    internal BitBhMenuFocus PeekFocusIntent() => _focusIntent;

    /// <summary>
    /// Opens the menu, highlighting the item selected by <paramref name="focus"/>. A menu the pointer opened
    /// (<see cref="OpenedByHover"/>) is taken over instead: the focus moves into it and it stays open.
    /// </summary>
    public Task OpenAsync(BitBhMenuFocus focus = BitBhMenuFocus.Container) => OpenAsync(focus, byHover: false);

    private async Task OpenAsync(BitBhMenuFocus focus, bool byHover)
    {
        if (Disabled) return;
        if (IsOpen)
        {
            if (!byHover && OpenedByHover) await TakeOverAsync(focus);
            else if (!byHover) _focusIntent = focus;
            return;
        }
        _hoverTimer?.Cancel();
        // A hover leaves the focus where it is: the panel opens without taking it.
        _focusIntent = byHover ? BitBhMenuFocus.None : focus;
        _openedByHover = byHover;
        await SetOpenAsync(true);
        if (MenubarContext is not null)
            await MenubarContext.OnMenuOpenedAsync(this, byHover);
    }

    /// <summary>Opens the menu the way a hover does: without moving the focus, closing when the pointer leaves it.</summary>
    internal Task OpenByHoverAsync() => OpenAsync(BitBhMenuFocus.None, byHover: true);

    /// <summary>Closes the menu (and every open submenu).</summary>
    public Task CloseAsync() => IsOpen ? SetOpenAsync(false) : Task.CompletedTask;

    internal Task ToggleAsync(BitBhMenuFocus focus)
    {
        _hoverTimer?.Cancel();
        // A click on the button of a hover-opened menu keeps it open (and gives it the focus) rather than closing it.
        if (OpenedByHover) return TakeOverAsync(focus);
        return IsOpen ? CloseAsync() : OpenAsync(focus);
    }

    // ── Open on hover ────────────────────────────────────────────────────────

    /// <summary>Whether a pointer event comes from a pointer that hovers: a mouse, not a touch or a pen.</summary>
    internal static bool IsHoverPointer(PointerEventArgs args) => args.PointerType is not ("touch" or "pen");

    /// <summary>
    /// The pointer entered a part of the menu: its button (<paramref name="canOpen"/>, which opens it after
    /// <see cref="HoverOpenDelay"/>) or one of its panels. The parts the pointer is over are counted rather than
    /// paired, so a submenu nested in the panel (no leave from the panel) and one portalled out of it (a leave, then an
    /// enter) are both inside.
    /// </summary>
    internal void HoverEnter(string part, bool canOpen)
    {
        if (_disposed || !TracksHover) return;
        _hoveredParts.Add(part);
        HoverTimer.Cancel();
        if (IsOpen || !canOpen || Disabled) return;
        // OpenOnHover is read again when the delay is over: it may have been turned off meanwhile.
        HoverTimer.Schedule(Math.Max(0, HoverOpenDelay), () => IsOpen || Disabled || !OpenOnHover ? Task.CompletedTask : OpenByHoverAsync());
    }

    /// <summary>The pointer left a part of the menu: once it is over none, a hover-opened menu closes after <see cref="HoverCloseDelay"/>.</summary>
    internal void HoverLeave(string part)
    {
        if (_disposed || !TracksHover) return;
        _hoveredParts.Remove(part);
        if (_hoveredParts.Count > 0) return;
        HoverTimer.Cancel();
        if (OpenedByHover)
            HoverTimer.Schedule(Math.Max(0, HoverCloseDelay), () => OpenedByHover && _hoveredParts.Count == 0 ? CloseAsync() : Task.CompletedTask);
    }

    /// <summary>Takes over a hover-opened menu: it stays open until it is dismissed, and the focus moves into it.</summary>
    private async Task TakeOverAsync(BitBhMenuFocus focus)
    {
        TakeOver();
        if (Items is { } items) await items.EnterAsync(focus);
    }

    /// <summary>The menu was clicked or focused: it no longer closes when the pointer leaves it.</summary>
    private void TakeOver()
    {
        if (!_openedByHover) return;
        _openedByHover = false;
        _hoverTimer?.Cancel();
        StateHasChanged();
        // The runtime still holds the open panels' layers as hover-driven ones, which Escape dismisses ahead of their keys.
        if (Items is { } items) _ = ReleaseHoverLayersAsync(items);
    }

    private async Task ReleaseHoverLayersAsync(BitBhMenuItems items)
    {
        try
        {
            await items.ReleaseHoverAsync();
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private void ResetHover()
    {
        _openedByHover = false;
        _hoveredParts.Clear();
    }

    private async Task SetOpenAsync(bool value)
    {
        if (!value)
        {
            _hoverTimer?.Cancel();
            ResetHover();
        }
        if (!IsProvided(nameof(Open)))
            _open = value;
        await OpenChanged.InvokeAsync(value);
        await OnOpenChange.InvokeAsync(value);
        StateHasChanged();
    }

    internal Task FocusButtonAsync() => Interop.FocusAsync(ButtonId).AsTask();

    internal bool IsDisabled => Disabled;

    internal void Refresh() => StateHasChanged();
}
