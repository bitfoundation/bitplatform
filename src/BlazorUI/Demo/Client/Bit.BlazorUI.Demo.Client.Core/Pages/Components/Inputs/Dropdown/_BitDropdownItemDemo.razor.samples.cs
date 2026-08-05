namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class _BitDropdownItemDemo
{
    private readonly string example1RazorCode = @"
<BitDropdown Label=""Single select""
             Items=""GetBasicItems()""
             Placeholder=""Select an item""
             TItem=""BitDropdownItem<string>"" TValue=""string"" />

<BitDropdown Label=""Multi select""
             MultiSelect
             DefaultValues=""@(Array.Empty<string>())""
             Items=""GetBasicItems()""
             Placeholder=""Select items"" />

<BitDropdown Label=""Required"" Required
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />

<BitDropdown Label=""PreserveCalloutWidth""
             PreserveCalloutWidth
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Disabled""
             IsEnabled=""false""
             Items=""GetBasicItems()""
             DefaultValue=""@(""f-ora"")""
             Placeholder=""Select an item"" />

<BitDropdown Label=""ReadOnly""
             ReadOnly
             Items=""GetBasicItems()""
             DefaultValue=""@(""f-app"")""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Custom delimiter""
             MultiSelect
             MultiSelectDelimiter="" - ""
             Items=""GetBasicItems()""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"" })""
             Placeholder=""Select items"" />

<BitDropdown Label=""Title""
             Title=""Pick your favorite fruit or vegetable""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Hover me"" />";
    private readonly string example1CsharpCode = @"
private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example2RazorCode = @"
<BitDropdown Label=""Grouped items""
             Items=""GetGroupedItems()""
             DefaultValue=""@("""")""
             Placeholder=""Select an item"" />

<BitDropdown Label=""StickyHeaders""
             StickyHeaders
             MultiSelect
             Items=""GetGroupedItems()""
             DefaultValues=""@(Array.Empty<string>())""
             Placeholder=""Select items"" />

<BitDropdown Label=""Grouping while searching""
             Immediate
             ShowSearchBox
             AutoFocusSearchBox
             HighlightSearch
             StickyHeaders
             Items=""GetGroupedItems()""
             DefaultValue=""@("""")""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Try 'a', then 'ric'"" />";
    private readonly string example2CsharpCode = @"
private List<BitDropdownItem<string>> GetGroupedItems() =>
[
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"" },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Text = ""Mango"", Value = ""f-man"" },
    new() { Text = ""Peach"", Value = ""f-pea"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" },
    new() { Text = ""Potato"", Value = ""v-pot"" },
    new() { Text = ""Tomato"", Value = ""v-tom"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Grains"" },
    new() { Text = ""Barley"", Value = ""g-bar"" },
    new() { Text = ""Oat"", Value = ""g-oat"" },
    new() { Text = ""Rice"", Value = ""g-ric"" },
    new() { Text = ""Wheat"", Value = ""g-whe"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Nuts"" },
    new() { Text = ""Almond"", Value = ""n-alm"" },
    new() { Text = ""Cashew"", Value = ""n-cas"" },
    new() { Text = ""Walnut"", Value = ""n-wal"" }
];";

    private readonly string example3RazorCode = @"
<BitDropdown Label=""Prefix""
             Prefix=""Fruits:""
             Items=""GetBasicItems()""
             Placeholder=""Select an item""
             TItem=""BitDropdownItem<string>""
             TValue=""string"" />

<BitDropdown Label=""Suffix""
             Suffix=""kg""
             Items=""GetBasicItems()""
             Placeholder=""Select an item""
             TItem=""BitDropdownItem<string>""
             TValue=""string"" />

<BitDropdown Label=""Prefix and Suffix""
             Prefix=""Fruits:""
             Suffix=""kg""
             Items=""GetBasicItems()""
             Placeholder=""Select an item""
             TItem=""BitDropdownItem<string>""
             TValue=""string"" />

<BitDropdown Label=""Templates""
             Items=""GetBasicItems()""
             Placeholder=""Select an item""
             TItem=""BitDropdownItem<string>""
             TValue=""string"">
    <PrefixTemplate>
        <BitIcon IconName=""@BitIconName.ShoppingCart"" Style=""padding-inline:0.5rem"" />
    </PrefixTemplate>
    <SuffixTemplate>
        <BitIcon IconName=""@BitIconName.Info"" Style=""padding-inline:0.5rem"" />
    </SuffixTemplate>
</BitDropdown>

<BitDropdown Label=""Disabled""
             Prefix=""Fruits:""
             Suffix=""kg""
             Items=""GetBasicItems()""
             Placeholder=""Select an item""
             TItem=""BitDropdownItem<string>""
             TValue=""string""
             IsEnabled=""false"" />";
    private readonly string example3CsharpCode = @"
private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example4RazorCode = @"
<BitDropdown Label=""Single select"" 
             FitWidth
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Multi select""
             FitWidth
             MultiSelect
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             DefaultValues=""@(Array.Empty<string>())"" />";
    private readonly string example4CsharpCode = @"
private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example5RazorCode = @"
<BitDropdown NoBorder
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />

<BitDropdown NoBorder
             MultiSelect
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             DefaultValues=""@(Array.Empty<string>())"" />

<BitDropdown Underlined
             Label=""Underlined""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />

<BitDropdown Underlined
             MultiSelect
             Label=""Underlined multi select""
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             DefaultValues=""@(Array.Empty<string>())"" />

<div style=""padding:0.5rem;border-radius:0.5rem;background:linear-gradient(90deg,#ff00cc7f,#3333997f)"">
    <BitDropdown Transparent
                 Items=""GetBasicItems()""
                 DefaultValue=""@string.Empty""
                 Placeholder=""Select an item"" />
</div>";
    private readonly string example5CsharpCode = @"
private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example6RazorCode = @"
<BitDropdown Label=""Responsive Dropdown""
             Responsive
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />";
    private readonly string example6CsharpCode = @"
private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example7RazorCode = @"
<BitDropdown Label=""All""
             Items=""dropDirectionItems""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             DropDirection=""BitDropDirection.All"" />

<BitDropdown Label=""TopAndBottom""
             Items=""dropDirectionItems""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             DropDirection=""BitDropDirection.TopAndBottom"" />

<BitDropdown Label=""MaxHeight (150px)""
             MaxHeight=""150""
             Items=""dropDirectionItems""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />";
    private readonly string example7CsharpCode = @"
private ICollection<BitDropdownItem<string>>? dropDirectionItems;

protected override void OnInitialized()
{
        dropDirectionItems = Enumerable.Range(1, 15)
                                       .Select(c => new BitDropdownItem<string> { Value = c.ToString(), Text = $""Category {c}"" })
                                       .ToArray();
}";

    private readonly string example8RazorCode = @"
<BitDropdown @bind-Value=""clearValue""
             ShowClearButton
             Items=""GetBasicItems()""
             Label=""Single select dropdown""
             Placeholder=""Select an option"" />
<div>Value: @clearValue</div>

<BitDropdown @bind-Values=""clearValues""
             MultiSelect
             ShowClearButton
             Items=""GetBasicItems()""
             Placeholder=""Select options""
             Label=""Multi select dropdown"" />
<div>Values: @string.Join(',', clearValues)</div>

<BitDropdown ShowClearButton
             Items=""GetBasicItems()""
             Label=""Single select dropdown""
             DefaultValue=""@(""f-app"")""
             Placeholder=""Select an option""
             OnClear=""() => clearCounter++"" />
<div>OnClear count: @clearCounter</div>";
    private readonly string example8CsharpCode = @"
private int clearCounter;
private string? clearValue = ""f-app"";
private IEnumerable<string?> clearValues = [""f-app"", ""f-ban""];

private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example9RazorCode = @"
<BitDropdown Label=""Single select & auto focus""
             Responsive
             ShowSearchBox
             AutoFocusSearchBox
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search item"" />

<BitDropdown Label=""Multi select""
             Responsive
             MultiSelect
             ShowSearchBox
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             DefaultValues=""@(Array.Empty<string>())""
             SearchBoxPlaceholder=""Search items"" />


<BitDropdown Label=""Single select & auto focus""
             Responsive
             ShowSearchBox
             AutoFocusSearchBox
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search item""
             SearchFunction=""(items, text) => items.Where(i => i.Text?.StartsWith(text, StringComparison.OrdinalIgnoreCase) ?? false).ToArray()"" />

<BitDropdown Label=""Multi select""
             Responsive
             MultiSelect
             ShowSearchBox
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             DefaultValues=""@(Array.Empty<string>())""
             SearchBoxPlaceholder=""Search items""
             SearchFunction=""(items, text) => items.Where(i => i.Text?.EndsWith(text, StringComparison.OrdinalIgnoreCase) ?? false).ToArray()"" />";
    private readonly string example9CsharpCode = @"
private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example10RazorCode = @"
<BitDropdown Label=""SearchMode: StartsWith""
             ShowSearchBox
             AutoFocusSearchBox
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Type a first letter""
             SearchMode=""BitDropdownSearchMode.StartsWith"" />

<BitDropdown Label=""MinSearchLength: 3""
             ShowSearchBox
             AutoFocusSearchBox
             MinSearchLength=""3""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Type at least 3 characters"" />

<BitDropdown Label=""SearchIgnoreDiacritics""
             ShowSearchBox
             AutoFocusSearchBox
             HighlightSearch
             SearchIgnoreDiacritics
             Items=""GetAccentedItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select a name""
             SearchBoxPlaceholder=""Try jose, muller or angstrom"" />

<BitDropdown Label=""HighlightSearch""
             ShowSearchBox
             AutoFocusSearchBox
             HighlightSearch
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search item"" />

<BitDropdown Label=""Highlight in the ComboBox""
             Combo
             HighlightSearch
             Items=""comboBoxItems""
             DefaultValue=""@string.Empty""
             Placeholder=""Type to filter"" />";
    private readonly string example10CsharpCode = @"
private List<BitDropdownItem<string>> GetAccentedItems() => new()
{
    new() { Text = ""José"", Value = ""n-jos"" },
    new() { Text = ""Renée"", Value = ""n-ren"" },
    new() { Text = ""Müller"", Value = ""n-mul"" },
    new() { Text = ""Ångström"", Value = ""n-ang"" },
    new() { Text = ""Zoë"", Value = ""n-zoe"" },
    new() { Text = ""Smith"", Value = ""n-smi"" }
};

private List<BitDropdownItem<string>> comboBoxItems = new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example11RazorCode = @"
<BitDropdown Label=""Immediate""
             ShowSearchBox
             Immediate
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search item""
             OnSearch=""v => immediateSearchValue = v"" />
<div>Search value: <b>@immediateSearchValue</b></div>

<BitDropdown Label=""Immediate + DebounceTime (500ms)""
             ShowSearchBox
             Immediate
             DebounceTime=""500""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search item""
             OnSearch=""v => debouncedSearchValue = v"" />
<div>Search value: <b>@debouncedSearchValue</b></div>

<BitDropdown Label=""Immediate ComboBox + ThrottleTime (500ms)""
             Combo
             Immediate
             ThrottleTime=""500""
             Items=""comboBoxItems""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />";
    private readonly string example11CsharpCode = @"
private string? immediateSearchValue;
private string? debouncedSearchValue;

private List<BitDropdownItem<string>> comboBoxItems =
[
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
];

private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example12RazorCode = @"
@using System.ComponentModel.DataAnnotations;

<style>
    .validation-message {
        color: #A4262C;
        font-size: 0.75rem;
    }
</style>

<EditForm Model=""validationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <BitDropdown @bind-Value=""validationModel.Category""
                 Label=""Select 1 item""
                 Items=""GetBasicItems()""
                 Placeholder=""Select an item"" />
    <ValidationMessage For=""@(() => validationModel.Category)"" />

    <BitDropdown @bind-Values=""validationModel.Products""
                 MultiSelect
                 Items=""GetBasicItems()""
                 Placeholder=""Select items""
                 Label=""Select min 1 and max 2 items"" />
    <ValidationMessage For=""@(() => validationModel.Products)"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example12CsharpCode = @"
public class FormValidationDropdownModel
{
    [MaxLength(2, ErrorMessage = ""The property {0} have more than {1} elements"")]
    [MinLength(1, ErrorMessage = ""The property {0} doesn't have at least {1} elements"")]
    public ICollection<string?> Products { get; set; } = new List<string?>();

    [Required]
    public string Category { get; set; }
}

private FormValidationDropdownModel validationModel = new();

private async Task HandleValidSubmit() { }

private void HandleInvalidSubmit() { }

private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example13RazorCode = @"
<style>
    .custom-drp {
        gap: 10px;
        display: flex;
        align-items: center;
        flex-flow: row nowrap;
        justify-content: flex-start;
    }

    .custom-drp.custom-drp-lbl {
        color: dodgerblue;
    }

    .custom-drp.custom-drp-txt {
        color: goldenrod;
    }

    .custom-drp.custom-drp-ph {
        color: orangered;
    }

    .custom-drp.custom-drp-item {
        width: 100%;
        cursor: pointer;
    }

    .custom-drp.custom-drp-header {
        width: 100%;
        padding: 5px 12px;
        color: #ff4600;
        font-weight: bold;
    }
</style>

<BitDropdown Label=""Header template""
             Items=""GetDataItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"">
    <HeaderTemplate Context=""item"">
        <div class=""custom-drp custom-drp-header"">
            <BitIcon IconName=""@((item.Data as DropdownItemData)?.IconName)"" />
            <div>@item.Text</div>
        </div>
    </HeaderTemplate>
</BitDropdown>

<BitDropdown Label=""Text & Item templates""
             Items=""GetDataItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"">
    <TextTemplate Context=""dropdown"">
        <div class=""custom-drp custom-drp-txt"">
            <BitIcon IconName=""@((dropdown.SelectedItem?.Data as DropdownItemData)?.IconName)"" />
            <div>@dropdown.SelectedItem?.Text</div>
        </div>
    </TextTemplate>
    <ItemTemplate Context=""item"">
        <div class=""custom-drp custom-drp-item"">
            <BitIcon IconName=""@((item.Data as DropdownItemData)?.IconName)"" />
            <div Style=""text-decoration:underline"">@item.Text</div>
        </div>
    </ItemTemplate>
</BitDropdown>

<BitDropdown Label=""Placeholder template""
             Items=""GetDataItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"">
    <PlaceholderTemplate Context=""dropdown"">
        <div class=""custom-drp custom-drp-ph"">
            <BitIcon IconName=""@BitIconName.MessageFill"" />
            <div>@dropdown.Placeholder</div>
        </div>
    </PlaceholderTemplate>
</BitDropdown>

<BitDropdown Label=""Label template""
             Items=""GetDataItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"">
    <LabelTemplate>
        <div class=""custom-drp custom-drp-lbl"">
            <div>Custom label</div>
            <BitIcon IconName=""@BitIconName.Info"" AriaLabel=""Info"" />
        </div>
    </LabelTemplate>
</BitDropdown>

<BitDropdown Label=""CaretDownIconName""
             Items=""GetDataItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             CaretDownIconName=""@BitIconName.ScrollUpDown"" />

<BitDropdown Label=""CaretDownTemplate""
             Items=""GetDataItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"">
    <CaretDownTemplate>
        <BitIcon IconName=""@BitIconName.FavoriteStar"" Style=""font-size:0.875rem"" />
    </CaretDownTemplate>
</BitDropdown>

<BitDropdown Label=""Callout templates""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"">
    <CalloutHeaderTemplate>
        <div Style=""padding:0.5rem;border-bottom:1px solid #555"">Best in the world</div>
    </CalloutHeaderTemplate>
    <CalloutFooterTemplate>
        <BitActionButton IconName=""@BitIconName.Add"">New Item</BitActionButton>
    </CalloutFooterTemplate>
</BitDropdown>";
    private readonly string example13CsharpCode = @"
public class DropdownItemData
{
    public string? IconName { get; set; }
}

private List<BitDropdownItem<string>> GetDataItems() =>  new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Items"", Data = new DropdownItemData { IconName = ""BulletedList2"" } },
    new() { Text = ""Item a"", Value = ""A"", Data = new DropdownItemData { IconName = ""Memo"" } },
    new() { Text = ""Item b"", Value = ""B"", Data = new DropdownItemData { IconName = ""Print"" } },
    new() { Text = ""Item c"", Value = ""C"", Data = new DropdownItemData { IconName = ""ShoppingCart"" } },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""More Items"", Data = new DropdownItemData { IconName = ""BulletedTreeList"" } },
    new() { Text = ""Item d"", Value = ""D"", Data = new DropdownItemData { IconName = ""Train"" } },
    new() { Text = ""Item e"", Value = ""E"", Data = new DropdownItemData { IconName = ""Repair"" } },
    new() { Text = ""Item f"", Value = ""F"", Data = new DropdownItemData { IconName = ""Running"" } }
};";

    private readonly string example14RazorCode = @"
<BitDropdown @bind-Value=""controlledValue""
             Label=""Single select""
             Items=""GetBasicItems()""
             Placeholder=""Select an item"" />
<div>Selected Value: @controlledValue</div>

<BitDropdown @bind-Values=""controlledValues""
             MultiSelect
             Label=""Multi select""
             Items=""GetBasicItems()""
             Placeholder=""Select items"" />
<div>Selected Values: @string.Join("","", controlledValues)</div>



<BitDropdown Label=""Single select""
             Items=""GetBasicItems()""
             Placeholder=""Select an item""
             TItem=""BitDropdownItem<string>"" TValue=""string""
             OnChange=""(string value) => changedValue = value"" />
<div>Changed Value: @changedValue</div>
            
<BitDropdown Label=""Multi select""
             MultiSelect
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             TItem=""BitDropdownItem<string>"" TValue=""string""
             OnValuesChange=""(IEnumerable<string> values) => changedValues = values"" />
<div>Changed Values: @string.Join("","", changedValues)</div>



<BitDropdown Label=""Single select""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             OnSelectItem=""(BitDropdownItem<string> item) => selectedItem1 = item"" />
<div>Selected Value: @selectedItem1?.Value</div>

<BitDropdown Label=""Multi select""
             MultiSelect
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             DefaultValues=""@(Array.Empty<string>())""
             OnSelectItem=""(BitDropdownItem<string> item) => selectedItem2 = item"" />
<div>Selected Value: @selectedItem2?.Value</div>

<BitDropdown Label=""Multi select""
             MultiSelect
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"" })""
             OnSelectItem=""(BitDropdownItem<string> item) => pickedItem = item""
             OnDeselectItem=""(BitDropdownItem<string> item) => deselectedItem = item"" />
<div>Last picked item: @pickedItem?.Text</div>
<div>Last unselected item: @deselectedItem?.Text</div>

<BitDropdown Label=""Single select""
             Reselectable
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             OnSelectItem=""(BitDropdownItem<string> item) => selectItemCounter++"" />
<div>OnSelectItem count: @selectItemCounter</div>

<BitButton OnClick=""() => isDropdownOpen = true"">Open the dropdown</BitButton>
<BitDropdown @bind-IsOpen=""isDropdownOpen""
             Label=""Single select""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Single select""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             OnOpen=""HandleOnCalloutOpen""
             OnClose=""HandleOnCalloutClose"" />
<div>The callout is @calloutState.</div>

<BitDropdown Label=""Single select""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Tab into me""
             OnFocusIn=""HandleOnFocusIn""
             OnFocusOut=""HandleOnFocusOut"" />
<div>The dropdown is @focusState.</div>
<BitDropdown @bind-Value=""comparerValue""
             Label=""Case-insensitive values""
             Items=""GetBasicItems()""
             Placeholder=""Select an item""
             ValueComparer=""StringComparer.OrdinalIgnoreCase"" />
<div>Value: @comparerValue</div>";
    private readonly string example14CsharpCode = @"
private bool isDropdownOpen;
private int selectItemCounter;
private string calloutState = ""closed"";
private string focusState = ""blurred"";

private void HandleOnCalloutOpen() => calloutState = ""opened"";

private void HandleOnCalloutClose() => calloutState = ""closed"";

private void HandleOnFocusIn() => focusState = ""focused"";

private void HandleOnFocusOut() => focusState = ""blurred"";

private string controlledValue = ""f-app"";
private IEnumerable<string> controlledValues = [""f-app"", ""f-ban""];

private string? changedValue;
private IEnumerable<string> changedValues = [];

private BitDropdownItem<string>? selectedItem1;
private BitDropdownItem<string>? selectedItem2;
private BitDropdownItem<string>? pickedItem;
private BitDropdownItem<string>? deselectedItem;

private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};
private string? comparerValue = ""F-APP"";";

    private readonly string example15RazorCode = @"
<BitDropdown @bind-Value=""autoSelectValue""
             Combo
             Immediate
             AutoSelectFirstMatch
             Items=""comboBoxItems""
             Label=""AutoSelectFirstMatch""
             Placeholder=""Type a few letters and press Enter"" />
<div>Value: @autoSelectValue</div>

<BitDropdown @bind-Value=""comboBoxValueSample1""
             Combo
             Responsive
             Items=""comboBoxItems"" 
             Placeholder=""Select an option""
             Label=""Single select combo box"" />
<div>Value: @comboBoxValueSample1</div>

<BitDropdown @bind-Values=""comboBoxValues1""
             Combo 
             Responsive
             MultiSelect
             Items=""comboBoxItems"" 
             Label=""Multi select combo box""
             Placeholder=""Select an option"" />
<div>Values: @string.Join(',', comboBoxValues1)</div>";
    private readonly string example15CsharpCode = @"
private string? autoSelectValue;
private string comboBoxValueSample1 = default!;
private IEnumerable<string> comboBoxValues1 = [];

private List<BitDropdownItem<string>> comboBoxItems = new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example16RazorCode = @"
<BitDropdown @bind-Value=""comboBoxValueSample2""
             Combo Chips
             Responsive
             Items=""comboBoxItems"" 
             Placeholder=""Select an option""
             Label=""Single select combo box & chips"" />
<div>Value: @comboBoxValueSample2</div>

<BitDropdown @bind-Values=""comboBoxValues2""
             Combo Chips 
             MultiSelect
             Responsive
             Items=""comboBoxItems"" 
             Placeholder=""Select an option""
             Label=""Multi select combo box & chips"" />
<div>Values: @string.Join(',', comboBoxValues2)</div>

<BitDropdown Chips
             MultiSelect
             Label=""ChipTemplate""
             Items=""GetDataItems()""
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""A"", ""D"" })"">
    <ChipTemplate Context=""item"">
        <span class=""custom-chip"">
            <BitIcon IconName=""@((item.Data as DropdownItemData)?.IconName)"" />
            <span>@item.Text</span>
        </span>
    </ChipTemplate>
</BitDropdown>";
    private readonly string example16CsharpCode = @"
private string comboBoxValueSample2 = default!;
private IEnumerable<string> comboBoxValues2 = [];

public class DropdownItemData
{
    public string? IconName { get; set; }
}

private List<BitDropdownItem<string>> GetDataItems() =>
[
    new() { Text = ""Item a"", Value = ""A"", Data = new DropdownItemData { IconName = ""Memo"" } },
    new() { Text = ""Item b"", Value = ""B"", Data = new DropdownItemData { IconName = ""Print"" } },
    new() { Text = ""Item c"", Value = ""C"", Data = new DropdownItemData { IconName = ""ShoppingCart"" } },
    new() { Text = ""Item d"", Value = ""D"", Data = new DropdownItemData { IconName = ""Train"" } }
];

private List<BitDropdownItem<string>> comboBoxItems = new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example17RazorCode = @"
<BitDropdown Label=""MaxDisplayedItems (chips)""
             Chips
             MultiSelect
             MaxDisplayedItems=""2""
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"", ""f-gra"", ""v-car"" })"" />

<BitDropdown Label=""OverflowTextFormat""
             Chips
             MultiSelect
             MaxDisplayedItems=""2""
             OverflowTextFormat=""and {0} more""
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"", ""f-gra"", ""v-car"" })"" />

<BitDropdown Label=""SelectedItemsTextFormat""
             MultiSelect
             MaxDisplayedItems=""2""
             SelectedItemsTextFormat=""{0} fruits and vegetables""
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"", ""f-gra"", ""v-car"" })"" />

<BitDropdown Label=""AutoClearSearch""
             Combo Chips
             MultiSelect
             AutoClearSearch
             Items=""comboBoxItems""
             DefaultValues=""@(Array.Empty<string>())""
             Placeholder=""Type to filter, then pick"" />

<BitDropdown Label=""HideSelectedItems""
             Chips
             MultiSelect
             HideSelectedItems
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"" })"" />";
    private readonly string example17CsharpCode = @"
private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private List<BitDropdownItem<string>> comboBoxItems = new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example18RazorCode = @"
<BitDropdown @bind-Value=""comboBoxValueSample3""
             Combo Dynamic
             Responsive
             Items=""comboBoxItems""
             Placeholder=""Select an option""
             Label=""Single select combo box & dynamic""
             DynamicValueGenerator=""(BitDropdownItem<string> item) => item.Text""
             OnDynamicAdd=""(BitDropdownItem<string> item) => HandleOnDynamicAdd(item)"" />
<div>Value: @comboBoxValueSample3</div>

<BitDropdown @bind-Value=""comboBoxValueSample4""
             Responsive
             Combo Chips Dynamic
             Items=""comboBoxItems""
             Placeholder=""Select an option""
             Label=""Single select combo box, chips & dynamic""
             DynamicValueGenerator=""(BitDropdownItem<string> item) => item.Text""
             OnDynamicAdd=""(BitDropdownItem<string> item) => HandleOnDynamicAdd(item)"" />
<div>Value: @comboBoxValueSample4</div>

<BitDropdown @bind-Values=""comboBoxValues3""
             Responsive
             MultiSelect
             Combo Chips Dynamic
             Items=""comboBoxItems""
             Placeholder=""Select options""
             Label=""Multi select combo box, chips & dynamic""
             DynamicValueGenerator=""(BitDropdownItem<string> item) => item.Text""
             OnDynamicAdd=""(BitDropdownItem<string> item) => HandleOnDynamicAdd(item)"" />
<div>Values: @string.Join(',', comboBoxValues3)</div>";
    private readonly string example18CsharpCode = @"
private string comboBoxValueSample3 = default!;
private string comboBoxValueSample4 = default!;
private IEnumerable<string> comboBoxValues3 = [];

private void HandleOnDynamicAdd(BitDropdownItem<string> item)
{
    comboBoxItems.Add(item);
}

private List<BitDropdownItem<string>> comboBoxItems = new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example19RazorCode = @"
<BitDropdown Label=""Single select""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Multi select""
             MultiSelect
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             DefaultValues=""@(Array.Empty<string>())"" />";
    private readonly string example19CsharpCode = @"
private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example20RazorCode = @"
<BitDropdown @bind-Values=""selectAllValues""
             MultiSelect
             ShowSelectAll
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             Label=""Select all"" />
<div>Values: @string.Join(',', selectAllValues)</div>

<BitDropdown MultiSelect
             ShowSelectAll
             ShowSearchBox
             SelectAllText=""Select all of them""
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             DefaultValues=""@(Array.Empty<string>())""
             SearchBoxPlaceholder=""Search items""
             Label=""Custom text & search"" />";
    private readonly string example20CsharpCode = @"
private IEnumerable<string?> selectAllValues = [];

private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example21RazorCode = @"
<BitDropdown @bind-Values=""maxSelectedValues""
             MultiSelect
             MaxSelectedItems=""2""
             Items=""GetBasicItems()""
             Placeholder=""Select up to 2 items""
             Label=""Max 2 items"" />
<div>Values: @string.Join(',', maxSelectedValues)</div>";
    private readonly string example21CsharpCode = @"
private IEnumerable<string?> maxSelectedValues = [];

private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example22RazorCode = @"
<style>
    .custom-drp {
        gap: 10px;
        display: flex;
        align-items: center;
        flex-flow: row nowrap;
        justify-content: flex-start;
    }

    .custom-drp.custom-drp-empty {
        color: orangered;
        padding: 5px 12px;
        justify-content: center;
    }
</style>

<BitDropdown Label=""Default""
             Items=""emptyItems""
             Placeholder=""Select an item""
             TItem=""BitDropdownItem<string>"" TValue=""string"" />

<BitDropdown Label=""EmptyText""
             Items=""emptyItems""
             Placeholder=""Select an item""
             EmptyText=""There is nothing here!""
             TItem=""BitDropdownItem<string>"" TValue=""string"" />

<BitDropdown Label=""EmptyTemplate""
             Items=""emptyItems""
             Placeholder=""Select an item""
             TItem=""BitDropdownItem<string>"" TValue=""string"">
    <EmptyTemplate>
        <div class=""custom-drp custom-drp-empty"">
            <BitIcon IconName=""@BitIconName.SearchIssue"" />
            <div>Nothing to show!</div>
        </div>
    </EmptyTemplate>
</BitDropdown>

<BitDropdown Label=""Search without result""
             ShowSearchBox
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search for anything"" />

<BitDropdown Label=""NoResultsText""
             ShowSearchBox
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search for anything""
             NoResultsText=""Nothing matched your search"" />

<BitDropdown Label=""NoResultsTemplate""
             ShowSearchBox
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search for anything"">
    <NoResultsTemplate>
        <div class=""custom-drp custom-drp-empty"">
            <BitIcon IconName=""@BitIconName.SearchAndApps"" />
            <div>No match. Try another term!</div>
        </div>
    </NoResultsTemplate>
</BitDropdown>";
    private readonly string example22CsharpCode = @"
private readonly List<BitDropdownItem<string>> emptyItems = [];

private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example23RazorCode = @"
<style>
    .custom-drp {
        gap: 10px;
        display: flex;
        align-items: center;
        flex-flow: row nowrap;
        justify-content: flex-start;
    }

    .custom-drp.custom-drp-empty {
        color: orangered;
        padding: 5px 12px;
        justify-content: center;
    }
</style>

<BitDropdown Label=""IsLoading""
             IsLoading
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />

<BitDropdown Label=""LoadingText""
             IsLoading
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             LoadingText=""Fetching the products..."" />

<BitDropdown Label=""LoadingTemplate""
             IsLoading
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"">
    <LoadingTemplate>
        <div class=""custom-drp custom-drp-empty"">
            <BitIcon IconName=""@BitIconName.Sync"" />
            <div>Just a moment...</div>
        </div>
    </LoadingTemplate>
</BitDropdown>

<BitButton OnClick=""LoadDelayedItems"" IsLoading=""isLoadingItems"">Load the items</BitButton>
<BitDropdown Label=""Products""
             IsLoading=""isLoadingItems""
             Items=""delayedItems""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />";
    private readonly string example23CsharpCode = @"
private bool isLoadingItems;
private ICollection<BitDropdownItem<string>> delayedItems = [];

private async Task LoadDelayedItems()
{
    isLoadingItems = true;
    delayedItems = [];

    await Task.Delay(2000);

    delayedItems = GetBasicItems();
    isLoadingItems = false;
}

private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example24RazorCode = @"
<BitDropdown Label=""Single select""
             Virtualize
             Items=""virtualizeItems1""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Multi select""
             Virtualize
             MultiSelect
             ItemSize=""35""
             OverscanCount=""5""
             Items=""virtualizeItems2""
             Placeholder=""Select items""
             DefaultValues=""@(Array.Empty<string>())"" />

<BitDropdown Label=""Opens on the selected item (5000 of 10,000)""
             Virtualize
             Items=""virtualizeItems3""
             DefaultValue=""@(""5000"")""
             Placeholder=""Select an item"" />


<BitDropdown Label=""Single select""
             Virtualize
             ItemsProvider=""LoadItems""
             Placeholder=""Select an item""
             TItem=""BitDropdownItem<string>"" TValue=""string"" />

<BitDropdown Label=""Multi select & ItemsProviderDebounceTime""
             Virtualize
             MultiSelect
             ItemsProvider=""LoadItems""
             Placeholder=""Select items""
             ItemsProviderDebounceTime=""300""
             TItem=""BitDropdownItem<string>"" TValue=""string"" />

<BitDropdown Label=""VirtualizePlaceholder""
             Virtualize
             ItemsProvider=""LoadItems""
             Placeholder=""Select an item""
             TItem=""BitDropdownItem<string>"" TValue=""string"">
    <VirtualizePlaceholder>
        <div style=""padding:0 0.5rem;color:gray"">Loading @(context.Index)...</div>
    </VirtualizePlaceholder>
</BitDropdown>

<BitDropdown Label=""Single select""
             Virtualize
             ItemsProvider=""LoadItems""
             Placeholder=""Select an item""
             InitialSelectedItems=""initialSelectedItem""
             TItem=""BitDropdownItem<string>"" TValue=""string"" />

<BitDropdown Label=""Multi select""
             Virtualize
             MultiSelect
             ItemsProvider=""LoadItems""
             Placeholder=""Select items""
             InitialSelectedItems=""initialSelectedItems""
             TItem=""BitDropdownItem<string>"" TValue=""string"" />";
    private readonly string example24CsharpCode = @"
private ICollection<BitDropdownItem<string>>? virtualizeItems1;
private ICollection<BitDropdownItem<string>>? virtualizeItems2;

private IEnumerable<BitDropdownItem<string>> initialSelectedItem = [
    new()
    {
        Text = ""Product 100"",
        Value = ""100"",
        Data = new ProductDto {
            Id = 100,
            Price = 60,
            Name = ""Product 100""
        },
        AriaLabel = ""Product 100"",
        IsEnabled = true,
        ItemType = BitDropdownItemType.Normal
    }
];

private IEnumerable<BitDropdownItem<string>> initialSelectedItems = [
    new()
    {
        Text = ""Product 100"",
        Value = ""100"",
        Data = new ProductDto {
            Id = 100,
            Price = 60,
            Name = ""Product 100""
        },
        AriaLabel = ""Product 100"",
        IsEnabled = true,
        ItemType = BitDropdownItemType.Normal
    },
    new()
    {
        Text = ""Product 99"",
        Value = ""99"",
        Data = new ProductDto {
            Id = 99,
            Price = 75,
            Name = ""Product 99""
        },
        AriaLabel = ""Product 99"",
        IsEnabled = true,
        ItemType = BitDropdownItemType.Normal
    }
];

protected override void OnInitialized()
{
    virtualizeItems1 = Enumerable.Range(1, 10_000)
                                 .Select(c => new BitDropdownItem<string> { Text = $""Category {c}"", Value = c.ToString() })
                                 .ToArray();

    virtualizeItems2 = Enumerable.Range(1, 10_000)
                                 .Select(c => new BitDropdownItem<string> { Text = $""Category {c}"", Value = c.ToString() })
                                 .ToArray();
}

private async ValueTask<BitDropdownItemsProviderResult<BitDropdownItem<string>>> LoadItems(
    BitDropdownItemsProviderRequest<BitDropdownItem<string>> request)
{
    try
    {
        // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview

        var query = new Dictionary<string, object?>()
        {
            { ""$top"", request.Count == 0 ? 50 : request.Count },
            { ""$skip"", request.StartIndex }
        };

        if (string.IsNullOrEmpty(request.Search) is false)
        {
            query.Add(""$filter"", $""contains(Name,'{request.Search}')"");
        }

        var url = NavManager.GetUriWithQueryParameters(""api/Products/GetProducts"", query);

        var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

        var items = data!.Items.Select(i => new BitDropdownItem<string>
        {
            Text = i.Name,
            Value = i.Id.ToString(),
            Data = i,
            AriaLabel = i.Name,
            IsEnabled = true,
            ItemType = BitDropdownItemType.Normal
        }).ToList();

        return BitDropdownItemsProviderResult.From(items, data!.TotalCount);
    }
    catch
    {
        return BitDropdownItemsProviderResult.From(new List<BitDropdownItem<string>>(), 0);
    }
}";

    private readonly string example25RazorCode = @"
<BitDropdown @bind-Values=""localizationValues""
             Chips
             MultiSelect
             ShowSelectAll
             ShowSearchBox
             ShowClearButton
             MaxDisplayedItems=""2""
             Label=""Zutaten""
             Items=""GetLocalizedItems()""
             Placeholder=""Zutaten auswählen""
             SearchBoxPlaceholder=""Suchen""
             SelectAllText=""Alle auswählen""
             NoResultsText=""Keine Treffer gefunden""
             OverflowTextFormat=""und {0} weitere""
             SearchResultsText=""{0} Treffer verfügbar""
             ClearButtonAriaLabel=""Auswahl löschen""
             SearchBoxAriaLabel=""Suchtext""
             SearchBoxClearButtonAriaLabel=""Text löschen""
             ChipsRemoveButtonAriaLabel=""{0} entfernen""
             MaxSelectedItems=""3""
             MaxSelectedItemsText=""Höchstens {0} Einträge ausgewählt"" />
<div>Values: @string.Join(',', localizationValues)</div>

<BitDropdown Label=""Leere Liste""
             Items=""emptyItems""
             Placeholder=""Zutat auswählen""
             EmptyText=""Es gibt hier nichts""
             TItem=""BitDropdownItem<string>"" TValue=""string"" />

<BitDropdown IsLoading
             Label=""Wird geladen""
             Items=""emptyItems""
             LoadingText=""Wird geladen...""
             Placeholder=""Zutat auswählen""
             TItem=""BitDropdownItem<string>"" TValue=""string"" />";
    private readonly string example25CsharpCode = @"
private List<BitDropdownItem<string>> GetLocalizedItems() =>
[
    new() { ItemType = BitDropdownItemType.Header, Text = ""Früchte"" },
    new() { Text = ""Apfel"", Value = ""f-app"" },
    new() { Text = ""Banane"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"" },
    new() { Text = ""Traube"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Gemüse"" },
    new() { Text = ""Brokkoli"", Value = ""v-bro"" },
    new() { Text = ""Karotte"", Value = ""v-car"" }
];

private readonly List<BitDropdownItem<string>> emptyItems = [];

private IEnumerable<string?> localizationValues = [""f-app"", ""f-ban"", ""v-bro""];";

    private readonly string example26RazorCode = @"
<BitDropdown Label=""CaretDownIconName""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             CaretDownIconName=""@BitIconName.ChevronDownMed"" />

<BitDropdown Label=""ClearButtonIconName""
             ShowClearButton
             Items=""GetBasicItems()""
             DefaultValue=""@(""f-app"")""
             Placeholder=""Select an item""
             ClearButtonIconName=""@BitIconName.ChromeClose"" />

<BitDropdown Label=""ItemCheckIconName""
             MultiSelect
             ShowSelectAll
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"" })""
             ItemCheckIconName=""@BitIconName.CheckMark"" />

<BitDropdown Label=""SearchBox icons""
             ShowSearchBox
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search item""
             SearchBoxIconName=""@BitIconName.Filter""
             SearchBoxClearIconName=""@BitIconName.EraseTool"" />

<BitDropdown Label=""ChipsRemoveIconName""
             Chips
             MultiSelect
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"" })""
             ChipsRemoveIconName=""@BitIconName.ChromeClose"" />

<BitDropdown Label=""Responsive panel icons""
             Combo
             Responsive
             Items=""comboBoxItems""
             DefaultValue=""@string.Empty""
             Placeholder=""Resize below the small breakpoint""
             ResponsiveCloseIconName=""@BitIconName.ChromeClose""
             ComboBoxAddButtonIconName=""@BitIconName.CircleAddition"" />";
    private readonly string example26CsharpCode = @"
private List<BitDropdownItem<string>> comboBoxItems =
[
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"" },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" }
];

private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example27RazorCode = @"
<BitDropdown @bind-Value=""closeOnSelectValue""
             CloseOnSelect=""false""
             Items=""GetBasicItems()""
             Placeholder=""Select an item""
             Label=""Single select that stays open"" />
<div>Value: @closeOnSelectValue</div>

<BitDropdown @bind-Values=""closeOnSelectValues""
             MultiSelect
             CloseOnSelect=""true""
             Items=""GetBasicItems()""
             Placeholder=""Select items""
             Label=""Multi select that closes on each pick"" />
<div>Values: @string.Join("", "", closeOnSelectValues)</div>";
    private readonly string example27CsharpCode = @"
private string? closeOnSelectValue;
private IEnumerable<string?> closeOnSelectValues = [];

private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example28RazorCode = @"
<BitDropdown @bind-Values=""tokenSeparatorValues""
             Responsive
             MultiSelect
             Combo Chips Dynamic
             Items=""comboBoxItems""
             TokenSeparators=""tokenSeparators""
             Label=""Multi select combo box with token separators""
             Placeholder=""Type or paste options separated by , or ;""
             DynamicValueGenerator=""(BitDropdownItem<string> item) => item.Text""
             OnDynamicAdd=""(BitDropdownItem<string> item) => HandleOnDynamicAdd(item)"" />
<div>Values: @string.Join(',', tokenSeparatorValues)</div>";
    private readonly string example28CsharpCode = @"
private char[] tokenSeparators = [',', ';'];
private IEnumerable<string?> tokenSeparatorValues = [];

private void HandleOnDynamicAdd(BitDropdownItem<string> item)
{
    comboBoxItems.Add(item);
}

private List<BitDropdownItem<string>> comboBoxItems = new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example29RazorCode = @"
<BitDropdown @bind-Value=""openOnFocusValue""
             OpenOnFocus
             Items=""GetBasicItems()""
             Placeholder=""Select an item""
             Label=""Open on focus"" />
<div>Value: @openOnFocusValue</div>";
    private readonly string example29CsharpCode = @"
private string? openOnFocusValue;

private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example30RazorCode = @"
<BitDropdown Label=""Category""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             Description=""Only the categories you have access to are listed."" />

<BitDropdown Combo
             Label=""ComboBox""
             Items=""comboBoxItems""
             DefaultValue=""@string.Empty""
             Placeholder=""Type to filter""
             Description=""Type a few letters to narrow the list down."" />

<BitDropdown Label=""DescriptionTemplate""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"">
    <DescriptionTemplate>
        <div class=""custom-description"">
            <BitIcon IconName=""@BitIconName.Info"" />
            <span>Nothing here is final &mdash; you can change it later.</span>
        </div>
    </DescriptionTemplate>
</BitDropdown>";
    private readonly string example30CsharpCode = @"
private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example31RazorCode = @"
<BitDropdown Label=""Primary""
             MultiSelect
             ShowSearchBox
             DefaultValues=""@(Array.Empty<string>())""
             Items=""GetBasicItems()""
             Color=""BitColor.Primary""
             Placeholder=""Select items"" />

<BitDropdown Label=""Secondary""
             MultiSelect
             ShowSearchBox
             DefaultValues=""@(Array.Empty<string>())""
             Items=""GetBasicItems()""
             Color=""BitColor.Secondary""
             Placeholder=""Select items"" />

<BitDropdown Label=""Tertiary""
             MultiSelect
             ShowSearchBox
             DefaultValues=""@(Array.Empty<string>())""
             Items=""GetBasicItems()""
             Color=""BitColor.Tertiary""
             Placeholder=""Select items"" />

<BitDropdown Label=""Info""
             MultiSelect
             ShowSearchBox
             DefaultValues=""@(Array.Empty<string>())""
             Items=""GetBasicItems()""
             Color=""BitColor.Info""
             Placeholder=""Select items"" />

<BitDropdown Label=""Success""
             MultiSelect
             ShowSearchBox
             DefaultValues=""@(Array.Empty<string>())""
             Items=""GetBasicItems()""
             Color=""BitColor.Success""
             Placeholder=""Select items"" />

<BitDropdown Label=""Warning""
             MultiSelect
             ShowSearchBox
             DefaultValues=""@(Array.Empty<string>())""
             Items=""GetBasicItems()""
             Color=""BitColor.Warning""
             Placeholder=""Select items"" />

<BitDropdown Label=""SevereWarning""
             MultiSelect
             ShowSearchBox
             DefaultValues=""@(Array.Empty<string>())""
             Items=""GetBasicItems()""
             Color=""BitColor.SevereWarning""
             Placeholder=""Select items"" />

<BitDropdown Label=""Error""
             MultiSelect
             ShowSearchBox
             DefaultValues=""@(Array.Empty<string>())""
             Items=""GetBasicItems()""
             Color=""BitColor.Error""
             Placeholder=""Select items"" />";
    private readonly string example31CsharpCode = @"
private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example32RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitDropdown Label=""Caret down icon (external)""
             CaretDownIcon=""@BitIconInfo.Css(""fa-solid fa-circle-chevron-down"")""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Clear button icon (external)""
             ShowClearButton
             ClearButtonIcon=""@BitIconInfo.Css(""fa-solid fa-circle-xmark"")""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Chips remove icon (external)""
             Chips
             MultiSelect
             ChipsRemoveIcon=""@BitIconInfo.Css(""bi bi-x-circle"")""
             Items=""GetBasicItems()""
             DefaultValues=""@(Array.Empty<string>())""
             Placeholder=""Select items"" />

<BitDropdown Label=""Search box icons (external)""
             ShowSearchBox
             SearchBoxIcon=""@BitIconInfo.Css(""fa-solid fa-magnifying-glass"")""
             SearchBoxClearIcon=""@BitIconInfo.Css(""fa-solid fa-circle-xmark"")""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Item check icon (external)""
             MultiSelect
             ItemCheckIcon=""@BitIconInfo.Css(""fa-solid fa-heart"")""
             Items=""GetBasicItems()""
             DefaultValues=""@(Array.Empty<string>())""
             Placeholder=""Select items"" />

<BitDropdown Label=""Item icons (IconName - Fluent UI)""
             Items=""GetExternalIconItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Item icons (Icon - FontAwesome)""
             Items=""GetExternalIconFaItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Item icons (Icon - Bootstrap Icons)""
             Items=""GetExternalIconBiItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />";
    private readonly string example32CsharpCode = @"
private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private List<BitDropdownItem<string>> GetExternalIconItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"", IconName = nameof(BitIconName.AllApps) },
    new() { Text = ""Banana"", Value = ""f-ban"", IconName = nameof(BitIconName.Calculator) },
    new() { Text = ""Orange"", Value = ""f-ora"", IconName = nameof(BitIconName.FavoriteStar), IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"", IconName = nameof(BitIconName.Edit) },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"", IconName = nameof(BitIconName.Health) },
    new() { Text = ""Carrot"", Value = ""v-car"", IconName = nameof(BitIconName.Add) },
    new() { Text = ""Lettuce"", Value = ""v-let"", IconName = nameof(BitIconName.ChevronDown) }
};

private List<BitDropdownItem<string>> GetExternalIconFaItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"", Icon = BitIconInfo.Css(""fa-solid fa-apple-whole"") },
    new() { Text = ""Banana"", Value = ""f-ban"", Icon = BitIconInfo.Css(""fa-solid fa-moon"") },
    new() { Text = ""Orange"", Value = ""f-ora"", Icon = BitIconInfo.Fa(""solid lemon""), IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"", Icon = BitIconInfo.Css(""fa-solid fa-droplet"") },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"", Icon = BitIconInfo.Css(""fa-solid fa-seedling"") },
    new() { Text = ""Carrot"", Value = ""v-car"", Icon = BitIconInfo.Css(""fa-solid fa-carrot"") },
    new() { Text = ""Lettuce"", Value = ""v-let"", Icon = BitIconInfo.Css(""fa-solid fa-leaf"") }
};

private List<BitDropdownItem<string>> GetExternalIconBiItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"", Icon = BitIconInfo.Bi(""apple"") },
    new() { Text = ""Banana"", Value = ""f-ban"", Icon = BitIconInfo.Bi(""flower1"") },
    new() { Text = ""Orange"", Value = ""f-ora"", Icon = BitIconInfo.Css(""bi bi-sun""), IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"", Icon = BitIconInfo.Bi(""droplet-fill"") },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"", Icon = BitIconInfo.Bi(""tree-fill"") },
    new() { Text = ""Carrot"", Value = ""v-car"", Icon = BitIconInfo.Bi(""egg"") },
    new() { Text = ""Lettuce"", Value = ""v-let"", Icon = BitIconInfo.Bi(""flower2"") }
};";

    private readonly string example33RazorCode = @"
<BitDropdown Label=""Small""
             ShowSearchBox
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Size=""BitSize.Small""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Medium""
             ShowSearchBox
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Size=""BitSize.Medium""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Large""
             ShowSearchBox
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Size=""BitSize.Large""
             Placeholder=""Select an item"" />";
    private readonly string example33CsharpCode = @"
private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};";

    private readonly string example34RazorCode = @"
<style>
    .custom-class {
        margin-inline: 1rem;
        box-shadow: dodgerblue 0 0 0.5rem;
        text-shadow: dodgerblue 0 0 0.5rem;
    }


    .custom-fruit {
        background-color: #a5104457;
    }

    .custom-veg {
        background-color: #1c73324d;
    }


    .custom-callout {
        border-radius: 1rem;
        border-color: lightgray;
        backdrop-filter: blur(20px);
        background-color: transparent;
        box-shadow: darkgray 0 0 0.5rem;
    }

    .custom-container, .custom-container::after {
        border-radius: 1rem;
    }

    .custom-item-button {
        border-bottom: 1px solid gray;
    }

    .custom-item-button:hover {
        background-color: rgba(255, 255, 255, 0.2);
    }

    .custom-scroll-container div:last-child .custom-item-button {
        border-bottom: none;
    }
</style>


<BitDropdown Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             Style=""margin: 1rem; box-shadow: aqua 0 0 0.5rem; text-shadow: aqua 0 0 0.5rem;"" />

<BitDropdown Class=""custom-class"" 
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />


<BitDropdown Items=""GetStyleClassItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"" />


<BitDropdown Label=""Styles""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             Styles=""@(new() { Label = ""text-shadow: dodgerblue 0 0 0.5rem;"",
                               Container = ""box-shadow: dodgerblue 0 0 0.5rem; border-color: lightskyblue; color: lightskyblue;"",
                               ItemHeader = ""color: dodgerblue; text-shadow: dodgerblue 0 0 0.5rem;"",
                               ItemButton = ""color: lightskyblue"",
                               Callout = ""border-radius: 0.25rem; box-shadow: lightskyblue 0 0 0.5rem;"" })"" />

<BitDropdown Label=""Classes""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             Classes=""@(new() { Callout = ""custom-callout"",
                                Container = ""custom-container"",
                                ItemButton = ""custom-item-button"",
                                ScrollContainer = ""custom-scroll-container"" })"" />";
    private readonly string example34CsharpCode = @"
private List<BitDropdownItem<string>> GetBasicItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"" },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private List<BitDropdownItem<string>> GetStyleClassItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Fruits"", Style = ""text-align: center;"" },
    new() { Text = ""Apple"", Value = ""f-app"", Class = ""custom-fruit"" },
    new() { Text = ""Banana"", Value = ""f-ban"", Class = ""custom-fruit"" },
    new() { Text = ""Orange"", Value = ""f-ora"", IsEnabled = false, Class = ""custom-fruit"" },
    new() { Text = ""Grape"", Value = ""f-gra"", Class = ""custom-fruit"" },
    new() { ItemType = BitDropdownItemType.Divider, Style = ""padding: 0 0.25rem;"" },
    new() { ItemType = BitDropdownItemType.Header, Text = ""Vegetables"", Style = ""text-align: center;"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"", Class = ""custom-veg"" },
    new() { Text = ""Carrot"", Value = ""v-car"", Class = ""custom-veg"" },
    new() { Text = ""Lettuce"", Value = ""v-let"", Class = ""custom-veg"" }
};";

    private readonly string example35RazorCode = @"
<BitDropdown Label=""تک انتخابی""
             Items=""GetRtlItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""لطفا انتخاب کنید""
             Dir=""BitDir.Rtl"" />

<BitDropdown Label=""چند انتخابی""
             MultiSelect
             Dir=""BitDir.Rtl""
             Items=""GetRtlItems()""
             DefaultValues=""@(Array.Empty<string>())""
             Placeholder=""انتخاب چند گزینه ای"" />

<BitDropdown Label=""تک انتخابی ریسپانسیو""
             Responsive
             Dir=""BitDir.Rtl""
             Items=""GetRtlItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""لطفا انتخاب کنید"" />";
    private readonly string example35CsharpCode = @"
private List<BitDropdownItem<string>> GetRtlItems() => new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""میوه ها"" },
    new() { Text = ""سیب"", Value = ""f-app"" },
    new() { Text = ""موز"", Value = ""f-ban"" },
    new() { Text = ""پرتقال"", Value = ""f-ora"", IsEnabled = false },
    new() { Text = ""انگور"", Value = ""f-gra"" },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""سیزیجات"" },
    new() { Text = ""کلم بروكلی"", Value = ""v-bro"" },
    new() { Text = ""هویج"", Value = ""v-car"" },
    new() { Text = ""کاهو"", Value = ""v-let"" }
};";
}
