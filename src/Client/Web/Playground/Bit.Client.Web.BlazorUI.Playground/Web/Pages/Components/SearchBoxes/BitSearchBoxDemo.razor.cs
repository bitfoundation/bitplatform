using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.SearchBoxes
{
    public partial class BitSearchBoxDemo
    {
        private string TextValue;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "defaultValue",
                Type = "string",
                DefaultValue = "",
                Description = "The default value of the text in the SearchBox, in the case of an uncontrolled component.",
            },
            new ComponentParameter()
            {
                Name = "disableAnimation",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not to animate the search box icon on focus.",
            },
            new ComponentParameter()
            {
                Name = "isUnderlined",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not the SearchBox is underlined.",
            },
            new ComponentParameter()
            {
                Name = "onChange",
                Type = "EventCallback<string?>",
                DefaultValue = "",
                Description = "Callback for when the input value changes.",
            },
            new ComponentParameter()
            {
                Name = "onClear",
                Type = "EventCallback",
                DefaultValue = "",
                Description = "Callback executed when the user clears the search box by either clicking 'X' or hitting escape.",
            },
            new ComponentParameter()
            {
                Name = "onEscape",
                Type = "EventCallback",
                DefaultValue = "",
                Description = "Callback executed when the user presses escape in the search box.",
            },
            new ComponentParameter()
            {
                Name = "onSearch",
                Type = "EventCallback<string?> ",
                DefaultValue = "",
                Description = "Callback executed when the user presses enter in the search box.",
            },
            new ComponentParameter()
            {
                Name = "placeholder",
                Type = "string",
                DefaultValue = "",
                Description = "Placeholder for the search box.",
            },
            new ComponentParameter()
            {
                Name = "showIcon",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not to make the icon be always visible (it hides by default when the search box is focused).",
            },
            new ComponentParameter()
            {
                Name = "value",
                Type = "string",
                DefaultValue = "",
                Description = "The value of the text in the search box.",
            },
            new ComponentParameter()
            {
                Name = "valueChanged",
                Type = "EventCallback<string?> ",
                DefaultValue = "",
                Description = "Callback for when the input value changes.",
            },
            new ComponentParameter()
            {
                Name = "width",
                Type = "string",
                DefaultValue = "",
                Description = "Specifies the width of the search box.",
            },
        };
    }
}
