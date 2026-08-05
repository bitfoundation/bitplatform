namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class _BitDropdownCustomDemo
{
    private readonly string example1RazorCode = @"
<BitDropdown Label=""Single select""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Multi select""
             MultiSelect
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors"" />

<BitDropdown Label=""Required"" Required
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />

<BitDropdown Label=""PreserveCalloutWidth""
             PreserveCalloutWidth
             Items=""GetBasicCustoms()""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors"" />

<BitDropdown Label=""Disabled""
             IsEnabled=""false""
             Items=""GetBasicCustoms()""
             DefaultValue=""@(""f-ora"")""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors"" />

<BitDropdown Label=""ReadOnly""
             ReadOnly
             Items=""GetBasicCustoms()""
             DefaultValue=""@(""f-app"")""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors"" />

<BitDropdown Label=""Custom delimiter""
             MultiSelect
             MultiSelectDelimiter="" - ""
             Items=""GetBasicCustoms()""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"" })""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors"" />

<BitDropdown Label=""Title""
             Title=""Pick your favorite fruit or vegetable""
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             NameSelectors=""nameSelectors""
             Placeholder=""Hover me"" />";
    private readonly string example1CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new() 
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example2RazorCode = @"
<BitDropdown Label=""Grouped items""
             Items=""GetGroupedCustoms()""
             NameSelectors=""nameSelectors""
             DefaultValue=""@("""")""
             Placeholder=""Select an item"" />

<BitDropdown Label=""StickyHeaders""
             StickyHeaders
             MultiSelect
             Items=""GetGroupedCustoms()""
             NameSelectors=""nameSelectors""
             DefaultValues=""@(Array.Empty<string>())""
             Placeholder=""Select items"" />";
    private readonly string example2CsharpCode = @"
public class Product
{
    public string? Text { get; set; }
    public string? Value { get; set; }
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
}

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    Text = { Selector = p => p.Text },
    Value = { Selector = p => p.Value },
    ItemType = { Selector = p => p.Type },
};

private List<Product> GetGroupedCustoms() =>
[
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"" },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Text = ""Mango"", Value = ""f-man"" },
    new() { Text = ""Peach"", Value = ""f-pea"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" },
    new() { Text = ""Potato"", Value = ""v-pot"" },
    new() { Text = ""Tomato"", Value = ""v-tom"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Grains"", Type = BitDropdownItemType.Header },
    new() { Text = ""Barley"", Value = ""g-bar"" },
    new() { Text = ""Oat"", Value = ""g-oat"" },
    new() { Text = ""Rice"", Value = ""g-ric"" },
    new() { Text = ""Wheat"", Value = ""g-whe"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Nuts"", Type = BitDropdownItemType.Header },
    new() { Text = ""Almond"", Value = ""n-alm"" },
    new() { Text = ""Cashew"", Value = ""n-cas"" },
    new() { Text = ""Walnut"", Value = ""n-wal"" }
];";

    private readonly string example3RazorCode = @"
<BitDropdown Label=""Prefix""
             Prefix=""Fruits:""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Suffix""
             Suffix=""kg""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Prefix and Suffix""
             Prefix=""Fruits:""
             Suffix=""kg""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Templates""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"">
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
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             IsEnabled=""false"" />";
    private readonly string example3CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new() 
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example4RazorCode = @"
<BitDropdown Label=""Single select""
             FitWidth
             Items=""GetBasicCustoms()""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors"" />

<BitDropdown Label=""Multi select""
             FitWidth
             MultiSelect
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors"" />";
    private readonly string example4CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new() 
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example5RazorCode = @"
<BitDropdown NoBorder
             Items=""GetBasicCustoms()""
             Placeholder=""Select an item"" 
             NameSelectors=""nameSelectors"" />

<BitDropdown NoBorder
             MultiSelect
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors"" />

<div style=""padding:0.5rem;border-radius:0.5rem;background:linear-gradient(90deg,#ff00cc7f,#3333997f)"">
    <BitDropdown Transparent
                 Items=""GetBasicCustoms()""
                 Placeholder=""Select an item""
                 NameSelectors=""nameSelectors"" />
</div>";
    private readonly string example5CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new() 
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example6RazorCode = @"
<BitDropdown Label=""Responsive Dropdown""
             Responsive
             Items=""GetBasicCustoms()""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors"" />";
    private readonly string example6CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new() 
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example7RazorCode = @"
<BitDropdown Label=""All""
             Items=""dropDirectionCustoms""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             DropDirection=""BitDropDirection.All"" />

<BitDropdown Label=""TopAndBottom""
             Items=""dropDirectionCustoms""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             DropDirection=""BitDropDirection.TopAndBottom"" />";
    private readonly string example7CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private ICollection<Product>? dropDirectionCustoms;

protected override void OnInitialized()
{
    dropDirectionCustoms = Enumerable.Range(1, 15)
                                     .Select(p => new Product { Text = $""Produce {p}"", Value = p.ToString() })
                                     .ToArray();
}

private BitDropdownNameSelectors<Product, string> nameSelectors = new() 
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example8RazorCode = @"
<BitDropdown @bind-Value=""clearValue""
             ShowClearButton
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Label=""Single select dropdown""
             Placeholder=""Select an option"" />
<div>Value: @clearValue</div>

<BitDropdown @bind-Values=""clearValues""
             MultiSelect
             ShowClearButton
             Items=""GetBasicCustoms()""
             Placeholder=""Select options""
             Label=""Multi select dropdown""
             NameSelectors=""nameSelectors"" />
<div>Values: @string.Join(',', clearValues)</div>

<BitDropdown ShowClearButton
             Items=""GetBasicCustoms()""
             DefaultValue=""@(""f-app"")""
             NameSelectors=""nameSelectors""
             Label=""Single select dropdown""
             Placeholder=""Select an option""
             OnClear=""() => clearCounter++"" />
<div>OnClear count: @clearCounter</div>";
    private readonly string example8CsharpCode = @"
private int clearCounter;
private string clearValue = ""f-app"";
private IEnumerable<string> clearValues = [""f-app"", ""f-ban""];

public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new() 
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example9RazorCode = @"
<BitDropdown Label=""Single select & auto focus""
             Responsive
             ShowSearchBox
             AutoFocusSearchBox
             Items=""GetBasicCustoms()""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             SearchBoxPlaceholder=""Search item"" />

<BitDropdown Label=""Multi select""
             Responsive
             MultiSelect
             ShowSearchBox
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors""
             SearchBoxPlaceholder=""Search items"" />


<BitDropdown Label=""Single select & auto focus""
             Responsive
             ShowSearchBox
             AutoFocusSearchBox
             Items=""GetBasicCustoms()""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             SearchBoxPlaceholder=""Search item""
             SearchFunction=""(items, text) => items.Where(i => i.Text?.StartsWith(text, StringComparison.OrdinalIgnoreCase) ?? false).ToArray()"" />

<BitDropdown Label=""Multi select""
             Responsive
             MultiSelect
             ShowSearchBox
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors""
             SearchBoxPlaceholder=""Search items""
             SearchFunction=""(items, text) => items.Where(i => i.Text?.EndsWith(text, StringComparison.OrdinalIgnoreCase) ?? false).ToArray()"" />";
    private readonly string example9CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new() 
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example10RazorCode = @"
<BitDropdown Label=""SearchMode: StartsWith""
             ShowSearchBox
             AutoFocusSearchBox
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Type a first letter""
             SearchMode=""BitDropdownSearchMode.StartsWith"" />

<BitDropdown Label=""MinSearchLength: 3""
             ShowSearchBox
             AutoFocusSearchBox
             MinSearchLength=""3""
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Type at least 3 characters"" />

<BitDropdown Label=""SearchIgnoreDiacritics""
             ShowSearchBox
             AutoFocusSearchBox
             HighlightSearch
             SearchIgnoreDiacritics
             Items=""GetAccentedCustoms()""
             DefaultValue=""@string.Empty""
             NameSelectors=""nameSelectors""
             Placeholder=""Select a name""
             SearchBoxPlaceholder=""Try jose, muller or angstrom"" />

<BitDropdown Label=""HighlightSearch""
             ShowSearchBox
             AutoFocusSearchBox
             HighlightSearch
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search item"" />

<BitDropdown Label=""Highlight in the ComboBox""
             Combo
             HighlightSearch
             Items=""comboBoxCustoms""
             DefaultValue=""@string.Empty""
             NameSelectors=""comboBoxNameSelectors""
             Placeholder=""Type to filter"" />";
    private readonly string example10CsharpCode = @"
private List<Product> GetAccentedCustoms() => new()
{
    new() { Text = ""José"", Value = ""n-jos"" },
    new() { Text = ""Renée"", Value = ""n-ren"" },
    new() { Text = ""Müller"", Value = ""n-mul"" },
    new() { Text = ""Ångström"", Value = ""n-ang"" },
    new() { Text = ""Zoë"", Value = ""n-zoe"" },
    new() { Text = ""Smith"", Value = ""n-smi"" }
};

public class Product
{
    public string? Label { get; set; }
    public string? CssClass { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? CssStyle { get; set; }
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    Text = { Selector = c => c.Text },
    Value = { Selector = c => c.Value },
    ItemType = { Selector = c => c.Type },
    IsEnabled = { Selector = c => c.Disabled is false },
};

private List<Product> comboBoxCustoms = new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> comboBoxNameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Class = { Selector = c => c.CssClass },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Style = { Selector = c => c.CssStyle },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
    ValueSetter = (Product item, string value) => item.Value = value,
    TextSetter = (string text, Product item) => item.Text = text
};";

    private readonly string example11RazorCode = @"
<BitDropdown Label=""Immediate""
             ShowSearchBox
             Immediate
             Items=""GetBasicCustoms()""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             SearchBoxPlaceholder=""Search item""
             OnSearch=""v => immediateSearchValue = v"" />
<div>Search value: <b>@immediateSearchValue</b></div>

<BitDropdown Label=""Immediate + DebounceTime (500ms)""
             ShowSearchBox
             Immediate
             DebounceTime=""500""
             Items=""GetBasicCustoms()""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             SearchBoxPlaceholder=""Search item""
             OnSearch=""v => debouncedSearchValue = v"" />
<div>Search value: <b>@debouncedSearchValue</b></div>

<BitDropdown Label=""Immediate ComboBox + ThrottleTime (500ms)""
             Combo
             Immediate
             ThrottleTime=""500""
             Items=""comboBoxCustoms""
             Placeholder=""Select an item""
             NameSelectors=""comboBoxNameSelectors"" />";
    private readonly string example11CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? CssClass { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? CssStyle { get; set; }
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private string? immediateSearchValue;
private string? debouncedSearchValue;

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private List<Product> comboBoxCustoms = new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};

private BitDropdownNameSelectors<Product, string> comboBoxNameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Class = { Selector = c => c.CssClass },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Style = { Selector = c => c.CssStyle },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
    ValueSetter = (Product item, string value) => item.Value = value,
    TextSetter = (string text, Product item) => item.Text = text
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
                 Items=""GetBasicCustoms()""
                 NameSelectors=""nameSelectors""
                 Placeholder=""Select an item"" />
    <ValidationMessage For=""@(() => validationModel.Category)"" />

    <BitDropdown @bind-Values=""validationModel.Products""
                 MultiSelect
                 Items=""GetBasicCustoms()""
                 Placeholder=""Select items""
                 NameSelectors=""nameSelectors""
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

public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new() 
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
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
             Items=""GetDataCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"">
    <HeaderTemplate Context=""item"">
        <div class=""custom-drp custom-drp-header"">
            <BitIcon IconName=""@((item.Payload as DropdownItemData)?.IconName)"" />
            <div>@item.Text</div>
        </div>
    </HeaderTemplate>
</BitDropdown>

<BitDropdown Label=""Text & Item templates""
             Items=""GetDataCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"">
    <TextTemplate Context=""dropdown"">
        <div class=""custom-drp custom-drp-txt"">
            <BitIcon IconName=""@((dropdown.SelectedItem?.Payload as DropdownItemData)?.IconName)"" />
            <div>@dropdown.SelectedItem?.Text</div>
        </div>
    </TextTemplate>
    <ItemTemplate Context=""item"">
        <div class=""custom-drp custom-drp-item"">
            <BitIcon IconName=""@((item.Payload as DropdownItemData)?.IconName)"" />
            <div Style=""text-decoration:underline"">@item.Text</div>
        </div>
    </ItemTemplate>
</BitDropdown>

<BitDropdown Label=""Placeholder template""
             Items=""GetDataCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"">
    <PlaceholderTemplate Context=""dropdown"">
        <div class=""custom-drp custom-drp-ph"">
            <BitIcon IconName=""@BitIconName.MessageFill"" />
            <div>@dropdown.Placeholder</div>
        </div>
    </PlaceholderTemplate>
</BitDropdown>

<BitDropdown Label=""Label template""
             Items=""GetDataCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"">
    <LabelTemplate>
        <div class=""custom-drp custom-drp-lbl"">
            <div>Custom label</div>
            <BitIcon IconName=""@BitIconName.Info"" AriaLabel=""Info"" />
        </div>
    </LabelTemplate>
</BitDropdown>

<BitDropdown Label=""CaretDownIconName""
             Items=""GetDataCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             CaretDownIconName=""@BitIconName.ScrollUpDown"" />

<BitDropdown Label=""CaretDownTemplate""
             Items=""GetDataCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"">
    <CaretDownTemplate>
        <BitIcon IconName=""@BitIconName.FavoriteStar"" Style=""font-size:0.875rem"" />
    </CaretDownTemplate>
</BitDropdown>

<BitDropdown Label=""Callout templates""
             Items=""GetDataCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"">
    <CalloutHeaderTemplate>
        <div Style=""padding:0.5rem;border-bottom:1px solid #555"">Best in the world</div>
    </CalloutHeaderTemplate>
    <CalloutFooterTemplate>
        <BitActionButton IconName=""@BitIconName.Add"">New Item</BitActionButton>
    </CalloutFooterTemplate>
</BitDropdown>";
    private readonly string example13CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

public class DropdownItemData
{
    public string? IconName { get; set; }
}

private List<Product> GetDataCustoms() => new()
{
    new() { Type = BitDropdownItemType.Header, Text = ""Items"", Payload = new DropdownItemData { IconName = ""BulletedList2"" } },
        new() { Text = ""Item a"", Value = ""A"", Payload = new DropdownItemData { IconName = ""Memo"" } },
        new() { Text = ""Item b"", Value = ""B"", Payload = new DropdownItemData { IconName = ""Print"" } },
        new() { Text = ""Item c"", Value = ""C"", Payload = new DropdownItemData { IconName = ""ShoppingCart"" } },
        new() { Type = BitDropdownItemType.Divider },
        new() { Type = BitDropdownItemType.Header, Text = ""More Items"", Payload = new DropdownItemData { IconName = ""BulletedTreeList"" } },
        new() { Text = ""Item d"", Value = ""D"", Payload = new DropdownItemData { IconName = ""Train"" } },
        new() { Text = ""Item e"", Value = ""E"", Payload = new DropdownItemData { IconName = ""Repair"" } },
        new() { Text = ""Item f"", Value = ""F"", Payload = new DropdownItemData { IconName = ""Running"" } }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new() 
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example14RazorCode = @"
<BitDropdown @bind-Value=""controlledValue""
             Label=""Single select""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />
<div>Selected Value: @controlledValue</div>

<BitDropdown @bind-Values=""controlledValues""
             MultiSelect
             Label=""Multi select""
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors"" />
<div>Selected Values: @string.Join("","", controlledValues)</div>



<BitDropdown Label=""Single select""
             Items=""GetBasicCustoms()""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             TItem=""Product"" TValue=""string""
             OnChange=""(string? value) => changedValue = value"" />
<div>Changed Value: @changedValue</div>

<BitDropdown Label=""Multi select""
             MultiSelect
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors""
             TItem=""Product"" TValue=""string""
             OnValuesChange=""(IEnumerable<string> values) => changedValues = values"" />
<div>Changed Values: @string.Join("","", changedValues)</div>



<BitDropdown Label=""Single select""
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             OnSelectItem=""(Product item) => selectedItem1 = item"" />
<div>Selected Value: @selectedItem1?.Value</div>

<BitDropdown Label=""Multi select""
             MultiSelect
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             DefaultValues=""@(Array.Empty<string>())""
             NameSelectors=""nameSelectors""
             OnSelectItem=""(Product item) => selectedItem2 = item"" />
<div>Selected Value: @selectedItem2?.Value</div>

<BitDropdown Label=""Multi select""
             MultiSelect
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors""
             DefaultValues=""@(new[] { ""f-app"" })""
             OnSelectItem=""(Product item) => pickedItem = item""
             OnDeselectItem=""(Product item) => deselectedItem = item"" />
<div>Last picked item: @pickedItem?.Text</div>
<div>Last unselected item: @deselectedItem?.Text</div>

<BitDropdown Label=""Single select""
             Reselectable
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             OnSelectItem=""(Product item) => selectItemCounter++"" />
<div>OnSelectItem count: @selectItemCounter</div>

<BitButton OnClick=""() => isDropdownOpen = true"">Open the dropdown</BitButton>
<BitDropdown @bind-IsOpen=""isDropdownOpen""
             Label=""Single select""
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors"" />

<BitDropdown Label=""Single select""
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             OnOpen=""HandleOnCalloutOpen""
             OnClose=""HandleOnCalloutClose"" />
<div>The callout is @calloutState.</div>

<BitDropdown Label=""Single select""
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             Placeholder=""Tab into me""
             NameSelectors=""nameSelectors""
             OnFocusIn=""HandleOnFocusIn""
             OnFocusOut=""HandleOnFocusOut"" />
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
private IEnumerable<string> controlledValues = [""f-app"", ""f-ban""];

private string? changedValue;
private IEnumerable<string> changedValues = [];

private Product? selectedItem1;
private Product? selectedItem2;
private Product? pickedItem;
private Product? deselectedItem;

public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new() 
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example15RazorCode = @"
<BitDropdown @bind-Value=""autoSelectValue""
             Combo
             Immediate
             AutoSelectFirstMatch
             Items=""comboBoxCustoms""
             Label=""AutoSelectFirstMatch""
             NameSelectors=""comboBoxNameSelectors""
             Placeholder=""Type a few letters and press Enter"" />
<div>Value: @autoSelectValue</div>

<BitDropdown @bind-Value=""comboBoxValueSample1""
             Combo
             Responsive
             Items=""comboBoxCustoms""
             Placeholder=""Select an option""
             Label=""Single select combo box""
             NameSelectors=""comboBoxNameSelectors"" />
<div>Value: @comboBoxValueSample1</div>

<BitDropdown @bind-Values=""comboBoxValues1""
             Combo
             Responsive
             MultiSelect
             Items=""comboBoxCustoms""
             Label=""Multi select combo box""
             Placeholder=""Select an option""
             NameSelectors=""comboBoxNameSelectors"" />
<div>Values: @string.Join(',', comboBoxValues1)</div>";
    private readonly string example15CsharpCode = @"
private string? autoSelectValue;
private string comboBoxValueSample1 = default!;
private IEnumerable<string> comboBoxValues1 = [];

public class Product
{
    public string? Label { get; set; }
    public string? CssClass { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? CssStyle { get; set; }
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> comboBoxCustoms = new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> comboBoxNameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Class = { Selector = c => c.CssClass },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Style = { Selector = c => c.CssStyle },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
    ValueSetter = (Product item, string value) => item.Value = value,
    TextSetter = (string text, Product item) => item.Text = text
};";

    private readonly string example16RazorCode = @"
<BitDropdown @bind-Value=""comboBoxValueSample2""
             Responsive
             Combo Chips
             Items=""comboBoxCustoms""
             Placeholder=""Select an option""
             NameSelectors=""comboBoxNameSelectors""
             Label=""Single select combo box & chips"" />
<div>Value: @comboBoxValueSample2</div>

<BitDropdown @bind-Values=""comboBoxValues2""
             Responsive
             Combo Chips 
             MultiSelect
             Items=""comboBoxCustoms""
             Placeholder=""Select an option""
             NameSelectors=""comboBoxNameSelectors""
             Label=""Multi select combo box & chips"" />
<div>Values: @string.Join(',', comboBoxValues2)</div>";
    private readonly string example16CsharpCode = @"
private string comboBoxValueSample2 = default!;
private IEnumerable<string> comboBoxValues2 = [];

public class Product
{
    public string? Label { get; set; }
    public string? CssClass { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? CssStyle { get; set; }
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> comboBoxCustoms = new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> comboBoxNameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Class = { Selector = c => c.CssClass },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Style = { Selector = c => c.CssStyle },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
    ValueSetter = (Product item, string value) => item.Value = value,
    TextSetter = (string text, Product item) => item.Text = text
};";

    private readonly string example17RazorCode = @"
<BitDropdown Label=""MaxDisplayedItems (chips)""
             Chips
             MultiSelect
             MaxDisplayedItems=""2""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"", ""f-gra"", ""v-car"" })"" />

<BitDropdown Label=""OverflowTextFormat""
             Chips
             MultiSelect
             MaxDisplayedItems=""2""
             OverflowTextFormat=""and {0} more""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"", ""f-gra"", ""v-car"" })"" />

<BitDropdown Label=""SelectedItemsTextFormat""
             MultiSelect
             MaxDisplayedItems=""2""
             SelectedItemsTextFormat=""{0} fruits and vegetables""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"", ""f-gra"", ""v-car"" })"" />

<BitDropdown Label=""AutoClearSearch""
             Combo Chips
             MultiSelect
             AutoClearSearch
             Items=""comboBoxCustoms""
             NameSelectors=""comboBoxNameSelectors""
             DefaultValues=""@(Array.Empty<string>())""
             Placeholder=""Type to filter, then pick"" />

<BitDropdown Label=""HideSelectedItems""
             Chips
             MultiSelect
             HideSelectedItems
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"" })"" />";
    private readonly string example17CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? CssClass { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? CssStyle { get; set; }
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};

private List<Product> comboBoxCustoms = new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> comboBoxNameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Class = { Selector = c => c.CssClass },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Style = { Selector = c => c.CssStyle },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
    ValueSetter = (Product item, string value) => item.Value = value,
    TextSetter = (string text, Product item) => item.Text = text
};";

    private readonly string example18RazorCode = @"
<BitDropdown @bind-Value=""comboBoxValueSample3""
             Combo Dynamic Responsive
             Items=""comboBoxCustoms""
             Placeholder=""Select an option""
             NameSelectors=""comboBoxNameSelectors""
             Label=""Single select combo box & dynamic""
             OnDynamicAdd=""(Product item) => HandleOnDynamicAdd(item)""
             DynamicValueGenerator=""@((Product item) => item.Text ?? """")"" />
<div>Value: @comboBoxValueSample3</div>

<BitDropdown @bind-Value=""comboBoxValueSample4""
             Responsive
             Combo Chips Dynamic
             Items=""comboBoxCustoms""
             Placeholder=""Select an option""
             NameSelectors=""comboBoxNameSelectors""
             Label=""Single select combo box, chips & dynamic""
             OnDynamicAdd=""(Product item) => HandleOnDynamicAdd(item)""
             DynamicValueGenerator=""@((Product item) => item.Text ?? """")"" />
<div>Value: @comboBoxValueSample4</div>

<BitDropdown @bind-Values=""comboBoxValues3""
             Responsive
             MultiSelect
             Combo Chips Dynamic
             Items=""comboBoxCustoms""
             Placeholder=""Select options""
             NameSelectors=""comboBoxNameSelectors""
             Label=""Multi select combo box, chips & dynamic""
             OnDynamicAdd=""(Product item) => HandleOnDynamicAdd(item)""
             DynamicValueGenerator=""@((Product item) => item.Text ?? """")"" />
<div>Values: @string.Join(',', comboBoxValues3)</div>";
    private readonly string example18CsharpCode = @"
private string comboBoxValueSample3 = default!;
private string comboBoxValueSample4 = default!;
private IEnumerable<string> comboBoxValues3 = [];

private void HandleOnDynamicAdd(Product item)
{
    comboBoxCustoms.Add(item);
}

public class Product
{
    public string? Label { get; set; }
    public string? CssClass { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? CssStyle { get; set; }
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> comboBoxCustoms = new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> comboBoxNameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Class = { Selector = c => c.CssClass },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Style = { Selector = c => c.CssStyle },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
    ValueSetter = (Product item, string value) => item.Value = value,
    TextSetter = (string text, Product item) => item.Text = text
};";

    private readonly string example19RazorCode = @"
<BitDropdown Label=""Single select""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Multi select""
             MultiSelect
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors"" />";
    private readonly string example19CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example20RazorCode = @"
<BitDropdown @bind-Values=""selectAllValues""
             MultiSelect
             ShowSelectAll
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items""
             Label=""Select all"" />
<div>Values: @string.Join(',', selectAllValues)</div>

<BitDropdown MultiSelect
             ShowSelectAll
             ShowSearchBox
             SelectAllText=""Select all of them""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items""
             DefaultValues=""@(Array.Empty<string>())""
             SearchBoxPlaceholder=""Search items""
             Label=""Custom text & search"" />";
    private readonly string example20CsharpCode = @"
private IEnumerable<string> selectAllValues = [];

public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example21RazorCode = @"
<BitDropdown @bind-Values=""maxSelectedValues""
             MultiSelect
             MaxSelectedItems=""2""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select up to 2 items""
             Label=""Max 2 items"" />
<div>Values: @string.Join(',', maxSelectedValues)</div>";
    private readonly string example21CsharpCode = @"
private IEnumerable<string> maxSelectedValues = [];

public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
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
             Items=""emptyCustoms""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             TItem=""Product"" TValue=""string"" />

<BitDropdown Label=""EmptyText""
             Items=""emptyCustoms""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             EmptyText=""There is nothing here!""
             TItem=""Product"" TValue=""string"" />

<BitDropdown Label=""EmptyTemplate""
             Items=""emptyCustoms""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             TItem=""Product"" TValue=""string"">
    <EmptyTemplate>
        <div class=""custom-drp custom-drp-empty"">
            <BitIcon IconName=""@BitIconName.SearchIssue"" />
            <div>Nothing to show!</div>
        </div>
    </EmptyTemplate>
</BitDropdown>

<BitDropdown Label=""Search without result""
             ShowSearchBox
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search for anything"" />

<BitDropdown Label=""NoResultsText""
             ShowSearchBox
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             SearchBoxPlaceholder=""Search for anything""
             NoResultsText=""Nothing matched your search"" />

<BitDropdown Label=""NoResultsTemplate""
             ShowSearchBox
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
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
private readonly List<Product> emptyCustoms = [];

public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
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
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />

<BitDropdown Label=""LoadingText""
             IsLoading
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             LoadingText=""Fetching the products..."" />

<BitDropdown Label=""LoadingTemplate""
             IsLoading
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"">
    <LoadingTemplate>
        <div class=""custom-drp custom-drp-empty"">
            <BitIcon IconName=""@BitIconName.Sync"" />
            <div>Just a moment...</div>
        </div>
    </LoadingTemplate>
</BitDropdown>

<BitButton OnClick=""LoadDelayedCustoms"" IsLoading=""isLoadingItems"">Load the items</BitButton>
<BitDropdown Label=""Products""
             IsLoading=""isLoadingItems""
             Items=""delayedCustoms""
             DefaultValue=""@string.Empty""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />";
    private readonly string example23CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private bool isLoadingItems;
private ICollection<Product> delayedCustoms = [];

private async Task LoadDelayedCustoms()
{
    isLoadingItems = true;
    delayedCustoms = [];

    await Task.Delay(2000);

    delayedCustoms = GetBasicCustoms();
    isLoadingItems = false;
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example24RazorCode = @"
<BitDropdown Label=""Single select""
             Virtualize
             Items=""virtualizeCustoms1""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors"" />

<BitDropdown Label=""Multi select""
             Virtualize
             MultiSelect
             ItemSize=""35""
             OverscanCount=""5""
             Items=""virtualizeCustoms2""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors"" />


<BitDropdown Label=""Single select""
             Virtualize
             ItemsProvider=""LoadItems""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors"" />

<BitDropdown Label=""Multi select & ItemsProviderDebounceTime""
             Virtualize
             MultiSelect
             ItemsProvider=""LoadItems""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors""
             ItemsProviderDebounceTime=""300"" />

<BitDropdown Label=""VirtualizePlaceholder""
             Virtualize
             ItemsProvider=""LoadItems""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             TItem=""Product"" TValue=""string"">
    <VirtualizePlaceholder>
        <div style=""padding:0 0.5rem;color:gray"">Loading @(context.Index)...</div>
    </VirtualizePlaceholder>
</BitDropdown>

<BitDropdown Label=""Single select""
             Virtualize
             ItemsProvider=""LoadItems""
             Placeholder=""Select an item""
             InitialSelectedItems=""initialSelectedItem""
             NameSelectors=""nameSelectors"" />

<BitDropdown Label=""Multi select""
             Virtualize
             MultiSelect
             ItemsProvider=""LoadItems""
             Placeholder=""Select items""
             InitialSelectedItems=""initialSelectedItems""
             NameSelectors=""nameSelectors"" />";
    private readonly string example24CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private ICollection<Product>? virtualizeCustoms1;
private ICollection<Product>? virtualizeCustoms2;

private IEnumerable<Product> initialSelectedItem = [
    new()
    {
        Text = ""Product 100"",
        Value = ""100"",
        Payload = new ProductDto {
            Id = 100,
            Price = 60,
            Name = ""Product 100""
        },
        Label = ""Product 100"",
        Type = BitDropdownItemType.Normal
    }
];

private IEnumerable<Product> initialSelectedItems = [
    new()
    {
        Text = ""Product 100"",
        Value = ""100"",
        Payload = new ProductDto {
            Id = 100,
            Price = 60,
            Name = ""Product 100""
        },
        Label = ""Product 100"",
        Type = BitDropdownItemType.Normal
    },
    new()
    {
        Text = ""Product 99"",
        Value = ""99"",
        Payload = new ProductDto {
            Id = 99,
            Price = 75,
            Name = ""Product 99""
        },
        Label = ""Product 99"",
        Type = BitDropdownItemType.Normal
    }
];

protected override void OnInitialized()
{
    virtualizeCustoms1 = Enumerable.Range(1, 10_000)
                                   .Select(p => new Product { Text = $""Produce {p}"", Value = p.ToString() })
                                   .ToArray();

    virtualizeCustoms2 = Enumerable.Range(1, 10_000)
                                   .Select(p => new Product { Text = $""Produce {p}"", Value = p.ToString() })
                                   .ToArray();
}

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};

private async ValueTask<BitDropdownItemsProviderResult<Product>> LoadItems(
    BitDropdownItemsProviderRequest<Product> request)
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

        var items = data!.Items.Select(i => new Product
        {
            Text = i.Name,
            Value = i.Id.ToString(),
            Payload = i,
            Label = i.Name,
            Disabled = false,
            Type = BitDropdownItemType.Normal
        }).ToList();

        return BitDropdownItemsProviderResult.From(items, data!.TotalCount);
    }
    catch
    {
        return BitDropdownItemsProviderResult.From(new List<Product>(), 0);
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
             Items=""GetLocalizedCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Zutaten auswählen""
             SearchBoxPlaceholder=""Suchen""
             SelectAllText=""Alle auswählen""
             NoResultsText=""Keine Treffer gefunden""
             OverflowTextFormat=""und {0} weitere""
             SearchResultsText=""{0} Treffer verfügbar""
             ClearButtonAriaLabel=""Auswahl löschen""
             SearchBoxAriaLabel=""Suchtext""
             SearchBoxClearButtonAriaLabel=""Text löschen""
             ChipsRemoveButtonAriaLabel=""{0} entfernen"" />
<div>Values: @string.Join(',', localizationValues)</div>

<BitDropdown Label=""Leere Liste""
             Items=""emptyCustoms""
             NameSelectors=""nameSelectors""
             Placeholder=""Zutat auswählen""
             EmptyText=""Es gibt hier nichts""
             TItem=""Product"" TValue=""string"" />

<BitDropdown IsLoading
             Label=""Wird geladen""
             Items=""emptyCustoms""
             NameSelectors=""nameSelectors""
             LoadingText=""Wird geladen...""
             Placeholder=""Zutat auswählen""
             TItem=""Product"" TValue=""string"" />";
    private readonly string example25CsharpCode = @"
public class Product
{
    public string? Key { get; set; }
    public string? Text { get; set; }
    public string? Value { get; set; }
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
}

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    Text = { Selector = p => p.Text },
    Value = { Selector = p => p.Value },
    ItemType = { Selector = p => p.Type },
};

private List<Product> GetLocalizedCustoms() =>
[
    new() { Text = ""Früchte"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apfel"", Value = ""f-app"" },
    new() { Text = ""Banane"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"" },
    new() { Text = ""Traube"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Gemüse"", Type = BitDropdownItemType.Header },
    new() { Text = ""Brokkoli"", Value = ""v-bro"" },
    new() { Text = ""Karotte"", Value = ""v-car"" }
];

private readonly List<Product> emptyCustoms = [];

private IEnumerable<string?> localizationValues = [""f-app"", ""f-ban"", ""v-bro""];";

    private readonly string example26RazorCode = @"
<BitDropdown Label=""CaretDownIconName""
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             CaretDownIconName=""@BitIconName.ChevronDownMed"" />

<BitDropdown Label=""ClearButtonIconName""
             ShowClearButton
             Items=""GetBasicCustoms()""
             DefaultValue=""@(""f-app"")""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             ClearButtonIconName=""@BitIconName.ChromeClose"" />

<BitDropdown Label=""ItemCheckIconName""
             MultiSelect
             ShowSelectAll
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors""
             DefaultValues=""@(new[] { ""f-app"" })""
             ItemCheckIconName=""@BitIconName.CheckMark"" />

<BitDropdown Label=""SearchBox icons""
             ShowSearchBox
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             SearchBoxPlaceholder=""Search item""
             SearchBoxIconName=""@BitIconName.Filter""
             SearchBoxClearIconName=""@BitIconName.EraseTool"" />

<BitDropdown Label=""ChipsRemoveIconName""
             Chips
             MultiSelect
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors""
             DefaultValues=""@(new[] { ""f-app"", ""f-ban"" })""
             ChipsRemoveIconName=""@BitIconName.ChromeClose"" />

<BitDropdown Label=""Responsive panel icons""
             Combo
             Responsive
             Items=""comboBoxCustoms""
             DefaultValue=""@string.Empty""
             NameSelectors=""nameSelectors""
             Placeholder=""Resize below the small breakpoint""
             ResponsiveCloseIconName=""@BitIconName.ChromeClose""
             ComboBoxAddButtonIconName=""@BitIconName.CircleAddition"" />";
    private readonly string example26CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> comboBoxCustoms = new()
{
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"" },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"" }
};

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example27RazorCode = @"
<BitDropdown @bind-Value=""closeOnSelectValue""
             CloseOnSelect=""false""
             Items=""GetBasicCustoms()""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             Label=""Single select that stays open"" />
<div>Value: @closeOnSelectValue</div>

<BitDropdown @bind-Values=""closeOnSelectValues""
             MultiSelect
             CloseOnSelect=""true""
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors""
             Label=""Multi select that closes on each pick"" />
<div>Values: @string.Join("", "", closeOnSelectValues)</div>";
    private readonly string example27CsharpCode = @"
private string? closeOnSelectValue;
private IEnumerable<string?> closeOnSelectValues = [];

public class Product
{
    public string? Label { get; set; }
    public string? CssClass { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? CssStyle { get; set; }
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    Text = { Selector = c => c.Text },
    Value = { Selector = c => c.Value },
    ItemType = { Selector = c => c.Type },
    IsEnabled = { Selector = c => c.Disabled is false }
};";

    private readonly string example28RazorCode = @"
<BitDropdown Label=""Primary""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.Primary""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items"" />

<BitDropdown Label=""Secondary""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.Secondary""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items"" />

<BitDropdown Label=""Tertiary""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.Tertiary""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items"" />

<BitDropdown Label=""Info""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.Info""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items"" />

<BitDropdown Label=""Success""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.Success""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items"" />

<BitDropdown Label=""Warning""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.Warning""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items"" />

<BitDropdown Label=""SevereWarning""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.SevereWarning""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items"" />

<BitDropdown Label=""Error""
             MultiSelect
             ShowSearchBox
             Color=""BitColor.Error""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items"" />";
    private readonly string example28CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example29RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitDropdown Label=""Caret down icon (external)""
             CaretDownIcon=""@BitIconInfo.Css(""fa-solid fa-circle-chevron-down"")""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />
        
<BitDropdown Label=""Clear button icon (external)""
             ShowClearButton
             ClearButtonIcon=""@BitIconInfo.Css(""fa-solid fa-circle-xmark"")""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />
        
<BitDropdown Label=""Chips remove icon (external)""
             Chips
             MultiSelect
             ChipsRemoveIcon=""@BitIconInfo.Css(""bi bi-x-circle"")""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             DefaultValues=""@(Array.Empty<string>())""
             Placeholder=""Select items"" />
        
<BitDropdown Label=""Search box icons (external)""
             ShowSearchBox
             SearchBoxIcon=""@BitIconInfo.Css(""fa-solid fa-magnifying-glass"")""
             SearchBoxClearIcon=""@BitIconInfo.Css(""fa-solid fa-circle-xmark"")""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Item check icon (external)""
             MultiSelect
             ItemCheckIcon=""@BitIconInfo.Css(""fa-solid fa-heart"")""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             DefaultValues=""@(Array.Empty<string>())""
             Placeholder=""Select items"" />

<BitDropdown Label=""Item icons (IconName - Fluent UI)""
             Items=""GetExternalIconCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Item icons (Icon - FontAwesome)""
             Items=""GetExternalIconFaCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Item icons (Icon - Bootstrap Icons)""
             Items=""GetExternalIconBiCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />";
    private readonly string example29CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? CssClass { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? CssStyle { get; set; }
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
    public BitIconInfo? Icon { get; set; }
    public string? IconName { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private List<Product> GetExternalIconCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"", IconName = nameof(BitIconName.AllApps) },
    new() { Text = ""Banana"", Value = ""f-ban"", IconName = nameof(BitIconName.Calculator) },
    new() { Text = ""Orange"", Value = ""f-ora"", IconName = nameof(BitIconName.FavoriteStar), Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"", IconName = nameof(BitIconName.Edit) },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"", IconName = nameof(BitIconName.Health) },
    new() { Text = ""Carrot"", Value = ""v-car"", IconName = nameof(BitIconName.Add) },
    new() { Text = ""Lettuce"", Value = ""v-let"", IconName = nameof(BitIconName.ChevronDown) }
};

private List<Product> GetExternalIconFaCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"", Icon = BitIconInfo.Css(""fa-solid fa-apple-whole"") },
    new() { Text = ""Banana"", Value = ""f-ban"", Icon = BitIconInfo.Css(""fa-solid fa-moon"") },
    new() { Text = ""Orange"", Value = ""f-ora"", Icon = BitIconInfo.Fa(""solid lemon""), Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"", Icon = BitIconInfo.Css(""fa-solid fa-droplet"") },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"", Icon = BitIconInfo.Css(""fa-solid fa-seedling"") },
    new() { Text = ""Carrot"", Value = ""v-car"", Icon = BitIconInfo.Css(""fa-solid fa-carrot"") },
    new() { Text = ""Lettuce"", Value = ""v-let"", Icon = BitIconInfo.Css(""fa-solid fa-leaf"") }
};

private List<Product> GetExternalIconBiCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"", Icon = BitIconInfo.Bi(""apple"") },
    new() { Text = ""Banana"", Value = ""f-ban"", Icon = BitIconInfo.Bi(""flower1"") },
    new() { Text = ""Orange"", Value = ""f-ora"", Icon = BitIconInfo.Css(""bi bi-sun""), Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"", Icon = BitIconInfo.Bi(""droplet-fill"") },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"", Icon = BitIconInfo.Bi(""tree-fill"") },
    new() { Text = ""Carrot"", Value = ""v-car"", Icon = BitIconInfo.Bi(""egg"") },
    new() { Text = ""Lettuce"", Value = ""v-let"", Icon = BitIconInfo.Bi(""flower2"") }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    AriaLabel = { Selector = c => c.Label },
    Class = { Selector = c => c.CssClass },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Style = { Selector = c => c.CssStyle },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
    Icon = { Selector = c => c.Icon },
    IconName = { Selector = c => c.IconName },
};";

    private readonly string example30RazorCode = @"
<BitDropdown Label=""Small""
             ShowSearchBox
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             NameSelectors=""nameSelectors""
             Size=""BitSize.Small""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Medium""
             ShowSearchBox
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             NameSelectors=""nameSelectors""
             Size=""BitSize.Medium""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Large""
             ShowSearchBox
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             NameSelectors=""nameSelectors""
             Size=""BitSize.Large""
             Placeholder=""Select an item"" />";
    private readonly string example30CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new()
{
    Text = { Selector = c => c.Text },
    Value = { Selector = c => c.Value },
    ItemType = { Selector = c => c.Type },
    IsEnabled = { Selector = c => c.Disabled is false },
};";

    private readonly string example31RazorCode = @"
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


<BitDropdown Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             Style=""margin: 1rem; box-shadow: aqua 0 0 0.5rem; text-shadow: aqua 0 0 0.5rem;"" />

<BitDropdown Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             Class=""custom-class"" />


<BitDropdown Items=""GetStyleClassCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />


<BitDropdown Label=""Styles""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             Styles=""@(new() { Label = ""text-shadow: dodgerblue 0 0 0.5rem;"",
                               Container = ""box-shadow: dodgerblue 0 0 0.5rem; border-color: lightskyblue; color: lightskyblue;"",
                               ItemHeader = ""color: dodgerblue; text-shadow: dodgerblue 0 0 0.5rem;"",
                               ItemButton = ""color: lightskyblue"",
                               Callout = ""border-radius: 0.25rem; box-shadow: lightskyblue 0 0 0.5rem;"" })"" />

<BitDropdown Label=""Classes""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             Classes=""@(new() { Callout = ""custom-callout"",
                                Container = ""custom-container"",
                                ItemButton = ""custom-item-button"",
                                ScrollContainer = ""custom-scroll-container"" })"" />";
    private readonly string example31CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? CssClass { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? CssStyle { get; set; }
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetBasicCustoms() => new()
{
    new() { Text = ""Fruits"", Type = BitDropdownItemType.Header },
    new() { Text = ""Apple"", Value = ""f-app"" },
    new() { Text = ""Banana"", Value = ""f-ban"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""Grape"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Text = ""Vegetables"", Type = BitDropdownItemType.Header },
    new() { Text = ""Broccoli"", Value = ""v-bro"" },
    new() { Text = ""Carrot"", Value = ""v-car"" },
    new() { Text = ""Lettuce"", Value = ""v-let"" }
};

private List<Product> GetStyleClassCustoms() => new()
{
    new() { Type = BitDropdownItemType.Header, Text = ""Fruits"", CssStyle = ""text-align: center;"" },
    new() { Text = ""Apple"", Value = ""f-app"", CssClass = ""custom-fruit"" },
    new() { Text = ""Banana"", Value = ""f-ban"", CssClass = ""custom-fruit"" },
    new() { Text = ""Orange"", Value = ""f-ora"", Disabled = true, CssClass = ""custom-fruit"" },
    new() { Text = ""Grape"", Value = ""f-gra"", CssClass = ""custom-fruit"" },
    new() { Type = BitDropdownItemType.Divider, CssStyle = ""padding: 0 0.25rem;"" },
    new() { Type = BitDropdownItemType.Header, Text = ""Vegetables"", CssStyle = ""text-align: center;"" },
    new() { Text = ""Broccoli"", Value = ""v-bro"", CssClass = ""custom-veg"" },
    new() { Text = ""Carrot"", Value = ""v-car"", CssClass = ""custom-veg"" },
    new() { Text = ""Lettuce"", Value = ""v-let"", CssClass = ""custom-veg"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new() 
{
    AriaLabel = { Selector = c => c.Label },
    Class = { Selector = c => c.CssClass },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Style = { Selector = c => c.CssStyle },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";

    private readonly string example32RazorCode = @"
<BitDropdown Label=""تک انتخابی""
             Dir=""BitDir.Rtl""
             Items=""GetRtlCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""لطفا انتخاب کنید"" />

<BitDropdown Label=""چند انتخابی""
             MultiSelect
             Dir=""BitDir.Rtl""
             Items=""GetRtlCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""انتخاب چند گزینه ای"" />

<BitDropdown Label=""تک انتخابی ریسپانسیو""
             Responsive
             Dir=""BitDir.Rtl""
             Items=""GetRtlCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""لطفا انتخاب کنید"" />";
    private readonly string example32CsharpCode = @"
public class Product
{
    public string? Label { get; set; }
    public string? Key { get; set; }
    public object? Payload { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;
    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Value { get; set; }
}

private List<Product> GetRtlCustoms() => new()
{
    new() { Type = BitDropdownItemType.Header, Text = ""میوه ها"" },
    new() { Text = ""سیب"", Value = ""f-app"" },
    new() { Text = ""موز"", Value = ""f-ban"" },
    new() { Text = ""پرتقال"", Value = ""f-ora"", Disabled = true },
    new() { Text = ""انگور"", Value = ""f-gra"" },
    new() { Type = BitDropdownItemType.Divider },
    new() { Type = BitDropdownItemType.Header, Text = ""سیزیجات"" },
    new() { Text = ""کلم بروكلی"", Value = ""v-bro"" },
    new() { Text = ""هویج"", Value = ""v-car"" },
    new() { Text = ""کاهو"", Value = ""v-let"" }
};

private BitDropdownNameSelectors<Product, string> nameSelectors = new() 
{
    AriaLabel = { Selector = c => c.Label },
    Id = { Selector = c => c.Key },
    Data = { Selector = c => c.Payload },
    IsEnabled = { Selector = c => c.Disabled is false },
    IsHidden = { Selector = c => c.Visible is false },
    ItemType = { Selector = c => c.Type },
    Text = { Selector = c => c.Text },
    Title = { Selector = c => c.Title },
    Value = { Selector = c => c.Value },
};";
}
