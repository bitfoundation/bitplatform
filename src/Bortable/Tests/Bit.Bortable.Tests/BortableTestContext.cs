using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Bortable.Tests;

/// <summary>
/// The shared set-up of the component tests. The sorting engine is C#, but it asks the browser layer
/// (<c>bit-bortable.js</c>) where the pointer is: <c>dropIndex</c> for the insertion point, <c>itemIndexAt</c> for
/// the item under the pointer in swap mode and <c>wouldNest</c> for drops into the dragged item's own subtree.
/// bUnit has no layout, so this stands in for the module and lets each test say what the browser "measured".
/// A drag then becomes a deterministic sequence of dragstart / dragover / drop events whose outcome is decided
/// by the engine alone, which is exactly what these tests are about.
/// </summary>
public abstract class BortableTestContext : BunitContext
{
    internal const string ModulePath = "./_content/Bit.Bortable/bit-bortable.js";

    private const string ItemSelector = "[data-bortable-item]:not([data-bortable-placeholder])";

    private readonly JSRuntimeInvocationHandler<int> _dropIndex;
    private readonly JSRuntimeInvocationHandler<int> _itemIndexAt;
    private readonly JSRuntimeInvocationHandler<bool> _wouldNest;

    protected BortableTestContext()
    {
        // Loose, because the FLIP calls (capture, play, clear) and setHandle are fire-and-forget: the
        // engine never reads their result, so they need no set-up of their own.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddBitBortableServices();

        Module = JSInterop.SetupModule(ModulePath);
        _dropIndex = Module.Setup<int>("dropIndex", _ => true);
        _itemIndexAt = Module.Setup<int>("itemIndexAt", _ => true);
        _wouldNest = Module.Setup<bool>("wouldNest", _ => true);

        // A handler without a result never completes, which would leave the drag handler hanging. Start
        // from "the pointer is over no item"; each dragover helper below sets the answer it needs.
        _dropIndex.SetResult(-1);
        _itemIndexAt.SetResult(-1);
        _wouldNest.SetResult(false);
    }

    /// <summary>The stand-in for <c>bit-bortable.js</c>, which also records every call made to it.</summary>
    protected BunitJSModuleInterop Module { get; }

    /// <summary>The <c>dropIndex</c> calls made so far, with the arguments the component sent.</summary>
    protected IReadOnlyList<JSRuntimeInvocation> DropIndexCalls => _dropIndex.Invocations["dropIndex"];

    /// <summary>The scoped drag coordinator every list of the test shares.</summary>
    internal BortableService Coordinator => Services.GetRequiredService<BortableService>();

    /// <summary>What <c>wouldNest</c> answers: whether the hovered list sits inside the dragged item.</summary>
    protected bool WouldNest { set => _wouldNest.SetResult(value); }

    /// <summary>Makes the browser layer fail the way a dropped circuit does.</summary>
    protected void DisconnectBrowserLayer()
    {
        _dropIndex.SetException(new JSDisconnectedException("The circuit is gone."));
        _itemIndexAt.SetException(new JSDisconnectedException("The circuit is gone."));
    }

    // ---- rendering ----

    protected IRenderedComponent<Bortable<string>> RenderList(
        IList<string>? items, string? id = null, Action<ComponentParameterCollectionBuilder<Bortable<string>>>? extra = null)
        => RenderList(items, item => item, id, extra);

    protected IRenderedComponent<Bortable<TItem>> RenderList<TItem>(
        IList<TItem>? items, Func<TItem, string> label, string? id = null,
        Action<ComponentParameterCollectionBuilder<Bortable<TItem>>>? extra = null)
        => Render<Bortable<TItem>>(p =>
        {
            if (id is not null) p.Add(c => c.Id, id);
            p.Add(c => c.Items, items);
            p.Add(c => c.ItemTemplate, item => $"<span>{label(item)}</span>");
            extra?.Invoke(p);
        });

    // ---- the drag gestures ----
    // Elements are looked up again for every event: each render hands out new event handler ids, so an
    // element found before a re-render can no longer dispatch anything.

    protected static IElement Container<TItem>(IRenderedComponent<Bortable<TItem>> list)
        => list.Find("[data-bortable-list]");

    /// <summary>The wrapper of the item labelled <paramref name="label"/> (never the drop placeholder).</summary>
    protected static IElement ItemElement<TItem>(IRenderedComponent<Bortable<TItem>> list, string label)
        => list.FindAll(ItemSelector).Single(e => e.TextContent.Trim() == label);

    protected static Task DragStartAsync<TItem>(IRenderedComponent<Bortable<TItem>> list, string label)
        => ItemElement(list, label).DragStartAsync(new DragEventArgs());

    /// <summary>A dragover of <paramref name="list"/> for which the browser measured <paramref name="dropIndex"/>.</summary>
    protected Task DragOverAsync<TItem>(IRenderedComponent<Bortable<TItem>> list, int dropIndex)
    {
        _dropIndex.SetResult(dropIndex);
        return Container(list).DragOverAsync(new DragEventArgs { ClientX = 10, ClientY = 20 });
    }

    /// <summary>A swap-mode dragover of <paramref name="list"/> with the pointer over item <paramref name="itemIndex"/>.</summary>
    protected Task HoverItemAsync<TItem>(IRenderedComponent<Bortable<TItem>> list, int itemIndex)
    {
        _itemIndexAt.SetResult(itemIndex);
        return Container(list).DragOverAsync(new DragEventArgs { ClientX = 10, ClientY = 20 });
    }

    protected static Task DropAsync<TItem>(IRenderedComponent<Bortable<TItem>> list)
        => Container(list).DropAsync(new DragEventArgs());

    protected static Task DragEndAsync<TItem>(IRenderedComponent<Bortable<TItem>> list, string label)
        => ItemElement(list, label).DragEndAsync(new DragEventArgs());

    protected static Task ClickAsync<TItem>(IRenderedComponent<Bortable<TItem>> list, string label)
        => ItemElement(list, label).ClickAsync(new MouseEventArgs());

    // ---- reading the markup ----

    /// <summary>The labels of the rendered items in DOM order, the drop placeholder left out.</summary>
    protected static string Labels<TItem>(IRenderedComponent<Bortable<TItem>> list)
        => string.Join(",", list.FindAll(ItemSelector).Select(e => e.TextContent.Trim()));

    protected static IElement? Placeholder<TItem>(IRenderedComponent<Bortable<TItem>> list)
        => list.FindAll("[data-bortable-placeholder]").SingleOrDefault();

    /// <summary>The position of the drop placeholder among the list's item wrappers, or -1.</summary>
    protected static int PlaceholderIndex<TItem>(IRenderedComponent<Bortable<TItem>> list)
    {
        var wrappers = list.FindAll("[data-bortable-item]");
        for (int i = 0; i < wrappers.Count; i++)
            if (wrappers[i].HasAttribute("data-bortable-placeholder")) return i;
        return -1;
    }

    protected static bool HasClass<TItem>(IRenderedComponent<Bortable<TItem>> list, string label, string cssClass)
        => ItemElement(list, label).ClassList.Contains(cssClass);

    /// <summary>The labels of the items that carry <paramref name="cssClass"/>, in DOM order.</summary>
    protected static string LabelsWithClass<TItem>(IRenderedComponent<Bortable<TItem>> list, string cssClass)
        => string.Join(",", list.FindAll(ItemSelector).Where(e => e.ClassList.Contains(cssClass)).Select(e => e.TextContent.Trim()));

    // ---- recording the events ----

    /// <summary>One lifecycle event (or <c>ItemsChanged</c> notification) raised by a list.</summary>
    protected sealed record Raised<TItem>(string List, string Name, BortableEventArgs<TItem>? Args);

    /// <summary>Wires every callback of a list into <paramref name="log"/>, in the order they are raised.</summary>
    protected static void Record<TItem>(ComponentParameterCollectionBuilder<Bortable<TItem>> p, string list, List<Raised<TItem>> log)
    {
        p.Add(c => c.OnStart, e => log.Add(new(list, "Start", e)));
        p.Add(c => c.OnChange, e => log.Add(new(list, "Change", e)));
        p.Add(c => c.OnUpdate, e => log.Add(new(list, "Update", e)));
        p.Add(c => c.OnRemove, e => log.Add(new(list, "Remove", e)));
        p.Add(c => c.OnAdd, e => log.Add(new(list, "Add", e)));
        p.Add(c => c.OnSort, e => log.Add(new(list, "Sort", e)));
        p.Add(c => c.OnEnd, e => log.Add(new(list, "End", e)));
        p.Add(c => c.ItemsChanged, _ => log.Add(new(list, "ItemsChanged", null)));
    }

    /// <summary>The log as "Name:List" entries, e.g. <c>Start:L1 Update:L1 End:L1</c>.</summary>
    protected static string Sequence<TItem>(IEnumerable<Raised<TItem>> log)
        => string.Join(" ", log.Select(r => $"{r.Name}:{r.List}"));

    protected static BortableEventArgs<TItem> Single<TItem>(IEnumerable<Raised<TItem>> log, string name, string list)
        => log.Single(r => r.Name == name && r.List == list).Args!;

    // ---- hosting ----

    /// <summary>
    /// Renders its content only while <see cref="Show"/> is set, so a test can take a list out of the
    /// render tree (and have the renderer dispose it) in the middle of a drag.
    /// </summary>
    protected sealed class Toggle : ComponentBase
    {
        [Parameter] public bool Show { get; set; } = true;
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Show) builder.AddContent(0, ChildContent);
        }
    }
}
