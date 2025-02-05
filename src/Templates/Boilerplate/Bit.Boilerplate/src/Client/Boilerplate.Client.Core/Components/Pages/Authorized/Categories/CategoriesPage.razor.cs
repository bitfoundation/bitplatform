//-:cnd:noEmit
using Boilerplate.Shared.Controllers.Categories;
using Boilerplate.Shared.Dtos.Categories;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Categories;

public partial class CategoriesPage
{
    protected override string? Title => Localizer[nameof(AppStrings.Categories)];
    protected override string? Subtitle => string.Empty;

    [AutoInject] ICategoryController categoryController = default!;

    private bool isLoading;
    private bool isDeleteDialogOpen;
    private CategoryDto? deletingCategory;
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

                var data = await categoryController.WithQueryString(odataQ.ToString()).GetCategories(req.CancellationToken);

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




