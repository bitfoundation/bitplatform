using System.Collections.Generic;
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

        private readonly string example1HTMLCode = @"<div>
    <BitSearchBox Placeholder=""Search""></BitSearchBox>
    <BitSearchBox Placeholder=""Search"" DefaultValue=""this is default value""></BitSearchBox>
    <BitSearchBox Placeholder=""Search with no animation"" DisableAnimation=""true""></BitSearchBox>
    <BitSearchBox Placeholder=""SearchBox with fixed icon"" ShowIcon=""true""></BitSearchBox>
    <BitSearchBox Placeholder=""Search with Binded value"" @bind-Value=""@TextValue""></BitSearchBox>
    <BitLabel>The value you are looking for: @TextValue</BitLabel>
</div>";

        private readonly string example1CSharpCode = @"
@code {
    private string TextValue;
}";

        private readonly string example2HTMLCode = @"<BitSearchBox Placeholder=""Search"" IsUnderlined=""true""></BitSearchBox>";

        private readonly string example3HTMLCode = @"<BitSearchBox Placeholder=""Filter"" IconName=""BitIconName.Filter""></BitSearchBox>";

        private readonly string example4HTMLCode = @"<BitSearchBox Placeholder=""Search"" Width=""250px""></BitSearchBox>";

        private readonly string example5HTMLCode = @"<BitSearchBox Placeholder=""Search"" IsEnabled=""false""></BitSearchBox>";
    }
}
