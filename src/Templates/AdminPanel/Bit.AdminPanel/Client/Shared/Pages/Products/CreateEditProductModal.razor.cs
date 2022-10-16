using AdminPanel.Shared.Dtos.Products;

namespace AdminPanel.Client.Shared.Pages.Products;

public partial class CreateEditProductModal
{
    [Parameter]
    public ProductDto Product { get; set; }
    [Parameter]
    public EventCallback<MouseEventArgs> OnSave { get; set; }

    private bool IsOpen { get; set; }
    public bool IsLoading { get; private set; }
    public bool IsSaveLoading { get; private set; }

    public List<BitDropDownItem>? AllCategoryList { get; set; } = new();

    public string SelectedCategoyId { get => Product!.CategoryId!.ToString(); set { Product.CategoryId = int.Parse(value); } }

    protected override async Task OnInitAsync()
    {
        AllCategoryList = await GetCategoryDropdownItemsAsync();

        await base.OnInitAsync();
    }

    public async Task ShowModal(ProductDto product)
    {
        await InvokeAsync(async () =>
        {
            IsOpen = true;
            await JsRuntime.SetToggleBodyOverflow(true);

            Product = product;

            StateHasChanged();
        });
    }

    private async Task<List<BitDropDownItem>> GetCategoryDropdownItemsAsync()
    {
        IsLoading = true;

        try
        {
            var categoryList = await StateService.GetValue($"{nameof(ProductsPage)}-{nameof(AllCategoryList)}", async () => await HttpClient.GetFromJsonAsync("Category/Get", AppJsonContext.Default.ListCategoryDto));

            return categoryList!.Select(c => new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = c.Name!,
                Value = c.Id!.ToString()
            }).ToList();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task Save()
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsSaveLoading = true;

            if (Product.Id == 0)
            {
                await HttpClient.PostAsJsonAsync("Product/Create", Product, AppJsonContext.Default.ProductDto);
            }
            else
            {
                await HttpClient.PutAsJsonAsync("Product/Update", Product, AppJsonContext.Default.ProductDto);
            }

            IsOpen = false;
            await JsRuntime.SetToggleBodyOverflow(false);
        }
        finally
        {
            await OnClickCallback.InvokeAsync();
            IsSaveLoading = false;
        }
    }

    private async Task OnCloseClick()
    {
        IsOpen = false;
        await JsRuntime.SetToggleBodyOverflow(false);
    }
}
