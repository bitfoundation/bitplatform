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
             NameSelectors=""nameSelectors"" />";
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

private BitDropdownNameSelectors<Product, string?> nameSelectors = new() 
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
    private readonly string example2CsharpCode = @"
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

private BitDropdownNameSelectors<Product, string?> nameSelectors = new() 
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

    private readonly string example3RazorCode = @"
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

private BitDropdownNameSelectors<Product, string?> nameSelectors = new() 
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
<BitDropdown NoBorder
             Items=""GetBasicCustoms()""
             Placeholder=""Select an item"" 
             NameSelectors=""nameSelectors"" />

<BitDropdown NoBorder
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

private BitDropdownNameSelectors<Product, string?> nameSelectors = new() 
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
             Responsive
             Items=""GetBasicCustoms()""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors"" />";
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

private BitDropdownNameSelectors<Product, string?> nameSelectors = new() 
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
    private readonly string example6CsharpCode = @"
private ICollection<Product>? dropDirectionCustoms;

protected override void OnInitialized()
{
    dropDirectionCustoms = Enumerable.Range(1, 15)
                                     .Select(p => new ProduceModel { Text = $""Produce {p}"", Value = p.ToString() })
                                     .ToArray();
}

private BitDropdownNameSelectors<Product, string?> nameSelectors = new() 
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
<div>Values: @string.Join(',', clearValues)</div>";
    private readonly string example7CsharpCode = @"
private string? clearValue = ""f-app"";
private ICollection<string?> clearValues = new[] { ""f-app"", ""f-ban"" };

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

private BitDropdownNameSelectors<Product, string?> nameSelectors = new() 
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
<BitDropdown Label=""Single select & auto focus""
             ShowSearchBox
             AutoFocusSearchBox
             Items=""GetBasicCustoms()""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             SearchBoxPlaceholder=""Search item"" />

<BitDropdown Label=""Multi select""
             MultiSelect
             ShowSearchBox
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors""
             SearchBoxPlaceholder=""Search items"" />


<BitDropdown Label=""Single select & auto focus""
             ShowSearchBox
             AutoFocusSearchBox
             Items=""GetBasicCustoms()""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors""
             SearchBoxPlaceholder=""Search item""
             SearchFunction=""(items, text) => items.Where(i => i.Text?.StartsWith(text, StringComparison.OrdinalIgnoreCase) ?? false).ToArray()"" />

<BitDropdown Label=""Multi select""
             MultiSelect
             ShowSearchBox
             Items=""GetBasicCustoms()""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors""
             SearchBoxPlaceholder=""Search items""
             SearchFunction=""(items, text) => items.Where(i => i.Text?.EndsWith(text, StringComparison.OrdinalIgnoreCase) ?? false).ToArray()"" />";
    private readonly string example8CsharpCode = @"
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

private BitDropdownNameSelectors<Product, string?> nameSelectors = new() 
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
                 MultiSelect
                 Items=""GetBasicCustoms()""
                 Placeholder=""Select items""
                 NameSelectors=""nameSelectors""
                 Label=""Select min 1 and max 2 items"" />
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
    private readonly string example10CsharpCode = @"
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

private BitDropdownNameSelectors<Product, string?> nameSelectors = new() 
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
             DefaultValue=""@string.Empty""
             NameSelectors=""nameSelectors""
             OnSelectItem=""(Product item) => selectedItem2 = item"" />
<div>Selected Value: @selectedItem2?.Value</div>";
    private readonly string example11CsharpCode = @"
private string controlledValue = ""f-app"";
private IEnumerable<string> controlledValues = [""f-app"", ""f-ban""];

private string? changedValue;
private IEnumerable<string> changedValues = [];

private Product? selectedItem1;
private Product? selectedItem2;

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

private BitDropdownNameSelectors<Product, string?> nameSelectors = new() 
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
<BitDropdown Label=""Single select""
             Virtualize
             Items=""virtualizeCustoms1""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors"" />

<BitDropdown Label=""Multi select""
             Virtualize
             MultiSelect
             Items=""virtualizeCustoms2""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors"" />


<BitDropdown Label=""Single select""
             Virtualize
             ItemsProvider=""LoadItems""
             Placeholder=""Select an item""
             NameSelectors=""nameSelectors"" />

<BitDropdown Label=""Multi select""
             Virtualize
             MultiSelect
             ItemsProvider=""LoadItems""
             Placeholder=""Select items""
             NameSelectors=""nameSelectors"" />

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
    private readonly string example12CsharpCode = @"
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

private BitDropdownNameSelectors<Product, string?> nameSelectors = new() 
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

        var url = NavManager.GetUriWithQueryParameters(""Products/GetProducts"", query);

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

    private readonly string example13RazorCode = @"
<BitDropdown @bind-Value=""comboBoxValueSample1""
             Combo
             Items=""comboBoxCustoms""
             Placeholder=""Select an option""
             Label=""Single select combo box""
             NameSelectors=""comboBoxNameSelectors"" />
<div>Value: @comboBoxValueSample1</div>

<BitDropdown @bind-Values=""comboBoxValues1""
             Combo 
             MultiSelect
             Items=""comboBoxCustoms""
             Label=""Multi select combo box""
             Placeholder=""Select an option""
             NameSelectors=""comboBoxNameSelectors"" />
<div>Values: @string.Join(',', comboBoxValues1)</div>";
    private readonly string example13CsharpCode = @"
private string comboBoxValueSample1 = default!;
private ICollection<string> comboBoxValues1 = [];

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
    TextSetter = (string? text, Product item) => item.Text = text
};";

    private readonly string example14RazorCode = @"
<BitDropdown @bind-Value=""comboBoxValueSample2""
             Combo Chips
             Items=""comboBoxCustoms""
             Placeholder=""Select an option""
             NameSelectors=""comboBoxNameSelectors""
             Label=""Single select combo box & chips"" />
<div>Value: @comboBoxValueSample2</div>

<BitDropdown @bind-Values=""comboBoxValues2""
             Combo Chips 
             MultiSelect
             Items=""comboBoxCustoms""
             Placeholder=""Select an option""
             NameSelectors=""comboBoxNameSelectors""
             Label=""Multi select combo box & chips"" />
<div>Values: @string.Join(',', comboBoxValues2)</div>";
    private readonly string example14CsharpCode = @"
private string comboBoxValueSample2 = default!;
private ICollection<string> comboBoxValues2 = [];

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
    TextSetter = (string? text, Product item) => item.Text = text
};";

    private readonly string example15RazorCode = @"
<BitDropdown @bind-Value=""comboBoxValueSample3""
             Combo Dynamic
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
    private readonly string example15CsharpCode = @"
private string comboBoxValueSample3 = default!;
private string comboBoxValueSample4 = default!;
private ICollection<string> comboBoxValues3 = [];

private void HandleOnDynamicAdd(Product item)
{
    comboBoxCustoms.Add(item);
}

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
    TextSetter = (string? text, Product item) => item.Text = text
};";

    private readonly string example16RazorCode = @"
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
    private readonly string example16CsharpCode = @"
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

private BitDropdownNameSelectors<Product, string?> nameSelectors = new() 
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

    private readonly string example17RazorCode = @"
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
    private readonly string example17CsharpCode = @"
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

private BitDropdownNameSelectors<Product, string?> nameSelectors = new() 
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
