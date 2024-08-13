using System.Diagnostics.Metrics;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SearchBox;

public partial class BitSearchBoxDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Name = "HideIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the icon is visible.",
        },
        new()
        {
            Name = "HideClearButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to hide the clear button when the BitSearchBox has value.",
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
            Name = "SuggestItems",
            Type = "ICollection<string>?",
            DefaultValue = "null",
            Description = "The list of suggest items to display in the callout."
        },
        new()
        {
            Name = "SuggestItemProvider",
            Type = "Func<string?, int, Task<ICollection<string>>>?",
            DefaultValue = "null",
            Description = "The function providing suggest items.",
        },
        new()
        {
            Name = "SuggestItemTemplate",
            Type = "RenderFragment<string>?",
            DefaultValue = "null",
            Description = "The custom template for rendering the suggest items of the BitSearchBox.",
        },
        new()
        {
            Name = "MaxSuggestCount",
            Type = "int",
            DefaultValue = "5",
            Description = "The maximum number of items or suggestions that will be displayed.",
        },
        new()
        {
            Name = "MinSuggestTriggerChars",
            Type = "int",
            DefaultValue = "3",
            Description = "The minimum character requirement for doing a search in suggested items.",
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
            Name = "SuggestFilterFunction",
            Type = "Func<string?, string?, bool>?",
            DefaultValue = "null",
            Description = "Custom search function to be used in place of the default search algorithm.",
        },
        new()
        {
            Name = "SuggestThrottleTime",
            Type = "int",
            DefaultValue = "400",
            Description = "The delay, in milliseconds, applied to the search functionality.",
        },
        new()
        {
            Name = "SearchButtonIconName",
            Type = "string",
            DefaultValue = "ChromeBackMirrored",
            Description = "Custom icon name for the search button.",
        },
        new()
        {
            Name = "ShowSearchButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show the search button.",
        }
    ];
    private readonly List<ComponentSubClass> componentSubClasses =
    [
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
                    Name = "InputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input container of the BitSearchBox.",
                },
                new()
                {
                    Name = "Focused",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the focus state of the BitSearchBox.",
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
    ];
    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "InputElement",
            Type = "ElementReference",
            Description = "The ElementReference to the input element of the BitSearchBox.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives focus to the input element of the BitSearchBox.",
        }
    ];

    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;

    private string? TwoWaySearchValue;
    private string? OnChangeSearchValue;
    private string? OnSearchValue;
    private string? SearchValue;
    private string? SearchValueWithSuggestFilterFunction;
    private string? SearchValueWithSearchDelay;
    private string? SearchValueWithMinSearchLength;
    private string? SearchValueWithMaxSuggestedItems;
    private string? ItemsProviderSearchValue;

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

    private Func<string, string, bool> SearchFunc = (string searchText, string itemText) =>
    {
        if (string.IsNullOrEmpty(searchText) || string.IsNullOrEmpty(itemText)) return false;

        return itemText.StartsWith(searchText, StringComparison.OrdinalIgnoreCase);
    };

    private async Task<ICollection<string>> LoadItems(string? search, int count)
    {
        try
        {
            // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview

            var query = new Dictionary<string, object?>()
            {
                { "$top", count < 1 ? 5 : count },
            };

            if (string.IsNullOrEmpty(search) is false)
            {
                query.Add("$filter", $"contains(Name,'{search}')");
            }

            var url = NavManager.GetUriWithQueryParameters("Products/GetProducts", query);

            var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

            return data!.Items!.Select(i => i.Name)!.ToList()!;
        }
        catch
        {
            return new List<string>();
        }
    }

    private readonly string example1RazorCode = @"
<BitLabel>Basic</BitLabel>
<BitSearchBox Placeholder=""Search"" />
    
<BitLabel>Disabled</BitLabel>
<BitSearchBox Placeholder=""Search"" IsEnabled=""false"" />";

    private readonly string example2RazorCode = @"
<BitLabel>Basic Underlined SearchBox</BitLabel>
<BitSearchBox Placeholder=""Search"" IsUnderlined=""true"" />

<BitLabel>Disabled Underlined SearchBox</BitLabel>
<BitSearchBox Placeholder=""Search"" IsUnderlined=""true"" IsEnabled=""false"" />";

    private readonly string example3RazorCode = @"
<BitLabel>SearchBox with fixed icon</BitLabel>
<BitSearchBox Placeholder=""Search"" FixedIcon=""true"" />

<BitLabel>SearchBox without icon animation</BitLabel>
<BitSearchBox Placeholder=""Search"" DisableAnimation=""true"" />

<BitLabel>SearchBox with custom icon</BitLabel>
<BitSearchBox Placeholder=""Search"" IconName=""@BitIconName.Filter"" />

<BitLabel>SearchBox without icon</BitLabel>
<BitSearchBox Placeholder=""Search"" HideIcon />";

    private readonly string example4RazorCode = @"
<BitLabel>Basic SearchBox</BitLabel>
<BitSearchBox  Placeholder=""Search"" ShowSearchButton=""true"" />
<br />
<BitLabel>Basic Underlined SearchBox</BitLabel>
<BitSearchBox Placeholder=""Search"" IsUnderlined=""true"" ShowSearchButton=""true"" />
<br />
<BitLabel>SearchBox with custom button icon</BitLabel>
<BitSearchBox Placeholder=""Search"" ShowSearchButton=""true"" SearchButtonIconName=""PageListFilter"" />
<br />
<BitLabel>Disabled Basic SearchBox</BitLabel>
<BitSearchBox Placeholder=""Search"" IsEnabled=""false"" ShowSearchButton=""true"" />
<br />
<BitLabel>Disabled Underlined SearchBox</BitLabel>
<BitSearchBox Placeholder=""Search"" IsUnderlined=""true"" IsEnabled=""false"" ShowSearchButton=""true"" />";

    private readonly string example5RazorCode = @"
<style>
    .custom-class {
        overflow: hidden;
        margin-inline: 1rem;
        border-radius: 1rem;
        border: 2px solid tomato;
    }

    .custom-class * {
        border: none;
    }


    .custom-root {
        margin-inline: 1rem;
    }

    .custom-input-container {
        height: 3rem;
        overflow: hidden;
        align-items: center;
        border-radius: 1.5rem;
        border-color: #13a3a375;
        background-color: #13a3a375;
    }

    .custom-focused .custom-input-container {
        border-width: 1px;
        border-color: #13a3a375;
    }

    .custom-clear:hover {
        background: transparent;
    }

    .custom-icon {
        color: darkslategrey;
    }

    .custom-icon-wrapper {
        width: 2rem;
        height: 2rem;
        border-radius: 1rem;
        margin-inline: 0.5rem;
        background-color: whitesmoke;
    }
    }
</style>


<BitSearchBox Placeholder=""Search"" Style=""box-shadow: dodgerblue 0 0 1rem; margin-inline: 1rem;"" />

<BitSearchBox Placeholder=""Search"" Class=""custom-class"" />


<BitSearchBox Placeholder=""Search""
              Styles=""@(new() { Root = ""margin-inline: 1rem;"",
                                Focused = ""--focused-background: #b2b2b25a;"",
                                Input = ""padding: 0.5rem;"",
                                InputContainer = ""background: var(--focused-background);"" })"" />

<BitSearchBox FixedIcon
              Placeholder=""Search"" DefaultValue=""This is default value""
              Classes=""@(new() { Root = ""custom-root"",
                                 Icon = ""custom-icon"",
                                 Focused = ""custom-focused"",
                                 ClearButton = ""custom-clear"",
                                 IconWrapper = ""custom-icon-wrapper"",
                                 InputContainer = ""custom-input-container"" })"" />";

    private readonly string example6RazorCode = @"
Visible: [ <BitSearchBox Visibility=""BitVisibility.Visible"" Placeholder=""Visible SearchBox"" /> ]
Hidden: [ <BitSearchBox Visibility=""BitVisibility.Hidden"" Placeholder=""Hidden SearchBox"" />  ]
Collapsed: [ <BitSearchBox Visibility=""BitVisibility.Collapsed"" Placeholder=""Collapsed SearchBox"" />  ]";

    private readonly string example7RazorCode = @"
<BitLabel>Two-way Bind</BitLabel>
<BitSearchBox Placeholder=""Search"" @bind-Value=""TwoWaySearchValue"" />
<BitTextField Placeholder=""Search Value"" Style=""margin-top: 5px;"" @bind-Value=""TwoWaySearchValue"" />

<BitLabel>OnChange</BitLabel>
<BitSearchBox Placeholder=""Search"" OnChange=""(s) => OnChangeSearchValue = s"" OnClear=""() => OnChangeSearchValue = string.Empty"" />
<BitLabel>Search Value: @OnChangeSearchValue</BitLabel>

<BitLabel>OnSearch (Serach by Enter)</BitLabel>
<BitSearchBox Placeholder=""Search"" OnSearch=""(s) => OnSearchValue = s"" OnClear=""() => OnSearchValue = string.Empty"" />
<BitLabel>Search Value: @OnSearchValue</BitLabel>";
    private readonly string example7CsharpCode = @"
private string TwoWaySearchValue;
private string OnChangeSearchValue;
private string OnSearchValue;";

    private readonly string example8RazorCode = @"
<EditForm Model=""ValidationSearchBoxModel"">
    <DataAnnotationsValidator />
    <BitSearchBox Placeholder=""Search"" DefaultValue=""This is default value"" @bind-Value=""ValidationSearchBoxModel.Text"" />
    <ValidationMessage For=""() => ValidationSearchBoxModel.Text"" />
</EditForm>";
    private readonly string example8CsharpCode = @"
public class ValidationSearchBoxModel
{
    [StringLength(6, MinimumLength = 2, ErrorMessage = ""The text field length must be between 6 and 2 characters in length."")]
    public string Text { get; set; }
}

private ValidationSearchBoxModel ValidationSearchBoxModel = new();";

    private readonly string example9RazorCode = @"
<BitLabel>Items:</BitLabel>
<BitSearchBox Placeholder=""e.g. Apple"" SuggestItems=""GetSuggestedItems()"" @bind-Value=""@SearchValue"" />
SearchValue: @SearchValue
                    

<BitLabel>SuggestFilterFunction (use StartsWith):</BitLabel>
<BitSearchBox Placeholder=""e.g. Apple"" SuggestItems=""GetSuggestedItems()"" SuggestFilterFunction=""@SearchFunc"" @bind-Value=""@SearchValueWithSuggestFilterFunction"" />
SearchValue: @SearchValueWithSuggestFilterFunction
                    

<BitLabel>MinSuggestTriggerChars equals 1:</BitLabel>
<BitSearchBox Placeholder=""e.g. Apple"" SuggestItems=""GetSuggestedItems()"" MinSuggestTriggerChars=""1"" @bind-Value=""@SearchValueWithMinSearchLength"" />
SearchValue: @SearchValueWithMinSearchLength
                    

<BitLabel>MaxSuggestCount equals 2:</BitLabel>
<BitSearchBox Placeholder=""e.g. Apple"" SuggestItems=""GetSuggestedItems()"" MaxSuggestCount=""2"" @bind-Value=""@SearchValueWithMaxSuggestedItems"" />
SearchValue: @SearchValueWithMaxSuggestedItems
                        
<BitLabel>SuggestThrottleTime equals 2 seconds:</BitLabel>
<BitSearchBox Placeholder=""e.g. Apple"" SuggestItems=""GetSuggestedItems()"" SuggestThrottleTime=""2000"" @bind-Value=""@SearchValueWithSearchDelay"" />
SearchValue: @SearchValueWithSearchDelay

<BitLabel>ItemsProvider:</BitLabel>
<BitSearchBox Placeholder=""e.g. Pro""
              SuggestItemProvider=""LoadItems""
              @bind-Value=""@ItemsProviderSearchValue"" />
SearchValue: @ItemsProviderSearchValue";
    private readonly string example9CsharpCode = @"
private string SearchValue;
private string SearchValueWithSuggestFilterFunction;
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

private Func<string, string, bool> SearchFunc = (string searchText, string itemText) =>
{
    if (string.IsNullOrEmpty(searchText) || string.IsNullOrEmpty(itemText)) return false;

    return itemText.StartsWith(searchText, StringComparison.OrdinalIgnoreCase);
};

private async Task<ICollection<string>> LoadItems(string? search, int count)
{
    try
    {
        var query = new Dictionary<string, object?>()
        {
            { ""$top"", count < 1 ? 5 : count },
        };

        if (string.IsNullOrEmpty(search) is false)
        {
            query.Add(""$filter"", $""contains(Name,'{search}')"");
        }

        var url = NavManager.GetUriWithQueryParameters(""Products/GetProducts"", query);

        var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

        return data!.Items.Select(i => i.Name).ToList();
    }
    catch
    {
        return new List<string>();
    }
}";

    private readonly string example10RazorCode = @"
<BitSearchBox Dir=""BitDir.Rtl"" Placeholder=""جستجو"" />
<BitSearchBox IsUnderlined Dir=""BitDir.Rtl"" Placeholder=""جستجو"" />
<BitSearchBox ShowSearchButton
              Dir=""BitDir.Rtl""
              Placeholder=""جستجو"" />";
}
