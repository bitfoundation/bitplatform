using System.Diagnostics.Metrics;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SearchBox;

public partial class BitSearchBoxDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Accent",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The accent color kind of the search box.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "Classes",
            Type = "BitSearchBoxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitSearchBox.",
            LinkType = LinkType.Link,
            Href = "#searchbox-class-styles",
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
            Name = "IconName",
            Type = "string",
            DefaultValue = "Search",
            Description = "The icon name for the icon shown at the beginning of the search box.",
        },
        new()
        {
            Name = "InputMode",
            Type = "BitInputMode?",
            DefaultValue = "null",
            Description = "Sets the inputmode html attribute of the input element.",
            LinkType = LinkType.Link,
            Href = "#input-mode",
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
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default border of the search box.",
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
            Name = "SuggestItems",
            Type = "ICollection<string>?",
            DefaultValue = "null",
            Description = "The list of suggest items to display in the callout."
        },
        new()
        {
            Name = "SuggestItemsProvider",
            Type = "BitSearchBoxSuggestItemsProvider?",
            DefaultValue = "null",
            Description = "The item provider function providing suggest items.",
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
            Name = "Underlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the SearchBox is underlined.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "searchbox-class-styles",
            Title = "BitSearchBoxClassStyles",
            Description = "",
            Parameters =
            [
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
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-kind-enum",
            Name = "BitColorKind",
            Description = "Defines the color kinds available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name = "Primary",
                    Description = "The primary color kind.",
                    Value = "0",
                },
                new()
                {
                    Name = "Secondary",
                    Description = "The secondary color kind.",
                    Value = "1",
                },
                new()
                {
                    Name = "Tertiary",
                    Description = "The tertiary color kind.",
                    Value = "2",
                },
                new()
                {
                    Name = "Transparent",
                    Description = "The transparent color kind.",
                    Value = "3",
                },
            ]
        },
        new()
        {
            Id = "input-mode",
            Name = "BitInputMode",
            Description = "This allows a browser to display an appropriate virtual keyboard.",
            Items =
            [
                new()
                {
                    Name= "None",
                    Description="The input expects text characters.",
                    Value="0",
                },
                new()
                {
                    Name= "Text",
                    Description="Standard input keyboard for the user's current locale.",
                    Value="1",
                },
                new()
                {
                    Name= "Decimal",
                    Description="Fractional numeric input keyboard containing the digits and decimal separator for the user's locale.",
                    Value="2",
                },
                new()
                {
                    Name= "Numeric",
                    Description="Numeric input keyboard, but only requires the digits 0–9.",
                    Value="3",
                },
                new()
                {
                    Name= "Tel",
                    Description="A telephone keypad input, including the digits 0–9, the asterisk (*), and the pound (#) key",
                    Value="4",
                },
                new()
                {
                    Name= "Search",
                    Description="A virtual keyboard optimized for search input.",
                    Value="5",
                },
                new()
                {
                    Name= "Email",
                    Description="A virtual keyboard optimized for entering email addresses.",
                    Value="6",
                },
                new()
                {
                    Name= "Url",
                    Description="A keypad optimized for entering URLs.",
                    Value="7",
                }
            ]
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

    private string? twoWaySearchValue;
    private string? immediateTwoWaySearchValue;
    private string? onChangeSearchValue;
    private string? onSearchValue;

    private string? searchValue;
    private string? searchValueWithSuggestFilterFunction;
    private string? searchValueWithSearchDelay;
    private string? searchValueWithMinSearchLength;
    private string? searchValueWithMaxSuggestedItems;
    private string? searchValueWithItemsProvider;

    private readonly ValidationSearchBoxModel validationModel = new();

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

    private async ValueTask<IEnumerable<string>> LoadItems(BitSearchBoxSuggestItemsProviderRequest request)
    {
        try
        {
            // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview

            var query = new Dictionary<string, object?>()
            {
                { "$top", request.Take < 1 ? 5 : request.Take },
            };

            if (string.IsNullOrEmpty(request.SearchTerm) is false)
            {
                query.Add("$filter", $"contains(toupper(Name),'{request.SearchTerm.ToUpper()}')");
            }

            var url = NavManager.GetUriWithQueryParameters("Products/GetProducts", query);

            var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto, request.CancellationToken);

            return data!.Items!.Select(i => i.Name)!;
        }
        catch
        {
            return [];
        }
    }



    private readonly string example1RazorCode = @"
<BitSearchBox Placeholder=""Search"" />
<BitSearchBox Placeholder=""Disabled"" IsEnabled=""false"" />";

    private readonly string example2RazorCode = @"
<BitSearchBox Placeholder=""Search"" Underlined />
<BitSearchBox Placeholder=""Disabled"" IsEnabled=""false"" Underlined />";

    private readonly string example3RazorCode = @"
<BitSearchBox Placeholder=""Search"" NoBorder/>
<BitSearchBox Placeholder=""Disabled"" IsEnabled=""false"" NoBorder/>";

    private readonly string example4RazorCode = @"
<BitSearchBox Placeholder=""Primary"" Accent=""BitColorKind.Primary"" NoBorder/>
<BitSearchBox Placeholder=""Secondary"" Accent=""BitColorKind.Secondary"" NoBorder/>
<BitSearchBox Placeholder=""Tertiary"" Accent=""BitColorKind.Tertiary"" NoBorder/>
<BitSearchBox Placeholder=""Transparent"" Accent=""BitColorKind.Transparent"" NoBorder/>";

    private readonly string example5RazorCode = @"
<BitSearchBox Placeholder=""FixedIcon"" FixedIcon />
<BitSearchBox Placeholder=""DisableAnimation"" DisableAnimation />
<BitSearchBox Placeholder=""Custom icon"" IconName=""@BitIconName.Filter"" />
<BitSearchBox Placeholder=""HideIcon"" HideIcon />";

    private readonly string example6RazorCode = @"
<BitSearchBox Placeholder=""Search"" ShowSearchButton />
<BitSearchBox Placeholder=""SearchButtonIconName"" ShowSearchButton SearchButtonIconName=""PageListFilter"" />
<BitSearchBox Placeholder=""Underlined"" Underlined ShowSearchButton />
<BitSearchBox Placeholder=""Disabled"" IsEnabled=""false"" ShowSearchButton />
<BitSearchBox Placeholder=""Disabled Underlined"" IsEnabled=""false"" Underlined ShowSearchButton />";

    private readonly string example7RazorCode = @"
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
</style>


<BitSearchBox Placeholder=""Style"" Style=""box-shadow: dodgerblue 0 0 1rem; margin-inline: 1rem;"" />

<BitSearchBox Placeholder=""Class"" Class=""custom-class"" />

<BitSearchBox Placeholder=""Styles""
              Styles=""@(new() { Root = ""margin-inline: 1rem;"",
                                Input = ""padding: 0.5rem;"",
                                Focused = ""--focused-background: #b2b2b25a;"",
                                InputContainer = ""background: var(--focused-background);"" })"" />

<BitSearchBox FixedIcon
              Placeholder=""Classes""
              Classes=""@(new() { Root = ""custom-root"",
                                 Icon = ""custom-icon"",
                                 Focused = ""custom-focused"",
                                 ClearButton = ""custom-clear"",
                                 IconWrapper = ""custom-icon-wrapper"",
                                 InputContainer = ""custom-input-container"" })"" />";

    private readonly string example8RazorCode = @"
<BitSearchBox Placeholder=""Search"" @bind-Value=""twoWaySearchValue"" />
<BitTextField Placeholder=""Search Value"" @bind-Value=""twoWaySearchValue"" />


<BitSearchBox Placeholder=""Search"" Immediate @bind-Value=""immediateTwoWaySearchValue"" />
<BitTextField Placeholder=""Search Value"" Immediate @bind-Value=""immediateTwoWaySearchValue"" />


<BitSearchBox Placeholder=""Search"" Immediate
              OnChange=""s => onChangeSearchValue = s""
              OnClear=""() => onChangeSearchValue = string.Empty"" />
<div>Search Value: @onChangeSearchValue</div>


<BitSearchBox Placeholder=""Search"" Immediate
              OnSearch=""s => onSearchValue = s""
              OnClear=""() => onSearchValue = string.Empty"" />
<div>Search Value: @onSearchValue</div>";
    private readonly string example8CsharpCode = @"
private string twoWaySearchValue;
private string onChangeSearchValue;
private string onSearchValue;";

    private readonly string example9RazorCode = @"
<EditForm Model=""validationBoxModel"">
    <DataAnnotationsValidator />
    <BitSearchBox Placeholder=""Search"" Immediate
                  DefaultValue=""This is default value""
                  @bind-Value=""validationBoxModel.Text"" />
    <ValidationMessage For=""() => validationBoxModel.Text"" />
</EditForm>";
    private readonly string example9CsharpCode = @"
public class ValidationSearchBoxModel
{
    [StringLength(6, MinimumLength = 2, ErrorMessage = ""Text must be between 2 and 6 chars."")]
    public string Text { get; set; }
}

private ValidationSearchBoxModel validationBoxModel = new();";

    private readonly string example10RazorCode = @"
<BitSearchBox @bind-Value=""@searchValue""
              Immediate
              Placeholder=""e.g. Apple""
              SuggestItems=""GetSuggestedItems()"" />


<BitSearchBox @bind-Value=""@searchValueWithSuggestFilterFunction""
              Immediate
              Placeholder=""e.g. Apple""
              SuggestItems=""GetSuggestedItems()""
              SuggestFilterFunction=""@SearchFunc"" />


<BitSearchBox @bind-Value=""@searchValueWithMinSearchLength""
              Immediate
              Placeholder=""e.g. Apple""
              MinSuggestTriggerChars=""1""
              SuggestItems=""GetSuggestedItems()"" />


<BitSearchBox @bind-Value=""@searchValueWithMaxSuggestedItems""
              Immediate
              Placeholder=""e.g. Apple""
              MaxSuggestCount=""2""
              SuggestItems=""GetSuggestedItems()"" />


<BitSearchBox @bind-Value=""@searchValueWithSearchDelay""
              Immediate
              DebounceTime=""2000""
              Placeholder=""e.g. Apple""
              SuggestItems=""GetSuggestedItems()"" />


<BitSearchBox @bind-Value=""@searchValueWithItemsProvider""
              Immediate
              Placeholder=""e.g. Pro""
              SuggestItemsProvider=""LoadItems"" />
<div>SearchValue: @searchValueWithItemsProvider</div>";
    private readonly string example10CsharpCode = @"
private string searchValue;
private string searchValueWithSuggestFilterFunction;
private string searchValueWithSearchDelay;
private string searchValueWithMinSearchLength;
private string searchValueWithMaxSuggestedItems;
private string searchValueWithItemsProvider;

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

private async ValueTask<IEnumerable<string>> LoadItems(BitSearchBoxSuggestItemsProviderRequest request)
{
    try
    {
        var query = new Dictionary<string, object?>()
        {
            { ""$top"", request.Take < 1 ? 5 : request.Take },
        };

        if (string.IsNullOrEmpty(request.SearchTerm) is false)
        {
            query.Add(""$filter"", $""contains(toupper(Name),'{request.SearchTerm.ToUpper()}')"");
        }

        var url = NavManager.GetUriWithQueryParameters(""Products/GetProducts"", query);

        var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto, request.CancellationToken);

        return data!.Items!.Select(i => i.Name)!;
    }
    catch
    {
        return [];
    }
}";

    private readonly string example11RazorCode = @"
<BitSearchBox Placeholder=""جستجو"" Dir=""BitDir.Rtl"" />
<BitSearchBox Placeholder=""جستجو"" Dir=""BitDir.Rtl"" ShowSearchButton />
<BitSearchBox Placeholder=""جستجو"" Dir=""BitDir.Rtl"" Underlined />";
}
