using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.SearchBox;

public partial class BitSearchBoxDemo
{
    private string TwoWaySearchValue;
    private string OnChangeSearchValue;
    private string OnSearchValue;

    private ValidationSearchBoxModel validationSearchBoxModel = new();

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
            Name = "IconName",
            Type = "BitIconName",
            Description = "The icon name for the icon shown at the beginning of the search box.",
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
    };

    #region Sample Code 1

    private readonly string example1HTMLCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitSearchBox Placeholder=""Search"" />
</div>
<div>
    <BitLabel>Disabled</BitLabel>
    <BitSearchBox Placeholder=""Search"" IsEnabled=""false"" />
</div>
";

    #endregion

    #region Sample Code 2

    private readonly string example2HTMLCode = @"
<div>
    <BitLabel>Basic Underlined SearchBox</BitLabel>
    <BitSearchBox Placeholder=""Search"" IsUnderlined=""true"" />
</div>
<div>
    <BitLabel>Disabled Underlined SearchBox</BitLabel>
    <BitSearchBox Placeholder=""Search"" IsUnderlined=""true"" IsEnabled=""false"" />
</div>
";

    #endregion

    #region Sample Code 3

    private readonly string example3HTMLCode = @"
<div>
    <BitLabel>SearchBox with fixed icon</BitLabel>
    <BitSearchBox Placeholder=""Search"" ShowIcon=""true"" />
</div>
<div>
    <BitLabel>SearchBox without icon animation</BitLabel>
    <BitSearchBox Placeholder=""Search"" DisableAnimation=""true"" />
</div>
<div>
    <BitLabel>SearchBox with custom icon</BitLabel>
    <BitSearchBox Placeholder=""Search"" IconName=""BitIconName.Filter"" />
</div>
";

    #endregion

    #region Sample Code 4

    private readonly string example4HTMLCode = @"
<div>
    <BitLabel>Two-way Bind</BitLabel>
    <BitSearchBox Placeholder=""Search"" @bind-Value=""TwoWaySearchValue"" />
    <BitTextField Placeholder=""Search Value"" Style=""margin-top: 5px;"" @bind-Value=""TwoWaySearchValue"" />
</div>
<div>
    <BitLabel>OnChange</BitLabel>
    <BitSearchBox Placeholder=""Search"" OnChange=""(s) => OnChangeSearchValue = s"" OnClear=""() => OnChangeSearchValue = string.Empty"" />
    <span>Search Value: @OnChangeSearchValue</span>
</div>
<div>
    <BitLabel>OnSearch (Serach by Enter)</BitLabel>
    <BitSearchBox Placeholder=""Search"" OnSearch=""(s) => OnSearchValue = s"" OnClear=""() => OnSearchValue = string.Empty"" />
    <span>Search Value: @OnSearchValue</span>
</div>
";

    private readonly string example4CSharpCode = @"
private string TwoWaySearchValue;
private string OnChangeSearchValue;
private string OnSearchValue;
";

    #endregion

    #region Sample Code 5

    private readonly string example5HTMLCode = @"
<EditForm Model=""validationSearchBoxModel"">
    <DataAnnotationsValidator />
    <BitSearchBox Placeholder=""Search"" DefaultValue=""This is default value"" @bind-Value=""validationSearchBoxModel.Text"" />
    <ValidationMessage For=""() => validationSearchBoxModel.Text"" />
</EditForm>
";

    private readonly string example5CSharpCode = @"
public class ValidationSearchBoxModel
{
    [StringLength(6, MinimumLength = 2, ErrorMessage = ""The text field length must be between 6 and 2 characters in length."")]
    public string Text { get; set; }
}

private ValidationSearchBoxModel validationSearchBoxModel = new();
";

    #endregion
}
