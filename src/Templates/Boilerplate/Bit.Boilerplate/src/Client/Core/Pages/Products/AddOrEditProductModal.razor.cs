using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Client.Core.Pages.Products;

public partial class AddOrEditProductModal
{

    private bool isOpen;
    private bool isLoading;
    private bool isSaving;
    private ProductDto product = new();
    private List<BitDropdownItem<string>> allCategoryList = [];
    private string selectedCategoyId = string.Empty;

    [Parameter] public EventCallback OnSave { get; set; }

    protected override async Task OnInitAsync()
    {
        await LoadAllCategoriesAsync();
    }

    public async Task ShowModal(ProductDto productToShow)
    {
        await InvokeAsync(() =>
        {
            _ = JSRuntime.SetBodyOverflow(true);

            isOpen = true;
            product = productToShow;
            selectedCategoyId = (productToShow.CategoryId ?? 0).ToString();

            StateHasChanged();
        });
    }

    private async Task LoadAllCategoriesAsync()
    {
        isLoading = true;

        try
        {
            var categoryList = await PrerenderStateService.GetValue($"{nameof(ProductsPage)}-AllCategoryList",
                                        async () => await HttpClient.GetFromJsonAsync("Category/Get",
                                            AppJsonContext.Default.ListCategoryDto)) ?? [];

            allCategoryList = categoryList.Select(c => new BitDropdownItem<string>()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = c.Name ?? string.Empty,
                Value = c.Id.ToString()
            }).ToList();
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task Save()
    {
        if (isLoading) return;

        isSaving = true;

        try
        {
            if (product.Id == 0)
            {
                await HttpClient.PostAsJsonAsync("Product/Create", product, AppJsonContext.Default.ProductDto);
            }
            else
            {
                await HttpClient.PutAsJsonAsync("Product/Update", product, AppJsonContext.Default.ProductDto);
            }

            isOpen = false;

            await JSRuntime.SetBodyOverflow(false);
        }
        finally
        {
            isSaving = false;

            await OnSave.InvokeAsync();
        }
    }

    private async Task OnCloseClick()
    {
        isOpen = false;

        await JSRuntime.SetBodyOverflow(false);
    }
}
