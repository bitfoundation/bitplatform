namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SearchBox;

public partial class BitSearchBoxDemo
{
    private readonly string example1RazorCode = @"
<BitSearchBox Placeholder=""Search"" />
<BitSearchBox Placeholder=""ReadOnly"" ReadOnly DefaultValue=""Read only value"" />
<BitSearchBox Placeholder=""Disabled"" IsEnabled=""false"" />
<BitSearchBox Placeholder=""Required"" Required />";

    private readonly string example2RazorCode = @"
<BitSearchBox Label=""Search products"" Placeholder=""e.g. keyboard"" />

<BitSearchBox Placeholder=""e.g. keyboard"">
    <LabelTemplate>
        <BitStack Horizontal FitHeight Gap=""0.25rem"" Alignment=""BitAlignment.Center"">
            <BitIcon IconName=""@BitIconName.Search"" Color=""BitColor.Info"" />
            <BitText Typography=""BitTypography.Subtitle2"">Advanced search</BitText>
        </BitStack>
    </LabelTemplate>
</BitSearchBox>";

    private readonly string example3RazorCode = @"
<BitSearchBox Placeholder=""Search"" Underlined />
<BitSearchBox Placeholder=""Disabled"" IsEnabled=""false"" Underlined />";

    private readonly string example4RazorCode = @"
<BitSearchBox Placeholder=""Search"" NoBorder/>
<BitSearchBox Placeholder=""Disabled"" IsEnabled=""false"" NoBorder/>";

    private readonly string example5RazorCode = @"
<BitSearchBox Placeholder=""Primary"" Background=""BitColorKind.Primary"" NoBorder/>
<BitSearchBox Placeholder=""Secondary"" Background=""BitColorKind.Secondary"" NoBorder/>
<BitSearchBox Placeholder=""Tertiary"" Background=""BitColorKind.Tertiary"" NoBorder/>
<BitSearchBox Placeholder=""Transparent"" Background=""BitColorKind.Transparent"" NoBorder/>";

    private readonly string example6RazorCode = @"
<BitSearchBox Placeholder=""FixedIcon"" FixedIcon />
<BitSearchBox Placeholder=""DisableAnimation"" DisableAnimation />
<BitSearchBox Placeholder=""Custom icon"" IconName=""@BitIconName.Filter"" />
<BitSearchBox Placeholder=""HideIcon"" HideIcon />";

    private readonly string example7RazorCode = @"
<BitSearchBox Placeholder=""Search"" ShowSearchButton />
<BitSearchBox Placeholder=""SearchButtonIconName"" ShowSearchButton SearchButtonIconName=""PageListFilter"" />
<BitSearchBox Placeholder=""SearchButtonTemplate"" ShowSearchButton>
    <SearchButtonTemplate>
        <BitIcon IconName=""@BitIconName.SearchAndApps"" />
    </SearchButtonTemplate>
</BitSearchBox>
<BitSearchBox Placeholder=""SearchButtonAriaLabel"" ShowSearchButton SearchButtonAriaLabel=""جستجو"" />
<BitSearchBox Placeholder=""Disabled"" IsEnabled=""false"" ShowSearchButton />
<BitSearchBox Placeholder=""Underlined"" Underlined ShowSearchButton />
<BitSearchBox Placeholder=""Disabled Underlined"" IsEnabled=""false"" Underlined ShowSearchButton />
<BitSearchBox Placeholder=""NoBorder"" NoBorder ShowSearchButton />
<BitSearchBox Placeholder=""Disabled NoBorder"" IsEnabled=""false"" NoBorder ShowSearchButton />";

    private readonly string example8RazorCode = @"
<BitSearchBox Placeholder=""HideClearButton"" HideClearButton />

<BitSearchBox Placeholder=""ClearButtonTemplate"">
    <ClearButtonTemplate>
        <BitIcon IconName=""@BitIconName.RemoveFilter"" />
    </ClearButtonTemplate>
</BitSearchBox>

<BitSearchBox Placeholder=""ClearButtonAriaLabel"" ClearButtonAriaLabel=""پاک کردن"" />

<BitSearchBox Placeholder=""NoClearOnEscape"" NoClearOnEscape />";

    private readonly string example9RazorCode = @"
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

    private readonly string example10RazorCode = @"
<BitSearchBox Immediate Placeholder=""MaxLength = 10"" MaxLength=""10"" @bind-Value=""maxLengthValue"" />
<div>Value: [@maxLengthValue]</div>

<BitSearchBox Immediate Trim Placeholder=""Trim (type spaces around a word)"" @bind-Value=""trimmedValue"" />
<div>Value: [@trimmedValue]</div>

<BitSearchBox Placeholder=""InputMode = Search"" InputMode=""BitInputMode.Search"" />

<BitSearchBox Placeholder=""EnterKeyHint = Go"" EnterKeyHint=""BitEnterKeyHint.Go"" />

<BitSearchBox Placeholder=""SpellCheck = false"" SpellCheck=""false"" />

<div style=""display:flex;gap:1rem;max-width:30rem"">
    <BitSearchBox FullWidth Placeholder=""FullWidth in a flex container"" />
    <BitButton>Go</BitButton>
</div>";
    private readonly string example10CsharpCode = @"
private string? maxLengthValue;
private string? trimmedValue;";

    private readonly string example11RazorCode = @"
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
<div>Search Value: @onSearchValue</div>


<BitSearchBox Placeholder=""Search"" DefaultValue=""bit BlazorUI""
              OnChange=""s => uncontrolledValue = s"" />
<div>Search Value: @uncontrolledValue</div>";
    private readonly string example11CsharpCode = @"
private string? twoWaySearchValue;
private string? immediateTwoWaySearchValue;
private string? onChangeSearchValue;
private string? onSearchValue;
private string? uncontrolledValue;";

    private readonly string example12RazorCode = @"
<BitSearchBox Placeholder=""Interact with me"" Immediate
              OnClick=""HandleOnClick""
              OnFocusIn=""HandleOnFocusIn""
              OnFocusOut=""HandleOnFocusOut""
              OnEscape=""HandleOnEscape""
              OnClear=""HandleOnClear""
              OnSearch=""HandleOnSearch""
              OnKeyDown=""HandleOnKeyDown"" />

<BitStack Gap=""0.25rem"" Style=""max-width:19rem;max-height:12rem;overflow:auto"">
    @foreach (var log in eventLogs)
    {
        <BitText Typography=""BitTypography.Caption1"">@log</BitText>
    }
</BitStack>";
    private readonly string example12CsharpCode = @"
private readonly List<string> eventLogs = [];

private void Log(string message)
{
    eventLogs.Insert(0, message);

    if (eventLogs.Count > 20)
    {
        eventLogs.RemoveAt(eventLogs.Count - 1);
    }
}

private void HandleOnClick() => Log(""OnClick"");
private void HandleOnFocusIn() => Log(""OnFocusIn"");
private void HandleOnFocusOut() => Log(""OnFocusOut"");
private void HandleOnEscape() => Log(""OnEscape"");
private void HandleOnClear() => Log(""OnClear"");
private void HandleOnSearch(string? value) => Log($""OnSearch: {value}"");
private void HandleOnKeyDown(KeyboardEventArgs args) => Log($""OnKeyDown: {args.Key}"");";

    private readonly string example13RazorCode = @"
<BitSearchBox @bind-Value=""@searchValue""
              Immediate
              Placeholder=""e.g. app""
              SuggestItems=""GetSuggestedItems()"" />
<div>SearchValue: @searchValue</div>


<BitSearchBox @bind-Value=""@searchValueWithSuggestFilterFunction""
              Immediate
              Placeholder=""e.g. app""
              SuggestItems=""GetSuggestedItems()""
              SuggestFilterFunction=""@SearchFunc"" />
<div>SearchValue: @searchValueWithSuggestFilterFunction</div>


<BitSearchBox @bind-Value=""@searchValueWithMinSearchLength""
              Immediate
              Placeholder=""e.g. app""
              MinSuggestTriggerChars=""1""
              SuggestItems=""GetSuggestedItems()"" />
<div>SearchValue: @searchValueWithMinSearchLength</div>


<BitSearchBox @bind-Value=""@searchValueWithMaxSuggestedItems""
              Immediate
              Placeholder=""e.g. app""
              MaxSuggestCount=""2""
              SuggestItems=""GetSuggestedItems()"" />
<div>SearchValue: @searchValueWithMaxSuggestedItems</div>


<BitSearchBox @bind-Value=""@searchValueWithSearchDelay""
              Immediate
              DebounceTime=""2000""
              Placeholder=""e.g. app""
              SuggestItems=""GetSuggestedItems()"" />
<div>SearchValue: @searchValueWithSearchDelay</div>


<BitSearchBox @bind-Value=""@searchValueWithItemsProvider""
              Immediate
              DebounceTime=""300""
              Placeholder=""e.g. pro""
              SuggestItemsProvider=""LoadItems"" />
<div>SearchValue: @searchValueWithItemsProvider</div>


<BitSearchBox Immediate
              Placeholder=""e.g. app""
              SuggestItems=""GetSuggestedItems()""
              OnSuggestItemSelect=""i => selectedSuggestItem = i"" />
<div>Selected item: @selectedSuggestItem</div>


<BitSearchBox Modeless
              Immediate
              DebounceTime=""300""
              Placeholder=""e.g. pro""
              SuggestItemsProvider=""LoadItems"" />


<BitSearchBox Immediate
              FixedCalloutWidth
              Placeholder=""e.g. app""
              SuggestItems=""GetLongSuggestedItems()"" />";
    private readonly string example13CsharpCode = @"
private string? searchValue;
private string? searchValueWithSuggestFilterFunction;
private string? searchValueWithSearchDelay;
private string? searchValueWithMinSearchLength;
private string? searchValueWithMaxSuggestedItems;
private string? searchValueWithItemsProvider;
private string? selectedSuggestItem;

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

private List<string> GetLongSuggestedItems() =>
[
    ""Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple"",
    ""Red Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple"",
    ""Blue Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple"",
    ""Green Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple"",
    ""Banana Banana Banana Banana Banana Banana Banana Banana Banana Banana Banana Banana Banana Banana Banana"",
    ""Orange Orange Orange Orange Orange Orange Orange Orange Orange Orange Orange Orange Orange Orange Orange"",
    ""Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape"",
    ""Broccoli Broccoli Broccoli Broccoli Broccoli Broccoli Broccoli Broccoli Broccoli Broccoli Broccoli Broccoli"",
    ""Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot"",
    ""Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce""
];

private Func<string?, string?, bool> SearchFunc = (string? searchText, string? itemText) =>
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

    private readonly string example14RazorCode = @"
<BitSearchBox Immediate
              HighlightSuggestItems
              MinSuggestTriggerChars=""1""
              Placeholder=""e.g. app""
              SuggestItems=""GetSuggestedItems()"" />


<BitSearchBox Immediate
              MinSuggestTriggerChars=""1""
              Placeholder=""e.g. app""
              SuggestItems=""GetSuggestedItems()"">
    <SuggestItemTemplate Context=""item"">
        <BitStack Horizontal Gap=""0.5rem"" Alignment=""BitAlignment.Center"">
            <BitIcon IconName=""@BitIconName.Search"" Color=""BitColor.Info"" />
            <BitText Typography=""BitTypography.Body2"">@item</BitText>
        </BitStack>
    </SuggestItemTemplate>
</BitSearchBox>


<BitSearchBox Immediate
              MinSuggestTriggerChars=""1""
              Placeholder=""e.g. xyz""
              NoResultsText=""No matching item found.""
              SuggestItems=""GetSuggestedItems()"" />


<BitSearchBox Immediate
              DebounceTime=""300""
              Placeholder=""e.g. pro""
              LoadingText=""Searching...""
              NoResultsText=""No product found.""
              SuggestItemsProvider=""LoadItemsSlowly"" />


<BitSearchBox Immediate
              MinSuggestTriggerChars=""1""
              Placeholder=""e.g. app""
              SuggestItems=""GetSuggestedItems()"">
    <CalloutHeaderTemplate>
        <BitText Typography=""BitTypography.Overline"">Top matches</BitText>
    </CalloutHeaderTemplate>
    <CalloutFooterTemplate>
        <BitLink Href=""/components/searchbox"" NoUnderline>See all results</BitLink>
    </CalloutFooterTemplate>
</BitSearchBox>


<BitSearchBox Immediate
              AutoSelectSuggestItem
              MinSuggestTriggerChars=""1""
              Placeholder=""e.g. app""
              SuggestItems=""GetSuggestedItems()"" />


<BitSearchBox Immediate
              ShowSuggestItemsOnFocus
              MinSuggestTriggerChars=""0""
              Placeholder=""Click to see the recent searches""
              SuggestItems=""GetRecentSearches()"" />";
    private readonly string example14CsharpCode = @"
private List<string> GetRecentSearches() =>
[
    ""Wireless keyboard"",
    ""Noise cancelling headphones"",
    ""Mechanical switches"",
    ""USB-C hub""
];

private async ValueTask<IEnumerable<string>> LoadItemsSlowly(BitSearchBoxSuggestItemsProviderRequest request)
{
    // an artificial delay to make the loading indicator of the callout observable.
    await Task.Delay(1500, request.CancellationToken);

    // LoadItems is the remote provider of the Suggestion (AutoComplete) example above.
    return await LoadItems(request);
}";

    private readonly string example15RazorCode = @"
<BitSearchBox Immediate
              MinSuggestTriggerChars=""2""
              Placeholder=""e.g. app""
              NoResultsText=""No matching item found.""
              SuggestItems=""GetSuggestedItems()"" />


<BitSearchBox Immediate
              MinSuggestTriggerChars=""2""
              Placeholder=""e.g. app""
              MaxSuggestCount=""0""
              SuggestItems=""GetSuggestedItems()""
              AnnouncementProvider=""AnnounceSuggestItems"" />
<div>Announced: @announcedText</div>";
    private readonly string example15CsharpCode = @"
private string? announcedText;

private string? AnnounceSuggestItems(BitSearchBoxAnnouncementArgs args)
{
    announcedText = args switch
    {
        { IsLoading: true } => ""Looking for matches..."",
        { SearchTerm: null or """" } => null,
        { IsSearchTermTooShort: true } => $""Keep typing, {args.MinSuggestTriggerChars} characters are needed to search."",
        { SuggestItems.Count: 0 } => $""Nothing matches '{args.SearchTerm}'. Try another word."",
        { SuggestItems.Count: 1 } => $""One match for '{args.SearchTerm}': {args.SuggestItems[0]}. Press enter to pick it."",
        _ => $""{args.SuggestItems.Count} matches for '{args.SearchTerm}', from {args.SuggestItems[0]} to {args.SuggestItems[^1]}.""
    };

    return announcedText;
}";

    private readonly string example16RazorCode = @"
<BitSearchBox @ref=""searchBoxRef""
              Immediate
              MinSuggestTriggerChars=""0""
              Placeholder=""Search fruits""
              DefaultValue=""Apple""
              SuggestItems=""GetSuggestedItems()""
              OnSuggestItemsToggle=""v => isSuggestOpen = v"" />
<div>The suggest callout is @(isSuggestOpen ? ""open"" : ""closed"").</div>

<BitStack Horizontal Wrap Gap=""0.5rem"">
    <BitButton OnClick=""() => searchBoxRef.FocusAsync()"">Focus</BitButton>
    <BitButton OnClick=""() => searchBoxRef.Clear()"">Clear</BitButton>
    <BitButton OnClick=""() => searchBoxRef.ShowSuggestItems()"">Show suggestions</BitButton>
    <BitButton OnClick=""() => searchBoxRef.HideSuggestItems()"">Hide suggestions</BitButton>
</BitStack>";
    private readonly string example16CsharpCode = @"
private bool isSuggestOpen;
private BitSearchBox searchBoxRef = default!;";

    private readonly string example17RazorCode = @"
<EditForm Model=""validationBoxModel"">
    <DataAnnotationsValidator />
    <BitSearchBox Placeholder=""Search"" Immediate
                  Label=""Search term""
                  DefaultValue=""This is default value""
                  @bind-Value=""validationBoxModel.Text"" />
    <ValidationMessage For=""() => validationBoxModel.Text"" />
</EditForm>";
    private readonly string example17CsharpCode = @"
public class ValidationSearchBoxModel
{
    [StringLength(6, MinimumLength = 2, ErrorMessage = ""Text must be between 2 and 6 chars."")]
    public string Text { get; set; }
}

private ValidationSearchBoxModel validationBoxModel = new();";

    private readonly string example18RazorCode = @"
<BitSearchBox Placeholder=""Primary"" ShowSearchButton Color=""BitColor.Primary"" />
<BitSearchBox Placeholder=""Primary"" ShowSearchButton Color=""BitColor.Primary"" Underlined />

<BitSearchBox Placeholder=""Secondary"" ShowSearchButton Color=""BitColor.Secondary"" />
<BitSearchBox Placeholder=""Secondary"" ShowSearchButton Color=""BitColor.Secondary"" Underlined />

<BitSearchBox Placeholder=""Tertiary"" ShowSearchButton Color=""BitColor.Tertiary"" />
<BitSearchBox Placeholder=""Tertiary"" ShowSearchButton Color=""BitColor.Tertiary"" Underlined />

<BitSearchBox Placeholder=""Info"" ShowSearchButton Color=""BitColor.Info"" />
<BitSearchBox Placeholder=""Info"" ShowSearchButton Color=""BitColor.Info"" Underlined />

<BitSearchBox Placeholder=""Success"" ShowSearchButton Color=""BitColor.Success"" />
<BitSearchBox Placeholder=""Success"" ShowSearchButton Color=""BitColor.Success"" Underlined />

<BitSearchBox Placeholder=""Warning"" ShowSearchButton Color=""BitColor.Warning"" />
<BitSearchBox Placeholder=""Warning"" ShowSearchButton Color=""BitColor.Warning"" Underlined />

<BitSearchBox Placeholder=""SevereWarning"" ShowSearchButton Color=""BitColor.SevereWarning"" />
<BitSearchBox Placeholder=""SevereWarning"" ShowSearchButton Color=""BitColor.SevereWarning"" Underlined />

<BitSearchBox Placeholder=""Error"" ShowSearchButton Color=""BitColor.Error"" />
<BitSearchBox Placeholder=""Error"" ShowSearchButton Color=""BitColor.Error"" Underlined />

<div style=""background:var(--bit-clr-fg-sec);padding:1rem"">
    <BitSearchBox Placeholder=""PrimaryBackground"" ShowSearchButton Color=""BitColor.PrimaryBackground"" />
    <BitSearchBox Placeholder=""PrimaryBackground"" ShowSearchButton Color=""BitColor.PrimaryBackground"" Underlined />

    <BitSearchBox Placeholder=""SecondaryBackground"" ShowSearchButton Color=""BitColor.SecondaryBackground"" />
    <BitSearchBox Placeholder=""SecondaryBackground"" ShowSearchButton Color=""BitColor.SecondaryBackground"" Underlined />

    <BitSearchBox Placeholder=""TertiaryBackground"" ShowSearchButton Color=""BitColor.TertiaryBackground"" />
    <BitSearchBox Placeholder=""TertiaryBackground"" ShowSearchButton Color=""BitColor.TertiaryBackground"" Underlined />
</div>

<BitSearchBox Placeholder=""PrimaryForeground"" ShowSearchButton Color=""BitColor.PrimaryForeground"" />
<BitSearchBox Placeholder=""PrimaryForeground"" ShowSearchButton Color=""BitColor.PrimaryForeground"" Underlined />

<BitSearchBox Placeholder=""SecondaryForeground"" ShowSearchButton Color=""BitColor.SecondaryForeground"" />
<BitSearchBox Placeholder=""SecondaryForeground"" ShowSearchButton Color=""BitColor.SecondaryForeground"" Underlined />

<BitSearchBox Placeholder=""TertiaryForeground"" ShowSearchButton Color=""BitColor.TertiaryForeground"" />
<BitSearchBox Placeholder=""TertiaryForeground"" ShowSearchButton Color=""BitColor.TertiaryForeground"" Underlined />

<BitSearchBox Placeholder=""PrimaryBorder"" ShowSearchButton Color=""BitColor.PrimaryBorder"" />
<BitSearchBox Placeholder=""PrimaryBorder"" ShowSearchButton Color=""BitColor.PrimaryBorder"" Underlined />

<BitSearchBox Placeholder=""SecondaryBorder"" ShowSearchButton Color=""BitColor.SecondaryBorder"" />
<BitSearchBox Placeholder=""SecondaryBorder"" ShowSearchButton Color=""BitColor.SecondaryBorder"" Underlined />

<BitSearchBox Placeholder=""TertiaryBorder"" ShowSearchButton Color=""BitColor.TertiaryBorder"" />
<BitSearchBox Placeholder=""TertiaryBorder"" ShowSearchButton Color=""BitColor.TertiaryBorder"" Underlined />


<BitSearchBox Placeholder=""Primary"" ShowSearchButton Color=""BitColor.Primary"" IsEnabled=""false"" />
<BitSearchBox Placeholder=""Primary"" ShowSearchButton Color=""BitColor.Primary"" IsEnabled=""false"" Underlined />

<BitSearchBox Placeholder=""Secondary"" ShowSearchButton Color=""BitColor.Secondary"" IsEnabled=""false"" />
<BitSearchBox Placeholder=""Secondary"" ShowSearchButton Color=""BitColor.Secondary"" IsEnabled=""false"" Underlined />

<BitSearchBox Placeholder=""Tertiary"" ShowSearchButton Color=""BitColor.Tertiary"" IsEnabled=""false"" />
<BitSearchBox Placeholder=""Tertiary"" ShowSearchButton Color=""BitColor.Tertiary"" IsEnabled=""false"" Underlined />

<BitSearchBox Placeholder=""Info"" ShowSearchButton Color=""BitColor.Info"" IsEnabled=""false"" />
<BitSearchBox Placeholder=""Info"" ShowSearchButton Color=""BitColor.Info"" IsEnabled=""false"" Underlined />

<BitSearchBox Placeholder=""Success"" ShowSearchButton Color=""BitColor.Success"" IsEnabled=""false"" />
<BitSearchBox Placeholder=""Success"" ShowSearchButton Color=""BitColor.Success"" IsEnabled=""false"" Underlined />

<BitSearchBox Placeholder=""Warning"" ShowSearchButton Color=""BitColor.Warning"" IsEnabled=""false"" />
<BitSearchBox Placeholder=""Warning"" ShowSearchButton Color=""BitColor.Warning"" IsEnabled=""false"" Underlined />

<BitSearchBox Placeholder=""SevereWarning"" ShowSearchButton Color=""BitColor.SevereWarning"" IsEnabled=""false"" />
<BitSearchBox Placeholder=""SevereWarning"" ShowSearchButton Color=""BitColor.SevereWarning"" IsEnabled=""false"" Underlined />

<BitSearchBox Placeholder=""Error"" ShowSearchButton Color=""BitColor.Error"" IsEnabled=""false"" />
<BitSearchBox Placeholder=""Error"" ShowSearchButton Color=""BitColor.Error"" IsEnabled=""false"" Underlined />

<div style=""background:var(--bit-clr-fg-sec);padding:1rem"">
    <BitSearchBox Placeholder=""PrimaryBackground"" ShowSearchButton Color=""BitColor.PrimaryBackground"" IsEnabled=""false"" />
    <BitSearchBox Placeholder=""PrimaryBackground"" ShowSearchButton Color=""BitColor.PrimaryBackground"" IsEnabled=""false"" Underlined />

    <BitSearchBox Placeholder=""SecondaryBackground"" ShowSearchButton Color=""BitColor.SecondaryBackground"" IsEnabled=""false"" />
    <BitSearchBox Placeholder=""SecondaryBackground"" ShowSearchButton Color=""BitColor.SecondaryBackground"" IsEnabled=""false"" Underlined />

    <BitSearchBox Placeholder=""TertiaryBackground"" ShowSearchButton Color=""BitColor.TertiaryBackground"" IsEnabled=""false"" />
    <BitSearchBox Placeholder=""TertiaryBackground"" ShowSearchButton Color=""BitColor.TertiaryBackground"" IsEnabled=""false"" Underlined />
</div>

<BitSearchBox Placeholder=""PrimaryForeground"" ShowSearchButton Color=""BitColor.PrimaryForeground"" IsEnabled=""false"" />
<BitSearchBox Placeholder=""PrimaryForeground"" ShowSearchButton Color=""BitColor.PrimaryForeground"" IsEnabled=""false"" Underlined />

<BitSearchBox Placeholder=""SecondaryForeground"" ShowSearchButton Color=""BitColor.SecondaryForeground"" IsEnabled=""false"" />
<BitSearchBox Placeholder=""SecondaryForeground"" ShowSearchButton Color=""BitColor.SecondaryForeground"" IsEnabled=""false"" Underlined />

<BitSearchBox Placeholder=""TertiaryForeground"" ShowSearchButton Color=""BitColor.TertiaryForeground"" IsEnabled=""false"" />
<BitSearchBox Placeholder=""TertiaryForeground"" ShowSearchButton Color=""BitColor.TertiaryForeground"" IsEnabled=""false"" Underlined />

<BitSearchBox Placeholder=""PrimaryBorder"" ShowSearchButton Color=""BitColor.PrimaryBorder"" IsEnabled=""false"" />
<BitSearchBox Placeholder=""PrimaryBorder"" ShowSearchButton Color=""BitColor.PrimaryBorder"" IsEnabled=""false"" Underlined />

<BitSearchBox Placeholder=""SecondaryBorder"" ShowSearchButton Color=""BitColor.SecondaryBorder"" IsEnabled=""false"" />
<BitSearchBox Placeholder=""SecondaryBorder"" ShowSearchButton Color=""BitColor.SecondaryBorder"" IsEnabled=""false"" Underlined />

<BitSearchBox Placeholder=""TertiaryBorder"" ShowSearchButton Color=""BitColor.TertiaryBorder"" IsEnabled=""false"" />
<BitSearchBox Placeholder=""TertiaryBorder"" ShowSearchButton Color=""BitColor.TertiaryBorder"" IsEnabled=""false"" Underlined />";

    private readonly string example19RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitSearchBox Placeholder=""Search"" Icon=""fa-solid fa-house"" />

<BitSearchBox Placeholder=""Search"" Icon=""@BitIconInfo.Css(""fa-solid fa-heart"")"" Color=""BitColor.Secondary"" />

<BitSearchBox Placeholder=""Search"" Icon=""@BitIconInfo.Fa(""fa-brands fa-github"")"" Color=""BitColor.Tertiary"" />

<BitSearchBox Placeholder=""Search"" Icon=""@BitIconInfo.Fa(""solid search"")"" Color=""BitColor.Error"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitSearchBox Placeholder=""Search"" Icon=""bi bi-house-fill"" />

<BitSearchBox Placeholder=""Search"" Icon=""@BitIconInfo.Css(""bi bi-heart-fill"")"" Color=""BitColor.Secondary"" />

<BitSearchBox Placeholder=""Search"" Icon=""@BitIconInfo.Bi(""search"")"" Color=""BitColor.Tertiary"" />

<BitSearchBox Placeholder=""Search"" Icon=""@BitIconInfo.Bi(""gear-fill"")"" Color=""BitColor.Error"" />


<BitSearchBox Placeholder=""Search"" ShowSearchButton SearchButtonIcon=""@BitIconInfo.Fa(""solid search"")"" />

<BitSearchBox Placeholder=""Search"" ShowSearchButton SearchButtonIcon=""@BitIconInfo.Bi(""search"")"" Color=""BitColor.Secondary"" />


<BitSearchBox Placeholder=""Type to see clear icon"" ClearButtonIcon=""@BitIconInfo.Fa(""solid xmark"")"" />

<BitSearchBox Placeholder=""Type to see clear icon"" ClearButtonIcon=""@BitIconInfo.Bi(""x-circle-fill"")"" Color=""BitColor.Secondary"" />";

    private readonly string example20RazorCode = @"
<BitSearchBox Placeholder=""Small"" Size=""BitSize.Small"" ShowSearchButton />
<BitSearchBox Placeholder=""Medium"" Size=""BitSize.Medium"" ShowSearchButton />
<BitSearchBox Placeholder=""Large"" Size=""BitSize.Large"" ShowSearchButton />

<BitSearchBox Label=""Small"" Placeholder=""Underlined"" Size=""BitSize.Small"" Underlined />
<BitSearchBox Label=""Medium"" Placeholder=""Underlined"" Size=""BitSize.Medium"" Underlined />
<BitSearchBox Label=""Large"" Placeholder=""Underlined"" Size=""BitSize.Large"" Underlined />";

    private readonly string example21RazorCode = @"
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


    .custom-callout {
        border-radius: 0.5rem;
        border: 1px solid #13a3a375;
    }

    .custom-suggest-item {
        font-style: italic;
    }

    .custom-highlight {
        color: var(--bit-clr-pri-text);
        background-color: var(--bit-clr-pri);
        font-weight: 700;
        text-decoration: none;
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
                                 InputContainer = ""custom-input-container"" })"" />

<BitSearchBox Immediate
              HighlightSuggestItems
              MinSuggestTriggerChars=""1""
              Placeholder=""e.g. app""
              SuggestItems=""GetSuggestedItems()""
              Classes=""@(new() { Callout = ""custom-callout"",
                                 SuggestItemButton = ""custom-suggest-item"",
                                 SuggestItemHighlight = ""custom-highlight"" })"" />";
    private readonly string example21CsharpCode = @"
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
];";

    private readonly string example22RazorCode = @"
<BitSearchBox Placeholder=""جستجو"" Dir=""BitDir.Rtl"" />
<BitSearchBox Placeholder=""جستجو"" Dir=""BitDir.Rtl"" ShowSearchButton />
<BitSearchBox Placeholder=""جستجو"" Dir=""BitDir.Rtl"" Underlined />

<BitSearchBox Dir=""BitDir.Rtl""
              Immediate
              Label=""جستجوی میوه""
              Placeholder=""مثلا: سیب""
              HighlightSuggestItems
              MinSuggestTriggerChars=""1""
              NoResultsText=""موردی یافت نشد.""
              SuggestItems=""GetPersianSuggestedItems()"" />";
    private readonly string example22CsharpCode = @"
private List<string> GetPersianSuggestedItems() =>
[
    ""سیب"",
    ""سیب قرمز"",
    ""سیب سبز"",
    ""موز"",
    ""پرتقال"",
    ""انگور""
];";
}
