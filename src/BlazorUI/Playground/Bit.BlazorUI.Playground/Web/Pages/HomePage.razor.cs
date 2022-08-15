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
                 Key = "f1",
                 href = "/components/breadcrumb",
                 OnClick = (() => OnClickValue = "Folder 1 clicked")
             },
             new()
             {
                 Text = "Folder 2",
                 Key = "f2",
                 href = "/components/breadcrumb",
                 OnClick = (() => OnClickValue = "Folder 2 clicked")
             },
             new()
             {
                 Text = "Folder 3",
                 Key = "f3",
                 href = "/components/breadcrumb",
                 OnClick = (() => OnClickValue = "Folder 3 clicked")
             },
             new()
             {
                 Text = "Folder 4",
                 Key = "f4",
                 href = "/components/breadcrumb",
                 IsCurrentItem = true,
                 OnClick = (() => OnClickValue = "Folder 4 clicked")
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
