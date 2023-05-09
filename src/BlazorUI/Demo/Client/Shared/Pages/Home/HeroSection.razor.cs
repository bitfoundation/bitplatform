namespace Bit.BlazorUI.Demo.Client.Shared.Pages;
public partial class HeroSection
{
    private List<BitDropdownItem> _productDropDownItems = default!;
    private string _selectedColor = "#0065EF";
    private bool _isToggleChecked = true;
    private bool _isToggleUnChecked = false;
    private string _pivotSelectedKey = "Overview";

    protected override Task OnInitAsync()
    {
        _productDropDownItems = new()
        {
            new()
            {
                ItemType = BitDropdownItemType1.Header,
                Text = "Fruits"
            },
            new()
            {
                ItemType = BitDropdownItemType1.Normal,
                Text = "Apple",
                Value = "f-app"
            },
            new()
            {
                ItemType = BitDropdownItemType1.Normal,
                Text = "Orange",
                Value = "f-ora",
                IsEnabled = false
            },
            new()
            {
                ItemType = BitDropdownItemType1.Normal,
                Text = "Banana",
                Value = "f-ban",
            },
            new()
            {
                ItemType = BitDropdownItemType1.Header,
                Text = "Vegetables"
            },
            new()
            {
                ItemType = BitDropdownItemType1.Normal,
                Text = "Broccoli",
                Value = "v-bro",
            }
        };

        return base.OnInitAsync();
    }
}
