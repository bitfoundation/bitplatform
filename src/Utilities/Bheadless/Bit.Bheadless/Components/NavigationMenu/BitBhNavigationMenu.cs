using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A headless Navigation Menu - a site navigation bar of links and disclosure buttons that reveal panels of
/// links (Radix-style API, the WAI-ARIA Disclosure Navigation Menu pattern; not a <c>role="menu"</c>).
///
/// <para><b>Anatomy:</b></para>
/// <code>
/// &lt;BitBhNavigationMenu aria-label="Main"&gt;
///     &lt;BitBhNavigationMenuList&gt;
///         &lt;BitBhNavigationMenuItem Value="products"&gt;
///             &lt;BitBhNavigationMenuTrigger&gt;Products&lt;/BitBhNavigationMenuTrigger&gt;
///             &lt;BitBhNavigationMenuContent&gt;
///                 &lt;BitBhNavigationMenuLink href="/analytics"&gt;Analytics&lt;/BitBhNavigationMenuLink&gt;
///             &lt;/BitBhNavigationMenuContent&gt;
///         &lt;/BitBhNavigationMenuItem&gt;
///         &lt;BitBhNavigationMenuItem&gt;
///             &lt;BitBhNavigationMenuLink href="/pricing" Active="true"&gt;Pricing&lt;/BitBhNavigationMenuLink&gt;
///         &lt;/BitBhNavigationMenuItem&gt;
///         &lt;BitBhNavigationMenuIndicator /&gt;      @* optional *@
///     &lt;/BitBhNavigationMenuList&gt;
///     &lt;BitBhNavigationMenuViewport /&gt;           @* optional: one shared panel the open content shows in *@
/// &lt;/BitBhNavigationMenu&gt;
/// </code>
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Hover with intent</b> - a trigger opens after <see cref="DelayDuration"/>; while one is open (or for <see cref="SkipDelayDuration"/> after it closed) the next opens at once. Leaving the trigger and its content closes it after <see cref="CloseDelay"/> (unless focus is inside the content). Touch is ignored: a tap is a click.</item>
///   <item><b>Click and keyboard</b> - click, Enter or Space toggles; ArrowDown (Right / Left when vertical) opens and moves into the content; Tab from an open trigger enters its content; Escape closes and returns focus to the trigger.</item>
///   <item><b>Arrow keys</b> - Left / Right (Up / Down when vertical, mirrored in RTL), Home and End move between the top-level triggers and links, which also all stay in the Tab order; every arrow moves between the links of a content.</item>
///   <item><b>Dismissal</b> - Escape, a press outside, or focus leaving the menu closes the open content.</item>
///   <item><b>Motion</b> - contents get <c>data-motion="from-start|from-end|to-start|to-end"</c> by where the newly open item is relative to the previous one; the optional <see cref="BitBhNavigationMenuViewport"/> exposes the open content's size and the <see cref="BitBhNavigationMenuIndicator"/> follows the open trigger.</item>
///   <item><b>Submenus</b> - a navigation menu inside a content is a submenu with items of its own.</item>
///   <item><b>Binding</b> - <c>@bind-Value</c> (the open item's value, null when closed), or uncontrolled with <see cref="DefaultValue"/>.</item>
/// </list>
/// </summary>
public class BitBhNavigationMenu : BitBhComponentBase, IDisposable
{
    private readonly BitBhCollection<BitBhNavigationMenuItem> _items = new(BitBhId.New("bit-bh-navmenu-items"));
    private string? _value;
    private bool _valueSeen;
    private string? _lastValue;
    private string? _previousValue;
    private DateTime _lastClosedAt = DateTime.MinValue;
    private string? _hoverOpenedValue;
    private DateTime _hoverOpenedAt;
    private string? _focusOnOpen;
    private string? _listId;
    private string? _viewportId;
    private BitBhDelayedAction? _openTimer;
    private BitBhDelayedAction? _closeTimer;

    /// <summary>A click this soon after hovering opened the item keeps it open rather than toggling it closed.</summary>
    private const int PatientClickMs = 500;

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    // The menu this one is a submenu of (it renders inside that menu's content).
    [CascadingParameter]
    private BitBhNavigationMenuContext? ParentMenu { get; set; }

    /// <summary>The value of the open item (null: none). Supplying it makes the menu controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>The item open initially when uncontrolled.</summary>
    [Parameter]
    public string? DefaultValue { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Fires whenever the open item changes (null when the menu closes).</summary>
    [Parameter]
    public EventCallback<string?> OnValueChange { get; set; }

    /// <summary>How long (ms) the pointer rests on a trigger before its content opens. Default: 200.</summary>
    [Parameter]
    public int DelayDuration { get; set; } = 200;

    /// <summary>
    /// How long (ms) after a content closed the next trigger hovered opens without <see cref="DelayDuration"/>.
    /// While a content is open, moving to another trigger always opens it at once. 0 turns skipping off. Default: 300.
    /// </summary>
    [Parameter]
    public int SkipDelayDuration { get; set; } = 300;

    /// <summary>How long (ms) the open content waits after the pointer left its trigger and itself before it closes. Default: 150.</summary>
    [Parameter]
    public int CloseDelay { get; set; } = 150;

    /// <summary>How the top-level items are laid out (which arrow keys move between them). Default: horizontal.</summary>
    [Parameter]
    public BitBhOrientation Orientation { get; set; }

    /// <summary>Reading direction (mirrors Left / Right). Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>Whether arrow keys wrap from the last item to the first. Default: false.</summary>
    [Parameter]
    public bool Loop { get; set; }

    /// <summary>
    /// The menu's parts: a <see cref="BitBhNavigationMenuList"/> of <see cref="BitBhNavigationMenuItem"/>s and,
    /// optionally, a <see cref="BitBhNavigationMenuViewport"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>A <c>&lt;nav&gt;</c> landmark; a <c>&lt;div&gt;</c> for a submenu.</summary>
    protected override string DefaultTag => ParentMenu is null ? "nav" : "div";

    private string? CurrentValue => Normalize(IsProvided(nameof(Value)) ? Value : _value);

    private BitBhDirection EffectiveDir => Dir ?? CascadedDir;

    private BitBhDelayedAction OpenTimer => _openTimer ??= new BitBhDelayedAction(a => InvokeAsync(a));

    private BitBhDelayedAction CloseTimer => _closeTimer ??= new BitBhDelayedAction(a => InvokeAsync(a));

    // While nothing is open and nothing closed a moment ago, hovering waits for DelayDuration.
    private bool IsOpenDelayed => SkipDelayDuration <= 0
        || (CurrentValue is null && (DateTime.UtcNow - _lastClosedAt).TotalMilliseconds >= SkipDelayDuration);

    private static string? Normalize(string? value) => string.IsNullOrEmpty(value) ? null : value;

    protected override void OnInitialized() => _value = Normalize(DefaultValue);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        TrackValue();
        var context = new BitBhNavigationMenuContext(this)
        {
            BaseId = ComponentId,
            Value = CurrentValue,
            PreviousValue = _previousValue,
            Orientation = Orientation,
            Dir = EffectiveDir,
            Loop = Loop,
            IsNested = ParentMenu is not null,
            ListId = _listId,
            ViewportId = _viewportId,
        };

        builder.OpenComponent<CascadingValue<BitBhNavigationMenuContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());
            inner.AddElementReferenceCapture(30, CaptureElement);
            inner.AddContent(40, ChildContent);
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataValue(attrs, "orientation", Orientation.ToAttribute());
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        SetDataFlag(attrs, "nested", ParentMenu is not null);
        return attrs;
    }

    // The value the parts saw last and the one before it: a controlled value changes without going through here.
    private void TrackValue()
    {
        var current = CurrentValue;
        if (!_valueSeen)
        {
            _valueSeen = true;
            _lastValue = current;
            return;
        }
        if (current == _lastValue) return;
        _previousValue = _lastValue;
        if (current is null) _lastClosedAt = DateTime.UtcNow;
        _lastValue = current;
    }

    // ── Registration ─────────────────────────────────────────────────────────

    internal void RegisterItem(BitBhNavigationMenuItem item) => _items.Register(item);

    internal void UnregisterItem(BitBhNavigationMenuItem item) => _items.Unregister(item);

    internal Task EnsureItemOrderAsync() => _items.EnsureOrderAsync(Interop);

    internal string ItemsKey => _items.Key;

    internal void RegisterList(string? id)
    {
        if (_listId == id) return;
        _listId = id;
        StateHasChanged();
    }

    internal void RegisterViewport(string? id)
    {
        if (_viewportId == id) return;
        _viewportId = id;
        StateHasChanged();
    }

    /// <summary>The values of the items, in document order.</summary>
    internal IReadOnlyList<string> ItemValues => _items.Items.Select(i => i.EffectiveValue).ToList();

    private BitBhNavigationMenuItem? FindItem(string? value)
        => value is null ? null : _items.Items.FirstOrDefault(i => i.EffectiveValue == value);

    // ── State ────────────────────────────────────────────────────────────────

    /// <summary>Opens the item with <paramref name="value"/>, or closes the menu with null.</summary>
    public Task SetValueAsync(string? value) => SetValueAsync(value, byHover: false);

    /// <summary>Closes the open content.</summary>
    public Task CloseAsync() => SetValueAsync(null, byHover: false);

    private async Task SetValueAsync(string? value, bool byHover)
    {
        value = Normalize(value);
        _openTimer?.Cancel();
        _closeTimer?.Cancel();
        _hoverOpenedValue = byHover ? value : null;
        if (byHover) _hoverOpenedAt = DateTime.UtcNow;
        if (CurrentValue == value) return;
        if (!IsProvided(nameof(Value)))
            _value = value;
        await ValueChanged.InvokeAsync(value);
        await OnValueChange.InvokeAsync(value);
        StateHasChanged();
    }

    /// <summary>Whether the content of <paramref name="value"/> should take focus as it opens (keyboard entry); asked once.</summary>
    internal bool TakeFocusOnOpen(string value)
    {
        if (_focusOnOpen != value) return false;
        _focusOnOpen = null;
        return true;
    }

    // ── Pointer ──────────────────────────────────────────────────────────────

    internal async Task TriggerEnterAsync(string value)
    {
        _openTimer?.Cancel();
        if (CurrentValue == value)
        {
            _closeTimer?.Cancel();
            return;
        }
        if (IsOpenDelayed)
            OpenTimer.Schedule(DelayDuration, () => SetValueAsync(value, byHover: true));
        else
            await SetValueAsync(value, byHover: true);
    }

    internal void TriggerLeave()
    {
        _openTimer?.Cancel();
        StartCloseTimer();
    }

    internal void ContentEnter() => _closeTimer?.Cancel();

    internal void ContentLeave() => StartCloseTimer();

    private void StartCloseTimer()
    {
        var value = CurrentValue;
        if (value is null) return;
        CloseTimer.Schedule(CloseDelay, async () =>
        {
            if (CurrentValue != value) return;
            // Focus inside the content (a field the pointer moved off) keeps it open; leaving it closes it.
            if (FindItem(value) is { } item && await Interop.ContainsFocusAsync(item.ContentId)) return;
            if (CurrentValue == value) await SetValueAsync(null, byHover: false);
        });
    }

    internal Task TriggerClickAsync(string value)
    {
        _openTimer?.Cancel();
        if (CurrentValue != value) return SetValueAsync(value, byHover: false);
        // A click right after hovering opened it means "open", which it already is.
        if (_hoverOpenedValue == value && (DateTime.UtcNow - _hoverOpenedAt).TotalMilliseconds < PatientClickMs)
        {
            _hoverOpenedValue = null;
            return Task.CompletedTask;
        }
        return SetValueAsync(null, byHover: false);
    }

    // ── Keyboard / focus ─────────────────────────────────────────────────────

    /// <summary>Opens the item and moves focus into its content (ArrowDown on a horizontal trigger).</summary>
    internal async Task EnterContentAsync(BitBhNavigationMenuItemContext item)
    {
        if (CurrentValue == item.Value)
        {
            await Interop.FocusFirstAsync(item.ContentId);
            return;
        }
        _focusOnOpen = item.Value;
        await SetValueAsync(item.Value, byHover: false);
    }

    /// <summary>
    /// Keyboard focus landed on a top-level trigger or link (<paramref name="value"/>: the trigger's item, null for a
    /// link): an open content it does not belong to was left, and closes.
    /// </summary>
    internal Task FocusMovedToAsync(string? value)
        => CurrentValue is { } open && open != value ? SetValueAsync(null, byHover: false) : Task.CompletedTask;

    /// <summary>Escape in the content of <paramref name="item"/>: back to its trigger, closed.</summary>
    internal async Task EscapeFromContentAsync(BitBhNavigationMenuItemContext item)
    {
        if (CurrentValue != item.Value) return;
        await Interop.FocusAsync(item.TriggerId);
        await SetValueAsync(null, byHover: false);
    }

    /// <summary>The content's layer asked to be dismissed (Escape with focus elsewhere, a press or focus outside).</summary>
    internal Task DismissAsync(string value, BitBhDismissReason reason)
        => CurrentValue == value ? SetValueAsync(null, byHover: false) : Task.CompletedTask;

    /// <summary>
    /// A link was chosen: this menu and the menus it is a submenu of close. With <paramref name="restoreFocus"/> (the
    /// link was activated from the keyboard) focus goes back to the trigger of the outermost open item - the link
    /// itself is about to be hidden.
    /// </summary>
    internal async Task CloseAllAsync(bool restoreFocus)
    {
        var outer = this;
        for (var menu = ParentMenu?.Root; menu is not null; menu = menu.ParentMenu?.Root)
            if (menu.CurrentValue is not null) outer = menu;
        var trigger = outer.FindItem(outer.CurrentValue)?.TriggerId;
        if (restoreFocus && trigger is not null)
            await Interop.FocusAsync(trigger);
        for (var menu = this; menu is not null; menu = menu.ParentMenu?.Root)
            await menu.SetValueAsync(null, byHover: false);
    }

    public void Dispose()
    {
        _openTimer?.Dispose();
        _closeTimer?.Dispose();
    }
}
