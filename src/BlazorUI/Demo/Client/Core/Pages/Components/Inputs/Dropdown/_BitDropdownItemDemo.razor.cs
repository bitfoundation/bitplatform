namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class _BitDropdownItemDemo
{
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;

    private string ControlledValue = "Apple";
    private ICollection<string?> ControlledValues = new[] { "f-app", "f-ban" };
    private FormValidationDropdownModel formValidationDropdownModel = new();
    private string SuccessMessage = string.Empty;
    private List<BitDropdownItem<string>> Categories = new();
    private List<BitDropdownItem<string>> Products = new();
    private List<BitDropdownItem<string>> LargeListOfCategoriesForSingleSelect = new();
    private List<BitDropdownItem<string>> LargeListOfCategoriesForMultiSelect = new();
    private List<BitDropdownItem<string>> LargeListOfCategoriesDropDirection = new();
    private string? CurrentCategory;
    private string? SelectedValue;
    private ICollection<string?> SelectedValues = new List<string?>();


    protected override void OnInitialized()
    {
        Categories = Enumerable.Range(1, 6).Select(c => new BitDropdownItem<string>
        {
            ItemType = BitDropdownItemType.Normal,
            Value = c.ToString(),
            Text = $"Category {c}"
        }).ToList();

        Products = Enumerable.Range(1, 50).Select(p => new BitDropdownItem<string>
        {
            ItemType = BitDropdownItemType.Normal,
            Text = $"Product {p}",
            Value = $"{((int)Math.Ceiling((double)p % 7))}-{p}"
        }).ToList();

        LargeListOfCategoriesForSingleSelect = Enumerable.Range(1, 4000).Select(c => new BitDropdownItem<string>
        {
            ItemType = BitDropdownItemType.Normal,
            Value = c.ToString(),
            Text = $"Category {c}"
        }).ToList();

        LargeListOfCategoriesForMultiSelect = Enumerable.Range(1, 4000).Select(c => new BitDropdownItem<string>
        {
            ItemType = BitDropdownItemType.Normal,
            Value = c.ToString(),
            Text = $"Category {c}"
        }).ToList();

        LargeListOfCategoriesDropDirection = Enumerable.Range(1, 60).Select(c => new BitDropdownItem<string>
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

    private List<BitDropdownItem<string>> GetCategoryDropdownItems()
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

    private List<BitDropdownItem<string>> GetProductDropdownItems()
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

    private List<BitDropdownItem<string>> GetDropdownItems()
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

    private List<BitDropdownItem<string>> GetArabicDropdownItems()
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

    private List<BitDropdownItem<string>> GetCustomDropdownItems()
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

    private async ValueTask<BitDropdownItemsProviderResult<BitDropdownItem<string>>> LoadDropdownItems(BitDropdownItemsProviderRequest<BitDropdownItem<string>> request)
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
    }
}
