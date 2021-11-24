using System;
using System.Collections.Generic;
using System.Reflection;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.SearchBox
{
    public partial class BitSearchBoxDemo
    {
        private string TextValue;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "DefaultValue",
                Type = "string",
                DefaultValue = "",
                Description = "The default value of the text in the SearchBox, in the case of an uncontrolled component.",
            },
            new ComponentParameter()
            {
                Name = "DisableAnimation",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not to animate the search box icon on focus.",
            },
            new ComponentParameter()
            {
                Name = "IsUnderlined",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not the SearchBox is underlined.",
            },
            new ComponentParameter()
            {
                Name = "OnChange",
                Type = "EventCallback<string?>",
                DefaultValue = "",
                Description = "Callback for when the input value changes.",
            },
            new ComponentParameter()
            {
                Name = "OnClear",
                Type = "EventCallback",
                DefaultValue = "",
                Description = "Callback executed when the user clears the search box by either clicking 'X' or hitting escape.",
            },
            new ComponentParameter()
            {
                Name = "OnEscape",
                Type = "EventCallback",
                DefaultValue = "",
                Description = "Callback executed when the user presses escape in the search box.",
            },
            new ComponentParameter()
            {
                Name = "OnSearch",
                Type = "EventCallback<string?> ",
                DefaultValue = "",
                Description = "Callback executed when the user presses enter in the search box.",
            },
            new ComponentParameter()
            {
                Name = "Placeholder",
                Type = "string",
                DefaultValue = "",
                Description = "Placeholder for the search box.",
            },
            new ComponentParameter()
            {
                Name = "ShowIcon",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not to make the icon be always visible (it hides by default when the search box is focused).",
            },
            new ComponentParameter()
            {
                Name = "Value",
                Type = "string",
                DefaultValue = "",
                Description = "The value of the text in the search box.",
            },
            new ComponentParameter()
            {
                Name = "ValueChanged",
                Type = "EventCallback<string?> ",
                DefaultValue = "",
                Description = "Callback for when the input value changes.",
            },
            new ComponentParameter()
            {
                Name = "Width",
                Type = "string",
                DefaultValue = "",
                Description = "Specifies the width of the search box.",
            },
        };

        private readonly string searchBoxSampleCode = $"<BitSearchBox Placeholder='Search'></BitSearchBox>{Environment.NewLine}" +
             $"<BitSearchBox Placeholder='Search' DefaultValue='this is default value'></BitSearchBox>{Environment.NewLine}" +
             $"<BitSearchBox Placeholder='Search with no animation' DisableAnimation='true'></BitSearchBox>{Environment.NewLine}" +
             $"<BitSearchBox Placeholder='SearchBox with fixed icon' ShowIcon='true'></BitSearchBox>{Environment.NewLine}" +
             $"<BitSearchBox Placeholder='Search with Binded value' @bind-Value='@TextValue'></BitSearchBox>{Environment.NewLine}" +
             $"<BitLabel>The value you are looking for: @TextValue</BitLabel>{Environment.NewLine}" +
             $"@code {{ {Environment.NewLine}" +
             $"private string TextValue;{Environment.NewLine}" +
             $"}}" ;
        
        private readonly string underlineSearchBoxSampleCode = "<BitSearchBox Placeholder='Search' IsUnderlined='true'></BitSearchBox>";
        private readonly string customIconSearchBoxSampleCode = "<BitSearchBox Placeholder='Filter' IconName='BitIcon.Filter'></BitSearchBox>";
        private readonly string fixedWidthSearchBoxSampleCode = "<BitSearchBox Placeholder='Search' Width='250px'></BitSearchBox>";
        private readonly string disabledSearchBoxSampleCode = "<BitSearchBox Placeholder='Search' IsEnabled='false'></BitSearchBox>";
    }
}
