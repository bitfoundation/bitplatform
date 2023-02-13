using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Buttons;

public partial class BitSplitButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter
        {
            Name = "AriaDescription",
            Type = "string?",
            Description = "Detailed description of the button for the benefit of screen readers."
        },
        new ComponentParameter
        {
            Name = "AriaHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, add an aria-hidden attribute instructing screen readers to ignore the element."
        },
        new ComponentParameter
        {
            Name = "ButtonStyle",
            Type = "BitButtonStyle",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of button, Possible values: Primary | Standard.",
            LinkType = LinkType.Link,
            Href = "#button-style-enum",
        },
        new ComponentParameter
        {
            Name = "ButtonType",
            Type = "BitButtonType",
            DefaultValue = "BitButtonType.Button",
            Description = "The type of the button.",
            LinkType = LinkType.Link,
            Href = "#button-type-enum",
        },
        new ComponentParameter()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            Description = "The content of the BitSplitButton, that are BitSplitButtonOption components.",
        },
        new ComponentParameter()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitSplitButtonItem>?",
            Description = "The content inside the item can be customized.",
        },
        new ComponentParameter
        {
            Name = "IsSticky",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the current item is going to be change selected item."
        },
        new ComponentParameter
        {
            Name = "Items",
            Type = "List<BitSplitButtonItem>",
            DefaultValue = "new List<BitSplitButtonItem>()",
            Description = "List of Item, each of which can be a Button with different action in the SplitButton.",
            LinkType = LinkType.Link,
            Href = "#split-button-items",
        },
        new ComponentParameter()
        {
            Name = "IsEnabledField",
            Type = "string",
            DefaultValue = "IsEnabled",
            Description = "Whether or not the item is enabled.",
        },
        new ComponentParameter()
        {
            Name = "IsEnabledFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "Whether or not the item is enabled.",
        },
        new ComponentParameter()
        {
            Name = "IconNameField",
            Type = "string",
            DefaultValue = "IconName",
            Description = "Name of an icon to render next to the item text.",
        },
        new ComponentParameter()
        {
            Name = "IconNameFieldSelector",
            Type = "Expression<Func<TItem, BitIconName>>?",
            Description = "Name of an icon to render next to the item text.",
        },
        new ComponentParameter
        {
            Name = "OnClick",
            Type = "EventCallback<BitSplitButtonItem>",
            Description = "The callback is called when the button or button item is clicked."
        },
        new ComponentParameter()
        {
            Name = "TextField",
            Type = "string",
            DefaultValue = "Text",
            Description = "Name of an icon to render next to the item text.",
        },
        new ComponentParameter()
        {
            Name = "TextFieldSelector",
            Type = "Expression<Func<TItem, string>>?",
            Description = "Name of an icon to render next to the item text.",
        },
        new ComponentParameter()
        {
            Name = "KeyField",
            Type = "string",
            DefaultValue = "Key",
            Description = "A unique value to use as a key of the item.",
        },
        new ComponentParameter()
        {
            Name = "KeyFieldSelector",
            Type = "Expression<Func<TItem, string>>?",
            Description = "A unique value to use as a key of the item.",
        },
    };
    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "split-button-items",
            Title = "BitSplitButtonItem",
            Description = "BitSplitButtonItem is default type for item.",
            Parameters = new List<ComponentParameter>()
            {
               new ComponentParameter()
               {
                   Name = "IconName",
                   Type = "BitIconName?",
                   Description = "Name of an icon to render next to the item text.",
               },
               new ComponentParameter()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the item is enabled.",
               },
               new ComponentParameter()
               {
                   Name = "Key",
                   Type = "string?",
                   Description = "A unique value to use as a key of the item.",
               },
               new ComponentParameter()
               {
                   Name = "Text",
                   Type = "string?",
                   Description = "Text to render in the item.",
               }
            }
        },
        new ComponentSubParameter()
        {
            Id = "split-button-options",
            Title = "BitSplitButtonOption",
            Description = "BitSplitButtonOption is a child component for BitSplitButton.",
            Parameters = new List<ComponentParameter>()
            {
               new ComponentParameter()
               {
                   Name = "IconName",
                   Type = "BitIconName?",
                   Description = "Name of an icon to render next to the item text.",
               },
               new ComponentParameter()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the item is enabled.",
               },
               new ComponentParameter()
               {
                   Name = "Key",
                   Type = "string?",
                   Description = "A unique value to use as a key of the item.",
               },
               new ComponentParameter()
               {
                   Name = "Text",
                   Type = "string?",
                   Description = "Text to render in the item.",
               }
            }
        }
    };
    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "button-style-enum",
            Title = "BitButtonStyle Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Primary",
                    Description="The button with white text on a blue background.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Standard",
                    Description="The button with black text on a white background.",
                    Value="1",
                }
            }
        },
        new EnumParameter()
        {
            Id = "button-type-enum",
            Title = "BitButtonType Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Button",
                    Description="The button is a clickable button.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Submit",
                    Description="The button is a submit button (submits form-data).",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Reset",
                    Description="The button is a reset button (resets the form-data to its initial values).",
                    Value="2",
                }
            }
        },
    };


    private string Example1ClickedItem;
    private string Example2ClickedItem;
    private string Example3ClickedItem;
    private string Example4ClickedItem;

    private List<BitSplitButtonItem> example1Items = new()
    {
        new BitSplitButtonItem()
        {
            Text = "Item A",
            Key = "A"
        },
        new BitSplitButtonItem()
        {
            Text = "Item B",
            Key = "B",
            IconName = BitIconName.Emoji
        },
        new BitSplitButtonItem()
        {
            Text = "Item C",
            Key = "C",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitSplitButtonItem> example2Items = new()
    {
        new BitSplitButtonItem()
        {
            Text = "Add",
            Key = "add-key",
            IconName = BitIconName.Add
        },
        new BitSplitButtonItem()
        {
            Text = "Edit",
            Key = "edit-key",
            IconName = BitIconName.Edit
        },
        new BitSplitButtonItem()
        {
            Text = "Delete",
            Key = "delete-key",
            IconName = BitIconName.Delete
        }
    };
    private List<BitSplitButtonItem> example3Items = new()
    {
        new BitSplitButtonItem()
        {
            Text = "Item A",
            Key = "A",
            IconName = BitIconName.Emoji,
            IsEnabled = false
        },
        new BitSplitButtonItem()
        {
            Text = "Item B",
            Key = "B",
            IconName = BitIconName.Emoji2
        },
        new BitSplitButtonItem()
        {
            Text = "Item C",
            Key = "C",
            IconName = BitIconName.Emoji,
            IsEnabled = false
        },
        new BitSplitButtonItem()
        {
            Text = "Item D",
            Key = "D",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitSplitButtonItem> example4Items = new()
    {
        new BitSplitButtonItem()
        {
            Text = "Add",
            Key = "add-key"
        },
        new BitSplitButtonItem()
        {
            Text = "Edit",
            Key = "edit-key"
        },
        new BitSplitButtonItem()
        {
            Text = "Delete",
            Key = "delete-key"
        }
    };

    private List<SplitActionItem> example1CustomItems = new()
    {
        new SplitActionItem()
        {
            Name = "Item A",
            Id = "A"
        },
        new SplitActionItem()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji
        },
        new SplitActionItem()
        {
            Name = "Item C",
            Id = "C",
            Icon = BitIconName.Emoji2
        }
    };
    private List<SplitActionItem> example2CustomItems = new()
    {
        new SplitActionItem()
        {
            Name = "Add",
            Id = "add-key",
            Icon = BitIconName.Add
        },
        new SplitActionItem()
        {
            Name = "Edit",
            Id = "edit-key",
            Icon = BitIconName.Edit
        },
        new SplitActionItem()
        {
            Name = "Delete",
            Id = "delete-key",
            Icon = BitIconName.Delete
        }
    };
    private List<SplitActionItem> example3CustomItems = new()
    {
        new SplitActionItem()
        {
            Name = "Item A",
            Id = "A",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new SplitActionItem()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji2
        },
        new SplitActionItem()
        {
            Name = "Item C",
            Id = "C",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new SplitActionItem()
        {
            Name = "Item D",
            Id = "D",
            Icon = BitIconName.Emoji2
        }
    };
    private List<SplitActionItem> example4CustomItems = new()
    {
        new SplitActionItem()
        {
            Name = "Add",
            Id = "add-key"
        },
        new SplitActionItem()
        {
            Name = "Edit",
            Id = "edit-key"
        },
        new SplitActionItem()
        {
            Name = "Delete",
            Id = "delete-key"
        }
    };

    private void OnTabClick()
    {
        Example1ClickedItem = string.Empty; 
        Example2ClickedItem = string.Empty;
        Example3ClickedItem = string.Empty;
        Example4ClickedItem = string.Empty;
    }

    private readonly string example1HTMLCode = @"
";

    private readonly string example2HTMLCode = @"
";

    private readonly string example3HTMLCode = @"
";

    private readonly string example4HTMLCode = @"
";

    private readonly string example1CSharpCode = @"
";

    private readonly string example2CSharpCode = @"
";

    private readonly string example3CSharpCode = @"
";

    private readonly string example4CSharpCode = @"
";

}
