using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Inputs
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
                Name = "caretDownFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Optional custom template for chevron icon.",
            },
            new ComponentParameter()
            {
                Name = "defaultSelectedKey",
                Type = "string",
                DefaultValue = "",
                Description = "Key that will be initially used to set selected item.",
            },
            new ComponentParameter()
            {
                Name = "defaultSelectedMultipleKeys",
                Type = "List<string>",
                DefaultValue = "",
                Description = "Keys that will be initially used to set selected items for multiSelect scenarios.",
            },
            new ComponentParameter()
            {
                Name = "isMultiSelect",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether multiple items are allowed to be selected.",
            },
            new ComponentParameter()
            {
                Name = "isOpen",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not this dropdown is open.",
            },
            new ComponentParameter()
            {
                Name = "isRequired",
                Type = "bool",
                DefaultValue = "false",
                Description = "Requires the end user to select an item in the dropdown.",
            },
            new ComponentParameter()
            {
                Name = "items",
                Type = "List<BitDropDownItem>",
                DefaultValue = "",
                Description = "A list of items to display in the dropdown.",
            },
            new ComponentParameter()
            {
                Name = "itemTemplate",
                Type = "RenderFragment<BitDropDownItem>",
                DefaultValue = "",
                Description = "Optional custom template for drop-down item.",
            },
            new ComponentParameter()
            {
                Name = "label",
                Type = "string",
                DefaultValue = "",
                Description = "The title to show when the mouse is placed on the drop down.",
            },
            new ComponentParameter()
            {
                Name = "labelFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Optional custom template for label.",
            },
            new ComponentParameter()
            {
                Name = "multiSelectDelimiter",
                Type = "string",
                DefaultValue = "",
                Description = "When multiple items are selected, this still will be used to separate values in the dropdown title.",
            },
            new ComponentParameter()
            {
                Name = "notifyOnReselect",
                Type = "bool",
                DefaultValue = "false",
                Description = "Optional preference to have OnSelectItem still be called when an already selected item is clicked in single select mode.",
            },
            new ComponentParameter()
            {
                Name = "onClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the action button clicked.",
            },
            new ComponentParameter()
            {
                Name = "onSelectItem",
                Type = "EventCallback<BitDropDownItem> ",
                DefaultValue = "",
                Description = "Callback for when an item is selected.",
            },
            new ComponentParameter()
            {
                Name = "placeholder",
                Type = "string",
                DefaultValue = "",
                Description = "Input placeholder Text, Displayed until an option is selected.",
            },
            new ComponentParameter()
            {
                Name = "placeholderTemplate",
                Type = "RenderFragment<BitDropDown>",
                DefaultValue = "",
                Description = "Optional custom template for placeholder Text.",
            },
            new ComponentParameter()
            {
                Name = "selectedMultipleKeys",
                Type = "List<string>",
                DefaultValue = "",
                Description = "Keys of the selected items for multiSelect scenarios. If you provide this, you must maintain selection state by observing onChange events and passing a new value in when changed",
            },
            new ComponentParameter()
            {
                Name = "selectedMultipleKeysChanged",
                Type = "EventCallback<List<string>>",
                DefaultValue = "",
                Description = "Callback for when the selectedMultipleKeys changed.",
            },
            new ComponentParameter()
            {
                Name = "textTemplate",
                Type = "RenderFragment<BitDropDown>",
                DefaultValue = "",
                Description = "Optional custom template for selected option displayed in after selection.",
            },
        };
    }
}
