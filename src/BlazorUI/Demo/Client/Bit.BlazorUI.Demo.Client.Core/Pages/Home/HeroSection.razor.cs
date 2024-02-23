namespace Bit.BlazorUI.Demo.Client.Core.Pages;
public partial class HeroSection
{
    private List<BitDropdownItem<string>> _productDropdownItems = default!;
    private string _selectedColor = "#0065EF";
    private bool _isToggleChecked = true;
    private bool _isToggleUnChecked = false;
    private string? _pivotSelectedKey = "Overview";

    protected override Task OnInitAsync()
    {
        _productDropdownItems = new()
        {
            new()
            {
                ItemType = BitDropdownItemType.Header,
                Text = "Fruits"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Apple",
                Value = "f-app"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Orange",
                Value = "f-ora",
                IsEnabled = false
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Banana",
                Value = "f-ban",
            },
            new()
            {
                ItemType = BitDropdownItemType.Header,
                Text = "Vegetables"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Broccoli",
                Value = "v-bro",
            }
        };

        return base.OnInitAsync();
    }
}
