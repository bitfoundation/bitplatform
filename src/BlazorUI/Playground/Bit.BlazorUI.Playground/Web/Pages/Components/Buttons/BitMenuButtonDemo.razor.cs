﻿using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Buttons;

public partial class BitMenuButtonDemo
{
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
            key = "A",
            IconName = BitIconName.Emoji
        },
        new BitMenuButtonItem()
        {
            Text = "Item B",
            key = "B",
            IconName = BitIconName.Emoji
        },
        new BitMenuButtonItem()
        {
            Text = "Item C",
            key = "C",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitMenuButtonItem> disabledItemMenuButton = new()
    {
        new BitMenuButtonItem()
        {
            Text = "Item A",
            key = "A",
            IconName = BitIconName.Emoji
        },
        new BitMenuButtonItem()
        {
            Text = "Item B",
            key = "B",
            IconName = BitIconName.Emoji,
            IsEnabled = false
        },
        new BitMenuButtonItem()
        {
            Text = "Item C",
            key = "C",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitMenuButtonItem> itemTemplateMenuButton = new()
    {
        new BitMenuButtonItem()
        {
            Text = "Add",
            key = "add-key",
            IconName = BitIconName.Add
        },
        new BitMenuButtonItem()
        {
            Text = "Edit",
            key = "edit-key",
            IconName = BitIconName.Edit
        },
        new BitMenuButtonItem()
        {
            Text = "Delete",
            key = "delete-key",
            IconName = BitIconName.Delete
        }
    };

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

    private readonly string example1HTMLCode = @"
<BitMenuButton Text=""Standard""
                ButtonStyle=""BitButtonStyle.Standard""
                Items=""basicMenuButton""
                OnItemClick=""(item) => example1SelectedItem = item.key"" />

<BitMenuButton Text=""Primary""
                ButtonStyle=""BitButtonStyle.Primary""
                Items=""basicMenuButton""
                OnItemClick=""(item) => example1SelectedItem = item.key"" />

<BitMenuButton Text=""Disabled""
                IsEnabled=""false"" />

<BitMenuButton Text=""Item Disabled""
                Items=""disabledItemMenuButton""
                OnItemClick=""(item) => example1SelectedItem = item.key"" />

<div class=""selected-item"">Selected Item: @example1SelectedItem</div>
";
    private readonly string example2HTMLCode = @"
<BitMenuButton Text=""Standard Button""
                IconName=""BitIconName.Add""
                ButtonStyle=""BitButtonStyle.Standard""
                Items=""basicMenuButton""
                OnItemClick=""(item) => example2SelectedItem = item.key"" />

<BitMenuButton Text=""Primary Button""
                IconName=""BitIconName.Edit""
                ButtonStyle=""BitButtonStyle.Primary""
                Items=""basicMenuButton""
                OnItemClick=""(item) => example2SelectedItem = item.key"" />

<div class=""selected-item"">Selected Item: @example2SelectedItem</div>
";
    private readonly string example3HTMLCode = @"
<style>
    .custom-menu-btn {
        height: rem(40px);
        width: rem(166px);
        background-color: #515151;
        border-color: black;

        &:hover {
            background-color: #515151;
        }
    }
</style>

<BitMenuButton Text=""Styled Button""
                Items=""basicMenuButton""
                OnItemClick=""(item) => example3SelectedItem = item.key""
                Style=""width: 200px; height: 40px; background-color: #8A8886; border-color: black;"" />

<BitMenuButton Text=""Classed Button""
                Items=""basicMenuButton""
                OnItemClick=""(item) => example3SelectedItem = item.key""
                Class=""custom-menu-btn"" />

<div class=""selected-item"">Selected Item: @example3SelectedItem</div>
";
    private readonly string example4HTMLCode = @"
<div>
    <BitMenuButton Text=""Visible Button""
                    Items=""basicMenuButton""
                    OnItemClick=""(item) => example4SelectedItem = item.key""
                    Visibility=""BitComponentVisibility.Visible"" />
</div>

<div>
    Hidden Button: [<BitMenuButton Text=""Styled Button""
                                    IconName=""BitIconName.Add""
                                    Items=""basicMenuButton""
                                    Visibility=""BitComponentVisibility.Hidden"" />]
</div>

<div>
    Collapsed Button: [<BitMenuButton Text=""Styled Button""
                                        IconName=""BitIconName.Add""
                                        Items=""basicMenuButton""
                                        Visibility=""BitComponentVisibility.Collapsed"" />]
</div>

<div class=""selected-item"">Selected Item: @example4SelectedItem</div>
";
    private readonly string example5HTMLCode = @"
<BitMenuButton Items=""basicMenuButton""
                OnItemClick=""(item) => example5SelectedItem = item.key""
                ButtonStyle=""BitButtonStyle.Standard"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
</BitMenuButton>

<BitMenuButton Items=""basicMenuButton""
                OnItemClick=""(item) => example5SelectedItem = item.key""
                ButtonStyle=""BitButtonStyle.Primary"">
    <HeaderTemplate>
        <BitIcon IconName=""BitIconName.Warning"" />
        <div style=""font-weight: 600; color: white;"">
            Custom Header!
        </div>
        <BitIcon IconName=""BitIconName.Warning"" />
    </HeaderTemplate>
</BitMenuButton>

<div class=""selected-item"">Selected Item: @example5SelectedItem</div>
";
    private readonly string example6HTMLCode = @"
<BitMenuButton Text=""Standard Button""
                IconName=""BitIconName.Edit""
                Items=""itemTemplateMenuButton""
                OnItemClick=""(item) => example6SelectedItem = item.key""
                ItemTemplate=""itemTemplate""
                ButtonStyle=""BitButtonStyle.Standard"" />

<BitMenuButton Text=""Primary Button""
                IconName=""BitIconName.Edit""
                Items=""itemTemplateMenuButton""
                OnItemClick=""(item) => example6SelectedItem = item.key""
                ItemTemplate=""itemTemplate""
                ButtonStyle=""BitButtonStyle.Primary"" />

<div class=""selected-item"">Selected Item: @example6SelectedItem</div>
";

    private readonly string example1CSharpCode = @"
private string example1SelectedItem;

private List<BitMenuButtonItem> basicMenuButton = new()
{
    new BitMenuButtonItem()
    {
        Text = ""Item A"",
        key = ""A"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item B"",
        key = ""B"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item C"",
        key = ""C"",
        IconName = BitIconName.Emoji2
    }
};

private List<BitMenuButtonItem> disabledItemMenuButton = new()
{
    new BitMenuButtonItem()
    {
        Text = ""Item A"",
        key = ""A"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item B"",
        key = ""B"",
        IconName = BitIconName.Emoji,
        IsEnabled = false
    },
    new BitMenuButtonItem()
    {
        Text = ""Item C"",
        key = ""C"",
        IconName = BitIconName.Emoji2
    }
};
";
    private readonly string example2CSharpCode = @"
private string example2SelectedItem;

private List<BitMenuButtonItem> basicMenuButton = new()
{
    new BitMenuButtonItem()
    {
        Text = ""Item A"",
        key = ""A"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item B"",
        key = ""B"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item C"",
        key = ""C"",
        IconName = BitIconName.Emoji2
    }
};
";
    private readonly string example3CSharpCode = @"
private string example3SelectedItem;

private List<BitMenuButtonItem> basicMenuButton = new()
{
    new BitMenuButtonItem()
    {
        Text = ""Item A"",
        key = ""A"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item B"",
        key = ""B"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item C"",
        key = ""C"",
        IconName = BitIconName.Emoji2
    }
};
";
    private readonly string example4CSharpCode = @"
private string example4SelectedItem;

private List<BitMenuButtonItem> basicMenuButton = new()
{
    new BitMenuButtonItem()
    {
        Text = ""Item A"",
        key = ""A"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item B"",
        key = ""B"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item C"",
        key = ""C"",
        IconName = BitIconName.Emoji2
    }
};
";
    private readonly string example5CSharpCode = @"
private string example5SelectedItem;

private List<BitMenuButtonItem> basicMenuButton = new()
{
    new BitMenuButtonItem()
    {
        Text = ""Item A"",
        key = ""A"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item B"",
        key = ""B"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item C"",
        key = ""C"",
        IconName = BitIconName.Emoji2
    }
};
";
    private readonly string example6CSharpCode = @"
private string example6SelectedItem;

private List<BitMenuButtonItem> itemTemplateMenuButton = new()
{
    new BitMenuButtonItem()
    {
        Text = ""Add"",
        key = ""add-key"",
        IconName = BitIconName.Add
    },
    new BitMenuButtonItem()
    {
        Text = ""Edit"",
        key = ""edit-key"",
        IconName = BitIconName.Edit
    },
    new BitMenuButtonItem()
    {
        Text = ""Delete"",
        key = ""delete-key"",
        IconName = BitIconName.Delete
    }
};

private RenderFragment<BitMenuButtonItem> itemTemplate = (item) =>
(
    @<div class=""item-template-box"">
        <span style=""color: @(item.key == ""add-key"" ? ""green"" : item.key == ""edit-key"" ? ""yellow"" : ""red"");"">
            @item.Text (@item.key)
        </span>
    </div>
);
";
}
