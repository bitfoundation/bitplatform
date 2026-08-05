namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class _BitDropdownOptionDemo
{
    private readonly string example1RazorCode = @"
<BitDropdown Label=""Single select""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <BitDropdownOption ItemType=""BitDropdownItemType.Header"" Text=""Fruits"" Value=""@string.Empty"" />
    <BitDropdownOption Text=""Apple"" Value=""@(""f-app"")"" />
    <BitDropdownOption Text=""Banana"" Value=""@(""f-ban"")"" />
    <BitDropdownOption Text=""Orange"" Value=""@(""f-ora"")"" IsEnabled=""false"" />
    <BitDropdownOption Text=""Grape"" Value=""@(""f-gra"")"" />
    <BitDropdownOption ItemType=""BitDropdownItemType.Divider"" Value=""@string.Empty"" />
    <BitDropdownOption ItemType=""BitDropdownItemType.Header"" Text=""Vegetables"" Value=""@string.Empty"" />
    <BitDropdownOption Text=""Broccoli"" Value=""@(""v-bro"")"" />
    <BitDropdownOption Text=""Carrot"" Value=""@(""v-car"")"" />
    <BitDropdownOption Text=""Lettuce"" Value=""@(""v-let"")"" />
</BitDropdown>

<BitDropdown Label=""Multi select""
             MultiSelect
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Required"" Required
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""PreserveCalloutWidth""
             PreserveCalloutWidth
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Disabled""
             DefaultValue=""@(""f-ora"")""
             Placeholder=""Select an item""
             IsEnabled=""false""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""ReadOnly""
             ReadOnly
             DefaultValue=""@(""f-app"")""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Custom delimiter""
             MultiSelect
             MultiSelectDelimiter="" - ""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"" })""
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Title""
             Title=""Pick your favorite fruit or vegetable""
             DefaultValue=""@string.Empty""
             Placeholder=""Hover me""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>";
    private readonly string example1CsharpCode = @"
private readonly List<BitDropdownItem<string>> basicItems =
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
];";

    private readonly string example2RazorCode = @"
<BitDropdown Label=""Grouped options""
             DefaultValue=""@("""")""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <Options>
        @foreach (var item in groupedItems)
        {
            <BitDropdownOption Text=""@item.Text"" Value=""@item.Value"" ItemType=""item.ItemType"" />
        }
    </Options>
</BitDropdown>

<BitDropdown Label=""StickyHeaders""
             StickyHeaders
             MultiSelect
             DefaultValues=""@(Array.Empty<string>())""
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <Options>
        @foreach (var item in groupedItems)
        {
            <BitDropdownOption Text=""@item.Text"" Value=""@item.Value"" ItemType=""item.ItemType"" />
        }
    </Options>
</BitDropdown>";
    private readonly string example2CsharpCode = @"
private readonly List<BitDropdownItem<string>> groupedItems =
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
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>""
             TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Suffix""
             Suffix=""kg""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>""
             TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Prefix and Suffix""
             Prefix=""Fruits:""
             Suffix=""kg""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>""
             TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Templates""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>""
             TValue=""string"">
    <PrefixTemplate>
        <BitIcon IconName=""@BitIconName.ShoppingCart"" Style=""padding-inline:0.5rem"" />
    </PrefixTemplate>
    <SuffixTemplate>
        <BitIcon IconName=""@BitIconName.Info"" Style=""padding-inline:0.5rem"" />
    </SuffixTemplate>
    <Options>
        @foreach (var item in basicItems)
        {
            <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
        }
    </Options>
</BitDropdown>

<BitDropdown Label=""Disabled""
             Prefix=""Fruits:""
             Suffix=""kg""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>""
             TValue=""string""
             IsEnabled=""false"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>";
    private readonly string example3CsharpCode = @"
private readonly List<BitDropdownItem<string>> basicItems =
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
];";

    private readonly string example4RazorCode = @"
<BitDropdown Label=""Single select"" FitWidth
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Multi select""
             FitWidth
             MultiSelect
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>";
    private readonly string example4CsharpCode = @"
private readonly List<BitDropdownItem<string>> basicItems =
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
];";

    private readonly string example5RazorCode = @"
<BitDropdown NoBorder
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown NoBorder
             MultiSelect
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<div style=""padding:0.5rem;border-radius:0.5rem;background:linear-gradient(90deg,#ff00cc7f,#3333997f)"">
    <BitDropdown Transparent
                 Placeholder=""Select an item""
                 TItem=""BitDropdownOption<string>"" TValue=""string"">
        @foreach (var item in basicItems)
        {
            <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
        }
    </BitDropdown>
</div>";
    private readonly string example5CsharpCode = @"
private readonly List<BitDropdownItem<string>> basicItems =
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
];";

    private readonly string example6RazorCode = @"
<BitDropdown Label=""Responsive Dropdown""
             Responsive
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <BitDropdownOption ItemType=""BitDropdownItemType.Header"" Text=""Fruits"" Value=""@string.Empty"" />
    <BitDropdownOption Text=""Apple"" Value=""@(""f-app"")"" />
    <BitDropdownOption Text=""Banana"" Value=""@(""f-ban"")"" />
    <BitDropdownOption Text=""Orange"" Value=""@(""f-ora"")"" IsEnabled=""false"" />
    <BitDropdownOption Text=""Grape"" Value=""@(""f-gra"")"" />
    <BitDropdownOption ItemType=""BitDropdownItemType.Divider"" Value=""@string.Empty"" />
    <BitDropdownOption ItemType=""BitDropdownItemType.Header"" Text=""Vegetables"" Value=""@string.Empty"" />
    <BitDropdownOption Text=""Broccoli"" Value=""@(""v-bro"")"" />
    <BitDropdownOption Text=""Carrot"" Value=""@(""v-car"")"" />
    <BitDropdownOption Text=""Lettuce"" Value=""@(""v-let"")"" />
</BitDropdown>";

    private readonly string example7RazorCode = @"
<BitDropdown Label=""All""
             Placeholder=""Select an item""
             DropDirection=""BitDropDirection.All""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in dropDirectionItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" />
    }
</BitDropdown>

<BitDropdown Label=""TopAndBottom""
             Placeholder=""Select an item""
             DropDirection=""BitDropDirection.TopAndBottom""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in dropDirectionItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" />
    }
</BitDropdown>";
    private readonly string example7CsharpCode = @"
private ICollection<BitDropdownItem<string>> dropDirectionItems = default!;

protected override void OnInitialized()
{
    dropDirectionItems = Enumerable.Range(1, 15)
                                   .Select(c => new BitDropdownItem<string> { Value = c.ToString(), Text = $""Category {c}"" })
                                   .ToArray();
}";

    private readonly string example8RazorCode = @"
<BitDropdown @bind-Value=""clearValue""
             ShowClearButton
             Label=""Single select dropdown""
             Placeholder=""Select an option""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Value: @clearValue</div>


<BitDropdown @bind-Values=""clearValues""
             MultiSelect
             ShowClearButton
             Placeholder=""Select options""
             Label=""Multi select dropdown""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Values: @string.Join(',', clearValues)</div>

<BitDropdown ShowClearButton
             DefaultValue=""@(""f-app"")""
             Label=""Single select dropdown""
             Placeholder=""Select an option""
             OnClear=""() => clearCounter++""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>OnClear count: @clearCounter</div>";
    private readonly string example8CsharpCode = @"
private int clearCounter;
private string? clearValue = ""f-app"";
private IEnumerable<string?> clearValues = [""f-app"", ""f-ban""];

private readonly List<BitDropdownItem<string>> basicItems =
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
];";

    private readonly string example9RazorCode = @"
<BitDropdown Label=""Single select & auto focus""
             Responsive
             ShowSearchBox
             AutoFocusSearchBox
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Multi select""
             Responsive
             MultiSelect
             ShowSearchBox
             Placeholder=""Select items""
             SearchBoxPlaceholder=""Search items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>



<BitDropdown Label=""Single select & auto focus""
             Responsive
             ShowSearchBox
             AutoFocusSearchBox
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search item""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             SearchFunction=""(items, text) => items.Where(i => i.Text?.StartsWith(text, StringComparison.OrdinalIgnoreCase) ?? false).ToArray()"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Multi select""
             Responsive
             MultiSelect
             ShowSearchBox
             Placeholder=""Select items""
             SearchBoxPlaceholder=""Search items""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             SearchFunction=""(items, text) => items.Where(i => i.Text?.EndsWith(text, StringComparison.OrdinalIgnoreCase) ?? false).ToArray()"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>";
    private readonly string example9CsharpCode = @"
private readonly List<BitDropdownItem<string>> basicItems =
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
];";

    private readonly string example10RazorCode = @"
<BitDropdown Label=""SearchMode: StartsWith""
             ShowSearchBox
             AutoFocusSearchBox
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Type a first letter""
             SearchMode=""BitDropdownSearchMode.StartsWith""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""MinSearchLength: 3""
             ShowSearchBox
             AutoFocusSearchBox
             MinSearchLength=""3""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Type at least 3 characters""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""SearchIgnoreDiacritics""
             ShowSearchBox
             AutoFocusSearchBox
             HighlightSearch
             SearchIgnoreDiacritics
             DefaultValue=""@string.Empty""
             Placeholder=""Select a name""
             SearchBoxPlaceholder=""Try jose, muller or angstrom""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in accentedItems)
    {
        <BitDropdownOption Text=""@item.Text"" Value=""item.Value"" />
    }
</BitDropdown>

<BitDropdown Label=""HighlightSearch""
             ShowSearchBox
             AutoFocusSearchBox
             HighlightSearch
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Highlight in the ComboBox""
             Combo
             HighlightSearch
             DefaultValue=""@string.Empty""
             Placeholder=""Type to filter""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in comboBoxItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>";
    private readonly string example10CsharpCode = @"
private readonly List<BitDropdownItem<string>> accentedItems =
[
    new() { Text = ""José"", Value = ""n-jos"" },
    new() { Text = ""Renée"", Value = ""n-ren"" },
    new() { Text = ""Müller"", Value = ""n-mul"" },
    new() { Text = ""Ångström"", Value = ""n-ang"" },
    new() { Text = ""Zoë"", Value = ""n-zoe"" },
    new() { Text = ""Smith"", Value = ""n-smi"" }
];

private readonly List<BitDropdownItem<string>> basicItems =
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

private readonly List<BitDropdownItem<string>> comboBoxItems =
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
];";

    private readonly string example11RazorCode = @"
<BitDropdown Label=""Immediate""
             ShowSearchBox
             Immediate
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search item""
             OnSearch=""v => immediateSearchValue = v""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Search value: <b>@immediateSearchValue</b></div>

<BitDropdown Label=""Immediate + DebounceTime (500ms)""
             ShowSearchBox
             Immediate
             DebounceTime=""500""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search item""
             OnSearch=""v => debouncedSearchValue = v""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Search value: <b>@debouncedSearchValue</b></div>

<BitDropdown Label=""Immediate ComboBox + ThrottleTime (500ms)""
             Combo
             Immediate
             ThrottleTime=""500""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in comboBoxItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>";
    private readonly string example11CsharpCode = @"
private string? immediateSearchValue;
private string? debouncedSearchValue;

private readonly List<BitDropdownItem<string>> basicItems =
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

private readonly List<BitDropdownItem<string>> comboBoxItems =
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
];";

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
                 Placeholder=""Select an item""
                 TItem=""BitDropdownOption<string>"" TValue=""string"">
        @foreach (var item in basicItems)
        {
            <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
        }
    </BitDropdown>
    <ValidationMessage For=""@(() => validationModel.Category)"" />

    <BitDropdown @bind-Values=""validationModel.Products""
                 MultiSelect
                 Placeholder=""Select items""
                 Label=""Select min 1 and max 2 items""
                 TItem=""BitDropdownOption<string>"" TValue=""string"">
        @foreach (var item in basicItems)
        {
            <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
        }
    </BitDropdown>
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

private readonly List<BitDropdownItem<string>> basicItems =
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
];";

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
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <HeaderTemplate Context=""item"">
        <div class=""custom-drp custom-drp-header"">
            <BitIcon IconName=""@((item.Data as DropdownItemData)?.IconName)"" />
            <div>@item.Text</div>
        </div>
    </HeaderTemplate>
    <Options>
        @foreach (var item in dataItems)
        {
            <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" Data=""item.Data"" />
        }
    </Options>
</BitDropdown>

<BitDropdown Label=""Text & Item templates""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
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
    <Options>
        @foreach (var item in dataItems)
        {
            <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" Data=""item.Data"" />
        }
    </Options>
</BitDropdown>

<BitDropdown Label=""Placeholder template""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <PlaceholderTemplate Context=""dropdown"">
        <div class=""custom-drp custom-drp-ph"">
            <BitIcon IconName=""@BitIconName.MessageFill"" />
            <div>@dropdown.Placeholder</div>
        </div>
    </PlaceholderTemplate>
    <Options>
        @foreach (var item in dataItems)
        {
            <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" />
        }
    </Options>
</BitDropdown>

<BitDropdown Label=""Label template""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <LabelTemplate>
        <div class=""custom-drp custom-drp-lbl"">
            <div>Custom label</div>
            <BitIcon IconName=""@BitIconName.Info"" AriaLabel=""Info"" />
        </div>
    </LabelTemplate>
    <Options>
        @foreach (var item in dataItems)
        {
            <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" />
        }
    </Options>
</BitDropdown>

<BitDropdown Label=""CaretDownIconName""
             Placeholder=""Select an item""
             CaretDownIconName=""@BitIconName.ScrollUpDown""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in dataItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" />
    }
</BitDropdown>

<BitDropdown Label=""CaretDownTemplate""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <CaretDownTemplate>
        <BitIcon IconName=""@BitIconName.FavoriteStar"" Style=""font-size:0.875rem"" />
    </CaretDownTemplate>
    <Options>
        @foreach (var item in dataItems)
        {
            <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" />
        }
    </Options>
</BitDropdown>

<BitDropdown Label=""Callout templates""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <CalloutHeaderTemplate>
        <div Style=""padding:0.5rem;border-bottom:1px solid #555"">Best in the world</div>
    </CalloutHeaderTemplate>
    <CalloutFooterTemplate>
        <BitActionButton IconName=""@BitIconName.Add"">New Item</BitActionButton>
    </CalloutFooterTemplate>
    <Options>
        @foreach (var item in dataItems)
        {
            <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" />
        }
    </Options>
</BitDropdown>";
    private readonly string example13CsharpCode = @"
public class DropdownItemData
{
    public string? IconName { get; set; }
}

private readonly List<BitDropdownItem<string>> dataItems =
[
    new() { ItemType = BitDropdownItemType.Header, Text = ""Items"", Data = new DropdownItemData { IconName = ""BulletedList2"" }  },
    new() { Text = ""Item a"", Value = ""A"", Data = new DropdownItemData { IconName = ""Memo"" } },
    new() { Text = ""Item b"", Value = ""B"", Data = new DropdownItemData { IconName = ""Print"" } },
    new() { Text = ""Item c"", Value = ""C"", Data = new DropdownItemData { IconName = ""ShoppingCart"" } },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""More Items"", Data = new DropdownItemData { IconName = ""BulletedTreeList"" }  },
    new() { Text = ""Item d"", Value = ""D"", Data = new DropdownItemData { IconName = ""Train"" } },
    new() { Text = ""Item e"", Value = ""E"", Data = new DropdownItemData { IconName = ""Repair"" } },
    new() { Text = ""Item f"", Value = ""F"", Data = new DropdownItemData { IconName = ""Running"" } }
];";

    private readonly string example14RazorCode = @"
<BitDropdown @bind-Value=""controlledValue""
             Label=""Single select""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <BitDropdownOption ItemType=""BitDropdownItemType.Header"" Text=""Fruits"" Value=""@string.Empty"" />
    <BitDropdownOption Text=""Apple"" Value=""@(""f-app"")"" />
    <BitDropdownOption Text=""Banana"" Value=""@(""f-ban"")"" />
    <BitDropdownOption Text=""Orange"" Value=""@(""f-ora"")"" IsEnabled=""false"" />
    <BitDropdownOption Text=""Grape"" Value=""@(""f-gra"")"" />
    <BitDropdownOption ItemType=""BitDropdownItemType.Divider"" Value=""@string.Empty"" />
    <BitDropdownOption ItemType=""BitDropdownItemType.Header"" Text=""Vegetables"" Value=""@string.Empty"" />
    <BitDropdownOption Text=""Broccoli"" Value=""@(""v-bro"")"" />
    <BitDropdownOption Text=""Carrot"" Value=""@(""v-car"")"" />
    <BitDropdownOption Text=""Lettuce"" Value=""@(""v-let"")"" />
</BitDropdown>
<div>Selected Value: @controlledValue</div>

<BitDropdown @bind-Values=""controlledValues""
             MultiSelect
             Label=""Multi select""
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Selected Values: @string.Join("","", controlledValues)</div>



<BitDropdown Label=""Single select""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             OnChange=""(string value) => changedValue = value"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Changed Value: @changedValue</div>

<BitDropdown Label=""Multi select""
             MultiSelect
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             OnValuesChange=""(IEnumerable<string> values) => changedValues = values"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Changed Values: @string.Join("","", changedValues)</div>



<BitDropdown Label=""Single select""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             OnSelectItem=""(BitDropdownOption<string> item) => selectedItem1 = item"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Selected Value: @selectedItem1?.Value</div>

<BitDropdown Label=""Multi select""
             MultiSelect
             Placeholder=""Select items""
             DefaultValues=""@(Array.Empty<string>())""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             OnSelectItem=""(BitDropdownOption<string> item) => selectedItem2 = item"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Selected Value: @selectedItem2?.Value</div>

<BitDropdown Label=""Multi select""
             MultiSelect
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"" })""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             OnSelectItem=""(BitDropdownOption<string> item) => pickedItem = item""
             OnDeselectItem=""(BitDropdownOption<string> item) => deselectedItem = item"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Last picked item: @pickedItem?.Text</div>
<div>Last unselected item: @deselectedItem?.Text</div>

<BitDropdown Label=""Single select""
             Reselectable
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             OnSelectItem=""(BitDropdownOption<string> item) => selectItemCounter++"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>OnSelectItem count: @selectItemCounter</div>

<BitButton OnClick=""() => isDropdownOpen = true"">Open the dropdown</BitButton>
<BitDropdown @bind-IsOpen=""isDropdownOpen""
             Label=""Single select""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Single select""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             OnOpen=""HandleOnCalloutOpen""
             OnClose=""HandleOnCalloutClose""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>The callout is @calloutState.</div>

<BitDropdown Label=""Single select""
             DefaultValue=""@string.Empty""
             Placeholder=""Tab into me""
             OnFocusIn=""HandleOnFocusIn""
             OnFocusOut=""HandleOnFocusOut""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>The dropdown is @focusState.</div>";
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
private IEnumerable<string?> controlledValues = [""f-app"", ""f-ban""];

private string? changedValue;
private IEnumerable<string> changedValues = [];

private BitDropdownOption<string>? selectedItem1;
private BitDropdownOption<string>? selectedItem2;
private BitDropdownOption<string>? pickedItem;
private BitDropdownOption<string>? deselectedItem;

private readonly List<BitDropdownItem<string>> basicItems =
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
];";

    private readonly string example15RazorCode = @"
<BitDropdown @bind-Value=""autoSelectValue""
             Combo
             Immediate
             AutoSelectFirstMatch
             Label=""AutoSelectFirstMatch""
             Placeholder=""Type a few letters and press Enter""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in comboBoxItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<br />
<div>Value: @autoSelectValue</div>

<BitDropdown @bind-Value=""comboBoxValueSample1""
             Combo
             Responsive
             Label=""Single select combo box""
             Placeholder=""Select an option""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in comboBoxItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<br />
<div>Value: @comboBoxValueSample1</div>

<BitDropdown @bind-Values=""comboBoxValues1""
             Combo 
             MultiSelect Responsive
             Label=""Multi select combo box""
             Placeholder=""Select an option""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in comboBoxItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<br />
<div>Values: @string.Join(',', comboBoxValues1)</div>";
    private readonly string example15CsharpCode = @"
private string? autoSelectValue;
private string comboBoxValueSample1 = default!;
private IEnumerable<string> comboBoxValues1 = [];

private readonly List<BitDropdownItem<string>> comboBoxItems =
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
];";

    private readonly string example16RazorCode = @"
<BitDropdown @bind-Value=""comboBoxValueSample2""
             Combo Chips Responsive
             Label=""Single select combo box & chips""
             Placeholder=""Select an option""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in comboBoxItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<br />
<div>Value: @comboBoxValueSample2</div>
<br /><br />
<BitDropdown @bind-Values=""comboBoxValues2""
             Combo Chips 
             Responsive MultiSelect
             Placeholder=""Select an option""
             Label=""Multi select combo box & chips""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in comboBoxItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<br />
<div>Values: @string.Join(',', comboBoxValues2)</div>";
    private readonly string example16CsharpCode = @"
private string comboBoxValueSample2 = default!;
private IEnumerable<string> comboBoxValues2 = [];

private readonly List<BitDropdownItem<string>> comboBoxItems =
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
];";

    private readonly string example17RazorCode = @"
<BitDropdown Label=""MaxDisplayedItems (chips)""
             Chips
             MultiSelect
             MaxDisplayedItems=""2""
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"", ""f-gra"", ""v-car"" })""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""OverflowTextFormat""
             Chips
             MultiSelect
             MaxDisplayedItems=""2""
             OverflowTextFormat=""and {0} more""
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"", ""f-gra"", ""v-car"" })""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""SelectedItemsTextFormat""
             MultiSelect
             MaxDisplayedItems=""2""
             SelectedItemsTextFormat=""{0} fruits and vegetables""
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"", ""f-gra"", ""v-car"" })""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""AutoClearSearch""
             Combo Chips
             MultiSelect
             AutoClearSearch
             DefaultValues=""@(Array.Empty<string>())""
             Placeholder=""Type to filter, then pick""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in comboBoxItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""HideSelectedItems""
             Chips
             MultiSelect
             HideSelectedItems
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"" })""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>";
    private readonly string example17CsharpCode = @"
private readonly List<BitDropdownItem<string>> basicItems =
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

private readonly List<BitDropdownItem<string>> comboBoxItems =
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
];";

    private readonly string example18RazorCode = @"
<BitDropdown @bind-Value=""comboBoxValueSample3""
             Combo Dynamic
             Responsive
             Placeholder=""Select an option""
             Label=""Single select combo box & dynamic""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             DynamicValueGenerator=""(BitDropdownOption<string> item) => item.Text""
             OnDynamicAdd=""(BitDropdownOption<string> item) => HandleOnDynamicAdd(item)"">
    @foreach (var item in comboBoxItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Value: @comboBoxValueSample3</div>
        
<BitDropdown @bind-Value=""comboBoxValueSample4""
             Responsive
             Combo Chips Dynamic
             Placeholder=""Select an option""
             Label=""Single select combo box, chips & dynamic""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             DynamicValueGenerator=""(BitDropdownOption<string> item) => item.Text""
             OnDynamicAdd=""(BitDropdownOption<string> item) => HandleOnDynamicAdd(item)"">
    @foreach (var item in comboBoxItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Value: @comboBoxValueSample4</div>

<BitDropdown @bind-Values=""comboBoxValues3""
             Responsive
             MultiSelect
             Combo Chips Dynamic
             Placeholder=""Select options""
             Label=""Multi select combo box, chips & dynamic""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             DynamicValueGenerator=""(BitDropdownOption<string> item) => item.Text""
             OnDynamicAdd=""(BitDropdownOption<string> item) => HandleOnDynamicAdd(item)"">
    @foreach (var item in comboBoxItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Values: @string.Join(',', comboBoxValues3)</div>";
    private readonly string example18CsharpCode = @"
private string comboBoxValueSample3 = default!;
private string comboBoxValueSample4 = default!;
private IEnumerable<string> comboBoxValues3 = [];

private void HandleOnDynamicAdd(BitDropdownOption<string> item)
{
    comboBoxItems.Add(new() { Text = item.Text, Value = item.Value });
}

private readonly List<BitDropdownItem<string>> comboBoxItems =
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
];";

    private readonly string example19RazorCode = @"
<BitDropdown Label=""Single select""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Multi select""
             MultiSelect
             Placeholder=""Select items""
             DefaultValues=""@(Array.Empty<string>())""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>";
    private readonly string example19CsharpCode = @"
private readonly List<BitDropdownItem<string>> basicItems =
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
];";

    private readonly string example20RazorCode = @"
<BitDropdown @bind-Values=""selectAllValues""
             MultiSelect
             ShowSelectAll
             Placeholder=""Select items""
             Label=""Select all""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Values: @string.Join(',', selectAllValues)</div>

<BitDropdown MultiSelect
             ShowSelectAll
             ShowSearchBox
             SelectAllText=""Select all of them""
             Placeholder=""Select items""
             DefaultValues=""@(Array.Empty<string>())""
             SearchBoxPlaceholder=""Search items""
             Label=""Custom text & search""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>";
    private readonly string example20CsharpCode = @"
private IEnumerable<string?> selectAllValues = [];

private readonly List<BitDropdownItem<string>> basicItems =
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
];";

    private readonly string example21RazorCode = @"
<BitDropdown @bind-Values=""maxSelectedValues""
             MultiSelect
             MaxSelectedItems=""2""
             Placeholder=""Select up to 2 items""
             Label=""Max 2 items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Values: @string.Join(',', maxSelectedValues)</div>";
    private readonly string example21CsharpCode = @"
private IEnumerable<string?> maxSelectedValues = [];

private readonly List<BitDropdownItem<string>> basicItems =
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
];";

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
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"" />

<BitDropdown Label=""EmptyText""
             Placeholder=""Select an item""
             EmptyText=""There is nothing here!""
             TItem=""BitDropdownOption<string>"" TValue=""string"" />

<BitDropdown Label=""EmptyTemplate""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <EmptyTemplate>
        <div class=""custom-drp custom-drp-empty"">
            <BitIcon IconName=""@BitIconName.SearchIssue"" />
            <div>Nothing to show!</div>
        </div>
    </EmptyTemplate>
</BitDropdown>

<BitDropdown Label=""Search without result""
             ShowSearchBox
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search for anything""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""NoResultsText""
             ShowSearchBox
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search for anything""
             NoResultsText=""Nothing matched your search""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""NoResultsTemplate""
             ShowSearchBox
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search for anything""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <NoResultsTemplate>
        <div class=""custom-drp custom-drp-empty"">
            <BitIcon IconName=""@BitIconName.SearchAndApps"" />
            <div>No match. Try another term!</div>
        </div>
    </NoResultsTemplate>
    <Options>
        @foreach (var item in basicItems)
        {
            <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
        }
    </Options>
</BitDropdown>";
    private readonly string example22CsharpCode = @"
private readonly List<BitDropdownItem<string>> basicItems =
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
];";

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
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""LoadingText""
             IsLoading
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             LoadingText=""Fetching the products...""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""LoadingTemplate""
             IsLoading
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <LoadingTemplate>
        <div class=""custom-drp custom-drp-empty"">
            <BitIcon IconName=""@BitIconName.Sync"" />
            <div>Just a moment...</div>
        </div>
    </LoadingTemplate>
    <Options>
        @foreach (var item in basicItems)
        {
            <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
        }
    </Options>
</BitDropdown>

<BitButton OnClick=""LoadDelayedItems"" IsLoading=""isLoadingItems"">Load the items</BitButton>
<BitDropdown Label=""Products""
             IsLoading=""isLoadingItems""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in delayedItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>";
    private readonly string example23CsharpCode = @"
private readonly List<BitDropdownItem<string>> basicItems =
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

private bool isLoadingItems;
private List<BitDropdownItem<string>> delayedItems = [];

private async Task LoadDelayedItems()
{
    isLoadingItems = true;
    delayedItems = [];

    await Task.Delay(2000);

    delayedItems = [.. basicItems];
    isLoadingItems = false;
}";

    private readonly string example24RazorCode = @"
<div>Virtualizing large item lists is demonstrated with the Items and ItemsProvider APIs; check the Item and Custom tabs for the examples.</div>";

    private readonly string example25RazorCode = @"
<BitDropdown @bind-Values=""localizationValues""
             Chips
             MultiSelect
             ShowSelectAll
             ShowSearchBox
             ShowClearButton
             MaxDisplayedItems=""2""
             Label=""Zutaten""
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
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <Options>
        <BitDropdownOption Text=""Früchte"" ItemType=""BitDropdownItemType.Header"" />
        <BitDropdownOption Text=""Apfel"" Value=""f-app"" />
        <BitDropdownOption Text=""Banane"" Value=""f-ban"" />
        <BitDropdownOption Text=""Orange"" Value=""f-ora"" />
        <BitDropdownOption Text=""Traube"" Value=""f-gra"" />
        <BitDropdownOption ItemType=""BitDropdownItemType.Divider"" />
        <BitDropdownOption Text=""Gemüse"" ItemType=""BitDropdownItemType.Header"" />
        <BitDropdownOption Text=""Brokkoli"" Value=""v-bro"" />
        <BitDropdownOption Text=""Karotte"" Value=""v-car"" />
    </Options>
</BitDropdown>
<div>Values: @string.Join(',', localizationValues)</div>

<BitDropdown Label=""Leere Liste""
             Placeholder=""Zutat auswählen""
             EmptyText=""Es gibt hier nichts""
             TItem=""BitDropdownOption<string>"" TValue=""string"" />

<BitDropdown IsLoading
             Label=""Wird geladen""
             LoadingText=""Wird geladen...""
             Placeholder=""Zutat auswählen""
             TItem=""BitDropdownOption<string>"" TValue=""string"" />";
    private readonly string example25CsharpCode = @"
private IEnumerable<string?> localizationValues = [""f-app"", ""f-ban"", ""v-bro""];";

    private readonly string example26RazorCode = @"
<BitDropdown Label=""CaretDownIconName""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             CaretDownIconName=""@BitIconName.ChevronDownMed"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""ClearButtonIconName""
             ShowClearButton
             DefaultValue=""@(""f-app"")""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             ClearButtonIconName=""@BitIconName.ChromeClose"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""ItemCheckIconName""
             MultiSelect
             ShowSelectAll
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"" })""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             ItemCheckIconName=""@BitIconName.CheckMark"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""SearchBox icons""
             ShowSearchBox
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search item""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             SearchBoxIconName=""@BitIconName.Filter""
             SearchBoxClearIconName=""@BitIconName.EraseTool"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""ChipsRemoveIconName""
             Chips
             MultiSelect
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"" })""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             ChipsRemoveIconName=""@BitIconName.ChromeClose"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Responsive panel icons""
             Combo
             Responsive
             DefaultValue=""@string.Empty""
             Placeholder=""Resize below the small breakpoint""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             ResponsiveCloseIconName=""@BitIconName.ChromeClose""
             ComboBoxAddButtonIconName=""@BitIconName.CircleAddition"">
    @foreach (var item in comboBoxItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>";
    private readonly string example26CsharpCode = @"
private readonly List<BitDropdownItem<string>> basicItems =
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

private readonly List<BitDropdownItem<string>> comboBoxItems =
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
];";

    private readonly string example27RazorCode = @"
<BitDropdown @bind-Value=""closeOnSelectValue""
             CloseOnSelect=""false""
             Placeholder=""Select an item""
             Label=""Single select that stays open""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<br />
<div>Value: @closeOnSelectValue</div>

<BitDropdown @bind-Values=""closeOnSelectValues""
             MultiSelect
             CloseOnSelect=""true""
             Placeholder=""Select items""
             Label=""Multi select that closes on each pick""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<br />
<div>Values: @string.Join("", "", closeOnSelectValues)</div>";
    private readonly string example27CsharpCode = @"
private string? closeOnSelectValue;
private IEnumerable<string?> closeOnSelectValues = [];

private readonly List<BitDropdownItem<string>> basicItems =
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
];";

    private readonly string example28RazorCode = @"
<BitDropdown @bind-Values=""tokenSeparatorValues""
             Responsive
             MultiSelect
             Combo Chips Dynamic
             TokenSeparators=""tokenSeparators""
             Label=""Multi select combo box with token separators""
             Placeholder=""Type or paste options separated by , or ;""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             DynamicValueGenerator=""(BitDropdownOption<string> item) => item.Text""
             OnDynamicAdd=""(BitDropdownOption<string> item) => HandleOnDynamicAdd(item)"">
    @foreach (var item in comboBoxItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Values: @string.Join(',', tokenSeparatorValues)</div>";
    private readonly string example28CsharpCode = @"
private char[] tokenSeparators = [',', ';'];
private IEnumerable<string?> tokenSeparatorValues = [];

private void HandleOnDynamicAdd(BitDropdownOption<string> item)
{
    comboBoxItems.Add(new() { Text = item.Text, Value = item.Value });
}

private readonly List<BitDropdownItem<string>> comboBoxItems =
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
];";

    private readonly string example29RazorCode = @"
<BitDropdown @bind-Value=""openOnFocusValue""
             OpenOnFocus
             Placeholder=""Select an item""
             Label=""Open on focus""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Value: @openOnFocusValue</div>";
    private readonly string example29CsharpCode = @"
private string? openOnFocusValue;

private readonly List<BitDropdownItem<string>> basicItems =
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
];";

    private readonly string example30RazorCode = @"
<BitDropdown Label=""Primary""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.Primary""
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Secondary""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.Secondary""
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Tertiary""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.Tertiary""
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Info""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.Info""
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Success""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.Success""
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Warning""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.Warning""
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""SevereWarning""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.SevereWarning""
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Error""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.Error""
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>";
    private readonly string example30CsharpCode = @"
private readonly List<BitDropdownItem<string>> basicItems =
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
];";

    private readonly string example31RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitDropdown Label=""Caret down icon (external)""
             CaretDownIcon=""@BitIconInfo.Css(""fa-solid fa-circle-chevron-down"")""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Clear button icon (external)""
             ShowClearButton
             ClearButtonIcon=""@BitIconInfo.Css(""fa-solid fa-circle-xmark"")""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Chips remove icon (external)""
             Chips
             MultiSelect
             ChipsRemoveIcon=""@BitIconInfo.Css(""bi bi-x-circle"")""
             DefaultValues=""@(Array.Empty<string>())""
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Search box icons (external)""
             ShowSearchBox
             SearchBoxIcon=""@BitIconInfo.Css(""fa-solid fa-magnifying-glass"")""
             SearchBoxClearIcon=""@BitIconInfo.Css(""fa-solid fa-circle-xmark"")""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Item check icon (external)""
             MultiSelect
             ItemCheckIcon=""@BitIconInfo.Css(""fa-solid fa-heart"")""
             DefaultValues=""@(Array.Empty<string>())""
             Placeholder=""Select items""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Item icons (IconName - Fluent UI)""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <BitDropdownOption ItemType=""BitDropdownItemType.Header"" Text=""Fruits"" Value=""@string.Empty"" />
    <BitDropdownOption Text=""Apple"" Value=""@(""f-app"")"" IconName=""@BitIconName.AllApps"" />
    <BitDropdownOption Text=""Banana"" Value=""@(""f-ban"")"" IconName=""@BitIconName.Calculator"" />
    <BitDropdownOption Text=""Orange"" Value=""@(""f-ora"")"" IconName=""@BitIconName.FavoriteStar"" IsEnabled=""false"" />
    <BitDropdownOption Text=""Grape"" Value=""@(""f-gra"")"" IconName=""@BitIconName.Edit"" />
    <BitDropdownOption ItemType=""BitDropdownItemType.Divider"" Value=""@string.Empty"" />
    <BitDropdownOption ItemType=""BitDropdownItemType.Header"" Text=""Vegetables"" Value=""@string.Empty"" />
    <BitDropdownOption Text=""Broccoli"" Value=""@(""v-bro"")"" IconName=""@BitIconName.Health"" />
    <BitDropdownOption Text=""Carrot"" Value=""@(""v-car"")"" IconName=""@BitIconName.Add"" />
    <BitDropdownOption Text=""Lettuce"" Value=""@(""v-let"")"" IconName=""@BitIconName.ChevronDown"" />
</BitDropdown>

<BitDropdown Label=""Item icons (Icon - FontAwesome)""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <BitDropdownOption ItemType=""BitDropdownItemType.Header"" Text=""Fruits"" Value=""@string.Empty"" />
    <BitDropdownOption Text=""Apple"" Value=""@(""f-app"")"" Icon=""@BitIconInfo.Css(""fa-solid fa-apple-whole"")"" />
    <BitDropdownOption Text=""Banana"" Value=""@(""f-ban"")"" Icon=""@BitIconInfo.Css(""fa-solid fa-moon"")"" />
    <BitDropdownOption Text=""Orange"" Value=""@(""f-ora"")"" Icon=""@BitIconInfo.Fa(""solid lemon"")"" IsEnabled=""false"" />
    <BitDropdownOption Text=""Grape"" Value=""@(""f-gra"")"" Icon=""@BitIconInfo.Css(""fa-solid fa-droplet"")"" />
    <BitDropdownOption ItemType=""BitDropdownItemType.Divider"" Value=""@string.Empty"" />
    <BitDropdownOption ItemType=""BitDropdownItemType.Header"" Text=""Vegetables"" Value=""@string.Empty"" />
    <BitDropdownOption Text=""Broccoli"" Value=""@(""v-bro"")"" Icon=""@BitIconInfo.Css(""fa-solid fa-seedling"")"" />
    <BitDropdownOption Text=""Carrot"" Value=""@(""v-car"")"" Icon=""@BitIconInfo.Css(""fa-solid fa-carrot"")"" />
    <BitDropdownOption Text=""Lettuce"" Value=""@(""v-let"")"" Icon=""@BitIconInfo.Css(""fa-solid fa-leaf"")"" />
</BitDropdown>

<BitDropdown Label=""Item icons (Icon - Bootstrap Icons)""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <BitDropdownOption ItemType=""BitDropdownItemType.Header"" Text=""Fruits"" Value=""@string.Empty"" />
    <BitDropdownOption Text=""Apple"" Value=""@(""f-app"")"" Icon=""@BitIconInfo.Bi(""apple"")"" />
    <BitDropdownOption Text=""Banana"" Value=""@(""f-ban"")"" Icon=""@BitIconInfo.Bi(""flower1"")"" />
    <BitDropdownOption Text=""Orange"" Value=""@(""f-ora"")"" Icon=""@BitIconInfo.Css(""bi bi-sun"")"" IsEnabled=""false"" />
    <BitDropdownOption Text=""Grape"" Value=""@(""f-gra"")"" Icon=""@BitIconInfo.Bi(""droplet-fill"")"" />
    <BitDropdownOption ItemType=""BitDropdownItemType.Divider"" Value=""@string.Empty"" />
    <BitDropdownOption ItemType=""BitDropdownItemType.Header"" Text=""Vegetables"" Value=""@string.Empty"" />
    <BitDropdownOption Text=""Broccoli"" Value=""@(""v-bro"")"" Icon=""@BitIconInfo.Bi(""tree-fill"")"" />
    <BitDropdownOption Text=""Carrot"" Value=""@(""v-car"")"" Icon=""@BitIconInfo.Bi(""egg"")"" />
    <BitDropdownOption Text=""Lettuce"" Value=""@(""v-let"")"" Icon=""@BitIconInfo.Bi(""flower2"")"" />
</BitDropdown>";
    private readonly string example31CsharpCode = @"
private readonly List<BitDropdownItem<string>> basicItems =
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
];";

    private readonly string example32RazorCode = @"
<BitDropdown Label=""Small""
             ShowSearchBox
             Size=""BitSize.Small""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Medium""
             ShowSearchBox
             Size=""BitSize.Medium""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Large""
             ShowSearchBox
             Size=""BitSize.Large""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>";
    private readonly string example32CsharpCode = @"
private readonly List<BitDropdownItem<string>> basicItems =
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
];";

    private readonly string example33RazorCode = @"
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


<BitDropdown Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             Style=""margin: 1rem; box-shadow: aqua 0 0 0.5rem; text-shadow: aqua 0 0 0.5rem;"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             Class=""custom-class"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>


<BitDropdown Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in styleClassItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" Style=""@item.Style"" Class=""@item.Class"" />
    }
</BitDropdown>


<BitDropdown Label=""Styles""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             Styles=""@(new() { Label = ""text-shadow: dodgerblue 0 0 0.5rem;"",
                               Container = ""box-shadow: dodgerblue 0 0 0.5rem; border-color: lightskyblue; color: lightskyblue;"",
                               ItemHeader = ""color: dodgerblue; text-shadow: dodgerblue 0 0 0.5rem;"",
                               ItemButton = ""color: lightskyblue"",
                               Callout = ""border-radius: 0.25rem; box-shadow: lightskyblue 0 0 0.5rem;"" })"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""Classes""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             Classes=""@(new() { Callout = ""custom-callout"",
                                Container = ""custom-container"",
                                ItemButton = ""custom-item-button"",
                                ScrollContainer = ""custom-scroll-container"" })"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>";
    private readonly string example33CsharpCode = @"
private readonly List<BitDropdownItem<string>> basicItems =
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

private readonly List<BitDropdownItem<string>> styleClassItems =
[
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
];";

    private readonly string example34RazorCode = @"
<BitDropdown Label=""تک انتخابی""
             Dir=""BitDir.Rtl""
             Placeholder=""لطفا انتخاب کنید""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in rtlItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""چند انتخابی""
             MultiSelect
             Dir=""BitDir.Rtl""
             Placeholder=""انتخاب چند گزینه ای""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in rtlItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>

<BitDropdown Label=""تک انتخابی ریسپانسیو""
             Responsive
             Dir=""BitDir.Rtl""
             Placeholder=""لطفا انتخاب کنید""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    @foreach (var item in rtlItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>";
    private readonly string example34CsharpCode = @"
private readonly List<BitDropdownItem<string>> rtlItems =
[
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
];";
}
