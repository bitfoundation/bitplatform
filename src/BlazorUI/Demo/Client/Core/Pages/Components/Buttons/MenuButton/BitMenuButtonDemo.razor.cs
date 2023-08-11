using Bit.BlazorUI.Demo.Client.Core.Models;
using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class BitMenuButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the button for the benefit of screen readers."
        },
        new()
        {
            Name = "AriaHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, add an aria-hidden attribute instructing screen readers to ignore the element."
        },
        new()
        {
            Name = "ButtonStyle",
            Type = "BitButtonStyle",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of button, Possible values: Primary | Standard.",
            LinkType = LinkType.Link,
            Href = "#button-style-enum"
        },
        new()
        {
            Name = "ButtonType",
            Type = "BitButtonType",
            DefaultValue = "null",
            Description = "The type of the button.",
            LinkType = LinkType.Link,
            Href = "#button-type-enum"
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the BitMenuButton, that are BitMenuButtonOption components.",
        },
        new()
        {
            Name = "ClassField",
            Type = "string",
            DefaultValue = "Class",
            Description = "The CSS Class field name of the custom input class.",
        },
        new()
        {
            Name = "ClassFieldSelector",
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "The CSS Class field selector of the custom input class.",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content inside the header of MenuButton can be customized.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name to show inside the header of MenuButton.",
        },
        new()
        {
            Name = "IconNameField",
            Type = "string",
            DefaultValue = "IconName",
            Description = "IconName field name of the custom input class.",
        },
        new()
        {
            Name = "IconNameFieldSelector",
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "IconName field selector of the custom input class.",
        },
        new()
        {
            Name = "IsEnabledField",
            Type = "string",
            DefaultValue = "IsEnabled",
            Description = "IsEnabled field name of the custom input class.",
        },
        new()
        {
            Name = "IsEnabledFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            DefaultValue = "null",
            Description = "IsEnabled field selector of the custom input class.",
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "List of Item, each of which can be a Button with different action in the MenuButton.",
            LinkType = LinkType.Link,
            Href = "#menu-button-items"
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom content to render each item.",
        },
        new()
        {
            Name = "KeyField",
            Type = "string",
            DefaultValue = "Key",
            Description = "Key field name of the custom input class.",
        },
        new()
        {
            Name = "KeyFieldSelector",
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "Key field selector of the custom input class.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "The callback is called when the MenuButton header is clicked."
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "OnClick of each item returns that item with its property."
        },
        new()
        {
            Name = "StyleField",
            Type = "string",
            DefaultValue = "Style",
            Description = "The CSS Style field name of the custom input class.",
        },
        new()
        {
            Name = "StyleFieldSelector",
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "The CSS Style field selector of the custom input class.",
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text to show inside the header of MenuButton."
        },
        new()
        {
            Name = "TextField",
            Type = "string",
            DefaultValue = "Text",
            Description = "Name of an icon to render next to the item text.",
        },
        new()
        {
            Name = "TextFieldSelector",
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "Name of an icon to render next to the item text.",
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "menu-button-items",
            Title = "BitMenuButtonItem",
            Parameters = new()
            {
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The custom CSS classes of the item.",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to the item text.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the item is enabled.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a Key of the item.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The custom value for the style attribute of the item.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitMenuButtonItem>?",
                   DefaultValue = "null",
                   Description = "The custom template for the item.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text to render in the item.",
               }
            }
        },
        new()
        {
            Id = "menu-button-options",
            Title = "BitMenuButtonOption",
            Parameters = new()
            {
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The custom CSS classes of the option.",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to the option text.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the option is enabled.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a key of the option.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The custom value for the style attribute of the option.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitMenuButtonOption>?",
                   DefaultValue = "null",
                   Description = "The custom template for the option.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text to render in the option.",
               }
            }
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "button-style-enum",
            Name = "BitButtonStyle",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "Primary",
                    Description="The button with white text on a blue background.",
                    Value="0",
                },
                new()
                {
                    Name= "Standard",
                    Description="The button with black text on a white background.",
                    Value="1",
                }
            }
        },
        new()
        {
            Id = "button-type-enum",
            Name = "BitButtonType",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "Button",
                    Description="The button is a clickable button.",
                    Value="0",
                },
                new()
                {
                    Name= "Submit",
                    Description="The button is a submit button (submits form-data).",
                    Value="1",
                },
                new()
                {
                    Name= "Reset",
                    Description="The button is a reset button (resets the form-data to its initial values).",
                    Value="2",
                }
            }
        }
    };
}
