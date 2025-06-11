//-:cnd:noEmit
using Boilerplate.Shared.Dtos.Categories;
using Boilerplate.Shared.Controllers.Categories;

namespace Boilerplate.Client.Core.Components.Pages.Categories;

public partial class CategoriesPage
{
    [AutoInject] ICategoryController categoryController = default!;

    private bool isLoading;
    private bool isDeleteDialogOpen;
    private CategoryDto? deletingCategory;
    private AddOrEditCategoryModal? modal;
    private BitDataGrid<CategoryDto>? dataGrid;
    private string categoryNameFilter = string.Empty;
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
        await base.OnInitAsync();

        PrepareGridDataProvider();
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

                var data = await categoryController.WithQuery(odataQ.ToString()).GetCategories(req.CancellationToken);

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

    private async Task CreateCategory()
    {
        await modal!.ShowModal(new CategoryDto());
    }

    private async Task EditCategory(CategoryDto category)
    {
        await modal!.ShowModal(category);
    }

    private async Task DeleteCategory()
    {
        if (deletingCategory is null) return;

        try
        {
            await categoryController.Delete(deletingCategory.Id, deletingCategory.ConcurrencyStamp.ToStampString(), CurrentCancellationToken);

            await RefreshData();
        }
        catch (KnownException exp)
        {
            SnackBarService.Error(exp.Message);
        }
        finally
        {
            deletingCategory = null;
        }
    }
}




