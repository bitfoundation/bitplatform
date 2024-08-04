namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class _BitDropdownCustomDemo
{
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;


    private BitDropdownNameSelectors<BitDropdownCustom, string> nameSelectors = new()
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

    private BitDropdownNameSelectors<BitDropdownCustom, string> comboBoxNameSelectors = new()
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
        ValueSetter = (BitDropdownCustom item, string value) => item.Value = value,
        TextSetter = (string text, BitDropdownCustom item) => item.Text = text
    };


    private List<BitDropdownCustom> GetBasicCustoms() =>
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

    private List<BitDropdownCustom> GetDataCustoms() =>
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

    private ICollection<BitDropdownCustom>? virtualizeCustoms1;
    private ICollection<BitDropdownCustom>? virtualizeCustoms2;

    private List<BitDropdownCustom> GetRtlCustoms() =>
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
    private ICollection<BitDropdownCustom>? dropDirectionCustoms;
    private List<BitDropdownCustom> GetStyleClassCustoms() => new()
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
    private List<BitDropdownCustom> comboBoxCustoms = new()
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

    private BitDropdownCustom? changedItem;
    private BitDropdownCustom[] changedItems = Array.Empty<BitDropdownCustom>();

    private BitDropdownCustom? selectedItem1;
    private BitDropdownCustom? selectedItem2;

    private string clearValue = "f-app";
    private ICollection<string> clearValues = ["f-app", "f-ban"];

    private string successMessage = string.Empty;
    private FormValidationDropdownModel validationModel = new();

    private string comboBoxValueSample1 = default!;
    private string comboBoxValueSample2 = default!;
    private string comboBoxValueSample3 = default!;
    private string comboBoxValueSample4 = default!;
    private ICollection<string> comboBoxValues = [];

    protected override void OnInitialized()
    {
        virtualizeCustoms1 = Enumerable.Range(1, 10_000)
                                       .Select(c => new BitDropdownCustom { Text = $"Category {c}", Value = c.ToString() })
                                       .ToArray();

        virtualizeCustoms2 = Enumerable.Range(1, 10_000)
                                       .Select(c => new BitDropdownCustom { Text = $"Category {c}", Value = c.ToString() })
                                       .ToArray();

        dropDirectionCustoms = Enumerable.Range(1, 15)
                                         .Select(c => new BitDropdownCustom { Value = c.ToString(), Text = $"Category {c}" })
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

    private void HandleOnDynamicAdd(BitDropdownCustom item)
    {
        comboBoxCustoms.Add(item);
    }

    private async ValueTask<BitDropdownItemsProviderResult<BitDropdownCustom>> LoadItems(
        BitDropdownItemsProviderRequest<BitDropdownCustom> request)
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

            var items = data!.Items!.Select(i => new BitDropdownCustom
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
            return BitDropdownItemsProviderResult.From(new List<BitDropdownCustom>(), 0);
        }
    }
}
