using AdminPanel.Shared.Dtos.Products;

namespace AdminPanel.Client.Shared.Pages;

public partial class CreateEditProductModal
{

    private bool _isOpen;
    private bool _isLoading;
    private bool _isSaveLoading;
    private List<BitDropDownItem>? _allCategoryList = new();
    private string SelectedCategoyId
    {
        get => (Product.CategoryId ?? 0).ToString();
        set { Product.CategoryId = int.Parse(value); }
    }

    [Parameter] public ProductDto Product { get; set; } = default!;

    [Parameter] public EventCallback OnSave { get; set; }

    protected override async Task OnInitAsync()
    {
        _allCategoryList = await GetCategoryDropdownItemsAsync();
    }

    public async Task ShowModal(ProductDto product)
    {
        await InvokeAsync(() =>
        {
            _isOpen = true;

            _ = JsRuntime.SetToggleBodyOverflow(true);

            Product = product;
        });
    }

    private async Task<List<BitDropDownItem>> GetCategoryDropdownItemsAsync()
    {
        _isLoading = true;

        try
        {
            var categoryList = await StateService.GetValue($"{nameof(ProductsPage)}-AllCategoryList",
                                        async () => await HttpClient.GetFromJsonAsync("Category/Get",
                                            AppJsonContext.Default.ListCategoryDto)) ?? new();

            return categoryList.Select(c => new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = c.Name ?? string.Empty,
                Value = c.Id.ToString()
            }).ToList();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task Save()
    {
        if (_isLoading) return;

        _isSaveLoading = true;

        try
        {
            if (Product.Id == 0)
            {
                await HttpClient.PostAsJsonAsync("Product/Create", Product, AppJsonContext.Default.ProductDto);
            }
            else
            {
                await HttpClient.PutAsJsonAsync("Product/Update", Product, AppJsonContext.Default.ProductDto);
            }

            _isOpen = false;

            await JsRuntime.SetToggleBodyOverflow(false);
        }
        finally
        {
            _isSaveLoading = false;

            await OnSave.InvokeAsync();
        }
    }

    private async Task OnCloseClick()
    {
        _isOpen = false;

        await JsRuntime.SetToggleBodyOverflow(false);
    }
}
