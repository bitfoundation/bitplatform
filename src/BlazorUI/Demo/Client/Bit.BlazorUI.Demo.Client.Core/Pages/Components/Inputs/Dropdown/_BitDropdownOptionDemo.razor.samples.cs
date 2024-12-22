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
    <BitDropdownOption Text=""Orange"" Value=""@(""f-org"")"" IsEnabled=""false"" />
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
    private readonly string example2CsharpCode = @"
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

    private readonly string example3RazorCode = @"
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
<BitDropdown Label=""Responsive Dropdown""
             Responsive
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <BitDropdownOption ItemType=""BitDropdownItemType.Header"" Text=""Fruits"" Value=""@string.Empty"" />
    <BitDropdownOption Text=""Apple"" Value=""@(""f-app"")"" />
    <BitDropdownOption Text=""Banana"" Value=""@(""f-ban"")"" />
    <BitDropdownOption Text=""Orange"" Value=""@(""f-org"")"" IsEnabled=""false"" />
    <BitDropdownOption Text=""Grape"" Value=""@(""f-gra"")"" />
    <BitDropdownOption ItemType=""BitDropdownItemType.Divider"" Value=""@string.Empty"" />
    <BitDropdownOption ItemType=""BitDropdownItemType.Header"" Text=""Vegetables"" Value=""@string.Empty"" />
    <BitDropdownOption Text=""Broccoli"" Value=""@(""f-bro"")"" />
    <BitDropdownOption Text=""Carrot"" Value=""@(""f-car"")"" />
    <BitDropdownOption Text=""Lettuce"" Value=""@(""f-let"")"" />
</BitDropdown>";

    private readonly string example6RazorCode = @"
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
    private readonly string example6CsharpCode = @"
private ICollection<BitDropdownItem<string>> dropDirectionItems = default!;

protected override void OnInitialized()
{
    dropDirectionItems = Enumerable.Range(1, 15)
                                   .Select(c => new BitDropdownItem<string> { Value = c.ToString(), Text = $""Category {c}"" })
                                   .ToArray();
}";

    private readonly string example7RazorCode = @"
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
<div>Values: @string.Join(',', clearValues)</div>";
    private readonly string example7CsharpCode = @"
private string? clearValue = ""f-app"";
private ICollection<string?> clearValues = new[] { ""f-app"", ""f-ban"" };

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

    private readonly string example8RazorCode = @"
<BitDropdown Label=""Single select & auto focus""
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
    private readonly string example8CsharpCode = @"
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
                 Placeholder=""Select and item""
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
    private readonly string example9CsharpCode = @"
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

    private readonly string example10RazorCode = @"
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
    private readonly string example10CsharpCode = @"
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

    private readonly string example11RazorCode = @"
<BitDropdown @bind-Value=""controlledValue""
             Label=""Single select""
             Placeholder=""Select an item""
             TItem=""BitDropdownOption<string>"" TValue=""string"">
    <BitDropdownOption ItemType=""BitDropdownItemType.Header"" Text=""Fruits"" Value=""@string.Empty"" />
    <BitDropdownOption Text=""Apple"" Value=""@(""f-app"")"" />
    <BitDropdownOption Text=""Banana"" Value=""@(""f-ban"")"" />
    <BitDropdownOption Text=""Orange"" Value=""@(""f-org"")"" IsEnabled=""false"" />
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
             DefaultValue=""@string.Empty""
             TItem=""BitDropdownOption<string>"" TValue=""string""
             OnSelectItem=""(BitDropdownOption<string> item) => selectedItem2 = item"">
    @foreach (var item in basicItems)
    {
        <BitDropdownOption ItemType=""item.ItemType"" Text=""@item.Text"" Value=""item.Value"" IsEnabled=""item.IsEnabled"" />
    }
</BitDropdown>
<div>Selected Value: @selectedItem2?.Value</div>";
    private readonly string example11CsharpCode = @"
private string controlledValue = ""f-app"";
private IEnumerable<string?> controlledValues = [""f-app"", ""f-ban""];

private string? changedValue;
private IEnumerable<string> changedValues = [];

private BitDropdownOption<string>? selectedItem1;
private BitDropdownOption<string>? selectedItem2;

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

    private readonly string example12RazorCode = @"
<BitDropdown @bind-Value=""comboBoxValueSample1""
             Combo
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
<br /><br />
<BitDropdown @bind-Values=""comboBoxValues1""
             Combo 
             MultiSelect
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
    private readonly string example12CsharpCode = @"
private string comboBoxValueSample1 = default!;
private ICollection<string> comboBoxValues1 = [];

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

    private readonly string example13RazorCode = @"
<BitDropdown @bind-Value=""comboBoxValueSample2""
             Combo Chips
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
             MultiSelect
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
    private readonly string example13CsharpCode = @"
private string comboBoxValueSample2 = default!;
private ICollection<string> comboBoxValues2 = [];

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

    private readonly string example14RazorCode = @"
<BitDropdown @bind-Value=""comboBoxValueSample3""
             Combo Dynamic
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
    private readonly string example14CsharpCode = @"
private string comboBoxValueSample3 = default!;
private string comboBoxValueSample4 = default!;
private ICollection<string> comboBoxValues3 = [];

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

    private readonly string example15RazorCode = @"
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
    private readonly string example15CsharpCode = @"
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

    private readonly string example16RazorCode = @"
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
    private readonly string example16CsharpCode = @"
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
