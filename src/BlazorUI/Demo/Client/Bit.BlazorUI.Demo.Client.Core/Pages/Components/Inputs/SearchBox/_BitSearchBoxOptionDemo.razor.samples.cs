namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SearchBox;

public partial class _BitSearchBoxOptionDemo
{
    private readonly string example1RazorCode = @"
<BitLabel>Basic</BitLabel>
<BitSearchBox Placeholder=""Search"" TItem=""BitSearchBoxOption"" />
    
<BitLabel>Disabled</BitLabel>
<BitSearchBox Placeholder=""Search"" IsEnabled=""false"" TItem=""BitSearchBoxOption"" />";

    private readonly string example2RazorCode = @"
<BitLabel>Basic Underlined SearchBox</BitLabel>
<BitSearchBox Placeholder=""Search"" IsUnderlined=""true"" TItem=""BitSearchBoxOption"" />

<BitLabel>Disabled Underlined SearchBox</BitLabel>
<BitSearchBox Placeholder=""Search"" IsUnderlined=""true"" IsEnabled=""false"" TItem=""BitSearchBoxOption"" />";

    private readonly string example3RazorCode = @"
<BitLabel>SearchBox with fixed icon</BitLabel>
<BitSearchBox Placeholder=""Search"" FixedIcon=""true"" TItem=""BitSearchBoxOption"" />

<BitLabel>SearchBox without icon animation</BitLabel>
<BitSearchBox Placeholder=""Search"" DisableAnimation=""true"" TItem=""BitSearchBoxOption"" />

<BitLabel>SearchBox with custom icon</BitLabel>
<BitSearchBox Placeholder=""Search"" IconName=""@BitIconName.Filter"" TItem=""BitSearchBoxOption"" />";

    private readonly string example4RazorCode = @"
<style>
    .custom-class {
        border: 1px solid red;
        box-shadow: aqua 0 0 1rem;
    }

    .custom-clear {
        color: blueviolet;
    }

    .custom-search {
        margin-right: 0.25rem;
        border-radius: 0.5rem;
        background-color: tomato;
    }
</style>

<BitSearchBox Placeholder=""Search"" Style=""background-color: lightskyblue; border-radius: 1rem; margin: 1rem 0"" TItem=""BitSearchBoxOption"" />
<BitSearchBox Placeholder=""Search"" Class=""custom-class"" TItem=""BitSearchBoxOption"" />

<BitSearchBox Placeholder=""Search""
              Styles=""@(new() {SearchIcon = ""color: darkorange;"",
                               Input = ""padding: 0.5rem; background-color: goldenrod""})"" 
              TItem=""BitSearchBoxOption"" />

<BitSearchBox Placeholder=""Search"" DefaultValue=""This is default value""
              Classes=""@(new() {ClearButtonIcon = ""custom-clear"",
                                SearchIconContainer = ""custom-search""})""
              TItem=""BitSearchBoxOption"" />";

    private readonly string example5RazorCode = @"
Visible: [ <BitSearchBox Visibility=""BitVisibility.Visible"" Placeholder=""Visible SearchBox"" TItem=""BitSearchBoxOption"" /> ]
Hidden: [ <BitSearchBox Visibility=""BitVisibility.Hidden"" Placeholder=""Hidden SearchBox"" TItem=""BitSearchBoxOption"" />  ]
Collapsed: [ <BitSearchBox Visibility=""BitVisibility.Collapsed"" Placeholder=""Collapsed SearchBox"" TItem=""BitSearchBoxOption"" />  ]";

    private readonly string example6RazorCode = @"
<BitLabel>Two-way Bind</BitLabel>
<BitSearchBox Placeholder=""Search"" TItem=""BitSearchBoxOption"" @bind-Value=""TwoWaySearchValue"" />
<BitTextField Placeholder=""Search Value"" Style=""margin-top: 5px;"" @bind-Value=""TwoWaySearchValue"" />

<BitLabel>OnChange</BitLabel>
<BitSearchBox Placeholder=""Search"" OnChange=""(s) => OnChangeSearchValue = s"" OnClear=""() => OnChangeSearchValue = string.Empty"" TItem=""BitSearchBoxOption"" />
<BitLabel>Search Value: @OnChangeSearchValue</BitLabel>

<BitLabel>OnSearch (Serach by Enter)</BitLabel>
<BitSearchBox Placeholder=""Search"" OnSearch=""(s) => OnSearchValue = s"" OnClear=""() => OnSearchValue = string.Empty"" TItem=""BitSearchBoxOption"" />
<BitLabel>Search Value: @OnSearchValue</BitLabel>";
    private readonly string example6CsharpCode = @"
private string TwoWaySearchValue;
private string OnChangeSearchValue;
private string OnSearchValue;";

    private readonly string example7RazorCode = @"
<EditForm Model=""ValidationSearchBoxModel"">
    <DataAnnotationsValidator />
    <BitSearchBox Placeholder=""Search"" DefaultValue=""This is default value"" TItem=""BitSearchBoxOption"" @bind-Value=""ValidationSearchBoxModel.Text"" />
    <ValidationMessage For=""() => ValidationSearchBoxModel.Text"" />
</EditForm>";
    private readonly string example7CsharpCode = @"
public class ValidationSearchBoxModel
{
    [StringLength(6, MinimumLength = 2, ErrorMessage = ""The text field length must be between 6 and 2 characters in length."")]
    public string Text { get; set; }
}

private ValidationSearchBoxModel ValidationSearchBoxModel = new();";

    private readonly string example8RazorCode = @"
<BitSearchBox Placeholder=""e.g. Apple""
              TItem=""BitSearchBoxOption""
              @bind-Value=""@SearchValue"">
    <BitSearchBoxOption Text=""Apple"" />
    <BitSearchBoxOption Text=""Banana"" />
    <BitSearchBoxOption Text=""Orange"" />
    <BitSearchBoxOption Text=""Grape"" />
    <BitSearchBoxOption Text=""Broccoli"" />
    <BitSearchBoxOption Text=""Carrot"" />
    <BitSearchBoxOption Text=""Lettuce"" />
</BitSearchBox>
<br />
SearchValue: @SearchValue";
    private readonly string example8CsharpCode = @"
private string SearchValue;";
}
