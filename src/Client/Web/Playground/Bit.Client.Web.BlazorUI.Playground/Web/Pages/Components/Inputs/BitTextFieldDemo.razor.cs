using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Inputs
{
    public partial class BitTextFieldDemo
    {
        private BitTextFieldType InputType = BitTextFieldType.Password;
        private string TextValue;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "canRevealPassword",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether to show the reveal password button for input type 'password'.",
            },
            new ComponentParameter()
            {
                Name = "defaultValue",
                Type = "string",
                DefaultValue = "",
                Description = "Default value of the text field. Only provide this if the text field is an uncontrolled component; otherwise, use the value property.",
            },
            new ComponentParameter()
            {
                Name = "description",
                Type = "string",
                DefaultValue = "",
                Description = "Description displayed below the text field to provide additional details about what text to enter.",
            },
            new ComponentParameter()
            {
                Name = "descriptionFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Shows the custom description for text field.",
            },
            new ComponentParameter()
            {
                Name = "hasBorder",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not the text field is borderless.",
            },
            new ComponentParameter()
            {
                Name = "iconName",
                Type = "string",
                DefaultValue = "",
                Description = "The icon name for the icon shown in the far right end of the text field.",
            },
            new ComponentParameter()
            {
                Name = "isMultiline",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not the text field is a Multiline text field.",
            },
            new ComponentParameter()
            {
                Name = "isReadonly",
                Type = "bool",
                DefaultValue = "false",
                Description = "If true, the text field is readonly.",
            },
            new ComponentParameter()
            {
                Name = "isResizable",
                Type = "bool",
                DefaultValue = "false",
                Description = "For multiline text fields, whether or not the field is resizable.",
            },
            new ComponentParameter()
            {
                Name = "isRequired",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the associated input is required or not, add an asterisk '*' to its label.",
            },
            new ComponentParameter()
            {
                Name = "isUnderlined",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not the text field is underlined.",
            },
            new ComponentParameter()
            {
                Name = "label",
                Type = "string",
                DefaultValue = "",
                Description = "Label displayed above the text field and read by screen readers.",
            },
            new ComponentParameter()
            {
                Name = "labelFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Shows the custom label for text field.",
            },
            new ComponentParameter()
            {
                Name = "maxLength",
                Type = "int",
                DefaultValue = "-1",
                Description = "Specifies the maximum number of characters allowed in the input.",
            },
            new ComponentParameter()
            {
                Name = "onChange",
                Type = "EventCallback<string?>",
                DefaultValue = "",
                Description = "Callback for when the input value changes. This is called on both input and change events.",
            },
            new ComponentParameter()
            {
                Name = "onClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the input clicked.",
            },
            new ComponentParameter()
            {
                Name = "onFocus",
                Type = "EventCallback<FocusEventArgs>",
                DefaultValue = "",
                Description = "Callback for when focus moves into the input.",
            },
            new ComponentParameter()
            {
                Name = "onKeyDown",
                Type = "EventCallback<KeyboardEventArgs>",
                DefaultValue = "",
                Description = "Callback for when a keyboard key is pressed.",
            },
            new ComponentParameter()
            {
                Name = "onFocusIn",
                Type = "EventCallback<FocusEventArgs>",
                DefaultValue = "",
                Description = "Callback for when focus moves into the input.",
            },
            new ComponentParameter()
            {
                Name = "onFocusOut",
                Type = "EventCallback<FocusEventArgs>",
                DefaultValue = "",
                Description = "Callback for when focus moves out of the input.",
            },
            new ComponentParameter()
            {
                Name = "onKeyUp",
                Type = "EventCallback<KeyboardEventArgs>",
                DefaultValue = "",
                Description = "Callback for When a keyboard key is released.",
            },
            new ComponentParameter()
            {
                Name = "placeholder",
                Type = "string",
                DefaultValue = "",
                Description = "Input placeholder text.",
            },  
            new ComponentParameter()
            {
                Name = "prefix",
                Type = "string",
                DefaultValue = "",
                Description = "Prefix displayed before the text field contents. This is not included in the value. Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.",
            },
            new ComponentParameter()
            {
                Name = "prefixFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Shows the custom prefix for text field.",
            },
            new ComponentParameter()
            {
                Name = "rows",
                Type = "int",
                DefaultValue = "3",
                Description = "For multiline text, Number of rows.",
            },
            new ComponentParameter()
            {
                Name = "rvealPasswordAriaLabel",
                Type = "string",
                DefaultValue = "",
                Description = "Aria label for the reveal password button.",
            },
            new ComponentParameter()
            {
                Name = "suffix",
                Type = "string",
                DefaultValue = "",
                Description = "Suffix displayed after the text field contents. Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.",
            },
            new ComponentParameter()
            {
                Name = "suffixFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Shows the custom suffix for text field.",
            },
            new ComponentParameter()
            {
                Name = "type",
                Type = "BitTextFieldType",
                LinkType = LinkType.Link,
                Href = "#text-field-type-enum",
                DefaultValue = "BitTextFieldType.text",
                Description = "Input type.",
            },
            new ComponentParameter()
            {
                Name = "value",
                Type = "string",
                DefaultValue = "",
                Description = "Current value of the text field.",
            },
            new ComponentParameter()
            {
                Name = "ValueChanged",
                Type = "EventCallback<string?>",
                DefaultValue = "",
                Description = "Callback for when the input value changes",
            },
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "text-field-type-enum",
                Title = "TextFieldType-enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "text",
                        Description="The TextField characters are shown as text.",
                        Value="text = 0",
                    },
                    new EnumItem()
                    {
                        Name= "password",
                        Description="The TextField characters are masked.",
                        Value="password = 1",
                    }
                }
            }
        };
    }
}
