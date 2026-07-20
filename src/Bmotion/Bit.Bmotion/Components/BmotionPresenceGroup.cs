using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bmotion;

/// <summary>
/// Keyed list presence - motion.dev's <c>AnimatePresence</c> wrapped around a collection.
/// Renders <see cref="ChildContent"/> once per item of <see cref="Items"/>; when an item is
/// removed its subtree stays in the DOM until its <c>Exit</c> animations finish, and an item
/// re-added mid-exit cancels the exit and re-enters. New and removed items animate
/// independently and concurrently:
/// <code>
/// &lt;BmotionPresenceGroup Items="_messages" ItemKey="m =&gt; ((Message)m).Id" Context="message"&gt;
///     &lt;Bmotion Initial="Bm.To(opacity: 0, x: 40)"
///              Animate="Bm.To(opacity: 1, x: 0)"
///              Exit="Bm.To(opacity: 0, scale: 0.9)"&gt;
///         &lt;div class="toast"&gt;@message.Text&lt;/div&gt;
///     &lt;/Bmotion&gt;
/// &lt;/BmotionPresenceGroup&gt;
/// </code>
/// Items are matched across renders by <see cref="ItemKey"/> (the item itself when omitted -
/// fine for value-semantic items like records, strings or numbers). Keys must be unique.
/// </summary>
public sealed class BmotionPresenceGroup<TItem> : ComponentBase
{
    /// <summary>The current items, in render order.</summary>
    [Parameter, EditorRequired] public IEnumerable<TItem>? Items { get; set; }

    /// <summary>
    /// Stable identity for an item, used to match items across renders. Defaults to the item
    /// itself (value equality) - supply one when items are re-created instances, e.g.
    /// <c>ItemKey="m =&gt; ((Message)m).Id"</c>.
    /// </summary>
    [Parameter] public Func<TItem, object>? ItemKey { get; set; }

    /// <summary>Template rendered for each present (and each still-exiting) item.</summary>
    [Parameter] public RenderFragment<TItem>? ChildContent { get; set; }

    /// <summary>
    /// <see cref="BmPresenceMode.Sync"/> (default) keeps exiting items in the layout flow while
    /// they animate out; <see cref="BmPresenceMode.PopLayout"/> pops them out
    /// (position: absolute at their current spot) so siblings reflow immediately - ideal for
    /// grids and flex rows. (<see cref="BmPresenceMode.Wait"/> has no meaning for concurrent
    /// list changes and behaves like Sync.)
    /// </summary>
    [Parameter] public BmPresenceMode Mode { get; set; }

    /// <summary>Fires each time a removed item finishes its exit animation.</summary>
    [Parameter] public EventCallback OnExitComplete { get; set; }

    private sealed class Entry
    {
        public required object Key { get; init; }
        public required TItem Item { get; set; }
        public required BmotionPresenceContext Ctx { get; init; }
        public bool Exiting { get; set; }
    }

    private readonly List<Entry> _entries = new();

    protected override void OnParametersSet()
    {
        var items = Items?.ToList() ?? new List<TItem>();
        Func<TItem, object> keyOf = ItemKey ?? (static item => item!);

        // Index the incoming items by key (also validates uniqueness - duplicate keys would make
        // exit/revival matching ambiguous and produce duplicate @key values in the render tree).
        var incoming = new Dictionary<object, TItem>(items.Count);
        var order = new List<object>(items.Count);
        foreach (var item in items)
        {
            var key = keyOf(item) ?? throw new InvalidOperationException(
                "BmotionPresenceGroup ItemKey returned null; keys must be non-null and unique.");
            if (!incoming.TryAdd(key, item))
                throw new InvalidOperationException(
                    $"BmotionPresenceGroup received duplicate item key '{key}'. Keys must be unique.");
            order.Add(key);
        }

        // Pass 1 - reconcile existing entries against the incoming set.
        foreach (var entry in _entries.ToArray())
        {
            if (incoming.TryGetValue(entry.Key, out var item))
            {
                entry.Item = item; // pick up new item data for kept entries
                if (entry.Exiting)
                {
                    // Removed then re-added while still exiting: cancel the exit. The child
                    // Bmotion components detect the flipped context and replay their enter.
                    entry.Exiting = false;
                    entry.Ctx.IsExiting = false;
                    entry.Ctx.Reset();
                }
            }
            else if (!entry.Exiting)
            {
                if (entry.Ctx.ChildCount == 0)
                {
                    // No animatable children ever registered, so AllExitsComplete would never
                    // fire - drop the entry immediately instead of stranding it.
                    _entries.Remove(entry);
                }
                else
                {
                    entry.Exiting = true;
                    entry.Ctx.PopLayout = Mode == BmPresenceMode.PopLayout;
                    entry.Ctx.IsExiting = true;
                }
            }
        }

        // Pass 2 - rebuild the list in the incoming order, creating entries for new keys and
        // keeping exiting entries anchored at their previous positions.
        var byKey = _entries.ToDictionary(e => e.Key, e => e);
        var oldIndex = new Dictionary<object, int>(_entries.Count);
        for (int i = 0; i < _entries.Count; i++) oldIndex[_entries[i].Key] = i;

        var next = new List<Entry>(order.Count + _entries.Count);
        foreach (var key in order)
        {
            if (byKey.TryGetValue(key, out var existing))
            {
                next.Add(existing);
            }
            else
            {
                var ctx = new BmotionPresenceContext();
                var entry = new Entry { Key = key, Item = incoming[key], Ctx = ctx };
                ctx.AllExitsComplete += () => OnEntryExited(entry);
                next.Add(entry);
            }
        }
        foreach (var entry in _entries)
        {
            if (!entry.Exiting) continue;
            next.Insert(Math.Min(oldIndex[entry.Key], next.Count), entry);
        }

        _entries.Clear();
        _entries.AddRange(next);
    }

    private void OnEntryExited(Entry entry)
    {
        if (!entry.Exiting) return; // stale callback from a cancelled/superseded exit
        _entries.Remove(entry);

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
