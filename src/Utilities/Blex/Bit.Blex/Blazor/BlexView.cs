using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Blex.Blazor;

/// <summary>
/// Re-renders its content whenever one of the given stores changes -- <see cref="BlexComponentBase"/>'s
/// <c>Subscribe(store)</c> as markup, for a component that cannot inherit it (it already has a base
/// class) or that only wants part of its output to follow a store.
/// </summary>
/// <remarks>
/// Only the content re-renders; the component hosting the view does not. Reach for
/// <see cref="BlexSelect{TValue}"/> instead when the content depends on one value of the store rather
/// than on all of it.
/// </remarks>
/// <example><code>
/// &lt;BlexView Store="Cart"&gt;
///     @Cart.Lines.Count items, @Cart.Total.ToString("C")
/// &lt;/BlexView&gt;
///
/// &lt;BlexView Stores="[Cart, Profile]"&gt; ... &lt;/BlexView&gt;
/// </code></example>
public sealed class BlexView : ComponentBase, IDisposable
{
    private readonly List<IBlexStore> _subscribed = [];
    private bool _disposed;

    /// <summary>A store whose changes re-render the content.</summary>
    [Parameter] public IBlexStore? Store { get; set; }

    /// <summary>Several stores whose changes re-render the content (combined with <see cref="Store"/>).</summary>
    [Parameter] public IEnumerable<IBlexStore>? Stores { get; set; }

    /// <summary>The content to keep current.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var wanted = new List<IBlexStore>();
        if (Store is not null)
            wanted.Add(Store);

        if (Stores is not null)
        {
            foreach (var store in Stores)
            {
                if (store is not null && !wanted.Contains(store))
                    wanted.Add(store);
            }
        }

        // Re-subscribe only what changed: a host re-render passes a fresh array of the same stores
        // every time, and churning the handlers for it would be pure overhead.
        for (var i = _subscribed.Count - 1; i >= 0; i--)
        {
            if (!wanted.Contains(_subscribed[i]))
            {
                _subscribed[i].StateChanged -= OnStoreChanged;
                _subscribed.RemoveAt(i);
            }
        }

        foreach (var store in wanted)
        {
            if (!_subscribed.Contains(store))
            {
                store.StateChanged += OnStoreChanged;
                _subscribed.Add(store);
            }
        }
    }

    private void OnStoreChanged()
    {
        if (_disposed)
            return;

        BlexRender.Observe(InvokeAsync(StateHasChanged), DispatchExceptionAsync);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
        => builder.AddContent(0, ChildContent);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (var store in _subscribed)
            store.StateChanged -= OnStoreChanged;
        _subscribed.Clear();
    }
}
