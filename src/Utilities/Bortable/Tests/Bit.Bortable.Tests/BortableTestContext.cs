using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Bortable.Tests;

/// <summary>
/// The shared set-up of the component tests. The sorting engine is C#; the browser layer (<c>bit-bortable.js</c>)
/// routes the input and hit-tests the lists where the pointer is, and calls a list through its bridge only when
/// something changes: <c>Start</c> (a drag of the item with this key starts), <c>Over</c> (the pointer reached
/// this drop position of this list) and <c>End</c> (the drag ended, and how). bUnit has no layout and runs no
/// JavaScript, so these tests make those calls themselves, with the positions the browser would have measured.
/// A drag then becomes a deterministic sequence of calls whose outcome is decided by the engine alone, which is
/// exactly what these tests are about.
/// </summary>
public abstract class BortableTestContext : BunitContext
{
    internal const string ModulePath = "./_content/Bit.Bortable/bit-bortable.js";

    private const string ItemSelector = "[data-bortable-item]:not([data-bortable-placeholder])";

    /// <param name="registerServices">
    /// False leaves <c>AddBitBortableServices</c> out, as an app that lazy loads Bit.Bortable on WebAssembly does.
    /// </param>
    protected BortableTestContext(bool registerServices = true)
    {
        // Loose, because every call the component makes into the module (register, configure, the FLIP calls) is
        // fire-and-forget: the engine never reads a result, so none needs a set-up of its own.
        JSInterop.Mode = JSRuntimeMode.Loose;
        if (registerServices) Services.AddBitBortableServices();

        Module = JSInterop.SetupModule(ModulePath);
    }

    /// <summary>The stand-in for <c>bit-bortable.js</c>, which records every call made to it.</summary>
    protected BunitJSModuleInterop Module { get; }

    /// <summary>The drag coordinator every list of the test shares: the registered one, or the one the lists created.</summary>
    internal BortableService Coordinator => BortableService.Of(Services);

    /// <summary>The options a list last sent the browser layer (with <c>register</c> or <c>configure</c>).</summary>
    internal BortableJsOptions? SentOptions<TItem>(IRenderedComponent<Bortable<TItem>> list)
        => Module.Invocations.Where(i => i.Identifier is "register" or "configure")
            .Select(i => i.Arguments[^1]).OfType<BortableJsOptions>().LastOrDefault(o => o.Id == list.Instance.Id);

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

    // ---- the drag, as the browser layer reports it ----
    // Every call runs on the renderer's dispatcher, as the browser's calls into .NET do.

    protected static IElement Container<TItem>(IRenderedComponent<Bortable<TItem>> list)
        => list.Find("[data-bortable-list]");

    /// <summary>The wrapper of the item labelled <paramref name="label"/> (never the drop placeholder).</summary>
    protected static IElement ItemElement<TItem>(IRenderedComponent<Bortable<TItem>> list, string label)
        => list.FindAll(ItemSelector).Single(e => e.TextContent.Trim() == label);

    /// <summary>The key the item labelled <paramref name="label"/> is rendered with, which the browser layer names it by.</summary>
    protected static string KeyOf<TItem>(IRenderedComponent<Bortable<TItem>> list, string label)
        => ItemElement(list, label).GetAttribute("data-bortable-key")
           ?? throw new InvalidOperationException($"{label} has no key.");

    /// <summary>The list with the id <paramref name="id"/> among the lists rendered inside <paramref name="root"/>.</summary>
    protected static IRenderedComponent<Bortable<TItem>> ListById<TItem>(IRenderedComponent<Bortable<TItem>> root, string id)
        => root.FindComponents<Bortable<TItem>>().Single(c => c.Instance.Id == id);

    internal static IBortableZone Zone<TItem>(IRenderedComponent<Bortable<TItem>> list) => list.Instance;

    /// <summary>A drag of the item labelled <paramref name="label"/> starts; returns what the list answers the browser layer.</summary>
    internal Task<BortableStartResult?> DragStartAsync<TItem>(IRenderedComponent<Bortable<TItem>> list, string label, string input = "mouse")
        => StartByKeyAsync(list, KeyOf(list, label), input);

    internal Task<BortableStartResult?> StartByKeyAsync<TItem>(IRenderedComponent<Bortable<TItem>> list, string key, string input = "mouse")
        => Renderer.Dispatcher.InvokeAsync(() => Zone(list).StartAsync(key, input));

    /// <summary>The pointer reached drop position <paramref name="dropIndex"/> of <paramref name="list"/>; returns whether a list changed.</summary>
    protected async Task<bool> DragOverAsync<TItem>(IRenderedComponent<Bortable<TItem>> list, int dropIndex)
        => await OverResultAsync(list, dropIndex) > 0;

    /// <summary>What the list answers the browser layer for drop position <paramref name="dropIndex"/>: 1 changed, 0 unchanged, -1 refused.</summary>
    protected Task<int> OverResultAsync<TItem>(IRenderedComponent<Bortable<TItem>> list, int dropIndex)
        => Renderer.Dispatcher.InvokeAsync(() => Zone(list).OverAsync(dropIndex));

    /// <summary>In swap mode: the pointer is over item <paramref name="itemIndex"/> of <paramref name="list"/> (<c>-1</c>: none).</summary>
    protected Task<bool> HoverItemAsync<TItem>(IRenderedComponent<Bortable<TItem>> list, int itemIndex)
        => DragOverAsync(list, itemIndex);

    /// <summary>The item was released over <paramref name="list"/>, a list that takes the drag.</summary>
    protected Task DropAsync<TItem>(IRenderedComponent<Bortable<TItem>> list)
        => EndAsync(list, "drop");

    /// <summary>The item was released outside every list (the browser reports only its dragend).</summary>
    protected Task DragEndAsync<TItem>(IRenderedComponent<Bortable<TItem>> list, string label)
        => EndAsync(list, "release");

    /// <summary>The browser layer reports the end of the drag, through the bridge of <paramref name="list"/>.</summary>
    protected Task EndAsync<TItem>(IRenderedComponent<Bortable<TItem>> list, string how)
        => Renderer.Dispatcher.InvokeAsync(() => Zone(list).EndAsync(how));

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
