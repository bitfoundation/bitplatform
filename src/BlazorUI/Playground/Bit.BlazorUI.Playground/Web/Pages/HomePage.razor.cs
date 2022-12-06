using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages;

public partial class HomePage
{
    public string OnClickValue { get; set; } = string.Empty;
    private string SelectedColor = "#0065EF";
    private bool IsToggleChecked = true;
    private bool IsToggleUnChecked = false; 
    private string PivotSelectedKey = "Overview";

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    private List<BitBreadcrumbItem> GetBreadcrumbItems()
    {
        return new List<BitBreadcrumbItem>()
        {
             new()
             {
                 Text = "Folder 1",
                 href = "/components/breadcrumb",
             },
             new()
             {
                 Text = "Folder 2",
                 href = "/components/breadcrumb",
             },
             new()
             {
                 Text = "Folder 3",
                 href = "/components/breadcrumb",
             },
             new()
             {
                 Text = "Folder 4",
                 href = "/components/breadcrumb",
             }
        };
    }

    private List<BitDropDownItem> GetProductDropdownItems()
    {
        List<BitDropDownItem> items = new();

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = "Fruits"
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Apple",
            Value = "f-app"
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Orange",
            Value = "f-ora",
            IsEnabled = false
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Banana",
            Value = "f-ban",
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Divider,
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = "Vegetables"
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Broccoli",
            Value = "v-bro",
        });

        return items;
    }
}
