namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class _BitDropdownItemDemo
{
    private readonly string example1RazorCode = @"
<BitDropdown Label=""Single select""
             Items=""GetBasicItems()""
             Placeholder=""Select an item""
             TItem=""BitDropdownItem<string>"" TValue=""string"" />

<BitDropdown Label=""Multi select""
             Items=""GetBasicItems()""
             DefaultValue=""@("""")""
             Placeholder=""Select items""
               IsMultiSelect=""true"" />

<BitDropdown Label=""IsRequired""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             IsRequired=""true"" />

<BitDropdown Label=""Disabled""
             Items=""GetBasicItems()""
             DefaultValue=""@(""f-ora"")""
             Placeholder=""Select an item""
             IsEnabled=""false"" />";
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
<div style=""display:inline-flex;white-space:nowrap;"">
    Visible: [ <BitDropdown DefaultValue=""@("""")""
                            Items=""GetBasicItems()""
                            Placeholder=""Select an item""
                            Visibility=""BitVisibility.Visible"" /> ]
</div>
<div style=""display:inline-flex;white-space:nowrap;"">
    Hidden: [ <BitDropdown DefaultValue=""@("""")""
                            Items=""GetBasicItems()""
                            Placeholder=""Select items""
                            Visibility=""BitVisibility.Hidden"" /> ]
</div>
<div style=""display:inline-flex;white-space:nowrap;"">
    Collapsed: [ <BitDropdown DefaultValue=""@("""")""
                              Items=""GetBasicItems()""
                              Placeholder=""Select items""
                              Visibility=""BitVisibility.Collapsed"" /> ]
</div>";
    private readonly string example2CsharpCode = @"
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

    private readonly string example3RazorCode = @"
<BitDropdown @bind-Value=""controlledValue""
                Label=""Single select""
                Items=""GetBasicItems()""
                Placeholder=""Select an item"" />
<BitLabel>Selected Value: @controlledValue</BitLabel>

<BitDropdown @bind-Values=""controlledValues""
                Label=""Multi select""
                Items=""GetBasicItems()""
                Placeholder=""Select items""
                IsMultiSelect=""true"" />
<BitLabel>Selected Values: @string.Join("","", controlledValues)</BitLabel>";
    private readonly string example3CsharpCode = @"
private string controlledValue = ""f-app"";
private ICollection<string?> controlledValues = new[] { ""f-app"", ""f-ban"" };

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
<style>
    .custom-drp {
        gap: 10px;
        display: flex;
        align-items: center;
        flex-flow: row nowrap;
        justify-content: flex-start;
    }

    custom-drp.custom-drp-lbl {
        color: dodgerblue;
    }

    custom-drp.custom-drp-txt {
        color: goldenrod;
    }

    custom-drp.custom-drp-ph {
        color: orangered;
    }

    custom-drp.custom-drp-item {
        width: 100%;
        cursor: pointer;
    }
</style>

<BitDropdown Label=""Text & Item templates""
             Items=""GetDataItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"">
    <TextTemplate>
        <div class=""custom-drp custom-drp-txt"">
            <BitIcon IconName=""@((context.Items!.Single(i => i.Value == context.Value).Data as DropdownItemData)!.IconName)"" />
            <BitLabel>@context.Items!.Single(i => i.Value == context.Value).Text</BitLabel>
        </div>
    </TextTemplate>
    <ItemTemplate>
        <div class=""custom-drp custom-drp-item"">
            <BitIcon IconName=""@((context.Data as DropdownItemData)!.IconName)"" />
            <BitLabel>@context.Text</BitLabel>
        </div>
    </ItemTemplate>
</BitDropdown>

<BitDropdown Label=""Placeholder template""
             Items=""GetDataItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"">
    <PlaceholderTemplate>
        <div class=""custom-drp custom-drp-ph"">
            <BitIcon IconName=""@BitIconName.MessageFill"" />
            <BitLabel>@context.Placeholder</BitLabel>
        </div>
    </PlaceholderTemplate>
</BitDropdown>

<BitDropdown Label=""Label template""
             Items=""GetDataItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item"">
    <LabelTemplate>
        <div class=""custom-drp custom-drp-lbl"">
            <BitLabel>Custom label</BitLabel>
            <BitIcon IconName=""@BitIconName.Info"" AriaLabel=""Info"" />
        </div>
    </LabelTemplate>
</BitDropdown>

<BitDropdown Label=""CaretDownIconName""
             Items=""GetDataItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             CaretDownIconName=""@BitIconName.ScrollUpDown"" />";
    private readonly string example4CsharpCode = @"
private List<BitDropdownItem<string>> GetDataItems() =>  new()
{
    new() { ItemType = BitDropdownItemType.Header, Text = ""Items"" },
    new() { Text = ""Item a"", Value = ""A"", Data = new DropdownItemData { IconName = ""Memo"" } },
    new() { Text = ""Item b"", Value = ""B"", Data = new DropdownItemData { IconName = ""Print"" } },
    new() { Text = ""Item c"", Value = ""C"", Data = new DropdownItemData { IconName = ""ShoppingCart"" } },
    new() { ItemType = BitDropdownItemType.Divider },
    new() { ItemType = BitDropdownItemType.Header, Text = ""More Items"" },
    new() { Text = ""Item d"", Value = ""D"", Data = new DropdownItemData { IconName = ""Train"" } },
    new() { Text = ""Item e"", Value = ""E"", Data = new DropdownItemData { IconName = ""Repair"" } },
    new() { Text = ""Item f"", Value = ""F"", Data = new DropdownItemData { IconName = ""Running"" } }
};
";

    private readonly string example5RazorCode = @"
<BitDropdown Label=""Responsive Dropdown""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             IsResponsive=true />";
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
<BitDropdown Label=""Single select""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             ShowSearchBox=""true""
             SearchBoxPlaceholder=""Search item"" />

<BitDropdown Label=""Multi select""
             Items=""GetBasicItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""Select items""
             IsMultiSelect=""true""
             ShowSearchBox=""true""
             SearchBoxPlaceholder=""Search items"" />";
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
<BitDropdown Label=""Single select""
             Items=""virtualizeItems1""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             Virtualize=""true"" />

<BitDropdown Label=""Multi select""
             Items=""virtualizeItems2""
             DefaultValue=""@string.Empty""
             IsMultiSelect=""true""
             Placeholder=""Select items""
             Virtualize=""true"" />



<BitDropdown Label=""Single select""
             Virtualize=""true""
             ItemsProvider=""LoadItems""
             Placeholder=""Select an item""
             TItem=""BitDropdownItem<string>"" TValue=""string"" />

<BitDropdown Label=""Multi select""
             Virtualize=""true""
             IsMultiSelect=""true""
             ItemsProvider=""LoadItems""
             Placeholder=""Select items""
             TItem=""BitDropdownItem<string>"" TValue=""string"" />";
    private readonly string example7CsharpCode = @"
private ICollection<BitDropdownItem<string>>? virtualizeItems1;
private ICollection<BitDropdownItem<string>>? virtualizeItems2;

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

        var url = NavManager.GetUriWithQueryParameters(""Products/GetProducts"", query);

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

    private readonly string example8RazorCode = @"
<BitDropdown Label=""تک انتخابی""
             Items=""GetRtlItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""لطفا انتخاب کنید""
             IsRtl=""true"" />

<BitDropdown Label=""چند انتخابی""
             Items=""GetRtlItems()""
             DefaultValue=""@string.Empty""
             Placeholder=""انتخاب چند گزینه ای""
             IsMultiSelect=""true""
             IsRtl=""true"" />";
    private readonly string example8CsharpCode = @"
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

    private readonly string example9RazorCode = @"
<BitDropdown Label=""Auto""
             Items=""dropDirectionItems""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             DropDirection=""BitDropDirection.Auto"" />

<BitDropdown Label=""TopAndBottom""
             Items=""dropDirectionItems""
             DefaultValue=""@string.Empty""
             Placeholder=""Select an item""
             DropDirection=""BitDropDirection.TopAndBottom"" />";
    private readonly string example9CsharpCode = @"
private ICollection<BitDropdownItem<string>>? dropDirectionItems;

protected override void OnInitialized()
{
        dropDirectionItems = Enumerable.Range(1, 15)
                                    .Select(c => new BitDropdownItem<string> { Value = c.ToString(), Text = $""Category {c}"" })
                                    .ToArray();
}";

    private readonly string example10RazorCode = @"
<BitDropdown @bind-Value=""clearValue""
             Label=""Single select dropdown""
             Items=""GetBasicItems()""
             Placeholder=""Select an option""
             ShowClearButton=""true"" />
<BitLabel>Value: @clearValue</BitLabel>


<BitDropdown @bind-Values=""clearValues""
             Label=""Multi select dropdown""
             Items=""GetBasicItems()""
             Placeholder=""Select options""
             IsMultiSelect=""true""
             ShowClearButton=""true"" />
<BitLabel>Values: @string.Join(',', clearValues)</BitLabel>";
    private readonly string example10CsharpCode = @"
private string? clearValue = ""f-app"";
private ICollection<string?> clearValues = new[] { ""f-app"", ""f-ban"" };

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
@using System.ComponentModel.DataAnnotations;

<style>
    .validation-message {
        color: #A4262C;
        font-size: 0.75rem;
    }
</style>

<EditForm style=""width: 100%"" Model=""validationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <BitDropdown @bind-Value=""validationModel.Category""
                 Label=""Select 1 item""
                 Items=""GetBasicItems()""
                 Placeholder=""Select an item"" />
    <ValidationMessage For=""@(() => validationModel.Category)"" />

    <BitDropdown @bind-Values=""validationModel.Products""
                 Label=""Select min 1 and max 2 items""
                 Items=""GetBasicItems()""
                 Placeholder=""Select items""
                 IsMultiSelect=""true"" />
    <ValidationMessage For=""@(() => validationModel.Products)"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example11CsharpCode = @"
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
}
