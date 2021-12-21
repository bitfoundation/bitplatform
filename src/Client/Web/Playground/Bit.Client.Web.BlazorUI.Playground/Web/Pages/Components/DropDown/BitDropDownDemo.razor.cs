using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.DropDown
{
    public partial class BitDropDownDemo
    {
        private string ControlledSelectedKey = "Apple";
        private List<string> ControlledSelectedMultipleKeys = new List<string>() { "Apple", "Banana", "Grape" };

        private List<BitDropDownItem> GetDropdownItems()
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
        private List<BitDropDownItem> GetCustomDropdownItems()
        {
            List<BitDropDownItem> items = new();
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "Options",
                Value = "Header"
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option a",
                Value = "A",
                Data = new DropDownItemData()
                {
                    IconName = "Memo"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option b",
                Value = "B",
                Data = new DropDownItemData()
                {
                    IconName = "Print"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option c",
                Value = "C",
                Data = new DropDownItemData()
                {
                    IconName = "ShoppingCart"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option d",
                Value = "D",
                Data = new DropDownItemData()
                {
                    IconName = "Train"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option e",
                Value = "E",
                Data = new DropDownItemData()
                {
                    IconName = "Repair"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Divider
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "More options",
                Value = "Header2"
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option f",
                Value = "F",
                Data = new DropDownItemData()
                {
                    IconName = "Running"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option g",
                Value = "G",
                Data = new DropDownItemData()
                {
                    IconName = "EmojiNeutral"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option h",
                Value = "H",
                Data = new DropDownItemData()
                {
                    IconName = "ChatInviteFriend"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option i",
                Value = "I",
                Data = new DropDownItemData()
                {
                    IconName = "SecurityGroup"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option j",
                Value = "J",
                Data = new DropDownItemData()
                {
                    IconName = "AddGroup"
                }
            });

            return items;
        }

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "CaretDownFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Optional custom template for chevron icon.",
            },
            new ComponentParameter()
            {
                Name = "DefaultSelectedKey",
                Type = "string",
                DefaultValue = "",
                Description = "Key that will be initially used to set selected item.",
            },
            new ComponentParameter()
            {
                Name = "DefaultSelectedMultipleKeys",
                Type = "List<string>",
                DefaultValue = "",
                Description = "Keys that will be initially used to set selected items for multiSelect scenarios.",
            },
            new ComponentParameter()
            {
                Name = "IsMultiSelect",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether multiple items are allowed to be selected.",
            },
            new ComponentParameter()
            {
                Name = "IsOpen",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not this dropdown is open.",
            },
            new ComponentParameter()
            {
                Name = "IsRequired",
                Type = "bool",
                DefaultValue = "false",
                Description = "Requires the end user to select an item in the dropdown.",
            },
            new ComponentParameter()
            {
                Name = "Items",
                Type = "List<BitDropDownItem>",
                DefaultValue = "",
                Description = "A list of items to display in the dropdown.",
            },
            new ComponentParameter()
            {
                Name = "ItemTemplate",
                Type = "RenderFragment<BitDropDownItem>",
                DefaultValue = "",
                Description = "Optional custom template for drop-down item.",
            },
            new ComponentParameter()
            {
                Name = "Label",
                Type = "string",
                DefaultValue = "",
                Description = "The title to show when the mouse is placed on the drop down.",
            },
            new ComponentParameter()
            {
                Name = "LabelFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Optional custom template for label.",
            },
            new ComponentParameter()
            {
                Name = "MultiSelectDelimiter",
                Type = "string",
                DefaultValue = "",
                Description = "When multiple items are selected, this still will be used to separate values in the dropdown title.",
            },
            new ComponentParameter()
            {
                Name = "NotifyOnReselect",
                Type = "bool",
                DefaultValue = "false",
                Description = "Optional preference to have OnSelectItem still be called when an already selected item is clicked in single select mode.",
            },
            new ComponentParameter()
            {
                Name = "OnClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the action button clicked.",
            },
            new ComponentParameter()
            {
                Name = "OnSelectItem",
                Type = "EventCallback<BitDropDownItem> ",
                DefaultValue = "",
                Description = "Callback for when an item is selected.",
            },
            new ComponentParameter()
            {
                Name = "Placeholder",
                Type = "string",
                DefaultValue = "",
                Description = "Input placeholder Text, Displayed until an option is selected.",
            },
            new ComponentParameter()
            {
                Name = "PlaceholderTemplate",
                Type = "RenderFragment<BitDropDown>",
                DefaultValue = "",
                Description = "Optional custom template for placeholder Text.",
            },
            new ComponentParameter()
            {
                Name = "SelectedMultipleKeys",
                Type = "List<string>",
                DefaultValue = "",
                Description = "Keys of the selected items for multiSelect scenarios. If you provide this, you must maintain selection state by observing onChange events and passing a new value in when changed",
            },
            new ComponentParameter()
            {
                Name = "SelectedMultipleKeysChanged",
                Type = "EventCallback<List<string>>",
                DefaultValue = "",
                Description = "Callback for when the selectedMultipleKeys changed.",
            },
            new ComponentParameter()
            {
                Name = "TextTemplate",
                Type = "RenderFragment<BitDropDown>",
                DefaultValue = "",
                Description = "Optional custom template for selected option displayed in after selection.",
            },
        };

        private static string getDropDownItemsSampleCode =
              $"private List<dropdownitem> GetDropdownItems (){Environment.NewLine}" +
              $"{{ {Environment.NewLine}" +
              $"List<dropdownitem> items = new();{Environment.NewLine}" +
              $"items.Add(new DropDownItem(){Environment.NewLine}" +
              $"{{ {Environment.NewLine}" +
              $"ItemType =  DropDownItemType.Header,{Environment.NewLine}" +
              $"Text = 'Fruits'{Environment.NewLine}" +
              $"}}); {Environment.NewLine}" +
              $"items.Add(new DropDownItem(){Environment.NewLine}" +
              $"{{ {Environment.NewLine}" +
              $"ItemType = DropDownItemType.Normal,{Environment.NewLine}" +
              $"Text = 'Apple',{Environment.NewLine}" +
              $"Value = 'f-app'{Environment.NewLine}" +
              $"}});{Environment.NewLine}" +
              $"items.Add(new DropDownItem(){Environment.NewLine}" +
              $"{{ {Environment.NewLine}" +
              $"ItemType = DropDownItemType.Normal,{Environment.NewLine}" +
              $"Text = 'Orange',{Environment.NewLine}" +
              $"Value = 'f-ora',{Environment.NewLine}" +
              $"IsEnabled = 'false'{Environment.NewLine}" +
              $"}}); {Environment.NewLine}" +
              $"items.Add(new DropDownItem(){Environment.NewLine}" +
              $"{{ {Environment.NewLine}" +
              $"ItemType = DropDownItemType.Normal,{Environment.NewLine}" +
              $"Text = 'Banana',{Environment.NewLine}" +
              $"Value = 'f-ban'{Environment.NewLine}" +
              $"}}); {Environment.NewLine}" +
              $"items.Add(new DropDownItem(){Environment.NewLine}" +
              $"{{ {Environment.NewLine}" +
              $"ItemType = DropDownItemType.Divider,{Environment.NewLine}" +
              $"}}); {Environment.NewLine}" +
              $"items.Add(new DropDownItem(){Environment.NewLine}" +
              $"{{ {Environment.NewLine}" +
              $"ItemType = DropDownItemType.Header,{Environment.NewLine}" +
              $"Text = 'Vegetables'{Environment.NewLine}" +
              $"}}); {Environment.NewLine}" +
              $"items.Add(new DropDownItem(){Environment.NewLine}" +
              $"ItemType = DropDownItemType.Normal,{Environment.NewLine}" +
              $"Text = 'Broccoli',{Environment.NewLine}" +
              $"Value = 'v-bro'{Environment.NewLine}" +
              $"}}); {Environment.NewLine}" +
              $"}} {Environment.NewLine}" +
              $"}}";

        private readonly string dropDownSampleCode = $"<BitDropDown Label='Basic Uncontrolled'{Environment.NewLine}" +
              $" Items='GetDropdownItems()'{Environment.NewLine}" +
              $"Placeholder='Select an option'{Environment.NewLine}" +
              $"Style='width: 290px; margin: 20px 0 20px 0'>{Environment.NewLine}" +
              $"</BitDropDown>{Environment.NewLine}" +
              $"<BitDropDown Label='Disabled with defaultSelectedKey'{Environment.NewLine}" +
              $"Items='GetDropdownItems()'{Environment.NewLine}" +
              $"Placeholder='Select an option'>{Environment.NewLine}" +
              $"IsEnabled='false'{Environment.NewLine}" +
              $"DefaultSelectedKey='Broccoli'{Environment.NewLine}" +
              $"Style='width: 290px; margin-bottom: 20px;'>{Environment.NewLine}" +
              $"</BitDropDown>{Environment.NewLine}" +
              $"<BitDropDown Label='Multi-select uncontrolled'{Environment.NewLine}" +
              $"Items='GetDropdownItems()'{Environment.NewLine}" +
              $"Placeholder='Select options'>{Environment.NewLine}" +
              $"IsMultiSelect='true'{Environment.NewLine}" +
              $"Style='width: 290px; margin: 20px 0 20px 0'>{Environment.NewLine}" +
              $"</BitDropDown>{Environment.NewLine}" +
              $"@code {{ {Environment.NewLine}" +
              getDropDownItemsSampleCode +
              $"}}";

        private readonly string singleSelectSampleCode = $"<BitDropDown Label='Single-select Controlled'{Environment.NewLine}" +
            $" Items='GetDropdownItems()'{Environment.NewLine}" +
            $"Placeholder='Select an option'{Environment.NewLine}" +
            $"@bind-SelectedKey='ControlledSelectedKey'{Environment.NewLine}" +
            $"Style='width: 290px; margin: 20px 0 20px 0'>{Environment.NewLine}" +
            $"</BitDropDown>{Environment.NewLine}" +
            $"@code {{ {Environment.NewLine}" +
            $"private string ControlledSelectedKey = 'Apple';{Environment.NewLine}" +
            getDropDownItemsSampleCode +
            $"}}";

        private readonly string multiSelectSampleCode = $"<BitDropDown Label='Multi-select Controlled'{Environment.NewLine}" +
           $" Items='GetDropdownItems()'{Environment.NewLine}" +
           $"Placeholder='Select an option'{Environment.NewLine}" +
           $"@bind-SelectedMultipleKeys='ControlledSelectedMultipleKeys'{Environment.NewLine}" +
           $"IsMultiSelect='true'{Environment.NewLine}" +
           $"Style='width: 290px; margin: 20px 0 20px 0'>{Environment.NewLine}" +
           $"</BitDropDown>{Environment.NewLine}" +
           $"@code {{ {Environment.NewLine}" +
           $"private List<string> ControlledSelectedMultipleKeys = new List<string>() {{ 'Apple','Banana', 'Grape' }};{Environment.NewLine}" +
           getDropDownItemsSampleCode +
           $"}}";

        private readonly string customizedSampleCode = $"<BitDropDown Label='Custom Controlled'{Environment.NewLine}" +
          $"Items='GetCustomDropdownItems()'{Environment.NewLine}" +
          $"Placeholder='Select an option'{Environment.NewLine}" +
          $"AriaLabel='Custom dropdown'{Environment.NewLine}" +
          $"Style='width: 290px; margin: 20px 0 20px 0'>{Environment.NewLine}" +
          $"<TextTemplate>{Environment.NewLine}" +
          $"<div>{Environment.NewLine}" +
          $"<i class='bit-icon bit-icon--@((context.Items.Find(i => i.Value == context.SelectedKey).Data as DropDownItemData).IconName)'{Environment.NewLine}" +
          $"aria-hidden='true'{Environment.NewLine}" +
          $"title='@((context.Items.Find(i => i.Value == context.SelectedKey).Data as DropDownItemData).IconName)'></i>{Environment.NewLine}" +
          $"<span>@context.Items.Find(i => i.Value == context.SelectedKey).Text</span>{Environment.NewLine}" +
          $"</div>{Environment.NewLine}" +
          $"</TextTemplate>{Environment.NewLine}" +
          $"<PlaceholderTemplate>{Environment.NewLine}" +
          $"<div>{Environment.NewLine}" +
          $"<i class='bit-icon bit-icon--MessageFill' aria-hidden='true'></i>{Environment.NewLine}" +
          $"<span>@context.Placeholder</span>{Environment.NewLine}" +
          $"</div>{Environment.NewLine}" +
          $"</PlaceholderTemplate>{Environment.NewLine}" +
          $"<CaretDownFragment>{Environment.NewLine}" +
          $"<ItemTemplate>{Environment.NewLine}" +
          $"<div>{Environment.NewLine}" +
          $"<i class='bit-icon bit-icon--@((context.Data as DropDownItemData).IconName)'{Environment.NewLine}" +
          $"aria-hidden='true'{Environment.NewLine}" +
          $"title='@((context.Data as DropDownItemData).IconName)'></i>{Environment.NewLine}" +
          $"<span>@context.Text</span>{Environment.NewLine}" +
          $"</div>{Environment.NewLine}" +
          $"</ItemTemplate>{Environment.NewLine}" +
          $"</BitDropDown>{Environment.NewLine}" +
          $"<BitDropDown Items='GetCustomDropdownItems()'{Environment.NewLine}" +
          $"Placeholder='Select an option'{Environment.NewLine}" +
          $"Label='Custom label'{Environment.NewLine}" +
          $"AriaLabel='Custom dropdown label'{Environment.NewLine}" +
          $"Style='width:290px'>{Environment.NewLine}" +
          $"<LabelFragment>{Environment.NewLine}" +
          $"<label>Custom label</label>{Environment.NewLine}" +
          $"<button type='button' title='Info' aria-label='Info' class='custom-drp-lbl-ic'>{Environment.NewLine}" +
          $"<i class='bit-icon bit-icon--Info'></i>{Environment.NewLine}" +
          $"</button>{Environment.NewLine}" +
          $"</LabelFragment>{Environment.NewLine}" +
          $"</BitDropDown>{Environment.NewLine}" +
          $"@code {{ {Environment.NewLine}" +
          $"private List<dropdownitem> GetCustomDropdownItems(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"List<dropdownitem> items = new();{Environment.NewLine}" +
          $"items.Add(new DropDownItem(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"ItemType = DropDownItemType.Header,{Environment.NewLine}" +
          $"Text = 'Options',{Environment.NewLine}" +
          $"Value = 'Header'{Environment.NewLine}" +
          $"}});{Environment.NewLine}" +
          $"items.Add(new DropDownItem(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"ItemType = DropDownItemType.Normal,{Environment.NewLine}" +
          $"Text = 'Options a',{Environment.NewLine}" +
          $"Value = 'A'{Environment.NewLine}" +
          $"Data = new DropDownItemData(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"IconName= 'Memo'{Environment.NewLine}" +
          $"}}); {Environment.NewLine}" +
          $"items.Add(new DropDownItem(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"ItemType = DropDownItemType.Normal,{Environment.NewLine}" +
          $"Text = 'Options b',{Environment.NewLine}" +
          $"Value = 'B'{Environment.NewLine}" +
          $"Data = new DropDownItemData(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"IconName= 'Print'{Environment.NewLine}" +
          $"}}); {Environment.NewLine}" +
          $"items.Add(new DropDownItem(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"ItemType = DropDownItemType.Normal,{Environment.NewLine}" +
          $"Text = 'Options c',{Environment.NewLine}" +
          $"Value = 'C'{Environment.NewLine}" +
          $"Data = new DropDownItemData(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"IconName= 'ShoppingCart'{Environment.NewLine}" +
          $"}}); {Environment.NewLine}" +
          $"items.Add(new DropDownItem(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"ItemType = DropDownItemType.Normal,{Environment.NewLine}" +
          $"Text = 'Options d',{Environment.NewLine}" +
          $"Value = 'D'{Environment.NewLine}" +
          $"Data = new DropDownItemData(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"IconName= 'Train'{Environment.NewLine}" +
          $"}}); {Environment.NewLine}" +
          $"items.Add(new DropDownItem(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"ItemType = DropDownItemType.Normal,{Environment.NewLine}" +
          $"Text = 'Options e',{Environment.NewLine}" +
          $"Value = 'E'{Environment.NewLine}" +
          $"Data = new DropDownItemData(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"IconName= 'Repair'{Environment.NewLine}" +
          $"}}); {Environment.NewLine}" +
          $"items.Add(new DropDownItem(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"ItemType = DropDownItemType.Divider,{Environment.NewLine}" +
          $"}}); {Environment.NewLine}" +
          $"items.Add(new DropDownItem(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"ItemType = DropDownItemType.Header,{Environment.NewLine}" +
          $"Text = 'More options',{Environment.NewLine}" +
          $"Value = 'Header2'{Environment.NewLine}" +
          $"}}); {Environment.NewLine}" +
          $"Data = new DropDownItemData(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"IconName= 'Repair'{Environment.NewLine}" +
          $"}}); {Environment.NewLine}" +
          $"items.Add(new DropDownItem(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"ItemType = DropDownItemType.Normal,{Environment.NewLine}" +
          $"Text = 'Options f',{Environment.NewLine}" +
          $"Value = 'F'{Environment.NewLine}" +
          $"Data = new DropDownItemData(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"IconName= 'Running'{Environment.NewLine}" +
          $"}}); {Environment.NewLine}" +
          $"items.Add(new DropDownItem(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"ItemType = DropDownItemType.Normal,{Environment.NewLine}" +
          $"Text = 'Options g',{Environment.NewLine}" +
          $"Value = 'G'{Environment.NewLine}" +
          $"Data = new DropDownItemData(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"IconName= 'EmoijNeutral'{Environment.NewLine}" +
          $"}}); {Environment.NewLine}" +
          $"ItemType = DropDownItemType.Normal,{Environment.NewLine}" +
          $"Text = 'Options h',{Environment.NewLine}" +
          $"Value = 'H'{Environment.NewLine}" +
          $"Data = new DropDownItemData(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"IconName= 'ChatInviteFriend'{Environment.NewLine}" +
          $"}}); {Environment.NewLine}" +
          $"ItemType = DropDownItemType.Normal,{Environment.NewLine}" +
          $"Text = 'Options i',{Environment.NewLine}" +
          $"Value = 'I'{Environment.NewLine}" +
          $"Data = new DropDownItemData(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"IconName= 'SecurityGroup'{Environment.NewLine}" +
          $"}}); {Environment.NewLine}" +
          $"ItemType = DropDownItemType.Normal,{Environment.NewLine}" +
          $"Text = 'Options j',{Environment.NewLine}" +
          $"Value = 'J'{Environment.NewLine}" +
          $"Data = new DropDownItemData(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"IconName= 'AddGroup'{Environment.NewLine}" +
          $"}}); {Environment.NewLine}" +
          $"return items; {Environment.NewLine}" +
          $"}} {Environment.NewLine}" +
          $"}}";
    }
}
