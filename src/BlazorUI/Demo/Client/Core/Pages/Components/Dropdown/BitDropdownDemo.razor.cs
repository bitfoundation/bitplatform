using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Dropdown;

public partial class BitDropdownDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "CaretDownTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Optional custom template for chevron icon.",
        },
        new()
        {
            Name = "CaretDownIconName",
            Type = "string",
            DefaultValue = "ChevronDown",
            Description = "Optional chevron icon.",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "string?",
            DefaultValue = "null",
            Description = "Key that will be initially used to set selected item.",
        },
        new()
        {
            Name = "DefaultValues",
            Type = "List<string>",
            DefaultValue = "new List<string>()",
            Description = "Keys that will be initially used to set selected items for multiSelect scenarios.",
        },
        new()
        {
            Name = "IsMultiSelect",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether multiple items are allowed to be selected.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not this dropdown is open.",
        },
        new()
        {
            Name = "IsRequired",
            Type = "bool",
            DefaultValue = "false",
            Description = "Requires the end user to select an item in the dropdown.",
        },
        new()
        {
            Name = "Items",
            Type = "List<BitDropdownItem>?",
            DefaultValue = "null",
            Description = "A list of items to display in the dropdown.",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitDropdownItem>?",
            DefaultValue = "null",
            Description = "Optional custom template for dropdown item.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "the label associated with the dropdown.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title to show when the mouse is placed on the drop down.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Optional custom template for label.",
        },
        new()
        {
            Name = "MultiSelectDelimiter",
            Type = "string",
            DefaultValue = ", ",
            Description = "When multiple items are selected, this still will be used to separate values in the dropdown title.",
        },
        new()
        {
            Name = "NotifyOnReselect",
            Type = "bool",
            DefaultValue = "false",
            Description = "Optional preference to have OnSelectItem still be called when an already selected item is clicked in single select mode.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the action button clicked.",
        },
        new()
        {
            Name = "OnSelectItem",
            Type = "EventCallback<BitDropdownItem>",
            Description = "Callback for when an item is selected.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "Input placeholder Text, Displayed until an option is selected.",
        },
        new()
        {
            Name = "PlaceholderTemplate",
            Type = "RenderFragment<BitDropdown>?",
            DefaultValue = "null",
            Description = "Optional custom template for placeholder Text.",
        },
        new()
        {
            Name = "Values",
            Type = "List<string>",
            DefaultValue = "new List<string>()",
            Description = "Keys of the selected items for multiSelect scenarios. If you provide this, you must maintain selection state by observing onChange events and passing a new value in when changed.",
        },
        new()
        {
            Name = "ValuesChanged",
            Type = "EventCallback<List<string>>",
            Description = "Callback for when the values changed.",
        },
        new()
        {
            Name = "TextTemplate",
            Type = "RenderFragment<BitDropdown>?",
            DefaultValue = "null",
            Description = "Optional custom template for selected option displayed in after selection.",
        },
        new()
        {
            Name = "IsResponsiveModeEnabled",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the drop down items get rendered in a side panel in small screen sizes or not.",
        },
        new()
        {
            Name = "ShowClearButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Clear Button is shown when something is selected.",
        },
        new()
        {
            Name = "ShowSearchBox",
            Type = "bool",
            DefaultValue = "false",
            Description = "Search box is enabled for the end user.",
        },
        new()
        {
            Name = "AutoFocusSearchBox",
            Type = "bool",
            DefaultValue = "false",
            Description = "Auto focus on search box when dropdown is open.",
        },
        new()
        {
            Name = "OnSearch",
            Type = "EventCallback<string>",
            Description = "Callback for when the search box input value changes.",
        },
        new()
        {
            Name = "SearchBoxPlaceholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "Search box input placeholder text.",
        },
        new()
        {
            Name = "Virtualize",
            Type = "bool",
            DefaultValue = "false",
            Description = "virtualize rendering the list, UI rendering to just the parts that are currently visible.",
        },
        new()
        {
            Name = "ItemSize",
            Type = "int",
            DefaultValue = "35",
            Description = "The height of each item in pixels.",
        },
        new()
        {
            Name = "ItemsProvider",
            Type = "BitDropdownItemsProvider<BitDropdownItem>?",
            DefaultValue = "null",
            Description = "The function providing items to the list.",
        },
        new()
        {
            Name = "VirtualizePlaceholder",
            Type = "RenderFragment<PlaceholderContext>?",
            DefaultValue = "null",
            Description = "The template for items that have not yet been loaded in memory.",
        },
        new()
        {
            Name = "OverscanCount",
            Type = "int",
            DefaultValue = "3",
            Description = "determines how many additional items are rendered before and after the visible region.",
        },
        new()
        {
            Name = "SelectedItems",
            Type = "List<BitDropdownItem>",
            DefaultValue = "new List<BitDropdownItem>()",
            Description = "The selected items for multiSelect scenarios.",
        },
        new()
        {
            Name = "SelectedItemsChanged",
            Type = "EventCallback<List<BitDropdownItem>>",
            Description = "Callback for when the SelectedItems changed.",
        },
        new()
        {
            Name = "SelectedItem",
            Type = "BitDropdownItem?",
            DefaultValue = "null",
            Description = "The selected item for singleSelect scenarios.",
        },
        new()
        {
            Name = "SelectedItemChanged",
            Type = "EventCallback<BitDropdownItem>",
            Description = "Callback for when the SelectedItem changed.",
        },
        new()
        {
            Name = "IsRtl",
            Type = "bool",
            DefaultValue = "false",
            Description = "Change direction to RTL.",
        },
        new()
        {
            Name = "DropDirection",
            Type = "BitDropDirection",
            DefaultValue = "BitDropDirection.TopAndBottom",
            Description = "Dropdown opening direction.",
        },
    };



    private readonly string example1HTMLCode = @"
<BitDropdown Label=""Single-select""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option"" />

<BitDropdown Label=""Disabled with defaultValue""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option""
             IsEnabled=""false""
             DefaultValue=""v-bro"" />

<BitDropdown Label=""Multi-select""
             Items=""GetDropdownItems()""
             Placeholder=""Select options""
             IsMultiSelect=""true"" />";
    private readonly string example1CSharpCode = @"
private List<BitDropdownItem> GetDropdownItems()
{
    return new()
    {
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""Fruits""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Apple"",
            Value = ""f-app""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Orange"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Banana"",
            Value = ""f-ban"",
        },
        new()
        {
            ItemType = BitDropdownItemType.Divider,
        },
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""Vegetables""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Broccoli"",
            Value = ""v-bro"",
        }
    };
}";

    private readonly string example2HTMLCode = @"
<BitDropdown Label=""Single-select Controlled""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option""
             @bind-Value=""ControlledValue"" />
<BitLabel>Selected Value: @ControlledValue</BitLabel>

<BitDropdown @bind-Values=""ControlledValues""
             Label=""Multi-select controlled""
             Items=""GetDropdownItems()""
             Placeholder=""Select options""
             IsMultiSelect=""true"" />
<BitLabel>Selected Value: @string.Join("","", ControlledValues)</BitLabel>";
    private readonly string example2CSharpCode = @"
private string ControlledValue = ""Apple"";
private List<BitDropdownItem> GetDropdownItems()
{
    return new()
    {
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""Fruits""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Apple"",
            Value = ""f-app""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Orange"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Banana"",
            Value = ""f-ban"",
        },
        new()
        {
            ItemType = BitDropdownItemType.Divider,
        },
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""Vegetables""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Broccoli"",
            Value = ""v-bro"",
        }
    };
}";

    private readonly string example3HTMLCode = @"
<BitDropdown Label=""Multi-select controlled""
             Items=""GetDropdownItems()""
             Placeholder=""Select options""
             @bind-Values=""ControlledValues""
             IsMultiSelect=""true"" />";
    private readonly string example3CSharpCode = @"
private List<string> ControlledValues = new() { ""Apple"", ""Banana"", ""Grape"" };
private List<BitDropdownItem> GetDropdownItems()
{
    return new()
    {
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""Fruits""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Apple"",
            Value = ""f-app""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Orange"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Banana"",
            Value = ""f-ban"",
        },
        new()
        {
            ItemType = BitDropdownItemType.Divider,
        },
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""Vegetables""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Broccoli"",
            Value = ""v-bro"",
        }
    };
}";

    private readonly string example4HTMLCode = @"
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

<BitDropdown Label=""Custom Items""
             Items=""GetCustomDropdownItems()""
             Placeholder=""Select an option""
             AriaLabel=""Custom dropdown"">
<TextTemplate>
    <div class=""custom-drp custom-drp-txt"">
        <BitIcon IconName=""@((context.Items.Find(i => i.Value == context.Value).Data as DropdownItemData).IconName)"" />
        <BitLabel>@context.Items.Find(i => i.Value == context.Value).Text</BitLabel>
    </div>
</TextTemplate>
<PlaceholderTemplate>
    <div class=""custom-drp custom-drp-ph"">
        <BitIcon IconName=""@BitIconName.MessageFill"" />
        <BitLabel>@context.Placeholder</BitLabel>
    </div>
</PlaceholderTemplate>
<CaretDownTemplate>
    <div class=""custom-drp"">
        <BitIcon IconName=""@BitIconName.CirclePlus"" />
    </div>
</CaretDownTemplate>
<ItemTemplate>
    <div class=""custom-drp custom-drp-item"">
        <BitIcon IconName=""@((context.Data as DropdownItemData).IconName)"" />
        <BitLabel>@context.Text</BitLabel>
    </div>
</ItemTemplate>
</BitDropdown>

<BitDropdown Items=""GetCustomDropdownItems()""
             Placeholder=""Select an option""
             Label=""Custom Label""
             IsMultiSelect=""true""
             AriaLabel=""Custom dropdown label"">
<LabelTemplate>
    <div class=""custom-drp custom-drp-lbl"">
        <BitLabel>Custom label</BitLabel>
        <BitIcon IconName=""@BitIconName.Info"" AriaLabel=""Info"" />
    </div>
</LabelTemplate>
</BitDropdown>

<BitDropdown Items=""GetCustomDropdownItems()""
             Placeholder=""Select an option""
             Label=""Custom CaretDownIconName""
             AriaLabel=""Custom dropdown chevron icon with icon name""
             CaretDownIconName=""@BitIconName.ScrollUpDown"" />";
    private readonly string example4CSharpCode = @"
private List<BitDropdownItem> GetCustomDropdownItems()
{
    return new List<BitDropdownItem>()
    {
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""Options"",
            Value = ""Header""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Option a"",
            Value = ""A"",
            Data = new DropdownItemData { IconName = ""Memo"" }
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Option b"",
            Value = ""B"",
            Data = new DropdownItemData { IconName = ""Print"" }
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Option c"",
            Value = ""C"",
            Data = new DropdownItemData { IconName = ""ShoppingCart"" }
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Option d"",
            Value = ""D"",
            Data = new DropdownItemData { IconName = ""Train"" }
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Option e"",
            Value = ""E"",
            Data = new DropdownItemData { IconName = ""Repair"" }
        },
        new()
        {
            ItemType = BitDropdownItemType.Divider
        },
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""More options"",
            Value = ""Header2""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Option f"",
            Value = ""F"",
            Data = new DropdownItemData { IconName = ""Running"" }
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Option g"",
            Value = ""G"",
            Data = new DropdownItemData { IconName = ""EmojiNeutral"" }
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Option h"",
            Value = ""H"",
            Data = new DropdownItemData { IconName = ""ChatInviteFriend"" }
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Option i"",
            Value = ""I"",
            Data = new DropdownItemData { IconName = ""SecurityGroup"" }
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Option j"",
            Value = ""J"",
            Data = new DropdownItemData { IconName = ""AddGroup"" }
        }
    };
}";

    private readonly string example5HTMLCode = @"
<BitDropdown Label=""Category""
             Items=""Categories""
             Placeholder=""Select options""
             @bind-Value=""@CurrentCategory"" />

<BitDropdown Label=""Product""
             Items=""@(Products.Where(p => p.Value.StartsWith($""{CurrentCategory}-"")).ToList())""
             Placeholder=""Select options""
             @bind-Value=""@CurrentProduct""
             IsEnabled=""string.IsNullOrEmpty(CurrentCategory) is false"" />

<BitLabel>Current category: @(Categories.FirstOrDefault(c => c.Value == CurrentCategory)?.Text ?? ""-"")</BitLabel>
<BitLabel>Current product: @(Products.FirstOrDefault(c => c.Value == CurrentProduct)?.Text ?? ""-"")</BitLabel>";
    private readonly string example5CSharpCode = @"
private List<BitDropdownItem> Categories = new();
private List<BitDropdownItem> Products = new();
private string CurrentCategory;
private string CurrentProduct;

protected override void OnInitialized()
{
    Categories = Enumerable.Range(1, 6).Select(c => new BitDropdownItem
    {
        ItemType = BitDropdownItemType.Normal,
        Value = c.ToString(),
        Text = $""Category {c}""
    }).ToList();

    Products = Enumerable.Range(1, 50).Select(p => new BitDropdownItem
    {
        ItemType = BitDropdownItemType.Normal,
        Text = $""Product {p}"",
        Value = $""{((int)Math.Ceiling((double)p % 7))}-{p}""
    }).ToList();

    base.OnInitialized();
}";

    private readonly string example6HTMLCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""formValidationDropdownModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <div class=""validation-summary"">
            <ValidationSummary />
        </div>

        <BitDropdown Label=""Select category""
                     Items=""GetCategoryDropdownItems()""
                     IsMultiSelect=""false""
                     @bind-Value=""formValidationDropdownModel.Category""
                     Placeholder=""Select an option"" />

        <ValidationMessage For=""@(() => formValidationDropdownModel.Category)"" />

        <br />

        <BitDropdown Label=""Select two ptoducts""
                     Items=""GetProductDropdownItems()""
                     IsMultiSelect=""true""
                     @bind-Values=""formValidationDropdownModel.Products""
                     Placeholder=""Select an option"" />

        <ValidationMessage For=""@(() => formValidationDropdownModel.Products)"" />

        <br />

        <BitButton ButtonType=""BitButtonType.Submit"">
            Submit
        </BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        @SuccessMessage
    </BitMessageBar>
}";
    private readonly string example6CSharpCode = @"
public class FormValidationDropdownModel
{
    [MaxLength(2, ErrorMessage = ""The property {0} doesn't have more than {1} elements"")]
    [MinLength(1, ErrorMessage = ""The property {0} doesn't have less than {1} elements"")]
    public List<string> Products { get; set; } = new();

    [Required]
    public string Category { get; set; }
}

private FormValidationDropdownModel formValidationDropdownModel = new();
private string SuccessMessage = string.Empty;

private async Task HandleValidSubmit()
{
    SuccessMessage = ""Form Submitted Successfully!"";
    await Task.Delay(3000);
    SuccessMessage = string.Empty;
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}

private List<BitDropdownItem> GetCategoryDropdownItems()
{
    return new()
    {
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Fruits"",
            Value = ""f""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Vegetables"",
            Value = ""v""
        }
    };
}

private List<BitDropdownItem> GetProductDropdownItems()
{
    return new()
    {
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""Fruits""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Apple"",
            Value = ""f-app""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Orange"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Banana"",
            Value = ""f-ban"",
        },
        new()
        {
            ItemType = BitDropdownItemType.Divider,
        },
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""Vegetables""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Broccoli"",
            Value = ""v-bro"",
        }
    };
}";

    private readonly string example7HTMLCode = @"
<BitDropdown Label=""Responsive Dropdown""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option""
             IsResponsiveModeEnabled=true />";
    private readonly string example7CSharpCode = @"
private List<BitDropdownItem> GetDropdownItems()
{
    return new()
    {
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""Fruits""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Apple"",
            Value = ""f-app""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Orange"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Banana"",
            Value = ""f-ban"",
        },
        new()
        {
            ItemType = BitDropdownItemType.Divider,
        },
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""Vegetables""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Broccoli"",
            Value = ""v-bro"",
        }
    };
}";

    private readonly string example8HTMLCode = @"
<BitDropdown Label=""Single-select dropdown with search box""
                Items=""GetDropdownItems()""
                Placeholder=""Select an option""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search item"" />

<BitDropdown Label=""Multi-select dropdown with search box""
                Items=""GetDropdownItems()""
                Placeholder=""Select options""
                IsMultiSelect=""true""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search items"" />";
    private readonly string example8CSharpCode = @"
private string ControlledValue = ""Apple"";
private List<BitDropdownItem> GetDropdownItems()
{
    return new()
    {
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""Fruits""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Apple"",
            Value = ""f-app""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Orange"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Banana"",
            Value = ""f-ban"",
        },
        new()
        {
            ItemType = BitDropdownItemType.Divider,
        },
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""Vegetables""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Broccoli"",
            Value = ""v-bro"",
        }
    };
}";

    private readonly string example9HTMLCode = @"
<BitDropdown Label=""Single-select dropdown with virtualization""
             Items=""LargeListOfCategoriesForSingleSelect""
             Virtualize=""true""
             Placeholder=""Select an option""
             IsResponsiveModeEnabled=""true""
             ShowSearchBox=""true""
             SearchBoxPlaceholder=""Search item"" />

<BitDropdown Label=""Multi-select dropdown with virtualization""
             Items=""LargeListOfCategoriesForMultiSelect""
             Virtualize=""true""
             Placeholder=""Select options""
             IsMultiSelect=""true""
             IsResponsiveModeEnabled=""true""
             ShowSearchBox=""true""
             SearchBoxPlaceholder=""Search items"" />";
    private readonly string example9CSharpCode = @"
private List<BitDropdownItem> LargeListOfCategories = new ();

protected override void OnInitialized()
{
    LargeListOfCategoriesForSingleSelect = Enumerable.Range(1, 4000).Select(c => new BitDropdownItem
    {
        ItemType = BitDropdownItemType.Normal,
        Value = c.ToString(),
        Text = $""Category {c}""
    }).ToList();

    LargeListOfCategoriesForMultiSelect = Enumerable.Range(1, 4000).Select(c => new BitDropdownItem
    {
        ItemType = BitDropdownItemType.Normal,
        Value = c.ToString(),
        Text = $""Category {c}""
    }).ToList();

    base.OnInitialized();
}";

    private readonly string example10HTMLCode = @"
<BitDropdown Label=""Single-select dropdown with virtualization""
             ItemsProvider=""LoadDropdownItems""
             Virtualize=""true""
             Placeholder=""Select an option""
             IsResponsiveModeEnabled=""true""
             ShowSearchBox=""true""
             SearchBoxPlaceholder=""Search item"" />

<BitDropdown Label=""Multi-select dropdown with virtualization""
             ItemsProvider=""LoadDropdownItems""
             Virtualize=""true""
             Placeholder=""Select options""
             IsMultiSelect=""true""
             IsResponsiveModeEnabled=""true""
             ShowSearchBox=""true""
             SearchBoxPlaceholder=""Search items"" />";
    private readonly string example10CSharpCode = @"
private async ValueTask<BitDropdownItemsProviderResult<BitDropdownItem>> LoadDropdownItems(BitDropdownItemsProviderRequest<BitDropdownItem> request)
{
    try
    {
        var query = new Dictionary<string, object>()
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

        var items = data!.Items.Select(i => new BitDropdownItem
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
        return BitDropdownItemsProviderResult.From(new List<BitDropdownItem>(), 0);
    }
}";

    private readonly string example11HTMLCode = @"
<BitDropdown Label=""Single-select dropdown with Rtl direction""
             Items=""GetArabicDropdownItems()""
             Placeholder=""حدد اختيارا""
             IsResponsiveModeEnabled=""true""
             IsRtl=""true""
             ShowSearchBox=""true""
             SearchBoxPlaceholder=""عناصر البحث"" />
<BitDropdown Label=""Multi-select dropdown with Rtl direction""
             Items=""GetArabicDropdownItems()""
             Placeholder=""اشر على الخيارات""
             IsMultiSelect=""true""
             IsResponsiveModeEnabled=""true""
             IsRtl=""true""
             ShowSearchBox=""true""
             SearchBoxPlaceholder=""عناصر البحث"" />";
    private readonly string example11CSharpCode = @"
private List<BitDropdownItem> GetArabicDropdownItems()
{
    return new()
    {
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""الفاكهة""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""تفاحة"",
            Value = ""f-app""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""البرتقالي"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""موز"",
            Value = ""f-ban"",
        },
        new()
        {
            ItemType = BitDropdownItemType.Divider,
        },
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""خضروات""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""بروكلي"",
            Value = ""v-bro"",
        }
    };
}";

    private readonly string example12HTMLCode = @"
<BitDropdown Label=""Auto drop direction""
             Items=""LargeListOfCategoriesDropDirection""
             Virtualize=""true""
             Placeholder=""Select an option""
             IsResponsiveModeEnabled=""true""
             ShowSearchBox=""true""
             SearchBoxPlaceholder=""Search item""
             DropDirection=""BitDropDirection.Auto"" />

<BitDropdown Label=""Top and bottom drop direction""
             Items=""LargeListOfCategoriesDropDirection""
             Virtualize=""true""
             Placeholder=""Select an option""
             IsResponsiveModeEnabled=""true""
             ShowSearchBox=""true""
             SearchBoxPlaceholder=""Search item""
             DropDirection=""BitDropDirection.TopAndBottom"" />";
    private readonly string example12CSharpCode = @"
private List<BitDropdownItem> LargeListOfCategoriesDropDirection = new();

protected override void OnInitialized()
{
    LargeListOfCategoriesDropDirection = Enumerable.Range(1, 60).Select(c => new BitDropdownItem
    {
        ItemType = BitDropdownItemType.Normal,
        Value = c.ToString(),
        Text = $""Category {c}""
    }).ToList();

    base.OnInitialized();
}";

    private readonly string example13HTMLCode = @"
<BitDropdown @bind-Value=""SelectedValue""
             ShowClearButton=""true""
             Label=""Single-select dropdown""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option"" />
<BitLabel>Value: @SelectedValue</BitLabel>

<BitDropdown @bind-Values=""SelectedValues""
             ShowClearButton=""true""
             Label=""Multi-select dropdown""
             Items=""GetDropdownItems()""
             Placeholder=""Select options""
             IsMultiSelect=""true"" />
<BitLabel>Values: @string.Join(',', SelectedValues)</BitLabel>";
    private readonly string example13CSharpCode = @"
private string? SelectedValue;
private List<string> SelectedValues = new();

private List<BitDropdownItem> GetDropdownItems()
{
    return new()
    {
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""Fruits""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Apple"",
            Value = ""f-app""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Orange"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Banana"",
            Value = ""f-ban"",
        },
        new()
        {
            ItemType = BitDropdownItemType.Divider,
        },
        new()
        {
            ItemType = BitDropdownItemType.Header,
            Text = ""Vegetables""
        },
        new()
        {
            ItemType = BitDropdownItemType.Normal,
            Text = ""Broccoli"",
            Value = ""v-bro"",
        }
    };
}";
}
