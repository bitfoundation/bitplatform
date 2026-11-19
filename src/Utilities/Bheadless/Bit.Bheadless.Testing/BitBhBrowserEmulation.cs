using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

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
        // The emulated browser has the strict CSP style bridge in place (a runtime that could not load it answers false).
        module.Setup<bool>("styles.enable").SetResult(true);
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
