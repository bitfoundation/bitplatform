using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Blex.Blazor;

/// <summary>
/// Gives the content its own instance of <typeparamref name="TStore"/> for as long as the scope is on
/// screen: created from DI when the scope appears, registered with the manager under
/// <c>"{name}:{key}"</c>, cascaded to every descendant, and unregistered and disposed when the scope
/// goes away. The Blazor face of <see cref="BlexManager.CreateScope{TStore}(TStore, string)"/>.
/// </summary>
/// <typeparam name="TStore">The store type to instantiate.</typeparam>
/// <remarks>
/// <para>
/// Use it for state that belongs to one part of the page rather than to the app: the form inside each
/// of two open dialogs, one editor per tab, a wizard that must start fresh every time it opens.
/// Descendants receive the instance with <c>[CascadingParameter] public TStore Store { get; set; }</c>,
/// or through the content's <c>context</c>. The store is not registered with
/// <c>AddBitBlexStore</c>; its constructor dependencies are resolved from DI all the same.
/// </para>
/// <para>
/// Changing <see cref="Key"/> replaces the instance (and the descendants, which are keyed to it), so
/// <c>Key="@DocumentId"</c> gives every document its own store. A store marked <c>Persist = true</c>
/// is hydrated when the scope registers it -- give it a stable key so the next scope for the same
/// entity finds its state. Scoped stores are not part of the prerender handoff.
/// </para>
/// </remarks>
/// <example><code>
/// &lt;BlexScope TStore="CheckoutStore" Key="@OrderId"&gt;
///     &lt;CheckoutSteps /&gt;   @* [CascadingParameter] public CheckoutStore Checkout { get; set; } *@
/// &lt;/BlexScope&gt;
/// </code></example>
public sealed class BlexScope<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore> : ComponentBase, IDisposable
    where TStore : BlexStoreBase
{
    private BlexStoreScope<TStore>? _scope;
    private string? _scopedKey;
    private bool _disposed;

    [Inject] private BlexManager Manager { get; set; } = default!;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    /// <summary>
    /// The instance key: an id of whatever the store is about. Changing it replaces the instance. When
    /// omitted, a key unique within the manager is assigned and kept for the scope's lifetime.
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// Creates the store, for one that needs more than DI can supply (an argument from the page).
    /// Defaults to constructing <typeparamref name="TStore"/> with its dependencies resolved from DI.
    /// </summary>
    [Parameter] public Func<IServiceProvider, TStore>? Factory { get; set; }

    /// <summary>The content, rendered with the scoped store as its <c>context</c>.</summary>
    [Parameter] public RenderFragment<TStore>? ChildContent { get; set; }

    /// <summary>The scoped store instance. Available once the scope has initialized.</summary>
    public TStore Store => _scope?.Store
        ?? throw new InvalidOperationException($"{nameof(BlexScope<TStore>)} has not created its store yet.");

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (_scope is not null && string.Equals(_scopedKey, Key, StringComparison.Ordinal))
            return;

        // Release the outgoing instance first: with persistence on, its last debounced save is
        // written as it unregisters, before a new instance for another key starts reading.
        var outgoing = _scope;
        _scope = null;
        outgoing?.Dispose();

        var store = Factory is null
            ? ActivatorUtilities.CreateInstance<TStore>(Services)
            : Factory(Services) ?? throw new InvalidOperationException($"The {nameof(Factory)} of {nameof(BlexScope<TStore>)} returned null.");

        try
        {
            _scope = Manager.CreateScope(store, Key);
        }
        catch
        {
            store.Dispose();
            throw;
        }

        _scopedKey = Key;
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_scope is not { } scope)
            return;

        var store = scope.Store;
        builder.OpenComponent<CascadingValue<TStore>>(0);

        // Keyed to the instance, so a new store (a new Key) rebuilds the subtree: descendants holding
        // per-instance state of their own must not carry it over to another entity.
        builder.SetKey(store);
        builder.AddComponentParameter(1, nameof(CascadingValue<TStore>.Value), store);
        builder.AddComponentParameter(2, nameof(CascadingValue<TStore>.IsFixed), true);
        builder.AddComponentParameter(3, nameof(CascadingValue<TStore>.ChildContent), (RenderFragment)(content =>
        {
            if (ChildContent is not null)
                content.AddContent(0, ChildContent, store);
        }));
        builder.CloseComponent();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _scope?.Dispose();
        _scope = null;
    }
}
