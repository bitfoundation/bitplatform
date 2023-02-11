using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Buttons;

public partial class BitMenuButtonDemo
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
            LinkType = LinkType.Link,
            Href = "#button-style-enum",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of button, Possible values: Primary | Standard."
        },
        new ComponentParameter
        {
            Name = "ButtonType",
            Type = "BitButtonType",
            LinkType = LinkType.Link,
            Href = "#button-type-enum",
            DefaultValue = "BitButtonType.Button",
            Description = "The type of the button."
        },
        new ComponentParameter()
        {
            Name = "HeaderTemplate",
            Type = "BitIconName?",
            Description = "The icon to show inside the header of MenuButton.",
        },
        new ComponentParameter()
        {
            Name = "IconName",
            Type = "RenderFragment<BitMenuButtonItem>?",
            Description = "The content inside the item can be customized.",
        },
        new ComponentParameter
        {
            Name = "Items",
            Type = "List<BitMenuButtonItem>",
            LinkType = LinkType.Link,
            Href = "#menu-button-items",
            DefaultValue = "new List<BitMenuButtonItem>()",
            Description = "List of Item, each of which can be a Button with different action in the MenuButton."
        },
        new ComponentParameter()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitMenuButtonItem>?",
            Description = "The content inside the item can be customized.",
        },
        new ComponentParameter
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "The callback is called when the MenuButton header is clicked."
        },
        new ComponentParameter
        {
            Name = "OnItemClick",
            Type = "EventCallback<BitMenuButtonItem>",
            Description = "OnClick of each item returns that item with its property."
        },
        new ComponentParameter
        {
            Name = "Text",
            Type = "string?",
            Description = "The text to show inside the header of MenuButton."
        },
    };
    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "menu-button-items",
            Title = "BitMenuButtonItem",
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
                   Description = "A unique value to use as a Key of the item.",
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
        new EnumParameter()
        {
            Id = "component-visibility-enum",
            Title = "BitComponentVisibility Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Visible",
                    Description="Show content of the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Hidden",
                    Description="Hide content of the component,though the space it takes on the page remains.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Collapsed",
                    Description="Hide content of the component,though the space it takes on the page gone.",
                    Value="2",
                }
            }
        }
    };


    private string example1SelectedItem;
    private string example2SelectedItem;
    private string example3SelectedItem;
    private string example4SelectedItem;
    private string example5SelectedItem;
    private string example6SelectedItem;

    private List<BitMenuButtonItem> basicMenuButton = new()
    {
        new BitMenuButtonItem()
        {
            Text = "Item A",
            Key = "A",
            IconName = BitIconName.Emoji
        },
        new BitMenuButtonItem()
        {
            Text = "Item B",
            Key = "B",
            IconName = BitIconName.Emoji
        },
        new BitMenuButtonItem()
        {
            Text = "Item C",
            Key = "C",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitMenuButtonItem> disabledItemMenuButton = new()
    {
        new BitMenuButtonItem()
        {
            Text = "Item A",
            Key = "A",
            IconName = BitIconName.Emoji
        },
        new BitMenuButtonItem()
        {
            Text = "Item B",
            Key = "B",
            IconName = BitIconName.Emoji,
            IsEnabled = false
        },
        new BitMenuButtonItem()
        {
            Text = "Item C",
            Key = "C",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitMenuButtonItem> itemTemplateMenuButton = new()
    {
        new BitMenuButtonItem()
        {
            Text = "Add",
            Key = "add-key",
            IconName = BitIconName.Add
        },
        new BitMenuButtonItem()
        {
            Text = "Edit",
            Key = "edit-key",
            IconName = BitIconName.Edit
        },
        new BitMenuButtonItem()
        {
            Text = "Delete",
            Key = "delete-key",
            IconName = BitIconName.Delete
        }
    };

    private List<MenuActionItem> basicMenuButtonWithCustomType = new()
    {
        new MenuActionItem()
        {
            Name = "Item A",
            Id = "A",
            Icon = BitIconName.Emoji
        },
        new MenuActionItem()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji
        },
        new MenuActionItem()
        {
            Name = "Item C",
            Id = "C",
            Icon = BitIconName.Emoji2
        }
    };
    private List<MenuActionItem> disabledItemMenuButtonWithCustomType = new()
    {
        new MenuActionItem()
        {
            Name = "Item A",
            Id = "A",
            Icon = BitIconName.Emoji
        },
        new MenuActionItem()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new MenuActionItem()
        {
            Name = "Item C",
            Id = "C",
            Icon = BitIconName.Emoji2
        }
    };
    private List<MenuActionItem> itemTemplateMenuButtonWithCustomType = new()
    {
        new MenuActionItem()
        {
            Name = "Add",
            Id = "add-key",
            Icon = BitIconName.Add
        },
        new MenuActionItem()
        {
            Name = "Edit",
            Id = "edit-key",
            Icon = BitIconName.Edit
        },
        new MenuActionItem()
        {
            Name = "Delete",
            Id = "delete-key",
            Icon = BitIconName.Delete
        }
    };

    private readonly string example1HTMLCode = @"
";
    private readonly string example1CSharpCode = @"
";

    private readonly string example2HTMLCode = @"
";
    private readonly string example2CSharpCode = @"
";

    private readonly string example3HTMLCode = @"
";
    private readonly string example3CSharpCode = @"
";

    private readonly string example4HTMLCode = @"
";
    private readonly string example4CSharpCode = @"
";

    private readonly string example5HTMLCode = @"
";
    private readonly string example5CSharpCode = @"
";

    private readonly string example6HTMLCode = @"
";
    private readonly string example6CSharpCode = @"
";
}
