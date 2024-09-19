namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class _BitDropdownCustomDemo
{
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;


    private BitDropdownNameSelectors<ProduceModel, string> nameSelectors = new()
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

    private BitDropdownNameSelectors<ProduceModel, string> comboBoxNameSelectors = new()
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
        ValueSetter = (ProduceModel item, string value) => item.Value = value,
        TextSetter = (string text, ProduceModel item) => item.Text = text
    };


    private List<ProduceModel> GetBasicCustoms() =>
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

    private List<ProduceModel> GetDataCustoms() =>
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

    private ICollection<ProduceModel>? virtualizeCustoms1;
    private ICollection<ProduceModel>? virtualizeCustoms2;

    private List<ProduceModel> GetRtlCustoms() =>
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
    private ICollection<ProduceModel>? dropDirectionCustoms;
    private List<ProduceModel> GetStyleClassCustoms() => new()
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
    private List<ProduceModel> comboBoxCustoms = new()
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
    private ICollection<string> controlledValues = ["f-app", "f-ban"];

    private ProduceModel? changedItem;
    private ProduceModel[] changedItems = Array.Empty<ProduceModel>();

    private ProduceModel? selectedItem1;
    private ProduceModel? selectedItem2;

    private string clearValue = "f-app";
    private ICollection<string> clearValues = ["f-app", "f-ban"];

    private string successMessage = string.Empty;
    private FormValidationDropdownModel validationModel = new();

    private string comboBoxValueSample1 = default!;
    private string comboBoxValueSample2 = default!;
    private string comboBoxValueSample3 = default!;
    private string comboBoxValueSample4 = default!;
    private ICollection<string> comboBoxValues1 = [];
    private ICollection<string> comboBoxValues2 = [];
    private ICollection<string> comboBoxValues3 = [];

    protected override void OnInitialized()
    {
        virtualizeCustoms1 = Enumerable.Range(1, 10_000)
                                       .Select(p => new ProduceModel { Text = $"Produce {p}", Value = p.ToString() })
                                       .ToArray();

        virtualizeCustoms2 = Enumerable.Range(1, 10_000)
                                       .Select(p => new ProduceModel { Text = $"Produce {p}", Value = p.ToString() })
                                       .ToArray();

        dropDirectionCustoms = Enumerable.Range(1, 15)
                                         .Select(p => new ProduceModel { Text = $"Produce {p}", Value = p.ToString() })
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

    private void HandleOnDynamicAdd(ProduceModel item)
    {
        comboBoxCustoms.Add(item);
    }

    private async ValueTask<BitDropdownItemsProviderResult<ProduceModel>> LoadItems(
        BitDropdownItemsProviderRequest<ProduceModel> request)
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

            var items = data!.Items!.Select(i => new ProduceModel
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
            return BitDropdownItemsProviderResult.From(new List<ProduceModel>(), 0);
        }
    }
}
