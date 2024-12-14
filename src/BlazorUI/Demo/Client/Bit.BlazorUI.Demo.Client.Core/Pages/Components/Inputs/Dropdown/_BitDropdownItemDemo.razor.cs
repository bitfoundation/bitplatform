namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class _BitDropdownItemDemo
{
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;


    private List<BitDropdownItem<string>> GetBasicItems() =>
    [
        new() { ItemType = BitDropdownItemType.Header, Text = "Fruits" },
        new() { Text = "Apple", Value = "f-app" },
        new() { Text = "Banana", Value = "f-ban" },
        new() { Text = "Orange", Value = "f-ora", IsEnabled = false },
        new() { Text = "Grape", Value = "f-gra" },
        new() { ItemType = BitDropdownItemType.Divider },
        new() { ItemType = BitDropdownItemType.Header, Text = "Vegetables" },
        new() { Text = "Broccoli", Value = "v-bro" },
        new() { Text = "Carrot", Value = "v-car" },
        new() { Text = "Lettuce", Value = "v-let" }
    ];

    private List<BitDropdownItem<string>> GetDataItems() =>
    [
        new() { ItemType = BitDropdownItemType.Header, Text = "Items", Data = new DropdownItemData { IconName = "BulletedList2" } },
        new() { Text = "Item a", Value = "A", Data = new DropdownItemData { IconName = "Memo" } },
        new() { Text = "Item b", Value = "B", Data = new DropdownItemData { IconName = "Print" } },
        new() { Text = "Item c", Value = "C", Data = new DropdownItemData { IconName = "ShoppingCart" } },
        new() { ItemType = BitDropdownItemType.Divider },
        new() { ItemType = BitDropdownItemType.Header, Text = "More Items", Data = new DropdownItemData { IconName = "BulletedTreeList" } },
        new() { Text = "Item d", Value = "D", Data = new DropdownItemData { IconName = "Train" } },
        new() { Text = "Item e", Value = "E", Data = new DropdownItemData { IconName = "Repair" } },
        new() { Text = "Item f", Value = "F", Data = new DropdownItemData { IconName = "Running" } }
    ];

    private ICollection<BitDropdownItem<string>>? virtualizeItems1;
    private ICollection<BitDropdownItem<string>>? virtualizeItems2;

    private List<BitDropdownItem<string>> GetRtlItems() =>
    [
        new() { ItemType = BitDropdownItemType.Header, Text = "میوه ها" },
        new() { Text = "سیب", Value = "f-app" },
        new() { Text = "موز", Value = "f-ban" },
        new() { Text = "پرتقال", Value = "f-ora", IsEnabled = false },
        new() { Text = "انگور", Value = "f-gra" },
        new() { ItemType = BitDropdownItemType.Divider },
        new() { ItemType = BitDropdownItemType.Header, Text = "سیزیجات" },
        new() { Text = "کلم بروكلی", Value = "v-bro" },
        new() { Text = "هویج", Value = "v-car" },
        new() { Text = "کاهو", Value = "v-let" }
    ];

    private ICollection<BitDropdownItem<string>>? dropDirectionItems;

    private List<BitDropdownItem<string>> GetStyleClassItems() =>
    [
        new() { ItemType = BitDropdownItemType.Header, Text = "Fruits", Style = "text-align: center;" },
        new() { Text = "Apple", Value = "f-app", Class = "custom-fruit" },
        new() { Text = "Banana", Value = "f-ban", Class = "custom-fruit" },
        new() { Text = "Orange", Value = "f-ora", IsEnabled = false, Class = "custom-fruit" },
        new() { Text = "Grape", Value = "f-gra", Class = "custom-fruit" },
        new() { ItemType = BitDropdownItemType.Divider, Style = "padding: 0 0.25rem;" },
        new() { ItemType = BitDropdownItemType.Header, Text = "Vegetables", Style = "text-align: center;" },
        new() { Text = "Broccoli", Value = "v-bro", Class = "custom-veg" },
        new() { Text = "Carrot", Value = "v-car", Class = "custom-veg" },
        new() { Text = "Lettuce", Value = "v-let", Class = "custom-veg" }
    ];

    private List<BitDropdownItem<string>> comboBoxItems =
    [
        new() { ItemType = BitDropdownItemType.Header, Text = "Fruits" },
        new() { Text = "Apple", Value = "f-app" },
        new() { Text = "Banana", Value = "f-ban" },
        new() { Text = "Orange", Value = "f-ora", IsEnabled = false },
        new() { Text = "Grape", Value = "f-gra" },
        new() { ItemType = BitDropdownItemType.Divider },
        new() { ItemType = BitDropdownItemType.Header, Text = "Vegetables" },
        new() { Text = "Broccoli", Value = "v-bro" },
        new() { Text = "Carrot", Value = "v-car" },
        new() { Text = "Lettuce", Value = "v-let" }
    ];


    private string controlledValue = "f-app";
    private IEnumerable<string> controlledValues = ["f-app", "f-ban"];

    private string? changedValue;
    private IEnumerable<string> changedValues = [];

    private BitDropdownItem<string>? selectedItem1;
    private BitDropdownItem<string>? selectedItem2;

    private string? clearValue = "f-app";
    private IEnumerable<string?> clearValues = ["f-app", "f-ban"];

    private string successMessage = string.Empty;
    private FormValidationDropdownModel validationModel = new();

    private string comboBoxValueSample1 = default!;
    private string comboBoxValueSample2 = default!;
    private string comboBoxValueSample3 = default!;
    private string comboBoxValueSample4 = default!;
    private IEnumerable<string> comboBoxValues1 = [];
    private IEnumerable<string> comboBoxValues2 = [];
    private IEnumerable<string> comboBoxValues3 = [];

    private IEnumerable<BitDropdownItem<string>> initialSelectedItem = [
        new()
        {
            Text = "Product 100",
            Value = "100",
            Data = new ProductDto {
                Id = 100,
                Price = 60,
                Name = "Product 100"
            },
            AriaLabel = "Product 100",
            IsEnabled = true,
            ItemType = BitDropdownItemType.Normal
        }
    ];

    private IEnumerable<BitDropdownItem<string>> initialSelectedItems = [
        new()
        {
            Text = "Product 100",
            Value = "100",
            Data = new ProductDto {
                Id = 100,
                Price = 60,
                Name = "Product 100"
            },
            AriaLabel = "Product 100",
            IsEnabled = true,
            ItemType = BitDropdownItemType.Normal
        },
        new()
        {
            Text = "Product 99",
            Value = "99",
            Data = new ProductDto {
                Id = 99,
                Price = 75,
                Name = "Product 99"
            },
            AriaLabel = "Product 99",
            IsEnabled = true,
            ItemType = BitDropdownItemType.Normal
        }
    ];

    protected override void OnInitialized()
    {
        virtualizeItems1 = Enumerable.Range(1, 10_000)
                                     .Select(c => new BitDropdownItem<string> { Text = $"Category {c}", Value = c.ToString() })
                                     .ToArray();

        virtualizeItems2 = Enumerable.Range(1, 10_000)
                                     .Select(c => new BitDropdownItem<string> { Text = $"Category {c}", Value = c.ToString() })
                                     .ToArray();

        dropDirectionItems = Enumerable.Range(1, 15)
                                       .Select(c => new BitDropdownItem<string> { Value = c.ToString(), Text = $"Category {c}" })
                                       .ToArray();

        base.OnInitialized();
    }


    private async Task HandleValidSubmit()
    {
        successMessage = "Form Submitted Successfully!";
        await Task.Delay(3000);
        successMessage = string.Empty;
        validationModel = new();
        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        successMessage = string.Empty;
    }

    private void HandleOnDynamicAdd(BitDropdownItem<string> item)
    {
        comboBoxItems.Add(item);
    }

    private async ValueTask<BitDropdownItemsProviderResult<BitDropdownItem<string>>> LoadItems(
        BitDropdownItemsProviderRequest<BitDropdownItem<string>> request)
    {
        try
        {
            // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview

            var query = new Dictionary<string, object?>()
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

            var items = data!.Items!.Select(i => new BitDropdownItem<string>
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
