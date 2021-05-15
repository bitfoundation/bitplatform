using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages
{
    public partial class Component
    {
        private bool IsCheckBoxChecked = false;
        private bool IsCheckBoxIndeterminate = true;
        private bool IsCheckBoxIndeterminateInCode = true;
        private bool IsToggleChecked = true;
        private bool IsToggleUnChecked = false;

        private bool IsMessageBarHidden = false;
        private TextFieldType InputType = TextFieldType.Password;

        private void HideMessageBar(MouseEventArgs args)
        {
            IsMessageBarHidden = true;
        }

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
                Text = "Banana",
                Value = "f-ban",
            });
            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Divider,
            });
            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Normal,
                Text = "Orange",
                Value = "f-ora",
                IsDisabled = true
            });
            return items;
        }
    }
}
