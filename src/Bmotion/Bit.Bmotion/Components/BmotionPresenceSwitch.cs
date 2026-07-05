using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bmotion;

/// <summary>
/// Animates between items: when <see cref="Item"/> changes, the outgoing item's subtree plays
/// its <c>Exit</c> animations before being removed, then (Wait mode) or while (Sync mode) the
/// new item enters. This is the paging/toast idiom that motion.dev's keyed
/// <c>AnimatePresence</c> covers:
/// <code>
/// &lt;BmotionPresenceSwitch Item="_page" Context="page"&gt;
///     &lt;Bmotion Initial="Bm.To(opacity: 0, x: 40)"
///              Animate="Bm.To(opacity: 1, x: 0)"
///              Exit="Bm.To(opacity: 0, x: -40)"&gt;
///         &lt;div&gt;Page @page&lt;/div&gt;
///     &lt;/Bmotion&gt;
/// &lt;/BmotionPresenceSwitch&gt;
/// </code>
/// The child content is a template of the item, so the outgoing subtree keeps rendering the
/// OLD item while it exits - unlike caching a plain <c>RenderFragment</c>, which would re-render
/// with current state.
/// </summary>
public sealed class BmotionPresenceSwitch<TItem> : ComponentBase
{
    /// <summary>The current item. Changing it (by <see cref="EqualityComparer{T}"/>) triggers exit → enter.</summary>
    [Parameter] public TItem Item { get; set; } = default!;

    /// <summary>Template rendered for the current (and any exiting) item.</summary>
    [Parameter] public RenderFragment<TItem>? ChildContent { get; set; }

    /// <summary>
    /// <see cref="BmPresenceMode.Wait"/> (default): the new item enters only after the old
    /// one finished exiting. <see cref="BmPresenceMode.Sync"/>: both animate simultaneously.
    /// </summary>
    [Parameter] public BmPresenceMode Mode { get; set; } = BmPresenceMode.Wait;

    /// <summary>Fires each time an outgoing item finishes its exit animation.</summary>
    [Parameter] public EventCallback OnExitComplete { get; set; }

    private sealed class Entry
    {
        public required TItem Item { get; init; }
        public required BmotionPresenceContext Ctx { get; init; }
        public required int Key { get; init; }
        public bool Exiting { get; set; }
    }

    private readonly List<Entry> _entries = new();
    private int _nextKey;
    private bool _initialized;
    private TItem _currentItem = default!;
    private bool _hasPending;
    private TItem _pendingItem = default!;

    protected override void OnParametersSet()
    {
        if (!_initialized)
        {
            _initialized = true;
            _currentItem = Item;
            AddEntry(Item);
            return;
        }

        if (EqualityComparer<TItem>.Default.Equals(Item, _currentItem)) return;
        _currentItem = Item;

        // Begin exiting every non-exiting entry (normally just the current one; in Sync mode a
        // rapid switch can catch a still-entering previous item too).
        foreach (var entry in _entries.Where(e => !e.Exiting).ToArray())
            BeginExit(entry);

        if (Mode != BmPresenceMode.Wait || _entries.Count == 0)
        {
            // Sync/PopLayout overlap exit and enter; and when nothing is actually exiting (e.g.
            // the old subtree had no animatable children) the new item can enter immediately.
            _hasPending = false;
            AddEntry(Item);
        }
        else
        {
            // Wait: enter once all exiting entries are gone. Rapid switches replace the pending item.
            _hasPending = true;
            _pendingItem = Item;
        }
    }

    private void AddEntry(TItem item)
    {
        var ctx = new BmotionPresenceContext();
        var entry = new Entry { Item = item, Ctx = ctx, Key = _nextKey++ };
        ctx.AllExitsComplete += () => OnEntryExited(entry);
        _entries.Add(entry);
    }

    private void BeginExit(Entry entry)
    {
        if (entry.Ctx.ChildCount == 0)
        {
            // No animatable children ever registered, so AllExitsComplete would never fire -
            // drop the entry immediately instead of stranding it.
            _entries.Remove(entry);
            return;
        }
        entry.Exiting = true;
        entry.Ctx.PopLayout = Mode == BmPresenceMode.PopLayout;
        entry.Ctx.IsExiting = true;
    }

    private void OnEntryExited(Entry entry)
    {
        if (!entry.Exiting) return; // stale callback from a superseded exit cycle
        // Clear the flag before removing: Unregister during a child's disposal can raise
        // AllExitsComplete again for the same entry, and a second callback must not reach the
        // completion path below twice.
        entry.Exiting = false;
        _entries.Remove(entry);

        if (_hasPending && !_entries.Any(e => e.Exiting))
        {
            _hasPending = false;
            AddEntry(_pendingItem);
        }

        _ = InvokeAsync(async () =>
        {
            await OnExitComplete.InvokeAsync();
            StateHasChanged();
        });
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent is null) return;
        foreach (var entry in _entries)
        {
            builder.OpenComponent<CascadingValue<BmotionPresenceContext>>(0);
            builder.SetKey(entry.Key);
            builder.AddComponentParameter(1, "Value", entry.Ctx);
            builder.AddComponentParameter(2, "IsFixed", false);
            builder.AddComponentParameter(3, "ChildContent",
                (RenderFragment)(b => b.AddContent(0, ChildContent, entry.Item)));
            builder.CloseComponent();
        }
    }
}
