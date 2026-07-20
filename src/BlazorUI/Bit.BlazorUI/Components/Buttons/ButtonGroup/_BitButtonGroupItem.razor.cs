namespace Bit.BlazorUI;

public partial class _BitButtonGroupItem<TItem> where TItem : class
{
    private ElementReference _element;


    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitButtonGroup<TItem> ButtonGroup { get; set; } = default!;


    // The group needs the element references of its items to move the focus while the arrow keys
    // are navigating the roving tabindex.
    protected override void OnAfterRender(bool firstRender)
    {
        ButtonGroup?.RegisterItemElement(Item, _element);

        base.OnAfterRender(firstRender);
    }
}
