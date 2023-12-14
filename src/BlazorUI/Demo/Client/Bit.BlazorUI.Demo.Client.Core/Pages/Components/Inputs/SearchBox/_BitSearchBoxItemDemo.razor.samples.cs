namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SearchBox;

public partial class _BitSearchBoxItemDemo
{
    private readonly string example1RazorCode = @"
<BitLabel>Basic</BitLabel>
<BitSearchBox Placeholder=""Search"" TItem=""BitSearchBoxItem"" />
    
<BitLabel>Disabled</BitLabel>
<BitSearchBox Placeholder=""Search"" IsEnabled=""false"" TItem=""BitSearchBoxItem"" />";

    private readonly string example2RazorCode = @"
<BitLabel>Basic Underlined SearchBox</BitLabel>
<BitSearchBox Placeholder=""Search"" IsUnderlined=""true"" TItem=""BitSearchBoxItem"" />

<BitLabel>Disabled Underlined SearchBox</BitLabel>
<BitSearchBox Placeholder=""Search"" IsUnderlined=""true"" IsEnabled=""false"" TItem=""BitSearchBoxItem"" />";

    private readonly string example3RazorCode = @"
<BitLabel>SearchBox with fixed icon</BitLabel>
<BitSearchBox Placeholder=""Search"" FixedIcon=""true"" TItem=""BitSearchBoxItem"" />

<BitLabel>SearchBox without icon animation</BitLabel>
<BitSearchBox Placeholder=""Search"" DisableAnimation=""true"" TItem=""BitSearchBoxItem"" />

<BitLabel>SearchBox with custom icon</BitLabel>
<BitSearchBox Placeholder=""Search"" IconName=""@BitIconName.Filter"" TItem=""BitSearchBoxItem"" />";

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

<BitSearchBox Placeholder=""Search"" Style=""background-color: lightskyblue; border-radius: 1rem; margin: 1rem 0"" TItem=""BitSearchBoxItem"" />
<BitSearchBox Placeholder=""Search"" Class=""custom-class"" TItem=""BitSearchBoxItem"" />

<BitSearchBox Placeholder=""Search""
              Styles=""@(new() {SearchIcon = ""color: darkorange;"",
                               Input = ""padding: 0.5rem; background-color: goldenrod""})"" 
              TItem=""BitSearchBoxItem"" />

<BitSearchBox Placeholder=""Search"" DefaultValue=""This is default value""
              Classes=""@(new() {ClearButtonIcon = ""custom-clear"",
                                SearchIconContainer = ""custom-search""})""
              TItem=""BitSearchBoxItem"" />";

    private readonly string example5RazorCode = @"
Visible: [ <BitSearchBox Visibility=""BitVisibility.Visible"" Placeholder=""Visible SearchBox"" TItem=""BitSearchBoxItem"" /> ]
Hidden: [ <BitSearchBox Visibility=""BitVisibility.Hidden"" Placeholder=""Hidden SearchBox"" TItem=""BitSearchBoxItem"" />  ]
Collapsed: [ <BitSearchBox Visibility=""BitVisibility.Collapsed"" Placeholder=""Collapsed SearchBox"" TItem=""BitSearchBoxItem"" />  ]";

    private readonly string example6RazorCode = @"
<BitLabel>Two-way Bind</BitLabel>
<BitSearchBox Placeholder=""Search"" TItem=""BitSearchBoxItem"" @bind-Value=""TwoWaySearchValue"" />
<BitTextField Placeholder=""Search Value"" Style=""margin-top: 5px;"" @bind-Value=""TwoWaySearchValue"" />

<BitLabel>OnChange</BitLabel>
<BitSearchBox Placeholder=""Search"" OnChange=""(s) => OnChangeSearchValue = s"" OnClear=""() => OnChangeSearchValue = string.Empty"" TItem=""BitSearchBoxItem"" />
<BitLabel>Search Value: @OnChangeSearchValue</BitLabel>

<BitLabel>OnSearch (Serach by Enter)</BitLabel>
<BitSearchBox Placeholder=""Search"" OnSearch=""(s) => OnSearchValue = s"" OnClear=""() => OnSearchValue = string.Empty"" TItem=""BitSearchBoxItem"" />
<BitLabel>Search Value: @OnSearchValue</BitLabel>";
    private readonly string example6CsharpCode = @"
private string TwoWaySearchValue;
private string OnChangeSearchValue;
private string OnSearchValue;";

    private readonly string example7RazorCode = @"
<EditForm Model=""ValidationSearchBoxModel"">
    <DataAnnotationsValidator />
    <BitSearchBox Placeholder=""Search"" DefaultValue=""This is default value"" TItem=""BitSearchBoxItem"" @bind-Value=""ValidationSearchBoxModel.Text"" />
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
<BitLabel>ItemsProvider:</BitLabel>
<BitSearchBox Placeholder=""e.g. Apple""
                Virtualize=""true""
                ItemsProvider=""LoadItems"" 
                TItem=""BitSearchBoxItem"" 
                @bind-Value=""@ItemsProviderSearchValue"" />
<br />
SearchValue: @ItemsProviderSearchValue

<BitLabel>ItemsProvider:</BitLabel>
<BitSearchBox Placeholder=""e.g. Pro""
                Virtualize=""true""
                ItemsProvider=""LoadItems"" 
                TItem=""BitSearchBoxItem"" 
                @bind-Value=""@ItemsProviderSearchValue"" />
<br />
SearchValue: @ItemsProviderSearchValue";
    private readonly string example8CsharpCode = @"
private string SearchValue;
private string ItemsProviderSearchValue;

private async ValueTask<BitSearchBoxItemsProviderResult<BitSearchBoxItem>> LoadItems(BitSearchBoxItemsProviderRequest<BitSearchBoxItem> request)
{
    try
    {
        var query = new Dictionary<string, object?>()
        {
            { ""$top"", request.Count == 0 ? 5 : request.Count },
            { ""$skip"", request.StartIndex }
        };

        if (string.IsNullOrEmpty(request.Search) is false)
        {
            query.Add(""$filter"", $""contains(Name,'{request.Search}')"");
        }

        var url = NavManager.GetUriWithQueryParameters(""Products/GetProducts"", query);

        var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

        var items = data!.Items.Select(i => new BitSearchBoxItem
        {
            Text = i.Name,
            Title = i.Name,
            AriaLabel = i.Name,
        }).ToList();

        return BitSearchBoxItemsProviderResult.From(items, data!.TotalCount);
    }
    catch
    {
        return BitSearchBoxItemsProviderResult.From(new List<BitSearchBoxItem>(), 0);
    }
}";
}
