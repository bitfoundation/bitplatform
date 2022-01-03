using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.TextField
{
    public partial class BitTextFieldDemo
    {
        private BitTextFieldType InputType = BitTextFieldType.Password;
        private string TextValue;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "CanRevealPassword",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether to show the reveal password button for input type 'password'.",
            },
            new ComponentParameter()
            {
                Name = "DefaultValue",
                Type = "string",
                DefaultValue = "",
                Description = "Default value of the text field. Only provide this if the text field is an uncontrolled component; otherwise, use the value property.",
            },
            new ComponentParameter()
            {
                Name = "Description",
                Type = "string",
                DefaultValue = "",
                Description = "Description displayed below the text field to provide additional details about what text to enter.",
            },
            new ComponentParameter()
            {
                Name = "DescriptionFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Shows the custom description for text field.",
            },
            new ComponentParameter()
            {
                Name = "HasBorder",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not the text field is borderless.",
            },
            new ComponentParameter()
            {
                Name = "IconName",
                Type = "BitIcon",
                DefaultValue = "",
                Description = "The icon name for the icon shown in the far right end of the text field.",
            },
            new ComponentParameter()
            {
                Name = "IsMultiline",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not the text field is a Multiline text field.",
            },
            new ComponentParameter()
            {
                Name = "IsReadonly",
                Type = "bool",
                DefaultValue = "false",
                Description = "If true, the text field is readonly.",
            },
            new ComponentParameter()
            {
                Name = "IsResizable",
                Type = "bool",
                DefaultValue = "false",
                Description = "For multiline text fields, whether or not the field is resizable.",
            },
            new ComponentParameter()
            {
                Name = "IsRequired",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the associated input is required or not, add an asterisk '*' to its label.",
            },
            new ComponentParameter()
            {
                Name = "IsUnderlined",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not the text field is underlined.",
            },
            new ComponentParameter()
            {
                Name = "Label",
                Type = "string",
                DefaultValue = "",
                Description = "Label displayed above the text field and read by screen readers.",
            },
            new ComponentParameter()
            {
                Name = "LabelFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Shows the custom label for text field.",
            },
            new ComponentParameter()
            {
                Name = "MaxLength",
                Type = "int",
                DefaultValue = "-1",
                Description = "Specifies the maximum number of characters allowed in the input.",
            },
            new ComponentParameter()
            {
                Name = "OnChange",
                Type = "EventCallback<string?>",
                DefaultValue = "",
                Description = "Callback for when the input value changes. This is called on both input and change events.",
            },
            new ComponentParameter()
            {
                Name = "OnClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the input clicked.",
            },
            new ComponentParameter()
            {
                Name = "OnFocus",
                Type = "EventCallback<FocusEventArgs>",
                DefaultValue = "",
                Description = "Callback for when focus moves into the input.",
            },
            new ComponentParameter()
            {
                Name = "OnKeyDown",
                Type = "EventCallback<KeyboardEventArgs>",
                DefaultValue = "",
                Description = "Callback for when a keyboard key is pressed.",
            },
            new ComponentParameter()
            {
                Name = "OnFocusIn",
                Type = "EventCallback<FocusEventArgs>",
                DefaultValue = "",
                Description = "Callback for when focus moves into the input.",
            },
            new ComponentParameter()
            {
                Name = "OnFocusOut",
                Type = "EventCallback<FocusEventArgs>",
                DefaultValue = "",
                Description = "Callback for when focus moves out of the input.",
            },
            new ComponentParameter()
            {
                Name = "OnKeyUp",
                Type = "EventCallback<KeyboardEventArgs>",
                DefaultValue = "",
                Description = "Callback for When a keyboard key is released.",
            },
            new ComponentParameter()
            {
                Name = "Placeholder",
                Type = "string",
                DefaultValue = "",
                Description = "Input placeholder text.",
            },  
            new ComponentParameter()
            {
                Name = "Prefix",
                Type = "string",
                DefaultValue = "",
                Description = "Prefix displayed before the text field contents. This is not included in the value. Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.",
            },
            new ComponentParameter()
            {
                Name = "PrefixFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Shows the custom prefix for text field.",
            },
            new ComponentParameter()
            {
                Name = "Rows",
                Type = "int",
                DefaultValue = "3",
                Description = "For multiline text, Number of rows.",
            },
            new ComponentParameter()
            {
                Name = "RvealPasswordAriaLabel",
                Type = "string",
                DefaultValue = "",
                Description = "Aria label for the reveal password button.",
            },
            new ComponentParameter()
            {
                Name = "Suffix",
                Type = "string",
                DefaultValue = "",
                Description = "Suffix displayed after the text field contents. Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.",
            },
            new ComponentParameter()
            {
                Name = "SuffixFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Shows the custom suffix for text field.",
            },
            new ComponentParameter()
            {
                Name = "Type",
                Type = "BitTextFieldType",
                LinkType = LinkType.Link,
                Href = "#text-field-type-enum",
                DefaultValue = "BitTextFieldType.text",
                Description = "Input type.",
            },
            new ComponentParameter()
            {
                Name = "Value",
                Type = "string",
                DefaultValue = "",
                Description = "Current value of the text field.",
            },
            new ComponentParameter()
            {
                Name = "ValueChanged",
                Type = "EventCallback<string?>",
                DefaultValue = "",
                Description = "Callback for when the input value changes.",
            },
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "text-field-type-enum",
                Title = "BitTextFieldType Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Text",
                        Description="The TextField characters are shown as text.",
                        Value="0",
                    },
                    new EnumItem()
                    {
                        Name= "Password",
                        Description="The TextField characters are masked.",
                        Value="1",
                    }
                }
            }
        };

        private readonly string textFieldSampleCode = $"<BitTextField Label='Standard'></BitTextField>{Environment.NewLine}" +
              $"<BitTextField IsReadonly='true'{Environment.NewLine}" +
              $"Label='Read-only'{Environment.NewLine}" +
              $"DefaultValue='I am read-only'>{Environment.NewLine}" +
              $"</BitTextField>{Environment.NewLine}" +
              $"<BitTextField IsEnabled='false'{Environment.NewLine}" +
              $"Label='Disabled'{Environment.NewLine}" +
              $"</BitTextField>{Environment.NewLine}" +
              $"<BitTextField IsEnabled='false'{Environment.NewLine}" +
              $"Label='Disabled'{Environment.NewLine}" +
              $"Placeholder='I am disabled'>{Environment.NewLine}" +
              $"</BitTextField>{Environment.NewLine}" +
              $"<BitTextField Placeholder='Please enter text here'{Environment.NewLine}" +
              $"Label='With placeholder'{Environment.NewLine}" +
              $"</BitTextField>{Environment.NewLine}" +
              $"<BitTextField MaxLength='10'{Environment.NewLine}" +
              $"Label='Controlled TextField limiting length of value to 10'{Environment.NewLine}" +
              $"</BitTextField>{Environment.NewLine}" +
              $"<BitTextField IconName='BitIcon.CalendarMirrored'{Environment.NewLine}" +
              $"Label='With an icon'{Environment.NewLine}" +
              $"</BitTextField>{Environment.NewLine}" +
              $"<BitTextField Type='@InputType'{Environment.NewLine}" +
              $"Label='Password with reveal button'{Environment.NewLine}" +
              $"CanRevealPassword='true'{Environment.NewLine}" +
              $"</BitTextField>{Environment.NewLine}" +
              $"<BitTextField IsRequired='true'{Environment.NewLine}" +
              $"Label='Required'{Environment.NewLine}" +
              $"</BitTextField>{Environment.NewLine}" +
              $"<BitTextField AriaLabel='Required without visible label'{Environment.NewLine}" +
              $"IsRequired='true'{Environment.NewLine}" +
              $"</BitTextField>{Environment.NewLine}" +
              $"@code {{ {Environment.NewLine}" +
              $"private string TextValue;{Environment.NewLine}" +
              $"private BitTextFieldType InputType = BitTextFieldType.Password;{Environment.NewLine}" +
              "}}";

        private readonly string multiLineSampleCode = $"<BitTextField Label='Standard'>{Environment.NewLine}" +
              $"<IsMultiline='true'{Environment.NewLine}" +
              $"Rows='3'{Environment.NewLine}" +
              $"</BitTextField>{Environment.NewLine}" +
              $"<BitTextField IsMultiline='true'{Environment.NewLine}" +
              $"Label='Limited multi-line text field - 10 chars'{Environment.NewLine}" +
              $"MaxLength='10'{Environment.NewLine}" +
              $"</BitTextField>{Environment.NewLine}" +
              $"<BitTextField Label='Disabled'{Environment.NewLine}" +
              $"IsEnabled='false'{Environment.NewLine}" +
              $"IsMultiline='true'>{Environment.NewLine}" +
              $"Value='Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat'>{Environment.NewLine}" +
              $"</BitTextField>{Environment.NewLine}" +
              $"<BitTextField Label='Non-resizable'{Environment.NewLine}" +
              $"IsMultiline='true'{Environment.NewLine}" +
              $"IsResizable='false'{Environment.NewLine}" +
              "</BitTextField>";

        private readonly string underlinedSampleCode = $"<BitTextField Label='Standard:'>{Environment.NewLine}" +
             $"<IsUnderlined='true'{Environment.NewLine}" +
             $"Rows='3'{Environment.NewLine}" +
             $"</BitTextField>{Environment.NewLine}" +
             $"<BitTextField Label='Disabled:'{Environment.NewLine}" +
             $"IsUnderlined='true'{Environment.NewLine}" +
             $"IsEnabled='false'{Environment.NewLine}" +
             $"DefaultValue='I am disabled'>{Environment.NewLine}" +
             $"</BitTextField>{Environment.NewLine}" +
             $"<BitTextField Label='Required:'{Environment.NewLine}" +
             $"IsEnabled='false'{Environment.NewLine}" +
             $"IsMultiline='true'>{Environment.NewLine}" +
             $"DefaultValue='I am disabled'>{Environment.NewLine}" +
             $"</BitTextField>{Environment.NewLine}" +
             $"<BitTextField Label='Borderless single-line TextField'{Environment.NewLine}" +
             $"HasBorder='false'{Environment.NewLine}" +
             $"</BitTextField>{Environment.NewLine}" +
             $"<BitTextField Label='Borderless multi-line TextField'{Environment.NewLine}" +
             $"HasBorder='false'{Environment.NewLine}" +
             $"IsMultiline='true'>{Environment.NewLine}" +
             "</BitTextField>";

        private readonly string prefixsuffixSampleCode = $"<BitTextField Label='With Prefix'>{Environment.NewLine}" +
            $"Prefix='https://'{Environment.NewLine}" +
            $"Rows='3'{Environment.NewLine}" +
            $"</BitTextField>{Environment.NewLine}" +
            $"<BitTextField Label='With Suffix'{Environment.NewLine}" +
            $"Suffix='.com'{Environment.NewLine}" +
            $"</BitTextField>{Environment.NewLine}" +
            $"<BitTextField Label='Disabled With Prefix'{Environment.NewLine}" +
            $"Prefix='https://'{Environment.NewLine}" +
            $"IsEnabled='false'{Environment.NewLine}" +
            $"</BitTextField>{Environment.NewLine}" +
            $"<BitTextField Label='With Prefix And Suffix'{Environment.NewLine}" +
            $"Prefix='https://'{Environment.NewLine}" +
            $"Suffix='.com'{Environment.NewLine}" +
            "</BitTextField>";

        private readonly string customLabelSampleCode = $"<BitTextField Description='Click the (i) icon!'>{Environment.NewLine}" +
            $"<LabelFragment>{Environment.NewLine}" +
            $"<BitLabel Style='display:inline-block;padding-bottom:10px;'>Custom label rendering</BitLabel>{Environment.NewLine}" +
            $"<BitIconButton IconName='BitIcon.Info'></BitIconButton>{Environment.NewLine}" +
            $"</LabelFragment>{Environment.NewLine}" +
            $"</BitTextField>{Environment.NewLine}" +
            $"<BitTextField>{Environment.NewLine}" +
            $"<DescriptionFragment>{Environment.NewLine}" +
            $"<BitLabel Style='color:green;'>Custom description rendering</BitLabel>{Environment.NewLine}" +
            $"</DescriptionFragment>{Environment.NewLine}" +
            "</BitTextField>";
    }
}
