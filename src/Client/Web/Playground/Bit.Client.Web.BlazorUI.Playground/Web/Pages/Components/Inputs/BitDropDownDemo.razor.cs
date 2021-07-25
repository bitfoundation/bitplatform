using System.Collections.Generic;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Inputs
{
    public partial class BitDropDownDemo
    {
        private List<DropDownItem> GetDropdownItems()
        {
            List<DropDownItem> items = new();

            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Header,
                Text = "Fruits"
            });

            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Normal,
                Text = "Apple",
                Value = "f-app"
            });

            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Normal,
                Text = "Orange",
                Value = "f-ora",
                IsEnabled = false
            });

            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Normal,
                Text = "Banana",
                Value = "f-ban",
            });

            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Divider,
            });

            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Header,
                Text = "Vegetables"
            });

            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Normal,
                Text = "Broccoli",
                Value = "v-bro",
            });

            return items;
        }
    }
}
