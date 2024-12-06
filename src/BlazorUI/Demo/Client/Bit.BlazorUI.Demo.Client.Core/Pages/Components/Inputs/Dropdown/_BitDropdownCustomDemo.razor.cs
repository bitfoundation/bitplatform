namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class _BitDropdownCustomDemo
{
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;


    private BitDropdownNameSelectors<Product, string> nameSelectors = new()
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
        TextSetter = (string text, Product item) => item.Text = text
    };


    private List<Product> GetBasicCustoms() =>
    [
        new() { Text = "Fruits", Type = BitDropdownItemType.Header },
        new() { Text = "Apple", Value = "f-app" },
        new() { Text = "Banana", Value = "f-ban" },
        new() { Text = "Orange", Value = "f-ora", Disabled = true },
        new() { Text = "Grape", Value = "f-gra" },
        new() { Type = BitDropdownItemType.Divider },
        new() { Text = "Vegetables", Type = BitDropdownItemType.Header },
        new() { Text = "Broccoli", Value = "v-bro" },
        new() { Text = "Carrot", Value = "v-car" },
        new() { Text = "Lettuce", Value = "v-let" }
    ];

    private List<Product> GetDataCustoms() =>
    [
        new() { Type = BitDropdownItemType.Header, Text = "Items", Payload = new DropdownItemData { IconName = "BulletedList2" } },
        new() { Text = "Item a", Value = "A", Payload = new DropdownItemData { IconName = "Memo" } },
        new() { Text = "Item b", Value = "B", Payload = new DropdownItemData { IconName = "Print" } },
        new() { Text = "Item c", Value = "C", Payload = new DropdownItemData { IconName = "ShoppingCart" } },
        new() { Type = BitDropdownItemType.Divider },
        new() { Type = BitDropdownItemType.Header, Text = "More Items", Payload = new DropdownItemData { IconName = "BulletedTreeList" } },
        new() { Text = "Item d", Value = "D", Payload = new DropdownItemData { IconName = "Train" } },
        new() { Text = "Item e", Value = "E", Payload = new DropdownItemData { IconName = "Repair" } },
        new() { Text = "Item f", Value = "F", Payload = new DropdownItemData { IconName = "Running" } }
    ];

    private ICollection<Product>? virtualizeCustoms1;
    private ICollection<Product>? virtualizeCustoms2;

    private List<Product> GetRtlCustoms() =>
    [
        new() { Type = BitDropdownItemType.Header, Text = "میوه ها" },
        new() { Text = "سیب", Value = "f-app" },
        new() { Text = "موز", Value = "f-ban" },
        new() { Text = "پرتقال", Value = "f-ora", Disabled = true },
        new() { Text = "انگور", Value = "f-gra" },
        new() { Type = BitDropdownItemType.Divider },
        new() { Type = BitDropdownItemType.Header, Text = "سیزیجات" },
        new() { Text = "کلم بروكلی", Value = "v-bro" },
        new() { Text = "هویج", Value = "v-car" },
        new() { Text = "کاهو", Value = "v-let" }
    ];
    private ICollection<Product>? dropDirectionCustoms;
    private List<Product> GetStyleClassCustoms() => new()
    {
        new() { Type = BitDropdownItemType.Header, Text = "Fruits", CssStyle = "text-align: center;" },
        new() { Text = "Apple", Value = "f-app", CssClass = "custom-fruit" },
        new() { Text = "Banana", Value = "f-ban", CssClass = "custom-fruit" },
        new() { Text = "Orange", Value = "f-ora", Disabled = true, CssClass = "custom-fruit" },
        new() { Text = "Grape", Value = "f-gra", CssClass = "custom-fruit" },
        new() { Type = BitDropdownItemType.Divider, CssStyle = "padding: 0 0.25rem;" },
        new() { Type = BitDropdownItemType.Header, Text = "Vegetables", CssStyle = "text-align: center;" },
        new() { Text = "Broccoli", Value = "v-bro", CssClass = "custom-veg" },
        new() { Text = "Carrot", Value = "v-car", CssClass = "custom-veg" },
        new() { Text = "Lettuce", Value = "v-let", CssClass = "custom-veg" }
    };
    private List<Product> comboBoxCustoms = new()
    {
        new() { Text = "Fruits", Type = BitDropdownItemType.Header },
        new() { Text = "Apple", Value = "f-app" },
        new() { Text = "Banana", Value = "f-ban" },
        new() { Text = "Orange", Value = "f-ora", Disabled = true },
        new() { Text = "Grape", Value = "f-gra" },
        new() { Type = BitDropdownItemType.Divider },
        new() { Text = "Vegetables", Type = BitDropdownItemType.Header },
        new() { Text = "Broccoli", Value = "v-bro" },
        new() { Text = "Carrot", Value = "v-car" },
        new() { Text = "Lettuce", Value = "v-let" }
    };



    private string controlledValue = "f-app";
    private IEnumerable<string> controlledValues = ["f-app", "f-ban"];

    private string? changedValue;
    private IEnumerable<string> changedValues = [];

    private Product? selectedItem1;
    private Product? selectedItem2;

    private string clearValue = "f-app";
    private IEnumerable<string> clearValues = ["f-app", "f-ban"];

    private string successMessage = string.Empty;
    private FormValidationDropdownModel validationModel = new();

    private string comboBoxValueSample1 = default!;
    private string comboBoxValueSample2 = default!;
    private string comboBoxValueSample3 = default!;
    private string comboBoxValueSample4 = default!;
    private IEnumerable<string> comboBoxValues1 = [];
    private IEnumerable<string> comboBoxValues2 = [];
    private IEnumerable<string> comboBoxValues3 = [];

    private IEnumerable<Product> initialSelectedItem = [
        new()
        {
            Text = "Product 100",
            Value = "100",
            Payload = new ProductDto {
                Id = 100,
                Price = 60,
                Name = "Product 100"
            },
            Label = "Product 100",
            Type = BitDropdownItemType.Normal
        }
    ];

    private IEnumerable<Product> initialSelectedItems = [
        new()
        {
            Text = "Product 100",
            Value = "100",
            Payload = new ProductDto {
                Id = 100,
                Price = 60,
                Name = "Product 100"
            },
            Label = "Product 100",
            Type = BitDropdownItemType.Normal
        },
        new()
        {
            Text = "Product 99",
            Value = "99",
            Payload = new ProductDto {
                Id = 99,
                Price = 75,
                Name = "Product 99"
            },
            Label = "Product 99",
            Type = BitDropdownItemType.Normal
        }
    ];

    protected override void OnInitialized()
    {
        virtualizeCustoms1 = Enumerable.Range(1, 10_000)
                                       .Select(p => new Product { Text = $"Produce {p}", Value = p.ToString() })
                                       .ToArray();

        virtualizeCustoms2 = Enumerable.Range(1, 10_000)
                                       .Select(p => new Product { Text = $"Produce {p}", Value = p.ToString() })
                                       .ToArray();

        dropDirectionCustoms = Enumerable.Range(1, 15)
                                         .Select(p => new Product { Text = $"Produce {p}", Value = p.ToString() })
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

    private void HandleOnDynamicAdd(Product item)
    {
        comboBoxCustoms.Add(item);
    }

    private async ValueTask<BitDropdownItemsProviderResult<Product>> LoadItems(
        BitDropdownItemsProviderRequest<Product> request)
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

            var items = data!.Items!.Select(i => new Product
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
    }
}
