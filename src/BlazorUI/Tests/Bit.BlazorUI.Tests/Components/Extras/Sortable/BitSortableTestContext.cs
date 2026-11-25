using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Sortable;

/// <summary>
/// The shared set-up of the BitSortable tests. The sorting engine is C#; the browser layer (BitSortable.ts) routes the
/// input and hit-tests the lists where the pointer is, and calls a list only when something changes: <c>OnDragStart</c>
/// (a drag of the item with this key starts), <c>OnDragOver</c> (the pointer reached this drop position of this list)
/// and <c>OnDragEnd</c> (the drag ended, and how). bUnit has no layout and runs no JavaScript, so these tests make those
/// calls themselves, through the list's <see cref="IBitSortableZone"/>, with the positions the browser would have
/// measured. A drag then becomes a deterministic sequence of calls whose outcome is decided by the engine alone, which
/// is exactly what these tests are about.
/// </summary>
/// <remarks>
/// The JS interop is loose (see <see cref="BunitTestContext"/>): every call the list makes into the script (register,
/// configure, the FLIP calls, announce) is fire-and-forget, so none needs a set-up of its own, and each is recorded under
/// its <c>BitBlazorUI.Sortable.*</c> identifier.
/// </remarks>
public abstract class BitSortableTestContext : BunitTestContext
{
    internal const string Register = "BitBlazorUI.Sortable.register";
    internal const string Configure = "BitBlazorUI.Sortable.configure";
    internal const string Unregister = "BitBlazorUI.Sortable.unregister";
    internal const string Capture = "BitBlazorUI.Sortable.capture";
    internal const string Play = "BitBlazorUI.Sortable.play";
    internal const string Announce = "BitBlazorUI.Sortable.announce";

    private const string ItemSelector = "[data-bit-srt-item]:not([data-bit-srt-placeholder])";

    protected BunitJSInterop JSInterop => Context.JSInterop;

    protected IRenderedComponent<TComponent> Render<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>>? parameterBuilder = null)
        where TComponent : IComponent
        => Context.Render(parameterBuilder);

    protected Task DisposeComponentsAsync() => Context.DisposeComponentsAsync();

    /// <summary>The drag coordinator every list of the test shares: the one the lists created for their scope.</summary>
    /// <remarks>Asked of the provider the lists are injected with, which is bUnit's inner one rather than <see cref="BunitTestContext.Services"/>.</remarks>
    internal BitSortableCoordinator Coordinator => BitSortableCoordinator.Of(Services.GetRequiredService<IServiceProvider>());

    /// <summary>The options a list last sent the browser layer (with <c>register</c> or <c>configure</c>).</summary>
    internal BitSortableJsOptions? SentOptions<TItem>(IRenderedComponent<BitSortable<TItem>> list)
        => JSInterop.Invocations.Where(i => i.Identifier is Register or Configure)
            .Select(i => i.Arguments[^1]).OfType<BitSortableJsOptions>().LastOrDefault(o => o.Id == Zone(list).Id);

    // ---- rendering ----

    protected IRenderedComponent<BitSortable<string>> RenderList(
        IList<string>? items, string? id = null, Action<ComponentParameterCollectionBuilder<BitSortable<string>>>? extra = null)
        => RenderList(items, item => item, id, extra);

    protected IRenderedComponent<BitSortable<TItem>> RenderList<TItem>(
        IList<TItem>? items, Func<TItem, string> label, string? id = null,
        Action<ComponentParameterCollectionBuilder<BitSortable<TItem>>>? extra = null)
        => Render<BitSortable<TItem>>(p =>
        {
            if (id is not null) p.Add(c => c.Id, id);
            p.Add(c => c.Items, items);
            p.Add(c => c.ItemTemplate, item => $"<span>{label(item)}</span>");
            extra?.Invoke(p);
        });

    // ---- the drag, as the browser layer reports it ----
    // Every call runs on the renderer's dispatcher, as the browser's calls into .NET do.

    protected static IElement Container<TItem>(IRenderedComponent<BitSortable<TItem>> list)
        => list.Find("[data-bit-srt-list]");

    /// <summary>The wrapper of the item labelled <paramref name="label"/> (never the drop placeholder).</summary>
    protected static IElement ItemElement<TItem>(IRenderedComponent<BitSortable<TItem>> list, string label)
        => list.FindAll(ItemSelector).Single(e => e.TextContent.Trim() == label);

    /// <summary>The key the item labelled <paramref name="label"/> is rendered with, which the browser layer names it by.</summary>
    protected static string KeyOf<TItem>(IRenderedComponent<BitSortable<TItem>> list, string label)
        => ItemElement(list, label).GetAttribute("data-bit-srt-key")
           ?? throw new InvalidOperationException($"{label} has no key.");

    /// <summary>The list with the id <paramref name="id"/> among the lists rendered inside <paramref name="root"/>.</summary>
    protected static IRenderedComponent<BitSortable<TItem>> ListById<TItem>(IRenderedComponent<BitSortable<TItem>> root, string id)
        => root.FindComponents<BitSortable<TItem>>().Single(c => Zone(c).Id == id);

    internal static IBitSortableZone Zone<TItem>(IRenderedComponent<BitSortable<TItem>> list) => list.Instance;

    /// <summary>A drag of the item labelled <paramref name="label"/> starts; returns what the list answers the browser layer.</summary>
    internal Task<BitSortableStartResult?> DragStartAsync<TItem>(IRenderedComponent<BitSortable<TItem>> list, string label, string input = "mouse")
        => StartByKeyAsync(list, KeyOf(list, label), input);

    internal Task<BitSortableStartResult?> StartByKeyAsync<TItem>(IRenderedComponent<BitSortable<TItem>> list, string key, string input = "mouse")
        => Renderer.Dispatcher.InvokeAsync(() => Zone(list).StartAsync(key, input));

    /// <summary>The pointer reached drop position <paramref name="dropIndex"/> of <paramref name="list"/>; returns whether a list changed.</summary>
    protected async Task<bool> DragOverAsync<TItem>(IRenderedComponent<BitSortable<TItem>> list, int dropIndex)
        => await OverResultAsync(list, dropIndex) > 0;

    /// <summary>What the list answers the browser layer for drop position <paramref name="dropIndex"/>: 1 changed, 0 unchanged, -1 refused.</summary>
    protected Task<int> OverResultAsync<TItem>(IRenderedComponent<BitSortable<TItem>> list, int dropIndex)
        => Renderer.Dispatcher.InvokeAsync(() => Zone(list).OverAsync(dropIndex));

    /// <summary>In swap mode: the pointer is over item <paramref name="itemIndex"/> of <paramref name="list"/> (<c>-1</c>: none).</summary>
    protected Task<bool> HoverItemAsync<TItem>(IRenderedComponent<BitSortable<TItem>> list, int itemIndex)
        => DragOverAsync(list, itemIndex);

    /// <summary>The item was released over <paramref name="list"/>, a list that takes the drag.</summary>
    protected Task DropAsync<TItem>(IRenderedComponent<BitSortable<TItem>> list)
        => EndAsync(list, "drop");

    /// <summary>The item was released outside every list (the browser reports only its dragend).</summary>
    protected Task DragEndAsync<TItem>(IRenderedComponent<BitSortable<TItem>> list, string label)
        => EndAsync(list, "release");

    /// <summary>The browser layer reports the end of the drag, through <paramref name="list"/>.</summary>
    protected Task EndAsync<TItem>(IRenderedComponent<BitSortable<TItem>> list, string how)
        => Renderer.Dispatcher.InvokeAsync(() => Zone(list).EndAsync(how));

    /// <summary>
    /// A key the browser layer reports through <paramref name="list"/>: <paramref name="action"/> on the item rendered
    /// with <paramref name="key"/> (<c>pick</c>, <c>select</c>), or a step of the keyboard drag in progress, to
    /// <paramref name="index"/> of the list <paramref name="toList"/> (or of the list the item is in).
    /// </summary>
    internal Task<BitSortableKeyResult> KeyAsync<TItem>(IRenderedComponent<BitSortable<TItem>> list, string action, string? toList = null, int index = -1, string key = "")
        => Renderer.Dispatcher.InvokeAsync(() => Zone(list).KeyAsync(key, action, toList, index));

    protected static Task ClickAsync<TItem>(IRenderedComponent<BitSortable<TItem>> list, string label)
        => ItemElement(list, label).ClickAsync(new MouseEventArgs());

    /// <summary>
    /// The exception a list passed on during a call the browser layer made, which no code of the app's could catch: it
    /// raises it as its own, for the nearest <c>ErrorBoundary</c>, and without one it reaches the renderer.
    /// </summary>
    protected async Task<Exception> PassedOnAsync()
    {
        var passedOn = Renderer.UnhandledException;
        Assert.IsTrue(passedOn.IsCompleted, "No exception was passed on.");
        return await passedOn;
    }

    // ---- reading the markup ----

    /// <summary>The labels of the rendered items in DOM order, the drop placeholder left out.</summary>
    protected static string Labels<TItem>(IRenderedComponent<BitSortable<TItem>> list)
        => string.Join(",", list.FindAll(ItemSelector).Select(e => e.TextContent.Trim()));

    protected static IElement? Placeholder<TItem>(IRenderedComponent<BitSortable<TItem>> list)
        => list.FindAll("[data-bit-srt-placeholder]").SingleOrDefault();

    /// <summary>The position of the drop placeholder among the list's item wrappers, or -1.</summary>
    protected static int PlaceholderIndex<TItem>(IRenderedComponent<BitSortable<TItem>> list)
    {
        var wrappers = list.FindAll("[data-bit-srt-item]");
        for (int i = 0; i < wrappers.Count; i++)
            if (wrappers[i].HasAttribute("data-bit-srt-placeholder")) return i;
        return -1;
    }

    protected static bool HasClass<TItem>(IRenderedComponent<BitSortable<TItem>> list, string label, string cssClass)
        => ItemElement(list, label).ClassList.Contains(cssClass);

    /// <summary>The labels of the items that carry <paramref name="cssClass"/>, in DOM order.</summary>
    protected static string LabelsWithClass<TItem>(IRenderedComponent<BitSortable<TItem>> list, string cssClass)
        => string.Join(",", list.FindAll(ItemSelector).Where(e => e.ClassList.Contains(cssClass)).Select(e => e.TextContent.Trim()));

    // ---- recording the events ----

    /// <summary>One lifecycle event (or <c>ItemsChanged</c> notification) raised by a list.</summary>
    protected sealed record Raised<TItem>(string List, string Name, BitSortableEventArgs<TItem>? Args);

    /// <summary>
    /// Wires every callback of a list into <paramref name="log"/>, in the order they are raised. The one named
    /// <paramref name="throwing"/> (<c>Start</c>, <c>End</c>, <c>ItemsChanged</c>...) then throws an
    /// <see cref="InvalidOperationException"/> with the message <c>boom</c>, once logged.
    /// </summary>
    protected static void Record<TItem>(ComponentParameterCollectionBuilder<BitSortable<TItem>> p, string list, List<Raised<TItem>> log, string? throwing = null)
    {
        void On(string name, BitSortableEventArgs<TItem>? e)
        {
            log.Add(new(list, name, e));
            if (name == throwing) throw new InvalidOperationException("boom");
        }

        p.Add(c => c.OnStart, e => On("Start", e));
        p.Add(c => c.OnChange, e => On("Change", e));
        p.Add(c => c.OnUpdate, e => On("Update", e));
        p.Add(c => c.OnRemove, e => On("Remove", e));
        p.Add(c => c.OnAdd, e => On("Add", e));
        p.Add(c => c.OnSort, e => On("Sort", e));
        p.Add(c => c.OnEnd, e => On("End", e));
        p.Add(c => c.ItemsChanged, _ => On("ItemsChanged", null));
    }

    /// <summary>The log as "Name:List" entries, e.g. <c>Start:L1 Update:L1 End:L1</c>.</summary>
    protected static string Sequence<TItem>(IEnumerable<Raised<TItem>> log)
        => string.Join(" ", log.Select(r => $"{r.Name}:{r.List}"));

    protected static BitSortableEventArgs<TItem> Single<TItem>(IEnumerable<Raised<TItem>> log, string name, string list)
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
