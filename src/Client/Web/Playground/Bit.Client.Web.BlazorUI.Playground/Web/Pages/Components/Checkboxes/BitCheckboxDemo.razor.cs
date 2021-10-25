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
                Name = "ariaDescription",
                Type = "string",
                DefaultValue = "",
                Description = "Detailed description of the checkbox for the benefit of screen readers.",
            },
            new ComponentParameter()
            {
                Name = "ariaLabelledby",
                Type = "string",
                DefaultValue = "",
                Description = "ID for element that contains label information for the checkbox.",
            },
            new ComponentParameter()
            {
                Name = "ariaPositionInSet",
                Type = "string",
                DefaultValue = "",
                Description = "The position in the parent set (if in a set) for aria-posinset.",
            },
            new ComponentParameter()
            {
                Name = "ariaSetSize",
                Type = "string",
                DefaultValue = "",
                Description = "The total size of the parent set (if in a set) for aria-setsize.",
            },
            new ComponentParameter()
            {
                Name = "boxSide",
                Type = "BitBoxSide",
                LinkType = LinkType.Link,
                Href = "#box-side-enum",
                DefaultValue = "BitBoxSide.start",
                Description = "Determines whether the checkbox should be shown before the label (start) or after (end).",
            },
            new ComponentParameter()
            {
                Name = "checkmarkIconName",
                Type = "string",
                DefaultValue = "",
                Description = "Custom icon for the check mark rendered by the checkbox instade of default check mark icon.",
            },
            new ComponentParameter()
            {
                Name = "checkmarkIconAriaLabel",
                Type = "string",
                DefaultValue = "",
                Description = "The aria label of the icon for the benefit of screen readers.",
            },
            new ComponentParameter()
            {
                Name = "childContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of checkbox, It can be Any custom tag or a text.",
            },
            new ComponentParameter()
            {
                Name = "defaultIsIndeterminate",
                Type = "bool",
                DefaultValue = "",
                Description = "Default indeterminate visual state for checkbox.",
            },
            new ComponentParameter()
            {
                Name = "isChecked",
                Type = "bool",
                DefaultValue = "",
                Description = "Checkbox state, control the checked state at a higher level.",
            },
            new ComponentParameter()
            {
                Name = "isCheckedChanged",
                Type = "EventCallback<bool>",
                DefaultValue = "",
                Description = "Callback that is called when the IsChecked parameter changed.",
            },
            new ComponentParameter()
            {
                Name = "isIndeterminate",
                Type = "bool",
                DefaultValue = "",
                Description = "Callback that is called when the IsIndeterminate parameter changed.",
            },
            new ComponentParameter()
            {
                Name = "isIndeterminateChanged",
                Type = "EventCallback<bool>",
                DefaultValue = "",
                Description = "An indeterminate visual state for checkbox. Setting indeterminate state takes visual precedence over checked given but does not affect on IsChecked state.",
            },
            new ComponentParameter()
            {
                Name = "name",
                Type = "string",
                DefaultValue = "",
                Description = "Name for the checkbox input. This is intended for use with forms and NOT displayed in the UI.",
            },
            new ComponentParameter()
            {
                Name = "onChange",
                Type = "EventCallback<bool>",
                DefaultValue = "",
                Description = "Callback that is called when the checked value has changed.",
            },
            new ComponentParameter()
            {
                Name = "onClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the checkbox clicked.",
            },
            new ComponentParameter()
            {
                Name = "title",
                Type = "string",
                DefaultValue = "",
                Description = "Title text applied to the root element and the hidden checkbox input.",
            },
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "button-style-enum",
                Title = "ButtonStyle enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "start",
                        Description="",
                        Value="start = 0",
                    },
                    new EnumItem()
                    {
                        Name= "end",
                        Description="",
                        Value="end = 1",
                    }
                }
            }
        };
    }
}
