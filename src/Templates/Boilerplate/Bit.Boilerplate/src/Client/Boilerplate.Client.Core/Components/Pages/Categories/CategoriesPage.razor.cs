//-:cnd:noEmit
using Boilerplate.Shared.Controllers.Categories;
using Boilerplate.Shared.Dtos.Categories;

namespace Boilerplate.Client.Core.Components.Pages.Categories;

[Authorize]
public partial class CategoriesPage
{
    protected override string? Title => Localizer[nameof(AppStrings.Categories)];
    protected override string? Subtitle => string.Empty;

    [AutoInject] ICategoryController categoryController = default!;

    private bool isLoading;
    private string categoryNameFilter = string.Empty;

    private ConfirmMessageBox confirmMessageBox = default!;
    private BitDataGrid<CategoryDto>? dataGrid;
    private BitDataGridItemsProvider<CategoryDto> categoriesProvider = default!;
    private BitDataGridPaginationState pagination = new() { ItemsPerPage = 10 };

    private string CategoryNameFilter
    {
        get => categoryNameFilter;
        set
        {
            categoryNameFilter = value;
            _ = RefreshData();
        }
    }

    protected override async Task OnInitAsync()
    {
        PrepareGridDataProvider();

        await base.OnInitAsync();
    }

    private void PrepareGridDataProvider()
    {
        categoriesProvider = async req =>
        {
            isLoading = true;

            try
            {
                var odataQ = new ODataQuery
                {
                    Top = req.Count ?? 10,
                    Skip = req.StartIndex,
                    OrderBy = string.Join(", ", req.GetSortByProperties().Select(p => $"{p.PropertyName} {(p.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}"))
                };

                if (string.IsNullOrEmpty(CategoryNameFilter) is false)
                {
                    odataQ.Filter = $"contains(tolower({nameof(CategoryDto.Name)}),'{CategoryNameFilter.ToLower()}')";
                }

                categoryController.AddQueryString(odataQ.ToString());

                var data = await categoryController.GetCategories(CurrentCancellationToken);

                return BitDataGridItemsProviderResult.From(data!.Items!, (int)data!.TotalCount);
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
                return BitDataGridItemsProviderResult.From(new List<CategoryDto> { }, 0);
            }
            finally
            {
                isLoading = false;

                StateHasChanged();
            }
        };
    }

    private async Task RefreshData()
    {
        await dataGrid!.RefreshDataAsync();
    }

    private void CreateCategory()
    {
        NavigationManager.NavigateTo(Urls.AddOrEditCategoryPage);
    }

    private void EditCategory(CategoryDto category)
    {
        NavigationManager.NavigateTo($"{Urls.AddOrEditCategoryPage}/{category.Id}");
    }

    private async Task DeleteCategory(CategoryDto category)
    {
        var confirmed = await confirmMessageBox.Show(Localizer.GetString(nameof(AppStrings.AreYouSureWannaDeleteCategory), category.Name ?? string.Empty),
                                                     Localizer[nameof(AppStrings.DeleteCategory)]);

        if (confirmed)
        {
            await categoryController.Delete(category.Id, CurrentCancellationToken);

            await RefreshData();
        }
    }
}




