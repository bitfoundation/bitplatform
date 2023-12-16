namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SearchBox;

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
            Name = "Classes",
            Type = "BitSearchBoxClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#searchbox-class-styles",
            Description = "Custom CSS classes for different parts of the BitSearchBox.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the SearchBox, a list of BitSearchBoxOption components.",
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
            Name = "Items",
            Type = "ICollection<TItem>?",
            DefaultValue = "null",
            Description = "The list of items to display in the callout."
        },
        new()
        {
            Name = "ItemSize",
            Type = "int",
            DefaultValue = "35",
            Description = "The height of each item in pixels for virtualization.",
        },
        new()
        {
            Name = "ItemsProvider",
            Type = "BitSearchBoxItemsProvider<TItem>?",
            DefaultValue = "null",
            Description = "The function providing items to the list for virtualization.",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template for rendering the items of the BitSearchBox.",
        },
        new()
        {
            Name = "MaxSuggestedItems",
            Type = "int",
            DefaultValue = "5",
            Description = "The maximum number of items or suggestions that will be displayed.",
        },
        new()
        {
            Name = "MinSearchLength",
            Type = "int",
            DefaultValue = "3",
            Description = "The minimum character requirement for doing a search in suggested items.",
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitSearchBoxNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors"
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
            Name = "Options",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of ChildContent.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "Placeholder for the search box.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitSearchBoxClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#searchbox-class-styles",
            Description = "Custom CSS styles for different parts of the BitSearchBox.",
        },
        new()
        {
            Name = "SearchFunction",
            Type = "Func<ICollection<BitSearchBoxSuggestedItem>, string, ICollection<BitSearchBoxSuggestedItem>>?",
            DefaultValue = "null",
            Description = "Custom search function to be used in place of the default search algorithm.",
        },
        new()
        {
            Name = "SearchDelay",
            Type = "int",
            DefaultValue = "400",
            Description = "The delay, in milliseconds, applied to the search functionality.",
        },
        new()
        {
            Name = "Virtualize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables virtualization to render only the visible items.",
        },
        new()
        {
            Name = "VirtualizePlaceholder",
            Type = "RenderFragment<PlaceholderContext>?",
            DefaultValue = "null",
            Description = "The template for items that have not yet been rendered in virtualization mode.",
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "searchbox-class-styles",
            Title = "BitSearchBoxClassStyles",
            Description = "",
            Parameters = new()
            {
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the search box.",
                },
                new()
                {
                    Name = "ClearButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's clear button.",
                },
                new()
                {
                    Name = "ClearButtonContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's clear button container.",
                },
                new()
                {
                    Name = "ClearButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's clear button icon.",
                },
                new()
                {
                    Name = "ClearButtonIconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's clear button icon container.",
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's Input.",
                },
                new()
                {
                    Name = "SearchIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box search icon.",
                },
                new()
                {
                    Name = "SearchIconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's search icon container.",
                }
            }
        }
    };

    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;

    private string TwoWaySearchValue;
    private string OnChangeSearchValue;
    private string OnSearchValue;
    private string SearchValue;
    private string SearchValueWithSearchDelay;
    private string SearchValueWithMinSearchLength;
    private string SearchValueWithMaxSuggestedItems;
    private string ItemsProviderSearchValue;

    private readonly ValidationSearchBoxModel ValidationSearchBoxModel = new();

    private List<string> GetSuggestedItems() =>
    [
         "Apple",
         "Red Apple",
         "Blue Apple",
         "Green Apple",
         "Banana",
         "Orange",
         "Grape",
         "Broccoli",
         "Carrot",
         "Lettuce"
    ];

    private async ValueTask<BitSearchBoxSuggestedItemsProviderResult<string>> LoadItems(BitSearchBoxSuggestedItemsProviderRequest<string> request)
    {
        try
        {
            // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview

            var query = new Dictionary<string, object?>()
            {
                { "$top", request.Count == 0 ? 5 : request.Count },
                { "$skip", request.StartIndex }
            };

            if (string.IsNullOrEmpty(request.Search) is false)
            {
                query.Add("$filter", $"contains(Name,'{request.Search}')");
            }

            var url = NavManager.GetUriWithQueryParameters("Products/GetProducts", query);

            var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

            var items = data!.Items.Select(i => i.Name).ToList();

            return BitSearchBoxSuggestedItemsProviderResult.From(items, data!.TotalCount);
        }
        catch
        {
            return BitSearchBoxSuggestedItemsProviderResult.From(new List<string>(), 0);
        }
    }

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
<BitLabel>Items:</BitLabel>
<BitSearchBox Placeholder=""e.g. Apple"" SuggestedItems=""GetSuggestedItems()"" @bind-Value=""@SearchValue"" />
SearchValue: @SearchValue

<BitLabel>MinSearchLength equals 1:</BitLabel>
<BitSearchBox Placeholder=""e.g. Apple"" SuggestedItems=""GetSuggestedItems()"" MinSearchLength=""1"" @bind-Value=""@SearchValueWithMinSearchLength"" />
SearchValue: @SearchValueWithMinSearchLength

<BitLabel>MaxSuggestedItems equals 2:</BitLabel>
<BitSearchBox Placeholder=""e.g. Apple"" SuggestedItems=""GetSuggestedItems()"" MaxSuggestedItems=""2"" @bind-Value=""@SearchValueWithMaxSuggestedItems"" />
SearchValue: @SearchValueWithMaxSuggestedItems

<BitLabel>SearchDelay equals 2 seconds:</BitLabel>
<BitSearchBox Placeholder=""e.g. Apple"" SuggestedItems=""GetSuggestedItems()"" SearchDelay=""2000"" @bind-Value=""@SearchValueWithSearchDelay"" />
SearchValue: @SearchValueWithSearchDelay

<BitLabel>ItemsProvider:</BitLabel>
<BitSearchBox Placeholder=""e.g. Pro""
                Virtualize=""true""
                SuggestedItemsProvider=""LoadItems""
                @bind-Value=""@ItemsProviderSearchValue"" />
SearchValue: @ItemsProviderSearchValue";
    private readonly string example8CsharpCode = @"
private string SearchValue;
private string SearchValueWithSearchDelay;
private string SearchValueWithMinSearchLength;
private string SearchValueWithMaxSuggestedItems;
private string ItemsProviderSearchValue;

private List<string> GetSuggestedItems() =>
[
        ""Apple"",
        ""Red Apple"",
        ""Blue Apple"",
        ""Green Apple"",
        ""Banana"",
        ""Orange"",
        ""Grape"",
        ""Broccoli"",
        ""Carrot"",
        ""Lettuce""
];

private async ValueTask<BitSearchBoxSuggestedItemsProviderResult<string>> LoadItems(BitSearchBoxSuggestedItemsProviderRequest<string> request)
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

        var items = data!.Items.Select(i => i.Name).ToList();

        return BitSearchBoxSuggestedItemsProviderResult.From(items, data!.TotalCount);
    }
    catch
    {
        return BitSearchBoxSuggestedItemsProviderResult.From(new List<string>(), 0);
    }
}";
}
