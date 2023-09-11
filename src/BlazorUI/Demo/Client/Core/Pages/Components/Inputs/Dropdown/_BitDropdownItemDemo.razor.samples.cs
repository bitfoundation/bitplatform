namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class _BitDropdownItemDemo
{
    private readonly string example1HtmlCode = @"
<BitDropdown Label=""Single select"" Items=""GetDropdownItems()"" Placeholder=""Select an option"" />

<BitDropdown Label=""Multi select"" Items=""GetDropdownItems()"" Placeholder=""Select options"" IsMultiSelect=""true"" />

<BitDropdown Label=""IsRequired"" Items=""GetDropdownItems()"" Placeholder=""Select an option"" IsRequired=""true"" />

<BitDropdown Label=""Disabled"" Items=""GetDropdownItems()"" Placeholder=""Select an option"" IsEnabled=""false"" />

<BitDropdown Label=""Disabled with default value"" Items=""GetDropdownItems()"" Placeholder=""Select an option"" IsEnabled=""false"" DefaultValue=""v-bro"" />";
    private readonly string example1CsharpCode = @"
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

    private readonly string example2HtmlCode = @"
Visible: [ <BitDropdown @bind-Value=""SelectedValue""
                        Items=""GetDropdownItems()""
                        Placeholder=""Select an option""
                        Visibility=""BitVisibility.Visible"" /> ]

Hidden: [ <BitDropdown @bind-Values=""SelectedValues""
                       Items=""GetDropdownItems()""
                       Placeholder=""Select options""
                       Visibility=""BitVisibility.Hidden"" /> ]

Collapsed: [ <BitDropdown @bind-Values=""SelectedValues""
                          Items=""GetDropdownItems()""
                          Placeholder=""Select options""
                          Visibility=""BitVisibility.Collapsed"" /> ]";
    private readonly string example2CsharpCode = @"
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

    private readonly string example3HtmlCode = @"
<BitDropdown @bind-Value=""ControlledValue""
             Label=""Single select""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option"" />
<BitLabel>Selected Value: @ControlledValue</BitLabel>

<BitDropdown @bind-Values=""ControlledValues""
             Label=""Multi select""
             Items=""GetDropdownItems()""
             Placeholder=""Select options""
             IsMultiSelect=""true"" />
<BitLabel>Selected Value: @string.Join("","", ControlledValues)</BitLabel>";
    private readonly string example3CsharpCode = @"
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

    private readonly string example4HtmlCode = @"
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
<ItemTemplate>
    <div class=""custom-drp custom-drp-item"">
        <BitIcon IconName=""@((context.Data as DropdownItemData).IconName)"" />
        <BitLabel>@context.Text</BitLabel>
    </div>
</ItemTemplate>
</BitDropdown>
                
<BitDropdown Label=""Custom placeholder""
             Items=""GetCustomDropdownItems()""
             Placeholder=""Select an option""
             AriaLabel=""Custom placeholder"">
<PlaceholderTemplate>
    <div class=""custom-drp custom-drp-ph"">
        <BitIcon IconName=""@BitIconName.MessageFill"" />
        <BitLabel>@context.Placeholder</BitLabel>
    </div>
</PlaceholderTemplate>
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
    private readonly string example4CsharpCode = @"
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

    private readonly string example5HtmlCode = @"
<BitDropdown @bind-Value=""@CurrentCategory""
             Label=""Category""
             Items=""Categories""
             Placeholder=""Select options"" />

<BitDropdown Label=""Product""
             Placeholder=""Select options""
             IsEnabled=""string.IsNullOrEmpty(CurrentCategory) is false""
             Items=""@(Products.Where(p => p.Value.StartsWith($""{CurrentCategory}-"")).ToList())"" />";
    private readonly string example5CsharpCode = @"
private string CurrentCategory;
private List<BitDropdownItem> Categories = new();
private List<BitDropdownItem> Products = new();

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

    private readonly string example6HtmlCode = @"
<BitDropdown Label=""Responsive Dropdown""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option""
             IsResponsiveModeEnabled=true />";
    private readonly string example6CsharpCode = @"
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

    private readonly string example7HtmlCode = @"
<BitDropdown Label=""Single select dropdown with search box""
                Items=""GetDropdownItems()""
                Placeholder=""Select an option""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search item"" />

<BitDropdown Label=""Multi select dropdown with search box""
                Items=""GetDropdownItems()""
                Placeholder=""Select options""
                IsMultiSelect=""true""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search items"" />";
    private readonly string example7CsharpCode = @"
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

    private readonly string example8HtmlCode = @"
<BitDropdown Label=""Single select""
             Items=""LargeListOfCategoriesForSingleSelect""
             Virtualize=""true""
             ShowSearchBox=""true""
             Placeholder=""Select an option"" />

<BitDropdown Label=""Multi select""
             Items=""LargeListOfCategoriesForMultiSelect""
             Virtualize=""true""
             IsMultiSelect=""true""
             Placeholder=""Select options"" />



<BitDropdown Label=""Single select""
             ItemsProvider=""LoadDropdownItems""
             Virtualize=""true""
             ShowSearchBox=""true""
             Placeholder=""Select an option"" />

<BitDropdown Label=""Multi select""
             ItemsProvider=""LoadDropdownItems""
             Virtualize=""true""
             IsMultiSelect=""true""
             Placeholder=""Select options"" />";
    private readonly string example8CsharpCode = @"
private List<BitDropdownItem> LargeListOfCategoriesForSingleSelect = new ();
private List<BitDropdownItem> LargeListOfCategoriesForMultiSelect = new ();

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
}

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

    private readonly string example9HtmlCode = @"
<BitDropdown Label=""Single select dropdown with Rtl direction""
             Items=""GetArabicDropdownItems()""
             Placeholder=""حدد اختيارا""
             IsResponsiveModeEnabled=""true""
             IsRtl=""true""
             ShowSearchBox=""true""
             SearchBoxPlaceholder=""عناصر البحث"" />
<BitDropdown Label=""Multi select dropdown with Rtl direction""
             Items=""GetArabicDropdownItems()""
             Placeholder=""اشر على الخيارات""
             IsMultiSelect=""true""
             IsResponsiveModeEnabled=""true""
             IsRtl=""true""
             ShowSearchBox=""true""
             SearchBoxPlaceholder=""عناصر البحث"" />";
    private readonly string example9CsharpCode = @"
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

    private readonly string example10HtmlCode = @"
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
    private readonly string example10CsharpCode = @"
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

    private readonly string example11HtmlCode = @"
<BitDropdown @bind-Value=""SelectedValue""
             ShowClearButton=""true""
             Label=""Single select dropdown""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option"" />
<BitLabel>Value: @SelectedValue</BitLabel>

<BitDropdown @bind-Values=""SelectedValues""
             ShowClearButton=""true""
             Label=""Multi select dropdown""
             Items=""GetDropdownItems()""
             Placeholder=""Select options""
             IsMultiSelect=""true"" />
<BitLabel>Values: @string.Join(',', SelectedValues)</BitLabel>";
    private readonly string example11CsharpCode = @"
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

    private readonly string example12HtmlCode = @"
<EditForm Model=""formValidationDropdownModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <BitDropdown Label=""Select category""
                 Items=""GetCategoryDropdownItems()""
                 IsMultiSelect=""false""
                 @bind-Value=""formValidationDropdownModel.Category""
                 Placeholder=""Select an option"" />
    <ValidationMessage For=""@(() => formValidationDropdownModel.Category)"" />

    <BitDropdown Label=""Select two products""
                 Items=""GetProductDropdownItems()""
                 IsMultiSelect=""true""
                 @bind-Values=""formValidationDropdownModel.Products""
                 Placeholder=""Select an option"" />
    <ValidationMessage For=""@(() => formValidationDropdownModel.Products)"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example12CsharpCode = @"
public class FormValidationDropdownModel
{
    [MaxLength(2, ErrorMessage = ""The property {0} doesn't have more than {1} elements"")]
    [MinLength(1, ErrorMessage = ""The property {0} doesn't have less than {1} elements"")]
    public List<string> Products { get; set; } = new();

    [Required]
    public string Category { get; set; }
}

private FormValidationDropdownModel formValidationDropdownModel = new();

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
}
