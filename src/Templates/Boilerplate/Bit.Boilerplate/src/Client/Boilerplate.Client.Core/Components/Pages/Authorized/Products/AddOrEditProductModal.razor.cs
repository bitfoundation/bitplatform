using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Controllers.Products;
using Boilerplate.Shared.Controllers.Categories;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Products;

public partial class AddOrEditProductModal
{
    [AutoInject] ICategoryController categoryController = default!;
    [AutoInject] IProductController productController = default!;

    private bool isOpen;
    private bool isSaving;
    private bool isLoading;
    private ProductDto product = new();
    private string selectedCategoryId = string.Empty;
    private List<BitDropdownItem<string>> allCategoryList = [];
    private AppDataAnnotationsValidator validatorRef = default!;

    [Parameter] public EventCallback OnSave { get; set; }

    protected override async Task OnInitAsync()
    {
        await LoadAllCategories();
    }

    public async Task ShowModal(ProductDto productToShow)
    {
        await InvokeAsync(() =>
        {
            isOpen = true;
            productToShow.Patch(product);
            selectedCategoryId = (product.CategoryId ?? default).ToString();

            StateHasChanged();
        });
    }

    private async Task LoadAllCategories()
    {
        isLoading = true;

        try
        {
            var categoryList = await categoryController.Get(CurrentCancellationToken);

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
            if (product.Id == default)
            {
                await productController.Create(product, CurrentCancellationToken);
            }
            else
            {
                await productController.Update(product, CurrentCancellationToken);
            }

            isOpen = false;
        }
        catch (ResourceValidationException exp)
        {
            validatorRef.DisplayErrors(exp);
        }
        finally
        {
            isSaving = false;

            await OnSave.InvokeAsync();
        }
    }

    private async Task CloseModal()
    {
        isOpen = false;
    }
}
