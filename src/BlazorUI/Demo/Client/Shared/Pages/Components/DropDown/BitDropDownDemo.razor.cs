using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.DropDown;

public partial class BitDropDownDemo
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
            Type = "List<BitDropDownItem>",
            DefaultValue = "",
            Description = "A list of items to display in the dropdown.",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitDropDownItem>",
            DefaultValue = "",
            Description = "Optional custom template for drop-down item.",
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
            Type = "EventCallback<BitDropDownItem> ",
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
            Type = "RenderFragment<BitDropDown>",
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
            Type = "RenderFragment<BitDropDown>",
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
            Type = "List<BitDropDownItem>",
            DefaultValue = "",
            Description = "The selected items for multiSelect scenarios.",
        },
        new()
        {
            Name = "SelectedItemsChanged",
            Type = "EventCallback<List<BitDropDownItem>>",
            DefaultValue = "",
            Description = "Callback for when the SelectedItems changed.",
        },
        new()
        {
            Name = "SelectedItem",
            Type = "BitDropDownItem",
            DefaultValue = "",
            Description = "The selected item for singleSelect scenarios.",
        },
        new()
        {
            Name = "SelectedItemChanged",
            Type = "EventCallback<BitDropDownItem>",
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
    private FormValidationDropDownModel formValidationDropDownModel = new();
    private string SuccessMessage = string.Empty;
    private List<BitDropDownItem> Categories = new();
    private List<BitDropDownItem> Products = new();
    private List<BitDropDownItem> LargeListOfCategoriesForSingleSelect = new();
    private List<BitDropDownItem> LargeListOfCategoriesForMultiSelect = new();
    private List<BitDropDownItem> LargeListOfCategoriesDropDirection = new();
    private string CurrentCategory = string.Empty;
    private string CurrentProduct = string.Empty;
    private string? SelectedValue;
    private List<string> SelectedValues = new();


    protected override void OnInitialized()
    {
        Categories = Enumerable.Range(1, 6).Select(c => new BitDropDownItem
        {
            ItemType = BitDropDownItemType.Normal,
            Value = c.ToString(),
            Text = $"Category {c}"
        }).ToList();

        Products = Enumerable.Range(1, 50).Select(p => new BitDropDownItem
        {
            ItemType = BitDropDownItemType.Normal,
            Text = $"Product {p}",
            Value = $"{((int)Math.Ceiling((double)p % 7))}-{p}"
        }).ToList();

        LargeListOfCategoriesForSingleSelect = Enumerable.Range(1, 4000).Select(c => new BitDropDownItem
        {
            ItemType = BitDropDownItemType.Normal,
            Value = c.ToString(),
            Text = $"Category {c}"
        }).ToList();

        LargeListOfCategoriesForMultiSelect = Enumerable.Range(1, 4000).Select(c => new BitDropDownItem
        {
            ItemType = BitDropDownItemType.Normal,
            Value = c.ToString(),
            Text = $"Category {c}"
        }).ToList();

        LargeListOfCategoriesDropDirection = Enumerable.Range(1, 60).Select(c => new BitDropDownItem
        {
            ItemType = BitDropDownItemType.Normal,
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

    private List<BitDropDownItem> GetCategoryDropdownItems()
    {
        return new()
        {
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Fruits",
                Value = "f"
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Vegetables",
                Value = "v"
            }
        };
    }

    private List<BitDropDownItem> GetProductDropdownItems()
    {
        return new()
        {
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "Fruits"
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Apple",
                Value = "f-app"
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Orange",
                Value = "f-ora",
                IsEnabled = false
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Banana",
                Value = "f-ban",
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Divider,
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "Vegetables"
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Broccoli",
                Value = "v-bro",
            }
        };
    }

    private List<BitDropDownItem> GetDropdownItems()
    {
        return new()
        {
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "Fruits"
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Apple",
                Value = "f-app"
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Orange",
                Value = "f-ora",
                IsEnabled = false
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Banana",
                Value = "f-ban",
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Divider,
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "Vegetables"
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Broccoli",
                Value = "v-bro",
            }
        };
    }

    private List<BitDropDownItem> GetArabicDropdownItems()
    {
        return new()
        {
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "الفاكهة"
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "تفاحة",
                Value = "f-app"
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "البرتقالي",
                Value = "f-ora",
                IsEnabled = false
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "موز",
                Value = "f-ban",
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Divider,
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "خضروات"
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "بروكلي",
                Value = "v-bro",
            }
        };
    }

    private List<BitDropDownItem> GetCustomDropdownItems()
    {
        return new List<BitDropDownItem>()
        {
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "Options",
                Value = "Header"
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option a",
                Value = "A",
                Data = new DropDownItemData()
                {
                    IconName = "Memo"
                }
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option b",
                Value = "B",
                Data = new DropDownItemData()
                {
                    IconName = "Print"
                }
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option c",
                Value = "C",
                Data = new DropDownItemData()
                {
                    IconName = "ShoppingCart"
                }
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option d",
                Value = "D",
                Data = new DropDownItemData()
                {
                    IconName = "Train"
                }
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option e",
                Value = "E",
                Data = new DropDownItemData()
                {
                    IconName = "Repair"
                }
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Divider
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "More options",
                Value = "Header2"
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option f",
                Value = "F",
                Data = new DropDownItemData()
                {
                    IconName = "Running"
                }
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option g",
                Value = "G",
                Data = new DropDownItemData()
                {
                    IconName = "EmojiNeutral"
                }
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option h",
                Value = "H",
                Data = new DropDownItemData()
                {
                    IconName = "ChatInviteFriend"
                }
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option i",
                Value = "I",
                Data = new DropDownItemData()
                {
                    IconName = "SecurityGroup"
                }
            },
            new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option j",
                Value = "J",
                Data = new DropDownItemData()
                {
                    IconName = "AddGroup"
                }
            }
        };
    }

    private async ValueTask<BitDropDownItemsProviderResult<BitDropDownItem>> LoadDropdownItems(BitDropDownItemsProviderRequest<BitDropDownItem> request)
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

            var items = data!.Items.Select(i => new BitDropDownItem
            {
                Text = i.Name,
                Value = i.Id.ToString(),
                Data = i,
                AriaLabel = i.Name,
                IsEnabled = true,
                ItemType = BitDropDownItemType.Normal
            }).ToList();

            return BitDropDownItemsProviderResult.From(items, data!.TotalCount);
        }
        catch
        {
            return BitDropDownItemsProviderResult.From(new List<BitDropDownItem>(), 0);
        }
    }



    private readonly string example1HTMLCode = @"<BitDropDown Label=""Basic Uncontrolled""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option""
             Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropDown>
<BitDropDown Label=""Disabled with defaultValue""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option""
             IsEnabled=""false""
             DefaultValue=""v-bro""
             Style=""width: 100%; max-width: 290px; margin-bottom: 20px;"">
</BitDropDown>
<BitDropDown Label=""Multi-select uncontrolled""
             Items=""GetDropdownItems()""
             Placeholder=""Select options""
             IsMultiSelect=""true""
             Style=""width: 100%; max-width: 290px; margin-bottom: 20px;"">
</BitDropDown>";
    private readonly string example1CSharpCode = @"
private List<BitDropDownItem> GetDropdownItems()
{
    return new()
    {
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""Fruits""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Apple"",
            Value = ""f-app""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Orange"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Banana"",
            Value = ""f-ban"",
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Divider,
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""Vegetables""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Broccoli"",
            Value = ""v-bro"",
        }
    };
}";

    private readonly string example2HTMLCode = @"<BitDropDown Label=""Single-select Controlled""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option""
             @bind-Value=""ControlledValue""
             Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropDown>";
    private readonly string example2CSharpCode = @"private string ControlledValue = ""Apple"";
private List<BitDropDownItem> GetDropdownItems()
{
    return new()
    {
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""Fruits""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Apple"",
            Value = ""f-app""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Orange"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Banana"",
            Value = ""f-ban"",
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Divider,
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""Vegetables""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Broccoli"",
            Value = ""v-bro"",
        }
    };
}";

    private readonly string example3HTMLCode = @"<BitDropDown Label=""Multi-select controlled""
             Items=""GetDropdownItems()""
             Placeholder=""Select options""
             @bind-Values=""ControlledValues""
             IsMultiSelect=""true""
             Style=""width: 100%; max-width: 290px; margin:20px 0 20px 0"">
</BitDropDown>";
    private readonly string example3CSharpCode = @"private List<string> ControlledValues = new() { ""Apple"", ""Banana"", ""Grape"" };
private List<BitDropDownItem> GetDropdownItems()
{
    return new()
    {
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""Fruits""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Apple"",
            Value = ""f-app""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Orange"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Banana"",
            Value = ""f-ban"",
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Divider,
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""Vegetables""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
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

<BitDropDown Label=""Custom Controlled""
             Items=""GetCustomDropdownItems()""
             Placeholder=""Select an option""
             AriaLabel=""Custom dropdown""
             Style=""width: 100%; max-width: 290px; margin:20px 0 20px 0"">
    <TextTemplate>
        <div style=""display: flex; gap: 10px; align-items: center;"">
            <i class=""bit-icon bit-icon--@((context.Items.Find(i => i.Value == context.Value).Data as DropDownItemData).IconName)""
               aria-hidden=""true""
               title=""@((context.Items.Find(i => i.Value == context.Value).Data as DropDownItemData).IconName)""></i>
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
            <i class=""bit-icon bit-icon--@((context.Data as DropDownItemData).IconName)""
               aria-hidden=""true""
               title=""@((context.Data as DropDownItemData).IconName)""></i>
            <span>@context.Text</span>
        </div>
    </ItemTemplate>
</BitDropDown>

<BitDropDown Items=""GetCustomDropdownItems()""
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
</BitDropDown>

<BitDropDown Items=""GetCustomDropdownItems()""
             Placeholder=""Select an option""
             Label=""Custom CaretDownIconName""
             AriaLabel=""Custom dropdown chevron icon with icon name""
             CaretDownIconName=""BitIconName.ScrollUpDown""
             Style=""width: 100%; max-width: 290px"">
</BitDropDown>";
    private readonly string example4CSharpCode = @"
private List<BitDropDownItem> GetCustomDropdownItems()
{
    return new List<BitDropDownItem>()
    {
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""Options"",
            Value = ""Header""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Option a"",
            Value = ""A"",
            Data = new DropDownItemData()
            {
                IconName = ""Memo""
            }
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Option b"",
            Value = ""B"",
            Data = new DropDownItemData()
            {
                IconName = ""Print""
            }
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Option c"",
            Value = ""C"",
            Data = new DropDownItemData()
            {
                IconName = ""ShoppingCart""
            }
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Option d"",
            Value = ""D"",
            Data = new DropDownItemData()
            {
                IconName = ""Train""
            }
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Option e"",
            Value = ""E"",
            Data = new DropDownItemData()
            {
                IconName = ""Repair""
            }
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Divider
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""More options"",
            Value = ""Header2""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Option f"",
            Value = ""F"",
            Data = new DropDownItemData()
            {
                IconName = ""Running""
            }
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Option g"",
            Value = ""G"",
            Data = new DropDownItemData()
            {
                IconName = ""EmojiNeutral""
            }
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Option h"",
            Value = ""H"",
            Data = new DropDownItemData()
            {
                IconName = ""ChatInviteFriend""
            }
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Option i"",
            Value = ""I"",
            Data = new DropDownItemData()
            {
                IconName = ""SecurityGroup""
            }
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Option j"",
            Value = ""J"",
            Data = new DropDownItemData()
            {
                IconName = ""AddGroup""
            }
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
        <BitDropDown Label=""Category""
                        Items=""Categories""
                        Placeholder=""Select options""
                        @bind-Value=""@CurrentCategory""
                        Style=""width: 100%; max-width: 290px; margin:20px 0 20px 0"">
        </BitDropDown>

        <BitDropDown Label=""Product""
                        Items=""@(Products.Where(p => p.Value.StartsWith($""{CurrentCategory}-"")).ToList())""
                        Placeholder=""Select options""
                        @bind-Value=""@CurrentProduct""
                        IsEnabled=""string.IsNullOrEmpty(CurrentCategory) is false""
                        Style=""width: 100%; max-width: 290px; margin:20px 0 20px 0"">
        </BitDropDown>
    </div>

    <div class=""cascading-dropdowns-info"">
        <h5>Current category: @(Categories.FirstOrDefault(c => c.Value == CurrentCategory)?.Text ?? ""-"")</h5>
        <h5>Current product: @(Products.FirstOrDefault(c => c.Value == CurrentProduct)?.Text ?? ""-"")</h5>
    </div>
</div>";
    private readonly string example5CSharpCode = @"private List<BitDropDownItem> Categories = new();
private List<BitDropDownItem> Products = new();
private string CurrentCategory;
private string CurrentProduct;

protected override void OnInitialized()
{
    Categories = Enumerable.Range(1, 6).Select(c => new BitDropDownItem
    {
        ItemType = BitDropDownItemType.Normal,
        Value = c.ToString(),
        Text = $""Category {c}""
    }).ToList();

    Products = Enumerable.Range(1, 50).Select(p => new BitDropDownItem
    {
        ItemType = BitDropDownItemType.Normal,
        Text = $""Product {p}"",
        Value = $""{((int)Math.Ceiling((double)p % 7))}-{p}""
    }).ToList();

    base.OnInitialized();
}";

    private readonly string example6HTMLCode = @"@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""formValidationDropDownModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <div class=""validation-summary"">
            <ValidationSummary />
        </div>

        <div>
            <BitDropDown Label=""Select category""
                        Items=""GetCategoryDropdownItems()""
                        IsMultiSelect=""false""
                        @bind-Value=""formValidationDropDownModel.Category""
                        Placeholder=""Select an option""
                        Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"" />

            <ValidationMessage For=""@(() => formValidationDropDownModel.Category)"" />
        </div>

        <div>
            <BitDropDown Label=""Select two ptoducts""
                        Items=""GetProductDropdownItems()""
                        IsMultiSelect=""true""
                        @bind-Values=""formValidationDropDownModel.Products""
                        Placeholder=""Select an option""
                        Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"" />

            <ValidationMessage For=""@(() => formValidationDropDownModel.Products)"" />
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
public class FormValidationDropDownModel
{
    [MaxLength(2, ErrorMessage = ""The property {0} doesn't have more than {1} elements"")]
    [MinLength(1, ErrorMessage = ""The property {0} doesn't have less than {1} elements"")]
    public List<string> Products { get; set; } = new();

    [Required]
    public string Category { get; set; }
}

private FormValidationDropDownModel formValidationDropDownModel = new();
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

private List<BitDropDownItem> GetCategoryDropdownItems()
{
    return new()
    {
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Fruits"",
            Value = ""f""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Vegetables"",
            Value = ""v""
        }
    };
}

private List<BitDropDownItem> GetProductDropdownItems()
{
    return new()
    {
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""Fruits""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Apple"",
            Value = ""f-app""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Orange"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Banana"",
            Value = ""f-ban"",
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Divider,
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""Vegetables""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Broccoli"",
            Value = ""v-bro"",
        }
    };
}";

    private readonly string example7HTMLCode = @"<BitDropDown Label=""Responsive DropDown""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option""
             IsResponsiveModeEnabled=true
             Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropDown>";
    private readonly string example7CSharpCode = @"
private List<BitDropDownItem> GetDropdownItems()
{
    return new()
    {
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""Fruits""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Apple"",
            Value = ""f-app""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Orange"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Banana"",
            Value = ""f-ban"",
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Divider,
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""Vegetables""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Broccoli"",
            Value = ""v-bro"",
        }
    };
}";

    private readonly string example8HTMLCode = @"<BitDropDown Label=""Single-select Controlled with search box""
                Items=""GetDropdownItems()""
                Placeholder=""Select an option""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search item""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropDown>

<BitDropDown Label=""Multi-select controlled with search box""
                Items=""GetDropdownItems()""
                Placeholder=""Select options""
                IsMultiSelect=""true""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search items""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropDown>";
    private readonly string example8CSharpCode = @"private string ControlledValue = ""Apple"";
private List<BitDropDownItem> GetDropdownItems()
{
    return new()
    {
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""Fruits""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Apple"",
            Value = ""f-app""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Orange"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Banana"",
            Value = ""f-ban"",
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Divider,
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""Vegetables""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Broccoli"",
            Value = ""v-bro"",
        }
    };
}";

    private readonly string example9HTMLCode = @"<BitDropDown Label=""Single-select Controlled with virtualization""
                Items=""LargeListOfCategoriesForSingleSelect""
                Virtualize=""true""
                Placeholder=""Select an option""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search item""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropDown>

<BitDropDown Label=""Multi-select controlled with virtualization""
                Items=""LargeListOfCategoriesForMultiSelect""
                Virtualize=""true""
                Placeholder=""Select options""
                IsMultiSelect=""true""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search items""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropDown>";
    private readonly string example9CSharpCode = @"private List<BitDropDownItem> LargeListOfCategories = new ();

protected override void OnInitialized()
{
    LargeListOfCategoriesForSingleSelect = Enumerable.Range(1, 4000).Select(c => new BitDropDownItem
    {
        ItemType = BitDropDownItemType.Normal,
        Value = c.ToString(),
        Text = $""Category {c}""
    }).ToList();

    LargeListOfCategoriesForMultiSelect = Enumerable.Range(1, 4000).Select(c => new BitDropDownItem
    {
        ItemType = BitDropDownItemType.Normal,
        Value = c.ToString(),
        Text = $""Category {c}""
    }).ToList();

    base.OnInitialized();
}";

    private readonly string example10HTMLCode = @"<BitDropDown Label=""Single-select Controlled with virtualization""
                ItemsProvider=""LoadDropdownItems""
                Virtualize=""true""
                Placeholder=""Select an option""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search item""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropDown>

<BitDropDown Label=""Multi-select controlled with virtualization""
                ItemsProvider=""LoadDropdownItems""
                Virtualize=""true""
                Placeholder=""Select options""
                IsMultiSelect=""true""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search items""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropDown>";
    private readonly string example10CSharpCode = @"private async ValueTask<BitDropDownItemsProviderResult<BitDropDownItem>> LoadDropdownItems(BitDropDownItemsProviderRequest<BitDropDownItem> request)
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

        var items = data!.Items.Select(i => new BitDropDownItem
        {
            Text = i.Name,
            Value = i.Id.ToString(),
            Data = i,
            AriaLabel = i.Name,
            IsEnabled = true,
            ItemType = BitDropDownItemType.Normal
        }).ToList();

        return BitDropDownItemsProviderResult.From(items, data!.TotalCount);
    }
    catch
    {
        return BitDropDownItemsProviderResult.From(new List<BitDropDownItem>(), 0);
    }
}";

    private readonly string example11HTMLCode = @"<BitDropDown Label=""Single-select with Rtl direction""
                Items=""GetArabicDropdownItems()""
                Placeholder=""حدد اختيارا""
                IsResponsiveModeEnabled=""true""
                IsRtl=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""عناصر البحث""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropDown>
<BitDropDown Label=""Multi-select with Rtl direction""
                Items=""GetArabicDropdownItems()""
                Placeholder=""اشر على الخيارات""
                IsMultiSelect=""true""
                IsResponsiveModeEnabled=""true""
                IsRtl=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""عناصر البحث""
                Style=""width: 100%; max-width: 290px; margin-bottom: 20px;"">
</BitDropDown>";
    private readonly string example11CSharpCode = @"private List<BitDropDownItem> GetArabicDropdownItems()
{
    return new()
    {
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""الفاكهة""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""تفاحة"",
            Value = ""f-app""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""البرتقالي"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""موز"",
            Value = ""f-ban"",
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Divider,
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""خضروات""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""بروكلي"",
            Value = ""v-bro"",
        }
    };
}";

    private readonly string example12HTMLCode = @"<BitDropDown Label=""Single-select Controlled with auto drop direction""
                Items=""LargeListOfCategoriesDropDirection""
                Virtualize=""true""
                Placeholder=""Select an option""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search item""
                DropDirection=""BitDropDirection.Auto""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropDown>

<BitDropDown Label=""Single-select Controlled with top and bottom drop direction""
                Items=""LargeListOfCategoriesDropDirection""
                Virtualize=""true""
                Placeholder=""Select an option""
                IsResponsiveModeEnabled=""true""
                ShowSearchBox=""true""
                SearchBoxPlaceholder=""Search item""
                DropDirection=""BitDropDirection.TopAndBottom""
                Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0"">
</BitDropDown>";
    private readonly string example12CSharpCode = @"private List<BitDropDownItem> LargeListOfCategoriesDropDirection = new();

protected override void OnInitialized()
{
    LargeListOfCategoriesDropDirection = Enumerable.Range(1, 60).Select(c => new BitDropDownItem
    {
        ItemType = BitDropDownItemType.Normal,
        Value = c.ToString(),
        Text = $""Category {c}""
    }).ToList();

    base.OnInitialized();
}";

    private readonly string example13HTMLCode = @"
<BitDropDown ShowClearButton=""true""
             Label=""Basic Uncontrolled""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option""
             Style=""width: 100%; max-width: 290px; margin: 20px 0 20px 0""
             @bind-Value=""SelectedValue"">
</BitDropDown>
<div>Value: @SelectedValue</div>
<br>
<hr />
<br>
<BitDropDown ShowClearButton=""true""
             Label=""Multi-select uncontrolled""
             Items=""GetDropdownItems()""
             Placeholder=""Select options""
             IsMultiSelect=""true""
             Style=""width: 100%; max-width: 290px; margin-bottom: 20px;""
             @bind-Values=""SelectedValues"">
</BitDropDown>
<div>Values: @string.Join(',', SelectedValues)</div>";
    private readonly string example13CSharpCode = @"
private string? SelectedValue;
private List<string> SelectedValues = new();

private List<BitDropDownItem> GetDropdownItems()
{
    return new()
    {
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""Fruits""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Apple"",
            Value = ""f-app""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Orange"",
            Value = ""f-ora"",
            IsEnabled = false
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Banana"",
            Value = ""f-ban"",
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Divider,
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = ""Vegetables""
        },
        new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = ""Broccoli"",
            Value = ""v-bro"",
        }
    };
}";
}
