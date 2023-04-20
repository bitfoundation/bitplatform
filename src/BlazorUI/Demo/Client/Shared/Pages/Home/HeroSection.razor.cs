namespace Bit.BlazorUI.Demo.Client.Shared.Pages;
public partial class HeroSection
{
    private List<BitDropDownItem> _productDropDownItems = default!;
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
                ItemType = BitDropDownItemType.Header,
                Text = "Fruits"
            },
            new()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Apple",
                Value = "f-app"
            },
            new()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Orange",
                Value = "f-ora",
                IsEnabled = false
            },
            new()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Banana",
                Value = "f-ban",
            },
            new()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "Vegetables"
            },
            new()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Broccoli",
                Value = "v-bro",
            }
        };

        return base.OnInitAsync();
    }
}
