namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SearchBox;

public partial class _BitSearchBoxCustomDemo
{
    private readonly string example1RazorCode = @"
<BitLabel>Basic</BitLabel>
<BitSearchBox Placeholder=""Search"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" />
    
<BitLabel>Disabled</BitLabel>
<BitSearchBox Placeholder=""Search"" IsEnabled=""false"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" />";

    private readonly string example1CsharpCode = @"
public class BitSearchBoxCustom
{
    public string? Label { get; set; }
    public string? CssClass { get; set; }
    public string? CssStyle { get; set; }
    public string? Key { get; set; }
    public string? Text { get; set; }
    public string? Title { get; set; }
    public bool IsSelected { get; set; }
}

private List<BitSearchBoxCustom> GetBasicCustoms() => new()
{
    new() { Text = ""Apple"" },
    new() { Text = ""Banana"" },
    new() { Text = ""Orange"" },
    new() { Text = ""Grape"" },
    new() { Text = ""Broccoli"" },
    new() { Text = ""Carrot"" },
    new() { Text = ""Lettuce"" }
};

private BitSearchBoxNameSelectors<BitSearchBoxCustom> nameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Class = { Selector = c => c.CssClass },
    Id = { Selector = c => c.Key },
    IsSelected = { Name = nameof(BitSearchBoxCustom.IsSelected) },
    Style = { Selector = c => c.CssStyle },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
};";

    private readonly string example2RazorCode = @"
<BitLabel>Basic Underlined SearchBox</BitLabel>
<BitSearchBox Placeholder=""Search"" IsUnderlined=""true"" IsEnabled=""false"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" />

<BitLabel>Disabled Underlined SearchBox</BitLabel>
<BitSearchBox Placeholder=""Search"" IsUnderlined=""true"" IsEnabled=""false"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" />";

    private readonly string example3RazorCode = @"
<BitLabel>SearchBox with fixed icon</BitLabel>
<BitSearchBox Placeholder=""Search"" FixedIcon=""true"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" />

<BitLabel>SearchBox without icon animation</BitLabel>
<BitSearchBox Placeholder=""Search"" DisableAnimation=""true"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" />

<BitLabel>SearchBox with custom icon</BitLabel>
<BitSearchBox Placeholder=""Search"" IconName=""@BitIconName.Filter"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" />";

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

<BitSearchBox Placeholder=""Search"" Style=""background-color: lightskyblue; border-radius: 1rem; margin: 1rem 0"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" />
<BitSearchBox Placeholder=""Search"" Class=""custom-class"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" />

<BitSearchBox Placeholder=""Search""
              Styles=""@(new() { SearchIcon = ""color: darkorange;"",
                                Input = ""padding: 0.5rem; background-color: goldenrod"" })""
              Items=""GetBasicCustoms()"" 
              NameSelectors=""nameSelectors"" />
<BitSearchBox Placeholder=""Search"" DefaultValue=""This is default value""
              Classes=""@(new() { ClearButtonIcon = ""custom-clear"",
                                 SearchIconContainer = ""custom-search"" })""
              Items=""GetBasicCustoms()"" 
              NameSelectors=""nameSelectors"" />";

    private readonly string example5RazorCode = @"
Visible: [ <BitSearchBox Visibility=""BitVisibility.Visible"" Placeholder=""Visible SearchBox"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" /> ]
Hidden: [ <BitSearchBox Visibility=""BitVisibility.Hidden"" Placeholder=""Hidden SearchBox"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" />  ]
Collapsed: [ <BitSearchBox Visibility=""BitVisibility.Collapsed"" Placeholder=""Collapsed SearchBox"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" />  ]";

    private readonly string example6RazorCode = @"
<BitLabel>Two-way Bind</BitLabel>
<BitSearchBox Placeholder=""Search"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" @bind-Value=""TwoWaySearchValue"" />
<BitTextField Placeholder=""Search Value"" Style=""margin-top: 5px;"" @bind-Value=""TwoWaySearchValue"" />

<BitLabel>OnChange</BitLabel>
<BitSearchBox Placeholder=""Search"" OnChange=""(s) => OnChangeSearchValue = s"" OnClear=""() => OnChangeSearchValue = string.Empty"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" />
<BitLabel>Search Value: @OnChangeSearchValue</BitLabel>

<BitLabel>OnSearch (Serach by Enter)</BitLabel>
<BitSearchBox Placeholder=""Search"" OnSearch=""(s) => OnSearchValue = s"" OnClear=""() => OnSearchValue = string.Empty"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" />
<BitLabel>Search Value: @OnSearchValue</BitLabel>";
    private readonly string example6CsharpCode = @"
public class BitSearchBoxCustom
{
    public string? Label { get; set; }
    public string? CssClass { get; set; }
    public string? CssStyle { get; set; }
    public string? Key { get; set; }
    public string? Text { get; set; }
    public string? Title { get; set; }
    public bool IsSelected { get; set; }
}

private List<BitSearchBoxCustom> GetBasicCustoms() => new()
{
    new() { Text = ""Apple"" },
    new() { Text = ""Banana"" },
    new() { Text = ""Orange"" },
    new() { Text = ""Grape"" },
    new() { Text = ""Broccoli"" },
    new() { Text = ""Carrot"" },
    new() { Text = ""Lettuce"" }
};

private BitSearchBoxNameSelectors<BitSearchBoxCustom> nameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Class = { Selector = c => c.CssClass },
    Id = { Selector = c => c.Key },
    IsSelected = { Name = nameof(BitSearchBoxCustom.IsSelected) },
    Style = { Selector = c => c.CssStyle },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
};

private string TwoWaySearchValue;
private string OnChangeSearchValue;
private string OnSearchValue;";

    private readonly string example7RazorCode = @"
<EditForm Model=""ValidationSearchBoxModel"">
    <DataAnnotationsValidator />
    <BitSearchBox Placeholder=""Search"" DefaultValue=""This is default value"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" @bind-Value=""ValidationSearchBoxModel.Text"" />
    <ValidationMessage For=""() => ValidationSearchBoxModel.Text"" />
</EditForm>";
    private readonly string example7CsharpCode = @"
public class ValidationSearchBoxModel
{
    [StringLength(6, MinimumLength = 2, ErrorMessage = ""The text field length must be between 6 and 2 characters in length."")]
    public string Text { get; set; }
}

public class BitSearchBoxCustom
{
    public string? Label { get; set; }
    public string? CssClass { get; set; }
    public string? CssStyle { get; set; }
    public string? Key { get; set; }
    public string? Text { get; set; }
    public string? Title { get; set; }
    public bool IsSelected { get; set; }
}

private List<BitSearchBoxCustom> GetBasicCustoms() => new()
{
    new() { Text = ""Apple"" },
    new() { Text = ""Banana"" },
    new() { Text = ""Orange"" },
    new() { Text = ""Grape"" },
    new() { Text = ""Broccoli"" },
    new() { Text = ""Carrot"" },
    new() { Text = ""Lettuce"" }
};

private BitSearchBoxNameSelectors<BitSearchBoxCustom> nameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Class = { Selector = c => c.CssClass },
    Id = { Selector = c => c.Key },
    IsSelected = { Name = nameof(BitSearchBoxCustom.IsSelected) },
    Style = { Selector = c => c.CssStyle },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
};

private ValidationSearchBoxModel ValidationSearchBoxModel = new();";

    private readonly string example8RazorCode = @"
<BitLabel>Items:</BitLabel>
<BitSearchBox Placeholder=""e.g. Apple"" Items=""GetBasicCustoms()"" NameSelectors=""nameSelectors"" @bind-Value=""@SearchValue"" />
<br />
SearchValue: @SearchValue

<BitLabel>ItemsProvider:</BitLabel>
<BitSearchBox Placeholder=""e.g. Pro""
              Virtualize=""true""
              ItemsProvider=""LoadItems""
              NameSelectors=""nameSelectors""
              @bind-Value=""@ItemsProviderSearchValue"" />
<br />
SearchValue: @ItemsProviderSearchValue";

    private readonly string example8CsharpCode = @"
public class BitSearchBoxCustom
{
    public string? Label { get; set; }
    public string? CssClass { get; set; }
    public string? CssStyle { get; set; }
    public string? Key { get; set; }
    public string? Text { get; set; }
    public string? Title { get; set; }
    public bool IsSelected { get; set; }
}

private List<BitSearchBoxCustom> GetBasicCustoms() => new()
{
    new() { Text = ""Apple"" },
    new() { Text = ""Banana"" },
    new() { Text = ""Orange"" },
    new() { Text = ""Grape"" },
    new() { Text = ""Broccoli"" },
    new() { Text = ""Carrot"" },
    new() { Text = ""Lettuce"" }
};

private BitSearchBoxNameSelectors<BitSearchBoxCustom> nameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Class = { Selector = c => c.CssClass },
    Id = { Selector = c => c.Key },
    IsSelected = { Name = nameof(BitSearchBoxCustom.IsSelected) },
    Style = { Selector = c => c.CssStyle },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
};

private string SearchValue;
private string ItemsProviderSearchValue;

private async ValueTask<BitSearchBoxItemsProviderResult<BitSearchBoxCustom>> LoadItems(BitSearchBoxItemsProviderRequest<BitSearchBoxCustom> request)
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

        var items = data!.Items.Select(i => new BitSearchBoxCustom
        {
            Text = i.Name,
            Label = i.Name,
        }).ToList();

        return BitSearchBoxItemsProviderResult.From(items, data!.TotalCount);
    }
    catch
    {
        return BitSearchBoxItemsProviderResult.From(new List<BitSearchBoxCustom>(), 0);
    }
}";
}
