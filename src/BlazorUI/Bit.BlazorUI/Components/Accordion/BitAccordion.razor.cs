
namespace Bit.BlazorUI;

public partial class BitAccordion
{
    private IEnumerable<BitAccordionItem> _items = default!;

    [Parameter] public IEnumerable<BitAccordionItem> Items { get; set; } = new List<BitAccordionItem>();

    [Parameter] public bool DefaultIsExpanded { get; set; }

    [Parameter] public bool OnlyOneIsExpand { get; set; }

    [Parameter] public EventCallback<BitAccordionItem> OnClick { get; set; }

    protected override string RootElementClass => "bit-acd";

    protected override async Task OnInitializedAsync()
    {
        _items = Items;

        if (DefaultIsExpanded)
        {
            if (OnlyOneIsExpand)
            {
                _items.First().IsExpanded = true;
            }
            else
            {
                foreach (var item in _items)
                {
                    item.IsExpanded = true;
                }
            }
        }

        await base.OnInitializedAsync();
    }

    private async Task HandleOnItemClick(BitAccordionItem item)
    {
        if (IsEnabled is false || item.IsEnabled is false) return;

        if (OnlyOneIsExpand && _items.Any(i => i != item && i.IsExpanded))
        {
            _items.First(i => i != item && i.IsExpanded).IsExpanded = false;
        }

        _items.First(i => i == item).IsExpanded = !(_items.First(i => i == item).IsExpanded);

        await OnClick.InvokeAsync(item);

        StateHasChanged();
    }
}
