namespace Bit.BlazorUI;

public partial class _BitButtonGroupItem<TItem> where TItem : class
{
    private ElementReference _element;


    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitButtonGroup<TItem> ButtonGroup { get; set; } = default!;


    // The group needs the element references of its items to move the focus while the arrow keys
    // are navigating the roving tabindex, and to give the focus away on AutoFocus and FocusAsync.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (ButtonGroup is not null)
        {
            await ButtonGroup.RegisterItemElement(Item, _element);
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}
