using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless.Testing;

/// <summary>
/// A bUnit test context prepared for Bheadless components. Derive your test
/// classes from it instead of <see cref="BunitContext"/>.
/// </summary>
/// <remarks>
/// <para>On top of <see cref="BitBhTestingExtensions.AddBitBheadlessServices"/> (loose JS interop,
/// library services, a recording <see cref="Announcer"/>), it emulates the part of the
/// browser runtime that affects behaviour rather than visuals: item collections
/// (options, menu items, tree items, roving items …) learn their <b>document order</b>
/// and <b>rendered text</b> from the rendered markup, exactly as they do from the real
/// DOM. Typeahead, text-based filtering and arrow-key order then work as in a browser.</para>
/// <para>Focus is not emulated; assert focus moves with <see cref="BitBhTestingExtensions.LastFocusRequest"/>.</para>
/// </remarks>
public abstract class BitBhTestContext : BunitContext
{
    private readonly BitBhBrowserEmulation _browser;

    /// <summary>Creates the context.</summary>
    protected BitBhTestContext()
    {
        Announcer = this.AddBitBheadlessServices();
        _browser = new BitBhBrowserEmulation(this);
    }

    /// <summary>Records live-region announcements.</summary>
    public BitBhTestAnnouncer Announcer { get; }

    /// <inheritdoc />
    public override IRenderedComponent<TComponent> Render<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>>? parameterBuilder = null)
        => _browser.Track(() => base.Render(parameterBuilder));

    /// <inheritdoc />
    public override IRenderedComponent<TComponent> Render<TComponent>(RenderFragment renderFragment)
        => _browser.Track(() => base.Render<TComponent>(renderFragment));

    /// <inheritdoc />
    public override IRenderedComponent<ContainerFragment> Render(RenderFragment renderFragment)
        => _browser.Track(() => base.Render(renderFragment));
}

/// <summary>Answers the runtime's DOM queries from bUnit's rendered markup.</summary>
internal sealed class BitBhBrowserEmulation
{
    private readonly BunitContext _context;
    private readonly List<Func<INodeList>> _roots = new();
    private readonly List<(string Key, TaskCompletionSource<string[][]?> Result)> _pending = new();
    private readonly Lock _gate = new();
    private int _rendering;

    public BitBhBrowserEmulation(BunitContext context)
    {
        _context = context;
        var handler = new CollectionOrderHandler(this);
        context.JSInterop.AddInvocationHandler(handler);
        // The runtime is an ES module: answer calls made on it as well.
        var module = context.JSInterop.SetupModule(BitBhInterop.ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        module.AddInvocationHandler(new CollectionOrderHandler(this));
    }

    public IRenderedComponent<TComponent> Track<TComponent>(Func<IRenderedComponent<TComponent>> render)
        where TComponent : IComponent
    {
        // Queries made during the first render are answered once the markup exists.
        lock (_gate) _rendering++;
        IRenderedComponent<TComponent>? cut = null;
        (string Key, TaskCompletionSource<string[][]?> Result)[] pending = [];
        try
        {
            cut = render();
        }
        finally
        {
            lock (_gate)
            {
                _rendering--;
                if (cut is not null) _roots.Add(() => cut.Nodes);
                if (_rendering == 0)
                {
                    pending = _pending.ToArray();
                    _pending.Clear();
                }
            }
            // The render failed: there is no markup to answer from, but the queries must not hang.
            if (cut is null)
                foreach (var (_, result) in pending) result.TrySetResult(null);
        }
        if (pending.Length > 0)
        {
            cut.InvokeAsync(() =>
            {
                foreach (var (key, result) in pending) result.TrySetResult(Query(key));
            }).GetAwaiter().GetResult();
            // The awaiting components resume through the renderer's queue; let them finish
            // before the test interacts (bUnit's synchronous event helpers don't wait).
            for (var i = 0; i < 3; i++) cut.InvokeAsync(static () => { }).GetAwaiter().GetResult();
        }
        return cut;
    }

    public Task<string[][]?> Order(string key)
    {
        lock (_gate)
        {
            if (_rendering > 0)
            {
                var tcs = new TaskCompletionSource<string[][]?>();
                _pending.Add((key, tcs));
                return tcs.Task;
            }
            // Nothing was rendered through this context (and nothing is rendering): no markup to read the order from.
            if (_roots.Count == 0) return Task.FromResult<string[][]?>(null);
        }
        return Task.FromResult(Query(key));
    }

    private string[][]? Query(string key)
    {
        var result = new List<string[]>();
        foreach (var root in _roots)
        {
            INodeList nodes;
            try { nodes = root(); }
            catch (ObjectDisposedException) { continue; }

            foreach (var node in nodes)
            {
                if (node is not IElement element) continue;
                var matches = Items(element, key);
                IEnumerable<IElement> all = element.GetAttribute(ItemAttribute) == key ? matches.Prepend(element) : matches;
                foreach (var item in all)
                {
                    var text = item.GetAttribute("data-bit-bh-text") ?? OwnText(item, key);
                    result.Add([item.Id ?? string.Empty, string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))]);
                }
            }
        }
        return result.Count == 0 ? null : result.ToArray();
    }

    /// <summary>The item's text without nested items of the same collection (e.g. child tree items).</summary>
    private static string OwnText(IElement item, string key)
    {
        if (!Items(item, key).Any()) return item.TextContent;
        var clone = (IElement)item.Clone(deep: true);
        foreach (var nested in Items(clone, key).ToArray()) nested.Remove();
        return clone.TextContent;
    }

    private const string ItemAttribute = "data-bit-bh-item";

    /// <summary>The descendants of <paramref name="element"/> in collection <paramref name="key"/>; compared, not selected, so any key works.</summary>
    private static IEnumerable<IElement> Items(IElement element, string key)
        => element.QuerySelectorAll($"[{ItemAttribute}]").Where(e => e.GetAttribute(ItemAttribute) == key);

    private sealed class CollectionOrderHandler(BitBhBrowserEmulation owner)
        : JSRuntimeInvocationHandlerBase<string[][]?>(i => i.Identifier == "collection.order", isCatchAllHandler: false)
    {
        protected override Task<string[][]?> HandleAsync(JSRuntimeInvocation invocation)
        {
            base.HandleAsync(invocation);
            return owner.Order(invocation.Arguments.Count > 0 ? invocation.Arguments[0] as string ?? string.Empty : string.Empty);
        }
    }
}
