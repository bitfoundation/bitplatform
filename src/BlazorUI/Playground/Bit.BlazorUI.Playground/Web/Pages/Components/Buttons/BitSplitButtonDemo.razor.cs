using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Buttons;

public partial class BitSplitButtonDemo
{
    private string example1SelectedItem;
    private string example2SelectedItem;
    private string example3SelectedItem;

    private List<BitSplitButtonItem> example1Items = new()
    {
        new BitSplitButtonItem()
        {
            Text = "Item A",
            key = "A"
        },
        new BitSplitButtonItem()
        {
            Text = "Item B",
            key = "B",
            IconName = BitIconName.Emoji
        },
        new BitSplitButtonItem()
        {
            Text = "Item C",
            key = "C",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitSplitButtonItem> example2Items = new()
    {
        new BitSplitButtonItem()
        {
            Text = "Item A",
            key = "A"
        },
        new BitSplitButtonItem()
        {
            Text = "Item B",
            key = "B",
            IconName = BitIconName.Emoji
        },
        new BitSplitButtonItem()
        {
            Text = "Item C",
            key = "C",
            IconName = BitIconName.Emoji2,
            IsEnabled = false
        },
        new BitSplitButtonItem()
        {
            Text = "Item D",
            key = "D",
            IconName = BitIconName.Emoji
        }
    };
    private List<BitSplitButtonItem> example3Items = new()
    {
        new BitSplitButtonItem()
        {
            Text = "Add",
            key = "add-key",
            IconName = BitIconName.Add
        },
        new BitSplitButtonItem()
        {
            Text = "Edit",
            key = "edit-key",
            IconName = BitIconName.Edit
        },
        new BitSplitButtonItem()
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
        new ComponentParameter
        {
            Name = "Items",
            Type = "List<BitSplitButtonItem>",
            LinkType = LinkType.Link,
            Href = "#split-button-items",
            DefaultValue = "new List<BitSplitButtonItem>()",
            Description = "List of Item, each of which can be a Button with different action in the SplitButton."
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
            Name = "OnClick",
            Type = "EventCallback<BitSplitButtonItem>",
            Description = "The callback is called when the button or button item is clicked."
        }
    };
    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "split-button-items",
            Title = "BitSplitButtonItem",
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
                   Name = "ItemTemplate",
                   Type = "RenderFragment<BitSplitButtonItem>?",
                   Description = "The content inside the item can be customized.",
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
<BitSplitButton Items=""example1Items"" ButtonStyle=""BitButtonStyle.Standard"" OnClick=""(e) => example1SelectedItem = e.Text"" />
<BitSplitButton Items=""example1Items"" ButtonStyle=""BitButtonStyle.Primary"" OnClick=""(e) => example1SelectedItem = e.Text"" />
<BitSplitButton Items=""example1Items"" IsEnabled=""false"" />
";
    private readonly string example2HTMLCode = @"
<BitSplitButton Items=""example2Items"" ButtonStyle=""BitButtonStyle.Standard"" OnClick=""(e) => examp2e1SelectedItem = e.Text"" />
<BitSplitButton Items=""example2Items"" ButtonStyle=""BitButtonStyle.Primary"" OnClick=""(e) => examp2e1SelectedItem = e.Text"" />
";
    private readonly string example3HTMLCode = @"
<BitSplitButton Items=""example3Items"" ButtonStyle=""BitButtonStyle.Standard"" IsSticky=""true"" OnClick=""(e) => examp3e1SelectedItem = e.Text"" />
<BitSplitButton Items=""example3Items"" ButtonStyle=""BitButtonStyle.Primary"" IsSticky=""true"" OnClick=""(e) => examp3e1SelectedItem = e.Text"" />
";
    private readonly string example4HTMLCode = @"
<BitSplitButton Items=""example4Items"" ButtonStyle=""BitButtonStyle.Standard"" IsSticky=""true"" OnClick=""(e) => examp4e1SelectedItem = e.Text"" />
<BitSplitButton Items=""example4Items"" ButtonStyle=""BitButtonStyle.Primary"" IsSticky=""true"" OnClick=""(e) => examp4e1SelectedItem = e.Text"" />
";

    private readonly string example1CSharpCode = @"
 private string example1SelectedItem;

private List<BitSplitButtonItem> example1Items = new()
{
    new BitSplitButtonItem()
    {
        Text = ""Item A"",
        key = ""A""
    },
    new BitSplitButtonItem()
    {
        Text = ""Item B"",
        key = ""B"",
        IconName = BitIconName.Emoji
    },
    new BitSplitButtonItem()
    {
        Text = ""Item C"",
        key = ""C"",
        IconName = BitIconName.Emoji2
    }
};
";
    private readonly string example2CSharpCode = @"
private string examp2e1SelectedItem;

private List<BitSplitButtonItem> example2Items = new()
{
    new BitSplitButtonItem()
    {
        Text = ""Item A"",
        key = ""A""
    },
    new BitSplitButtonItem()
    {
        Text = ""Item B"",
        key = ""B"",
        IconName = BitIconName.Emoji
    },
    new BitSplitButtonItem()
    {
        Text = ""Item C"",
        key = ""C"",
        IconName = BitIconName.Emoji2,
        IsEnabled = false
    },
    new BitSplitButtonItem()
    {
        Text = ""Item D"",
        key = ""D"",
        IconName = BitIconName.Emoji
    }
};
";
    private readonly string example3CSharpCode = @"
private string examp3e1SelectedItem;

private List<BitSplitButtonItem> example3Items = new()
{
    new BitSplitButtonItem()
    {
        Text = ""Add"",
        key = ""add-key"",
        IconName = BitIconName.Add
    },
    new BitSplitButtonItem()
    {
        Text = ""Edit"",
        key = ""edit-key"",
        IconName = BitIconName.Edit
    },
    new BitSplitButtonItem()
    {
        Text = ""Delete"",
        key = ""delete-key"",
        IconName = BitIconName.Delete
    }
};
";
    private readonly string example4CSharpCode = @"
private string examp4e1SelectedItem;

private List<BitSplitButtonItem> example4Items = new()
{
    new BitSplitButtonItem()
    {
        Text = ""Add"",
        key = ""add-key"",
        ItemTemplate = (item) => 
        (
            @<div class=""item-template-box"">
                <span style=""color: green;"">@item.Text (@item.key)</span>
            </div>
        )
    },
    new BitSplitButtonItem()
    {
        Text = ""Edit"",
        key = ""edit-key"",
        ItemTemplate = (item) =>
        (
            @<div class=""item-template-box"">
                <span style=""color: yellow;"">@item.Text (@item.key)</span>
            </div>
        )
    },
    new BitSplitButtonItem()
    {
        Text = ""Delete"",
        key = ""delete-key"",
        ItemTemplate = (item) =>
        (
            @<div class=""item-template-box"">
                <span style=""color: red;"">@item.Text (@item.key)</span>
            </div>
        )
    }
};
";
}
