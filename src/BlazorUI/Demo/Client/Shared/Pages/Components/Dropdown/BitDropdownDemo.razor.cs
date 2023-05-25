using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.Dropdown;

public partial class BitDropdownDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "CaretDownTemplate",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "Optional custom template for chevron icon.",
        },
        new()
        {
            Name = "CaretDownIconName",
            Type = "BitIconName",
            DefaultValue = "BitIconName.ChevronDown",
            Description = "Optional chevron icon.",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "string",
            DefaultValue = "",
            Description = "Key that will be initially used to set selected item.",
        },
        new()
        {
            Name = "DefaultValues",
            Type = "List<string>",
            DefaultValue = "",
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
            Type = "List<BitDropdownItem>",
            DefaultValue = "",
            Description = "A list of items to display in the dropdown.",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitDropdownItem>",
            DefaultValue = "",
            Description = "Optional custom template for dropdown item.",
        },
        new()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "",
            Description = "The title to show when the mouse is placed on the drop down.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "Optional custom template for label.",
        },
        new()
        {
            Name = "MultiSelectDelimiter",
            Type = "string",
            DefaultValue = "",
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
            DefaultValue = "",
            Description = "Callback for when the action button clicked.",
        },
        new()
        {
            Name = "OnSelectItem",
            Type = "EventCallback<BitDropdownItem> ",
            DefaultValue = "",
            Description = "Callback for when an item is selected.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string",
            DefaultValue = "",
            Description = "Input placeholder Text, Displayed until an option is selected.",
        },
        new()
        {
            Name = "PlaceholderTemplate",
            Type = "RenderFragment<BitDropdown>",
            DefaultValue = "",
            Description = "Optional custom template for placeholder Text.",
        },
        new()
        {
            Name = "Values",
            Type = "List<string>",
            DefaultValue = "",
            Description = "Keys of the selected items for multiSelect scenarios. If you provide this, you must maintain selection state by observing onChange events and passing a new value in when changed.",
        },
        new()
        {
            Name = "ValuesChanged",
            Type = "EventCallback<List<string>>",
            DefaultValue = "",
            Description = "Callback for when the values changed.",
        },
        new()
        {
            Name = "TextTemplate",
            Type = "RenderFragment<BitDropdown>",
            DefaultValue = "",
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
            Name = "SearchBoxPlaceholder",
            Type = "string",
            DefaultValue = "",
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
            Name = "OverscanCount",
            Type = "int",
            DefaultValue = "3",
            Description = "determines how many additional items are rendered before and after the visible region.",
        },
        new()
        {
            Name = "SelectedItems",
            Type = "List<BitDropdownItem>",
            DefaultValue = "",
            Description = "The selected items for multiSelect scenarios.",
        },
        new()
        {
            Name = "SelectedItemsChanged",
            Type = "EventCallback<List<BitDropdownItem>>",
            DefaultValue = "",
            Description = "Callback for when the SelectedItems changed.",
        },
        new()
        {
            Name = "SelectedItem",
            Type = "BitDropdownItem",
            DefaultValue = "",
            Description = "The selected item for singleSelect scenarios.",
        },
        new()
        {
            Name = "SelectedItemChanged",
            Type = "EventCallback<BitDropdownItem>",
            DefaultValue = "",
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
            Description = "Darpdown opening direction.",
        },
    };



    private string ControlledValue = "Apple";
    private List<string> ControlledValues = new() { "Apple", "Banana", "Grape" };
    private FormValidationDropdownModel formValidationDropdownModel = new();
    private string SuccessMessage = string.Empty;
    private List<BitDropdownItem> Categories = new();
    private List<BitDropdownItem> Products = new();
    private List<BitDropdownItem> LargeListOfCategoriesForSingleSelect = new();
    private List<BitDropdownItem> LargeListOfCategoriesForMultiSelect = new();
    private List<BitDropdownItem> LargeListOfCategoriesDropDirection = new();
    private string CurrentCategory = string.Empty;
    private string CurrentProduct = string.Empty;
    private string? SelectedValue;
    private List<string> SelectedValues = new();


    protected override void OnInitialized()
    {
        Categories = Enumerable.Range(1, 6).Select(c => new BitDropdownItem
        {
            ItemType = BitDropdownItemType.Normal,
            Value = c.ToString(),
            Text = $"Category {c}"
        }).ToList();

        Products = Enumerable.Range(1, 50).Select(p => new BitDropdownItem
        {
            ItemType = BitDropdownItemType.Normal,
            Text = $"Product {p}",
            Value = $"{((int)Math.Ceiling((double)p % 7))}-{p}"
        }).ToList();

        LargeListOfCategoriesForSingleSelect = Enumerable.Range(1, 4000).Select(c => new BitDropdownItem
        {
            ItemType = BitDropdownItemType.Normal,
            Value = c.ToString(),
            Text = $"Category {c}"
        }).ToList();

        LargeListOfCategoriesForMultiSelect = Enumerable.Range(1, 4000).Select(c => new BitDropdownItem
        {
            ItemType = BitDropdownItemType.Normal,
            Value = c.ToString(),
            Text = $"Category {c}"
        }).ToList();

        LargeListOfCategoriesDropDirection = Enumerable.Range(1, 60).Select(c => new BitDropdownItem
        {
            ItemType = BitDropdownItemType.Normal,
            Value = c.ToString(),
            Text = $"Category {c}"
        }).ToList();

        base.OnInitialized();
    }


    private async Task HandleValidSubmit()
    {
        SuccessMessage = "Form Submitted Successfully!";
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
                Text = "Fruits",
                Value = "f"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Vegetables",
                Value = "v"
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
                Text = "Fruits"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Apple",
                Value = "f-app"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Orange",
                Value = "f-ora",
                IsEnabled = false
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Banana",
                Value = "f-ban",
            },
            new()
            {
                ItemType = BitDropdownItemType.Divider,
            },
            new()
            {
                ItemType = BitDropdownItemType.Header,
                Text = "Vegetables"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Broccoli",
                Value = "v-bro",
            }
        };
    }

    private List<BitDropdownItem> GetDropdownItems()
    {
        return new()
        {
            new()
            {
                ItemType = BitDropdownItemType.Header,
                Text = "Fruits"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Apple",
                Value = "f-app"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Banana",
                Value = "f-ban"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Orange",
                Value = "f-ora",
                IsEnabled = false
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Grape",
                Value = "f-gra",
            },
            new()
            {
                ItemType = BitDropdownItemType.Divider,
            },
            new()
            {
                ItemType = BitDropdownItemType.Header,
                Text = "Vegetables"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Broccoli",
                Value = "v-bro",
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Carrot",
                Value = "v-car",
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Lettuce",
                Value = "v-let",
            }
        };
    }

    private List<BitDropdownItem> GetArabicDropdownItems()
    {
        return new()
        {
            new()
            {
                ItemType = BitDropdownItemType.Header,
                Text = "الفاكهة"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "تفاحة",
                Value = "f-app"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "البرتقالي",
                Value = "f-ora",
                IsEnabled = false
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "موز",
                Value = "f-ban",
            },
            new()
            {
                ItemType = BitDropdownItemType.Divider,
            },
            new()
            {
                ItemType = BitDropdownItemType.Header,
                Text = "خضروات"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "بروكلي",
                Value = "v-bro",
            }
        };
    }

    private List<BitDropdownItem> GetCustomDropdownItems()
    {
        return new()
        {
            new()
            {
                ItemType = BitDropdownItemType.Header,
                Text = "Options",
                Value = "Header"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Option a",
                Value = "A",
                Data = new DropdownItemData { IconName = "Memo" }
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Option b",
                Value = "B",
                Data = new DropdownItemData { IconName = "Print" }
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Option c",
                Value = "C",
                Data = new DropdownItemData { IconName = "ShoppingCart" }
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Option d",
                Value = "D",
                Data = new DropdownItemData { IconName = "Train" }
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Option e",
                Value = "E",
                Data = new DropdownItemData { IconName = "Repair" }
            },
            new()
            {
                ItemType = BitDropdownItemType.Divider
            },
            new()
            {
                ItemType = BitDropdownItemType.Header,
                Text = "More options",
                Value = "Header2"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Option f",
                Value = "F",
                Data = new DropdownItemData { IconName = "Running" }
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Option g",
                Value = "G",
                Data = new DropdownItemData { IconName = "EmojiNeutral" }
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Option h",
                Value = "H",
                Data = new DropdownItemData { IconName = "ChatInviteFriend" }
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Option i",
                Value = "I",
                Data = new DropdownItemData { IconName = "SecurityGroup" }
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Option j",
                Value = "J",
                Data = new DropdownItemData { IconName = "AddGroup" }
            }
        };
    }

    private async ValueTask<BitDropdownItemsProviderResult<BitDropdownItem>> LoadDropdownItems(BitDropdownItemsProviderRequest<BitDropdownItem> request)
    {
        try
        {
            // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview

            var query = new Dictionary<string, object>()
            {
                { "$top", request.Count == 0 ? 50 : request.Count },
                { "$skip", request.StartIndex }
            };

            if (string.IsNullOrEmpty(request.Search) is false)
            {
                query.Add("$filter", $"contains(Name,'{request.Search}')");
            }

            var url = NavManager.GetUriWithQueryParameters("Products/GetProducts", query);

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
    }



    private readonly string example1HTMLCode = @"
<BitDropdown Label=""Basic Uncontrolled""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option""
             Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropdown>

<BitDropdown Label=""Disabled with defaultValue""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option""
             IsEnabled=""false""
             DefaultValue=""v-bro""
             Style=""width: 100%; max-width: 290px; margin-bottom: 20px;"">
</BitDropdown>

<BitDropdown Label=""Multi-select uncontrolled""
             Items=""GetDropdownItems()""
             Placeholder=""Select options""
             IsMultiSelect=""true""
             Style=""width: 100%; max-width: 290px; margin-bottom: 20px;"">
</BitDropdown>";
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
             @bind-Value=""ControlledValue""
             Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropdown>";
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
             IsMultiSelect=""true""
             Style=""width: 100%; max-width: 290px; margin:20px 0 20px 0"">
</BitDropdown>";
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
    .custom-drp-lbl-ic {
        width: 32px;
        height: 32px;
        outline: transparent;
        font-size: 14px;
        font-weight: 400;
        box-sizing: border-box;
        border: none;
        display: inline-block;
        text-decoration: none;
        text-align: center;
        cursor: pointer;
        padding: 0 4px;
        border-radius: 2px;
        background-color: transparent;
        color: #0078d4;
        margin-bottom: -8px;
        user-select: none;
    }

    .custom-drp-lbl-ic:hover {
        color: #106EBE;
        background-color: #f3f2f1;
    }
</style>

<BitDropdown Label=""Custom Controlled""
             Items=""GetCustomDropdownItems()""
             Placeholder=""Select an option""
             AriaLabel=""Custom dropdown""
             Style=""width: 100%; max-width: 290px; margin:20px 0 20px 0"">
    <TextTemplate>
        <div style=""display: flex; gap: 10px; align-items: center;"">
            <i class=""bit-icon bit-icon--@((context.Items.Find(i => i.Value == context.Value).Data as DropdownItemData).IconName)""
               aria-hidden=""true""
               title=""@((context.Items.Find(i => i.Value == context.Value).Data as DropdownItemData).IconName)""></i>
            <span>@context.Items.Find(i => i.Value == context.Value).Text</span>
        </div>
    </TextTemplate>
    <PlaceholderTemplate>
        <div style=""display: flex; gap: 10px; align-items: center;"">
            <i class=""bit-icon bit-icon--MessageFill"" aria-hidden=""true""></i>
            <span>@context.Placeholder</span>
        </div>
    </PlaceholderTemplate>
    <CaretDownTemplate>
        <i class=""bit-icon bit-icon--CirclePlus""></i>
    </CaretDownTemplate>
    <ItemTemplate>
        <div style=""display: flex; flex-flow: row nowrap; justify-content: flex-start; align-items: center; gap: 10px;"">
            <i class=""bit-icon bit-icon--@((context.Data as DropdownItemData).IconName)""
               aria-hidden=""true""
               title=""@((context.Data as DropdownItemData).IconName)""></i>
            <span>@context.Text</span>
        </div>
    </ItemTemplate>
</BitDropdown>

<BitDropdown Items=""GetCustomDropdownItems()""
             Placeholder=""Select an option""
             Label=""Custom Label""
             AriaLabel=""Custom dropdown label""
             Style=""width: 100%; max-width: 290px; margin:0 0 20px 0"">
    <LabelTemplate>
        <label>Custom label</label>
        <button type=""button"" title=""Info"" aria-label=""Info"" class=""custom-drp-lbl-ic"">
            <i class=""bit-icon bit-icon--Info""></i>
        </button>
    </LabelTemplate>
</BitDropdown>

<BitDropdown Items=""GetCustomDropdownItems()""
             Placeholder=""Select an option""
             Label=""Custom CaretDownIconName""
             AriaLabel=""Custom dropdown chevron icon with icon name""
             CaretDownIconName=""BitIconName.ScrollUpDown""
             Style=""width: 100%; max-width: 290px"">
</BitDropdown>";
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
<style>
    .grid-wrap {
        display: grid;
        grid-template-columns: auto auto;
    }

    .grid-wrap .cascading-dropdowns-info {
        padding-top: 1.875rem;
    }

    @media screen and (max-width: 37.5em) {
        .grid-wrap {
            display: block;
            grid-template-columns: none;
        }

        .grid-wrap .cascading-dropdowns-info {
            padding-top: 0;
        }
    }
</style>

<div class=""grid-wrap"">
    <div>
        <BitDropdown Label=""Category""
                        Items=""Categories""
                        Placeholder=""Select options""
                        @bind-Value=""@CurrentCategory""
                        Style=""width: 100%; max-width: 290px; margin:20px 0 20px 0"">
        </BitDropdown>

        <BitDropdown Label=""Product""
                        Items=""@(Products.Where(p => p.Value.StartsWith($""{CurrentCategory}-"")).ToList())""
                        Placeholder=""Select options""
                        @bind-Value=""@CurrentProduct""
                        IsEnabled=""string.IsNullOrEmpty(CurrentCategory) is false""
                        Style=""width: 100%; max-width: 290px; margin:20px 0 20px 0"">
        </BitDropdown>
    </div>

    <div class=""cascading-dropdowns-info"">
        <h5>Current category: @(Categories.FirstOrDefault(c => c.Value == CurrentCategory)?.Text ?? ""-"")</h5>
        <h5>Current product: @(Products.FirstOrDefault(c => c.Value == CurrentProduct)?.Text ?? ""-"")</h5>
    </div>
</div>";
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

        <div>
            <BitDropdown Label=""Select category""
                        Items=""GetCategoryDropdownItems()""
                        IsMultiSelect=""false""
                        @bind-Value=""formValidationDropdownModel.Category""
                        Placeholder=""Select an option""
                        Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"" />

            <ValidationMessage For=""@(() => formValidationDropdownModel.Category)"" />
        </div>

        <div>
            <BitDropdown Label=""Select two ptoducts""
                        Items=""GetProductDropdownItems()""
                        IsMultiSelect=""true""
                        @bind-Values=""formValidationDropdownModel.Products""
                        Placeholder=""Select an option""
                        Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"" />

            <ValidationMessage For=""@(() => formValidationDropdownModel.Products)"" />
        </div>

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
             IsResponsiveModeEnabled=true
             Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropdown>";
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
<BitDropdown Label=""Single-select Controlled with search box""
                Items=""GetDropdownItems()""
                Placeholder=""Select an option""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search item""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropdown>

<BitDropdown Label=""Multi-select controlled with search box""
                Items=""GetDropdownItems()""
                Placeholder=""Select options""
                IsMultiSelect=""true""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search items""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropdown>";
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
<BitDropdown Label=""Single-select Controlled with virtualization""
                Items=""LargeListOfCategoriesForSingleSelect""
                Virtualize=""true""
                Placeholder=""Select an option""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search item""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropdown>

<BitDropdown Label=""Multi-select controlled with virtualization""
                Items=""LargeListOfCategoriesForMultiSelect""
                Virtualize=""true""
                Placeholder=""Select options""
                IsMultiSelect=""true""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search items""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropdown>";
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
<BitDropdown Label=""Single-select Controlled with virtualization""
                ItemsProvider=""LoadDropdownItems""
                Virtualize=""true""
                Placeholder=""Select an option""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search item""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropdown>

<BitDropdown Label=""Multi-select controlled with virtualization""
                ItemsProvider=""LoadDropdownItems""
                Virtualize=""true""
                Placeholder=""Select options""
                IsMultiSelect=""true""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search items""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropdown>";
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
<BitDropdown Label=""Single-select with Rtl direction""
                Items=""GetArabicDropdownItems()""
                Placeholder=""حدد اختيارا""
                IsResponsiveModeEnabled=""true""
                IsRtl=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""عناصر البحث""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropdown>
<BitDropdown Label=""Multi-select with Rtl direction""
                Items=""GetArabicDropdownItems()""
                Placeholder=""اشر على الخيارات""
                IsMultiSelect=""true""
                IsResponsiveModeEnabled=""true""
                IsRtl=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""عناصر البحث""
                Style=""width: 100%; max-width: 290px; margin-bottom: 20px;"">
</BitDropdown>";
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
<BitDropdown Label=""Single-select Controlled with auto drop direction""
             Items=""LargeListOfCategoriesDropDirection""
             Virtualize=""true""
             Placeholder=""Select an option""
             IsResponsiveModeEnabled=""true""
             ShowSearchBox=""true""
             SearchBoxPlaceholder=""Search item""
             DropDirection=""BitDropDirection.Auto""
             Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropdown>

<BitDropdown Label=""Single-select Controlled with top and bottom drop direction""
             Items=""LargeListOfCategoriesDropDirection""
             Virtualize=""true""
             Placeholder=""Select an option""
             IsResponsiveModeEnabled=""true""
             ShowSearchBox=""true""
             SearchBoxPlaceholder=""Search item""
             DropDirection=""BitDropDirection.TopAndBottom""
             Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropdown>";
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
<BitDropdown ShowClearButton=""true""
             Label=""Basic Uncontrolled""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option""
             Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0""
             @bind-Value=""SelectedValue"">
</BitDropdown>
<div>Value: @SelectedValue</div>
<br>
<hr />
<br>
<BitDropdown ShowClearButton=""true""
             Label=""Multi-select uncontrolled""
             Items=""GetDropdownItems()""
             Placeholder=""Select options""
             IsMultiSelect=""true""
             Style=""width: 100%; max-width: 290px; margin-bottom: 20px;""
             @bind-Values=""SelectedValues"">
</BitDropdown>
<div>Values: @string.Join(',', SelectedValues)</div>";
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
