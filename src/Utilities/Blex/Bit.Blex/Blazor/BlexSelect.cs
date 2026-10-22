using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Blex.Blazor;

/// <summary>
/// Renders its content from a projection of a store, and re-renders that content -- and nothing else --
/// when the projection changes. Selective rendering in markup: the component that hosts it does not
/// subscribe, does not re-render, and does not need to inherit <see cref="BlexComponentBase"/>.
/// </summary>
/// <typeparam name="TValue">The projected value; inferred from <see cref="Selector"/>.</typeparam>
/// <remarks>
/// <para>
/// A page that shows a cart total in its header re-renders as a whole on every cart change when the
/// page subscribes to the cart. Wrapped in a <c>&lt;BlexSelect&gt;</c>, only the total does, and only
/// when the total differs -- adding a line that leaves it unchanged costs nothing.
/// </para>
/// <para>
/// The selector is re-read whenever the host re-renders, so it may capture the host's parameters
/// (an id, a filter) freely: the comparison is always against what is on screen.
/// </para>
/// </remarks>
/// <example><code>
/// &lt;BlexSelect Store="Cart" Selector="() =&gt; Cart.Total" Context="total"&gt;
///     &lt;strong&gt;@total.ToString("C")&lt;/strong&gt;
/// &lt;/BlexSelect&gt;
/// </code></example>
public sealed class BlexSelect<TValue> : ComponentBase, IDisposable
{
    private IBlexStore? _subscribed;
    private TValue _value = default!;
    private bool _disposed;

    /// <summary>The store whose changes re-evaluate <see cref="Selector"/>.</summary>
    [Parameter, EditorRequired] public IBlexStore Store { get; set; } = default!;

    /// <summary>Projects the rendered value from the store.</summary>
    [Parameter, EditorRequired] public Func<TValue> Selector { get; set; } = default!;

    /// <summary>
    /// Decides what counts as a change; defaults to <see cref="EqualityComparer{T}.Default"/>. Use
    /// <see cref="BlexComparers.Sequence{T}"/> for a projection that builds a new collection each time.
    /// </summary>
    [Parameter] public IEqualityComparer<TValue>? Comparer { get; set; }

    /// <summary>The content, rendered with the selected value as its <c>context</c>.</summary>
    [Parameter] public RenderFragment<TValue>? ChildContent { get; set; }

    /// <summary>The value the content was last rendered with.</summary>
    public TValue Value => _value;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Store is null)
            throw new InvalidOperationException($"{nameof(BlexSelect<TValue>)} requires a {nameof(Store)} parameter.");
        if (Selector is null)
            throw new InvalidOperationException($"{nameof(BlexSelect<TValue>)} requires a {nameof(Selector)} parameter.");

        if (!ReferenceEquals(Store, _subscribed))
        {
            if (_subscribed is not null)
                _subscribed.StateChanged -= OnStoreChanged;

            _subscribed = Store;
            _subscribed.StateChanged += OnStoreChanged;
        }

        // The host re-rendered, so the selector may now read different state (another id, another
        // filter). Comparing later changes against this value, not against whatever an earlier
        // selector last saw, is what keeps a change from being missed.
        _value = Selector();
    }

    private void OnStoreChanged()
    {
        if (_disposed)
            return;

        var value = Selector();
        if ((Comparer ?? EqualityComparer<TValue>.Default).Equals(value, _value))
            return;

        _value = value;
        BlexRender.Observe(InvokeAsync(StateHasChanged), DispatchExceptionAsync);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent is not null)
            builder.AddContent(0, ChildContent, _value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_subscribed is not null)
            _subscribed.StateChanged -= OnStoreChanged;
        _subscribed = null;
    }
}
