namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class _BitDropdownCustomDemo
{
    private readonly string example1RazorCode = @"
<BitDropdown Label=""Single select""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Multi select""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items""
             IsMultiSelect=""true"" />

<BitDropdown Label=""Required"" Required
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Disabled""
             Items=""GetBasicCustoms()""
             DefaultValue=""@(""f-ora"")""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             IsEnabled=""false"" />";
    private readonly string example1CsharpCode = @"
public class BitDropdownCustom
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

private List<BitDropdownCustom> GetBasicCustoms() => new()
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

private BitDropdownNameSelectors<BitDropdownCustom, string?> nameSelectors = new() 
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

    .custom-container, .custom-container:after {
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
    private readonly string example2CsharpCode = @"
public class BitDropdownCustom
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

private List<BitDropdownCustom> GetBasicCustoms() => new()
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

private List<BitDropdownCustom> GetStyleClassCustoms() => new()
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

private BitDropdownNameSelectors<BitDropdownCustom, string?> nameSelectors = new() 
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

    private readonly string example3RazorCode = @"
<BitDropdown @bind-Value=""controlledValue""
             Label=""Single select""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />
<div>Selected Value: @controlledValue</div>

<BitDropdown @bind-Values=""controlledValues""
             Label=""Multi select""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items""
             IsMultiSelect=""true"" />
<div>Selected Values: @string.Join("","", controlledValues)</div>



<BitDropdown Label=""Single select""
             Items=""GetBasicCustoms()""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             TItem=""BitDropdownCustom"" TValue=""string""
             OnValuesChange=""(BitDropdownCustom[] items) => changedItem = items.SingleOrDefault()"" />
<div>Changed Value: @changedItem?.Value</div>

<BitDropdown Label=""Multi select""
             IsMultiSelect=""true""
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors""
             TItem=""BitDropdownCustom"" TValue=""string""
             OnValuesChange=""(BitDropdownCustom[] items) => changedItems = items"" />
<div>Changed Values: @string.Join("","", changedItems.Select(i => i.Value))</div>



<BitDropdown Label=""Single select""
             Items=""GetBasicCustoms()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             OnSelectItem=""(BitDropdownCustom item) => selectedItem1 = item"" />
<div>Selected Value: @selectedItem1?.Value</div>

<BitDropdown Label=""Multi select""
             IsMultiSelect=""true""
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             DefaultValue=""@string.Empty""
             NameSelectors=""nameSelectors""
             OnSelectItem=""(BitDropdownCustom item) => selectedItem2 = item"" />
<div>Selected Value: @selectedItem2?.Value</div>";
    private readonly string example3CsharpCode = @"
private string? controlledValue = ""f-app"";
private ICollection<string?> controlledValues = new[] { ""f-app"", ""f-ban"" };

private BitDropdownCustom? changedItem;
private BitDropdownCustom[] changedItems = Array.Empty<BitDropdownCustom>();

private BitDropdownCustom? selectedItem1;
private BitDropdownCustom? selectedItem2;

public class BitDropdownCustom
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

private List<BitDropdownCustom> GetBasicCustoms() => new()
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

private BitDropdownNameSelectors<BitDropdownCustom, string?> nameSelectors = new() 
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
    private readonly string example4CsharpCode = @"
public class BitDropdownCustom
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

private List<BitDropdownCustom> GetDataCustoms() => new()
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

private BitDropdownNameSelectors<BitDropdownCustom, string?> nameSelectors = new() 
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
<BitDropdown Label=""Responsive Dropdown""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             IsResponsive=true />";
    private readonly string example5CsharpCode = @"
public class BitDropdownCustom
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

private List<BitDropdownCustom> GetBasicCustoms() => new()
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

private BitDropdownNameSelectors<BitDropdownCustom, string?> nameSelectors = new() 
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
<BitDropdown Label=""Single select & auto foucs""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             ShowSearchBox=""true""
             AutoFocusSearchBox=""true""
             SearchBoxPlaceholder=""Search item"" />

<BitDropdown Label=""Multi select""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items""
             IsMultiSelect=""true""
             ShowSearchBox=""true""
             SearchBoxPlaceholder=""Search items"" />



<BitDropdown Label=""Single select & auto focus""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             ShowSearchBox=""true""
             AutoFocusSearchBox=""true""
             SearchFunction=""(items, text) => items.Where(i => i.Text?.StartsWith(text, StringComparison.OrdinalIgnoreCase) ?? false).ToArray()""
             SearchBoxPlaceholder=""Search item"" />

<BitDropdown Label=""Multi select""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items""
             IsMultiSelect=""true""
             ShowSearchBox=""true""
             SearchFunction=""(items, text) => items.Where(i => i.Text?.EndsWith(text, StringComparison.OrdinalIgnoreCase) ?? false).ToArray()""
             SearchBoxPlaceholder=""Search items"" />";
    private readonly string example6CsharpCode = @"
public class BitDropdownCustom
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

private List<BitDropdownCustom> GetBasicCustoms() => new()
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

private BitDropdownNameSelectors<BitDropdownCustom, string?> nameSelectors = new() 
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
<BitDropdown @bind-Value=""comboBoxValueSample1""
             Combo
             Label=""Single select combo box""
             Placeholder=""Select an option""
             Items=""comboBoxCustoms""
             NameSelectors=""comboBoxNameSelectors"" />
<div>Value: @comboBoxValueSample1</div>

<BitDropdown @bind-Value=""comboBoxValueSample2""
             Combo Chips
             Label=""Single select combo box & chips""
             Placeholder=""Select an option""
             Items=""comboBoxCustoms""
             NameSelectors=""comboBoxNameSelectors"" />
<div>Value: @comboBoxValueSample2</div>

<BitDropdown @bind-Value=""comboBoxValueSample3""
             Combo Dynamic
             Label=""Single select combo box & dynamic""
             Placeholder=""Select an option""
             Items=""comboBoxCustoms""
             NameSelectors=""comboBoxNameSelectors""
             DynamicValueGenerator=""@((BitDropdownCustom item) => item.Text ?? """")""
             OnDynamicAdd=""(BitDropdownCustom item) => HandleOnDynamicAdd(item)"" />
<div>Value: @comboBoxValueSample3</div>

<BitDropdown @bind-Value=""comboBoxValueSample4""
             Combo Chips Dynamic
             Label=""Single select combo box, chips & dynamic""
             Placeholder=""Select an option""
             Items=""comboBoxCustoms""
             IsResponsive=""true""
             NameSelectors=""comboBoxNameSelectors""
             DynamicValueGenerator=""@((BitDropdownCustom item) => item.Text ?? """")""
             OnDynamicAdd=""(BitDropdownCustom item) => HandleOnDynamicAdd(item)"" />
<div>Value: @comboBoxValueSample4</div>

<BitDropdown @bind-Values=""comboBoxValues""
             Combo Chips Dynamic
             Label=""Multi select combo box, chips & dynamic""
             Placeholder=""Select options""
             Items=""comboBoxCustoms""
             IsMultiSelect=""true""
             IsResponsive=""true""
             NameSelectors=""comboBoxNameSelectors""
             DynamicValueGenerator=""@((BitDropdownCustom item) => item.Text ?? """")""
             OnDynamicAdd=""(BitDropdownCustom item) => HandleOnDynamicAdd(item)"" />
<div>Values: @string.Join(',', comboBoxValues)</div>";
    private readonly string example7CsharpCode = @"
private string comboBoxValueSample1 = default!;
private string comboBoxValueSample2 = default!;
private string comboBoxValueSample3 = default!;
private string comboBoxValueSample4 = default!;
private ICollection<string> comboBoxValues = [];

private void HandleOnDynamicAdd(BitDropdownCustom item)
{
    comboBoxCustoms.Add(item);
}

public class BitDropdownCustom
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

private List<BitDropdownCustom> comboBoxCustoms = new()
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

private BitDropdownNameSelectors<BitDropdownCustom, string> comboBoxNameSelectors = new()
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
    ValueSetter = (BitDropdownCustom item, string value) => item.Value = value,
    TextSetter = (string? text, BitDropdownCustom item) => item.Text = text
};";

    private readonly string example8RazorCode = @"
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

<BitDropdown Label=""Disabled""
             Prefix=""Fruits:""
             Suffix=""kg""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             IsEnabled=""false"" />";
    private readonly string example8CsharpCode = @"
public class BitDropdownCustom
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

private List<BitDropdownCustom> GetBasicCustoms() => new()
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

private BitDropdownNameSelectors<BitDropdownCustom, string?> nameSelectors = new() 
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
<BitDropdown Label=""تک انتخابی""
             Items=""GetRtlCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""لطفا انتخاب کنید""
             Dir=""BitDir.Rtl"" />

<BitDropdown Label=""چند انتخابی""
             Items=""GetRtlCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""انتخاب چند گزینه ای""
             IsMultiSelect=""true""
             Dir=""BitDir.Rtl"" />";
    private readonly string example9CsharpCode = @"
public class BitDropdownCustom
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

private List<BitDropdownCustom> GetRtlCustoms() => new()
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

private BitDropdownNameSelectors<BitDropdownCustom, string?> nameSelectors = new() 
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
    private readonly string example10CsharpCode = @"
private ICollection<BitDropdownCustom>? dropDirectionCustoms;

protected override void OnInitialized()
{
    dropDirectionCustoms = Enumerable.Range(1, 15)
                                     .Select(c => new BitDropdownCustom { Value = c.ToString(), Text = $""Category {c}"" })
                                     .ToArray();
}

private BitDropdownNameSelectors<BitDropdownCustom, string?> nameSelectors = new() 
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

    private readonly string example11RazorCode = @"
<BitDropdown @bind-Value=""clearValue""
             Label=""Single select dropdown""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an option""
             ShowClearButton=""true"" />
<div>Value: @clearValue</div>


<BitDropdown @bind-Values=""clearValues""
             Label=""Multi select dropdown""
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select options""
             IsMultiSelect=""true""
             ShowClearButton=""true"" />
<div>Values: @string.Join(',', clearValues)</div>";
    private readonly string example11CsharpCode = @"
private string? clearValue = ""f-app"";
private ICollection<string?> clearValues = new[] { ""f-app"", ""f-ban"" };

public class BitDropdownCustom
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

private List<BitDropdownCustom> GetBasicCustoms() => new()
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

private BitDropdownNameSelectors<BitDropdownCustom, string?> nameSelectors = new() 
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
                 Placeholder=""Select and item"" />
    <ValidationMessage For=""@(() => validationModel.Category)"" />

    <BitDropdown @bind-Values=""validationModel.Products""
                 Label=""Select min 1 and max 2 items""
                 Items=""GetBasicCustoms()""
                 NameSelectors=""nameSelectors""
                 Placeholder=""Select items""
                 IsMultiSelect=""true"" />
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

public class BitDropdownCustom
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

private List<BitDropdownCustom> GetBasicCustoms() => new()
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

private BitDropdownNameSelectors<BitDropdownCustom, string> nameSelectors = new() 
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
<BitDropdown Label=""Single select""
             Items=""virtualizeCustoms1""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item""
             Virtualize=""true"" />

<BitDropdown Label=""Multi select""
             Items=""virtualizeCustoms2""
             NameSelectors=""nameSelectors""
             IsMultiSelect=""true""
             Placeholder=""Select items""
             Virtualize=""true"" />



<BitDropdown Label=""Single select""
             Virtualize=""true""
             ItemsProvider=""LoadItems""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Multi select""
             Virtualize=""true""
             IsMultiSelect=""true""
             ItemsProvider=""LoadItems""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items"" />";
    private readonly string example13CsharpCode = @"
public class BitDropdownCustom
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

private ICollection<BitDropdownCustom>? virtualizeCustoms1;
private ICollection<BitDropdownCustom>? virtualizeCustoms2;

protected override void OnInitialized()
{
    virtualizeCustoms1 = Enumerable.Range(1, 10_000)
                                   .Select(c => new BitDropdownCustom { Text = $""Category {c}"", Value = c.ToString() })
                                   .ToArray();

    virtualizeCustoms2 = Enumerable.Range(1, 10_000)
                                   .Select(c => new BitDropdownCustom { Text = $""Category {c}"", Value = c.ToString() })
                                   .ToArray();
}

private BitDropdownNameSelectors<BitDropdownCustom, string?> nameSelectors = new() 
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

private async ValueTask<BitDropdownItemsProviderResult<BitDropdownCustom>> LoadItems(
    BitDropdownItemsProviderRequest<BitDropdownCustom> request)
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

        var url = NavManager.GetUriWithQueryParameters(""Products/GetProducts"", query);

        var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

        var items = data!.Items.Select(i => new BitDropdownCustom
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
        return BitDropdownItemsProviderResult.From(new List<BitDropdownCustom>(), 0);
    }
}";

    private readonly string example14RazorCode = @"
<BitDropdown Label=""Single select"" FitWidth
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select an item"" />

<BitDropdown Label=""Multi select"" FitWidth
             Items=""GetBasicCustoms()""
             NameSelectors=""nameSelectors""
             Placeholder=""Select items""
             IsMultiSelect=""true"" />";
    private readonly string example14CsharpCode = @"
public class BitDropdownCustom
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

private List<BitDropdownCustom> GetBasicCustoms() => new()
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

private BitDropdownNameSelectors<BitDropdownCustom, string?> nameSelectors = new() 
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
