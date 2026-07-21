namespace Bit.BlazorUI;

public partial class _BitChoiceGroupItem<TItem, TValue> : ComponentBase where TItem : class, new ()
{
    private ElementReference _inputElement;

    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitChoiceGroup<TItem, TValue> ChoiceGroup { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        await ChoiceGroup.SetInputElement(Item, _inputElement);
    }
}
