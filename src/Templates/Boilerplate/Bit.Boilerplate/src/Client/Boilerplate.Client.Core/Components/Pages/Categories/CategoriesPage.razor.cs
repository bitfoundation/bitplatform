﻿//-:cnd:noEmit
using Boilerplate.Shared.Dtos.Categories;

namespace Boilerplate.Client.Core.Components.Pages.Categories;

[Authorize]
public partial class CategoriesPage
{
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
                // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview
                var query = new Dictionary<string, object?>()
                {
                    { "$top", req.Count ?? 10 },
                    { "$skip", req.StartIndex }
                };

                if (string.IsNullOrEmpty(categoryNameFilter) is false)
                {
                    query.Add("$filter", $"contains(Name,'{categoryNameFilter}')");
                }

                if (req.GetSortByProperties().Any())
                {
                    query.Add("$orderby", string.Join(", ", req.GetSortByProperties().Select(p => $"{p.PropertyName} {(p.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}")));
                }

                var url = NavigationManager.GetUriWithQueryParameters("Category/GetCategories", query);

                var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultCategoryDto, CurrentCancellationToken) ?? new();

                return BitDataGridItemsProviderResult.From(await data.Items!.ToListAsync(CurrentCancellationToken)!, (int)data.TotalCount);
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
        NavigationManager.NavigateTo("add-edit-category");
    }

    private void EditCategory(CategoryDto category)
    {
        NavigationManager.NavigateTo($"add-edit-category/{category.Id}");
    }

    private async Task DeleteCategory(CategoryDto category)
    {
        var confirmed = await confirmMessageBox.Show(Localizer.GetString(nameof(AppStrings.AreYouSureWannaDeleteCategory), category.Name ?? string.Empty),
                                                     Localizer[nameof(AppStrings.DeleteCategory)]);

        if (confirmed)
        {
            await HttpClient.DeleteAsync($"Category/Delete/{category.Id}", CurrentCancellationToken);

            await RefreshData();
        }
    }
}




