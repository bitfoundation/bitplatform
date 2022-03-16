using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.SearchBox
{
    public partial class BitSearchBoxDemo
    {
        private string TextValue;

        ValidationSearchBoxModel validationSearchBoxModel = new();

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "Autocomplete",
                Type = "string",
                DefaultValue = "",
                Description = "Specifies the value of the autocomplete attribute of the input component.",
            },
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
            new ComponentParameter()
            {
                Name = "Visibility",
                Type = "BitComponentVisibility",
                LinkType = LinkType.Link,
                Href = "#component-visibility-enum",
                DefaultValue = "BitComponentVisibility.Visible",
                Description = "Whether the component is Visible,Hidden,Collapsed.",
            },
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
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

        private readonly string example1HTMLCode = @"<div>
    <BitSearchBox Placeholder=""Search""></BitSearchBox>
    <BitSearchBox Placeholder=""Search"" DefaultValue=""this is default value""></BitSearchBox>
    <BitSearchBox Placeholder=""Search with no animation"" DisableAnimation=""true""></BitSearchBox>
    <BitSearchBox Placeholder=""SearchBox with fixed icon"" ShowIcon=""true""></BitSearchBox>
    <BitSearchBox Placeholder=""Search with Binded value"" @bind-Value=""@TextValue""></BitSearchBox>
    <BitLabel>The value you are looking for: @TextValue</BitLabel>
</div>";

        private readonly string example1CSharpCode = @"
private string TextValue;";

        private readonly string example2HTMLCode = @"<BitSearchBox Placeholder=""Search"" IsUnderlined=""true""></BitSearchBox>";

        private readonly string example3HTMLCode = @"<BitSearchBox Placeholder=""Filter"" IconName=""BitIconName.Filter""></BitSearchBox>";

        private readonly string example4HTMLCode = @"<BitSearchBox Placeholder=""Search"" Width=""250px""></BitSearchBox>";

        private readonly string example5HTMLCode = @"<BitSearchBox Placeholder=""Search"" IsEnabled=""false""></BitSearchBox>";

        private readonly string example6HTMLCode = @"<EditForm Model=""validationSearchBoxModel"">
     <DataAnnotationsValidator/>
     <BitSearchBox DefaultValue = ""This is default value"" @bind-Value=""validationSearchBoxModel.text""/>
     <ValidationMessage For = ""()=>validationSearchBoxModel.Text"" ></ ValidationMessage >
</EditForm> ";

        private readonly string example6CSharpCode = @"
ValidationSearchBoxModel validationSearchBoxModel = new();

public class ValidationSearchBoxModel
{
    [RegularExpression("" ^.{2,6}$"",
    ErrorMessage = ""The field  must be between 2 and 6."")]
    public string Text { get; set; }
}
";
    }

    public class ValidationSearchBoxModel
    {
        [RegularExpression("^.{2,6}$",
        ErrorMessage = "The field  must be between 2 and 6.")]
        public string Text { get; set; }
    }
}
