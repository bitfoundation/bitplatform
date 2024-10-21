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
        await _virtualizeRef!.RefreshDataAsync();
    }



    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;
        builder.OpenComponent<Virtualize<TItem>>(seq++);
        builder.AddAttribute(seq++, nameof(Virtualize<TItem>.Items), Items);
        builder.AddAttribute(seq++, nameof(Virtualize<TItem>.ItemSize), ItemSize);
        builder.AddAttribute(seq++, nameof(Virtualize<TItem>.Placeholder), Placeholder);
        builder.AddAttribute(seq++, nameof(Virtualize<TItem>.ItemsProvider), ItemsProvider);
        builder.AddAttribute(seq++, nameof(Virtualize<TItem>.OverscanCount), OverscanCount);

        builder.AddAttribute(seq++, nameof(Virtualize<TItem>.ItemContent),
            (RenderFragment<TItem>)(item => b => b.AddContent(seq++, (ItemContent ?? ChildContent)?.Invoke(item))));

        builder.AddAttribute(seq++, nameof(Virtualize<TItem>.EmptyContent), (RenderFragment)(b => b.AddContent(seq++, EmptyContent)));

        builder.AddComponentReferenceCapture(seq++, v => _virtualizeRef = (Virtualize<TItem>)v);

        builder.CloseComponent();

        base.BuildRenderTree(builder);
    }
}
