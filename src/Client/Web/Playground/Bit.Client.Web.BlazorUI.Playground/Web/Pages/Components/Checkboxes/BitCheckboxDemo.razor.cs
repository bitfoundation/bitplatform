using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Checkboxes
{
    public partial class BitCheckboxDemo
    {
        private bool IsCheckBoxChecked = false;
        private bool IsCheckBoxIndeterminate = true;
        private bool IsCheckBoxIndeterminateInCode = true;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "AriaDescription",
                Type = "string",
                DefaultValue = "",
                Description = "Detailed description of the checkbox for the benefit of screen readers.",
            },
            new ComponentParameter()
            {
                Name = "AriaLabelledby",
                Type = "string",
                DefaultValue = "",
                Description = "ID for element that contains label information for the checkbox.",
            },
            new ComponentParameter()
            {
                Name = "AriaPositionInSet",
                Type = "string",
                DefaultValue = "",
                Description = "The position in the parent set (if in a set) for aria-posinset.",
            },
            new ComponentParameter()
            {
                Name = "AriaSetSize",
                Type = "string",
                DefaultValue = "",
                Description = "The total size of the parent set (if in a set) for aria-setsize.",
            },
            new ComponentParameter()
            {
                Name = "BoxSide",
                Type = "BitBoxSide",
                LinkType = LinkType.Link,
                Href = "#box-side-enum",
                DefaultValue = "BitBoxSide.Start",
                Description = "Determines whether the checkbox should be shown before the label (start) or after (end).",
            },
            new ComponentParameter()
            {
                Name = "CheckmarkIconName",
                Type = "BitIcon",
                DefaultValue = "",
                Description = "Custom icon for the check mark rendered by the checkbox instade of default check mark icon.",
            },
            new ComponentParameter()
            {
                Name = "CheckmarkIconAriaLabel",
                Type = "string",
                DefaultValue = "",
                Description = "The aria label of the icon for the benefit of screen readers.",
            },
            new ComponentParameter()
            {
                Name = "ChildContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of checkbox, It can be Any custom tag or a text.",
            },
            new ComponentParameter()
            {
                Name = "DefaultIsIndeterminate",
                Type = "bool",
                DefaultValue = "",
                Description = "Default indeterminate visual state for checkbox.",
            },
            new ComponentParameter()
            {
                Name = "IsChecked",
                Type = "bool",
                DefaultValue = "",
                Description = "Checkbox state, control the checked state at a higher level.",
            },
            new ComponentParameter()
            {
                Name = "IsCheckedChanged",
                Type = "EventCallback<bool>",
                DefaultValue = "",
                Description = "Callback that is called when the IsChecked parameter changed.",
            },
            new ComponentParameter()
            {
                Name = "IsIndeterminate",
                Type = "bool",
                DefaultValue = "",
                Description = "Callback that is called when the IsIndeterminate parameter changed.",
            },
            new ComponentParameter()
            {
                Name = "IsIndeterminateChanged",
                Type = "EventCallback<bool>",
                DefaultValue = "",
                Description = "An indeterminate visual state for checkbox. Setting indeterminate state takes visual precedence over checked given but does not affect on IsChecked state.",
            },
            new ComponentParameter()
            {
                Name = "Name",
                Type = "string",
                DefaultValue = "",
                Description = "Name for the checkbox input. This is intended for use with forms and NOT displayed in the UI.",
            },
            new ComponentParameter()
            {
                Name = "OnChange",
                Type = "EventCallback<bool>",
                DefaultValue = "",
                Description = "Callback that is called when the checked value has changed.",
            },
            new ComponentParameter()
            {
                Name = "OnClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the checkbox clicked.",
            },
            new ComponentParameter()
            {
                Name = "Title",
                Type = "string",
                DefaultValue = "",
                Description = "Title text applied to the root element and the hidden checkbox input.",
            },
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "box-side-enum",
                Title = "BitBoxSide Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Start",
                        Description="The checkbox shows before the label.",
                        Value="0",
                    },
                    new EnumItem()
                    {
                        Name= "End",
                        Description="The checkbox shows after the label.",
                        Value="1",
                    }
                }
            }
        };

        private readonly string checkboxSampleCode = $"<BitCheckbox @bind-IsChecked='IsCheckBoxChecked'>Basic Checkbox</BitCheckbox>{Environment.NewLine}" +
              $"<BitCheckbox IsChecked='true'>Checked Checkbox</BitCheckbox>{Environment.NewLine}" +
              $"<BitCheckbox IsEnabled='false'>Disable Checkbox</BitCheckbox>{Environment.NewLine}" +
              $"<BitCheckbox CheckmarkIconName='BitIcon.Heart'>Custom checkmark Checkbox</BitCheckbox>{Environment.NewLine}" +
              $"<@code {{ {Environment.NewLine}" +
              $"private bool IsCheckBoxChecked = false;{Environment.NewLine}" +
              "}";

        private readonly string reversedSampleCode = $"<BitCheckbox BoxSide='@BoxSide.End'>Reversed -- Basic Checkbox</BitCheckbox>{Environment.NewLine}" +
                 $"<BitCheckbox BoxSide='@BoxSide.End' IsChecked='true'>Reversed -- Checked Checkbox</BitCheckbox>{Environment.NewLine}" +
                 $"<BitCheckbox BoxSide='@BoxSide.End' IsEnabled='true'>Reversed -- Disable Checkbox</BitCheckbox>{Environment.NewLine}" +
                 $"<BitCheckbox BoxSide='@BoxSide.End' IsEnabled='false' IsChecked='true'>Reversed -- Disable Checked Checkbox</BitCheckbox>";

        private readonly string indeterminateSampleCode = $"<BitCheckbox @bind-IsChecked='IsCheckBoxIndeterminate' @bind-IsIndeterminate='IsCheckBoxChecked'>Basic Checkbox</BitCheckbox>{Environment.NewLine}" +
                 $"<BitCheckbox IsIndeterminate='true'>Indeterminate checkbox</BitCheckbox>{Environment.NewLine}" +
                 $"<BitCheckbox IsIndeterminate='true' IsEnabled='false'>Disabled indeterminated checkbox</BitCheckbox>{Environment.NewLine}" +
                 $"<BitCheckbox @bind-IsIndeterminate='IsCheckBoxIndeterminateInCode' @bind-IsChecked='IsCheckBoxChecked' IsChecked='true'>Controlled indeterminated checkbox</BitCheckbox>" +
                 $"<BitCheckbox OnClick='() => IsCheckBoxIndeterminateInCode = true'>Make Checkbox Indeterminate</BitCheckbox>" +
                 $"<@code {{ {Environment.NewLine}" +
                 $"private bool IsCheckBoxChecked = false;{Environment.NewLine}" +
                 $"private bool IsCheckBoxChecked = false;{Environment.NewLine}" +
                 "}";

        private readonly string customLabelSampleCode = $"<BitCheckbox>Custom-rendered label with a link go to <a href='https://github.com/bitfoundation/bitframework'>bit foundation repository page</a></BitCheckbox>";
    }
}
