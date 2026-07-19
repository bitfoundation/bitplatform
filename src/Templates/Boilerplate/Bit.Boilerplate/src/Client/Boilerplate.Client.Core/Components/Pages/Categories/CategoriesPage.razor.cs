//-:cnd:noEmit
using Boilerplate.Shared.Features.Categories;

namespace Boilerplate.Client.Core.Components.Pages.Categories;

public partial class CategoriesPage
{
    private bool isLoading;
    private bool isDeleteDialogOpen;
    private CategoryDto? deletingCategory;
    private AddOrEditCategoryModal? modal;

    private BitDataGrid<CategoryDto>? dataGrid;


    [AutoInject] ICategoryController categoryController = default!;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();
    }


    private async Task<BitDataGridReadResult<CategoryDto>> LoadCategories(BitDataGridReadRequest req)
    {
        isLoading = true;
        StateHasChanged();

        try
        {
            var query = new ODataQuery
            {
                Top = req.Take,
                Skip = req.Skip,
                OrderBy = req.Sorts.Count > 0
                    ? string.Join(", ", req.Sorts.Select(s => $"{s.ColumnId} {(s.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}"))
                    : $"{nameof(CategoryDto.Name)} asc"
            };

            var filter = string.Join(" and ", req.Filters
                .Where(f => string.IsNullOrEmpty(f.Value?.ToString()) is false)
                .Select(f => $"contains(tolower({f.ColumnId}),'{f.Value!.ToString()!.ToLower().Replace("'", "''")}')"));
            if (string.IsNullOrEmpty(filter) is false)
            {
                query.Filter = filter;
            }

            var data = await categoryController.WithQuery(query.ToString())
                                               .GetCategories(req.CancellationToken);

            return new BitDataGridReadResult<CategoryDto>(data!.Items!, (int)data!.TotalCount);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
            return new BitDataGridReadResult<CategoryDto>([], 0);
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task RefreshData()
    {
        await dataGrid!.RefreshAsync();
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
            await categoryController.Delete(deletingCategory.Id, deletingCategory.Version, CurrentCancellationToken);

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
