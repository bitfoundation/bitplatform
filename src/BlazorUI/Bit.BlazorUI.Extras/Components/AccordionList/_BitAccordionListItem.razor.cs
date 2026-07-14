namespace Bit.BlazorUI;

public partial class _BitAccordionListItem<TItem> : ComponentBase where TItem : class
{
    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitAccordionList<TItem> AccordionList { get; set; } = default!;
}
