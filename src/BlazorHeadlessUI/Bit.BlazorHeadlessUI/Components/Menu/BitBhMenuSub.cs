using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A nested submenu. Contains a <see cref="BitBhMenuSubTrigger"/> (an item of the
/// parent menu) and a <see cref="BitBhMenuSubContent"/> (the nested panel). Works in
/// dropdown menus, context menus and menubars, to any depth.
///
/// <para><b>Interaction:</b> hovering the trigger opens the submenu after
/// <see cref="OpenDelay"/>; leaving it closes after <see cref="CloseDelay"/> unless
/// the pointer reaches the submenu (so diagonal movement doesn't close it).
/// ArrowRight / Enter / Space open it and move focus in; ArrowLeft / Escape close
/// it and return focus to the trigger (directions flip in RTL).</para>
///
/// <code>
/// &lt;BitBhMenuSub&gt;
///     &lt;BitBhMenuSubTrigger&gt;Share ▸&lt;/BitBhMenuSubTrigger&gt;
///     &lt;BitBhMenuSubContent Anchor="@("right start")"&gt;
///         &lt;BitBhMenuItem OnClick="Email"&gt;Email&lt;/BitBhMenuItem&gt;
///     &lt;/BitBhMenuSubContent&gt;
/// &lt;/BitBhMenuSub&gt;
/// </code>
/// </summary>
public class BitBhMenuSub : ComponentBase, IDisposable
{
    private bool _open;
    private BitBhMenuFocus _focusIntent;
    private BitBhDelayedAction? _openTimer;
    private BitBhDelayedAction? _closeTimer;
    private readonly string _baseId = BitBhId.New("bit-bh-sub");

    [CascadingParameter]
    private BitBhMenuLevel? ParentLevel { get; set; }

    /// <summary>Hover delay (ms) before the submenu opens. Default: 100.</summary>
    [Parameter]
    public int OpenDelay { get; set; } = 100;

    /// <summary>Grace period (ms) before the submenu closes after the pointer leaves. Default: 300.</summary>
    [Parameter]
    public int CloseDelay { get; set; } = 300;

    /// <summary>Fires whenever the submenu opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>The trigger and content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether the submenu is open (and its parent menu too).</summary>
    public bool IsOpen => _open && (ParentLevel?.IsOpen ?? false);

    internal BitBhMenuLevel? Parent => ParentLevel;
    internal BitBhMenuSubTrigger? TriggerItem { get; set; }
    internal string TriggerId => TriggerItem?.ItemId ?? $"{_baseId}-trigger";
    internal string ContentId => $"{_baseId}-content";
    internal BitBhDirection Dir => ParentLevel?.Root.Dir ?? BitBhDirection.Ltr;

    protected override void OnParametersSet()
    {
        // A closed parent never leaves its submenus open for the next time.
        if (_open && ParentLevel is { IsOpen: false })
        {
            _open = false;
            if (ParentLevel.OpenSub == this) ParentLevel.OpenSub = null;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<BitBhMenuSub>>(0);
        builder.AddComponentParameter(1, "Value", this);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    internal BitBhMenuFocus ConsumeFocusIntent()
    {
        var intent = _focusIntent;
        _focusIntent = BitBhMenuFocus.None;
        return intent;
    }

    internal BitBhMenuFocus PeekFocusIntent() => _focusIntent;

    internal async Task OpenAsync(BitBhMenuFocus focus)
    {
        CancelTimers();
        if (ParentLevel is null || !ParentLevel.IsOpen) return;

        // Only one submenu per level.
        if (ParentLevel.OpenSub is { } other && other != this)
            await other.CloseAsync(focusParent: false);

        _focusIntent = focus;
        ParentLevel.OpenSub = this;
        if (_open)
        {
            // Already open (e.g. via hover): keyboard entry moves focus inside.
            if (focus is BitBhMenuFocus.First or BitBhMenuFocus.Last && Content is not null)
                await Content.EnterAsync(focus);
            return;
        }
        _open = true;
        await OnOpenChange.InvokeAsync(true);
        Refresh();
    }

    internal async Task CloseAsync(bool focusParent)
    {
        CancelTimers();
        if (!_open) return;
        _open = false;
        if (ParentLevel?.OpenSub == this) ParentLevel.OpenSub = null;
        await OnOpenChange.InvokeAsync(false);
        Refresh();
        if (focusParent && ParentLevel is not null)
        {
            await ParentLevel.FocusAsync();
            ParentLevel.SetActive(TriggerItem);
        }
    }

    internal void ScheduleOpen()
    {
        _closeTimer?.Cancel();
        if (_open) return;
        _openTimer ??= new BitBhDelayedAction(a => InvokeAsync(a));
        _openTimer.Schedule(OpenDelay, () => OpenAsync(BitBhMenuFocus.None));
    }

    internal void ScheduleClose()
    {
        _openTimer?.Cancel();
        if (!_open) return;
        _closeTimer ??= new BitBhDelayedAction(a => InvokeAsync(a));
        _closeTimer.Schedule(CloseDelay, () => CloseAsync(focusParent: false));
    }

    internal void CancelClose() => _closeTimer?.Cancel();

    internal void CancelOpen() => _openTimer?.Cancel();

    internal BitBhMenuSubContent? Content { get; set; }

    private void Refresh()
    {
        TriggerItem?.Refresh();
        Content?.Refresh();
    }

    private void CancelTimers()
    {
        _openTimer?.Cancel();
        _closeTimer?.Cancel();
    }

    public void Dispose()
    {
        _openTimer?.Dispose();
        _closeTimer?.Dispose();
        if (ParentLevel?.OpenSub == this) ParentLevel.OpenSub = null;
    }
}
