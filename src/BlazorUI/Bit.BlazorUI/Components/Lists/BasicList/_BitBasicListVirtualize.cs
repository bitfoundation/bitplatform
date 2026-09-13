namespace Bit.BlazorUI;

public class _BitBasicListVirtualize<TItem> : ComponentBase
{
    private Virtualize<TItem>? _virtualizeRef;



    [Parameter] public RenderFragment<TItem>? ChildContent { get; set; }

    [Parameter] public RenderFragment? EmptyContent { get; set; }

    [Parameter] public ICollection<TItem>? Items { get; set; }

    [Parameter] public RenderFragment<TItem>? ItemContent { get; set; }

    [Parameter] public float ItemSize { get; set; } = 50;

    [Parameter] public ItemsProviderDelegate<TItem>? ItemsProvider { get; set; }

    [Parameter] public int OverscanCount { get; set; } = 3;

    [Parameter] public RenderFragment<PlaceholderContext>? Placeholder { get; set; }



    public async Task RefreshDataAsync()
    {
        // The reference is only captured once the component has rendered, so a refresh asked for
        // before that (or after the list stopped rendering the virtualized branch) has nothing to
        // refresh rather than being an error.
        if (_virtualizeRef is null) return;

        await _virtualizeRef.RefreshDataAsync();
    }



    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // The sequence numbers are literal constants on purpose: they identify a position in the render
        // tree across renders, so a counter that keeps running (and the closures below would keep it
        // running on every invocation of the fragment) would defeat the diffing rather than help it.
        builder.OpenComponent<Virtualize<TItem>>(0);
        builder.AddAttribute(1, nameof(Virtualize<>.Items), Items);
        builder.AddAttribute(2, nameof(Virtualize<>.ItemSize), ItemSize);
        builder.AddAttribute(3, nameof(Virtualize<>.Placeholder), Placeholder);
        builder.AddAttribute(4, nameof(Virtualize<>.ItemsProvider), ItemsProvider);
        builder.AddAttribute(5, nameof(Virtualize<>.OverscanCount), OverscanCount);
        builder.AddAttribute(6, nameof(Virtualize<>.EmptyContent), EmptyContent);

        builder.AddAttribute(7, nameof(Virtualize<>.ItemContent),
                                   (RenderFragment<TItem>)(item => b => b.AddContent(0, (ItemContent ?? ChildContent)?.Invoke(item))));

        builder.AddComponentReferenceCapture(8, v => _virtualizeRef = (Virtualize<TItem>)v);

        builder.CloseComponent();

        base.BuildRenderTree(builder);
    }
}
