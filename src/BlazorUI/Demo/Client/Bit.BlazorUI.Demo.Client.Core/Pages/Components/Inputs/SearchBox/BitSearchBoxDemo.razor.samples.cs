namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SearchBox;

public partial class BitSearchBoxDemo
{
    private readonly string example1RazorCode = @"
<BitSearchBox Placeholder=""Search"" />
<BitSearchBox Placeholder=""ReadOnly"" ReadOnly />
<BitSearchBox Placeholder=""Disabled"" IsEnabled=""false"" />";

    private readonly string example2RazorCode = @"
<BitSearchBox Placeholder=""Search"" Underlined />
<BitSearchBox Placeholder=""Disabled"" IsEnabled=""false"" Underlined />";

    private readonly string example3RazorCode = @"
<BitSearchBox Placeholder=""Search"" NoBorder/>
<BitSearchBox Placeholder=""Disabled"" IsEnabled=""false"" NoBorder/>";

    private readonly string example4RazorCode = @"
<BitSearchBox Placeholder=""Primary"" Background=""BitColorKind.Primary"" NoBorder/>
<BitSearchBox Placeholder=""Secondary"" Background=""BitColorKind.Secondary"" NoBorder/>
<BitSearchBox Placeholder=""Tertiary"" Background=""BitColorKind.Tertiary"" NoBorder/>
<BitSearchBox Placeholder=""Transparent"" Background=""BitColorKind.Transparent"" NoBorder/>";

    private readonly string example5RazorCode = @"
<BitSearchBox Placeholder=""FixedIcon"" FixedIcon />
<BitSearchBox Placeholder=""DisableAnimation"" DisableAnimation />
<BitSearchBox Placeholder=""Custom icon"" IconName=""@BitIconName.Filter"" />
<BitSearchBox Placeholder=""HideIcon"" HideIcon />";

    private readonly string example6RazorCode = @"
<BitSearchBox Placeholder=""Search"" ShowSearchButton />
<BitSearchBox Placeholder=""SearchButtonIconName"" ShowSearchButton SearchButtonIconName=""PageListFilter"" />
<BitSearchBox Placeholder=""SearchButtonTemplate"" ShowSearchButton>
    <SearchButtonTemplate>
        <BitIcon IconName=""@BitIconName.SearchAndApps"" />
    </SearchButtonTemplate>
</BitSearchBox>
<BitSearchBox Placeholder=""Underlined"" Underlined ShowSearchButton />
<BitSearchBox Placeholder=""Disabled"" IsEnabled=""false"" ShowSearchButton />
<BitSearchBox Placeholder=""Disabled Underlined"" IsEnabled=""false"" Underlined ShowSearchButton />";

    private readonly string example7RazorCode = @"
<BitSearchBox Placeholder=""HideClearButton"" HideClearButton />

<BitSearchBox Placeholder=""ClearButtonTemplate"">
    <ClearButtonTemplate>
        <BitIcon IconName=""@BitIconName.RemoveFilter"" />
    </ClearButtonTemplate>
</BitSearchBox>";

    private readonly string example8RazorCode = @"
<BitSearchBox Placeholder=""Prefix"" Prefix=""https://"" HideIcon />

<BitSearchBox Placeholder=""Suffix"" Suffix="".com"" HideIcon />

<BitSearchBox Placeholder=""Prefix & Suffix + Icon"" Prefix=""https://"" Suffix="".com"" />

<BitSearchBox Placeholder=""Templates"" HideIcon>
    <PrefixTemplate>
        <BitStack FitWidth Alignment=""BitAlignment.Center"" style=""background:gray;padding:0 0.5rem;"">
            <BitIcon IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryForeground"" />
        </BitStack>
    </PrefixTemplate>
    <SuffixTemplate>
        <BitStack FitWidth Alignment=""BitAlignment.Center"" style=""background:cadetblue;padding:0 0.5rem;"">
            <BitIcon IconName=""@BitIconName.ArrowTallUpRight"" Color=""BitColor.PrimaryForeground"" />
        </BitStack>
    </SuffixTemplate>
</BitSearchBox>";

    private readonly string example9RazorCode = @"
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
    private readonly string example9CsharpCode = @"
private string twoWaySearchValue;
private string onChangeSearchValue;
private string onSearchValue;";

    private readonly string example10RazorCode = @"
<BitSearchBox @bind-Value=""@searchValue""
              Immediate
              Placeholder=""e.g. app""
              SuggestItems=""GetSuggestedItems()"" />


<BitSearchBox @bind-Value=""@searchValueWithSuggestFilterFunction""
              Immediate
              Placeholder=""e.g. app""
              SuggestItems=""GetSuggestedItems()""
              SuggestFilterFunction=""@SearchFunc"" />


<BitSearchBox @bind-Value=""@searchValueWithMinSearchLength""
              Immediate
              Placeholder=""e.g. app""
              MinSuggestTriggerChars=""1""
              SuggestItems=""GetSuggestedItems()"" />


<BitSearchBox @bind-Value=""@searchValueWithMaxSuggestedItems""
              Immediate
              Placeholder=""e.g. app""
              MaxSuggestCount=""2""
              SuggestItems=""GetSuggestedItems()"" />


<BitSearchBox @bind-Value=""@searchValueWithSearchDelay""
              Immediate
              DebounceTime=""2000""
              Placeholder=""e.g. app""
              SuggestItems=""GetSuggestedItems()"" />


<BitSearchBox @bind-Value=""@searchValueWithItemsProvider""
              Immediate
              DebounceTime=""300""
              Placeholder=""e.g. pro""
              SuggestItemsProvider=""LoadItems"" />
<div>SearchValue: @searchValueWithItemsProvider</div>


<BitSearchBox Modeless
              Immediate
              DebounceTime=""300""
              Placeholder=""e.g. pro""
              SuggestItemsProvider=""LoadItems"" />";
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

        var url = NavManager.GetUriWithQueryParameters(""api/Products/GetProducts"", query);

        var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto, request.CancellationToken);

        return data!.Items!.Select(i => i.Name)!;
    }
    catch
    {
        return [];
    }
}";

    private readonly string example11RazorCode = @"
<EditForm Model=""validationBoxModel"">
    <DataAnnotationsValidator />
    <BitSearchBox Placeholder=""Search"" Immediate
                  DefaultValue=""This is default value""
                  @bind-Value=""validationBoxModel.Text"" />
    <ValidationMessage For=""() => validationBoxModel.Text"" />
</EditForm>";
    private readonly string example11CsharpCode = @"
public class ValidationSearchBoxModel
{
    [StringLength(6, MinimumLength = 2, ErrorMessage = ""Text must be between 2 and 6 chars."")]
    public string Text { get; set; }
}

private ValidationSearchBoxModel validationBoxModel = new();";

    private readonly string example12RazorCode = @"
<BitSearchBox Placeholder=""Search"" />
<BitSearchBox Placeholder=""Disabled"" IsEnabled=""false"" />";

    private readonly string example13RazorCode = @"
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

    private readonly string example14RazorCode = @"
<BitSearchBox Placeholder=""جستجو"" Dir=""BitDir.Rtl"" />
<BitSearchBox Placeholder=""جستجو"" Dir=""BitDir.Rtl"" ShowSearchButton />
<BitSearchBox Placeholder=""جستجو"" Dir=""BitDir.Rtl"" Underlined />";
}
