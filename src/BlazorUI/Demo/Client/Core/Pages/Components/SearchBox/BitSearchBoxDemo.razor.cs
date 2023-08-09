﻿using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.SearchBox;

public partial class BitSearchBoxDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Autocomplete",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies the value of the autocomplete attribute of the input component.",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "string?",
            DefaultValue = "null",
            Description = "The default value of the text in the SearchBox, in the case of an uncontrolled component.",
        },
        new()
        {
            Name = "DisableAnimation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not to animate the search box icon on focus.",
        },
        new()
        {
            Name = "FixedIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not to make the icon be always visible (it hides by default when the search box is focused).",
        },
        new()
        {
            Name = "IsUnderlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the SearchBox is underlined.",
        },
        new()
        {
            Name = "IconName",
            Type = "string",
            DefaultValue = "Search",
            Description = "The icon name for the icon shown at the beginning of the search box.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<string?>",
            Description = "Callback for when the input value changes.",
        },
        new()
        {
            Name = "OnClear",
            Type = "EventCallback",
            Description = "Callback executed when the user clears the search box by either clicking 'X' or hitting escape.",
        },
        new()
        {
            Name = "OnEscape",
            Type = "EventCallback",
            Description = "Callback executed when the user presses escape in the search box.",
        },
        new()
        {
            Name = "OnSearch",
            Type = "EventCallback<string?>",
            Description = "Callback executed when the user presses enter in the search box.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "Placeholder for the search box.",
        },
    };



    private readonly string example1HTMLCode = @"
<BitLabel>Basic</BitLabel>
<BitSearchBox Placeholder=""Search"" />
    
<BitLabel>Disabled</BitLabel>
<BitSearchBox Placeholder=""Search"" IsEnabled=""false"" />";

    private readonly string example2HTMLCode = @"
<BitLabel>Basic Underlined SearchBox</BitLabel>
<BitSearchBox Placeholder=""Search"" IsUnderlined=""true"" />

<BitLabel>Disabled Underlined SearchBox</BitLabel>
<BitSearchBox Placeholder=""Search"" IsUnderlined=""true"" IsEnabled=""false"" />";

    private readonly string example3HTMLCode = @"
<BitLabel>SearchBox with fixed icon</BitLabel>
<BitSearchBox Placeholder=""Search"" FixedIcon=""true"" />

<BitLabel>SearchBox without icon animation</BitLabel>
<BitSearchBox Placeholder=""Search"" DisableAnimation=""true"" />

<BitLabel>SearchBox with custom icon</BitLabel>
<BitSearchBox Placeholder=""Search"" IconName=""@BitIconName.Filter"" />";

    private readonly string example4HTMLCode = @"
<BitLabel>Two-way Bind</BitLabel>
<BitSearchBox Placeholder=""Search"" @bind-Value=""TwoWaySearchValue"" />
<BitTextField Placeholder=""Search Value"" Style=""margin-top: 5px;"" @bind-Value=""TwoWaySearchValue"" />

<BitLabel>OnChange</BitLabel>
<BitSearchBox Placeholder=""Search"" OnChange=""(s) => OnChangeSearchValue = s"" OnClear=""() => OnChangeSearchValue = string.Empty"" />
<BitLabel>Search Value: @OnChangeSearchValue</BitLabel>

<BitLabel>OnSearch (Serach by Enter)</BitLabel>
<BitSearchBox Placeholder=""Search"" OnSearch=""(s) => OnSearchValue = s"" OnClear=""() => OnSearchValue = string.Empty"" />
<BitLabel>Search Value: @OnSearchValue</BitLabel>";
    private readonly string example4CSharpCode = @"
private string TwoWaySearchValue;
private string OnChangeSearchValue;
private string OnSearchValue;";

    private readonly string example5HTMLCode = @"
<EditForm Model=""ValidationSearchBoxModel"">
    <DataAnnotationsValidator />
    <BitSearchBox Placeholder=""Search"" DefaultValue=""This is default value"" @bind-Value=""ValidationSearchBoxModel.Text"" />
    <ValidationMessage For=""() => ValidationSearchBoxModel.Text"" />
</EditForm>";
    private readonly string example5CSharpCode = @"
public class ValidationSearchBoxModel
{
    [StringLength(6, MinimumLength = 2, ErrorMessage = ""The text field length must be between 6 and 2 characters in length."")]
    public string Text { get; set; }
}

private ValidationSearchBoxModel ValidationSearchBoxModel = new();";
}
